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
namespace AuditIntegration20261111.Example05;
public static class Example
{
public static async Task RunAsync()
{
var device = await YubiKeyManager.FindFirstAsync();
if (device.CachedDeviceInfo is { } info)
{
    Console.WriteLine($"Last observed firmware: {info.FirmwareVersion}");
    Console.WriteLine($"Last observed USB capabilities: {info.UsbEnabled}");
}
else
{
    Console.WriteLine("Device metadata is unavailable.");
}

}
}
