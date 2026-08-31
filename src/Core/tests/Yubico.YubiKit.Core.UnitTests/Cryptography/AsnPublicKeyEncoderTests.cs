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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

/// <summary>
/// Tests for <see cref="AsnPublicKeyEncoder"/>.
/// </summary>
/// <remarks>
/// Every EC/RSA test anchors to the .NET BCL as an independent oracle: this encoder produces a
/// SubjectPublicKeyInfo and the BCL is asked to import it, with the parameters the BCL reports
/// compared against the parameters that went in. This is intentional: a same-code round trip
/// (encode here, decode with <see cref="AsnPublicKeyDecoder"/>) would pass even if both sides
/// shared the same bug, which is the most likely failure mode for a bespoke ASN.1 implementation.
/// <para>
/// <see cref="AsnPublicKeyEncoder.EncodeECDsaPublicKey"/> and
/// <see cref="AsnPublicKeyEncoder.EncodeCurve25519PublicKey"/> are private; they are covered
/// indirectly through the public overloads below rather than by widening their accessibility.
/// </para>
/// </remarks>
public class AsnPublicKeyEncoderTests
{
    // RFC 8410 section 10.1, "Example Ed25519 Public Key". Base64-decoded SubjectPublicKeyInfo,
    // reproduced byte for byte. There is no Ed25519/X25519 type in the .NET 10 BCL, so this
    // published vector is the independent oracle for Curve25519 instead of the BCL.
    private static readonly byte[] Rfc8410Ed25519PublicKeySpki =
    [
        0x30, 0x2A, 0x30, 0x05, 0x06, 0x03, 0x2B, 0x65, 0x70, 0x03, 0x21, 0x00,
        0x19, 0xBF, 0x44, 0x09, 0x69, 0x84, 0xCD, 0xFE, 0x85, 0x41, 0xBA, 0xC1,
        0x67, 0xDC, 0x3B, 0x96, 0xC8, 0x50, 0x86, 0xAA, 0x30, 0xB6, 0xB6, 0xCB,
        0x0C, 0x5C, 0x38, 0xAD, 0x70, 0x31, 0x66, 0xE1
    ];

    private static readonly byte[] Rfc8410Ed25519RawPublicKey = Rfc8410Ed25519PublicKeySpki[12..];

    // RFC 8410 section 10.2, "Example X25519 Certificate". The certificate's ASN.1 dump shows
    // the subjectPublicKeyInfo field verbatim (a self-contained 44-byte SEQUENCE). Reproduced
    // byte for byte from the RFC's own hex dump, not invented.
    private static readonly byte[] Rfc8410X25519PublicKeySpki =
    [
        0x30, 0x2A, 0x30, 0x05, 0x06, 0x03, 0x2B, 0x65, 0x6E, 0x03, 0x21, 0x00,
        0x85, 0x20, 0xF0, 0x09, 0x89, 0x30, 0xA7, 0x54, 0x74, 0x8B, 0x7D, 0xDC,
        0xB4, 0x3E, 0xF7, 0x5A, 0x0D, 0xBF, 0x3A, 0x0D, 0x26, 0x38, 0x1A, 0xF4,
        0xEB, 0xA4, 0xA9, 0x8E, 0xAA, 0x9B, 0x4E, 0x6A
    ];

    private static readonly byte[] Rfc8410X25519RawPublicKey = Rfc8410X25519PublicKeySpki[12..];

