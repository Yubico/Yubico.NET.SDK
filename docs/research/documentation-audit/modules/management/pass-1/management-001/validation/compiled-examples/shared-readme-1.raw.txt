using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Xunit;

public class MyIntegrationTests
{
    [SkippableTheory]
    [WithYubiKey]
    public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(state.SerialNumber, info.SerialNumber);
        });
    }

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.7.0")]
    public async Task ModernFeatures_RequireFirmware570(YubiKeyTestState state)
    {
        using var scpKeyParams = Scp03KeyParameters.Default;
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var fresh = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(deviceInfo.SerialNumber, fresh.SerialNumber);
        }, scpKeyParams: scpKeyParams);
    }
}
