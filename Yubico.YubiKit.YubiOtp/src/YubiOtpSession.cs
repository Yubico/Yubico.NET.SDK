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

using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Session for the YubiOTP application. Supports dual transport (SmartCard and OTP HID).
/// Use <see cref="CreateAsync"/> factory method to create instances.
/// </summary>
public sealed class YubiOtpSession : ApplicationSession, IYubiOtpSession
{
    private static readonly Feature FeatureSerial = new("Serial Number Read", 2, 2, 0);
    private static readonly Feature FeatureHmacSha1 = new("HMAC-SHA1 Challenge-Response", 2, 2, 0);
    private static readonly Feature FeatureUpdate = new("Slot Update", 2, 3, 0);
    private static readonly Feature FeatureSwap = new("Slot Swap", 2, 3, 0);
    private static readonly Feature FeatureNdef = new("NDEF Configuration", 3, 0, 0);

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
    private readonly ScpKeyParameters? _scpKeyParams;

    private IProtocol _protocol;
    private IYubiOtpBackend _backend;
    private ReadOnlyMemory<byte> _status;

    private YubiOtpSession(
        IConnection connection,
        ScpKeyParameters? scpKeyParams = null)
    {
        _scpKeyParams = scpKeyParams;
        _logger = Logger;

        (_protocol, _backend) = connection switch
        {
            ISmartCardConnection sc => CreateSmartCardBackend(sc),
            IOtpHidConnection otp => CreateOtpBackend(otp),
            _ => throw new NotSupportedException(
                $"Connection type {connection.GetType().Name} is not supported by YubiOtpSession. " +
                "Supported types: ISmartCardConnection, IOtpHidConnection.")
        };

        Protocol = _protocol;
    }

    /// <summary>
    /// Creates and initializes a new YubiOTP session.
    /// </summary>
    public static async Task<YubiOtpSession> CreateAsync(
        IConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        var session = new YubiOtpSession(connection, scpKeyParams);
        await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
        return session;
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
        {
            return;
        }

        FirmwareVersion version;

        if (_protocol is ISmartCardProtocol scProtocol)
        {
            version = await InitializeSmartCardAsync(scProtocol, cancellationToken).ConfigureAwait(false);
        }
        else if (_protocol is IOtpHidProtocol otpProtocol)
        {
            _status = await otpProtocol.ReadStatusAsync(cancellationToken).ConfigureAwait(false);
            version = otpProtocol.FirmwareVersion
                      ?? new FirmwareVersion(_status.Span[0], _status.Span[1], _status.Span[2]);
        }
        else
        {
            throw new InvalidOperationException("Unsupported protocol type.");
        }

        await InitializeCoreAsync(
                _protocol,
                version,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        _protocol = Protocol ?? throw new InvalidOperationException();

        if (IsAuthenticated)
        {
            // Recreate SmartCard backend with SCP-wrapped protocol
            var wrappedScProtocol = _protocol as ISmartCardProtocol
                                    ?? throw new InvalidOperationException(
                                        "SCP authentication succeeded but protocol is not ISmartCardProtocol.");
            _backend = new SmartCardBackend(
                wrappedScProtocol,
                FirmwareVersion,
                GetProgSeq());
        }

        _logger.LogDebug("YubiOTP session initialized with protocol {ProtocolType}", _protocol.GetType().Name);
    }

    private async Task<FirmwareVersion> InitializeSmartCardAsync(
        ISmartCardProtocol scProtocol,
        CancellationToken cancellationToken)
    {
        FirmwareVersion? managementVersion = null;

        // For NFC transport, SELECT management first for reliable version on NEO
        try
        {
            var mgmtResponse = await scProtocol
                .SelectAsync(ApplicationIds.Management, cancellationToken)
                .ConfigureAwait(false);

            var deviceText = Encoding.UTF8.GetString(mgmtResponse.Span);
            var versionString = deviceText.Split(' ').Last();
            var versionParts = versionString.Split('.').Select(int.Parse).ToArray();
            if (versionParts.Length == 3)
            {
                managementVersion = new FirmwareVersion(versionParts[0], versionParts[1], versionParts[2]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get version from Management SELECT, continuing with OTP SELECT.");
        }

        // SELECT OTP application
        var otpResponse = await scProtocol
            .SelectAsync(ApplicationIds.Otp, cancellationToken)
            .ConfigureAwait(false);

        _status = otpResponse;

        if (_status.Length < YubiOtpConstants.StatusBytesLength)
        {
            throw new BadResponseException(
                $"OTP SELECT returned {_status.Length} bytes, expected at least {YubiOtpConstants.StatusBytesLength}.");
        }

        var otpVersion = new FirmwareVersion(_status.Span[0], _status.Span[1], _status.Span[2]);

        // NEO workaround: use the higher of the two versions
        if (managementVersion is not null && otpVersion.Major == 3)
        {
            return managementVersion > otpVersion ? managementVersion : otpVersion;
        }

        return managementVersion ?? otpVersion;
    }

    public Task<int> GetSerialAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureSerial);

        return GetSerialCoreAsync(cancellationToken);
    }

    private async Task<int> GetSerialCoreAsync(CancellationToken cancellationToken)
    {
        var response = await _backend.SendAndReceiveAsync(
                ConfigSlot.DeviceSerial,
                ReadOnlyMemory<byte>.Empty,
                4,
                cancellationToken)
            .ConfigureAwait(false);

        // Serial is big-endian 4 bytes
        var span = response.Span;
        return (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];
    }

    public ConfigState GetConfigState()
    {
        ThrowIfDisposed();
        return new ConfigState(_status.Span);
    }

    public Task PutConfigurationAsync(
        Slot slot,
        SlotConfiguration config,
        ReadOnlyMemory<byte> accessCode = default,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(config);

        if (!config.IsSupportedBy(FirmwareVersion))
        {
            throw new NotSupportedException(
                $"This configuration requires firmware {config.MinimumFirmwareVersion}+, " +
                $"but device has {FirmwareVersion}.");
        }

        var configSlot = slot.Map(SlotOperation.Configure);
        return WriteConfigAsync(configSlot, config, accessCode, currentAccessCode, cancellationToken);
    }

    public Task UpdateConfigurationAsync(
        Slot slot,
        UpdateConfiguration config,
        ReadOnlyMemory<byte> accessCode = default,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(config);
        EnsureSupports(FeatureUpdate);

        if (!config.IsSupportedBy(FirmwareVersion))
        {
            throw new NotSupportedException(
                $"This configuration requires firmware {config.MinimumFirmwareVersion}+, " +
                $"but device has {FirmwareVersion}.");
        }

        var configSlot = slot.Map(SlotOperation.Update);
        return WriteConfigAsync(configSlot, config, accessCode, currentAccessCode, cancellationToken);
    }

    public async Task SwapSlotsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureSwap);

