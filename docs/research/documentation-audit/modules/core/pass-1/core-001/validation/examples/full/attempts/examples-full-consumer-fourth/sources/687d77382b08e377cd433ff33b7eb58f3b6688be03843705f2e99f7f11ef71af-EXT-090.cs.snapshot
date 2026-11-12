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
using Yubico.YubiKit.Ctap2;
namespace Audit20261111.DocExamples.External90;
internal static class Body
{
internal static async Task CompileOnly(IYubiKey device, CancellationToken cancellationToken)
{
await using var fido = await device.CreateCtap2SessionAsync(
    cancellationToken: cancellationToken);

var info = await fido.GetInfoAsync(cancellationToken);
}
}
