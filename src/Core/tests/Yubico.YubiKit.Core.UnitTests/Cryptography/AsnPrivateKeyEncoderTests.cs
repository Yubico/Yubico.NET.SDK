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

/// <summary>Tests for <see cref="AsnPrivateKeyEncoder"/>.</summary>
/// <remarks>
/// RSA and EC output is checked against .NET cryptography. Curve25519 output is checked against
/// RFC 8410 and the OpenSSL encoding of the RFC 7748 section 6.1 Alice key material.
/// </remarks>
public class AsnPrivateKeyEncoderTests
{
    private static ECCurve GetNamedCurve(string curveOid) => curveOid switch
    {
        Oids.ECP256 => ECCurve.NamedCurves.nistP256,
        Oids.ECP384 => ECCurve.NamedCurves.nistP384,
        Oids.ECP521 => ECCurve.NamedCurves.nistP521,
        _ => throw new ArgumentOutOfRangeException(nameof(curveOid))
    };

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
    public void EncodeToPkcs8_EcParametersWithPublicPoint_BclImportThrowsCryptographicException(
        string curveOid)
    {
        // Known limitation: the optional public-key field uses implicit tagging, so a
        // standards-compliant importer rejects the encoded key.
        using var bcl = ECDsa.Create(GetNamedCurve(curveOid));
        var expected = bcl.ExportParameters(includePrivateParameters: true);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(expected);

        using var check = ECDsa.Create();
        Assert.Throws<CryptographicException>(() => check.ImportPkcs8PrivateKey(ourPkcs8, out _));
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

    [Fact]
    public void EncodeToPkcs8_EcParametersDIsNull_ThrowsArgumentException()
    {
        var parameters = new ECParameters { Curve = ECCurve.NamedCurves.nistP256 };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
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
    public void EncodeToPkcs8_RawEcKeyWithPublicPoint_BclImportThrowsCryptographicException(
        string curveOid)
    {
        // Known limitation: the optional public-key field uses implicit tagging, so a
        // standards-compliant importer rejects the encoded key.
        using var bcl = ECDsa.Create(GetNamedCurve(curveOid));
        var expected = bcl.ExportParameters(includePrivateParameters: true);
        var point = new byte[1 + expected.Q.X!.Length + expected.Q.Y!.Length];
        point[0] = 0x04;
        expected.Q.X.CopyTo(point, 1);
        expected.Q.Y.CopyTo(point, 1 + expected.Q.X.Length);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(expected.D!, point, GetKeyType(curveOid));

        using var check = ECDsa.Create();
        Assert.Throws<CryptographicException>(() => check.ImportPkcs8PrivateKey(ourPkcs8, out _));
    }

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