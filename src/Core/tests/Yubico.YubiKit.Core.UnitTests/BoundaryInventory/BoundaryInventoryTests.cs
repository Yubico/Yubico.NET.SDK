using System.Text.Json;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

public class BoundaryInventoryTests
{
    private const string Fixture = """
        using System;
        using System.Runtime.InteropServices;
        using System.Threading;
        using System.Threading.Tasks;
        interface IPort { Task ReadAsync(); }
        interface IValuePort { ValueTask<int> ReadAsync(); }
        class Port : IPort { public Task ReadAsync() { Task.Delay(1).Wait(); return Task.CompletedTask; } }
        class Probe {
            [DllImport("example")] internal static extern int Native();
            public int Result => 4;
            public int Ordinary() => Result;
            public void OrdinaryWait() { new ManualResetEvent(false).WaitOne(0); new ManualResetEventSlim(false).Wait(0); new SemaphoreSlim(0).Wait(0); new Thread(() => { }).Join(0); }
            public int Block(Task<int> task) => task.Result;
            public void Schedule() { Task.Run(() => 1); new Thread(() => { }).Start(); }
            public Task FalseViaInterface(IPort port) { port.ReadAsync(); return Task.CompletedTask; }
            public int BlockingLeaf(Task<int> task) => task.Result;
            public Task FalseViaDirect(Task<int> task) { BlockingLeaf(task); return Task.CompletedTask; }
            public Task<int> GenericTask(IPort port) { port.ReadAsync(); return Task.FromResult(1); }
            public ValueTask<int> GenericValueTask(IValuePort port) { port.ReadAsync(); return ValueTask.FromResult(1); }
            public Task<int> GenericDirect(Task<int> task) { task.Wait(); return Task.FromResult(1); }
            public Task DirectWait(Task task) { task.Wait(); return Task.CompletedTask; }
            public Task DirectNative() { Native(); return Task.CompletedTask; }
            public int Configured(Task<int> task, ValueTask<int> value) => task.ConfigureAwait(false).GetAwaiter().GetResult() + value.ConfigureAwait(false).GetAwaiter().GetResult();
            public Task Offload(IPort port) => Task.Run(() => port.ReadAsync().GetAwaiter().GetResult());
            public void Export() { NativeLibrary.Load("example"); NativeLibrary.TryGetExport(IntPtr.Zero, "x", out _); NativeLibrary.GetExport(IntPtr.Zero, "x"); }
            public void Long() { Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default); }
        }
        """;

