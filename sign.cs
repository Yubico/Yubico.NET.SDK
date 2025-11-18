#!/usr/bin/env dotnet run

#:package Bullseye
#:package SimpleExec

/*
 * Yubico.YubiKit Package Signing Script
 * ======================================
 *
 * Signs NuGet and Symbol packages using a certificate.
 *
 * ⚠️ PLATFORM LIMITATIONS:
 *    This script is WINDOWS-ONLY due to dependencies on:
 *    - signtool.exe (Windows SDK)
 *    - Windows Certificate Store
 *
 * USAGE:
 *   dotnet sign.cs [options]
 *
 * REQUIRED OPTIONS:
 *   --thumbprint <hash>           Certificate thumbprint (SHA-1)
 *   --working-dir <path>          Directory containing zip files
 *
 * PACKAGE OPTIONS (at least one set required):
 *   --nuget-zip <filename>        NuGet packages zip (e.g., "Nuget Packages.zip")
 *   --symbols-zip <filename>      Symbol packages zip (e.g., "Symbols Packages.zip")
 *   --native-zip <filename>       Native shims zip (e.g., "Yubico.NativeShims.nupkg.zip")
 *
 * OPTIONAL OPTIONS:
 *   --signtool <path>             Path to signtool.exe (default: "signtool.exe")
 *   --nuget <path>                Path to nuget.exe (default: "nuget.exe")
 *   --timestamp <url>             Timestamp server (default: http://timestamp.digicert.com)
 *   --repo <name>                 GitHub repo for attestation (default: Yubico/Yubico.NET.SDK)
 *   --clean                       Clean directories before signing
 *
 * EXAMPLES:
 *   # Sign core packages
 *   dotnet sign.cs --thumbprint ABC123 --working-dir C:\Releases\1.12 --nuget-zip "Nuget Packages.zip" --symbols-zip "Symbols Packages.zip"
 *
 *   # Sign native shims only
 *   dotnet sign.cs --thumbprint ABC123 --working-dir C:\Releases\1.12 --native-zip "Yubico.NativeShims.nupkg.zip"
 *
 *   # Sign everything with cleanup
 *   dotnet sign.cs --thumbprint ABC123 --working-dir C:\Releases\1.12 --nuget-zip "Nuget Packages.zip" --symbols-zip "Symbols Packages.zip" --native-zip "Yubico.NativeShims.nupkg.zip" --clean
 *
 * PREREQUISITES:
 *   - Windows OS
 *   - Windows SDK (for signtool.exe)
 *   - NuGet CLI (nuget.exe)
 *   - GitHub CLI (gh)
 *   - Smart card with signing certificate inserted
 *
 * WORKFLOW:
 *   1. Verify tools and certificate
 *   2. Extract packages from zip files
 *   3. Verify GitHub attestations
 *   4. Extract assemblies from packages
 *   5. Sign assemblies with signtool.exe
 *   6. Repack signed assemblies
 *   7. Sign final packages with nuget.exe
 *   8. Display summary
 */

using static Bullseye.Targets;
using static SimpleExec.Command;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;

// Configuration from command line
var thumbprint = GetArgument("--thumbprint") ?? throw new ArgumentException("--thumbprint is required");
var workingDir = GetArgument("--working-dir") ?? throw new ArgumentException("--working-dir is required");
var nugetZip = GetArgument("--nuget-zip");
var symbolsZip = GetArgument("--symbols-zip");
var nativeZip = GetArgument("--native-zip");
var signToolPath = GetArgument("--signtool") ?? "signtool.exe";
var nugetPath = GetArgument("--nuget") ?? "nuget.exe";
var timestampServer = GetArgument("--timestamp") ?? "http://timestamp.digicert.com";
var repoName = GetArgument("--repo") ?? "Yubico/Yubico.NET.SDK";
var shouldClean = HasFlag("--clean");

// Validate Windows platform
if (!OperatingSystem.IsWindows())
{
    throw new PlatformNotSupportedException("This signing script only runs on Windows due to signtool.exe and certificate store dependencies.");
}

