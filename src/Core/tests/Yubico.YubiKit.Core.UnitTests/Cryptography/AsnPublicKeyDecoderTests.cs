// Copyright 2026 Yubico AB
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

using System.Formats.Asn1;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

/// <summary>
/// Tests for <see cref="AsnPublicKeyDecoder"/>.
/// </summary>
/// <remarks>
/// EC and RSA tests use .NET cryptography APIs as an independent encoding oracle rather than
/// round-tripping through the matching SDK encoder, which could share the same defect. Ed25519 and
/// X25519 tests use RFC 8410 vectors because .NET 10 has no standalone Ed25519 or X25519 key-import
/// API suitable as a test oracle.
/// </remarks>
public class AsnPublicKeyDecoderTests
{
    // RFC 8410 section 10.1, "Example Ed25519 Public Key":
    //   -----BEGIN PUBLIC KEY-----
    //   MCowBQYDK2VwAyEAGb9ECWmEzf6FQbrBZ9w7lshQhqowtrbLDFw4rXAxZuE=
    //   -----END PUBLIC KEY-----
    // Base64-decoded as the DER-encoded SubjectPublicKeyInfo below.
    private static readonly byte[] Rfc8410Ed25519PublicKeySpki =
    [
        0x30, 0x2A, 0x30, 0x05, 0x06, 0x03, 0x2B, 0x65, 0x70, 0x03, 0x21, 0x00,
        0x19, 0xBF, 0x44, 0x09, 0x69, 0x84, 0xCD, 0xFE, 0x85, 0x41, 0xBA, 0xC1,
        0x67, 0xDC, 0x3B, 0x96, 0xC8, 0x50, 0x86, 0xAA, 0x30, 0xB6, 0xB6, 0xCB,
        0x0C, 0x5C, 0x38, 0xAD, 0x70, 0x31, 0x66, 0xE1
    ];

    private static readonly byte[] Rfc8410Ed25519RawPublicKey = Rfc8410Ed25519PublicKeySpki[12..];

    // RFC 8410 section 10.2, "Example X25519 Certificate". SubjectPublicKeyInfo reconstructed from
    // the certificate's ASN.1 dump at offset 115.
    private static readonly byte[] Rfc8410X25519PublicKeySpki =
    [
        0x30, 0x2A, 0x30, 0x05, 0x06, 0x03, 0x2B, 0x65, 0x6E, 0x03, 0x21, 0x00,
        0x85, 0x20, 0xF0, 0x09, 0x89, 0x30, 0xA7, 0x54, 0x74, 0x8B, 0x7D, 0xDC,
        0xB4, 0x3E, 0xF7, 0x5A, 0x0D, 0xBF, 0x3A, 0x0D, 0x26, 0x38, 0x1A, 0xF4,
        0xEB, 0xA4, 0xA9, 0x8E, 0xAA, 0x9B, 0x4E, 0x6A
    ];

    private static readonly byte[] Rfc8410X25519RawPublicKey = Rfc8410X25519PublicKeySpki[12..];

    [Theory]
    [InlineData("1.2.840.10045.3.1.7")] // P-256
    [InlineData("1.3.132.0.34")] // P-384
    [InlineData("1.3.132.0.35")] // P-521
    public void CreatePublicKey_EcSpkiFromBcl_DecodesToMatchingParameters(string curveOid)
    {
        var curve = ECCurve.CreateFromValue(curveOid);
        using var ecdsa = ECDsa.Create(curve);
        var spki = ecdsa.ExportSubjectPublicKeyInfo();
        var expected = ecdsa.ExportParameters(includePrivateParameters: false);

        var decoded = AsnPublicKeyDecoder.CreatePublicKey(spki).Cast<ECPublicKey>();

        Assert.Equal(expected.Curve.Oid.Value, decoded.Parameters.Curve.Oid.Value);
        Assert.Equal(expected.Q.X, decoded.Parameters.Q.X);
        Assert.Equal(expected.Q.Y, decoded.Parameters.Q.Y);
    }

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    public void CreatePublicKey_RsaSpkiFromBcl_DecodesToMatchingParameters(int keySizeBits)
    {
        using var rsa = RSA.Create(keySizeBits);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var expected = rsa.ExportParameters(includePrivateParameters: false);

        var decoded = AsnPublicKeyDecoder.CreatePublicKey(spki).Cast<RSAPublicKey>();

        Assert.Equal(expected.Modulus, decoded.Parameters.Modulus);
        Assert.Equal(expected.Exponent, decoded.Parameters.Exponent);
    }

    [Fact]
    public void CreatePublicKey_Rfc8410Ed25519Vector_DecodesToPublishedRawKey()
    {
        var decoded = AsnPublicKeyDecoder.CreatePublicKey(Rfc8410Ed25519PublicKeySpki).Cast<Curve25519PublicKey>();

        Assert.Equal(KeyType.Ed25519, decoded.KeyType);
        Assert.Equal(Rfc8410Ed25519RawPublicKey, decoded.PublicPoint.ToArray());
    }

