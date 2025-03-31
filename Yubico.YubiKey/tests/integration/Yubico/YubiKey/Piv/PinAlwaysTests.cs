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

using System.Linq;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PinAlwaysTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void PinAlways_Sign_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            byte slotNumber = 0x9A;

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                _ = pivSession.GenerateKeyPair(
                    slotNumber, KeyType.P256, PivPinPolicy.Always, PivTouchPolicy.Never);

                byte[] dataToSign = {
                    0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                    0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
                    0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88
                };

                byte[] signature1 = pivSession.Sign(slotNumber, dataToSign, KeyType.P256);
                Assert.Equal(0x30, signature1[0]);

                byte[] signature2 = pivSession.Sign(slotNumber, dataToSign, KeyType.P256);
                bool isSame = signature1.SequenceEqual(signature2);
                Assert.False(isSame);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void Slot9C_Default_Sign_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            byte slotNumber = 0x9C;

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                _ = pivSession.GenerateKeyPair(
                    slotNumber, KeyType.P256, PivPinPolicy.Default, PivTouchPolicy.Never);

                byte[] dataToSign = {
                    0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                    0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                    0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
                    0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88
                };

                byte[] signature1 = pivSession.Sign(slotNumber, dataToSign, KeyType.P256);
                Assert.Equal(0x30, signature1[0]);

                byte[] signature2 = pivSession.Sign(slotNumber, dataToSign, KeyType.P256);
                bool isSame = signature1.SequenceEqual(signature2);
                Assert.False(isSame);
            }
        }
    }
}
