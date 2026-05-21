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

using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Flag bits encoded in the MakeCredential previewSign extension input.
    /// </summary>
    public enum PreviewSignOptions
    {
        /// <summary>Require user presence for signing operations.</summary>
        RequireUserPresence = 0b001,

        /// <summary>Require user verification for signing operations.</summary>
        RequireUserVerification = 0b101,
    }

    /// <summary>
    /// CBOR encoder/decoder for the "previewSign" WebAuthn extension.
    /// </summary>
    /// <remarks>
    /// Wire format follows yubikit-swift release/1.3.0. The same integer key
    /// can mean different things depending on whether it appears in a
    /// MakeCredential extension input or a GetAssertion extension input —
    /// per-context enums (<see cref="MakeCredentialKey"/>,
    /// <see cref="GetAssertionKey"/>) keep the call sites readable.
    /// </remarks>
    public static class PreviewSignExtension
    {
        /// <summary>CTAP key on the MakeCredential RESPONSE map carrying unsigned extension outputs.</summary>
        internal const int CtapUnsignedExtensionOutputsKey = 6;
        internal const string ExtensionName = "previewSign";

        // Cross-ref: yubikit-swift release/1.3.0 PreviewSign.swift, python-fido2 extensions.py:670-810
        /// <summary>Map keys used by the MakeCredential previewSign input AND signed output.</summary>
        internal enum MakeCredentialKey
        {
            Algorithm = 3,
            Flags = 4,
            AttestationObject = 7,
        }

        /// <summary>Map keys used by the GetAssertion previewSign input and output.</summary>
        internal enum GetAssertionKey
        {
            KeyHandle = 2,
            TbsOrSignature = 6,
            AdditionalArgs = 7,
        }

        /// <summary>
        /// Encode the MakeCredential extension input map: {3:[algs], 4:flags}.
        /// </summary>
        /// <param name="algorithms">The algorithms to include in the input map.</param>
        /// <param name="flags">The flags value to encode.</param>
        /// <returns>The CBOR-encoded extension input.</returns>
        public static byte[] EncodeGenerateKeyInput(
            ReadOnlySpan<CoseAlgorithmIdentifier> algorithms,
            PreviewSignOptions flags)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(2);

            cbor.WriteInt32((int)MakeCredentialKey.Algorithm);
            cbor.WriteStartArray(algorithms.Length);
            for (int i = 0; i < algorithms.Length; i++)
            {
                cbor.WriteInt32((int)algorithms[i]);
            }

            cbor.WriteEndArray();

            cbor.WriteInt32((int)MakeCredentialKey.Flags);
            cbor.WriteInt32((int)flags);

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        /// <summary>
        /// Encode the GetAssertion extension input as a flat map.
        /// {2:keyHandle, 6:tbs, 7:additionalArgs?}.
        /// </summary>
        public static byte[] EncodeSignInput(
            ReadOnlyMemory<byte> keyHandle,
            ReadOnlyMemory<byte> toBeSigned,
            ReadOnlyMemory<byte>? additionalArgs = null)
        {
            int entries = additionalArgs.HasValue ? 3 : 2;

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(entries);

            cbor.WriteInt32((int)GetAssertionKey.KeyHandle);
            cbor.WriteByteString(keyHandle.Span);

            cbor.WriteInt32((int)GetAssertionKey.TbsOrSignature);
            cbor.WriteByteString(toBeSigned.Span);

            if (additionalArgs.HasValue)
            {
                cbor.WriteInt32((int)GetAssertionKey.AdditionalArgs);
                cbor.WriteByteString(additionalArgs.Value.Span);
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        /// <summary>
        /// Parse the unsigned extension outputs map from the MakeCredential
        /// response (CTAP key 6). Returns name → encoded-value pairs.
        /// </summary>
        public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ParseUnsignedExtensionOutputs(
            ReadOnlyMemory<byte> encodedMap)
        {
            var result = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
            var reader = new CborReader(encodedMap, CborConformanceMode.Ctap2Canonical);
            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                string name = reader.ReadTextString();
                byte[] value = reader.ReadEncodedValue().ToArray();
                result[name] = value;
            }

            reader.ReadEndMap();
            return result;
        }

        /// <summary>
        /// Decode the previewSign generated-key payload from an extension output value.
        /// </summary>
        /// <param name="previewSignValue">The CBOR-encoded previewSign generated-key payload.</param>
        /// <returns>
        /// A generated key container if the payload contains generated key material;
        /// otherwise, <c>null</c>.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The payload contains malformed generated-key material.
        /// </exception>
        public static PreviewSignGeneratedKey? DecodeGeneratedKey(ReadOnlyMemory<byte> previewSignValue) =>
            DecodeGeneratedKey(previewSignValue, fallbackAlgorithm: null);

        /// <summary>
        /// Decode the generated-key algorithm from a previewSign output value.
        /// </summary>
        /// <param name="previewSignValue">The CBOR-encoded previewSign output value.</param>
        /// <returns>
        /// The decoded <see cref="CoseAlgorithmIdentifier"/> if the input is a map
        /// containing the algorithm key; otherwise,
        /// <c>null</c> when the input is not a map or the algorithm key is absent.
        /// </returns>
        public static CoseAlgorithmIdentifier? DecodeGeneratedKeyAlgorithm(ReadOnlyMemory<byte> previewSignValue)
        {
            var reader = new CborReader(previewSignValue, CborConformanceMode.Ctap2Canonical);
            if (reader.PeekState() != CborReaderState.StartMap)
            {
                return null;
            }

            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                int key = (int)reader.ReadInt64();
                if (key == (int)MakeCredentialKey.Algorithm)
                {
                    return (CoseAlgorithmIdentifier)reader.ReadInt32();
                }

                reader.SkipValue();
            }

            reader.ReadEndMap();
            return null;
        }

        /// <summary>
        /// Decode the previewSign generated-key payload using a fallback algorithm
        /// when the payload omits the algorithm key.
        /// </summary>
        /// <param name="previewSignValue">The CBOR-encoded previewSign generated-key payload.</param>
        /// <param name="fallbackAlgorithm">
        /// The algorithm to use when <paramref name="previewSignValue"/> omits the
        /// algorithm key; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// A generated key container if the payload contains generated key material;
        /// otherwise, <c>null</c>.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The payload contains malformed generated-key material.
        /// </exception>
        internal static PreviewSignGeneratedKey? DecodeGeneratedKey(
            ReadOnlyMemory<byte> previewSignValue,
            CoseAlgorithmIdentifier? fallbackAlgorithm)
        {
            var reader = new CborReader(previewSignValue, CborConformanceMode.Ctap2Canonical);
            if (reader.PeekState() != CborReaderState.StartMap)
            {
                return null;
            }

            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;

            var algorithm = CoseAlgorithmIdentifier.None;
            byte[]? attestationObject = null;

            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                int key = (int)reader.ReadInt64();
                if (key == (int)MakeCredentialKey.Algorithm)
                {
                    algorithm = (CoseAlgorithmIdentifier)reader.ReadInt32();
                }
                else if (key == (int)MakeCredentialKey.AttestationObject)
                {
                    attestationObject = reader.ReadByteString();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (algorithm == CoseAlgorithmIdentifier.None && fallbackAlgorithm.HasValue)
            {
                algorithm = fallbackAlgorithm.Value;
            }

            if (algorithm == CoseAlgorithmIdentifier.None)
            {
                throw new Ctap2DataException(
                    "previewSign generated key is missing an algorithm.");
            }

            if (attestationObject is null)
            {
                return null;
            }

            var attestationObj = new AttestationObject(attestationObject, parseFullDetails: false);
            if (attestationObj.AuthenticatorData.CredentialId is null ||
                attestationObj.AuthenticatorData.EncodedCredentialPublicKey is null)
            {
                return null;
            }

            byte[] keyHandle = attestationObj.AuthenticatorData.CredentialId.Id.ToArray();

            return new PreviewSignGeneratedKey(
                keyHandle,
                attestationObj.AuthenticatorData.EncodedCredentialPublicKey.Value,
                algorithm,
                attestationObj);
        }

        /// <summary>
        /// Parse the signed previewSign extension output produced by the
        /// authenticator after a GetAssertion (key 6 inside
        /// authData.extensions["previewSign"]). The value is a CBOR map
        /// containing a single byte-string entry whose value is the DER-encoded
        /// ECDSA signature.
        /// </summary>
        public static byte[]? DecodeSignature(ReadOnlyMemory<byte> previewSignAuthDataValue)
        {
            var reader = new CborReader(previewSignAuthDataValue, CborConformanceMode.Ctap2Canonical);
            if (reader.PeekState() != CborReaderState.StartMap)
            {
                return null;
            }

            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;

            byte[]? signature = null;
            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                int key = (int)reader.ReadInt64();
                if (key == (int)GetAssertionKey.TbsOrSignature)
                {
                    signature = reader.ReadByteString();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();
            return signature;
        }
    }
}
