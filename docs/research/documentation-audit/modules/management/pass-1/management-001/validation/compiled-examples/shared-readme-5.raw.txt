using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;

[SkippableTheory]
[WithYubiKey(MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
public async Task Scp03_AfterReset_ReturnsKeyInfo(YubiKeyTestState state)
{
    using var scpKeyParams = Scp03KeyParameters.Default;
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keyInfo = await session.ListKeyInformationAsync();
            Assert.NotEmpty(keyInfo);
        },
        scpKeyParams: scpKeyParams);
}
