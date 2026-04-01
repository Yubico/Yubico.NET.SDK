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

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Entry point for interacting with the YubiHSM Auth applet on a YubiKey.
///     Stores credentials used to authenticate to YubiHSM 2 hardware security modules.
/// </summary>
public sealed class HsmAuthSession : ApplicationSession, IHsmAuthSession
{
    // Feature detection constants
    public static readonly Feature FeatureHsmAuth = new("YubiHSM Auth", 5, 4, 3);
    public static readonly Feature FeatureAsymmetric = new("Asymmetric credentials", 5, 6, 0);
    public static readonly Feature FeatureGetChallenge = new("Get challenge", 5, 6, 0);
    public static readonly Feature FeaturePasswordChange = new("Credential password change", 5, 8, 0);
    public static readonly Feature FeatureGetChallengeNoPassword = new("Get challenge without password", 5, 7, 1);

    // APDU instruction bytes
    internal const byte InsPut = 0x01;
    internal const byte InsDelete = 0x02;
    internal const byte InsCalculate = 0x03;
    internal const byte InsGetChallenge = 0x04;
    internal const byte InsList = 0x05;
    internal const byte InsReset = 0x06;
    internal const byte InsGetVersion = 0x07;
    internal const byte InsPutManagementKey = 0x08;
    internal const byte InsGetManagementKeyRetries = 0x09;
    internal const byte InsGetPublicKey = 0x0A;
    internal const byte InsChangeCredentialPassword = 0x0B;

    // TLV tags
    internal const byte TagLabel = 0x71;
    internal const byte TagLabelList = 0x72;
    internal const byte TagCredentialPassword = 0x73;
    internal const byte TagAlgorithm = 0x74;
    internal const byte TagKeyEnc = 0x75;
    internal const byte TagKeyMac = 0x76;
    internal const byte TagContext = 0x77;
    internal const byte TagResponse = 0x78;
    internal const byte TagVersion = 0x79;
    internal const byte TagTouch = 0x7A;
    internal const byte TagManagementKey = 0x7B;
    internal const byte TagPublicKey = 0x7C;
    internal const byte TagPrivateKey = 0x7D;

    // Reset P1/P2
    private const byte ResetP1 = 0xDE;
    private const byte ResetP2 = 0xAD;

    // Credential password constraints
    internal const int CredentialPasswordLength = 16;

    // Management key length
    internal const int ManagementKeyLength = 16;

    // EC P256 key lengths
    internal const int EcP256PrivateKeyLength = 32;
    internal const int EcP256PublicKeyLength = 65; // 0x04 + x[32] + y[32]

    // Label constraints
    internal const int MinLabelLength = 1;
    internal const int MaxLabelLength = 64;

    // PBKDF2 derivation constants
    internal const int Pbkdf2Iterations = 10_000;
    internal static readonly byte[] Pbkdf2Salt = "Yubico"u8.ToArray();
    internal const int Pbkdf2DerivedKeyLength = 32;

    private readonly ISmartCardConnection _connection;
    private readonly ILogger _logger;
    private readonly ScpKeyParameters? _scpKeyParams;
    private ISmartCardProtocol? _protocol;

    private HsmAuthSession(
        ISmartCardConnection connection,
        ScpKeyParameters? scpKeyParams = null)
    {
        _connection = connection;
        _logger = Logger;
        _scpKeyParams = scpKeyParams;
    }

