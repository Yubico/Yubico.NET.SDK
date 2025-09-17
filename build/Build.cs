#!/usr/bin/env dotnet run

#:package System.CommandLine@2.0.0-beta4.22272.1



// To run locally: 
// dotnet run build.cs -- --nuget-feed-path "C:\LocalNuGet"
//
// To run in CI (e.g., GitHub Actions):
// dotnet run build.cs -- --package-version "1.2.3-ci.${{ github.run_number }}" --nuget-feed-name "GPR"
//
// build.cs - An automated and CI-aware CS-Script to build the Yubico.NET.SDK.

using System.Diagnostics;
using System.CommandLine;
using System.Xml.Linq;

#region Setup
var versionOption = new Option<string?>("--package-version", "The version to assign to the built NuGet packages.");
var feedNameOption = new Option<string>("--nuget-feed-name", () => "Yubico.NET.SDK-LocalNuGet", "The name of the NuGet feed to publish to.");
var feedPathOption = new Option<string>("--nuget-feed-path", () => Path.Combine(Path.GetTempPath(), "Yubico.NET.SDK-LocalNuGet"), "The path to the local NuGet feed.");
var includeDocsOption = new Option<bool>("--include-docs", () => false, "Whether to include documentation in the package.");
var dryRunOption = new Option<bool>("--dry-run", () => false, "Whether to perform a dry run without making any changes.");
var cleanOption = new Option<bool>("--clean", () => false, "Whether to clean the output directory before building.");

var rootCommand = new RootCommand("Builds the Yubico.NET.SDK project locally or in CI.")
{
    versionOption, feedNameOption, feedPathOption, includeDocsOption, dryRunOption, cleanOption
};

rootCommand.SetHandler(async ctx =>
{
    string? version = ctx.ParseResult.GetValueForOption(versionOption);
    string feedName = ctx.ParseResult.GetValueForOption(feedNameOption)!;
    string? feedPath = ctx.ParseResult.GetValueForOption(feedPathOption);
    bool includeDocs = ctx.ParseResult.GetValueForOption(includeDocsOption);
    bool dryRun = ctx.ParseResult.GetValueForOption(dryRunOption);
    bool clean = ctx.ParseResult.GetValueForOption(cleanOption);
    await ExecuteBuild(version, feedName, feedPath, includeDocs, dryRun, clean);
});

return await rootCommand.InvokeAsync(args);
#endregion

#region Main Build Logic
async Task ExecuteBuild(string? version, string feedName, string? feedPath, bool includeDocs, bool dryRun, bool clean)
{
    bool isCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
    string rootDir = Utils_GetRepositoryRoot();
    string solutionFile = Path.Combine(rootDir, "Yubico.NET.SDK.sln");
    string versionPropsFile = Path.Combine(rootDir, "build", "Versions.props");
    string[] packagePaths = { "Yubico.Core/src/bin/Release/*.nupkg", "Yubico.YubiKey/src/bin/Release/*.nupkg" };

    Utils_WriteHeader("Yubico.NET.SDK Build");
    Utils_WriteInfo($"CI Environment: {isCi}");

    try
    {
        await Step_TestPrerequisites(solutionFile, feedName, feedPath, isCi);
        Step_SetBuildVersion(version, versionPropsFile);
        Step_CleanBuild(clean, packagePaths, rootDir);
        await Step_BuildSolution(solutionFile);

        var packages = Utils_GetBuildArtifacts(packagePaths, rootDir);
        await Step_PublishToFeed(packages, feedName, dryRun);
        Utils_ShowBuildSummary(packages, version, feedName, dryRun);
    }
    catch (Exception ex)
    {
        Utils_WriteError($"Build failed: {ex.Message}");
        Environment.Exit(1);
    }
    finally
    {
        Step_RestoreBuildVersion(version, versionPropsFile);
    }
}
#endregion

