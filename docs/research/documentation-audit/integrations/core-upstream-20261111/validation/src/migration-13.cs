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
namespace AuditMerge20261111.Examples.Group13;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial, int expectedSerialNumber, string[] args, Action<DeviceChange> Apply, IYubiKey device)
{
var smartCardYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.SmartCard, cancellationToken: cancellationToken);
var fidoYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.HidFido, cancellationToken: cancellationToken);

await Task.CompletedTask;
}
}
