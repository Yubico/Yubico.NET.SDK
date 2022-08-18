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
    public class GetKeyAgreeCommandTests
    {
        [Fact]
        public void GetKeyAgreeCommand_Succeeds()
        {
            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                return;
            }

            var connection = new FidoConnection(deviceToUse);
            Assert.NotNull(connection);

            var cmd = new GetKeyAgreementCommand() { PinUvAuthProtocol = PinUvAuthProtocol.ProtocolTwo, };
            GetKeyAgreementResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey pubKey = rsp.GetData();
            Assert.Equal(CoseAlgorithmIdentifier.ES256, pubKey.Algorithm);
        }

        private static HidDevice? GetFidoHid(IEnumerable<HidDevice> devices)
        {
            foreach (HidDevice currentDevice in devices)
            {
                if ((currentDevice.VendorId == 0x1050) &&
                    (currentDevice.UsagePage == HidUsagePage.Fido))
                {
                    return currentDevice;
                }
            }

            return null;
        }
    }
}
