// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class OidsTests
{
    [Theory]
    [InlineData(KeyType.Rsa1024, Oids.Rsa, null)]
    [InlineData(KeyType.Rsa2048, Oids.Rsa, null)]
    [InlineData(KeyType.Rsa3072, Oids.Rsa, null)]
    [InlineData(KeyType.Rsa4096, Oids.Rsa, null)]
    [InlineData(KeyType.P256, Oids.EcPublicKey, Oids.P256)]
    [InlineData(KeyType.P384, Oids.EcPublicKey, Oids.P384)]
    [InlineData(KeyType.P521, Oids.EcPublicKey, Oids.P521)]
    [InlineData(KeyType.X25519, Oids.X25519, null)]
    [InlineData(KeyType.Ed25519, Oids.Ed25519, null)]
    [InlineData(KeyType.Aes128, Oids.Aes128Cbc, null)]
    [InlineData(KeyType.Aes192, Oids.Aes192Cbc, null)]
    [InlineData(KeyType.Aes256, Oids.Aes256Cbc, null)]
    [InlineData(KeyType.TripleDes, Oids.TripleDesCbc, null)]
    public void GetOidsByKeyType_MappedKeyType_ReturnsExpectedOidPair(
        KeyType keyType,
        string expectedAlgorithmOid,
        string? expectedCurveOid)
    {
        (string AlgorithmOid, string? CurveOid) result = Oids.GetOidsByKeyType(keyType);

        Assert.Equal(expectedAlgorithmOid, result.AlgorithmOid);
        Assert.Equal(expectedCurveOid, result.CurveOid);
    }

    [Fact]
    public void GetOidsByKeyType_UnsupportedKeyType_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => Oids.GetOidsByKeyType(KeyType.None));
}