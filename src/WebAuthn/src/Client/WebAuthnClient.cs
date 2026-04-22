// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Client.Validation;
using Yubico.YubiKit.WebAuthn.Cose;
using Fido2AttestationStatement = Yubico.YubiKit.Fido2.Credentials.AttestationStatement;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// WebAuthn Client for high-level credential registration and authentication.
/// </summary>
/// <remarks>
/// This client wraps CTAP2 operations and handles WebAuthn protocol details like
/// clientDataJSON construction, RP ID validation, UV/PIN token acquisition, and retry logic.
/// </remarks>
public sealed class WebAuthnClient : IAsyncDisposable
{
    private readonly IWebAuthnBackend _backend;
    private readonly WebAuthnOrigin _origin;
    private readonly Func<string, bool> _isPublicSuffix;
    private readonly IReadOnlySet<string> _enterpriseRpIds;
    private bool _disposed;

    private const int MaxPinAuthRetries = 3;

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClient"/>.
    /// </summary>
    /// <param name="backend">The backend that performs CTAP2 operations (ownership transferred).</param>
    /// <param name="origin">The WebAuthn origin for this client.</param>
    /// <param name="isPublicSuffix">Predicate to determine if a domain is a public suffix.</param>
    /// <param name="enterpriseRpIds">Optional set of enterprise-allowed RP IDs.</param>
    public WebAuthnClient(
        IWebAuthnBackend backend,
        WebAuthnOrigin origin,
        Func<string, bool> isPublicSuffix,
        IReadOnlySet<string>? enterpriseRpIds = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        _isPublicSuffix = isPublicSuffix ?? throw new ArgumentNullException(nameof(isPublicSuffix));
        _enterpriseRpIds = enterpriseRpIds ?? new HashSet<string>();
    }

    /// <summary>
    /// Creates a new WebAuthn credential via CTAP2 MakeCredential.
    /// </summary>
    /// <param name="options">The registration options.</param>
    /// <param name="pinBytes">Optional PIN bytes (UTF-8 encoded).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration response with credential details.</returns>
    /// <exception cref="WebAuthnClientError">Thrown on validation or operation failure.</exception>
    public async Task<RegistrationResponse> MakeCredentialAsync(
        RegistrationOptions options,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);


        // Validate options
        ValidateRegistrationOptions(options);

        // Validate RP ID against origin
        RpIdValidator.EnsureValid(options.Rp.Id, _origin, _enterpriseRpIds, _isPublicSuffix);

        // Build client data
        var clientData = WebAuthnClientData.Create(
            type: "webauthn.create",
            challenge: options.Challenge,
            origin: _origin,
            crossOrigin: options.CrossOrigin,
            topOrigin: options.TopOrigin);


        // Get authenticator info
        var info = await _backend.GetCachedInfoAsync(cancellationToken).ConfigureAwait(false);

        // Determine UV/PIN strategy
        var uvDecision = UvDecisionLogic.Decide(
            info,
            options.UserVerification,
            pinAvailable: pinBytes is not null,
            requestedPermissions: PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion);


        // Acquire PIN/UV token with retry on PinAuthInvalid
        PinUvAuthTokenSession? tokenSession = null;
        IMemoryOwner<byte>? pinOwner = null;

