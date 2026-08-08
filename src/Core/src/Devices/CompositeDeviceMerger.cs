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
///     deduction (unique candidate, and no interface type outnumbering the candidate keys), and anything
///     still ambiguous stays conservatively standalone. When <c>pidCorrelationUntrusted</c> is set (an
///     unparsed USB CCID reader made PID correlation untrustworthy for this scan), topology evidence still
///     groups first and only the interfaces topology leaves behind are merged by serial, with conservative
///     no-collapse for null serials. NFC and null-PID interfaces stand alone.
/// </remarks>
internal static class CompositeDeviceMerger
{
    public static IReadOnlyList<IYubiKey> Merge(
        IReadOnlyList<DeviceInterfaceDescriptor> descriptors,
        bool pidCorrelationUntrusted = false)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var result = new List<IYubiKey>();

        // Tier 1 — topology evidence (strongest, when available). Interfaces carrying a topology key are
        // grouped by the physical USB device that owns them and are removed from all later tiers; every
        // interface without a key falls through to the unchanged serial / PID / deduction / conservative
        // tiers. Absent topology therefore degrades to exactly the macOS/Linux semantics.
        var usbRemaining = MergeUsbByTopology(descriptors.Where(d => d.IsUsb), result);

        if (pidCorrelationUntrusted)
        {
            // Reader-name drift: correlate remaining USB interfaces by serial (Phase 37 behavior) so an
            // unparsed CCID rejoins its HID siblings. Non-USB interfaces still stand alone.
            MergeUsbBySerial(usbRemaining, result);
            result.AddRange(descriptors.Where(d => !d.IsUsb).Select(d => d.Device));
            return result;
        }

        var pidCounts = ComputePidCounts(usbRemaining);

        // Serials observed under more than one PID this scan. Normally empty: a physical key has exactly
        // one PID at a time, so one serial under two PIDs is ONE key caught mid-reconfiguration (serial is
        // globally unique per key; PID changes with configuration), enumerated once staly and once fresh.
        // Those enumerations must not be fused into one composite - it would hold two members of the same
        // connection type, which AvailableConnections' flags-union hides and TryResolveMember's first-match
        // would route to arbitrarily - and they must not both mint ykphysical:{serial}. The winner rule
        // resolves it; see ResolveContestedSerials.
        var contested = ResolveContestedSerials(usbRemaining);

        // USB interfaces with a known PID present on exactly one physical key: merge by PID (no serial).
        // A group holding a contested serial never takes the no-serial shortcut: the shortcut would discard
        // the very evidence (the serial) that decides which enumeration keeps the durable identity.
        var mergeableByPid = usbRemaining.Where(d => d.Pid is { } pid && pidCounts.GetValueOrDefault(pid) == 1);
        foreach (var group in mergeableByPid.GroupBy(d => d.Pid!.Value).OrderBy(g => g.Key))
        {
            if (CanMergeByPidWithoutSerial(group) && !group.Any(d => d.Serial is { } s && contested.Serials.Contains(s)))
                AddGroupedDevice(group, $"ykphysical:pid:{group.Key:X4}", result);
            else
                MergeSamePidBySerialWithDeduction(group.Key, [.. group], result, contested);
        }

        // USB interfaces with a known PID present on more than one physical key: disambiguate by serial
        // within each PID class, then attribute remaining orphans by pigeonhole deduction.
        var ambiguous = usbRemaining.Where(d => d.Pid is { } pid && pidCounts.GetValueOrDefault(pid) > 1);
        foreach (var group in ambiguous.GroupBy(d => d.Pid!.Value).OrderBy(g => g.Key))
            MergeSamePidBySerialWithDeduction(group.Key, [.. group], result, contested);

        // USB interfaces without a known PID (e.g. unparsed CCID outside the serial-only path), NFC, and
        // other non-USB interfaces stand alone (conservative).
        result.AddRange(usbRemaining.Where(d => d.Pid is null || !ReaderNamePidParser.IsKnownPid(d.Pid.Value)).Select(d => d.Device));
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
    ///     untouched.
    /// </summary>
    /// <remarks>
    ///     Why topology outranks serial and PID, and what an absent or partial topology read degrades to,
    ///     are documented once in <c>docs/architecture/device-discovery-guarantees.md</c>, sections
    ///     "G4: serial-less keys" and "G9: topology-read failure".
    /// </remarks>
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
            AddGroupedDevice(group, $"ykphysical:topology:{group.Key}", result);

