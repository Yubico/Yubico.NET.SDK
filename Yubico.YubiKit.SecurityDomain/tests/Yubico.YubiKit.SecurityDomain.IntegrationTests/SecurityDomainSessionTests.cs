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
    private const byte DefaultScp03Kid = 0x01;

    /// <summary>
    ///     Validates that a Security Domain session can be created with SCP03 on devices
    ///     running firmware 5.3.0 or newer.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task CreateAsync_WithScp03_Succeeds(YubiKeyTestState state)
    {
        using var scpParams = Scp03KeyParams.Default;

        await state.WithSecurityDomainSessionAsync(
            session =>
            {
                Assert.NotNull(session);
                return Task.CompletedTask;
            },
            scpParams,
            CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that the Security Domain responds to GET DATA for the card recognition
    ///     data object when no secure channel is established.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task GetDataAsync_CardRecognition_ReturnsPayload(YubiKeyTestState state)
    {
        await state.WithSecurityDomainSessionAsync(async session =>
        {
            var response = await session.GetDataAsync(
                CardRecognitionDataObject,
                cancellationToken: CancellationToken.None);

            Assert.False(response.IsEmpty);
        }, cancellationToken: CancellationToken.None);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task GetKeyInformationAsync_ReturnsDefaultScpKey(YubiKeyTestState state)
    {
        await state.WithSecurityDomainSessionAsync(async session =>
        {
            var keyInformation = await session.GetKeyInformationAsync(CancellationToken.None);

            Assert.NotEmpty(keyInformation);
            Assert.Contains(keyInformation.Keys, keyRef => keyRef.Kid == DefaultScp03Kid);
        }, cancellationToken: CancellationToken.None);
    }
}
