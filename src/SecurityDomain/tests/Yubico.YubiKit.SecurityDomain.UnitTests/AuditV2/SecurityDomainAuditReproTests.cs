using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace Yubico.YubiKit.SecurityDomain.UnitTests.AuditV2;

public sealed class SecurityDomainAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_EmptyAllowListDoesNotIssueRestrictionRemovingStoreData()
    {
        var connection = new RecordingSmartCardConnection([0x90, 0x00], [0x90, 0x00]);
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        int before = connection.TransmittedCommands.Count;

        await Assert.ThrowsAsync<ArgumentException>(() => session.StoreAllowListAsync(
            new KeyReference(0x11, 0x01), [], TestContext.Current.CancellationToken));

        Assert.Equal(before, connection.TransmittedCommands.Count);
    }
}
