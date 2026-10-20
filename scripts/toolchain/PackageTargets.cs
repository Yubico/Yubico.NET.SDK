// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using static Bullseye.Targets;
using static SimpleExec.Command;
using static Output;

static class PackageTargets
{
    public static void Register(ToolchainContext ctx)
    {
        Target("pack", DependsOn("build"), () =>
        {
            PrintHeader("Creating NuGet packages");

            Directory.CreateDirectory(ctx.PackagesDir);

            var versionArg = string.IsNullOrEmpty(ctx.PackageVersion) ? "" : $"/p:Version={ctx.PackageVersion}";
            var docsArg = ctx.IncludeDocs ? "" : "/p:GenerateDocumentationFile=false";

            // --embed-symbols: embed the portable PDB inside the .nupkg instead of emitting a
            // separate .snupkg. Used for the ad hoc Sleet/GitHub Pages alpha feed, which is a
            // static file feed and cannot serve .snupkg symbol packages. Requires a recompile
            // (no --no-build) so DebugType takes effect. Default packing keeps the standard
            // .snupkg for real symbol-server releases.
            var symbolsArg = ctx.EmbedSymbols ? "/p:DebugType=embedded /p:IncludeSymbols=false" : "";
            var noBuildArg = ctx.EmbedSymbols ? "" : "--no-build";

            foreach (var project in ctx.PackableProjects)
            {
                Console.WriteLine($"\nPacking: {Path.GetFileNameWithoutExtension(project)}");
                Run("dotnet", $"pack {project} -c {ctx.Configuration} {noBuildArg} -o {ctx.PackagesDir} {versionArg} {docsArg} {symbolsArg}");
                PrintInfo($"Packed {Path.GetFileNameWithoutExtension(project)}");
            }

            var packages = Directory.GetFiles(ctx.PackagesDir, "*.nupkg");
            PrintInfo($"Created {packages.Length} package(s) in {ctx.PackagesDir}");
        });

        Target("setup-feed", async () =>
        {
            PrintHeader("Setting up local NuGet feed");

            Directory.CreateDirectory(ctx.NugetFeedPath);

            try
            {
                var result = await ReadAsync("dotnet", "nuget list source");

                if (!result.StandardOutput.Contains(ctx.NugetFeedName))
                {
                    Run("dotnet", $"nuget add source {ctx.NugetFeedPath} -n {ctx.NugetFeedName}");
                    PrintInfo($"Added NuGet source: {ctx.NugetFeedName}");
                }
                else
                {
                    PrintInfo($"NuGet source already exists: {ctx.NugetFeedName}");
                }
            }
            catch (Exception ex)
            {
                PrintColored($"Warning: Could not list NuGet sources ({ex.Message}), attempting to add anyway...", ConsoleColor.Yellow);
                Run("dotnet", $"nuget add source {ctx.NugetFeedPath} -n {ctx.NugetFeedName}");
                PrintInfo($"Added NuGet source: {ctx.NugetFeedName}");
            }
        });

        Target("publish", DependsOn("pack", "setup-feed"), () =>
        {
            PrintHeader(ctx.DryRun ? "Dry run - packages to publish" : "Publishing packages");

            var packages = Directory.GetFiles(ctx.PackagesDir, "*.nupkg");
            PublishPackages(packages, ctx.DryRun, ctx.NugetFeedName, package =>
            {
                Run("dotnet", $"nuget push {package} -s {ctx.NugetFeedName} --skip-duplicate");
            });
        });

        Target("publish-remote", () =>
        {
            PrintHeader(ctx.DryRun ? "Dry run - remote packages to publish" : "Publishing packages to remote feed");

            if (string.IsNullOrEmpty(ctx.NugetFeedUrl))
                throw new InvalidOperationException("--nuget-feed-url is required for publish-remote");
            if (string.IsNullOrEmpty(ctx.NugetApiKey))
                throw new InvalidOperationException("NUGET_API_KEY env var or --nuget-api-key argument is required for publish-remote");

            var packages = Directory.GetFiles(ctx.PackagesDir, "*.nupkg");

            if (packages.Length == 0)
                throw new InvalidOperationException($"No packages found in {ctx.PackagesDir}. Run 'pack' first.");

            PublishPackages(packages, ctx.DryRun, ctx.NugetFeedUrl, package =>
            {
                Run("dotnet", $"nuget push {package} -s {ctx.NugetFeedUrl} --api-key {ctx.NugetApiKey} --skip-duplicate");
            });
        });
    }

    static void PublishPackages(string[] packages, bool dryRun, string feedLabel, Action<string> pushAction)
    {
        if (packages.Length == 0)
        {
            Console.WriteLine("No packages found to publish");
            return;
        }

        foreach (var package in packages)
        {
            var packageName = Path.GetFileName(package);
            if (dryRun)
            {
                Console.WriteLine($"  Would publish to {feedLabel}: {packageName}");
            }
            else
            {
                Console.WriteLine($"\nPublishing: {packageName}");
                pushAction(package);
                PrintInfo($"Published {packageName}");
            }
        }

        if (dryRun)
            Console.WriteLine($"\n(Dry run - no packages were actually published)");
    }
}
