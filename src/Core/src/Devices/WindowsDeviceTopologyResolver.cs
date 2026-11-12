// Copyright 2026 Yubico AB
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
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     The scriptable native operations the Windows topology tier is composed from. Kept as a seam
///     (mirroring the <c>LinuxUdevHidEventSource</c> pattern) so all resolver logic is unit-testable keyless
///     on any OS.
/// </summary>
internal interface IWindowsTopologyNativeOps
{
    /// <summary>
    ///     PC/SC reader name → device instance ID (<c>SCardGetReaderDeviceInstanceIdW</c>, Windows 8+).
    /// </summary>
    bool TryGetReaderDeviceInstanceId(string readerName, out string? deviceInstanceId);

    /// <summary>
    ///     Device instance ID → <c>DEVPKEY_Device_ContainerId</c> (via <c>CM_Locate_DevNode</c>).
    /// </summary>
    bool TryGetContainerIdByInstanceId(string deviceInstanceId, out Guid containerId);

    /// <summary>
    ///     HID device interface path → <c>DEVPKEY_Device_ContainerId</c>.
    /// </summary>
    bool TryGetContainerIdByDevicePath(string devicePath, out Guid containerId);
}

/// <summary>
///     Windows topology tier: maps an interface to its PnP device container. Interfaces reporting the same
///     nonempty Container ID supply the grouping key; the resolver relies on that operating-system evidence.
/// </summary>
/// <remarks>
///     CCID resolves through <c>SCardGetReaderDeviceInstanceIdW</c> → devnode → ContainerId; HID resolves
///     directly from its device interface path. EVERY failure mode — API unavailable, unknown reader, stale
///     devnode (<c>CR_NO_SUCH_DEVNODE</c>), missing ContainerId property, or an all-zero ContainerId (which
///     Windows reports for devices with no container and would otherwise fuse unrelated interfaces) — yields
///     "unknown", never a guess. The interface then falls through to the unchanged serial / PID / deduction
///     tiers, degrading Windows to exactly the macOS/Linux semantics.
///     The CCID reader-instance API requires Windows 8 / Server 2012 or later and is not redirected in a
///     Remote Desktop session, where SCARD_E_READER_UNAVAILABLE leaves topology unknown. Native calls are
///     synchronous and have no SDK wait budget.
/// </remarks>
internal sealed class WindowsDeviceTopologyResolver(IWindowsTopologyNativeOps nativeOps) : IDeviceTopologyResolver
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<WindowsDeviceTopologyResolver>();

    public bool TryGetTopologyKey(IDevice device, ConnectionType connection, out string? topologyKey)
    {
        topologyKey = null;
        if (device is null || string.IsNullOrEmpty(device.ReaderName))
            return false;

        try
        {
            var resolved = connection == ConnectionType.SmartCard
                ? TryResolveSmartCard(device.ReaderName, out var containerId)
                : nativeOps.TryGetContainerIdByDevicePath(device.ReaderName, out containerId);

            if (!resolved || containerId == Guid.Empty)
            {
                Logger.LogDebug(
                    "Topology unknown for interface over {Connection} (resolved: {Resolved}); falling back to serial/PID evidence.",
                    connection,
                    resolved);
                return false;
            }

            topologyKey = containerId.ToString("D");
            return true;
        }
        catch (Exception e)
        {
            // Best-effort by contract: any native failure degrades to no topology evidence.
            Logger.LogDebug(
                e,
                "Topology read failed for interface over {Connection}; falling back to serial/PID evidence.",
                connection);
            return false;
        }
    }

    private bool TryResolveSmartCard(string readerName, out Guid containerId)
    {
        containerId = Guid.Empty;
        return nativeOps.TryGetReaderDeviceInstanceId(readerName, out var instanceId)
            && !string.IsNullOrEmpty(instanceId)
            && nativeOps.TryGetContainerIdByInstanceId(instanceId, out containerId);
    }
}