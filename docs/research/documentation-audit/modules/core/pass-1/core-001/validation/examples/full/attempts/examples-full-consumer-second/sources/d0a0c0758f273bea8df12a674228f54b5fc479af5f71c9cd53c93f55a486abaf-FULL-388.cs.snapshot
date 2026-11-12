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
internal static class Body388
{
internal static async Task CompileOnly(IPivSession session, CancellationToken ct)
{
var result = await Attestation.GetIntermediateCertificateAsync(session, ct);
// This helper returns F9 in AttestationCertificate; IntermediateCertificate remains null.
using var f9Cert = result.AttestationCertificate;
using var additionalCert = result.IntermediateCertificate;
if (result.Success && f9Cert is not null)
{
    Console.WriteLine(f9Cert.Subject);
}
}

}
