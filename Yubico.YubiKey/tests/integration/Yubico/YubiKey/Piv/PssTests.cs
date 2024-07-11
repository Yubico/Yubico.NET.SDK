// Copyright 2021 Yubico AB
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
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait("Category", "Simple")]
    public class PssTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void Parse_FromRsaClass(StandardTestDevice testDeviceType)
        {
            _ = SampleKeyPairs.GetKeysAndCertPem(PivAlgorithm.Rsa1024, validAttest: false, out _, out var publicKeyPem,
                out var privateKeyPem);

            var publicKey = new KeyConverter(publicKeyPem.ToCharArray());
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());

            byte[] dataToSign =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
            };

            using var rsaPrivate = privateKey.GetRsaObject();
            var signature = rsaPrivate.SignData(
                dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

            Assert.Equal(expected: 128, signature.Length);

            var isValid = CryptoSupport.CSharpRawRsaPublic(publicKeyPem, signature, out var formattedData);
            Assert.True(isValid);
            Assert.Equal(expected: 128, formattedData.Length);

            using HashAlgorithm digester = CryptographyProviders.Sha256Creator();
            _ = digester.TransformFinalBlock(dataToSign, inputOffset: 0, dataToSign.Length);

            isValid = RsaFormat.TryParsePkcs1Pss(
                formattedData,
                digester.Hash,
                RsaFormat.Sha256,
                out var mPrimePlusH,
                out var isVerified);

            Assert.True(isValid);
            Assert.True(isVerified);
            Assert.Equal(expected: 104, mPrimePlusH.Length);

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
                isValid = pivSession.TryAuthenticateManagementKey(mutualAuthentication: false);
                Assert.True(isValid);
                isValid = pivSession.TryVerifyPin();
                Assert.True(isValid);

                var pivPrivate = privateKey.GetPivPrivateKey();
                pivSession.ImportPrivateKey(slotNumber: 0x86, pivPrivate);

                var yData = RsaFormat.FormatPkcs1Pss(digester.Hash, RsaFormat.Sha256, keySizeBits: 1024);
                var signCommand = new AuthenticateSignCommand(yData, slotNumber: 0x86);
                var signResponse = pivSession.Connection.SendCommand(signCommand);

                Assert.Equal(ResponseStatus.Success, signResponse.Status);

                var ySignature = signResponse.GetData();

                using var rsaPublic = publicKey.GetRsaObject();

                isVerified = rsaPublic.VerifyData(dataToSign, ySignature, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);
                Assert.True(isVerified);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, 1024)]
        [InlineData(PivAlgorithm.Rsa2048, 2048)]
        [InlineData(PivAlgorithm.EccP256, 256)]
        [InlineData(PivAlgorithm.EccP384, 384)]
        public void UseKeyConverter(PivAlgorithm algorithm, int keySize)
        {
            _ = SampleKeyPairs.GetKeysAndCertPem(algorithm, validAttest: false, out _, out var publicPem,
                out var privatePem);

            var publicKey = new KeyConverter(publicPem.ToCharArray());
            Assert.Equal(algorithm, publicKey.Algorithm);

            var privateKey = new KeyConverter(privatePem.ToCharArray());
            Assert.Equal(algorithm, privateKey.Algorithm);

            if (algorithm == PivAlgorithm.Rsa1024 || algorithm == PivAlgorithm.Rsa2048)
            {
                using var rsaPublic = publicKey.GetRsaObject();
                Assert.Equal(keySize, rsaPublic.KeySize);
                using var rsaPrivate = privateKey.GetRsaObject();
                Assert.Equal(keySize, rsaPrivate.KeySize);
            }
            else
            {
                using var eccPublic = publicKey.GetEccObject();
                Assert.Equal(keySize, eccPublic.KeySize);

                using var eccPrivate = privateKey.GetEccObject();
                Assert.Equal(keySize, eccPrivate.KeySize);
            }

            var convertedPub = privateKey.GetPivPublicKey();
            Assert.Equal(algorithm, convertedPub.Algorithm);

            var publicPemArray = publicKey.GetPemKeyString();
            Assert.NotNull(publicPemArray);
            if (!(publicPemArray is null))
            {
                var pemStringPublic = new string(publicPemArray);
                Assert.NotNull(pemStringPublic);
            }

            var privatePemArray = privateKey.GetPemKeyString();
            Assert.NotNull(privatePemArray);
            if (!(privatePemArray is null))
            {
                var pemStringPrivate = new string(privatePemArray);
                Assert.NotNull(pemStringPrivate);
            }
        }
    }
}
