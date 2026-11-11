// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Text.Json;

namespace WindowsHidRouteVerification;

internal static class PublicationSealTests
{
    internal static int Run()
    {
        string root = Path.Combine("artifacts", "w1-reviewfix", "seal-tests", Guid.NewGuid().ToString("N")).Replace('\\', '/');
        Directory.CreateDirectory(root + "/jit");
        Directory.CreateDirectory(root + "/aot");
        File.WriteAllText(root + "/source.cs", "original source");
        File.WriteAllText(root + "/jit/host.exe", "original JIT fixture");
        File.WriteAllText(root + "/jit/runtime.dll", "runtime fixture");
        File.WriteAllText(root + "/aot/host.exe", "original AOT fixture");
        string manifest = root + "/publish.json";
        WriteSeal(manifest, root);
        int failures = 0;
        int executed = 0;
        Check("sealed-fixture", Validate());
        DateTime timestamp = File.GetLastWriteTimeUtc(root + "/source.cs");
        File.WriteAllText(root + "/source.cs", "mutated source");
        File.SetLastWriteTimeUtc(root + "/source.cs", timestamp);
        Check("timestamp-preserved-source-drift", !Validate());
        File.WriteAllText(root + "/source.cs", "original source");
        File.WriteAllText(root + "/jit/host.exe", "mutated JIT fixture");
        Check("artifact-mutation-before-execution", !Validate());
        File.WriteAllText(root + "/jit/host.exe", "original JIT fixture");
        File.Move(root + "/jit/runtime.dll", root + "/saved-runtime.dll");
        Check("missing-artifact", !Validate());
        File.Move(root + "/saved-runtime.dll", root + "/jit/runtime.dll");
        File.WriteAllText(root + "/aot/extra.dll", "unexpected artifact");
        Check("extra-artifact", !Validate());
        File.Delete(root + "/aot/extra.dll");
        Check("stale-source-revision", !Validate("different-head"));
        Check("source-status-drift", !Validate(status: " M source.cs"));
        string original = File.ReadAllText(manifest);
        File.WriteAllText(manifest, original.Replace("original-head", "stale-head", StringComparison.Ordinal));
        Check("loaded-stale-manifest", !Validate());
        File.WriteAllText(manifest, original);
        using JsonDocument seal = JsonDocument.Parse(original);
        RuntimeChecks(seal.RootElement.GetProperty("Jit"), root + "/jit", Check);
        ResetChecks(root, Check);
        Console.WriteLine($"Portable publication gates: {executed} executed, {failures} failed; synthetic files only. Fixtures: {root}");
        return failures == 0 ? 0 : 1;

        bool Validate(string commit = "original-head", string status = "")
        {
            using JsonDocument loaded = JsonDocument.Parse(File.ReadAllText(manifest));
            return PublicationSeal.Validate(loaded.RootElement, PublicationSeal.HashFiles([root + "/source.cs"]),
                HashTree(root + "/jit"), HashTree(root + "/aot"), commit, status);
        }

        void Check(string name, bool passed)
        {
            executed++;
            Console.WriteLine($"{(passed ? "PASS" : "FAIL")} publication-gate/{name}");
            if (!passed) failures++;
        }
    }

    private static void RuntimeChecks(JsonElement sealedFiles, string output, Action<string, bool> check)
    {
        string hashes = sealedFiles.GetRawText();
        string relative = hashes.Replace(output + "/", "", StringComparison.Ordinal);
        Check("runtime-relative-paths", relative, "jit", true);
        Check("runtime-sealed-prefix", hashes, "jit", true);
        Check("runtime-slash-normalization", hashes.Replace("/", "\\\\", StringComparison.Ordinal), "jit", true);
        Check("runtime-hash-mismatch", relative.Replace(sealedFiles.EnumerateObject().First().Value.GetString() ?? "", new string('0', 64), StringComparison.Ordinal), "jit", false);
        Check("runtime-missing-artifacts", "{}", "jit", false);
        Check("runtime-extra-artifact", relative.Replace("{", "{\"extra\":\"hash\",", StringComparison.Ordinal), "jit", false);
        Check("runtime-duplicate-artifact", relative.Replace("{", "{\"host.exe\":\"hash\",", StringComparison.Ordinal), "jit", false);
        Check("runtime-kind-mismatch", relative, "aot", false);
        using JsonDocument aotSummary = JsonDocument.Parse(EvidenceGateTests.Summary(relative).Replace("\"DynamicCode\":true", "\"DynamicCode\":false", StringComparison.Ordinal));
        check("aot-runtime-identity", PublicationSeal.ValidateRuntime(aotSummary.RootElement, sealedFiles, "aot", output));

        void Check(string name, string map, string kind, bool expected)
        {
            using JsonDocument summary = JsonDocument.Parse(EvidenceGateTests.Summary(map));
            check(name, PublicationSeal.ValidateRuntime(summary.RootElement, sealedFiles, kind, output) == expected);
        }
    }

    private static void ResetChecks(string root, Action<string, bool> check)
    {
        foreach (string kind in new[] { "jit", "aot", "unowned" })
        {
            Directory.CreateDirectory($"{root}/build/{kind}/obj");
            File.WriteAllText($"{root}/build/{kind}/obj/newer-than-source.dll", "stale intermediate");
            File.SetLastWriteTimeUtc($"{root}/build/{kind}/obj/newer-than-source.dll", DateTime.UtcNow.AddDays(1));
        }
        PublicationSeal.ResetBuild(root);
        check("fresh-intermediates-for-preserved-source-time", !Directory.Exists(root + "/build/jit") && !Directory.Exists(root + "/build/aot"));
        check("cleanup-only-owned-intermediates", File.Exists(root + "/build/unowned/obj/newer-than-source.dll") && File.Exists(root + "/jit/host.exe"));
    }

    private static SortedDictionary<string, string> HashTree(string path) =>
        PublicationSeal.HashFiles(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories));

    private static void WriteSeal(string path, string root)
    {
        using FileStream stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WriteNumber("Version", 1);
        writer.WriteString("SourceCommit", "original-head");
        writer.WriteString("SourceStatus", "");
        WriteMap("Inputs", PublicationSeal.HashFiles([root + "/source.cs"]));
        WriteMap("Jit", HashTree(root + "/jit"));
        WriteMap("Aot", HashTree(root + "/aot"));
        writer.WriteEndObject();

        void WriteMap(string name, IEnumerable<KeyValuePair<string, string>> hashes)
        {
            writer.WriteStartObject(name);
            foreach ((string file, string hash) in hashes) writer.WriteString(file, hash);
            writer.WriteEndObject();
        }
    }
}