using System.Text.Json;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

public class AdapterContractRegistryTests
{
    private static AdapterContractRow[] Load() => JsonSerializer.Deserialize<AdapterContractRow[]>(
        File.ReadAllText(Path.Combine(Directory.GetParent(BoundaryScanner.CoreSourceRoot())?.FullName
            ?? throw new InvalidOperationException("Core root missing"), "tests", "Yubico.YubiKit.Core.UnitTests",
            "BoundaryInventory", "adapter-contracts.v1.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("No rows");

    [Fact]
    public void Migrated_routes_have_resolved_nonempty_contract_profiles()
    {
        var rows = Load();
        // Independent acceptance totals, not values derived from the manifest under test:
        // Baseline 43/76 plus Windows: FIDO 4/11, OTP 4/9, direct input 4/4,
        // direct feature 4/4 (operations/unique concrete test links).
        Assert.Equal(59, rows.Length);
        Assert.Equal(104, rows.Sum(row => row.Profiles.Length));
        Assert.Empty(AdapterContractRegistry.Validate(rows));
        Assert.Equal(4, rows.Count(row => row.Route == "macOS FIDO"));
        Assert.Equal(4, rows.Count(row => row.Route == "macOS OTP"));
        Assert.Equal(5, rows.Count(row => row.Route == "portable PCSC"));
        Assert.Equal(4, rows.Count(row => row.Route == "macOS direct input (sync)"));
        Assert.Equal(4, rows.Count(row => row.Route == "macOS direct feature (sync)"));
        Assert.Equal(2, rows.Count(row => row.Route == "macOS listener (sync)"));
        Assert.Equal(4, rows.Count(row => row.Route == "portable PCSC (sync)"));
    }

    [Theory]
    [InlineData("Windows HID FIDO", "open")]
    [InlineData("Windows HID FIDO", "send")]
    [InlineData("Windows HID FIDO", "receive")]
    [InlineData("Windows HID FIDO", "dispose")]
    [InlineData("Windows HID OTP", "open")]
    [InlineData("Windows HID OTP", "send")]
    [InlineData("Windows HID OTP", "receive")]
    [InlineData("Windows HID OTP", "dispose")]
    [InlineData("Windows direct input (sync)", "open")]
    [InlineData("Windows direct input (sync)", "get")]
    [InlineData("Windows direct input (sync)", "set")]
    [InlineData("Windows direct input (sync)", "dispose")]
    [InlineData("Windows direct feature (sync)", "open")]
    [InlineData("Windows direct feature (sync)", "get")]
    [InlineData("Windows direct feature (sync)", "set")]
    [InlineData("Windows direct feature (sync)", "dispose")]
    [InlineData("Linux HID FIDO", "open")]
    [InlineData("Linux HID FIDO", "send")]
    [InlineData("Linux HID FIDO", "receive")]
    [InlineData("Linux HID FIDO", "dispose")]
    [InlineData("Linux HID OTP", "open")]
    [InlineData("Linux HID OTP", "send")]
    [InlineData("Linux HID OTP", "receive")]
    [InlineData("Linux HID OTP", "dispose")]
    [InlineData("Linux direct input (sync)", "open")]
    [InlineData("Linux direct input (sync)", "get")]
    [InlineData("Linux direct input (sync)", "set")]
    [InlineData("Linux direct input (sync)", "dispose")]
    [InlineData("Linux direct feature (sync)", "open")]
    [InlineData("Linux direct feature (sync)", "get")]
    [InlineData("Linux direct feature (sync)", "set")]
    [InlineData("Linux direct feature (sync)", "dispose")]
    public void Removing_any_Linux_or_Windows_boundary_or_required_role_fails_closed(string route, string operation)
    {
        var rows = Load();
        var row = Assert.Single(rows, row => row.Route == route && row.Operation == operation);
        Assert.Contains($"missing required operation: {route}/{operation}",
            AdapterContractRegistry.Validate(rows.Where(candidate => candidate != row)));
        Assert.Contains($"empty profile: {route}/{operation}",
            AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row ? candidate with { Profiles = [] } : candidate)));
        foreach (var profile in row.Profiles)
        {
            var withoutRole = row with { Profiles = [.. row.Profiles.Where(candidate => candidate != profile)] };
            Assert.Contains(withoutRole.Profiles.Length == 0 ? $"empty profile: {route}/{operation}" : $"missing role: {route}/{operation}/{profile.Role}",
                AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row ? withoutRole : candidate)));
            Assert.Contains($"missing role: {route}/{operation}/{profile.Role}",
                AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row
                    ? candidate with { Profiles = [profile with { Role = "wrong role" }] } : candidate)));
        }
    }

    [Theory]
    [InlineData("transaction begin", "native wait")]
    [InlineData("transaction end", "native drain")]
    [InlineData("dispose", "native drain")]
    [InlineData("async default begin", "caller block")]
    public void Removing_any_sync_smartcard_boundary_or_role_fails_closed(string operation, string role)
    {
        const string route = "portable PCSC (sync)";
        var rows = Load();
        var row = Assert.Single(rows, row => row.Route == route && row.Operation == operation);
        Assert.Contains($"missing required operation: {route}/{operation}",
            AdapterContractRegistry.Validate(rows.Where(candidate => candidate != row)));
        Assert.Contains($"empty profile: {route}/{operation}",
            AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row
                ? candidate with { Profiles = [.. candidate.Profiles.Where(profile => profile.Role != role)] }
                : candidate)));
        Assert.Contains($"missing role: {route}/{operation}/{role}",
            AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row
                ? candidate with { Profiles = [candidate.Profiles[0] with { Role = "wrong role" }] }
                : candidate)));
    }

    [Theory]
    [InlineData("macOS direct input (sync)", "open")]
    [InlineData("macOS direct input (sync)", "get")]
    [InlineData("macOS direct input (sync)", "set")]
    [InlineData("macOS direct input (sync)", "dispose")]
    [InlineData("macOS direct feature (sync)", "open")]
    [InlineData("macOS direct feature (sync)", "get")]
    [InlineData("macOS direct feature (sync)", "set")]
    [InlineData("macOS direct feature (sync)", "dispose")]
    [InlineData("macOS listener (sync)", "start")]
    [InlineData("macOS listener (sync)", "stop")]
    public void Removing_any_direct_macOS_boundary_fails_closed(string route, string operation)
    {
        var rows = Load();
        var row = Assert.Single(rows, row => row.Route == route && row.Operation == operation);
        Assert.Contains($"missing required operation: {route}/{operation}",
            AdapterContractRegistry.Validate(rows.Where(row => row.Route != route || row.Operation != operation)));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row
            ? candidate with { Profiles = [candidate.Profiles[0] with { Role = "wrong profile" }] }
            : candidate)), error => error == $"unexpected role: {route}/{operation}/wrong profile");
    }

    [Fact]
    public void Missing_duplicate_and_stale_rows_or_profiles_fail_closed()
    {
        // Keep the existing macOS multi-role mutation fixture independent of JSON row ordering.
        var rows = Load().OrderBy(row => row.Route != "macOS FIDO").ToArray();
        Assert.Contains($"missing required operation: {rows[0].Route}/{rows[0].Operation}", AdapterContractRegistry.Validate(rows.Skip(1)));
        Assert.Contains(AdapterContractRegistry.Validate([.. rows, rows[0]]), error => error.Contains("duplicate", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Owner = "Missing.Owner" } : row)),
            error => error == "missing owner: Missing.Owner");
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Symbol = "AbsentMethod" } : row)),
            error => error == "missing symbol: " + rows[0].Owner + ".AbsentMethod");
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Owner = rows[4].Owner, Symbol = rows[4].Symbol } : row)),
            error => error == $"wrong owner: {rows[0].Route}/{rows[0].Operation}/{rows[4].Owner}");
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Symbol = rows[1].Symbol } : row)),
            error => error == $"wrong symbol: {rows[0].Route}/{rows[0].Operation}/{rows[1].Symbol}");
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [new("pending", "Yubico.YubiKit.Core.UnitTests.Transports.Hid.MacOSOtpRouteTests.AbsentTest")] } : row)),
            error => error.Contains("profile", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [] } : row)),
            error => error.Contains("profile", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Operation = "" } : row)),
            error => error.Contains("unexpected", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [rows[0].Profiles[0], rows[0].Profiles[0]] } : row)),
            error => error.Contains("duplicate", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 1 ? row with { Profiles = [rows[0].Profiles[0]] } : row)),
            error => error.Contains("duplicate profile", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [new("pending", "Yubico.YubiKit.Core.UnitTests.BoundaryInventory.AdapterContractRegistryTests.ToString")] } : row)),
            error => error.Contains("profile", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [new("pending", "Yubico.YubiKit.Core.UnitTests.BoundaryInventory.AdapterContractRegistryTests.Load")] } : row)),
            error => error.Contains("profile", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [rows[0].Profiles[0], rows[0].Profiles[1] with { Method = rows[0].Profiles[0].Method }] } : row)),
            error => error.Contains("duplicate profile", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [rows[0].Profiles[0] with { Role = "other" }] } : row)),
            error => error.Contains("role", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [rows[0].Profiles[0] with { Role = "pending" }, rows[0].Profiles[1] with { Role = "pending" }] } : row)),
            error => error.Contains("duplicate role", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [rows[0].Profiles[0], rows[0].Profiles[1] with { Method = rows[0].Profiles[0].Method }, rows[0].Profiles[2]] } : row)),
            error => error.Contains("duplicate profile", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Profiles = [rows[0].Profiles[0], rows[0].Profiles[1]] } : row)),
            error => error.Contains("missing role", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Select((row, i) => i == 0 ? row with { Route = "Windows HID unknown" } : row)),
            error => error.Contains("unexpected operation", StringComparison.Ordinal));
        Assert.Contains(AdapterContractRegistry.Validate(rows.Where(row => row.Route != "portable PCSC")),
            error => error == "missing required operation: portable PCSC/open");
    }

    [Fact]
    public void Windows_inherited_profiles_require_concrete_containers_and_preserve_owner_and_uniqueness_gates()
    {
        var rows = Load();
        var row = Assert.Single(rows, row => row.Route == "Windows direct input (sync)" && row.Operation == "dispose");
        var profile = Assert.Single(row.Profiles);
        Assert.Empty(AdapterContractRegistry.Validate(rows));
        foreach (var method in new[]
        {
            profile.Method.Replace("WindowsFidoDirectFacadeTests", "WindowsRoutingDirectContract", StringComparison.Ordinal),
            profile.Method + "Stale"
        })
            Assert.Contains($"stale profile: {method}", AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row
                ? row with { Profiles = [profile with { Method = method }] } : candidate)));
        var other = Assert.Single(rows, row => row.Route == "Windows direct input (sync)" && row.Operation == "set");
        Assert.Contains($"duplicate profile: {other.Profiles[0].Method}", AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row
            ? row with { Profiles = [profile with { Method = other.Profiles[0].Method }] } : candidate)));
        Assert.Contains($"wrong owner: {row.Route}/{row.Operation}/{other.Owner}Stale", AdapterContractRegistry.Validate(rows.Select(candidate => candidate == row
            ? row with { Owner = other.Owner + "Stale" } : candidate)));
    }

    [Fact]
    public void Conditional_or_skipped_fact_attributes_are_not_runnable_profiles()
    {
        Assert.True(AdapterContractRegistry.IsRunnable(new FactAttribute()));
        Assert.False(AdapterContractRegistry.IsRunnable(new FactAttribute { Skip = "not running" }));
        Assert.False(AdapterContractRegistry.IsRunnable(new FactAttribute { Explicit = true }));
        Assert.False(AdapterContractRegistry.IsRunnable(new FactAttribute { SkipUnless = "Enabled" }));
        Assert.False(AdapterContractRegistry.IsRunnable(new FactAttribute { SkipWhen = "Disabled" }));
        Assert.False(AdapterContractRegistry.IsRunnable(new FactAttribute { SkipExceptions = [typeof(InvalidOperationException)] }));
    }
}