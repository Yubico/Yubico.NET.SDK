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
namespace AuditMerge20261111.Examples.Group20;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial, int expectedSerialNumber, string[] args, Action<DeviceChange> Apply, IYubiKey device)
{
await using var monitor = await YubiKeyManager.CreateMonitorAsync(cancellationToken);
await foreach (var change in monitor.WatchAsync(cancellationToken))
{
    // DeviceAdded replay, SnapshotComplete (even if empty), then live DeviceChange records.
    Apply(change);
}

await Task.CompletedTask;
}
}
