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
using Yubico.YubiKit.Core.Utilities;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Audit20261111.DocExamples.Full;
internal static class Body164
{
static async Task SendProtectedAsync(
    Yubico.YubiKit.Core.Abstractions.IYubiKey yubiKey,
    ScpKeyParameters scpParameters,
    FirmwareVersion firmwareVersion,
    ReadOnlyMemory<byte> applicationId,
    ApduCommand command,
    CancellationToken cancellationToken)
{
    await using ISmartCardConnection connection =
        await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
    await using (var plain = await RawSmartCardSession.CreateAsync(connection, cancellationToken))
    {
        plain.Configure(firmwareVersion);
        await plain.SelectAsync(applicationId, cancellationToken);
    }

    await using var secure = await RawSmartCardSession.CreateAsync(
        connection, scpParameters, firmwareVersion, cancellationToken: cancellationToken);
    ApduResponse response = await secure.TransmitAndReceiveAsync(
        command, throwOnError: false, cancellationToken: cancellationToken);
    try
    {
        Console.WriteLine($"SW={response.SW:X4}, {response.Data.Length} bytes");
    }
    finally
    {
        response.Clear();
    }
}
}
