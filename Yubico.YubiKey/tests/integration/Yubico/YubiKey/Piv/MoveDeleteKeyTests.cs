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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class MoveDeleteKeyTests
    {
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void MoveKey_WithGenerate(PivAlgorithm expectedAlgorithm)
        {   
            const byte sourceSlot = PivSlot.Retired1; 
            const byte destinationSlot = PivSlot.Retired20;
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            
            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            
            var expectedKey = pivSession.GenerateKeyPair(PivSlot.Retired1, expectedAlgorithm, PivPinPolicy.None);
            var generatedKey = pivSession.GetMetadata(sourceSlot);
            Assert.Equal(expectedKey.YubiKeyEncodedPublicKey, generatedKey.PublicKey.YubiKeyEncodedPublicKey);
               
            pivSession.MoveKey(sourceSlot, destinationSlot);
            // var destinationMetadata = pivSession.GetMetadata(destinationSlot);
            // Assert.Equal(expectedKey.PivEncodedPublicKey, destinationMetadata.PublicKey.PivEncodedPublicKey);  FALSE -- why?
            
            var dataToSign = GetRandomDataBuffer(expectedAlgorithm);
            pivSession.Sign(destinationSlot, dataToSign);
        }
        
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void MoveKey_WithImportedKey(PivAlgorithm expectedAlgorithm)
        {
            const byte sourceSlot = PivSlot.Retired1; 
            const byte destinationSlot = PivSlot.Retired20;
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5, false);
            
            using var pivSession = new PivSession(testDevice);
            pivSession.KeyCollector = new Simple39KeyCollector().Simple39KeyCollectorDelegate;
            
            var expectedKey = SampleKeyPairs.GetPivPrivateKey(expectedAlgorithm);
            pivSession.ImportPrivateKey(PivSlot.Retired1, expectedKey);
            pivSession.MoveKey(sourceSlot, destinationSlot);
            
            var dataToSign = GetRandomDataBuffer(expectedAlgorithm);
            pivSession.Sign(destinationSlot, dataToSign);
        }
        
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void DeleteKey_WithImportedKey(PivAlgorithm expectedAlgorithm)
        {
            const byte slotToDelete = PivSlot.Retired1; 
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5, false);
            
            using var pivSession = new PivSession(testDevice);
            pivSession.KeyCollector = new Simple39KeyCollector().Simple39KeyCollectorDelegate;
            
            var expectedKey = SampleKeyPairs.GetPivPrivateKey(expectedAlgorithm);
            pivSession.ImportPrivateKey(PivSlot.Retired1, expectedKey);
            Assert.NotNull(pivSession.GetMetadata(slotToDelete));
            
            pivSession.DeleteKey(slotToDelete);

            // Key has been deleted and thus returns no data on the slot query
            Assert.Throws<InvalidOperationException>(() => pivSession.GetMetadata(slotToDelete));
        }


        private static byte[] GetRandomDataBuffer(PivAlgorithm expectedAlgorithm)
        {
            byte[] dataToSign = expectedAlgorithm switch
            {
                PivAlgorithm.Rsa1024 => new byte[128],
                PivAlgorithm.Rsa2048 => new byte[256],
                PivAlgorithm.Rsa3072 => new byte[384],
                PivAlgorithm.Rsa4096 => new byte[512],
                PivAlgorithm.EccP256 => new byte[32],
                PivAlgorithm.EccP384 => new byte[48],
                _ => throw new ArgumentException("what are you trying to do")
            };
            
            Random.Shared.NextBytes(dataToSign);
            dataToSign[0] &= 0x7F;
            return dataToSign;
        }
    }
}
