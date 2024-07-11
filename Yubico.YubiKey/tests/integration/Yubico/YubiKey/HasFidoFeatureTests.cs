// Copyright 2022 Yubico AB
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

using System.Linq;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey
{
    public class HasFidoFeatureTests
    {
        [Fact]
        public void HasFeature_ApplicationOTP_Correct()
        {
            IYubiKeyDevice yubiKeyDevice = YubiKeyDevice.FindByTransport().First();

            bool hasFeature = yubiKeyDevice.HasFeature(YubiKeyFeature.OtpApplication);
            bool expectedResult = !yubiKeyDevice.IsSkySeries;
            Assert.Equal(hasFeature, expectedResult);
        }

        [Fact]
        public void HasFeature_ApplicationU2F_Correct()
        {
            IYubiKeyDevice yubiKeyDevice = IntegrationTestDeviceEnumeration.GetTestDevices().First();
            bool expectedResult = true;

            //Can this be removed?
            if (!yubiKeyDevice.IsSkySeries && yubiKeyDevice.FirmwareVersion.Major < 3)
            {
                expectedResult = false;
            }

            bool hasFeature = yubiKeyDevice.HasFeature(YubiKeyFeature.U2fApplication);

            Assert.Equal(hasFeature, expectedResult);
        }

        [Fact]
        public void HasFeature_ApplicationFido2_Correct()
        {
            IYubiKeyDevice yubiKeyDevice = IntegrationTestDeviceEnumeration.GetTestDevices().First();
            bool expectedResult = true;
            if (!yubiKeyDevice.IsSkySeries && yubiKeyDevice.FirmwareVersion.Major < 5)
            {
                expectedResult = false;
            }

            bool hasFeature = yubiKeyDevice.HasFeature(YubiKeyFeature.Fido2Application);

            Assert.Equal(hasFeature, expectedResult);
        }
    }
}
