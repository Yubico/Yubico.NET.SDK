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
using Yubico.Core.Devices.Hid;
using Yubico.PlatformInterop;
using Yubico.YubiKey.U2f.Commands;
using Xunit;

namespace Yubico.YubiKey.U2f
{
    public class CommandTests : IDisposable
    {
        private readonly FidoConnection _fidoConnection;

        public CommandTests()
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

        [Fact]
        public void RunGetDeviceInfo()
        {
            if (_fidoConnection is null)
            {
                return;
            }

            var cmd = new GetDeviceInfoCommand();
            GetDeviceInfoResponse rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            YubiKeyDeviceInfo getData = rsp.GetData();
            Assert.False(getData.IsFipsSeries);
        }

        [Fact]
        public void RunSetDeviceInfo()
        {
            if (_fidoConnection is null)
            {
                return;
            }

            var cmd = new SetDeviceInfoCommand();
            Assert.Null(cmd.DeviceFlags);
//            GetDeviceInfoResponse rsp = _fidoConnection.SendCommand(cmd);
//            Assert.Equal(ResponseStatus.Success, rsp.Status);

//            YubiKeyDeviceInfo getData = rsp.GetData();
//            Assert.False(getData.IsFipsSeries);
        }

        [Fact]
        public void VerifyFipsMode()
        {
            if (_fidoConnection is null)
            {
                return;
            }

            var cmd = new VerifyFipsModeCommand();
            VerifyFipsModeResponse rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            bool getData = rsp.GetData();
            Assert.False(getData);
        }

        public static HidDevice? GetFidoHid(IEnumerable<HidDevice> devices)
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

