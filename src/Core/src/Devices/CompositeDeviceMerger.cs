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
/// <param name="TopologyKey">
///     Optional platform topology evidence identifying the physical USB device that owns this interface
///     (Windows Container ID). <c>null</c> whenever topology evidence is unavailable — always on macOS and
///     Linux, and on Windows when the topology read fails. See <see cref="IDeviceTopologyResolver" />.
/// </param>
internal readonly record struct DeviceInterfaceDescriptor(
    IYubiKey Device,
    ConnectionType Connection,
    bool IsUsb,
    ushort? Pid,
    int? Serial,
    DeviceInfo? DeviceInfo,
    string? TopologyKey = null);

/// <summary>
///     Deterministic, side-effect-free merge of per-interface descriptors into physical YubiKey devices,
///     correlating by USB Product ID (the Rust reference model).
/// </summary>
/// <remarks>
///     Primary key is the USB Product ID, applied as a deterministic evidence hierarchy. USB interfaces
///     sharing a known PID that is present on exactly one physical key (PID count == 1) merge with no serial
///     required, but only when the observed connection set exactly equals the PID's expected interface set
///     (the generalized guard); partial observations fall through to the serial path. When a PID is present
///     on more than one physical key (PID count > 1), or when the guard refuses, interfaces are grouped by
///     serial evidence within their PID class, remaining null-serial orphans are attributed by pigeonhole
///     deduction (unique candidate + type-count closure), and anything still ambiguous stays conservatively
///     standalone. When <c>forceSerialMerge</c> is set (an unparsed USB CCID reader forced the scan onto the
///     serial path), USB interfaces are merged by serial only, with conservative no-collapse for null
///     serials. NFC and null-PID interfaces stand alone.
/// </remarks>
internal static class CompositeDeviceMerger
{
    public static IReadOnlyList<IYubiKey> Merge(
        IReadOnlyList<DeviceInterfaceDescriptor> descriptors,
        bool forceSerialMerge = false)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var result = new List<IYubiKey>();

        // Tier 1 — topology evidence (strongest, when available). Interfaces carrying a topology key are
        // grouped by the physical USB device that owns them and are removed from all later tiers; every
        // interface without a key falls through to the unchanged serial / PID / deduction / conservative
        // tiers. Absent topology therefore degrades to exactly the macOS/Linux semantics.
        var usbRemaining = MergeUsbByTopology(descriptors.Where(d => d.IsUsb), result);

        if (forceSerialMerge)
        {
            // Reader-name drift: correlate remaining USB interfaces by serial (Phase 37 behavior) so an
            // unparsed CCID rejoins its HID siblings. Non-USB interfaces still stand alone.
            MergeUsbBySerial(usbRemaining, result);
            result.AddRange(descriptors.Where(d => !d.IsUsb).Select(d => d.Device));
            return result;
        }

        var usb = usbRemaining;
        var pidCounts = ComputePidCounts(usb);

        // USB interfaces with a known PID present on exactly one physical key: merge by PID (no serial).
        var mergeableByPid = usb.Where(d => d.Pid is { } pid && pidCounts.GetValueOrDefault(pid) == 1);
        foreach (var group in mergeableByPid.GroupBy(d => d.Pid!.Value).OrderBy(g => g.Key))
        {
            if (CanMergeByPidWithoutSerial(group))
                AddMerged(group, $"ykphysical:pid:{group.Key:X4}", result);
            else
                MergeSamePidBySerialWithDeduction(group.Key, [.. group], result);
        }

        // USB interfaces with a known PID present on more than one physical key: disambiguate by serial
        // within each PID class (a physical key has exactly one PID at a time, so serial evidence never
        // needs to correlate across PID classes), then attribute remaining orphans by pigeonhole deduction.
        var ambiguous = usb.Where(d => d.Pid is { } pid && pidCounts.GetValueOrDefault(pid) > 1);
        foreach (var group in ambiguous.GroupBy(d => d.Pid!.Value).OrderBy(g => g.Key))
            MergeSamePidBySerialWithDeduction(group.Key, [.. group], result);

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

    /// <summary>
    ///     Tier 1 — topology evidence. Groups USB interfaces sharing a topology key (the physical USB
    ///     device that owns them) and returns the interfaces WITHOUT a key, which flow to the later tiers
    ///     untouched. Topology outranks serial and PID because it identifies the physical device directly
    ///     rather than inferring it, and it is the only tier that can group serial-less multi-interface keys.
    /// </summary>
    private static List<DeviceInterfaceDescriptor> MergeUsbByTopology(
        IEnumerable<DeviceInterfaceDescriptor> usbDescriptors,
        List<IYubiKey> result)
    {
        var descriptors = usbDescriptors.ToList();
        var withTopology = descriptors.Where(d => !string.IsNullOrEmpty(d.TopologyKey)).ToList();
        if (withTopology.Count == 0)
            return descriptors;

        foreach (var group in withTopology
                     .GroupBy(d => d.TopologyKey!, StringComparer.Ordinal)
                     .OrderBy(g => g.Key, StringComparer.Ordinal))
            AddMerged(group, $"ykphysical:topology:{group.Key}", result);

        return [.. descriptors.Where(d => string.IsNullOrEmpty(d.TopologyKey))];
    }

