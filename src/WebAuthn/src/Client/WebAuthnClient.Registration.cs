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

using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Client.Validation;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;

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
        PublicKeyCredentialCreationOptions options,
        ReadOnlyMemory<byte>? pinBytes = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(options);

        // Validate before the timer starts (WebAuthn L3 §5.1.3 orders the checks before lifetimeTimer).
        ValidateRegistrationOptions(options);
        RpIdValidator.EnsureValid(options.Rp.Id, _origin, _options.EnterpriseRpIds, _isPublicSuffix);

        using var deadline = CeremonyDeadline.Start(options.Timeout, cancellationToken);
        try
        {
            return await MakeCredentialCoreAsync(options, pinBytes, deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (deadline.Expired)
        {
            throw CeremonyTimedOut(options.Timeout, ex);
        }
    }

    /// <summary>
    /// Runs the MakeCredential ceremony: validation, UV/PIN decision, token acquisition,
    /// excludeList pre-flight, and CTAP execution.
    /// </summary>
    private async Task<RegistrationResponse> MakeCredentialCoreAsync(
        PublicKeyCredentialCreationOptions options,
        ReadOnlyMemory<byte>? callerPinBytes,
        CancellationToken cancellationToken)
    {
        // Build client data
        var clientData = WebAuthnClientData.Create(
            type: "webauthn.create",
            challenge: options.Challenge,
            origin: _origin,
            crossOrigin: options.CrossOrigin,
            topOrigin: options.TopOrigin);

        // Get authenticator info
        var info = await _backend.GetCachedInfoAsync(cancellationToken).ConfigureAwait(false);

        // Resolve the resident-key preference against the authenticator (WebAuthn L3 §5.4.6):
        // required → rk, preferred → rk only if the authenticator can, discouraged → omit.
        // An authenticator that cannot store discoverable credentials is skipped for "required" (L3 §5.1.3),
        // never downgraded to a non-discoverable credential.
        bool canResidentKey = info.Options.TryGetValue("rk", out var rkSupported) && rkSupported;
        bool requireResidentKey = options.ResidentKey switch
        {
            Preferences.ResidentKeyPreference.Required when !canResidentKey => throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "Resident key required but the authenticator does not support discoverable credentials."),
            Preferences.ResidentKeyPreference.Required => true,
            Preferences.ResidentKeyPreference.Preferred => canResidentKey,
            _ => false
        };

        // Extension processors are created per attempt; their permissions join the token request.
        var context = new ExtensionContext(info, _backend.Protocol, _backend);
        var permissions = MakeCredentialCeremonyPermissions
            | RegistrationExtensionSession.Create(options.Extensions, options, requireResidentKey, context).Permissions;
        bool hasPreviewSign = options.Extensions?.PreviewSign is not null;

        using var auth = new CeremonyAuth(this, options.Rp.Id, callerPinBytes);
        IReadOnlyList<PublicKeyCredentialDescriptor>? probedExcludeList = null;

        var (ctapResponse, extensions) = await RunWithUvEscalationAsync(
            info, options.UserVerification, permissions, auth, hasPreviewSign, AttemptAsync, cancellationToken)
            .ConfigureAwait(false);

        // Outputs are prepared outside the escalation scope, while the token is still live.
        using (extensions)
        {
            return await BuildRegistrationResponseAsync(ctapResponse, clientData, options, extensions, auth.Token, cancellationToken)
                .ConfigureAwait(false);
        }

        async Task<(MakeCredentialResponse Response, RegistrationExtensionSession Extensions)> AttemptAsync(
            UvDecision decision, CancellationToken ct)
        {
            var extensions = RegistrationExtensionSession.Create(options.Extensions, options, requireResidentKey, context);
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

        async Task<(MakeCredentialResponse Response, RegistrationExtensionSession Extensions)> AttemptWithAsync(
            RegistrationExtensionSession extensions, UvDecision decision, CancellationToken ct)
        {
            await extensions.PrepareAsync(ct).ConfigureAwait(false);

            // Exclude-list pre-flight: with a token, probe the
            // list and send only the match so the authenticator never sees more than it can process.
            // Without a token the caller's list goes through unchanged and the authenticator enforces it.
            // The probe's answer does not change across a UV escalation, so it runs once per ceremony.
            var excludeList = options.ExcludeCredentials;
            if (probedExcludeList is not null)
            {
                excludeList = probedExcludeList;
            }
            else if (excludeList is { Count: > 0 } && auth.Token is not null)
            {
                var matched = await ProbeCredentialsAsync(
                    options.Rp.Id, excludeList, info, auth, decision, PinUvAuthTokenPermissions.MakeCredential, ct)
                    .ConfigureAwait(false);
                excludeList = probedExcludeList = matched is null ? [] : [matched];
            }

            var request = BuildMakeCredentialRequest(options, clientData, auth, decision, excludeList, requireResidentKey, extensions);
            var response = await _backend.MakeCredentialAsync(request, ct).ConfigureAwait(false);
            return (response, extensions);
        }
    }

    private static BackendMakeCredentialRequest BuildMakeCredentialRequest(
        PublicKeyCredentialCreationOptions options,
        WebAuthnClientData clientData,
        CeremonyAuth auth,
        UvDecision decision,
        IReadOnlyList<PublicKeyCredentialDescriptor>? excludeList,
        bool requireResidentKey,
        RegistrationExtensionSession extensions)
    {
        var pubKeyCredParams = options.PubKeyCredParams
            .Select(alg => new PublicKeyCredentialParameters { Algorithm = (CoseAlgorithmIdentifier)alg.Value })
            .ToList();

        // Extension inputs first: an input that cannot be built must fail before any pinUvAuthParam exists.
        var extensionsCbor = extensions.PrepareInputs(auth.Token);
        var (pinUvAuthParam, pinUvAuthProtocol) = auth.SignClientData(clientData.Hash.Span);

        return new BackendMakeCredentialRequest
        {
            ClientDataHash = clientData.Hash,
            Rp = options.Rp,
            User = options.User,
            PubKeyCredParams = pubKeyCredParams,
            ExcludeList = excludeList,
            Extensions = extensionsCbor,
            Options = CtapOptions(decision, requireResidentKey),
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = pinUvAuthProtocol
        };
    }

    private static async Task<RegistrationResponse> BuildRegistrationResponseAsync(
        MakeCredentialResponse ctapResponse,
        WebAuthnClientData clientData,
        PublicKeyCredentialCreationOptions options,
        RegistrationExtensionSession extensions,
        PinUvAuthTokenSession? tokenSession,
        CancellationToken cancellationToken)
    {
        // Extract attested credential data from AuthenticatorData
        var attestedCred = ctapResponse.AuthenticatorData.AttestedCredentialData!;

        // Decode public key from COSE
        var publicKey = CoseKey.Decode(attestedCred.CredentialPublicKey);

        // Apply the attestation conveyance preference (WebAuthn L3 §5.4.7, §5.1.3). For "none" the
        // client replaces any statement that is not a self attestation with fmt "none" and an empty
        // attStmt, so the RP never sees the attestation certificate chain it declined. The AAGUID is
        // kept as the authenticator returned it, as L3 specifies. Indirect, Direct and
        // Enterprise pass the authenticator's statement through.
        var webAuthnStatement = ctapResponse.AttestationStatement;
        if (options.Attestation == Preferences.AttestationPreference.None
            && !IsSelfAttestation(attestedCred.Aaguid, webAuthnStatement))
        {
            webAuthnStatement = NoneAttestationStatement.Empty;
        }

        // Wrap authenticator data
        var webAuthnAuthData = WebAuthnAuthenticatorData.Decode(ctapResponse.AuthenticatorDataRaw);

        // Create attestation object from the (possibly replaced) statement so AttestationObject,
        // RawAttestationObject and AttestationStatement agree.
        var attestationObject = WebAuthnAttestationObject.Create(webAuthnAuthData, webAuthnStatement);

        // Parse extension outputs via pipeline
        var extensionOutputs = await extensions.PrepareOutputsAsync(ctapResponse, webAuthnAuthData, tokenSession, cancellationToken)
            .ConfigureAwait(false);

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

    /// <summary>
    /// WebAuthn L3 §5.1.3 ("none" branch): "If the aaguid in the attested credential data is 16 zero
    /// bytes, fmt is "packed", and "x5c" is absent ... then self attestation is being used and no
    /// further action is needed."
    /// </summary>
    private static bool IsSelfAttestation(Guid aaguid, AttestationStatement statement) =>
        statement is PackedAttestationStatement { X5c: null or { Count: 0 } }
        && aaguid == Guid.Empty;
}