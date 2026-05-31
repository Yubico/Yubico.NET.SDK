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
    public class AttestationObjectTests
    {
        private const int KeyFormat = 1;
        private const int KeyAuthenticatorData = 2;
        private const int KeyAttestationStatement = 3;

        private static readonly byte[] PackedSignature = { 0x01, 0x02, 0x03 };

        [Fact]
        public void Parse_PackedFormat_PopulatesPackedAttestationFields()
        {
            byte[] certificate = BuildSelfSignedCertificate();
            byte[] authenticatorData = BuildMinimalAuthData(BuildEs256CoseKey());
            byte[] attestationStatement = BuildPackedAttestationStatement(PackedSignature, certificate);
            byte[] encoding = BuildAttestationObject(
                AttestationFormats.Packed,
                authenticatorData,
                attestationStatement);

            var obj = new AttestationObject(encoding);

            Assert.Equal(AttestationFormats.Packed, obj.Format);
            Assert.Equal(CoseAlgorithmIdentifier.ES256, obj.AttestationAlgorithm!.Value);
            Assert.True(obj.AttestationSignature.HasValue);
            Assert.Equal(PackedSignature, obj.AttestationSignature.Value.ToArray());
            Assert.Equal(attestationStatement, obj.EncodedAttestationStatement.ToArray());
            Assert.Equal(authenticatorData, obj.AuthenticatorData.EncodedAuthenticatorData.ToArray());
            Assert.NotNull(obj.AuthenticatorData.CredentialPublicKey);
            Assert.NotNull(obj.AuthenticatorData.EncodedCredentialPublicKey);

            X509Certificate2 parsedCertificate = Assert.Single(obj.AttestationCertificates!);
            Assert.Contains("Test Attestation", parsedCertificate.Subject, StringComparison.Ordinal);
        }

        [Fact]
        public void PublicBytesConstructor_CborEncode_EmitsStandaloneThreeKeyObject()
        {
            byte[] authenticatorData = BuildMinimalAuthData(BuildEs256CoseKey());
            byte[] attestationStatement = BuildPackedAttestationStatement(PackedSignature);
            byte[] encoding = BuildAttestationObject(
                AttestationFormats.Packed,
                authenticatorData,
                attestationStatement);
            var obj = new AttestationObject(encoding);

            byte[] encoded = obj.CborEncode();

            AssertStandaloneAttestationObjectEncoding(
                encoded,
                AttestationFormats.Packed,
                authenticatorData,
                attestationStatement);
        }

        [Fact]
        public void FieldConstructor_CborEncode_EmitsStandaloneThreeKeyObject()
        {
            byte[] authenticatorData = BuildMinimalAuthData(BuildEs256CoseKey());
            byte[] attestationStatement = BuildPackedAttestationStatement(PackedSignature);

            var obj = new AttestationObject(
                AttestationFormats.Packed,
                authenticatorData,
                attestationStatement);

            Assert.Equal(AttestationFormats.Packed, obj.Format);
            Assert.Equal(CoseAlgorithmIdentifier.ES256, obj.AttestationAlgorithm!.Value);
            Assert.Equal(PackedSignature, obj.AttestationSignature!.Value.ToArray());
            AssertStandaloneAttestationObjectEncoding(
                obj.Encoded.ToArray(),
                AttestationFormats.Packed,
                authenticatorData,
                attestationStatement);
        }

        [Fact]
        public void PublicBytesConstructor_WithBytesRead_EqualsInputLength()
        {
            byte[] encoding = BuildAttestationObject(
                AttestationFormats.Packed,
                BuildMinimalAuthData(BuildEs256CoseKey()),
                BuildPackedAttestationStatement(PackedSignature));

            var obj = new AttestationObject(encoding, out int bytesRead);

            Assert.Equal(encoding.Length, bytesRead);
            Assert.Equal(encoding, obj.Encoded.ToArray());
        }

        [Fact]
        public void PublicBytesConstructor_WithTrailingData_BytesReadAndEncodedExcludeTrailingData()
        {
            byte[] encoding = BuildAttestationObject(
                AttestationFormats.Packed,
                BuildMinimalAuthData(BuildEs256CoseKey()),
                BuildPackedAttestationStatement(PackedSignature));
            byte[] withTrailing = AppendTrailingBytes(encoding, 0xFF, 10);

            var obj = new AttestationObject(withTrailing, out int bytesRead);

            Assert.Equal(encoding.Length, bytesRead);
            Assert.Equal(encoding, obj.Encoded.ToArray());
        }

        [Fact]
        public void Parse_UnknownFormat_DoesNotPopulatePackedProperties()
        {
            byte[] authenticatorData = BuildMinimalAuthData(BuildEs256CoseKey());
            byte[] attestationStatement = BuildEmptyMap();
            byte[] encoding = BuildAttestationObject(
                "none",
                authenticatorData,
                attestationStatement);

            var obj = new AttestationObject(encoding);

            Assert.Equal("none", obj.Format);
            Assert.Equal(authenticatorData, obj.AuthenticatorData.EncodedAuthenticatorData.ToArray());
            Assert.Null(obj.AttestationAlgorithm);
            Assert.Null(obj.AttestationSignature);
            Assert.Null(obj.AttestationCertificates);
            Assert.Equal(attestationStatement, obj.EncodedAttestationStatement.ToArray());
        }

        [Fact]
        public void Parse_UnsupportedCredentialPublicKey_PreservesRawKey()
        {
            byte[] coseKey = BuildFutureCoseKey();
            byte[] encoding = BuildAttestationObject(
                AttestationFormats.Packed,
                BuildMinimalAuthData(coseKey),
                BuildPackedAttestationStatement(PackedSignature));

            var obj = new AttestationObject(encoding);

            Assert.Null(obj.AuthenticatorData.CredentialPublicKey);
            Assert.Equal(coseKey, obj.AuthenticatorData.EncodedCredentialPublicKey!.Value.ToArray());
        }

        [Fact]
        public void Parse_MalformedPackedAttestation_PreservesRawStatement_AndLeavesTypedFieldsNull()
        {
            byte[] attestationStatement = BuildMalformedPackedStatementWithTextAlgorithm();
            byte[] encoding = BuildAttestationObject(
                AttestationFormats.Packed,
                BuildMinimalAuthData(BuildEs256CoseKey()),
                attestationStatement);

            var obj = new AttestationObject(encoding);

            Assert.Equal(AttestationFormats.Packed, obj.Format);
            Assert.Equal(attestationStatement, obj.EncodedAttestationStatement.ToArray());
            Assert.Null(obj.AttestationAlgorithm);
            Assert.Null(obj.AttestationSignature);
            Assert.Null(obj.AttestationCertificates);
        }

        [Fact]
        public void Parse_PackedAttestationWithMalformedSignature_PreservesRawStatement_AndLeavesTypedFieldsNull()
        {
            byte[] attestationStatement = BuildMalformedPackedStatementWithTextSignature();
            byte[] encoding = BuildAttestationObject(
                AttestationFormats.Packed,
                BuildMinimalAuthData(BuildEs256CoseKey()),
                attestationStatement);

            var obj = new AttestationObject(encoding);

            Assert.Equal(AttestationFormats.Packed, obj.Format);
            Assert.Equal(attestationStatement, obj.EncodedAttestationStatement.ToArray());
            Assert.Null(obj.AttestationAlgorithm);
            Assert.Null(obj.AttestationSignature);
            Assert.Null(obj.AttestationCertificates);
        }

        private static byte[] BuildAttestationObject(
            string format,
            byte[] authenticatorData,
            byte[] encodedAttestationStatement)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(3);
            cbor.WriteInt32(KeyFormat);
            cbor.WriteTextString(format);
            cbor.WriteInt32(KeyAuthenticatorData);
            cbor.WriteByteString(authenticatorData);
            cbor.WriteInt32(KeyAttestationStatement);
            cbor.WriteEncodedValue(encodedAttestationStatement);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static void AssertStandaloneAttestationObjectEncoding(
            byte[] encoding,
            string expectedFormat,
            byte[] expectedAuthenticatorData,
            byte[] expectedAttestationStatement)
        {
            var reader = new CborReader(encoding, CborConformanceMode.Ctap2Canonical);
            Assert.Equal(3, reader.ReadStartMap());

            Assert.Equal(KeyFormat, reader.ReadInt32());
            Assert.Equal(expectedFormat, reader.ReadTextString());

            Assert.Equal(KeyAuthenticatorData, reader.ReadInt32());
            Assert.Equal(expectedAuthenticatorData, reader.ReadByteString());

            Assert.Equal(KeyAttestationStatement, reader.ReadInt32());
            Assert.Equal(expectedAttestationStatement, reader.ReadEncodedValue().ToArray());

            reader.ReadEndMap();
            Assert.Equal(0, reader.BytesRemaining);
        }

        private static byte[] BuildPackedAttestationStatement(
            byte[] signature,
            byte[]? certificate = null)
        {
            int entryCount = certificate is null ? 2 : 3;
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(entryCount);
            cbor.WriteTextString("alg");
            cbor.WriteInt32((int)CoseAlgorithmIdentifier.ES256);
            cbor.WriteTextString("sig");
            cbor.WriteByteString(signature);

            if (certificate is not null)
            {
                cbor.WriteTextString("x5c");
                cbor.WriteStartArray(1);
                cbor.WriteByteString(certificate);
                cbor.WriteEndArray();
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildMalformedPackedStatementWithTextAlgorithm()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteTextString("alg");
            cbor.WriteTextString("not-an-int");
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildMalformedPackedStatementWithTextSignature()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(2);
            cbor.WriteTextString("alg");
            cbor.WriteInt32((int)CoseAlgorithmIdentifier.ES256);
            cbor.WriteTextString("sig");
            cbor.WriteTextString("not-bytes");
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildEmptyMap()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(0);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] AppendTrailingBytes(byte[] encoding, byte value, int count)
        {
            var result = new byte[encoding.Length + count];
            Array.Copy(encoding, result, encoding.Length);
            Array.Fill(result, value, encoding.Length, count);
            return result;
        }

        private static byte[] BuildMinimalAuthData(byte[] coseEncoded)
        {
            var credId = new byte[] { 0x01 };
            var result = new byte[32 + 1 + 4 + 16 + 2 + credId.Length + coseEncoded.Length];
            var offset = 0;
            offset += 32;
            result[offset++] = 0x41;
            offset += 4;
            offset += 16;
            result[offset++] = 0x00;
            result[offset++] = (byte)credId.Length;
            Array.Copy(credId, 0, result, offset, credId.Length);
            offset += credId.Length;
            Array.Copy(coseEncoded, 0, result, offset, coseEncoded.Length);
            return result;
        }

        private static byte[] BuildEs256CoseKey()
        {
            var x = new byte[32];
            var y = new byte[32];
            x[31] = 1;
            y[31] = 2;

            var coseKey = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            coseKey.WriteStartMap(5);
            coseKey.WriteInt32(1);
            coseKey.WriteInt32((int)CoseKeyType.Ec2);
            coseKey.WriteInt32(3);
            coseKey.WriteInt32((int)CoseAlgorithmIdentifier.ES256);
            coseKey.WriteInt32(-1);
            coseKey.WriteInt32((int)CoseEcCurve.P256);
            coseKey.WriteInt32(-2);
            coseKey.WriteByteString(x);
            coseKey.WriteInt32(-3);
            coseKey.WriteByteString(y);
            coseKey.WriteEndMap();
            return coseKey.Encode();
        }

        private static byte[] BuildFutureCoseKey()
        {
            byte[] x = Enumerable.Repeat((byte)0x55, 32).ToArray();
            byte[] y = Enumerable.Repeat((byte)0x66, 32).ToArray();

            var coseKey = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            coseKey.WriteStartMap(5);
            coseKey.WriteInt32(1);
            coseKey.WriteInt32((int)CoseKeyType.Ec2);
            coseKey.WriteInt32(3);
            coseKey.WriteInt32(-70000);
            coseKey.WriteInt32(-1);
            coseKey.WriteInt32((int)CoseEcCurve.P256);
            coseKey.WriteInt32(-2);
            coseKey.WriteByteString(x);
            coseKey.WriteInt32(-3);
            coseKey.WriteByteString(y);
            coseKey.WriteEndMap();
            return coseKey.Encode();
        }

        private static byte[] BuildSelfSignedCertificate()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest(
                "CN=Test Attestation",
                ecdsa,
                HashAlgorithmName.SHA256);

            using X509Certificate2 certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1));

            return certificate.RawData;
        }
    }
}
