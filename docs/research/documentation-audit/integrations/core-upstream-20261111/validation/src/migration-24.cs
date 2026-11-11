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
namespace AuditMerge20261111.Examples.Group24;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial, int expectedSerialNumber, string[] args, Action<DeviceChange> Apply)
{

var device = await YubiKeyManager.FindFirstAsync(cancellationToken: cancellationToken);
await using var session = await device.CreateManagementSessionAsync(
    cancellationToken: cancellationToken);

var info = await session.GetDeviceInfoAsync(cancellationToken);
Console.WriteLine(info.SerialNumber);
Console.WriteLine(info.VersionName);

await Task.CompletedTask;
}
}
