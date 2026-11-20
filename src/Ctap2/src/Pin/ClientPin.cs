// Copyright 2025 Yubico AB
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

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Cbor;
using static Yubico.YubiKit.Ctap2.Cbor.CoseKeyWriter;

namespace Yubico.YubiKit.Ctap2.Pin;

/// <summary>
/// Provides PIN and UV token management operations for CTAP2 authenticators.
/// </summary>
/// <remarks>
/// <para>
/// ClientPin handles the CTAP2 authenticatorClientPin command, which manages:
/// <list type="bullet">
///   <item><description>Setting and changing PINs</description></item>
///   <item><description>Getting PIN and UV retry counts</description></item>
///   <item><description>Obtaining PIN/UV auth tokens for credential operations</description></item>
/// </list>
/// </para>
/// <para>
/// Select a mutually supported protocol from authenticatorGetInfo's PinUvAuthProtocols before
/// constructing this helper. The helper does not negotiate V1/V2. It borrows the session but owns
/// disposal of the supplied protocol; keep the session open until all helper operations complete.
/// Each PIN set/change or token request performs fresh key agreement.
/// </para>
/// <para>
/// Supply PINs as NFC-normalized UTF-8, with at least four Unicode characters and no final 0x00
/// byte. For a new PIN, also meet the authenticator's current minimum in Unicode code points and
/// any additional device policy. The SDK checks only 4–63 input bytes: it does not validate UTF-8,
/// normalization, code-point count, a final zero byte or the advertised minimum. It hashes the
/// exact supplied bytes, so do not append padding to a PIN argument.
/// </para>
/// <para>
/// If GetInfo reports forcePINChange=true, change the PIN before requesting a PIN token.
/// Under original CTAP 2.1, after verifying the correct PIN, legacy getPinToken (0x05) returns
/// PIN_INVALID for forced change, while getPinUvAuthTokenUsingPinWithPermissions (0x09) returns
/// PIN_POLICY_VIOLATION. PIN_INVALID alone proves neither an incorrect PIN nor forced-change state.
/// This helper does not preflight forcePINChange or automatically retry PIN requests.
/// </para>
/// <para>
/// PIN arguments remain caller-owned. Keep them unchanged until the returned task completes,
/// faults or is canceled, then clear them when no longer needed. Returned tokens are separate
/// caller-owned secrets; clear them after their last use. The helper clears its named shared-secret,
/// PIN-hash and padded-PIN buffers in finally blocks, not every builder/request/response copy.
/// Disposal does not provide end-to-end secret erasure or invalidate tokens on the authenticator.
/// </para>
/// <para>
/// The authenticator enforces token scope and lifetime. Ordinary tokens can expire, lose permissions
/// after user-presence checks, or be invalidated by another token issuance, PIN change, power cycle
/// or reset. A host byte array remaining readable is not evidence that its token is still authorized.
/// When authorizing MakeCredential or GetAssertion with pinUvAuthParam, omit the uv option key,
/// even false. Inspect the returned authenticator data for the operation's verification result.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorClientPIN
/// </para>
/// </remarks>
public sealed class ClientPin : IDisposable
{
    private const int PinMinLength = 4;
    private const int PinMaxLength = 63;
    private const int PinBlockSize = 64;

    private readonly ICtap2Session _session;
    private readonly IPinUvAuthProtocol _protocol;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientPin"/> class.
    /// </summary>
    /// <param name="session">The borrowed CTAP2 session. This helper does not dispose it.</param>
    /// <param name="protocol">The caller-selected, mutually supported protocol. Disposing this helper disposes this supplied object; do not share it with a longer-lived consumer.</param>
    /// <exception cref="ArgumentNullException">If session or protocol is null.</exception>
    public ClientPin(ICtap2Session session, IPinUvAuthProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(protocol);

        _session = session;
        _protocol = protocol;
    }

    /// <summary>
    /// Gets the PIN/UV auth protocol used by this instance.
    /// </summary>
    /// <remarks>This property remains readable after disposal and returns the same, then-disposed protocol reference. It does not extend that protocol's usable lifetime.</remarks>
    public IPinUvAuthProtocol Protocol => _protocol;

