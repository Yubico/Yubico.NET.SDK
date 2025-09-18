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

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class PssTests : PivSessionIntegrationTestBase
{
    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void Parse_FromRsaClass(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        TestDeviceType = testDeviceType;
        var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(KeyType.RSA1024);

        byte[] dataToSign =
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
        };

        using var rsaPrivate = testPrivateKey.AsRSA();
        var signature = rsaPrivate.SignData(
            dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        Assert.Equal(128, signature.Length);

        var isValid = CryptoSupport.CSharpRawRsaPublic(testPublicKey, signature, out var formattedData);
        Assert.True(isValid);
        Assert.Equal(128, formattedData.Length);

        using HashAlgorithm digester = CryptographyProviders.Sha256Creator();
        _ = digester.TransformFinalBlock(dataToSign, 0, dataToSign.Length);

        isValid = RsaFormat.TryParsePkcs1Pss(
            formattedData,
            digester.Hash,
            RsaFormat.Sha256,
            out var mPrimePlusH,
            out var isVerified);

        Assert.True(isValid);
        Assert.True(isVerified);
        Assert.Equal(104, mPrimePlusH.Length);

        isValid = Session.TryAuthenticateManagementKey(false);
        Assert.True(isValid);
        isValid = Session.TryVerifyPin();
        Assert.True(isValid);

        Session.ImportPrivateKey(0x86, testPrivateKey.AsPrivateKey());

        var yData = RsaFormat.FormatPkcs1Pss(digester.Hash, RsaFormat.Sha256, 1024);
        var signCommand = new AuthenticateSignCommand(yData, 0x86);
        var signResponse = Session.Connection.SendCommand(signCommand);
        Assert.Equal(ResponseStatus.Success, signResponse.Status);

        using var rsaPublic = testPublicKey.AsRSA();
        var ySignature = signResponse.GetData();
        isVerified =
            rsaPublic.VerifyData(dataToSign, ySignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        Assert.True(isVerified);
    }

    [Theory]
    [InlineData(KeyType.RSA1024, 1024)]
    [InlineData(KeyType.RSA2048, 2048)]
    [InlineData(KeyType.ECP256, 256)]
    [InlineData(KeyType.ECP384, 384)]
    public void UseKeyConverter(
        KeyType keyType,
        int keySize)
    {
        var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
        if (keyType is KeyType.RSA1024 or KeyType.RSA2048)
        {
            using var rsaPublic = testPublicKey.AsRSA();
            Assert.Equal(keySize, rsaPublic.KeySize);
            using var rsaPrivate = testPrivateKey.AsRSA();
            Assert.Equal(keySize, rsaPrivate.KeySize);
        }
        else
        {
            using var eccPublic = testPublicKey.AsECDsa();
            Assert.Equal(keySize, eccPublic.KeySize);

            using var eccPrivate = testPrivateKey.AsECDsa();
            Assert.Equal(keySize, eccPrivate.KeySize);
        }

        var convertedPub = testPrivateKey.AsPublicKey();
        Assert.Equal(keyType, convertedPub.KeyType);

        var publicPemArray = testPublicKey.AsPemString();
        Assert.NotNull(publicPemArray);
        if (publicPemArray is not null)
        {
            var pemStringPublic = new string(publicPemArray);
            Assert.NotNull(pemStringPublic);
        }

        var privatePemArray = testPublicKey.AsPemString();
        Assert.NotNull(privatePemArray);

        var pemStringPrivate = new string(privatePemArray);
        Assert.NotNull(pemStringPrivate);
    }
}
