using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core;

namespace Yubico.YubiKit.Management.UnitTests.AuditV2;

public sealed class ManagementAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_BuiltConfigurationIsSnapshotOfBuilder()
    {
        var builder = DeviceConfig.CreateBuilder().WithCapabilities(Transport.Usb, (int)DeviceCapabilities.Oath);
        DeviceConfig built = builder.Build();

        builder.WithCapabilities(Transport.Usb, 0);

        Assert.Equal((int)DeviceCapabilities.Oath, built.GetEnabledCapabilities(Transport.Usb));
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }
}
