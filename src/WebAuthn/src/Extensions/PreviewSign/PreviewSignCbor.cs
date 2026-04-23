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
/// CBOR decoding for previewSign extension output (WebAuthn-specific types).
/// </summary>
/// <remarks>
/// <para>
/// Encoding logic lives in Yubico.YubiKit.Fido2.Extensions.PreviewSignCbor.
/// This file contains only WebAuthn-level decoding that works with WebAuthn-specific
/// types like WebAuthnAttestationObject and GeneratedSigningKey.
/// </para>
/// </remarks>
internal static class PreviewSignCbor
{
    /// <summary>
    /// CBOR keys for registration output (unsigned extension outputs).
    /// </summary>
    private static class RegistrationOutputKeys
    {
        internal const int AttestationObject = 7;
    }

    /// <summary>
    /// CBOR keys for authentication output.
    /// </summary>
    private static class AuthenticationOutputKeys
    {
        internal const int Signature = 6;
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
                if (key == RegistrationOutputKeys.AttestationObject)
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
        catch (WebAuthnClientError)
        {
            // Already typed WebAuthnClientError - propagate as-is
            throw;
        }
        catch (CborContentException ex)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign nested attestation CBOR is malformed",
                ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidState,
                "previewSign nested attestation parse failed",
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
                if (key == AuthenticationOutputKeys.Signature)
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
