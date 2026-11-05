using System.Threading.Tasks;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Piv;
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
namespace AuditPiv001.Examples.V4.Annotations;

public sealed class MetadataTestAnnotation
{
[SkippableTheory]
[WithYubiKey(Capability = DeviceCapabilities.Piv, MinFirmware = "5.3.0")]
public async Task GetMetadata_ReadOnly_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    var metadata = await session.GetPinMetadataAsync();
    Assert.InRange(metadata.RetriesRemaining, 0, metadata.TotalRetries);
}

}

