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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.YubiHsm.Backend;

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
    public static readonly Feature FeatureGetChallengeWithPassword = new("Get challenge with password", 5, 7, 1);

    /// <summary>
    /// Compatibility alias for <see cref="FeatureGetChallengeWithPassword"/>.
    /// </summary>
    [Obsolete("Use FeatureGetChallengeWithPassword. Firmware 5.7.1 added password support; earlier firmware already allowed GetChallenge without a password.")]
    public static readonly Feature FeatureGetChallengeNoPassword = FeatureGetChallengeWithPassword;

    // APDU instruction bytes
    internal const byte InsPut = 0x01;
    internal const byte InsDelete = 0x02;
    internal const byte InsCalculate = 0x03;
    internal const byte InsGetChallenge = 0x04;
    internal const byte InsList = 0x05;
    internal const byte InsReset = 0x06;
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

    internal const int SymmetricContextLength = 16; // host challenge[8] + HSM challenge[8]
    internal const int AsymmetricContextLength = EcP256PublicKeyLength * 2;

    // Label constraints
    internal const int MinLabelLength = 1;
    internal const int MaxLabelLength = 64;

    // PBKDF2 derivation constants
    internal const int Pbkdf2Iterations = 10_000;
    internal static ReadOnlySpan<byte> Pbkdf2Salt => "Yubico"u8;
    internal const int Pbkdf2DerivedKeyLength = 32;

    private readonly ScpKeyParameters? _scpKeyParams;
    private ISmartCardProtocol _protocol = null!;
    private IHsmAuthBackend _backend = null!;

    /// <summary>
    ///     Gets or sets a callback invoked when a session-key calculation may require the user
    ///     to physically touch the YubiKey.
    /// </summary>
    /// <remarks>
    ///     Each session-key calculation snapshots the callback before querying the credential
    ///     list. Changes made while that query is in flight apply only to later calculations.
    /// </remarks>
    /// <example>
    ///     <code>
    /// session.OnTouchRequired = () => Console.WriteLine("Touch your YubiKey now...");
    /// </code>
    /// </example>
    public Action? OnTouchRequired { get; set; }

    private HsmAuthSession(
        ISmartCardConnection connection,
        ScpKeyParameters? scpKeyParams = null)
        : base(connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _scpKeyParams = scpKeyParams;
    }

    /// <summary>
    ///     Factory helper that creates and initializes a YubiHSM Auth session.
    /// </summary>
    /// <param name="connection">The SmartCard connection to use.</param>
    /// <param name="options">Optional cross-cutting session creation settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized <see cref="HsmAuthSession" />.</returns>
    public static async Task<HsmAuthSession> CreateAsync(
        ISmartCardConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var configuration = options?.ProtocolConfiguration;
        var scpKeyParams = options?.ScpKeyParameters;
        var firmwareVersionOverride = options?.FirmwareVersionOverride;

        ValidatePreferredConnectionType(connection, options);

        // A session that fails to initialize must not keep its claim on the connection: the connection
        // outlives it, and the next session over it would otherwise be refused forever.
        var session = Construct(connection, () => new HsmAuthSession(connection, scpKeyParams));
        try
        {
            await session.InitializeAsync(configuration, firmwareVersionOverride, cancellationToken)
                .ConfigureAwait(false);
            return session;
        }
        catch
        {
            session.DisposeAfterInitializationFailure();
            throw;
        }
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration = null,
        FirmwareVersion? firmwareVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        var protocol = ProtocolFactory.Create((ISmartCardConnection)Connection);
        Protocol = protocol;
        IHsmAuthBackend backend = new HsmAuthBackend(protocol);

        var initializationFirmwareVersion = await backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var resolvedFirmwareVersion = firmwareVersion ?? initializationFirmwareVersion;

        var effectiveProtocol = (ISmartCardProtocol)await InitializeProtocolAsync(
                protocol,
                resolvedFirmwareVersion,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        if (!ReferenceEquals(protocol, effectiveProtocol))
        {
            backend = new HsmAuthBackend(effectiveProtocol);
        }

        _protocol = effectiveProtocol;
        _backend = backend;
    }

    /// <summary>
    ///     Validates a UTF-8 encoded credential password.
    /// </summary>
    /// <param name="passwordUtf8">The UTF-8 encoded password bytes.</param>
    /// <remarks>
    ///     The applet wire format carries a fixed 16-byte credential password. The SDK accepts at
    ///     most 16 bytes and null-pads shorter values in <see cref="ParseCredentialPassword" />.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when the password exceeds 16 bytes.</exception>
    internal static void ValidateCredentialPassword(ReadOnlySpan<byte> passwordUtf8)
    {
        if (passwordUtf8.Length > CredentialPasswordLength)
            throw new ArgumentException(
                $"Credential password UTF-8 encoding ({passwordUtf8.Length} bytes) exceeds maximum of {CredentialPasswordLength} bytes.",
                nameof(passwordUtf8));
    }

    /// <summary>
    ///     Validates a UTF-8 encoded credential password and copies it into a 16-byte buffer,
    ///     null-padding any remaining bytes.
    /// </summary>
    /// <param name="passwordUtf8">The UTF-8 encoded password bytes (at most 16).</param>
    /// <returns>
    ///     A newly allocated 16-byte array containing the padded password. The caller owns the
    ///     buffer and must zero it with <see cref="CryptographicOperations.ZeroMemory(Span{byte})" />.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the password exceeds 16 bytes.</exception>
    internal static byte[] ParseCredentialPassword(ReadOnlySpan<byte> passwordUtf8)
    {
        ValidateCredentialPassword(passwordUtf8);

        var buffer = new byte[CredentialPasswordLength];
        passwordUtf8.CopyTo(buffer);
        return buffer;
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
    internal static int? ExtractRetries(short sw) =>
        SWConstants.ExtractRetryCount(sw);

    // ─── IHsmAuthSession implementations ─────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<HsmAuthCredential>> ListCredentialsAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var command = new ApduCommand { Ins = InsList };
        var response = await _backend.SendAsync(command, cancellationToken: cancellationToken)
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
            var labelBytes = value[2..^1]; // Everything except algorithm, touch, and retries-remaining
            var retriesRemaining = value[^1];
            var label = Encoding.UTF8.GetString(labelBytes);

            credentials.Add(new HsmAuthCredential(label, algorithm, retriesRemaining, touchRequired));
        }

        return credentials;
    }

    /// <inheritdoc />
    public async Task PutCredentialSymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> keyEnc,
        ReadOnlyMemory<byte> keyMac,
        ReadOnlyMemory<byte> credentialPasswordUtf8,
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
        Memory<byte> data = default;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPasswordUtf8.Span);

            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagAlgorithm, [(byte)HsmAuthAlgorithm.Aes128YubicoAuthentication]),
                new Tlv(TagKeyEnc, keyEnc.Span),
                new Tlv(TagKeyMac, keyMac.Span),
                new Tlv(TagCredentialPassword, credPwBytes),
                new Tlv(TagTouch, [touchRequired ? (byte)0x01 : (byte)0x00]));

            var command = new ApduCommand { Ins = InsPut, Data = data };
            await TransmitWithRetryCheckAsync(
                command, ThrowOnManagementKeyFailure, "PUT credential", cancellationToken);
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
    public async Task PutCredentialDerivedAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> derivationPasswordUtf8,
        ReadOnlyMemory<byte> credentialPasswordUtf8,
        bool touchRequired = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (derivationPasswordUtf8.IsEmpty)
            throw new ArgumentException(
                "Derivation password must not be empty.",
                nameof(derivationPasswordUtf8));

        byte[]? derivedKey = null;
        try
        {
            derivedKey = DeriveKeys(derivationPasswordUtf8);

            await PutCredentialSymmetricAsync(
                    managementKey,
                    label,
                    derivedKey.AsMemory(0, 16),
                    derivedKey.AsMemory(16, 16),
                    credentialPasswordUtf8,
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

        Memory<byte> data = default;
        try
        {
            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagLabel, labelBytes));

            var command = new ApduCommand { Ins = InsDelete, Data = data };
            await TransmitWithRetryCheckAsync(
                command, ThrowOnManagementKeyFailure, "DELETE credential", cancellationToken);
        }
        finally
        {
            if (!data.IsEmpty)
                CryptographicOperations.ZeroMemory(data.Span);
        }
    }

    /// <inheritdoc />
    public async Task<SessionKeys> CalculateSessionKeysSymmetricAsync(
        string label,
        ReadOnlyMemory<byte> context,
        ReadOnlyMemory<byte> credentialPasswordUtf8,
        ReadOnlyMemory<byte>? cardCryptogram = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateContextLength(context, SymmetricContextLength);
        var labelBytes = ValidateAndEncodeLabel(label);

        await NotifyTouchIfRequiredAsync(label, cancellationToken).ConfigureAwait(false);

        byte[]? credPwBytes = null;
        Memory<byte> data = default;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPasswordUtf8.Span);

            var tlvs = new List<Tlv>
            {
                new(TagLabel, labelBytes),
                new(TagContext, context.Span)
            };

            if (cardCryptogram is { } cc)
                tlvs.Add(new Tlv(TagResponse, cc.Span));

            tlvs.Add(new Tlv(TagCredentialPassword, credPwBytes));

            data = TlvHelper.EncodeAndDisposeList([.. tlvs]);

            var command = new ApduCommand { Ins = InsCalculate, Data = data };
            var response = await TransmitWithRetryCheckAsync(
                command, ThrowOnCredentialPasswordFailure, "CALCULATE symmetric session keys", cancellationToken);

            try
            {
                return SessionKeys.Parse(response.Data.Span);
            }
            finally
            {
                ZeroApduResponse(response);
            }
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
    public async Task<int> GetManagementKeyRetriesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var command = new ApduCommand { Ins = InsGetManagementKeyRetries };
        var response = await _backend.SendAsync(command, cancellationToken: cancellationToken)
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

        Memory<byte> data = default;
        try
        {
            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagManagementKey, currentManagementKey.Span),
                new Tlv(TagManagementKey, newManagementKey.Span));

            var command = new ApduCommand { Ins = InsPutManagementKey, Data = data };
            await TransmitWithRetryCheckAsync(
                command, ThrowOnManagementKeyFailure, "PUT management key", cancellationToken);
        }
        finally
        {
            if (!data.IsEmpty)
                CryptographicOperations.ZeroMemory(data.Span);
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var command = new ApduCommand { Ins = InsReset, P1 = ResetP1, P2 = ResetP2 };
        await _backend.SendAsync(command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Re-SELECT the applet using the existing protocol to refresh cached state.
        // Do NOT create a new protocol here — that would abandon the current one without
        // disposing it, leaking the PCSC transaction and causing SW=0x6985 on next operation.
        var resolvedFirmwareVersion = await _backend.InitializeAsync(cancellationToken).ConfigureAwait(false);

        FirmwareVersion = resolvedFirmwareVersion;
    }

    /// <inheritdoc />
    public async Task<SessionKeys> CalculateSessionKeysAsymmetricAsync(
        string label,
        ReadOnlyMemory<byte> context,
        ReadOnlyMemory<byte> publicKey,
        ReadOnlyMemory<byte> credentialPasswordUtf8,
        ReadOnlyMemory<byte> cardCryptogram,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureAsymmetric);
        ValidateContextLength(context, AsymmetricContextLength);
        var labelBytes = ValidateAndEncodeLabel(label);

        await NotifyTouchIfRequiredAsync(label, cancellationToken).ConfigureAwait(false);

        byte[]? credPwBytes = null;
        Memory<byte> data = default;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPasswordUtf8.Span);

            // APDU payload order matches Python canonical SDK:
            // TAG_LABEL, TAG_CONTEXT, TAG_PUBLIC_KEY, TAG_RESPONSE, TAG_CREDENTIAL_PASSWORD
            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagContext, context.Span),
                new Tlv(TagPublicKey, publicKey.Span),
                new Tlv(TagResponse, cardCryptogram.Span),
                new Tlv(TagCredentialPassword, credPwBytes));

            var command = new ApduCommand { Ins = InsCalculate, Data = data };
            var response = await TransmitWithRetryCheckAsync(
                command, ThrowOnCredentialPasswordFailure, "CALCULATE asymmetric session keys", cancellationToken);

            try
            {
                return SessionKeys.Parse(response.Data.Span);
            }
            finally
            {
                ZeroApduResponse(response);
            }
        }
        finally
        {
            if (credPwBytes is not null)
                CryptographicOperations.ZeroMemory(credPwBytes);
            if (!data.IsEmpty)
                CryptographicOperations.ZeroMemory(data.Span);
        }
    }

    private static void ValidateContextLength(ReadOnlyMemory<byte> context, int expectedLength)
    {
        if (context.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Context must be exactly {expectedLength} bytes.",
                nameof(context));
        }
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetChallengeAsync(
        string label,
        ReadOnlyMemory<byte>? credentialPasswordUtf8 = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureGetChallenge);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? credPwBytes = null;
        Memory<byte> data = default;
        try
        {
            var tlvs = new List<Tlv> { new(TagLabel, labelBytes) };

            if (credentialPasswordUtf8 is { } credentialPassword &&
                IsSupported(FeatureGetChallengeWithPassword))
            {
                credPwBytes = ParseCredentialPassword(credentialPassword.Span);
                tlvs.Add(new Tlv(TagCredentialPassword, credPwBytes));
            }

            data = TlvHelper.EncodeAndDisposeList([.. tlvs]);

            var command = new ApduCommand { Ins = InsGetChallenge, Data = data };
            var response = await _backend.SendAsync(
                    command, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Data;
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
    public async Task PutCredentialAsymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> privateKey,
        ReadOnlyMemory<byte> credentialPasswordUtf8,
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
            credPwBytes = ParseCredentialPassword(credentialPasswordUtf8.Span);

            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagAlgorithm, [(byte)HsmAuthAlgorithm.EcP256YubicoAuthentication]),
                new Tlv(TagPrivateKey, privateKey.Span),
                new Tlv(TagCredentialPassword, credPwBytes),
                new Tlv(TagTouch, [touchRequired ? (byte)0x01 : (byte)0x00]));

            var command = new ApduCommand { Ins = InsPut, Data = data };
            await TransmitWithRetryCheckAsync(
                command, ThrowOnManagementKeyFailure, "PUT asymmetric credential", cancellationToken);
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
        ReadOnlyMemory<byte> credentialPasswordUtf8,
        bool touchRequired = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureAsymmetric);
        ValidateManagementKey(managementKey.Span);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? credPwBytes = null;
        Memory<byte> data = default;
        try
        {
            credPwBytes = ParseCredentialPassword(credentialPasswordUtf8.Span);

            // TAG_PRIVATE_KEY with empty value signals on-device key generation.
            // Python canonical: _put_credential(management_key, label, b"", EC_P256, credential_password)
            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagAlgorithm, [(byte)HsmAuthAlgorithm.EcP256YubicoAuthentication]),
                new Tlv(TagPrivateKey, ReadOnlySpan<byte>.Empty),
                new Tlv(TagCredentialPassword, credPwBytes),
                new Tlv(TagTouch, [touchRequired ? (byte)0x01 : (byte)0x00]));

            var command = new ApduCommand { Ins = InsPut, Data = data };
            await TransmitWithRetryCheckAsync(
                command, ThrowOnManagementKeyFailure, "GENERATE asymmetric credential", cancellationToken);
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
    public async Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        string label,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeatureAsymmetric);
        var labelBytes = ValidateAndEncodeLabel(label);

        var data = TlvHelper.EncodeAndDisposeList(new Tlv(TagLabel, labelBytes));

        var command = new ApduCommand { Ins = InsGetPublicKey, Data = data };
        var response = await _backend.SendAsync(
                command, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var publicKey = response.Data;
        if (publicKey.Length != EcP256PublicKeyLength)
            throw new InvalidOperationException(
                $"Expected {EcP256PublicKeyLength}-byte public key, got {publicKey.Length}");

        return publicKey;
    }

    /// <inheritdoc />
    public async Task ChangeCredentialPasswordAsync(
        string label,
        ReadOnlyMemory<byte> currentPasswordUtf8,
        ReadOnlyMemory<byte> newPasswordUtf8,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeaturePasswordChange);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? currentPwBytes = null;
        byte[]? newPwBytes = null;
        Memory<byte> data = default;
        try
        {
            currentPwBytes = ParseCredentialPassword(currentPasswordUtf8.Span);
            newPwBytes = ParseCredentialPassword(newPasswordUtf8.Span);

            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagCredentialPassword, currentPwBytes),
                new Tlv(TagCredentialPassword, newPwBytes));

            var command = new ApduCommand { Ins = InsChangeCredentialPassword, P1 = 0x00, Data = data };
            await TransmitWithRetryCheckAsync(
                command, ThrowOnCredentialPasswordFailure, "CHANGE credential password", cancellationToken);
        }
        finally
        {
            if (currentPwBytes is not null)
                CryptographicOperations.ZeroMemory(currentPwBytes);
            if (newPwBytes is not null)
                CryptographicOperations.ZeroMemory(newPwBytes);
            if (!data.IsEmpty)
                CryptographicOperations.ZeroMemory(data.Span);
        }
    }

    /// <inheritdoc />
    public async Task ChangeCredentialPasswordAdminAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> newPasswordUtf8,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureSupports(FeaturePasswordChange);
        ValidateManagementKey(managementKey.Span);
        var labelBytes = ValidateAndEncodeLabel(label);

        byte[]? newPwBytes = null;
        Memory<byte> data = default;
        try
        {
            newPwBytes = ParseCredentialPassword(newPasswordUtf8.Span);

            data = TlvHelper.EncodeAndDisposeList(
                new Tlv(TagLabel, labelBytes),
                new Tlv(TagManagementKey, managementKey.Span),
                new Tlv(TagCredentialPassword, newPwBytes));

            var command = new ApduCommand { Ins = InsChangeCredentialPassword, P1 = 0x01, Data = data };
            await TransmitWithRetryCheckAsync(
                command, ThrowOnManagementKeyFailure, "CHANGE credential password (admin)", cancellationToken);
        }
        finally
        {
            if (newPwBytes is not null)
                CryptographicOperations.ZeroMemory(newPwBytes);
            if (!data.IsEmpty)
                CryptographicOperations.ZeroMemory(data.Span);
        }
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    ///     Derives AES-128 key pair (K-ENC, K-MAC) from a password using PBKDF2-HMAC-SHA256.
    /// </summary>
    /// <param name="derivationPasswordUtf8">The UTF-8 encoded derivation password.</param>
    /// <returns>
    ///     A newly allocated 32-byte array: K-ENC in <c>[0..16]</c>, K-MAC in <c>[16..32]</c>.
    ///     The caller owns the buffer and must zero it.
    /// </returns>
    /// <remarks>
    ///     The caller owns <paramref name="derivationPasswordUtf8" /> and is responsible for
    ///     zeroing it; this method makes no copy of the input.
    /// </remarks>
    internal static byte[] DeriveKeys(ReadOnlyMemory<byte> derivationPasswordUtf8) =>
        Rfc2898DeriveBytes.Pbkdf2(
            derivationPasswordUtf8.Span,
            Pbkdf2Salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            Pbkdf2DerivedKeyLength);

    /// <summary>
    ///     Transmits an APDU command with <c>throwOnError: false</c>, checks for retry failures
    ///     using the specified checker, and throws <see cref="ApduException" /> if the response
    ///     does not indicate success.
    /// </summary>
    private async Task<ApduResponse> TransmitWithRetryCheckAsync(
        ApduCommand command,
        Action<ApduResponse, ApduCommand> retryChecker,
        string operationName,
        CancellationToken cancellationToken)
    {
        var response = await _backend.SendAsync(
                command, throwOnError: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        retryChecker(response, command);
        if (!response.IsOK())
            throw ApduException.FromResponse(response, command, $"{operationName} failed");

        return response;
    }

    /// <summary>
    ///     Notifies <see cref="OnTouchRequired" /> before a CALCULATE session-key exchange when
    ///     the target credential's touch requirement is set or cannot be determined.
    /// </summary>
    /// <remarks>
    ///     Short-circuits with no device I/O when no callback is registered, so callers who do
    ///     not opt in observe no behavior or performance change.
    /// </remarks>
    private async Task NotifyTouchIfRequiredAsync(string label, CancellationToken cancellationToken)
    {
        Action? callback = OnTouchRequired;
        if (callback is null)
            return;

        // The try/catch below guards only the credential-list query. callback.Invoke() is
        // called unconditionally outside of it so a throwing caller callback propagates normally
        // to the caller instead of being caught by the query's error handling, misdiagnosed as a
        // query failure, and invoked a second time.
        IReadOnlyList<HsmAuthCredential> credentials;
        try
        {
            credentials = await ListCredentialsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogDebug(
                ex, "YubiHSM Auth: failed to query credential list for touch policy, notifying conservatively");
            callback.Invoke();
            return;
        }

        var credential = credentials.FirstOrDefault(
            c => string.Equals(c.Label, label, StringComparison.Ordinal));

        // Unknown touch semantics (null) are treated conservatively: notify so the caller
        // can prompt the user before the blocking CALCULATE exchange. A missing credential
        // means the subsequent CALCULATE call will fail for an unrelated reason, so no
        // notification is warranted.
        if (credential is { TouchRequired: not false })
        {
            callback.Invoke();
        }
    }

    private static void ValidateManagementKey(ReadOnlySpan<byte> managementKey)
    {
        if (managementKey.Length != ManagementKeyLength)
            throw new ArgumentException(
                $"Management key must be exactly {ManagementKeyLength} bytes, got {managementKey.Length}.",
                nameof(managementKey));
    }

    internal static void ZeroApduResponse(ApduResponse response)
    {
        if (MemoryMarshal.TryGetArray(response.RawData, out var rawData))
            CryptographicOperations.ZeroMemory(rawData.AsSpan());
    }

    /// <summary>
    ///     Checks an APDU response for the 0x63Cx retry failure pattern and throws
    ///     <see cref="ApduException" /> with retry information if detected.
    /// </summary>
    private static void ThrowOnRetryFailure(ApduResponse response, ApduCommand command, string errorContext)
    {
        var retries = ExtractRetries(response.SW);
        if (retries is null)
            return;

        throw new HsmAuthRetryException(
            retries.Value,
            $"{errorContext}, {retries} attempt(s) remaining (SW=0x{response.SW:X4})")
        {
            SW = response.SW,
            Cla = command.Cla,
            Ins = command.Ins,
            P1 = command.P1,
            P2 = command.P2
        };
    }

    /// <summary>
    ///     Checks an APDU response for the 0x63Cx management key verification failure pattern.
    ///     Throws <see cref="ApduException" /> with retry information if detected.
    /// </summary>
    private static void ThrowOnManagementKeyFailure(ApduResponse response, ApduCommand command) =>
        ThrowOnRetryFailure(response, command, "Management key verification failed");

    /// <summary>
    ///     Checks an APDU response for the 0x63Cx credential password verification failure pattern.
    ///     Throws <see cref="ApduException" /> with retry information if detected.
    ///     Matches the Python SDK behavior in <c>_calculate_session_keys</c>.
    /// </summary>
    private static void ThrowOnCredentialPasswordFailure(ApduResponse response, ApduCommand command) =>
        ThrowOnRetryFailure(response, command, "Invalid credential password");
}
