// Copyright 2024 Yubico AB
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
using Yubico.YubiKey.Piv.Converters;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class MoveDeleteKeyTests
    {
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void MoveKey_WithGenerate(KeyType expectedAlgorithm)
        {
            // Arrange
            const byte sourceSlot = PivSlot.Retired1;
            const byte destinationSlot = PivSlot.Retired20;

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            DeleteKeys(pivSession, sourceSlot, destinationSlot);

            var generatedKeyPair = pivSession.GenerateKeyPair(sourceSlot, expectedAlgorithm, PivPinPolicy.None);
            var metadataForKeyPair = pivSession.GetMetadata(sourceSlot);
            Assert.Equal(generatedKeyPair.EncodeAsPiv(), metadataForKeyPair.PublicKeyParameters?.EncodeAsPiv());

            // Act
            pivSession.MoveKey(sourceSlot, destinationSlot);

            // Assert
            // Moved key slot should now be empty
            Assert.Throws<InvalidOperationException>(() => pivSession.GetMetadata(sourceSlot));

            var destinationMetadata = pivSession.GetMetadata(destinationSlot);
            Assert.Equal(generatedKeyPair.EncodeAsPiv(), destinationMetadata.PublicKeyParameters?.EncodeAsPiv());
        }

        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void MoveKey_WithImportedKey(KeyType expectedAlgorithm)
        {
            // Arrange
            const byte sourceSlot = PivSlot.Retired1;
            const byte destinationSlot = PivSlot.Retired20;

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            using var pivSession = new PivSession(testDevice);
            pivSession.KeyCollector = new Simple39KeyCollector().Simple39KeyCollectorDelegate;

            DeleteKeys(pivSession, sourceSlot, destinationSlot);

            var (publicKey, privateKey) = TestKeys.GetKeyPair(expectedAlgorithm);
            var importedPrivateKey = AsnPrivateKeyReader.CreatePrivateKey(privateKey.EncodedKey);
            var importedPublicKey = AsnPublicKeyReader.CreatePublicKey(publicKey.EncodedKey);

            pivSession.ImportPrivateKey(sourceSlot, importedPrivateKey);

            // Act
            pivSession.MoveKey(sourceSlot, destinationSlot);

            // Assert
            // Moved key slot should now be empty
            Assert.Throws<InvalidOperationException>(() => pivSession.GetMetadata(sourceSlot));

            var destinationMetadata = pivSession.GetMetadata(destinationSlot);
            Assert.Equal(importedPublicKey.EncodeAsPiv(), destinationMetadata.PublicKeyParameters?.EncodeAsPiv());
        }

        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void DeleteKey_WithImportedKey(KeyType expectedAlgorithm)
        {
            // Arrange
            const byte slotToDelete = PivSlot.Retired1;
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice();

            using var pivSession = new PivSession(testDevice);
            pivSession.KeyCollector = new Simple39KeyCollector().Simple39KeyCollectorDelegate;

            var testPrivateKey = TestKeys.GetTestPrivateKey(expectedAlgorithm);
            var privateKey = AsnPrivateKeyReader.CreatePrivateKey(testPrivateKey.EncodedKey);
            pivSession.ImportPrivateKey(slotToDelete, privateKey);

            // Act
            pivSession.DeleteKey(slotToDelete);

            // Assert
            // Key has been deleted and thus returns no data on the slot query
            Assert.Throws<InvalidOperationException>(() => pivSession.GetMetadata(slotToDelete));
        }

        private static byte[] GetRandomDataBuffer(KeyType expectedAlgorithm)
        {
            byte[] dataToSign = expectedAlgorithm switch
            {
                KeyType.RSA1024 => new byte[128],
                KeyType.RSA2048 => new byte[256],
                KeyType.RSA3072 => new byte[384],
                KeyType.RSA4096 => new byte[512],
                KeyType.ECP256 => new byte[32],
                KeyType.ECP384 => new byte[48],
                _ => throw new ArgumentException("what are you trying to do")
            };

            Random.Shared.NextBytes(dataToSign);
            dataToSign[0] &= 0x7F;
            return dataToSign;
        }

        private static void DeleteKeys(PivSession pivSession, byte sourceSlot, byte destinationSlot)
        {
            pivSession.DeleteKey(sourceSlot);
            pivSession.DeleteKey(destinationSlot);
        }
    }
}
