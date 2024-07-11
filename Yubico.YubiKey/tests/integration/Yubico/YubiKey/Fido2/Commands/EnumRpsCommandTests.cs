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

using System;
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
            GetKeyAgreementResponse getKeyRsp = Connection.SendCommand(getKeyCmd);
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);

            protocol.Encapsulate(getKeyRsp.GetData());
            PinUvAuthTokenPermissions permissions = PinUvAuthTokenPermissions.CredentialManagement;
            var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(protocol, pin, permissions, null);
            GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status); /*Xunit.Sdk.EqualException
Assert.Equal() Failure: Values differ
Expected: Success
Actual:   Failed*/

            ReadOnlyMemory<byte> pinToken = getTokenRsp.GetData();
            var cmd = new EnumerateRpsBeginCommand(pinToken, protocol);
            EnumerateRpsBeginResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            (int rpCount, RelyingParty rpZero) = rsp.GetData();
            Assert.NotEqual(26, rpCount);
            Assert.True(rpZero.RelyingPartyIdHash.Span[0] != 0);

            for (int index = 1; index < rpCount; index++)
            {
                var getNextCmd = new EnumerateRpsGetNextCommand();
                EnumerateRpsGetNextResponse getNextRsp = Connection.SendCommand(getNextCmd);
                Assert.Equal(ResponseStatus.Success, getNextRsp.Status);

                RelyingParty nextRp = getNextRsp.GetData();
                Assert.True(nextRp.RelyingPartyIdHash.Span[0] != 0);
            }
        }

        [Fact]
        public void EnumRpsCommand_Preview_Succeeds()
        {
            byte[] pin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolTwo();
            var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse getKeyRsp = Connection.SendCommand(getKeyCmd);
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);

            protocol.Encapsulate(getKeyRsp.GetData());
            var getTokenCmd = new GetPinTokenCommand(protocol, pin);
            GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status); /*Xunit.Sdk.EqualException
Assert.Equal() Failure: Values differ
Expected: Success
Actual:   Failed*/
            ReadOnlyMemory<byte> pinToken = getTokenRsp.GetData();

            var cmd = new EnumerateRpsBeginCommand(pinToken, protocol)
            {
                IsPreview = true
            };
            EnumerateRpsBeginResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status); /*Xunit.Sdk.EqualException
Assert.Equal() Failure: Values differ
Expected: Success
Actual:   NoData*/

            (int rpCount, RelyingParty rpZero) = rsp.GetData();
            Assert.NotEqual(26, rpCount);
            Assert.True(rpZero.RelyingPartyIdHash.Span[0] != 0);

            for (int index = 1; index < rpCount; index++)
            {
                var getNextCmd = new EnumerateRpsGetNextCommand
                {
                    IsPreview = true
                };
                EnumerateRpsGetNextResponse getNextRsp = Connection.SendCommand(getNextCmd);
                Assert.Equal(ResponseStatus.Success, getNextRsp.Status);

                RelyingParty nextRp = getNextRsp.GetData();
                Assert.True(nextRp.RelyingPartyIdHash.Span[0] != 0);
            }
        }
    }
}