    [Fact]
    public void CreatePublicKey_Rfc8410X25519Vector_DecodesToPublishedRawKey()
    {
        var decoded = AsnPublicKeyDecoder.CreatePublicKey(Rfc8410X25519PublicKeySpki).Cast<Curve25519PublicKey>();

        Assert.Equal(KeyType.X25519, decoded.KeyType);
        Assert.Equal(Rfc8410X25519RawPublicKey, decoded.PublicPoint.ToArray());
    }

    [Fact]
    public void CreatePublicKey_EcPointPrefixNotUncompressed_ThrowsCryptographicExceptionWithExpectedMessage()
    {
        var spki = BuildEcSubjectPublicKeyInfo(Oids.ECP256, PointWithBadPrefix(0x02, coordinateSize: 32));

        var ex = Assert.Throws<CryptographicException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));

        Assert.Equal("Unsupported EC point format", ex.Message);
    }

    [Fact]
    public void CreatePublicKey_EcPointWrongLength_ThrowsCryptographicExceptionWithExpectedMessage()
    {
        // Valid 0x04 prefix but truncated coordinate data for P-256 (expects 65 bytes total).
        var badPoint = new byte[] { 0x04, 0x01, 0x02, 0x03 };
        var spki = BuildEcSubjectPublicKeyInfo(Oids.ECP256, badPoint);

        var ex = Assert.Throws<CryptographicException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));

        Assert.Equal("Invalid EC public key encoding", ex.Message);
    }

    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void CreatePublicKey_EmptyEcPoint_ThrowsCryptographicExceptionNotIndexOutOfRange(string curveOid)
    {
        // A zero-length BIT STRING has no prefix octet to inspect. The decoder must report this as
        // malformed encoded data, not index past the end of the buffer.
        var spki = BuildEcSubjectPublicKeyInfo(curveOid, []);

        var ex = Assert.Throws<CryptographicException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));

        Assert.Equal("Invalid EC public key encoding", ex.Message);
    }

    [Fact]
    public void CreatePublicKey_UnknownBitStringUnusedBits_ThrowsCryptographicException()
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(Oids.RSA);
                writer.WriteNull();
            }

            // A non-zero unused-bit-count is invalid for a public key BIT STRING.
            writer.WriteBitString([0x00], unusedBitCount: 3);
        }

        var spki = writer.Encode();

        Assert.Throws<CryptographicException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));
    }

    [Fact]
    public void CreatePublicKey_UnsupportedAlgorithmOid_ThrowsNotSupportedException()
    {
        // The exception type, rather than a particular diagnostic string, is the contract here.
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                // commonName (2.5.4.3) is a syntactically valid OID that is not one of the
                // algorithms AsnPublicKeyDecoder understands.
                writer.WriteObjectIdentifier("2.5.4.3");
            }

            writer.WriteBitString([0x00]);
        }

        var spki = writer.Encode();

        Assert.Throws<NotSupportedException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));
    }

    [Fact]
    public void CreatePublicKey_UnsupportedCurveOid_ThrowsNotSupportedException()
    {
        // brainpoolP256r1 (1.3.36.3.3.2.8.1.1.7) is a syntactically valid EC curve OID that is
        // not one of P-256/P-384/P-521, which AsnPublicKeyDecoder.CreateECPublicKey rejects.
        var spki = BuildEcSubjectPublicKeyInfo("1.3.36.3.3.2.8.1.1.7", PointWithBadPrefix(0x04, coordinateSize: 32));

        Assert.Throws<NotSupportedException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));
    }

    [Fact]
    public void CreatePublicKey_MalformedDer_ThrowsAsnContentException()
    {
        byte[] malformed = [0xFF, 0xFF, 0xFF];

        Assert.Throws<AsnContentException>(() => AsnPublicKeyDecoder.CreatePublicKey(malformed));
    }

    [Fact]
    public void CreatePublicKey_TruncatedSpki_ThrowsAsnContentException()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki = ecdsa.ExportSubjectPublicKeyInfo();
        var truncated = spki[..^5];

        Assert.Throws<AsnContentException>(() => AsnPublicKeyDecoder.CreatePublicKey(truncated));
    }

    private static byte[] BuildEcSubjectPublicKeyInfo(string curveOid, byte[] point)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(Oids.ECDSA);
                writer.WriteObjectIdentifier(curveOid);
            }

            writer.WriteBitString(point);
        }

        return writer.Encode();
    }

    private static byte[] PointWithBadPrefix(byte prefix, int coordinateSize)
    {
        var point = new byte[1 + (2 * coordinateSize)];
        point[0] = prefix;
        return point;
    }
}