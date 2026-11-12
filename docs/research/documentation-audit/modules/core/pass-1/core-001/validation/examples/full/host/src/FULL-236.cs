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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;
using Yubico.YubiKit.Core.Cryptography;
using static Yubico.YubiKit.Core.Cryptography.CryptographicDefinitions;

namespace Audit20261111.DocExamples.Full;
internal static class Body236
{
internal static void CompileOnly()
{
KeyInfo keyInfo = Keys.Get(KeyType.P256); // same instance as Keys.P256
Console.WriteLine(keyInfo.DisplayName); // P-256
Console.WriteLine(keyInfo.Curve?.FieldSizeBits); // 256
Console.WriteLine(keyInfo.Asn1?.AlgorithmOid); // 1.2.840.10045.2.1 (id-ecPublicKey)
Console.WriteLine(keyInfo.Asn1?.NamedCurveOid); // 1.2.840.10045.3.1.7

CurveInfo curveInfo = Curves.GetByOid("1.2.840.10045.3.1.7"); // named curves only
foreach (KeyInfo construction in Keys.FindByCurve(curveInfo))
    Console.WriteLine(construction.DisplayName); // a curve can have multiple concrete constructions
foreach (AlgorithmInfo algorithmInfo in Algorithms.FindForKey(keyInfo))
    Console.WriteLine(algorithmInfo.DisplayName); // multiple ECDSA choices, ECDH, ...

if (Keys.TryGet((KeyType)999, out KeyInfo? unknown))
    Console.WriteLine(unknown.DisplayName); // non-null on success
bool found = Curves.TryGetByOid("1.2.3.4.5", out _); // false
var matches = Algorithms.FindByOid(Oids.RsaPss); // generic + parameterized RSA-PSS
CurveInfo? rsaCurve = Keys.Rsa2048.Curve; // null
CurveInfo? aesCurve = Keys.Aes256.Curve; // null
Asn1KeyInfo? aesAsn1 = Keys.Aes256.Asn1; // null; CBC is an operation, not a bare key
}

}
