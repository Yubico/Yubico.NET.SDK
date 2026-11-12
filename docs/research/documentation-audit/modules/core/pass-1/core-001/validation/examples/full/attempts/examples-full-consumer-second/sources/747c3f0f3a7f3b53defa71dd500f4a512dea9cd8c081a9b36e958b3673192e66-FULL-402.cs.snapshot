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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Audit20261111.DocExamples.Full;
internal static class Body402
{
internal static async Task CompileOnly(IPivSession session, ReadOnlyMemory<byte> currentKey, ReadOnlyMemory<byte> newKey, CancellationToken ct)
{
// currentKey and newKey are caller-owned buffers; securely back up newKey before this task.
// This example installs a 24-byte Triple-DES key on a compatible device.
await session.AuthenticateAsync(currentKey, ct);
var result = await PinManagement.ChangeManagementKeyAsync(
    session, newKey, KeyType.TripleDes, requireTouch: false, cancellationToken: ct);
// Keep the backup through fresh-session authentication; the caller clears both buffers in finally.
}

}
