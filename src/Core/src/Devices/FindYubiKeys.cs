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
using System.Collections.Concurrent;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

public interface IFindYubiKeys
{
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Signals that hotplug activity was observed on a transport, so identity evidence cached from
    ///     that transport's interfaces may describe hardware that is no longer there.
    /// </summary>
    /// <param name="transport">
    ///     The transport family the activity was observed on — <see cref="ConnectionType.SmartCard" /> or
    ///     <see cref="ConnectionType.Hid" />.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         Scan-time eviction alone cannot catch a swap that completes <em>between</em> scans: a key
    ///         unplugged and a same-model key plugged into the same slot can reuse the same per-interface
    ///         identifier (PC/SC reader names are slot-derived), so the interface is never observed absent
    ///         and a cached serial from the old key would be attributed to the new one. For an SDK whose
    ///         consumers bind sessions to serial-derived identity, that is key substitution, not a stale
    ///         cache entry. Hotplug events are the signal that physical topology changed; implementations
    ///         discard cached identity for that transport and re-read on the next scan.
    ///     </para>
    ///     <para>
    ///         Default is a no-op so that test fakes and custom implementations without an identity cache
    ///         are unaffected. Callers must not rely on any synchronous effect.
    ///     </para>
    /// </remarks>
    void NotifyTransportActivity(ConnectionType transport)
    {
    }
}
public class FindYubiKeys : IFindYubiKeys
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<FindYubiKeys>();

    private readonly IFindPcscDevices findPcscService;
    private readonly IFindHidDevices findHidService;
    private readonly IYubiKeyFactory yubiKeyFactory;

    // Tier-1 evidence source. Optional by contract: the no-topology resolver on macOS/Linux, and on Windows
    // any per-interface read failure simply yields no key for that interface.
    private readonly IDeviceTopologyResolver _topologyResolver;

    internal FindYubiKeys(
        IFindPcscDevices findPcscService,
        IFindHidDevices findHidService,
        IYubiKeyFactory yubiKeyFactory)
    {
        this.findPcscService = findPcscService;
        this.findHidService = findHidService;
        this.yubiKeyFactory = yubiKeyFactory;
        _topologyResolver = DeviceTopologyResolver.Create();
    }

    // Hard total wall-clock budget for one best-effort metadata read (shared across that device's
    // transports). Bounded so a busy/locked CCID cannot stall discovery; reads run concurrently across
    // keys so total added scan latency is at most ~one budget.
    private static readonly TimeSpan MetadataReadBudget = TimeSpan.FromSeconds(3);

    // Serial-disambiguation identity cache (PID-count>1 / force-serial path), keyed by per-interface DeviceId.
    // Only successful reads are cached, so presence means the interface's identity was read successfully;
    // a failed or serial-disabled read is simply absent and is retried on the next scan.
    //
    // A cached identity is evidence about specific hardware in a specific configuration, and it expires
    // with either:
    //  - Configuration: the entry records the PID observed at read time, and a hit under a different PID
    //    is a miss (see ReadIdentityAsync). On current platforms a reconfiguration usually changes the
    //    interface id too, which self-evicts - the PID check pins the invariant for any id scheme where it
    //    does not.
    //  - Hardware: scan-time eviction (EvictAbsentIdentities) only catches interfaces observed absent. A
    //    same-slot swap completing BETWEEN scans reuses the slot-derived interface id, so the old key's
    //    serial would be attributed to the new key - key substitution. Hotplug events are the signal that
    //    hardware changed; NotifyTransportActivity discards that transport's entries.
    private readonly ConcurrentDictionary<string, CachedIdentity> _identityCache = new();

    /// <summary>A successful identity read plus the evidence context that makes it reusable.</summary>
    private readonly record struct CachedIdentity(DeviceInfo Info, ushort? Pid, ConnectionType Transport);

    // Best-effort metadata cache, keyed by the published device's stable interface-set key
    // (YubiKeyDevice.PhysicalIdentityKey, NOT DeviceId, which can flip between pid- and
    // serial-forms). Evicted when any member interface disappears.
    private readonly ConcurrentDictionary<string, MetadataCacheEntry> _metadataCache = new();

    // Serializes discovery so two concurrent scans do not open connections to the same interface at once.
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default)
    {
        if (type == ConnectionType.Unknown)
            return [];

        await _scanLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Enumerate all transports regardless of the requested filter so per-interface devices can be
            // merged into physical devices; the filter is applied to the merged capability set at the end.
            var pcscDevices = await findPcscService.FindAllAsync(cancellationToken).ConfigureAwait(false);
            var hidDevices = await findHidService.FindAllAsync(cancellationToken).ConfigureAwait(false);

            var interfaces = BuildInterfaces(pcscDevices, hidDevices);
            EvictAbsentIdentities(interfaces);

            // Reader-name drift: if any USB CCID reader name failed to parse to a known PID, PID correlation
            // is untrustworthy this scan; degrade to serial-based merge for all USB interfaces (ISC-11).
            var pidCorrelationUntrusted = interfaces.Any(i =>
                i is { IsUsb: true, Connection: ConnectionType.SmartCard, Pid: null });
            if (pidCorrelationUntrusted)
            {
                Logger.LogWarning(
                    "A USB CCID reader name did not parse to a known YubiKey PID; falling back to serial-based " +
                    "merge for all USB interfaces this scan (PID correlation degraded).");
            }

            var pidCounts = CompositeDeviceMerger.ComputePidCounts(
                interfaces.Select(i => i.ToDescriptor(null)).Where(d => d.IsUsb));

            var descriptors = await Task.WhenAll(interfaces.Select(async iface =>
            {
                var needsSerial = iface.IsUsb &&
                    (pidCorrelationUntrusted
                        || (iface.Pid is { } pid && pidCounts.GetValueOrDefault(pid) > 1)
                        || NeedsSerialForAmbiguousPartialPid(interfaces, iface));

                var info = needsSerial
                    ? await ReadIdentityAsync(iface, cancellationToken).ConfigureAwait(false)
                    : null;

                return iface.ToDescriptor(info, needsSerial);
            })).ConfigureAwait(false);

            var merged = CompositeDeviceMerger.Merge(descriptors, pidCorrelationUntrusted);
            await PopulateMetadataAsync(merged, interfaces, cancellationToken).ConfigureAwait(false);

            return [.. merged.Where(d => type.Matches(d.AvailableConnections))];
        }
        finally
        {
            _scanLock.Release();
        }
    }

    private List<InterfaceCandidate> BuildInterfaces(
        IReadOnlyList<IPcscDevice> pcscDevices,
        IReadOnlyList<IHidDevice> hidDevices)
    {
        var interfaces = new List<InterfaceCandidate>(pcscDevices.Count + hidDevices.Count);

        foreach (var pcscDevice in pcscDevices)
        {
            var device = yubiKeyFactory.Create(pcscDevice);
            var isUsb = pcscDevice.Kind == PscsConnectionKind.Usb;
            var pid = isUsb ? ReaderNamePidParser.FromReaderName(pcscDevice.ReaderName) : null;
            // NFC and other non-USB readers never share a USB container; do not even probe topology.
            var topologyKey = isUsb ? ResolveTopologyKey(pcscDevice, ConnectionType.SmartCard) : null;
            interfaces.Add(new InterfaceCandidate(device, ConnectionType.SmartCard, isUsb, pid, topologyKey));
        }

        foreach (var hidDevice in hidDevices)
        {
            var connection = ConnectionTypeMapper.ToConnectionType(hidDevice.InterfaceType)
                .SingleConcreteConnectionOrUnknown();
            if (connection == ConnectionType.Unknown)
            {
                Logger.LogDebug(
                    "Skipping unsupported HID interface {ReaderName} classified as {HidInterfaceType}.",
                    hidDevice.ReaderName,
                    hidDevice.InterfaceType);
                continue;
            }

            var device = yubiKeyFactory.Create(hidDevice);
            var rawPid = hidDevice.DescriptorInfo.ProductId;
            ushort? pid = rawPid > 0 && ReaderNamePidParser.IsKnownPid((ushort)rawPid) ? (ushort)rawPid : null;
            var topologyKey = ResolveTopologyKey(hidDevice, connection);
            interfaces.Add(new InterfaceCandidate(device, connection, IsUsb: true, pid, topologyKey));
        }

        return interfaces;
    }

    /// <summary>
    ///     Best-effort tier-1 topology read. Never throws and never blocks meaningfully: a failure is a
    ///     debug log and a null key, which drops the interface to the unchanged serial/PID tiers.
    /// </summary>
    private string? ResolveTopologyKey(IDevice device, ConnectionType connection)
    {
        try
        {
            return _topologyResolver.TryGetTopologyKey(device, connection, out var topologyKey)
                ? topologyKey
                : null;
        }
        catch (Exception e)
        {
            Logger.LogDebug(
                e,
                "Topology resolution threw for an interface over {Connection}; continuing without topology evidence.",
                connection);
            return null;
        }
    }

    private static bool NeedsSerialForAmbiguousPartialPid(
        IReadOnlyList<InterfaceCandidate> interfaces,
        InterfaceCandidate iface)
    {
        // Only an all-three-interface PID (OTP+FIDO+CCID) can present the ambiguous partial shape this
        // guard covers; ask the PID table for that rather than hardcoding which PIDs qualify.
        if (iface.Pid is not { } pid)
            return false;

        var expected = ReaderNamePidParser.ExpectedConnectionsForPid(pid);
        if (expected != ConnectionTypeExtensions.ConcreteConnections)
            return false;

        var samePid = interfaces.Where(i => i.IsUsb && i.Pid == pid).ToList();
        if (samePid.Count != 2)
            return false;

        var observed = samePid.Aggregate(
            ConnectionType.Unknown,
            static (current, candidate) => current | candidate.Connection);

        return observed.SupportsConnection(ConnectionType.SmartCard) && observed != expected;
    }

    private async Task<DeviceInfo?> ReadIdentityAsync(InterfaceCandidate iface, CancellationToken cancellationToken)
    {
        // A hit is valid only for the configuration it was read under: the PID is part of the evidence,
        // not incidental metadata. A mismatch is a miss, and a successful re-read overwrites below.
        if (_identityCache.TryGetValue(iface.Device.DeviceId, out var cached) && cached.Pid == iface.Pid)
            return cached.Info;

        var info = await DiscoveryIdentityReader
            .TryReadAsync(iface.Device, iface.Connection, Logger, cancellationToken)
            .ConfigureAwait(false);

        // Cache only successful reads so a transient failure is retried on the next scan (not poisoned).
        if (info is { } identity)
            _identityCache[iface.Device.DeviceId] = new CachedIdentity(identity, iface.Pid, iface.Connection);

        return info;
    }

    /// <inheritdoc />
    public void NotifyTransportActivity(ConnectionType transport)
    {
        // Coarse per-transport eviction, deliberately. The events carry no reliable per-interface identity
        // (the PC/SC listener is payload-less; HID hints are diagnostic-only), and hotplug is rare enough
        // that re-reading one transport's serials on the next scan is the proportionate price for never
        // attributing a departed key's serial to its same-slot successor. Scoped per transport because a
        // removal observed on HID says nothing about what is in the PC/SC slots.
        //
        // Races with an in-flight scan are benign: entries are re-added only by a successful fresh read,
        // and the event that triggered this also triggers a rescan, so a stale value consumed by an
        // overlapping scan is corrected by the next one.
        foreach (var entry in _identityCache)
        {
            // Matches expands the Hid group filter to HidFido|HidOtp; a raw & would not (Hid is its own bit).
            if (transport.Matches(entry.Value.Transport))
                _ = _identityCache.TryRemove(entry.Key, out _);
        }
    }

    private async Task PopulateMetadataAsync(
        IReadOnlyList<YubiKeyDevice> merged,
        IReadOnlyList<InterfaceCandidate> interfaces,
        CancellationToken cancellationToken)
    {
        // Always evict stale metadata once per scan, including scans with no published devices.
        EvictAbsentMetadata(interfaces);

        var devices = merged.Where(device => device.DeviceInfo is null).ToList();
        if (devices.Count == 0)
            return;

        // Read best-effort metadata for each published key concurrently (bounded by one timeout, never blocks
        // the merge result which is already computed).
        var reads = devices.Select(async device =>
        {
            var key = device.PhysicalIdentityKey;
            if (_metadataCache.TryGetValue(key, out var cached))
            {
                device.DeviceInfo = cached.Info;
                return;
            }

            // Identity disambiguation already consumed this scan's bounded device-info budget, potentially
            // while waiting for worker admission before native open. Do not stack a metadata budget onto it;
            // a failed identity read is retried on the next scan.
            if (device.IdentityReadBudgetConsumedThisScan)
                return;

            var info = await CompositeMetadataReader
                .TryReadAsync(device, MetadataReadBudget, Logger, cancellationToken)
                .ConfigureAwait(false);

            if (info is { } metadata)
            {
                _metadataCache[key] = new MetadataCacheEntry(metadata, device.InterfaceIds);
                device.DeviceInfo = metadata;
            }
        });

        await Task.WhenAll(reads).ConfigureAwait(false);
    }

    private void EvictAbsentIdentities(IReadOnlyList<InterfaceCandidate> interfaces)
    {
        var present = interfaces.Select(i => i.Device.DeviceId).ToHashSet();
        foreach (var staleKey in _identityCache.Keys.Where(k => !present.Contains(k)).ToList())
            _ = _identityCache.TryRemove(staleKey, out _);
    }

    private void EvictAbsentMetadata(IReadOnlyList<InterfaceCandidate> interfaces)
    {
        var present = interfaces.Select(i => i.Device.DeviceId).ToHashSet();
        foreach (var entry in _metadataCache)
        {
            // An entry is kept only while all of its member interface ids are still enumerated.
            if (entry.Value.InterfaceIds.Any(id => !present.Contains(id)))
                _ = _metadataCache.TryRemove(entry.Key, out _);
        }
    }

    public static FindYubiKeys Create() =>
        new(FindPcscDevices.Create(), FindHidDevices.Create(), YubiKeyFactory.Create());

    private readonly record struct InterfaceCandidate(
        IYubiKeyConnectionSlot Device,
        ConnectionType Connection,
        bool IsUsb,
        ushort? Pid,
        string? TopologyKey = null)
    {
        public DeviceInterfaceDescriptor ToDescriptor(
            DeviceInfo? info,
            bool identityReadBudgetConsumed = false) =>
            new(
                Device,
                Connection,
                IsUsb,
                Pid,
                info?.SerialNumber,
                info,
                TopologyKey,
                identityReadBudgetConsumed);
    }

    // Only successful metadata reads are cached, so Info is always present.
    private readonly record struct MetadataCacheEntry(DeviceInfo Info, IReadOnlyList<string> InterfaceIds);
}