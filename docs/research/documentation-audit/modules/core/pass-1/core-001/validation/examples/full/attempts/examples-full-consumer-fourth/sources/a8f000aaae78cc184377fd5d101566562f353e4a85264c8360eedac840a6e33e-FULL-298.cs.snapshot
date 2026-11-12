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
using Yubico.YubiKit.SecurityDomain;

namespace Audit20261111.DocExamples.Full;
internal static class Body298
{
internal static async Task CompileOnly(ISecurityDomainSession session)
{
byte[] keyMaterial = RandomNumberGenerator.GetBytes(48);
try
{
    // Escrow keyMaterial before you send it: the device will not give it back.
    using var newKeys = new StaticKeys(keyMaterial.AsSpan(0, 16), keyMaterial.AsSpan(16, 16), keyMaterial.AsSpan(32, 16));
    await session.PutKeyAsync(new KeyReference(ScpKid.SCP03, Kvn: 0x02), newKeys, replaceKvn: 0);
}
finally
{
    CryptographicOperations.ZeroMemory(keyMaterial);
}
}

}
