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
internal static class Body381
{
internal static async Task CompileOnly(IYubiKey device)
{
await using var session = await device.CreatePivSessionAsync();

byte[] digest = [];
Memory<byte> signature = Memory<byte>.Empty;
byte[] pin = Encoding.UTF8.GetBytes("123456");
try
{
    digest = SHA256.HashData(Encoding.UTF8.GetBytes("message to sign"));
    await session.VerifyPinAsync(pin);
    var metadata = await session.GetSlotMetadataAsync(Slot.Authentication)
        ?? throw new InvalidOperationException("Generate a P-256 key first.");
    using var verifier = metadata.GetECDsaPublicKey();
    signature = await session.SignOrDecryptAsync(Slot.Authentication, KeyType.P256, digest);
    if (!verifier.VerifyHash(digest, signature.Span, DSASignatureFormat.Rfc3279DerSequence))
        throw new CryptographicException("The slot signature failed host verification.");
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    CryptographicOperations.ZeroMemory(digest);
    CryptographicOperations.ZeroMemory(signature.Span);
}
}

}
