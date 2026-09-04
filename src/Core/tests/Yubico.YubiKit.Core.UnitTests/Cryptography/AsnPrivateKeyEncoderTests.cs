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

/// <summary>Tests for <see cref="AsnPrivateKeyEncoder"/>.</summary>
/// <remarks>
/// RSA and EC output is checked against .NET cryptography. Curve25519 output is checked against
/// RFC 8410 and the OpenSSL encoding of the RFC 7748 section 6.1 Alice key material.
/// </remarks>
public class AsnPrivateKeyEncoderTests
{
    private static KeyType GetKeyType(string curveOid) => curveOid switch
    {
        Oids.ECP256 => KeyType.ECP256,
        Oids.ECP384 => KeyType.ECP384,
        Oids.ECP521 => KeyType.ECP521,
        _ => throw new ArgumentOutOfRangeException(nameof(curveOid))
    };

    #region EncodeToPkcs8(RSAParameters) - Encoder vs BCL oracle

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    public void EncodeToPkcs8_RsaParameters_BclAcceptsAndMatchesOriginal(int keySizeBits)
    {
        using var rsa = RSA.Create(keySizeBits);
        var expected = rsa.ExportParameters(includePrivateParameters: true);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(expected);

        using var check = RSA.Create();
        check.ImportPkcs8PrivateKey(ourPkcs8, out _);
        var actual = check.ExportParameters(includePrivateParameters: true);

        Assert.Equal(expected.Modulus, actual.Modulus);
        Assert.Equal(expected.Exponent, actual.Exponent);
        Assert.Equal(expected.D, actual.D);
        Assert.Equal(expected.P, actual.P);
        Assert.Equal(expected.Q, actual.Q);
        Assert.Equal(expected.DP, actual.DP);
        Assert.Equal(expected.DQ, actual.DQ);
        Assert.Equal(expected.InverseQ, actual.InverseQ);
    }