        _status = await _backend.WriteUpdateAsync(
                ConfigSlot.Swap,
                ReadOnlyMemory<byte>.Empty,
                cancellationToken)
            .ConfigureAwait(false);
    }

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

    public async Task SetScanMapAsync(
        ReadOnlyMemory<byte> scanMap,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (scanMap.Length != YubiOtpConstants.ScanCodesSize)
        {
            throw new ArgumentException(
                $"Scan map must be exactly {YubiOtpConstants.ScanCodesSize} bytes, got {scanMap.Length}.",
                nameof(scanMap));
        }

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

    public async Task SetNdefConfigurationAsync(
        Slot slot,
        string? uri = null,
        ReadOnlyMemory<byte> currentAccessCode = default,
        NdefType ndefType = NdefType.Uri,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureNdef);

        var configSlot = slot.Map(SlotOperation.Ndef);
        var ndefPayload = BuildNdefPayload(uri, ndefType);
        var data = BuildPayloadWithAccessCode(ndefPayload, currentAccessCode);

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

    public Task<ReadOnlyMemory<byte>> CalculateHmacSha1Async(
        Slot slot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureHmacSha1);

        if (challenge.Length > YubiOtpConstants.HmacChallengeSize)
        {
            throw new ArgumentException(
                $"Challenge must be at most {YubiOtpConstants.HmacChallengeSize} bytes, got {challenge.Length}.",
                nameof(challenge));
        }

        var configSlot = slot.Map(SlotOperation.ChallengeHmac);
        return CalculateHmacSha1CoreAsync(configSlot, challenge, cancellationToken);
    }

    private async Task<ReadOnlyMemory<byte>> CalculateHmacSha1CoreAsync(
        ConfigSlot configSlot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken)
    {
        var paddedChallenge = PadHmacChallenge(challenge.Span);

        try
        {
            return await _backend.SendAndReceiveAsync(
                    configSlot,
                    paddedChallenge,
                    YubiOtpConstants.HmacResponseSize,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(paddedChallenge);
        }
    }

    /// <summary>
    /// Pads the HMAC challenge to 64 bytes. The pad byte must differ from the last byte
    /// of the challenge to allow the YubiKey to detect the actual data length.
    /// </summary>
    internal static byte[] PadHmacChallenge(ReadOnlySpan<byte> challenge)
    {
        var padded = new byte[YubiOtpConstants.HmacChallengeSize];
        challenge.CopyTo(padded);

        if (challenge.Length < YubiOtpConstants.HmacChallengeSize)
        {
            byte lastByte = challenge.Length > 0 ? challenge[^1] : (byte)0;
            byte padByte = lastByte == 0 ? (byte)1 : (byte)0;

            padded.AsSpan(challenge.Length).Fill(padByte);
        }

        return padded;
    }

    /// <summary>
    /// Builds the 56-byte NDEF payload: [length][type][data (54 bytes, zero-padded)].
    /// </summary>
    internal static byte[] BuildNdefPayload(string? content, NdefType ndefType)
    {
        const int payloadSize = YubiOtpConstants.NdefDataSize + 2; // 56 bytes total
        var payload = new byte[payloadSize];

        if (content is null)
        {
            // Disable NDEF: all zeros
            return payload;
        }

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
            if (uri.StartsWith(prefix, StringComparison.Ordinal) && prefix.Length > bestPrefixLength)
            {
                bestPrefixIndex = i;
                bestPrefixLength = prefix.Length;
            }
        }

        var remaining = uri[bestPrefixLength..];
        var remainingBytes = Encoding.UTF8.GetBytes(remaining);

        int dataLength = Math.Min(remainingBytes.Length + 1, YubiOtpConstants.NdefDataSize);

        // payload[0] = length of NDEF data (prefix index byte + remaining URI bytes)
        payload[0] = (byte)dataLength;

        // First byte of data area is the URI prefix index
        payload[2] = (byte)bestPrefixIndex;

        // Copy remaining URI bytes
        int copyLength = Math.Min(remainingBytes.Length, YubiOtpConstants.NdefDataSize - 1);
        Array.Copy(remainingBytes, 0, payload, 3, copyLength);
    }

    private static void BuildNdefText(string text, byte[] payload)
    {
        // Text record: [language_length=0x02]["en"][text_content]
        const byte languageLength = 0x02;
        ReadOnlySpan<byte> langCode = "en"u8;
        var textBytes = Encoding.UTF8.GetBytes(text);

        int dataLength = Math.Min(1 + langCode.Length + textBytes.Length, YubiOtpConstants.NdefDataSize);
        payload[0] = (byte)dataLength;

        // Language header
        payload[2] = languageLength;
        langCode.CopyTo(payload.AsSpan(3));

        // Text content
        int textCopyLength = Math.Min(textBytes.Length, YubiOtpConstants.NdefDataSize - 1 - langCode.Length);
        textBytes.AsSpan(0, textCopyLength).CopyTo(payload.AsSpan(3 + langCode.Length));
    }

    private async Task WriteConfigAsync(
        ConfigSlot configSlot,
        SlotConfiguration config,
        ReadOnlyMemory<byte> accessCode,
        ReadOnlyMemory<byte> currentAccessCode,
        CancellationToken cancellationToken)
    {
        var configBytes = config.GetConfig(accessCode.Span);
        var data = BuildPayloadWithAccessCode(configBytes, currentAccessCode);

        try
        {
            _status = await _backend.WriteUpdateAsync(configSlot, data, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
            CryptographicOperations.ZeroMemory(configBytes);
        }
    }

    /// <summary>
    /// Appends the current access code (6 bytes) to the payload data for SmartCard transmission.
    /// </summary>
    private static byte[] BuildPayloadWithAccessCode(
        ReadOnlyMemory<byte> data,
        ReadOnlyMemory<byte> currentAccessCode)
    {
        if (currentAccessCode.IsEmpty)
        {
            return data.ToArray();
        }

        if (currentAccessCode.Length != YubiOtpConstants.AccessCodeSize)
        {
            throw new ArgumentException(
                $"Access code must be exactly {YubiOtpConstants.AccessCodeSize} bytes, got {currentAccessCode.Length}.",
                nameof(currentAccessCode));
        }

        var result = new byte[data.Length + YubiOtpConstants.AccessCodeSize];
        data.Span.CopyTo(result);
        currentAccessCode.Span.CopyTo(result.AsSpan(data.Length));
        return result;
    }

    private byte GetProgSeq()
    {
        if (_status.Length >= YubiOtpConstants.StatusBytesLength)
        {
            return _status.Span[3];
        }

        return 0;
    }

    private static (IProtocol protocol, IYubiOtpBackend backend) CreateSmartCardBackend(
        ISmartCardConnection connection)
    {
        var protocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(connection);

        // Initial prog_seq and firmware version will be set after SELECT
        var backend = new SmartCardBackend(
            protocol as ISmartCardProtocol ?? throw new InvalidOperationException(),
            new FirmwareVersion(),
            0);

        return (protocol, backend);
    }

    private static (IProtocol protocol, IYubiOtpBackend backend) CreateOtpBackend(
        IOtpHidConnection connection)
    {
        var protocol = OtpProtocolFactory
            .Create()
            .Create(connection);

        var backend = new OtpHidBackend(protocol);
        return (protocol, backend);
    }
}
