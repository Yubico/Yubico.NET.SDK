<#
.SYNOPSIS
    Builds Yubico.NativeShims for the current host platform and publishes to LocalNuGetFeed.

.DESCRIPTION
    This script builds the native Yubico.NativeShims library for the current operating system,
    creates a NuGet package containing only the current platform's binaries, and publishes
    it to the LocalNuGetFeed repository.

.PARAMETER Version
    Override the version for the NuGet package. If not specified, uses default "1.0.0-local"

.PARAMETER NuGetFeedName
    Name of the local NuGet feed to publish to. Default: "LocalNuGetFeed"

.PARAMETER NuGetFeedPath
    Path for the local NuGet feed directory. If not specified and feed doesn't exist, will prompt for path or use default

.PARAMETER DryRun
    Perform all build steps but don't actually publish to NuGet feed

.PARAMETER Clean
    Clean build directories before building

.EXAMPLE
    .\Build-NativeShims.ps1
    Build for current platform and publish to LocalNuGetFeed

.EXAMPLE
    .\Build-NativeShims.ps1 -Version "1.12.0-dev.123" -DryRun
    Build with custom version but don't publish

.EXAMPLE
    .\Build-NativeShims.ps1 -Clean -Version "1.12.1"
    Clean build with specific version
#>

param(
    [string]$Version = "1.0.0-local",
    [string]$NuGetFeedName = "LocalNuGetFeed",
    [string]$NuGetFeedPath,
    [switch]$DryRun,
    [switch]$Clean
)

# Script configuration
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

# Paths (relative to repository root)
$NativeShimsDir = "../Yubico.NativeShims"
$NuspecTemplatePath = Join-Path $NativeShimsDir "Yubico.NativeShims.nuspec"
$TempNuspecFileName = "Yubico.NativeShims.Local.nuspec"
$TempNuspecFilePath = Join-Path $NativeShimsDir $TempNuspecFileName

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

