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

using System;
using System.Linq;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class MakeCredentialBlobTests
    {

        [Fact]
        [Trait(TraitTypes.Category, TestCategories.Elevated)]
        public void CredBlobExtension_Correct()
        {
            var yubiKeyDevice = IntegrationTestDeviceEnumeration.GetTestDevice();
            var connection = yubiKeyDevice.Connect(YubiKeyApplication.Fido2);

            var getInfoCmd = new GetInfoCommand();
            var getInfoRsp = connection.SendCommand(getInfoCmd);
            Assert.Equal(ResponseStatus.Success, getInfoRsp.Status);

            var authInfo = getInfoRsp.GetData();
            Assert.Equal(32, authInfo.MaximumCredentialBlobLength); /* Assert.Equal() Failure: Values differExpected: 32 Actual: null */

            var maxCredBlobLength = authInfo.MaximumCredentialBlobLength ?? 0;
            var isValid = authInfo.IsExtensionSupported("credBlob") && maxCredBlobLength > 0;
            Assert.True(isValid);
        }

        [Fact]
        public void MakeCredentialCommand_Succeeds()
        {
            var yubiKey = YubiKeyDevice.FindAll().First();

            using var fido2Session = new Fido2Session(yubiKey);

            var isValid = fido2Session.TryVerifyPin(FidoSessionIntegrationTestBase.TestPinDefault, null, null, out var retriesRemaining, out var reboot);
            Assert.True(isValid);
            Assert.True(fido2Session.AuthenticatorInfo.IsExtensionSupported("largeBlobKey"));

            isValid = GetParams(fido2Session, out var makeParams);
            Assert.True(isValid);

            var keyCollector = new TestKeyCollector();
            fido2Session.KeyCollector = keyCollector.HandleRequest;

            var mcData = fido2Session.MakeCredential(makeParams);
            Assert.True(mcData.VerifyAttestation(FidoSessionIntegrationTestBase.ClientDataHash));
        }

        private bool GetParams(
            Fido2Session fido2Session,
            out MakeCredentialParameters makeParams)
        {

            byte[] arbitraryData =
            {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A
            };

            var rp = new RelyingParty("SomeRpId")
            {
                Name = "SomeRpName",
            };
            byte[] userId = { 0x11, 0x22, 0x33, 0x44 };
            var user = new UserEntity(new ReadOnlyMemory<byte>(userId))
            {
                Name = "SomeUserName",
                DisplayName = "User",
            };

            makeParams = new MakeCredentialParameters(rp, user);

            if (fido2Session.AuthToken is null)
            {
                return false;
            }

            var token = (ReadOnlyMemory<byte>)fido2Session.AuthToken;
            var pinUvAuthParam = fido2Session.AuthProtocol.AuthenticateUsingPinToken(
                token.ToArray(), FidoSessionIntegrationTestBase.ClientDataHash);

            makeParams.ClientDataHash = FidoSessionIntegrationTestBase.ClientDataHash;
            makeParams.Protocol = fido2Session.AuthProtocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, true);
            makeParams.AddExtension("largeBlob", arbitraryData);

            return true;
        }
    }
}
