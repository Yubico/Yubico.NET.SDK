using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;

internal static class CurrentProfile
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private const string NativeUnavailable = "Native-only duration is not observable without native instrumentation";

    internal sealed record Row(string Scenario, string Classification, double? CallerReturnMs,
        double? OperationCompleteMs, double? DisposalMs, double? ReopenMs, double? IdleMs,
        long? ProcessAllocatedBytes, int? ThreadCountAtReturn, int? ThreadCountAtEnd,
        double? CpuDeltaMs, double? NativeDurationMs, string NativeDurationUnavailableReason,
        int? PendingOrdinaryCount, string PendingOrdinaryCountUnavailableReason, string? ErrorType = null,
        double? IdleCpuMs = null, long? IdleAllocatedBytes = null,
        double? CloseCpuMs = null, long? CloseAllocatedBytes = null,
        string? ConnectionTypeName = null, int? DeviceSerial = null,
        string? FixtureFirmware = null, string? FixtureFirmwareUnavailableReason = "Not recorded",
        int? ThreadDelta = null);

    internal sealed record Summary(int Count, double? Median, double? P95);

    internal sealed record SampleResult(string Outcome, int ExitCode, int WarmupCompleted, int WarmupCensored,
        int WarmupFailed, int NormalCompleted, int NormalCensored, int NormalFailed,
        int IdleCompleted, int IdleCensored, int IdleFailed);

    private static SampleResult SampleOutcome(IReadOnlyList<Row> samples)
    {
        var warmups = samples.Take(2).ToArray();
        var normal = samples.Skip(2).Take(10).ToArray();
        var idle = samples.Skip(12).Take(1).ToArray();
        var done = warmups.Length == 2 && normal.Length == 10 && idle.Length == 1 &&
            samples.All(s => s.Classification == "completed");
        static int Count(IEnumerable<Row> rows, string classification) => rows.Count(s => s.Classification == classification);
        static int FailedCount(IEnumerable<Row> rows) => rows.Count(s => s.Classification is not ("completed" or "censored_timeout"));
        return new SampleResult(done ? "completed" : "incomplete", done ? 0 : 1,
            Count(warmups, "completed"), Count(warmups, "censored_timeout"), FailedCount(warmups),
            Count(normal, "completed"), Count(normal, "censored_timeout"), FailedCount(normal),
            Count(idle, "completed"), Count(idle, "censored_timeout"), FailedCount(idle));
    }

    internal static Summary Stats(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0) return new Summary(0, null, null);
        var middle = sorted.Length / 2;
        return new Summary(sorted.Length, sorted.Length % 2 == 0
            ? (sorted[middle - 1] + sorted[middle]) / 2 : sorted[middle],
            sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1]);
    }

    internal static long? AllocationDelta(long before, long after) => after >= before ? after - before : null;

    private const string PendingUnavailable = "Owner pending ordinary count is not exposed without production instrumentation";

    internal static bool NativeVersionMatches(string deployed, string checkout) =>
        string.Equals(deployed, checkout, StringComparison.Ordinal);

    internal static async Task<int> SelfTestAsync()
    {
        var stats = Stats([1, 2, 3, 4, 20]);
        if (stats.Count != 5 || stats.Median != 3 || stats.P95 != 20 ||
            AllocationDelta(100, 250) != 150 || AllocationDelta(250, 100) is not null ||
            Stats([]).Count != 0 || NativeVersionMatches("1.18.1-async.3", "1.18.1-async.7"))
            throw new InvalidOperationException("Profile aggregation or allocation delta contract failed");
        await CheckChildClassification();
        CheckSampleOutcome();
        Console.WriteLine("Profile self-test: 4 passed, 0 failed");
        return 0;
    }

    private static async Task CheckChildClassification()
    {
        var fault = await RunChildAsync("synthetic-fault", 1);
        var timeout = await RunChildAsync("synthetic-timeout", 1);
        if (fault.Classification != "child_failed" || fault.ErrorType != nameof(InvalidOperationException) ||
            timeout.Classification != "censored_timeout" || timeout.ErrorType is not null ||
            await ChildAsync("invalid-mode", 1) != 2)
            throw new InvalidOperationException("Profile child failure and timeout classification failed");
    }

    private static void CheckSampleOutcome()
    {
        var partial = SampleOutcome([Failed("normal", "censored_timeout")]);
        var failed = SampleOutcome([Failed("normal", "completed"), Failed("normal", "completed"),
            Failed("normal", "child_failed", nameof(InvalidOperationException))]);
        if (partial.Outcome != "incomplete" || partial.ExitCode != 1 ||
            partial.WarmupCompleted != 0 || partial.WarmupCensored != 1 || partial.NormalCompleted != 0 ||
            partial.IdleCompleted != 0 || failed.NormalFailed != 1 || failed.Outcome != "incomplete")
            throw new InvalidOperationException("Partial dataset must not claim planned samples were completed");
        var complete = SampleOutcome(Enumerable.Repeat(Failed("normal", "completed"), 12)
            .Append(Failed("idle", "completed")).ToArray());
        if (complete.Outcome != "completed" || complete.ExitCode != 0 ||
            complete.WarmupCompleted != 2 || complete.NormalCompleted != 10 || complete.IdleCompleted != 1)
            throw new InvalidOperationException("Complete dataset outcome contract failed");
    }

    internal static async Task<int> RunAsync(int serial)
    {
        if (!OperatingSystem.IsMacOS() || serial <= 0) return 2;
        var provenance = await CollectProvenance();
        var samples = await RunSamples(serial);
        await WriteDataset(serial, provenance, samples);
        return SampleOutcome(samples).ExitCode;
    }

    private static async Task<(string Root, string Core, string Version, string CheckoutVersion,
        string Native, string NativeHash, string Package, string Diff, object UntrackedHashes, object RunnerHashes,
        string ManifestSha512)> CollectProvenance()
    {
        var root = Path.GetFullPath(Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_SOURCE_ROOT")
            ?? throw new InvalidOperationException("Set YUBIKIT_BOUNDARY_SOURCE_ROOT to the current Core checkout"));
        var core = Path.GetFullPath(Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_CORE_PATH")
            ?? Path.Combine(root, "src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll"));
        if (Hash(core) != Hash(typeof(YubiKeyManager).Assembly.Location))
            throw new InvalidOperationException("Profile runner is not executing the selected Core binary");
        var nativeDetails = await ValidateNative(root);
        var diff = await GitAsync(root, "diff", "HEAD", "--binary", "--", "src", "Directory.Packages.props");
        var untracked = await GitAsync(root, "ls-files", "--others", "--exclude-standard", "--", "src", "Directory.Packages.props");
        var untrackedHashes = untracked.Length == 0 ? [] : untracked.Split('\n').Select(p => new { path = p, sha256 = Hash(Path.Combine(root, p)) }).ToArray();
        var runnerRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_RUNNER_ROOT")
            ?? throw new InvalidOperationException("Set YUBIKIT_BOUNDARY_RUNNER_ROOT to the runner checkout"));
        var runner = Path.Combine(runnerRoot, "verification/MacOSHidBoundaryComparison");
        var runnerHashes = new { program = Hash(Path.Combine(runner, "Program.cs")),
            profile = Hash(Path.Combine(runner, "CurrentProfile.cs")),
            selfTest = Hash(Path.Combine(runner, "RunnerSelfTest.cs")),
            project = Hash(Path.Combine(runner, "MacOSHidBoundaryComparison.csproj")),
            buildProps = Hash(Path.Combine(runner, "Directory.Build.props")) };
        return (root, core, nativeDetails.Version, nativeDetails.CheckoutVersion, nativeDetails.Native,
            nativeDetails.NativeHash, nativeDetails.Package, diff, untrackedHashes, runnerHashes, nativeDetails.ManifestSha512);
    }

    private static async Task<(string Version, string CheckoutVersion, string Native, string NativeHash,
        string Package, string ManifestSha512)> ValidateNative(string root)
    {
        using var deps = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,
            "MacOSHidBoundaryComparison.deps.json")));
        var libraries = deps.RootElement.GetProperty("libraries").EnumerateObject()
            .Where(e => e.Name.StartsWith("Yubico.NativeShims/", StringComparison.Ordinal)).ToArray();
        if (libraries.Length != 1) throw new InvalidOperationException("Expected exactly one deployed native dependency");
        var version = libraries[0].Name.Split('/')[1];
        var packageProps = await File.ReadAllTextAsync(Path.Combine(root, "Directory.Packages.props"));
        var checkoutVersion = Regex.Match(packageProps,
            "<PackageVersion Include=\"Yubico.NativeShims\" Version=\"([^\"]+)\" />").Groups[1].Value;
        if (!NativeVersionMatches(version, checkoutVersion))
            throw new InvalidOperationException("Deployed native version differs from current Core checkout");
        var native = Path.Combine(AppContext.BaseDirectory, "libYubico.NativeShims.dylib");
        if (!File.Exists(native)) native = Path.Combine(AppContext.BaseDirectory, "runtimes",
            RuntimeInformation.RuntimeIdentifier, "native", "libYubico.NativeShims.dylib");
        var package = Path.Combine(Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget/packages"),
            "yubico.nativeshims", version, $"yubico.nativeshims.{version}.nupkg");
        using var archive = ZipFile.OpenRead(package);
        using var asset = (archive.GetEntry($"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libYubico.NativeShims.dylib")
            ?? throw new InvalidOperationException("Native asset absent from package")).Open();
        var nativeHash = Hash(native);
        if (Convert.ToHexString(SHA256.HashData(asset)) != nativeHash)
            throw new InvalidOperationException("Deployed native binary differs from package asset");
        return (version, checkoutVersion, native, nativeHash, package,
            libraries[0].Value.GetProperty("sha512").GetString() ?? "");
    }

    private static async Task<List<Row>> RunSamples(int serial)
    {
        var samples = new List<Row>();
        for (var i = 0; i < 12; i++)
        {
            var row = await RunChildAsync("normal", serial);
            samples.Add(row);
            Console.WriteLine($"normal {(i < 2 ? "warmup" : "sample")} {i + 1}: {row.Classification}");
            if (row.Classification != "completed") break;
        }
        if (samples.Count == 12 && samples.All(s => s.Classification == "completed"))
            samples.Add(await RunChildAsync("idle", serial));
        return samples;
    }

    private static async Task WriteDataset(int serial, (string Root, string Core, string Version,
        string CheckoutVersion, string Native, string NativeHash, string Package, string Diff,
        object UntrackedHashes, object RunnerHashes, string ManifestSha512) provenance, List<Row> samples)
    {
        var (root, core, version, checkoutVersion, native, nativeHash, package, diff,
            untrackedHashes, runnerHashes, manifestSha512) = provenance;
        var output = Path.Combine(root, "artifacts/measurements");
        Directory.CreateDirectory(output);
        var filename = Path.Combine(output, $"current-profile-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}.json");
        var result = SampleOutcome(samples);
        var dataset = new { schema = "macos-hid-current-profile/2", command = $"--profile --serial {serial}",
            result.Outcome, result.ExitCode, expectedWarmups = 2, expectedNormalSamples = 10,
            expectedIdleSamples = 1, actualCounts = new { result.WarmupCompleted, result.WarmupCensored,
                result.WarmupFailed, result.NormalCompleted, result.NormalCensored, result.NormalFailed,
                result.IdleCompleted, result.IdleCensored, result.IdleFailed },
            sourceRoot = root, sourceCommit = await GitAsync(root, "rev-parse", "HEAD"),
            shippingDiffSha256 = HashText(diff), untrackedSource = untrackedHashes,
            runnerSourceSha256 = runnerHashes, runnerAssemblySha256 = Hash(typeof(CurrentProfile).Assembly.Location),
            coreSha256 = Hash(core), loadedCorePath = typeof(YubiKeyManager).Assembly.Location,
            coreBuiltFromSourceDiff = "not independently verified",
            nativeShims = new { version, checkoutVersion, manifestSha512,
                packageSha256 = Hash(package), deployedNativeSha256 = nativeHash, deployedNativePath = native,
                loadedNativePath = (string?)null, loadedNativePathUnavailableReason = "not observed in process" },
            machine = new { os = RuntimeInformation.OSDescription, architecture = RuntimeInformation.OSArchitecture,
                runtime = RuntimeInformation.FrameworkDescription }, fixtureSerial = serial,
            idlePeriodMs = 1000,
            sampling = "fresh child per cycle; two fresh-child warmups excluded from statistics. OperationCompleteMs includes deferred channel initialization and CTAP GetInfo request; DisposalMs includes session and first connection disposal. IdleCpuMs and IdleAllocatedBytes stop before disposal; CloseCpuMs and CloseAllocatedBytes measure first connection disposal only, not session disposal. CpuDeltaMs and ProcessAllocatedBytes cover idle plus connection close in idle mode, and opening/reopening through before reopened connection disposal in normal mode; neither is native-only. Not comparable to historical cold-start comparison datasets or in-process warmup results.",
            timeoutMs = 10000, samples,
            fixtureFirmware = samples.Select(s => s.FixtureFirmware).FirstOrDefault(v => v is not null),
            fixtureFirmwareUnavailableReason = samples.Any(s => s.FixtureFirmware is not null) ? null : "IYubiKey does not expose firmware version; GetInfo is not parsed",
            stats = new { callerReturnMs = Stats(samples.Skip(2).Take(10).Where(s => s.Classification == "completed").Select(s => s.CallerReturnMs).OfType<double>()),
                operationCompleteMs = Stats(samples.Skip(2).Take(10).Where(s => s.Classification == "completed").Select(s => s.OperationCompleteMs).OfType<double>()),
                disposalMs = Stats(samples.Skip(2).Take(10).Where(s => s.Classification == "completed").Select(s => s.DisposalMs).OfType<double>()),
                allocatedBytes = Stats(samples.Skip(2).Take(10).Where(s => s.Classification == "completed").Select(s => s.ProcessAllocatedBytes).OfType<long>().Select(v => (double)v)) } };
        await File.WriteAllTextAsync(filename, JsonSerializer.Serialize(dataset, Json));
        Console.WriteLine($"dataset: {filename}");
    }

    private static async Task<Row> RunChildAsync(string mode, int serial, int timeoutMs = 10000)
    {
        using var child = new Process();
        child.StartInfo = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { typeof(CurrentProfile).Assembly.Location, "--profile-child", mode, serial.ToString() })
            child.StartInfo.ArgumentList.Add(arg);
        child.Start();
        var stdout = child.StandardOutput.ReadToEndAsync();
        var stderr = child.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(timeoutMs);
        try { await child.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        {
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync();
            _ = await stdout;
            _ = await stderr;
            return Failed(mode, "censored_timeout"); // A killed child cannot confirm its terminal cause or cleanup.
        }
        var text = await stdout;
        var error = (await stderr).Trim();
        if (child.ExitCode != 0) return Failed(mode, child.ExitCode == 4 ? "fixture_unavailable" : "child_failed",
            Regex.IsMatch(error, "^[A-Za-z_][A-Za-z0-9_]{0,99}$") ? error : null);
        try { return JsonSerializer.Deserialize<Row>(text, Json) ?? throw new JsonException("Empty row"); }
        catch (JsonException) { return Failed(mode, "malformed_child_output"); }
    }

    private static Row Failed(string mode, string classification, string? errorType = null) =>
        new(mode, classification, null, null, null, null, null, null, null, null, null, null,
            NativeUnavailable, null, PendingUnavailable, errorType);

    internal static async Task<int> ChildAsync(string mode, int serial)
    {
        if (mode == "synthetic-fault") { Console.Error.WriteLine(nameof(InvalidOperationException)); return 6; }
        if (mode == "synthetic-timeout") { await Task.Delay(Timeout.Infinite); return 0; }
        if (!OperatingSystem.IsMacOS() || serial <= 0 || mode is not ("normal" or "idle")) return 2;
        try
        {
            var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido, forceRescan: true);
            var matches = devices.Where(d => d.SerialNumber == serial && d.SupportsConnection(ConnectionType.HidFido)).ToArray();
            if (matches.Length != 1) return 4;
            var row = mode == "normal" ? await MeasureNormal(matches[0]) : await MeasureIdle(matches[0]);
            Console.WriteLine(JsonSerializer.Serialize(row, Json));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.GetType().Name);
            return 6;
        }
        finally { await YubiKeyManager.ShutdownAsync(); }
    }

    private static async Task<Row> MeasureNormal(IYubiKey device)
    {
        using var process = Process.GetCurrentProcess();
        var watch = Stopwatch.StartNew();
        var allocated = GC.GetTotalAllocatedBytes(false);
        var cpu = process.TotalProcessorTime;
        var threadsBefore = process.Threads.Count;
        double returned, total;
        int threadsAtReturn;
        string connectionType;
        double closeCpu;
        long? closeAllocated;
        double disposal, taskAt;
        TimeSpan closeCpuStart;
        long closeAllocatedStart;
        await using (var connection = await device.ConnectAsync<IFidoHidConnection>())
        {
            connectionType = connection.GetType().FullName ?? "unknown";
            if (connection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Unexpected FIDO connection type");
            await using (var session = await RawFidoHidSession.CreateAsync(connection))
            {
                var invocationStart = watch.Elapsed.TotalMilliseconds;
                var pending = session.SendAndReceiveAsync(0x10, new byte[] { 0x04 });
                returned = watch.Elapsed.TotalMilliseconds - invocationStart;
                threadsAtReturn = process.Threads.Count;
                var response = await pending;
                if (response.IsEmpty || response.Span[0] != 0)
                    throw new InvalidDataException("GetInfo did not succeed");
                taskAt = watch.Elapsed.TotalMilliseconds;
                total = taskAt - invocationStart;
            }
            closeCpuStart = process.TotalProcessorTime;
            closeAllocatedStart = GC.GetTotalAllocatedBytes(false);
            // The session is already disposed: these snapshots isolate connection disposal.
        }
        closeCpu = (process.TotalProcessorTime - closeCpuStart).TotalMilliseconds;
        closeAllocated = AllocationDelta(closeAllocatedStart, GC.GetTotalAllocatedBytes(false));
        disposal = watch.Elapsed.TotalMilliseconds - taskAt;
        var closeAt = watch.Elapsed.TotalMilliseconds;
        await using var opened = await device.ConnectAsync<IFidoHidConnection>();
        var reopen = watch.Elapsed.TotalMilliseconds - closeAt;
        var threadsAfter = process.Threads.Count;
        return new Row("normal", "completed", returned, total, disposal, reopen, null,
            AllocationDelta(allocated, GC.GetTotalAllocatedBytes(false)), threadsAtReturn, threadsAfter,
            (process.TotalProcessorTime - cpu).TotalMilliseconds, null, NativeUnavailable, null, PendingUnavailable,
            CloseCpuMs: closeCpu, CloseAllocatedBytes: closeAllocated, ConnectionTypeName: connectionType,
            DeviceSerial: device.SerialNumber, FixtureFirmwareUnavailableReason: "IYubiKey does not expose firmware version; GetInfo is not parsed",
            ThreadDelta: threadsAfter - threadsBefore);
    }

    private static async Task<Row> MeasureIdle(IYubiKey device)
    {
        using var process = Process.GetCurrentProcess();
        var watch = Stopwatch.StartNew();
        var threadsBefore = process.Threads.Count;
        double idle, idleCpu;
        long? idleAllocated;
        string connectionType;
        TimeSpan cpuStart, closeCpuStart;
        long allocatedStart, closeAllocatedStart;
        await using (var connection = await device.ConnectAsync<IFidoHidConnection>())
        {
            connectionType = connection.GetType().FullName ?? "unknown";
            if (connection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Unexpected FIDO connection type");
            // No receive is started; the snapshot ends before connection disposal.
            var idleStart = watch.Elapsed.TotalMilliseconds;
            allocatedStart = GC.GetTotalAllocatedBytes(false);
            cpuStart = process.TotalProcessorTime;
            await Task.Delay(1000);
            idle = watch.Elapsed.TotalMilliseconds - idleStart;
            idleCpu = (process.TotalProcessorTime - cpuStart).TotalMilliseconds;
            idleAllocated = AllocationDelta(allocatedStart, GC.GetTotalAllocatedBytes(false));
            closeCpuStart = process.TotalProcessorTime;
            closeAllocatedStart = GC.GetTotalAllocatedBytes(false);
        }
        var closeCpu = (process.TotalProcessorTime - closeCpuStart).TotalMilliseconds;
        var closeAllocated = AllocationDelta(closeAllocatedStart, GC.GetTotalAllocatedBytes(false));
        var threadsAfter = process.Threads.Count;
        return new Row("idle", "completed", null, null, null, null, idle,
            AllocationDelta(allocatedStart, GC.GetTotalAllocatedBytes(false)), null, threadsAfter,
            (process.TotalProcessorTime - cpuStart).TotalMilliseconds, null, NativeUnavailable, null, PendingUnavailable,
            IdleCpuMs: idleCpu, IdleAllocatedBytes: idleAllocated, CloseCpuMs: closeCpu,
            CloseAllocatedBytes: closeAllocated, ConnectionTypeName: connectionType, DeviceSerial: device.SerialNumber,
            FixtureFirmwareUnavailableReason: "IYubiKey does not expose firmware version; GetInfo is not parsed",
            ThreadDelta: threadsAfter - threadsBefore);
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static string HashText(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static async Task<string> GitAsync(string root, params string[] args)
    {
        using var git = new Process();
        git.StartInfo = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true };
        foreach (var arg in args) git.StartInfo.ArgumentList.Add(arg);
        git.Start();
        var output = await git.StandardOutput.ReadToEndAsync();
        await git.WaitForExitAsync();
        if (git.ExitCode != 0) throw new InvalidOperationException("Git provenance failed");
        return output.Trim();
    }
}
