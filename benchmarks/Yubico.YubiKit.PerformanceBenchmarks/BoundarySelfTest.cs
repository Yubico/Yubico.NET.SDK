using System.Text.Json;

internal static class BoundarySelfTest
{
    public static async Task<int> RunAsync()
    {
        var passed = 0;
        var samples = new List<AsyncBoundaryBaseline.Sample>();
        foreach (var (scenario, expected) in new[]
        {
            ("synthetic-complete", "completed"),
            ("synthetic-prefix-block", "censored_timeout"),
            ("synthetic-returned-pending", "censored_timeout"),
            ("synthetic-sync-throw", "child_failed"),
            ("synthetic-task-fault", "child_failed"),
            ("synthetic-malformed", "malformed_child_output"),
            ("synthetic-failure", "child_failed")
        })
        {
            var result = await AsyncBoundaryBaseline.RunChildAsync(scenario, null, 2000);
            samples.Add(result);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(result,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            var root = document.RootElement;
            if (root.GetProperty("classification").GetString() != expected ||
                root.GetProperty("elapsedMs").GetDouble() < 0 ||
                (scenario is "synthetic-complete" or "synthetic-returned-pending" or "synthetic-task-fault") !=
                    (root.GetProperty("invocationReturnMs").ValueKind == JsonValueKind.Number) ||
                (scenario is "synthetic-complete" or "synthetic-task-fault") !=
                    (root.GetProperty("taskTerminalMs").ValueKind == JsonValueKind.Number) ||
                root.GetProperty("nativeDurationMs").ValueKind != JsonValueKind.Null)
                throw new InvalidOperationException($"Self-test failed: {scenario}: {JsonSerializer.Serialize(result)}");
            if (expected == "censored_timeout" && result.ChildExitCode is not null ||
                expected == "child_failed" && result.ChildExitCode is not > 0 ||
                expected == "malformed_child_output" && result.ChildExitCode != 0)
                throw new InvalidOperationException($"Incorrect exit classification: {scenario}");
            if (scenario == "synthetic-prefix-block" && result.Phase != "invocation_started" ||
                scenario == "synthetic-returned-pending" && result.Phase != "invocation_returned")
                throw new InvalidOperationException($"Incorrect progress classification: {scenario}");
            if (scenario == "synthetic-sync-throw" && (result.Phase != "invocation_threw" ||
                result.InvocationReturnMs is not null || result.FailureType != "InvalidOperationException") ||
                scenario == "synthetic-task-fault" && (result.Phase != "task_faulted" ||
                result.TaskTerminalMs is null || result.FailureType != "InvalidOperationException"))
                throw new InvalidOperationException($"Incorrect exception phase: {scenario}");
            passed++;
        }

        var artifact = Path.GetTempFileName();
        try
        {
            await AsyncBoundaryBaseline.WriteDatasetAsync(artifact, "synthetic", 1, samples.Count, 2000, samples);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(artifact));
            var root = document.RootElement;
            var expectedSourceRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("YUBIKIT_BOUNDARY_SOURCE_ROOT") ??
                Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            if (root.GetProperty("schema").GetString() != "yubikit-async-boundary-baseline/1" ||
                root.GetProperty("samples").GetArrayLength() != 7 ||
                root.GetProperty("samples")[2].GetProperty("taskTerminalMs").ValueKind != JsonValueKind.Null ||
                root.GetProperty("samples")[2].GetProperty("invocationReturnMs").ValueKind != JsonValueKind.Number ||
                root.GetProperty("samples")[2].GetProperty("classification").GetString() != "censored_timeout" ||
                root.GetProperty("sourceCommit").GetString() is not { Length: 40 } ||
                root.GetProperty("sourceRoot").GetString() != expectedSourceRoot ||
                root.GetProperty("coreAssemblySha256").GetString() is not { Length: 64 } ||
                root.GetProperty("shippingDiffSha256").GetString() is not { Length: 64 } ||
                root.GetProperty("nativeShims").GetProperty("version").GetString() is not { Length: > 0 } ||
                root.GetProperty("nativeShims").GetProperty("manifestSha512").GetString() is not { Length: > 40 } ||
                root.GetProperty("nativeShims").GetProperty("cachedPackageSha256").GetString() is not { Length: 64 } ||
                root.GetProperty("nativeShims").GetProperty("cachedNativeAssetMatchesDeployed").ValueKind != JsonValueKind.True ||
                root.GetProperty("nativeShims").GetProperty("packageSha256").GetString() !=
                    root.GetProperty("nativeShims").GetProperty("cachedPackageSha256").GetString() ||
                root.GetProperty("nativeShims").GetProperty("deployedShimSha256").GetString() is not { Length: 64 } ||
                root.GetProperty("toolingFilesSha256").GetProperty("AsyncBoundaryBaseline.cs").GetString() is not { Length: 64 })
                throw new InvalidOperationException("Self-test failed: dataset schema/provenance/censored sample");
            passed++;
        }
        finally
        {
            File.Delete(artifact);
        }

        Console.WriteLine($"Boundary self-test: {passed} passed, 0 failed");
        return 0;
    }
}