function Get-PlatformInfo {
    $os = $null
    $buildScript = $null
    $outputDirectories = @()
    $libraryName = $null

    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        $os = "Windows"
        $buildScript = "build-windows.ps1"
        # Windows builds all architectures
        $outputDirectories = @("win-x86", "win-x64", "win-arm64")
        $libraryName = "Yubico.NativeShims.dll"
    }
    elseif ($IsMacOS -or ($env:OSTYPE -and $env:OSTYPE.StartsWith("darwin"))) {
        $os = "macOS"
        $buildScript = "build-macOS.sh"
        # macOS builds both architectures
        $outputDirectories = @("osx-x64", "osx-arm64")
        $libraryName = "libYubico.NativeShims.dylib"
    }
    elseif ($IsLinux -or ($env:OSTYPE -and $env:OSTYPE.StartsWith("linux"))) {
        $os = "Linux"
        # For now, Linux builds single architecture based on host
        $archOutput = & uname -m 2>$null
        if ($archOutput -eq "aarch64" -or $archOutput -eq "arm64") {
            $buildScript = "build-linux-arm64.sh"
            $outputDirectories = @("linux-arm64")
        } else {
            $buildScript = "build-linux-amd64.sh"
            $outputDirectories = @("linux-x64")
        }
        $libraryName = "libYubico.NativeShims.so"
    }
    else {
        throw "Unsupported operating system. This script supports Windows, macOS, and Linux."
    }

    return @{
        OS = $os
        BuildScript = $buildScript
        OutputDirectories = $outputDirectories
        LibraryName = $libraryName
    }
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
    param($PlatformInfo)

    Write-Header "Checking Prerequisites"

    # Check if we're in the right directory
    if (-not (Test-Path $NativeShimsDir)) {
        throw "NativeShims directory '$NativeShimsDir' not found. Please run this script from the repository root."
    }

    # Check if build script exists
    $buildScriptPath = Join-Path $NativeShimsDir $PlatformInfo.BuildScript
    if (-not (Test-Path $buildScriptPath)) {
        throw "Build script '$buildScriptPath' not found."
    }

    # Check platform-specific prerequisites
    Write-Step "Checking platform prerequisites for $($PlatformInfo.OS) $($PlatformInfo.Architecture)"

    if ($PlatformInfo.OS -eq "Windows") {
        # Check for VCPKG
        if (-not $env:VCPKG_INSTALLATION_ROOT) {
            throw "VCPKG_INSTALLATION_ROOT environment variable not set. Please install VCPKG and set the environment variable."
        }
        if (-not (Test-Path $env:VCPKG_INSTALLATION_ROOT)) {
            throw "VCPKG installation not found at: $env:VCPKG_INSTALLATION_ROOT"
        }

        # Check for cmake
        try {
            $cmakeVersion = cmake --version 2>$null
            Write-Information "✓ CMake found"
        } catch {
            throw "CMake not found. Please install CMake and ensure it's in your PATH."
        }
    }
    elseif ($PlatformInfo.OS -eq "macOS") {
        # Check for VCPKG
        if (-not $env:VCPKG_INSTALLATION_ROOT) {
            throw "VCPKG_INSTALLATION_ROOT environment variable not set. Please install VCPKG and set the environment variable."
        }

        # Check for cmake and basic tools
        try {
            cmake --version | Out-Null
            Write-Information "✓ CMake found"
        } catch {
            throw "CMake not found. Install with: brew install cmake"
        }
    }
    elseif ($PlatformInfo.OS -eq "Linux") {
        # Check for basic build tools
        try {
            cmake --version | Out-Null
            Write-Information "✓ CMake found"
        } catch {
            throw "CMake not found. Install with: sudo apt-get install cmake"
        }

        try {
            gcc --version | Out-Null
            Write-Information "✓ GCC found"
        } catch {
            throw "GCC not found. Install with: sudo apt-get install build-essential"
        }
    }

    # Check NuGet feed
    Write-Step "Checking NuGet feed configuration"
    $nugetSources = dotnet nuget list source
    if ($nugetSources -match $NuGetFeedName) {
        Write-Information "✓ NuGet source '$NuGetFeedName' is configured"
    } else {
        Write-Information "NuGet source '$NuGetFeedName' not found. Creating local feed..."
        Initialize-LocalNuGetFeed
    }

    # Check for nuget tool
    try {
        nuget help | Out-Null
        Write-Information "✓ NuGet CLI found"
    } catch {
        throw "NuGet CLI not found. Please install nuget.exe and ensure it's in your PATH."
    }
}

function Invoke-CleanBuild {
    param($PlatformInfo)

    if ($Clean) {
        Write-Header "Cleaning Build Artifacts"

        Push-Location $NativeShimsDir
        try {
            # Remove platform-specific build directories and output directories
            $dirsToClean = @("build32", "build64", "buildarm", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "win-x86", "win-x64", "win-arm64")

            # Add the current platform's output directories
            $dirsToClean += $PlatformInfo.OutputDirectories

            foreach ($dir in ($dirsToClean | Sort-Object -Unique)) {
                if (Test-Path $dir) {
                    Write-Information "Removing $dir"
                    Remove-Item $dir -Recurse -Force
                }
            }

            Write-Information "✓ Build artifacts cleaned"
        }
        finally {
            Pop-Location
        }
    }
}

function Invoke-NativeBuild {
    param($PlatformInfo, $BaseVersion)

    Write-Header "Building NativeShims for $($PlatformInfo.OS)"

    Push-Location $NativeShimsDir
    try {
        $buildScriptPath = "./$($PlatformInfo.BuildScript)"

        Write-Step "Executing build script: $buildScriptPath"
        if ($BaseVersion) {
            Write-Information "Passing version parameter: $BaseVersion"
        }

        if ($PlatformInfo.OS -eq "Windows") {
            if ($BaseVersion) {
                & powershell.exe -ExecutionPolicy Bypass -File $buildScriptPath -Version $BaseVersion
            } else {
                & powershell.exe -ExecutionPolicy Bypass -File $buildScriptPath
            }
        } else {
            if ($BaseVersion) {
                & bash $buildScriptPath $BaseVersion
            } else {
                & bash $buildScriptPath
            }
        }

        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }

        # Verify outputs exist
        foreach ($outputDir in $PlatformInfo.OutputDirectories) {
            $outputPath = Join-Path $outputDir $PlatformInfo.LibraryName
            if (-not (Test-Path $outputPath)) {
                throw "Build output not found at: $outputPath"
            }

            $fileSize = (Get-Item $outputPath).Length
            Write-Information "✓ Built $outputDir ($([math]::Round($fileSize / 1KB, 2)) KB)"
        }
        Write-Information "✓ Build completed successfully for all architectures"
    }
    finally {
        Pop-Location
    }
}

