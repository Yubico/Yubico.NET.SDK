using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Oath;
namespace Audit20261111.DocExamples.External99;
internal static class Body
{
static async Task ReadCodesAsync(
    IYubiKey device,
    byte[] ownedPassword,
    CancellationToken cancellationToken)
{
    try
    {
        await using var oath = await device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        var codes = await oath.AuthenticateAndRetryAsync(
            ct => oath.CalculateAllAsync(cancellationToken: ct),
            _ => Task.FromResult<ReadOnlyMemory<byte>>(ownedPassword),
            cancellationToken);
        Console.WriteLine($"Returned {codes.Count} credential entries.");
    }
    finally
    {
        CryptographicOperations.ZeroMemory(ownedPassword);
    }
}
}
