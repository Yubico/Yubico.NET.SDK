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

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class PinCollectionTests : FidoSessionIntegrationTestBase
    {
        [Fact]
        [Trait(TraitTypes.Category, TestCategories.RequiresSetup)]
        [Trait(TraitTypes.Category, TestCategories.Elevated)]
        public void PinOperations_Succeed()
        {
            Session.ChangePin();
            Session.VerifyPin();

            var isValid = Session.TryChangePin(TestPin1, TestPin2);
            Assert.False(isValid);

            isValid = Session.TryChangePin(TestPin2, TestPin1);
            Assert.True(isValid);

            isValid = Session.TryVerifyPin(TestPin2, null, null, out _, out _);
            Assert.False(isValid);

            isValid = Session.TryVerifyPin(TestPin1, null, null, out _, out _);
            Assert.True(isValid);
        }

        [Fact]
        [Trait(TraitTypes.Category, TestCategories.RequiresSetup)]
        public void InvalidPinFollowedByValidPin_Succeeds()
        {
            var invalidPin = "000000"u8.ToArray();
            var validPin = TestPin1.ToArray();

            var success = Session.TryVerifyPin(invalidPin, PinUvAuthTokenPermissions.CredentialManagement, "", out _, out _);
            Assert.False(success);

            success = Session.TryVerifyPin(validPin, PinUvAuthTokenPermissions.CredentialManagement, "", out _, out _);
            Assert.True(success);
        }
    }
}