        try
        {
            // Copy PIN bytes into a secure buffer for the duration of this operation
            if (pinBytes is not null)
            {
                pinOwner = MemoryPool<byte>.Shared.Rent(pinBytes.Value.Length);
                pinBytes.Value.Span.CopyTo(pinOwner.Memory.Span);
            }

            if (uvDecision.UseToken)
            {
                tokenSession = await AcquirePinUvTokenWithRetryAsync(
                    uvDecision.Method!.Value,
                    uvDecision.Permissions,
                    options.Rp.Id,
                    pinOwner?.Memory.Slice(0, pinBytes!.Value.Length),
                    cancellationToken).ConfigureAwait(false);
            }

            // Build backend request
            var request = BuildMakeCredentialRequest(options, clientData, tokenSession, uvDecision);

            // Execute MakeCredential
            var ctapResponse = await _backend.MakeCredentialAsync(request, progress: null, cancellationToken)
                .ConfigureAwait(false);

            // Build WebAuthn response
            return BuildRegistrationResponse(ctapResponse, clientData);
        }
        finally
        {
            tokenSession?.Dispose();

            if (pinOwner is not null)
            {
                CryptographicOperations.ZeroMemory(pinOwner.Memory.Span);
                pinOwner.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _backend.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }

    private static void ValidateRegistrationOptions(RegistrationOptions options)
    {
        if (options.Challenge.Length == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "Challenge cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(options.Rp.Id))
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "RP ID cannot be null or empty");
        }

        if (options.User.Id.Length is < 1 or > 64)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                $"User ID length must be 1-64 bytes, got {options.User.Id.Length}");
        }

        if (options.PubKeyCredParams.Count == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "At least one public key credential parameter is required");
        }
    }

    private async Task<PinUvAuthTokenSession> AcquirePinUvTokenWithRetryAsync(
        PinUvAuthMethod method,
        PinUvAuthTokenPermissions permissions,
        string rpId,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        PinUvAuthTokenSession? previousSession = null;

        while (attempt < MaxPinAuthRetries)
        {
            attempt++;

            try
            {

                var session = await _backend.GetPinUvTokenAsync(
                    method,
                    permissions,
                    rpId,
                    pinBytes,
                    progress: null,
                    cancellationToken).ConfigureAwait(false);

                return session;
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.PinAuthInvalid && attempt < MaxPinAuthRetries)
            {

                // Dispose the previous session (if any) to zero the token
                previousSession?.Dispose();
                previousSession = null;

                // Continue to next attempt
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.PinAuthInvalid)
            {
                // Max retries exhausted
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.Security,
                    $"PIN/UV authentication failed after {MaxPinAuthRetries} attempts",
                    ex);
            }
        }

        // Should not reach here
        throw new WebAuthnClientError(
            WebAuthnClientErrorCode.Unknown,
            "Unexpected state in PIN/UV token acquisition");
    }

    private BackendMakeCredentialRequest BuildMakeCredentialRequest(
        RegistrationOptions options,
        WebAuthnClientData clientData,
        PinUvAuthTokenSession? tokenSession,
        UvDecision uvDecision)
    {
        // Map options to backend request
        var optionsDict = new Dictionary<string, bool>();

        if (options.ResidentKey == Preferences.ResidentKeyPreference.Required)
        {
            optionsDict["rk"] = true;
        }

        if (uvDecision.UvOption is not null)
        {
            optionsDict["uv"] = uvDecision.UvOption.Value;
        }

        // Compute PIN/UV auth parameter if we have a token
        ReadOnlyMemory<byte>? pinUvAuthParam = null;
        byte? pinUvAuthProtocol = null;

        if (tokenSession is not null)
        {
            var authParam = tokenSession.Protocol.Authenticate(tokenSession.Token, clientData.Hash.Span);
            pinUvAuthParam = authParam;
            pinUvAuthProtocol = (byte)tokenSession.Protocol.Version;
        }

        return new BackendMakeCredentialRequest
        {
            ClientDataHash = clientData.Hash,
            Rp = options.Rp,
            User = options.User,
            PubKeyCredParams = options.PubKeyCredParams
                .Select(alg => new PublicKeyCredentialParameters { Algorithm = (CoseAlgorithmIdentifier)alg.Value })
                .ToList(),
            ExcludeList = options.ExcludeCredentials,
            Options = optionsDict.Count > 0 ? optionsDict : null,
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = pinUvAuthProtocol
        };
    }

    private RegistrationResponse BuildRegistrationResponse(
        MakeCredentialResponse ctapResponse,
        WebAuthnClientData clientData)
    {
        // Extract attested credential data from AuthenticatorData
        var attestedCred = ctapResponse.AuthenticatorData.AttestedCredentialData!;

        // Decode public key from COSE
        var publicKey = CoseKey.Decode(attestedCred.CredentialPublicKey);

        // Build WebAuthn attestation statement from CTAP response
        var webAuthnStatement = Attestation.AttestationStatement.Decode(
            new AttestationFormat(ctapResponse.Format),
            ctapResponse.AttestationStatement.RawData);

        // Wrap authenticator data
        var webAuthnAuthData = WebAuthnAuthenticatorData.Decode(ctapResponse.AuthenticatorDataRaw);

        // Build raw attestation object by encoding the CBOR map structure
        var rawAttestationObject = EncodeAttestationObject(
            ctapResponse.Format,
            ctapResponse.AuthenticatorDataRaw,
            ctapResponse.AttestationStatement.RawData);

        // Decode the raw bytes to create the WebAuthnAttestationObject
        var attestationObject = WebAuthnAttestationObject.Decode(rawAttestationObject);

        return new RegistrationResponse
        {
            CredentialId = attestedCred.CredentialId,
            AttestationObject = attestationObject,
            RawAttestationObject = rawAttestationObject,
            AuthenticatorData = webAuthnAuthData,
            RawAuthenticatorData = ctapResponse.AuthenticatorDataRaw,
            AttestationStatement = webAuthnStatement,
            Transports = null, // Phase 6
            PublicKey = publicKey,
            Aaguid = new Aaguid(attestedCred.Aaguid),
            SignCount = ctapResponse.AuthenticatorData.SignCount,
            ClientData = clientData,
            ClientExtensionResults = null // Phase 6
        };
    }

    private static byte[] EncodeAttestationObject(
        string format,
        ReadOnlyMemory<byte> authData,
        ReadOnlyMemory<byte> attStmtRawCbor)
    {
        var writer = new System.Formats.Cbor.CborWriter(System.Formats.Cbor.CborConformanceMode.Ctap2Canonical);

        writer.WriteStartMap(3);

        // Keys are sorted lexicographically: "authData", "attStmt", "fmt"
        writer.WriteTextString("authData");
        writer.WriteByteString(authData.Span);

        writer.WriteTextString("attStmt");
        writer.WriteEncodedValue(attStmtRawCbor.Span);

        writer.WriteTextString("fmt");
        writer.WriteTextString(format);

        writer.WriteEndMap();

        return writer.Encode();
    }
}
