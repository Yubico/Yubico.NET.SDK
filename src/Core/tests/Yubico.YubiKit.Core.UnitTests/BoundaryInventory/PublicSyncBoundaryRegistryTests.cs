namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

public class PublicSyncBoundaryRegistryTests
{
    [Fact]
    public void Supported_synchronous_entries_resolve_to_public_contracts_and_scoped_owners()
    {
        var rows = PublicSyncBoundaryRegistry.Rows;
        // Upstream's 15 entries plus eight Linux and eight Windows entries.
        Assert.Equal(31, rows.Length);
        Assert.Empty(PublicSyncBoundaryRegistry.Validate(rows));
    }

    [Theory]
    [InlineData("windows input open")]
    [InlineData("windows input get")]
    [InlineData("windows input set")]
    [InlineData("windows input dispose")]
    [InlineData("windows feature open")]
    [InlineData("windows feature get")]
    [InlineData("windows feature set")]
    [InlineData("windows feature dispose")]
    [InlineData("linux input open")]
    [InlineData("linux input get")]
    [InlineData("linux input set")]
    [InlineData("linux input dispose")]
    [InlineData("linux feature open")]
    [InlineData("linux feature get")]
    [InlineData("linux feature set")]
    [InlineData("linux feature dispose")]
    public void Removing_Linux_or_Windows_public_reachability_fails_closed(string entry)
    {
        var rows = PublicSyncBoundaryRegistry.Rows;
        Assert.Single(rows, row => row.Entry == entry);
        Assert.Contains($"missing entry: {entry} (found 0)",
            PublicSyncBoundaryRegistry.Validate(rows.Where(row => row.Entry != entry)));
    }

    [Theory]
    [InlineData("windows input open")]
    [InlineData("windows input get")]
    [InlineData("windows input set")]
    [InlineData("windows input dispose")]
    [InlineData("windows feature open")]
    [InlineData("windows feature get")]
    [InlineData("windows feature set")]
    [InlineData("windows feature dispose")]
    public void Windows_documented_gap_cannot_replace_behavioral_evidence(string entry)
    {
        var rows = PublicSyncBoundaryRegistry.Rows;
        Assert.Single(rows, row => row.Entry == entry);
        Assert.Contains($"stale evidence: {entry}", PublicSyncBoundaryRegistry.Validate(
            rows.Select(row => row.Entry == entry ? row with { Evidence = "No held-native Windows dispose" } : row)));
    }

    [Fact]
    public void Non_Windows_documented_gap_remains_allowed()
    {
        var rows = PublicSyncBoundaryRegistry.Rows;
        Assert.Single(rows, row => row.Entry == "listener stop");
        Assert.Empty(PublicSyncBoundaryRegistry.Validate(rows.Select(row => row.Entry == "listener stop"
            ? row with { Evidence = "No held-native listener stop evidence" } : row)));
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
    [InlineData("WindowsRoutingDirectContract.SyncDispose_HeldReportAndCheckedClose_DrainsBeforeReturningSharedOutcome")]
    [InlineData("WindowsFidoDirectFacadeTests.StaleMethod")]
    public void Unsupported_bare_or_stale_evidence_fails_closed(string evidence)
    {
        var rows = PublicSyncBoundaryRegistry.Rows;
        Assert.Contains($"stale evidence: {rows[0].Entry}", PublicSyncBoundaryRegistry.Validate(
            rows.Select((row, index) => index == 0 ? row with { Evidence = evidence } : row)));
    }
}