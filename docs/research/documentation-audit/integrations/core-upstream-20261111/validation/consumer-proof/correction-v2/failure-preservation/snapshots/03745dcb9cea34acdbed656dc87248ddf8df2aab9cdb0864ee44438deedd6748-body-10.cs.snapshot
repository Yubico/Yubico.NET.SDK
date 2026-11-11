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
namespace AuditIntegration20261111.Example10;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken)
{
try
{
    var devices = await YubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);
}
catch (OperationCanceledException)
{
    // Scan was cancelled
}
catch (PlatformInteropException ex)
{
    // Platform API error (e.g., SmartCard service not running)
    Console.WriteLine($"Platform error: {ex.Message}");
}

}
}