    /// <summary>
    ///     Tier-3 generalized guard: a PID-unique group may merge without serial evidence only when the
    ///     observed connection set exactly equals the PID's expected interface set. Any partial observation
    ///     (observed != expected) falls through to serial evidence / pigeonhole deduction / conservative
    ///     standalone. Note the documented epistemic bound: complementary partial observations from two
    ///     same-PID keys that together equal the expected set are indistinguishable from one fully-visible
    ///     key and DO merge here; the misattribution is bounded to the partial-visibility window and heals
    ///     on the first scan with complete visibility, serial evidence, or topology evidence.
    /// </summary>
    private static bool CanMergeByPidWithoutSerial(IGrouping<ushort, DeviceInterfaceDescriptor> group)
    {
        var observed = group.Aggregate(
            ConnectionType.Unknown,
            static (current, descriptor) => current | descriptor.Connection);
        return observed == ReaderNamePidParser.ExpectedConnectionsForPid(group.Key);
    }

    /// <summary>
    ///     Tier 2 + tier 4 for one same-PID group: serial evidence groups anchored interfaces; then
    ///     pigeonhole deduction attributes a null-serial orphan to a serial-anchored key when (a) exactly
    ///     ONE anchored key is missing the orphan's connection type and (b) type-count closure holds — for
    ///     every interface type in the PID's expected set, the count of visible same-PID interfaces of that
    ///     type does not exceed the number of anchored candidate keys. Any ambiguity (two candidates, or
    ///     counts exceeding candidates) leaves the orphan conservatively standalone.
    /// </summary>
    private static void MergeSamePidBySerialWithDeduction(
        ushort pid,
        IReadOnlyList<DeviceInterfaceDescriptor> descriptors,
        List<IYubiKey> result)
    {
        var anchored = descriptors
            .Where(d => d.Serial is not null)
            .GroupBy(d => d.Serial!.Value)
            .OrderBy(g => g.Key)
            .Select(g => (Serial: g.Key, Members: g.ToList()))
            .ToList();
        var orphans = descriptors.Where(d => d.Serial is null).ToList();

        var attributed = new Dictionary<int, List<DeviceInterfaceDescriptor>>();
        var standalone = new List<DeviceInterfaceDescriptor>();

        var expected = ReaderNamePidParser.ExpectedConnectionsForPid(pid);
        var deducible = anchored.Count > 0 && orphans.Count > 0 && TypeCountClosureHolds(expected, descriptors, anchored.Count);

        foreach (var orphan in orphans)
        {
            var candidates = deducible && expected.SupportsConnection(orphan.Connection)
                ? anchored.Where(candidate => !ObservedConnections(candidate.Members).SupportsConnection(orphan.Connection)).ToList()
                : [];

            if (candidates.Count == 1)
            {
                var extras = attributed.TryGetValue(candidates[0].Serial, out var existing)
                    ? existing
                    : attributed[candidates[0].Serial] = [];
                extras.Add(orphan);
            }
            else
            {
                standalone.Add(orphan);
            }
        }

        foreach (var (serial, members) in anchored)
        {
            IEnumerable<DeviceInterfaceDescriptor> allMembers = attributed.TryGetValue(serial, out var extras)
                ? [.. members, .. extras]
                : members;
            AddMerged(allMembers, $"ykphysical:{serial}", result);
        }

        // Unattributed null serials do not collapse (conservative standalone).
        result.AddRange(standalone.Select(d => d.Device));
    }

    private static bool TypeCountClosureHolds(
        ConnectionType expected,
        IReadOnlyList<DeviceInterfaceDescriptor> descriptors,
        int candidateKeyCount)
    {
        ReadOnlySpan<ConnectionType> concreteTypes =
            [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];

        foreach (var type in concreteTypes)
        {
            if (!expected.SupportsConnection(type))
                continue;

            var visible = descriptors.Count(d => d.Connection == type);
            if (visible > candidateKeyCount)
                return false;
        }

        return true;
    }

    private static ConnectionType ObservedConnections(IReadOnlyList<DeviceInterfaceDescriptor> descriptors) =>
        descriptors.Aggregate(
            ConnectionType.Unknown,
            static (current, descriptor) => current | descriptor.Connection);

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