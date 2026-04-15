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
using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Manages an OATH applet session on a YubiKey device.
/// </summary>
public sealed class OathSession : ApplicationSession, IOathSession
{
    private static readonly Feature FeatureRenameCredential = new("Rename Credential", 5, 3, 1);
    private static readonly Feature FeatureScp03 = new("SCP03 for OATH", 5, 6, 3);

    private readonly ILogger _logger;
    private readonly ScpKeyParameters? _scpKeyParams;

    private ISmartCardProtocol _protocol = null!;
    private byte[] _salt = [];
    private byte[] _challenge = [];

    /// <inheritdoc />
    public string DeviceId { get; private set; } = string.Empty;

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Salt => _salt;

    /// <inheritdoc />
    public bool IsLocked { get; private set; }

    private OathSession(
        ISmartCardConnection connection,
        ScpKeyParameters? scpKeyParams = null)
    {
        _scpKeyParams = scpKeyParams;
        _logger = Logger;

        _protocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(connection) as ISmartCardProtocol
            ?? throw new InvalidOperationException("Failed to create SmartCard protocol.");

        Protocol = _protocol;
    }

    /// <summary>
    ///     Creates and initializes an OATH session.
    /// </summary>
    public static async Task<OathSession> CreateAsync(
        ISmartCardConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        var session = new OathSession(connection, scpKeyParams);
        await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
        return session;
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        var selectResponse = await _protocol
            .SelectAsync(ApplicationIds.Oath, cancellationToken)
            .ConfigureAwait(false);

        ParseSelectResponse(selectResponse.Span);

        if (_scpKeyParams is not null)
        {
            if (!IsSupported(FeatureScp03))
            {
                throw new NotSupportedException(
                    $"SCP03 for OATH requires firmware {FeatureScp03.Version}+, " +
                    $"but this device has {FirmwareVersion}.");
            }
        }

        await InitializeCoreAsync(
                _protocol,
                FirmwareVersion,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        _protocol = Protocol as ISmartCardProtocol
            ?? throw new InvalidOperationException("Protocol is not an ISmartCardProtocol after initialization.");

        _logger.LogDebug("OATH session initialized, DeviceId={DeviceId}, IsLocked={IsLocked}", DeviceId, IsLocked);
    }

    private void ParseSelectResponse(ReadOnlySpan<byte> data)
    {
        using var tlvs = TlvHelper.DecodeList(data);

        foreach (var tlv in tlvs)
        {
            switch (tlv.Tag)
            {
                case OathConstants.TagVersion:
                    var versionBytes = tlv.Value.Span;
                    FirmwareVersion = new FirmwareVersion(versionBytes[0], versionBytes[1], versionBytes[2]);
                    break;

                case OathConstants.TagName:
                    CryptographicOperations.ZeroMemory(_salt);
                    _salt = tlv.Value.ToArray();
                    break;

                case OathConstants.TagChallenge:
                    CryptographicOperations.ZeroMemory(_challenge);
                    _challenge = tlv.Value.ToArray();
                    break;
            }
        }

        DeviceId = ComputeDeviceId(_salt);
        IsLocked = _challenge.Length > 0;
    }

    internal static string ComputeDeviceId(ReadOnlySpan<byte> salt)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(salt, hash);
        return Convert.ToBase64String(hash[..16]).TrimEnd('=');
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Credential>> ListCredentialsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var command = new ApduCommand(0x00, OathConstants.InsList, 0x00, 0x00);
        var response = await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var responseData = await CollectResponseData(response, cancellationToken).ConfigureAwait(false);

        if (responseData.Length == 0)
            return [];

        var credentials = new List<Credential>();
        using var tlvs = TlvHelper.DecodeList(responseData.Span);

        foreach (var tlv in tlvs)
        {
            if (tlv.Tag != OathConstants.TagNameList)
                continue;

            var value = tlv.Value.Span;
            byte typeByte = value[0];
            var oathType = (OathType)(typeByte & OathConstants.MaskType);
            var credentialId = value[1..].ToArray();

            var (issuer, name, period) = Credential.ParseCredentialId(credentialId, oathType);

            credentials.Add(new Credential(
                DeviceId,
                credentialId,
                issuer,
                name,
                oathType,
                period,
                touchRequired: null));
        }

        return credentials;
    }

    /// <inheritdoc />
    public async Task PutCredentialAsync(
        CredentialData credentialData,
        bool requireTouch = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credentialData);

        byte[] credId = credentialData.GetId();
        byte[] secret = credentialData.GetProcessedSecret();

        byte typeByte = (byte)((byte)credentialData.OathType | (byte)credentialData.HashAlgorithm);

