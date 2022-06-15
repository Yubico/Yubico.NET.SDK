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
using Moq;
using Xunit;
using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey
{
    public class YubiKeyDeviceExtensionsTests
    {
        [Fact]
        public void WithScp03_WhenDeviceIsNull_Throws()
        {
            YubiKeyDevice? ykDevice = null;
            _ = Assert.Throws<ArgumentNullException>(() => ykDevice!.WithScp03(new StaticKeys()));
        }

        [Fact]
        public void WithScp03_WhenDeviceDoesNotHaveSmartCard_ThrowsException()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.All,
                EnabledUsbCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);
            _ = Assert.Throws<NotSupportedException>(() => ykDevice.WithScp03(new StaticKeys()));
        }

        [Fact]
        public void WithScp03_WhenDeviceCorrect_Succeeds()
        {
            var mockSmartCard = new Mock<ISmartCardDevice>();
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.All,
                EnabledUsbCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(mockSmartCard.Object, null, null, ykDeviceInfo);
            IYubiKeyDevice scp03Device = ykDevice.WithScp03(new StaticKeys());
            _ = Assert.IsAssignableFrom<Scp03YubiKeyDevice>(scp03Device);
        }
    }
}
