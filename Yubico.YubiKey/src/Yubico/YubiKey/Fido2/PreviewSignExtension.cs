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
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
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
        /// <summary>CTAP key on the MakeCredential RESPONSE map carrying unsigned extension outputs.</summary>
        internal const int CtapUnsignedExtensionOutputsKey = 6;

        /// <summary>Flag bits encoded in the MakeCredential previewSign input (key 4).</summary>
        internal const int FlagsRequireUserPresence = 0b001;
        internal const int FlagsRequireUserVerification = 0b101;

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
        public static byte[] EncodeGenerateKeyInput(
            ReadOnlySpan<CoseAlgorithmIdentifier> algorithms,
            bool requireUv)
        {
            int flags = requireUv ? FlagsRequireUserVerification : FlagsRequireUserPresence;

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
            cbor.WriteInt32(flags);

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        /// <summary>
        /// Encode the GetAssertion extension input as a flat map.
        /// {2:keyHandle, 6:tbs, 7:additionalArgs?}.
        /// </summary>
        public static byte[] EncodeSignByCredentialInput(
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
        public static PreviewSignGeneratedKey? DecodeGeneratedKey(ReadOnlyMemory<byte> previewSignValue)
        {
            var reader = new CborReader(previewSignValue, CborConformanceMode.Ctap2Canonical);
            if (reader.PeekState() != CborReaderState.StartMap)
            {
                return null;
            }

            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;

            CoseAlgorithmIdentifier alg = CoseAlgorithmIdentifier.None;
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
                    alg = (CoseAlgorithmIdentifier)reader.ReadInt32();
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

            if (attestationObject is null)
            {
                return null;
            }

            (byte[] keyHandle, byte[] pkBl, byte[] pkKem) = ParseInnerAttestationObject(attestationObject);
            return new PreviewSignGeneratedKey(
                keyHandle,
                pkBl,
                pkKem,
                alg);
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
        /// Inner attestation object decoder. Extracts the credential ID
        /// (key handle) from authData and the ARKG public seed (pkBl, pkKem)
        /// from the credentialPublicKey COSE map.
        /// </summary>
        private static (byte[] keyHandle, byte[] pkBl, byte[] pkKem) ParseInnerAttestationObject(byte[] encoded)
        {
            var reader = new CborReader(encoded, CborConformanceMode.Ctap2Canonical);
            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;

            byte[]? authData = null;
            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                int key = (int)reader.ReadInt64();
                if (key == 2)
                {
                    authData = reader.ReadByteString();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (authData is null)
            {
                throw new System.Security.Cryptography.CryptographicException(
                    "previewSign attestation object missing authData (key 2).");
            }

            return ParseAuthDataForArkgSeed(authData);
        }

        /// <summary>
        /// Parse a CTAP authenticator-data buffer and return (credentialId,
        /// pkBl, pkKem) extracted from the attested credential data + COSE key.
        /// </summary>
        private static (byte[] keyHandle, byte[] pkBl, byte[] pkKem) ParseAuthDataForArkgSeed(byte[] authData)
        {
            // CTAP authenticator-data layout:
            //   [0..32)   rpIdHash
            //   [32]      flags
            //   [33..37)  signCount (big-endian uint32)
            //   [37..53)  AAGUID (only present when AT flag is set)
            //   [53..55)  credentialIdLength (big-endian uint16)
            //   [55..55+L) credentialId
            //   [55+L..)  credentialPublicKey (CBOR)
            //   ...
            const int FixedHeaderLength = 37;
            const int AaguidLength = 16;
            const byte AttestedCredentialDataFlag = 0x40;

            if (authData.Length < FixedHeaderLength + AaguidLength + 2)
            {
                throw new System.Security.Cryptography.CryptographicException(
                    "previewSign authData too short for attested credential data.");
            }

            byte flags = authData[32];
            if ((flags & AttestedCredentialDataFlag) == 0)
            {
                throw new System.Security.Cryptography.CryptographicException(
                    "previewSign authData missing AT flag.");
            }

            int credIdLengthOffset = FixedHeaderLength + AaguidLength;
            int credIdLength = (authData[credIdLengthOffset] << 8) | authData[credIdLengthOffset + 1];
            int credIdOffset = credIdLengthOffset + 2;
            if (authData.Length < credIdOffset + credIdLength)
            {
                throw new System.Security.Cryptography.CryptographicException(
                    "previewSign authData truncated in credentialId.");
            }

            byte[] credentialId = new byte[credIdLength];
            Buffer.BlockCopy(authData, credIdOffset, credentialId, 0, credIdLength);

            int coseOffset = credIdOffset + credIdLength;
            byte[] coseSlice = new byte[authData.Length - coseOffset];
            Buffer.BlockCopy(authData, coseOffset, coseSlice, 0, coseSlice.Length);

            (byte[] pkBl, byte[] pkKem) = ParseArkgCoseKey(coseSlice);
            return (credentialId, pkBl, pkKem);
        }

        /// <summary>
        /// Parse the ARKG-P256 COSE key:
        ///   { 1: kty, 3: alg, -1: pkBl_cose_ec2, -2: pkKem_cose_ec2 }
        /// where each EC2 sub-map is {1:2, 3:-9, -1:1, -2:x, -3:y}.
        /// </summary>
        private static (byte[] pkBl, byte[] pkKem) ParseArkgCoseKey(byte[] coseEncoded)
        {
            var reader = new CborReader(coseEncoded, CborConformanceMode.Ctap2Canonical);
            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;

            byte[]? pkBl = null;
            byte[]? pkKem = null;

            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                long key = reader.ReadInt64();
                if (key == -1)
                {
                    pkBl = ReadEc2PointAsSec1(reader);
                }
                else if (key == -2)
                {
                    pkKem = ReadEc2PointAsSec1(reader);
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (pkBl is null || pkKem is null)
            {
                throw new System.Security.Cryptography.CryptographicException(
                    "previewSign COSE key missing pkBl (-1) or pkKem (-2).");
            }

            return (pkBl, pkKem);
        }

        private static byte[] ReadEc2PointAsSec1(CborReader reader)
        {
            int? subEntries = reader.ReadStartMap();
            int subCount = subEntries ?? int.MaxValue;

            byte[]? x = null;
            byte[]? y = null;
            for (int j = 0; j < subCount; j++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                long subKey = reader.ReadInt64();
                if (subKey == -2)
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

            if (x is null || y is null || x.Length != 32 || y.Length != 32)
            {
                throw new System.Security.Cryptography.CryptographicException(
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
