# Helper Functions
function Sign-SingleFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        
        [Parameter(Mandatory = $true)]
        [string]$Thumbprint,
        
        [Parameter(Mandatory = $true)]
        [string]$SignToolPath,
        
        [Parameter(Mandatory = $true)]
        [string]$TimestampServer
    )
    
    $signParams = @(
        "sign", "/fd", "SHA256",
        "/sha1", $Thumbprint,
        "/t", $TimestampServer,
        $FilePath
    )

    $output = & $SignToolPath @signParams 2>&1
    if ($LASTEXITCODE -ne 0) {
        $output | ForEach-Object { Write-Host $_ }
        throw "Signing failed for file: $FilePath"
    }
}

function Clean-Directory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$BaseDirectory
    )
    
    Write-Host "`nCleaning up working directories..." -ForegroundColor Yellow
    
    # Only clean unsigned and signed directories
    $dirsToClean = @(
        Join-Path $BaseDirectory "unsigned"
        Join-Path $BaseDirectory "signed"
    )
    
    foreach ($dir in $dirsToClean) {
        if (Test-Path $dir) {
            Write-Host "Removing: $dir"
            Remove-Item $dir -Recurse -Force
        }
    }
    Write-Host "✓ Cleanup completed"
}

function Test-RequiredAssets {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        
        [Parameter(Mandatory = $true)]
        [string]$NuGetPackagesZip,
        
        [Parameter(Mandatory = $true)]
        [string]$SymbolsPackagesZip
    )

    Write-Host "`nValidating required build assets..."
    $requiredFiles = @{
        $NuGetPackagesZip   = "NuGet packages"
        $SymbolsPackagesZip = "Symbol packages"
    }
    
    foreach ($required in $requiredFiles.GetEnumerator()) {
        $found = Get-ChildItem -Path $WorkingDirectory -Filter $required.Key -ErrorAction SilentlyContinue
        if (-not $found) {
            throw "Required build asset not found: $($required.Key)`nThis file should contain $($required.Value)"
        }

        Write-Host "  ✅ Found $($required.Value) in: $($found.Name)" -ForegroundColor Green
    }
}

function Initialize-DirectoryStructure {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$BaseDirectory
    )

    $directories = @{
        WorkingDir = $BaseDirectory
        Unsigned   = Join-Path $BaseDirectory "unsigned"
        Signed     = Join-Path $BaseDirectory "signed"
        Libraries  = Join-Path $BaseDirectory "signed\libraries"
        Packages   = Join-Path $BaseDirectory "signed\packages"
    }

    Write-Host "`nCreating directory structure..."
    # Only create the directories we'll manage
    $directories.Keys | Where-Object { $_ -ne 'WorkingDir' } | ForEach-Object {
        $dir = $directories[$_]
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Host "✓ Created: $dir"
        }
    }

    return $directories
}

function Test-GithubAttestation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        
        [Parameter(Mandatory = $true)]
        [string]$RepoName
    )
    
    # Get the parent directory name and the file name
    $fileName = (Get-ChildItem $FilePath).Name

    Write-Host "  🔐 Verifying attestation for: ..$parentDir\$fileName" -ForegroundColor Gray
    
    try {
        $output = gh attestation verify $FilePath --repo $RepoName 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host $output -ForegroundColor Red
            throw $output  # This will trigger the catch block
        }
        
        Write-Host "    ✅ Verified" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "    ❌ Verification failed: $_" -ForegroundColor Red
        return $false
    }
}

<#
.SYNOPSIS
Signs NuGet and Symbol packages using a smart card certificate.

.DESCRIPTION
Signs NuGet packages (*.nupkg) and their corresponding symbol packages (*.snupkg) using a hardware-based certificate. 
The script processes the contents of two required zip files ('Nuget Packages.zip' and 'Symbols Packages.zip'), 
signs all assemblies within the NuGet packages, repacks them, and then signs both the NuGet and Symbol packages.

How to use:
1. Create a release folder on your machine e.g. ../releases/1.12
2. Download the build assets "Nuget Packages.zip" and "Symbols Packages.zip" from the latest SDK build action 
   to the newly created folder.
3. Start a Powershell terminal, and load the script by running the following command:
   > . \.Yubico.NET.SDK\build\sign.ps1
4. The script can be invoked by following the examples below.

.PARAMETER Thumbprint
The thumbprint of the signing certificate stored on the smart card.

.PARAMETER WorkingDirectory
The directory containing the zip files and where the signing process will take place.

.PARAMETER SignToolPath
Optional. Path to signtool.exe. Defaults to "signtool.exe" (expects it in PATH).

.PARAMETER NuGetPath
Optional. Path to nuget.exe. Defaults to "nuget.exe" (expects it in PATH).

.PARAMETER TimestampServer
Optional. URL of the timestamp server. Defaults to "http://timestamp.digicert.com".

