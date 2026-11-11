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
namespace AuditIntegration20261111.Example15;
public static class Example
{
public static async Task RunAsync(int expectedSerialNumber, CancellationToken cancellationToken)
{
// After closing the competing client; expectedSerialNumber is the serial you want to find.
IYubiKey? matchingDevice = await YubiKeyManager.FindFirstOrDefaultAsync(
    key => key.SerialNumber == expectedSerialNumber, cancellationToken: cancellationToken);

}
}
