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
    [TestCaseOrderer(PriorityOrderer.TypeName, PriorityOrderer.AssembyName)]
    [Trait("Category", "Simple")]
    public sealed class OathSessionPasswordTests
    {
        [Theory, TestPriority(0)]
        [InlineData(StandardTestDevice.Fw5)]
        public void SetPassword(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                oathSession.SetPassword();

                Assert.False(oathSession._oathData.Challenge.IsEmpty);
            }
        }

        [Theory, TestPriority(1)]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyCorrectPassword(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                bool isVerified = oathSession.TryVerifyPassword();
                Assert.True(isVerified);
            }
        }

        [Theory, TestPriority(2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyWrongPassword(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                collectorObj.KeyFlag = 1;

                bool isVerified = oathSession.TryVerifyPassword();
                Assert.False(isVerified);
            }
        }

        [Theory, TestPriority(3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePassword(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                collectorObj.KeyFlag = 1;
                oathSession.SetPassword();

                Assert.False(oathSession._oathData.Challenge.IsEmpty);
            }
        }

        [Theory, TestPriority(4)]
        [InlineData(StandardTestDevice.Fw5)]
        public void UnsetPassword(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                oathSession.ResetApplication();

                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                oathSession.SetPassword();

                Assert.True(oathSession.IsPasswordProtected);
            }

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                oathSession.UnsetPassword();

                Assert.False(oathSession.IsPasswordProtected);
            }
        }
    }
}