// Validate at least one package type is specified
var hasCorePackages = !string.IsNullOrWhiteSpace(nugetZip) && !string.IsNullOrWhiteSpace(symbolsZip);
var hasNativeShims = !string.IsNullOrWhiteSpace(nativeZip);

if (!hasCorePackages && !hasNativeShims)
{
    throw new ArgumentException("No package files specified. Provide either --nuget-zip/--symbols-zip or --native-zip");
}

// Directory structure
var unsignedDir = Path.Combine(workingDir, "unsigned");
var signedDir = Path.Combine(workingDir, "signed");
var librariesDir = Path.Combine(signedDir, "libraries");
var packagesDir = Path.Combine(signedDir, "packages");

// Single default target that does everything
Target("default", async () =>
{
    try
    {
        PrintHeader("Initializing NuGet package signing process");

        // Step 1: Clean if requested
        if (shouldClean)
        {
            CleanDirectories();
        }

        // Step 2: Initialize directory structure
        InitializeDirectories();

        // Step 3: Verify required tools
        VerifyTools();

        // Step 4: Verify certificate
        VerifyCertificate();

        // Step 5: Process packages based on what was provided
        if (hasCorePackages)
        {
            ProcessCorePackages();
        }

        if (hasNativeShims)
        {
            ProcessNativePackage();
        }

        // Step 6: Sign all final packages (both nupkg and snupkg)
        SignFinalPackages();

        // Step 7: Print summary
        PrintSummary();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✨ Package signing process completed successfully! ✨");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n❌ Error occurred: {ex.Message}");
        Console.ResetColor();

        // Clean up on error
        Console.WriteLine("\nCleaning up after error...");
        CleanDirectories();

        throw;
    }
});

// Run Bullseye
await RunTargetsAndExitAsync(args);

// ============================================================================
// Helper Functions
// ============================================================================

void CleanDirectories()
{
    PrintHeader("Cleaning up working directories");

    var dirsToClean = new[] { unsignedDir, signedDir };
    foreach (var dir in dirsToClean)
    {
        if (Directory.Exists(dir))
        {
            Console.WriteLine($"Removing: {dir}");
            Directory.Delete(dir, recursive: true);
        }
    }

    PrintInfo("Cleanup completed");
}

void InitializeDirectories()
{
    Directory.CreateDirectory(unsignedDir);
    Directory.CreateDirectory(signedDir);
    Directory.CreateDirectory(librariesDir);
    Directory.CreateDirectory(packagesDir);
}

void VerifyTools()
{
    PrintHeader("Verifying required tools");

    // Check signtool
    try
    {
        Run(signToolPath, "/?", noEcho: true);
        PrintInfo($"SignTool found at: {signToolPath}");
    }
    catch
    {
        throw new FileNotFoundException($"SignTool not found at path: {signToolPath}");
    }

    // Check nuget
    try
    {
        Run(nugetPath, "help", noEcho: true);
        PrintInfo($"NuGet found at: {nugetPath}");
    }
    catch
    {
        throw new FileNotFoundException($"NuGet not found at path: {nugetPath}");
    }

    // Check GitHub CLI
    try
    {
        Run("gh", "--version", noEcho: true);
        PrintInfo("GitHub CLI found");
    }
    catch
    {
        throw new FileNotFoundException("GitHub CLI (gh) not found in PATH. Install from https://cli.github.com/");
    }
}

void VerifyCertificate()
{
    PrintHeader("Verifying signing certificate");

    var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    try
    {
        store.Open(OpenFlags.ReadOnly);
        var cert = store.Certificates
            .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
            .Cast<X509Certificate2>()
            .FirstOrDefault();

        if (cert is null)
        {
            throw new InvalidOperationException($"Certificate with thumbprint {thumbprint} not found in CurrentUser\\My store");
        }

        Console.WriteLine("\nCertificate Details:");
        Console.WriteLine($"  Subject:      {cert.Subject}");
        Console.WriteLine($"  Issuer:       {cert.Issuer}");
        Console.WriteLine($"  Thumbprint:   {cert.Thumbprint}");
        Console.WriteLine($"  Valid From:   {cert.NotBefore:yyyy-MM-dd}");
        Console.WriteLine($"  Valid To:     {cert.NotAfter:yyyy-MM-dd}");

        if (cert.NotAfter <= DateTime.Now.AddMonths(1))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  WARNING: Certificate will expire within one month on {cert.NotAfter:yyyy-MM-dd}");
            Console.ResetColor();
        }

        PrintInfo("Certificate verified");
    }
    finally
    {
        store.Close();
    }
}

