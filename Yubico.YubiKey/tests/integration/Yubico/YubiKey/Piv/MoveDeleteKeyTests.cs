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
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Converters;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class MoveDeleteKeyTests : PivSessionIntegrationTestBase
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

            DeleteKeys(Session, sourceSlot, destinationSlot);
            var devicePublicKey = Session.GenerateKeyPair(sourceSlot, expectedAlgorithm, PivPinPolicy.None);
            var devicePublicKeySpan = devicePublicKey.EncodeAsPiv().Span;

            // Act
            Session.MoveKey(sourceSlot, destinationSlot);
            var destinationMetadata = Session.GetMetadata(destinationSlot);

            // Assert
            // Moved key slot should now be empty
            Assert.Throws<InvalidOperationException>(() => Session.GetMetadata(sourceSlot));
            var movedPublicKey = destinationMetadata.PublicKeyParameters!.EncodeAsPiv().Span;
            var isMoved = devicePublicKeySpan.SequenceEqual(movedPublicKey);
            Assert.True(isMoved);
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
            var testPrivateKey = TestKeys.GetTestPrivateKey(expectedAlgorithm);

            DeleteKeys(Session, sourceSlot, destinationSlot);

            Session.ImportPrivateKey(sourceSlot, testPrivateKey.AsPrivateKey());
            var devicePublicKey = Session.GetMetadata(sourceSlot);

            // Act
            Session.MoveKey(sourceSlot, destinationSlot);
            var destinationMetadata = Session.GetMetadata(destinationSlot);

            // Assert
            // Moved key slot should now be empty
            Assert.Throws<InvalidOperationException>(() => Session.GetMetadata(sourceSlot));
            var movedPublicKey = devicePublicKey.PublicKeyParameters!.EncodeAsPiv().Span;
            var isMoved = devicePublicKey.PublicKeyParameters!.EncodeAsPiv().Span.SequenceEqual(movedPublicKey);
            Assert.True(isMoved);
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

            var testPrivateKey = TestKeys.GetTestPrivateKey(expectedAlgorithm);
            var privateKey = AsnPrivateKeyDecoder.CreatePrivateKey(testPrivateKey.EncodedKey);
            Session.ImportPrivateKey(slotToDelete, privateKey);

            // Act
            Session.DeleteKey(slotToDelete);

            // Assert
            // Key has been deleted and thus returns no data on the slot query
            Assert.Throws<InvalidOperationException>(() => Session.GetMetadata(slotToDelete));
        }

        private static void DeleteKeys(PivSession Session, byte sourceSlot, byte destinationSlot)
        {
            Session.DeleteKey(sourceSlot);
            Session.DeleteKey(destinationSlot);
        }
    }
}
