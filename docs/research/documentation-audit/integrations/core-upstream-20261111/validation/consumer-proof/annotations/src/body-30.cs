using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
namespace AuditIntegration20261111.Example30;
using Xunit;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
public sealed class ScpExample
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
