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
    /// Flag bits encoded in the MakeCredential previewSign extension input (key 4).
    /// </summary>
    public enum PreviewSignOptions
    {
        /// <summary>Require user presence for signing operations.</summary>
        RequireUserPresence = 0b001,

        /// <summary>Require user verification (PIN/biometric) for signing operations.</summary>
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
    internal static class PreviewSignExtension
    {
        // Cross-ref: yubikit-swift, python-fido2, Rust, JS.
        /// <summary>CTAP key on the MakeCredential RESPONSE map carrying unsigned extension outputs.</summary>
        internal const int CtapUnsignedExtensionOutputsKey = 6;
        private const int CoseKeyTypeArkgP256 = -65537;
        private const int CoseAlgorithmArkgP256 = -65700;

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
        /// <param name="flags">The flags value to encode (e.g., <see cref="PreviewSignOptions.RequireUserPresence"/> or <see cref="PreviewSignOptions.RequireUserVerification"/>).</param>
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
        /// Encode ARKG sign args for the previewSign extension.
        /// </summary>
        /// <param name="arkgKeyHandle">The ARKG key handle.</param>
        /// <param name="context">The context string used during key derivation.</param>
        /// <returns>CBOR-encoded COSE_Sign_Args map {3: alg, -1: arkg_kh, -2: ctx}.</returns>
        /// <remarks>
        /// The alg field identifies the SIGN-ARGS request as ARKG-derived (-65539), not the
        /// raw signing algorithm. Rust hid-test, python-fido2, and the JS test page all pass
        /// -65539 here; firmware rejects other values.
        /// </remarks>
        public static byte[] EncodeArkgSignArgs(
            ReadOnlyMemory<byte> arkgKeyHandle,
            ReadOnlyMemory<byte> context)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(3);

            // Cross-ref: python-fido2 cose.py:391 (-65539), Rust hid-test main.rs:28, JS test-page index.html:555
            cbor.WriteInt32(3);
            cbor.WriteInt32((int)Cose.CoseAlgorithmIdentifier.ArkgP256Esp256);

            cbor.WriteInt32(-1);
            cbor.WriteByteString(arkgKeyHandle.Span);

            cbor.WriteInt32(-2);
            cbor.WriteByteString(context.Span);

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
            ReadOnlyMemory<byte>? additionalArgs)
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
        /// Decode the previewSign generated-key payload from an unsigned-extension
        /// output value. Per yubikit-swift, the outer map is
        /// <c>{ 3: alg, 7: nested_attestation_object_bytes }</c> and the nested
        /// attestation object is <c>{ 1: fmt, 2: authData, 3: attStmt }</c>.
        /// We extract the algorithm and key handle from the outer map plus the
        /// inner authData, and the COSE-encoded blinding/KEM public keys from
        /// the inner authData's credentialPublicKey field.
        /// </summary>
        /// <param name="previewSignValue">The CBOR-encoded previewSign generated-key payload.</param>
        /// <returns>
        /// A <see cref="PreviewSignGeneratedKey"/> if the payload contains ARKG-P256
        /// generated key material; otherwise, <c>null</c>.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The payload declares an unsupported generated-key algorithm, or contains
        /// malformed ARKG-P256 COSE key material.
        /// </exception>
        public static PreviewSignGeneratedKey? DecodeGeneratedKey(ReadOnlyMemory<byte> previewSignValue) =>
            DecodeGeneratedKey(previewSignValue, fallbackAlgorithm: null);

        /// <summary>
        /// Decode the generated-key algorithm from a previewSign output value.
        /// </summary>
        /// <param name="previewSignValue">The CBOR-encoded previewSign output value.</param>
        /// <returns>
        /// The decoded <see cref="CoseAlgorithmIdentifier"/> if the input is a map
        /// containing key <see cref="MakeCredentialKey.Algorithm"/>; otherwise,
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
        /// The algorithm to use when <paramref name="previewSignValue"/> omits key
        /// <see cref="MakeCredentialKey.Algorithm"/>; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// A <see cref="PreviewSignGeneratedKey"/> if the payload contains ARKG-P256
        /// generated key material; otherwise, <c>null</c>.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The payload declares an unsupported generated-key algorithm, or contains
        /// malformed ARKG-P256 COSE key material.
        /// </exception>
        public static PreviewSignGeneratedKey? DecodeGeneratedKey(
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

            if (attestationObject is null)
            {
                return null;
            }

            if (algorithm != CoseAlgorithmIdentifier.ArkgP256Esp256)
            {
                throw new Ctap2DataException(
                    "previewSign generated key uses an unsupported algorithm.");
            }

            var attestationObj = new AttestationObject(attestationObject, parseFullDetails: false);
            if (attestationObj.AuthenticatorData.CredentialId is null ||
                attestationObj.AuthenticatorData.EncodedCredentialPublicKey is null)
            {
                return null;
            }

            byte[] keyHandle = attestationObj.AuthenticatorData.CredentialId.Id.ToArray();

            // Validate ARKG-P256 COSE structure before exposing the key.
            // We discard the parsed halves; the public PublicKey is the raw CBOR blob.
            _ = ParseArkgCoseKey(attestationObj.AuthenticatorData.EncodedCredentialPublicKey.Value.ToArray());

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

        /// <summary>
        /// Parse the ARKG-P256 COSE key:
        ///   { 1: kty, 3: alg, -1: blindingPublicKey_cose_ec2, -2: kemPublicKey_cose_ec2 }
        /// where each EC2 sub-map is {1:2, -1:1, -2:x, -3:y} and can include
        /// an algorithm identifier chosen by the authenticator.
        /// </summary>
        internal static (byte[] blindingPublicKey, byte[] kemPublicKey) ParseArkgCoseKey(byte[] coseEncoded)
        {
            var reader = new CborReader(coseEncoded, CborConformanceMode.Ctap2Canonical);
            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;

            bool? isEc2Key = null;
            bool? isArkgP256Key = null;
            byte[]? blindingPublicKey = null;
            byte[]? kemPublicKey = null;

            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                long key = reader.ReadInt64();
                if (key == 1)
                {
                    int keyType = reader.ReadInt32();
                    isEc2Key = keyType == (int)CoseKeyType.Ec2 || keyType == CoseKeyTypeArkgP256;
                }
                else if (key == 3)
                {
                    int algorithm = reader.ReadInt32();
                    isArkgP256Key =
                        algorithm == (int)CoseAlgorithmIdentifier.ArkgP256Esp256 ||
                        algorithm == CoseAlgorithmArkgP256;
                }
                else if (key == -1)
                {
                    blindingPublicKey = ReadEc2PointAsSec1(reader);
                }
                else if (key == -2)
                {
                    kemPublicKey = ReadEc2PointAsSec1(reader);
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (isEc2Key == false || isArkgP256Key == false)
            {
                throw new Ctap2DataException(
                    "previewSign COSE key must be an EC2 ARKG-P256 key.");
            }

            if (blindingPublicKey is null || kemPublicKey is null)
            {
                throw new Ctap2DataException(
                    "previewSign COSE key missing blindingPublicKey (-1) or kemPublicKey (-2).");
            }

            return (blindingPublicKey, kemPublicKey);
        }

        private static byte[] ReadEc2PointAsSec1(CborReader reader)
        {
            int? subEntries = reader.ReadStartMap();
            int subCount = subEntries ?? int.MaxValue;

            bool? isEc2Key = null;
            bool? isP256Curve = null;
            byte[]? x = null;
            byte[]? y = null;
            for (int j = 0; j < subCount; j++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                long subKey = reader.ReadInt64();
                if (subKey == 1)
                {
                    isEc2Key = reader.ReadInt32() == (int)CoseKeyType.Ec2;
                }
                else if (subKey == 3)
                {
                    reader.SkipValue();
                }
                else if (subKey == -1)
                {
                    isP256Curve = reader.ReadInt32() == (int)CoseEcCurve.P256;
                }
                else if (subKey == -2)
                {
                    x = reader.ReadByteString();
                }
                else if (subKey == -3)
                {
                    y = reader.ReadByteString();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (isEc2Key == false || isP256Curve == false)
            {
                throw new Ctap2DataException(
                    "previewSign ARKG public-key components must be EC2 P-256 keys.");
            }

            if (x is null || y is null || x.Length != 32 || y.Length != 32)
            {
                throw new Ctap2DataException(
                    "previewSign EC2 point coordinates must be 32 bytes each.");
            }

            byte[] sec1 = new byte[65];
            sec1[0] = 0x04;
            Buffer.BlockCopy(x, 0, sec1, 1, 32);
            Buffer.BlockCopy(y, 0, sec1, 33, 32);
            return sec1;
        }
    }
}
