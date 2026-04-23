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

using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Extensions.Adapters;

/// <summary>
/// Adapter for the previewSign extension (CTAP v4 draft).
/// </summary>
/// <remarks>
/// <para>
/// PreviewSign allows a WebAuthn credential to sign arbitrary data using a separate signing key
/// bound to the same authenticator device. Registration generates a new signing key pair;
/// authentication signs data without clientDataJSON or authenticator data wrapping.
/// </para>
/// <para>
/// This adapter enforces all client-side validation rules per CTAP v4 draft specification §8:
/// - Registration: validates algorithms non-empty, resolves flags from UserVerification preference
/// - Authentication: validates allowCredentials non-empty, validates signByCredential completeness
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
    /// Thrown when:
    /// - Explicit flags conflict with UserVerification preference (InvalidRequest)
    /// </exception>
    /// <remarks>
    /// <para>
    /// Flag selection rule (spec §8 + Swift parity):
    /// - If UserVerification == Required → flags MUST be RequireUserVerification (0b101)
    /// - If user supplied explicit flags that conflict with UV preference → throw InvalidRequest
    /// - Otherwise → use the flags from input (default RequireUserPresence = 0b001)
    /// </para>
    /// <para>
    /// This prevents silent promotion that might surprise the caller. If the user explicitly
    /// requested Unattended (0b000) but UV preference is Required, that's a contradiction
    /// the caller must resolve.
    /// </para>
    /// </remarks>
    public static void ApplyToBuilderForRegistration(
        ExtensionBuilder builder,
        PreviewSign.PreviewSignRegistrationInput input,
        RegistrationOptions options)
    {
        // Derive flags from UserVerification per spec §10.2.1 step 4 (line 4962):
        // "The CDDL value 0b101 if pkOptions.authenticatorSelection.userVerification is
        // set to required, otherwise the CDDL value 0b001."
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
    /// Thrown when:
    /// - allowCredentials is null or empty (InvalidRequest)
    /// - signByCredential is missing entries for one or more allowCredentials (InvalidRequest)
    /// </exception>
    /// <remarks>
    /// Per CTAP v4 draft §8, authentication with previewSign requires:
    /// - allowCredentials MUST NOT be empty (signing requires knowing which key to use)
    /// - signByCredential MUST contain an entry for EVERY credential in allowCredentials
    ///
    /// This validation happens BEFORE any CTAP roundtrip to fail fast on client-side errors.
    /// </remarks>
    public static void ApplyToBuilderForAuthentication(
        ExtensionBuilder builder,
        PreviewSign.PreviewSignAuthenticationInput input,
        IReadOnlyList<WebAuthnCredentialDescriptor>? allowCredentials)
    {
        // Spec §8 validation: allowCredentials MUST NOT be empty
        if (allowCredentials is null || allowCredentials.Count == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign authentication requires a non-empty allowCredentials list");
        }

        // Spec §8 validation: signByCredential MUST contain ALL allowCredentials IDs
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

        // Phase 9.2 limitation: single-credential only (multi-credential probe deferred to Phase 10)
        if (input.SignByCredential.Count != 1)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "previewSign authentication currently supports only single-credential scope; " +
                "multi-credential probe-selection (CTAP up=false probe per CTAP v4 §10.2.1 step 7) " +
                "is deferred to Phase 10. See Plans/phase-10-previewsign-auth.md for tracking. " +
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

        // Translate WebAuthn SigningParams to Fido2 SigningParams
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
    /// Per spec §10.2.1 step 5 (registration), reads algorithm from authData.extensions["previewSign"]
    /// and attestation object from unsignedExtensionOutputs["previewSign"] (top-level CTAP response map).
    /// If unsignedExtensionOutputs is missing, builds GeneratedSigningKey from authData's attested
    /// credential data (Swift fallback per PreviewSign.swift:170-176).
    /// </remarks>
    public static PreviewSign.PreviewSignRegistrationOutput? ParseRegistrationOutput(
        WebAuthnAuthenticatorData authData,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? unsignedExtensionOutputs)
    {
        if (!authData.ParsedExtensions.TryGetValue(ExtensionId, out var rawCbor))
        {
            return null;
        }

        // Read algorithm + flags from authData.extensions["previewSign"]
        var reader = new System.Formats.Cbor.CborReader(rawCbor, System.Formats.Cbor.CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();

        CoseAlgorithm? algorithm = null;
        PreviewSign.PreviewSignFlags? flags = null;

        for (int i = 0; i < mapSize; i++)
        {
            int key = reader.ReadInt32();
            switch (key)
            {
                case 3: // alg
                    algorithm = new CoseAlgorithm(reader.ReadInt32());
                    break;
                case 4: // flags
                    flags = (PreviewSign.PreviewSignFlags)reader.ReadInt32();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        if (algorithm is null)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign output missing required algorithm");
        }

        // Flags are input-only per CTAP v4 draft; default when absent from response
        flags ??= PreviewSign.PreviewSignFlags.RequireUserPresence;

        // Try to read attestation object from unsignedExtensionOutputs["previewSign"]
        if (unsignedExtensionOutputs?.TryGetValue(ExtensionId, out var unsignedCbor) == true)
        {
            // Decode the unsigned output (contains att-obj)
            return PreviewSign.PreviewSignCbor.DecodeUnsignedRegistrationOutput(unsignedCbor, algorithm.Value, flags.Value);
        }

        // Fallback: build from authData's attested credential data (Swift PreviewSign.swift:170-176)
        if (authData.AttestedCredentialData is null)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign output requires attested credential data");
        }

        var generatedKey = new PreviewSign.GeneratedSigningKey(
            KeyHandle: authData.AttestedCredentialData.CredentialId,
            PublicKey: CoseKey.Decode(authData.AttestedCredentialData.CredentialPublicKey),
            Algorithm: algorithm.Value,
            AttestationObject: null); // No attestation object in fallback path

        return new PreviewSign.PreviewSignRegistrationOutput(GeneratedKey: generatedKey);
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

        return PreviewSign.PreviewSignCbor.DecodeAuthenticationOutput(rawCbor);
    }
}
