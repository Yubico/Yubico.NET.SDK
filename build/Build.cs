#!/usr/bin/env dotnet run

#:package System.CommandLine@2.0.0-rc.1.25451.107

// To run locally: 
// dotnet run build.cs -- --nuget-feed-path "C:\LocalNuGet"
//
// To run in CI (e.g., GitHub Actions):
// dotnet run build.cs -- --version "1.2.3-ci.${{ github.run_number }}" --nuget-feed-name "GPR"
//
// build.cs - An automated and CI-aware CS-Script to build the Yubico.NET.SDK.

using System.Diagnostics;
using System.CommandLine;
using System.Xml.Linq;

// --- 1. Define Command-Line Interface ---
var versionOption = new Option<string?>("--version");
var feedNameOption = new Option<string>("--nuget-feed-name")
{
    DefaultValueFactory = _ => "LocalNuGetFeed"
};
var feedPathOption = new Option<string>("--nuget-feed-path")
{
    DefaultValueFactory = _ => Path.Combine(Path.GetTempPath(), "Yubico.LocalNuGet")
};
var includeDocsOption = new Option<bool>("--include-docs");
var dryRunOption = new Option<bool>("--dry-run");
var cleanOption = new Option<bool>("--clean");

var rootCommand = new RootCommand("Builds the Yubico.NET.SDK project locally or in CI.")
{
    versionOption, feedNameOption, feedPathOption, includeDocsOption, dryRunOption, cleanOption
};

rootCommand.SetAction(async (parseResult) =>
    {
        string? version = parseResult.GetValue(versionOption);
        string? feedName = parseResult.GetValue(feedNameOption);
        string? feedPath = parseResult.GetValue(feedPathOption);
        bool includeDocs = parseResult.GetValue(includeDocsOption);
        bool dryRun = parseResult.GetValue(dryRunOption);
        bool clean = parseResult.GetValue(cleanOption);

        await ExecuteBuild(version, feedName!, feedPath, includeDocs, dryRun, clean);
    });

await rootCommand.Parse(args).InvokeAsync();


// --- 2. Main Build Logic ---
async Task ExecuteBuild(string? version, string feedName, string? feedPath, bool includeDocs, bool dryRun, bool clean)
{
    bool isCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
    string rootDir = GetRepositoryRoot();
    string solutionFile = Path.Combine(rootDir, "Yubico.NET.SDK.sln");
    string versionPropsFile = Path.Combine(rootDir, "build", "Versions.props");
    string[] packagePaths = { "Yubico.Core/src/bin/Release/*.nupkg", "Yubico.YubiKey/src/bin/Release/*.nupkg" };

    WriteHeader("Yubico.NET.SDK Build");
    WriteInfo($"CI Environment: {isCi}");

    try
    {
        await TestPrerequisites(solutionFile, feedName, feedPath, isCi);
        SetBuildVersion(version, versionPropsFile);
        InvokeCleanBuild(clean, packagePaths, rootDir);
        await InvokeBuildSolution(solutionFile);

        var packages = GetBuildArtifacts(packagePaths, rootDir);
        await PublishToFeed(packages, feedName, dryRun);
        ShowBuildSummary(packages, version, feedName, dryRun);
    }
    catch (Exception ex)
    {
        WriteError($"Build failed: {ex.Message}");
        Environment.Exit(1);
    }
    finally
    {
        RestoreBuildVersion(version, versionPropsFile);
    }
}


// --- 3. Build Steps & Helper Methods ---

async Task TestPrerequisites(string solutionFile, string feedName, string? feedPath, bool isCi)
{
    WriteHeader("Checking Prerequisites");
    if (!File.Exists(solutionFile))
    {
        throw new FileNotFoundException($"Solution file not found: {solutionFile}");
    }

    WriteStep("Checking .NET SDK");
    _ = await RunCommand("dotnet", "--version");

    WriteStep("Checking NuGet feed configuration");
    if (isCi)
    {
        WriteInfo("✓ Skipping local feed setup in CI environment.");
    }
    else
    {
        await InitializeLocalNuGetFeed(feedName, feedPath!);
    }
}

async Task InitializeLocalNuGetFeed(string feedName, string feedPath)
{
    if (string.IsNullOrWhiteSpace(feedPath))
    {
        // This should not happen due to the default value factory, but we'll keep it for safety.
        feedPath = Path.Combine(Path.GetTempPath(), "Yubico.NET.SDK-LocalNuGet");
    }

    string sources = await RunCommand("dotnet", "nuget list source", captureOutput: true);
    if (sources.Contains(feedName))
    {
        WriteInfo($"✓ NuGet source '{feedName}' is already configured.");
        return;
    }

    if (!Directory.Exists(feedPath))
    {
        WriteInfo($"Creating directory: {feedPath}");
        _ = Directory.CreateDirectory(feedPath);
    }

    _ = await RunCommand("dotnet", $"nuget add source \"{feedPath}\" --name \"{feedName}\"");
    WriteInfo($"✓ Local NuGet feed '{feedName}' created at '{feedPath}'.");
}

