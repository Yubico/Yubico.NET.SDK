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

/// <summary>Tests for <see cref="AsnPrivateKeyDecoder"/>.</summary>
/// <remarks>
/// EC and RSA vectors are checked against .NET cryptography. Curve25519 vectors come from
/// RFC 8410 section 10.3 and the OpenSSL encoding of RFC 7748 section 6.1 Alice key material.
/// </remarks>
public class AsnPrivateKeyDecoderTests
{
    private static ECCurve GetNamedCurve(string curveOid) => curveOid switch
    {
        Oids.ECP256 => ECCurve.NamedCurves.nistP256,
        Oids.ECP384 => ECCurve.NamedCurves.nistP384,
        Oids.ECP521 => ECCurve.NamedCurves.nistP521,
        _ => throw new ArgumentOutOfRangeException(nameof(curveOid))
    };

    private static void AssertEcParametersMatch(ECParameters expected, ECParameters actual, string expectedCurveOid)
    {
        Assert.Equal(expectedCurveOid, actual.Curve.Oid.Value);
        Assert.Equal(expected.D, actual.D);
        Assert.Equal(expected.Q.X, actual.Q.X);
        Assert.Equal(expected.Q.Y, actual.Q.Y);
    }

    #region CreateECParameters - Decoder vs BCL oracle (happy paths, all three curves)

    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void CreateECParameters_BclEncodedKey_MatchesBclExportedParameters(string curveOid)
    {
        using var bcl = ECDsa.Create(GetNamedCurve(curveOid));
        var pkcs8 = bcl.ExportPkcs8PrivateKey();
        var expected = bcl.ExportParameters(includePrivateParameters: true);

        var actual = AsnPrivateKeyDecoder.CreateECParameters(pkcs8);

        AssertEcParametersMatch(expected, actual, curveOid);
    }

    #endregion

    #region CreateECParameters - malformed vectors

    private static byte[] BuildEcPkcs8(
        string algorithmOid,
        string? curveOid,
        int ecVersion,
        byte[] privateKeyValue,
        byte[]? publicKeyPointBytes = null,
        int publicKeyUnusedBits = 0,
        bool includeParametersField = false,
        int pkcs8Version = 0,
        bool explicitPublicKeyTag = false)
    {
        var ecWriter = new AsnWriter(AsnEncodingRules.DER);
        ecWriter.PushSequence();
        ecWriter.WriteInteger(ecVersion);
        ecWriter.WriteOctetString(privateKeyValue);

        if (includeParametersField)
        {
            var parametersTag = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
            ecWriter.PushSequence(parametersTag);
            ecWriter.WriteObjectIdentifier(curveOid ?? Oids.ECP256);
            ecWriter.PopSequence(parametersTag);
        }

        if (publicKeyPointBytes is not null)
        {
            if (explicitPublicKeyTag)
            {
                // RFC 5915 form: constructed [1] wrapping a universal BIT STRING.
                var publicKeyTag = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
                ecWriter.PushSequence(publicKeyTag);
                ecWriter.WriteBitString(publicKeyPointBytes, publicKeyUnusedBits);
                ecWriter.PopSequence(publicKeyTag);
            }
            else
            {
                // Legacy form emitted by earlier releases of this SDK: primitive, implicit [1].
                var publicKeyTag = new Asn1Tag(TagClass.ContextSpecific, 1);
                ecWriter.WriteBitString(publicKeyPointBytes, publicKeyUnusedBits, publicKeyTag);
            }
        }

        ecWriter.PopSequence();
        var ecPrivateKeyBytes = ecWriter.Encode();

        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(pkcs8Version);

        writer.PushSequence();
        writer.WriteObjectIdentifier(algorithmOid);
        if (curveOid is not null)
        {
            writer.WriteObjectIdentifier(curveOid);
        }
        writer.PopSequence();

        writer.WriteOctetString(ecPrivateKeyBytes);
        writer.PopSequence();

        return writer.Encode();
    }

