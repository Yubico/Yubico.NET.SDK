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
using Yubico.YubiKit.YubiOtp;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;

namespace Audit20261111.DocExamples.Full;
internal static class Body331
{
internal static async Task CompileOnly(IYubiKey device)
{
byte[] challenge = Encoding.UTF8.GetBytes("some-challenge");
ReadOnlyMemory<byte> response = default;
try
{
    response = await device.CalculateHmacSha1Async(
        Slot.Two, challenge,
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    // Consume the response with your verifier here; do not log it.
}
finally
{
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
}
}

}
