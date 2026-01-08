using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Integration coverage for establishing a Security Domain session using SCP03.
/// </summary>
public class SecurityDomainScp03Tests
{
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Validates that a Security Domain session can be created with SCP03 on devices
    ///     running firmware 5.7.2 or newer.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task CreateAsync_WithScp03_Succeeds(YubiKeyTestState state)
    {
        using var scpParams = Scp03KeyParameters.Default;

        await state.WithSecurityDomainSessionAsync(true,
            session =>
            {
                Assert.NotNull(session);
                return Task.CompletedTask;
            },
            scpKeyParams: scpParams, cancellationToken: CancellationTokenSource.Token);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task GetKeyInformationAsync_ReturnsDefaultScpKey(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                var keyInformation = await session.GetKeyInformationAsync(CancellationTokenSource.Token);

                Assert.Equal(state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3, keyInformation.Count);
                Assert.Equal(0xFF, keyInformation.Keys.First().Kvn);
            }, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task ResetAsync_ReinitializesSession(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                await session.ResetAsync(CancellationTokenSource.Token);

                var keyInformation = await session.GetKeyInformationAsync(CancellationTokenSource.Token);

                Assert.Equal(state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3, keyInformation.Count);
                Assert.Contains(keyInformation.Keys, keyRef => keyRef.Kvn == 0xFF);
            }, cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies that PutKeyAsync can import SCP03 static keys and that authentication
    ///     works with the new keys, while the old default keys no longer work.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task PutKeyAsync_WithStaticKeys_ImportsAndAuthenticates(YubiKeyTestState state)
    {
        // Custom key set (non-default) for testing
        byte[] keyBytes =
        [
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F
        ];

        using var newStaticKeys = new StaticKeys(keyBytes, keyBytes, keyBytes);
        var newKeyReference = new KeyReference(0x01, 0x01);
        var newKeyParams = new Scp03KeyParameters(newKeyReference, newStaticKeys);

        // Step 1: Authenticate with default keys and import new keys
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                await session.PutKeyAsync(newKeyReference, newStaticKeys, 0,
                    CancellationTokenSource.Token);
            }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);

        // Step 2: Verify new keys work
        await state.WithSecurityDomainSessionAsync(false,
            session =>
            {
                Assert.NotNull(session);
                return Task.CompletedTask;
            }, scpKeyParams: newKeyParams, cancellationToken: CancellationTokenSource.Token);

        // Step 3: Verify default keys no longer work
        await Assert.ThrowsAsync<ApduException>(async () =>
        {
            await state.WithSecurityDomainSessionAsync(false,
                session => Task.CompletedTask, scpKeyParams: Scp03KeyParameters.Default,
                cancellationToken: CancellationTokenSource.Token);
        });
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task GetCardRecognitionData_Succeeds(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                var result = await session.GetCardRecognitionDataAsync();
                Assert.True(result.Length > 0);
            }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task GetData_Succeeds(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                var result = await session.GetDataAsync(0x66); // TagCardData
                Assert.True(result.Length > 0);
            }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    // TODO troubleshoot: Check if response is different each time? Then ENC/MAC/DEC error
    public async Task GetSupportedCaIdentifiers_Succeeds(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                var result = await session.GetSupportedCaIdentifiersAsync(true, true);
                Assert.True(result.Count > 0);
            },
            scpKeyParams: Scp03KeyParameters.Default,
            configuration: new ProtocolConfiguration { ForceShortApdus = true },
            cancellationToken: CancellationTokenSource.Token);
}