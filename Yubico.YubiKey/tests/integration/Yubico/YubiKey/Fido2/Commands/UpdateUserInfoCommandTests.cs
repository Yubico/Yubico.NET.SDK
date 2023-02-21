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
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Fido2
{
    public class UpdateUserInfoCommandTests : SimpleIntegrationTestConnection
    {
        public UpdateUserInfoCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio)
        {
        }

        [Fact]
        public void UpdateInfoCommand_Succeeds()
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

            CredentialManagementData rpMgmtData = rsp.GetData();
            Assert.NotNull(rpMgmtData.RelyingPartyIdHash);
            if (rpMgmtData.RelyingPartyIdHash is null)
            {
                return;
            }

            var credCmd = new EnumerateCredentialsBeginCommand(rpMgmtData.RelyingPartyIdHash.Value, pinToken, protocol);
            rsp = Connection.SendCommand(credCmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CredentialManagementData origMgmtData = rsp.GetData();
            Assert.NotNull(origMgmtData.TotalCredentialsForRelyingParty);
            Assert.NotNull(origMgmtData.CredentialId);
            Assert.NotNull(origMgmtData.User);

            if ((origMgmtData.CredentialId is null) || (origMgmtData.User is null))
            {
                return;
            }

            Assert.NotNull(origMgmtData.User.DisplayName);
            int count = origMgmtData.TotalCredentialsForRelyingParty ?? 0;

            if (origMgmtData.User.DisplayName is null)
            {
                return;
            }

            var newInfo = new UserEntity(origMgmtData.User.Id)
            {
                Name = origMgmtData.User.Name,
                DisplayName = origMgmtData.User.DisplayName + " Updated",
            };

            Assert.NotEqual(0, count);
            var updateCmd = new UpdateUserInfoCommand(origMgmtData.CredentialId, newInfo, pinToken, protocol);
            Fido2Response updateRsp = Connection.SendCommand(updateCmd);
            Assert.Equal(ResponseStatus.Success, updateRsp.Status);

            credCmd = new EnumerateCredentialsBeginCommand(rpMgmtData.RelyingPartyIdHash.Value, pinToken, protocol);
            rsp = Connection.SendCommand(credCmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CredentialManagementData newMgmtData = rsp.GetData();
            Assert.NotNull(newMgmtData.User);

            if (newMgmtData.User is null)
            {
                return;
            }

            Assert.NotNull(newMgmtData.User.DisplayName);
            if (newMgmtData.User.DisplayName is null)
            {
                return;
            }

            Assert.Equal(origMgmtData.User.DisplayName + " Updated", newMgmtData.User.DisplayName);
        }
    }
}
