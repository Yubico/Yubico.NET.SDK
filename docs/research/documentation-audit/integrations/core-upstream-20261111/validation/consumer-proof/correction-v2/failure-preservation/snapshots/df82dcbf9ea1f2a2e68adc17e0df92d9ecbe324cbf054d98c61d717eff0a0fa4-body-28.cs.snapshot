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
namespace AuditIntegration20261111.Example28;
public static class Example
{
public static async Task RunAsync()
{
// Explicit compatibility opt-in, before any discovery or monitoring:
SmartCardConnectionFactory.DefaultSharing = SmartCardSharing.ExclusiveWithSharedFallback;
var devices = await YubiKeyManager.FindAllAsync();

}
}
