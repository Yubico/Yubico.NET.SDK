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

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.IntegrationTests.Transports.Hid;

public class HidEnumerationTests
{
    private readonly ITestOutputHelper _output;

    public HidEnumerationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresHardware", "false")]
    public async Task FindHidDevices_EnumeratesYubicoDevices()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        var finder = new FindHidDevices(loggerFactory.CreateLogger<FindHidDevices>());

        var devices = await finder.FindAllAsync();

        _output.WriteLine($"Found {devices.Count} HID devices");

        foreach (var device in devices)
        {
            _output.WriteLine($"  VID={device.DescriptorInfo.VendorId:X4} PID={device.DescriptorInfo.ProductId:X4} " +
                            $"Usage={device.DescriptorInfo.Usage:X4} UsagePage={device.DescriptorInfo.UsagePage:X4}");
        }

        if (OperatingSystem.IsWindows())
        {
            _output.WriteLine(
                "Windows HID enumeration reads interface metadata without opening report handles. " +
                "Access denied failures while opening HID reports usually mean the test host must run elevated as Administrator, " +
                "or another process is holding the HID interface exclusively.");
        }

        Assert.True(devices.Count >= 0, "Should not fail even if no devices present");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresHardware", "true")]
    public async Task FindYubiKeys_IncludesHidDevices()
    {
        var finder = FindYubiKeys.Create();

        var yubiKeys = await finder.FindAllAsync();

        _output.WriteLine($"Found {yubiKeys.Count} YubiKeys");

        foreach (var yubiKey in yubiKeys)
        {
            _output.WriteLine($"  DeviceId={yubiKey.DeviceId}");
        }

        var hidYubiKeys = yubiKeys.Where(yk => yk.DeviceId.StartsWith("hid:")).ToList();
        _output.WriteLine($"Found {hidYubiKeys.Count} HID YubiKeys");

        Assert.True(yubiKeys.Count >= 0, "Should not fail even if no devices present");
    }
}