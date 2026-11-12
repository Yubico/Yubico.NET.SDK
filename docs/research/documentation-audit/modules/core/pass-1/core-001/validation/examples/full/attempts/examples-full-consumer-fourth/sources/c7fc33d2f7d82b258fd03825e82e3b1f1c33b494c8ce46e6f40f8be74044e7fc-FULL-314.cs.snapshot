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
internal static class Body314
{
internal static async Task CompileOnly(IYubiKey device)
{
byte[] password = Encoding.UTF8.GetBytes("correct horse battery staple");
byte[] accessKey = [];
try
{
    await using var session = await device.CreateOathSessionAsync();
    accessKey = session.DeriveKey(password);
    await (session.IsLocked
        ? session.ValidateAsync(accessKey)
        : session.SetKeyAsync(accessKey));
}
finally
{
    CryptographicOperations.ZeroMemory(password);
    CryptographicOperations.ZeroMemory(accessKey);
}
}

}
