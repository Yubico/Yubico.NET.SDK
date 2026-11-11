// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace WindowsHidRouteVerification;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args is ["--evidence-self-test"])
        {
            int childResult = EvidenceGateTests.Run();
            int publicationResult = PublicationSealTests.Run();
            return childResult == 0 && publicationResult == 0 ? 0 : 1;
        }
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("Windows native verification requires Windows; no native checks executed.");
            return 2;
        }
        if (args.Length != 3 || args[0] is not ("--native" or "--child"))
        {
            Console.Error.WriteLine("Usage: WindowsHidRouteVerification --native <jit|aot> <evidence-directory>");
            return 2;
        }
        Directory.CreateDirectory(args[2]);
        return args[0] == "--child" ? await ChildAsync(args[1], args[2]) : await RunAsync(args[1], args[2]);
    }

    private static async Task<int> ChildAsync(string scenario, string directory)
    {
        var evidence = new Evidence(scenario);
        Exception? failure = null;
        try { await NativeScenarios.RunAsync(scenario, evidence); }
        catch (Exception exception) { failure = exception; Console.Error.WriteLine(exception); }
        return evidence.Write(Path.Combine(directory, scenario + ".json"), failure) ? 0 : 1;
    }

    private static async Task<int> RunAsync(string kind, string directory)
    {
        File.Delete(Path.Combine(directory, "summary.json"));
        var evidence = new Evidence("native-summary");
        Exception? failure = null;
        try
        {
            if (!RequiredNativeChecks.ValidateScenarios(NativeScenarios.Names))
                throw new InvalidOperationException("Native scenario list differs from the independent required contract.");
            evidence.Check("requested-runtime-kind", kind is "jit" or "aot" && RuntimeFeature.IsDynamicCodeSupported == (kind == "jit"));
            foreach (string scenario in NativeScenarios.Names)
            {
                try { await RunChildAsync(scenario, directory, evidence); }
                catch (Exception exception)
                {
                    failure ??= exception;
                    evidence.Observe(scenario + "-failure", exception.ToString());
                    Console.Error.WriteLine(exception);
                }
            }
        }
        catch (Exception exception) { failure = exception; Console.Error.WriteLine(exception); }
        return evidence.Write(Path.Combine(directory, "summary.json"), failure) ? 0 : 1;
    }

    private static async Task RunChildAsync(string scenario, string directory, Evidence evidence)
    {
        string resultPath = Path.Combine(directory, scenario + ".json");
        File.Delete(resultPath); // A previous run must not satisfy this invocation.
        var start = new ProcessStartInfo(Environment.ProcessPath ?? throw new InvalidOperationException("Missing executable path."))
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("--child");
        start.ArgumentList.Add(scenario);
        start.ArgumentList.Add(Path.GetFullPath(directory));
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Child start failed.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await process.WaitForExitAsync(watchdog.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await SaveLogsAsync();
            throw new TimeoutException($"{scenario}: watchdog expired; process killed, NOT native drain or release proof.");
        }
        await SaveLogsAsync();
        evidence.Check(scenario + "-exit", process.ExitCode == 0);
        using JsonDocument result = JsonDocument.Parse(await File.ReadAllTextAsync(resultPath));
        evidence.Check(scenario + "-required-checks", RequiredNativeChecks.Validate(result.RootElement, scenario, RuntimeFeature.IsDynamicCodeSupported));

        async Task SaveLogsAsync()
        {
            await File.WriteAllTextAsync(Path.Combine(directory, scenario + ".stdout.log"), await stdout);
            await File.WriteAllTextAsync(Path.Combine(directory, scenario + ".stderr.log"), await stderr);
        }
    }
}