#region Build Steps
async Task Step_TestPrerequisites(string solutionFile, string feedName, string? feedPath, bool isCi)
{
    Utils_WriteHeader("Checking Prerequisites");
    if (!File.Exists(solutionFile))
    {
        throw new FileNotFoundException($"Solution file not found: {solutionFile}");
    }

    Utils_WriteStep("Checking .NET SDK");
    _ = await Utils_RunCommand("dotnet", "--version");

    Utils_WriteStep("Checking NuGet feed configuration");
    if (isCi)
    {
        Utils_WriteInfo("✓ Skipping local feed setup in CI environment.");
    }
    else
    {
        await Helper_InitializeLocalNuGetFeed(feedName, feedPath!);
    }
}

void Step_CleanBuild(bool clean, string[] packagePatterns, string rootDir)
{
    Utils_WriteHeader("Cleaning Build Artifacts");
    foreach (string pattern in packagePatterns)
    {
        string fullPattern = Path.Combine(rootDir, pattern);
        string dir = Path.GetDirectoryName(fullPattern)!;
        string filePattern = Path.GetFileName(fullPattern);
        if (!Directory.Exists(dir))
        {
            continue;
        }

        foreach (string file in Directory.GetFiles(dir, filePattern))
        {
            File.Delete(file);
        }
    }

    Utils_WriteInfo("✓ Old NuGet packages cleaned.");

    if (clean)
    {
        Utils_WriteStep("Performing full clean (bin/obj)");
        foreach (string dir in Directory.EnumerateDirectories(rootDir, "bin", SearchOption.AllDirectories))
        {
            Directory.Delete(dir, recursive: true);
        }
        foreach (string dir in Directory.EnumerateDirectories(rootDir, "obj", SearchOption.AllDirectories))
        {
            Directory.Delete(dir, recursive: true);
        }

        Utils_WriteStep("Clearing NuGet cache");
        Utils_RunCommand("dotnet", "nuget locals all --clear").Wait();
        Utils_WriteInfo("✓ Full clean completed.");
    }
}

async Task Step_BuildSolution(string solutionFile)
{
    Utils_WriteHeader("Building Solution");
    _ = await Utils_RunCommand("dotnet", $"pack \"{solutionFile}\" --configuration Release --nologo --verbosity minimal");
}

void Step_SetBuildVersion(string? version, string versionPropsFile)
{
    if (string.IsNullOrEmpty(version))
    {
        return;
    }
    Utils_WriteHeader($"Setting Custom Version: {version}");
    string backupFile = $"{versionPropsFile}.backup";
    if (File.Exists(backupFile))
    {
        File.Delete(backupFile);
    }
    File.Copy(versionPropsFile, backupFile, overwrite: true);
    var doc = XDocument.Load(versionPropsFile);
    doc.Descendants("YubicoCoreVersion").FirstOrDefault()?.SetValue(version);
    doc.Descendants("YubicoYubiKeyVersion").FirstOrDefault()?.SetValue(version);
    doc.Save(versionPropsFile);
    Utils_WriteInfo($"✓ Updated version to: {version}");
}

async Task Step_PublishToFeed(List<string> packages, string feedName, bool dryRun)
{
    if (dryRun)
    {
        Utils_WriteHeader($"DRY RUN: Would Publish to '{feedName}'");
        packages.ForEach(p => Utils_WriteInfo($"  - {Path.GetFileName(p)}"));
        return;
    }

    Utils_WriteHeader($"Publishing to '{feedName}'");
    foreach (string package in packages)
    {
        _ = await Utils_RunCommand("dotnet", $"nuget push \"{package}\" --source \"{feedName}\" --skip-duplicate");
    }
}

void Step_RestoreBuildVersion(string? version, string versionPropsFile)
{
    string backupFile = $"{versionPropsFile}.backup";
    if (string.IsNullOrEmpty(version) || !File.Exists(backupFile))
    {
        return;
    }
    Utils_WriteStep("Restoring original version file");
    File.Move(backupFile, versionPropsFile, overwrite: true);
    Utils_WriteInfo("✓ Version file restored.");
}

