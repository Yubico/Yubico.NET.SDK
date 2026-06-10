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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
///     Describes one discovered per-interface device as input to the composite-device merge.
/// </summary>
/// <param name="Device">The per-interface <see cref="IYubiKey" /> (e.g. a <see cref="PcscYubiKey" /> or HID key).</param>
/// <param name="Connection">The single concrete connection this interface exposes.</param>
/// <param name="IsUsb">
///     Whether this interface is USB-attached (HID is always USB; PC/SC is USB only when its kind is
///     <see cref="SmartCard.PscsConnectionKind.Usb" />). NFC and unknown-kind PC/SC readers are not USB and never merge.
/// </param>
/// <param name="Serial">The application serial number read during discovery, or <c>null</c> when unknown/unreadable.</param>
/// <param name="DeviceInfo">The device info read during discovery, when available.</param>
internal readonly record struct DeviceInterfaceDescriptor(
    IYubiKey Device,
    ConnectionType Connection,
    bool IsUsb,
    int? Serial,
    DeviceInfo? DeviceInfo);

/// <summary>
///     Deterministic, side-effect-free merge of per-interface descriptors into physical YubiKey devices.
/// </summary>
/// <remarks>
///     Identity is the application serial number: USB interfaces sharing the same non-null serial collapse
///     into one <see cref="CompositeYubiKey" />. USB interfaces with no readable serial, and all non-USB
///     (NFC / unknown-kind) interfaces, pass through unchanged (conservative no-collapse). A serial group
///     with a single member also passes through unwrapped.
/// </remarks>
internal static class CompositeDeviceMerger
{
    public static IReadOnlyList<IYubiKey> Merge(IReadOnlyList<DeviceInterfaceDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var result = new List<IYubiKey>();

        var mergeableGroups = descriptors
            .Where(d => d is { IsUsb: true, Serial: not null })
            .GroupBy(d => d.Serial!.Value)
            .OrderBy(g => g.Key);

        foreach (var group in mergeableGroups)
        {
            var ordered = group
                .OrderBy(m => ConnectionOrder(m.Connection))
                .ToList();

            if (ordered.Count == 1)
            {
                // Strong evidence but only one interface: no composite wrapper (ISC-10).
                result.Add(ordered[0].Device);
                continue;
            }

            var deviceInfo = ordered
                .Select(m => m.DeviceInfo)
                .FirstOrDefault(di => di.HasValue);

            var members = ordered.Select(m => m.Device).ToList();
            result.Add(new CompositeYubiKey($"ykphysical:{group.Key}", members, deviceInfo));
        }

        // Conservative no-collapse: non-USB (NFC/unknown) and USB-without-serial interfaces stand alone.
        foreach (var descriptor in descriptors.Where(d => d is not { IsUsb: true, Serial: not null }))
            result.Add(descriptor.Device);

        return result;
    }

    private static int ConnectionOrder(ConnectionType connection) => connection switch
    {
        ConnectionType.SmartCard => 0,
        ConnectionType.HidFido => 1,
        ConnectionType.HidOtp => 2,
        _ => 3
    };
}