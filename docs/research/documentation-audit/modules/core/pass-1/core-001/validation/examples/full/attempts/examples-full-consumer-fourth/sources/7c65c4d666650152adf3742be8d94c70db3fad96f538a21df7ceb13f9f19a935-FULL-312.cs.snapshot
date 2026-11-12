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
using Yubico.YubiKit.Oath;

namespace Audit20261111.DocExamples.Full;
internal static class Body312
{
internal static async Task CompileOnly(IYubiKey device)
{
byte[] seed = "12345678901234567890"u8.ToArray(); // Example seed only; obtain real input securely.
try
{
    using var credentialData = new CredentialData
    {
        Name = "alice@example.com",
        OathType = OathType.Totp,
        HashAlgorithm = OathHashAlgorithm.Sha1,
        Secret = seed
    };
    await using var session = await device.CreateOathSessionAsync();
    await session.PutCredentialAsync(credentialData);
}
finally
{
    CryptographicOperations.ZeroMemory(seed);
}
}

}
