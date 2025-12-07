using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.SecurityDomain;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Integration coverage for establishing a Security Domain session using SCP03.
/// </summary>
public class SecurityDomainSessionTests
{
    /// <summary>
    ///     Validates that a Security Domain session can be created with SCP03 on devices
    ///     running firmware 5.3.0 or newer.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.3.0")]
    public async Task CreateAsync_WithScp03_Succeeds(YubiKeyTestState state)
    {
        await state.WithConnectionAsync(async connection =>
        {
            using var scpParams = Scp03KeyParams.Default;
            using var session = await SecurityDomainSession.CreateAsync(
                connection,
                scpKeyParams: scpParams,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(session);
        }, CancellationToken.None);
    }
}
