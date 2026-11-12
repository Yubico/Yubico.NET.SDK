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
internal static class Body282
{
internal static async Task CompileOnly(IYubiKey device, CancellationToken ct)
{
await using var connection = await device.ConnectAsync<ISmartCardConnection>(ct);
await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: ct);

var result = await DeviceInfoQuery.GetDeviceInfoAsync(session, ct);
if (result.Success && result.DeviceInfo.HasValue)
{
    var info = result.DeviceInfo.Value;
    Console.WriteLine($"Serial: {info.SerialNumber}");
    Console.WriteLine($"Firmware: {info.FirmwareVersion}");
}
}

}
