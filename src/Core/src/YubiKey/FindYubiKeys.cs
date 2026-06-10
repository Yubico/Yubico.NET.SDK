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

    // Discovery-time identity cache keyed by the per-interface stable pre-key (the interface IYubiKey's
    // DeviceId). Presence of a key means the interface's identity was already read this session; the value
    // is the read result (null = read failed or serial-disabled). Entries are evicted when the interface
    // leaves the inventory so a recycled reader name / HID path cannot reuse a stale serial (ISC-12.1).
    private readonly ConcurrentDictionary<string, DeviceInfo?> _identityCache = new();

    // Serializes discovery so two concurrent scans (e.g. the monitor's rescan and a caller's forced
    // rescan) do not open connections to the same interface at once, which causes PC/SC sharing violations.
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

            // Merging is only possible when more than one USB interface is present; otherwise skip the cost
            // of opening connections to read identity (ISC-12).
            var usbInterfaceCount = interfaces.Count(i => i.IsUsb);
            var descriptors = new List<DeviceInterfaceDescriptor>(interfaces.Count);
            foreach (var iface in interfaces)
            {
                DeviceInfo? info = null;
                if (iface.IsUsb && usbInterfaceCount > 1)
                    info = await ReadIdentityAsync(iface, cancellationToken).ConfigureAwait(false);

                descriptors.Add(new DeviceInterfaceDescriptor(
                    iface.Device,
                    iface.Connection,
                    iface.IsUsb,
                    info?.SerialNumber,
                    info));
            }

            var merged = CompositeDeviceMerger.Merge(descriptors);
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
            interfaces.Add(new InterfaceCandidate(
                device,
                ConnectionType.SmartCard,
                pcscDevice.Kind == PscsConnectionKind.Usb));
        }

        foreach (var hidDevice in hidDevices)
        {
            var device = yubiKeyFactory.Create(hidDevice);
            interfaces.Add(new InterfaceCandidate(device, device.AvailableConnections, IsUsb: true));
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

        // Cache only successful reads (including an authoritative null-serial read on a serial-disabled key).
        // A failed read returns null and is NOT cached, so a transient failure (e.g. a brief PC/SC sharing
        // violation) is retried on the next scan instead of permanently splitting the physical device.
        if (info is not null)
            _identityCache[iface.Device.DeviceId] = info;

        return info;
    }

    private void EvictAbsentIdentities(IReadOnlyList<InterfaceCandidate> interfaces)
    {
        var present = interfaces.Select(i => i.Device.DeviceId).ToHashSet();
        foreach (var staleKey in _identityCache.Keys.Where(k => !present.Contains(k)).ToList())
            _ = _identityCache.TryRemove(staleKey, out _);
    }

    public static FindYubiKeys Create() =>
        new(FindPcscDevices.Create(), FindHidDevices.Create(), YubiKeyFactory.Create());

    private readonly record struct InterfaceCandidate(IYubiKey Device, ConnectionType Connection, bool IsUsb);
}