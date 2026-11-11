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
namespace AuditIntegration20261111.Example06;
public static class Example
{
public static async Task RunAsync()
{

using var stop = new CancellationTokenSource();
var ct = stop.Token; // Cancel from the application's shutdown path.
await foreach (var change in YubiKeyManager.WatchAsync(ct))
{
    Console.WriteLine(change switch
    {
        DeviceAdded added => $"Connected: {added.Device.DeviceId}",
        DeviceRemoved removed => $"Removed: {removed.Device.DeviceId}",
        DeviceUpdated updated => $"Metadata changed: {updated.Device.DeviceId}",
        SnapshotComplete complete => $"Initial snapshot ready at revision {complete.Revision}",
        _ => "Unknown change"
    });
}

}
}
