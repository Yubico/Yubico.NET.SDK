// Copyright 2021 Yubico AB
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
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;
using Yubico.PlatformInterop;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.U2f
{
    public class SetDeviceInfoTests : IDisposable
    {
        private readonly FidoConnection _fidoConnection;

        public SetDeviceInfoTests()
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    throw new ArgumentException("Windows not elevated.");
                }
            }

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetFidoHid(devices);
            Assert.NotNull(deviceToUse);

            if (deviceToUse is null)
            {
                throw new ArgumentException("null device");
            }

            _fidoConnection = new FidoConnection(deviceToUse);
            Assert.NotNull(_fidoConnection);
        }

        public void Dispose()
        {
            _fidoConnection.Dispose();
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

        [Fact]
        public void SetCRTimeout_Succeeds()
        {
            var cmd = new SetDeviceInfoCommand
            {
                ChallengeResponseTimeout = 0x20
            };
            SetDeviceInfoResponse rsp = _fidoConnection.SendCommand(cmd);

            Assert.Equal(ResponseStatus.Success, rsp.Status);

            var getCmd = new GetDeviceInfoCommand();
            GetDeviceInfoResponse getRsp = _fidoConnection.SendCommand(getCmd);
            Assert.Equal(ResponseStatus.Success, getRsp.Status);

            YubiKeyDeviceInfo getData = getRsp.GetData();
            Assert.False(getData.IsFipsSeries);
        }

        [Fact]
        public void SetLockCode_Succeeds()
        {
            byte[] newCode = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48
            };
            byte[] wrongCode = new byte[] {
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58
            };
            byte[] clearCode = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            var cmd = new SetDeviceInfoCommand
            {
                ChallengeResponseTimeout = 0x21
            };
            SetDeviceInfoResponse rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            cmd = new SetDeviceInfoCommand();
            cmd.SetLockCode(newCode);
            rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            cmd = new SetDeviceInfoCommand
            {
                ChallengeResponseTimeout = 0x22
            };
            rsp = _fidoConnection.SendCommand(cmd);
            Assert.NotEqual(ResponseStatus.Success, rsp.Status);

            cmd = new SetDeviceInfoCommand();
            cmd.ApplyLockCode(wrongCode);
            cmd.ChallengeResponseTimeout = 0x23;
            rsp = _fidoConnection.SendCommand(cmd);
            Assert.NotEqual(ResponseStatus.Success, rsp.Status);

            cmd = new SetDeviceInfoCommand();
            cmd.ApplyLockCode(newCode);
            cmd.ChallengeResponseTimeout = 0x24;
            rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            cmd = new SetDeviceInfoCommand();
            cmd.ApplyLockCode(newCode);
            cmd.SetLockCode(clearCode);
            rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            var getCmd = new GetDeviceInfoCommand();
            GetDeviceInfoResponse getRsp = _fidoConnection.SendCommand(getCmd);
            Assert.Equal(ResponseStatus.Success, getRsp.Status);

            YubiKeyDeviceInfo getData = getRsp.GetData();
            Assert.Equal(0x24, getData.ChallengeResponseTimeout);
        }

        [Fact]
        public void SetLegacyCRTimeout_Succeeds()
        {
            var cmd = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.Ccid, 0x21, true, 255)
            {
                YubiKeyInterfaces = YubiKeyCapabilities.All
            };
            YubiKeyResponse rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            var getCmd = new GetDeviceInfoCommand();
            GetDeviceInfoResponse getRsp = _fidoConnection.SendCommand(getCmd);
            Assert.Equal(ResponseStatus.Success, getRsp.Status);

            YubiKeyDeviceInfo getData = getRsp.GetData();
            Assert.False(getData.IsFipsSeries);
        }
    }
}
