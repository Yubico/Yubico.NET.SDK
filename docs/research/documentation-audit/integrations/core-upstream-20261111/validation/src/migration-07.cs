using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
namespace AuditMerge20261111.Examples.Group07;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial, int expectedSerialNumber, string[] args, Action<DeviceChange> Apply, IYubiKey device)
{

using var stop = new CancellationTokenSource();
var ct = stop.Token;
var devices = new HashSet<IYubiKey>(ReferenceEqualityComparer.Instance);
var loaded = false;
Console.WriteLine("Loading…");
await foreach (var change in YubiKeyManager.WatchAsync(ct))
{
    if (change is DeviceAdded added)
        devices.Add(added.Device);
    else if (change is DeviceRemoved removed)
        devices.Remove(removed.Device);
    else if (change is SnapshotComplete)
        loaded = true;
    // DeviceUpdated retains the object; render its updated CachedDeviceInfo below.
    Console.WriteLine(!loaded ? "Loading…" : devices.Count == 0 ? "No YubiKeys connected" : $"{devices.Count} YubiKey(s)");
}

await Task.CompletedTask;
}
}
