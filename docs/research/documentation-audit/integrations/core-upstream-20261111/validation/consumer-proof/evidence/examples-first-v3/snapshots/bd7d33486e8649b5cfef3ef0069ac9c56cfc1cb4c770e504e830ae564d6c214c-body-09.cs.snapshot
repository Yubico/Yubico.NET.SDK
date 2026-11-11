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
namespace AuditIntegration20261111.Example09;
public static class Example
{
static async Task WatchOnUiAsync(Action<DeviceChange> apply, CancellationToken ct)
{
    var ui = SynchronizationContext.Current
        ?? throw new InvalidOperationException("Start watching from the UI thread.");
    await foreach (var change in YubiKeyManager.WatchAsync(ct))
        ui.Post(_ => apply(change), null);
}
}
