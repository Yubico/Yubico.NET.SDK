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

using System.Collections.Generic;
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Xunit;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public class SetPinCommandTests
    {
        [Fact]
        public void SetPinCommand_Succeeds()
        {
            byte[] newPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetKeyAgreeCommandTests.GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                return;
            }

            var connection = new FidoConnection(deviceToUse);
            Assert.NotNull(connection);

            var protocol = new PinUvAuthProtocolOne();

            var cmd = new GetKeyAgreementCommand(protocol.Protocol);
            GetKeyAgreementResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey authenticatorPubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, authenticatorPubKey.Curve);

            protocol.Encapsulate(authenticatorPubKey);

            var setPinCmd = new SetPinCommand(protocol, newPin);
            SetPinResponse setPinRsp = connection.SendCommand(setPinCmd);
            Assert.Equal(ResponseStatus.Success, setPinRsp.Status);
        }
    }
}
