using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Piv;
namespace Audit20261111.DocExamples.External87;
internal static class Body
{
internal static async Task CompileOnly(IYubiKey device, CancellationToken cancellationToken)
{
await using var piv = await device.CreatePivSessionAsync(
    cancellationToken: cancellationToken);

// Evaluation only: the installed active key must be this 24-byte factory value.
byte[] managementKey = PivSession.DefaultManagementKey.ToArray();
try
{
    await piv.AuthenticateAsync(managementKey, cancellationToken);

    var publicKey = await piv.GenerateKeyAsync(
        Slot.Authentication,
        KeyType.P256,
        new KeyCreationOptions
        {
            PinPolicy = PinPolicy.Default,
            TouchPolicy = TouchPolicy.Default
        },
        cancellationToken);
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey);
}
}
}
