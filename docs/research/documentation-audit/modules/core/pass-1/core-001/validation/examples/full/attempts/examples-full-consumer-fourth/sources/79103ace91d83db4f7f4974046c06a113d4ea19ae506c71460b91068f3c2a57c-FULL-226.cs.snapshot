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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;

namespace Audit20261111.DocExamples.Full;
internal static class Body226
{
internal static async Task CompileOnly()
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
}

}
