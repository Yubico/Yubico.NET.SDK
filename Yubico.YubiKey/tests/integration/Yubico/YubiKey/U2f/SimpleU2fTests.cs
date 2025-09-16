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
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;
using Yubico.PlatformInterop;
using Yubico.YubiKey.U2f.Commands;

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
                    Assert.True(false);
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
                    Assert.True(false);
                }
            }

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                Assert.True(false);
            }

            var connection = new FidoConnection(deviceToUse!);
            Assert.NotNull(connection);

            var cmd = new GetPagedDeviceInfoCommand();
            GetPagedDeviceInfoResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            var getData = YubiKeyDeviceInfo.CreateFromResponseData(rsp.GetData());
            Assert.False(getData.ConfigurationLocked);
        }

        [Fact]
        public void U2fHid_CommandIns0x77_ReturnsInvalidCommand()
        {
            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices() ?? throw new InvalidOperationException();
            HidDevice deviceToUse = GetFidoHid(devices) ?? throw new InvalidOperationException();
            FidoConnection connection = new FidoConnection(deviceToUse) ?? throw new InvalidOperationException();

            var cmdApdu = new CommandApdu
            {
                Ins = 0x77,
            };

            var cmd = new U2fHidTestingCommand(cmdApdu);
            U2fHidTestingResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Failed, rsp.Status);
            Assert.Equal(SWConstants.CommandNotAllowed, rsp.StatusWord);
            Assert.Equal(1, rsp.rspApdu.Data.Length);
            Assert.Equal((byte)U2fHidStatus.Ctap1ErrInvalidCommand, rsp.rspApdu.Data.Span[0]);
        }

        [Fact]
        public void U2fHid_U2fInitNoData_ReturnsInvalidDataLength()
        {
            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices() ?? throw new InvalidOperationException();
            HidDevice deviceToUse = GetFidoHid(devices) ?? throw new InvalidOperationException();
            FidoConnection connection = new FidoConnection(deviceToUse) ?? throw new InvalidOperationException();

            var cmdApdu = new CommandApdu
            {
                Ins = 0x06,
            };

            var cmd = new U2fHidTestingCommand(cmdApdu);
            U2fHidTestingResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Failed, rsp.Status);
            Assert.Equal(SWConstants.WrongLength, rsp.StatusWord);
            Assert.Equal(1, rsp.rspApdu.Data.Length);
            Assert.Equal((byte)U2fHidStatus.Ctap1ErrInvalidLength, rsp.rspApdu.Data.Span[0]);
        }

        [Fact]
        public void GetProtocolVersion_Succeeds()
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    _ = Assert.Throws<UnauthorizedAccessException>(() => YubiKeyDevice.FindByTransport(Transport.HidFido));
                    Assert.True(false);
                }
            }

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                Assert.False(true);
            }

            var connection = new FidoConnection(deviceToUse!);
            Assert.NotNull(connection);

            var cmd = new GetProtocolVersionCommand();
            GetProtocolVersionResponse rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            string appVersion = rsp.GetData();
            Assert.False(string.IsNullOrEmpty(appVersion));
        }

        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x01, 0x02, 0x03 })]
        public void EchoCommand_GetCorrectData(ReadOnlyMemory<byte> sendData)
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    _ = Assert.Throws<UnauthorizedAccessException>(() => YubiKeyDevice.FindByTransport(Transport.HidFido));
                    Assert.True(false);
                }
            }

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetFidoHid(devices);
            Assert.NotNull(deviceToUse);
            if (deviceToUse is null)
            {
                Assert.True(false);
            }

            IYubiKeyConnection connection = new FidoConnection(deviceToUse!);
            Assert.NotNull(connection);

            var echoCommand = new EchoCommand(sendData);

            EchoResponse echoResponse = connection.SendCommand(echoCommand);
            ReadOnlyMemory<byte> echoData = echoResponse.GetData();

            Assert.True(echoCommand.Data.Span.SequenceEqual(echoData.Span));
        }

        private static HidDevice? GetFidoHid(IEnumerable<HidDevice> devices)
        {
            foreach (HidDevice currentDevice in devices)
            {
                if (currentDevice.VendorId == 0x1050 &&
                    currentDevice.UsagePage == HidUsagePage.Fido)
                {
                    return currentDevice;
                }
            }

            return null;
        }

        private class U2fHidTestingResponse : U2fResponse
        {
            public U2fHidTestingResponse(ResponseApdu responseApdu) :
                base(responseApdu)
            { }

            public ResponseApdu rspApdu => ResponseApdu;
        }

        private class U2fHidTestingCommand : IYubiKeyCommand<U2fHidTestingResponse>
        {
            public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

            public CommandApdu CommandApdu { get; set; }

            public U2fHidTestingCommand(CommandApdu commandApdu)
            {
                CommandApdu = commandApdu;
            }

            public CommandApdu CreateCommandApdu() => CommandApdu;

            public U2fHidTestingResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
                new U2fHidTestingResponse(responseApdu);
        }
    }
}
