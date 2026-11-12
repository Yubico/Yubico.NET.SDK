using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;

namespace Audit20261111.DocExamples.Full;
internal static class Body188
{
static async Task WatchOnUiAsync(Action<DeviceChange> apply, CancellationToken ct)
{
    var ui = SynchronizationContext.Current
        ?? throw new InvalidOperationException("Start watching from the UI thread.");
    await foreach (var change in YubiKeyManager.WatchAsync(ct))
        ui.Post(_ => apply(change), null);
}
}
