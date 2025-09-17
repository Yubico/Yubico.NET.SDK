<#
.SYNOPSIS
    Builds the Yubico.NET.SDK project locally and publishes to LocalNuGetFeed.
    Replicates the GitHub Actions build workflow for local development.

.DESCRIPTION
    This script performs the same build steps as the GitHub Actions workflow:
    1. Builds the solution in Release configuration
    2. Optionally generates documentation with DocFX
    3. Collects NuGet packages and symbols
    4. Publishes to the LocalNuGetFeed repository

.PARAMETER Version
    Override the version for the build. If not specified, uses version from build/Versions.props

.PARAMETER NuGetFeedName
    Name of the local NuGet feed to publish to. Default: "LocalNuGetFeed"

.PARAMETER NuGetFeedPath
    Path for the local NuGet feed directory. If not specified and feed doesn't exist, will prompt for path or use default

.PARAMETER IncludeDocumentation
    Include building documentation with DocFX (skipped by default for faster builds)

.PARAMETER DryRun
    Perform all build steps but don't actually publish to NuGet feed

.PARAMETER Clean
    Clean all bin/obj directories before building

.EXAMPLE
    .\Build-LocalRelease.ps1
    Standard build and publish to LocalNuGetFeed (no documentation)

.EXAMPLE
    .\Build-LocalRelease.ps1 -Version "1.2.3-local.123" -DryRun
    Build with custom version but don't publish

.EXAMPLE
    .\Build-LocalRelease.ps1 -IncludeDocumentation
    Build with documentation generation included

.EXAMPLE
    .\Build-LocalRelease.ps1 -NuGetFeedPath "C:\MyLocalNuGet" -Clean
    Build with clean and specify custom local NuGet feed path
#>

param(
    [string]$Version,
    [string]$NuGetFeedName = "LocalNuGetFeed",
    [string]$NuGetFeedPath,
    [switch]$IncludeDocumentation,
    [switch]$DryRun,
    [switch]$Clean
)

# Script configuration
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

# Paths and constants (relative to repository root)
$SolutionFile = "../Yubico.NET.SDK.sln"
$VersionPropsFile = "Versions.props"
$DocFxConfigFile = "../docfx.json"
$DocFxLogFile = "../docfx.log"

# Package output paths (matching GitHub workflow)
$PackagePaths = @(
    "../Yubico.Core/src/bin/Release/*.nupkg",
    "../Yubico.YubiKey/src/bin/Release/*.nupkg"
)

$SymbolPaths = @(
    "../Yubico.Core/src/bin/Release/*.snupkg",
    "../Yubico.YubiKey/src/bin/Release/*.snupkg"
)

function Write-Header {
    param([string]$Message)
    Write-Information ""
    Write-Information "=================================================="
    Write-Information $Message
    Write-Information "=================================================="
}

function Write-Step {
    param([string]$Message)
    Write-Information ""
    Write-Information ">>> $Message"
}

function Initialize-LocalNuGetFeed {
    Write-Step "Setting up local NuGet feed"

    # Determine feed path
    $feedPath = $NuGetFeedPath
    if ([string]::IsNullOrWhiteSpace($feedPath)) {
        # Default path for local feed
        $defaultFeedPath = Join-Path $env:USERPROFILE "LocalNuGetFeed"

        # Prompt user for path if not provided as parameter
        $feedPath = Read-Host "Enter path for LocalNuGetFeed (press Enter for default: $defaultFeedPath)"
        if ([string]::IsNullOrWhiteSpace($feedPath)) {
            $feedPath = $defaultFeedPath
        }
    }

    # Create directory if it doesn't exist
    if (-not (Test-Path $feedPath)) {
        Write-Information "Creating directory: $feedPath"
        New-Item -Path $feedPath -ItemType Directory -Force | Out-Null
    }

    # Add the NuGet source
    Write-Information "Adding NuGet source '$NuGetFeedName' at: $feedPath"

    try {
        dotnet nuget add source $feedPath --name $NuGetFeedName
        Write-Information "✓ Local NuGet feed '$NuGetFeedName' created and configured successfully"
        Write-Information "  Location: $feedPath"
    } catch {
        throw "Failed to add NuGet source '$NuGetFeedName'. Error: $($_.Exception.Message)"
    }
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"

    # Check if we're in the right directory
    if (-not (Test-Path $SolutionFile)) {
        throw "Solution file '$SolutionFile' not found. Please run this script from the repository root."
    }

    # Check .NET SDK
    Write-Step "Checking .NET SDK"
    try {
        $dotnetVersion = dotnet --version
        Write-Information "✓ .NET SDK version: $dotnetVersion"
    } catch {
        throw ".NET SDK not found. Please install .NET SDK matching global.json requirements."
    }

    # Check global.json for required .NET version
    if (Test-Path "../global.json") {
        $globalJson = Get-Content "../global.json" | ConvertFrom-Json
        $requiredVersion = $globalJson.sdk.version
        Write-Information "✓ Required .NET SDK version per global.json: $requiredVersion"
    }

    # Check if LocalNuGetFeed source exists, create if needed
    Write-Step "Checking NuGet feed configuration"
    $nugetSources = dotnet nuget list source
    if ($nugetSources -match $NuGetFeedName) {
        Write-Information "✓ NuGet source '$NuGetFeedName' is configured"
    } else {
        Write-Information "NuGet source '$NuGetFeedName' not found. Creating local feed..."
        Initialize-LocalNuGetFeed
    }

    # Check DocFX (install if needed and if including documentation)
    if ($IncludeDocumentation) {
        Write-Step "Checking DocFX"
        try {
            $docfxVersion = docfx --version 2>$null
            Write-Information "✓ DocFX is installed: $docfxVersion"
        } catch {
            Write-Information "Installing DocFX..."
            dotnet tool install --global docfx --version "2.*"
            Write-Information "✓ DocFX installed successfully"
        }
    }
}

