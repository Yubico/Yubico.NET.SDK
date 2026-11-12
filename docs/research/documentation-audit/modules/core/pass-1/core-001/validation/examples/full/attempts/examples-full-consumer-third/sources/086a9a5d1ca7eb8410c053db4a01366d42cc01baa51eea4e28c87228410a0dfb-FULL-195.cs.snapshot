using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Security.Cryptography;

namespace Audit20261111.DocExamples.Full;
internal static class Body195
{
internal static void CompileOnly(byte[] privateKeyBytes, Action<ReadOnlySpan<byte>> UseSensitiveEncoding)
{
Memory<byte> innerEncoding = Memory<byte>.Empty;
Memory<byte> outerEncoding = Memory<byte>.Empty;

try
{
    innerEncoding = TlvHelper.EncodeAndDisposeList(
        new Tlv(0x81, privateKeyBytes));
    outerEncoding = TlvHelper.EncodeAndDisposeList(
        new Tlv(0x7F49, innerEncoding));

    UseSensitiveEncoding(outerEncoding.Span);
}
finally
{
    CryptographicOperations.ZeroMemory(outerEncoding.Span);
    CryptographicOperations.ZeroMemory(innerEncoding.Span);
    CryptographicOperations.ZeroMemory(privateKeyBytes);
}
}

}
