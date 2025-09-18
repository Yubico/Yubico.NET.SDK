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

namespace Yubico.YubiKey.U2f;

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

        var yubiKeys = YubiKeyDevice.FindByTransport(Transport.HidFido);
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

        var devices = HidDevice.GetHidDevices();
        Assert.NotNull(devices);

        var deviceToUse = GetFidoHid(devices);
        Assert.NotNull(deviceToUse);
        if (deviceToUse is null)
        {
            Assert.True(false);
        }

        var connection = new FidoConnection(deviceToUse!);
        Assert.NotNull(connection);

        var cmd = new GetPagedDeviceInfoCommand();
        var rsp = connection.SendCommand(cmd);
        Assert.Equal(ResponseStatus.Success, rsp.Status);

        var getData = YubiKeyDeviceInfo.CreateFromResponseData(rsp.GetData());
        Assert.False(getData.ConfigurationLocked);
    }

    [Fact]
    public void U2fHid_CommandIns0x77_ReturnsInvalidCommand()
    {
        var devices = HidDevice.GetHidDevices() ?? throw new InvalidOperationException();
        var deviceToUse = GetFidoHid(devices) ?? throw new InvalidOperationException();
        var connection = new FidoConnection(deviceToUse) ?? throw new InvalidOperationException();

        var cmdApdu = new CommandApdu
        {
            Ins = 0x77
        };

        var cmd = new U2fHidTestingCommand(cmdApdu);
        var rsp = connection.SendCommand(cmd);
        Assert.Equal(ResponseStatus.Failed, rsp.Status);
        Assert.Equal(SWConstants.CommandNotAllowed, rsp.StatusWord);
        Assert.Equal(1, rsp.rspApdu.Data.Length);
        Assert.Equal((byte)U2fHidStatus.Ctap1ErrInvalidCommand, rsp.rspApdu.Data.Span[0]);
    }

    [Fact]
    public void U2fHid_U2fInitNoData_ReturnsInvalidDataLength()
    {
        var devices = HidDevice.GetHidDevices() ?? throw new InvalidOperationException();
        var deviceToUse = GetFidoHid(devices) ?? throw new InvalidOperationException();
        var connection = new FidoConnection(deviceToUse) ?? throw new InvalidOperationException();

        var cmdApdu = new CommandApdu
        {
            Ins = 0x06
        };

        var cmd = new U2fHidTestingCommand(cmdApdu);
        var rsp = connection.SendCommand(cmd);
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

        var devices = HidDevice.GetHidDevices();
        Assert.NotNull(devices);

        var deviceToUse = GetFidoHid(devices);
        Assert.NotNull(deviceToUse);
        if (deviceToUse is null)
        {
            Assert.False(true);
        }

        var connection = new FidoConnection(deviceToUse!);
        Assert.NotNull(connection);

        var cmd = new GetProtocolVersionCommand();
        var rsp = connection.SendCommand(cmd);
        Assert.Equal(ResponseStatus.Success, rsp.Status);

        var appVersion = rsp.GetData();
        Assert.False(string.IsNullOrEmpty(appVersion));
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 0x01, 0x02, 0x03 })]
    public void EchoCommand_GetCorrectData(
        ReadOnlyMemory<byte> sendData)
    {
        if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
        {
            if (!SdkPlatformInfo.IsElevated)
            {
                _ = Assert.Throws<UnauthorizedAccessException>(() => YubiKeyDevice.FindByTransport(Transport.HidFido));
                Assert.True(false);
            }
        }

        var devices = HidDevice.GetHidDevices();
        Assert.NotNull(devices);

        var deviceToUse = GetFidoHid(devices);
        Assert.NotNull(deviceToUse);
        if (deviceToUse is null)
        {
            Assert.True(false);
        }

        IYubiKeyConnection connection = new FidoConnection(deviceToUse!);
        Assert.NotNull(connection);

        var echoCommand = new EchoCommand(sendData);

        var echoResponse = connection.SendCommand(echoCommand);
        var echoData = echoResponse.GetData();

        Assert.True(echoCommand.Data.Span.SequenceEqual(echoData.Span));
    }

    private static HidDevice? GetFidoHid(
        IEnumerable<HidDevice> devices)
    {
        foreach (var currentDevice in devices)
        {
            if (currentDevice.VendorId == 0x1050 &&
                currentDevice.UsagePage == HidUsagePage.Fido)
            {
                return currentDevice;
            }
        }

        return null;
    }

    #region Nested type: U2fHidTestingCommand

    private class U2fHidTestingCommand : IYubiKeyCommand<U2fHidTestingResponse>
    {
        public U2fHidTestingCommand(
            CommandApdu commandApdu)
        {
            CommandApdu = commandApdu;
        }

        public CommandApdu CommandApdu { get; }

        #region IYubiKeyCommand<U2fHidTestingResponse> Members

        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        public CommandApdu CreateCommandApdu()
        {
            return CommandApdu;
        }

        public U2fHidTestingResponse CreateResponseForApdu(
            ResponseApdu responseApdu)
        {
            return new U2fHidTestingResponse(responseApdu);
        }

        #endregion
    }

    #endregion

    #region Nested type: U2fHidTestingResponse

    private class U2fHidTestingResponse : U2fResponse
    {
        public U2fHidTestingResponse(
            ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        public ResponseApdu rspApdu => ResponseApdu;
    }

    #endregion
}
