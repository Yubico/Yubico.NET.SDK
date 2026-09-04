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
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Client.Validation;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Util;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <content>The GetAssertion (authentication) ceremony.</content>
public sealed partial class WebAuthnClient
{
    /// <summary>
    /// Authenticates using an existing credential (GetAssertion).
    /// </summary>
    /// <param name="options">The authentication options.</param>
    /// <param name="pinBytes">Optional PIN bytes (UTF-8 encoded). Caller owns and zeroes this memory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of matched credentials. Each credential exposes <see cref="MatchedCredential.SelectAsync"/>
    /// to complete the authentication and retrieve the assertion response.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method follows the deferred-selection pattern: the authenticator enumerates
    /// all matching credentials, and the caller can present a credential picker UI before
    /// calling <see cref="MatchedCredential.SelectAsync"/> to retrieve the assertion.
    /// </para>
    /// <para>
    /// If the allow list is empty, discoverable credentials for the RP ID are returned.
    /// If no credentials match, an empty list is returned (not an exception).
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<MatchedCredential>> GetAssertionAsync(
        AuthenticationOptions options,
        ReadOnlyMemory<byte>? pinBytes = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        return await GetAssertionCoreAsync(options, pinBytes, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the GetAssertion ceremony: validation, UV/PIN decision, token acquisition,
    /// credential matching, and CTAP execution.
    /// </summary>
    private async Task<IReadOnlyList<MatchedCredential>> GetAssertionCoreAsync(
        AuthenticationOptions options,
        ReadOnlyMemory<byte>? callerPinBytes,
        CancellationToken cancellationToken)
    {
        // Validate options
        ValidateAuthenticationOptions(options);

        // Validate RP ID against origin
        RpIdValidator.EnsureValid(options.RpId, _origin, _options.EnterpriseRpIds, _isPublicSuffix);

        // Build client data
        var clientData = WebAuthnClientData.Create(
            type: "webauthn.get",
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
            requestedPermissions: PinUvAuthTokenPermissions.GetAssertion);

        // Acquire PIN/UV token with retry on PinAuthInvalid
        PinUvAuthTokenSession? tokenSession = null;
        IMemoryOwner<byte>? pinOwner = null;
        ReadOnlyMemory<byte>? pinBytes = callerPinBytes;

        try
        {
            (tokenSession, pinOwner, pinBytes) = await AcquireTokenForDecisionAsync(
                uvDecision,
                options.RpId,
                pinOwner,
                pinBytes,
                cancellationToken).ConfigureAwait(false);

            // Build backend request
            var request = BuildGetAssertionRequest(options, clientData, tokenSession, uvDecision);

            // Match credentials (handles allow-list probing and discoverable enumeration)
            IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)> matches;
            try
            {
                matches = await MatchCredentialsAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (CtapException ex) when (ShouldRetryWithRequiredUv(ex, options.UserVerification))
            {
                tokenSession?.Dispose();
                tokenSession = null;

                uvDecision = UvDecisionLogic.Decide(
                    info,
                    Preferences.UserVerificationPreference.Required,
                    pinAvailable: callerPinBytes is not null || _options.CredentialPrompt is not null,
                    requestedPermissions: PinUvAuthTokenPermissions.GetAssertion);

                (tokenSession, pinOwner, pinBytes) = await AcquireTokenForDecisionAsync(
                    uvDecision,
                    options.RpId,
                    pinOwner,
                    pinBytes,
                    cancellationToken).ConfigureAwait(false);

                request = BuildGetAssertionRequest(options, clientData, tokenSession, uvDecision);
                try
                {
                    matches = await MatchCredentialsAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (CtapException retryEx)
                {
                    throw MapGetAssertionCtapException(retryEx, options.Extensions?.PreviewSign is not null);
                }
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

            // Wrap each match into a MatchedCredential with deferred SelectAsync
            var results = new List<MatchedCredential>();
            bool requiresSelection = matches.Count > 1;

            foreach (var (credId, user, response) in matches)
            {
                var matchedCred = new MatchedCredential(
                    id: credId,
                    user: user,
                    requiresSelection: requiresSelection,
                    responseFactory: _ => Task.FromResult(BuildAuthenticationResponse(response, clientData, options)));

                results.Add(matchedCred);
            }

            return results;
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

    private static WebAuthnClientError MapGetAssertionCtapException(
        CtapException ex,
        bool hasPreviewSign) =>
        hasPreviewSign
            ? Extensions.PreviewSign.PreviewSignErrors.MapCtapError(ex)
            : MapCtapStatusToWebAuthnError(ex);

    private async Task<IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)>> MatchCredentialsAsync(
        BackendGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CredentialMatcher.MatchAsync(_backend, request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            SensitiveMemory.Zero(request.PinUvAuthParam);
        }
    }

    private BackendGetAssertionRequest BuildGetAssertionRequest(
        AuthenticationOptions options,
        WebAuthnClientData clientData,
        PinUvAuthTokenSession? tokenSession,
        UvDecision uvDecision)
    {
        // Map options to backend request
        var optionsDict = new Dictionary<string, bool>();

        if (uvDecision.UvOption.HasValue)
        {
            optionsDict["uv"] = uvDecision.UvOption.Value;
        }

        // Map allow credentials to backend descriptors
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowList = null;
        if (options.AllowCredentials is not null && options.AllowCredentials.Count > 0)
        {
            allowList = options.AllowCredentials
                .Select(desc => new PublicKeyCredentialDescriptor(
                    desc.Id,
                    desc.Type,
                    desc.Transports))
                .ToList();
        }

        // Build PIN/UV auth params
        ReadOnlyMemory<byte>? pinUvAuthParam = null;
        byte? pinUvAuthProtocol = null;

        if (tokenSession is not null)
        {
            // Compute pinUvAuthParam = HMAC(token, clientDataHash)
            pinUvAuthParam = tokenSession.Protocol.Authenticate(tokenSession.Token, clientData.Hash.Span);
            pinUvAuthProtocol = (byte)tokenSession.Protocol.Version;
        }

        // Build extensions CBOR via pipeline
        var extensionsCbor = ExtensionPipeline.BuildAuthenticationExtensionsCbor(
            options.Extensions,
            options.AllowCredentials);

        return new BackendGetAssertionRequest
        {
            ClientDataHash = clientData.Hash,
            RpId = options.RpId,
            AllowList = allowList,
            Extensions = extensionsCbor,
            Options = optionsDict.Count > 0 ? optionsDict : null,
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = pinUvAuthProtocol
        };
    }

    private static AuthenticationResponse BuildAuthenticationResponse(
        GetAssertionResponse ctapResponse,
        WebAuthnClientData clientData,
        AuthenticationOptions options)
    {
        // Wrap authenticator data
        var webAuthnAuthData = WebAuthnAuthenticatorData.Decode(ctapResponse.AuthenticatorDataRaw);

        // Extract credential ID from the response or use empty if not present
        var credentialId = ctapResponse.Credential?.Id ?? ReadOnlyMemory<byte>.Empty;

        // User from CTAP response can be used directly
        var user = ctapResponse.User;

        // Parse extension outputs via pipeline
        var extensionOutputs = ExtensionPipeline.ParseAuthenticationOutputs(
            options.Extensions,
            webAuthnAuthData);

        return new AuthenticationResponse
        {
            CredentialId = credentialId,
            AuthenticatorData = webAuthnAuthData,
            RawAuthenticatorData = ctapResponse.AuthenticatorDataRaw,
            Signature = ctapResponse.Signature,
            User = user,
            SignCount = ctapResponse.AuthenticatorData.SignCount,
            ClientData = clientData,
            ClientExtensionResults = extensionOutputs
        };
    }
}