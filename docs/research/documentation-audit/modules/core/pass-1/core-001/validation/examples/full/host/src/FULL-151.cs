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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

namespace Audit20261111.DocExamples.Full;
internal static class Body151
{
internal static async Task CompileOnly()
{
// One IYubiKey per physical device, even when CCID + HID FIDO + HID OTP are all present.
var devices = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.All);

foreach (var device in devices)
{
    Console.WriteLine($"{device.DeviceId}: {device.AvailableConnections}");

    if (device.SupportsConnection(ConnectionType.SmartCard))
    {
        // This physical key exposes a smart card interface.
    }
}

// Keep full composite slots, then select physical keys capable of FIDO.
var fidoCapable = devices.Where(key => key.SupportsConnection(ConnectionType.HidFido));

// Or intentionally discover only FIDO interfaces; these YubiKeys have no CCID/OTP slots.
var fidoYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.HidFido);
}

}
