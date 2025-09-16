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
using System.Threading;
using Xunit;
using Yubico.PlatformInterop;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey
{
    [Trait(TraitTypes.Category, TestCategories.Elevated)]
    public class YubiKeyDeviceListenerTests
    {
        private IYubiKeyDevice WaitForDevice()
        {
            IYubiKeyDevice? device = null;

            AutoResetEvent reset = new AutoResetEvent(false);
            EventHandler<YubiKeyDeviceEventArgs> handler = (sender, args) =>
            {
                device = args.Device;
                reset.Set();
            };

            YubiKeyDeviceListener.Instance.Arrived += handler;
            reset.WaitOne();
            YubiKeyDeviceListener.Instance.Arrived -= handler;

            Assert.NotNull(device);
            return device;
        }

        [Fact]
        public void KeyArrived_SkyEe_IsSkySeriesIsTrue()
        {
            // Needs to run elevated so the listener finds and enumerates any hidFido
            // devices else no Merge will happen, and it won't be a valid test
            // See https://github.com/Yubico/Yubico.NET.SDK/issues/156
            Assert.True(SdkPlatformInfo.IsElevated);

            IYubiKeyDevice device = WaitForDevice();

            Assert.True(device.IsSkySeries);
        }
    }
}
