// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Security.Cryptography;
using System.Text.Json;

namespace WindowsHidRouteVerification;

internal static class PublicationSeal
{
    internal static void ResetBuild(string root)
    {
        // Only this verifier's two owned intermediate trees. Do this before either restore.
        foreach (string kind in new[] { "jit", "aot" })
        {
            string directory = Path.Combine(root, "build", kind);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    internal static SortedDictionary<string, string> HashFiles(IEnumerable<string> files)
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in files)
        {
            using FileStream stream = File.OpenRead(file);
            hashes.Add(file.Replace('\\', '/'), Convert.ToHexString(SHA256.HashData(stream)));
        }
        return hashes;
    }

    internal static bool Matches(JsonElement expected, IEnumerable<KeyValuePair<string, string>> actual, string prefix = "")
    {
        if (expected.ValueKind != JsonValueKind.Object) return false;
        var sealedFiles = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty property in expected.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String ||
                !sealedFiles.TryAdd(Normalize(property.Name, prefix), property.Value.GetString() ?? "")) return false;
        }
        var observedFiles = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string name, string hash) in actual)
            if (!observedFiles.TryAdd(Normalize(name, prefix), hash)) return false;
        return sealedFiles.Count > 0 && sealedFiles.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(observedFiles.OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }

    internal static bool Validate(JsonElement seal, IEnumerable<KeyValuePair<string, string>> inputs,
        IEnumerable<KeyValuePair<string, string>> jit, IEnumerable<KeyValuePair<string, string>> aot,
        string commit, string status) =>
        seal.GetProperty("Version").GetInt32() == 1 &&
        seal.GetProperty("SourceCommit").ValueEquals(commit) && seal.GetProperty("SourceStatus").ValueEquals(status) &&
        Matches(seal.GetProperty("Inputs"), inputs) && Matches(seal.GetProperty("Jit"), jit) && Matches(seal.GetProperty("Aot"), aot);

    internal static bool ValidateRuntime(JsonElement summary, JsonElement sealedFiles, string kind, string output)
    {
        if (kind is not ("jit" or "aot") || !RequiredNativeChecks.Validate(summary, "native-summary", kind == "jit")) return false;
        JsonElement hashes = summary.GetProperty("PublishedFileSha256");
        if (hashes.ValueKind != JsonValueKind.Object) return false;
        return Matches(sealedFiles, hashes.EnumerateObject().Select(property =>
            new KeyValuePair<string, string>(property.Name, property.Value.GetString() ?? "")), output);
    }

    private static string Normalize(string path, string prefix)
    {
        string normalized = path.Replace('\\', '/');
        string normalizedPrefix = prefix.Replace('\\', '/').TrimEnd('/') + "/";
        return prefix.Length > 0 && normalized.StartsWith(normalizedPrefix, StringComparison.Ordinal)
            ? normalized[normalizedPrefix.Length..] : normalized;
    }
}