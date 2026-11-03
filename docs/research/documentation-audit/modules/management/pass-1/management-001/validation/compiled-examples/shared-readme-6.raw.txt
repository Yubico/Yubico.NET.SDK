public sealed class ProductionKeysOnly : IYubiKeyFilter
{
    public bool Matches(YubiKeyTestState device) => device.SerialNumber > 10_000_000;
    public string GetDescription() => "Production keys";
}

[SkippableTheory]
[WithYubiKey(CustomFilter = typeof(ProductionKeysOnly))]
public async Task RunsOnlyOnProductionKeyRange(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, info) =>
    {
        var freshInfo = await mgmt.GetDeviceInfoAsync();
        Assert.True(freshInfo.SerialNumber > 10_000_000);
        Assert.Equal(info.SerialNumber, freshInfo.SerialNumber);
    });
}
