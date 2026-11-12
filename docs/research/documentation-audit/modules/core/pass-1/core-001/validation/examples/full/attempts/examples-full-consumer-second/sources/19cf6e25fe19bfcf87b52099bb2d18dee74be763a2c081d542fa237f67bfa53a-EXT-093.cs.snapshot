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
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
namespace Audit20261111.DocExamples.External93;
internal static class Body
{
internal static async Task CompileOnly(Yubico.YubiKit.Oath.CredentialData credentialData)
{
byte[] copy = credentialData.Secret.ToArray();
try
{
    // Pass copy to the array-only consumer; await completion before clearing it.
}
finally
{
    System.Security.Cryptography.CryptographicOperations.ZeroMemory(copy);
}
}
}
