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
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.Scp03;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class SignTests
    {
        [SkippableTheory(typeof(DeviceNotFoundException))]

        [InlineData(false, StandardTestDevice.Fw5, PivPinPolicy.Always)]
        [InlineData(false, StandardTestDevice.Fw5, PivPinPolicy.Never)]
        [InlineData(true, StandardTestDevice.Fw5, PivPinPolicy.Always)]
        [InlineData(true, StandardTestDevice.Fw5, PivPinPolicy.Never)]
        [InlineData(true, StandardTestDevice.Fw5Fips, PivPinPolicy.Always)]
        [InlineData(true, StandardTestDevice.Fw5Fips, PivPinPolicy.Never)]
        [InlineData(false, StandardTestDevice.Fw5Fips, PivPinPolicy.Always)]
        [InlineData(false, StandardTestDevice.Fw5Fips, PivPinPolicy.Never)]
        public void Sign_EccP256_Succeeds(bool useScp03, StandardTestDevice device, PivPinPolicy pinPolicy)
        {
            byte[] dataToSign = {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(device);

            bool isValid = LoadKey(PivAlgorithm.EccP256, 0x89, pinPolicy, PivTouchPolicy.Never, testDevice);
            Assert.True(isValid);
            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            using var pivSession = useScp03
                ? new PivSession(testDevice, Scp03KeyParameters.DefaultKey)
                : new PivSession(testDevice);

            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            byte[] signature = pivSession.Sign(0x89, dataToSign);
            if (signature.Length > 2)
            {
                Assert.Equal(0x30, signature[0]);
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa1024, 0x86)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x87)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x87)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.EccP256, 0x88)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x87)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.EccP384, 0x89)]

        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa1024, 0x86)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x87)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x87)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.EccP256, 0x88)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x87)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.EccP384, 0x89)]
        public void Sign_RandomData_Succeeds(StandardTestDevice testDeviceType, PivAlgorithm algorithm, byte slotNumber)
        {
            byte[] dataToSign = algorithm switch
            {
                PivAlgorithm.Rsa1024 => new byte[128],
                PivAlgorithm.Rsa2048 => new byte[256],
                PivAlgorithm.Rsa3072 => new byte[384],
                PivAlgorithm.Rsa4096 => new byte[512],
                PivAlgorithm.EccP256 => new byte[32],
                _ => new byte[48],
            };

            Random.Shared.NextBytes(dataToSign);

            dataToSign[0] &= 0x7F;

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            bool isValid = LoadKey(algorithm, slotNumber, PivPinPolicy.Always, PivTouchPolicy.Never, testDevice);
            Assert.True(isValid);

            using (var pivSession = new PivSession(testDevice))
            {
                var command = new AuthenticateSignCommand(dataToSign, slotNumber);
                var response = pivSession.Connection.SendCommand(command);

                if (response.Status == ResponseStatus.Success)
                {
                    byte[] signature1 = response.GetData();
                    Assert.NotEmpty(signature1);
                }

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                byte[] signature = pivSession.Sign(slotNumber, dataToSign);
                Assert.True(isValid);
                Assert.NotEmpty(signature);
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha384, 1)]

        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha384, 2)]

        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha512, 1)]

        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha512, 2)]

        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha512, 1)]

        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha512, 2)]

        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha512, 1)]

        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha512, 2)]

        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha384, 1)]

        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha384, 2)]

        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha512, 1)]

        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha512, 2)]

        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha512, 1)]

        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072, 0x94, RsaFormat.Sha512, 2)]

        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha1, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha256, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha384, 1)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha512, 1)]

        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha1, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha256, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha384, 2)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096, 0x95, RsaFormat.Sha512, 2)]
        public void SignRsa_VerifyCSharp_Correct(StandardTestDevice testDeviceType, PivAlgorithm algorithm, byte slotNumber, int digestAlgorithm,
            int paddingScheme)
        {
            int keySizeBits = algorithm.KeySizeBits();
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

            _ = SampleKeyPairs.GetKeysAndCertPem(algorithm, false, out _, out var pubKeyPem, out var priKeyPem);
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
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.EccP256, 0x94)]
        [InlineData(StandardTestDevice.Fw5Fips, PivAlgorithm.EccP384, 0x95)]
        public void SignEcc_VerifyCSharp_Correct(StandardTestDevice testDeviceType, PivAlgorithm algorithm, byte slotNumber)
        {
            byte[] dataToSign = new byte[128];
            Random.Shared.NextBytes(dataToSign);

            var hashAlgorithm = algorithm switch
            {
                PivAlgorithm.EccP256 => HashAlgorithmName.SHA256,
                _ => HashAlgorithmName.SHA384,
            };

            using HashAlgorithm digester = algorithm switch
            {
                PivAlgorithm.EccP256 => CryptographyProviders.Sha256Creator(),
                _ => CryptographyProviders.Sha384Creator(),
            };

            digester.TransformFinalBlock(dataToSign, 0, dataToSign.Length);

            _ = SampleKeyPairs.GetKeysAndCertPem(algorithm, false, out _, out var pubKeyPem, out var priKeyPem);
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

                byte[] signature = pivSession.Sign(slotNumber, digester.Hash);

                bool isValid = ConvertEcdsaSignature(signature, digester.Hash!.Length, out byte[] rsSignature);
                Assert.True(isValid);

                using ECDsa eccPublic = pubKey.GetEccObject();
                bool isVerified = eccPublic.VerifyData(dataToSign, rsSignature, hashAlgorithm);
                Assert.True(isVerified);
            }
            finally
            {
                priKey.Clear();
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void NoKeyInSlot_Sign_Exception(StandardTestDevice testDeviceType)
        {
            byte[] dataToSign = {
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

        private static bool LoadKey(PivAlgorithm algorithm, byte slotNumber, PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy, IYubiKeyDevice testDevice)
        {
            if (!testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv))
            {
                return false;
            }

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            PivPrivateKey privateKey = SampleKeyPairs.GetPivPrivateKey(algorithm);
            pivSession.ImportPrivateKey(slotNumber, privateKey, pinPolicy, touchPolicy);
            return true;
        }

        // Convert from the DER SEQ { INT r, INT s } to r || s.
        // This is because the ECDsa class operates on only this form of the signature.
        private static bool ConvertEcdsaSignature(byte[] signature, int integerLength, out byte[] rsSignature)
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
    }
}
