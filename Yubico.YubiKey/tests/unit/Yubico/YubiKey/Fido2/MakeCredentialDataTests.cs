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
using System.Formats.Cbor;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    public class MakeCredentialDataTests
    {
        /// <summary>
        /// AttestationObject.CborEncode() emits the standalone {1,2,3}
        /// attestation object bytes, not the full CTAP response. The response
        /// includes optional keys 4, 5, and 6 to verify that the AttestationObject
        /// only contains keys 1, 2, and 3.
        /// </summary>
        [Fact]
        public void AttestationObject_CborEncode_OnlyContainsKeys123_NotFullRawData()
        {
            byte[] ctapResponse = BuildMakeCredentialResponseWithOptionalKeys();

            var data = new MakeCredentialData(ctapResponse);

            // Re-encode the AttestationObject
            byte[] attestationEncoded = data.AttestationObject.CborEncode();

            // Parse the re-encoded bytes and verify it ONLY has keys 1, 2, 3
            var reader = new CborReader(attestationEncoded, CborConformanceMode.Ctap2Canonical);
            int? count = reader.ReadStartMap();
            Assert.Equal(3, count);

            var keysFound = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < 3; i++)
            {
                int key = reader.ReadInt32();
                keysFound.Add(key);
                reader.SkipValue();
            }
            reader.ReadEndMap();

            Assert.Contains(1, keysFound);  // fmt
            Assert.Contains(2, keysFound);  // authData
            Assert.Contains(3, keysFound);  // attStmt
            Assert.Equal(3, keysFound.Count);

            // Verify the encoded bytes are NOT equal to the original RawData
            // (because RawData has optional keys, but AttestationObject should only
            // have 1, 2, and 3)
            Assert.NotEqual(ctapResponse, attestationEncoded);
            Assert.Equal(attestationEncoded, data.AttestationObject.Encoded.ToArray());

            Assert.True(data.EnterpriseAttestation.HasValue);
            Assert.True(data.EnterpriseAttestation.Value);
            Assert.True(data.LargeBlobKey.HasValue);
            Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C }, data.LargeBlobKey.Value.ToArray());
            Assert.NotNull(data.UnsignedExtensionOutputs);

            ReadOnlyMemory<byte> originalEncodedAttStmt = ReadEncodedMapValue(ctapResponse, 3);
            ReadOnlyMemory<byte> encodedAttStmt = ReadEncodedMapValue(attestationEncoded, 3);

            Assert.True(data.AttestationObject.EncodedAttestationStatement.Span.SequenceEqual(originalEncodedAttStmt.Span),
                "EncodedAttestationStatement must be byte-identical to key 3 value from the CTAP response.");
            Assert.True(data.AttestationObject.EncodedAttestationStatement.Span.SequenceEqual(encodedAttStmt.Span),
                "EncodedAttestationStatement must be byte-identical to key 3 value from AttestationObject.CborEncode().");
        }

        /// <summary>
        /// Constructing MakeCredentialData from a CTAP response whose attStmt has
        /// format=="packed" but is missing the required 'sig' field throws Ctap2DataException.
        /// </summary>
        [Fact]
        public void MakeCredentialData_PackedAttestationMissingSig_ThrowsCtap2DataException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponseWithMalformedPackedAttestation(missingSig: true, extraKey: false);

            var ex = Assert.Throws<Ctap2DataException>(() => new MakeCredentialData(ctapResponse));

            Assert.NotNull(ex.Message);
        }

        /// <summary>
        /// Constructing MakeCredentialData from a CTAP response whose attStmt has
        /// format=="packed" but has an unexpected extra key throws Ctap2DataException.
        /// </summary>
        [Fact]
        public void MakeCredentialData_PackedAttestationWithExtraKey_ThrowsCtap2DataException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponseWithMalformedPackedAttestation(missingSig: false, extraKey: true);

            var ex = Assert.Throws<Ctap2DataException>(() => new MakeCredentialData(ctapResponse));

            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void MakeCredentialData_CapturedCertificateBackedPackedAttestation_PopulatesFieldsCorrectly()
        {
            byte[] ctapResponse = CredentialDataTests.GetSampleEncoding();

            var data = new MakeCredentialData(ctapResponse);

            Assert.Equal("packed", data.Format);
            Assert.Equal(Cose.CoseAlgorithmIdentifier.ES256, data.AttestationAlgorithm);
            Assert.False(data.AttestationStatement.IsEmpty);
            Assert.False(data.EncodedAttestationStatement.IsEmpty);
            Assert.Equal(3, CountEncodedMapEntries(data.EncodedAttestationStatement));
            Assert.NotNull(data.AttestationCertificates);
            X509Certificate2 certificate = Assert.Single(data.AttestationCertificates!);
            Assert.Contains("Yubico", certificate.Subject, StringComparison.Ordinal);
            Assert.Equal(180, data.AuthenticatorData.EncodedAuthenticatorData.Length);
            Assert.True(data.AuthenticatorData.UserPresence);
            Assert.True(data.AuthenticatorData.UserVerification);
            Assert.NotNull(data.AuthenticatorData.CredentialId);
            Assert.Equal(48, data.AuthenticatorData.CredentialId!.Id.Length);
            Assert.NotNull(data.AuthenticatorData.CredentialPublicKey);
            Assert.NotNull(data.AuthenticatorData.EncodedCredentialPublicKey);
        }

        /// <summary>
        /// Response missing key 3 (attStmt) throws Ctap2DataException.
        /// </summary>
        [Fact]
        public void MakeCredentialData_ResponseMissingAttStmt_ThrowsCtap2DataException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponseMissingKey(missingKey: 3);

            var ex = Assert.Throws<Ctap2DataException>(() => new MakeCredentialData(ctapResponse));
            Assert.NotNull(ex.Message);
        }

        /// <summary>
        /// Response missing key 1 (fmt) throws Ctap2DataException.
        /// </summary>
        [Fact]
        public void MakeCredentialData_ResponseMissingFmt_ThrowsCtap2DataException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponseMissingKey(missingKey: 1);

            var ex = Assert.Throws<Ctap2DataException>(() => new MakeCredentialData(ctapResponse));
            Assert.NotNull(ex.Message);
        }

        /// <summary>
        /// Response missing key 2 (authData) throws Ctap2DataException.
        /// </summary>
        [Fact]
        public void MakeCredentialData_ResponseMissingAuthData_ThrowsCtap2DataException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponseMissingKey(missingKey: 2);

            var ex = Assert.Throws<Ctap2DataException>(() => new MakeCredentialData(ctapResponse));
            Assert.NotNull(ex.Message);
        }

        /// <summary>
        /// AttestationObject with packed attStmt where 'alg' is
        /// a text string instead of int throws Ctap2DataException (wrapping InvalidOperationException).
        /// </summary>
        [Fact]
        public void AttestationObject_PackedAttStmtAlgWrongCborType_ThrowsCtap2DataException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponseWithWrongAlgType();

            var ex = Assert.Throws<Ctap2DataException>(() => new MakeCredentialData(ctapResponse));
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void MakeCredentialData_PackedAttestationMissingAlg_ThrowsCtap2DataException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponse(
                BuildMinimalAuthDataWithEs256Key(),
                cbor => WritePackedAttestationStatement(cbor, includeAlgorithm: false));

            var ex = Assert.Throws<Ctap2DataException>(() => new MakeCredentialData(ctapResponse));
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void MakeCredentialData_OkpEdDsaCredentialPublicKey_PopulatesRichKey()
        {
            byte[] coseKey = BuildOkpEdDsaCoseKey();
            byte[] ctapResponse = BuildMakeCredentialResponse(
                BuildMinimalAuthDataWithCredentialPublicKey(coseKey),
                cbor => WritePackedAttestationStatement(cbor));

            var data = new MakeCredentialData(ctapResponse);

            var publicKey = Assert.IsType<CoseEdDsaPublicKey>(data.AuthenticatorData.CredentialPublicKey);
            Assert.Equal(CoseKeyType.Okp, publicKey.Type);
            Assert.Equal(CoseAlgorithmIdentifier.EdDSA, publicKey.Algorithm);
            Assert.Equal(coseKey, data.AuthenticatorData.EncodedCredentialPublicKey!.Value.ToArray());
        }

        [Fact]
        public void MakeCredentialData_UnsupportedCredentialPublicKey_PreservesRawKey()
        {
            byte[] coseKey = BuildFutureCoseKey();
            byte[] ctapResponse = BuildMakeCredentialResponse(
                BuildMinimalAuthDataWithCredentialPublicKey(coseKey),
                cbor => WritePackedAttestationStatement(cbor));

            var data = new MakeCredentialData(ctapResponse);

            Assert.Null(data.AuthenticatorData.CredentialPublicKey);
            Assert.Equal(coseKey, data.AuthenticatorData.EncodedCredentialPublicKey!.Value.ToArray());
        }

        [Fact]
        public void MakeCredentialData_SupportedAlgorithmKeyTypeMismatch_ThrowsCtap2DataException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponse(
                BuildMinimalAuthDataWithCredentialPublicKey(BuildMismatchedEs256OkpCoseKey()),
                cbor => WritePackedAttestationStatement(cbor));

            var ex = Assert.Throws<Ctap2DataException>(() => new MakeCredentialData(ctapResponse));
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void VerifyAttestation_UnsupportedAlgorithm_ThrowsNotSupportedException()
        {
            byte[] ctapResponse = BuildMakeCredentialResponse(
                BuildMinimalAuthDataWithEs256Key(),
                cbor => WritePackedAttestationStatement(
                    cbor,
                    algorithm: (int)CoseAlgorithmIdentifier.EdDSA));
            var data = new MakeCredentialData(ctapResponse);

            var ex = Assert.Throws<NotSupportedException>(() => data.VerifyAttestation(new byte[32]));

            Assert.Contains("ES256", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void VerifyAttestation_NonEcdsaCertificate_ThrowsNotSupportedException()
        {
            byte[] rsaCertificate = BuildRsaCertificate();
            byte[] ctapResponse = BuildMakeCredentialResponse(
                BuildMinimalAuthDataWithEs256Key(),
                cbor => WritePackedAttestationStatement(cbor, certificate: rsaCertificate));
            var data = new MakeCredentialData(ctapResponse);

            var ex = Assert.Throws<NotSupportedException>(() => data.VerifyAttestation(new byte[32]));

            Assert.Contains("ECDSA", ex.Message, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------------
        // Helper methods for building test CTAP responses
        // ------------------------------------------------------------------

        /// <summary>
        /// Builds a CTAP MakeCredential response with optional keys 4, 5, and
        /// 6 to test that AttestationObject only encodes keys 1, 2, 3.
        /// </summary>
        private static byte[] BuildMakeCredentialResponseWithOptionalKeys()
        {
            byte[] authData = BuildMinimalAuthDataWithEs256Key();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(6);

            // Key 1: fmt
            cbor.WriteInt32(1);
            cbor.WriteTextString("packed");

            // Key 2: authData
            cbor.WriteInt32(2);
            cbor.WriteByteString(authData);

            // Key 3: attStmt (structurally complete packed statement)
            cbor.WriteInt32(3);
            cbor.WriteStartMap(2);
            cbor.WriteTextString("alg");
            cbor.WriteInt32(-7);  // ES256
            cbor.WriteTextString("sig");
            cbor.WriteByteString(SampleDerEncodedEs256Signature());
            cbor.WriteEndMap();

            // Key 4: enterpriseAttestation
            cbor.WriteInt32(4);
            cbor.WriteBoolean(true);

            // Key 5: largeBlobKey
            cbor.WriteInt32(5);
            cbor.WriteByteString(new byte[] { 0x0A, 0x0B, 0x0C });

            // Key 6: unsignedExtensionOutputs
            cbor.WriteInt32(6);
            cbor.WriteStartMap(1);
            cbor.WriteTextString("example");
            cbor.WriteByteString(new byte[] { 0x01, 0x02 });
            cbor.WriteEndMap();

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        /// <summary>
        /// Builds a synthetic CTAP response with packed format but a malformed
        /// attestation statement. This is a structural parser fixture, not a
        /// valid attestation.
        /// </summary>
        private static byte[] BuildMakeCredentialResponseWithMalformedPackedAttestation(
            bool missingSig,
            bool extraKey)
        {
            if (missingSig == extraKey)
            {
                throw new ArgumentException("Specify exactly one malformed packed attestation shape.");
            }

            byte[] authData = BuildMinimalAuthDataWithEs256Key();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(3);

            cbor.WriteInt32(1);
            cbor.WriteTextString("packed");

            cbor.WriteInt32(2);
            cbor.WriteByteString(authData);

            cbor.WriteInt32(3);
            if (missingSig)
            {
                // Malformed packed statement: only 'alg', missing 'sig'.
                cbor.WriteStartMap(1);
                cbor.WriteTextString("alg");
                cbor.WriteInt32(-7);
                cbor.WriteEndMap();
            }
            else
            {
                // Malformed packed statement: alg, sig, x5c, plus an unexpected fourth key.
                cbor.WriteStartMap(4);
                cbor.WriteTextString("alg");
                cbor.WriteInt32(-7);
                cbor.WriteTextString("sig");
                cbor.WriteByteString(SampleDerEncodedEs256Signature());
                cbor.WriteTextString("x5c");
                cbor.WriteStartArray(0);
                cbor.WriteEndArray();
                cbor.WriteTextString("unexpected");
                cbor.WriteInt32(42);
                cbor.WriteEndMap();
            }
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        /// <summary>
        /// Builds a CTAP MakeCredential response missing one of the required keys (1, 2, or 3).
        /// </summary>
        private static byte[] BuildMakeCredentialResponseMissingKey(int missingKey)
        {
            if (missingKey < 1 || missingKey > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(missingKey), "missingKey must be 1, 2, or 3.");
            }

            byte[] authData = BuildMinimalAuthDataWithEs256Key();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(2);  // Only 2 keys instead of 3

            if (missingKey != 1)
            {
                cbor.WriteInt32(1);
                cbor.WriteTextString("packed");
            }

            if (missingKey != 2)
            {
                cbor.WriteInt32(2);
                cbor.WriteByteString(authData);
            }

            if (missingKey != 3)
            {
                cbor.WriteInt32(3);
                cbor.WriteStartMap(2);
                cbor.WriteTextString("alg");
                cbor.WriteInt32(-7);
                cbor.WriteTextString("sig");
                cbor.WriteByteString(SampleDerEncodedEs256Signature());
                cbor.WriteEndMap();
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        /// <summary>
        /// Builds a synthetic malformed packed parser fixture where 'alg' is a
        /// text string instead of an int.
        /// </summary>
        private static byte[] BuildMakeCredentialResponseWithWrongAlgType()
        {
            byte[] authData = BuildMinimalAuthDataWithEs256Key();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(3);

            cbor.WriteInt32(1);
            cbor.WriteTextString("packed");

            cbor.WriteInt32(2);
            cbor.WriteByteString(authData);

            cbor.WriteInt32(3);
            cbor.WriteStartMap(2);
            cbor.WriteTextString("alg");
            cbor.WriteTextString("ES256");
            cbor.WriteTextString("sig");
            cbor.WriteByteString(SampleDerEncodedEs256Signature());
            cbor.WriteEndMap();

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildMakeCredentialResponse(
            byte[] authData,
            Action<CborWriter> writeAttestationStatement)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(3);

            cbor.WriteInt32(1);
            cbor.WriteTextString("packed");

            cbor.WriteInt32(2);
            cbor.WriteByteString(authData);

            cbor.WriteInt32(3);
            writeAttestationStatement(cbor);

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static void WritePackedAttestationStatement(
            CborWriter cbor,
            int algorithm = (int)CoseAlgorithmIdentifier.ES256,
            bool includeAlgorithm = true,
            byte[]? certificate = null)
        {
            int entryCount = 1 + (includeAlgorithm ? 1 : 0) + (certificate is null ? 0 : 1);
            cbor.WriteStartMap(entryCount);
            if (includeAlgorithm)
            {
                cbor.WriteTextString("alg");
                cbor.WriteInt32(algorithm);
            }

            cbor.WriteTextString("sig");
            cbor.WriteByteString(SampleDerEncodedEs256Signature());

            if (certificate is not null)
            {
                cbor.WriteTextString("x5c");
                cbor.WriteStartArray(1);
                cbor.WriteByteString(certificate);
                cbor.WriteEndArray();
            }

            cbor.WriteEndMap();
        }

        private static ReadOnlyMemory<byte> ReadEncodedMapValue(byte[] encodedMap, int keyToFind)
        {
            var reader = new CborReader(encodedMap, CborConformanceMode.Ctap2Canonical);
            _ = reader.ReadStartMap();
            while (reader.PeekState() != CborReaderState.EndMap)
            {
                int key = reader.ReadInt32();
                if (key == keyToFind)
                {
                    return reader.ReadEncodedValue().ToArray();
                }

                reader.SkipValue();
            }

            throw new InvalidOperationException($"CBOR map did not contain key {keyToFind}.");
        }

        private static int CountEncodedMapEntries(ReadOnlyMemory<byte> encodedMap)
        {
            var reader = new CborReader(encodedMap, CborConformanceMode.Ctap2Canonical);
            int? count = reader.ReadStartMap();
            while (reader.PeekState() != CborReaderState.EndMap)
            {
                reader.SkipValue();
                reader.SkipValue();
            }

            reader.ReadEndMap();

            return count ?? throw new InvalidOperationException("Expected definite-length CBOR map.");
        }

        private static byte[] SampleDerEncodedEs256Signature()
        {
            byte[] signature = new byte[70];
            signature[0] = 0x30;
            signature[1] = 0x44;
            signature[2] = 0x02;
            signature[3] = 0x20;
            Array.Fill(signature, (byte)0x11, 4, 32);
            signature[36] = 0x02;
            signature[37] = 0x20;
            Array.Fill(signature, (byte)0x22, 38, 32);

            return signature;
        }

        /// <summary>
        /// Builds minimal authenticator data with an ES256 credential public key.
        /// </summary>
        private static byte[] BuildMinimalAuthDataWithEs256Key()
        {
            return BuildMinimalAuthDataWithCredentialPublicKey(BuildEs256CoseKey());
        }

        private static byte[] BuildMinimalAuthDataWithCredentialPublicKey(byte[] coseEncoded)
        {
            byte[] credId = { 0x01, 0x02, 0x03, 0x04 };

            // authData: rpIdHash(32) || flags(1) || signCount(4) || aaguid(16) || credIdLen(2) || credId || pubKey
            var result = new byte[32 + 1 + 4 + 16 + 2 + credId.Length + coseEncoded.Length];
            int offset = 0;

            // rpIdHash (zeros)
            offset += 32;

            // flags: UP=1, AT=1
            result[offset++] = 0x41;

            // signCount (zeros)
            offset += 4;

            // AAGUID (zeros)
            offset += 16;

            // credIdLen (big-endian)
            result[offset++] = (byte)((credId.Length >> 8) & 0xFF);
            result[offset++] = (byte)(credId.Length & 0xFF);

            // credId
            Array.Copy(credId, 0, result, offset, credId.Length);
            offset += credId.Length;

            // COSE public key
            Array.Copy(coseEncoded, 0, result, offset, coseEncoded.Length);

            return result;
        }

        private static byte[] BuildEs256CoseKey()
        {
            byte[] x = Enumerable.Repeat((byte)0x11, 32).ToArray();
            byte[] y = Enumerable.Repeat((byte)0x22, 32).ToArray();

            var coseKey = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            coseKey.WriteStartMap(5);
            coseKey.WriteInt32(1);   coseKey.WriteInt32(2);   // kty = EC2
            coseKey.WriteInt32(3);   coseKey.WriteInt32(-7);  // alg = ES256
            coseKey.WriteInt32(-1);  coseKey.WriteInt32(1);   // crv = P-256
            coseKey.WriteInt32(-2);  coseKey.WriteByteString(x);
            coseKey.WriteInt32(-3);  coseKey.WriteByteString(y);
            coseKey.WriteEndMap();
            return coseKey.Encode();
        }

        private static byte[] BuildOkpEdDsaCoseKey()
        {
            var publicKey = Enumerable.Repeat((byte)0x33, 32).ToArray();

            var coseKey = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            coseKey.WriteStartMap(4);
            coseKey.WriteInt32(1);   coseKey.WriteInt32((int)CoseKeyType.Okp);
            coseKey.WriteInt32(3);   coseKey.WriteInt32((int)CoseAlgorithmIdentifier.EdDSA);
            coseKey.WriteInt32(-1);  coseKey.WriteInt32((int)CoseEcCurve.Ed25519);
            coseKey.WriteInt32(-2);  coseKey.WriteByteString(publicKey);
            coseKey.WriteEndMap();
            return coseKey.Encode();
        }

        private static byte[] BuildMismatchedEs256OkpCoseKey()
        {
            var publicKey = Enumerable.Repeat((byte)0x44, 32).ToArray();

            var coseKey = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            coseKey.WriteStartMap(4);
            coseKey.WriteInt32(1);   coseKey.WriteInt32((int)CoseKeyType.Okp);
            coseKey.WriteInt32(3);   coseKey.WriteInt32((int)CoseAlgorithmIdentifier.ES256);
            coseKey.WriteInt32(-1);  coseKey.WriteInt32((int)CoseEcCurve.Ed25519);
            coseKey.WriteInt32(-2);  coseKey.WriteByteString(publicKey);
            coseKey.WriteEndMap();
            return coseKey.Encode();
        }

        private static byte[] BuildFutureCoseKey()
        {
            byte[] x = Enumerable.Repeat((byte)0x55, 32).ToArray();
            byte[] y = Enumerable.Repeat((byte)0x66, 32).ToArray();

            var coseKey = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            coseKey.WriteStartMap(5);
            coseKey.WriteInt32(1);   coseKey.WriteInt32((int)CoseKeyType.Ec2);
            coseKey.WriteInt32(3);   coseKey.WriteInt32(-70000);
            coseKey.WriteInt32(-1);  coseKey.WriteInt32((int)CoseEcCurve.P256);
            coseKey.WriteInt32(-2);  coseKey.WriteByteString(x);
            coseKey.WriteInt32(-3);  coseKey.WriteByteString(y);
            coseKey.WriteEndMap();
            return coseKey.Encode();
        }

        private static byte[] BuildRsaCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=Test RSA Attestation",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            using var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1));

            return certificate.RawData;
        }
    }
}
