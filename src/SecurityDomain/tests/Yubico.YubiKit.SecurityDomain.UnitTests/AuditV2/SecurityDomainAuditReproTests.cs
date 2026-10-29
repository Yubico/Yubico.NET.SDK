using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Tests.Shared;

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

    /// <summary>GP Amd F v1.3 §7.9: removal is a zero-length allowlist TLV, sent only by the explicit clear.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_ClearAllowList_SendsZeroLengthAllowListTlv()
    {
        var connection = new RecordingSmartCardConnection([0x90, 0x00], [0x90, 0x00]);
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        int before = connection.TransmittedCommands.Count;

        await session.ClearAllowListAsync(new KeyReference(0x11, 0x01), TestContext.Current.CancellationToken);

        var storeData = Assert.Single(connection.TransmittedCommands.Skip(before));
        Assert.Equal(new byte[] { 0x00, 0xE2, 0x90, 0x00 }, storeData[..4]); // STORE DATA, last block, BER-TLV
        int lc = storeData[4];
        Assert.Equal(new byte[] { 0xA6, 0x04, 0x83, 0x02, 0x11, 0x01, 0x70, 0x00 }, storeData[5..(5 + lc)]);
    }
}