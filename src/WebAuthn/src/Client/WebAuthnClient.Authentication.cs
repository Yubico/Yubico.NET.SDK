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

using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;

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
    /// Zero, one, or multiple matched credentials. Use <see cref="MatchedCredential.SelectAsync"/>
    /// to retrieve a match's already-built assertion response for relying party verification.
    /// </returns>
    /// <remarks>
    /// <para>
    /// With a nonempty allow list, the client probes for a matching credential and narrows the final
    /// request to one descriptor; it does not return every allowed credential held by the authenticator.
    /// Without an allow list, the authenticator can return multiple discoverable credentials for the RP,
    /// subject to its own selection behavior. No match produces an empty list rather than an exception.
    /// </para>
    /// <para>
    /// Assertions and extension output processing, including extension writes, finish before this method
    /// returns. A caller can present a picker for multiple matches, but selecting a match performs no
    /// further device I/O. Cancelling selection does not undo an assertion or extension side effect.
    /// </para>
    /// <para>
    /// CTAP2.0 §5.2 permits omission of the final credential descriptor for a singleton allow list;
    /// CTAP2.1/2.3 §6.2.2 require the descriptor. The client currently exposes an empty credential ID
    /// when it is omitted instead of recovering the selected ID.
    /// Only an original single-entry allow list gives the application an unambiguous requested ID;
    /// never infer the selected ID from the first of multiple allowed credentials. The RP must still
    /// verify the response and its credential/account binding.
    /// </para>
    /// <para>
    /// Capability discovery, PIN/UV token acquisition, transport operations, and extension output
    /// processing can propagate lower-level exceptions; not every failure is normalized to
    /// <see cref="WebAuthnClientError"/>. The timeout signals cooperative cancellation as described by
    /// <see cref="PublicKeyCredentialRequestOptions.Timeout"/>; it is not a hard completion deadline.
    /// </para>
    /// </remarks>
    /// <exception cref="WebAuthnClientError">Request validation or a mapped ceremony failure occurred.</exception>
    /// <exception cref="OperationCanceledException">The operation observed caller cancellation.</exception>
    public async Task<IReadOnlyList<MatchedCredential>> GetAssertionAsync(
        PublicKeyCredentialRequestOptions options,
        ReadOnlyMemory<byte>? pinBytes = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        // Validate before the timer starts (WebAuthn L3 §5.1.4 orders the checks before lifetimeTimer).
        ValidateAuthenticationOptions(options);
        EnsureRpIdAuthorized(options.RpId);

        // The deadline covers everything up to the returned matches. MatchedCredential.SelectAsync does
        // no further authenticator work (its response is already computed), so nothing is left to bound.
        using var deadline = CeremonyDeadline.Start(options.Timeout, cancellationToken);
        try
        {
            return await GetAssertionCoreAsync(options, pinBytes, deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (deadline.Expired)
        {
            throw CeremonyTimedOut(options.Timeout, ex);
        }
    }

    /// <summary>
    /// Runs the GetAssertion ceremony: validation, UV/PIN decision, token acquisition,
    /// credential matching, and CTAP execution.
    /// </summary>
    private async Task<IReadOnlyList<MatchedCredential>> GetAssertionCoreAsync(
        PublicKeyCredentialRequestOptions options,
        ReadOnlyMemory<byte>? callerPinBytes,
        CancellationToken cancellationToken)
    {
        // Build client data
        var clientData = WebAuthnClientData.Create(
            type: "webauthn.get",
            challenge: options.Challenge,
            origin: _origin,
            crossOrigin: options.CrossOrigin,
            topOrigin: options.TopOrigin);

        // Get authenticator info
        var info = await _backend.GetCachedInfoAsync(cancellationToken).ConfigureAwait(false);

        // Extension processors are created per attempt; their permissions join the token request.
        var context = new ExtensionContext(info, _backend.Protocol, _backend);
        var permissions = PinUvAuthTokenPermissions.GetAssertion
            | AuthenticationExtensionSession.Create(options.Extensions, options.AllowCredentials, context).Permissions;
        bool hasPreviewSign = options.Extensions?.PreviewSign is not null;

        using var auth = new CeremonyAuth(this, options.RpId, callerPinBytes);

        var (matches, extensions) = await RunWithUvEscalationAsync(
            info, options.UserVerification, permissions, auth, hasPreviewSign, AttemptAsync, cancellationToken)
            .ConfigureAwait(false);

        // Build every response now, outside the escalation scope and while the token is live (extension
        // outputs may need it); MatchedCredential.SelectAsync then does no further authenticator work.
        using (extensions)
        {
            var results = new List<MatchedCredential>(matches.Count);
            foreach (var (credentialId, user, response) in matches)
            {
                var authenticationResponse = await BuildAuthenticationResponseAsync(
                    response, clientData, extensions, auth.Token, cancellationToken).ConfigureAwait(false);
                results.Add(new MatchedCredential(credentialId, user, _ => Task.FromResult(authenticationResponse)));
            }

            return results;
        }

        async Task<(IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)> Matches,
            AuthenticationExtensionSession Extensions)> AttemptAsync(UvDecision decision, CancellationToken ct)
        {
            var extensions = AuthenticationExtensionSession.Create(options.Extensions, options.AllowCredentials, context);
            try
            {
                return await AttemptWithAsync(extensions, decision, ct).ConfigureAwait(false);
            }
            catch
            {
                extensions.Dispose();
                throw;
            }
        }

        async Task<(IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)> Matches,
            AuthenticationExtensionSession Extensions)> AttemptWithAsync(
            AuthenticationExtensionSession extensions, UvDecision decision, CancellationToken ct)
        {
            await extensions.PrepareAsync(ct).ConfigureAwait(false);

            // Allow-list selection (WebAuthn L3 §5.1.4): find the credential with an up:false probe, then
            // assert with exactly that descriptor so per-credential extension inputs can name it. When
            // nothing matched, a dummy descriptor still requests user presence rather than returning
            // the silent probe's miss directly. This does not establish equal timing or prompts.
            PublicKeyCredentialDescriptor? selected = null;
            IReadOnlyList<PublicKeyCredentialDescriptor>? allowList = null;
            if (options.AllowCredentials is { Count: > 0 } candidates)
            {
                selected = await ProbeCredentialsAsync(options.RpId, candidates, info, auth, decision, remintPermissions: null, ct)
                    .ConfigureAwait(false);
                allowList = [selected ?? new PublicKeyCredentialDescriptor(DummyCredentialId, candidates[0].Type)];
            }

            var request = BuildGetAssertionRequest(options, clientData, auth, decision, extensions, selected, allowList);
            var matches = await CredentialMatcher.MatchAsync(_backend, request, ct).ConfigureAwait(false);
            return (matches, extensions);
        }
    }

    // On an allow-list probe miss, still request user presence in the final command instead of
    // exposing the silent result directly. Exact timing and prompts depend on the authenticator.
    private static readonly byte[] DummyCredentialId = [0x00];

    private static BackendGetAssertionRequest BuildGetAssertionRequest(
        PublicKeyCredentialRequestOptions options,
        WebAuthnClientData clientData,
        CeremonyAuth auth,
        UvDecision decision,
        AuthenticationExtensionSession extensions,
        PublicKeyCredentialDescriptor? selected,
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowList)
    {
        // Extension inputs first: an input that cannot be built must fail before any pinUvAuthParam exists.
        var extensionsCbor = extensions.PrepareInputs(selected, auth.Token);
        var (pinUvAuthParam, pinUvAuthProtocol) = auth.SignClientData(clientData.Hash.Span);

        return new BackendGetAssertionRequest
        {
            ClientDataHash = clientData.Hash,
            RpId = options.RpId,
            AllowList = allowList,
            Extensions = extensionsCbor,
            Options = CtapOptions(decision),
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = pinUvAuthProtocol
        };
    }

    private static async Task<AuthenticationResponse> BuildAuthenticationResponseAsync(
        GetAssertionResponse ctapResponse,
        WebAuthnClientData clientData,
        AuthenticationExtensionSession extensions,
        PinUvAuthTokenSession? tokenSession,
        CancellationToken cancellationToken)
    {
        // Wrap authenticator data
        var webAuthnAuthData = WebAuthnAuthenticatorData.Decode(ctapResponse.AuthenticatorDataRaw);

        // Known limitation: descriptor omission (permitted by CTAP2.0 §5.2) loses the selected ID here.
        var credentialId = ctapResponse.Credential?.Id ?? ReadOnlyMemory<byte>.Empty;

        // User from CTAP response can be used directly
        var user = ctapResponse.User;

        // Parse extension outputs via pipeline
        var extensionOutputs = await extensions.PrepareOutputsAsync(ctapResponse, webAuthnAuthData, tokenSession, cancellationToken)
            .ConfigureAwait(false);

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