    [Fact]
    public void CreateECParameters_Pkcs8VersionOne_ThrowsValidButUnsupportedCryptographicException()
    {
        var pkcs8 = BuildEcPkcs8(Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue: new byte[32], pkcs8Version: 1);

        var exception = Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));

        Assert.Contains("valid but unsupported", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateECParameters_AlgorithmOidNotEcdsa_ThrowsInvalidOperationException()
    {
        var pkcs8 = BuildEcPkcs8(Oids.RSA, Oids.ECP256, ecVersion: 1, privateKeyValue: new byte[32]);

        Assert.Throws<InvalidOperationException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    [Fact]
    public void CreateECParameters_UnsupportedCurveOid_ThrowsInvalidOperationException()
    {
        var pkcs8 = BuildEcPkcs8(Oids.ECDSA, "1.2.3.4.5", ecVersion: 1, privateKeyValue: new byte[32]);

        Assert.Throws<InvalidOperationException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    [Fact]
    public void CreateECParameters_InnerEcVersionNotOne_ThrowsCryptographicException()
    {
        var pkcs8 = BuildEcPkcs8(Oids.ECDSA, Oids.ECP256, ecVersion: 2, privateKeyValue: new byte[32]);

        Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    [Fact]
    public void CreateECParameters_NoPublicKeyField_LoopNotEntered_QStaysDefault()
    {
        var privateKeyValue = new byte[32];
        Array.Fill(privateKeyValue, (byte)0x07);
        var pkcs8 = BuildEcPkcs8(Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue);

        var result = AsnPrivateKeyDecoder.CreateECParameters(pkcs8);

        Assert.Equal(privateKeyValue, result.D);
        Assert.Null(result.Q.X);
        Assert.Null(result.Q.Y);
    }

    [Fact]
    public void CreateECParameters_PublicKeyBitStringHasUnusedBits_ThrowsCryptographicException()
    {
        var point = new byte[65];
        point[0] = 0x04;
        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue: new byte[32],
            publicKeyPointBytes: point, publicKeyUnusedBits: 1);

        Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    [Fact]
    public void CreateECParameters_CompressedPointPrefix_ThrowsCryptographicException()
    {
        var compressedPoint = new byte[33];
        compressedPoint[0] = 0x02;
        var privateKeyValue = new byte[32];
        Array.Fill(privateKeyValue, (byte)0x09);
        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue,
            publicKeyPointBytes: compressedPoint);

        var exception = Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
        Assert.Equal("Unsupported EC point format", exception.Message);
    }

    [Fact]
    public void CreateECParameters_WrongLengthPublicPoint_ThrowsCryptographicException()
    {
        var wrongLengthPoint = new byte[11];
        wrongLengthPoint[0] = 0x04;
        var privateKeyValue = new byte[32];
        Array.Fill(privateKeyValue, (byte)0x0A);
        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue,
            publicKeyPointBytes: wrongLengthPoint);

        var exception = Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
        Assert.Equal("Invalid EC public key encoding", exception.Message);
    }

    [Fact]
    public void CreateECParameters_ZeroLengthPublicKeyBitString_ThrowsCryptographicException()
    {
        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue: new byte[32],
            publicKeyPointBytes: []);

        var exception = Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
        Assert.Equal("Invalid EC public key encoding", exception.Message);
    }

    [Theory]
    [InlineData(false)] // legacy implicit [1] BIT STRING
    [InlineData(true)] // RFC 5915 explicit [1] BIT STRING
    public void CreateECParameters_PublicKeyInEitherTagForm_DecodesToTheSamePoint(bool explicitPublicKeyTag)
    {
        using var bcl = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var expected = bcl.ExportParameters(includePrivateParameters: true);
        var point = new byte[65];
        point[0] = 0x04;
        expected.Q.X!.CopyTo(point, 1);
        expected.Q.Y!.CopyTo(point, 33);

        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, expected.D!,
            publicKeyPointBytes: point, explicitPublicKeyTag: explicitPublicKeyTag);

        var result = AsnPrivateKeyDecoder.CreateECParameters(pkcs8);

        AssertEcParametersMatch(expected, result, Oids.ECP256);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateECParameters_MalformedPointInEitherTagForm_ThrowsCryptographicException(
        bool explicitPublicKeyTag)
    {
        // A compressed point must be rejected regardless of how the [1] field is tagged.
        var compressedPoint = new byte[33];
        compressedPoint[0] = 0x02;

        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue: new byte[32],
            publicKeyPointBytes: compressedPoint, explicitPublicKeyTag: explicitPublicKeyTag);

        var exception = Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
        Assert.Equal("Unsupported EC point format", exception.Message);
    }

    [Fact]
    public void CreateECParameters_OptionalParametersFieldPresent_IsSkippedAndDecodesFine()
    {
        using var bcl = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var expected = bcl.ExportParameters(includePrivateParameters: true);
        var point = new byte[65];
        point[0] = 0x04;
        expected.Q.X!.CopyTo(point, 1);
        expected.Q.Y!.CopyTo(point, 33);

        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, expected.D!,
            publicKeyPointBytes: point, includeParametersField: true);

        var result = AsnPrivateKeyDecoder.CreateECParameters(pkcs8);

        AssertEcParametersMatch(expected, result, Oids.ECP256);
    }

    #endregion

    #region CreateRSAParameters - Decoder vs BCL oracle

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    public void CreateRSAParameters_BclEncodedKey_MatchesBclExportedParameters(int keySizeBits)
    {
        using var rsa = RSA.Create(keySizeBits);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var expected = rsa.ExportParameters(includePrivateParameters: true);

        var actual = AsnPrivateKeyDecoder.CreateRSAParameters(pkcs8);

        Assert.Equal(expected.Modulus, actual.Modulus);
        Assert.Equal(expected.Exponent, actual.Exponent);
        Assert.Equal(expected.D, actual.D);
        Assert.Equal(expected.P, actual.P);
        Assert.Equal(expected.Q, actual.Q);
        Assert.Equal(expected.DP, actual.DP);
        Assert.Equal(expected.DQ, actual.DQ);
        Assert.Equal(expected.InverseQ, actual.InverseQ);
    }

    private static byte[] BuildRsaPkcs8WithAlgorithmOid(string algorithmOid)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(0);
        writer.PushSequence();
        writer.WriteObjectIdentifier(algorithmOid);
        writer.PopSequence();
        writer.WriteOctetString([]);
        writer.PopSequence();
        return writer.Encode();
    }

    [Fact]
    public void CreateRSAParameters_Pkcs8VersionOne_ThrowsValidButUnsupportedCryptographicException()
    {
        using var rsa = RSA.Create(2048);
        var real = rsa.ExportPkcs8PrivateKey();
        var reader = new AsnReader(real, AsnEncodingRules.DER);
        var seq = reader.ReadSequence();
        _ = seq.ReadInteger();
        var algSeq = seq.ReadEncodedValue();
        var keyOctets = seq.ReadOctetString();

        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(1); // Valid RFC 5958 version value not supported by this decoder.
        writer.WriteEncodedValue(algSeq.Span);
        writer.WriteOctetString(keyOctets);
        writer.PopSequence();

        var exception = Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateRSAParameters(writer.Encode()));

        Assert.Contains("valid but unsupported", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateRSAParameters_AlgorithmOidNotRsa_ThrowsInvalidOperationException()
    {
        var pkcs8 = BuildRsaPkcs8WithAlgorithmOid(Oids.ECDSA);

        Assert.Throws<InvalidOperationException>(() => AsnPrivateKeyDecoder.CreateRSAParameters(pkcs8));
    }

    #endregion

    #region CreatePrivateKey - dispatches by algorithm OID

    [Fact]
    public void CreatePrivateKey_RsaPkcs8_ReturnsRsaPrivateKeyWithMatchingParameters()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var expected = rsa.ExportParameters(includePrivateParameters: true);

        var key = AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8);

        var rsaKey = Assert.IsType<RSAPrivateKey>(key);
        Assert.Equal(expected.Modulus, rsaKey.Parameters.Modulus);
        Assert.Equal(expected.D, rsaKey.Parameters.D);
    }

    [Fact]
    public void CreatePrivateKey_EcPkcs8_ReturnsEcPrivateKeyWithMatchingParameters()
    {
        using var bcl = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pkcs8 = bcl.ExportPkcs8PrivateKey();
        var expected = bcl.ExportParameters(includePrivateParameters: true);

        var key = AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8);

        var ecKey = Assert.IsType<ECPrivateKey>(key);
        Assert.Equal(expected.D, ecKey.Parameters.D);
        Assert.Equal(expected.Q.X, ecKey.Parameters.Q.X);
    }

    [Fact]
    public void CreatePrivateKey_Ed25519Pkcs8_ReturnsCurve25519PrivateKeyWithRfc8410Value()
    {
        var key = AsnPrivateKeyDecoder.CreatePrivateKey(Rfc8410Ed25519Pkcs8Der);

        var curveKey = Assert.IsType<Curve25519PrivateKey>(key);
        Assert.Equal(KeyType.Ed25519, curveKey.KeyType);
        Assert.Equal(Rfc8410Ed25519RawPrivateKey, curveKey.PrivateKey.ToArray());
    }

    [Fact]
    public void CreatePrivateKey_UnsupportedAlgorithmOid_ThrowsInvalidOperationException()
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(0);
        writer.PushSequence();
        writer.WriteObjectIdentifier("2.16.840.1.101.3.4.2.1"); // SHA-256 OID, not a key algorithm
        writer.PopSequence();
        writer.WriteOctetString([]);
        writer.PopSequence();

        Assert.Throws<InvalidOperationException>(() => AsnPrivateKeyDecoder.CreatePrivateKey(writer.Encode()));
    }

    [Fact]
    public void CreatePrivateKey_Pkcs8VersionOne_ThrowsValidButUnsupportedCryptographicException()
    {
        var pkcs8 = BuildCurve25519Pkcs8(Oids.Ed25519, new byte[32], pkcs8Version: 1);

        var exception = Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8));

        Assert.Contains("valid but unsupported", exception.Message, StringComparison.Ordinal);
    }

    #endregion

    #region Curve25519 - RFC 8410 section 10.3 Ed25519 vector (independent oracle)

    // RFC 8410 section 10.3, "Examples of Ed25519 Private Key" (first example, without the
    // public key field): https://www.rfc-editor.org/rfc/rfc8410.html#section-10.3
    // Base64 "MC4CAQAwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC" decodes to the
    // PKCS#8 DER below; the RFC states the raw private key value explicitly.
    private static readonly byte[] Rfc8410Ed25519Pkcs8Der = Convert.FromBase64String(
        "MC4CAQAwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC");

    private static readonly byte[] Rfc8410Ed25519RawPrivateKey = Convert.FromHexString(
        "D4EE72DBF913584AD5B6D8F1F769F8AD3AFE7C28CBF1D4FBE097A88F44755842");

    // RFC 7748 section 6.1 gives Alice's X25519 private key as a raw 32-byte scalar:
    // https://www.rfc-editor.org/rfc/rfc7748.html#section-6.1
    // OpenSSL's evppkey_ecx.txt test vectors ("X25519 test vectors (from RFC7748 6.1)")
    // encode that same scalar as an RFC 8410 PKCS#8 DER blob:
    // https://github.com/openssl/openssl/blob/master/test/recipes/30-test_evp_data/evppkey_ecx.txt
    // Verified by decoding the base64 below and confirming the trailing 32 bytes equal the raw
    // scalar from RFC 7748.
    private static readonly byte[] Rfc7748X25519AlicePkcs8Der = Convert.FromBase64String(
        "MC4CAQAwBQYDK2VuBCIEIHcHbQpzGKV9PBbBclGyZkXfTC+H68CZKrF3+6UduSwq");

    private static readonly byte[] Rfc7748X25519AliceRawPrivateKey = Convert.FromHexString(
        "77076D0A7318A57D3C16C17251B26645DF4C2F87EBC0992AB177FBA51DB92C2A");

    [Fact]
    public void GetCurve25519PrivateKeyData_Rfc8410Ed25519Vector_MatchesPublishedRawKey()
    {
        (var privateKey, var keyType) = AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(Rfc8410Ed25519Pkcs8Der);

        Assert.Equal(KeyType.Ed25519, keyType);
        Assert.Equal(Rfc8410Ed25519RawPrivateKey, privateKey);
    }

    [Fact]
    public void GetCurve25519PrivateKeyData_Rfc7748X25519AliceVector_MatchesPublishedRawKey()
    {
        (var privateKey, var keyType) = AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(Rfc7748X25519AlicePkcs8Der);

        Assert.Equal(KeyType.X25519, keyType);
        Assert.Equal(Rfc7748X25519AliceRawPrivateKey, privateKey);
    }

    [Fact]
    public void CreateCurve25519Key_Rfc8410Ed25519Vector_ReturnsMatchingKey()
    {
        using var key = AsnPrivateKeyDecoder.CreateCurve25519Key(Rfc8410Ed25519Pkcs8Der);

        Assert.Equal(KeyType.Ed25519, key.KeyType);
        Assert.Equal(Rfc8410Ed25519RawPrivateKey, key.PrivateKey.ToArray());
    }

    [Fact]
    public void CreateCurve25519Key_Rfc7748X25519AliceVector_ReturnsMatchingKey()
    {
        using var key = AsnPrivateKeyDecoder.CreateCurve25519Key(Rfc7748X25519AlicePkcs8Der);

        Assert.Equal(KeyType.X25519, key.KeyType);
        Assert.Equal(Rfc7748X25519AliceRawPrivateKey, key.PrivateKey.ToArray());
    }

    [Fact]
    public void CreateCurve25519Key_Pkcs8VersionOne_ThrowsValidButUnsupportedCryptographicException()
    {
        var pkcs8 = BuildCurve25519Pkcs8(Oids.Ed25519, new byte[32], pkcs8Version: 1);

        var exception = Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateCurve25519Key(pkcs8));

        Assert.Contains("valid but unsupported", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreatePrivateKey_Rfc7748X25519AliceVector_ReturnsMatchingKey()
    {
        var key = AsnPrivateKeyDecoder.CreatePrivateKey(Rfc7748X25519AlicePkcs8Der);
        using var disposableKey = Assert.IsAssignableFrom<IDisposable>(key);

        var curveKey = Assert.IsType<Curve25519PrivateKey>(key);
        Assert.Equal(KeyType.X25519, curveKey.KeyType);
        Assert.Equal(Rfc7748X25519AliceRawPrivateKey, curveKey.PrivateKey.ToArray());
    }

    #endregion

    #region GetCurve25519PrivateKeyData - malformed input branches

    private static byte[] BuildCurve25519Pkcs8(
        string algorithmOid,
        byte[] innerPrivateKeyOctets,
        int pkcs8Version = 0,
        bool wrapInnerAsOctetString = true)
    {
        byte[] innerEncoded;
        if (wrapInnerAsOctetString)
        {
            var innerWriter = new AsnWriter(AsnEncodingRules.DER);
            innerWriter.WriteOctetString(innerPrivateKeyOctets);
            innerEncoded = innerWriter.Encode();
        }
        else
        {
            innerEncoded = innerPrivateKeyOctets;
        }

        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(pkcs8Version);
        writer.PushSequence();
        writer.WriteObjectIdentifier(algorithmOid);
        writer.PopSequence();
        writer.WriteOctetString(innerEncoded);
        writer.PopSequence();
        return writer.Encode();
    }

    [Fact]
    public void GetCurve25519PrivateKeyData_Rfc5958VersionValueOne_IsValidButUnsupported()
    {
        var pkcs8 = BuildCurve25519Pkcs8(Oids.Ed25519, new byte[32], pkcs8Version: 1);

        var exception = Assert.Throws<CryptographicException>(() =>
            AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(pkcs8));
        Assert.Contains("valid but unsupported", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetCurve25519PrivateKeyData_UnsupportedAlgorithmOid_ThrowsArgumentException()
    {
        var pkcs8 = BuildCurve25519Pkcs8(Oids.RSA, new byte[32]);

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(pkcs8));
    }

    [Fact]
    public void GetCurve25519PrivateKeyData_InnerTagNotOctetString_ThrowsCryptographicException()
    {
        // The inner value is an INTEGER with one zero content octet, not an OCTET STRING.
        var badInner = new AsnWriter(AsnEncodingRules.DER);
        badInner.WriteInteger(0);

        var pkcs8 = BuildCurve25519Pkcs8(Oids.Ed25519, badInner.Encode(), wrapInnerAsOctetString: false);

        Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(pkcs8));
    }

    [Fact]
    public void GetCurve25519PrivateKeyData_InnerKeyWrongLength_ThrowsCryptographicException()
    {
        var pkcs8 = BuildCurve25519Pkcs8(Oids.Ed25519, new byte[16]); // not 32 bytes

        Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(pkcs8));
    }

    #endregion
}