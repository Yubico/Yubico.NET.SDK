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

using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions.Adapters;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Extensions;

/// <summary>
/// Orchestrates extension input/output processing for WebAuthn operations.
/// </summary>
internal sealed class ExtensionPipeline
{
    /// <summary>
    /// Builds the CBOR extensions map for registration (MakeCredential).
    /// </summary>
    /// <param name="inputs">The extension inputs.</param>
    /// <param name="options">The registration options (used for UV preference in previewSign).</param>
    /// <returns>The CBOR-encoded extensions map, or null if no extensions requested.</returns>
    public static ReadOnlyMemory<byte>? BuildRegistrationExtensionsCbor(
        RegistrationExtensionInputs? inputs,
        RegistrationOptions options)
    {
        if (inputs is null)
        {
            return null;
        }

        // PreviewSign has its own CBOR format - cannot use ExtensionBuilder
        var previewSignCbor = PreviewSignAdapter.BuildRegistrationCbor(inputs.PreviewSign, options);

        // If ONLY previewSign is present, return its CBOR directly wrapped in the extensions map
        if (previewSignCbor is not null &&
            inputs.CredProtect is null &&
            inputs.CredBlob is null &&
            inputs.MinPinLength is null &&
            inputs.LargeBlob is null &&
            inputs.Prf is null)
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(1);
            writer.WriteTextString("previewSign");
            writer.WriteEncodedValue(previewSignCbor);
            writer.WriteEndMap();
            return writer.Encode();
        }

        // Build standard extensions via ExtensionBuilder
        var builder = new ExtensionBuilder();
        var hasStandardExtensions = false;

        // CredProtect
        if (inputs.CredProtect is not null)
        {
            CredProtectAdapter.ApplyToBuilder(builder, inputs.CredProtect);
            hasStandardExtensions = true;
        }

        // CredBlob
        if (inputs.CredBlob is not null)
        {
            CredBlobAdapter.ApplyToBuilder(builder, inputs.CredBlob);
            hasStandardExtensions = true;
        }

        // MinPinLength
        if (inputs.MinPinLength is not null)
        {
            MinPinLengthAdapter.ApplyToBuilder(builder);
            hasStandardExtensions = true;
        }

        // LargeBlob
        if (inputs.LargeBlob is not null)
        {
            LargeBlobAdapter.ApplyToBuilder(builder, inputs.LargeBlob);
            hasStandardExtensions = true;
        }

        // PRF
        if (inputs.Prf is not null)
        {
            PrfAdapter.ApplyToBuilderForRegistration(builder, inputs.Prf);
            hasStandardExtensions = true;
        }

        // CredProps - no CTAP input, client-side only
        // (credProps is derived from residentKey option, not sent to authenticator)

        // If previewSign AND standard extensions are present, merge them
        if (previewSignCbor is not null && hasStandardExtensions)
        {
            // Build standard extensions map via ExtensionBuilder
            var standardCbor = builder.Build();
            if (standardCbor is null)
            {
                // This shouldn't happen (hasStandardExtensions is true), but handle defensively
                var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
                writer.WriteStartMap(1);
                writer.WriteTextString("previewSign");
                writer.WriteEncodedValue(previewSignCbor);
                writer.WriteEndMap();
                return writer.Encode();
            }

            // Parse the standard map, count entries, and re-encode with previewSign
            var reader = new CborReader(standardCbor.Value, CborConformanceMode.Ctap2Canonical);
            int? standardMapSize = reader.ReadStartMap();
            var standardEntries = new List<(string key, ReadOnlyMemory<byte> value)>();

            for (int i = 0; i < (standardMapSize ?? int.MaxValue); i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap) break;
                var key = reader.ReadTextString();
                var valueStart = reader.BytesRemaining;
                reader.SkipValue();
                var valueEnd = reader.BytesRemaining;
                var valueLength = valueStart - valueEnd;
                var value = standardCbor.Value.Slice(standardCbor.Value.Length - valueStart, valueLength);
                standardEntries.Add((key, value));
            }

            // Merge standard entries + previewSign, sort by CTAP2 canonical (length-then-lex)
            var allEntries = new List<KeyValuePair<string, ReadOnlyMemory<byte>>>(standardEntries.Count + 1);
            allEntries.AddRange(standardEntries.Select(t => new KeyValuePair<string, ReadOnlyMemory<byte>>(t.key, t.value)));
            allEntries.Add(new KeyValuePair<string, ReadOnlyMemory<byte>>("previewSign", previewSignCbor));

            allEntries.Sort((a, b) => Ctap2CanonicalKeyComparer.Instance.Compare(a.Key, b.Key));

            // Write canonically-ordered map
            var mergedWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
            mergedWriter.WriteStartMap(allEntries.Count);

            foreach (var (key, value) in allEntries)
            {
                mergedWriter.WriteTextString(key);
                mergedWriter.WriteEncodedValue(value.Span);
            }

