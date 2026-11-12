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
internal static class Body405
{
internal static async Task CompileOnly(IYubiKey device, CancellationToken ct)
{
await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);

var result = await SlotInfoQuery.GetAllSlotsInfoAsync(session, ct);
try
{
    if (result.Success)
    {
        foreach (var slot in result.Slots)
            Console.WriteLine($"{slot.Name}: Key={slot.HasKey}, Cert={slot.HasCertificate}");
    }
}
finally
{
    foreach (var slot in result.Slots)
        slot.Certificate?.Dispose();
}
}

}
