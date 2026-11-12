using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Sessions;
using Xunit;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Audit20261111.DocExamples.Test142;
public sealed class DocumentationBody142
{
static async Task AssertSmartCardConnectionAsync(IYubiKey device, CancellationToken cancellationToken)
{
    await using ISmartCardConnection connection =
        await device.ConnectAsync<ISmartCardConnection>(cancellationToken);

    Assert.NotNull(connection);
    Assert.Equal(ConnectionType.SmartCard, connection.Type);
}
}
