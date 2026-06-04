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

using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Extensions.Adapters;

/// <summary>
/// Adapter for the <c>previewSign</c> WebAuthn extension.
/// </summary>
/// <remarks>
/// <para>
/// The previewSign extension generates a separate signing key bound to the same authenticator.
/// Registration requests the generated signing key; authentication requests a signature over
/// algorithm-specific signing inputs.
/// </para>
/// <para>
/// This adapter translates WebAuthn-level previewSign inputs to the Fido2 extension encoder and
/// translates extension outputs back into WebAuthn-level previewSign outputs.
/// </para>
/// </remarks>
internal static class PreviewSignAdapter
{
    private const string ExtensionId = "previewSign";

    /// <summary>
    /// Applies previewSign input to the CTAP extension builder for registration.
    /// </summary>
    /// <param name="builder">The extension builder.</param>
    /// <param name="input">The previewSign registration input.</param>
    /// <param name="options">The original registration options (used for UV preference).</param>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when the previewSign registration input cannot be applied.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The adapter derives the previewSign signing policy from the WebAuthn user verification
    /// preference: required user verification maps to <see cref="PreviewSignFlags.RequireUserVerification"/>;
    /// other values map to <see cref="PreviewSignFlags.RequireUserPresence"/>.
    /// </para>
    /// </remarks>
    public static void ApplyToBuilderForRegistration(
        ExtensionBuilder builder,
        PreviewSign.PreviewSignRegistrationInput input,
        RegistrationOptions options)
    {
        // Derive flags from the WebAuthn user verification preference.
        byte resolvedFlags = options.UserVerification == UserVerificationPreference.Required
            ? (byte)PreviewSignFlags.RequireUserVerification
            : (byte)PreviewSignFlags.RequireUserPresence;

        // Translate WebAuthn input to Fido2 input
        var fido2Input = new Fido2.Extensions.PreviewSignRegistrationInput(
            algorithms: input.Algorithms.Select(a => a.Value).ToList(),
            flags: resolvedFlags);

        builder.WithPreviewSign(fido2Input);
    }