    // ---------------------------------------------------------------------
    // Overload 1: EncodeToSubjectPublicKeyInfo(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
    // EC branch. Our encoder encodes; BCL decodes and reports parameters. Covers the private
    // EncodeECDsaPublicKey helper indirectly.
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("1.2.840.10045.3.1.7", KeyType.ECP256)] // P-256
    [InlineData("1.3.132.0.34", KeyType.ECP384)] // P-384
    [InlineData("1.3.132.0.35", KeyType.ECP521)] // P-521
    public void EncodeToSubjectPublicKeyInfo_PublicPointAndKeyType_BclAcceptsAndMatchesOriginal(
        string curveOid, KeyType keyType)
    {
        var curve = ECCurve.CreateFromValue(curveOid);
        using var ecdsa = ECDsa.Create(curve);
        var original = ecdsa.ExportParameters(includePrivateParameters: false);
        var publicPoint = BuildUncompressedPoint(original.Q.X!, original.Q.Y!);

        var spki = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(publicPoint, keyType);

        using var check = ECDsa.Create();
        check.ImportSubjectPublicKeyInfo(spki, out var bytesRead);
        Assert.Equal(spki.Length, bytesRead);

        var decoded = check.ExportParameters(includePrivateParameters: false);
        Assert.Equal(original.Q.X, decoded.Q.X);
        Assert.Equal(original.Q.Y, decoded.Q.Y);
        Assert.Equal(curveOid, decoded.Curve.Oid.Value);
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_PublicPointAndKeyType_Ed25519_MatchesRfc8410Vector()
    {
        var spki = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(Rfc8410Ed25519RawPublicKey, KeyType.Ed25519);

        Assert.Equal(Rfc8410Ed25519PublicKeySpki, spki);
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_PublicPointAndKeyType_X25519_MatchesRfc8410Vector()
    {
        var spki = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(Rfc8410X25519RawPublicKey, KeyType.X25519);

        Assert.Equal(Rfc8410X25519PublicKeySpki, spki);
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_UnsupportedKeyType_ThrowsNotSupportedException() =>
        Assert.Throws<NotSupportedException>(
            () => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(new byte[32], KeyType.RSA2048));

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_EcPointWrongPrefix_ThrowsArgumentException()
    {
        var badPoint = new byte[65];
        badPoint[0] = 0x02; // compressed-form prefix, not supported

        Assert.Throws<ArgumentException>(
            () => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(badPoint, KeyType.ECP256));
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_EcPointWrongLength_ThrowsArgumentException()
    {
        // Correct 0x04 prefix but P-256 requires 65 bytes total; this is truncated.
        byte[] badPoint = [0x04, 0x01, 0x02, 0x03];

        Assert.Throws<ArgumentException>(
            () => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(badPoint, KeyType.ECP256));
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_Curve25519PointWrongLength_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(
            () => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(new byte[16], KeyType.Ed25519));

    // ---------------------------------------------------------------------
    // Overload 2: EncodeToSubjectPublicKeyInfo(ReadOnlyMemory<byte> modulus, ReadOnlyMemory<byte> exponent)
    // Our encoder encodes; BCL decodes and reports parameters.
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    public void EncodeToSubjectPublicKeyInfo_ModulusAndExponent_BclAcceptsAndMatchesOriginal(int keySizeBits)
    {
        using var rsa = RSA.Create(keySizeBits);
        var original = rsa.ExportParameters(includePrivateParameters: false);

        var spki = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(original.Modulus!, original.Exponent!);

        using var check = RSA.Create();
        check.ImportSubjectPublicKeyInfo(spki, out var bytesRead);
        Assert.Equal(spki.Length, bytesRead);

        var decoded = check.ExportParameters(includePrivateParameters: false);
        Assert.Equal(original.Modulus, decoded.Modulus);
        Assert.Equal(original.Exponent, decoded.Exponent);
    }

    // ---------------------------------------------------------------------
    // Overload 3: EncodeToSubjectPublicKeyInfo(RSAParameters)
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    public void EncodeToSubjectPublicKeyInfo_RsaParameters_BclAcceptsAndMatchesOriginal(int keySizeBits)
    {
        using var rsa = RSA.Create(keySizeBits);
        var original = rsa.ExportParameters(includePrivateParameters: false);

        var spki = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(original);

        using var check = RSA.Create();
        check.ImportSubjectPublicKeyInfo(spki, out _);
        var decoded = check.ExportParameters(includePrivateParameters: false);

        Assert.Equal(original.Modulus, decoded.Modulus);
        Assert.Equal(original.Exponent, decoded.Exponent);
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_RsaParametersMissingModulus_ThrowsInvalidOperationException()
    {
        var parameters = new RSAParameters { Modulus = null, Exponent = [0x01, 0x00, 0x01] };

        Assert.Throws<InvalidOperationException>(
            () => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(parameters));
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_RsaParametersMissingExponent_ThrowsInvalidOperationException()
    {
        var parameters = new RSAParameters { Modulus = new byte[256], Exponent = null };

        Assert.Throws<InvalidOperationException>(
            () => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(parameters));
    }

    // ---------------------------------------------------------------------
    // Overload 4: EncodeToSubjectPublicKeyInfo(ECParameters)
    // Covers the private EncodeECDsaPublicKey helper indirectly through a second call path.
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("1.2.840.10045.3.1.7")] // P-256
    [InlineData("1.3.132.0.34")] // P-384
    [InlineData("1.3.132.0.35")] // P-521
    public void EncodeToSubjectPublicKeyInfo_EcParameters_BclAcceptsAndMatchesOriginal(string curveOid)
    {
        var curve = ECCurve.CreateFromValue(curveOid);
        using var ecdsa = ECDsa.Create(curve);
        var original = ecdsa.ExportParameters(includePrivateParameters: false);

        var spki = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(original);

        using var check = ECDsa.Create();
        check.ImportSubjectPublicKeyInfo(spki, out _);
        var decoded = check.ExportParameters(includePrivateParameters: false);

        Assert.Equal(original.Q.X, decoded.Q.X);
        Assert.Equal(original.Q.Y, decoded.Q.Y);
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_EcParametersMissingX_ThrowsArgumentException()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var original = ecdsa.ExportParameters(includePrivateParameters: false);
        var parameters = new ECParameters
        {
            Curve = original.Curve,
            Q = new ECPoint { X = null, Y = original.Q.Y }
        };

        Assert.Throws<ArgumentException>(() => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(parameters));
    }

    [Fact]
    public void EncodeToSubjectPublicKeyInfo_EcParametersMissingY_ThrowsArgumentException()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var original = ecdsa.ExportParameters(includePrivateParameters: false);
        var parameters = new ECParameters
        {
            Curve = original.Curve,
            Q = new ECPoint { X = original.Q.X, Y = null }
        };

        Assert.Throws<ArgumentException>(() => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(parameters));
    }

    private static byte[] BuildUncompressedPoint(byte[] x, byte[] y)
    {
        var point = new byte[1 + x.Length + y.Length];
        point[0] = 0x04;
        x.CopyTo(point, 1);
        y.CopyTo(point, 1 + x.Length);
        return point;
    }
}