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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Describes one discovered per-interface device as input to the composite-device merge.
/// </summary>
/// <param name="Device">The per-interface <see cref="IYubiKey" /> (e.g. a <see cref="PcscYubiKey" /> or HID key).</param>
/// <param name="Connection">The single concrete connection this interface exposes.</param>
/// <param name="IsUsb">
///     Whether this interface is USB-attached (HID is always USB; PC/SC is USB only when its kind is
///     <see cref="Yubico.YubiKit.Core.Transports.SmartCard.PscsConnectionKind.Usb" />). NFC and unknown-kind PC/SC readers are not USB and never merge.
/// </param>
/// <param name="Pid">The known Yubico USB Product ID for this interface (CCID parsed from the reader name, HID from the descriptor), or <c>null</c> when unknown/unparsed.</param>
/// <param name="Serial">The application serial number (populated only for interfaces that took the serial-disambiguation path), or <c>null</c>.</param>
/// <param name="DeviceInfo">The device info read during serial disambiguation, when available.</param>
internal readonly record struct DeviceInterfaceDescriptor(
    IYubiKey Device,
    ConnectionType Connection,
    bool IsUsb,
    ushort? Pid,
    int? Serial,
    DeviceInfo? DeviceInfo);

/// <summary>
///     Deterministic, side-effect-free merge of per-interface descriptors into physical YubiKey devices,
///     correlating by USB Product ID (the Rust reference model).
/// </summary>
/// <remarks>
///     Primary key is the USB Product ID. USB interfaces sharing a known PID that is present on exactly one
///     physical key (PID count == 1) merge with no serial required. When a PID is present on more than one
///     physical key (PID count > 1), or when <c>forceSerialMerge</c> is set (an unparsed USB CCID
///     reader forced the scan onto the serial path), USB interfaces are merged by serial instead, with
///     conservative no-collapse for null serials. NFC and null-PID interfaces stand alone.
/// </remarks>
internal static class CompositeDeviceMerger
{
    public static IReadOnlyList<IYubiKey> Merge(
        IReadOnlyList<DeviceInterfaceDescriptor> descriptors,
        bool forceSerialMerge = false)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var result = new List<IYubiKey>();

        if (forceSerialMerge)
        {
            // Reader-name drift: correlate all USB interfaces by serial (Phase 37 behavior) so an unparsed
            // CCID rejoins its HID siblings. Non-USB interfaces still stand alone.
            MergeUsbBySerial(descriptors.Where(d => d.IsUsb), result);
            result.AddRange(descriptors.Where(d => !d.IsUsb).Select(d => d.Device));
            return result;
        }

        var usb = descriptors.Where(d => d.IsUsb).ToList();
        var pidCounts = ComputePidCounts(usb);

        // USB interfaces with a known PID present on exactly one physical key: merge by PID (no serial).
        var mergeableByPid = usb.Where(d => d.Pid is { } pid && pidCounts.GetValueOrDefault(pid) == 1);
        foreach (var group in mergeableByPid.GroupBy(d => d.Pid!.Value).OrderBy(g => g.Key))
        {
            if (CanMergeByPidWithoutSerial(group))
                AddMerged(group, $"ykphysical:pid:{group.Key:X4}", result);
            else
                MergeUsbBySerial(group, result);
        }

        // USB interfaces with a known PID present on more than one physical key: disambiguate by serial.
        var ambiguous = usb.Where(d => d.Pid is { } pid && pidCounts.GetValueOrDefault(pid) > 1);
        MergeUsbBySerial(ambiguous, result);

        // USB interfaces without a known PID (e.g. unparsed CCID outside the force-serial path), NFC, and
        // other non-USB interfaces stand alone (conservative).
        result.AddRange(usb.Where(d => d.Pid is null || !ReaderNamePidParser.IsKnownPid(d.Pid.Value)).Select(d => d.Device));
        result.AddRange(descriptors.Where(d => !d.IsUsb).Select(d => d.Device));

        return result;
    }

    /// <summary>
    ///     Per-PID physical-key count over USB interfaces: the max across transports for each known PID,
    ///     mirroring the Rust reference (the same physical key appears once per transport under the same PID).
    /// </summary>
    public static IReadOnlyDictionary<ushort, int> ComputePidCounts(IEnumerable<DeviceInterfaceDescriptor> usbDescriptors)
    {
        var perPidPerConnection = new Dictionary<ushort, Dictionary<ConnectionType, int>>();
        foreach (var d in usbDescriptors)
        {
            if (d.Pid is not { } pid || !ReaderNamePidParser.IsKnownPid(pid))
                continue;

            var byConnection = perPidPerConnection.TryGetValue(pid, out var existing)
                ? existing
                : perPidPerConnection[pid] = new Dictionary<ConnectionType, int>();
            byConnection[d.Connection] = byConnection.GetValueOrDefault(d.Connection) + 1;
        }

        return perPidPerConnection.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Values.Max());
    }

    private static bool CanMergeByPidWithoutSerial(IGrouping<ushort, DeviceInterfaceDescriptor> group)
    {
        var descriptors = group.ToList();
        if (descriptors.Count < 2)
            return true;

        var observed = descriptors.Aggregate(
            ConnectionType.Unknown,
            static (current, descriptor) => current | descriptor.Connection);
        var expected = ReaderNamePidParser.ExpectedConnectionsForPid(group.Key);

        // A full OTP+FIDO+CCID PID observed as CCID plus exactly one HID interface is ambiguous: the missing
        // HID interface could mean partial enumeration of one key or disjoint interfaces from two same-model
        // keys. Keep the observations separate unless serial evidence exists.
        var hasSmartCard = observed.SupportsConnection(ConnectionType.SmartCard);
        var hidCount = descriptors.Count(d => d.Connection is ConnectionType.HidFido or ConnectionType.HidOtp);
        return !(expected == (ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp)
            && hasSmartCard
            && hidCount == 1
            && observed != expected);
    }

    private static void MergeUsbBySerial(IEnumerable<DeviceInterfaceDescriptor> usbDescriptors, List<IYubiKey> result)
    {
        var descriptors = usbDescriptors.ToList();

        foreach (var group in descriptors
                     .Where(d => d.Serial is not null)
                     .GroupBy(d => d.Serial!.Value)
                     .OrderBy(g => g.Key))
            AddMerged(group, $"ykphysical:{group.Key}", result);

        // Null/unreadable serial does not collapse.
        result.AddRange(descriptors.Where(d => d.Serial is null).Select(d => d.Device));
    }

    private static void AddMerged(IEnumerable<DeviceInterfaceDescriptor> group, string deviceId, List<IYubiKey> result)
    {
        var ordered = group.OrderBy(m => ConnectionOrder(m.Connection)).ToList();

        if (ordered.Count == 1)
        {
            // Strong evidence but only one interface: no composite wrapper.
            result.Add(ordered[0].Device);
            return;
        }

        var deviceInfo = ordered.Select(m => m.DeviceInfo).FirstOrDefault(di => di.HasValue);
        var members = ordered.Select(m => m.Device).ToList();
        result.Add(new CompositeYubiKey(deviceId, members, deviceInfo));
    }

    private static int ConnectionOrder(ConnectionType connection) => connection switch
    {
        ConnectionType.SmartCard => 0,
        ConnectionType.HidFido => 1,
        ConnectionType.HidOtp => 2,
        _ => 3
    };
}