using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;

// One invocation per child. A killed process is censored: it says nothing about native cleanup.
internal static class AsyncBoundaryBaseline
{
    private const string Lifecycle = "lifecycle";
    private const string NoInput = "no-input-return";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string ProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../"));
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(ProjectDirectory, "../.."));
    private static readonly string SourceRoot = Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_SOURCE_ROOT") is { Length: > 0 } root
        ? Path.GetFullPath(root) : RepoRoot;

    internal sealed record Sample(
        string Scenario, string Classification, double ElapsedMs, double? InvocationReturnMs,
        double? TaskTerminalMs, double? NativeDurationMs, string NativeDurationUnavailableReason,
        int? NativeReportCount, string NativeReportCountUnavailableReason, int? ChildExitCode,
        string? FailureType, int? FixtureSerial, string? FirmwareVersion,
        double? LifecycleCompleteMs = null, string? Phase = null,
        long? ProcessAllocatedBytesAtReturn = null, int? ProcessThreadCountAtReturn = null);


    internal static async Task<int> RunAsync(string[] args)
    {
        if (args is ["--help"] or ["--list"] or ["--dry-run"])
        {
            Console.WriteLine("Scenarios: lifecycle (open, CTAP getInfo, dispose, reopen); no-input-return (raw receive without sending a request, watchdog-censored if pending). Pending: cancel-after-keepalive-and-recovery (needs operator-controlled keepalive); overlap-refusal-during-held-exchange (needs a real held exchange). No native durations or report counts are observed.");
            Console.WriteLine("Run: --async-boundary-baseline --fixture-serial <positive integer> --scenario lifecycle|no-input-return --samples 1|3|5 --timeout-ms 2000|5000|10000");
            Console.WriteLine("--list, --dry-run and --help do not discover or open devices. Only macOS is supported.");
            return 0;
        }

        if (args is not ["--fixture-serial", var serialText, "--scenario", var scenario, "--samples", var countText, "--timeout-ms", var timeoutText] ||
            !int.TryParse(serialText, out var serial) || serial <= 0 ||
            scenario is not (Lifecycle or NoInput) ||
            !int.TryParse(countText, out var count) || count is not (1 or 3 or 5) ||
            !int.TryParse(timeoutText, out var timeout) || timeout is not (2000 or 5000 or 10000) ||
            !OperatingSystem.IsMacOS())
        {
            Console.Error.WriteLine("Invalid or unsupported selection; use --async-boundary-baseline --help. No device accessed.");
            return 2;
        }

        var samples = new List<Sample>();
        for (var index = 0; index < count; index++)
        {
            var sample = await RunChildAsync(scenario, serial, timeout);
            samples.Add(sample);
            Console.WriteLine($"sample {index + 1}: {sample.Classification}");
        }

        var artifactDirectory = Path.Combine(RepoRoot, "artifacts", "measurements");
        Directory.CreateDirectory(artifactDirectory);
        var artifact = Path.Combine(artifactDirectory, $"async-boundary-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.json");
        await WriteDatasetAsync(artifact, scenario, serial, count, timeout, samples);
        Console.WriteLine($"dataset: {artifact}");
        return samples.All(s => s.Classification == "completed") ? 0 : 1;
    }

    internal static async Task WriteDatasetAsync(string artifact, string scenario, int serial, int count, int timeout, List<Sample> samples)
    {
        var deps = Path.Combine(AppContext.BaseDirectory, "Yubico.YubiKit.PerformanceBenchmarks.deps.json");
        using var depsJson = JsonDocument.Parse(await File.ReadAllTextAsync(deps));
        var nativeLibrary = depsJson.RootElement.GetProperty("libraries").EnumerateObject()
            .SingleOrDefault(entry => entry.Name.StartsWith("Yubico.NativeShims/", StringComparison.Ordinal));
        var version = nativeLibrary.Name?.Split('/')[1];
        var packagesRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrWhiteSpace(packagesRoot))
            packagesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var package = version is null ? null : Path.Combine(packagesRoot,
            "yubico.nativeshims", version, $"yubico.nativeshims.{version}.nupkg");
        var rid = RuntimeInformation.RuntimeIdentifier;
        var nativeFile = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native",
            OperatingSystem.IsMacOS() ? "libYubico.NativeShims.dylib" :
            OperatingSystem.IsWindows() ? "Yubico.NativeShims.dll" : "libYubico.NativeShims.so");
        var nativeAsset = $"runtimes/{rid}/native/{Path.GetFileName(nativeFile)}";
        var packageAvailable = package is { } existingPackage && File.Exists(existingPackage);
        bool? cachedNativeAssetMatchesDeployed = null;
        string? assetComparisonUnavailableReason = !packageAvailable ? "cached package unavailable" :
            !File.Exists(nativeFile) ? "deployed native binary unavailable" : null;
        if (assetComparisonUnavailableReason is null && package is { } availablePackage)
        {
            try
            {
                using var archive = ZipFile.OpenRead(availablePackage);
                var entry = archive.GetEntry(nativeAsset);
                if (entry is null)
                    assetComparisonUnavailableReason = "runtime native asset absent from cached package";
                else
                {
                    using var stream = entry.Open();
                    cachedNativeAssetMatchesDeployed = Convert.ToHexString(SHA256.HashData(stream)) ==
                        Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(nativeFile)));
                }
            }
            catch (InvalidDataException)
            {
                assetComparisonUnavailableReason = "cached package cannot be read as an archive";
            }
            catch (IOException)
            {
                assetComparisonUnavailableReason = "cached package native asset cannot be read";
            }
        }
        var diff = await GitAsync("diff", "HEAD", "--binary", "--", "Directory.Packages.props", "src");
        var untracked = await GitAsync("ls-files", "--others", "--exclude-standard", "--", "src");
        var dataset = new
        {
            schema = "yubikit-async-boundary-baseline/1",
            toolingVersion = "2",
            toolingFilesSha256 = new Dictionary<string, string>
            {
                ["Program.cs"] = Hash(Path.Combine(ProjectDirectory, "Program.cs")),
                ["AsyncBoundaryBaseline.cs"] = Hash(Path.Combine(ProjectDirectory, "AsyncBoundaryBaseline.cs")),
                ["BoundarySelfTest.cs"] = Hash(Path.Combine(ProjectDirectory, "BoundarySelfTest.cs"))
            },
            sourceCommit = await GitAsync("rev-parse", "HEAD"),
            sourceRoot = SourceRoot,
            coreAssemblySha256 = Hash(typeof(YubiKeyManager).Assembly.Location),
            workingTreeState = string.IsNullOrWhiteSpace(await GitAsync("status", "--porcelain")) ? "clean" : "dirty",
            shippingDiffSha256 = string.IsNullOrWhiteSpace(untracked) ? HashText(diff) : null,
            shippingDiffUnavailableReason = string.IsNullOrWhiteSpace(untracked) ? null : "untracked source files exist",
            nativeShims = new { version, versionUnavailableReason = version is null ? "not present in executing dependency manifest" : null,
                manifestSha512 = nativeLibrary.Name is null ? null : nativeLibrary.Value.GetProperty("sha512").GetString(),
                cachedPackageSha256 = package is { } cachedPackage && packageAvailable ? Hash(cachedPackage) : null,
                packageSha256 = package is { } verifiedPackage && cachedNativeAssetMatchesDeployed == true ? Hash(verifiedPackage) : null,
                packageHashUnavailableReason = !packageAvailable ? "resolved package file unavailable locally" :
                    cachedNativeAssetMatchesDeployed == true ? null : "cached package native asset not verified against deployed binary",
                cachedNativeAssetMatchesDeployed,
                assetComparisonUnavailableReason,
                deployedShimSha256 = File.Exists(nativeFile) ? Hash(nativeFile) : null,
                deployedShimHashUnavailableReason = File.Exists(nativeFile) ? null : "deployed native binary unavailable for runtime identifier",
                deployedShimPath = File.Exists(nativeFile) ? nativeAsset : null },
            machine = new { os = RuntimeInformation.OSDescription, architecture = RuntimeInformation.OSArchitecture,
                runtime = RuntimeInformation.FrameworkDescription },
            fixture = new { serial, firmwareVersion = samples.FirstOrDefault(s => s.FirmwareVersion is not null)?.FirmwareVersion,
                firmwareVersionUnavailableReason = "not read by this raw FIDO route" },
            sampleCount = count, timeoutMs = timeout, scenario, samples
        };
        await File.WriteAllTextAsync(artifact, JsonSerializer.Serialize(dataset, JsonOptions));
    }

    internal static async Task<Sample> RunChildAsync(string scenario, int? serial, int timeoutMs)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
        };
        process.StartInfo.ArgumentList.Add(typeof(AsyncBoundaryBaseline).Assembly.Location);
        process.StartInfo.ArgumentList.Add("--boundary-child");
        process.StartInfo.ArgumentList.Add(scenario);
        process.StartInfo.ArgumentList.Add(serial?.ToString() ?? "synthetic");
        var watch = Stopwatch.StartNew();
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(timeoutMs);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { } // Exited between HasExited and Kill; still censored.
            }
            await process.WaitForExitAsync();
            var progress = ReadProgress(await output);
            _ = await errors;
            return Empty(scenario, "censored_timeout", watch.Elapsed.TotalMilliseconds, null, serial) with
            {
                Phase = progress.Phase, InvocationReturnMs = progress.ReturnMs,
                FailureType = progress.ErrorType,
                ProcessAllocatedBytesAtReturn = progress.AllocatedBytes, ProcessThreadCountAtReturn = progress.ThreadCount
            };
        }

        var text = await output;
        _ = await errors; // Never persist native/SDK diagnostics or response payloads.
        var state = ReadProgress(text);
        if (process.ExitCode != 0)
            return Empty(scenario, "child_failed", watch.Elapsed.TotalMilliseconds, process.ExitCode, serial) with
            {
                Phase = state.Phase, InvocationReturnMs = state.ReturnMs,
                TaskTerminalMs = state.TerminalMs, FailureType = state.ErrorType,
                ProcessAllocatedBytesAtReturn = state.AllocatedBytes, ProcessThreadCountAtReturn = state.ThreadCount
            };
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return Empty(scenario, "malformed_child_output", watch.Elapsed.TotalMilliseconds, 0, serial);
            var sample = JsonSerializer.Deserialize<Sample>(text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last(), JsonOptions);
            if (sample is null || sample.Scenario != scenario || sample.Classification != "completed" ||
                sample.InvocationReturnMs is null || sample.TaskTerminalMs is null ||
                sample.InvocationReturnMs < 0 || sample.TaskTerminalMs < sample.InvocationReturnMs ||
                state.Phase != "invocation_returned" || state.ReturnMs is null ||
                sample.NativeDurationMs is not null || sample.NativeReportCount is not null ||
                (scenario == Lifecycle && sample.LifecycleCompleteMs is null) ||
                (serial is not null && sample.FixtureSerial != serial))
                return Empty(scenario, "malformed_child_output", watch.Elapsed.TotalMilliseconds, 0, serial);
            return sample with { ElapsedMs = watch.Elapsed.TotalMilliseconds, ChildExitCode = 0,
                Phase = state.Phase, ProcessAllocatedBytesAtReturn = state.AllocatedBytes,
                ProcessThreadCountAtReturn = state.ThreadCount };
        }
        catch (JsonException)
        {
            return Empty(scenario, "malformed_child_output", watch.Elapsed.TotalMilliseconds, 0, serial);
        }
    }

    private static (string? Phase, double? ReturnMs, double? TerminalMs, string? ErrorType,
        long? AllocatedBytes, int? ThreadCount) ReadProgress(string output)
    {
        string? phase = null;
        double? returned = null;
        double? terminal = null;
        string? error = null;
        long? allocated = null;
        int? threads = null;
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                using var json = JsonDocument.Parse(line);
                var root = json.RootElement;
                if (!root.TryGetProperty("kind", out var kind) || kind.GetString() != "progress" ||
                    !root.TryGetProperty("phase", out var phaseValue)) continue;
                var next = phaseValue.GetString();
                if (next == "invocation_started" && phase is null) phase = next;
                else if (next == "invocation_returned" && phase == "invocation_started")
                {
                    phase = next;
                    returned = root.GetProperty("durationMs").GetDouble();
                    allocated = root.GetProperty("processAllocatedBytes").GetInt64();
                    threads = root.GetProperty("processThreadCount").GetInt32();
                }
                else if (next is "invocation_threw" or "task_faulted" && phase is not null)
                {
                    phase = next;
                    error = root.GetProperty("errorType").GetString();
                    if (next == "task_faulted" && returned is not null)
                        terminal = root.GetProperty("durationMs").GetDouble();
                }
            }
            catch (JsonException) { } // Malformed progress never creates a successful sample.
            catch (InvalidOperationException) { }
            catch (KeyNotFoundException) { }
        }
        return (phase, returned, terminal, error, allocated, threads);
    }

    private static void EmitProgress(string phase, Stopwatch watch, string? errorType = null, double? durationMs = null)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { kind = "progress", phase, durationMs = durationMs ?? watch.Elapsed.TotalMilliseconds,
            errorType, processAllocatedBytes = GC.GetTotalAllocatedBytes(false),
            processThreadCount = Process.GetCurrentProcess().Threads.Count }));
        Console.Out.Flush();
    }

    internal static async Task<int> ChildAsync(string[] args)
    {
        if (args is not [var scenario, var serialText]) return 2;
        if (scenario == "synthetic-malformed") { Console.WriteLine("not JSON"); return 0; }
        if (scenario == "synthetic-failure") return 3;
        if (scenario is not ("synthetic-complete" or "synthetic-prefix-block" or "synthetic-returned-pending" or
            "synthetic-sync-throw" or "synthetic-task-fault" or Lifecycle or NoInput)) return 2;
        if (!scenario.StartsWith("synthetic-", StringComparison.Ordinal) &&
            (!OperatingSystem.IsMacOS() || !int.TryParse(serialText, out var selected) || selected <= 0)) return 2;

        try
        {
            var watch = Stopwatch.StartNew();
            double returned;
            double terminal;
            int? serial = null;
            double? lifecycleComplete = null;
            if (scenario.StartsWith("synthetic-", StringComparison.Ordinal))
            {
                EmitProgress("invocation_started", watch);
                if (scenario == "synthetic-prefix-block")
                {
                    using var blocker = new ManualResetEventSlim();
                    blocker.Wait();
                    return 7;
                }
                if (scenario == "synthetic-sync-throw")
                {
                    EmitProgress("invocation_threw", watch, nameof(InvalidOperationException));
                    return 6;
                }
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var task = gate.Task;
                returned = watch.Elapsed.TotalMilliseconds;
                EmitProgress("invocation_returned", watch, durationMs: returned);
                if (scenario == "synthetic-task-fault")
                {
                    gate.SetException(new InvalidOperationException());
                    try { await task; }
                    catch (InvalidOperationException)
                    {
                        EmitProgress("task_faulted", watch, nameof(InvalidOperationException));
                        return 6;
                    }
                }
                if (scenario == "synthetic-returned-pending") await task;
                gate.SetResult();
                await task;
                terminal = watch.Elapsed.TotalMilliseconds;
            }
            else
            {
                serial = int.Parse(serialText);
                // Discovery can itself open connections for metadata; the process watchdog bounds this phase too.
                var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido, forceRescan: true);
                var matches = devices.Where(d => d.SerialNumber == serial && d.SupportsConnection(ConnectionType.HidFido)).ToArray();
                if (matches.Length != 1) return 4; // Unknown/ambiguous identity is not a fixture match.
                var device = matches[0];
                var lifecycleWatch = Stopwatch.StartNew();
                await using (var connection = await device.ConnectAsync<IFidoHidConnection>())
                {
                    if (scenario == NoInput)
                    {
                        // No session and no request: measure a single raw receive. Timeout is not a safe abort.
                        var operationWatch = Stopwatch.StartNew();
                        EmitProgress("invocation_started", operationWatch);
                        Task<ReadOnlyMemory<byte>> pending;
                        try { pending = connection.ReceiveAsync(); }
                        catch (Exception exception)
                        {
                            EmitProgress("invocation_threw", operationWatch, exception.GetType().Name);
                            return 6;
                        }
                        returned = operationWatch.Elapsed.TotalMilliseconds;
                        EmitProgress("invocation_returned", operationWatch, durationMs: returned);
                        try { _ = await pending; }
                        catch (Exception exception)
                        {
                            EmitProgress("task_faulted", operationWatch, exception.GetType().Name);
                            return 6;
                        }
                        terminal = operationWatch.Elapsed.TotalMilliseconds;
                    }
                    else
                    {
                        await using var session = await RawFidoHidSession.CreateAsync(connection);
                        // CTAPHID_CBOR 0x10; authenticatorGetInfo 0x04, a non-mutating command.
                        var operationWatch = Stopwatch.StartNew();
                        EmitProgress("invocation_started", operationWatch);
                        Task<ReadOnlyMemory<byte>> task;
                        try { task = session.SendAndReceiveAsync(0x10, new byte[] { 0x04 }); }
                        catch (Exception exception)
                        {
                            EmitProgress("invocation_threw", operationWatch, exception.GetType().Name);
                            return 6;
                        }
                        returned = operationWatch.Elapsed.TotalMilliseconds;
                        EmitProgress("invocation_returned", operationWatch, durationMs: returned);
                        ReadOnlyMemory<byte> response;
                        try { response = await task; }
                        catch (Exception exception)
                        {
                            EmitProgress("task_faulted", operationWatch, exception.GetType().Name);
                            return 6;
                        }
                        terminal = operationWatch.Elapsed.TotalMilliseconds;
                        if (response.IsEmpty || response.Span[0] != 0) return 5;
                    }
                }

                if (scenario == Lifecycle)
                {
                    await using (var reopened = await device.ConnectAsync<IFidoHidConnection>())
                    await using (var reopenedSession = await RawFidoHidSession.CreateAsync(reopened))
                    {
                        var response = await reopenedSession.SendAndReceiveAsync(0x10, new byte[] { 0x04 });
                        if (response.IsEmpty || response.Span[0] != 0) return 5;
                    }
                    lifecycleComplete = lifecycleWatch.Elapsed.TotalMilliseconds;
                }
            }

            Console.WriteLine(JsonSerializer.Serialize(new Sample(scenario, "completed", watch.Elapsed.TotalMilliseconds,
                returned, terminal, null, "not exposed by public raw session", null,
                "not exposed by public raw session", 0, null, serial, null, lifecycleComplete),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            return 0;
        }
        catch (Exception exception)
        {
            // No exception message or device bytes in the dataset; exit code denotes failure.
            Console.Error.WriteLine(exception.GetType().Name);
            return 6;
        }
        finally
        {
            if (!scenario.StartsWith("synthetic-", StringComparison.Ordinal)) await YubiKeyManager.ShutdownAsync();
        }
    }

    private static Sample Empty(string scenario, string classification, double elapsed, int? exit, int? serial) =>
        new(scenario, classification, elapsed, null, null, null, "not observed", null, "not observed",
            exit, null, serial, null);

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string HashText(string text) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static async Task<string> GitAsync(params string[] arguments)
    {
        using var git = new Process();
        git.StartInfo = new ProcessStartInfo("git") { WorkingDirectory = SourceRoot, RedirectStandardOutput = true };
        foreach (var argument in arguments) git.StartInfo.ArgumentList.Add(argument);
        git.Start();
        var output = await git.StandardOutput.ReadToEndAsync();
        await git.WaitForExitAsync();
        if (git.ExitCode != 0) throw new InvalidOperationException("Unable to record source provenance");
        return output.Trim();
    }
}
