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
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.Transports.Hid;

public interface IFindHidInterfaces
{
    Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default);
}

public class FindHidInterfaces : IFindHidInterfaces
{
    private readonly ILogger<FindHidInterfaces> _logger;
    private readonly Func<IReadOnlyList<IHidInterface>> _getPlatformDevices;

    public FindHidInterfaces(ILogger<FindHidInterfaces> logger)
    {
        _logger = logger;
        _getPlatformDevices = GetPlatformDevices;
    }

    internal FindHidInterfaces(ILogger<FindHidInterfaces> logger,
        Func<IReadOnlyList<IHidInterface>> getPlatformDevices)
    {
        _logger = logger;
        _getPlatformDevices = getPlatformDevices;
    }

    public async Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default) =>
        await Task.Run(FindAll, cancellationToken).ConfigureAwait(false);

    private IReadOnlyList<IHidInterface> FindAll()
    {
        _logger.LogDebug("Getting list of HID devices");

        var allDevices = _getPlatformDevices();

        var yubicoDevices = allDevices
            .Where(d => d.DescriptorInfo.VendorId == HidConstants.YubicoVendorId)
            .ToList();

        _logger.LogDebug("Found {Count} Yubico HID devices", yubicoDevices.Count);

        return yubicoDevices;
    }

    private IReadOnlyList<IHidInterface> GetPlatformDevices() =>
        SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.MacOS => FindAllMacOS(),
            SdkPlatform.Linux => FindAllLinux(),
            SdkPlatform.Windows => FindAllWindows(),
            SdkPlatform.Unknown => [],
            _ => []
        };

    [SupportedOSPlatform("macos")]
    private static IReadOnlyList<IHidInterface> FindAllMacOS() =>
        MacOSHidInterface.GetList();

    [SupportedOSPlatform("linux")]
    private IReadOnlyList<IHidInterface> FindAllLinux()
    {
        try
        {
            return LinuxHidInterface.GetList();
        }
        catch (DllNotFoundException ex)
        {
            _logger.LogWarning("udev native library not available, returning no HID devices: {Message}", ex.Message);
            return [];
        }
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<IHidInterface> FindAllWindows() =>
        WindowsHidInterface.GetList();

    public static FindHidInterfaces Create(ILogger<FindHidInterfaces>? logger = null) =>
        new(logger ?? NullLogger<FindHidInterfaces>.Instance);
}