    /// <summary>
    /// Reads a snapshot of the PIN retry count and optional power-cycle state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticator's current PIN retry status.</returns>
    /// <remarks>
    /// Missing response fields currently default to zero retries and false power-cycle state;
    /// false therefore does not prove another attempt is safe. PIN_AUTH_BLOCKED indicates a
    /// temporary block requiring a power cycle, whereas PIN_BLOCKED indicates exhausted retries
    /// requiring reset. Do not blindly retry an incorrect PIN.
    /// </remarks>
    public async Task<PinRetryStatus> GetPinRetriesAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
            .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetRetries)
            .Build();

        var response = await _session.SendCborRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var (retriesRemaining, powerCycleRequired) = ParsePinRetriesResponse(response);
        return new PinRetryStatus(retriesRemaining, powerCycleRequired);
    }

    /// <summary>
    /// Reads a snapshot of the built-in user-verification retry count (CTAP 2.1).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticator's current user-verification retry status.</returns>
    /// <remarks>
    /// Missing counts currently default to zero. CTAP 2.1 defines no power-cycle field for this
    /// response; the result's false default is not recovery evidence. Exhausted built-in UV retries
    /// can be re-enabled by reset or a successful configured ClientPIN token request. This method
    /// does not attempt recovery or decide whether the caller should fall back to PIN.
    /// </remarks>
    public async Task<UserVerificationRetryStatus> GetUvRetriesAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
            .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetUvRetries)
            .Build();

        var response = await _session.SendCborRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var (retriesRemaining, powerCycleRequired) = ParseUvRetriesResponse(response);
        return new UserVerificationRetryStatus(retriesRemaining, powerCycleRequired);
    }

    /// <summary>
    /// Sets a new PIN on the authenticator (first-time setup).
    /// </summary>
    /// <param name="newPin">
    ///     The borrowed, unpadded NFC UTF-8 new PIN, 4–63 bytes. Meet the current device minimum
    ///     in code points and additional policy; the SDK checks byte length only. Keep the buffer
    ///     unchanged through task completion and clear it after its last use, including on failure.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">If the PIN byte length is invalid.</exception>
    /// <exception cref="CtapException">If the authenticator rejects setup, for example because a PIN is already set or device PIN policy is not met.</exception>
    /// <remarks>The PIN must not end in 0x00. This method right-zero-pads it to exactly 64 bytes before encryption; AES adds no padding.</remarks>
    public async Task SetPinAsync(ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidatePin(newPin, nameof(newPin));

        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);

        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);

        byte[]? pinBytes = null;
        byte[]? newPinEnc = null;
        byte[]? pinUvAuthParam = null;
        try
        {
            // Right-zero-pad the unpadded PIN to 64 bytes; AES itself uses no padding.
            pinBytes = PadPin(newPin.Span);
            newPinEnc = _protocol.Encrypt(sharedSecret, pinBytes);

            // Compute pinUvAuthParam = authenticate(sharedSecret, newPinEnc)
            pinUvAuthParam = _protocol.Authenticate(sharedSecret, newPinEnc);

            var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.SetPin)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithBytes(ClientPinParam.NewPinEnc, newPinEnc)
                .WithBytes(ClientPinParam.PinUvAuthParam, pinUvAuthParam)
                .Build();

            await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
            if (pinBytes is not null) CryptographicOperations.ZeroMemory(pinBytes);
            if (newPinEnc is not null) CryptographicOperations.ZeroMemory(newPinEnc);
            if (pinUvAuthParam is not null) CryptographicOperations.ZeroMemory(pinUvAuthParam);
        }
    }

    /// <summary>
    /// Changes the existing PIN on the authenticator.
    /// </summary>
    /// <param name="currentPin">
    ///     The borrowed, unpadded NFC UTF-8 current PIN, 4–63 bytes, with no final 0x00.
    ///     Keep it unchanged through task completion and clear it after its last use, including on failure.
    /// </param>
    /// <param name="newPin">
    ///     The borrowed, unpadded NFC UTF-8 new PIN, 4–63 bytes, with no final 0x00. Meet the
    ///     current device minimum in code points and additional policy; the SDK checks byte length
    ///     only. Keep it unchanged through task completion and clear it after its last use.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">If either PIN byte length is invalid.</exception>
    /// <exception cref="CtapException">If the authenticator rejects the change, including an incorrect/blocked current PIN or a new PIN that violates policy.</exception>
    /// <remarks>
    /// A successful change invalidates existing authenticator tokens. This helper does not retain
    /// authorization state or roll the change back; a failed or canceled call is not proof that
    /// the device PIN remained unchanged.
    /// </remarks>
    public async Task ChangePinAsync(
        ReadOnlyMemory<byte> currentPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidatePin(currentPin, nameof(currentPin));
        ValidatePin(newPin, nameof(newPin));

        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);

        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);

        byte[]? pinHashEnc = null;
        byte[]? newPinBytes = null;
        byte[]? newPinEnc = null;
        byte[]? message = null;
        byte[]? currentPinHash = null;
        byte[]? pinUvAuthParam = null;
        try
        {
            // Compute PIN hash for current PIN: LEFT(SHA-256(currentPin), 16)
            currentPinHash = ComputePinHash(currentPin.Span);
            pinHashEnc = _protocol.Encrypt(sharedSecret, currentPinHash);

            // Right-zero-pad the new PIN to 64 bytes before no-padding AES encryption.
            newPinBytes = PadPin(newPin.Span);
            newPinEnc = _protocol.Encrypt(sharedSecret, newPinBytes);

            // Compute pinUvAuthParam = authenticate(sharedSecret, newPinEnc || pinHashEnc)
            message = new byte[newPinEnc.Length + pinHashEnc.Length];
            newPinEnc.CopyTo(message.AsSpan());
            pinHashEnc.CopyTo(message.AsSpan(newPinEnc.Length));

            pinUvAuthParam = _protocol.Authenticate(sharedSecret, message);

            var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.ChangePin)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithBytes(ClientPinParam.PinHashEnc, pinHashEnc)
                .WithBytes(ClientPinParam.NewPinEnc, newPinEnc)
                .WithBytes(ClientPinParam.PinUvAuthParam, pinUvAuthParam)
                .Build();

            await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
            if (currentPinHash is not null) CryptographicOperations.ZeroMemory(currentPinHash);
            if (pinHashEnc is not null) CryptographicOperations.ZeroMemory(pinHashEnc);
            if (newPinEnc is not null) CryptographicOperations.ZeroMemory(newPinEnc);
            if (message is not null) CryptographicOperations.ZeroMemory(message);
            if (newPinBytes is not null) CryptographicOperations.ZeroMemory(newPinBytes);
            if (pinUvAuthParam is not null) CryptographicOperations.ZeroMemory(pinUvAuthParam);
        }
    }

    /// <summary>
    /// Requests a token with the legacy getPinToken subcommand using the PIN.
    /// </summary>
    /// <param name="pin">
    ///     The borrowed, unpadded NFC UTF-8 PIN, 4–63 bytes, with no final 0x00. The SDK checks
    ///     byte length only. Keep it unchanged through task completion and clear it after its last use.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new caller-owned decrypted token. Clear it after its last use, including if a subsequent operation fails. CTAP V1 tokens are 16 or 32 bytes; V2 tokens are 32 bytes. This helper does not validate the decrypted token length.</returns>
    /// <exception cref="ArgumentException">If the PIN byte length is outside 4–63 bytes.</exception>
    /// <exception cref="CtapException">If the PIN is incorrect.</exception>
    /// <remarks>
    /// Use <see cref="IPinUvAuthProtocol.Authenticate"/> to compute pinUvAuthParam, not to
    /// establish device authorization. Under CTAP 2.1, legacy tokens receive default MakeCredential
    /// and GetAssertion permissions and bind to the RP on first use, unless
    /// noMcGaPermissionsWithClientPin suppresses those permissions. This command sends no explicit
    /// permissions or RP ID; it does not grant arbitrary administrative permissions. PIN token
    /// issuance does not itself establish user presence.
    /// </remarks>
    public async Task<byte[]> GetPinTokenAsync(
        ReadOnlyMemory<byte> pin,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidatePin(pin, nameof(pin));

        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);

        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);

        byte[]? pinHashEnc = null;
        byte[]? pinHash = null;
        try
        {
            // Compute PIN hash: LEFT(SHA-256(pin), 16)
            pinHash = ComputePinHash(pin.Span);
            pinHashEnc = _protocol.Encrypt(sharedSecret, pinHash);

            var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetPinToken)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithBytes(ClientPinParam.PinHashEnc, pinHashEnc)
                .Build();

            var response = await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);

            var encryptedToken = ParsePinTokenResponse(response);
            return _protocol.Decrypt(sharedSecret, encryptedToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
            if (pinHash is not null) CryptographicOperations.ZeroMemory(pinHash);
            if (pinHashEnc is not null) CryptographicOperations.ZeroMemory(pinHashEnc);
        }
    }

    /// <summary>
    /// Requests a PIN/UV auth token using PIN, with explicit permissions when supported.
    /// </summary>
    /// <param name="pin">
    ///     The borrowed, unpadded NFC UTF-8 PIN, 4–63 bytes, with no final 0x00. The SDK checks
    ///     byte length only. Keep it unchanged through task completion and clear it after its last use.
    /// </param>
    /// <param name="permissions">The nonzero permissions to request. The caller must choose a valid combination supported by the authenticator; only None is locally rejected.</param>
    /// <param name="rpId">Required for MakeCredential/GetAssertion permissions; optional to restrict CredentialManagement to one RP; ignored for BioEnrollment, LargeBlobWrite, AuthenticatorConfig and CredentialManagementRO. Null or empty omits the field.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new caller-owned decrypted token. Clear it after its last use, including on subsequent failure. CTAP V1 tokens are 16 or 32 bytes; V2 tokens are 32 bytes; decrypted length is not checked here. The array carries no permission, RP, expiry or persistence metadata.</returns>
    /// <exception cref="ArgumentException">If the PIN byte length is outside 4–63 bytes or permissions is None.</exception>
    /// <exception cref="CtapException">If the PIN is incorrect or permissions are invalid.</exception>
    /// <remarks>
    /// <para>
    /// This method calls the supplied session's GetInfoAsync. With a concrete Ctap2Session,
    /// that is a fresh authenticator query. If and only if options contains pinUvAuthToken=true,
    /// it sends getPinUvAuthTokenUsingPinWithPermissions (0x09). Otherwise it calls legacy
    /// getPinToken (0x05), dropping the requested permissions and RP ID. The supplied protocol
    /// version is unchanged, and a failed 0x09 exchange is not retried with 0x05.
    /// </para>
    /// <para>
    /// Permission-token issuance requires a configured clientPin and pinUvAuthToken=true.
    /// The authenticator also checks operation capabilities and noMcGaPermissionsWithClientPin;
    /// this helper does not preflight these checks or enforce RP requirements. PIN acquisition
    /// does not establish user presence, and no PIN retry loop is performed.
    /// </para>
    /// <para>
    /// CredentialManagementRO is CTAP 2.2's persistent read-only permission (pcmr). Request it
    /// alone, only with perCredMgmtRO=true; the authenticator ignores RP ID. Ordinary token timers
    /// are not its lifetime contract. This helper does not check those conditions or model persistent
    /// token identity; legacy fallback cannot request pcmr.
    /// </para>
    /// </remarks>
    public async Task<byte[]> GetPinUvAuthTokenUsingPinAsync(
        ReadOnlyMemory<byte> pin,
        PinUvAuthTokenPermissions permissions,
        string? rpId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ValidatePin(pin, nameof(pin));

        if (permissions == PinUvAuthTokenPermissions.None)
        {
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));
        }

        // Choose the subcommand from GetInfo, not the protocol version. Concrete Ctap2Session
        // queries fresh info; absent/false pinUvAuthToken drops permissions and RP on legacy 0x05.
        var info = await _session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
        bool supportsTokenWithPermissions = info.Options.TryGetValue("pinUvAuthToken", out var supported) && supported;

        if (!supportsTokenWithPermissions)
        {
            return await GetPinTokenAsync(pin, cancellationToken).ConfigureAwait(false);
        }

        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);

        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);

        byte[]? pinHashEnc = null;
        byte[]? pinHash = null;
        try
        {
            // Compute PIN hash: LEFT(SHA-256(pin), 16)
            pinHash = ComputePinHash(pin.Span);
            pinHashEnc = _protocol.Encrypt(sharedSecret, pinHash);

            var builder = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetPinUvAuthTokenUsingPinWithPermissions)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithBytes(ClientPinParam.PinHashEnc, pinHashEnc)
                .WithUInt(ClientPinParam.Permissions, (uint)permissions);

            if (!string.IsNullOrEmpty(rpId))
            {
                builder.WithString(ClientPinParam.RpId, rpId);
            }

            var request = builder.Build();
            var response = await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);

            var encryptedToken = ParsePinTokenResponse(response);
            return _protocol.Decrypt(sharedSecret, encryptedToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
            if (pinHash is not null) CryptographicOperations.ZeroMemory(pinHash);
            if (pinHashEnc is not null) CryptographicOperations.ZeroMemory(pinHashEnc);
        }
    }

    /// <summary>
    /// Requests a permission token using the authenticator's configured built-in user verification.
    /// </summary>
    /// <param name="permissions">The nonzero permissions to request. Choose a valid, supported combination; CredentialManagementRO must be requested alone.</param>
    /// <param name="rpId">Required for MakeCredential/GetAssertion; optional to scope CredentialManagement; ignored for BioEnrollment, LargeBlobWrite, AuthenticatorConfig and CredentialManagementRO. Null or empty omits the field.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new caller-owned decrypted token. Clear it after its last use, including on subsequent failure. CTAP V1 tokens are 16 or 32 bytes; V2 tokens are 32 bytes; decrypted length is not checked here. No scope or persistence metadata accompanies the array.</returns>
    /// <exception cref="ArgumentException">If permissions is None.</exception>
    /// <exception cref="CtapException">If UV fails or permissions are invalid.</exception>
    /// <remarks>
    /// <para>
    /// Use this method when GetInfo reports both uv=true and pinUvAuthToken=true and built-in
    /// UV, such as biometrics, is configured. CTAP 2.1 requires the authenticator to implement
    /// this subcommand when both option IDs are true; it does not require the caller to invoke it.
    /// Operation capabilities also apply; BioEnrollment and AuthenticatorConfig additionally
    /// require uvBioEnroll and uvAcfg respectively. This helper checks only nonzero permissions;
    /// it does not query GetInfo or preflight these capabilities, configuration or RP requirements.
    /// </para>
    /// <para>
    /// The authenticator performs the verification interaction and may establish user presence.
    /// This helper uses generic CBOR exchange, so no operation-specific SDK touch notification is
    /// guaranteed. It performs one token request without an automatic UV retry or PIN fallback;
    /// the caller or higher-level client decides recovery from UV errors.
    /// </para>
    /// <para>
    /// CredentialManagementRO requests CTAP 2.2's persistent read-only token only when
    /// perCredMgmtRO=true and no other permission is requested. RP ID is ignored. The helper
    /// does not enforce those conditions or manage the persistent token's lifetime.
    /// </para>
    /// </remarks>
    public async Task<byte[]> GetPinUvAuthTokenUsingUvAsync(
        PinUvAuthTokenPermissions permissions,
        string? rpId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        if (permissions == PinUvAuthTokenPermissions.None)
        {
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));
        }

        // Get authenticator's key agreement key
        var authenticatorKey = await GetKeyAgreementAsync(cancellationToken)
            .ConfigureAwait(false);

        // Perform ECDH key agreement
        var (platformKey, sharedSecret) = _protocol.Encapsulate(authenticatorKey);

        try
        {
            var builder = CtapRequestBuilder.Create(CtapCommand.ClientPin)
                .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
                .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetPinUvAuthTokenUsingUvWithPermissions)
                .WithMap(ClientPinParam.KeyAgreement, writer => WriteCoseKey(writer, platformKey))
                .WithUInt(ClientPinParam.Permissions, (uint)permissions);

            if (!string.IsNullOrEmpty(rpId))
            {
                builder.WithString(ClientPinParam.RpId, rpId);
            }

            var request = builder.Build();
            var response = await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);

            var encryptedToken = ParsePinTokenResponse(response);
            return _protocol.Decrypt(sharedSecret, encryptedToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
        }
    }

    /// <summary>
    /// Gets the authenticator's key agreement public key.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The COSE public key as a dictionary.</returns>
    internal async Task<Dictionary<int, object?>> GetKeyAgreementAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(ClientPinParam.PinUvAuthProtocol, _protocol.Version)
            .WithInt(ClientPinParam.SubCommand, ClientPinSubCommand.GetKeyAgreement)
            .Build();

        var response = await _session.SendCborRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return ParseKeyAgreementResponse(response);
    }

    /// <summary>
    ///     Checks only the PIN argument's byte length, not its Unicode encoding or device policy.
    /// </summary>
    /// <param name="pin">The supplied, unpadded PIN bytes.</param>
    /// <param name="paramName">
    ///     The name of the public parameter <paramref name="pin" /> was supplied through. The
    ///     public surface spells this parameter three different ways — <c>pin</c>,
    ///     <c>currentPin</c> and <c>newPin</c> — so the caller has to name its own, otherwise
    ///     <see cref="ArgumentException.ParamName" /> would report this helper's local name,
    ///     which matches nothing a caller can filter on.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown when the supplied data is shorter than <see cref="PinMinLength" /> bytes or
    ///     longer than <see cref="PinMaxLength" /> bytes.
    /// </exception>
    private static void ValidatePin(ReadOnlyMemory<byte> pin, string paramName)
    {
        if (pin.Length < PinMinLength)
        {
            throw new ArgumentException(
                $"PIN must be at least {PinMinLength} bytes.", paramName);
        }

        if (pin.Length > PinMaxLength)
        {
            throw new ArgumentException(
                $"PIN must not exceed {PinMaxLength} bytes.", paramName);
        }
    }

    private static byte[] PadPin(ReadOnlySpan<byte> pin)
    {
        // Zero-initialized tail supplies CTAP's right padding, not PKCS#7.
        var padded = new byte[PinBlockSize];
        pin.CopyTo(padded);
        return padded;
    }

    private static byte[] ComputePinHash(ReadOnlySpan<byte> pin)
    {
        Span<byte> hash = stackalloc byte[32];
        try
        {
            SHA256.HashData(pin, hash);
            return hash[..16].ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
        }
    }

    private static (int Retries, bool PowerCycleRequired) ParsePinRetriesResponse(ReadOnlyMemory<byte> data)
    {
        // Omitted powerCycleState means unknown in CTAP; this model collapses it to false.
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        var mapLength = reader.ReadStartMap();

        int retries = 0;
        var powerCycleRequired = false;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();

            switch (key)
            {
                case ClientPinResponse.PinRetries:
                    retries = reader.ReadInt32();
                    break;
                case ClientPinResponse.PowerCycleState:
                    powerCycleRequired = reader.ReadBoolean();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();
        return (retries, powerCycleRequired);
    }

    private static (int Retries, bool PowerCycleRequired) ParseUvRetriesResponse(ReadOnlyMemory<byte> data)
    {
        // CTAP 2.1 defines no UV powerCycleState; current parsing still accepts field 4.
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        var mapLength = reader.ReadStartMap();

        int retries = 0;
        var powerCycleRequired = false;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();

            switch (key)
            {
                case ClientPinResponse.UvRetries:
                    retries = reader.ReadInt32();
                    break;
                case ClientPinResponse.PowerCycleState:
                    powerCycleRequired = reader.ReadBoolean();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();
        return (retries, powerCycleRequired);
    }

    private static Dictionary<int, object?> ParseKeyAgreementResponse(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        var mapLength = reader.ReadStartMap();

        Dictionary<int, object?>? keyAgreement = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();

            if (key == ClientPinResponse.KeyAgreement)
            {
                keyAgreement = ParseCoseKey(reader);
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap();

        return keyAgreement ?? throw new InvalidOperationException(
            "Response missing required keyAgreement field.");
    }

    private static byte[] ParsePinTokenResponse(ReadOnlyMemory<byte> data)
    {
        // This encrypted-token copy is not cleared here; the caller receives separate plaintext.
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        var mapLength = reader.ReadStartMap();

        byte[]? pinToken = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();

            if (key == ClientPinResponse.PinUvAuthToken)
            {
                pinToken = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap();

        return pinToken ?? throw new InvalidOperationException(
            "Response missing required pinUvAuthToken field.");
    }

    private static Dictionary<int, object?> ParseCoseKey(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();
        var result = new Dictionary<int, object?>((int)mapLength!);

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();

            switch (reader.PeekState())
            {
                case CborReaderState.UnsignedInteger:
                case CborReaderState.NegativeInteger:
                    result[key] = reader.ReadInt32();
                    break;
                case CborReaderState.ByteString:
                    result[key] = reader.ReadByteString();
                    break;
                case CborReaderState.TextString:
                    result[key] = reader.ReadTextString();
                    break;
                case CborReaderState.Null:
                    reader.ReadNull();
                    result[key] = null;
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();
        return result;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>Disposes the supplied protocol and marks this helper disposed, without disposing the borrowed session.</summary>
    /// <remarks>Repeated disposal has no effect. Operations reject subsequent use; Protocol remains readable. Caller PINs and returned tokens are not cleared by this method.</remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        _protocol.Dispose();
        _disposed = true;
    }
}