void InvokeCleanBuild(bool clean, string[] packagePatterns, string rootDir)
{
    WriteHeader("Cleaning Build Artifacts");
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
    WriteInfo("✓ Old NuGet packages cleaned.");

    if (clean)
    {
        WriteStep("Performing full clean (bin/obj)");
        foreach (string dir in Directory.EnumerateDirectories(rootDir, "bin", SearchOption.AllDirectories))
        {
            Directory.Delete(dir, recursive: true);
        }
        foreach (string dir in Directory.EnumerateDirectories(rootDir, "obj", SearchOption.AllDirectories))
        {
            Directory.Delete(dir, recursive: true);
        }

        WriteStep("Clearing NuGet cache");
        RunCommand("dotnet", "nuget locals all --clear").Wait();
        WriteInfo("✓ Full clean completed.");
    }
}

async Task InvokeBuildSolution(string solutionFile)
{
    WriteHeader("Building Solution");
    _ = await RunCommand("dotnet", $"pack \"{solutionFile}\" --configuration Release --nologo --verbosity minimal");
}

List<string> GetBuildArtifacts(string[] packagePatterns, string rootDir)
{
    WriteHeader("Collecting Build Artifacts");
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
            WriteInfo($"✓ Found: {Path.GetFileName(file)}");
        }
    }
    if (packages.Count == 0)
    {
        throw new InvalidOperationException("No NuGet packages found. Build may have failed.");
    }
    return packages;
}

async Task PublishToFeed(List<string> packages, string feedName, bool dryRun)
{
    if (dryRun)
    {
        WriteHeader($"DRY RUN: Would Publish to '{feedName}'");
        packages.ForEach(p => WriteInfo($"  - {Path.GetFileName(p)}"));
        return;
    }

    WriteHeader($"Publishing to '{feedName}'");
    foreach (string package in packages)
    {
        _ = await RunCommand("dotnet", $"nuget push \"{package}\" --source \"{feedName}\" --skip-duplicate");
    }
}

// --- Versioning ---
void SetBuildVersion(string? version, string versionPropsFile)
{
    if (string.IsNullOrEmpty(version))
    {
        return;
    }
    WriteHeader($"Setting Custom Version: {version}");
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
    WriteInfo($"✓ Updated version to: {version}");
}

void RestoreBuildVersion(string? version, string versionPropsFile)
{
    string backupFile = $"{versionPropsFile}.backup";
    if (string.IsNullOrEmpty(version) || !File.Exists(backupFile))
    {
        return;
    }
    WriteStep("Restoring original version file");
    File.Move(backupFile, versionPropsFile, overwrite: true);
    WriteInfo("✓ Version file restored.");
}

// --- Utilities ---
void ShowBuildSummary(List<string> packages, string? version, string feedName, bool dryRun)
{
    WriteHeader("Build Summary");
    if (!string.IsNullOrEmpty(version))
    {
        WriteInfo($"Version: {version}");
    }

    Console.WriteLine("\nPackages Built:");
    packages.ForEach(p =>
    {
        var fi = new FileInfo(p);
        WriteInfo($"  📦 {fi.Name} ({Math.Round(fi.Length / 1024.0, 2)} KB)");
    });

    Console.WriteLine();
    if (dryRun)
    {
        WriteInfo("🔍 DRY RUN completed - no packages were published.");
    }
    else
    {
        WriteInfo($"✅ Build completed and published to '{feedName}'!");
    }
}

string GetRepositoryRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository root.");
}

void WriteHeader(string m) 
{ 
    const string separator = "==================================================";
    Console.ForegroundColor = ConsoleColor.Cyan; 
    Console.WriteLine($"\n{separator}\n{m}\n{separator}"); 
    Console.ResetColor(); 
}
void WriteStep(string m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"\n>>> {m}"); Console.ResetColor(); }
void WriteInfo(string m) => Console.WriteLine(m);
void WriteError(string m) { Console.ForegroundColor = ConsoleColor.Red; Console.Error.WriteLine(m); Console.ResetColor(); }

async Task<string> RunCommand(string command, string args, bool captureOutput = false)
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
                WriteInfo(e.Data);
            }
        }
    };
    
    process.ErrorDataReceived += (s, e) =>
    {
        if (e.Data is not null)
        {
            WriteError(e.Data);
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
