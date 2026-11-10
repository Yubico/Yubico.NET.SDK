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
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

public interface IFindYubiKeys
{
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default);
}
/// <summary>
///     Discovers physical YubiKeys with bounded native enumeration and read waits. Await scans and pass a
///     cancellation token to cancel their waits. Enumeration timeouts omit only the affected transport;
///     caller cancellation throws <see cref="OperationCanceledException" />, and other enumeration failures propagate.
/// </summary>
public class FindYubiKeys : IFindYubiKeys, IMonitoredDiscovery
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<FindYubiKeys>();

    private readonly IFindPcscDevices? findPcscService;
    private readonly IFindHidInterfaces? findHidService;
    private readonly Func<IDevice, IYubiKeyConnectionSlot> createSlot;
    private readonly ConnectionType _scope;
    private readonly TimeSpan _enumerationBudget;
    private readonly Lock _evidenceLock = new();

    // Tier-1 evidence source. Optional by contract: the no-topology resolver on macOS/Linux, and on Windows
    // any per-interface read failure simply yields no key for that interface.
    private readonly IDeviceTopologyResolver _topologyResolver;

    /// <summary>Creates a scoped finder. Callers must retire its read scope when discovery ends.</summary>
    /// <param name="findPcscService">Enumerator required when the scope includes smart cards.</param>
    /// <param name="findHidService">Enumerator required when the scope includes HID.</param>
    /// <param name="createSlot">Creates a connection slot for each included interface.</param>
    /// <param name="scope">Interfaces to enumerate.</param>
    /// <param name="topologyResolver">Optional platform topology resolver.</param>
    /// <param name="enumerationBudget">Maximum wait per transport; defaults to the discovery enumeration budget.</param>
    /// <exception cref="ArgumentNullException">An enumerator required by the scope is missing.</exception>
    internal FindYubiKeys(
        IFindPcscDevices? findPcscService,
        IFindHidInterfaces? findHidService,
        Func<IDevice, IYubiKeyConnectionSlot> createSlot,
        ConnectionType scope = ConnectionType.All,
        IDeviceTopologyResolver? topologyResolver = null,
        TimeSpan? enumerationBudget = null)
    {
        _scope = scope.ToConcreteScope();
        _enumerationBudget = enumerationBudget ?? DiscoveryBudgets.Enumeration;
        this.findPcscService = _scope.SupportsConnection(ConnectionType.SmartCard)
            ? findPcscService ?? throw new ArgumentNullException(nameof(findPcscService))
            : null;
        this.findHidService = _scope.IncludesHidScan()
            ? findHidService ?? throw new ArgumentNullException(nameof(findHidService))
            : null;
        this.createSlot = createSlot;
        _topologyResolver = topologyResolver ?? DeviceTopologyResolver.Create();
    }

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
    //    hardware changed; InvalidateEvidence discards all cached evidence.
    private EvidenceEpoch _evidence = new();

    /// <summary>A successful identity read plus the evidence context that makes it reusable.</summary>
    private readonly record struct CachedIdentity(DeviceInfo Info, ushort? Pid);

    // Best-effort metadata cache, keyed by the published device's stable interface-set key
    // (YubiKeyDevice.PhysicalIdentityKey, NOT DeviceId, which can flip between pid- and
    // serial-forms). Evicted when any member interface disappears.
    private sealed class EvidenceEpoch
    {
        internal ConcurrentDictionary<string, CachedIdentity> IdentityCache { get; } = new();
        internal ConcurrentDictionary<string, MetadataCacheEntry> MetadataCache { get; } = new();
        internal ProtocolDeviceInfo.ReadScope ReadScope { get; } = ProtocolDeviceInfo.CreateScope();
    }

    // Serializes discovery so two concurrent scans do not open connections to the same interface at once.
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    // One outstanding native enumeration per transport, guarded by _scanLock. A scan that times out leaves
    // its enumeration here so the next scan waits on the same work instead of dispatching another native
    // call behind a hung one; repeated timeouts must not accumulate blocked workers.
    private readonly OutstandingEnumeration<IPcscDevice> _pcscEnumeration = new();
    private readonly OutstandingEnumeration<IHidInterface> _hidEnumeration = new();

    private sealed class OutstandingEnumeration<T>
    {
        internal Task<IReadOnlyList<T>>? Task;
    }

    public Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default) =>
        FindAllCoreAsync(type, tolerateEnumerationTimeout: true, cancellationToken);

    // One-shot callers prefer a partial answer now over none. A monitor must not publish a partial scan: with a
    // previous snapshot to diff against, a stalled transport would surface as removals of every key on it.
    private async Task<IReadOnlyList<IYubiKey>> FindAllCoreAsync(
        ConnectionType type,
        bool tolerateEnumerationTimeout,
        CancellationToken cancellationToken)
    {
        if (type == ConnectionType.Unknown || _scope == ConnectionType.Unknown)
            return [];

        await _scanLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EvidenceEpoch evidence;
            lock (_evidenceLock)
                evidence = _evidence;

            var interfaces = await EnumerateInterfacesAsync(tolerateEnumerationTimeout, cancellationToken).ConfigureAwait(false);
            EvictAbsentIdentities(evidence, interfaces);
            foreach (var iface in interfaces.Where(iface => iface.IsUsb))
                evidence.ReadScope.CoordinateUsbRead(iface.Device.InterfaceId, iface.Pid);

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
                    ? await ReadIdentityAsync(evidence, iface, cancellationToken).ConfigureAwait(false)
                    : null;

                return iface.ToDescriptor(info, needsSerial);
            })).ConfigureAwait(false);

            var merged = CompositeDeviceMerger.Merge(descriptors, pidCorrelationUntrusted);
            await PopulateMetadataAsync(evidence, merged, interfaces, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return [.. merged.Where(d => type.Matches(d.AvailableConnections))];
        }
        finally
        {
            _scanLock.Release();
        }
    }

    private async Task<List<InterfaceCandidate>> EnumerateInterfacesAsync(
        bool tolerateTimeout,
        CancellationToken cancellationToken)
    {
        // The immutable finder scope bounds enumeration and evidence. The requested result filter still
        // applies only AFTER merging; it never changes this finder's cache/absence interpretation.
        // Both waits start together so the enumeration stage consumes at most one budget, not two.
        var pcscTask = findPcscService is { } pcsc
            ? EnumerateBoundedAsync(_pcscEnumeration, pcsc.FindAllAsync, "PC/SC", tolerateTimeout, cancellationToken)
            : Task.FromResult<IReadOnlyList<IPcscDevice>>([]);
        var hidTask = findHidService is { } hid
            ? EnumerateBoundedAsync(_hidEnumeration, hid.FindAllAsync, "HID", tolerateTimeout, cancellationToken)
            : Task.FromResult<IReadOnlyList<IHidInterface>>([]);
        await Task.WhenAll(pcscTask, hidTask).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return BuildInterfaces(await pcscTask.ConfigureAwait(false), await hidTask.ConfigureAwait(false));
    }

    private async Task<IReadOnlyList<T>> EnumerateBoundedAsync<T>(
        OutstandingEnumeration<T> outstanding,
        Func<CancellationToken, Task<IReadOnlyList<T>>> enumerate,
        string transport,
        bool tolerateTimeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reused = outstanding.Task is { IsCompleted: false };
        var enumeration = reused ? outstanding.Task! : enumerate(cancellationToken);
        outstanding.Task = enumeration;
        try
        {
            var result = await enumeration.WaitAsync(_enumerationBudget, cancellationToken).ConfigureAwait(false);
            outstanding.Task = null;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (!reused)
                _ = ObserveAbandonedEnumerationAsync(enumeration, transport);
            throw;
        }
        catch (OperationCanceledException) when (reused)
        {
            // The reused enumeration honoured an earlier caller's cancellation; nothing is outstanding now.
            outstanding.Task = null;
            return await EnumerateBoundedAsync(outstanding, enumerate, transport, tolerateTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (!reused)
                _ = ObserveAbandonedEnumerationAsync(enumeration, transport);
            cancellationToken.ThrowIfCancellationRequested();
            if (!tolerateTimeout)
            {
                throw new TimeoutException(
                    $"{transport} interface enumeration exceeded its {_enumerationBudget} budget; the scan was not published.");
            }

            Logger.LogWarning(
                "{Transport} interface enumeration exceeded its {Budget} budget; continuing without its interfaces this scan.",
                transport, _enumerationBudget);
            return [];
        }
    }

    private static async Task ObserveAbandonedEnumerationAsync(Task enumeration, string transport)
    {
        try
        {
            await enumeration.ConfigureAwait(false);
            Logger.LogDebug("Abandoned {Transport} interface enumeration completed.", transport);
        }
        catch (Exception exception)
        {
            Logger.LogDebug(exception, "Abandoned {Transport} interface enumeration failed.", transport);
        }
    }

    private List<InterfaceCandidate> BuildInterfaces(
        IReadOnlyList<IPcscDevice> pcscDevices,
        IReadOnlyList<IHidInterface> hidInterfaces)
    {
        var interfaces = new List<InterfaceCandidate>(pcscDevices.Count + hidInterfaces.Count);

        foreach (var pcscDevice in pcscDevices)
        {
            var device = createSlot(pcscDevice);
            var isUsb = pcscDevice.Kind == PscsConnectionKind.Usb;
            var pid = isUsb ? ReaderNamePidParser.FromReaderName(pcscDevice.ReaderName) : null;
            // NFC and other non-USB readers never share a USB container; do not even probe topology.
            var topologyKey = isUsb ? ResolveTopologyKey(pcscDevice, ConnectionType.SmartCard) : null;
            interfaces.Add(new InterfaceCandidate(device, ConnectionType.SmartCard, isUsb, pid, topologyKey));
        }

        foreach (var hidInterface in hidInterfaces)
        {
            var connection = ConnectionTypeMapper.ToConnectionType(hidInterface.InterfaceType)
                .SingleConcreteConnectionOrUnknown();
            if (connection == ConnectionType.Unknown)
            {
                Logger.LogDebug(
                    "Skipping unsupported HID interface {ReaderName} classified as {HidInterfaceType}.",
                    hidInterface.ReaderName,
                    hidInterface.InterfaceType);
                continue;
            }

            // OTP and FIDO share descriptor enumeration. Excluded interfaces never become slots, resolve
            // topology, contribute grouping evidence, or open a connection for identity/metadata reads.
            if (!_scope.SupportsConnection(connection))
                continue;

            var device = createSlot(hidInterface);
            var rawPid = hidInterface.DescriptorInfo.ProductId;
            ushort? pid = rawPid > 0 && ReaderNamePidParser.IsKnownPid((ushort)rawPid) ? (ushort)rawPid : null;
            var topologyKey = ResolveTopologyKey(hidInterface, connection);
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

    private async Task<DeviceInfo?> ReadIdentityAsync(
        EvidenceEpoch evidence,
        InterfaceCandidate iface,
        CancellationToken cancellationToken)
    {
        // A hit is valid only for the configuration it was read under: the PID is part of the evidence,
        // not incidental metadata. A mismatch is a miss, and a successful re-read overwrites below.
        if (evidence.IdentityCache.TryGetValue(iface.Device.InterfaceId, out var cached) && cached.Pid == iface.Pid)
            return cached.Info;

        var info = await DiscoveryIdentityReader
            .TryReadAsync(iface.Device, iface.Connection, Logger, cancellationToken, evidence.ReadScope)
            .ConfigureAwait(false);

        // Cache only successful reads in the captured epoch. If transport activity replaced the epoch while
        // this read was running, this write lands in unreachable old evidence and cannot affect later scans.
        if (info is { } identity)
            evidence.IdentityCache[iface.Device.InterfaceId] = new CachedIdentity(identity, iface.Pid);

        return info;
    }

    Task<IReadOnlyList<IYubiKey>> IMonitoredDiscovery.FindAllAsync(CancellationToken cancellationToken) =>
        FindAllCoreAsync(ConnectionType.All, tolerateEnumerationTimeout: false, cancellationToken);

    void IMonitoredDiscovery.InvalidateEvidence(ConnectionType transport)
    {
        // Full eviction, deliberately - the events carry no reliable per-interface identity (the PC/SC
        // listener is payload-less; HID hints are diagnostic-only), and per-transport scoping is unsound
        // for composite keys: a swap whose events arrive on one transport first would refresh that
        // transport's evidence while the sibling transport still names the departed key, splitting one
        // replacement key into phantoms. Hotplug is rare and identity/metadata reads are budgeted, so
        // re-reading on the next scan is the proportionate price for never mixing two keys' evidence.
        //
        // Replace caches and read scope as one atomic evidence epoch. Scans already in flight keep only the
        // retired object, so their writes cannot repopulate current caches; scans starting afterward cannot
        // join reads started against the departed topology.
        lock (_evidenceLock)
        {
            // Retirement is terminal, including callbacks already in flight during listener teardown.
            if (!_evidence.ReadScope.TryCaptureEpoch(out _))
                return;

            var retired = _evidence;
            ProtocolDeviceInfo.Retire(retired.ReadScope);
            _evidence = new EvidenceEpoch();
        }
    }

    void IMonitoredDiscovery.Retire() => RetireReadScope();

    internal void RetireReadScope()
    {
        // A discarded one-shot finder must not leave identity reads queued behind hung native workers.
        // Retirement prevents new opens; already-open reads keep their admission/lease through teardown.
        lock (_evidenceLock)
            ProtocolDeviceInfo.Retire(_evidence.ReadScope);
    }

    private async Task PopulateMetadataAsync(
        EvidenceEpoch evidence,
        IReadOnlyList<YubiKeyDevice> merged,
        IReadOnlyList<InterfaceCandidate> interfaces,
        CancellationToken cancellationToken)
    {
        // Always evict stale metadata once per scan, including scans with no published devices.
        EvictAbsentMetadata(evidence, interfaces);

        var devices = merged.Where(device => device.DeviceInfo is null).ToList();
        if (devices.Count == 0)
            return;

        // Read best-effort metadata for each published key concurrently. Grouping is already computed and
        // does not depend on these results, but the bounded reads are awaited and may delay scan completion
        // by up to the metadata budget. Reads and cache writes remain in the captured evidence epoch; if
        // transport activity swaps it out, stale completion cannot reach current scans.
        var reads = devices.Select(async device =>
        {
            var key = device.PhysicalIdentityKey;
            if (evidence.MetadataCache.TryGetValue(key, out var cached))
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
                .TryReadAsync(device, DiscoveryBudgets.MetadataRead, Logger, cancellationToken, evidence.ReadScope)
                .ConfigureAwait(false);

            if (info is { } metadata)
            {
                evidence.MetadataCache[key] = new MetadataCacheEntry(metadata, device.InterfaceIds);
                device.DeviceInfo = metadata;
            }
        });

        await Task.WhenAll(reads).ConfigureAwait(false);
    }

    private static void EvictAbsentIdentities(EvidenceEpoch evidence, IReadOnlyList<InterfaceCandidate> interfaces)
    {
        var present = interfaces.Select(i => i.Device.InterfaceId).ToHashSet();
        foreach (var staleKey in evidence.IdentityCache.Keys.Where(k => !present.Contains(k)).ToList())
            _ = evidence.IdentityCache.TryRemove(staleKey, out _);
    }

    private static void EvictAbsentMetadata(EvidenceEpoch evidence, IReadOnlyList<InterfaceCandidate> interfaces)
    {
        var present = interfaces.Select(i => i.Device.InterfaceId).ToHashSet();
        foreach (var entry in evidence.MetadataCache)
        {
            // An entry is kept only while all of its member interface ids are still enumerated.
            if (entry.Value.InterfaceIds.Any(id => !present.Contains(id)))
                _ = evidence.MetadataCache.TryRemove(entry.Key, out _);
        }
    }

    public static FindYubiKeys Create() => CreateForScope(ConnectionType.All);

    internal static FindYubiKeys CreateForScope(ConnectionType scope)
    {
        scope = scope.ToConcreteScope();
        // Do not even construct excluded transport backends. In particular a HID-only scan must not bind
        // the PC/SC native API or snapshot its connection policy.
        var smartCardConnectionFactory = scope.SupportsConnection(ConnectionType.SmartCard)
            ? SmartCardConnectionFactory.CreateDefault()
            : null;
        return new(
            scope.SupportsConnection(ConnectionType.SmartCard) ? FindPcscDevices.Create() : null,
            scope.IncludesHidScan() ? FindHidInterfaces.Create() : null,
            device => device switch
            {
                IPcscDevice pcscDevice when smartCardConnectionFactory is not null =>
                    new PcscConnectionSlot(pcscDevice, smartCardConnectionFactory),
                IHidInterface hidInterface => new HidConnectionSlot(hidInterface),
                _ => throw new NotSupportedException(
                    $"Device type {device.GetType().Name} is not supported as a connection slot.")
            },
            scope);
    }

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