    [Fact]
    public void Scanner_resolves_native_blocking_and_scheduling_without_confusing_ordinary_result()
    {
        var sites = BoundaryScanner.Scan([("Fixture.cs", Fixture)]);
        Assert.Contains(sites, site => site.Kind == "native-import" && site.Target.Contains("Probe.Native", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "blocking-wait" && site.Target.Contains("Task<TResult>.Result", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "blocking-wait" && site.Target.Contains("GetResult", StringComparison.Ordinal));
        Assert.DoesNotContain(sites, site => site.Target.Contains("Probe.Result", StringComparison.Ordinal));
        Assert.Equal(4, sites.Count(site => site.Owner.Contains("OrdinaryWait", StringComparison.Ordinal) && site.Kind == "blocking-wait"));
        Assert.Contains(sites, site => site.Kind == "scheduling" && site.Target.Contains("Thread.Start", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "scheduling" && site.Target.Contains("Task.Run", StringComparison.Ordinal));
        Assert.Equal(3, sites.Count(site => site.Kind == "native-export"));
        Assert.Contains(sites, site => site.Kind == "scheduling" && site.Target.Contains("StartNew", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "scheduling" && site.Target.Contains("LongRunning", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_native_and_stale_entry_both_fail_gate()
    {
        var sites = BoundaryScanner.Scan([("Fixture.cs", Fixture)]);
        var approved = sites.Select(site => BoundaryManifest.Review(site, "fixture owner", "Fixture-only classification", "fixture inspection", "outstanding")).ToArray();
        Assert.Empty(BoundaryManifest.Validate(sites, approved));
        Assert.Contains(BoundaryManifest.Validate(sites, approved.Where(row => row.Id != sites.Single(s => s.Kind == "native-import").Id)),
            error => error.Contains("unclassified", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(BoundaryManifest.Validate(sites, [.. approved, approved[0] with { Id = "Fixture.cs|missing|native-import|x|1" }]),
            error => error.Contains("stale", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(BoundaryManifest.Validate(sites, approved.Select(row => row.Id == sites[0].Id
            ? row with { Owner = "global::Probe.Other()" } : row)),
            error => error.Contains("stale", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(BoundaryManifest.Validate(sites, approved.Select(row => row.Id == sites[0].Id
            ? row with { Evidence = [] } : row)),
            error => error.Contains("invalid review", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Exact_fingerprint_baseline_rejects_new_and_stale_native_symbols()
    {
        var sites = BoundaryScanner.Scan([("Fixture.cs", Fixture)]);
        var native = sites.Single(site => site.Kind == "native-import");
        var entry = "Native#" + BoundaryManifest.Fingerprint(native);
        var manifest = new BoundaryManifest(1, [new BoundaryFamily("Fixture.cs", "Core maintainers",
            "Native entry point is an unmanaged synchronous call; invocation thread needs review.",
            ["Fixture.cs native declaration"], ["documented", "outstanding"], [entry])]);
        Assert.Contains(BoundaryManifest.Validate(sites, manifest), error => error.StartsWith("unclassified:", StringComparison.Ordinal));
        Assert.Contains(BoundaryManifest.Validate([native with { Id = native.Id.Replace("|1", "|2", StringComparison.Ordinal) }], manifest),
            error => error.StartsWith("stale:", StringComparison.Ordinal));
        Assert.Empty(BoundaryManifest.Validate([native], manifest));
    }

    [Fact]
    public void Baseline_rejects_duplicate_and_non_native_family_waiver()
    {
        var sites = BoundaryScanner.Scan([("Fixture.cs", Fixture)]);
        var native = sites.Single(site => site.Kind == "native-import");
        var entry = "Native#" + BoundaryManifest.Fingerprint(native);
        var family = new BoundaryFamily("Fixture.cs", "Core maintainers", "Native import at caller thread",
            ["Fixture.cs native declaration"], ["documented", "outstanding"], [entry, entry]);
        Assert.Contains(BoundaryManifest.Validate([native], new BoundaryManifest(1, [family])),
            error => error.StartsWith("duplicate:", StringComparison.Ordinal));
        var wait = sites.First(site => site.Kind == "blocking-wait");
        Assert.Contains(BoundaryManifest.Validate([wait], new BoundaryManifest(1,
            [family with { Entries = ["ReadAsync#" + BoundaryManifest.Fingerprint(wait)] }])),
            error => error.Contains("non-native", StringComparison.Ordinal));
    }

    [Fact]
    public void False_async_direct_and_interface_dispatch_remain_unsafe_even_with_approved_owner()
    {
        var sites = BoundaryScanner.Scan([("Fixture.cs", Fixture)]);
        var approved = sites.Select(site => BoundaryManifest.Review(site, "Probe", "Fixture inspection", "fixture inspection", "verified")).ToArray();
        Assert.Contains(BoundaryManifest.Validate(sites, approved), error => error.Contains("unsafe", StringComparison.OrdinalIgnoreCase));
        Assert.True(sites.Any(site => site.Kind == "dispatch-gap" && site.Owner.Contains("FalseViaInterface", StringComparison.Ordinal)),
            string.Join("; ", sites.Where(site => site.Owner.Contains("FalseViaInterface", StringComparison.Ordinal))));
        Assert.Contains(sites, site => site.Kind == "dispatch-gap" && site.Owner.Contains("FalseViaDirect", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "dispatch-gap" && site.Owner.Contains("FalseViaInterface", StringComparison.Ordinal) && !site.UnsafeAsync);
        Assert.DoesNotContain(sites, site => site.Kind == "dispatch-gap" && site.Owner.Contains("Offload", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "dispatch-gap" && site.Owner.Contains("GenericTask", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "dispatch-gap" && site.Owner.Contains("GenericValueTask", StringComparison.Ordinal));
        Assert.All(sites.Where(site => (site.Kind == "dispatch-gap" || site.Kind == "blocking-wait") && (site.Owner.Contains("GenericDirect", StringComparison.Ordinal) ||
            site.Owner.Contains("DirectWait", StringComparison.Ordinal) || site.Owner.Contains("DirectNative", StringComparison.Ordinal))), site => Assert.True(site.UnsafeAsync));
        Assert.Contains(sites, site => site.Kind == "blocking-wait" && site.Owner.Contains("GenericDirect", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "blocking-wait" && site.Owner.Contains("DirectWait", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "dispatch-gap" && site.Owner.Contains("DirectNative", StringComparison.Ordinal));
        Assert.Equal(2, sites.Count(site => site.Kind == "blocking-wait" && site.Owner.Contains("Configured", StringComparison.Ordinal)));
    }

    [Fact]
    public void Callback_conversion_native_delegate_and_unmanaged_address_are_separate_sites()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            using System.Runtime.CompilerServices;
            unsafe class Hooks {
                delegate void Handler();
                [DllImport("example")] static extern void Register(Handler callback);
                [UnmanagedCallersOnly] static void OnNative() { }
                static void OnManaged() { }
                static void Setup() { Register(OnManaged); Marshal.GetFunctionPointerForDelegate((Handler)OnManaged); delegate* unmanaged<void> ptr = &OnNative; }
            }
            """;
        var sites = BoundaryScanner.Scan([("Hooks.cs", source)]);
        Assert.Contains(sites, site => site.Kind == "callback-registration" && site.Target.Contains("Hooks.Register", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "callback-conversion" && site.Target.Contains("GetFunctionPointerForDelegate", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "callback-address" && site.Target.Contains("Hooks.OnNative", StringComparison.Ordinal));
        Assert.Contains(sites, site => site.Kind == "callback-registration" && site.Owner.Contains("Hooks.Setup", StringComparison.Ordinal));
    }

    [Fact]
    public void Callback_function_pointer_parameter_on_native_import_is_recognized()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            unsafe class Hooks {
                [DllImport("example")] static extern void Register(delegate* unmanaged<void> callback);
                static void Setup(delegate* unmanaged<void> callback) { Register(callback); }
            }
            """;
        Assert.Contains(BoundaryScanner.Scan([("Hooks.cs", source)]), site =>
            site.Kind == "callback-registration" && site.Owner.Contains("Hooks.Setup", StringComparison.Ordinal));
    }

    [Fact]
    public void Callback_native_invocation_is_detected_through_single_managed_forwarder()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;
            unsafe class Hooks {
                [DllImport("example")] static extern void Register(delegate* unmanaged<void> callback);
                static void Forward(delegate* unmanaged<void> callback) { Register(callback); }
                static void Setup(delegate* unmanaged<void> callback) { Forward(callback); }
            }
            """;
        Assert.Contains(BoundaryScanner.Scan([("Hooks.cs", source)]), site =>
            site.Kind == "callback-registration" && site.Owner.Contains("Hooks.Setup", StringComparison.Ordinal));
    }

    [Fact]
    public void Other_real_blocking_primitives_are_resolved_not_name_matched()
    {
        const string source = """
            using System;
            using System.Threading;
            class Probe {
                void Block(object gate, WaitHandle[] handles) { Thread.Sleep(1); lock (gate) Monitor.Wait(gate, 1); WaitHandle.WaitAll(handles, 1); WaitHandle.WaitAny(handles, 1); }
                void Wait() { }
            }
            """;
        var sites = BoundaryScanner.Scan([("Waits.cs", source)]);
        Assert.Equal(4, sites.Count(site => site.Kind == "blocking-wait"));
        Assert.DoesNotContain(sites, site => site.Owner.Contains("Probe.Wait()", StringComparison.Ordinal));
    }

    [Fact]
    public void Callback_forwarder_scan_is_independent_of_source_file_order()
    {
        var sources = new[]
        {
            ("Caller.cs", "unsafe class Caller { static void Forward(delegate* unmanaged<void> callback) => Native.Register(callback); static void Setup(delegate* unmanaged<void> callback) => Forward(callback); }"),
            ("Native.cs", "using System.Runtime.InteropServices; unsafe class Native { [DllImport(\"example\")] internal static extern void Register(delegate* unmanaged<void> callback); }")
        };
        var forward = BoundaryScanner.Scan(sources);
        var reverse = BoundaryScanner.Scan(sources.Reverse());
        Assert.Equal(forward, reverse);
        Assert.Contains(forward, site => site.Kind == "callback-registration" && site.Owner.Contains("Caller.Setup", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_conditional_symbol_fails_closed()
    {
        const string source = """
            #if UNREVIEWED_PLATFORM
            class Hidden { }
            #endif
            """;
        Assert.Throws<InvalidOperationException>(() => BoundaryScanner.Scan([("Conditional.cs", source)]));
    }

    [Fact]
    public void Actual_core_source_has_a_reviewed_inventory_with_nonzero_counts()
    {
        var root = BoundaryScanner.CoreSourceRoot();
        var sites = BoundaryScanner.ScanDirectory(root);
        Assert.Equal(200, sites.Count);
        Assert.Equal(sites.Count, sites.Select(site => site.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(130, sites.Count(site => site.Kind == "native-import"));
        Assert.Equal(24, sites.Count(site => site.Kind == "blocking-wait"));
        Assert.Equal(15, sites.Count(site => site.Kind == "scheduling"));
        Assert.Equal(21, sites.Count(site => site.Kind == "dispatch-gap"));
        Assert.Equal(6, sites.Count(site => site.Kind == "callback-registration"));
        Assert.Equal(2, sites.Count(site => site.Kind == "callback-conversion"));
        Assert.Equal(2, sites.Count(site => site.Kind == "callback-address"));
        var manifestPath = Path.Combine(Directory.GetParent(root)?.FullName ?? throw new InvalidOperationException("Core root missing"),
            "tests", "Yubico.YubiKit.Core.UnitTests", "BoundaryInventory", "core-boundaries.v1.json");
        var manifest = JsonSerializer.Deserialize<BoundaryManifest>(File.ReadAllText(manifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(manifest);
        Assert.Equal(1, manifest.Version);
        Assert.All(manifest.Families, family => Assert.Contains("outstanding", family.Status));
        Assert.All(manifest.Families, family => Assert.DoesNotContain("verified", family.Status));
        Assert.Equal(sites.Count, manifest.Families.Sum(family => family.Entries.Length));
        Assert.Empty(BoundaryManifest.Validate(sites, manifest));
    }

    [Fact]
    public void Unresolvable_symbol_fails_semantic_scan_instead_of_guessing_from_spelling()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => BoundaryScanner.Scan(
            [("Fixture.cs", "class Probe { void Broken() { UnknownNativeLibrary.Load(1); } }")]));
        Assert.Contains("Semantic compilation failed", exception.Message, StringComparison.Ordinal);
    }
}
