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

using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class CredMgmtTests
    {
        private readonly IYubiKeyDevice _testDevice;

        public CredMgmtTests()
        {
            _testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Bio);
        }

        [Fact]
        public void GetMetadata_Succeeds()
        {
            using (var fido2Session = new Fido2Session(_testDevice))
            {
                fido2Session.KeyCollector = Fido2ResetForTest.ResetForTestKeyCollectorDelegate;
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.MakeCredential, "rp-1");

                CredentialManagementData mgmtData = fido2Session.GetCredentialMetadata();
                Assert.NotNull(mgmtData.NumberOfDiscoverableCredentials);
            }
        }
    }
}
