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
    public class AdminIntegrationTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ReadAdmin_IsEmpty_Correct(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                AdminData admin = pivSession.ReadObject<AdminData>();

                Assert.True(admin.IsEmpty);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteAdminData_Read_NotEmpty(StandardTestDevice testDeviceType)
        {
            byte[] salt = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            using var admin = new AdminData();
            admin.PukBlocked = true;
            admin.PinProtected = true;
            admin.SetSalt(salt);
            admin.PinLastUpdated = DateTime.UtcNow;

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();
                    pivSession.WriteObject(admin);

                    AdminData adminCopy = pivSession.ReadObject<AdminData>();
                    Assert.False(adminCopy.IsEmpty);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteAdminData_Read_Correct(StandardTestDevice testDeviceType)
        {
            byte[] salt = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            using var admin = new AdminData();
            admin.PukBlocked = true;
            admin.PinProtected = true;
            admin.SetSalt(salt);
            admin.PinLastUpdated = DateTime.UtcNow;

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();
                    pivSession.WriteObject(admin);

                    AdminData adminCopy = pivSession.ReadObject<AdminData>();
                    _ = Assert.NotNull(adminCopy.Salt);
                    _ = Assert.NotNull(adminCopy.PinLastUpdated);

                    if (!(adminCopy.Salt is null))
                    {
                        var cmpObj = (ReadOnlyMemory<byte>)adminCopy.Salt;
                        var expected = new Span<byte>(salt);
                        bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, cmpObj.Span);
                        Assert.True(isValid);
                    }

                    Assert.True(adminCopy.PukBlocked);
                    Assert.True(adminCopy.PinProtected);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }
    }
}
