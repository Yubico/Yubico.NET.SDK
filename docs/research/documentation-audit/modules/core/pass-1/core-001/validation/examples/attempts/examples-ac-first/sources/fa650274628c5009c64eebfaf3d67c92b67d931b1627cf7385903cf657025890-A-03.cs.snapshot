using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Devices;

namespace Audit20261111.DocExamples;
internal static class ExampleA03
{
static async Task TrackAsync(CancellationToken cancellationToken)
{
    await using var monitor = await YubiKeyManager.CreateMonitorAsync(cancellationToken);
    await foreach (var change in monitor.WatchAsync(cancellationToken))
    {
        Console.WriteLine(change switch
        {
            DeviceAdded added => $"Added: {added.Device.DeviceId}",
            DeviceRemoved removed => $"Removed: {removed.Device.DeviceId}",
            DeviceUpdated updated => $"Updated: {updated.Device.DeviceId}",
            SnapshotComplete complete => $"Snapshot complete at revision {complete.Revision}",
            _ => throw new InvalidOperationException("Unknown device change.")
        });
    }
}
}
