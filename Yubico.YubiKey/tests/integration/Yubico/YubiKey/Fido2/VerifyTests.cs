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
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

[Trait(TraitTypes.Category, TestCategories.Elevated)]
[Trait(TraitTypes.Category, TestCategories.RequiresSetup)]
[Trait(TraitTypes.Category, TestCategories.RequiresBio)]
public class VerifyTests : FidoSessionIntegrationTestBase
{
    // Requires a biometric-capable YubiKey with a fingerprint enrolled.
    // Also requires that the PIN is set to the default value.
    [SkippableFact(typeof(DeviceNotFoundException))]
    public void VerifyUv_Succeed()
    {
        Session.VerifyUv(PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion,
            "relyingParty1");
    }
}
