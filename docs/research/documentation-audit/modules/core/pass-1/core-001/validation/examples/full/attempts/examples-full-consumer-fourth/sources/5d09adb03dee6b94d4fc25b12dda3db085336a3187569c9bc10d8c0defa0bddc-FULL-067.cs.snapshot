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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;

namespace Audit20261111.DocExamples.Full;
internal static class Body67
{
internal static void CompileOnly(ReadOnlySpan<byte> inputData, ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, Span<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> ciphertext)
{
// Hashing
Span<byte> hash = stackalloc byte[32];
SHA256.HashData(inputData, hash);

// HMAC
Span<byte> hmac = stackalloc byte[32];
HMACSHA256.HashData(key, data, hmac);

// Random
Span<byte> random = stackalloc byte[16];
RandomNumberGenerator.Fill(random);

// AES
using var aes = Aes.Create();
aes.EncryptCbc(plaintext, iv, ciphertext, PaddingMode.PKCS7);
aes.DecryptCbc(ciphertext, iv, plaintext, PaddingMode.PKCS7);
}

}
