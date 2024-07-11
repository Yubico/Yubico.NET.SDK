﻿// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey
{
    public class YubiKeyDeviceYubiHsmAuth
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void TryConnect_YubiHsmAuth(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            bool result = testDevice.TryConnect(YubiKeyApplication.YubiHsmAuth, out _);

            Assert.True(result);
        }
    }
}
