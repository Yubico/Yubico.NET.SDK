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
/// Tests for the internal <see cref="AsnPrivateKeyDecoder"/>. It is reachable from this
/// assembly because <c>Directory.Build.targets</c> grants <c>InternalsVisibleTo</c> for
/// <c>Yubico.YubiKit.Core.UnitTests</c> to every non-test project, so no production seam
/// change is needed.
/// </summary>
/// <remarks>
/// Every supported key type is anchored to an independent oracle: for EC and RSA this is the
/// .NET BCL's own PKCS#8 implementation (<see cref="ECDsa"/> / <see cref="RSA"/>). The .NET 10
/// BCL used by this repo has no Ed25519/X25519 oracle (no
/// <c>System.Security.Cryptography.Ed25519</c> / <c>X25519</c> types), so Curve25519 decode
/// tests are anchored instead to the published RFC 8410 &#167;10.3 Ed25519 vector and to an
/// OpenSSL <c>evppkey_ecx.txt</c> PKCS#8 encoding of the RFC 7748 &#167;6.1 X25519 "Alice" test
/// vector. A same-code round-trip is only ever used as a convenience check layered on top of
/// one of these oracle-anchored assertions, never as the sole assertion.
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

    #region CreateECParameters - malformed / branch coverage (AsnWriter-built vectors)

    private static byte[] BuildEcPkcs8(
        string algorithmOid,
        string? curveOid,
        int ecVersion,
        byte[] privateKeyValue,
        byte[]? publicKeyPointBytes = null,
        int publicKeyUnusedBits = 0,
        bool includeParametersField = false,
        int pkcs8Version = 0)
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
            var publicKeyTag = new Asn1Tag(TagClass.ContextSpecific, 1);
            ecWriter.WriteBitString(publicKeyPointBytes, publicKeyUnusedBits, publicKeyTag);
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

    // Line 150: PKCS#8 version must be 0.
    [Fact]
    public void CreateECParameters_Pkcs8VersionNotZero_ThrowsCryptographicException()
    {
        var pkcs8 = BuildEcPkcs8(Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue: new byte[32], pkcs8Version: 1);

        Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    // Line 157: algorithm OID must be Oids.ECDSA. The thrown message is one of the four known
    // broken "ExceptionMessages.UnsupportedAlgorithm));" literal sites (AsnPrivateKeyDecoder.cs
    // 159-162) -- an un-substituted resource placeholder, not real text. We assert exception
    // type only; asserting the message would pin a defect that a real fix must not regress
    // against.
    [Fact]
    public void CreateECParameters_AlgorithmOidNotEcdsa_ThrowsInvalidOperationException()
    {
        var pkcs8 = BuildEcPkcs8(Oids.RSA, Oids.ECP256, ecVersion: 1, privateKeyValue: new byte[32]);

        Assert.Throws<InvalidOperationException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    // Line 166: curve OID must be P-256/P-384/P-521. Broken message site
    // AsnPrivateKeyDecoder.cs 171-174 -- type only, see comment above.
    [Fact]
    public void CreateECParameters_UnsupportedCurveOid_ThrowsInvalidOperationException()
    {
        var pkcs8 = BuildEcPkcs8(Oids.ECDSA, "1.2.3.4.5", ecVersion: 1, privateKeyValue: new byte[32]);

        Assert.Throws<InvalidOperationException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    // Line 185: the inner RFC 5915 ECPrivateKey version must be 1.
    [Fact]
    public void CreateECParameters_InnerEcVersionNotOne_ThrowsCryptographicException()
    {
        var pkcs8 = BuildEcPkcs8(Oids.ECDSA, Oids.ECP256, ecVersion: 2, privateKeyValue: new byte[32]);

        Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    // Line 194: the optional-trailing-field while loop, NOT entered -- no [1] public key field
    // present at all. This is a same-code decode used only to pin a structural branch (the
    // loop terminates immediately because seqEcPrivateKey.HasData is false); the BCL happy-path
    // tests above already anchor the "loop entered" side against an independent oracle.
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

    // Line 200: BIT STRING unusedBits must be 0.
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

    // Line 206 -- KNOWN DEFECT, PINNED NOT ENDORSED: a public-point prefix other than 0x04
    // (e.g. a compressed point) silently skips the "if" and leaves Q at default (X=null,
    // Y=null) instead of throwing. AsnPublicKeyDecoder.cs:116-119 throws
    // CryptographicException("Unsupported EC point format") for exactly the same condition, so
    // this decoder is inconsistent with its sibling. Fixing this is a behaviour change for a
    // separate reviewed PR; this test only pins the current silent behaviour.
    [Fact]
    public void CreateECParameters_CompressedPointPrefix_SilentlyLeavesQDefault_PinnedNotEndorsed()
    {
        var compressedPoint = new byte[33];
        compressedPoint[0] = 0x02; // compressed point format, not handled
        var privateKeyValue = new byte[32];
        Array.Fill(privateKeyValue, (byte)0x09);
        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue,
            publicKeyPointBytes: compressedPoint);

        var result = AsnPrivateKeyDecoder.CreateECParameters(pkcs8);

        Assert.Equal(privateKeyValue, result.D);
        Assert.Null(result.Q.X);
        Assert.Null(result.Q.Y);
    }

    // Line 210 -- KNOWN DEFECT, PINNED NOT ENDORSED: a public point whose length does not match
    // 2*coordinateSize+1 is silently ignored (Q stays default) rather than throwing. Same shape
    // and same treatment as the compressed-point case above.
    [Fact]
    public void CreateECParameters_WrongLengthPublicPoint_SilentlyLeavesQDefault_PinnedNotEndorsed()
    {
        var wrongLengthPoint = new byte[11];
        wrongLengthPoint[0] = 0x04;
        var privateKeyValue = new byte[32];
        Array.Fill(privateKeyValue, (byte)0x0A);
        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue,
            publicKeyPointBytes: wrongLengthPoint);

        var result = AsnPrivateKeyDecoder.CreateECParameters(pkcs8);

        Assert.Equal(privateKeyValue, result.D);
        Assert.Null(result.Q.X);
        Assert.Null(result.Q.Y);
    }

    // Noted but not pinned as its own defect test per task scope: publicKeyBytes.Span[0] is
    // indexed without a length check at line 206, so a zero-length BIT STRING throws
    // IndexOutOfRangeException instead of a CryptographicException. Pinned here anyway since it
    // is cheap and documents the exact failure mode.
    [Fact]
    public void CreateECParameters_ZeroLengthPublicKeyBitString_ThrowsIndexOutOfRangeException()
    {
        var pkcs8 = BuildEcPkcs8(
            Oids.ECDSA, Oids.ECP256, ecVersion: 1, privateKeyValue: new byte[32],
            publicKeyPointBytes: []);

        Assert.Throws<IndexOutOfRangeException>(() => AsnPrivateKeyDecoder.CreateECParameters(pkcs8));
    }

    // Line 226: an optional [0] parameters field (RFC 5915 EXPLICIT tag) is present and must be
    // skipped via ReadEncodedValue() without disturbing decoding of the surrounding fields.
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
    public void CreateRSAParameters_Pkcs8VersionNotZero_ThrowsCryptographicException()
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
        writer.WriteInteger(1); // invalid version
        writer.WriteEncodedValue(algSeq.Span);
        writer.WriteOctetString(keyOctets);
        writer.PopSequence();

        Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.CreateRSAParameters(writer.Encode()));
    }

    // Broken message site AsnPrivateKeyDecoder.cs 257-260 -- type only, see comment on the EC
    // "AlgorithmOidNotEcdsa" test above for why the message is not pinned.
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

    // Line 78-81: no case in the switch matches -- broken message site, type only.
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

    // GetCurve25519PrivateKeyData performs no RFC 7748 bit-clamping validation (only the Curve25519
    // decoder's higher-level factories do, via the Curve25519PrivateKey constructor), so the
    // unclamped RFC 7748 raw scalar decodes here without throwing.
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
        // Ed25519 does not go through AsnUtilities.VerifyX25519PrivateKey's bit-clamping check,
        // so the RFC 8410 vector can be used directly through this higher-level factory too.
        using var key = AsnPrivateKeyDecoder.CreateCurve25519Key(Rfc8410Ed25519Pkcs8Der);

        Assert.Equal(KeyType.Ed25519, key.KeyType);
        Assert.Equal(Rfc8410Ed25519RawPrivateKey, key.PrivateKey.ToArray());
    }

    // Curve25519PrivateKey's constructor calls AsnUtilities.VerifyX25519PrivateKey for X25519
    // keys, and the published RFC 7748 scalar above is not bit-clamped, so it cannot be used to
    // exercise CreateCurve25519Key/CreatePrivateKey for X25519 (only the lower-level
    // GetCurve25519PrivateKeyData, tested above, skips that check). No independently published
    // X25519 vector with pre-clamped bytes was found, so this uses a locally constructed,
    // properly clamped key and a same-code round trip through the encoder to build valid PKCS#8
    // input -- documented here as round-trip-only, not oracle-anchored.
    [Fact]
    public void CreateCurve25519Key_ClampedX25519Value_RoundTripsThroughEncoder()
    {
        var clamped = new byte[32];
        Array.Fill(clamped, (byte)0x22);
        clamped[0] &= 0xF8;
        clamped[31] &= 0x7F;
        clamped[31] |= 0x40;

        var pkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(clamped, KeyType.X25519);
        using var key = AsnPrivateKeyDecoder.CreateCurve25519Key(pkcs8);

        Assert.Equal(KeyType.X25519, key.KeyType);
        Assert.Equal(clamped, key.PrivateKey.ToArray());
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
    public void GetCurve25519PrivateKeyData_Pkcs8VersionNotZero_ThrowsCryptographicException()
    {
        var pkcs8 = BuildCurve25519Pkcs8(Oids.Ed25519, new byte[32], pkcs8Version: 1);

        Assert.Throws<CryptographicException>(() => AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(pkcs8));
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
        // Inner content is a bare INTEGER instead of the expected [UNIVERSAL 4] OCTET STRING.
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