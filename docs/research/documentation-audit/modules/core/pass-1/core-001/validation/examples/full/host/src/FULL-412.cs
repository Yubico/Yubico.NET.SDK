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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Audit20261111.DocExamples.Full;
internal static class Body412
{
    // The caller supplies an open connection, an applet-specific header, and owned payload bytes.
    // This helper borrows the connection and consumes the payload, clearing it even if creation fails.
    static async Task<short> SendAndClearAsync(
        Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection connection,
        byte cla, byte ins, byte p1, byte p2, byte[] payload, CancellationToken ct)
    {
        try
        {
            await using var session = await Yubico.YubiKit.Core.Sessions.RawSmartCardSession.CreateAsync(connection, ct);
            var command = new ApduCommand(cla, ins, p1, p2, payload);
            var response = await session.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken: ct);
            try
            {
                return response.SW; // Inspect status before clearing the response owner.
            }
            finally
            {
                response.Clear();
            }
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(payload);
        }
    }
    
}
