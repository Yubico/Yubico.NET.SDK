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
using Yubico.YubiKit.OpenPgp;

namespace Audit20261111.DocExamples.Full;
internal static class Body355
{
static void ShowKeyMetadata()
{
    KeyInfo keyInfo = CryptographicDefinitions.Keys.Ed25519;
    OpenPgpKeyInfo? openPgpInfo = keyInfo.GetOpenPgpInfo();
    Console.WriteLine(openPgpInfo?.CurveOid); // 1.3.6.1.4.1.11591.15.1 (OpenPGP)
    Console.WriteLine(keyInfo.Asn1?.AlgorithmOid); // 1.3.101.112 (RFC 8410)
    EcAttributes attributes = EcAttributes.Create(KeyRef.Sig, KeyType.Ed25519); // EdDSA, 0x16
    Console.WriteLine(attributes.AlgorithmId);
}
}
