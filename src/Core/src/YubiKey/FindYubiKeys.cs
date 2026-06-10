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
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

public interface IFindYubiKeys
{
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default);
}

public class FindYubiKeys(
    IFindPcscDevices findPcscService,
    IFindHidDevices findHidService,
    IYubiKeyFactory yubiKeyFactory) : IFindYubiKeys
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<FindYubiKeys>();

    // Hard timeout for a single best-effort metadata read over one transport. Bounded so a locked/slow CCID
    // cannot stall discovery; reads run concurrently across keys so total added latency is ~one timeout.
    private static readonly TimeSpan MetadataReadTimeout = TimeSpan.FromSeconds(3);

    // Serial-disambiguation identity cache (PID-count>1 / force-serial path), keyed by per-interface DeviceId.
    // Presence means the interface's identity was read; null value = read failed or serial-disabled.
    private readonly ConcurrentDictionary<string, DeviceInfo?> _identityCache = new();

    // Best-effort metadata cache, keyed by the merged device's stable interface-set key (NOT the composite
    // DeviceId, which can flip between pid- and serial-forms). Evicted when any member interface disappears.
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
            var forceSerial = interfaces.Any(i =>
                i is { IsUsb: true, Connection: ConnectionType.SmartCard, Pid: null });
            if (forceSerial)
            {
                Logger.LogWarning(
                    "A USB CCID reader name did not parse to a known YubiKey PID; falling back to serial-based " +
                    "merge for all USB interfaces this scan (PID correlation degraded).");
            }

            var pidCounts = CompositeDeviceMerger.ComputePidCounts(
                interfaces.Select(i => i.ToDescriptor(null)).Where(d => d.IsUsb));

            var descriptors = new List<DeviceInterfaceDescriptor>(interfaces.Count);
            foreach (var iface in interfaces)
            {
                var needsSerial = iface.IsUsb &&
                    (forceSerial || (iface.Pid is { } pid && pidCounts.GetValueOrDefault(pid) > 1));

                var info = needsSerial
                    ? await ReadIdentityAsync(iface, cancellationToken).ConfigureAwait(false)
                    : null;

                descriptors.Add(iface.ToDescriptor(info));
            }

            var merged = CompositeDeviceMerger.Merge(descriptors, forceSerial);
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
            interfaces.Add(new InterfaceCandidate(device, ConnectionType.SmartCard, isUsb, pid));
        }

        foreach (var hidDevice in hidDevices)
        {
            var device = yubiKeyFactory.Create(hidDevice);
            var rawPid = hidDevice.DescriptorInfo.ProductId;
            ushort? pid = rawPid > 0 && ReaderNamePidParser.IsKnownPid((ushort)rawPid) ? (ushort)rawPid : null;
            interfaces.Add(new InterfaceCandidate(device, device.AvailableConnections, IsUsb: true, pid));
        }

        return interfaces;
    }

    private async Task<DeviceInfo?> ReadIdentityAsync(InterfaceCandidate iface, CancellationToken cancellationToken)
    {
        if (_identityCache.TryGetValue(iface.Device.DeviceId, out var cached))
            return cached;

        var info = await DiscoveryIdentityReader
            .TryReadAsync(iface.Device, iface.Connection, Logger, cancellationToken)
            .ConfigureAwait(false);

        // Cache only successful reads so a transient failure is retried on the next scan (not poisoned).
        if (info is not null)
            _identityCache[iface.Device.DeviceId] = info;

        return info;
    }

    private async Task PopulateMetadataAsync(
        IReadOnlyList<IYubiKey> merged,
        IReadOnlyList<InterfaceCandidate> interfaces,
        CancellationToken cancellationToken)
    {
        // Always evict stale metadata once per scan, even when this scan has no composites (so unplugging
        // the last composite does not leave entries behind).
        EvictAbsentMetadata(interfaces);

        var composites = merged.OfType<CompositeYubiKey>().Where(c => c.DeviceInfo is null).ToList();
        if (composites.Count == 0)
            return;

        // Read best-effort metadata for each merged key concurrently (bounded by one timeout, never blocks
        // the merge result which is already computed).
        var reads = composites.Select(async composite =>
        {
            var key = MetadataKey(composite);
            if (_metadataCache.TryGetValue(key, out var cached))
            {
                composite.DeviceInfo = cached.Info;
                return;
            }

            var info = await CompositeMetadataReader
                .TryReadAsync(composite, MetadataReadTimeout, Logger, cancellationToken)
                .ConfigureAwait(false);

            if (info is not null)
            {
                _metadataCache[key] = new MetadataCacheEntry(info, composite.MemberDeviceIds);
                composite.DeviceInfo = info;
            }
        });

        await Task.WhenAll(reads).ConfigureAwait(false);
    }

    // Collision-free key over the (already sorted) member ids: length-prefixing each part makes the
    // boundaries unambiguous even if a reader name / device path contains delimiter characters.
    private static string MetadataKey(CompositeYubiKey composite)
    {
        var builder = new StringBuilder();
        foreach (var id in composite.MemberDeviceIds)
            builder.Append(id.Length).Append(':').Append(id);
        return builder.ToString();
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
            if (entry.Value.MemberIds.Any(id => !present.Contains(id)))
                _ = _metadataCache.TryRemove(entry.Key, out _);
        }
    }

    public static FindYubiKeys Create() =>
        new(FindPcscDevices.Create(), FindHidDevices.Create(), YubiKeyFactory.Create());

    private readonly record struct InterfaceCandidate(IYubiKey Device, ConnectionType Connection, bool IsUsb, ushort? Pid)
    {
        public DeviceInterfaceDescriptor ToDescriptor(DeviceInfo? info) =>
            new(Device, Connection, IsUsb, Pid, info?.SerialNumber, info);
    }

    private readonly record struct MetadataCacheEntry(DeviceInfo? Info, IReadOnlyList<string> MemberIds);
}