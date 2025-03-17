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
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class ImportTests
    {
        
        // [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        // [InlineData(PivAlgorithm.EccP256, StandardTestDevice.Fw5)]
        // [InlineData(PivAlgorithm.EccP384, StandardTestDevice.Fw5)]
        // [InlineData(PivAlgorithm.EccEd25519, StandardTestDevice.Fw5)]
        // [InlineData(PivAlgorithm.EccX25519, StandardTestDevice.Fw5)]
        // public void Import_with_PublicKeyParameters_Succeeds(
        //     PivAlgorithm algorithm,
        //     StandardTestDevice testDeviceType)
        // {
        //     var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
        //     var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(algorithm);
        //     var asPivPrivateKey = testPrivateKey.AsPivPrivateKey();
        //     var pivPrivateKey = PivPrivateKey.Create(asPivPrivateKey.EncodedPrivateKey);
        //     
        //     Assert.Equal(algorithm, pivPrivateKey.Algorithm);
        //     
        // }
        
        // [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        // [InlineData(PivAlgorithm.EccP256, StandardTestDevice.Fw5)]
        // [InlineData(PivAlgorithm.EccP384, StandardTestDevice.Fw5)]
        // [InlineData(PivAlgorithm.EccEd25519, StandardTestDevice.Fw5)]
        // [InlineData(PivAlgorithm.EccX25519, StandardTestDevice.Fw5)]
        // public void PivTlvImportSucceeds(
        //     PivAlgorithm algorithm,
        //     StandardTestDevice testDeviceType)
        // {
        //     var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(algorithm);
        //     var asPivPrivateKey = testPrivateKey.AsPivPrivateKey();
        //     var pivPrivateKey = PivPrivateKey.Create(asPivPrivateKey.EncodedPrivateKey);
        //     
        //     Assert.Equal(algorithm, pivPrivateKey.Algorithm);
        //     
        //     // Act
        //     var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
        //     using var pivSession = GetSession(testDevice);
        //     pivSession.ImportPrivateKey(PivSlot.Retired1, pivPrivateKey);
        //
        //     // Assert
        //     var slotMetadata = pivSession.GetMetadata(PivSlot.Retired1);
        //     Assert.Equal(algorithm, slotMetadata.Algorithm);
        //     
        //     var testPivPublicKey = testPublicKey.AsPivPublicKey();
        //     Assert.Equal(slotMetadata.PublicKey.YubiKeyEncodedPublicKey, testPivPublicKey.YubiKeyEncodedPublicKey);
        // }
        //
        
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(PivAlgorithm.Rsa1024, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa2048, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP256, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccEd25519, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccX25519, StandardTestDevice.Fw5)]
        public void RawKeyImportSucceeds( // Works
            PivAlgorithm algorithm,
            StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(algorithm);

            var parser = new PrivateKeyInfoParser();
            PivPrivateKey pivPrivateKey;
            switch (algorithm)
            {
                case PivAlgorithm.EccP256 or PivAlgorithm.EccP384:
                    {
                        var keyInfo = parser.ParsePrivateKey<EcPrivateKeyInfo>(testPrivateKey.KeyBytes);
                        pivPrivateKey = new PivEccPrivateKey(keyInfo.PrivateKey, algorithm);
                        break;
                    }
                case PivAlgorithm.EccEd25519 or PivAlgorithm.EccX25519:
                    {
                        var keyInfo = parser.ParsePrivateKey<EdPrivateKeyInfo>(testPrivateKey.KeyBytes);
                        pivPrivateKey = new PivEccPrivateKey(keyInfo.PrivateKey, algorithm);
                        break;
                    }
                case PivAlgorithm.Rsa1024 or PivAlgorithm.Rsa2048 or PivAlgorithm.Rsa3072 or PivAlgorithm.Rsa4096:
                    {
                        var keyInfo = parser.ParsePrivateKey<RsaPrivateKeyInfo>(testPrivateKey.KeyBytes);
                        pivPrivateKey = new PivRsaPrivateKey(keyInfo.Prime1, keyInfo.Prime2, keyInfo.Exponent1,
                        keyInfo.Exponent2, keyInfo.Coefficient);
                        break;
                    }
                default:
                    throw new ArgumentException($"Unexpected algorithm {algorithm}", nameof(algorithm));
            }
            
            const PivPinPolicy expectedPinPolicy = PivPinPolicy.Once;
            const PivTouchPolicy expectedTouchPolicy = PivTouchPolicy.Always;
            
            // Act
            using var pivSession = GetSession(testDevice);
            
            pivSession.ImportPrivateKey(PivSlot.Retired1, pivPrivateKey, expectedPinPolicy, expectedTouchPolicy);

            // Assert
            var slotMetadata = pivSession.GetMetadata(PivSlot.Retired1);
            Assert.Equal(algorithm, slotMetadata.Algorithm);

            var testPivPublicKey = testPublicKey.AsPivPublicKey();
            Assert.Equal(slotMetadata.PublicKey.YubiKeyEncodedPublicKey, testPivPublicKey.YubiKeyEncodedPublicKey);
            
            Assert.Equal(expectedPinPolicy, slotMetadata.PinPolicy);
            Assert.Equal(expectedTouchPolicy, slotMetadata.TouchPolicy);
        }

        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(PivAlgorithm.EccEd25519, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa1024, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa2048, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, StandardTestDevice.Fw5)]
        public void KeyAndCertImport(
            PivAlgorithm algorithm,
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var pivSession = new PivSession(testDevice);
            var isValid = PivSupport.ResetPiv(pivSession);
            Assert.True(isValid);

            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            isValid = SampleKeyPairs.GetMatchingKeyAndCert(algorithm, out var cert, out var privateKey);
            Assert.True(isValid);

            pivSession.ImportPrivateKey(0x90, privateKey!);
            pivSession.ImportCertificate(0x90, cert!);
        }

        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(PivAlgorithm.Rsa1024, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa2048, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccEd25519, StandardTestDevice.Fw5)]
        public void CertImport(
            PivAlgorithm algorithm,
            StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            var isValid = SampleKeyPairs.GetMatchingKeyAndCert(algorithm, out var cert, out var _);
            Assert.True(isValid);

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            pivSession.ImportCertificate(0x90, cert!);

            var getCert = pivSession.GetCertificate(0x90);
            Assert.True(getCert.Equals(cert));
        }

        private static PivSession GetSession(IYubiKeyDevice testDevice)
        {
            var pivSession = new PivSession(testDevice);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            return pivSession;
        }
    }
}
