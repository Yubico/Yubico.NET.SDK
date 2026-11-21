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
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <content>
/// The frame both ceremonies share: token and prompted-PIN ownership, the single UV escalation, the
/// up:false credential probe with its token re-mint, and the CTAP-to-WebAuthn error mapping.
/// </content>
public sealed partial class WebAuthnClient
{
    /// <summary>
    /// Owns the PIN/UV token and the prompted PIN for one ceremony. The token is replaced on escalation
    /// and on re-mint; the pinUvAuthParam it signs is zeroed when superseded or on dispose; the prompted
    /// PIN is zeroed on dispose.
    /// </summary>
    private sealed class CeremonyAuth(WebAuthnClient client, string rpId, ReadOnlyMemory<byte>? callerPin) : IDisposable
    {
        private IMemoryOwner<byte>? _promptedPin;
        private ReadOnlyMemory<byte>? _pin = callerPin;
        private byte[]? _lastAuthParam;

        public PinUvAuthTokenSession? Token { get; private set; }

        public bool PinAvailable => _pin is not null || client._options.CredentialPrompt is not null;

        /// <summary>Acquires the token the decision calls for (none when the decision needs no token).</summary>
        public async Task AcquireAsync(UvDecision decision, CancellationToken cancellationToken)
        {
            Token?.Dispose();
            Token = null;

            var (token, promptedPin, pin) = await client.AcquireTokenForDecisionAsync(
                decision, rpId, _promptedPin, _pin, cancellationToken).ConfigureAwait(false);
            Token = token;
            _promptedPin = promptedPin;
            _pin = pin;
        }

        /// <summary>
        /// Replaces the token with a fresh one. CTAP 2.1 §6.5.2 token state and §6.1.2/§6.2.2 command
        /// checks govern permission reuse; a token used for an up:false probe may be refused by the
        /// following makeCredential. Token acquisition itself is described in §6.5.5.7.
        /// </summary>
        public async Task RemintAsync(UvDecision decision, PinUvAuthTokenPermissions permissions, CancellationToken cancellationToken)
        {
            Token?.Dispose();
            Token = await client.AcquirePinUvTokenAsync(decision.Method!.Value, permissions, rpId, _pin, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Signs the client data hash with the current token (CTAP §6.1/§6.2 pinUvAuthParam), or returns
        /// nulls when the ceremony has no token. The returned buffer belongs to this object.
        /// </summary>
        public (ReadOnlyMemory<byte>? Param, byte? Protocol) SignClientData(ReadOnlySpan<byte> clientDataHash)
        {
            ClearAuthParam();
            if (Token is null)
            {
                return (null, null);
            }

            _lastAuthParam = Token.Protocol.Authenticate(Token.Token.Span, clientDataHash);
            return (_lastAuthParam, (byte)Token.Protocol.Version);
        }

        public void Dispose()
        {
            ClearAuthParam();
            Token?.Dispose();
            Token = null;
            ZeroAndDispose(_promptedPin);
            _promptedPin = null;
        }

        private void ClearAuthParam()
        {
            if (_lastAuthParam is not null)
            {
                CryptographicOperations.ZeroMemory(_lastAuthParam);
                _lastAuthParam = null;
            }
        }
    }

    /// <summary>
    /// Runs one ceremony attempt and, if the authenticator answers CTAP2_ERR_PUAT_REQUIRED to a
    /// request made without required verification, runs it once more with verification required.
    /// Only the initial credential command leaves an eligible PUAT_REQUIRED unmapped. Preparation,
    /// probes and continuations handle their CTAP statuses separately and cannot retry that command.
    /// </summary>
    /// <remarks>
    /// The attempt must end at the authenticator's answer. Follow-up work that may itself fail with a
    /// CTAP status (extension output preparation) stays outside, so a failure there can never re-run
    /// the command and, for registration, create a second credential.
    /// </remarks>
    private async Task<TAttempt> RunWithUvEscalationAsync<TAttempt>(
        AuthenticatorInfo info,
        UserVerificationPreference preference,
        PinUvAuthTokenPermissions permissions,
        CeremonyAuth auth,
        Func<UvDecision, UserVerificationPreference, CancellationToken, Task<TAttempt>> attempt,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var decision = UvDecisionLogic.Decide(info, preference, auth.PinAvailable, permissions);
            await AcquireCeremonyTokenAsync(auth, decision, cancellationToken).ConfigureAwait(false);

            try
            {
                return await attempt(decision, preference, cancellationToken).ConfigureAwait(false);
            }
            catch (CtapException ex) when (ShouldRetryWithRequiredUv(ex, preference))
            {
                preference = UserVerificationPreference.Required;
            }
        }
    }

