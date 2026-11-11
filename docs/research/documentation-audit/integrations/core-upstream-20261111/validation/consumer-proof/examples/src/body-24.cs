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
namespace AuditIntegration20261111.Example24;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken)
{

var device = await YubiKeyManager.FindFirstAsync(cancellationToken: cancellationToken);
await using var session = await device.CreateManagementSessionAsync(
    cancellationToken: cancellationToken);

var info = await session.GetDeviceInfoAsync(cancellationToken);
Console.WriteLine(info.SerialNumber);
Console.WriteLine(info.VersionName);

}
}
