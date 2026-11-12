using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
namespace Audit20261111.DocExamples.Test61;
public sealed class DocumentationBody61
{
// ✅ GOOD - Tests actual SCP handshake with real hardware
[Fact]
public async Task CreateSession_WithSCP03_AuthenticatesAndCommunicates()
{
    var device = await YubiKeyManager.FindFirstAsync(cancellationToken: TestContext.Current.CancellationToken);
    using var scpKeyParams = Scp03KeyParameters.Default;
    await using var session = await device.CreateManagementSessionAsync(
        new SessionCreationOptions { ScpKeyParameters = scpKeyParams },
        cancellationToken: TestContext.Current.CancellationToken);

    var deviceInfo = await session.GetDeviceInfoAsync(TestContext.Current.CancellationToken);
    Assert.Equal(device.SerialNumber, deviceInfo.SerialNumber);
}
}
