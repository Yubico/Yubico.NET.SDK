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

using Xunit;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    [TestCaseOrderer(PriorityOrderer.TypeName, PriorityOrderer.AssembyName)]
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public sealed class OathSessionPasswordTests
    {
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, false)]
        [InlineData(StandardTestDevice.Fw5, true)]
        [InlineData(StandardTestDevice.Fw5Fips, false)]
        [InlineData(StandardTestDevice.Fw5Fips, true)]
        public void SetPassword(
            StandardTestDevice testDeviceType, bool useScp)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            var keyParameters = useScp ? Scp03KeyParameters.DefaultKey : null;
            using (var resetSession = new OathSession(testDevice, keyParameters))
            {
                resetSession.ResetApplication();
            }

            using var oathSession = new OathSession(testDevice, keyParameters);
            var collectorObj = new SimpleOathKeyCollector();
            oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

            oathSession.SetPassword();

            Assert.False(oathSession._oathData.Challenge.IsEmpty);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, false)]
        [InlineData(StandardTestDevice.Fw5, true)]
        [InlineData(StandardTestDevice.Fw5Fips, false)]
        [InlineData(StandardTestDevice.Fw5Fips, true)]
        public void VerifyCorrectPassword(
            StandardTestDevice testDeviceType, bool useScp)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            SetTestPassword(testDevice);

            using var oathSession = new OathSession(testDevice, useScp ? Scp03KeyParameters.DefaultKey : null);
            var collectorObj = new SimpleOathKeyCollector();
            oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

            var isVerified = oathSession.TryVerifyPassword();

            Assert.True(isVerified);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, false)]
        [InlineData(StandardTestDevice.Fw5, true)]
        [InlineData(StandardTestDevice.Fw5Fips, false)]
        [InlineData(StandardTestDevice.Fw5Fips, true)]
        public void VerifyWrongPassword(
            StandardTestDevice testDeviceType, bool useScp)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            SetTestPassword(testDevice);

            using var oathSession = new OathSession(testDevice, useScp ? Scp03KeyParameters.DefaultKey : null);
            var collectorObj = new SimpleOathKeyCollector();
            oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

            collectorObj.KeyFlag = 1;

            var isVerified = oathSession.TryVerifyPassword();
            Assert.False(isVerified);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, false)]
        [InlineData(StandardTestDevice.Fw5, true)]
        [InlineData(StandardTestDevice.Fw5Fips, false)]
        [InlineData(StandardTestDevice.Fw5Fips, true)]
        public void ChangePassword(
            StandardTestDevice testDeviceType, bool useScp)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            SetTestPassword(testDevice);

            using var oathSession = new OathSession(testDevice, useScp ? Scp03KeyParameters.DefaultKey : null);
            var collectorObj = new SimpleOathKeyCollector();
            oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;
            collectorObj.KeyFlag = 1;

            oathSession.SetPassword();

            Assert.False(oathSession._oathData.Challenge.IsEmpty);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, false)]
        [InlineData(StandardTestDevice.Fw5, true)]
        [InlineData(StandardTestDevice.Fw5Fips, false)]
        [InlineData(StandardTestDevice.Fw5Fips, true)]
        public void UnsetPassword(
            StandardTestDevice testDeviceType, bool useScp)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            SetTestPassword(testDevice);
            using var oathSession = new OathSession(testDevice, useScp ? Scp03KeyParameters.DefaultKey : null);
            var collectorObj = new SimpleOathKeyCollector();
            oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

            oathSession.UnsetPassword();

            Assert.False(oathSession.IsPasswordProtected);
        }

        private void SetTestPassword(IYubiKeyDevice testDevice, Scp03KeyParameters? keyParameters = null)
        {
            using (var resetSession = new OathSession(testDevice, keyParameters))
            {
                resetSession.ResetApplication();
            }

            using (var oathSession = new OathSession(testDevice, keyParameters))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;
                oathSession.SetPassword();

                Assert.True(oathSession.IsPasswordProtected);
            }
        }
    }
}
