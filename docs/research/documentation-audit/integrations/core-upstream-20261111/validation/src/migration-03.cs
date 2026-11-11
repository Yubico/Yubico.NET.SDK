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
namespace AuditMerge20261111.Examples.Group03;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial, int expectedSerialNumber, string[] args, Action<DeviceChange> Apply, IYubiKey device)
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

await Task.CompletedTask;
}
}
