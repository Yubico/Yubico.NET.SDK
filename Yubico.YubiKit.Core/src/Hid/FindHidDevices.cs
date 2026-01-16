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
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Linux;
using Yubico.YubiKit.Core.Hid.MacOS;

namespace Yubico.YubiKit.Core.Hid;

public interface IFindHidDevices
{
    Task<IReadOnlyList<IHidDevice>> FindAllAsync(CancellationToken cancellationToken = default);
}

public class FindHidDevices(ILogger<FindHidDevices> logger) : IFindHidDevices
{
    private const short YubicoVendorId = 0x1050;

    public async Task<IReadOnlyList<IHidDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
        await Task.Run(FindAll, cancellationToken).ConfigureAwait(false);

    private IReadOnlyList<IHidDevice> FindAll()
    {
        logger.LogInformation("Getting list of HID devices");

        var allDevices = GetPlatformDevices();

        var yubicoDevices = allDevices
            .Where(d => d.DescriptorInfo.VendorId == YubicoVendorId)
            .ToList();

        logger.LogInformation("Found {Count} Yubico HID devices", yubicoDevices.Count);

        return yubicoDevices;
    }

    private static IReadOnlyList<IHidDevice> GetPlatformDevices()
    {
        if (OperatingSystem.IsMacOS())
        {
            return FindAllMacOS();
        }

        if (OperatingSystem.IsLinux())
        {
            return FindAllLinux();
        }

        // Windows not yet implemented, return empty list
        return [];
    }

    [SupportedOSPlatform("macos")]
    private static IReadOnlyList<IHidDevice> FindAllMacOS() =>
        MacOSHidDevice.GetList();

    [SupportedOSPlatform("linux")]
    private static IReadOnlyList<IHidDevice> FindAllLinux() =>
        LinuxHidDevice.GetList();

    public static FindHidDevices Create(ILogger<FindHidDevices>? logger = null) =>
        new(logger ?? NullLogger<FindHidDevices>.Instance);
}
