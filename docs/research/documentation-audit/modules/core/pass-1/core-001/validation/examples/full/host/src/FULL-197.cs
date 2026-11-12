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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Piv;

namespace Audit20261111.DocExamples.Full;
internal static class Body197
{
static async Task ReadAndVerifyPinAsync(
    IPivSession session, ISecureCredentialReader reader, CancellationToken cancellationToken)
{
    using var credential = reader.ReadCredential(
        CredentialReaderOptions.ForPin(), cancellationToken);
    if (credential is null)
        return;

    await session.VerifyPinAsync(credential.Memory, cancellationToken);
}
}
