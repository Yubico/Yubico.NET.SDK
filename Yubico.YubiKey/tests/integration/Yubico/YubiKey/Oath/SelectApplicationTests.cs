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

using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    [Trait("Category", "Simple")]
    public sealed class SelectApplicationTests
    {
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void ConnectOathHasData(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using IYubiKeyConnection connection = testDevice.Connect(YubiKeyApplication.Oath);

            Assert.NotNull(connection!.SelectApplicationData);
            OathApplicationData data = Assert.IsType<OathApplicationData>(connection!.SelectApplicationData);

            Assert.False(data.Salt.IsEmpty);
            Assert.True(data.Salt.Length >= 8);
        }
    }
}
