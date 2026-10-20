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
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Extensions.Codec;

/// <summary>
/// Translation between the WebAuthn <c>previewSign</c> extension and its CTAP encoding.
/// </summary>
/// <remarks>
/// <para>
/// The previewSign extension generates a separate signing key bound to the same authenticator.
/// Registration requests the generated signing key; authentication requests a signature over
/// algorithm-specific signing inputs.
/// </para>
/// <para>
/// WebAuthn-level previewSign inputs are translated to the Fido2 extension encoder, and extension
/// outputs are translated back into WebAuthn-level previewSign outputs.
/// </para>
/// </remarks>
internal static class PreviewSignCodec
{
    private const string ExtensionId = "previewSign";

    /// <summary>
    /// Applies previewSign input to the CTAP extension builder for registration.
    /// </summary>
    /// <param name="builder">The extension builder.</param>
    /// <param name="input">The previewSign registration input.</param>
    /// <param name="userVerification">The WebAuthn user verification preference (drives the previewSign flags).</param>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when the previewSign registration input cannot be applied.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The previewSign signing policy is derived from the WebAuthn user verification
    /// preference: required user verification maps to <see cref="PreviewSignFlags.RequireUserVerification"/>;
    /// other values map to <see cref="PreviewSignFlags.RequireUserPresence"/>.
    /// </para>
    /// </remarks>
    public static void ApplyToBuilderForRegistration(
        ExtensionBuilder builder,
        PreviewSign.PreviewSignRegistrationInput input,
        UserVerificationPreference userVerification)
    {
        // Derive flags from the WebAuthn user verification preference.
        byte resolvedFlags = userVerification == UserVerificationPreference.Required
            ? (byte)PreviewSignFlags.RequireUserVerification
            : (byte)PreviewSignFlags.RequireUserPresence;

        // Translate WebAuthn input to Fido2 input
        var fido2Input = new Fido2.Extensions.PreviewSignRegistrationInput(
            algorithms: input.Algorithms.Select(a => a.Value).ToList(),
            flags: resolvedFlags);

        builder.WithPreviewSign(fido2Input);
    }

    /// <summary>
    /// Translates one credential's WebAuthn signing parameters to the CTAP2 shape. additionalArgs are
    /// algorithm-specific bytes and pass through unchanged.
    /// </summary>
    public static Fido2.Extensions.PreviewSignSigningParams ToCtap(PreviewSign.PreviewSignSigningParams signingParams) =>
        new(keyHandle: signingParams.KeyHandle, tbs: signingParams.Tbs, additionalArgs: signingParams.AdditionalArgs);

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
    /// Thrown when the extension output is present but malformed, or when the generated-key attestation
    /// object is missing from the unsigned extension outputs (InvalidState).
    /// </exception>
    /// <remarks>
    /// Reads the algorithm from <c>authData.extensions["previewSign"]</c> and the generated signing
    /// key attestation object from <c>unsignedExtensionOutputs["previewSign"][7]</c>. The key handle and
    /// public key come from the attested credential data of that <em>inner</em> attestation object
    /// (previewSign draft v4 §10.2.1); the outer WebAuthn credential is never a substitute, so a missing
    /// unsigned output fails the parse rather than returning the wrong key.
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

        // previewSign draft v4 §10.2.1: "Let origAttObj be unsignedExtOutputs["previewSign"][att-obj]";
        // keyHandle and publicKey are copied from innerAuthData.attestedCredentialData, and
        // attestationObject is a required member of the generated key. The outer credential's key is
        // the WebAuthn credential, not the signing key, so there is nothing to fall back to.
        if (unsignedExtensionOutputs?.TryGetValue(ExtensionId, out var unsignedCbor) != true)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign output is present in the authenticator data but the generated-key attestation object is missing from unsignedExtensionOutputs (previewSign draft v4 §10.2.1)");
        }

        // Decode the CTAP-shaped inner attestation object via the Fido2 decoder. The wire
        // payload uses integer keys ({1:fmt, 2:authData, 3:attStmt}), so WebAuthn rebuilds
        // the attestation object from the decoded components.
        Fido2.Extensions.PreviewSignCbor.InnerAttestationObject inner =
            Fido2.Extensions.PreviewSignCbor.DecodeUnsignedRegistrationOutput(unsignedCbor);

        var innerAuthData = WebAuthnAuthenticatorData.Decode(inner.AuthData);
        var format = new AttestationFormat(inner.Fmt);
        var statement = AttestationStatement.Decode(format, inner.AttStmtRawCbor);
        var attestationObject = WebAuthnAttestationObject.Create(innerAuthData, statement);

        // Extract key handle and public key from the inner attested credential data
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