using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2;
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;

namespace AuditCt001.Prepared.hardware_info;

public sealed class Example
{
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
public async Task GetInfoAsync_Returns_Aaguid(YubiKeyTestState state)
{
    await using var session = await state.Device.CreateCtap2SessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido });
    var info = await session.GetInfoAsync();
    Assert.Equal(16, info.Aaguid.Length);
}
}
