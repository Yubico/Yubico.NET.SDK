// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class KeyDefinitionsConsistencyTests
{
    [Fact]
    public void EquivalentCoseMetadata_GivesDefinitionsValueEquality()
    {
        var first = new KeyDefinition
        {
            KeyType = KeyType.Ed25519,
            CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Okp, CurveIdentifier = CoseEcCurve.Ed25519 }
        };
        var second = new KeyDefinition
        {
            KeyType = KeyType.Ed25519,
            CoseKeyDefinition = new CoseKeyDefinition { Type = CoseKeyType.Okp, CurveIdentifier = CoseEcCurve.Ed25519 }
        };
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void NoneAndUnknown_AreNotAsymmetric()
    {
        Assert.False(new KeyDefinition { KeyType = KeyType.None }.IsAsymmetric);
        Assert.False(new KeyDefinition { KeyType = (KeyType)999 }.IsAsymmetric);
    }

    [Fact]
    public void Registry_CannotBeMutatedThroughDictionaryInterface()
    {
        Assert.False(KeyDefinitions.All is Dictionary<KeyType, KeyDefinition>);
        if (KeyDefinitions.All is IDictionary<KeyType, KeyDefinition> dictionary)
        {
            Assert.True(dictionary.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => dictionary.Remove(KeyType.P256));
        }
    }

    [Theory]
    [InlineData(KeyType.P256, 1, 256, 32, "P-256", CoseKeyType.Ec2, CoseEcCurve.P256)]
    [InlineData(KeyType.P384, 2, 384, 48, "P-384", CoseKeyType.Ec2, CoseEcCurve.P384)]
    [InlineData(KeyType.P521, 3, 521, 66, "P-521", CoseKeyType.Ec2, CoseEcCurve.P521)]
    [InlineData(KeyType.X25519, 4, 256, 32, "X25519", CoseKeyType.Okp, CoseEcCurve.X25519)]
    [InlineData(KeyType.Ed25519, 5, 256, 32, "Ed25519", CoseKeyType.Okp, CoseEcCurve.Ed25519)]
    [InlineData(KeyType.Rsa1024, 6, 1024, 128, "RSA-1024", CoseKeyType.Rsa, null)]
    [InlineData(KeyType.Rsa2048, 7, 2048, 256, "RSA-2048", CoseKeyType.Rsa, null)]
    [InlineData(KeyType.Rsa3072, 8, 3072, 384, "RSA-3072", CoseKeyType.Rsa, null)]
    [InlineData(KeyType.Rsa4096, 9, 4096, 512, "RSA-4096", CoseKeyType.Rsa, null)]
    [InlineData(KeyType.TripleDes, 10, 192, 24, "Triple DES", null, null)]
    [InlineData(KeyType.Aes128, 11, 128, 16, "AES-128", CoseKeyType.Symmetric, null)]
    [InlineData(KeyType.Aes192, 12, 192, 24, "AES-192", CoseKeyType.Symmetric, null)]
    [InlineData(KeyType.Aes256, 13, 256, 32, "AES-256", CoseKeyType.Symmetric, null)]
    public void ConcreteVariants_HaveStableValuesAndRepresentationMetadata(
        KeyType type, int value, int bits, int bytes, string displayName, CoseKeyType? family, CoseEcCurve? curve)
    {
        var definition = type.GetKeyDefinition();
        Assert.Equal(value, (int)type);
        Assert.Equal(type, definition.KeyType);
        Assert.Same(definition, KeyDefinitions.All[type]);
        Assert.Equal(bits, type.GetKeySizeBits());
        Assert.Equal(bytes, type.GetKeySizeBytes());
        Assert.Equal(displayName, definition.DisplayName);
        Assert.Equal($"{displayName} ({bits} bits)", definition.ToString());
        Assert.Equal(family, type.GetCoseKeyType());
        Assert.Equal(curve, definition.CoseKeyDefinition?.CurveIdentifier);
        Assert.Equal(family is not null, type.HasCoseMapping());
        Assert.Equal((definition.AlgorithmOid, definition.CurveOid), Oids.GetOidsByKeyType(type));
        Assert.Equal(definition.AlgorithmOid, type.GetAlgorithmOid());
        Assert.Equal(definition.CurveOid, type.GetCurveOid());
        if (curve is not null)
        {
            Assert.Same(definition, KeyDefinitions.GetByCoseCurve(curve.Value));
        }
    }

    [Fact]
    public void Registry_ContainsExactlyTheSupportedConcreteVariants()
    {
        Assert.Equal(0, (int)KeyType.None);
        Assert.Equal(Enum.GetValues<KeyType>().Where(type => type != KeyType.None).Order(), KeyDefinitions.All.Keys.Order());
        Assert.Equal(4, KeyDefinitions.GetRsaKeyDefinitions().Count);
        Assert.Equal(5, KeyDefinitions.GetEcKeyDefinitions().Count);
        Assert.Throws<ArgumentException>(() => ((KeyType)999).GetCoseKeyType());
        Assert.Throws<ArgumentException>(() => KeyType.None.HasCoseMapping());
    }

    [Theory]
    [InlineData(KeyType.P256, true, true, false, false)]
    [InlineData(KeyType.P384, true, true, false, false)]
    [InlineData(KeyType.P521, true, true, false, false)]
    [InlineData(KeyType.X25519, true, false, false, false)]
    [InlineData(KeyType.Ed25519, true, false, false, false)]
    [InlineData(KeyType.Rsa2048, true, false, true, false)]
    [InlineData(KeyType.Aes256, false, false, false, true)]
    [InlineData(KeyType.TripleDes, false, false, false, true)]
    [InlineData(KeyType.None, false, false, false, false)]
    [InlineData((KeyType)999, false, false, false, false)]
    public void Classifications_DistinguishKeyFamiliesFromSigningAlgorithms(
        KeyType type, bool asymmetric, bool ecdsa, bool rsa, bool symmetric)
    {
        var definition = new KeyDefinition { KeyType = type };
        Assert.Equal(asymmetric, definition.IsAsymmetric);
        Assert.Equal(ecdsa, type.IsECDsa());
        Assert.Equal(rsa, definition.IsRsa);
        Assert.Equal(symmetric, definition.IsSymmetric);
        Assert.Equal(asymmetric && !rsa, definition.IsEllipticCurve);
    }

    [Theory]
    [InlineData(Oids.Rsa)]
    [InlineData(Oids.EcPublicKey)]
    public void SharedAlgorithmOid_DoesNotSelectConcreteVariant(string oid)
    {
        Assert.Throws<NotSupportedException>(() => KeyDefinitions.GetByOid(oid));
    }

    [Theory]
    [InlineData(CoseAlgorithmIdentifier.Es256, -7)]
    [InlineData(CoseAlgorithmIdentifier.Es384, -35)]
    [InlineData(CoseAlgorithmIdentifier.Es512, -36)]
    [InlineData(CoseAlgorithmIdentifier.EdDsa, -8)]
    [InlineData(CoseAlgorithmIdentifier.Rs256, -257)]
    [InlineData(CoseAlgorithmIdentifier.EcdhEsHkdf256, -25)]
    public void AlgorithmNames_PreserveRegisteredValues(CoseAlgorithmIdentifier algorithm, int value) =>
        Assert.Equal(value, (int)algorithm);

    [Fact]
    public void DefinitionEquality_IncludesEveryMetadataField()
    {
        var baseline = KeyDefinitions.P256;
        Assert.Equal(baseline, Copy(baseline));
        Assert.Equal(baseline.GetHashCode(), Copy(baseline).GetHashCode());
        Assert.NotEqual(baseline, Copy(baseline, keyType: KeyType.P384));
        Assert.NotEqual(baseline, Copy(baseline, bytes: 33));
        Assert.NotEqual(baseline, Copy(baseline, bits: 257));
        Assert.NotEqual(baseline, Copy(baseline, algorithmOid: Oids.Rsa));
        Assert.NotEqual(baseline, Copy(baseline, curveOid: Oids.P384));
        Assert.NotEqual(baseline, Copy(baseline, displayName: "another name"));
        Assert.NotEqual(baseline, Copy(baseline, cose: new CoseKeyDefinition { Type = CoseKeyType.Okp }));
        Assert.NotEqual(baseline, Copy(baseline, cose: new CoseKeyDefinition { Type = CoseKeyType.Ec2, CurveIdentifier = CoseEcCurve.P384 }));
    }

    private static KeyDefinition Copy(KeyDefinition source, KeyType? keyType = null, int? bytes = null,
        int? bits = null, string? algorithmOid = null, string? curveOid = null, string? displayName = null,
        CoseKeyDefinition? cose = null) => new()
        {
            KeyType = keyType ?? source.KeyType,
            LengthInBytes = bytes ?? source.LengthInBytes,
            LengthInBits = bits ?? source.LengthInBits,
            AlgorithmOid = algorithmOid ?? source.AlgorithmOid,
            CurveOid = curveOid ?? source.CurveOid,
            DisplayName = displayName ?? source.DisplayName,
            CoseKeyDefinition = cose ?? source.CoseKeyDefinition
        };
}