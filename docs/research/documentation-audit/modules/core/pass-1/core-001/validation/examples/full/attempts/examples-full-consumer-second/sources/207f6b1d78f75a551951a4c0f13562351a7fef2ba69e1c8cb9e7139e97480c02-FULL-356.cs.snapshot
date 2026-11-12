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
using Yubico.YubiKit.OpenPgp;

namespace Audit20261111.DocExamples.Full;
internal static class Body356
{
static async Task GenerateSigningKeyAsync(
    IYubiKey device, ReadOnlyMemory<byte> adminPin, CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    await session.VerifyAdminAsync(adminPin, cancellationToken);
    await session.GenerateKeyAsync(KeyRef.Sig,
        EcAttributes.Create(KeyRef.Sig, KeyType.P256), cancellationToken);
    Console.WriteLine((await session.ListKeyInformationAsync(cancellationToken))[KeyRef.Sig]);
}
}
