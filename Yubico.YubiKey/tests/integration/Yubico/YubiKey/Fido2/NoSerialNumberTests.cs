// Copyright 2023 Yubico AB
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

namespace Yubico.YubiKey.Fido2
{
    public class NoSerialNumberTests
    {
        [Fact]
        [Trait("Category", "Simple")]
        public void GetTestDevice_NoSerialNumber_Succeeds()
        {
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5, false);
            Assert.NotNull(device);
            if (device.SerialNumber is null)
            {
                _ = Assert.Throws<InvalidOperationException>(() => IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5));
            }
        }
    }
}
