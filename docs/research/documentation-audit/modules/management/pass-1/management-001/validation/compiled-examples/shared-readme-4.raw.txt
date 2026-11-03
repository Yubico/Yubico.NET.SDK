[SkippableTheory]
[WithYubiKey]
public async Task GetDeviceInfo_AllDevices_ReturnsValidData(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();

        Assert.Equal(state.SerialNumber, info.SerialNumber);
        Assert.Equal(cachedInfo.FirmwareVersion, info.FirmwareVersion);
    });
}
