// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using static Output;

static class PackageTargets
{
    public static void Register(ToolchainContext ctx)
    {
        TargetRegistry.Define("pack", "Create NuGet packages in artifacts/packages", ["build"], () =>
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
                Dotnet.Run($"pack {project} -c {Conventions.Configuration} {noBuildArg} -o {ctx.PackagesDir} {versionArg} {docsArg} {symbolsArg}", $"pack-{ProjectDiscovery.ShortName(project)}");
                PrintInfo($"Packed {ProjectDiscovery.ShortName(project)}");
            }

            var packages = Directory.GetFiles(ctx.PackagesDir, "*.nupkg");
            PrintInfo($"Created {packages.Length} package(s) in {ctx.PackagesDir}");
        });

        TargetRegistry.Define("setup-feed", "Register the local NuGet feed as a package source", () =>
        {
            PrintHeader("Setting up local NuGet feed");

            Directory.CreateDirectory(ctx.NugetFeedPath);

            var (exitCode, sources) = Dotnet.Capture(["nuget", "list", "source"]);
            if (exitCode != 0)
                PrintWarn($"Could not list NuGet sources (exit code {exitCode}); adding the source anyway");

            if (exitCode == 0 && sources.Contains(ctx.NugetFeedName))
            {
                PrintInfo($"NuGet source already exists: {ctx.NugetFeedName}");
                return;
            }

            Dotnet.Run($"nuget add source {ctx.NugetFeedPath} -n {ctx.NugetFeedName}", "nuget-add-source");
            PrintInfo($"Added NuGet source: {ctx.NugetFeedName}");
        });

        TargetRegistry.Define("publish", "Push packages to the local feed", ["pack", "setup-feed"], () =>
        {
            PrintHeader(ctx.DryRun ? "Dry run - packages to publish" : "Publishing packages");

            var packages = Directory.GetFiles(ctx.PackagesDir, "*.nupkg");
            PublishPackages(packages, ctx.DryRun, ctx.NugetFeedName, package =>
            {
                Dotnet.Run($"nuget push {package} -s {ctx.NugetFeedName} --skip-duplicate", $"push-{Path.GetFileNameWithoutExtension(package)}");
            });
        });

        TargetRegistry.Define("publish-remote", "Push packages to a remote NuGet feed (e.g. GitHub Packages)", () =>
        {
            PrintHeader(ctx.DryRun ? "Dry run - remote packages to publish" : "Publishing packages to remote feed");

            if (string.IsNullOrEmpty(ctx.NugetFeedUrl))
                throw new ToolchainException("publish-remote requires --nuget-feed-url <url>");
            if (string.IsNullOrEmpty(ctx.NugetApiKey))
                throw new ToolchainException("publish-remote requires --nuget-api-key <key> or the NUGET_API_KEY environment variable");

            var packages = Directory.GetFiles(ctx.PackagesDir, "*.nupkg");

            if (packages.Length == 0)
                throw new ToolchainException("No packages found in artifacts/packages. Run `dotnet toolchain.cs pack` first.");

            PublishPackages(packages, ctx.DryRun, ctx.NugetFeedUrl, package =>
            {
                Dotnet.Run($"nuget push {package} -s {ctx.NugetFeedUrl} --api-key {ctx.NugetApiKey} --skip-duplicate", $"push-{Path.GetFileNameWithoutExtension(package)}", ctx.NugetApiKey);
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
