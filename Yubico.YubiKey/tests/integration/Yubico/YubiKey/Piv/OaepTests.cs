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
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class OaepTests : PivSessionIntegrationTestBase
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void Parse_FromRsaClass(
            StandardTestDevice testDeviceType)
        {
            // Arrange
            TestDeviceType = testDeviceType;

            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(KeyType.RSA1024);
            byte[] dataToEncrypt =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20,
            };

            using var rsaPublic = testPublicKey.AsRSA();
            var encryptedData = rsaPublic.Encrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA1);
            Assert.Equal(128, encryptedData.Length);

            var isValid = CryptoSupport.CSharpRawRsaPrivate(testPrivateKey, encryptedData, out var formattedData);
            Assert.True(isValid);
            Assert.Equal(128, formattedData.Length);

            isValid = RsaFormat.TryParsePkcs1Oaep(
                formattedData,
                RsaFormat.Sha1,
                out var decryptedData);

            Assert.True(isValid);
            Assert.Equal(32, decryptedData.Length);

            isValid = Session.TryAuthenticateManagementKey(false);
            Assert.True(isValid);
            
            isValid = Session.TryVerifyPin();
            Assert.True(isValid);

            Session.ImportPrivateKey(0x86, testPrivateKey.AsPrivateKey());

            var decryptCommand = new AuthenticateDecryptCommand(encryptedData, 0x86);
            var decryptResponse = Session.Connection.SendCommand(decryptCommand);
            Assert.Equal(ResponseStatus.Success, decryptResponse.Status);
            
            var yFormattedData = decryptResponse.GetData();
            isValid = RsaFormat.TryParsePkcs1Oaep(
                yFormattedData,
                RsaFormat.Sha1,
                out _);

            Assert.True(isValid);
            Assert.Equal(32, decryptedData.Length);
        }
    }
}
