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

using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;
using Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;
using Yubico.YubiKit.WebAuthn.Preferences;

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
    /// With a nonempty allow list, the client probes for a matching credential and keeps the first
    /// successful selection; it does not enumerate every allowed credential. If built-in verification
    /// is already in effect without a token, a silent miss is checked with real, verified assertions
    /// against the eligible list. A reported count greater than one for a final allow-list request is
    /// rejected before any continuation or extension processing.
    /// If that probe misses and PRF applies to every eligible entry, a configured, caller-supplied PIN
    /// can be used for one additional silent selection pass to find a verification-protected credential.
    /// Without an allow list, the authenticator can return multiple discoverable credentials for the RP,
    /// subject to its own selection behavior. No match produces an empty list rather than an exception.
    /// A reported assertion count above the SDK's 1024-per-ceremony limit raises a Constraint error
    /// before requesting continuations. This host resource limit is not a CTAP validity rule.
    /// </para>
    /// <para>
    /// When verified selection cannot supply a selected-credential extension input in the first
    /// assertion, the client can request one more assertion for that same credential. This can require
    /// another user gesture and advance the signature counter again; gestures are not cached by the SDK.
    /// </para>
    /// <para>
    /// Assertions and extension output processing, including extension writes, finish before this method
    /// returns. A caller can present a picker for multiple matches, but selecting a match performs no
    /// further device I/O. Cancelling selection does not undo an assertion or extension side effect.
    /// </para>
    /// <para>
    /// CTAP2.0 §5.2 permits omission of the final credential descriptor for a singleton allow list;
    /// CTAP2.1/2.3 §6.2.2 require the descriptor. For compatibility, the client recovers an omitted ID
    /// only from the single real descriptor sent in that request, never from a multi-entry list.
    /// Unassociated or inconsistent responses fail with InvalidState. The RP must still
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
            throw CeremonyTimedOut(deadline.EffectiveTimeout, ex);
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
        var info = await GetCeremonyInfoAsync(cancellationToken).ConfigureAwait(false);

        // Extension processors are created per attempt; their permissions join the token request.
        var context = new ExtensionContext(info, null, _backend);
        var permissions = PinUvAuthTokenPermissions.GetAssertion
            | AuthenticationExtensionSession.Create(options.Extensions, options.AllowCredentials, context).Permissions;
        using var auth = new CeremonyAuth(this, options.RpId, callerPinBytes);
        PublicKeyCredentialDescriptor? selected = null;

        var (matches, extensions) = await RunWithUvEscalationAsync(
            info, options.UserVerification, permissions, auth, AttemptAsync, cancellationToken)
            .ConfigureAwait(false);

        // Build every response now, outside the escalation scope and while the token is live (extension
        // outputs may need it); MatchedCredential.SelectAsync then does no further authenticator work.
        using (extensions)
            return await BuildMatchedResponsesAsync(matches, clientData, extensions, auth.Token, cancellationToken).ConfigureAwait(false);

        async Task<(IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)> Matches,
            AuthenticationExtensionSession Extensions)> AttemptAsync(UvDecision decision, UserVerificationPreference preference, CancellationToken ct)
        {
            var extensions = AuthenticationExtensionSession.Create(options.Extensions, options.AllowCredentials, context);
            try
            {
                return await AttemptWithAsync(extensions, decision, preference, ct).ConfigureAwait(false);
            }
            catch
            {
                extensions.Dispose();
                throw;
            }
        }

        async Task<(IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)> Matches,
            AuthenticationExtensionSession Extensions)> AttemptWithAsync(
            AuthenticationExtensionSession extensions, UvDecision decision, UserVerificationPreference preference, CancellationToken ct)
        {
            // Allow-list selection (WebAuthn L3 §5.1.4): find the credential with an up:false probe, then
            // assert with exactly that descriptor so per-credential extension inputs can name it. When
            // nothing matched, a dummy descriptor still requests user presence rather than returning
            // the silent probe's miss directly. This does not establish equal timing or prompts.
            IReadOnlyList<PublicKeyCredentialDescriptor>? allowList = null;
            if (options.AllowCredentials is { Count: > 0 } candidates)
            {
                selected ??= await ProbeCredentialsAsync(options.RpId, candidates, info, auth, decision, remintPermissions: null, ct)
                    .ConfigureAwait(false);
                if (selected is null && decision.UvOption is true && auth.Token is null)
                {
                    var verified = await GetTokenlessUvAssertionAsync(options.RpId, clientData, candidates, info, extensions, ct)
                        .ConfigureAwait(false);
                    return (verified, extensions);
                }
                if (selected is null && CanReprobeForPrf(options, context, candidates, auth, callerPinBytes))
                {
                    // A UV-protected credential may be invisible to the first silent probe. Only
                    // retry with the supplied PIN if every eligible candidate would evaluate PRF.
                    ct.ThrowIfCancellationRequested();
                    preference = UserVerificationPreference.Required;
                    decision = UvDecisionLogic.Decide(info, preference, auth.PinAvailable, permissions);
                    await AcquireCeremonyTokenAsync(auth, decision, ct).ConfigureAwait(false);
                    selected = await ProbeCredentialsAsync(options.RpId, candidates, info, auth, decision, remintPermissions: null, ct)
                        .ConfigureAwait(false);
                }
                allowList = [selected ?? new PublicKeyCredentialDescriptor(DummyCredentialId, candidates[0].Type)];
            }

            if ((allowList is null || selected is not null)
                && PrfProcessor.RequiresVerifiedAssertionEvaluation(options.Extensions?.Prf, context, selected))
            {
                preference = UserVerificationPreference.Required;
                if (!decision.UseToken && !decision.UseUv)
                {
                    decision = UvDecisionLogic.Decide(info, preference, auth.PinAvailable, permissions);
                    await AcquireCeremonyTokenAsync(auth, decision, ct).ConfigureAwait(false);
                }
            }

            if (allowList is null || selected is not null)
                await extensions.PrepareAsync(ct, selected).ConfigureAwait(false);
            var request = BuildGetAssertionRequest(options, clientData, auth, decision, extensions, selected, allowList, out bool hasPreviewSign);
            var first = await GetFirstAssertionAsync(request, preference, hasPreviewSign, ct).ConfigureAwait(false);
            var matches = first is null ? [] : await CredentialMatcher.MatchAsync(_backend, first, allowList is not null, selected, ct)
                .ConfigureAwait(false);
            return (matches, extensions);
        }
    }

    private async Task<IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)>>
        GetTokenlessUvAssertionAsync(string rpId, WebAuthnClientData clientData,
            IReadOnlyList<PublicKeyCredentialDescriptor> candidates, AuthenticatorInfo info,
            AuthenticationExtensionSession extensions, CancellationToken ct)
    {
        bool deferPrf = false;
        bool deferPreviewSign = false;
        var match = await FindTokenlessUvCredentialAsync(CredentialFilter.Eligible(candidates, info), info, SendChunkAsync, ct)
            .ConfigureAwait(false);
        if (match is not { } found)
            return [];

        var (selected, response) = found;
        bool needsSecond;
        try
        {
            needsSecond = deferPreviewSign || (deferPrf && extensions.HasPrfInput(selected));
        }
        catch
        {
            CredentialMatcher.ClearUnreturnedAssertion(response);
            throw;
        }

        if (needsSecond)
        {
            // Never publish the provisional assertion, even if the second command fails or is cancelled.
            CredentialMatcher.ClearUnreturnedAssertion(response);
            IReadOnlyList<PublicKeyCredentialDescriptor> singleton = [selected];
            response = await SendTokenlessUvAssertionAsync(
                rpId, clientData, singleton, extensions, deferPrf: false, deferPreviewSign: false, allowChunkStatus: false, ct)
                .ConfigureAwait(false);
            _ = CredentialFilter.Associate(response, singleton, requireUvAndUp: true, ct);
        }

        return await CredentialMatcher.MatchAsync(_backend, response, hasAllowList: true, selected, ct).ConfigureAwait(false);

        Task<GetAssertionResponse> SendChunkAsync(IReadOnlyList<PublicKeyCredentialDescriptor> chunk)
        {
            // The first descriptor is a representative only for PRF inputs common to the entire wire
            // chunk. It is not a selection, and previewSign must wait for the actual credential ID.
            deferPrf = extensions.PrfInputsDiffer(chunk);
            deferPreviewSign = chunk.Count > 1 && extensions.HasPreviewSign;
            return SendTokenlessUvAssertionAsync(rpId, clientData, chunk, extensions,
                deferPrf, deferPreviewSign, allowChunkStatus: true, ct);
        }
    }

    private async Task<GetAssertionResponse> SendTokenlessUvAssertionAsync(string rpId, WebAuthnClientData clientData,
        IReadOnlyList<PublicKeyCredentialDescriptor> chunk, AuthenticationExtensionSession extensions,
        bool deferPrf, bool deferPreviewSign, bool allowChunkStatus, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await extensions.PrepareAsync(ct, chunk[0], deferPrf).ConfigureAwait(false);
        var encoded = extensions.PrepareInputs(chunk[0], token: null, out bool hasPreviewSign, deferPrf, deferPreviewSign);
        try
        {
            ct.ThrowIfCancellationRequested();
            return await _backend.GetAssertionAsync(BuildTokenlessUvAssertion(rpId, clientData, chunk, encoded), ct)
                .ConfigureAwait(false);
        }
        catch (CtapException ex) when (!allowChunkStatus || ex.Status is not (CtapStatus.NoCredentials or CtapStatus.RequestTooLarge))
        {
            throw MapCeremonyCtapException(ex, hasPreviewSign);
        }
        finally
        {
            Util.SensitiveMemory.Zero(encoded);
        }
    }

    private static bool CanReprobeForPrf(
        PublicKeyCredentialRequestOptions options, ExtensionContext context,
        IReadOnlyList<PublicKeyCredentialDescriptor> candidates, CeremonyAuth auth, ReadOnlyMemory<byte>? callerPinBytes)
    {
        if (auth.Token is not null || callerPinBytes is null || options.Extensions?.Prf is not { } prf
            || !context.Info.Options.TryGetValue("clientPin", out bool configured) || !configured)
            return false;

        // Use the same ID eligibility rule as CredentialFilter. A mixed eligible list cannot prove
        // the hidden credential has an applicable PRF input, so it retains ordinary ceremony policy.
        bool eligible = false;
        foreach (var candidate in candidates)
        {
            if (context.Info.MaxCredentialIdLength is { } max && candidate.Id.Length > max)
                continue;
            if (!PrfProcessor.RequiresVerifiedAssertionEvaluation(prf, context, candidate))
                return false;
            eligible = true;
        }
        return eligible;
    }

    private static async Task<IReadOnlyList<MatchedCredential>> BuildMatchedResponsesAsync(
        IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)> matches,
        WebAuthnClientData clientData, AuthenticationExtensionSession extensions, PinUvAuthTokenSession? token, CancellationToken ct)
    {
        var results = new List<MatchedCredential>(matches.Count);
        var prepared = new List<AuthenticationResponse>(matches.Count);
        bool published = false;
        try
        {
            foreach (var (credentialId, user, response) in matches)
            {
                var authenticationResponse = await BuildAuthenticationResponseAsync(
                    response, credentialId, clientData, extensions, token, ct).ConfigureAwait(false);
                prepared.Add(authenticationResponse);
                results.Add(new MatchedCredential(credentialId, user, _ => Task.FromResult(authenticationResponse)));
            }
            published = true;
            return results;
        }
        finally
        {
            ClearAssertionBuffers(matches, prepared, published);
        }
    }

    private async Task<GetAssertionResponse?> GetFirstAssertionAsync(
        BackendGetAssertionRequest request, UserVerificationPreference preference, bool hasPreviewSign, CancellationToken ct)
    {
        try
        {
            return await _backend.GetAssertionAsync(request, ct).ConfigureAwait(false);
        }
        catch (CtapException ex) when (CredentialMatcher.IsNoCredentialsError(ex.Status))
        {
            return null;
        }
        catch (CtapException ex) when (!ShouldRetryWithRequiredUv(ex, preference))
        {
            throw MapCeremonyCtapException(ex, hasPreviewSign);
        }
        finally
        {
            Util.SensitiveMemory.Zero(request.Extensions);
        }
    }

    private static void ClearAssertionBuffers(
        IReadOnlyList<(ReadOnlyMemory<byte> Id, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)> matches,
        IReadOnlyList<AuthenticationResponse> prepared, bool published)
    {
        foreach (var (id, _, response) in matches)
        {
            if (!published)
            {
                CredentialMatcher.ClearUnreturnedAssertion(response);
                if (response.Credential is null)
                    Util.SensitiveMemory.Zero(id); // The matcher copied the wire singleton's ID for the response.
            }
            else if (response.LargeBlobKey is { } key)
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(key.Span);
        }
        if (published)
            return;
        foreach (var response in prepared)
        {
            Util.SensitiveMemory.Zero(response.ClientExtensionResults?.CredBlob?.Blob);
            Util.SensitiveMemory.ZeroPrf(response.ClientExtensionResults?.Prf?.Results);
            if (response.ClientExtensionResults?.LargeBlob?.Blob is { } blob)
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(blob.Span);
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
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowList,
        out bool hasPreviewSign)
    {
        // Extension inputs first: an input that cannot be built must fail before any pinUvAuthParam exists.
        var extensionsCbor = extensions.PrepareInputs(selected, auth.Token, out hasPreviewSign);
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
        ReadOnlyMemory<byte> credentialId,
        WebAuthnClientData clientData,
        AuthenticationExtensionSession extensions,
        PinUvAuthTokenSession? tokenSession,
        CancellationToken cancellationToken)
    {
        // Wrap authenticator data
        var webAuthnAuthData = WebAuthnAuthenticatorData.FromOwned(ctapResponse.AuthenticatorData);

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