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
internal static class Body382
{
internal static async Task CompileOnly(IYubiKey device)
{
await using var session = await device.CreatePivSessionAsync();
using var certificate = await session.GetCertificateAsync(Slot.KeyManagement)
    ?? throw new InvalidOperationException("Provision a matching RSA certificate first.");
using var rsa = certificate.GetRSAPublicKey()
    ?? throw new InvalidOperationException("The certificate must contain an RSA public key.");
byte[] expected = [];
Memory<byte> plaintext = Memory<byte>.Empty;
byte[] pin = Encoding.UTF8.GetBytes("123456");
try
{
    expected = Encoding.UTF8.GetBytes("known plaintext");
    byte[] ciphertext = rsa.Encrypt(expected, RSAEncryptionPadding.OaepSHA256);
    await session.VerifyPinAsync(pin);
    plaintext = await session.DecryptAsync(Slot.KeyManagement, ciphertext, RSAEncryptionPadding.OaepSHA256);
    if (!CryptographicOperations.FixedTimeEquals(expected, plaintext.Span))
        throw new CryptographicException("Decryption returned unexpected plaintext.");
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    CryptographicOperations.ZeroMemory(expected);
    CryptographicOperations.ZeroMemory(plaintext.Span);
}
}

}
