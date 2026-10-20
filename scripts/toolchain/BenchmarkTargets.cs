// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using static Bullseye.Targets;
using static SimpleExec.Command;
using static Output;

static class BenchmarkTargets
{
    public static void Register(ToolchainContext ctx)
    {
        Target("benchmark", () =>
        {
            PrintHeader("Running performance benchmarks");

            var benchmarkProject = Path.Combine(
                ctx.RepoRoot,
                "benchmarks",
                "Yubico.YubiKit.PerformanceBenchmarks",
                "Yubico.YubiKit.PerformanceBenchmarks.csproj");

            Run("dotnet", $"run --project \"{benchmarkProject}\" -c {ctx.Configuration} -- {ctx.BenchmarkArgs}");
        });
    }
}