    [Fact]
    public void EncodeToPkcs8_RsaParameters_MissingPrivateParts_ThrowsArgumentException()
    {
        using var rsa = RSA.Create(2048);
        var publicParameters = rsa.ExportParameters(includePrivateParameters: false);
        var publicOnly = new RSAParameters
        {
            Modulus = publicParameters.Modulus,
            Exponent = publicParameters.Exponent
        };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(publicOnly));
    }

    #endregion

    #region EncodeToPkcs8(ECParameters) - Encoder vs BCL oracle

    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void EncodeToPkcs8_EcParametersWithPublicPoint_BclImportsAndRoundTrips(string curveOid)
    {
        using var bcl = ECDsa.Create(EcTestSupport.NamedCurve(curveOid));
        var expected = bcl.ExportParameters(includePrivateParameters: true);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(expected);

        using var check = ECDsa.Create();
        check.ImportPkcs8PrivateKey(ourPkcs8, out var bytesRead);
        Assert.Equal(ourPkcs8.Length, bytesRead);

        var actual = check.ExportParameters(includePrivateParameters: true);
        Assert.Equal(curveOid, actual.Curve.Oid.Value);
        Assert.Equal(expected.D, actual.D);
        Assert.Equal(expected.Q.X, actual.Q.X);
        Assert.Equal(expected.Q.Y, actual.Q.Y);
    }

    [Theory]
    [InlineData(Oids.ECP256, 32)]
    [InlineData(Oids.ECP384, 48)]
    [InlineData(Oids.ECP521, 66)]
    public void EncodeToPkcs8_EcParametersWithPublicPoint_EmitsExplicitlyTaggedBitString(
        string curveOid, int coordinateSize)
    {
        using var bcl = ECDsa.Create(EcTestSupport.NamedCurve(curveOid));
        var parameters = bcl.ExportParameters(includePrivateParameters: true);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(parameters);
        var ecPrivateKeyDer = ReadPkcs8PrivateKeyOctets(ourPkcs8);

        // RFC 5915 defines publicKey as EXPLICIT [1] BIT STRING, so the encoding is a constructed
        // [1] wrapper (0xA1) around a universal BIT STRING (0x03) with zero unused bits.
        var point = EcTestSupport.UncompressedPoint(parameters.Q.X!, parameters.Q.Y!);
        Assert.Equal(1 + (2 * coordinateSize), point.Length);

        // BIT STRING: 03 <len> 00 || point, where the 00 is the unused-bit count.
        byte[] bitString = [0x03, .. DerLength(point.Length + 1), 0x00, .. point];

        // EXPLICIT [1]: A1 <len> || BIT STRING.
        byte[] expectedTail = [0xA1, .. DerLength(bitString.Length), .. bitString];

        Assert.Equal(expectedTail, ecPrivateKeyDer[^expectedTail.Length..]);
    }

    /// <summary>DER definite-length octets for lengths below 256, which covers every case here.</summary>
    private static byte[] DerLength(int length)
    {
        Assert.InRange(length, 0, 255);
        return length < 0x80 ? [(byte)length] : [0x81, (byte)length];
    }

    /// <summary>Returns the PKCS#8 privateKey OCTET STRING contents (the RFC 5915 ECPrivateKey DER).</summary>
    private static byte[] ReadPkcs8PrivateKeyOctets(byte[] pkcs8)
    {
        var reader = new AsnReader(pkcs8, AsnEncodingRules.DER);
        var privateKeyInfo = reader.ReadSequence();
        _ = privateKeyInfo.ReadInteger();
        _ = privateKeyInfo.ReadSequence();
        return privateKeyInfo.ReadOctetString();
    }

    [Fact]
    public void EncodeToPkcs8_EcParametersWithoutPublicPoint_BclStillAcceptsAndMatchesD()
    {
        using var bcl = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var full = bcl.ExportParameters(includePrivateParameters: true);
        var withoutQ = new ECParameters
        {
            Curve = full.Curve,
            D = full.D
        };

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(withoutQ);

        using var check = ECDsa.Create();
        check.ImportPkcs8PrivateKey(ourPkcs8, out _);
        var actual = check.ExportParameters(includePrivateParameters: true);

        Assert.Equal(full.D, actual.D);
    }

    #endregion

    #region EncodeToPkcs8(ECParameters) - optional RFC 5915 publicKey field

    /// <summary>
    /// True when the RFC 5915 ECPrivateKey carries the optional <c>[1] publicKey</c> field.
    /// </summary>
    private static bool HasPublicKeyField(byte[] pkcs8)
    {
        var seqEcPrivateKey = new AsnReader(ReadPkcs8PrivateKeyOctets(pkcs8), AsnEncodingRules.BER)
            .ReadSequence();
        _ = seqEcPrivateKey.ReadInteger();
        _ = seqEcPrivateKey.ReadOctetString();

        while (seqEcPrivateKey.HasData)
        {
            if (seqEcPrivateKey.PeekTag() is { TagValue: 1, TagClass: TagClass.ContextSpecific })
            {
                return true;
            }

            _ = seqEcPrivateKey.ReadEncodedValue();
        }

        return false;
    }

    [Fact]
    public void EncodeToPkcs8_EcParametersBothCoordinatesPresent_EmitsPublicKeyField()
    {
        using var bcl = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = bcl.ExportParameters(includePrivateParameters: true);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(parameters);

        Assert.True(HasPublicKeyField(ourPkcs8));
    }

    [Fact]
    public void EncodeToPkcs8_EcParametersBothCoordinatesNull_OmitsPublicKeyField()
    {
        using var bcl = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var full = bcl.ExportParameters(includePrivateParameters: true);
        var withoutQ = new ECParameters
        {
            Curve = full.Curve,
            D = full.D,
            Q = new ECPoint { X = null, Y = null }
        };

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(withoutQ);

        Assert.False(HasPublicKeyField(ourPkcs8));
    }

    /// <summary>
    /// A half-supplied point is caller error, not an instruction to drop the public key. Silently
    /// omitting <c>[1]</c> would return a valid encoding that has quietly discarded key material.
    /// </summary>
    [Theory]
    [InlineData(true, false)] // X supplied, Y missing
    [InlineData(false, true)] // Y supplied, X missing
    public void EncodeToPkcs8_EcParametersPartialPublicPoint_ThrowsArgumentException(
        bool includeX, bool includeY)
    {
        using var bcl = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var full = bcl.ExportParameters(includePrivateParameters: true);
        var partial = new ECParameters
        {
            Curve = full.Curve,
            D = full.D,
            Q = new ECPoint
            {
                X = includeX ? full.Q.X : null,
                Y = includeY ? full.Q.Y : null
            }
        };

        var exception = Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(partial));

        Assert.DoesNotContain(Convert.ToHexString(full.D!), exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region EncodeToPkcs8(ECParameters) - curve OID must be a supported prime curve

    /// <summary>
    /// The curve OID comes straight from the caller. Resolving it through a general OID lookup
    /// would let a non-EC algorithm OID through and emit it as an id-ecPublicKey curve parameter.
    /// </summary>
    [Theory]
    [InlineData(Oids.X25519)]
    [InlineData(Oids.Ed25519)]
    [InlineData(Oids.AES256Cbc)]
    public void EncodeToPkcs8_EcParametersNonEcCurveOid_ThrowsArgumentException(string oid)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.CreateFromValue(oid),
            D = new byte[32],
            Q = new ECPoint { X = new byte[32], Y = new byte[32] }
        };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    [Theory]
    [InlineData("1.3.132.0.10")] // secp256k1
    [InlineData("1.2.840.10045.3.1.1")] // secp192r1
    public void EncodeToPkcs8_EcParametersUnsupportedCurveOid_ThrowsArgumentException(string oid)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.CreateFromValue(oid),
            D = new byte[32],
            Q = new ECPoint { X = new byte[32], Y = new byte[32] }
        };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    /// <summary>
    /// Without a public point there is no point-shape check to catch the curve, but emitting
    /// id-ecPublicKey with an unsupported curve parameter is just as wrong.
    /// </summary>
    [Theory]
    [InlineData(Oids.X25519)]
    [InlineData("1.3.132.0.10")] // secp256k1
    public void EncodeToPkcs8_EcParametersUnsupportedCurveOidWithoutPublicPoint_ThrowsArgumentException(string oid)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.CreateFromValue(oid),
            D = new byte[32]
        };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    [Theory]
    [InlineData(Oids.ECP256, 31, 32)] // X one byte short
    [InlineData(Oids.ECP256, 32, 31)] // Y one byte short
    [InlineData(Oids.ECP256, 33, 33)] // both oversized but self-consistent
    [InlineData(Oids.ECP256, 48, 48)] // P-384 sized coordinates on a P-256 curve
    [InlineData(Oids.ECP384, 32, 32)] // P-256 sized coordinates on a P-384 curve
    [InlineData(Oids.ECP521, 65, 66)] // P-521 coordinates are 66 bytes each
    public void EncodeToPkcs8_EcParametersCoordinateSizeMismatch_ThrowsArgumentException(
        string curveOid, int xLength, int yLength)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.CreateFromValue(curveOid),
            // Curve-correct, so the point is the only thing wrong with these parameters.
            D = new byte[EcTestSupport.CoordinateSize(curveOid)],
            Q = new ECPoint { X = new byte[xLength], Y = new byte[yLength] }
        };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    [Fact]
    public void EncodeToPkcs8_EcParametersDIsNull_ThrowsArgumentException()
    {
        var parameters = new ECParameters { Curve = ECCurve.NamedCurves.nistP256 };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    /// <summary>
    /// D is the key. The curve OID and the public point are both checked strictly, so leaving the
    /// private scalar unchecked emitted an RFC 5915 <c>ECPrivateKey</c> whose <c>privateKey</c>
    /// OCTET STRING was the wrong width for the curve named right next to it in the same structure:
    /// malformed, but plausible enough that the caller got no signal at all.
    /// </summary>
    [Theory]
    [InlineData(Oids.ECP256, 31)] // one byte short
    [InlineData(Oids.ECP256, 33)] // one byte long
    [InlineData(Oids.ECP256, 48)] // a P-384 scalar on a P-256 curve
    [InlineData(Oids.ECP384, 32)] // a P-256 scalar on a P-384 curve
    [InlineData(Oids.ECP384, 66)] // a P-521 scalar on a P-384 curve
    [InlineData(Oids.ECP521, 32)] // a P-256 scalar on a P-521 curve
    [InlineData(Oids.ECP521, 65)] // one byte short of P-521's 66
    [InlineData(Oids.ECP256, 0)] // empty
    public void EncodeToPkcs8_EcParametersPrivateScalarWrongLength_ThrowsArgumentException(
        string curveOid, int dLength)
    {
        var coordinateSize = EcTestSupport.CoordinateSize(curveOid);
        var parameters = new ECParameters
        {
            Curve = ECCurve.CreateFromValue(curveOid),
            D = new byte[dLength],
            // A well-formed point, so the scalar is the only thing wrong.
            Q = new ECPoint { X = new byte[coordinateSize], Y = new byte[coordinateSize] }
        };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    /// <summary>
    /// The scalar check must not need a public point to run: a private key encoded without the
    /// optional RFC 5915 <c>publicKey</c> field is exactly where a bad D has nothing else to trip on.
    /// </summary>
    [Theory]
    [InlineData(Oids.ECP256, 48)]
    [InlineData(Oids.ECP384, 32)]
    [InlineData(Oids.ECP521, 48)]
    public void EncodeToPkcs8_EcParametersPrivateScalarWrongLengthWithoutPublicPoint_ThrowsArgumentException(
        string curveOid, int dLength)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.CreateFromValue(curveOid),
            D = new byte[dLength]
        };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void EncodeToPkcs8_EcParametersPrivateScalarCurveSized_IsAccepted(string curveOid)
    {
        using var bcl = ECDsa.Create(EcTestSupport.NamedCurve(curveOid));
        var expected = bcl.ExportParameters(includePrivateParameters: true);
        Assert.Equal(EcTestSupport.CoordinateSize(curveOid), expected.D!.Length);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(expected);

        using var check = ECDsa.Create();
        check.ImportPkcs8PrivateKey(ourPkcs8, out _);
        Assert.Equal(expected.D, check.ExportParameters(includePrivateParameters: true).D);
    }

    /// <summary>
    /// The message must not leak the private scalar it just rejected.
    /// </summary>
    [Fact]
    public void EncodeToPkcs8_EcParametersPrivateScalarWrongLength_MessageDoesNotContainD()
    {
        var d = Convert.FromHexString("00112233445566778899AABBCCDDEEFF");
        var parameters = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = d };

        var exception = Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));

        Assert.DoesNotContain(Convert.ToHexString(d), exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EncodeToPkcs8_EcParametersCurveOidValueIsNull_ThrowsArgumentException()
    {
        var curve = ECCurve.CreateFromOid(new Oid(null, "not-a-real-curve"));
        var parameters = new ECParameters { Curve = curve, D = new byte[32] };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    #endregion

    #region EncodeToPkcs8(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>?, KeyType) - EC via raw values

    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void EncodeToPkcs8_RawEcKeyWithPublicPoint_BclImportsAndRoundTrips(string curveOid)
    {
        using var bcl = ECDsa.Create(EcTestSupport.NamedCurve(curveOid));
        var expected = bcl.ExportParameters(includePrivateParameters: true);
        var point = EcTestSupport.UncompressedPoint(expected.Q.X!, expected.Q.Y!);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(expected.D!, point, GetKeyType(curveOid));

        using var check = ECDsa.Create();
        check.ImportPkcs8PrivateKey(ourPkcs8, out var bytesRead);
        Assert.Equal(ourPkcs8.Length, bytesRead);

        var actual = check.ExportParameters(includePrivateParameters: true);
        Assert.Equal(curveOid, actual.Curve.Oid.Value);
        Assert.Equal(expected.D, actual.D);
        Assert.Equal(expected.Q.X, actual.Q.X);
        Assert.Equal(expected.Q.Y, actual.Q.Y);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x02)] // compressed, even Y
    [InlineData(0x03)] // compressed, odd Y
    [InlineData(0x06)] // hybrid
    [InlineData(0x40)]
    public void EncodeToPkcs8_RawEcKeyPublicPointWrongPrefix_ThrowsArgumentException(byte prefix)
    {
        var point = new byte[65]; // correct P-256 length, wrong prefix
        point[0] = prefix;

        Assert.Throws<ArgumentException>(() =>
            AsnPrivateKeyEncoder.EncodeToPkcs8(new byte[32], point, KeyType.ECP256));
    }

    [Theory]
    [InlineData(KeyType.ECP256, 64)] // one byte short
    [InlineData(KeyType.ECP256, 66)] // one byte long
    [InlineData(KeyType.ECP256, 97)] // a P-384 point on a P-256 key
    [InlineData(KeyType.ECP384, 65)] // a P-256 point on a P-384 key
    [InlineData(KeyType.ECP521, 97)] // a P-384 point on a P-521 key
    public void EncodeToPkcs8_RawEcKeyPublicPointWrongLength_ThrowsArgumentException(
        KeyType keyType, int pointLength)
    {
        var point = new byte[pointLength];
        point[0] = 0x04;

        Assert.Throws<ArgumentException>(() =>
            AsnPrivateKeyEncoder.EncodeToPkcs8(new byte[32], point, keyType));
    }

    [Fact]
    public void EncodeToPkcs8_RawEcKeyEmptyPublicPoint_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() =>
            AsnPrivateKeyEncoder.EncodeToPkcs8(new byte[32], ReadOnlyMemory<byte>.Empty, KeyType.ECP256));

    [Fact]
    public void EncodeToPkcs8_UnsupportedKeyType_ThrowsNotSupportedException() =>
        Assert.Throws<NotSupportedException>(() =>
            AsnPrivateKeyEncoder.EncodeToPkcs8(new byte[32], null, KeyType.RSA2048));

    #endregion

    #region EncodeToPkcs8(ReadOnlyMemory<byte>, KeyType) / Curve25519 - Ed25519 vs published oracle

    // RFC 8410 section 10.3, first Ed25519 private-key example.
    // https://www.rfc-editor.org/rfc/rfc8410.html#section-10.3
    private static readonly byte[] Rfc8410Ed25519RawPrivateKey = Convert.FromHexString(
        "D4EE72DBF913584AD5B6D8F1F769F8AD3AFE7C28CBF1D4FBE097A88F44755842");

    private static readonly byte[] Rfc8410Ed25519Pkcs8Der = Convert.FromBase64String(
        "MC4CAQAwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC");

    [Fact]
    public void EncodeToPkcs8_Ed25519RawKey_TwoArgOverload_ExactlyMatchesRfc8410PublishedDer()
    {
        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(Rfc8410Ed25519RawPrivateKey, KeyType.Ed25519);

        Assert.Equal(Rfc8410Ed25519Pkcs8Der, ourPkcs8);
    }

    [Fact]
    public void EncodeToPkcs8_Ed25519RawKey_ThreeArgOverload_IgnoresPublicPointAndMatchesRfc8410()
    {
        var arbitraryPublicPoint = new byte[] { 0xAA, 0xBB, 0xCC };

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(
            Rfc8410Ed25519RawPrivateKey, arbitraryPublicPoint, KeyType.Ed25519);

        Assert.Equal(Rfc8410Ed25519Pkcs8Der, ourPkcs8);
    }

    [Fact]
    public void EncodeToPkcs8_Curve25519Key_WrongLength_ThrowsArgumentExceptionWithPrivateKeyParamName()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            AsnPrivateKeyEncoder.EncodeToPkcs8(new byte[10], KeyType.Ed25519));

        Assert.Equal("privateKey", exception.ParamName);
    }

    // RFC 7748 section 6.1 Alice key, encoded by OpenSSL's evppkey_ecx.txt vector.
    private static readonly byte[] Rfc7748X25519AliceRawPrivateKey = Convert.FromHexString(
        "77076D0A7318A57D3C16C17251B26645DF4C2F87EBC0992AB177FBA51DB92C2A");

    private static readonly byte[] Rfc7748X25519AlicePkcs8Der = Convert.FromBase64String(
        "MC4CAQAwBQYDK2VuBCIEIHcHbQpzGKV9PBbBclGyZkXfTC+H68CZKrF3+6UduSwq");

    [Fact]
    public void EncodeToPkcs8_Rfc7748X25519AliceKey_MatchesOpenSslPkcs8Vector()
    {
        var encoded = AsnPrivateKeyEncoder.EncodeToPkcs8(
            Rfc7748X25519AliceRawPrivateKey,
            KeyType.X25519);

        Assert.Equal(Rfc7748X25519AlicePkcs8Der, encoded);
    }

    #endregion
}