// Copyright 2024 Yubico AB
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
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey
{
    public class DeviceResetTests
    {
        [SkippableFact]
        public void Reset()
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Bio);
            testDevice.DeviceReset();
            
            using var fido2Session = new Fido2Session(testDevice);
            var optionValue = fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.clientPin);
            Assert.True(optionValue == OptionValue.False);
        }
    }
}
