// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Text.Json;

namespace WindowsHidRouteVerification;

internal static class RequiredNativeChecks
{
    // Acceptance contract independent of NativeScenarios.Names and the checks actually emitted.
    private static readonly Dictionary<string, string[]> Scenarios = new(StringComparer.Ordinal)
    {
        ["pending-read-gc"] = ["accepted-with-empty-peer", "pending-storage-retained", "native-read-payload", "exactly-one-success", "terminal-storage-cleared", "retired-and-checked-close", "public-terminal-before-native-close"],
        ["output"] = ["native-output-framing", "exactly-one-success", "terminal-storage-cleared", "retired-and-checked-close", "public-terminal-before-native-close"],
        ["early-callback"] = ["accepted-with-empty-peer", "pending-storage-retained", "callback-before-submit-return", "native-read-payload", "exactly-one-success", "terminal-storage-cleared", "retired-and-checked-close", "public-terminal-before-native-close"],
        ["cancel-retained"] = ["targeted-cancel-requested", "abort-from-native", "callback-barrier-retains-storage", "public-canceled-before-disposal", "terminal-storage-cleared", "retired-and-checked-close", "public-terminal-before-native-close"],
        ["success-cancel-race"] = ["cancel-intent-with-native-success", "native-success-wins", "terminal-storage-cleared", "retired-and-checked-close", "public-terminal-before-native-close"],
        ["cancel-not-found"] = ["data-ready-native-success", "completed-request-cancel-not-found", "callback-tail-retirement"],
        ["reject-read"] = ["disconnected-peer-rejected", "rejected-no-callback", "rejected-tracking-retired"],
        ["reject-write"] = ["disconnected-peer-rejected", "rejected-no-callback", "rejected-tracking-retired"],
        ["reopen-stress"] = ["100-owned-pipe-close-reopen-callback-tails"],
        ["metadata-default"] = ["ordinary-construction-queries-hid-metadata", "metadata-failure-handle-released"]
    };

    internal static bool ValidateScenarios(IEnumerable<string> scenarios) =>
        scenarios.Order(StringComparer.Ordinal).SequenceEqual(Scenarios.Keys.Order(StringComparer.Ordinal));

    internal static bool Complete(string scenario, IEnumerable<KeyValuePair<string, bool>> checks)
    {
        IEnumerable<string>? required = scenario == "native-summary"
            ? Scenarios.Keys.SelectMany(name => new[] { name + "-exit", name + "-required-checks" }).Prepend("requested-runtime-kind")
            : Scenarios.GetValueOrDefault(scenario);
        if (required is null) return false;
        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string name, bool passed) in checks)
            if (!passed || !actual.Add(name)) return false;
        return actual.SetEquals(required);
    }

    internal static bool Validate(JsonElement result, string scenario, bool dynamicCode)
    {
        if (result.ValueKind != JsonValueKind.Object) return false;
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in result.EnumerateObject())
            if (!properties.TryAdd(property.Name, property.Value)) return false;
        return StringEquals(properties.GetValueOrDefault("Scenario"), scenario) &&
            StringEquals(properties.GetValueOrDefault("Status"), "PASSED") &&
            properties.GetValueOrDefault("DynamicCode").ValueKind == (dynamicCode ? JsonValueKind.True : JsonValueKind.False) &&
            ValidateChecks(scenario, properties.GetValueOrDefault("Checks"));
    }

    private static bool StringEquals(JsonElement value, string expected) =>
        value.ValueKind == JsonValueKind.String && value.ValueEquals(expected);

    private static bool ValidateChecks(string scenario, JsonElement checks) =>
        checks.ValueKind == JsonValueKind.Object && Complete(scenario,
            checks.EnumerateObject().Select(property => new KeyValuePair<string, bool>(property.Name, property.Value.ValueKind == JsonValueKind.True)));
}