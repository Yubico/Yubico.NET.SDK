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
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <content>
/// The frame both ceremonies share: token and prompted-PIN ownership, the single UV escalation, the
/// up:false credential probe with its token re-mint, and the CTAP-to-WebAuthn error mapping.
/// </content>
public sealed partial class WebAuthnClient
{
    private static readonly PinUvAuthTokenPermissions MakeCredentialCeremonyPermissions =
        PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion;

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
        /// Replaces the token with a fresh one. CTAP 2.1 §6.5.5.7 lets an authenticator consume
        /// permissions on use; YubiKey 5.8.0 refuses the next command after an up:false probe.
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
    /// Any other CTAP failure is mapped to a typed <see cref="WebAuthnClientError"/>.
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
        bool hasPreviewSign,
        Func<UvDecision, CancellationToken, Task<TAttempt>> attempt,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var decision = UvDecisionLogic.Decide(info, preference, auth.PinAvailable, permissions);
            await auth.AcquireAsync(decision, cancellationToken).ConfigureAwait(false);

            try
            {
                return await attempt(decision, cancellationToken).ConfigureAwait(false);
            }
            catch (CtapException ex) when (ShouldRetryWithRequiredUv(ex, preference))
            {
                preference = UserVerificationPreference.Required;
            }
            catch (CtapException ex)
            {
                throw MapCeremonyCtapException(ex, hasPreviewSign);
            }
        }
    }

    /// <summary>
    /// Probes <paramref name="candidates"/> with up:false (exclude-list pre-flight, allow-list selection)
    /// and re-mints the token afterwards. Probe failures other than "no match" are reported as
    /// <see cref="WebAuthnClientErrorCode.Unknown"/> and never escalate verification.
    /// </summary>
    private async Task<PublicKeyCredentialDescriptor?> ProbeCredentialsAsync(
        string rpId,
        IReadOnlyList<PublicKeyCredentialDescriptor> candidates,
        AuthenticatorInfo info,
        CeremonyAuth auth,
        UvDecision decision,
        PinUvAuthTokenPermissions remintPermissions,
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

        if (auth.Token is not null)
        {
            await auth.RemintAsync(decision, remintPermissions, cancellationToken).ConfigureAwait(false);
        }

        return match;
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
    /// Maps a CTAP failure of the main command to the typed error the module promises: CredentialExcluded
    /// is WebAuthn L3 §5.1.3's InvalidStateError; previewSign requests use the previewSign mapper; the rest
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