        // TAG_KEY value: [type_byte][digits_byte][secret...]
        byte[] keyValue = [(byte)typeByte, (byte)credentialData.Digits, .. secret];

        try
        {
            // TAG_NAME + TAG_KEY
            using var nameTlv = new Tlv(OathConstants.TagName, credId);
            using var keyTlv = new Tlv(OathConstants.TagKey, keyValue);

            // Calculate total size for all TLVs
            int totalSize = nameTlv.TotalLength + keyTlv.TotalLength;

            // TAG_PROPERTY is raw bytes [tag, value], NOT TLV-encoded (no length field).
            // This matches the ykman Python canonical: struct.pack(">BB", TAG_PROPERTY, PROP_REQUIRE_TOUCH)
            byte[]? propBytes = null;
            Tlv? imfTlv = null;

            if (requireTouch)
            {
                propBytes = [OathConstants.TagProperty, OathConstants.PropRequireTouch];
                totalSize += propBytes.Length;
            }

            if (credentialData.OathType == OathType.Hotp && credentialData.Counter > 0)
            {
                Span<byte> imfBytes = stackalloc byte[4];
                BinaryPrimitives.WriteInt32BigEndian(imfBytes, credentialData.Counter);
                imfTlv = new Tlv(OathConstants.TagImf, imfBytes);
                totalSize += imfTlv.TotalLength;
            }

            byte[]? data = null;
            try
            {
                data = new byte[totalSize];
                int offset = 0;

                nameTlv.AsSpan().CopyTo(data.AsSpan(offset));
                offset += nameTlv.TotalLength;

                keyTlv.AsSpan().CopyTo(data.AsSpan(offset));
                offset += keyTlv.TotalLength;

                if (propBytes is not null)
                {
                    propBytes.CopyTo(data.AsSpan(offset));
                    offset += propBytes.Length;
                }

                if (imfTlv is not null)
                {
                    imfTlv.AsSpan().CopyTo(data.AsSpan(offset));
                }

                var command = new ApduCommand(0x00, OathConstants.InsPut, 0x00, 0x00, data);
                await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (data is not null)
                {
                    CryptographicOperations.ZeroMemory(data);
                }

                imfTlv?.Dispose();
            }

            _logger.LogDebug("Credential stored: {CredentialIdLength} bytes", credId.Length);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyValue);
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <inheritdoc />
    public async Task DeleteCredentialAsync(
        Credential credential,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);

        using var nameTlv = new Tlv(OathConstants.TagName, credential.Id);
        var command = new ApduCommand(0x00, OathConstants.InsDelete, 0x00, 0x00, nameTlv.AsMemory());
        await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Credential> RenameCredentialAsync(
        Credential credential,
        string? newIssuer,
        string newName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        EnsureSupports(FeatureRenameCredential);

        ReadOnlyMemory<byte> oldId = credential.Id;
        byte[] newId = Credential.FormatCredentialId(newIssuer, newName, credential.OathType, credential.Period);

        using var oldNameTlv = new Tlv(OathConstants.TagName, oldId);
        using var newNameTlv = new Tlv(OathConstants.TagName, newId);

        byte[] data = ConcatTlvs(oldNameTlv.AsSpan(), newNameTlv.AsSpan());
        var command = new ApduCommand(0x00, OathConstants.InsRename, 0x00, 0x00, data);
        await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new Credential(
            DeviceId,
            newId,
            newIssuer,
            newName,
            credential.OathType,
            credential.Period,
            credential.TouchRequired);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> CalculateAsync(
        Credential credential,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);

        using var nameTlv = new Tlv(OathConstants.TagName, credential.Id);
        using var challengeTlv = new Tlv(OathConstants.TagChallenge, challenge);

        byte[] data = ConcatTlvs(nameTlv.AsSpan(), challengeTlv.AsSpan());
        var command = new ApduCommand(0x00, OathConstants.InsCalculate, 0x00, 0x00, data);
        var response = await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        using var responseTlvs = TlvHelper.DecodeList(response.Data.Span);
        foreach (var tlv in responseTlvs)
        {
            if (tlv.Tag == OathConstants.TagResponse)
                return tlv.Value.ToArray();
        }

        throw new BadResponseException("No TAG_RESPONSE in CALCULATE response.");
    }

    /// <inheritdoc />
    public async Task<Code> CalculateCodeAsync(
        Credential credential,
        long? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);

        long ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        byte[] challenge;

        if (credential.OathType == OathType.Totp)
        {
            long timeStep = ts / credential.Period;
            challenge = new byte[OathConstants.ChallengeLength];
            BinaryPrimitives.WriteInt64BigEndian(challenge, timeStep);
        }
        else
        {
            challenge = [];
        }