void ProcessCorePackages()
{
    PrintHeader("Processing Core Packages");

    // Verify assets exist
    ValidateAsset(nugetZip!, "NuGet packages");
    ValidateAsset(symbolsZip!, "Symbol packages");

    // Process NuGet packages
    ProcessZipPackage(nugetZip!);

    // Copy symbol packages
    PrintSubHeader("Copying Symbol Packages");
    var symbolsExtractPath = Path.Combine(unsignedDir, Path.GetFileNameWithoutExtension(symbolsZip!));
    ZipFile.ExtractToDirectory(Path.Combine(workingDir, symbolsZip!), symbolsExtractPath, overwriteFiles: true);

    var symbolPackages = Directory.GetFiles(symbolsExtractPath, "*.snupkg", SearchOption.AllDirectories);
    foreach (var package in symbolPackages)
    {
        Console.WriteLine($"  Copying: {Path.GetFileName(package)}");
        File.Copy(package, Path.Combine(packagesDir, Path.GetFileName(package)), overwrite: true);
    }

    PrintInfo($"Copied {symbolPackages.Length} symbol package(s)");
}

void ProcessNativePackage()
{
    PrintHeader("Processing Native Shims Package");

    ValidateAsset(nativeZip!, "Native shims package");
    ProcessZipPackage(nativeZip!);
}

void ProcessZipPackage(string zipFileName)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n  🔄 Processing: {zipFileName}");
    Console.ResetColor();

    var zipPath = Path.Combine(workingDir, zipFileName);
    var extractPath = Path.Combine(unsignedDir, Path.GetFileNameWithoutExtension(zipFileName));

    Console.WriteLine($"    📂 Extracting to: {extractPath}");
    ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

    Console.WriteLine("    📋 Copying packages to unsigned directory");
    var packages = Directory.GetFiles(extractPath, "*.nupkg", SearchOption.AllDirectories);

    foreach (var package in packages)
    {
        var fileName = Path.GetFileName(package);
        Console.WriteLine($"      📦 Copying: {fileName}");

        // Verify GitHub attestation
        VerifyAttestation(package);

        File.Copy(package, Path.Combine(unsignedDir, fileName), overwrite: true);
    }

    PrintInfo($"Copied {packages.Length} package(s)");

    // Process each package
    var nugetPackages = Directory.GetFiles(unsignedDir, "*.nupkg");
    foreach (var package in nugetPackages)
    {
        ProcessNuGetPackage(package);
    }
}

void ProcessNuGetPackage(string packagePath)
{
    var packageName = Path.GetFileName(packagePath);
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"\n    📝 Signing contents of: {packageName}");
    Console.ResetColor();

    var extractPath = Path.Combine(librariesDir, Path.GetFileNameWithoutExtension(packageName));
    Console.WriteLine($"      📂 Extracting to: {extractPath}");

    ZipFile.ExtractToDirectory(packagePath, extractPath, overwriteFiles: true);

    // Clean package structure
    Console.WriteLine("      🧹 Cleaning package structure");
    foreach (var dir in new[] { "_rels", "package" })
    {
        var dirPath = Path.Combine(extractPath, dir);
        if (Directory.Exists(dirPath))
        {
            Directory.Delete(dirPath, recursive: true);
        }
    }

    var contentTypesFile = Path.Combine(extractPath, "[Content_Types].xml");
    if (File.Exists(contentTypesFile))
    {
        File.Delete(contentTypesFile);
    }

    // Sign assemblies
    Console.WriteLine("      ✍️  Signing assemblies...");
    var dlls = Directory.GetFiles(extractPath, "*.dll", SearchOption.AllDirectories);
    foreach (var dll in dlls)
    {
        var frameworkDir = Path.GetFileName(Path.GetDirectoryName(dll)!);
        var fileName = Path.GetFileName(dll);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"        🔏 Signing: ..\\{frameworkDir}\\{fileName}");
        Console.ResetColor();

        SignFile(dll);
    }

    // Repack
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("      📦 Repacking assemblies...");
    Console.ResetColor();

    var nuspecFiles = Directory.GetFiles(extractPath, "*.nuspec", SearchOption.AllDirectories);
    foreach (var nuspec in nuspecFiles)
    {
        Console.WriteLine($"        📥 Packing: {Path.GetFileName(nuspec)}");
        Run(nugetPath, $"pack \"{nuspec}\" -OutputDirectory \"{packagesDir}\" -p TreatWarningsAsErrors=true");
    }
}

