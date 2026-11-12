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
internal static class Body409
{
internal static async Task CompileOnly(IPivSession session)
{
// Requires an open session, firmware 5.3+ and a provisioned RSA slot.
var metadata = await session.GetSlotMetadataAsync(Slot.Authentication)
    ?? throw new InvalidOperationException("Provision an RSA key first.");
using var rsa = metadata.GetRsaPublicKey();
byte[] plaintext = "example"u8.ToArray();
try
{
    byte[] ciphertext = rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA256);
}
finally
{
    CryptographicOperations.ZeroMemory(plaintext);
}
}

}
