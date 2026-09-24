namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

public class PublicSyncBoundaryRegistryTests
{
    [Fact]
    public void Supported_synchronous_entries_resolve_to_public_contracts_and_scoped_owners()
    {
        var rows = PublicSyncBoundaryRegistry.Rows;
        Assert.Equal(16, rows.Length);
        Assert.Empty(PublicSyncBoundaryRegistry.Validate(rows));
    }

    [Fact]
    public void Missing_public_entry_or_wrong_public_symbol_fails_closed()
    {
        var rows = PublicSyncBoundaryRegistry.Rows;
        Assert.Contains(PublicSyncBoundaryRegistry.Validate(rows.Skip(1)), error => error.StartsWith("missing entry:", StringComparison.Ordinal));
        Assert.Contains(PublicSyncBoundaryRegistry.Validate(rows.Select((row, index) => index == 0
            ? row with { PublicSymbol = "NotARealMethod" } : row)), error => error.StartsWith("wrong public symbol:", StringComparison.Ordinal));
        Assert.Contains(PublicSyncBoundaryRegistry.Validate(rows.Select((row, index) => index == 0
            ? row with { Owner = rows[1].Owner } : row)), error => error.StartsWith("wrong owner:", StringComparison.Ordinal));
        Assert.Contains(PublicSyncBoundaryRegistry.Validate([.. rows, rows[0]]), error => error.StartsWith("missing entry:", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("TODO")]
    [InlineData("unverified evidence")]
    [InlineData("FakeClass.FakeTest")]
    public void Unsupported_bare_or_stale_evidence_fails_closed(string evidence)
    {
        var rows = PublicSyncBoundaryRegistry.Rows;
        Assert.Contains($"stale evidence: {rows[0].Entry}", PublicSyncBoundaryRegistry.Validate(
            rows.Select((row, index) => index == 0 ? row with { Evidence = evidence } : row)));
    }
}