#endregion

#region Helper and Utility Functions
async Task Helper_InitializeLocalNuGetFeed(string feedName, string feedPath)
{
    if (string.IsNullOrWhiteSpace(feedPath))
    {
        throw new ArgumentException(
            "Feed path must be specified for local NuGet feed." +
            " Use --nuget-feed-path to set a custom path.", nameof(feedPath));
    }

    string sources = await Utils_RunCommand("dotnet", "nuget list source", captureOutput: true);
    if (sources.Contains(feedName))
    {
        Utils_WriteInfo($"✓ NuGet source '{feedName}' is already configured.");
        return;
    }

    if (!Directory.Exists(feedPath))
    {
        Utils_WriteInfo($"Creating directory: {feedPath}");
        _ = Directory.CreateDirectory(feedPath);
    }

    _ = await Utils_RunCommand("dotnet", $"nuget add source \"{feedPath}\" --name \"{feedName}\"");
    Utils_WriteInfo($"✓ Local NuGet feed '{feedName}' created at '{feedPath}'.");
}

List<string> Utils_GetBuildArtifacts(string[] packagePatterns, string rootDir)
{
    Utils_WriteHeader("Collecting Build Artifacts");
    var packages = new List<string>();
    foreach (string pattern in packagePatterns)
    {
        string fullPattern = Path.Combine(rootDir, pattern);
        string dir = Path.GetDirectoryName(fullPattern)!;
        string filePattern = Path.GetFileName(fullPattern);
        if (!Directory.Exists(dir))
        {
            continue;
        }

        foreach (string file in Directory.GetFiles(dir, filePattern))
        {
            packages.Add(file);
            Utils_WriteInfo($"✓ Found: {Path.GetFileName(file)}");
        }
    }
    if (packages.Count == 0)
    {
        throw new InvalidOperationException("No NuGet packages found. Build may have failed.");
    }
    return packages;
}

void Utils_ShowBuildSummary(List<string> packages, string? version, string feedName, bool dryRun)
{
    Utils_WriteHeader("Build Summary");
    if (!string.IsNullOrEmpty(version))
    {
        Utils_WriteInfo($"Version: {version}");
    }

    Console.WriteLine("\nPackages Built:");
    packages.ForEach(p =>
    {
        var fi = new FileInfo(p);
        Utils_WriteInfo($"  📦 {fi.Name} ({Math.Round(fi.Length / 1024.0, 2)} KB)");
    });

    Console.WriteLine();
    if (dryRun)
    {
        Utils_WriteInfo("🔍 DRY RUN completed - no packages were published.");
    }
    else
    {
        Utils_WriteInfo($"✅ Build completed and published to '{feedName}'!");
    }
}

string Utils_GetRepositoryRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository root.");
}

void Utils_WriteHeader(string m)
{
    const string separator = "==================================================";
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n{separator}\n{m}\n{separator}");
    Console.ResetColor();
}
void Utils_WriteStep(string m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"\n>>> {m}"); Console.ResetColor(); }
void Utils_WriteInfo(string m) => Console.WriteLine(m);
void Utils_WriteError(string m) { Console.ForegroundColor = ConsoleColor.Red; Console.Error.WriteLine(m); Console.ResetColor(); }

async Task<string> Utils_RunCommand(string command, string args, bool captureOutput = false)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    var outputBuilder = new System.Text.StringBuilder();

    process.OutputDataReceived += (s, e) =>
    {
        if (e.Data is not null)
        {
            if (captureOutput)
            {
                _ = outputBuilder.AppendLine(e.Data);
            }
            else
            {
                Utils_WriteInfo(e.Data);
            }
        }
    };

    process.ErrorDataReceived += (s, e) =>
    {
        if (e.Data is not null)
        {
            Utils_WriteError(e.Data);
        }
    };

    _ = process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Command '{command} {args}' failed with exit code {process.ExitCode}");
    }

    return outputBuilder.ToString();
}
#endregion
