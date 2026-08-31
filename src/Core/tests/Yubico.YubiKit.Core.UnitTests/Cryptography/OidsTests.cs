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
    [InlineData(KeyType.RSA1024, Oids.RSA, null)]
    [InlineData(KeyType.RSA2048, Oids.RSA, null)]
    [InlineData(KeyType.RSA3072, Oids.RSA, null)]
    [InlineData(KeyType.RSA4096, Oids.RSA, null)]
    [InlineData(KeyType.ECP256, Oids.ECDSA, Oids.ECP256)]
    [InlineData(KeyType.ECP384, Oids.ECDSA, Oids.ECP384)]
    [InlineData(KeyType.ECP521, Oids.ECDSA, Oids.ECP521)]
    [InlineData(KeyType.X25519, Oids.X25519, null)]
    [InlineData(KeyType.Ed25519, Oids.Ed25519, null)]
    [InlineData(KeyType.AES128, Oids.AES128Cbc, null)]
    [InlineData(KeyType.AES192, Oids.AES192Cbc, null)]
    [InlineData(KeyType.AES256, Oids.AES256Cbc, null)]
    [InlineData(KeyType.TripleDES, Oids.TripleDESCbc, null)]
    public void GetOidsByKeyType_MappedKeyType_ReturnsExpectedOidPair(
        KeyType keyType,
        string expectedAlgorithmOid,
        string? expectedCurveOid)
    {
        (string AlgorithmOid, string? Curveoid) result = Oids.GetOidsByKeyType(keyType);

        Assert.Equal(expectedAlgorithmOid, result.AlgorithmOid);
        Assert.Equal(expectedCurveOid, result.Curveoid);
    }

    [Fact]
    public void GetOidsByKeyType_UnsupportedKeyType_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => Oids.GetOidsByKeyType(KeyType.None));
}