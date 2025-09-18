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
using Xunit;
using Yubico.Core.Devices.Hid;
using Yubico.PlatformInterop;
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.U2f;

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

        var devices = HidDevice.GetHidDevices();
        Assert.NotNull(devices);

        var deviceToUse = GetFidoHid(devices);
        Assert.NotNull(deviceToUse);

        if (deviceToUse is null)
        {
            throw new ArgumentException("null device");
        }

        _fidoConnection = new FidoConnection(deviceToUse);
        Assert.NotNull(_fidoConnection);
    }

    #region IDisposable Members

    public void Dispose()
    {
        _fidoConnection.Dispose();
    }

    #endregion

    [Fact]
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public void RunGetDeviceInfo()
    {
        var cmd = new GetPagedDeviceInfoCommand();
        var rsp = _fidoConnection.SendCommand(cmd);
        Assert.Equal(ResponseStatus.Success, rsp.Status);

        var deviceInfo = YubiKeyDeviceInfo.CreateFromResponseData(rsp.GetData());
        Assert.False(deviceInfo.IsFipsSeries);
    }

    [Fact]
    public void RunSetDeviceInfo()
    {
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
        var cmd = new VerifyFipsModeCommand();
        var rsp = _fidoConnection.SendCommand(cmd);
        Assert.Equal(ResponseStatus.Success, rsp.Status);

        var getData = rsp.GetData();
        Assert.False(getData);
    }

    public static HidDevice? GetFidoHid(
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
}
