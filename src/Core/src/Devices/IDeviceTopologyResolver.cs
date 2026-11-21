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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Resolves an OPTIONAL per-interface topology key that identifies the physical USB device an interface
///     belongs to — the strongest evidence tier in composite grouping (tier 1, above serial and PID).
/// </summary>
/// <remarks>
///     <para>
///         Topology evidence is optional by contract. This SDK supplies no topology keys on macOS or Linux.
///         On Windows it is absent whenever the
///         topology read fails — a stale devnode mid-hotplug, <c>CR_NO_SUCH_DEVNODE</c>, a missing
///         ContainerId property, or the API being unavailable. A resolver NEVER guesses: it either returns a
///         key it read, or returns <c>false</c>.
///     </para>
///     <para>
///         When no key is available the interface falls through to the unchanged serial / PID-complete /
///         deduction / conservative tiers, so a topology failure degrades Windows to exactly the
///         macOS/Linux semantics.
///     </para>
/// </remarks>
internal interface IDeviceTopologyResolver
{
    /// <summary>
    ///     Attempts to read the topology key grouping <paramref name="device" />'s interface with the other
    ///     interfaces of the same physical USB device.
    /// </summary>
    /// <returns><c>true</c> and a non-empty key when topology evidence was read; otherwise <c>false</c>.</returns>
    bool TryGetTopologyKey(IDevice device, ConnectionType connection, out string? topologyKey);
}

/// <summary>
///     The resolver currently selected on macOS and Linux. It always reports unknown; this describes the
///     SDK implementation rather than the availability of topology APIs on those operating systems.
/// </summary>
internal sealed class NullDeviceTopologyResolver : IDeviceTopologyResolver
{
    public static NullDeviceTopologyResolver Instance { get; } = new();

    public bool TryGetTopologyKey(IDevice device, ConnectionType connection, out string? topologyKey)
    {
        topologyKey = null;
        return false;
    }
}

/// <summary>
///     Platform selection for <see cref="IDeviceTopologyResolver" />, mirroring the repo's
///     <see cref="SdkPlatformInfo" /> factory pattern.
/// </summary>
internal static class DeviceTopologyResolver
{
    /// <summary>
    ///     The Windows Container-ID resolver on Windows; the no-topology resolver everywhere else. The
    ///     Windows implementation is constructed only on Windows, so no Windows-only native entry point is
    ///     bound on other platforms.
    /// </summary>
    public static IDeviceTopologyResolver Create() =>
        SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows
            ? new WindowsDeviceTopologyResolver(new WindowsTopologyNativeOps())
            : NullDeviceTopologyResolver.Instance;
}