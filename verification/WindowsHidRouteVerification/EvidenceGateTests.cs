// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Text.Json;

namespace WindowsHidRouteVerification;

internal static class EvidenceGateTests
{
    internal static int Run()
    {
        const string valid = """
            {"Scenario":"metadata-default","DynamicCode":true,"Status":"PASSED","Checks":{
            "ordinary-construction-queries-hid-metadata":true,"metadata-failure-handle-released":true}}
            """;
        (string Name, string Json, bool Expected)[] cases =
        [
            ("complete-child", valid, true),
            ("missing-check", valid.Replace(",\"metadata-failure-handle-released\":true", "", StringComparison.Ordinal), false),
            ("duplicate-check", valid.Replace("\"Checks\":{", "\"Checks\":{\"metadata-failure-handle-released\":true,", StringComparison.Ordinal), false),
            ("wrong-scenario", valid.Replace("metadata-default", "output", StringComparison.Ordinal), false),
            ("wrong-runtime", valid.Replace("\"DynamicCode\":true", "\"DynamicCode\":false", StringComparison.Ordinal), false),
            ("false-check", valid.Replace("released\":true", "released\":false", StringComparison.Ordinal), false),
            ("unexpected-check", valid.Replace("\"Checks\":{", "\"Checks\":{\"unexpected\":true,", StringComparison.Ordinal), false),
            ("empty-checks", """{"Scenario":"metadata-default","DynamicCode":true,"Status":"PASSED","Checks":{}}""", false),
            ("failed-status", valid.Replace("PASSED", "FAILED", StringComparison.Ordinal), false)
        ];
        int failures = 0;
        foreach (var item in cases)
        {
            using JsonDocument document = JsonDocument.Parse(item.Json);
            bool passed = RequiredNativeChecks.Validate(document.RootElement, "metadata-default", true) == item.Expected;
            Console.WriteLine($"{(passed ? "PASS" : "FAIL")} evidence-gate/{item.Name}");
            if (!passed) failures++;
        }
        int executed = cases.Length;
        Check("required-scenarios", RequiredNativeChecks.ValidateScenarios(NativeScenarios.Names));
        Check("missing-scenario", !RequiredNativeChecks.ValidateScenarios(NativeScenarios.Names.Skip(1)));
        Check("empty-scenarios", !RequiredNativeChecks.ValidateScenarios([]));
        Check("duplicate-scenario", !RequiredNativeChecks.ValidateScenarios(NativeScenarios.Names.Append("output")));
        Check("unexpected-scenario", !RequiredNativeChecks.ValidateScenarios(NativeScenarios.Names.Append("unexpected")));
        using JsonDocument summary = JsonDocument.Parse(Summary());
        Check("complete-summary", RequiredNativeChecks.Validate(summary.RootElement, "native-summary", true));
        using JsonDocument incomplete = JsonDocument.Parse(Summary().Replace("\"output-exit\":true,", "", StringComparison.Ordinal));
        Check("missing-summary-check", !RequiredNativeChecks.Validate(incomplete.RootElement, "native-summary", true));
        using JsonDocument zero = JsonDocument.Parse("""{"Scenario":"native-summary","DynamicCode":true,"Status":"PASSED","Checks":{"requested-runtime-kind":true}}""");
        Check("zero-native-summary", !RequiredNativeChecks.Validate(zero.RootElement, "native-summary", true));
        string directory = Path.Combine("artifacts", "w1-reviewfix", "writer-tests");
        Directory.CreateDirectory(directory);
        var evidence = new Evidence("native-summary");
        evidence.Observe("fixture-kind", "Portable writer regression test; NOT native-runtime evidence.");
        evidence.Check("requested-runtime-kind", true);
        string path = Path.Combine(directory, "incomplete-summary.json");
        Check("writer-incomplete-exit", !evidence.Write(path, null));
        using JsonDocument written = JsonDocument.Parse(File.ReadAllText(path));
        Check("writer-incomplete-status", written.RootElement.GetProperty("Status").ValueEquals("FAILED"));
        var child = new Evidence("metadata-default");
        child.Observe("fixture-kind", "Portable writer regression test; NOT native-runtime evidence.");
        child.Check("ordinary-construction-queries-hid-metadata", true);
        Check("writer-incomplete-child", !child.Write(Path.Combine(directory, "incomplete-child.json"), null));
        child.Check("metadata-failure-handle-released", true);
        Check("writer-complete-child", child.Write(Path.Combine(directory, "complete-child.json"), null));
        Console.WriteLine($"Portable child evidence gates: {executed} executed, {failures} failed; no native APIs invoked.");
        return failures == 0 ? 0 : 1;

        void Check(string name, bool passed)
        {
            executed++;
            Console.WriteLine($"{(passed ? "PASS" : "FAIL")} evidence-gate/{name}");
            if (!passed) failures++;
        }
    }

    internal static string Summary(string hashes = "{}") =>
        "{\"Scenario\":\"native-summary\",\"DynamicCode\":true,\"Status\":\"PASSED\",\"Checks\":{" +
        string.Join(",", NativeScenarios.Names.SelectMany(name => new[] { $"\"{name}-exit\":true", $"\"{name}-required-checks\":true" })) +
        ",\"requested-runtime-kind\":true},\"PublishedFileSha256\":" + hashes + "}";
}