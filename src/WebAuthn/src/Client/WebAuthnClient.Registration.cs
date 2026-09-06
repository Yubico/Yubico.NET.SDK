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
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Client.Validation;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Util;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <content>The MakeCredential (registration) ceremony.</content>
public sealed partial class WebAuthnClient
{
    /// <summary>
    /// Creates a new WebAuthn credential via CTAP2 MakeCredential.
    /// </summary>
    /// <param name="options">The registration options.</param>
    /// <param name="pinBytes">
    /// Optional PIN bytes (UTF-8 encoded). The caller owns and zeroes this memory. When omitted
    /// and the operation needs a PIN, the client asks the configured
    /// <see cref="ICredentialPrompt"/>; if none is configured the operation fails with
    /// <see cref="WebAuthnClientErrorCode.NotAllowed"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration response with credential details.</returns>
    /// <exception cref="WebAuthnClientError">Thrown on validation or operation failure.</exception>
    public async Task<RegistrationResponse> MakeCredentialAsync(
        RegistrationOptions options,
        ReadOnlyMemory<byte>? pinBytes = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        return await MakeCredentialCoreAsync(options, pinBytes, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the MakeCredential ceremony: validation, UV/PIN decision, token acquisition,
    /// excludeList pre-flight, and CTAP execution.
    /// </summary>
    private async Task<RegistrationResponse> MakeCredentialCoreAsync(
        RegistrationOptions options,
        ReadOnlyMemory<byte>? callerPinBytes,
        CancellationToken cancellationToken)
    {
        // Validate options
        ValidateRegistrationOptions(options);

        // Validate RP ID against origin
        RpIdValidator.EnsureValid(options.Rp.Id, _origin, _options.EnterpriseRpIds, _isPublicSuffix);

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
            pinAvailable: callerPinBytes is not null || _options.CredentialPrompt is not null,
            requestedPermissions: PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion);

        // Acquire PIN/UV token with retry on PinAuthInvalid
        PinUvAuthTokenSession? tokenSession = null;
        IMemoryOwner<byte>? pinOwner = null;
        ReadOnlyMemory<byte>? pinBytes = callerPinBytes;

        try
        {
            (tokenSession, pinOwner, pinBytes) = await AcquireTokenForDecisionAsync(
                uvDecision,
                options.Rp.Id,
                pinOwner,
                pinBytes,
                cancellationToken).ConfigureAwait(false);

            // Pre-flight excludeList when non-empty (mirroring yubikit-android Ctap2Client.filterCreds)
            PublicKeyCredentialDescriptor? matchedExclude = null;
            bool preflightPerformed;
            (matchedExclude, tokenSession, preflightPerformed) = await PreflightExcludeListAndRemintAsync(
                options,
                info,
                uvDecision,
                tokenSession,
                pinBytes,
                cancellationToken).ConfigureAwait(false);

            // Build backend request with filtered exclude list
            var request = BuildMakeCredentialRequest(options, clientData, tokenSession, uvDecision, matchedExclude, preflightPerformed);

            // Execute MakeCredential
            MakeCredentialResponse ctapResponse;
            try
            {
                ctapResponse = await ExecuteMakeCredentialAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (CtapException ex) when (ShouldRetryWithRequiredUv(ex, options.UserVerification))
            {
                tokenSession?.Dispose();
                tokenSession = null;

                uvDecision = UvDecisionLogic.Decide(
                    info,
                    Preferences.UserVerificationPreference.Required,
                    pinAvailable: callerPinBytes is not null || _options.CredentialPrompt is not null,
                    requestedPermissions: PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion);

                (tokenSession, pinOwner, pinBytes) = await AcquireTokenForDecisionAsync(
                    uvDecision,
                    options.Rp.Id,
                    pinOwner,
                    pinBytes,
                    cancellationToken).ConfigureAwait(false);

                (matchedExclude, tokenSession, preflightPerformed) = await PreflightExcludeListAndRemintAsync(
                    options,
                    info,
                    uvDecision,
                    tokenSession,
                    pinBytes,
                    cancellationToken).ConfigureAwait(false);

                request = BuildMakeCredentialRequest(options, clientData, tokenSession, uvDecision, matchedExclude, preflightPerformed);
                try
                {
                    ctapResponse = await ExecuteMakeCredentialAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (CtapException retryEx)
                {
                    throw MapMakeCredentialCtapException(retryEx, options.Extensions?.PreviewSign is not null);
                }
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.CredentialExcluded)
            {
                // WebAuthn L2 §5.1.3 step 3: when the authenticator returns
                // CredentialExcluded, the client surfaces an InvalidStateError.
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "A credential matching the exclude list already exists on this authenticator.",
                    ex);
            }
            catch (CtapException ex) when (options.Extensions?.PreviewSign is not null)
            {
                throw Extensions.PreviewSign.PreviewSignErrors.MapCtapError(ex);
            }
            catch (CtapException ex)
            {
                // Map remaining CTAP statuses to typed WebAuthn errors per CLAUDE.md:
                // "never expose raw CTAP status codes to high-level API consumers".
                throw MapCtapStatusToWebAuthnError(ex);
            }

            // Build WebAuthn response
            return BuildRegistrationResponse(ctapResponse, clientData, options);
        }
        finally
        {
            tokenSession?.Dispose();

            if (pinOwner is not null)
            {
                ZeroAndDispose(pinOwner);
            }
        }
    }

    private static WebAuthnClientError MapMakeCredentialCtapException(
        CtapException ex,
        bool hasPreviewSign)
    {
        if (ex.Status == CtapStatus.CredentialExcluded)
        {
            return new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "A credential matching the exclude list already exists on this authenticator.",
                ex);
        }

        return hasPreviewSign
            ? Extensions.PreviewSign.PreviewSignErrors.MapCtapError(ex)
            : MapCtapStatusToWebAuthnError(ex);
    }

    private async Task<MakeCredentialResponse> ExecuteMakeCredentialAsync(
        BackendMakeCredentialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _backend.MakeCredentialAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            SensitiveMemory.Zero(request.PinUvAuthParam);
        }
    }

