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
namespace AuditMerge20261111.Examples.Group18;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial, int expectedSerialNumber, string[] args, Action<DeviceChange> Apply, IYubiKey device)
{
await foreach (DeviceChange change in YubiKeyManager.WatchAsync())
{
    Console.WriteLine(change switch
    {
        DeviceAdded added => $"Added: serial {added.Device.SerialNumber}",
        DeviceRemoved removed => $"Removed: serial {removed.Device.SerialNumber}",
        DeviceUpdated updated => $"Updated: serial {updated.Device.SerialNumber}",
        SnapshotComplete => "Initial snapshot complete",
        _ => "Unknown change"
    });
}

await Task.CompletedTask;
}
}
