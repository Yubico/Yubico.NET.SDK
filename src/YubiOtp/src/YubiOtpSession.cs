// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.YubiOtp.Backend;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Session for the YubiOTP application. Supports dual transport (SmartCard and OTP HID).
/// Use <see cref="CreateAsync"/> factory method to create instances.
/// </summary>
public sealed class YubiOtpSession : ApplicationSession, IYubiOtpSession
{
    private static readonly Feature FeatureSerial = new("Serial Number Read", 2, 2, 0);
    private static readonly Feature FeatureHmacSha1 = new("HMAC-SHA1 Challenge-Response", 2, 2, 0);
    private static readonly Feature FeatureYubicoOtpChallengeResponse = new("Yubico OTP Challenge-Response", 2, 2, 0);
    private static readonly Feature FeatureUpdate = new("Slot Update", 2, 2, 0);
    private static readonly Feature FeatureSwap = new("Slot Swap", 2, 3, 0);
    private static readonly Feature FeatureNdef = new("NDEF Configuration", 3, 0, 0);
    private static readonly Feature FeatureScanMap = new("Keyboard scan map", 3, 0, 0);
    private const int ScanMapSize = 45;
    private static readonly FirmwareVersion AccessCodeUpdateRestrictedFrom = new(4, 3, 2);
    private static readonly FirmwareVersion AccessCodeUpdateRestrictedUntil = new(4, 3, 6);

    /// <summary>
    /// NFC Forum URI prefix table (36 entries). Index 0 means no prefix compression.
    /// Matching Python's NDEF_URL_PREFIXES.
    /// </summary>
    internal static readonly string[] NdefUriPrefixes =
    [
        "",
        "http://www.",
        "https://www.",
        "http://",
        "https://",
        "tel:",
        "mailto:",
        "ftp://anonymous:anonymous@",
        "ftp://ftp.",
        "ftps://",
        "sftp://",
        "smb://",
        "nfs://",
        "ftp://",
        "dav://",
        "news:",
        "telnet://",
        "imap:",
        "rtsp://",
        "urn:",
        "pop:",
        "sip:",
        "sips:",
        "tftp:",
        "btspp://",
        "btl2cap://",
        "btgoep://",
        "tcpobex://",
        "irdaobex://",
        "file://",
        "urn:epc:id:",
        "urn:epc:tag:",
        "urn:epc:pat:",
        "urn:epc:raw:",
        "urn:epc:",
        "urn:nfc:"
    ];

    private readonly ILogger _logger;

    private IYubiOtpBackend _backend = null!;
    private ReadOnlyMemory<byte> _status;
    private bool _admitAllFirmwareGates;

