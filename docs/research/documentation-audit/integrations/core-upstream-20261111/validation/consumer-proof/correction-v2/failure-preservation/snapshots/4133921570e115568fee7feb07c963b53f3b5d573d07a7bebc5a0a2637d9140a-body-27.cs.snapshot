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
namespace AuditIntegration20261111.Example27;
public static class Example
{
public static async Task RunAsync(int serial, CancellationToken ct, Action<DeviceChange> Apply)
{
var key = await YubiKeyManager.FindFirstAsync(
    predicate: key => key.SerialNumber == serial, cancellationToken: ct);

await using var monitor = await YubiKeyManager.CreateMonitorAsync(ct);
await foreach (var change in monitor.WatchAsync(ct))
    Apply(change); // DeviceAdded replay, SnapshotComplete, then live changes.

}
}