function Set-BuildVersion {
    if ($Version) {
        Write-Header "Setting Custom Version: $Version"

        if (-not (Test-Path $VersionPropsFile)) {
            throw "Version properties file '$VersionPropsFile' not found."
        }

        # Backup original file
        Copy-Item $VersionPropsFile "$VersionPropsFile.backup"

        # Update version in XML
        [xml]$versionProps = Get-Content $VersionPropsFile
        $versionProps.Project.PropertyGroup.YubicoCoreVersion = $Version
        $versionProps.Project.PropertyGroup.YubicoYubiKeyVersion = $Version
        $versionProps.Save((Resolve-Path $VersionPropsFile).Path)

        Write-Information "✓ Updated version to: $Version"
    }
}

function Restore-BuildVersion {
    if ($Version -and (Test-Path "$VersionPropsFile.backup")) {
        Write-Step "Restoring original version file"
        Move-Item "$VersionPropsFile.backup" $VersionPropsFile -Force
        Write-Information "✓ Version file restored"
    }
}

function Invoke-CleanBuild {
    Write-Header "Cleaning Build Artifacts"

    # Always clean old NuGet packages to ensure fresh builds
    Write-Step "Cleaning old NuGet packages"
    foreach ($path in $PackagePaths) {
        $files = Get-ChildItem $path -ErrorAction SilentlyContinue
        if ($files) {
            $files | Remove-Item -Force
            Write-Information "✓ Removed old packages: $($files.Name -join ', ')"
        }
    }

    foreach ($path in $SymbolPaths) {
        $files = Get-ChildItem $path -ErrorAction SilentlyContinue
        if ($files) {
            $files | Remove-Item -Force
            Write-Information "✓ Removed old symbol packages: $($files.Name -join ', ')"
        }
    }

    if ($Clean) {
        # Remove bin and obj directories only when explicitly requested
        Write-Step "Cleaning bin/obj directories"
        Get-ChildItem -Path ".." -Include "bin", "obj" -Recurse -Directory | Remove-Item -Recurse -Force

        # Clear NuGet cache
        Write-Step "Clearing NuGet cache"
        dotnet nuget locals all --clear

        Write-Information "✓ Full clean completed"
    } else {
        Write-Information "✓ Package artifacts cleaned (use -Clean for full clean)"
    }
}

function Invoke-BuildSolution {
    Write-Header "Building Solution"

    Write-Step "Running dotnet pack (Release configuration)"

    # This matches the exact command from GitHub workflow
    $buildArgs = @(
        "pack",
        "--configuration", "Release",
        "--nologo",
        "--verbosity", "minimal",
        $SolutionFile
    )

    & dotnet @buildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    Write-Information "✓ Solution built successfully"
}

