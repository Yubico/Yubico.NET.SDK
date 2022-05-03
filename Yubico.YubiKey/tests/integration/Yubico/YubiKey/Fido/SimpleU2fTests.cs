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
    public class SimpleU2FTests
    {
        [Fact]
        public void GetList_Succeeds()
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    _ = Assert.Throws<UnauthorizedAccessException>(() => YubiKeyDevice.FindByTransport(Transport.HidFido));
                    return;
                }
            }

            IEnumerable<IYubiKeyDevice> yubiKeys = YubiKeyDevice.FindByTransport(Transport.HidFido);
            var yubiKeyList = yubiKeys.ToList();

            Assert.NotEmpty(yubiKeyList);
        }

        [Fact]
        public void U2fCommand_Succeeds()
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    _ = Assert.Throws<UnauthorizedAccessException>(() => YubiKeyDevice.FindByTransport(Transport.HidFido));
                    return;
                }
            }

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

            var cmd = new GetDeviceInfoCommand();
            GetDeviceInfoResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            YubiKeyDeviceInfo deviceInfo = rsp.GetData();
            Assert.False(deviceInfo.ConfigurationLocked);
        }
        
        [Theory]
        [InlineData(new byte[]{ })]
        [InlineData(new byte[]{ 0x01, 0x02, 0x03 })]
        public void EchoCommand_GetCorrectData(ReadOnlyMemory<byte> sendData)
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    _ = Assert.Throws<UnauthorizedAccessException>(() => YubiKeyDevice.FindByTransport(Transport.HidFido));
                    return;
                }
            }

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                return;
            }

            IYubiKeyConnection connection = new FidoConnection(deviceToUse);
            Assert.NotNull(connection);
            
            EchoCommand echoCommand = new EchoCommand(sendData);

            EchoResponse echoResponse = connection.SendCommand(echoCommand);
            ReadOnlyMemory<byte> echoData = echoResponse.GetData();
            
            Assert.True(echoCommand.Data.Span.SequenceEqual(echoData.Span));
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
