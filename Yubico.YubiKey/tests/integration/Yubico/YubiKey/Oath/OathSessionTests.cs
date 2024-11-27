﻿// Copyright 2021 Yubico AB
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

using System.Collections.Generic;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    public sealed class OathSessionTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ResetOathApplication(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                oathSession.ResetApplication();
                IList<Credential> data = oathSession.GetCredentials();

                Assert.True(oathSession._oathData.Challenge.IsEmpty);
                Assert.Empty(data);
            }
        }
    }
}
