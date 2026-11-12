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
internal static class Body182
{
internal static async Task CompileOnly()
{
// Find all devices
var allDevices = await YubiKeyManager.FindAllAsync();

// Enumerate/probe only SmartCard interfaces (no HID slots are returned).
var smartCardDevices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);

// Find only HID-connected devices (FIDO2, OTP)
var hidDevices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);

// Find one device. The predicate is optional.
var firstDevice = await YubiKeyManager.FindFirstAsync();
var firstSmartCardDevice = await YubiKeyManager.FindFirstOrDefaultAsync(
    device => device.SupportsConnection(ConnectionType.SmartCard));
}

}