    private YubiOtpSession(
        IConnection connection,
        IUserPresencePrompt? userPresencePrompt = null)
        : base(EnsureSupportedConnection(connection), userPresencePrompt)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _logger = Logger;
    }

    private static IConnection EnsureSupportedConnection(IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection is ISmartCardConnection or IOtpHidConnection
            ? connection
            : throw new NotSupportedException(
                $"Connection type {connection.GetType().Name} is not supported by YubiOtpSession. " +
                "Supported types: ISmartCardConnection, IOtpHidConnection.");
    }

    /// <summary>
    /// Creates and initializes a new YubiOTP session.
    /// </summary>
    /// <param name="connection">An open SmartCard or OTP HID connection, borrowed and not disposed by this session.</param>
    /// <param name="options">Optional firmware/protocol, presence-prompt and secure-channel settings. SCP is unsupported on OTP HID.</param>
    /// <param name="cancellationToken">A token for session creation.</param>
    /// <returns>A caller-owned session. Dispose it before disposing the caller-owned connection.</returns>
    /// <remarks>
    /// Initialization reads status; support for a transport does not guarantee every command on every
    /// device/interface. Use the device extension when the session should own its hidden connection.
    /// </remarks>
    public static Task<YubiOtpSession> CreateAsync(
        IConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default) =>
        CreateUsingCachedInfoAsync(connection, options, null, cancellationToken);

    internal static Task<YubiOtpSession> CreateUsingCachedInfoAsync(
        IConnection connection,
        SessionCreationOptions? options,
        DeviceInfo? cachedDeviceInfo,
        CancellationToken cancellationToken) =>
        ConstructInitializedAsync(
            connection,
            options,
            () => new YubiOtpSession(connection, options?.UserPresencePrompt),
            (session, token) => session.InitializeAsync(
                options, cachedDeviceInfo, token),
            cancellationToken);

    private async Task InitializeAsync(
        SessionCreationOptions? options,
        DeviceInfo? cachedDeviceInfo,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
        {
            return;
        }

        var scpKeyParams = options?.ScpKeyParameters;
        var cachedVersion = cachedDeviceInfo?.FirmwareVersion;
        var protocol = await PrepareProtocolAsync(scpKeyParams, cancellationToken).ConfigureAwait(false);
        var initialization = protocol switch
        {
            ISmartCardProtocol smartCard =>
                await SmartCardBackend.ReadInitialStateAsync(smartCard, cancellationToken, cachedVersion).ConfigureAwait(false),
            IOtpHidProtocol otp =>
                await HidBackend.ReadInitialStateAsync(otp, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException("Unsupported OTP protocol.")
        };
        _status = initialization.Status;

        var (firmwareForProtocol, admitAllFirmwareGates) = ResolveFirmware(
            options?.FirmwareVersionOverride, cachedDeviceInfo, initialization);
        _admitAllFirmwareGates = admitAllFirmwareGates;

        var effectiveProtocol = await ConfigureProtocolAsync(
                protocol,
                firmwareForProtocol,
                options?.ProtocolConfiguration,
                scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        _backend = CreateBackend(effectiveProtocol, FirmwareVersion, GetProgSeq());

        _logger.LogDebug("YubiOTP session initialized with protocol {ProtocolType}", effectiveProtocol.GetType().Name);
    }

    private static (FirmwareVersion ProtocolVersion, bool AdmitAllPreflight) ResolveFirmware(
        FirmwareVersion? firmwareOverride, DeviceInfo? cachedDeviceInfo, YubiOtpInitialization initialization)
    {
        if (firmwareOverride is not null)
        {
            return (firmwareOverride, firmwareOverride.IsAlphaOrBeta);
        }

        var cachedVersion = cachedDeviceInfo?.FirmwareVersion;
        var version = cachedVersion ?? initialization.FirmwareVersion;
        var protocolVersion = version.IsAlphaOrBeta
            ? initialization.ProtocolVersion ?? initialization.FirmwareVersion
            : version;
        bool prerelease = cachedVersion is not null &&
                          cachedDeviceInfo?.VersionQualifier?.Type is { } type && type != VersionQualifierType.Final;
        return (protocolVersion, version.IsAlphaOrBeta || prerelease);
    }

    /// <inheritdoc />
    public Task<int> GetSerialNumberAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureOtpSupports(FeatureSerial);

        return GetSerialCoreAsync(cancellationToken);
    }

    private async Task<int> GetSerialCoreAsync(CancellationToken cancellationToken)
    {
        var response = await _backend.SendAndReceiveAsync(
                ConfigSlot.DeviceSerial,
                ReadOnlyMemory<byte>.Empty,
                4,
                UserPresenceNotification.None,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Serial is big-endian 4 bytes
        var span = response.Span;
        return (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];
    }

    /// <inheritdoc />
    public ConfigState GetConfigState()
    {
        ThrowIfDisposed();
        return new ConfigState(_status.Span);
    }

    /// <inheritdoc />
    public Task PutConfigurationAsync(
        Slot slot,
        SlotConfiguration config,
        ReadOnlyMemory<byte> accessCode = default,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(config);

        if (!_admitAllFirmwareGates && !config.IsSupportedBy(FirmwareVersion))
        {
            throw new NotSupportedException(
                $"This configuration requires firmware {config.MinimumFirmwareVersion}+, " +
                $"but device has {FirmwareVersion}.");
        }

        var configSlot = slot.Map(SlotOperation.Configure);
        return WriteConfigAsync(configSlot, config, accessCode, currentAccessCode, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateConfigurationAsync(
        Slot slot,
        UpdateConfiguration config,
        ReadOnlyMemory<byte> accessCode = default,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(config);
        EnsureOtpSupports(FeatureUpdate);

        if (!_admitAllFirmwareGates && !config.IsSupportedBy(FirmwareVersion))
        {
            throw new NotSupportedException(
                $"This configuration requires firmware {config.MinimumFirmwareVersion}+, " +
                $"but device has {FirmwareVersion}.");
        }

        var configSlot = slot.Map(SlotOperation.Update);
        if (!_admitAllFirmwareGates && FirmwareVersion >= AccessCodeUpdateRestrictedFrom &&
            FirmwareVersion < AccessCodeUpdateRestrictedUntil &&
            !CryptographicOperations.FixedTimeEquals(accessCode.Span, currentAccessCode.Span))
        {
            throw new NotSupportedException("Updating the access code is not supported on firmware 4.3.2–4.3.5.");
        }

        return WriteConfigAsync(configSlot, config, accessCode, currentAccessCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SwapSlotsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureOtpSupports(FeatureSwap);

        _status = await _backend.WriteUpdateAsync(
                ConfigSlot.Swap,
                ReadOnlyMemory<byte>.Empty,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteSlotAsync(
        Slot slot,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var configSlot = slot.Map(SlotOperation.Configure);
        var data = BuildPayloadWithAccessCode(new byte[YubiOtpConstants.ConfigSize], currentAccessCode);

        try
        {
            _status = await _backend.WriteUpdateAsync(configSlot, data, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <inheritdoc />
    public Task SetScanMapAsync(
        ReadOnlyMemory<byte> scanMap,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureOtpSupports(FeatureScanMap);

        if (scanMap.Length != ScanMapSize)
        {
            throw new ArgumentException(
                $"Scan map must be exactly {ScanMapSize} bytes, got {scanMap.Length}.",
                nameof(scanMap));
        }

        return WriteScanMapAsync(scanMap, currentAccessCode, cancellationToken);
    }

    /// <inheritdoc />
    public Task ResetScanMapAsync(ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureOtpSupports(FeatureScanMap);
        // Personalization v1.20.0's -S without an argument sends 45 zero bytes.
        return WriteScanMapAsync(new byte[ScanMapSize], currentAccessCode, cancellationToken);
    }

    private async Task WriteScanMapAsync(ReadOnlyMemory<byte> scanMap, ReadOnlyMemory<byte> currentAccessCode,
        CancellationToken cancellationToken)
    {
        var data = BuildPayloadWithAccessCode(scanMap, currentAccessCode);

        try
        {
            _status = await _backend.WriteUpdateAsync(ConfigSlot.ScanMap, data, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <inheritdoc />
    public Task SetNdefConfigurationAsync(
        Slot slot,
        string uri,
        ReadOnlyMemory<byte> currentAccessCode = default,
        NdefType ndefType = NdefType.Uri,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(uri);
        EnsureOtpSupports(FeatureNdef);

        return WriteNdefAsync(slot.Map(SlotOperation.Ndef), BuildNdefPayload(uri, ndefType), currentAccessCode,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DisableNdefAsync(
        Slot slot,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureOtpSupports(FeatureNdef);
        return WriteNdefAsync(slot.Map(SlotOperation.Ndef), new byte[YubiOtpConstants.NdefDataSize + 2],
            currentAccessCode, cancellationToken);
    }

    private async Task WriteNdefAsync(ConfigSlot configSlot, byte[] payload, ReadOnlyMemory<byte> currentAccessCode,
        CancellationToken cancellationToken)
    {
        var data = BuildPayloadWithAccessCode(payload, currentAccessCode);

        try
        {
            _status = await _backend.WriteUpdateAsync(configSlot, data, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <inheritdoc />
    public Task<Memory<byte>> CalculateHmacSha1Async(
        Slot slot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureOtpSupports(FeatureHmacSha1);

        if (challenge.Length > YubiOtpConstants.HmacChallengeSize)
        {
            throw new ArgumentException(
                $"Challenge must be at most {YubiOtpConstants.HmacChallengeSize} bytes, got {challenge.Length}.",
                nameof(challenge));
        }

        var configSlot = slot.Map(SlotOperation.ChallengeHmac);
        return CalculateHmacSha1CoreAsync(configSlot, challenge, cancellationToken);
    }

    private async Task<Memory<byte>> CalculateHmacSha1CoreAsync(
        ConfigSlot configSlot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken)
    {
        var paddedChallenge = PadHmacChallenge(challenge.Span);

        try
        {
            var response = await _backend.SendAndReceiveAsync(
                    configSlot,
                    paddedChallenge,
                    YubiOtpConstants.HmacResponseSize,
                    CreateUserPresenceNotification(CreateUserPresenceContext(configSlot, UserPresenceOperations.YubiOtp.CalculateHmacSha1)),
                    cancellationToken)
                .ConfigureAwait(false);
            // Both backends transfer fresh SDK-owned storage, never the caller's input.
            return System.Runtime.InteropServices.MemoryMarshal.AsMemory(response);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(paddedChallenge);
        }
    }

    /// <inheritdoc />
    public Task<ReadOnlyMemory<byte>> CalculateYubicoOtpAsync(
        Slot slot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureOtpSupports(FeatureYubicoOtpChallengeResponse);

        if (challenge.Length != YubiOtpConstants.YubicoOtpChallengeSize)
        {
            throw new ArgumentException(
                $"Challenge must be exactly {YubiOtpConstants.YubicoOtpChallengeSize} bytes, got {challenge.Length}.",
                nameof(challenge));
        }

        var configSlot = slot.Map(SlotOperation.ChallengeYubicoOtp);
        return CalculateYubicoOtpCoreAsync(configSlot, challenge, cancellationToken);
    }

    private async Task<ReadOnlyMemory<byte>> CalculateYubicoOtpCoreAsync(
        ConfigSlot configSlot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken)
    {
        return await _backend.SendAndReceiveAsync(
                configSlot,
                challenge,
                YubiOtpConstants.YubicoOtpResponseSize,
                CreateUserPresenceNotification(CreateUserPresenceContext(configSlot, UserPresenceOperations.YubiOtp.CalculateYubicoOtp)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Pads the HMAC challenge to 64 bytes with a byte that differs from its last byte, the same rule
    /// as ykman: 0x01 after a trailing 0x00, otherwise 0x00 (so an empty challenge pads with 0x00).
    /// In short-challenge mode the YubiKey reads the final byte as the end-of-input marker and discards
    /// the run of equal bytes before it, which is why the pad byte must not equal the last data byte.
    /// </summary>
    internal static byte[] PadHmacChallenge(ReadOnlySpan<byte> challenge)
    {
        var padded = new byte[YubiOtpConstants.HmacChallengeSize];
        challenge.CopyTo(padded);

        if (challenge.Length < YubiOtpConstants.HmacChallengeSize)
        {
            byte padByte = challenge.Length > 0 && challenge[^1] == 0 ? (byte)1 : (byte)0;
            padded.AsSpan(challenge.Length).Fill(padByte);
        }

        return padded;
    }

    /// <summary>
    /// Builds the 56-byte NDEF payload: [length][type][data (54 bytes, zero-padded)].
    /// </summary>
    internal static byte[] BuildNdefPayload(string content, NdefType ndefType)
    {
        if (ndefType is not (NdefType.Uri or NdefType.Text))
        {
            throw new ArgumentOutOfRangeException(nameof(ndefType), ndefType, "Expected URI or text NDEF type.");
        }

        const int payloadSize = YubiOtpConstants.NdefDataSize + 2; // 56 bytes total
        var payload = new byte[payloadSize];

        payload[1] = (byte)ndefType;

        if (ndefType == NdefType.Uri)
        {
            BuildNdefUri(content, payload);
        }
        else
        {
            BuildNdefText(content, payload);
        }

        return payload;
    }

    private static void BuildNdefUri(string uri, byte[] payload)
    {
        // Find the longest matching prefix
        int bestPrefixIndex = 0;
        int bestPrefixLength = 0;

        for (int i = 1; i < NdefUriPrefixes.Length; i++)
        {
            var prefix = NdefUriPrefixes[i];
            if (uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && prefix.Length > bestPrefixLength)
            {
                bestPrefixIndex = i;
                bestPrefixLength = prefix.Length;
            }
        }

        var remaining = uri[bestPrefixLength..];
        var remainingBytes = Encoding.UTF8.GetBytes(remaining);

        // 1 byte for prefix index + remaining URI bytes must fit in the data area
        int totalDataLength = remainingBytes.Length + 1;
        if (totalDataLength > YubiOtpConstants.NdefDataSize)
        {
            throw new ArgumentException(
                $"URI content exceeds the maximum NDEF data size of {YubiOtpConstants.NdefDataSize} bytes. " +
                $"After prefix compression, the URI requires {totalDataLength} bytes.",
                nameof(uri));
        }

        // payload[0] = length of NDEF data (prefix index byte + remaining URI bytes)
        payload[0] = (byte)totalDataLength;

        // First byte of data area is the URI prefix index
        payload[2] = (byte)bestPrefixIndex;

        // Copy remaining URI bytes
        Array.Copy(remainingBytes, 0, payload, 3, remainingBytes.Length);
    }

    private static void BuildNdefText(string text, byte[] payload)
    {
        // Text record: [language_length=0x02]["en"][text_content]
        const byte languageLength = 0x02;
        ReadOnlySpan<byte> langCode = "en"u8;
        var textBytes = Encoding.UTF8.GetBytes(text);

        // 1 byte for language length + 2 bytes for "en" + text bytes must fit in the data area
        int totalDataLength = 1 + langCode.Length + textBytes.Length;
        if (totalDataLength > YubiOtpConstants.NdefDataSize)
        {
            int maxTextBytes = YubiOtpConstants.NdefDataSize - 1 - langCode.Length;
            throw new ArgumentException(
                $"Text content exceeds the maximum NDEF data size of {YubiOtpConstants.NdefDataSize} bytes. " +
                $"The text requires {totalDataLength} bytes (including language header), " +
                $"but only {maxTextBytes} bytes are available for text content.",
                nameof(text));
        }

        payload[0] = (byte)totalDataLength;

        // Language header
        payload[2] = languageLength;
        langCode.CopyTo(payload.AsSpan(3));

        // Text content
        textBytes.AsSpan().CopyTo(payload.AsSpan(3 + langCode.Length));
    }

    private async Task WriteConfigAsync(
        ConfigSlot configSlot,
        SlotConfiguration config,
        ReadOnlyMemory<byte> accessCode,
        ReadOnlyMemory<byte> currentAccessCode,
        CancellationToken cancellationToken)
    {
        ValidateCurrentAccessCode(currentAccessCode);
        var configBytes = config.GetConfig(accessCode.Span);
        try
        {
            var data = BuildPayloadWithAccessCode(configBytes, currentAccessCode);
            try
            {
                _status = await _backend.WriteUpdateAsync(configSlot, data, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(data);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(configBytes);
        }
    }

    /// <summary>
    /// Appends the current authentication code (6 bytes) to the payload used by either backend.
    /// </summary>
    private static byte[] BuildPayloadWithAccessCode(
        ReadOnlyMemory<byte> data,
        ReadOnlyMemory<byte> currentAccessCode)
    {
        if (currentAccessCode.IsEmpty)
        {
            return data.ToArray();
        }

        ValidateCurrentAccessCode(currentAccessCode);
        var currentCode = currentAccessCode.Span;
        var payload = data.Span;
        var result = new byte[data.Length + YubiOtpConstants.AccessCodeSize];
        payload.CopyTo(result);
        currentCode.CopyTo(result.AsSpan(data.Length));
        return result;
    }

    private static void ValidateCurrentAccessCode(ReadOnlyMemory<byte> currentAccessCode)
    {
        if (!currentAccessCode.IsEmpty && currentAccessCode.Length != YubiOtpConstants.AccessCodeSize)
        {
            throw new ArgumentException(
                $"Access code must be exactly {YubiOtpConstants.AccessCodeSize} bytes, got {currentAccessCode.Length}.",
                nameof(currentAccessCode));
        }

    }

    private void EnsureOtpSupports(Feature feature)
    {
        if (!_admitAllFirmwareGates)
        {
            EnsureSupports(feature);
        }
    }

    private byte GetProgSeq()
    {
        if (_status.Length >= YubiOtpConstants.StatusBytesLength)
        {
            return _status.Span[3];
        }

        return 0;
    }

    private UserPresenceContext? CreateUserPresenceContext(ConfigSlot configSlot, string operation)
    {
        if (!IsUserPresenceNotificationEnabled || Protocol is not IOtpHidProtocol)
        {
            return null;
        }

        Slot slot = configSlot is ConfigSlot.ChalHmac1 or ConfigSlot.ChalYubico1
            ? Slot.One
            : Slot.Two;

        return new UserPresenceContext
        {
            Basis = UserPresenceBasis.DeviceWaiting,
            Application = UserPresenceApplications.YubiOtp,
            Operation = operation,
            Scope = slot.ToString()
        };
    }

    private static IYubiOtpBackend CreateBackend(
        IProtocol protocol,
        FirmwareVersion firmwareVersion,
        byte initialProgSeq) =>
        protocol switch
        {
            ISmartCardProtocol smartCard => new SmartCardBackend(
                smartCard,
                firmwareVersion,
                initialProgSeq),
            IOtpHidProtocol otpHid => new HidBackend(otpHid),
            _ => throw new NotSupportedException(
                $"Protocol type {protocol.GetType().Name} is not supported by YubiOtpSession. " +
                "Supported protocols: SmartCard and OTP HID.")
        };
}