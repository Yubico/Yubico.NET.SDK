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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;

namespace Audit20261111.DocExamples.Full;
internal static class Body214
{
internal static async Task CompileOnly(RawSmartCardSession raw, ReadOnlyMemory<byte> applicationId, byte InsVerify, byte[] pinnedPin, CancellationToken ct)
{
// Non-sensitive command — no zeroing needed
var command = new ApduCommand
{
    Cla = 0x00,
    Ins = 0xA4,
    P1 = 0x04,
    P2 = 0x00,
    Data = applicationId
};
var responseData = await raw.TransmitAndReceiveAsync(command, cancellationToken: ct);

// Separate sensitive-command fragment: caller clears the source even on failure.
// ApduCommand is a readonly record struct: it stores a reference, not a clone.
try
{
    var verify = new ApduCommand(0x00, InsVerify, 0x00, 0x80, pinnedPin.AsMemory(0, 8));
    var response = await raw.TransmitAndReceiveAsync(verify, throwOnError: false, cancellationToken: ct);
    try { Console.WriteLine($"SW={response.SW:X4}"); }
    finally { response.Clear(); }
}
finally
{
    CryptographicOperations.ZeroMemory(pinnedPin);
}
}

}