void SignFile(string filePath)
{
    var args = $"sign /fd SHA256 /sha1 {thumbprint} /t {timestampServer} \"{filePath}\"";
    Run(signToolPath, args);
}

void SignFinalPackages()
{
    PrintHeader("Signing final packages");

    var finalPackages = Directory.GetFiles(packagesDir, "*.nupkg")
        .Concat(Directory.GetFiles(packagesDir, "*.snupkg"))
        .ToArray();

    foreach (var package in finalPackages)
    {
        var packageName = Path.GetFileName(package);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  ✒️  Signing package: {packageName}");
        Console.ResetColor();

        var args = $"sign \"{package}\" -CertificateFingerprint {thumbprint} -Timestamper {timestampServer} -NonInteractive";
        Run(nugetPath, args);
    }

    PrintInfo($"Signed {finalPackages.Length} final package(s)");
}

void VerifyAttestation(string filePath)
{
    var fileName = Path.GetFileName(filePath);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write($"      🔐 Verifying attestation for: {fileName}...");
    Console.ResetColor();

    try
    {
        Run("gh", $"attestation verify \"{filePath}\" --repo {repoName}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ✅ Verified");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($" ❌ FAILED");
        Console.WriteLine($"        Error: {ex.Message}");
        Console.ResetColor();
        throw new InvalidOperationException($"Attestation verification failed for: {fileName}", ex);
    }
}

void ValidateAsset(string fileName, string description)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($"  🔍 Validating {description}...");
    Console.ResetColor();

    var filePath = Path.Combine(workingDir, fileName);
    if (!File.Exists(filePath))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(" ❌ NOT FOUND");
        Console.ResetColor();
        throw new FileNotFoundException($"Required build asset not found: {fileName}", filePath);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($" ✅ Found");
    Console.ResetColor();
}

void PrintSummary()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n📊 Signed Packages Summary:");
    Console.ResetColor();

    // NuGet packages
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  NuGet Packages:");
    Console.ResetColor();
    var nupkgs = Directory.GetFiles(packagesDir, "*.nupkg");
    foreach (var pkg in nupkgs)
    {
        var size = new FileInfo(pkg).Length / 1024.0;
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"    📦 {Path.GetFileName(pkg)} [{size:N2} KB]");
        Console.ResetColor();
    }

    // Symbol packages
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  Symbol Packages:");
    Console.ResetColor();
    var snupkgs = Directory.GetFiles(packagesDir, "*.snupkg");
    foreach (var pkg in snupkgs)
    {
        var size = new FileInfo(pkg).Length / 1024.0;
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"    🔍 {Path.GetFileName(pkg)} [{size:N2} KB]");
        Console.ResetColor();
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n➡️  Locate your signed packages here: {packagesDir}");
    Console.ResetColor();
}

string? GetArgument(string name)
{
    var args = Environment.GetCommandLineArgs();
    var index = Array.IndexOf(args, name);
    return index >= 0 && index < args.Length - 1 ? args[index + 1] : null;
}

bool HasFlag(string name)
{
    return Environment.GetCommandLineArgs().Contains(name);
}

void PrintInfo(string message) => Console.WriteLine($"✓ {message}");

void PrintHeader(string message)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"=== {message} ===");
    Console.ResetColor();
    Console.WriteLine();
}

void PrintSubHeader(string message)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"📦 {message}");
    Console.ResetColor();
}