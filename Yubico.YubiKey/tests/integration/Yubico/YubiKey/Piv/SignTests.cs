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

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Xunit;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class SignTests
    {
        
        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Sign_WithEd25519_RandomData_Succeeds(
            StandardTestDevice testDeviceType)
        {
            // Arrange
            var dataToSign = new byte[3062]; // APDU cannot be bigger than this
            Random.Shared.NextBytes(dataToSign);
            
            // -> Generate a Ed25519 key
            using var pivSession = GetSession(testDeviceType);
            var publicKeyParameters = pivSession.GenerateKeyPair(PivSlot.Retired12, KeyType.Ed25519);

            // Act
            var signature = pivSession.Sign(PivSlot.Retired12, dataToSign);

            // -> Verify the signature
            var bouncyKeyParameters = GetBouncyCastleKeyParameters(publicKeyParameters);
            var verifier = new Ed25519Signer();
            verifier.Init(false, bouncyKeyParameters);
            verifier.BlockUpdate(dataToSign, 0, dataToSign.Length);

            // Assert
            var isValidSignature = verifier.VerifySignature(signature);
            Assert.True(isValidSignature);
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA1024)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096)]
        [InlineData(StandardTestDevice.Fw5, KeyType.ECP256)]
        [InlineData(StandardTestDevice.Fw5, KeyType.ECP384)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA1024)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.ECP256)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.ECP384)]
        public async Task Sign_with_RSAandECDsa_Succeeds( // TODO Tests are flaky
            StandardTestDevice testDeviceType,
            KeyType keyType)
        {
            const byte slotNumber = PivSlot.Retired12;
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            var dataToSign = keyType switch
            {
                KeyType.RSA1024 => new byte[128],
                KeyType.RSA2048 => new byte[256],
                KeyType.RSA3072 => new byte[384],
                KeyType.RSA4096 => new byte[512],
                KeyType.ECP256 => new byte[32],
                _ => new byte[48],
            };

            Random.Shared.NextBytes(dataToSign);

            var isValid = await ImportKey(keyType, slotNumber, PivPinPolicy.Never, PivTouchPolicy.Never, testDevice);
            Assert.True(isValid);

            using var pivSession = new PivSession(testDevice);

            // Sign using the command directly
            var command = new AuthenticateSignCommand(dataToSign, slotNumber, keyType.GetPivAlgorithm());
            var response = pivSession.Connection.SendCommand(command);
            var signature1 = response.GetData();

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.NotEmpty(signature1);

            // Sign using the PivSession
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            var signature2 = pivSession.Sign(slotNumber, dataToSign);
            Assert.True(isValid);
            Assert.NotEmpty(signature2);
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA1024, 0x92, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA1024, 0x92, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA1024, 0x92, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA1024, 0x92, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA1024, 0x92, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA1024, 0x92, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048, 0x93, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048, 0x93, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048, 0x93, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048, 0x93, RsaFormat.Sha512, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048, 0x93, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048, 0x93, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048, 0x93, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA2048, 0x93, RsaFormat.Sha512, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072, 0x94, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072, 0x94, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072, 0x94, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072, 0x94, RsaFormat.Sha512, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072, 0x94, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072, 0x94, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072, 0x94, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA3072, 0x94, RsaFormat.Sha512, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096, 0x95, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096, 0x95, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096, 0x95, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096, 0x95, RsaFormat.Sha512, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096, 0x95, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096, 0x95, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096, 0x95, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.RSA4096, 0x95, RsaFormat.Sha512, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA1024, 0x92, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA1024, 0x92, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA1024, 0x92, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA1024, 0x92, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA1024, 0x92, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA1024, 0x92, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048, 0x93, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048, 0x93, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048, 0x93, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048, 0x93, RsaFormat.Sha512, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048, 0x93, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048, 0x93, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048, 0x93, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA2048, 0x93, RsaFormat.Sha512, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072, 0x94, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072, 0x94, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072, 0x94, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072, 0x94, RsaFormat.Sha512, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072, 0x94, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072, 0x94, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072, 0x94, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA3072, 0x94, RsaFormat.Sha512, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096, 0x95, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096, 0x95, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096, 0x95, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096, 0x95, RsaFormat.Sha512, 1)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096, 0x95, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096, 0x95, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096, 0x95, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5, KeyType.RSA4096, 0x95, RsaFormat.Sha512, 2)]
        [Obsolete("Use the keyparameters method instead")]
        public void SignRsa_VerifyCSharp_Correct(
            StandardTestDevice testDeviceType,
            KeyType keyType,
            byte slotNumber,
            int digestAlgorithm,
            int paddingScheme)
        {
            int keySizeBits = keyType.GetKeyDefinition().LengthInBits;
            byte[] dataToSign = new byte[128];
            Random.Shared.NextBytes(dataToSign);

            var hashAlgorithm = digestAlgorithm switch
            {
                RsaFormat.Sha256 => HashAlgorithmName.SHA256,
                RsaFormat.Sha384 => HashAlgorithmName.SHA384,
                RsaFormat.Sha512 => HashAlgorithmName.SHA512,
                _ => HashAlgorithmName.SHA1,
            };

            using HashAlgorithm digester = digestAlgorithm switch
            {
                RsaFormat.Sha256 => CryptographyProviders.Sha256Creator(),
                RsaFormat.Sha384 => CryptographyProviders.Sha384Creator(),
                RsaFormat.Sha512 => CryptographyProviders.Sha512Creator(),
                _ => CryptographyProviders.Sha1Creator(),
            };

            _ = digester.TransformFinalBlock(dataToSign, 0, dataToSign.Length);

            byte[] formattedData = paddingScheme switch
            {
                1 => RsaFormat.FormatPkcs1Sign(digester.Hash, digestAlgorithm, keySizeBits),
                _ => RsaFormat.FormatPkcs1Pss(digester.Hash, digestAlgorithm, keySizeBits),
            };

            RSASignaturePadding padding = paddingScheme switch
            {
                1 => RSASignaturePadding.Pkcs1,
                _ => RSASignaturePadding.Pss,
            };

            _ = SampleKeyPairs.GetKeysAndCertPem(keyType, false, out _, out var pubKeyPem, out var priKeyPem);
            var pubKey = new KeyConverter(pubKeyPem!.ToCharArray());
            var priKey = new KeyConverter(priKeyPem!.ToCharArray());

            try
            {
                IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
                Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                using var pivSession = new PivSession(testDevice);
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportPrivateKey(slotNumber, priKey.GetPivPrivateKey());
                byte[] signature = pivSession.Sign(slotNumber, formattedData);

                using RSA rsaPublic = pubKey.GetRsaObject();
                bool isVerified = rsaPublic.VerifyData(dataToSign, signature, hashAlgorithm, padding);
                Assert.True(isVerified);
            }
            finally
            {
                priKey.Clear();
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, KeyType.ECP256, 0x94)]
        [InlineData(StandardTestDevice.Fw5Fips, KeyType.ECP384, 0x95)]
        public void SignEcc_VerifyCSharp_Correct(
            StandardTestDevice testDeviceType,
            KeyType keyType,
            byte slotNumber)
        {
            byte[] dataToSign = new byte[128];
            Random.Shared.NextBytes(dataToSign);

            var hashAlgorithm = keyType switch
            {
                KeyType.ECP256 => HashAlgorithmName.SHA256,
                _ => HashAlgorithmName.SHA384,
            };

            using HashAlgorithm digester = keyType switch
            {
                KeyType.ECP256 => CryptographyProviders.Sha256Creator(),
                _ => CryptographyProviders.Sha384Creator(),
            };

            digester.TransformFinalBlock(dataToSign, 0, dataToSign.Length);

            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
            var privateKey = ECPrivateKey.CreateFromPkcs8(testPrivateKey.EncodedKey);
            try
            {
                IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
                Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                using var pivSession = new PivSession(testDevice);
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportPrivateKey(slotNumber, privateKey);

                byte[] signature = pivSession.Sign(slotNumber, digester.Hash);

                bool isValid = ConvertEcdsaSignature(signature, digester.Hash!.Length, out byte[] rsSignature);
                Assert.True(isValid);

                using ECDsa eccPublic = testPublicKey.AsECDsa();
                bool isVerified = eccPublic.VerifyData(dataToSign, rsSignature, hashAlgorithm);
                Assert.True(isVerified);
            }
            finally
            {
                privateKey.Clear();
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void NoKeyInSlot_Sign_Exception(
            StandardTestDevice testDeviceType)
        {
            byte[] dataToSign =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            pivSession.ResetApplication();

            _ = Assert.Throws<InvalidOperationException>(() => pivSession.Sign(0x9a, dataToSign));
        }

        private async static Task<bool> ImportKey(
            KeyType keyType,
            byte slotNumber,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy,
            IYubiKeyDevice testDevice)
        {
            if (!testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv))
            {
                return false;
            }

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            var testKey = TestKeys.GetTestPrivateKey(keyType);
            var privateKey = AsnPrivateKeyDecoder.CreatePrivateKey(testKey.EncodedKey);
            pivSession.ImportPrivateKey(slotNumber, privateKey, pinPolicy, touchPolicy);

            await Task.Delay(200);

            return true;
        }

        // Convert from the DER SEQ { INT r, INT s } to r || s.
        // This is because the ECDsa class operates on only this form of the signature.
        private static bool ConvertEcdsaSignature(
            byte[] signature,
            int integerLength,
            out byte[] rsSignature)
        {
            rsSignature = new byte[2 * integerLength];

            var tlvReader = new TlvReader(signature);
            TlvReader seq = tlvReader.ReadNestedTlv(0x30);
            ReadOnlyMemory<byte> rValue = seq.ReadValue(0x02);
            ReadOnlyMemory<byte> sValue = seq.ReadValue(0x02);

            // Leading 00 bytes?
            if (rValue.Length > integerLength)
            {
                if (rValue.Length > integerLength + 1 || rValue.Span[0] != 0)
                {
                    return false;
                }

                rValue = rValue[1..];
            }

            if (sValue.Length > integerLength)
            {
                if (sValue.Length > integerLength + 1 || sValue.Span[0] != 0)
                {
                    return false;
                }

                sValue = sValue[1..];
            }

            var sigAsSpan = new Span<byte>(rsSignature);
            int offset = integerLength - rValue.Length;
            rValue.Span.CopyTo(sigAsSpan.Slice(offset, rValue.Length));

            offset = integerLength - sValue.Length;
            sValue.Span.CopyTo(sigAsSpan.Slice(integerLength + offset, sValue.Length));

            return true;
        }
        
        private static Ed25519PublicKeyParameters GetBouncyCastleKeyParameters(IPublicKey publicKey)
        {
            var bouncyEd25519PublicKey =
                new Ed25519PublicKeyParameters(
                    ((Curve25519PublicKey)publicKey).PublicPoint.ToArray());
            
            return bouncyEd25519PublicKey;
        }

        private static PivSession GetSession(
            StandardTestDevice testDeviceType)
        {
            PivSession? pivSession = null;
            try
            {
                var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
                pivSession = new PivSession(testDevice);
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
                return pivSession;
            }
            catch
            {
                pivSession?.Dispose();
                throw;
            }
        }
    }
}
