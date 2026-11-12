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
using Yubico.YubiKit.Oath;
namespace Audit20261111.DocExamples.External92;
internal static class Body
{
internal static async Task CompileOnly(IYubiKey device, CancellationToken cancellationToken)
{
await using var oath = await device.CreateOathSessionAsync(
    cancellationToken: cancellationToken);

using var credentialData = CredentialData.ParseUri(
    "otpauth://totp/Yubico:alice@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Yubico&algorithm=SHA1&digits=6&period=30");

await oath.PutCredentialAsync(credentialData, cancellationToken: cancellationToken);
var codes = await oath.CalculateAllAsync(cancellationToken: cancellationToken);
}
}
