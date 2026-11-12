using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Audit20261111.DocExamples.Full;
internal static class Body415
{
static async Task SelectPivSharedAsync(IPcscDevice reader, CancellationToken cancellationToken)
{
    // Coordinate discovery/monitoring and other connections before this direct open.
    var factory = new SmartCardConnectionFactory(SmartCardSharing.Shared);
    await using ISmartCardConnection connection = await factory.CreateAsync(reader, cancellationToken);
    await using RawSmartCardSession session =
        await RawSmartCardSession.CreateAsync(connection, cancellationToken);
    await session.SelectAsync(ApplicationIds.Piv, cancellationToken);
}
}
