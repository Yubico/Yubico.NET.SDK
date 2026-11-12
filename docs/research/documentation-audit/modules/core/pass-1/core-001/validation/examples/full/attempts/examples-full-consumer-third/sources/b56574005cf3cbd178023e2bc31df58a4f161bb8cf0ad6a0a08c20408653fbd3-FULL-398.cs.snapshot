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
internal static class Body398
{
internal static async Task CompileOnly(IYubiKey device, CancellationToken ct)
{
await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);

var pin = Encoding.UTF8.GetBytes("123456");
try
{
    var result = await PinManagement.VerifyPinAsync(session, pin, ct);
    if (result.Success)
    {
        Console.WriteLine("PIN verified");
    }
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
}

}
