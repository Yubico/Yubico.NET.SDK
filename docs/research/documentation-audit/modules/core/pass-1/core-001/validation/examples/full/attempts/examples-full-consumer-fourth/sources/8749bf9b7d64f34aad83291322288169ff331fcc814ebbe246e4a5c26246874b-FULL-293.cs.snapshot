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
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.SecurityDomain;

namespace Audit20261111.DocExamples.Full;
internal static class Body293
{
internal static async Task CompileOnly(ISecurityDomainSession session, Microsoft.Extensions.Logging.ILogger logger, CancellationToken cancellationToken)
{
var keyInfo = await session.ListKeyInformationAsync(cancellationToken);
foreach (var entry in keyInfo)
{
    logger.LogDebug(
        "Key: KID={Kid:X2}, KVN={Kvn:X2}",
        entry.KeyReference.Kid,
        entry.KeyReference.Kvn);
}
}

}