    /// <summary>
    /// Applies previewSign input to the CTAP extension builder for authentication.
    /// </summary>
    /// <param name="builder">The extension builder.</param>
    /// <param name="input">The previewSign authentication input.</param>
    /// <param name="allowCredentials">The allow list from authentication options.</param>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when the previewSign authentication input cannot be applied.
    /// </exception>
    /// <remarks>
    /// Authentication with previewSign requires a non-empty allow list and a corresponding
    /// <c>signByCredential</c> entry for each allowed credential.
    /// </remarks>
    public static void ApplyToBuilderForAuthentication(
        ExtensionBuilder builder,
        PreviewSign.PreviewSignAuthenticationInput input,
        IReadOnlyList<Fido2.Credentials.PublicKeyCredentialDescriptor>? allowCredentials)
    {
        // Signing requires knowing which credential's generated key to use.
        if (allowCredentials is null || allowCredentials.Count == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign authentication requires a non-empty allowCredentials list");
        }

        // Each allowed credential must have a corresponding previewSign input entry.
        var signByCredIds = new HashSet<ReadOnlyMemory<byte>>(
            input.SignByCredential.Keys,
            ByteArrayKeyComparer.Instance);

        foreach (var allowedCred in allowCredentials)
        {
            if (!signByCredIds.Contains(allowedCred.Id))
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidRequest,
                    "previewSign signByCredential is missing entries for one or more allowCredentials");
            }
        }

        // Multi-credential probe-selection is not implemented yet; keep the current auth path single-credential.
        if (input.SignByCredential.Count != 1)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "previewSign authentication currently supports only single-credential scope; " +
                "multi-credential probe-selection is not implemented. " +
                "To use previewSign now: scope signByCredential to exactly one credential " +
                "that matches the single entry in allowCredentials.");
        }

        // Extract the single credential's params
        var (credentialId, signingParams) = input.SignByCredential.First();

        // Verify it matches the single allowCredentials entry
        if (allowCredentials.Count != 1 || !allowCredentials[0].Id.Span.SequenceEqual(credentialId.Span))
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign signByCredential's single entry must match allowCredentials[0]");
        }

        // Translate WebAuthn SigningParams to Fido2 SigningParams. additionalArgs are
        // algorithm-specific bytes and pass through unchanged.
        var fido2SigningParams = new Fido2.Extensions.PreviewSignSigningParams(
            keyHandle: signingParams.KeyHandle,
            tbs: signingParams.Tbs,
            additionalArgs: signingParams.AdditionalArgs);

        // Translate to Fido2 authentication input (expects dictionary)
        var signByCredential = new Dictionary<ReadOnlyMemory<byte>, Fido2.Extensions.PreviewSignSigningParams>(
            ByteArrayKeyComparer.Instance)
        {
            [credentialId] = fido2SigningParams
        };
        var fido2Input = new Fido2.Extensions.PreviewSignAuthenticationInput(signByCredential);
        builder.WithPreviewSign(fido2Input);
    }

    /// <summary>
    /// Parses the previewSign registration output from authenticator data.
    /// </summary>
    /// <param name="authData">The authenticator data with parsed extensions.</param>
    /// <param name="unsignedExtensionOutputs">Top-level unsigned extension outputs (CTAP key 8).</param>
    /// <returns>
    /// A <see cref="PreviewSignRegistrationOutput"/> if the extension output is present and valid;
    /// otherwise null.
    /// </returns>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when the extension output is present but malformed (InvalidState).
    /// </exception>
    /// <remarks>
    /// Reads the algorithm from <c>authData.extensions["previewSign"]</c> and the generated signing
    /// key attestation object from <c>unsignedExtensionOutputs["previewSign"]</c>. If unsigned
    /// extension outputs are missing, the method builds the generated key from the response
    /// authenticator data's attested credential data.
    /// </remarks>
    public static PreviewSign.PreviewSignRegistrationOutput? ParseRegistrationOutput(
        WebAuthnAuthenticatorData authData,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? unsignedExtensionOutputs)
    {
        if (!authData.ParsedExtensions.TryGetValue(ExtensionId, out var rawCbor))
        {
            return null;
        }

        // Decode algorithm + flags from authData.extensions["previewSign"] via Fido2 decoder
        var reader = new System.Formats.Cbor.CborReader(rawCbor, System.Formats.Cbor.CborConformanceMode.Ctap2Canonical);

        (int algorithmInt, int? flagsInt) = Fido2.Extensions.PreviewSignCbor.DecodeRegistrationOutput(reader);

        var algorithm = new CoseAlgorithm(algorithmInt);
        var flags = flagsInt.HasValue
            ? (PreviewSign.PreviewSignFlags)flagsInt.Value
            : PreviewSign.PreviewSignFlags.RequireUserPresence;

        // Try to read attestation object from unsignedExtensionOutputs["previewSign"]
        if (unsignedExtensionOutputs?.TryGetValue(ExtensionId, out var unsignedCbor) == true)
        {
            // Decode the CTAP-shaped inner attestation object via the Fido2 decoder. The wire
            // payload uses integer keys ({1:fmt, 2:authData, 3:attStmt}), so WebAuthn rebuilds
            // the attestation object from the decoded components.
            Fido2.Extensions.PreviewSignCbor.InnerAttestationObject inner =
                Fido2.Extensions.PreviewSignCbor.DecodeUnsignedRegistrationOutput(unsignedCbor);

            var innerAuthData = WebAuthnAuthenticatorData.Decode(inner.AuthData);
            var format = new AttestationFormat(inner.Fmt);
            var statement = AttestationStatement.Decode(format, inner.AttStmtRawCbor);
            var attestationObject = WebAuthnAttestationObject.Create(innerAuthData, statement);

            // Extract key handle and public key from attested credential data
            var attestedCredData = attestationObject.AuthenticatorData.AttestedCredentialData;
            if (attestedCredData is null)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "previewSign attestation object missing attested credential data");
            }

            var generatedKey = new PreviewSign.GeneratedSigningKey(
                KeyHandle: attestedCredData.CredentialId,
                PublicKey: CoseKey.Decode(attestedCredData.CredentialPublicKey),
                Algorithm: algorithm,
                AttestationObject: attestationObject);

            return new PreviewSign.PreviewSignRegistrationOutput(generatedKey);
        }

        // Fallback: build from authData's attested credential data.
        if (authData.AttestedCredentialData is null)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign output requires attested credential data");
        }

        var fallbackKey = new PreviewSign.GeneratedSigningKey(
            KeyHandle: authData.AttestedCredentialData.CredentialId,
            PublicKey: CoseKey.Decode(authData.AttestedCredentialData.CredentialPublicKey),
            Algorithm: algorithm,
            AttestationObject: null); // No attestation object in fallback path

        return new PreviewSign.PreviewSignRegistrationOutput(GeneratedKey: fallbackKey);
    }

    /// <summary>
    /// Parses the previewSign authentication output from authenticator data.
    /// </summary>
    /// <param name="authData">The authenticator data with parsed extensions.</param>
    /// <returns>
    /// A <see cref="PreviewSignAuthenticationOutput"/> if the extension output is present and valid;
    /// otherwise null.
    /// </returns>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when the extension output is present but malformed (InvalidState).
    /// </exception>
    public static PreviewSign.PreviewSignAuthenticationOutput? ParseAuthenticationOutput(
        WebAuthnAuthenticatorData authData)
    {
        if (!authData.ParsedExtensions.TryGetValue(ExtensionId, out var rawCbor))
        {
            return null;
        }

        // Decode signature via Fido2 decoder
        ReadOnlyMemory<byte> signature = Fido2.Extensions.PreviewSignCbor.DecodeAuthenticationOutput(rawCbor);

        return new PreviewSign.PreviewSignAuthenticationOutput(signature);
    }
}
