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
    [Trait(TraitTypes.Category, TestCategories.RequiresBio)]
    public class ConfigCommandTests : NeedPinToken
    {
        public ConfigCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5Bio, null)
        {
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void EnableEnterpriseAttestationCommand_Succeeds()
        {
            var infoCmd = new GetInfoCommand();
            GetInfoResponse infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            AuthenticatorInfo authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);

            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.AuthenticatorConfiguration, out byte[] pinToken);
            Assert.True(isValid);

            var cmd = new EnableEnterpriseAttestationCommand(pinToken, protocol);
            Fido2Response rsp = Connection.SendCommand(cmd);

            Assert.Equal(CtapStatus.Ok, rsp.CtapStatus);

            infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void ToggleAlwaysUvCommand_Succeeds()
        {
            var infoCmd = new GetInfoCommand();
            GetInfoResponse infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            AuthenticatorInfo authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);

            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.AuthenticatorConfiguration, out byte[] pinToken);
            Assert.True(isValid);

            var cmd = new ToggleAlwaysUvCommand(pinToken, protocol);
            Fido2Response rsp = Connection.SendCommand(cmd);

            Assert.Equal(CtapStatus.Ok, rsp.CtapStatus);

            infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void SetMinPinLengthCommand_Pin_Succeeds()
        {
            var infoCmd = new GetInfoCommand();
            GetInfoResponse infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            AuthenticatorInfo authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);

            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.AuthenticatorConfiguration, out byte[] pinToken);
            Assert.True(isValid);

            var cmd = new SetMinPinLengthCommand(7, null, null, pinToken, protocol);
            Fido2Response rsp = Connection.SendCommand(cmd);

            Assert.Equal(CtapStatus.Ok, rsp.CtapStatus);

            infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void SetMinPinLengthCommand_ForceChange_Succeeds()
        {
            var infoCmd = new GetInfoCommand();
            GetInfoResponse infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            AuthenticatorInfo authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);

            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.AuthenticatorConfiguration, out byte[] pinToken);
            Assert.True(isValid);

            var cmd = new SetMinPinLengthCommand(null, null, true, pinToken, protocol);
            Fido2Response rsp = Connection.SendCommand(cmd);

            Assert.Equal(CtapStatus.Ok, rsp.CtapStatus);

            infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            authInfo = infoRsp.GetData();
            Assert.True(authInfo.ForcePinChange);

            byte[] currentPin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            byte[] newPin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 };

            var changePinCmd = new ChangePinCommand(protocol, currentPin, newPin);
            ChangePinResponse changePinRsp = Connection.SendCommand(changePinCmd);
            Assert.Equal(ResponseStatus.Success, changePinRsp.Status);

            infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            authInfo = infoRsp.GetData();
            Assert.False(authInfo.ForcePinChange);

            changePinCmd = new ChangePinCommand(protocol, newPin, currentPin);
            changePinRsp = Connection.SendCommand(changePinCmd);
            Assert.Equal(ResponseStatus.Success, changePinRsp.Status);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void SetMinPinLengthCommand_AllNull_Succeeds()
        {
            var infoCmd = new GetInfoCommand();
            GetInfoResponse infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            AuthenticatorInfo authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);

            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.AuthenticatorConfiguration, out byte[] pinToken);
            Assert.True(isValid);

            var cmd = new SetMinPinLengthCommand(null, null, null, pinToken, protocol);
            Fido2Response rsp = Connection.SendCommand(cmd);

            Assert.Equal(CtapStatus.Ok, rsp.CtapStatus);

            infoRsp = Connection.SendCommand(infoCmd);
            Assert.Equal(ResponseStatus.Success, infoRsp.Status);
            authInfo = infoRsp.GetData();
            Assert.NotNull(authInfo.Options);
        }
    }
}
