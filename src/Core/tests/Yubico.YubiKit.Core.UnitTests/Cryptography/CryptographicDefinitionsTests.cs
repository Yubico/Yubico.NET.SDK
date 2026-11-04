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
using Yubico.YubiKit.Core.Cryptography.Cose;
using static Yubico.YubiKit.Core.Cryptography.CryptographicDefinitions;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class CryptographicDefinitionsTests
{
    [Fact]
    public void CurveRelationships_AllowMultipleConstructionsWithoutAmbiguousInference()
    {
        var otherConstruction = new KeyInfo((KeyType)999, "Other P-256 construction", KeyFamily.EllipticCurve,
            256, 32, Curves.P256, new("1.2.3.4.5", Asn1ParameterKind.Absent));
        var index = Keys.CreateCurveIndex(Keys.All.Add(otherConstruction));
        var matches = index[Curves.P256.Id];

        Assert.Equal([Keys.P256, otherConstruction], matches);
        Assert.Contains(Curves.P256.Id, Assert.Throws<InvalidOperationException>(() =>
            Keys.ResolveByCurve(Curves.P256, matches)).Message, StringComparison.Ordinal);
        Assert.Same(Keys.P256, Keys.ResolveByCurve(Curves.P256, matches, Oids.EcPublicKey));
        Assert.Same(otherConstruction, Keys.ResolveByCurve(Curves.P256, matches, "1.2.3.4.5"));
        Assert.Equal([Keys.P256], Keys.FindByCurve(Curves.P256)); // No global registration.
        Assert.Same(Keys.P256, Keys.GetByCurve(Curves.P256));
        var unmodeled = new CurveInfo("unmodeled", "Unmodeled", CurveForm.Edwards, 255, 253, 32);
        Assert.Empty(Keys.FindByCurve(unmodeled));
        Assert.Throws<NotSupportedException>(() => Keys.GetByCurve(unmodeled));
    }

    [Fact]
    public void Catalog_HasSeparateKeyCurveAndAlgorithmRoutes()
    {
        Assert.Same(Keys.P256, Keys.Get(KeyType.P256));
        Assert.Same(Curves.P256, Keys.P256.Curve);
        Assert.Same(Curves.P256, Curves.Get(Curves.P256.Id));
        Assert.Same(Algorithms.EcdsaSha256, Algorithms.Get(Algorithms.EcdsaSha256.Id));
        Assert.Equal(Enum.GetValues<KeyType>().Where(keyType => keyType != KeyType.None), Keys.All.Select(keyInfo => keyInfo.KeyType));
        Assert.Equal(Keys.All.Count, Keys.All.Distinct().Count());
        Assert.All(Keys.All, keyInfo => Assert.Same(keyInfo, Keys.Get(keyInfo.KeyType)));
        Assert.All(Curves.All, curveInfo => Assert.Same(curveInfo, Curves.Get(curveInfo.Id)));
        Assert.All(Algorithms.All, algorithmInfo => Assert.Same(algorithmInfo, Algorithms.Get(algorithmInfo.Id)));
    }

    [Fact]
    public void Curve25519PublicKey_DoesNotMistakeEqualSizedKeysFor25519()
    {
        Assert.Throws<ArgumentException>(() => Curve25519PublicKey.CreateFromValue(new byte[32], KeyType.P256));
        Assert.Throws<ArgumentException>(() => Curve25519PublicKey.CreateFromValue(new byte[32], KeyType.Aes256));
    }

    [Theory]
    [InlineData(Oids.X25519)]
    [InlineData(Oids.Ed25519)]
    [InlineData(Oids.Aes256Cbc)]
    public void EcParameters_RejectAlgorithmOidsInNamedCurvePosition(string curveOid)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.CreateFromValue(curveOid),
            Q = new ECPoint { X = new byte[32], Y = new byte[32] }
        };
        Assert.Throws<NotSupportedException>(() => ECPublicKey.CreateFromParameters(parameters));
    }

    [Fact]
    public void OidDomains_DoNotSelectKeysBySizeOrAlgorithmAlone()
    {
        Assert.Same(Curves.P256, Curves.GetByOid("1.2.840.10045.3.1.7"));
        Assert.False(Curves.TryGetByOid(Oids.EcPublicKey, out var curveInfo));
        Assert.Null(curveInfo);
        Assert.False(Curves.TryGetByOid(Oids.Rsa, out _));
        Assert.False(Curves.TryGetByOid(Oids.X25519, out _));
        Assert.Empty(Algorithms.FindByOid(Oids.P256));
        Assert.Empty(Algorithms.FindByOid(Oids.EcPublicKey));
        Assert.Empty(Algorithms.FindByOid(Oids.Rsa));
        Assert.Same(Keys.Rsa2048, Keys.GetByRsaLength(2048));
        Assert.Same(Keys.Rsa2048, Keys.GetByRsaLength(2047));
        Assert.Equal(Keys.P256.KeySizeBytes, Keys.Secp256k1.KeySizeBytes);
        Assert.NotEqual(Keys.P256.Curve, Keys.Secp256k1.Curve);
        Assert.Null(Keys.Rsa2048.Curve);
        Assert.Null(Keys.Aes256.Curve);
        Assert.Null(Keys.Aes256.Asn1);
        Assert.Equal(Oids.Aes256Cbc, Algorithms.Aes256Cbc.AlgorithmOid);
        Assert.Equal(Asn1ParameterKind.Null, Keys.Rsa2048.Asn1?.ParameterKind);
        Assert.Equal(Asn1ParameterKind.NamedCurve, Keys.P256.Asn1?.ParameterKind);
        Assert.Equal(Oids.EcPublicKey, Keys.P256.Asn1?.AlgorithmOid);
        Assert.Equal(Oids.P256, Keys.P256.Asn1?.NamedCurveOid);
    }

    [Fact]
    public void Curve25519_MetadataSeparatesFieldOrderAndRepresentation()
    {
        Assert.Equal(255, Curves.Curve25519.FieldSizeBits);
        Assert.Equal(253, Curves.Curve25519.OrderSizeBits);
        Assert.Equal(32, Keys.X25519.KeySizeBytes);
        Assert.Equal(256, Keys.X25519.KeySizeBits);
        Assert.NotEqual(Keys.X25519.Curve, Keys.Ed25519.Curve);
        Assert.Equal(CurveForm.Montgomery, Keys.X25519.Curve?.Form);
        Assert.Equal(CurveForm.Edwards, Keys.Ed25519.Curve?.Form);
        Assert.Null(Curves.Curve25519.NamedCurveOid);
        Assert.Null(Keys.X25519.Asn1?.NamedCurveOid);
        Assert.Equal(Asn1ParameterKind.Absent, Keys.X25519.Asn1?.ParameterKind);
    }

    [Fact]
    public void Misses_HaveNullableFlowAndContextualErrors()
    {
        Assert.False(Keys.TryGet(KeyType.None, out var keyInfo));
        Assert.Null(keyInfo);
        Assert.False(Curves.TryGet("future-curve", out var curveInfo));
        Assert.Null(curveInfo);
        Assert.False(Algorithms.TryGet("future-algorithm", out var algorithmInfo));
        Assert.Null(algorithmInfo);
        Assert.Empty(Algorithms.FindByOid("1.2.3.4.5"));
        Assert.Contains("None", Assert.Throws<ArgumentException>(() => Keys.Get(KeyType.None)).Message, StringComparison.Ordinal);
        Assert.Contains("future-curve", Assert.Throws<NotSupportedException>(() => Curves.Get("future-curve")).Message, StringComparison.Ordinal);
        Assert.Contains("1.2.3.4.5", Assert.Throws<NotSupportedException>(() => Curves.GetByOid("1.2.3.4.5")).Message, StringComparison.Ordinal);
        Assert.Contains("future-algorithm", Assert.Throws<NotSupportedException>(() => Algorithms.Get("future-algorithm")).Message, StringComparison.Ordinal);
        Assert.False(KeyType.None.IsAsymmetric());
        Assert.False(((KeyType)999).IsAsymmetric());
    }

    [Fact]
    public void ImmutableCollectionsAndNestedRecords_HaveConsistentValueSemantics()
    {
        var keyInfo = Keys.P256;
        var curveInfo = new CurveInfo("p-256", "P-256", CurveForm.ShortWeierstrass, 256, 256, 32, Oids.P256);
        var copy = new KeyInfo(KeyType.P256, "P-256", KeyFamily.EllipticCurve, 256, 32, curveInfo,
            new(Oids.EcPublicKey, Asn1ParameterKind.NamedCurve, Oids.P256), new(CoseKeyType.Ec2, CoseEcCurve.P256), 32, 65);
        Assert.Equal(keyInfo, copy);
        Assert.Equal(keyInfo.GetHashCode(), copy.GetHashCode());
        Assert.NotSame(keyInfo.Curve, copy.Curve);
        Assert.NotEqual(keyInfo, Keys.Secp256k1);
        Assert.Equal(Keys.All.Count - 1, Keys.All.Remove(keyInfo).Count);
        Assert.Contains(keyInfo, Keys.All);
        Assert.All(typeof(KeyInfo).GetProperties(), property => Assert.False(property.CanWrite));
        Assert.All(typeof(CurveInfo).GetProperties(), property => Assert.False(property.CanWrite));
        Assert.All(typeof(Asn1KeyInfo).GetProperties(), property => Assert.False(property.CanWrite));
        Assert.All(typeof(CoseKeyInfo).GetProperties(), property => Assert.False(property.CanWrite));
        Assert.Equal(8, (int)CoseEcCurve.Secp256k1);
        Assert.Equal(256, (int)CoseEcCurve.BrainpoolP256r1);
        Assert.Equal(CoseEcCurve.BrainpoolP256r1, Keys.BrainpoolP256r1.Cose?.CurveIdentifier);
        Assert.Equal(258, (int)CoseEcCurve.BrainpoolP384r1);
        Assert.Equal(259, (int)CoseEcCurve.BrainpoolP512r1);
    }

    [Fact]
    public void IndexExtension_RequiresOnlyDataAndRejectsDuplicateIdentity()
    {
        var extra = new CurveInfo("test-curve", "Test", CurveForm.ShortWeierstrass, 256, 256, 32, "1.2.3.4.5");
        var extended = Curves.All.Add(extra);
        var index = CryptographicDefinitions.CreateIndex(extended, curveInfo => curveInfo.Id);
        Assert.Same(extra, index["test-curve"]);
        Assert.Same(Curves.P256, index[Curves.P256.Id]);
        Assert.False(Curves.TryGet("test-curve", out _)); // no mutable global registration
        Assert.Throws<InvalidOperationException>(() => CryptographicDefinitions.CreateIndex(extended.Add(extra), curveInfo => curveInfo.Id));
    }

    [Fact]
    public void NamedProperties_AreTheIndexedInstancesAndAllCannotBeMutated()
    {
        foreach (var property in typeof(Keys).GetProperties().Where(property => property.PropertyType == typeof(KeyInfo)))
        {
            var keyInfo = Assert.IsType<KeyInfo>(property.GetValue(null));
            Assert.Same(keyInfo, Keys.Get(keyInfo.KeyType));
        }
        foreach (var property in typeof(Curves).GetProperties().Where(property => property.PropertyType == typeof(CurveInfo)))
        {
            var curveInfo = Assert.IsType<CurveInfo>(property.GetValue(null));
            Assert.Same(curveInfo, Curves.Get(curveInfo.Id));
        }
        foreach (var property in typeof(Algorithms).GetProperties().Where(property => property.PropertyType == typeof(AlgorithmInfo)))
        {
            var algorithmInfo = Assert.IsType<AlgorithmInfo>(property.GetValue(null));
            Assert.Same(algorithmInfo, Algorithms.Get(algorithmInfo.Id));
        }
        IList<KeyInfo> list = Keys.All;
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list[0] = Keys.Rsa2048);
        Assert.Same(Keys.P256, Keys.All[0]);
    }

    [Theory]
    [InlineData(KeyType.P256, 1, 256, 32)]
    [InlineData(KeyType.P384, 2, 384, 48)]
    [InlineData(KeyType.P521, 3, 521, 66)]
    [InlineData(KeyType.X25519, 4, 256, 32)]
    [InlineData(KeyType.Ed25519, 5, 256, 32)]
    [InlineData(KeyType.Rsa1024, 6, 1024, 128)]
    [InlineData(KeyType.Rsa2048, 7, 2048, 256)]
    [InlineData(KeyType.Rsa3072, 8, 3072, 384)]
    [InlineData(KeyType.Rsa4096, 9, 4096, 512)]
    [InlineData(KeyType.TripleDes, 10, 192, 24)]
    [InlineData(KeyType.Aes128, 11, 128, 16)]
    [InlineData(KeyType.Aes192, 12, 192, 24)]
    [InlineData(KeyType.Aes256, 13, 256, 32)]
    [InlineData(KeyType.Secp256k1, 14, 256, 32)]
    [InlineData(KeyType.BrainpoolP256r1, 15, 256, 32)]
    [InlineData(KeyType.BrainpoolP384r1, 16, 384, 48)]
    [InlineData(KeyType.BrainpoolP512r1, 17, 512, 64)]
    public void ConcreteKeyIdsAndSizes_AreStable(KeyType keyType, int id, int keySizeBits, int keySizeBytes)
    {
        Assert.Equal(id, (int)keyType);
        Assert.Equal(keySizeBits, keyType.GetKeySizeBits());
        Assert.Equal(keySizeBytes, keyType.GetKeySizeBytes());
    }
}