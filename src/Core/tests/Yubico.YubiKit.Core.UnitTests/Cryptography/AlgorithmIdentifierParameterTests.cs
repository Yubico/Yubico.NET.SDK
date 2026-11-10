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
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class AlgorithmIdentifierParameterTests
{
    [Theory]
    [InlineData(Oids.Ed25519, "", true)]
    [InlineData(Oids.X25519, "", true)]
    [InlineData(Oids.Ed25519, "0500", false)] // NULL is not ABSENT (RFC 8410 section 3).
    [InlineData(Oids.X25519, "0500", false)]
    [InlineData(Oids.Ed25519, "020101", false)] // Arbitrary INTEGER parameter.
    [InlineData(Oids.X25519, "020101", false)]
    [InlineData(Oids.Ed25519, "05000500", false)] // Multiple parameter fields.
    [InlineData(Oids.X25519, "05000500", false)]
    public void Rfc8410_ParameterRuleAgreesAcrossPublicAndPrivateEntryPoints(
        string algorithmOid, string parametersHex, bool valid)
    {
        // RFC 8410 section 10.1 public value and section 10.3 seed. The X25519
        // variant uses the same fixed-width fixture to exercise decoding, not key agreement.
        var publicValue = Convert.FromHexString("19BF44096984CDFE8541BAC167DC3B96C85086AA30B6B6CB0C5C38AD703166E1");
        using var seed = new DisposableBufferHandle(Convert.FromHexString(
            "D4EE72DBF913584AD5B6D8F1F769F8AD3AFE7C28CBF1D4FBE097A88F44755842"));
        using var innerPrivateKey = new DisposableBufferHandle((byte[])[0x04, 0x20, .. seed.Data.Span]);
        var spki = EncodeKey(algorithmOid, parametersHex, publicValue, privateKey: false);
        using var pkcs8 = new DisposableBufferHandle(EncodeKey(algorithmOid, parametersHex, innerPrivateKey.Data.Span, privateKey: true));

        if (valid)
        {
            var publicKey = Assert.IsType<Curve25519PublicKey>(AsnPublicKeyDecoder.CreatePublicKey(spki));
            Assert.Equal(publicValue, publicKey.PublicPoint.ToArray());
            using var privateKey = Assert.IsType<Curve25519PrivateKey>(AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8.Data));
            Assert.True(CryptographicOperations.FixedTimeEquals(seed.Data.Span, privateKey.PrivateKey.Span));
            var (decodedValue, keyType) = AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(pkcs8.Data);
            using var decoded = new DisposableBufferHandle(decodedValue);
            Assert.Equal(publicKey.KeyType, keyType);
            Assert.True(CryptographicOperations.FixedTimeEquals(seed.Data.Span, decoded.Data.Span));
        }
        else
        {
            Assert.Throws<AsnContentException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));
            Assert.Throws<AsnContentException>(() =>
            {
                using var key = Assert.IsType<Curve25519PrivateKey>(AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8.Data));
            });
            Assert.Throws<AsnContentException>(() =>
            {
                var (decodedValue, _) = AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(pkcs8.Data);
                using var decoded = new DisposableBufferHandle(decodedValue);
            });
        }
    }

    [Theory]
    [InlineData("06082A8648CE3D030107", true)] // Exactly one P-256 named-curve OID.
    [InlineData("", false)]
    [InlineData("0500", false)]
    [InlineData("06082A8648CE3D0301070500", false)] // Curve plus trailing NULL.
    [InlineData("06082A8648CE3D03010706052B81040022", false)] // P-256 then P-384.
    public void OrdinaryEc_RequiresExactlyOneNamedCurveAcrossEntryPoints(string parametersHex, bool valid)
    {
        using var oracle = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki = ReplaceParameters(oracle.ExportSubjectPublicKeyInfo(), parametersHex, privateKey: false);
        using var originalPkcs8 = new DisposableBufferHandle(oracle.ExportPkcs8PrivateKey());
        using var pkcs8 = new DisposableBufferHandle(ReplaceParameters(originalPkcs8.Data, parametersHex, privateKey: true));

        if (valid)
        {
            var publicKey = Assert.IsType<ECPublicKey>(AsnPublicKeyDecoder.CreatePublicKey(spki));
            var importedPublicKey = ECPublicKey.CreateFromSubjectPublicKeyInfo(spki);
            Assert.Equal(publicKey.Parameters.Q.X, importedPublicKey.Parameters.Q.X);
            using var privateKey = Assert.IsType<ECPrivateKey>(AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8.Data));
            var parameters = AsnPrivateKeyDecoder.CreateECParameters(pkcs8.Data);
            try
            {
                Assert.Equal(publicKey.Parameters.Q.X, parameters.Q.X);
                Assert.True(CryptographicOperations.FixedTimeEquals(privateKey.Parameters.D, parameters.D));
                Assert.Equal(Oids.P256, parameters.Curve.Oid.Value);
            }
            finally { CryptographicOperations.ZeroMemory(parameters.D); }
        }
        else
        {
            Assert.Throws<AsnContentException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));
            Assert.ThrowsAny<CryptographicException>(() => ECPublicKey.CreateFromSubjectPublicKeyInfo(spki));
            Assert.Throws<AsnContentException>(() =>
            {
                using var key = Assert.IsType<ECPrivateKey>(AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8.Data));
            });
            Assert.Throws<AsnContentException>(() =>
            {
                var parameters = AsnPrivateKeyDecoder.CreateECParameters(pkcs8.Data);
                CryptographicOperations.ZeroMemory(parameters.D);
            });
        }
    }

    [Theory]
    [InlineData("", true)] // Existing input tolerance; encoder still writes NULL (RFC 8017 appendix A.1).
    [InlineData("0500", true)]
    [InlineData("020101", false)]
    [InlineData("05000500", false)]
    [InlineData("0500020101", false)]
    public void Rsa_AllowsOnlyAbsentOrSingleNullAcrossEntryPoints(string parametersHex, bool valid)
    {
        using var oracle = RSA.Create(1024);
        var spki = ReplaceParameters(oracle.ExportSubjectPublicKeyInfo(), parametersHex, privateKey: false);
        using var originalPkcs8 = new DisposableBufferHandle(oracle.ExportPkcs8PrivateKey());
        using var pkcs8 = new DisposableBufferHandle(ReplaceParameters(originalPkcs8.Data, parametersHex, privateKey: true));

        if (valid)
        {
            var publicKey = Assert.IsType<RSAPublicKey>(AsnPublicKeyDecoder.CreatePublicKey(spki));
            using var privateKey = Assert.IsType<RSAPrivateKey>(AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8.Data));
            var parameters = AsnPrivateKeyDecoder.CreateRSAParameters(pkcs8.Data);
            try
            {
                Assert.Equal(publicKey.Parameters.Modulus, parameters.Modulus);
                Assert.True(CryptographicOperations.FixedTimeEquals(privateKey.Parameters.D, parameters.D));
            }
            finally { ClearRsaPrivateParameters(parameters); }
        }
        else
        {
            Assert.Throws<AsnContentException>(() => AsnPublicKeyDecoder.CreatePublicKey(spki));
            Assert.Throws<AsnContentException>(() =>
            {
                using var key = Assert.IsType<RSAPrivateKey>(AsnPrivateKeyDecoder.CreatePrivateKey(pkcs8.Data));
            });
            Assert.Throws<AsnContentException>(() =>
            {
                var parameters = AsnPrivateKeyDecoder.CreateRSAParameters(pkcs8.Data);
                ClearRsaPrivateParameters(parameters);
            });
        }
    }

    private static byte[] ReplaceParameters(ReadOnlyMemory<byte> encodedKey, string parametersHex, bool privateKey)
    {
        var sequence = new AsnReader(encodedKey, AsnEncodingRules.DER).ReadSequence();
        if (privateKey) _ = sequence.ReadInteger();
        var algorithmOid = sequence.ReadSequence().ReadObjectIdentifier();
        var encodedValue = sequence.ReadEncodedValue();
        var writer = new AsnWriter(AsnEncodingRules.DER);
        try
        {
            using (writer.PushSequence())
            {
                if (privateKey) writer.WriteInteger(0);
                WriteAlgorithmIdentifier(writer, algorithmOid, parametersHex);
                writer.WriteEncodedValue(encodedValue.Span);
            }
            return writer.Encode();
        }
        finally { writer.Reset(); }
    }

    private static byte[] EncodeKey(string algorithmOid, string parametersHex, ReadOnlySpan<byte> keyValue, bool privateKey)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        try
        {
            using (writer.PushSequence())
            {
                if (privateKey) writer.WriteInteger(0);
                WriteAlgorithmIdentifier(writer, algorithmOid, parametersHex);
                if (privateKey) writer.WriteOctetString(keyValue);
                else writer.WriteBitString(keyValue);
            }
            return writer.Encode();
        }
        finally { writer.Reset(); }
    }

    private static void WriteAlgorithmIdentifier(AsnWriter writer, string algorithmOid, string parametersHex)
    {
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier(algorithmOid);
            var parameters = new AsnReader(Convert.FromHexString(parametersHex), AsnEncodingRules.DER);
            while (parameters.HasData) writer.WriteEncodedValue(parameters.ReadEncodedValue().Span);
        }
    }

    private static void ClearRsaPrivateParameters(RSAParameters parameters)
    {
        CryptographicOperations.ZeroMemory(parameters.D);
        CryptographicOperations.ZeroMemory(parameters.P);
        CryptographicOperations.ZeroMemory(parameters.Q);
        CryptographicOperations.ZeroMemory(parameters.DP);
        CryptographicOperations.ZeroMemory(parameters.DQ);
        CryptographicOperations.ZeroMemory(parameters.InverseQ);
    }
}