    /// <summary>
    ///     Factory helper that creates and initializes a YubiHSM Auth session.
    /// </summary>
    /// <param name="connection">The SmartCard connection to use.</param>
    /// <param name="configuration">Optional protocol configuration.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
    /// <param name="firmwareVersion">Optional firmware version override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized <see cref="HsmAuthSession" />.</returns>
    public static async Task<HsmAuthSession> CreateAsync(
        ISmartCardConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        FirmwareVersion? firmwareVersion = null,
        CancellationToken cancellationToken = default)
    {
        var session = new HsmAuthSession(connection, scpKeyParams);
        await session.InitializeAsync(configuration, firmwareVersion, cancellationToken)
            .ConfigureAwait(false);
        return session;
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration = null,
        FirmwareVersion? firmwareVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        var smartCardProtocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(_connection);

        var selectResponse = await smartCardProtocol
            .SelectAsync(ApplicationIds.YubiHsmAuth, cancellationToken)
            .ConfigureAwait(false);

        // Parse firmware version from SELECT response TAG_VERSION TLV if not explicitly provided.
        var resolvedFirmwareVersion = firmwareVersion
            ?? ParseVersionFromSelectResponse(selectResponse)
            ?? FeatureHsmAuth.Version;

        await InitializeCoreAsync(
                smartCardProtocol,
                resolvedFirmwareVersion,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        _protocol = Protocol as ISmartCardProtocol;
        if (_protocol is null)
            throw new InvalidOperationException("Protocol initialization failed.");
    }

    private static FirmwareVersion? ParseVersionFromSelectResponse(ReadOnlyMemory<byte> response)
    {
        if (response.IsEmpty)
            return null;

        if (!TlvHelper.TryFindValue(TagVersion, response.Span, out var versionData))
            return null;

        if (versionData.Length != 3)
            return null;

        var span = versionData.Span;
        return new FirmwareVersion(span[0], span[1], span[2]);
    }

    /// <summary>
    ///     Parses a credential password string into a 16-byte buffer.
    ///     The string is UTF-8 encoded and padded with null bytes to 16 bytes.
    /// </summary>
    /// <param name="password">The password string.</param>
    /// <returns>A 16-byte array containing the padded password.</returns>
    /// <exception cref="ArgumentException">Thrown when the UTF-8 encoding exceeds 16 bytes.</exception>
    internal static byte[] ParseCredentialPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var utf8Length = Encoding.UTF8.GetByteCount(password);
        if (utf8Length > CredentialPasswordLength)
            throw new ArgumentException(
                $"Credential password UTF-8 encoding ({utf8Length} bytes) exceeds maximum of {CredentialPasswordLength} bytes.",
                nameof(password));

        var buffer = new byte[CredentialPasswordLength];
        Encoding.UTF8.GetBytes(password, buffer);
        return buffer;
    }

    /// <summary>
    ///     Validates a raw credential password byte array.
    /// </summary>
    /// <param name="password">The password bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the password is not exactly 16 bytes.</exception>
    internal static void ValidateCredentialPassword(ReadOnlySpan<byte> password)
    {
        if (password.Length != CredentialPasswordLength)
            throw new ArgumentException(
                $"Credential password must be exactly {CredentialPasswordLength} bytes, got {password.Length}.",
                nameof(password));
    }

    /// <summary>
    ///     Validates and encodes a credential label to UTF-8 bytes.
    /// </summary>
    /// <param name="label">The label string.</param>
    /// <returns>The UTF-8 encoded label bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when the label is empty or exceeds 64 UTF-8 bytes.</exception>
    internal static byte[] ValidateAndEncodeLabel(string label)
    {
        ArgumentException.ThrowIfNullOrEmpty(label);

        var encoded = Encoding.UTF8.GetBytes(label);
        if (encoded.Length < MinLabelLength)
            throw new ArgumentException(
                $"Label must be at least {MinLabelLength} UTF-8 byte(s).",
                nameof(label));

        if (encoded.Length > MaxLabelLength)
            throw new ArgumentException(
                $"Label UTF-8 encoding ({encoded.Length} bytes) exceeds maximum of {MaxLabelLength} bytes.",
                nameof(label));

        return encoded;
    }

    /// <summary>
    ///     Extracts the remaining management key retries from a 0x63Cx status word.
    /// </summary>
    /// <param name="sw">The status word from an APDU response.</param>
    /// <returns>The number of remaining retries, or <c>null</c> if the SW is not a retry indicator.</returns>
    internal static int? ExtractRetries(short sw)
    {
        // 0x63Cx where x = remaining retries
        if ((sw & 0xFFF0) == 0x63C0)
            return sw & 0x000F;

        return null;
    }

