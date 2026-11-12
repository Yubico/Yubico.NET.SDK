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
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;
using Yubico.YubiKit.Core.Devices;

namespace Audit20261111.DocExamples.Full;
internal static class Body185
{
internal static async Task CompileOnly()
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
