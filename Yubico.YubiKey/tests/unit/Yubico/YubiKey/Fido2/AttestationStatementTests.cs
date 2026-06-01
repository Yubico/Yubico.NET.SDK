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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    public class AttestationStatementTests
    {
        private static readonly byte[] AttestationSignature = { 0x01, 0x02, 0x03 };

        [Fact]
        public void FromCbor_PackedFormat_ParsesAlgorithmSignatureAndCertificates()
        {
            byte[] certificate = BuildSelfSignedCertificate();
            byte[] encoded = BuildPackedAttestationStatement(AttestationSignature, certificate);

            var statement = Assert.IsType<PackedAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.Packed, encoded));

            Assert.Equal(AttestationFormats.Packed, statement.Format);
            Assert.Equal(CoseAlgorithmIdentifier.ES256, statement.Algorithm);
            Assert.Equal(AttestationSignature, statement.Signature.ToArray());
            Assert.Equal(encoded, statement.Encoded.ToArray());
            X509Certificate2 parsedCertificate = Assert.Single(statement.Certificates!);
            Assert.Equal(certificate, parsedCertificate.RawData);
        }

        [Fact]
        public void FromCbor_PackedFormatWithUnsupportedByteStringField_ReturnsUnknownStatement()
        {
            byte[] unsupportedValue = { 0x0A, 0x0B };
            byte[] encoded = BuildPackedAttestationStatement(
                AttestationSignature,
                unsupportedByteStringKey: "ecdaa" + "KeyId",
                unsupportedByteStringValue: unsupportedValue);

            var statement = Assert.IsType<UnknownAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.Packed, encoded));

            Assert.Equal(AttestationFormats.Packed, statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
        }

        [Fact]
        public void FromCbor_PackedFormatWithMalformedAlgorithm_ReturnsUnknownStatement()
        {
            byte[] encoded = BuildMalformedPackedStatementWithTextAlgorithm();

            var statement = Assert.IsType<UnknownAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.Packed, encoded));

            Assert.Equal(AttestationFormats.Packed, statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
        }

        [Fact]
        public void FromCbor_PackedFormatWithMalformedSignature_ReturnsUnknownStatement()
        {
            byte[] encoded = BuildMalformedPackedStatementWithTextSignature();

            var statement = Assert.IsType<UnknownAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.Packed, encoded));

            Assert.Equal(AttestationFormats.Packed, statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
        }

        [Fact]
        public void FromCbor_FidoU2fFormat_ParsesSignatureAndCertificates()
        {
            byte[] certificate = BuildSelfSignedCertificate();
            byte[] encoded = BuildSignatureAndCertificateAttestationStatement(AttestationSignature, certificate);

            var statement = Assert.IsType<FidoU2fAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.FidoU2f, encoded));

            Assert.Equal(AttestationFormats.FidoU2f, statement.Format);
            Assert.Equal(AttestationSignature, statement.Signature.ToArray());
            Assert.Equal(encoded, statement.Encoded.ToArray());
            X509Certificate2 parsedCertificate = Assert.Single(statement.Certificates);
            Assert.Equal(certificate, parsedCertificate.RawData);
        }

        [Fact]
        public void FromCbor_FidoU2fFormatWithExtraKey_ReturnsUnknownStatement()
        {
            byte[] certificate = BuildSelfSignedCertificate();
            byte[] encoded = BuildSignatureAndCertificateAttestationStatement(
                AttestationSignature,
                certificate,
                includeExtraKey: true);

            var statement = Assert.IsType<UnknownAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.FidoU2f, encoded));

            Assert.Equal(AttestationFormats.FidoU2f, statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
        }

        [Fact]
        public void FromCbor_AppleFormat_ParsesCertificates()
        {
            byte[] certificate = BuildSelfSignedCertificate();
            byte[] encoded = BuildCertificateOnlyAttestationStatement(certificate);

            var statement = Assert.IsType<AppleAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.Apple, encoded));

            Assert.Equal(AttestationFormats.Apple, statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
            X509Certificate2 parsedCertificate = Assert.Single(statement.Certificates);
            Assert.Equal(certificate, parsedCertificate.RawData);
        }

        [Fact]
        public void FromCbor_AppleFormatWithExtraKey_ReturnsUnknownStatement()
        {
            byte[] certificate = BuildSelfSignedCertificate();
            byte[] encoded = BuildCertificateOnlyAttestationStatement(certificate, includeExtraKey: true);

            var statement = Assert.IsType<UnknownAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.Apple, encoded));

            Assert.Equal(AttestationFormats.Apple, statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
        }

        [Fact]
        public void FromCbor_NoneFormatWithEmptyMap_ReturnsNoneStatement()
        {
            byte[] encoded = BuildEmptyMap();

            var statement = Assert.IsType<NoneAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.None, encoded));

            Assert.Equal(AttestationFormats.None, statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
        }

        [Fact]
        public void FromCbor_NoneFormatWithNonEmptyMap_ReturnsUnknownStatement()
        {
            byte[] encoded = BuildPackedAttestationStatement(AttestationSignature);

            var statement = Assert.IsType<UnknownAttestationStatement>(
                AttestationStatement.FromCbor(AttestationFormats.None, encoded));

            Assert.Equal(AttestationFormats.None, statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
        }

        [Fact]
        public void FromCbor_UnknownFormat_PreservesFormatAndEncodedStatement()
        {
            byte[] encoded = BuildEmptyMap();

            var statement = Assert.IsType<UnknownAttestationStatement>(
                AttestationStatement.FromCbor("vendor-format", encoded));

            Assert.Equal("vendor-format", statement.Format);
            Assert.Equal(encoded, statement.Encoded.ToArray());
        }

        private static byte[] BuildPackedAttestationStatement(
            byte[] signature,
            byte[]? certificate = null,
            string? unsupportedByteStringKey = null,
            byte[]? unsupportedByteStringValue = null)
        {
            int entryCount = 2;
            if (certificate is not null)
            {
                entryCount++;
            }

            if (unsupportedByteStringKey is not null)
            {
                entryCount++;
            }

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

            if (unsupportedByteStringKey is not null)
            {
                cbor.WriteTextString(unsupportedByteStringKey);
                cbor.WriteByteString(unsupportedByteStringValue ?? Array.Empty<byte>());
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildSignatureAndCertificateAttestationStatement(
            byte[] signature,
            byte[] certificate,
            bool includeExtraKey = false)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(includeExtraKey ? 3 : 2);
            cbor.WriteTextString("sig");
            cbor.WriteByteString(signature);
            cbor.WriteTextString("x5c");
            cbor.WriteStartArray(1);
            cbor.WriteByteString(certificate);
            cbor.WriteEndArray();
            if (includeExtraKey)
            {
                cbor.WriteTextString("zzzz");
                cbor.WriteBoolean(true);
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildCertificateOnlyAttestationStatement(
            byte[] certificate,
            bool includeExtraKey = false)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(includeExtraKey ? 2 : 1);
            cbor.WriteTextString("x5c");
            cbor.WriteStartArray(1);
            cbor.WriteByteString(certificate);
            cbor.WriteEndArray();
            if (includeExtraKey)
            {
                cbor.WriteTextString("zzzz");
                cbor.WriteBoolean(true);
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildMalformedPackedStatementWithTextAlgorithm()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(2);
            cbor.WriteTextString("alg");
            cbor.WriteTextString("not-an-int");
            cbor.WriteTextString("sig");
            cbor.WriteByteString(AttestationSignature);
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
