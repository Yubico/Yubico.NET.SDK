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
    /// Decodes unsigned registration output (nested attestation object).
    /// </summary>
    /// <param name="cbor">CBOR-encoded map with key {7: att-obj (nested CBOR)}.</param>
    /// <param name="algorithm">Algorithm already read from authData.extensions["previewSign"].</param>
    /// <param name="flags">Flags already read from authData.extensions["previewSign"].</param>
    /// <returns>
    /// A <see cref="PreviewSignRegistrationOutput"/> with the decoded attestation object.
    /// </returns>
    /// <remarks>
    /// Per spec §10.2.1 step 5, algorithm comes from authData.extensions["previewSign"][alg],
    /// while the attestation object comes from unsignedExtensionOutputs["previewSign"][att-obj].
    /// This method decodes the attestation object from unsignedExtensionOutputs.
    /// </remarks>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when CBOR is malformed or the nested attestation object cannot be decoded (InvalidState).
    /// </exception>
    public static PreviewSignRegistrationOutput DecodeUnsignedRegistrationOutput(
        ReadOnlyMemory<byte> cbor,
        CoseAlgorithm algorithm,
        PreviewSignFlags flags)
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

            // Extract key handle and public key from attested credential data
            var attestedCredData = attestationObject.AuthenticatorData.AttestedCredentialData;
            if (attestedCredData is null)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidState,
                    "previewSign attestation object missing attested credential data");
            }

            ReadOnlyMemory<byte> keyHandle = attestedCredData.CredentialId;
            CoseKey publicKey = CoseKey.Decode(attestedCredData.CredentialPublicKey);

            var generatedKey = new GeneratedSigningKey(
                KeyHandle: keyHandle,
                PublicKey: publicKey,
                Algorithm: algorithm,
                AttestationObject: attestationObject);

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
