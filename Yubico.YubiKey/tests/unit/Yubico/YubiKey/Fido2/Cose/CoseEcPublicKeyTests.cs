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

using System;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2.Cose
{
    public class CoseEcPublicKeyTests
    {
        [Theory]
        [InlineData(Oids.ECP256)]
        [InlineData(Oids.ECP384)]
        [InlineData(Oids.ECP521)]
        public void Encoding_Decoding_Key_Returns_ExpectedValues(
            string oid)
        {
            // Arrange
            var ecDsa = ECDsa.Create(ECCurve.CreateFromOid(Oid.FromOidValue(oid, OidGroup.PublicKeyAlgorithm)));
            var publicKey = ecDsa.ExportParameters(false);
            var coseKey = new CoseEcPublicKey(
                CoseEcCurve.P384,
                CoseAlgorithmIdentifier.ES384,
                publicKey.Q.X,
                publicKey.Q.Y);

            // Act
            var coseEncodedKey = coseKey.Encode(); // Encode
            var coseKey2 = new CoseEcPublicKey(coseEncodedKey); // Decode
            var publicKey2 = coseKey2.ToEcParameters();

            // Assert
            // The EC parameter values should be the same between the two keys.
            Assert.Equal(publicKey.Q.X, publicKey2.Q.X);
            Assert.Equal(publicKey.Q.Y, publicKey2.Q.Y);
        }

        [Theory]
        [InlineData(Oids.ECP256)]
        [InlineData(Oids.ECP384)]
        [InlineData(Oids.ECP521)]
        public void Constructor_with_EcParameters(
            string oid)
        {
            var ecDsa = ECDsa.Create(ECCurve.CreateFromOid(Oid.FromOidValue(oid, OidGroup.PublicKeyAlgorithm)));
            var publicKeyParams = ecDsa.ExportParameters(false);

            // Test EC Parameters constructor
            var coseKey = new CoseEcPublicKey(publicKeyParams);
            Assert.True(coseKey.XCoordinate.Span.SequenceEqual(publicKeyParams.Q.X));
        }

        [Fact]
        public void Create_WithEsp256Key_ReturnsEcPublicKey()
        {
            byte[] encodedKey = HexToBytes(
                "a5010203282001215820a5fd5ce1b1c458c530a54fa61b31bf6b04be8b97afde54dd8cbb69275a8a1be1" +
                "225820fa3a3231dd9deed9d1897be5a6228c59501e4bcd12975d3dff730f01278ea61c");

            CoseKey coseKey = CoseKey.Create(encodedKey, out int bytesRead);

            var ecPublicKey = Assert.IsType<CoseEcPublicKey>(coseKey);
            Assert.Equal(encodedKey.Length, bytesRead);
            Assert.Equal(CoseAlgorithmIdentifier.Esp256, ecPublicKey.Algorithm);
            Assert.Equal(CoseEcCurve.P256, ecPublicKey.Curve);
        }

        [Fact]
        public void Esp256Key_VerifiesPythonFido2Fixture()
        {
            byte[] encodedKey = HexToBytes(
                "a5010203282001215820a5fd5ce1b1c458c530a54fa61b31bf6b04be8b97afde54dd8cbb69275a8a1be1" +
                "225820fa3a3231dd9deed9d1897be5a6228c59501e4bcd12975d3dff730f01278ea61c");
            byte[] signedData = HexToBytes(
                "0021f5fc0b85cd22e60623bcd7d1ca48948909249b4776eb515154e57b66ae12010000002c" +
                "7b89f12a9088b0f5ee0ef8f6718bccc374249c31aeebaeb79bd0450132cd536c");
            byte[] signature = HexToBytes(
                "304402202b3933fe954a2d29de691901eb732535393d4859aaa80d58b08741598109516d" +
                "0220236fbe6b52326c0a6b1cfdc6bf0a35bda92a6c2e41e40c3a1643428d820941e0");

            var ecPublicKey = (CoseEcPublicKey)CoseKey.Create(encodedKey, out _);
            using var verifier = new EcdsaVerify(ecPublicKey);

            bool verified = verifier.VerifyData(signedData, signature, isStandardSignature: true);

            Assert.True(verified);
        }

        private static byte[] HexToBytes(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
    }
}
