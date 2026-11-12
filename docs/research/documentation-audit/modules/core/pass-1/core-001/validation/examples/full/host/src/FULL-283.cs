using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;

namespace Audit20261111.DocExamples.Full;
internal static class Body283
{
internal static async Task CompileOnly(IManagementSession session, CancellationToken ct)
{
// Prerequisite: caller confirmed Bio MPE and authorized reset; session uses SmartCard.
// Metadata below supplies the helper's firmware check, not a product-family check.
var infoResult = await DeviceInfoQuery.GetDeviceInfoAsync(session, ct);
if (!infoResult.Success || !infoResult.DeviceInfo.HasValue)
{
    Console.WriteLine("Failed to get device info");
    return;
}

// Request reset; the helper returns failure results for operation exceptions.
var resetResult = await DeviceReset.ResetDeviceAsync(
    session, 
    infoResult.DeviceInfo.Value, 
    ct);

if (resetResult.Success)
{
    Console.WriteLine("Device reset successfully");
}
else
{
    Console.WriteLine($"Reset failed: {resetResult.ErrorMessage}");
}
}

}
