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
    private const ushort CardRecognitionDataObject = 0x0073;

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

    /// <summary>
    ///     Verifies that the Security Domain responds to GET DATA for the card recognition
    ///     data object when no secure channel is established.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.3.0")]
    public async Task GetDataAsync_CardRecognition_ReturnsPayload(YubiKeyTestState state)
    {
        await state.WithConnectionAsync(async connection =>
        {
            using var session = await SecurityDomainSession.CreateAsync(
                connection,
                cancellationToken: CancellationToken.None);

            var response = await session.GetDataAsync(
                CardRecognitionDataObject,
                cancellationToken: CancellationToken.None);

            Assert.False(response.IsEmpty);
        }, CancellationToken.None);
    }
}
