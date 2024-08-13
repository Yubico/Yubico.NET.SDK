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

namespace Yubico.YubiKey.Fido2
{
    public class GetAuthenticatorInfoTests
    {
        [Fact]
        [Trait(TraitTypes.Category, TestCategories.Elevated)]
        public void GetAuthenticator_Succeeds()
        {
            IYubiKeyDevice yubiKey = YubiKeyDevice.FindAll().First();

            using (var fido2 = new Fido2Session(yubiKey))
            {
                Assert.True(fido2.AuthenticatorInfo.Aaguid.Length > 0);
                Assert.NotNull(fido2.AuthenticatorInfo.PinUvAuthProtocols);
            }
        }
    }
}
