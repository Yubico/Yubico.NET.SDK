using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;

internal static class Program
{
    private const int Serial = 31683481;
    private const string RunnerSourceRelativePath = "verification/MacOSHidBoundaryComparison";
    private const int TimeoutMs = 10000;
    private static readonly Regex ExceptionTypeName = new(@"^[A-Za-z_][A-Za-z0-9_]{0,99}$", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private static string SourceRoot => Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_SOURCE_ROOT")
        ?? throw new InvalidOperationException("Set YUBIKIT_BOUNDARY_SOURCE_ROOT to the built source checkout");

    internal sealed record Sample(string Scenario, string Classification, string? Phase, double ElapsedMs,
        double? InvocationReturnMs, double? TaskTerminalMs, double? LifecycleCompleteMs, int? ExitCode,
        string? ErrorType, double? OpenCompleteMs, double? SessionReadyMs, double? FirstCloseCompleteMs,
        double? ReopenCompleteMs, long? AllocatedBytesAtReturn, int? ThreadCountAtReturn,
        double? ProcessCpuMsAtReturn);

    private static async Task<int> Main(string[] args)
    {
        if (args is ["--self-test"]) return await RunnerSelfTest.RunAsync();
        if (args is ["--profile-self-test"]) return await CurrentProfile.SelfTestAsync();
        if (args is ["--profile", "--serial", var serial] && int.TryParse(serial, out var selectedSerial))
            return await CurrentProfile.RunAsync(selectedSerial);
        if (args is ["--profile-child", var childMode, var childSerial] && int.TryParse(childSerial, out var parsedSerial))
            return await CurrentProfile.ChildAsync(childMode, parsedSerial);
        if (args is ["--child", var childScenario]) return await ChildAsync(childScenario);
        if (args is ["--compare", var beforeFile, var afterFile])
        {
            CheckPairSummary(beforeFile, afterFile);
            return 0;
        }
        if (args is not ["--check"] && (args is not ["--measure", "before"] && args is not ["--measure", "after"]))
        {
            Console.WriteLine("--self-test | --check | --compare before.json after.json (hardware-free) | --measure before|after (historical .3) | --profile --serial N (current native dependency; two warmups, ten normal cycles and one 1s idle cycle)");
            return 2;
        }
        if (!OperatingSystem.IsMacOS()) return 2;
        var checkOnly = args is ["--check"];
        var variant = checkOnly ? "check" : args[1];
        var root = Path.GetFullPath(SourceRoot);
        var builtCore = Path.Combine(root, "src", "Core", "src", "bin", "Release", "net10.0", "Yubico.YubiKit.Core.dll");
        var preservedCore = Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_CORE_PATH");
        var core = preservedCore is null ? builtCore : Path.GetFullPath(preservedCore);
        if (Hash(core) != Hash(typeof(YubiKeyManager).Assembly.Location))
            throw new InvalidOperationException("Runner is not using the selected Core assembly");
        using var dependencies = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "MacOSHidBoundaryComparison.deps.json")));
        const string expectedNativeVersion = "1.18.1-async.3";
        var native = dependencies.RootElement.GetProperty("libraries").EnumerateObject()
            .Single(e => e.Name == $"Yubico.NativeShims/{expectedNativeVersion}");
        var nativeFile = Path.Combine(AppContext.BaseDirectory, "libYubico.NativeShims.dylib");
        if (!File.Exists(nativeFile)) nativeFile = Path.Combine(AppContext.BaseDirectory,
            "runtimes", RuntimeInformation.RuntimeIdentifier, "native", "libYubico.NativeShims.dylib");
        if (!File.Exists(nativeFile)) throw new FileNotFoundException("Deployed native library missing", nativeFile);
        var package = Path.Combine(Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"),
            "yubico.nativeshims", expectedNativeVersion, $"yubico.nativeshims.{expectedNativeVersion}.nupkg");
        using var archive = ZipFile.OpenRead(package);
        var entry = archive.GetEntry($"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libYubico.NativeShims.dylib")
            ?? throw new InvalidOperationException("Native asset missing from package");
        using var asset = entry.Open();
        var nativeHash = Hash(nativeFile);
        if (Convert.ToHexString(SHA256.HashData(asset)) != nativeHash)
            throw new InvalidOperationException("Deployed native asset differs from pinned package");
        var packageHash = Hash(package);
        const string expectedPackageHash = "B6DF35457DA06409F5BFD6643DD7DBC9DA8B0E7E8404BB5FA99FCDDE076F4A3C";
        if (packageHash != expectedPackageHash)
            throw new InvalidOperationException("Unexpected native package hash");
        var diff = await GitAsync(root, "diff", "HEAD", "--binary", "--", "Directory.Packages.props", "src");
        var untracked = await GitAsync(root, "ls-files", "--others", "--exclude-standard", "--", "src", "Directory.Packages.props");
        if (untracked.Length != 0) throw new InvalidOperationException("Untracked source (including tests): cannot hash complete source diff");
        var checkoutPackages = await File.ReadAllTextAsync(Path.Combine(root, "Directory.Packages.props"));
        var checkoutNativeVersion = Regex.Match(checkoutPackages,
            "<PackageVersion Include=\"Yubico.NativeShims\" Version=\"([^\"]+)\" />").Groups[1].Value;
        if (checkoutNativeVersion.Length == 0) throw new InvalidOperationException("Cannot identify checkout native package version");
        var runnerRoot = Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_RUNNER_ROOT")
            ?? throw new InvalidOperationException("Set YUBIKIT_BOUNDARY_RUNNER_ROOT to the worktree containing this project's source");
        var runnerSource = Path.Combine(Path.GetFullPath(runnerRoot), RunnerSourceRelativePath);
        var projectPath = Path.Combine(runnerSource, "MacOSHidBoundaryComparison.csproj");
        var programPath = Path.Combine(runnerSource, "Program.cs");
        var testsPath = Path.Combine(runnerSource, "RunnerSelfTest.cs");
        var propsPath = Path.Combine(runnerSource, "Directory.Build.props");
        var runnerHashes = new { project = Hash(projectPath), buildProps = Hash(propsPath),
            program = Hash(programPath), selfTest = Hash(testsPath) };
        var selectedCommit = await GitAsync(root, "rev-parse", "HEAD");
        if (!checkOnly && ((variant == "before" && !selectedCommit.StartsWith("65964966", StringComparison.Ordinal)) ||
            (variant == "after" && !selectedCommit.StartsWith("89420aa6", StringComparison.Ordinal))))
            throw new InvalidOperationException("Measurement checkout does not match the original before/after source commits");
        if (checkOnly)
        {
            Console.WriteLine($"Comparison preflight: source {selectedCommit}, checkout native version {checkoutNativeVersion}, comparison package {expectedNativeVersion} {packageHash}, native {nativeHash}, Core {Hash(core)}, runner source {runnerHashes.program}; no hardware accessed");
            return 0;
        }
        if (checkoutNativeVersion != expectedNativeVersion)
            throw new InvalidOperationException("Measurement checkout native version differs from deployed comparison package");
        var samples = new List<Sample>();
        foreach (var scenario in new[] { "lifecycle", "no-input-return" })
        {
            var count = scenario == "lifecycle" ? 12 : 1;
            for (var i = 0; i < count; i++)
            {
                var sample = await RunChildAsync(scenario, TimeoutMs);
                samples.Add(sample);
                Console.WriteLine($"{variant} {scenario} {(i < 2 && scenario == "lifecycle" ? "warmup" : "sample")} {i + 1}: {sample.Classification} / {sample.Phase} / {sample.ErrorType}");
                if (scenario == "lifecycle" && sample.Classification != "completed") break;
            }
        }
        var output = Path.Combine(root, "artifacts", "measurements");
        Directory.CreateDirectory(output);
        var filename = Path.Combine(output, $"comparison-v2-{variant}-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}.json");
        var dataset = new { schema = "macos-hid-boundary-comparison/2", variant,
            sourceCommit = selectedCommit, sourceRoot = root,
            shippingDiffSha256 = HashText(diff), checkoutNativeVersion,
            runnerSourceSha256 = runnerHashes,
            runnerAssemblySha256 = Hash(typeof(Program).Assembly.Location),
            coreSha256 = Hash(core), coreSourcePath = core, loadedCorePath = typeof(YubiKeyManager).Assembly.Location,
             nativeShims = new { version = expectedNativeVersion, manifestSha512 = native.Value.GetProperty("sha512").GetString(),
                packageSha256 = packageHash, deployedNativeSha256 = nativeHash, deployedNativePath = nativeFile,
                loadedNativePath = (string?)null, loadedNativePathUnavailableReason = "not observed in process" },
            machine = new { os = RuntimeInformation.OSDescription, architecture = RuntimeInformation.OSArchitecture,
                runtime = RuntimeInformation.FrameworkDescription }, fixtureSerial = Serial,
            lifecycleWarmups = 2, warmupKind = "fresh-child cold starts (not in-process JIT warmup)", lifecycleSamples = 10, noInputSamples = 1, timeoutMs = TimeoutMs, samples };
        await File.WriteAllTextAsync(filename, JsonSerializer.Serialize(dataset, Json));
        Console.WriteLine($"dataset: {filename}");
        return samples.Count == 13 && samples.Take(12).All(s => s.Classification == "completed") &&
            samples[12].Classification == "censored_timeout" ? 0 : 1;
    }

    internal static async Task<Sample> RunChildAsync(string scenario, int timeoutMs)
    {
        using var child = new Process();
        child.StartInfo = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        child.StartInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        child.StartInfo.ArgumentList.Add("--child");
        child.StartInfo.ArgumentList.Add(scenario);
        var watch = Stopwatch.StartNew();
        child.Start();
        var stdout = child.StandardOutput.ReadToEndAsync();
        var stderr = child.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(timeoutMs);
        var timedOut = false;
        try { await child.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        {
            timedOut = true;
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync();
        }
        var lines = (await stdout).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        _ = await stderr; // Do not persist native diagnostics, response bytes, or exception messages.
        string? phase = null;
        string? errorType = null;
        double? returned = null, terminal = null, lifecycle = null, open = null, ready = null, close = null, reopen = null;
        long? allocated = null;
        int? threads = null;
        double? cpu = null;
        foreach (var line in lines)
        {
            try
            {
                using var json = JsonDocument.Parse(line);
                var progress = json.RootElement;
                if (!progress.TryGetProperty("phase", out var next)) continue;
                phase = next.GetString();
                var ms = progress.GetProperty("ms").GetDouble();
                if (phase == "invocation_returned")
                {
                    returned = ms;
                    allocated = progress.GetProperty("allocatedBytes").GetInt64();
                    threads = progress.GetProperty("threadCount").GetInt32();
                    cpu = progress.GetProperty("processCpuMs").GetDouble();
                }
                if (phase == "task_terminal") terminal = ms;
                if (phase == "lifecycle_complete") lifecycle = ms;
                if (phase == "open_complete") open = ms;
                if (phase == "session_ready") ready = ms;
                if (phase == "first_close_complete") close = ms;
                if (phase == "reopen_complete") reopen = ms;
                if (phase is "invocation_threw" or "task_faulted" or "child_failed" &&
                    progress.TryGetProperty("errorType", out var reportedType) && reportedType.ValueKind == JsonValueKind.String)
                {
                    var name = reportedType.GetString();
                    errorType = name is not null && ExceptionTypeName.IsMatch(name) ? name : null;
                }
            }
            catch (JsonException) { }
            catch (InvalidOperationException) { }
            catch (KeyNotFoundException) { }
        }
        var classification = timedOut ? "censored_timeout" : child.ExitCode != 0 ? "child_failed" :
            terminal is not null && (scenario != "lifecycle" || lifecycle is not null) ? "completed" : "malformed_child_output";
        if (timedOut) errorType = null; // A killed child cannot confirm its eventual terminal cause.
        return new Sample(scenario, classification, phase, watch.Elapsed.TotalMilliseconds, returned, terminal,
            lifecycle, timedOut ? null : child.ExitCode, errorType, open, ready, close, reopen, allocated, threads, cpu);
    }

    private static void Progress(string phase, Stopwatch watch, string? errorType = null)
    {
        using var process = Process.GetCurrentProcess();
        Console.WriteLine(JsonSerializer.Serialize(new { phase, ms = watch.Elapsed.TotalMilliseconds, errorType,
            allocatedBytes = GC.GetTotalAllocatedBytes(false), threadCount = process.Threads.Count,
            processCpuMs = process.TotalProcessorTime.TotalMilliseconds }));
        Console.Out.Flush();
    }

    private static async Task<int> ChildAsync(string scenario)
    {
        if (scenario is "synthetic-prefix-block" or "synthetic-returned-pending" or "synthetic-complete" or
            "synthetic-sync-fault" or "synthetic-task-fault" or "synthetic-child-failed" or "synthetic-unsafe-type")
        {
            var synthetic = Stopwatch.StartNew();
            Progress("invocation_started", synthetic);
            if (scenario == "synthetic-prefix-block") await Task.Delay(Timeout.Infinite);
            if (scenario == "synthetic-sync-fault") { Progress("invocation_threw", synthetic, nameof(InvalidOperationException)); return 6; }
            if (scenario == "synthetic-child-failed") { Progress("child_failed", synthetic, nameof(InvalidOperationException)); return 6; }
            if (scenario == "synthetic-unsafe-type") { Progress("child_failed", synthetic, "IOException: private details"); return 6; }
            Progress("invocation_returned", synthetic);
            if (scenario == "synthetic-returned-pending") await Task.Delay(Timeout.Infinite);
            if (scenario == "synthetic-task-fault") { Progress("task_faulted", synthetic, nameof(InvalidOperationException)); return 6; }
            Progress("task_terminal", synthetic);
            return 0;
        }
        if (scenario is not ("lifecycle" or "no-input-return") || !OperatingSystem.IsMacOS()) return 2;
        Stopwatch? watch = null;
        string phase = "discovery";
        try
        {
            var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido, forceRescan: true);
            var matches = devices.Where(d => d.SerialNumber == Serial && d.SupportsConnection(ConnectionType.HidFido)).ToArray();
            if (matches.Length != 1) return 4;
            var device = matches[0];
            var lifecycle = Stopwatch.StartNew();
            phase = "open";
            await using (var connection = await device.ConnectAsync<IFidoHidConnection>())
            {
                Progress("open_complete", lifecycle);
                if (scenario == "no-input-return")
                {
                    watch = Stopwatch.StartNew();
                    phase = "invocation_started";
                    Progress(phase, watch);
                    Task<ReadOnlyMemory<byte>> pending;
                    try { pending = connection.ReceiveAsync(); }
                    catch (Exception exception) { Progress("invocation_threw", watch, exception.GetType().Name); return 6; }
                    phase = "invocation_returned";
                    Progress(phase, watch);
                    try { _ = await pending; }
                    catch (Exception exception) { Progress("task_faulted", watch, exception.GetType().Name); return 6; }
                    Progress("task_terminal", watch);
                }
                else
                {
                    phase = "session_create";
                    await using var session = await RawFidoHidSession.CreateAsync(connection);
                    Progress("session_ready", lifecycle);
                    watch = Stopwatch.StartNew();
                    phase = "invocation_started";
                    Progress(phase, watch);
                    Task<ReadOnlyMemory<byte>> pending;
                    try { pending = session.SendAndReceiveAsync(0x10, new byte[] { 0x04 }); }
                    catch (Exception exception) { Progress("invocation_threw", watch, exception.GetType().Name); return 6; }
                    phase = "invocation_returned";
                    Progress(phase, watch);
                    ReadOnlyMemory<byte> response;
                    try { response = await pending; }
                    catch (Exception exception) { Progress("task_faulted", watch, exception.GetType().Name); return 6; }
                    Progress("task_terminal", watch);
                    if (response.IsEmpty || response.Span[0] != 0) return 5;
                    phase = "first_close";
                }
            }
            if (scenario == "lifecycle")
            {
                Progress("first_close_complete", lifecycle);
                phase = "reopen";
                await using (var reopened = await device.ConnectAsync<IFidoHidConnection>())
                await using (var session = await RawFidoHidSession.CreateAsync(reopened))
                {
                    Progress("reopen_complete", lifecycle);
                    phase = "reopen_getinfo";
                    var response = await session.SendAndReceiveAsync(0x10, new byte[] { 0x04 });
                    if (response.IsEmpty || response.Span[0] != 0) return 5;
                    phase = "reopen_close";
                }
                Progress("lifecycle_complete", lifecycle);
            }
            return 0;
        }
        catch (Exception exception)
        {
            Progress(phase is "invocation_started" ? "invocation_threw" :
                phase is "invocation_returned" ? "task_faulted" : "child_failed",
                watch ?? Stopwatch.StartNew(), exception.GetType().Name);
            return 6;
        }
        finally { await YubiKeyManager.ShutdownAsync(); }
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static string HashText(string text) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));

    internal static void CheckPair(string beforePath, string afterPath)
    {
        using var before = JsonDocument.Parse(File.ReadAllText(beforePath));
        using var after = JsonDocument.Parse(File.ReadAllText(afterPath));
        var b = before.RootElement;
        var a = after.RootElement;
        static string Value(JsonElement element, string key) => element.GetProperty(key).GetString() ?? "";
        if (Value(b, "variant") != "before" || Value(a, "variant") != "after" ||
            Value(b, "sourceCommit") == Value(a, "sourceCommit") ||
            Value(b, "coreSha256") == Value(a, "coreSha256") ||
            Value(b.GetProperty("nativeShims"), "version") != Value(a.GetProperty("nativeShims"), "version") ||
            Value(b.GetProperty("nativeShims"), "packageSha256") != Value(a.GetProperty("nativeShims"), "packageSha256") ||
            Value(b.GetProperty("nativeShims"), "deployedNativeSha256") != Value(a.GetProperty("nativeShims"), "deployedNativeSha256") ||
            Value(b.GetProperty("machine"), "runtime") != Value(a.GetProperty("machine"), "runtime") ||
            Value(b.GetProperty("machine"), "os") != Value(a.GetProperty("machine"), "os") ||
            b.GetProperty("fixtureSerial").GetInt32() != a.GetProperty("fixtureSerial").GetInt32())
            throw new InvalidDataException("Mismatched comparison provenance");
        foreach (var dataset in new[] { b, a })
        {
            var samples = dataset.GetProperty("samples").EnumerateArray().ToArray();
            if (dataset.GetProperty("lifecycleWarmups").GetInt32() != 2 ||
                dataset.GetProperty("lifecycleSamples").GetInt32() != 10 || samples.Length < 3 ||
                samples.Take(samples.Length - 1).Any(s => s.GetProperty("scenario").GetString() != "lifecycle") ||
                samples[^1].GetProperty("scenario").GetString() != "no-input-return")
                throw new InvalidDataException("Incomplete comparison sample structure");
        }
        Console.WriteLine($"Compatible pair: {Path.GetFileName(beforePath)} / {Path.GetFileName(afterPath)} (distinct Core hashes; same native package, asset, runtime, OS, fixture)");
    }

    internal static void CheckPairSummary(string beforePath, string afterPath)
    {
        CheckPair(beforePath, afterPath);
        using var before = JsonDocument.Parse(File.ReadAllText(beforePath));
        using var after = JsonDocument.Parse(File.ReadAllText(afterPath));
        foreach (var dataset in new[] { before.RootElement, after.RootElement })
        {
            var measured = dataset.GetProperty("samples").EnumerateArray()
                .Skip(dataset.GetProperty("lifecycleWarmups").GetInt32())
                .Take(dataset.GetProperty("lifecycleSamples").GetInt32()).ToArray();
            var completed = measured.Where(s => s.GetProperty("classification").GetString() == "completed").ToArray();
            Console.WriteLine($"{dataset.GetProperty("variant").GetString()}: measured lifecycle {completed.Length}/{measured.Length} completed; no-input {dataset.GetProperty("samples").EnumerateArray().Last().GetProperty("classification").GetString()}");
            foreach (var field in new[] { "invocationReturnMs", "taskTerminalMs", "lifecycleCompleteMs" })
            {
                var values = completed.Select(s => s.GetProperty(field).GetDouble()).Order().ToArray();
                if (values.Length == 0) continue;
                var median = values.Length % 2 == 0 ? (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2 : values[values.Length / 2];
                var p95 = values[(int)Math.Ceiling(values.Length * 0.95) - 1];
                Console.WriteLine($"  {field}: median {median:F2} ms; nearest-rank p95 {p95:F2} ms (descriptive only, n={values.Length})");
            }
        }
    }

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
