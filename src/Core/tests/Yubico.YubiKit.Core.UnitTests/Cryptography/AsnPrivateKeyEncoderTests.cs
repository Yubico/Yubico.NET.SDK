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
/// Tests for the internal <see cref="AsnPrivateKeyEncoder"/>. It is reachable from this
/// assembly because <c>Directory.Build.targets</c> grants <c>InternalsVisibleTo</c> for
/// <c>Yubico.YubiKit.Core.UnitTests</c> to every non-test project, so no production seam
/// change is needed.
/// </summary>
/// <remarks>
/// Direction 2 of the BCL-anchored contract: our code encodes, and the BCL's own
/// <see cref="RSA.ImportPkcs8PrivateKey"/> / <see cref="ECDsa.ImportPkcs8PrivateKey"/> decodes
/// and must accept the bytes. The private helpers <c>EncodeECKey</c> and
/// <c>EncodeCurve25519Key</c> are exercised only through the public overloads listed in the
/// task scope; their accessibility is not widened.
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

    // NEWLY DISCOVERED DEFECT (not one of the three pre-identified in the task description) --
    // PINNED, NOT ENDORSED, NOT FIXED (production changes are out of scope for this task).
    //
    // RFC 5915's `publicKey [1] BIT STRING OPTIONAL` field uses EXPLICIT tagging by default
    // (the RFC's ASN.1 module does not declare IMPLICIT TAGS): the correct wire form is a
    // constructed context-tag-1 container (0xA1) wrapping a complete, ordinary universal BIT
    // STRING TLV (0x03 ...). This is exactly what the .NET BCL itself emits from
    // ECDsa.ExportPkcs8PrivateKey() and what any RFC 5915-compliant consumer (OpenSSL, other
    // PKCS#8 libraries, etc.) expects.
    //
    // AsnPrivateKeyEncoder.EncodeECKey instead writes this field with IMPLICIT tagging -- a
    // single Asn1Tag(TagClass.ContextSpecific, 1) with the default isConstructed:false passed
    // straight to WriteBitString -- producing a primitive 0x81 tag with no nested BIT STRING
    // TLV. The BCL's own ECDsa.ImportPkcs8PrivateKey rejects that byte layout outright with
    // CryptographicException("ASN1 corrupted data."). Confirmed directly: hand-building both
    // the EXPLICIT (accepted) and IMPLICIT (rejected) forms in isolation reproduces this
    // exactly, isolating the defect to the tagging style, independent of any other field.
    //
    // Because AsnPrivateKeyDecoder.CreateECParameters reads the private-key SEQUENCE under
    // AsnEncodingRules.BER, and BER's constructed-primitive-chunking rules for a
    // single-nested-TLV constructed BIT STRING happen to yield the same effective content as
    // an EXPLICIT wrapper, our own decoder can read BCL-produced (EXPLICIT) keys correctly
    // (see CreateECParameters_BclEncodedKey_MatchesBclExportedParameters) even though our own
    // encoder cannot produce them. This is a one-directional defect: any EC private key
    // exported by this SDK with its public point populated (the common case -- see
    // ECPrivateKey.CreateFromParameters/CreateFromEcdh, which are typically fed a full
    // ECParameters with Q already populated) is NOT importable by a standards-compliant
    // RFC 5915 consumer, including the .NET BCL's own ECDsa. Only when the public point is
    // omitted (see the "WithoutPublicPoint" test below) does the encoder's output happen to
    // still be importable.
    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void EncodeToPkcs8_EcParametersWithPublicPoint_BclRejectsImplicitlyTaggedPublicKeyField_PinnedNotEndorsed(
        string curveOid)
    {
        using var bcl = ECDsa.Create(GetNamedCurve(curveOid));
        var expected = bcl.ExportParameters(includePrivateParameters: true);

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(expected);

        using var check = ECDsa.Create();
        Assert.Throws<CryptographicException>(() => check.ImportPkcs8PrivateKey(ourPkcs8, out _));
    }

    // EncodeECKey's `publicPoint.HasValue` false branch: when ECParameters.Q is not populated,
    // the [1] public key field is omitted entirely from the RFC 5915 structure. The BCL still
    // accepts the resulting PKCS#8 (it derives the public point from D and the named curve).
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
        // A curve whose Oid has a FriendlyName but no Value -- constructible only via
        // ECCurve.CreateFromOid with a custom Oid, since the named-curve factories always
        // populate Value.
        var curve = ECCurve.CreateFromOid(new Oid(null, "not-a-real-curve"));
        var parameters = new ECParameters { Curve = curve, D = new byte[32] };

        Assert.Throws<ArgumentException>(() => AsnPrivateKeyEncoder.EncodeToPkcs8(parameters));
    }

    #endregion

    #region EncodeToPkcs8(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>?, KeyType) - EC via raw values

    // Same newly discovered EXPLICIT-vs-IMPLICIT [1] publicKey tagging defect pinned above,
    // reached this time through the raw-memory overload (which funnels into the same private
    // EncodeECKey helper). See the detailed comment on
    // EncodeToPkcs8_EcParametersWithPublicPoint_BclRejectsImplicitlyTaggedPublicKeyField_PinnedNotEndorsed.
    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void EncodeToPkcs8_RawEcKeyWithPublicPoint_BclRejectsImplicitlyTaggedPublicKeyField_PinnedNotEndorsed(
        string curveOid)
    {
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

    // Same RFC 8410 section 10.3 vector used in AsnPrivateKeyDecoderTests, restated here so this
    // file's oracle-anchoring is self-contained.
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

    // The three-arg overload's publicPoint parameter is ignored entirely for Curve25519 key
    // types (EncodeCurve25519Key never reads it), so passing an arbitrary non-null value here
    // must not change the output at all.
    [Fact]
    public void EncodeToPkcs8_Ed25519RawKey_ThreeArgOverload_IgnoresPublicPointAndMatchesRfc8410()
    {
        var arbitraryPublicPoint = new byte[] { 0xAA, 0xBB, 0xCC };

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(
            Rfc8410Ed25519RawPrivateKey, arbitraryPublicPoint, KeyType.Ed25519);

        Assert.Equal(Rfc8410Ed25519Pkcs8Der, ourPkcs8);
    }

    [Fact]
    public void EncodeToPkcs8_Curve25519Key_WrongLength_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() =>
            AsnPrivateKeyEncoder.EncodeToPkcs8(new byte[10], KeyType.Ed25519));

    // No X25519 oracle exists in the .NET BCL used by this repo (no Ed25519/X25519 types), and
    // no independently published vector combining a pre-clamped raw X25519 key with its PKCS#8
    // DER was found (the RFC 7748 test vectors used elsewhere in this suite are NOT
    // bit-clamped, and AsnUtilities.VerifyX25519PrivateKey -- invoked here via
    // EncodeCurve25519Key -- rejects unclamped keys). This test therefore verifies the produced
    // DER's ASN.1 *structure* independently, using System.Formats.Asn1.AsnReader (the BCL's own
    // ASN.1 parser) directly rather than our production AsnPrivateKeyDecoder, and only then adds
    // a same-code round trip through our decoder as a convenience check. The round trip alone
    // would not be sufficient per this task's bar; the AsnReader-based structural check is the
    // best available independent anchor for X25519 in the absence of a BCL crypto oracle.
    [Fact]
    public void EncodeToPkcs8_ClampedX25519Key_ProducesRfc8410CompliantDerStructure()
    {
        var clamped = new byte[32];
        Array.Fill(clamped, (byte)0x33);
        clamped[0] &= 0xF8;
        clamped[31] &= 0x7F;
        clamped[31] |= 0x40;

        var ourPkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(clamped, KeyType.X25519);

        var reader = new AsnReader(ourPkcs8, AsnEncodingRules.DER);
        var seq = reader.ReadSequence();
        Assert.Equal(0, seq.ReadInteger());
        var algorithmSeq = seq.ReadSequence();
        Assert.Equal(Oids.X25519, algorithmSeq.ReadObjectIdentifier());
        algorithmSeq.ThrowIfNotEmpty();
        var outerOctetString = seq.ReadOctetString();
        seq.ThrowIfNotEmpty();

        var innerReader = new AsnReader(outerOctetString, AsnEncodingRules.DER);
        var innerOctetString = innerReader.ReadOctetString();
        innerReader.ThrowIfNotEmpty();
        Assert.Equal(clamped, innerOctetString);

        // Convenience round trip through our own decoder (not itself an independent oracle).
        (var decoded, var keyType) = AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(ourPkcs8);
        Assert.Equal(KeyType.X25519, keyType);
        Assert.Equal(clamped, decoded);
    }

    #endregion
}