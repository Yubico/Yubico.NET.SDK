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
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2.Commands
{
    [Trait(TraitTypes.Category, TestCategories.RequiresBio)]
    public class GetAssertionCommandTests : NeedPinToken
    {
        public GetAssertionCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5Bio, null)
        {
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetAssertionCommand_Succeeds()
        {
            var protocol = new PinUvAuthProtocolTwo();

            bool isValid = GetParams(protocol, out GetAssertionParameters assertionParams);
            Assert.True(isValid);

            var cmd = new GetAssertionCommand(assertionParams);
            GetAssertionResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            GetAssertionData cData = rsp.GetData();
            if (!(cData.NumberOfCredentials is null) && cData.NumberOfCredentials > 0)
            {
                int count = (int)cData.NumberOfCredentials;
                for (int index = 1; index < count; index++)
                {
                    var nextCmd = new GetNextAssertionCommand();
                    rsp = Connection.SendCommand(nextCmd);
                    Assert.Equal(ResponseStatus.Success, rsp.Status);
                    cData = rsp.GetData();
                }
            }
            Assert.Equal(48, cData.CredentialId.Id.Length);
        }

        private bool GetParams(
            PinUvAuthProtocolBase protocol,
            out GetAssertionParameters assertionParams)
        {
            byte[] clientDataHash = {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            var rp = new RelyingParty("SomeRpId")
            {
                Name = "SomeRpName",
            };

            assertionParams = new GetAssertionParameters(rp, clientDataHash);

            if (!GetPinToken(protocol, PinUvAuthTokenPermissions.None, out byte[] pinToken))
            {
                return false;
            }

            byte[] pinUvAuthParam = protocol.AuthenticateUsingPinToken(pinToken, clientDataHash);

            assertionParams.Protocol = protocol.Protocol;
            assertionParams.PinUvAuthParam = pinUvAuthParam;

            //assertionParams.AddOption("rk", true);
            assertionParams.AddOption("up", true);
            //assertionParams.AddOption("uv", false);

            return true;
        }
    }
}