    private async Task<(
        PublicKeyCredentialDescriptor? MatchedExclude,
        PinUvAuthTokenSession? TokenSession,
        bool PreflightPerformed)> PreflightExcludeListAndRemintAsync(
        RegistrationOptions options,
        AuthenticatorInfo info,
        UvDecision uvDecision,
        PinUvAuthTokenSession? tokenSession,
        ReadOnlyMemory<byte>? pinBytes,
        CancellationToken cancellationToken)
    {
        if (options.ExcludeCredentials is null || options.ExcludeCredentials.Count == 0 || tokenSession is null)
        {
            return (null, tokenSession, false);
        }

        // ToArray() creates a copy; pre-flight needs to pass token across async boundary.
        var tokenCopy = tokenSession.Token.ToArray();
        try
        {
            var matchedExclude = await Internal.ExcludeListPreflight.FindFirstMatchAsync(
                _backend,
                options.Rp.Id,
                options.ExcludeCredentials,
                info,
                tokenCopy,
                tokenSession.Protocol,
                cancellationToken).ConfigureAwait(false);

            // Re-mint the pinUvAuthToken between pre-flight and MakeCredential.
            // CTAP 2.1 §6.5.5.7: authenticators MAY consume permissions on use. On
            // YubiKey 5.8.0, the pre-flight's GetAssertion(up=false) consumes the
            // GetAssertion permission and the same token can no longer authorize a
            // subsequent MakeCredential; the device returns PinAuthInvalid.
            tokenSession.Dispose();
            tokenSession = await AcquirePinUvTokenAsync(
                uvDecision.Method!.Value,
                PinUvAuthTokenPermissions.MakeCredential,
                options.Rp.Id,
                pinBytes,
                cancellationToken).ConfigureAwait(false);

            return (matchedExclude, tokenSession, true);
        }
        catch (CtapException preflightEx)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.Unknown,
                $"Pre-flight excludeList probe failed (device returned {preflightEx.Status}). " +
                "This authenticator may not support silent excludeList probing.",
                preflightEx);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenCopy);
        }
    }

    private BackendMakeCredentialRequest BuildMakeCredentialRequest(
        RegistrationOptions options,
        WebAuthnClientData clientData,
        PinUvAuthTokenSession? tokenSession,
        UvDecision uvDecision,
        PublicKeyCredentialDescriptor? matchedExclude,
        bool preflightPerformed)
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

        // Build extensions CBOR via pipeline
        var extensionsCbor = ExtensionPipeline.BuildRegistrationExtensionsCbor(options.Extensions, options);

        // Use filtered exclude list only after pre-flight actually ran. Without pre-flight, preserve
        // the caller's original exclude list so authenticators still enforce it.
        IReadOnlyList<PublicKeyCredentialDescriptor>? excludeList = preflightPerformed
            ? matchedExclude is not null ? new[] { matchedExclude } : Array.Empty<PublicKeyCredentialDescriptor>()
            : options.ExcludeCredentials;

        return new BackendMakeCredentialRequest
        {
            ClientDataHash = clientData.Hash,
            Rp = options.Rp,
            User = options.User,
            PubKeyCredParams = options.PubKeyCredParams
                .Select(alg => new PublicKeyCredentialParameters { Algorithm = (CoseAlgorithmIdentifier)alg.Value })
                .ToList(),
            ExcludeList = excludeList,
            Extensions = extensionsCbor,
            Options = optionsDict.Count > 0 ? optionsDict : null,
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = pinUvAuthProtocol
        };
    }

    private RegistrationResponse BuildRegistrationResponse(
        MakeCredentialResponse ctapResponse,
        WebAuthnClientData clientData,
        RegistrationOptions options)
    {
        // Extract attested credential data from AuthenticatorData
        var attestedCred = ctapResponse.AuthenticatorData.AttestedCredentialData!;

        // Decode public key from COSE
        var publicKey = CoseKey.Decode(attestedCred.CredentialPublicKey);

        // Use the typed attestation statement from CTAP response (already decoded)
        var webAuthnStatement = ctapResponse.AttestationStatement;

        // Wrap authenticator data
        var webAuthnAuthData = WebAuthnAuthenticatorData.Decode(ctapResponse.AuthenticatorDataRaw);

        // Create attestation object from decoded components
        var attestationObject = WebAuthnAttestationObject.Create(webAuthnAuthData, webAuthnStatement);

        // Parse extension outputs via pipeline
        var extensionOutputs = ExtensionPipeline.ParseRegistrationOutputs(
            options.Extensions,
            webAuthnAuthData,
            ctapResponse.UnsignedExtensionOutputs,
            options);

        return new RegistrationResponse
        {
            CredentialId = attestedCred.CredentialId,
            AttestationObject = attestationObject,
            RawAttestationObject = attestationObject.RawCbor,
            AuthenticatorData = webAuthnAuthData,
            RawAuthenticatorData = ctapResponse.AuthenticatorDataRaw,
            AttestationStatement = webAuthnStatement,
            Transports = null, // TODO Phase 6+
            PublicKey = publicKey,
            Aaguid = new Aaguid(attestedCred.Aaguid),
            SignCount = ctapResponse.AuthenticatorData.SignCount,
            ClientData = clientData,
            ClientExtensionResults = extensionOutputs
        };
    }
}