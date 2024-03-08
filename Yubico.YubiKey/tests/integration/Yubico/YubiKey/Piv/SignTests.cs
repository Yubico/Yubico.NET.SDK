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
using Xunit;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class SignTests
    {
        [Theory]
        [InlineData(PivPinPolicy.Always)]
        [InlineData(PivPinPolicy.Never)]
        public void Sign_EccP256_Succeeds(PivPinPolicy pinPolicy)
        {
            byte[] dataToSign = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetScp03TestDevice();

            bool isValid = LoadKey(PivAlgorithm.EccP256, 0x89, pinPolicy, PivTouchPolicy.Never, testDevice);
            Assert.True(isValid);
            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                byte[] signature = pivSession.Sign(0x89, dataToSign);
                if (signature.Length > 2)
                {
                    Assert.Equal(0x30, signature[0]);
                }
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, 0x86)]
        [InlineData(PivAlgorithm.Rsa2048, 0x87)]
        [InlineData(PivAlgorithm.EccP256, 0x88)]
        [InlineData(PivAlgorithm.EccP384, 0x89)]
        public void Sign_RandomData_Succeeds(PivAlgorithm algorithm, byte slotNumber)
        {
            byte[] dataToSign = algorithm switch
            {
                PivAlgorithm.Rsa1024 => new byte[128],
                PivAlgorithm.Rsa2048 => new byte[256],
                PivAlgorithm.EccP256 => new byte[32],
                _ => new byte[48],
            };

            GetArbitraryData(dataToSign);
            dataToSign[0] &= 0x7F;

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetScp03TestDevice();

            bool isValid = LoadKey(algorithm, slotNumber, PivPinPolicy.Always, PivTouchPolicy.Never, testDevice);
            Assert.True(isValid);
            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(testDevice))
            {
                var command = new AuthenticateSignCommand(dataToSign, slotNumber);
                AuthenticateSignResponse response = pivSession.Connection.SendCommand(command);

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

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha1, 1)]
        [InlineData(PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha256, 1)]
        [InlineData(PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha384, 1)]
        [InlineData(PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha1, 2)]
        [InlineData(PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha256, 2)]
        [InlineData(PivAlgorithm.Rsa1024, 0x92, RsaFormat.Sha384, 2)]
        [InlineData(PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha1, 1)]
        [InlineData(PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha256, 1)]
        [InlineData(PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha384, 1)]
        [InlineData(PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha512, 1)]
        [InlineData(PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha1, 2)]
        [InlineData(PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha256, 2)]
        [InlineData(PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha384, 2)]
        [InlineData(PivAlgorithm.Rsa2048, 0x93, RsaFormat.Sha512, 2)]
        public void SignRsa_VerifyCSharp_Correct(PivAlgorithm algorithm, byte slotNumber, int digestAlgorithm, int paddingScheme)
        {
            int keySizeBits = RsaFormat.KeySizeBits1024;
            if (algorithm != PivAlgorithm.Rsa1024)
            {
                keySizeBits = RsaFormat.KeySizeBits2048;
            }

            byte[] dataToSign = new byte[128];
            GetArbitraryData(dataToSign);

            HashAlgorithmName hashAlgorithm = digestAlgorithm switch
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

            SampleKeyPairs.GetPemKeyPair(algorithm, out string pubKeyPem, out string priKeyPem);
            var pubKey = new KeyConverter(pubKeyPem.ToCharArray());
            var priKey = new KeyConverter(priKeyPem.ToCharArray());

            try
            {
                IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetScp03TestDevice();
                Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                using (var pivSession = new PivSession(testDevice))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ImportPrivateKey(slotNumber, priKey.GetPivPrivateKey());

                    byte[] signature = pivSession.Sign(slotNumber, formattedData);

                    using RSA rsaPublic = pubKey.GetRsaObject();
                    bool isVerified = rsaPublic.VerifyData(dataToSign, signature, hashAlgorithm, padding);
                    Assert.True(isVerified);
                }
            }
            finally
            {
                priKey.Clear();
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256, 0x94)]
        [InlineData(PivAlgorithm.EccP384, 0x95)]
        public void SignEcc_VerifyCSharp_Correct(PivAlgorithm algorithm, byte slotNumber)
        {
            byte[] dataToSign = new byte[128];
            GetArbitraryData(dataToSign);

            HashAlgorithmName hashAlgorithm = algorithm switch
            {
                PivAlgorithm.EccP256 => HashAlgorithmName.SHA256,
                _ => HashAlgorithmName.SHA384,
            };

            using HashAlgorithm digester = algorithm switch
            {
                PivAlgorithm.EccP256 => CryptographyProviders.Sha256Creator(),
                _ => CryptographyProviders.Sha384Creator(),
            };

            _ = digester.TransformFinalBlock(dataToSign, 0, dataToSign.Length);

            SampleKeyPairs.GetPemKeyPair(algorithm, out string pubKeyPem, out string priKeyPem);
            var pubKey = new KeyConverter(pubKeyPem.ToCharArray());
            var priKey = new KeyConverter(priKeyPem.ToCharArray());

            try
            {
                IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetScp03TestDevice();
                Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                using (var pivSession = new PivSession(testDevice))
                {
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
            }
            finally
            {
                priKey.Clear();
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void NoKeyInSlot_Sign_Exception(StandardTestDevice testDeviceType)
        {
            byte[] dataToSign = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                _ = Assert.Throws<InvalidOperationException>(() => pivSession.Sign(0x9a, dataToSign));
            }
        }

        public static bool LoadKey(PivAlgorithm algorithm, byte slotNumber, PivPinPolicy pinPolicy, PivTouchPolicy touchPolicy, IYubiKeyDevice testDevice)
        {
            if (testDevice != null)
            {
                if (testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv) == true)
                {
                    using (var pivSession = new PivSession(testDevice))
                    {
                        var collectorObj = new Simple39KeyCollector();
                        pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                        PivPrivateKey privateKey = SampleKeyPairs.GetPrivateKey(algorithm);
                        pivSession.ImportPrivateKey(slotNumber, privateKey, pinPolicy, touchPolicy);
                    }
                }
            }

            return true;
        }

        // Convert from the DER SEQ { INT r, INT s } to r || s.
        // This is because the ECDsa class operates on only this form of the signature.
        public static bool ConvertEcdsaSignature(byte[] signature, int integerLength, out byte[] rsSignature)
        {
            rsSignature = new byte[2 * integerLength];

            var tlvReader = new TlvReader(signature);
            TlvReader seq = tlvReader.ReadNestedTlv(0x30);
            ReadOnlyMemory<byte> rValue = seq.ReadValue(0x02);
            ReadOnlyMemory<byte> sValue = seq.ReadValue(0x02);

            // Leading 00 bytes?
            if (rValue.Length > integerLength)
            {
                if ((rValue.Length > (integerLength + 1)) || (rValue.Span[0] != 0))
                {
                    return false;
                }
                rValue = rValue[1..];
            }
            if (sValue.Length > integerLength)
            {
                if ((sValue.Length > (integerLength + 1)) || (sValue.Span[0] != 0))
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

        // Fill a byte array with "random" data. Up to 256 bytes.
        private static void GetArbitraryData(byte[] bufferToFill)
        {
            byte[] arbitraryData = new byte[] {
                0x3E, 0xE8, 0xC1, 0xBE, 0xFB, 0x55, 0x48, 0x82, 0xE6, 0xAD, 0x9A, 0xBC, 0x84, 0x04, 0xF4, 0xA4,
                0xF0, 0xE3, 0x08, 0x53, 0x02, 0x03, 0x01, 0x00, 0x01, 0x02, 0x41, 0x00, 0xAA, 0xA0, 0xBB, 0x04,
                0x9E, 0xD7, 0xBA, 0x33, 0x0D, 0x44, 0x84, 0xEC, 0x30, 0x0A, 0xB0, 0x8E, 0xF2, 0x47, 0x1D, 0x89,
                0xF5, 0x99, 0x5D, 0x99, 0xE7, 0xA1, 0x35, 0x26, 0x0B, 0xC7, 0x15, 0xA8, 0x5E, 0x75, 0x55, 0x63,
                0x1A, 0x89, 0xD8, 0x0E, 0x55, 0xD9, 0x1C, 0x89, 0x8A, 0xF4, 0xDE, 0x54, 0x05, 0xA5, 0x53, 0xA0,
                0x40, 0x32, 0x49, 0xC4, 0xC6, 0x10, 0xC5, 0x03, 0xCD, 0x66, 0xDB, 0x81, 0x02, 0x21, 0x00, 0xE0,
                0x8C, 0x19, 0x1D, 0x98, 0xB8, 0xC1, 0xB2, 0x0E, 0x6B, 0xD5, 0x4E, 0x20, 0xCE, 0x60, 0xCB, 0x1E,
                0x71, 0x2F, 0xB4, 0xE9, 0x2D, 0xE0, 0x51, 0x5B, 0xCD, 0xDE, 0xBF, 0x3C, 0xE7, 0x9A, 0x71, 0x02,
                0x21, 0x00, 0xC5, 0xCD, 0x80, 0x23, 0x17, 0x2D, 0xB0, 0xFE, 0x9D, 0xF0, 0x28, 0x6C, 0x50, 0xBE,
                0x66, 0x31, 0x28, 0x76, 0xC0, 0x86, 0x9B, 0x69, 0xDB, 0xD9, 0xA8, 0x47, 0xD1, 0xAC, 0x3E, 0x42,
                0x49, 0x03, 0x02, 0x21, 0x00, 0xCE, 0xBB, 0xED, 0xBB, 0xB4, 0x0A, 0x16, 0x3B, 0x0A, 0xCF, 0xF8,
                0xF9, 0x0F, 0x77, 0x32, 0xE2, 0x8F, 0x4A, 0x82, 0x33, 0xBB, 0xA3, 0x83, 0x2D, 0x24, 0xAA, 0xAB,
                0xF3, 0xC1, 0xED, 0x31, 0xE1, 0x02, 0x20, 0x58, 0x44, 0x4C, 0xC2, 0xDB, 0xEC, 0x02, 0xC8, 0x8C,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F
            };

            int count = 256;
            if (bufferToFill.Length < 256)
            {
                count = bufferToFill.Length;
            }

            Array.Copy(arbitraryData, 0, bufferToFill, 0, count);
        }
    }
}
