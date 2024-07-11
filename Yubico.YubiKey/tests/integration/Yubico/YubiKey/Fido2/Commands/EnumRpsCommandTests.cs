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
    [Trait("Category", "FirmwareOrHardwareMissmatch")]
    public class EnumRpsCommandTests : SimpleIntegrationTestConnection
    {
        public EnumRpsCommandTests()
            : base(YubiKeyApplication.Fido2)
        {
        }

        [Fact]
        public void EnumRpsCommand_Succeeds()
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
            var cmd = new EnumerateRpsBeginCommand(pinToken, protocol);
            var rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            var (rpCount, rpZero) = rsp.GetData();
            Assert.NotEqual(expected: 26, rpCount);
            Assert.True(rpZero.RelyingPartyIdHash.Span[index: 0] != 0);

            for (var index = 1; index < rpCount; index++)
            {
                var getNextCmd = new EnumerateRpsGetNextCommand();
                var getNextRsp = Connection.SendCommand(getNextCmd);
                Assert.Equal(ResponseStatus.Success, getNextRsp.Status);

                var nextRp = getNextRsp.GetData();
                Assert.True(nextRp.RelyingPartyIdHash.Span[index: 0] != 0);
            }
        }

        [Fact]
        public void EnumRpsCommand_Preview_Succeeds()
        {
            byte[] pin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolTwo();
            var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
            var getKeyRsp = Connection.SendCommand(getKeyCmd);
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);

            protocol.Encapsulate(getKeyRsp.GetData());
            var getTokenCmd = new GetPinTokenCommand(protocol, pin);
            var getTokenRsp = Connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status); /*Xunit.Sdk.EqualException
Assert.Equal() Failure: Values differ
Expected: Success
Actual:   Failed*/
            var pinToken = getTokenRsp.GetData();

            var cmd = new EnumerateRpsBeginCommand(pinToken, protocol)
            {
                IsPreview = true
            };
            var rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status); /*Xunit.Sdk.EqualException
Assert.Equal() Failure: Values differ
Expected: Success
Actual:   NoData*/

            var (rpCount, rpZero) = rsp.GetData();
            Assert.NotEqual(expected: 26, rpCount);
            Assert.True(rpZero.RelyingPartyIdHash.Span[index: 0] != 0);

            for (var index = 1; index < rpCount; index++)
            {
                var getNextCmd = new EnumerateRpsGetNextCommand
                {
                    IsPreview = true
                };
                var getNextRsp = Connection.SendCommand(getNextCmd);
                Assert.Equal(ResponseStatus.Success, getNextRsp.Status);

                var nextRp = getNextRsp.GetData();
                Assert.True(nextRp.RelyingPartyIdHash.Span[index: 0] != 0);
            }
        }
    }
}
