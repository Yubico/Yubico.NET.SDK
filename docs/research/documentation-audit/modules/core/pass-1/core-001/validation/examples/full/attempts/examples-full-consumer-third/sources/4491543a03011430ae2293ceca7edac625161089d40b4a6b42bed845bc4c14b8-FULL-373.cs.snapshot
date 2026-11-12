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
internal static class Body373
{
static async Task AuthenticateWithStoreAsync(
    IPivSession session,
    Action<Memory<byte>> loadManagementKey,
    CancellationToken cancellationToken)
{
    // This example store contract supplies a 24-byte Triple-DES or AES-192 active key.
    byte[] managementKey = ArrayPool<byte>.Shared.Rent(24);
    try
    {
        loadManagementKey(managementKey.AsMemory(0, 24));
        await session.AuthenticateAsync(managementKey.AsMemory(0, 24), cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(managementKey.AsSpan(0, 24));
        ArrayPool<byte>.Shared.Return(managementKey, clearArray: true);
    }
}
}
