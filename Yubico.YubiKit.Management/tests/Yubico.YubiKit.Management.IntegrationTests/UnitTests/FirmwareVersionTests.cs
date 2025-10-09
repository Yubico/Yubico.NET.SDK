using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management.IntegrationTests.UnitTests;

public class FirmwareVersionTests
{
    [Fact]
    public void LessThan()
    {
        Assert.True(new FirmwareVersion(5, 0, 0).IsLessThan(6, 0, 0));
        Assert.True(new FirmwareVersion(5, 9, 9).IsLessThan(6, 0, 0));

        Assert.False(new FirmwareVersion(6, 0, 0).IsLessThan(6, 0, 0));
        Assert.False(new FirmwareVersion(6, 9, 0).IsLessThan(6, 0, 0));
    }

    [Fact]
    public void IsAtleast()
    {
        Assert.False(new FirmwareVersion(5, 0, 0).IsAtLeast(6, 0, 0));
        Assert.False(new FirmwareVersion(5, 9, 9).IsAtLeast(6, 0, 0));

        Assert.True(new FirmwareVersion(6, 0, 0).IsAtLeast(6, 0, 0));
        Assert.True(new FirmwareVersion(6, 9, 0).IsAtLeast(6, 0, 0));
    }
}