// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

/// <summary>A target as listed in --help.</summary>
sealed record TargetInfo(string Name, string Description, string[] DependsOn);

/// <summary>
/// Registers Bullseye targets and remembers them, so --help is generated from the same definitions
/// that run and cannot list a target that does not exist or miss one that does.
/// </summary>
static class TargetRegistry
{
    static readonly List<TargetInfo> s_targets = [];

    public static IReadOnlyList<TargetInfo> All => s_targets;

    public static void Define(string name, string description, Action action) =>
        Define(name, description, [], action);

    public static void Define(string name, string description, string[] dependsOn, Action action)
    {
        s_targets.Add(new TargetInfo(name, description, dependsOn));
        Bullseye.Targets.Target(name, description, dependsOn, action);
    }

    public static void Define(string name, string description, string[] dependsOn)
    {
        s_targets.Add(new TargetInfo(name, description, dependsOn));
        Bullseye.Targets.Target(name, description, dependsOn);
    }

    /// <summary>A command handled before Bullseye runs; listed in --help but not a Bullseye target.</summary>
    public static void Describe(string name, string description) =>
        s_targets.Add(new TargetInfo(name, description, []));
}