    // ─── IHsmAuthSession implementations ─────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<HsmAuthCredential>> ListCredentialsAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var command = new ApduCommand { Ins = InsList };
        var response = await _protocol!.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var credentials = new List<HsmAuthCredential>();
        if (response.Data.IsEmpty)
            return credentials;

        using var tlvs = TlvHelper.DecodeList(response.Data.Span);
        foreach (var tlv in tlvs)
        {
            if (tlv.Tag != TagLabelList)
                continue;

            var value = tlv.Value.Span;
            if (value.Length < 3)
                continue;

            var algorithm = (HsmAuthAlgorithm)value[0];
            var touchByte = value[1];
            bool? touchRequired = touchByte switch
            {
                0x00 => false,
                0x01 => true,
                _ => null
            };
            var labelBytes = value[2..^1]; // Everything except algorithm, touch, and counter
            var counter = value[^1];
            var label = Encoding.UTF8.GetString(labelBytes);

            credentials.Add(new HsmAuthCredential(label, algorithm, counter, touchRequired));
        }

        return credentials;
    }

    /// <inheritdoc />
    public async Task PutCredentialSymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> keyEnc,
        ReadOnlyMemory<byte> keyMac,
        string credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateManagementKey(managementKey.Span);
        var labelBytes = ValidateAndEncodeLabel(label);

        if (keyEnc.Length != HsmAuthAlgorithm.Aes128YubicoAuthentication.KeyLength)
            throw new ArgumentException(
                $"Encryption key must be {HsmAuthAlgorithm.Aes128YubicoAuthentication.KeyLength} bytes.",
                nameof(keyEnc));

        if (keyMac.Length != HsmAuthAlgorithm.Aes128YubicoAuthentication.KeyLength)
            throw new ArgumentException(
                $"MAC key must be {HsmAuthAlgorithm.Aes128YubicoAuthentication.KeyLength} bytes.",
                nameof(keyMac));

        byte[]? credPwBytes = null;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPassword);

            var data = TlvHelper.EncodeList(
            [
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagAlgorithm, [(byte)HsmAuthAlgorithm.Aes128YubicoAuthentication]),
                new Tlv(TagKeyEnc, keyEnc.Span),
                new Tlv(TagKeyMac, keyMac.Span),
                new Tlv(TagCredentialPassword, credPwBytes),
                new Tlv(TagTouch, [touchRequired ? (byte)0x01 : (byte)0x00])
            ]);

            var command = new ApduCommand { Ins = InsPut, Data = data };
            var response = await _protocol!.TransmitAndReceiveAsync(
                    command, throwOnError: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            ThrowOnManagementKeyFailure(response, command);
            if (!response.IsOK())
                throw ApduException.FromResponse(response, command, "PUT credential failed");
        }
        finally
        {
            if (credPwBytes is not null)
                CryptographicOperations.ZeroMemory(credPwBytes);
        }
    }

    /// <inheritdoc />
    public async Task PutCredentialDerivedAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        string derivationPassword,
        string credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(derivationPassword);

        byte[]? derivedKey = null;
        try
        {
            derivedKey = DeriveKeys(derivationPassword);

            await PutCredentialSymmetricAsync(
                    managementKey,
                    label,
                    derivedKey.AsMemory(0, 16),
                    derivedKey.AsMemory(16, 16),
                    credentialPassword,
                    touchRequired,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (derivedKey is not null)
                CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    /// <inheritdoc />
    public async Task DeleteCredentialAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateManagementKey(managementKey.Span);
        var labelBytes = ValidateAndEncodeLabel(label);

        var data = TlvHelper.EncodeList(
        [
            new Tlv(TagManagementKey, managementKey.Span),
            new Tlv(TagLabel, labelBytes)
        ]);

        var command = new ApduCommand { Ins = InsDelete, Data = data };
        var response = await _protocol!.TransmitAndReceiveAsync(
                command, throwOnError: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ThrowOnManagementKeyFailure(response, command);
        if (!response.IsOK())
            throw ApduException.FromResponse(response, command, "DELETE credential failed");
    }

    /// <inheritdoc />
    public async Task<SessionKeys> CalculateSessionKeysSymmetricAsync(
        string label,
        ReadOnlyMemory<byte> context,
        string credentialPassword,
        ReadOnlyMemory<byte>? cardCryptogram = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? credPwBytes = null;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPassword);

            var tlvs = new List<Tlv>
            {
                new(TagLabel, labelBytes),
                new(TagContext, context.Span)
            };

            if (cardCryptogram is { } cc)
                tlvs.Add(new Tlv(TagResponse, cc.Span));

            tlvs.Add(new Tlv(TagCredentialPassword, credPwBytes));

            var data = TlvHelper.EncodeList([.. tlvs]);

            var command = new ApduCommand { Ins = InsCalculate, Data = data };
            var response = await _protocol!.TransmitAndReceiveAsync(
                    command, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return SessionKeys.Parse(response.Data.Span);
        }
        finally
        {
            if (credPwBytes is not null)
                CryptographicOperations.ZeroMemory(credPwBytes);
        }
    }

    /// <inheritdoc />
    public async Task<int> GetManagementKeyRetriesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var command = new ApduCommand { Ins = InsGetManagementKeyRetries };
        var response = await _protocol!.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var data = response.Data.Span;
        return data.Length switch
        {
            1 => data[0],
            2 => (data[0] << 8) | data[1],
            _ => throw new InvalidOperationException(
                $"Unexpected response length {data.Length} for GET_MANAGEMENT_KEY_RETRIES.")
        };
    }

    /// <inheritdoc />
    public async Task PutManagementKeyAsync(
        ReadOnlyMemory<byte> currentManagementKey,
        ReadOnlyMemory<byte> newManagementKey,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateManagementKey(currentManagementKey.Span);
        ValidateManagementKey(newManagementKey.Span);

        var data = TlvHelper.EncodeList(
        [
            new Tlv(TagManagementKey, currentManagementKey.Span),
            new Tlv(TagManagementKey, newManagementKey.Span)
        ]);

        var command = new ApduCommand { Ins = InsPutManagementKey, Data = data };
        var response = await _protocol!.TransmitAndReceiveAsync(
                command, throwOnError: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ThrowOnManagementKeyFailure(response, command);
        if (!response.IsOK())
            throw ApduException.FromResponse(response, command, "PUT management key failed");
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var command = new ApduCommand { Ins = InsReset, P1 = ResetP1, P2 = ResetP2 };
        await _protocol!.TransmitAndReceiveAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Reset initialization flag so InitializeCoreAsync will run again
        IsInitialized = false;

        // Re-SELECT the applet after reset to refresh cached state
        var smartCardProtocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(_connection);

        var selectResponse = await smartCardProtocol
            .SelectAsync(ApplicationIds.YubiHsmAuth, cancellationToken)
            .ConfigureAwait(false);

        var resolvedFirmwareVersion = ParseVersionFromSelectResponse(selectResponse)
            ?? FeatureHsmAuth.Version;

        await InitializeCoreAsync(
                smartCardProtocol,
                resolvedFirmwareVersion,
                scpKeyParams: _scpKeyParams,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _protocol = Protocol as ISmartCardProtocol;
    }

    /// <inheritdoc />
    public async Task<SessionKeys> CalculateSessionKeysAsymmetricAsync(
        string label,
        ReadOnlyMemory<byte> context,
        string credentialPassword,
        ReadOnlyMemory<byte>? cardCryptogram = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureAsymmetric);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? credPwBytes = null;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPassword);

            var tlvs = new List<Tlv>
            {
                new(TagLabel, labelBytes),
                new(TagContext, context.Span)
            };

            if (cardCryptogram is { } cc)
                tlvs.Add(new Tlv(TagResponse, cc.Span));

            tlvs.Add(new Tlv(TagCredentialPassword, credPwBytes));

            var data = TlvHelper.EncodeList([.. tlvs]);

            var command = new ApduCommand { Ins = InsCalculate, Data = data };
            var response = await _protocol!.TransmitAndReceiveAsync(
                    command, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return SessionKeys.Parse(response.Data.Span);
        }
        finally
        {
            if (credPwBytes is not null)
                CryptographicOperations.ZeroMemory(credPwBytes);
        }
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetChallengeAsync(
        string label,
        string? credentialPassword = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureGetChallenge);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? credPwBytes = null;
        try
        {
            var tlvs = new List<Tlv> { new(TagLabel, labelBytes) };

            // Firmware >= 5.7.1 does not require credential password.
            // Older firmware requires it.
            if (credentialPassword is not null)
            {
                credPwBytes = ParseCredentialPassword(credentialPassword);
                tlvs.Add(new Tlv(TagCredentialPassword, credPwBytes));
            }
            else if (!IsSupported(FeatureGetChallengeNoPassword) && FirmwareVersion.Major != 0)
            {
                throw new ArgumentException(
                    "Credential password is required for firmware versions before 5.7.1.",
                    nameof(credentialPassword));
            }

            var data = TlvHelper.EncodeList([.. tlvs]);

            var command = new ApduCommand { Ins = InsGetChallenge, Data = data };
            var response = await _protocol!.TransmitAndReceiveAsync(
                    command, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Data;
        }
        finally
        {
            if (credPwBytes is not null)
                CryptographicOperations.ZeroMemory(credPwBytes);
        }
    }

    /// <inheritdoc />
    public async Task PutCredentialAsymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> privateKey,
        string credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureAsymmetric);
        ValidateManagementKey(managementKey.Span);
        var labelBytes = ValidateAndEncodeLabel(label);

        if (privateKey.Length != EcP256PrivateKeyLength)
            throw new ArgumentException(
                $"EC P256 private key must be exactly {EcP256PrivateKeyLength} bytes.",
                nameof(privateKey));

        byte[]? credPwBytes = null;
        Memory<byte> data = default;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPassword);

            data = TlvHelper.EncodeList(
            [
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagAlgorithm, [(byte)HsmAuthAlgorithm.EcP256YubicoAuthentication]),
                new Tlv(TagPrivateKey, privateKey.Span),
                new Tlv(TagCredentialPassword, credPwBytes),
                new Tlv(TagTouch, [touchRequired ? (byte)0x01 : (byte)0x00])
            ]);

            var command = new ApduCommand { Ins = InsPut, Data = data };
            var response = await _protocol!.TransmitAndReceiveAsync(
                    command, throwOnError: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            ThrowOnManagementKeyFailure(response, command);
            if (!response.IsOK())
                throw ApduException.FromResponse(response, command, "PUT asymmetric credential failed");
        }
        finally
        {
            if (credPwBytes is not null)
                CryptographicOperations.ZeroMemory(credPwBytes);
            if (!data.IsEmpty)
                CryptographicOperations.ZeroMemory(data.Span);
        }
    }

    /// <inheritdoc />
    public async Task GenerateCredentialAsymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        string credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureAsymmetric);
        ValidateManagementKey(managementKey.Span);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? credPwBytes = null;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPassword);

            var data = TlvHelper.EncodeList(
            [
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagAlgorithm, [(byte)HsmAuthAlgorithm.EcP256YubicoAuthentication]),
                new Tlv(TagCredentialPassword, credPwBytes),
                new Tlv(TagTouch, [touchRequired ? (byte)0x01 : (byte)0x00])
            ]);

            var command = new ApduCommand { Ins = InsPut, Data = data };
            var response = await _protocol!.TransmitAndReceiveAsync(
                    command, throwOnError: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            ThrowOnManagementKeyFailure(response, command);
            if (!response.IsOK())
                throw ApduException.FromResponse(response, command, "GENERATE asymmetric credential failed");
        }
        finally
        {
            if (credPwBytes is not null)
                CryptographicOperations.ZeroMemory(credPwBytes);
        }
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        string label,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureAsymmetric);
        var labelBytes = ValidateAndEncodeLabel(label);

        var data = TlvHelper.EncodeList([new Tlv(TagLabel, labelBytes)]);

        var command = new ApduCommand { Ins = InsGetPublicKey, Data = data };
        var response = await _protocol!.TransmitAndReceiveAsync(
                command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.Data;
    }

    /// <inheritdoc />
    public async Task ChangeCredentialPasswordAsync(
        string label,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeaturePasswordChange);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? currentPwBytes = null;
        byte[]? newPwBytes = null;
        try
        {
            currentPwBytes = ParseCredentialPassword(currentPassword);
            newPwBytes = ParseCredentialPassword(newPassword);

            var data = TlvHelper.EncodeList(
            [
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagCredentialPassword, currentPwBytes),
                new Tlv(TagCredentialPassword, newPwBytes)
            ]);

            var command = new ApduCommand { Ins = InsChangeCredentialPassword, P1 = 0x00, Data = data };
            var response = await _protocol!.TransmitAndReceiveAsync(
                    command, throwOnError: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            ThrowOnManagementKeyFailure(response, command);
            if (!response.IsOK())
                throw ApduException.FromResponse(response, command, "CHANGE credential password failed");
        }
        finally
        {
            if (currentPwBytes is not null)
                CryptographicOperations.ZeroMemory(currentPwBytes);
            if (newPwBytes is not null)
                CryptographicOperations.ZeroMemory(newPwBytes);
        }
    }

    /// <inheritdoc />
    public async Task ChangeCredentialPasswordAdminAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeaturePasswordChange);
        ValidateManagementKey(managementKey.Span);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? newPwBytes = null;
        try
        {
            newPwBytes = ParseCredentialPassword(newPassword);

            var data = TlvHelper.EncodeList(
            [
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagCredentialPassword, newPwBytes)
            ]);

            var command = new ApduCommand { Ins = InsChangeCredentialPassword, P1 = 0x01, Data = data };
            var response = await _protocol!.TransmitAndReceiveAsync(
                    command, throwOnError: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            ThrowOnManagementKeyFailure(response, command);
            if (!response.IsOK())
                throw ApduException.FromResponse(response, command, "CHANGE credential password (admin) failed");
        }
        finally
        {
            if (newPwBytes is not null)
                CryptographicOperations.ZeroMemory(newPwBytes);
        }
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    ///     Derives AES-128 key pair (K-ENC, K-MAC) from a password using PBKDF2-HMAC-SHA256.
    /// </summary>
    internal static byte[] DeriveKeys(string derivationPassword)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(derivationPassword);
        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                Pbkdf2Salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                Pbkdf2DerivedKeyLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static void ValidateManagementKey(ReadOnlySpan<byte> managementKey)
    {
        if (managementKey.Length != ManagementKeyLength)
            throw new ArgumentException(
                $"Management key must be exactly {ManagementKeyLength} bytes, got {managementKey.Length}.",
                nameof(managementKey));
    }

    /// <summary>
    ///     Checks an APDU response for the 0x63Cx management key verification failure pattern.
    ///     Throws <see cref="ApduException" /> with retry information if detected.
    /// </summary>
    private static void ThrowOnManagementKeyFailure(ApduResponse response, ApduCommand command)
    {
        var retries = ExtractRetries(response.SW);
        if (retries is null)
            return;

        throw new ApduException(
            $"Management key verification failed, {retries} attempt(s) remaining (SW=0x{response.SW:X4})")
        {
            SW = response.SW,
            Cla = command.Cla,
            Ins = command.Ins,
            P1 = command.P1,
            P2 = command.P2
        };
    }
}
