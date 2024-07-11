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
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait("Category", "Simple")]
    public class PinProtectedIntegrationTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ReadPinProtect_IsEmpty_Correct(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.True(pinProtect.IsEmpty);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteMgmtKey_Read_NotEmpty(StandardTestDevice testDeviceType)
        {
            Memory<byte> mgmtKey = GetArbitraryMgmtKey();

            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(mgmtKey);

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();
                    pivSession.WriteObject(pinProtect);

                    PinProtectedData pinProtectCopy = pivSession.ReadObject<PinProtectedData>();
                    Assert.False(pinProtectCopy.IsEmpty);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteMgmtKey_Read_Correct(StandardTestDevice testDeviceType)
        {
            Memory<byte> mgmtKey = GetArbitraryMgmtKey();

            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(mgmtKey);

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();
                    pivSession.WriteObject(pinProtect);

                    PinProtectedData pinProtectCopy = pivSession.ReadObject<PinProtectedData>();

                    _ = Assert.NotNull(pinProtectCopy.ManagementKey);
                    if (!(pinProtectCopy.ManagementKey is null))
                    {
                        var getData = (ReadOnlyMemory<byte>)pinProtectCopy.ManagementKey;
                        bool isValid = mgmtKey.Span.SequenceEqual(getData.Span);
                        Assert.True(isValid);
                    }

                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        private Memory<byte> GetArbitraryMgmtKey()
        {
            byte[] keyData = {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68
            };

            return new Memory<byte>(keyData);
        }
    }
}
