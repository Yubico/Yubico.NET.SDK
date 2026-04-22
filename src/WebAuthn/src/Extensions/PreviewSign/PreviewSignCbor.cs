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

using System.Formats.Cbor;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Cose;

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// CBOR encoding and decoding for previewSign extension wire format.
/// </summary>
/// <remarks>
/// <para>
/// Per CTAP v4 draft Web Authentication sign extension, previewSign uses integer-keyed
/// CBOR maps with canonical encoding (sorted keys).
/// </para>
/// <para>
/// CBOR map keys (all integer-keyed):
/// - kh (key handle) = 2
/// - alg (algorithm) = 3
/// - flags (UP/UV policy) = 4
/// - tbs (to-be-signed) = 6
/// - args (additional args) = 7
/// - sig (signature) = 6
/// - att-obj (attestation object) = 7
/// </para>
/// </remarks>
internal static class PreviewSignCbor
{
    // CBOR integer map keys per CTAP v4 draft spec
    private const int KeyHandle = 2;
    private const int Algorithm = 3;
    private const int Flags = 4;
    private const int ToBeSigned = 6;
    private const int AdditionalArgs = 7;
    private const int Signature = 6;
    private const int AttestationObject = 7;

    /// <summary>
    /// Encodes registration input (algorithm list + flags) as canonical CBOR.
    /// </summary>
    /// <param name="input">The registration input.</param>
    /// <returns>CBOR-encoded map with keys {3: [alg...], 4: flags}.</returns>
    public static byte[] EncodeRegistrationInput(PreviewSignRegistrationInput input)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2); // Two keys: alg (3) and flags (4)

        // Key 3: algorithms array
        writer.WriteInt32(Algorithm);
        writer.WriteStartArray(input.Algorithms.Count);
        foreach (var alg in input.Algorithms)
        {
            writer.WriteInt32(alg.Value);
        }
        writer.WriteEndArray();

        // Key 4: flags byte
        writer.WriteInt32(Flags);
        writer.WriteInt32((byte)input.Flags);

        writer.WriteEndMap();
        return writer.Encode();
    }

    /// <summary>
    /// Encodes authentication input (signByCredential dictionary) as canonical CBOR.
    /// </summary>
    /// <param name="input">The authentication input.</param>
    /// <returns>
    /// CBOR-encoded map keyed by credential ID (bstr) with values {2: kh, 6: tbs, 7?: args}.
    /// </returns>
    /// <remarks>
    /// Per CTAP v4 draft §5.2, the wire format is a map from credentialId → signing params.
    /// Each signing params map contains:
    /// - 2 (kh): key handle (bstr)
    /// - 6 (tbs): to-be-signed data (bstr)
    /// - 7 (args): optional additional args wrapped as bstr
    /// </remarks>
    public static byte[] EncodeAuthenticationInput(PreviewSignAuthenticationInput input)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(input.SignByCredential.Count);

        foreach (var (credId, signingParams) in input.SignByCredential)
        {
            // Key: credential ID as byte string
            writer.WriteByteString(credId.Span);

            // Value: map with kh, tbs, and optional args
            int paramCount = signingParams.AdditionalArgs.HasValue ? 3 : 2;
            writer.WriteStartMap(paramCount);

            // Key 2: keyHandle
            writer.WriteInt32(KeyHandle);
            writer.WriteByteString(signingParams.KeyHandle.Span);

            // Key 6: tbs
            writer.WriteInt32(ToBeSigned);
            writer.WriteByteString(signingParams.Tbs.Span);

            // Key 7: args (optional, wrapped as bstr)
            if (signingParams.AdditionalArgs.HasValue)
            {
                writer.WriteInt32(AdditionalArgs);
                writer.WriteByteString(signingParams.AdditionalArgs.Value.Span);
            }

            writer.WriteEndMap();
        }

        writer.WriteEndMap();
        return writer.Encode();
    }

    /// <summary>
    /// Decodes signed registration output (algorithm + signature + flags).
    /// </summary>
    /// <param name="cbor">CBOR-encoded map with keys {3: alg, 6: sig, 4: flags}.</param>
    /// <returns>
    /// A <see cref="PreviewSignRegistrationOutput"/> if decoding succeeds; otherwise null.
    /// </returns>
    /// <remarks>
    /// This is the SIGNED variant of registration output. Per CTAP v4 draft §4, the
    /// unsigned variant (att-obj) is preferred as the authoritative source.
    /// This method is provided for completeness but should be considered a fallback.
    /// </remarks>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when CBOR is malformed (InvalidState).
    /// </exception>
    public static PreviewSignRegistrationOutput? DecodeSignedRegistrationOutput(ReadOnlyMemory<byte> cbor)
    {
        try
        {
            var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
            int? mapSize = reader.ReadStartMap();

            CoseAlgorithm? algorithm = null;
            ReadOnlyMemory<byte>? signature = null;
            PreviewSignFlags? flags = null;

            for (int i = 0; i < mapSize; i++)
            {
                int key = reader.ReadInt32();
                switch (key)
                {
                    case Algorithm:
                        algorithm = new CoseAlgorithm(reader.ReadInt32());
                        break;
                    case Signature:
                        signature = reader.ReadByteString();
                        break;
                    case Flags:
                        flags = (PreviewSignFlags)reader.ReadInt32();
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }

            reader.ReadEndMap();

            // For signed output, we don't have the full GeneratedSigningKey structure
            // This variant is less trusted per spec §4
            // Return null to indicate we should prefer the unsigned att-obj variant
            return null;
        }
        catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign output is malformed",
                ex);
        }
    }

    /// <summary>
    /// Decodes unsigned registration output (nested attestation object).
    /// </summary>
    /// <param name="cbor">CBOR-encoded map with key {7: att-obj (nested CBOR)}.</param>
    /// <returns>
    /// A <see cref="PreviewSignRegistrationOutput"/> with the decoded attestation object.
    /// </returns>
    /// <remarks>
    /// Per CTAP v4 draft §4, this is the PREFERRED output format. The attestation object
    /// contains the authoritative public key, key handle (as credentialId), and flags
    /// embedded in the authenticator data extensions.
    /// </remarks>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when CBOR is malformed or the nested attestation object cannot be decoded (InvalidState).
    /// </exception>
    public static PreviewSignRegistrationOutput DecodeUnsignedRegistrationOutput(ReadOnlyMemory<byte> cbor)
    {
        try
        {
            var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
            int? mapSize = reader.ReadStartMap();

            ReadOnlyMemory<byte>? attestationObjectBytes = null;

            for (int i = 0; i < mapSize; i++)
            {
                int key = reader.ReadInt32();
                if (key == AttestationObject)
                {
                    attestationObjectBytes = reader.ReadByteString();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (!attestationObjectBytes.HasValue)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "previewSign unsigned output missing attestation object (key 7)");
            }

            // Decode the nested attestation object
            var attestationObject = WebAuthnAttestationObject.Decode(attestationObjectBytes.Value);

            // Extract the embedded previewSign extension output from authenticator data
            var extensions = attestationObject.AuthenticatorData.ParsedExtensions;
            if (!extensions.TryGetValue("previewSign", out var previewSignExtBytes))
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "previewSign attestation object missing embedded previewSign extension");
            }

            // Decode the embedded extension to extract flags
            var extReader = new CborReader(previewSignExtBytes, CborConformanceMode.Ctap2Canonical);
            int? extMapSize = extReader.ReadStartMap();

            PreviewSignFlags? flags = null;
            CoseAlgorithm? algorithm = null;

            for (int i = 0; i < extMapSize; i++)
            {
                int key = extReader.ReadInt32();
                switch (key)
                {
                    case Flags:
                        flags = (PreviewSignFlags)extReader.ReadInt32();
                        break;
                    case Algorithm:
                        algorithm = new CoseAlgorithm(extReader.ReadInt32());
                        break;
                    default:
                        extReader.SkipValue();
                        break;
                }
            }

            extReader.ReadEndMap();

            if (!flags.HasValue)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "previewSign embedded extension missing flags (key 4)");
            }

            if (!algorithm.HasValue)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "previewSign embedded extension missing algorithm (key 3)");
            }

            // Extract key handle from attestation object's attested credential data
            var attestedCredData = attestationObject.AuthenticatorData.AttestedCredentialData;
            if (attestedCredData is null)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "previewSign attestation object missing attested credential data");
            }

            ReadOnlyMemory<byte> keyHandle = attestedCredData.CredentialId;

            // Decode the public key from attested credential data
            CoseKey publicKey = CoseKey.Decode(attestedCredData.CredentialPublicKey);

            var generatedKey = new GeneratedSigningKey(
                KeyHandle: keyHandle,
                PublicKey: publicKey,
                Algorithm: algorithm.Value,
                AttestationObject: attestationObject,
                Flags: flags.Value);

            return new PreviewSignRegistrationOutput(generatedKey);
        }
        catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign unsigned output is malformed",
                ex);
        }
    }

    /// <summary>
    /// Decodes authentication output (signature bytes).
    /// </summary>
    /// <param name="cbor">CBOR-encoded map with key {6: sig}.</param>
    /// <returns>
    /// A <see cref="PreviewSignAuthenticationOutput"/> with the signature bytes.
    /// </returns>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when CBOR is malformed or signature is missing (InvalidState).
    /// </exception>
    public static PreviewSignAuthenticationOutput DecodeAuthenticationOutput(ReadOnlyMemory<byte> cbor)
    {
        try
        {
            var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
            int? mapSize = reader.ReadStartMap();

            ReadOnlyMemory<byte>? signature = null;

            for (int i = 0; i < mapSize; i++)
            {
                int key = reader.ReadInt32();
                if (key == Signature)
                {
                    signature = reader.ReadByteString();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (!signature.HasValue)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "previewSign authentication output missing signature (key 6)");
            }

            return new PreviewSignAuthenticationOutput(signature.Value);
        }
        catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign authentication output is malformed",
                ex);
        }
    }
}
