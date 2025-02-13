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
using Xunit;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2.Commands
{
    [Trait(TraitTypes.Category, TestCategories.RequiresBio)]
    public class MakeCredentialCommandTests : NeedPinToken
    {
        public MakeCredentialCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5Bio, null)
        {
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void MakeCredentialCommand_Succeeds()
        {
            var protocol = new PinUvAuthProtocolTwo();

            bool isValid = GetParams(protocol, out MakeCredentialParameters makeParams);
            Assert.True(isValid);

            var cmd = new MakeCredentialCommand(makeParams);
            MakeCredentialResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            MakeCredentialData cData = rsp.GetData();
            isValid = cData.VerifyAttestation(makeParams.ClientDataHash);
            Assert.True(isValid);
        }

        private bool GetParams(
            PinUvAuthProtocolBase protocol,
            out MakeCredentialParameters makeParams)
        {
            byte[] clientDataHash = {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
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

            if (!GetPinToken(protocol, PinUvAuthTokenPermissions.None, out byte[] pinToken))
            {
                return false;
            }

            byte[] pinUvAuthParam = protocol.AuthenticateUsingPinToken(pinToken, clientDataHash);

            makeParams.ClientDataHash = clientDataHash;
            makeParams.Protocol = protocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, true);
            //makeParams.AddOption("up", true);
            //makeParams.AddOption("uv", false);

            return true;
        }
    }
}
