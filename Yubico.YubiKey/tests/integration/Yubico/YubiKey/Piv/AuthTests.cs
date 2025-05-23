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

using System.Security;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class AuthTests : PivSessionIntegrationTestBase
    {
        public AuthTests()
        {
            var collectorObj = new Simple39KeyCollector
            {
                KeyFlag = 1,
                RetryFlag = 1
            };

            Session.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyPin_Blocked_ThrowsSecurityException(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            _ = Assert.Throws<SecurityException>(() => Session.VerifyPin());
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePin_Blocked_ThrowsSecurityException(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            _ = Assert.Throws<SecurityException>(() => Session.ChangePin());
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePuk_Blocked_ThrowsSecurityException(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            _ = Assert.Throws<SecurityException>(() => Session.ChangePuk());
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ResetPin_Blocked_ThrowsSecurityException(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            _ = Assert.Throws<SecurityException>(() => Session.ResetPin());
        }
    }
}
