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

using Yubico.YubiKit.WebAuthn.Client.Registration;
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
    /// <summary>
    /// Builds the CBOR-encoded previewSign extension input for registration.
    /// </summary>
    /// <param name="input">The previewSign registration input, or null if not requested.</param>
    /// <param name="options">The original registration options (used for UV preference).</param>
    /// <returns>
    /// CBOR-encoded map fragment {3: [alg...], 4: flags}, or null if input is null.
    /// </returns>
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
    public static byte[]? BuildRegistrationCbor(
        PreviewSignRegistrationInput? input,
        RegistrationOptions options)
    {
        if (input is null)
        {
            return null;
        }

        // Resolve effective flags per spec §8 UV preference rule
        var effectiveFlags = input.Flags;

        if (options.UserVerification == UserVerificationPreference.Required)
        {
            // UV is required — flags MUST include UV bit
            if (input.Flags == PreviewSignFlags.RequireUserPresence)
            {
                // User didn't specify explicit flags (used default) — promote to UV
                effectiveFlags = PreviewSignFlags.RequireUserVerification;
            }
            else if (input.Flags != PreviewSignFlags.RequireUserVerification)
            {
                // User explicitly requested incompatible flags (e.g., Unattended)
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidRequest,
                    "previewSign Flags conflict with UserVerification preference: " +
                    $"UserVerification=Required but Flags={input.Flags}");
            }
            // else: user explicitly requested RequireUserVerification — use as-is
        }

        // Build CBOR input with resolved flags
        var resolvedInput = new PreviewSignRegistrationInput(
            algorithms: input.Algorithms,
            flags: effectiveFlags);

        return PreviewSignCbor.EncodeRegistrationInput(resolvedInput);
    }

    /// <summary>
    /// Builds the CBOR-encoded previewSign extension input for authentication.
    /// </summary>
    /// <param name="input">The previewSign authentication input, or null if not requested.</param>
    /// <param name="allowCredentials">The allow list from authentication options.</param>
    /// <returns>
    /// CBOR-encoded map {credId: {2: kh, 6: tbs, 7?: args}}, or null if input is null.
    /// </returns>
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
    public static byte[]? BuildAuthenticationCbor(
        PreviewSignAuthenticationInput? input,
        IReadOnlyList<WebAuthnCredentialDescriptor>? allowCredentials)
    {
        if (input is null)
        {
            return null;
        }

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

        // All validation passed — encode the full signByCredential map
        return PreviewSignCbor.EncodeAuthenticationInput(input);
    }

    /// <summary>
    /// Parses the previewSign registration output from authenticator data.
    /// </summary>
    /// <param name="authData">The authenticator data with parsed extensions.</param>
    /// <returns>
    /// A <see cref="PreviewSignRegistrationOutput"/> if the extension output is present and valid;
    /// otherwise null.
    /// </returns>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when the extension output is present but malformed (InvalidState).
    /// </exception>
    /// <remarks>
    /// <para>
    /// Per CTAP v4 draft §4, the unsigned attestation object form (att-obj key 7) is the
    /// preferred authoritative source. This method tries the unsigned form FIRST, then falls
    /// back to the signed form (keys 3,4,6) if unsigned is not present.
    /// </para>
    /// <para>
    /// Verified attestation values supersede loose top-level fields when both are present.
    /// </para>
    /// </remarks>
    public static PreviewSignRegistrationOutput? ParseRegistrationOutput(
        WebAuthnAuthenticatorData authData)
    {
        const string ExtensionId = "previewSign";

        if (!authData.ParsedExtensions.TryGetValue(ExtensionId, out var rawCbor))
        {
            return null;
        }

        // Try unsigned form FIRST (preferred per spec §4)
        var output = PreviewSignCbor.DecodeUnsignedRegistrationOutput(rawCbor);

        // Fallback to signed form
        output ??= PreviewSignCbor.DecodeSignedRegistrationOutput(rawCbor);

        return output;
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
    public static PreviewSignAuthenticationOutput? ParseAuthenticationOutput(
        WebAuthnAuthenticatorData authData)
    {
        const string ExtensionId = "previewSign";

        if (!authData.ParsedExtensions.TryGetValue(ExtensionId, out var rawCbor))
        {
            return null;
        }

        return PreviewSignCbor.DecodeAuthenticationOutput(rawCbor);
    }
}
