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
    [Trait(TraitTypes.Category, TestCategories.Elevated)]
    public class EnumCredsCommandTests : SimpleIntegrationTestConnection
    {
        public EnumCredsCommandTests()
            : base(YubiKeyApplication.Fido2)
        {
        }

        [Fact]
        public void EnumCredsCommand_Succeeds()
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

            (int rpCount, RelyingParty firstRp) = rsp.GetData();
            Assert.True(rpCount != 0);

            var credCmd = new EnumerateCredentialsBeginCommand(firstRp, pinToken, protocol);
            EnumerateCredentialsBeginResponse credRsp = Connection.SendCommand(credCmd);
            Assert.Equal(ResponseStatus.Success, credRsp.Status);

            (int credCount, CredentialUserInfo userInfo) = credRsp.GetData();
            Assert.True(credCount != 0);
            Assert.True(userInfo.CredProtectPolicy != CredProtectPolicy.None);

            for (int index = 1; index < credCount; index++)
            {
                var getNextCmd = new EnumerateCredentialsGetNextCommand();
                EnumerateCredentialsGetNextResponse getNextRsp = Connection.SendCommand(getNextCmd);
                Assert.Equal(ResponseStatus.Success, getNextRsp.Status);

                userInfo = getNextRsp.GetData();
                Assert.True(userInfo.CredProtectPolicy != CredProtectPolicy.None);
            }
        }

        [Fact]
        public void EnumCredsCommand_Preview_Succeeds()
        {
            byte[] pin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolOne();
            var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse getKeyRsp = Connection.SendCommand(getKeyCmd);
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);

            protocol.Encapsulate(getKeyRsp.GetData());
            var getTokenCmd = new GetPinTokenCommand(protocol, pin);
            GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status); //Fail
            ReadOnlyMemory<byte> pinToken = getTokenRsp.GetData();

            var cmd = new EnumerateRpsBeginCommand(pinToken, protocol)
            {
                IsPreview = true
            };
            EnumerateRpsBeginResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            (int rpCount, RelyingParty firstRp) = rsp.GetData();
            Assert.True(rpCount != 0);

            var credCmd = new EnumerateCredentialsBeginCommand(firstRp, pinToken, protocol)
            {
                IsPreview = true
            };
            EnumerateCredentialsBeginResponse credRsp = Connection.SendCommand(credCmd);
            Assert.Equal(ResponseStatus.Success, credRsp.Status);

            (int credCount, CredentialUserInfo userInfo) = credRsp.GetData();
            Assert.True(credCount != 0);
            Assert.True(userInfo.CredProtectPolicy != CredProtectPolicy.None);

            for (int index = 1; index < credCount; index++)
            {
                var getNextCmd = new EnumerateCredentialsGetNextCommand
                {
                    IsPreview = true
                };
                EnumerateCredentialsGetNextResponse getNextRsp = Connection.SendCommand(getNextCmd);
                Assert.Equal(ResponseStatus.Success, getNextRsp.Status);

                userInfo = getNextRsp.GetData();
                Assert.True(userInfo.CredProtectPolicy != CredProtectPolicy.None);
            }
        }
    }
}

