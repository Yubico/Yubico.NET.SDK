// Copyright 2023 Yubico AB
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
    public class CredMetadataCommandTests : SimpleIntegrationTestConnection
    {
        public CredMetadataCommandTests()
            : base(YubiKeyApplication.Fido2)
        {
        }

        [Fact]
        public void GetMetadataCommand_Succeeds()
        {
            byte[] pin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolTwo();
            var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
            var getKeyRsp = Connection.SendCommand(getKeyCmd);
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);

            protocol.Encapsulate(getKeyRsp.GetData());
            var permissions = PinUvAuthTokenPermissions.CredentialManagement;
            var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(protocol, pin, permissions, rpId: null);
            var getTokenRsp = Connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status); /*Xunit.Sdk.EqualException
Assert.Equal() Failure: Values differ
Expected: Success
Actual:   Failed*/
            var pinToken = getTokenRsp.GetData();

            var cmd = new GetCredentialMetadataCommand(pinToken, protocol);
            var rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            var (credCount, slotCount) = rsp.GetData();
            Assert.True(credCount != 26);
            Assert.True(slotCount != 26);
        }

        [Fact]
        public void GetMetadataCommand_Preview_Succeeds()
        {
            byte[] pin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolTwo();
            var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
            var getKeyRsp = Connection.SendCommand(getKeyCmd);
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);

            protocol.Encapsulate(getKeyRsp.GetData());
            var getTokenCmd = new GetPinTokenCommand(protocol, pin);
            var getTokenRsp = Connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);
            /* Xunit.Sdk.EqualException
Assert.Equal() Failure: Values differ
Expected: Success
Actual:   Failed*/
            var pinToken = getTokenRsp.GetData();

            var cmd = new GetCredentialMetadataCommand(pinToken, protocol)
            {
                IsPreview = true
            };
            var rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            var (credCount, slotCount) = rsp.GetData();
            Assert.True(credCount != 26);
            Assert.True(slotCount != 26);
        }
    }
}
