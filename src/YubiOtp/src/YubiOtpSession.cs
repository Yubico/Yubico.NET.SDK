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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
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
    private readonly IConnection _connection;
    private readonly ScpKeyParameters? _scpKeyParams;

    private IProtocol _protocol = null!;
    private IYubiOtpBackend _backend = null!;
    private ReadOnlyMemory<byte> _status;

    private YubiOtpSession(
        IConnection connection,
        ScpKeyParameters? scpKeyParams = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _scpKeyParams = scpKeyParams;
        _logger = Logger;
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
        ArgumentNullException.ThrowIfNull(connection);

        var session = new YubiOtpSession(connection, scpKeyParams);
        try
        {
            await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch
        {
            session.DisposeAfterInitializationFailure();
            throw;
        }
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
        {
            return;
        }

        var protocol = ProtocolFactory.Create(_connection);
        Protocol = protocol;
        var backend = CreateBackend(protocol);

        var initialization = await backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
        _status = initialization.Status;

        var effectiveProtocol = await InitializeProtocolAsync(
                protocol,
                initialization.FirmwareVersion,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        if (effectiveProtocol is ISmartCardProtocol smartCardProtocol)
        {
            // Rebinding must carry forward the programming sequence read during SELECT.
            backend = new SmartCardBackend(
                smartCardProtocol,
                FirmwareVersion,
                GetProgSeq());
        }

        _protocol = effectiveProtocol;
        _backend = backend;

        _logger.LogDebug("YubiOTP session initialized with protocol {ProtocolType}", _protocol.GetType().Name);
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

    private static IYubiOtpBackend CreateBackend(IProtocol protocol) =>
        protocol switch
        {
            // Initial prog_seq and firmware version will be set after SELECT.
            ISmartCardProtocol smartCard => new SmartCardBackend(smartCard, new FirmwareVersion(), 0),
            IOtpHidProtocol otpHid => new HidBackend(otpHid),
            _ => throw new NotSupportedException(
                $"Protocol type {protocol.GetType().Name} is not supported by YubiOtpSession. " +
                "Supported protocols: SmartCard and OTP HID.")
        };
}