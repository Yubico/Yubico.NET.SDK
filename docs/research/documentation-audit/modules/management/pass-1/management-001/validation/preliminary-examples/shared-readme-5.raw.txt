using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;

[SkippableTheory]
[WithYubiKey(MinFirmware = "5.7.2", Capability = DeviceCapabilities.SecurityDomain)]
public async Task Scp11_ModernDevice_ReturnsKeyInfo(YubiKeyTestState state)
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