        using var nameTlv = new Tlv(OathConstants.TagName, credential.Id);
        using var challengeTlv = new Tlv(OathConstants.TagChallenge, challenge);

        byte[] data = ConcatTlvs(nameTlv.AsSpan(), challengeTlv.AsSpan());
        // P2=0x01 requests truncated response
        var command = new ApduCommand(0x00, OathConstants.InsCalculate, 0x00, 0x01, data);
        var response = await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        using var responseTlvs = TlvHelper.DecodeList(response.Data.Span);
        foreach (var tlv in responseTlvs)
        {
            if (tlv.Tag == OathConstants.TagTruncated)
                return Code.FormatCode(credential, ts, tlv.Value.Span);
        }

        throw new BadResponseException("No TAG_TRUNCATED in CALCULATE response.");
    }

    /// <inheritdoc />
    public async Task<Dictionary<Credential, Code?>> CalculateAllAsync(
        long? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        long ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long timeStep = ts / OathConstants.DefaultPeriod;

        byte[] challenge = new byte[OathConstants.ChallengeLength];
        BinaryPrimitives.WriteInt64BigEndian(challenge, timeStep);

        using var challengeTlv = new Tlv(OathConstants.TagChallenge, challenge);

        // P2=0x01 requests truncated responses
        var command = new ApduCommand(0x00, OathConstants.InsCalculateAll, 0x00, 0x01, challengeTlv.AsMemory());
        var response = await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var responseData = await CollectResponseData(response, cancellationToken).ConfigureAwait(false);

        var result = new Dictionary<Credential, Code?>();

        if (responseData.Length == 0)
            return result;

        using var tlvs = TlvHelper.DecodeList(responseData.Span);
        var tlvList = tlvs.ToList();

        for (int i = 0; i < tlvList.Count - 1; i += 2)
        {
            var nameTlv = tlvList[i];
            var responseTlv = tlvList[i + 1];

            if (nameTlv.Tag != OathConstants.TagName)
                continue;

            // In CALCULATE_ALL, TAG_NAME (0x71) contains the raw credential ID directly.
            // The oath type is derived from the response TLV tag (TAG_HOTP/TAG_TOUCH = HOTP,
            // TAG_TRUNCATED = TOTP). This differs from LIST where TAG_NAME_LIST (0x72) prepends
            // a type byte before the credential ID.
            var credentialId = nameTlv.Value.ToArray();
            var oathType = responseTlv.Tag == OathConstants.TagHotp ? OathType.Hotp : OathType.Totp;
            var (issuer, name, period) = Credential.ParseCredentialId(credentialId, oathType);

            bool touchRequired = responseTlv.Tag == OathConstants.TagTouch;
            var credential = new Credential(DeviceId, credentialId, issuer, name, oathType, period, touchRequired);

            Code? code = responseTlv.Tag switch
            {
                OathConstants.TagTruncated when credential.OathType == OathType.Totp
                    && credential.Period != OathConstants.DefaultPeriod
                    => await CalculateCodeAsync(credential, ts, cancellationToken).ConfigureAwait(false),
                OathConstants.TagTruncated => Code.FormatCode(credential, ts, responseTlv.Value.Span),
                OathConstants.TagHotp => null,
                OathConstants.TagTouch => null,
                _ => null
            };

            result[credential] = code;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var command = new ApduCommand(0x00, OathConstants.InsReset, 0xDE, 0xAD);
        await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Re-select and re-parse to get new state
        var selectResponse = await _protocol
            .SelectAsync(ApplicationIds.Oath, cancellationToken)
            .ConfigureAwait(false);

        ParseSelectResponse(selectResponse.Span);

        _logger.LogInformation("OATH application reset, new DeviceId={DeviceId}", DeviceId);
    }

    /// <inheritdoc />
    public byte[] DeriveKey(ReadOnlyMemory<byte> passwordUtf8)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            passwordUtf8.Span,
            _salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);
    }

    /// <inheritdoc />
    public async Task ValidateAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (key.Length == 0)
            throw new ArgumentException("Key must not be empty.", nameof(key));

        if (_challenge.Length == 0)
            throw new InvalidOperationException("Device is not locked; no challenge to validate against.");

        byte[]? clientResponse = null;
        byte[] clientChallenge = new byte[OathConstants.ChallengeLength];

        try
        {
            clientResponse = HMACSHA1.HashData(key.Span, _challenge);
            RandomNumberGenerator.Fill(clientChallenge);

            using var responseTlv = new Tlv(OathConstants.TagResponse, clientResponse);
            using var challengeTlv = new Tlv(OathConstants.TagChallenge, clientChallenge);

            byte[] data = ConcatTlvs(responseTlv.AsSpan(), challengeTlv.AsSpan());
            var command = new ApduCommand(0x00, OathConstants.InsValidate, 0x00, 0x00, data);
            var response = await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Verify device's response
            byte[] expectedResponse = HMACSHA1.HashData(key.Span, clientChallenge);
            byte[]? deviceResponse = null;
            try
            {
                using var deviceTlvs = TlvHelper.DecodeList(response.Data.Span);

                foreach (var tlv in deviceTlvs)
                {
                    if (tlv.Tag == OathConstants.TagResponse)
                    {
                        deviceResponse = tlv.Value.ToArray();
                        break;
                    }
                }

                if (deviceResponse is null)
                    throw new BadResponseException("No TAG_RESPONSE in VALIDATE response.");

                if (!CryptographicOperations.FixedTimeEquals(expectedResponse, deviceResponse))
                    throw new InvalidOperationException("Device mutual authentication failed.");

                IsLocked = false;
                CryptographicOperations.ZeroMemory(_challenge);
                _challenge = [];
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expectedResponse);
                if (deviceResponse is not null)
                    CryptographicOperations.ZeroMemory(deviceResponse);
            }
        }
        finally
        {
            if (clientResponse is not null)
                CryptographicOperations.ZeroMemory(clientResponse);
            CryptographicOperations.ZeroMemory(clientChallenge);
        }
    }

    /// <inheritdoc />
    public async Task SetKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (key.Length == 0)
            throw new ArgumentException("Key must not be empty.", nameof(key));

        byte[] clientChallenge = new byte[OathConstants.ChallengeLength];
        byte[]? clientResponse = null;
        byte[]? keyValue = null;

        try
        {
            RandomNumberGenerator.Fill(clientChallenge);
            clientResponse = HMACSHA1.HashData(key.Span, clientChallenge);

            byte typeByte = (byte)((byte)OathType.Totp | (byte)OathHashAlgorithm.Sha1);
            keyValue = new byte[1 + key.Length];
            keyValue[0] = typeByte;
            key.Span.CopyTo(keyValue.AsSpan(1));

            using var keyTlv = new Tlv(OathConstants.TagKey, keyValue);
            using var challengeTlv = new Tlv(OathConstants.TagChallenge, clientChallenge);
            using var responseTlv = new Tlv(OathConstants.TagResponse, clientResponse);

            byte[] data = ConcatTlvs(keyTlv.AsSpan(), challengeTlv.AsSpan(), responseTlv.AsSpan());
            var command = new ApduCommand(0x00, OathConstants.InsSetCode, 0x00, 0x00, data);
            await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clientChallenge);
            if (clientResponse is not null)
                CryptographicOperations.ZeroMemory(clientResponse);
            if (keyValue is not null)
                CryptographicOperations.ZeroMemory(keyValue);
        }
    }

    /// <inheritdoc />
    public async Task UnsetKeyAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Send SET CODE with empty key TLV to clear the access key
        using var keyTlv = new Tlv(OathConstants.TagKey, ReadOnlySpan<byte>.Empty);
        var command = new ApduCommand(0x00, OathConstants.InsSetCode, 0x00, 0x00, keyTlv.AsMemory());
        await _protocol.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static byte[] ConcatTlvs(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var result = new byte[a.Length + b.Length];
        a.CopyTo(result);
        b.CopyTo(result.AsSpan(a.Length));
        return result;
    }

    private static byte[] ConcatTlvs(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, ReadOnlySpan<byte> c)
    {
        var result = new byte[a.Length + b.Length + c.Length];
        a.CopyTo(result);
        b.CopyTo(result.AsSpan(a.Length));
        c.CopyTo(result.AsSpan(a.Length + b.Length));
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CryptographicOperations.ZeroMemory(_salt);
            CryptographicOperations.ZeroMemory(_challenge);
        }

        base.Dispose(disposing);
    }

    private async Task<ReadOnlyMemory<byte>> CollectResponseData(
        ApduResponse response,
        CancellationToken cancellationToken)
    {
        if (response.Data.Length == 0 && response.IsOK())
            return ReadOnlyMemory<byte>.Empty;

        // Check for chained response (SW1=0x61)
        if (response.SW1 != 0x61)
            return response.Data;

        // Collect all chained data using ArrayBufferWriter to avoid the double allocation from MemoryStream.ToArray()
        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write(response.Data.Span);

        var currentResponse = response;
        while (currentResponse.SW1 == 0x61)
        {
            var sendRemaining = new ApduCommand(0x00, OathConstants.InsSendRemaining, 0x00, 0x00);
            currentResponse = await _protocol
                .TransmitAndReceiveAsync(sendRemaining, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            buffer.Write(currentResponse.Data.Span);
        }

        return buffer.WrittenMemory;
    }
}
