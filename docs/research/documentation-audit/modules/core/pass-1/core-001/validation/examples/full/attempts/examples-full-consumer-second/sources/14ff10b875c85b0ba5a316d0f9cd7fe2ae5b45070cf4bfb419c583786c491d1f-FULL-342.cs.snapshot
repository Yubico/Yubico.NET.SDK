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
using Yubico.YubiKit.YubiHsm;

namespace Audit20261111.DocExamples.Full;
internal static class Body342
{
static async Task EnrollSymmetricAsync(
    IYubiKey device,
    ReadOnlyMemory<byte> managementKey,
    ReadOnlyMemory<byte> credentialPassword,
    string label,
    ushort hsmAuthenticationKeyId,
    Func<ushort, string, ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, CancellationToken, Task> provisionHsmAsync,
    CancellationToken cancellationToken)
{
    byte[] keyMaterial = RandomNumberGenerator.GetBytes(32);
    try
    {
        await provisionHsmAsync(hsmAuthenticationKeyId, label,
            keyMaterial.AsMemory(0, 16), keyMaterial.AsMemory(16, 16), cancellationToken);
        await using var session = await device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await session.PutCredentialSymmetricAsync(managementKey, label,
            keyMaterial.AsMemory(0, 16), keyMaterial.AsMemory(16, 16), credentialPassword,
            touchRequired: true, cancellationToken: cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(keyMaterial);
    }
}
}
