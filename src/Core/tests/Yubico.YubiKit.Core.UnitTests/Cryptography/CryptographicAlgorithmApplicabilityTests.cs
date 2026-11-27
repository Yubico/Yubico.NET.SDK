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
using static Yubico.YubiKit.Core.Cryptography.CryptographicDefinitions;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class CryptographicAlgorithmApplicabilityTests
{
    [Theory]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void FullySpecified25519Operations_DoNotFollowMathematicalCurveAlone(KeyType keyType)
    {
        var keyInfo = Keys.Get(keyType);
        var otherConstruction = new KeyInfo((KeyType)999, "Other construction", keyInfo.Family,
            keyInfo.KeySizeBits, keyInfo.KeySizeBytes, keyInfo.Curve);
        var algorithms = Algorithms.FindForKey(otherConstruction);

        Assert.DoesNotContain(Algorithms.Ed25519, algorithms);
        Assert.DoesNotContain(Algorithms.X25519, algorithms);
        Assert.Contains(keyType == KeyType.Ed25519 ? Algorithms.EdDsa : Algorithms.EcdhEsHkdf256, algorithms);
        Assert.Contains(keyType == KeyType.Ed25519 ? Algorithms.Ed25519 : Algorithms.X25519, Algorithms.FindForKey(keyInfo));
    }

    [Fact]
    public void P256_HasMultipleOperationsButNoIntrinsicAlgorithm()
    {
        var algorithms = Algorithms.FindForKey(Keys.P256);
        Assert.Contains(Algorithms.Ecdsa, algorithms);
        Assert.Contains(Algorithms.EcdsaSha256, algorithms);
        Assert.Contains(Algorithms.EcdsaSha384, algorithms);
        Assert.Contains(Algorithms.Ecdh, algorithms);
        Assert.Contains(Algorithms.EcdhEsHkdf256, algorithms);
        Assert.DoesNotContain(Algorithms.Ed25519, algorithms);
        Assert.DoesNotContain(Algorithms.RsaPssSha256, algorithms);
        Assert.DoesNotContain(Algorithms.Aes256Cbc, algorithms);
        // These assertions check structural applicability and the stored -7 mapping separately.
        // They do not establish that COSE or a consuming profile permits every listed curve/alg pair.
        Assert.Contains(Algorithms.EcdsaSha256, Algorithms.FindForKey(Keys.P384));
        Assert.Contains(Algorithms.EcdsaSha256, Algorithms.FindForKey(Keys.Secp256k1));
        Assert.Equal(-7, Algorithms.EcdsaSha256.CoseAlgorithmId);
        Assert.Null(Algorithms.Ecdsa.HashAlgorithm);
        Assert.Null(Algorithms.Ecdsa.AlgorithmOid);
    }

    [Fact]
    public void ConstructionsSizesAndPadding_AreIndependentConstraints()
    {
        Assert.Contains(Algorithms.EdDsa, Algorithms.FindForKey(Keys.Ed25519));
        Assert.DoesNotContain(Algorithms.Ecdsa, Algorithms.FindForKey(Keys.Ed25519));
        Assert.DoesNotContain(Algorithms.EcdhEsHkdf256, Algorithms.FindForKey(Keys.Ed25519));
        Assert.Contains(Algorithms.EcdhEsHkdf256, Algorithms.FindForKey(Keys.X25519));
        Assert.DoesNotContain(Algorithms.EdDsa, Algorithms.FindForKey(Keys.X25519));
        Assert.DoesNotContain(Algorithms.Aes128Cbc, Algorithms.FindForKey(Keys.Aes256));
        Assert.Contains(Algorithms.Aes256Cbc, Algorithms.FindForKey(Keys.Aes256));
        Assert.DoesNotContain(Algorithms.RsaPssSha512, Algorithms.FindForKey(Keys.Rsa1024));
        Assert.Contains(Algorithms.RsaPssSha512, Algorithms.FindForKey(Keys.Rsa2048));
        Assert.Equal(HashAlgorithmName.SHA512, Algorithms.RsaPssSha512.HashAlgorithm);
        Assert.Equal(HashAlgorithmName.SHA512, Algorithms.RsaPssSha512.MaskGenerationHashAlgorithm);
        Assert.Equal(64, Algorithms.RsaPssSha512.SaltLengthBytes);
        Assert.Equal(1, Algorithms.RsaPssSha512.TrailerField);
        Assert.Equal(CipherMode.CBC, Algorithms.Aes256Cbc.CipherMode);
        Assert.Equal(-65529, Algorithms.Aes256Cbc.CoseAlgorithmId);
        Assert.Equal(-19, Algorithms.Ed25519.CoseAlgorithmId);
    }

    [Fact]
    public void SharedOperationOid_ReturnsGenericAndParameterizedDefinitionsInStableOrder()
    {
        Assert.Equal([Algorithms.RsaPss, Algorithms.RsaPssSha256, Algorithms.RsaPssSha384, Algorithms.RsaPssSha512], Algorithms.FindByOid("1.2.840.113549.1.1.10"));
        Assert.Same(Algorithms.EcdsaSha256, Assert.Single(Algorithms.FindByOid("1.2.840.10045.4.3.2")));
        Assert.Equal(-8, Algorithms.EdDsa.CoseAlgorithmId);
        Assert.Equal("1.3.101.112", Algorithms.Ed25519.AlgorithmOid);
    }

    [Fact]
    public void ApplicabilityCollections_AreImmutableAndComparedByValue()
    {
        var first = new AlgorithmInfo("example", "Example", CryptographicOperation.Signature,
            [new(KeyFamily.EllipticCurve, CurveForm.ShortWeierstrass)], hashAlgorithm: HashAlgorithmName.SHA256);
        var second = new AlgorithmInfo("example", "Example", CryptographicOperation.Signature,
            [new(KeyFamily.EllipticCurve, CurveForm.ShortWeierstrass)], hashAlgorithm: HashAlgorithmName.SHA256);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotSame(first.Applicability, second.Applicability);
        Assert.Empty(first.Applicability.Clear());
        Assert.Single(first.Applicability);
        Assert.All(typeof(AlgorithmInfo).GetProperties(), property => Assert.False(property.CanWrite));
    }
}