function Set-NativeShimsVersion {
    param($Version)

    # Extract base version (major.minor.patch) from full version like the GitHub workflow does
    $baseVersion = $Version.Split('-')[0]

    Write-Header "Setting NativeShims Version"
    Write-Information "Full version: $Version"
    Write-Information "Base version (for CMake): $baseVersion"
    Write-Information "✓ Version will be passed dynamically to CMake (no file modification needed)"

    # Return the base version for use by build scripts
    return $baseVersion
}

function New-PlatformNuspec {
    param($PlatformInfo, $Version)

    Write-Header "Creating Platform-Specific NuGet Package"

    $architectures = $PlatformInfo.OutputDirectories -join ", "
    Write-Step "Generating nuspec for $($PlatformInfo.OS) ($architectures)"

    # Read original nuspec
    [xml]$nuspec = Get-Content $NuspecTemplatePath

    # Update metadata
    $nuspec.package.metadata.id = "Yubico.NativeShims"
    $nuspec.package.metadata.version = $Version
    $nuspec.package.metadata.description = `
        "LOCAL BUILD - Yubico.NativeShims for $($PlatformInfo.OS) ($architectures). " +
        "This package contains native binaries for the built platform architectures."

    # Clear existing files and add current platform binaries
    $nuspec.package.RemoveChild($nuspec.package.files)
    $filesElement = $nuspec.CreateElement("files")
    $nuspec.package.AppendChild($filesElement)

    # Add all built platform binaries
    foreach ($outputDir in $PlatformInfo.OutputDirectories) {
        $fileElement = $nuspec.CreateElement("file")
        $fileElement.SetAttribute("src", "$outputDir/$($PlatformInfo.LibraryName)")
        $fileElement.SetAttribute("target", "runtimes/$outputDir/native/$($PlatformInfo.LibraryName)")
        $filesElement.AppendChild($fileElement)
    }

    # Add support files (keep these from original)
    $supportFiles = @(
        @{ src = "msbuild/Yubico.NativeShims.targets"; target = "build/net47/Yubico.NativeShims.targets" },
        @{ src = "msbuild/Yubico.NativeShims.targets"; target = "buildTransitive/net47/Yubico.NativeShims.targets" },
        @{ src = "msbuild/_._"; target = "lib/net47/_._" },
        @{ src = "msbuild/_._"; target = "lib/netstandard20/_._" },
        @{ src = "readme.md"; target = "docs/" }
    )

    foreach ($file in $supportFiles) {
        $fileElement = $nuspec.CreateElement("file")
        $fileElement.SetAttribute("src", $file.src)
        $fileElement.SetAttribute("target", $file.target)
        $filesElement.AppendChild($fileElement)
    }

    # Save platform-specific nuspec
    $absolutePath = (Resolve-Path $NativeShimsDir).Path
    $absoluteNuspecPath = Join-Path $absolutePath $TempNuspecFileName
    $nuspec.Save($absoluteNuspecPath)

    Write-Information "✓ Created platform-specific nuspec: $TempNuspecFilePath"
    Write-Information "  Package ID: Yubico.NativeShims"
    Write-Information "  Version: $Version"
    Write-Information "  Architectures: $($PlatformInfo.OutputDirectories -join ', ')"
}

function New-NuGetPackage {
    param($PlatformInfo)

    Write-Step "Creating NuGet package"

    Push-Location $NativeShimsDir
    try {
        # Create NuGet package (suppress output to avoid capturing it)
        $null = nuget pack "$TempNuspecFileName"

        if ($LASTEXITCODE -ne 0) {
            throw "NuGet pack failed with exit code $LASTEXITCODE"
        }

        # Find generated package
        $packagePattern = "Yubico.NativeShims.*.nupkg"
        $packageFile = Get-ChildItem $packagePattern | Select-Object -First 1

        if (-not $packageFile) {
            throw "Generated NuGet package not found matching pattern: $packagePattern"
        }

        Write-Information "✓ NuGet package created: $($packageFile.Name)"
        Write-Information "  Size: $([math]::Round($packageFile.Length / 1KB, 2)) KB"

        # Return absolute path to ensure it's accessible from calling context
        $absolutePackagePath = $packageFile.FullName
        Write-Information "  Full path: $absolutePackagePath"
        return $absolutePackagePath
    }
    finally {
        Pop-Location
    }
}

function Publish-ToLocalFeed {
    param($PackagePath)

    if ($DryRun) {
        Write-Header "DRY RUN: Would Publish to $NuGetFeedName"
        Write-Information "Package that would be published:"
        Write-Information "  - $(Split-Path $PackagePath -Leaf)"
        return
    }

    Write-Header "Publishing to $NuGetFeedName"

    Write-Step "Publishing $(Split-Path $PackagePath -Leaf)"

    $publishArgs = @(
        "nuget", "push",
        $PackagePath,
        "--source", $NuGetFeedName
    )

    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to publish package (exit code $LASTEXITCODE)"
    } else {
        Write-Information "✓ Published successfully"
    }
}

function Show-BuildSummary {
    param($PlatformInfo, $PackagePath)

    Write-Header "Build Summary"

    Write-Information "Platform: $($PlatformInfo.OS)"
    Write-Information "Architectures: $($PlatformInfo.OutputDirectories -join ', ')"
    Write-Information "Version: $Version"

    if ($PackagePath) {
        $packageInfo = Get-Item $PackagePath
        Write-Information ""
        Write-Information "Package Created:"
        Write-Information "  📦 $($packageInfo.Name) ($([math]::Round($packageInfo.Length / 1KB, 2)) KB)"
        Write-Information "  📍 $($packageInfo.FullName)"
    }

    Write-Information ""
    if ($DryRun) {
        Write-Information "🔍 DRY RUN completed - no package was published"
    } else {
        Write-Information "✅ Build and publish completed successfully!"
        Write-Information "📡 Published to NuGet feed: $NuGetFeedName"
    }
}

# Main execution
try {
    Write-Header "Yubico.NativeShims Local Platform Build"
    Write-Information "Working Directory: $(Get-Location)"

    # Detect current platform
    $platformInfo = Get-PlatformInfo
    Write-Information "Detected Platform: $($platformInfo.OS)"
    Write-Information "Target Architectures: $($platformInfo.OutputDirectories -join ', ')"
    Write-Information "Target NuGet Feed: $NuGetFeedName"

    Test-Prerequisites -PlatformInfo $platformInfo
    $baseVersion = Set-NativeShimsVersion -Version $Version
    Invoke-CleanBuild -PlatformInfo $platformInfo
    Invoke-NativeBuild -PlatformInfo $platformInfo -BaseVersion $baseVersion
    New-PlatformNuspec -PlatformInfo $platformInfo -Version $Version
    $packagePath = New-NuGetPackage -PlatformInfo $platformInfo

    if ([string]::IsNullOrWhiteSpace($packagePath)) {
        throw "Package path is empty. NuGet package creation may have failed."
    }

    Write-Information "Package path for publishing: $packagePath"
    Publish-ToLocalFeed -PackagePath $packagePath
    Show-BuildSummary -PlatformInfo $platformInfo -PackagePath $packagePath

} catch {
    Write-Error "❌ Build failed: $($_.Exception.Message)"
    exit 1
} finally {
    # Clean up temporary files
    $cleanupPath = Join-Path (Resolve-Path $NativeShimsDir -ErrorAction SilentlyContinue).Path $TempNuspecFileName
    if (Test-Path $cleanupPath -ErrorAction SilentlyContinue) {
        Remove-Item $cleanupPath -Force
    }
}