    private async Task AcquireCeremonyTokenAsync(CeremonyAuth auth, UvDecision decision, CancellationToken ct)
    {
        try
        {
            if (decision.UseToken)
                _ = _backend.Protocol;
            await auth.AcquireAsync(decision, ct).ConfigureAwait(false);
        }
        catch (CtapException ex)
        {
            throw MapCtapStatusToWebAuthnError(ex);
        }
    }

    /// <summary>
    /// Probes <paramref name="candidates"/> with up:false (exclude-list pre-flight, allow-list selection).
    /// Probe CTAP failures other than "no match" map to <see cref="WebAuthnClientErrorCode.Unknown"/>.
    /// Unassociated successful responses fail with <see cref="WebAuthnClientErrorCode.InvalidState"/>.
    /// Neither failure can trigger verification escalation.
    /// </summary>
    /// <param name="rpId">The relying party identifier.</param>
    /// <param name="candidates">The descriptors to probe.</param>
    /// <param name="info">The authenticator info.</param>
    /// <param name="auth">The ceremony's token owner.</param>
    /// <param name="decision">The UV decision in force (its method is reused for a re-mint).</param>
    /// <param name="remintPermissions">
    /// When set and a token exists, the token is re-minted with these permissions after the probe.
    /// Registration passes <c>MakeCredential</c>; authentication selection passes null and reuses its
    /// existing token for the final assertion. This is the SDK's reuse choice, not a guarantee of
    /// authenticator acceptance; the authenticator's token state and expiry conditions still apply.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<PublicKeyCredentialDescriptor?> ProbeCredentialsAsync(
        string rpId,
        IReadOnlyList<PublicKeyCredentialDescriptor> candidates,
        AuthenticatorInfo info,
        CeremonyAuth auth,
        UvDecision decision,
        PinUvAuthTokenPermissions? remintPermissions,
        CancellationToken cancellationToken)
    {
        PublicKeyCredentialDescriptor? match;
        try
        {
            match = await CredentialFilter.SelectAsync(_backend, rpId, candidates, info, auth.Token, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CtapException ex)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.Unknown,
                $"Pre-flight excludeList probe failed (device returned {ex.Status}). " +
                "This authenticator may not support silent excludeList probing.",
                ex);
        }

        if (auth.Token is not null && remintPermissions is { } permissions)
        {
            try
            {
                await auth.RemintAsync(decision, permissions, cancellationToken).ConfigureAwait(false);
            }
            catch (CtapException ex)
            {
                throw MapCtapStatusToWebAuthnError(ex);
            }
        }

        return match;
    }

    private async Task<AuthenticatorInfo> GetCeremonyInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _backend.GetCachedInfoAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (CtapException ex)
        {
            throw MapCtapStatusToWebAuthnError(ex);
        }
    }

    private static BackendGetAssertionRequest BuildTokenlessUvAssertion(string rpId, WebAuthnClientData clientData,
        IReadOnlyList<PublicKeyCredentialDescriptor> chunk, ReadOnlyMemory<byte>? extensions = null) => new()
        {
            RpId = rpId,
            ClientDataHash = clientData.Hash,
            AllowList = chunk,
            Options = new Dictionary<string, bool> { ["uv"] = true, ["up"] = true },
            Extensions = extensions
        };

    private static async Task<(PublicKeyCredentialDescriptor Selected, GetAssertionResponse Response)?> FindTokenlessUvCredentialAsync(
        IReadOnlyList<PublicKeyCredentialDescriptor> eligible, AuthenticatorInfo info,
        Func<IReadOnlyList<PublicKeyCredentialDescriptor>, Task<(bool Fits, CtapStatus Status, GetAssertionResponse? Response)>> getAssertion, CancellationToken ct)
    {
        try
        {
            return await CredentialFilter.FindAsync(eligible, info, requireUvAndUp: true, getAssertion, ct).ConfigureAwait(false);
        }
        catch (CtapException ex)
        {
            // This branch already requested built-in UV: failure cannot cause UV escalation or PIN acquisition.
            throw MapCeremonyCtapException(ex, hasPreviewSign: false);
        }
    }

    /// <summary>CTAP "uv" option key from the decision, plus "rk" for registration; null when neither applies.</summary>
    private static IReadOnlyDictionary<string, bool>? CtapOptions(UvDecision decision, bool requireResidentKey = false)
    {
        var options = new Dictionary<string, bool>();
        if (requireResidentKey)
        {
            options["rk"] = true;
        }

        if (decision.UvOption is { } uv)
        {
            options["uv"] = uv;
        }

        return options.Count > 0 ? options : null;
    }

    /// <summary>
    /// Maps a caught CTAP failure of the ceremony attempt: CredentialExcluded
    /// is WebAuthn L3 §5.1.3's InvalidStateError; commands carrying previewSign use its mapper; the rest
    /// go through <see cref="MapCtapStatusToWebAuthnError"/>.
    /// </summary>
    private static WebAuthnClientError MapCeremonyCtapException(CtapException ex, bool hasPreviewSign)
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
}