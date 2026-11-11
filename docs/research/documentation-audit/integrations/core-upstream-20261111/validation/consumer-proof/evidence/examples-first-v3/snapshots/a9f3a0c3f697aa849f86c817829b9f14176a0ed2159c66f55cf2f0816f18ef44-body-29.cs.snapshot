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
namespace AuditIntegration20261111.Example29;
public static class Example
{
public static async Task RunAsync(IPcscDevice reader, CancellationToken cancellationToken)
{
// Explicitly accept shared access for this factory only:
// Coordinate discovery/monitoring and all other connections to this key before opening directly.
var factory = new SmartCardConnectionFactory(SmartCardSharing.Shared);
await using var connection = await factory.CreateAsync(reader, cancellationToken);

}
}