.PARAMETER NuGetPackagesZip
Optional. Name of the NuGet packages zip file. Defaults to "Nuget Packages.zip".

.PARAMETER SymbolsPackagesZip
Optional. Name of the symbols packages zip file. Defaults to "Symbols Packages.zip".

.PARAMETER CleanWorkingDirectory
Optional switch. If specified, cleans the working directories before processing.

.EXAMPLE
Invoke-NuGetPackageSigning -Thumbprint "0123456789ABCDEF" -WorkingDirectory "C:\Signing"

.EXAMPLE
Invoke-NuGetPackageSigning -Thumbprint "0123456789ABCDEF" -WorkingDirectory "C:\Signing" -CleanWorkingDirectory -NuGetPath "C:\Tools\nuget.exe"

.NOTES
Requires:
- A smart card with the signing certificate
- Github CLI for attestation
- signtool.exe (Windows SDK)
- nuget.exe
- PowerShell 5.1 or later
#>
function Invoke-NuGetPackageSigning {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Thumbprint,
        
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        
        [Parameter(Mandatory = $false)]
        [string]$SignToolPath = "signtool.exe",
        
        [Parameter(Mandatory = $false)]
        [string]$NuGetPath = "nuget.exe",
        
        [Parameter(Mandatory = $false)]
        [string]$TimestampServer = "http://timestamp.digicert.com",
        
        [Parameter(Mandatory = $false)]
        [string]$NuGetPackagesZip = "Nuget Packages.zip",
        
        [Parameter(Mandatory = $false)]
        [string]$SymbolsPackagesZip = "Symbols Packages.zip",
        
        [Parameter(Mandatory = $false)]
        [switch]$CleanWorkingDirectory
    )

    try {
        Write-Host "`nInitializing NuGet package signing process..." -ForegroundColor Cyan
        
        # Validate tools existence
        Write-Host "`nVerifying required tools..."
        if (-not (Get-Command $SignToolPath -ErrorAction SilentlyContinue)) {
            throw "SignTool not found at path: $SignToolPath"
        }
        Write-Host "✓ SignTool found at: $SignToolPath"
        
        if (-not (Get-Command $NuGetPath -ErrorAction SilentlyContinue)) {
            throw "NuGet not found at path: $NuGetPath"
        }
        Write-Host "✓ NuGet found at: $NuGetPath"

        if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
            throw "GitHub CLI installed or not found in PATH"
        }
        Write-Host "✓ GitHub CLI found at: $NuGetPath"

        # Verify certificate is available and log details
        $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $Thumbprint }
        if (-not $cert) {
            throw "Certificate with thumbprint $Thumbprint not found in current user store"
        }

        Write-Host "`nCertificate Details:" -ForegroundColor Cyan
        Write-Host "  Subject:      $($cert.Subject)"
        Write-Host "  Issuer:       $($cert.Issuer)"
        Write-Host "  Thumbprint:   $($cert.Thumbprint)"
        Write-Host "  Valid From:   $($cert.NotBefore)"
        Write-Host "  Valid To:     $($cert.NotAfter)"

        if ($cert.NotAfter -le (Get-Date).AddMonths(1)) {
            Write-Warning "Certificate will expire within one month on $($cert.NotAfter)"
        }

        # Clean if requested
        if ($CleanWorkingDirectory) {
            Clean-Directory -BaseDirectory $WorkingDirectory
        }

        # Initialize directory structure
        $directories = Initialize-DirectoryStructure -BaseDirectory $WorkingDirectory
        
        # Validate required zip files
        Test-RequiredAssets -WorkingDirectory $WorkingDirectory -NuGetPackagesZip $NuGetPackagesZip -SymbolsPackagesZip $SymbolsPackagesZip

        # Process each zip file
        Write-Host "`n📦 Processing ZIP files..." -ForegroundColor Yellow
        $zipFiles = Get-ChildItem -Path $WorkingDirectory -Filter "*.zip"
        foreach ($zip in $zipFiles) {
            Write-Host "`n  🔄 Processing: $($zip.Name)" -ForegroundColor Cyan
            
            $extractPath = Join-Path $directories.Unsigned ([System.IO.Path]::GetFileNameWithoutExtension($zip.Name))
            Write-Host "    📂 Extracting to: $extractPath" -ForegroundColor Gray
            Expand-Archive -Path $zip.FullName -DestinationPath $extractPath -Force

            Write-Host "    📋 Copying packages to unsigned directory" -ForegroundColor Gray
            $packages = Get-ChildItem -Path $extractPath -Recurse -Include *.nupkg, *.snupkg
            foreach ($package in $packages) {
                Write-Host "  Copying: $($package.Name)"

                # Verify GitHub attestation (that the file has been downloaded from our repo)
                if (-not (Test-GithubAttestation -FilePath $package.FullName -RepoName "Yubico/Yubico.NET.SDK")) {
                    throw "Attestation verification failed for: $($package.Name)"
                }

                Copy-Item -Path $package.FullName -Destination $directories.Unsigned -Force
            }
            Write-Host "✓ Copied $($packages.Count) package(s)"
        }

        # First process nupkg files to sign their contents
        Write-Host "`n📦 Processing NuGet packages..." -ForegroundColor Yellow
        $nugetPackages = Get-ChildItem -Path $directories.Unsigned -Filter "*.nupkg"
        foreach ($package in $nugetPackages) {
            Write-Host "`nSigning contents of: $($package.Name)"
            
            $extractPath = Join-Path $directories.Libraries ([System.IO.Path]::GetFileNameWithoutExtension($package.Name))
            Write-Host "Extracting to: $extractPath"
            Expand-Archive -Path $package.FullName -DestinationPath $extractPath -Force

            Write-Host "Cleaning package structure"
            Get-ChildItem -Path $extractPath -Recurse -Include "_rels", "package" | Remove-Item -Force -Recurse
            Get-ChildItem -Path $extractPath -Recurse -Filter '[Content_Types].xml' | Remove-Item -Force

            Write-Host "Signing assemblies..."
            $dlls = Get-ChildItem -Path $extractPath -Include "*.dll" -Recurse
            foreach ($dll in $dlls) {
                # Get the parent directory name (framework target) and the file name
                $frameworkDir = Split-Path (Split-Path $dll.FullName -Parent) -Leaf
                $fileName = Split-Path $dll.FullName -Leaf
                Write-Host "    ✍️ Signing: ..\$frameworkDir\$fileName" -ForegroundColor Gray
                Sign-SingleFile -FilePath $dll.FullName -Thumbprint $Thumbprint -SignToolPath $SignToolPath -TimestampServer $TimestampServer
            }

            Write-Host "Repacking assemblies..."
            Get-ChildItem -Path $extractPath -Recurse -Filter "*.nuspec" |
            ForEach-Object { 
                Write-Host "  Packing: $($_.Name)"
                $output = & $NuGetPath pack $_.FullName -OutputDirectory $directories.Packages 2>&1
                
                if ($LASTEXITCODE -ne 0) {
                    $output | ForEach-Object { Write-Host $_ }
                    throw "Signing failed for file: $FilePath"
                }
            }
        }

        # Copy symbol packages to output directory
        Write-Host "`nCopying symbol packages..." -ForegroundColor Yellow
        $symbolPackages = Get-ChildItem -Path $directories.Unsigned -Filter "*.snupkg"
        foreach ($package in $symbolPackages) {
            Write-Host "  Copying: $($package.Name)"
            Copy-Item -Path $package.FullName -Destination $directories.Packages -Force
        }

        # Sign all final packages (both nupkg and snupkg)
        Write-Host "`n🔏 Signing final packages..." -ForegroundColor Cyan
        $finalPackages = Get-ChildItem -Path $directories.Packages -Include *.nupkg, *.snupkg -Recurse
        foreach ($package in $finalPackages) {
            Write-Host "  ✒️ Signing package: $($package.Name)" -ForegroundColor White
            $nugetSignParams = @(
                "sign", $package.FullName,
                "-CertificateFingerprint", $Thumbprint,
                "-Timestamper", $TimestampServer,
                "-NonInteractive"
            )

            $output = & $NuGetPath @nugetSignParams 2>&1
                
            if ($LASTEXITCODE -ne 0) {
                $output | ForEach-Object { Write-Host $_ }
                throw "Signing failed for file: $FilePath"
            }
        }

        # Print summary of signed packages
        Write-Host "`n📊 Signed Packages Summary:" -ForegroundColor Yellow
        Write-Host "  NuGet Packages:" -ForegroundColor White
        Get-ChildItem -Path $directories.Packages -Filter "*.nupkg" | ForEach-Object {
            $size = "{0:N2}" -f ($_.Length / 1KB)
            Write-Host "    📦 $($_.Name) [$size KB]" -ForegroundColor Gray
        }
            
        Write-Host "  Symbol Packages:" -ForegroundColor White
        Get-ChildItem -Path $directories.Packages -Filter "*.snupkg" | ForEach-Object {
            $size = "{0:N2}" -f ($_.Length / 1KB)
            Write-Host "    🔍 $($_.Name) [$size KB]" -ForegroundColor Gray
        }

        Write-Host "`n✨ Package signing process completed successfully! ✨" -ForegroundColor Green
        return $directories.Packages
    }
    catch {
        Write-Host "`n❌ Error occurred:" -ForegroundColor Red
        Write-Error $_.Exception.Message
        Clean-Directory -BaseDirectory $WorkingDirectory
        throw
    }
}