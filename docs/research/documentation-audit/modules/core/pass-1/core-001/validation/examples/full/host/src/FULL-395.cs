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
internal static class Body395
{
internal static async Task CompileOnly(IYubiKey device, IUserPresencePrompt userPresencePrompt, ReadOnlyMemory<byte> pin, ReadOnlyMemory<byte> encryptedBytes, CancellationToken ct)
{
var options = new SessionCreationOptions { UserPresencePrompt = userPresencePrompt };
await using var session = await device.CreatePivSessionAsync(options, ct);
await session.VerifyPinAsync(pin, ct);

var result = await Decryption.DecryptDataAsync(
    session,
    Slot.KeyManagement,
    encryptedBytes,
    cancellationToken: ct);

if (result.Success)
{
    var plaintext = result.DecryptedData;
}
}

}