        return [.. descriptors.Where(d => string.IsNullOrEmpty(d.TopologyKey))];
    }

    /// <summary>
    ///     Tier-3 generalized guard: a PID-unique group may merge without serial evidence only when the
    ///     observed connection set exactly equals the PID's expected interface set. Any partial observation
    ///     (observed != expected) falls through to serial evidence / pigeonhole deduction / conservative
    ///     standalone.
    /// </summary>
    /// <remarks>
    ///     This exact-match rule is what makes the G2 epistemic bound representable. Why no merge logic can
    ///     resolve that bound, its blast radius, how it heals, and the alternatives rejected for closing it
    ///     are documented once in <c>docs/architecture/device-discovery-guarantees.md</c>, section
    ///     "G2: the epistemic bound".
    /// </remarks>
    private static bool CanMergeByPidWithoutSerial(IGrouping<ushort, DeviceInterfaceDescriptor> group)
    {
        var observed = group.Aggregate(
            ConnectionType.Unknown,
            static (current, descriptor) => current | descriptor.Connection);
        return observed == ReaderNamePidParser.ExpectedConnectionsForPid(group.Key);
    }

    /// <summary>
    ///     The cross-PID contested-serial verdict for one scan: which serials were observed under more
    ///     than one PID, and for each, which PID group (if any) won the durable <c>ykphysical:{serial}</c>
    ///     identity.
    /// </summary>
    private readonly record struct ContestedSerials(
        HashSet<int> Serials,
        IReadOnlyDictionary<int, ushort> WinnerPidBySerial)
    {
        public static readonly ContestedSerials None = new([], new Dictionary<int, ushort>());

        /// <summary>Whether this (pid, serial) group carries the durable identity.</summary>
        public bool IsWinner(ushort pid, int serial) =>
            !Serials.Contains(serial)
            || (WinnerPidBySerial.TryGetValue(serial, out var winner) && winner == pid);
    }

    /// <summary>
    ///     Finds serials observed under more than one PID and decides which PID group, if any, keeps the
    ///     durable <c>ykphysical:{serial}</c> identity: the group whose anchored census exactly matches its
    ///     PID's expected interface set. Zero or multiple complete groups leaves the serial with no winner.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         One serial under two PIDs is one physical key mid-reconfiguration, observed through a stale
    ///         enumeration and a fresh one. Completeness against <see
    ///         cref="ReaderNamePidParser.ExpectedConnectionsForPid" /> identifies the live enumeration
    ///         without guessing: the stale one is dying interface-by-interface and its census is failing,
    ///         while the fresh one converges on its PID's full set. When completeness cannot decide - no
    ///         group complete, or more than one - every contested interface publishes standalone and the
    ///         next scan converges. Any tie-break beyond completeness (newest PID, enumeration order) would
    ///         be guessing with a different name.
    ///     </para>
    ///     <para>
    ///         The census deliberately uses <em>anchored</em> members only (interfaces that actually bear
    ///         the serial). If attributed null-serial orphans could complete a contested census, a
    ///         deduction would decide which enumeration gets the durable identity - inverting the evidence
    ///         hierarchy during the one window where the evidence is least trustworthy.
    ///     </para>
    ///     <para>
    ///         This upholds two invariants together: output <c>DeviceId</c>s are pairwise distinct
    ///         (pinned by <c>Merge_AnyVector_ProducesPairwiseDistinctDeviceIds</c>), and at most one
    ///         <c>ykphysical:{serial}</c> object exists per serial per scan, so the id never silently
    ///         names two objects and never needs a scan-scoped qualified form that would look more durable
    ///         than it is.
    ///     </para>
    /// </remarks>
    private static ContestedSerials ResolveContestedSerials(
        IReadOnlyList<DeviceInterfaceDescriptor> usbDescriptors)
    {
        var byContestedSerial = usbDescriptors
            .Where(d => d.Serial is not null && d.Pid is { } pid && ReaderNamePidParser.IsKnownPid(pid))
            .GroupBy(d => d.Serial!.Value)
            .Where(g => g.Select(d => d.Pid!.Value).Distinct().Count() > 1)
            .ToList();

        if (byContestedSerial.Count == 0) return ContestedSerials.None;

        var serials = new HashSet<int>();
        var winners = new Dictionary<int, ushort>();
        foreach (var serialGroup in byContestedSerial)
        {
            serials.Add(serialGroup.Key);

            var completePids = serialGroup
                .GroupBy(d => d.Pid!.Value)
                .Where(pidGroup => ObservedConnections(pidGroup) == ReaderNamePidParser.ExpectedConnectionsForPid(pidGroup.Key))
                .Select(pidGroup => pidGroup.Key)
                .ToList();

            if (completePids.Count == 1) winners[serialGroup.Key] = completePids[0];
        }

        return new ContestedSerials(serials, winners);
    }

    /// <summary>
    ///     Tier 2 + tier 4 for one same-PID group: serial evidence groups anchored interfaces; then
    ///     pigeonhole deduction attributes a null-serial orphan to a serial-anchored key when (a) exactly
    ///     ONE anchored key is missing the orphan's connection type and (b) no interface type outnumbers
    ///     the candidate keys (see <see cref="NoInterfaceTypeOutnumbersCandidateKeys" />). Any ambiguity
    ///     leaves the orphan conservatively standalone.
    /// </summary>
    /// <remarks>
    ///     A contested serial that lost (or had no winner) publishes its members standalone and is excluded
    ///     from pigeonhole candidacy — see <see cref="ResolveContestedSerials" />. A contested winner is
    ///     complete by construction, so it has no missing slot an orphan could be attributed into.
    /// </remarks>
    private static void MergeSamePidBySerialWithDeduction(
        ushort pid,
        IReadOnlyList<DeviceInterfaceDescriptor> descriptors,
        List<IYubiKey> result,
        ContestedSerials contested)
    {
        var anchored = descriptors
            .Where(d => d.Serial is not null)
            .GroupBy(d => d.Serial!.Value)
            .OrderBy(g => g.Key)
            .Select(g => (Serial: g.Key, Members: g.ToList()))
            .ToList();
        var orphans = descriptors.Where(d => d.Serial is null).ToList();

        // Contested serials that did not win in this PID group publish standalone: the durable identity,
        // if awarded at all, belongs to the complete enumeration in another PID group.
        var eligible = anchored.Where(a => contested.IsWinner(pid, a.Serial)).ToList();
        foreach (var loser in anchored.Where(a => !contested.IsWinner(pid, a.Serial)))
            result.AddRange(loser.Members.Select(m => m.Device));

        var attributed = new Dictionary<int, List<DeviceInterfaceDescriptor>>();
        var standalone = new List<DeviceInterfaceDescriptor>();

        var expected = ReaderNamePidParser.ExpectedConnectionsForPid(pid);
        var canAttributeOrphans = eligible.Count > 0
            && orphans.Count > 0
            && NoInterfaceTypeOutnumbersCandidateKeys(expected, descriptors, eligible.Count);

        foreach (var orphan in orphans)
        {
            var candidates = canAttributeOrphans && expected.SupportsConnection(orphan.Connection)
                ? eligible.Where(candidate => !ObservedConnections(candidate.Members).SupportsConnection(orphan.Connection)).ToList()
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

        foreach (var (serial, members) in eligible)
        {
            IEnumerable<DeviceInterfaceDescriptor> allMembers = attributed.TryGetValue(serial, out var extras)
                ? [.. members, .. extras]
                : members;
            AddGroupedDevice(allMembers, $"ykphysical:{serial}", result);
        }

        // Unattributed null serials do not collapse (conservative standalone).
        result.AddRange(standalone.Select(d => d.Device));
    }

    private static bool NoInterfaceTypeOutnumbersCandidateKeys(
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

    private static ConnectionType ObservedConnections(IEnumerable<DeviceInterfaceDescriptor> descriptors) =>
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
            AddGroupedDevice(group, $"ykphysical:{group.Key}", result);

        // Null/unreadable serial does not collapse.
        result.AddRange(descriptors.Where(d => d.Serial is null).Select(d => d.Device));
    }

    private static void AddGroupedDevice(IEnumerable<DeviceInterfaceDescriptor> group, string deviceId, List<IYubiKey> result)
    {
        var ordered = group.OrderBy(m => ConnectionOrder(m.Connection)).ToList();

        if (ordered.Count == 1)
        {
            // G6: a group of one is published as the bare interface device, never wrapped in a composite —
            // a composite must always represent two or more interfaces.
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