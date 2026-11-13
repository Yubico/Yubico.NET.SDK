// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace WindowsHidRouteVerification;

internal sealed class Evidence(string scenario)
{
    private readonly Dictionary<string, bool> _checks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _observations = new(StringComparer.Ordinal);

    internal void Check(string name, bool passed)
    {
        _checks.Add(name, passed);
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {scenario}/{name}");
        if (!passed) throw new InvalidOperationException($"Check failed: {scenario}/{name}");
    }

    internal void Observe(string name, string value) => _observations.Add(name, value);

    internal bool Write(string path, Exception? failure)
    {
        bool passed = failure is null && RequiredNativeChecks.Complete(scenario, _checks);
        using FileStream stream = File.Create(path);
        using var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        json.WriteStartObject();
        json.WriteString("Scenario", scenario);
        json.WriteString("Status", passed ? "PASSED" : "FAILED");
        json.WriteString("Failure", failure?.ToString());
        json.WriteBoolean("DynamicCode", RuntimeFeature.IsDynamicCodeSupported);
        json.WriteString("Runtime", RuntimeInformation.FrameworkDescription);
        json.WriteString("OS", RuntimeInformation.OSDescription);
        json.WriteString("Architecture", RuntimeInformation.ProcessArchitecture.ToString());
        json.WriteString("Host", Environment.ProcessPath);
        json.WriteString("Machine", Environment.MachineName);
        json.WriteString("Utc", DateTimeOffset.UtcNow);
        json.WriteString("ThreadPoolEnvironment", Environment.GetEnvironmentVariable("DOTNET_ThreadPool_UseWindowsThreadPool"));
        bool switchPresent = AppContext.TryGetSwitch("System.Threading.ThreadPool.UseWindowsThreadPool", out bool windowsPool);
        json.WriteBoolean("WindowsThreadPoolSwitchPresent", switchPresent);
        json.WriteBoolean("WindowsThreadPoolSwitchValue", windowsPool);
        json.WriteString("CoreIdentity", typeof(Yubico.YubiKit.Core.Transports.Hid.Windows.WindowsHidReportApi).Assembly.FullName);
        json.WriteString("EvidenceScope", "Real Windows kernel/BCL overlapped byte-mode pipe; synthetic metadata; NOT HID-driver or hardware evidence.");
        json.WriteString("MissingCoverage", "Raw immediate versus ERROR_IO_PENDING return is normalized by production InterpretSubmission; empty-peer pending and data-ready candidate are observed, raw immediate success is not attested. No BCL allocator-failure injection. No arbitrary hung-native recovery claim.");
        json.WriteStartObject("Checks");
        foreach ((string name, bool checkPassed) in _checks) json.WriteBoolean(name, checkPassed);
        json.WriteEndObject();
        json.WriteStartObject("Observations");
        foreach ((string name, string value) in _observations) json.WriteString(name, value);
        json.WriteEndObject();
        json.WriteStartObject("PublishedFileSha256");
        foreach (string file in Directory.EnumerateFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            using FileStream input = File.OpenRead(file);
            json.WriteString(Path.GetRelativePath(AppContext.BaseDirectory, file), Convert.ToHexString(SHA256.HashData(input)));
        }
        json.WriteEndObject();
        json.WriteEndObject();
        return passed;
    }
}