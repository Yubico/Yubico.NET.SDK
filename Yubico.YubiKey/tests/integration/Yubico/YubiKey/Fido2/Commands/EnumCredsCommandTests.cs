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
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.PinProtocols;
using Xunit;

namespace Yubico.YubiKey.Fido2
{
    public class EnumCredsCommandTests : SimpleIntegrationTestConnection
    {
        public EnumCredsCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio)
        {
        }

        [Fact]
        public void EnumCredsCommand_Succeeds()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolTwo();
            var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse getKeyRsp = Connection.SendCommand(getKeyCmd);
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);

            protocol.Encapsulate(getKeyRsp.GetData());
            PinUvAuthTokenPermissions permissions = PinUvAuthTokenPermissions.CredentialManagement;
            var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(protocol, pin, permissions, null);
            GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
            Assert.Equal(ResponseStatus.Success, getTokenRsp.Status);
            ReadOnlyMemory<byte> pinToken = getTokenRsp.GetData();

            var cmd = new EnumerateRpsBeginCommand(pinToken, protocol);
            CredentialManagementResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CredentialManagementData mgmtData = rsp.GetData();
            Assert.NotNull(mgmtData.RelyingPartyIdHash);
            if (mgmtData.RelyingPartyIdHash is null)
            {
                return;
            }

            var credCmd = new EnumerateCredentialsBeginCommand(mgmtData.RelyingPartyIdHash.Value, pinToken, protocol);
            rsp = Connection.SendCommand(credCmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            mgmtData = rsp.GetData();
            Assert.NotNull(mgmtData.TotalCredentialsForRelyingParty);

            int count = mgmtData.TotalCredentialsForRelyingParty ?? 0;
            for (int index = 1; index < count; index++)
            {
                var getNextCmd = new EnumerateCredentialsGetNextCommand();
                CredentialManagementResponse getNextRsp = Connection.SendCommand(getNextCmd);
                Assert.Equal(ResponseStatus.Success, getNextRsp.Status);

                mgmtData = getNextRsp.GetData();
                Assert.Null(mgmtData.TotalRelyingPartyCount);
            }
        }
    }
}
