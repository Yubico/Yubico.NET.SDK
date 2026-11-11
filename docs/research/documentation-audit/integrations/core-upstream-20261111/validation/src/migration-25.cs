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
namespace AuditMerge20261111.Examples.Group25;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial, int expectedSerialNumber, string[] args, Action<DeviceChange> Apply, IYubiKey device)
{
// Find all connected YubiKeys
var devices = await YubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);
foreach (var device in devices)
{
    var info = await device.GetDeviceInfoAsync(cancellationToken: cancellationToken);
    Console.WriteLine($"Serial: {info.SerialNumber}");
    Console.WriteLine($"Firmware: {info.FirmwareVersion}");
    Console.WriteLine($"USB enabled: {info.UsbEnabled}");
}

// Find specific device
var yubiKey = devices.SingleOrDefault(device => device.SerialNumber == 12345678);

await Task.CompletedTask;
}
}
