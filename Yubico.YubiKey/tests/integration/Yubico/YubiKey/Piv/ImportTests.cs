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
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class ImportTests
    {
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
        [InlineData(KeyType.P256, StandardTestDevice.Fw5)]
        [InlineData(KeyType.P384, StandardTestDevice.Fw5)]
        [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5)]
        [InlineData(KeyType.X25519, StandardTestDevice.Fw5)]
        [Obsolete("Obsolete")]
        public void Import_with_PrivateKeyParameters_Succeeds_and_HasExpectedValues(
            KeyType keyType,
            StandardTestDevice testDeviceType)
        {
            // Arrange
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
            var testPivPublicKey = testPublicKey.AsPivPublicKey();
            var keyParameters = AsnPrivateKeyReader.CreateKeyParameters(testPrivateKey.EncodedKey);

            const PivPinPolicy expectedPinPolicy = PivPinPolicy.Once;
            const PivTouchPolicy expectedTouchPolicy = PivTouchPolicy.Always;

            // Act
            using var pivSession = GetSession(testDevice);
            pivSession.ImportPrivateKey(PivSlot.Retired1, keyParameters, expectedPinPolicy, expectedTouchPolicy);

            // Assert
            var slotMetadata = pivSession.GetMetadata(PivSlot.Retired1);
            Assert.Equal(keyType.GetPivAlgorithm(), slotMetadata.Algorithm);
            Assert.Equal(testPivPublicKey.YubiKeyEncodedPublicKey, slotMetadata.PublicKey.YubiKeyEncodedPublicKey);
            Assert.Equal(testPivPublicKey.PivEncodedPublicKey, slotMetadata.PublicKeyParameters.ToPivEncodedPublicKey()); // Remove because eventually we dont want pivAlgorithm
            Assert.Equal(expectedPinPolicy, slotMetadata.PinPolicy);
            Assert.Equal(expectedTouchPolicy, slotMetadata.TouchPolicy);
        }

        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
        [InlineData(KeyType.P256, StandardTestDevice.Fw5)]
        [InlineData(KeyType.P384, StandardTestDevice.Fw5)]
        [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5)]
        [InlineData(KeyType.X25519, StandardTestDevice.Fw5)]
        public void
            ImportPrivateKey_with_PivPrivateKey_Succeeds_and_HasExpectedValues(
                KeyType keyType,
                StandardTestDevice testDeviceType)
        {
            
            // Arrange
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);

            var privateKey = AsnPrivateKeyReader.CreateKeyParameters(testPrivateKey.EncodedKey);

            const PivPinPolicy expectedPinPolicy = PivPinPolicy.Once;
            const PivTouchPolicy expectedTouchPolicy = PivTouchPolicy.Always;

            // Act
            using var pivSession = GetSession(testDevice);

            pivSession.ImportPrivateKey(PivSlot.Retired1, privateKey, expectedPinPolicy, expectedTouchPolicy);

            // Assert
            var slotMetadata = pivSession.GetMetadata(PivSlot.Retired1);
            Assert.Equal(keyType.GetPivAlgorithm(), slotMetadata.Algorithm);
            Assert.Equal(expectedPinPolicy, slotMetadata.PinPolicy);
            Assert.Equal(expectedTouchPolicy, slotMetadata.TouchPolicy);
            
            if (keyType.IsEcKey())
            {
                var publicPoint = slotMetadata.PublicKeyParameters switch
                {
                    ECPublicKeyParameters ecDsa => ecDsa.PublicPoint.ToArray(),
                    Curve25519PublicKeyParameters edDsa => edDsa.PublicPoint.ToArray(),
                    _ => throw new ArgumentException("Invalid public key type")
                };
                Assert.Equal(testPublicKey.GetPublicPoint(), publicPoint);
            }
            else
            {
                var publicRSAParameters = slotMetadata.PublicKeyParameters as RSAPublicKeyParameters;
                
                Assert.NotNull(publicRSAParameters);
                var rsaParameters = testPublicKey.AsRSA().ExportParameters(false);
                Assert.Equal(rsaParameters.Modulus, publicRSAParameters.Parameters.Modulus);
                Assert.Equal(rsaParameters.Exponent, publicRSAParameters.Parameters.Exponent);
            }

        }

        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
        [Obsolete("Replaced by IPrivateKeyParameters")] // TODO
        public void KeyAndCertImport(
            KeyType keyType,
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var pivSession = new PivSession(testDevice);
            var isValid = PivSupport.ResetPiv(pivSession);
            Assert.True(isValid);

            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            isValid = SampleKeyPairs.GetMatchingKeyAndCert(keyType, out var cert, out var privateKey);
            Assert.True(isValid);

            pivSession.ImportPrivateKey(0x90, privateKey!);
            pivSession.ImportCertificate(0x90, cert!);
        }

        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
        [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5)]
        public void CertImport(
            KeyType keyType,
            StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            var isValid = SampleKeyPairs.GetMatchingKeyAndCert(keyType, out var cert, out var _);
            Assert.True(isValid);

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            pivSession.ImportCertificate(0x90, cert!);

            var getCert = pivSession.GetCertificate(0x90);
            Assert.True(getCert.Equals(cert));
        }

        private static PivSession GetSession(
            IYubiKeyDevice testDevice)
        {
            var pivSession = new PivSession(testDevice);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            return pivSession;
        }
    }
}
