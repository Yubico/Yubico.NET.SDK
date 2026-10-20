// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using static Output;

static class BenchmarkTargets
{
    public static void Register(ToolchainContext ctx)
    {
        TargetRegistry.Define("benchmark", "Run performance benchmarks (manual, not CI)", () =>
        {
            PrintHeader("Running performance benchmarks");

            var benchmarkProject = Path.Combine(
                ctx.RepoRoot,
                "benchmarks",
                "Yubico.YubiKit.PerformanceBenchmarks",
                "Yubico.YubiKit.PerformanceBenchmarks.csproj");

            Dotnet.Run($"run --project \"{benchmarkProject}\" -c {Conventions.Configuration} -- {ctx.BenchmarkArgs}");
        });
    }
}
