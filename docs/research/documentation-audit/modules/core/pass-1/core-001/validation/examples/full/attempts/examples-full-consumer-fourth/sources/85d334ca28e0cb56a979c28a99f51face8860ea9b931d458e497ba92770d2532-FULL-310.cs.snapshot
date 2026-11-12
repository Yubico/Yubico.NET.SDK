using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Oath;

namespace Audit20261111.DocExamples.Full;
internal static class Body310
{
internal static async Task CompileOnly()
{
IYubiKey device = await YubiKeyManager.FindFirstAsync(
    predicate: key => key.SupportsConnection(ConnectionType.SmartCard));
await using var session = await device.CreateOathSessionAsync();
IReadOnlyList<Credential> credentials = await session.ListCredentialsAsync();
Console.WriteLine(string.Join('\n', credentials.Select(c => $"{c.Issuer}:{c.Name} ({c.OathType})")));
}

}
