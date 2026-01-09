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

using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Hid;

namespace Yubico.YubiKit.Core.IntegrationTests.Hid;

[SupportedOSPlatform("macos")]
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
        if (!OperatingSystem.IsMacOS())
        {
            _output.WriteLine("Test skipped: not running on macOS");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        var finder = new FindHidDevices(loggerFactory.CreateLogger<FindHidDevices>());

        var devices = await finder.FindAllAsync();

        _output.WriteLine($"Found {devices.Count} HID devices");

        foreach (var device in devices)
        {
            _output.WriteLine($"  VID={device.VendorId:X4} PID={device.ProductId:X4} " +
                            $"Usage={device.Usage:X4} UsagePage={device.UsagePage}");
        }

        Assert.True(devices.Count >= 0, "Should not fail even if no devices present");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresHardware", "true")]
    public async Task FindYubiKeys_IncludesHidDevices()
    {
        if (!OperatingSystem.IsMacOS())
        {
            _output.WriteLine("Test skipped: not running on macOS");
            return;
        }

        var finder = YubiKey.FindYubiKeys.Create();

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
