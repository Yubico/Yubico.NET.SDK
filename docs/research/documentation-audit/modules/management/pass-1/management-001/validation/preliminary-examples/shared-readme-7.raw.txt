[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
public async Task ReadMetadata_OverOtpHid(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, _) =>
    {
        Assert.Equal(ConnectionType.HidOtp, mgmt.ConnectionType);
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    }, preferredConnection: state.ConnectionType);
}