function Invoke-BuildDocumentation {
    if (-not $IncludeDocumentation) {
        Write-Information "Skipping documentation build (use -IncludeDocumentation to enable)"
        return
    }

    Write-Header "Building Documentation"

    Write-Step "Running DocFX"

    # Remove old log file
    if (Test-Path $DocFxLogFile) {
        Remove-Item $DocFxLogFile
    }

    # This matches the exact command from GitHub workflow
    $docfxArgs = @(
        $DocFxConfigFile,
        "--logLevel", "warning",
        "--log", $DocFxLogFile,
        "--warningsAsErrors"
    )

    & docfx @docfxArgs

    if ($LASTEXITCODE -ne 0) {
        if (Test-Path $DocFxLogFile) {
            Write-Information "DocFX log contents:"
            Get-Content $DocFxLogFile | Write-Information
        }
        throw "Documentation build failed with exit code $LASTEXITCODE"
    }

    Write-Information "✓ Documentation built successfully"

    if (Test-Path "../docs/_site") {
        $docFiles = (Get-ChildItem "../docs/_site" -Recurse -File).Count
        Write-Information "✓ Generated $docFiles documentation files"
    }
}

function Get-BuildArtifacts {
    Write-Header "Collecting Build Artifacts"

    $packages = @()
    $symbols = @()

    # Collect NuGet packages
    Write-Step "Collecting NuGet packages"
    foreach ($path in $PackagePaths) {
        $files = Get-ChildItem $path -ErrorAction SilentlyContinue
        if ($files) {
            $packages += $files
            Write-Information "✓ Found: $($files.Name -join ', ')"
        }
    }

    # Collect symbol packages
    Write-Step "Collecting symbol packages"
    foreach ($path in $SymbolPaths) {
        $files = Get-ChildItem $path -ErrorAction SilentlyContinue
        if ($files) {
            $symbols += $files
            Write-Information "✓ Found: $($files.Name -join ', ')"
        }
    }

    if ($packages.Count -eq 0) {
        throw "No NuGet packages found. Build may have failed."
    }

    Write-Information "✓ Total packages: $($packages.Count), symbols: $($symbols.Count)"

    return @{
        Packages = $packages
        Symbols = $symbols
    }
}

function Publish-ToLocalFeed {
    param($Artifacts)

    if ($DryRun) {
        Write-Header "DRY RUN: Would Publish to $NuGetFeedName"
        Write-Information "Packages that would be published:"
        $Artifacts.Packages | ForEach-Object { Write-Information "  - $($_.Name)" }
        return
    }

    Write-Header "Publishing to $NuGetFeedName"

    foreach ($package in $Artifacts.Packages) {
        Write-Step "Publishing $($package.Name)"

        $publishArgs = @(
            "nuget", "push",
            $package.FullName,
            "--source", $NuGetFeedName
        )

        & dotnet @publishArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to publish $($package.Name) (exit code $LASTEXITCODE)"
        } else {
            Write-Information "✓ Published successfully"
        }
    }
}

function Show-BuildSummary {
    param($Artifacts)

    Write-Header "Build Summary"

    Write-Information "Solution: $SolutionFile"
    Write-Information "Configuration: Release"

    if ($Version) {
        Write-Information "Custom Version: $Version"
    }

    Write-Information ""
    Write-Information "Packages Built:"
    $Artifacts.Packages | ForEach-Object {
        Write-Information "  📦 $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)"
    }

    if ($Artifacts.Symbols.Count -gt 0) {
        Write-Information ""
        Write-Information "Symbol Packages:"
        $Artifacts.Symbols | ForEach-Object {
            Write-Information "  🔍 $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)"
        }
    }

    if ($IncludeDocumentation -and (Test-Path "../docs/_site")) {
        Write-Information ""
        Write-Information "📚 Documentation: Generated in docs/_site/"
    }

    Write-Information ""
    if ($DryRun) {
        Write-Information "🔍 DRY RUN completed - no packages were published"
    } else {
        Write-Information "✅ Build and publish completed successfully!"
        Write-Information "📡 Published to NuGet feed: $NuGetFeedName"
    }
}

# Main execution
try {
    Write-Header "Yubico.NET.SDK Local Release Build"
    Write-Information "Target NuGet Feed: $NuGetFeedName"
    Write-Information "Working Directory: $(Get-Location)"

    Test-Prerequisites
    Set-BuildVersion
    Invoke-CleanBuild
    Invoke-BuildSolution
    Invoke-BuildDocumentation

    $artifacts = Get-BuildArtifacts
    Publish-ToLocalFeed -Artifacts $artifacts
    Show-BuildSummary -Artifacts $artifacts

} catch {
    Write-Error "❌ Build failed: $($_.Exception.Message)"
    exit 1
} finally {
    # Always restore version file if we modified it
    Restore-BuildVersion
}