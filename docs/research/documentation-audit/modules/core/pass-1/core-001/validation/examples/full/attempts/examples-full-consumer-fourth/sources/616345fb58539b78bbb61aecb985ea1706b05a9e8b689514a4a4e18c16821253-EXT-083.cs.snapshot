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
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;
namespace Audit20261111.DocExamples.External83;
internal static class Body
{
internal static async Task CompileOnly(CancellationToken cancellationToken)
{
var device = await YubiKeyManager.FindFirstAsync(cancellationToken: cancellationToken);
await using var session = await device.CreateManagementSessionAsync(
    cancellationToken: cancellationToken);

var info = await session.GetDeviceInfoAsync(cancellationToken);
Console.WriteLine(info.SerialNumber);
Console.WriteLine(info.VersionName);
}
}