            mergedWriter.WriteEndMap();
            return mergedWriter.Encode();
        }

        // Only standard extensions (no previewSign)
        if (!hasStandardExtensions && previewSignCbor is null)
        {
            return null;
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds the CBOR extensions map for authentication (GetAssertion).
    /// </summary>
    /// <param name="inputs">The extension inputs.</param>
    /// <param name="allowCredentials">The allow list for filtering per-credential inputs.</param>
    /// <returns>The CBOR-encoded extensions map, or null if no extensions requested.</returns>
    public static ReadOnlyMemory<byte>? BuildAuthenticationExtensionsCbor(
        AuthenticationExtensionInputs? inputs,
        IReadOnlyList<WebAuthnCredentialDescriptor>? allowCredentials)
    {
        if (inputs is null)
        {
            return null;
        }

        // PreviewSign has its own CBOR format - cannot use ExtensionBuilder
        var previewSignCbor = PreviewSignAdapter.BuildAuthenticationCbor(inputs.PreviewSign, allowCredentials);

        // If ONLY previewSign is present, return its CBOR directly wrapped in the extensions map
        if (previewSignCbor is not null &&
            inputs.LargeBlob is null &&
            inputs.Prf is null)
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(1);
            writer.WriteTextString("previewSign");
            writer.WriteEncodedValue(previewSignCbor);
            writer.WriteEndMap();
            return writer.Encode();
        }

        var builder = new ExtensionBuilder();
        var hasStandardExtensions = false;

        // LargeBlob (read operations during assertion)
        if (inputs.LargeBlob is not null)
        {
            // Phase 6 scope deferred - not yet fully implemented
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "LargeBlob authentication operations are not yet implemented (Phase 6 scope deferred). Upgrade SDK for full support.");
        }

        // PRF
        if (inputs.Prf is not null)
        {
            PrfAdapter.ApplyToBuilderForAuthentication(builder, inputs.Prf, allowCredentials);
            hasStandardExtensions = true;
        }

        // If previewSign AND standard extensions are present, merge them
        if (previewSignCbor is not null && hasStandardExtensions)
        {
            // Build standard extensions map via ExtensionBuilder
            var standardCbor = builder.Build();
            if (standardCbor is null)
            {
                // This shouldn't happen (hasStandardExtensions is true), but handle defensively
                var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
                writer.WriteStartMap(1);
                writer.WriteTextString("previewSign");
                writer.WriteEncodedValue(previewSignCbor);
                writer.WriteEndMap();
                return writer.Encode();
            }

            // Parse the standard map, count entries, and re-encode with previewSign
            var reader = new CborReader(standardCbor.Value, CborConformanceMode.Ctap2Canonical);
            int? standardMapSize = reader.ReadStartMap();
            var standardEntries = new List<(string key, ReadOnlyMemory<byte> value)>();

            for (int i = 0; i < (standardMapSize ?? int.MaxValue); i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap) break;
                var key = reader.ReadTextString();
                var valueStart = reader.BytesRemaining;
                reader.SkipValue();
                var valueEnd = reader.BytesRemaining;
                var valueLength = valueStart - valueEnd;
                var value = standardCbor.Value.Slice(standardCbor.Value.Length - valueStart, valueLength);
                standardEntries.Add((key, value));
            }

            // Merge standard entries + previewSign, sort by CTAP2 canonical (length-then-lex)
            var allEntries = new List<KeyValuePair<string, ReadOnlyMemory<byte>>>(standardEntries.Count + 1);
            allEntries.AddRange(standardEntries.Select(t => new KeyValuePair<string, ReadOnlyMemory<byte>>(t.key, t.value)));
            allEntries.Add(new KeyValuePair<string, ReadOnlyMemory<byte>>("previewSign", previewSignCbor));

            allEntries.Sort((a, b) => Ctap2CanonicalKeyComparer.Instance.Compare(a.Key, b.Key));

            // Write canonically-ordered map
            var mergedWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
            mergedWriter.WriteStartMap(allEntries.Count);

            foreach (var (key, value) in allEntries)
            {
                mergedWriter.WriteTextString(key);
                mergedWriter.WriteEncodedValue(value.Span);
            }

            mergedWriter.WriteEndMap();
            return mergedWriter.Encode();
        }

        // Only standard extensions (no previewSign)
        if (!hasStandardExtensions && previewSignCbor is null)
        {
            return null;
        }

        return builder.Build();
    }

    /// <summary>
    /// Parses registration extension outputs from authenticator data.
    /// </summary>
    /// <param name="inputs">The original inputs (to know what was requested).</param>
    /// <param name="authData">The authenticator data with extension outputs.</param>
    /// <param name="unsignedExtensionOutputs">Top-level unsigned extension outputs map (CTAP key 8).</param>
    /// <param name="originalOptions">The original registration options.</param>
    /// <returns>The parsed extension outputs, or null if no extensions were requested.</returns>
    public static RegistrationExtensionOutputs? ParseRegistrationOutputs(
        RegistrationExtensionInputs? inputs,
        WebAuthnAuthenticatorData authData,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? unsignedExtensionOutputs,
        RegistrationOptions originalOptions)
    {
        if (inputs is null)
        {
            return null;
        }

        Outputs.CredProtectOutput? credProtect = null;
        Outputs.CredBlobOutput? credBlob = null;
        Outputs.MinPinLengthOutput? minPinLength = null;
        Outputs.LargeBlobRegistrationOutput? largeBlob = null;
        Outputs.PrfRegistrationOutput? prf = null;
        Outputs.CredPropsOutput? credProps = null;

        // CredProtect
        if (inputs.CredProtect is not null)
        {
            try
            {
                credProtect = CredProtectAdapter.ParseRegistrationOutput(authData.ParsedExtensions);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                credProtect = null;
            }
        }

        // CredBlob
        if (inputs.CredBlob is not null)
        {
            try
            {
                credBlob = CredBlobAdapter.ParseRegistrationOutput(authData.ParsedExtensions);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                credBlob = null;
            }
        }

        // MinPinLength
        if (inputs.MinPinLength is not null)
        {
            try
            {
                minPinLength = MinPinLengthAdapter.ParseOutput(authData.ParsedExtensions);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                minPinLength = null;
            }
        }

        // LargeBlob
        if (inputs.LargeBlob is not null)
        {
            try
            {
                largeBlob = LargeBlobAdapter.ParseRegistrationOutput(authData.ParsedExtensions);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                largeBlob = null;
            }
        }

        // PRF
        if (inputs.Prf is not null)
        {
            try
            {
                prf = PrfAdapter.ParseRegistrationOutput(authData.ParsedExtensions);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                prf = null;
            }
        }

        // CredProps - client-derived from residentKey option
        if (inputs.CredProps is not null)
        {
            credProps = CredPropsAdapter.DeriveOutput(originalOptions.ResidentKey);
        }

        // PreviewSign
        PreviewSign.PreviewSignRegistrationOutput? previewSign = null;
        if (inputs.PreviewSign is not null)
        {
            try
            {
                previewSign = PreviewSignAdapter.ParseRegistrationOutput(authData, unsignedExtensionOutputs);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                previewSign = null;
            }
        }

        return new RegistrationExtensionOutputs(
            CredProtect: credProtect,
            CredBlob: credBlob,
            MinPinLength: minPinLength,
            LargeBlob: largeBlob,
            Prf: prf,
            CredProps: credProps,
            PreviewSign: previewSign);
    }

    /// <summary>
    /// Parses authentication extension outputs from authenticator data.
    /// </summary>
    /// <param name="inputs">The original inputs (to know what was requested).</param>
    /// <param name="authData">The authenticator data with extension outputs.</param>
    /// <returns>The parsed extension outputs, or null if no extensions were requested.</returns>
    public static AuthenticationExtensionOutputs? ParseAuthenticationOutputs(
        AuthenticationExtensionInputs? inputs,
        WebAuthnAuthenticatorData authData)
    {
        if (inputs is null)
        {
            return null;
        }

        Outputs.CredBlobAssertionOutput? credBlob = null;
        Outputs.LargeBlobAuthenticationOutput? largeBlob = null;
        Outputs.PrfAuthenticationOutput? prf = null;

        // CredBlob - always check (can be present even if not explicitly requested)
        try
        {
            credBlob = CredBlobAdapter.ParseAuthenticationOutput(authData.ParsedExtensions);
        }
        catch (System.Formats.Cbor.CborContentException)
        {
            // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
            credBlob = null;
        }

        // LargeBlob
        if (inputs.LargeBlob is not null)
        {
            try
            {
                largeBlob = LargeBlobAdapter.ParseAuthenticationOutput(authData.ParsedExtensions);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                largeBlob = null;
            }
        }

        // PRF
        if (inputs.Prf is not null)
        {
            try
            {
                prf = PrfAdapter.ParseAuthenticationOutput(authData.ParsedExtensions);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                prf = null;
            }
        }

        // PreviewSign
        PreviewSign.PreviewSignAuthenticationOutput? previewSign = null;
        if (inputs.PreviewSign is not null)
        {
            try
            {
                previewSign = PreviewSignAdapter.ParseAuthenticationOutput(authData);
            }
            catch (System.Formats.Cbor.CborContentException)
            {
                // Malformed extension output: skip silently per WebAuthn spec; some authenticators return junk.
                previewSign = null;
            }
        }

        return new AuthenticationExtensionOutputs(
            CredBlob: credBlob,
            LargeBlob: largeBlob,
            Prf: prf,
            PreviewSign: previewSign);
    }
}

/// <summary>
/// Comparer for CTAP2 canonical key ordering (length-ascending, then lexicographic).
/// </summary>
internal sealed class Ctap2CanonicalKeyComparer : IComparer<string>
{
    public static readonly Ctap2CanonicalKeyComparer Instance = new();

    private Ctap2CanonicalKeyComparer() { }

    public int Compare(string? a, string? b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }
        if (a is null)
        {
            return -1;
        }
        if (b is null)
        {
            return 1;
        }

        // CTAP2 canonical: length first, then lexicographic
        int lengthDiff = a.Length - b.Length;
        return lengthDiff != 0 ? lengthDiff : string.CompareOrdinal(a, b);
    }
}
