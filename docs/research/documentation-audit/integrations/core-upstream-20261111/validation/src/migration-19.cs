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
namespace AuditMerge20261111.Examples.Group19;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial, int expectedSerialNumber, string[] args, Action<DeviceChange> Apply, IYubiKey device)
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

await Task.CompletedTask;
}
}
