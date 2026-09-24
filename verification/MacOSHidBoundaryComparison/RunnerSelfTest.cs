using System.Text.Json;

internal static class RunnerSelfTest
{
    internal static async Task<int> RunAsync()
    {
        using var deps = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "MacOSHidBoundaryComparison.deps.json")));
        var nativeVersion = Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_NATIVE_VERSION") ?? "1.18.1-async.3";
        if (!deps.RootElement.GetProperty("libraries").TryGetProperty($"Yubico.NativeShims/{nativeVersion}", out _))
            throw new InvalidOperationException("Comparison runner must deploy the baseline native package");
        var prefix = await Program.RunChildAsync("synthetic-prefix-block", 2000);
        var pending = await Program.RunChildAsync("synthetic-returned-pending", 2000);
        var complete = await Program.RunChildAsync("synthetic-complete", 2000);
        var taskFault = await Program.RunChildAsync("synthetic-task-fault", 2000);
        var syncFault = await Program.RunChildAsync("synthetic-sync-fault", 2000);
        var unsafeType = await Program.RunChildAsync("synthetic-unsafe-type", 2000);
        if (!CurrentProfile.NativeVersionMatches("1.18.1-async.7", "1.18.1-async.7") ||
            CurrentProfile.NativeVersionMatches("1.18.1-async.7", "1.18.1-async.3"))
            throw new InvalidOperationException("Profile accepted a mismatched deployed native dependency");
        if (prefix.Classification != "censored_timeout" || prefix.Phase != "invocation_started" ||
            prefix.InvocationReturnMs is not null || pending.Classification != "censored_timeout" ||
            pending.Phase != "invocation_returned" || pending.InvocationReturnMs is null ||
            complete.Classification != "completed" || complete.TaskTerminalMs is null ||
            complete.AllocatedBytesAtReturn is null || complete.ThreadCountAtReturn is null ||
            complete.ProcessCpuMsAtReturn is null ||
            pending.ErrorType is not null ||
            taskFault.Classification != "child_failed" || taskFault.Phase != "task_faulted" ||
            taskFault.ErrorType != nameof(InvalidOperationException) || taskFault.InvocationReturnMs is null ||
            syncFault.Phase != "invocation_threw" || syncFault.ErrorType != nameof(InvalidOperationException) ||
            unsafeType.ErrorType is not null)
            throw new InvalidOperationException("Child watchdog/typed-fault contract failed");
        var source = Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_RUNNER_ROOT")
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var measurements = Path.Combine(source, "artifacts", "measurements");
        var before1 = Path.Combine(measurements, "comparison-before-20260923T212455107Z.json");
        var before2 = Path.Combine(measurements, "comparison-before-20260923T212640783Z.json");
        var after1 = Path.Combine(measurements, "comparison-after-20260923T212552013Z.json");
        var after2 = Path.Combine(measurements, "comparison-after-20260923T212725515Z.json");
        if (File.Exists(before1) && File.Exists(after1) && File.Exists(before2) && File.Exists(after2))
        {
            Program.CheckPairSummary(before1, after1);
            Program.CheckPairSummary(before2, after2);
            try
            {
                Program.CheckPair(before1, before2);
                throw new InvalidOperationException("Identical Core binaries were accepted as a comparison");
            }
            catch (InvalidDataException) { }
        }
        else Console.WriteLine("Historical datasets unavailable; pair checks not run");
        Console.WriteLine("Comparison self-test: 6 passed, 0 failed");
        return 0;
    }
}
