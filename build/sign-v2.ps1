# Helper Functions
#
# ============================================================================
# sign-v2.ps1 - NuGet release signing via the .NET Sign CLI (dotnet/sign).
#
# This is the replacement for sign.ps1. It signs the assemblies *inside* each
# .nupkg/.snupkg and re-signs the container in place, without the lossy
# extract -> strip -> `nuget pack` regenerate round-trip that sign.ps1 used.
#
# What is kept from sign.ps1:
#   - required-asset validation (Test-RequiredAssets)
#   - GitHub attestation verification before signing (Test-GithubAttestation)
#   - certificate expiry warning
#   - signed-package summary
#   - the push function (Invoke-NuGetPackagePush), verbatim - never auto-run
#
# What is removed (the Sign CLI does it internally, in place):
#   - Expand-Archive of each package
#   - stripping _rels / package / [Content_Types].xml
#   - per-DLL signtool loop
#   - `nuget pack` regeneration from the extracted .nuspec
#   - the separate final `nuget sign` loop
#
# Prerequisites:
#   - .NET 8+ SDK
#   - Sign CLI: dotnet tool install --global sign --prerelease
#     (invoke via -SignCliPath if it is not on PATH, e.g. the repo `sign`
#     shim shadows it)
#   - GitHub CLI (gh), authenticated
#   - The signing certificate in Cert:\CurrentUser\My with its private key on
#     the YubiKey (CNG Smart Card KSP)
#
# NOTE: dot-source EITHER sign.ps1 OR sign-v2.ps1 in a session, not both - they
# share helper function names by design so this file can replace sign.ps1 later.
# ============================================================================

function Clean-Directory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$BaseDirectory
    )

    Write-Host "`nCleaning up working directories..." -ForegroundColor Yellow

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
    Write-Host "OK Cleanup completed"
}

function Test-RequiredAssets {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [string]$NuGetPackagesZip,

        [Parameter(Mandatory = $false)]
        [string]$SymbolsPackagesZip,

        [Parameter(Mandatory = $false)]
        [string]$NativeShimsZip
    )

    Write-Host "`nValidating required build assets..."

    $hasCorePackages = -not [string]::IsNullOrWhiteSpace($NuGetPackagesZip) -and -not [string]::IsNullOrWhiteSpace($SymbolsPackagesZip)
    $hasNativeShims = -not [string]::IsNullOrWhiteSpace($NativeShimsZip)

    if (-not $hasCorePackages -and -not $hasNativeShims) {
        throw "No package files specified. Please provide either core packages or native shims package paths."
    }

    if ($hasCorePackages) {
        Write-Host "  Validating core packages..." -ForegroundColor Cyan
        $coreFiles = @{
            $NuGetPackagesZip   = "NuGet packages"
            $SymbolsPackagesZip = "Symbol packages"
        }

        foreach ($required in $coreFiles.GetEnumerator()) {
            $found = Get-ChildItem -Path $WorkingDirectory -Filter $required.Key -ErrorAction SilentlyContinue
            if (-not $found) {
                throw "Required build asset not found: $($required.Key)`nThis file should contain $($required.Value)"
            }
            Write-Host "    OK Found $($required.Value) in: $($found.Name)" -ForegroundColor Green
        }
    }

    if ($hasNativeShims) {
        Write-Host "  Validating native shims package..." -ForegroundColor Cyan
        $found = Get-ChildItem -Path $WorkingDirectory -Filter $NativeShimsZip -ErrorAction SilentlyContinue
        if (-not $found) {
            throw "Required native shims asset not found: $NativeShimsZip"
        }
        Write-Host "    OK Found Native Shims package in: $($found.Name)" -ForegroundColor Green
    }
}

function Test-GithubAttestation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string]$RepoName
    )

    $fileName = (Get-ChildItem $FilePath).Name
    Write-Host "      Verifying attestation for: $fileName" -ForegroundColor Gray

    try {
        $output = gh attestation verify $FilePath --repo $RepoName 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host $output -ForegroundColor Red
            throw $output
        }

        Write-Host "        OK Verified" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "        FAILED Verification failed: $_" -ForegroundColor Red
        return $false
    }
}

function Resolve-SigningCertificate {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Fingerprint
    )

    # The Sign CLI (and NuGet) identify the certificate by a SHA-256/384/512
    # fingerprint (the hash over the DER-encoded certificate), NOT the SHA-1
    # thumbprint that sign.ps1 used. Reject a 40-hex SHA-1 value early with an
    # actionable message.
    $fp = $Fingerprint -replace '[^0-9A-Fa-f]', ''
    if ($fp.Length -eq 40) {
        throw @"
The value '$Fingerprint' looks like a SHA-1 thumbprint (40 hex chars).
The Sign CLI requires a SHA-256 (64), SHA-384 (96) or SHA-512 (128) fingerprint.
Derive the SHA-256 fingerprint from the certificate, e.g.:

  `$c = Get-ChildItem Cert:\CurrentUser\My | Where-Object Thumbprint -eq '$Fingerprint'
  [BitConverter]::ToString(
    [System.Security.Cryptography.SHA256]::Create().ComputeHash(`$c.RawData)
  ).Replace('-','')
"@
    }
    if ($fp.Length -notin 64, 96, 128) {
        throw "Certificate fingerprint must be a SHA-256 (64), SHA-384 (96) or SHA-512 (128) hex string. Got $($fp.Length) hex chars."
    }

    $algo = switch ($fp.Length) { 64 { 'SHA256' } 96 { 'SHA384' } 128 { 'SHA512' } }
    # HashAlgorithm.Create(string) is obsolete and can return $null on modern
    # .NET/PowerShell; use the algorithm-specific Create() instead.
    $hasher = switch ($algo) {
        'SHA256' { [System.Security.Cryptography.SHA256]::Create() }
        'SHA384' { [System.Security.Cryptography.SHA384]::Create() }
        'SHA512' { [System.Security.Cryptography.SHA512]::Create() }
    }

    $match = $null
    try {
        foreach ($c in (Get-ChildItem Cert:\CurrentUser\My)) {
            $certFp = [BitConverter]::ToString($hasher.ComputeHash($c.RawData)).Replace('-', '')
            if ($certFp -eq $fp) { $match = $c; break }
        }
    }
    finally {
        $hasher.Dispose()
    }

    if (-not $match) {
        throw "Certificate with $algo fingerprint $fp not found in Cert:\CurrentUser\My"
    }

    return @{ Certificate = $match; Fingerprint = $fp }
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
        Packages   = Join-Path $BaseDirectory "signed\packages"
    }

    $directories.Keys | Where-Object { $_ -ne 'WorkingDir' } | ForEach-Object {
        $dir = $directories[$_]
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Debug "Created: $dir"
        }
    }

    return $directories
}

function Expand-PackagesFromZip {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ZipFile,

        [Parameter(Mandatory = $true)]
        [hashtable]$Directories,

        [Parameter(Mandatory = $true)]
        [string]$RepoName
    )

    Write-Host "`n  Extracting: $ZipFile" -ForegroundColor Cyan

    $zipPath = Join-Path $Directories.WorkingDir $ZipFile
    $extractPath = Join-Path $Directories.Unsigned ([System.IO.Path]::GetFileNameWithoutExtension($ZipFile))
    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

    $packages = Get-ChildItem -Path $extractPath -Recurse -Include *.nupkg, *.snupkg
    foreach ($package in $packages) {
        Write-Host "      Package: $($package.Name)"

        # Verify GitHub attestation on the unsigned artifact before we alter its bytes.
        if (-not (Test-GithubAttestation -FilePath $package.FullName -RepoName $RepoName)) {
            throw "Attestation verification failed for: $($package.Name)"
        }

        Copy-Item -Path $package.FullName -Destination $Directories.Unsigned -Force
    }
    Write-Host "    OK Staged $($packages.Count) package(s)"
}

function Invoke-SignCliOnPackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackagePath,

        [Parameter(Mandatory = $true)]
        [string]$OutputDirectory,

        [Parameter(Mandatory = $true)]
        [string]$SignCliPath,

        [Parameter(Mandatory = $true)]
        [string]$Fingerprint,

        [Parameter(Mandatory = $true)]
        [string]$TimestampServer,

        [Parameter(Mandatory = $false)]
        [string]$FileList,

        [Parameter(Mandatory = $false)]
        [switch]$Interactive
    )

    $outputFile = Join-Path $OutputDirectory (Split-Path $PackagePath -Leaf)

    # certificate-store with fingerprint-only lookup. For a CNG smart-card key we
    # deliberately OMIT --crypto-service-provider / --key-container: those are the
    # legacy CSP path and cause LegacyKeySpec failures against a KSP-backed key
    # (dotnet/sign #780). Fingerprint-only resolves the key via GetRSAPrivateKey().
    #
    # --max-concurrency 1 serialises packages. NOTE: the Sign CLI still signs the
    # DLLs *within* a single package concurrently (unbounded Parallel.ForEachAsync);
    # if that inner concurrency ever proves unsafe against the single PIV session,
    # the mitigation is to invoke this per assembly with a one-entry --file-list.
    $signArgs = @(
        'code', 'certificate-store',
        '--certificate-fingerprint', $Fingerprint,
        '--file-digest', 'SHA256',
        '--timestamp-url', $TimestampServer,
        '--timestamp-digest', 'SHA256',
        '--max-concurrency', '1',
        '--output', $outputFile,
        '--verbosity', 'information'
    )
    if (-not [string]::IsNullOrWhiteSpace($FileList)) {
        $signArgs += @('--file-list', $FileList)
    }
    if ($Interactive) {
        $signArgs += '--interactive'
    }
    $signArgs += $PackagePath

    Write-Host "    Signing: $(Split-Path $PackagePath -Leaf)" -ForegroundColor White
    $output = & $SignCliPath @signArgs 2>&1
    $output | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
    if ($LASTEXITCODE -ne 0) {
        throw "Sign CLI failed (exit $LASTEXITCODE) for: $PackagePath"
    }
}

<#
.SYNOPSIS
Signs NuGet and Symbol packages using a YubiKey-held certificate via the .NET Sign CLI.

.DESCRIPTION
Replacement for Invoke-NuGetPackageSigning. Signs assemblies inside each
.nupkg/.snupkg and re-signs the container in place using the .NET Sign CLI
(dotnet/sign), avoiding the lossy extract/strip/`nuget pack` round-trip.

Flow:
1. Validate assets and tools.
2. Resolve the signing certificate by SHA-256 fingerprint.
3. Extract the GitHub build-artifact zips.
4. Verify GitHub attestation on each unsigned package.
5. Run `sign code certificate-store` on each package into signed\packages.

How to use:
1. Create a release folder, e.g. ../releases/1.12
2. Download the build assets from the SDK build action into it.
3. In PowerShell:  . .\Yubico.NET.SDK\build\sign-v2.ps1
4. Call Invoke-NuGetPackageSigningV2 (see examples).

.PARAMETER Fingerprint
SHA-256 (or SHA-384/512) fingerprint of the signing certificate. NOT the SHA-1
thumbprint. Can also be provided via the YUBICO_SIGNING_SHA256_FINGERPRINT
environment variable.

.PARAMETER WorkingDirectory
Directory containing the build-artifact zips and where signing takes place.

.PARAMETER SignCliPath
Optional. Path to the Sign CLI executable. Defaults to "sign". Set this if the
repo's `sign` shim shadows the tool (e.g. the tool-path executable).

.PARAMETER TimestampServer
Optional. RFC3161 timestamp URL. Defaults to "http://timestamp.acs.microsoft.com".

.PARAMETER FileList
Optional. Path to a Sign CLI --file-list scoping which assemblies inside the
container get Authenticode-signed. Omitted by default, so every signable
assembly in the container is signed (matches the previous script). Supply this
only if a package ever bundles a third-party or pre-signed assembly that must be
skipped.

.PARAMETER NuGetPackagesZip
Optional. Name of the NuGet packages zip. Required for core packages.

.PARAMETER SymbolsPackagesZip
Optional. Name of the symbols packages zip. Required for core packages.

.PARAMETER NativeShimsZip
Optional. Name of the native shims package zip.

.PARAMETER NonInteractive
Optional switch. Omit --interactive (no PIN dialog). By default the PIN dialog
is shown so the operator can enter the YubiKey PIN once.

.PARAMETER CleanWorkingDirectory
Optional switch. Cleans unsigned/ and signed/ before processing.

.EXAMPLE
Invoke-NuGetPackageSigningV2 -Fingerprint "A01E...DE6" -WorkingDirectory "C:\Signing" -NuGetPackagesZip "Nuget Packages.zip" -SymbolsPackagesZip "Symbols Packages.zip"

.EXAMPLE
Invoke-NuGetPackageSigningV2 -WorkingDirectory "C:\Signing" -NativeShimsZip "Yubico.NativeShims.nupkg.zip"
# Fingerprint taken from YUBICO_SIGNING_SHA256_FINGERPRINT

.NOTES
Requires: .NET 8+ SDK, Sign CLI (dotnet/sign), GitHub CLI, the certificate in
Cert:\CurrentUser\My with the private key on the YubiKey. Windows-only:
Authenticode signing of .dll/.exe in the Sign CLI is Windows-only.
#>
function Invoke-NuGetPackageSigningV2 {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]$Fingerprint,

        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [string]$SignCliPath = "sign",

        [Parameter(Mandatory = $false)]
        [string]$TimestampServer = "http://timestamp.acs.microsoft.com",

        [Parameter(Mandatory = $false)]
        [string]$FileList,

        [Parameter(Mandatory = $false)]
        [string]$NuGetPackagesZip,

        [Parameter(Mandatory = $false)]
        [string]$SymbolsPackagesZip,

        [Parameter(Mandatory = $false)]
        [string]$NativeShimsZip,

        [Parameter(Mandatory = $false)]
        [switch]$NonInteractive,

        [Parameter(Mandatory = $false)]
        [switch]$CleanWorkingDirectory
    )

    $RepoName = "Yubico/Yubico.NET.SDK"

    try {
        Write-Host "`nInitializing NuGet package signing (Sign CLI)..." -ForegroundColor Cyan

        # Resolve fingerprint from environment variable if not provided.
        if ([string]::IsNullOrWhiteSpace($Fingerprint)) {
            $Fingerprint = $env:YUBICO_SIGNING_SHA256_FINGERPRINT
        }
        if ([string]::IsNullOrWhiteSpace($Fingerprint)) {
            throw "Fingerprint is required. Provide via -Fingerprint or YUBICO_SIGNING_SHA256_FINGERPRINT (SHA-256, SHA-384, or SHA-512 certificate fingerprint, not the SHA-1 thumbprint)."
        }


        # Validate tools.
        Write-Host "`nVerifying required tools..."
        $signCmd = Get-Command $SignCliPath -ErrorAction SilentlyContinue
        if (-not $signCmd) {
            throw "Sign CLI not found at: $SignCliPath. Install: dotnet tool install --global sign --prerelease"
        }
        $signCliResolved = if ($signCmd.Source) { $signCmd.Source } else { $SignCliPath }
        Write-Host "OK Sign CLI found at: $signCliResolved"

        if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
            throw "GitHub CLI not installed or not found in PATH"
        }
        Write-Host "OK GitHub CLI found"

        # Resolve and validate the certificate.
        $resolved = Resolve-SigningCertificate -Fingerprint $Fingerprint
        $cert = $resolved.Certificate
        $Fingerprint = $resolved.Fingerprint

        Write-Host "`nCertificate Details:" -ForegroundColor Cyan
        Write-Host "  Subject:      $($cert.Subject)"
        Write-Host "  Issuer:       $($cert.Issuer)"
        $fingerprintAlgo = switch ($Fingerprint.Length) { 64 { 'SHA-256' } 96 { 'SHA-384' } 128 { 'SHA-512' } }
        Write-Host "  Thumbprint:   $($cert.Thumbprint) (SHA-1)"
        Write-Host "  Fingerprint:  $Fingerprint ($fingerprintAlgo)"
        Write-Host "  Valid From:   $($cert.NotBefore)"
        Write-Host "  Valid To:     $($cert.NotAfter)"

        if ($cert.NotAfter -le (Get-Date).AddMonths(1)) {
            Write-Warning "Certificate will expire within one month on $($cert.NotAfter)"
        }

        if ($CleanWorkingDirectory) {
            Clean-Directory -BaseDirectory $WorkingDirectory
        }

        $directories = Initialize-DirectoryStructure -BaseDirectory $WorkingDirectory

        Test-RequiredAssets -WorkingDirectory $WorkingDirectory -NuGetPackagesZip $NuGetPackagesZip -SymbolsPackagesZip $SymbolsPackagesZip -NativeShimsZip $NativeShimsZip

        $hasCorePackages = -not [string]::IsNullOrWhiteSpace($NuGetPackagesZip) -and -not [string]::IsNullOrWhiteSpace($SymbolsPackagesZip)
        $hasNativeShims = -not [string]::IsNullOrWhiteSpace($NativeShimsZip)

        # Extract + attest all packages (nupkg and snupkg) into the unsigned dir.
        if ($hasCorePackages) {
            Write-Host "`nProcessing Core Packages..." -ForegroundColor Yellow
            Expand-PackagesFromZip -ZipFile $NuGetPackagesZip -Directories $directories -RepoName $RepoName
            Expand-PackagesFromZip -ZipFile $SymbolsPackagesZip -Directories $directories -RepoName $RepoName
        }
        if ($hasNativeShims) {
            Write-Host "`nProcessing Native Shims Package..." -ForegroundColor Yellow
            Expand-PackagesFromZip -ZipFile $NativeShimsZip -Directories $directories -RepoName $RepoName
        }

        # Sign every staged package in place (assemblies + container) into signed\packages.
        Write-Host "`nSigning packages with the Sign CLI..." -ForegroundColor Cyan
        $staged = Get-ChildItem -Path $directories.Unsigned -Include *.nupkg, *.snupkg -File
        if (-not $staged -or $staged.Count -eq 0) {
            throw "No .nupkg/.snupkg found to sign in $($directories.Unsigned)"
        }
        foreach ($package in $staged) {
            Invoke-SignCliOnPackage -PackagePath $package.FullName `
                -OutputDirectory $directories.Packages `
                -SignCliPath $SignCliPath `
                -Fingerprint $Fingerprint `
                -TimestampServer $TimestampServer `
                -FileList $FileList `
                -Interactive:(-not $NonInteractive)
        }

        # Summary of signed packages.
        Write-Host "`nSigned Packages Summary:" -ForegroundColor Yellow
        Write-Host "  NuGet Packages:" -ForegroundColor White
        Get-ChildItem -Path $directories.Packages -Filter "*.nupkg" | ForEach-Object {
            $size = "{0:N2}" -f ($_.Length / 1KB)
            Write-Host "    $($_.Name) [$size KB]" -ForegroundColor Gray
        }
        Write-Host "  Symbol Packages:" -ForegroundColor White
        Get-ChildItem -Path $directories.Packages -Filter "*.snupkg" | ForEach-Object {
            $size = "{0:N2}" -f ($_.Length / 1KB)
            Write-Host "    $($_.Name) [$size KB]" -ForegroundColor Gray
        }

        Write-Host "`nPackage signing completed." -ForegroundColor Green
        Write-Host "Signed packages: $($directories.Packages)" -ForegroundColor Yellow

        $examplePackage = Get-ChildItem -Path $directories.Packages -Filter "*.nupkg" | Select-Object -First 1
        Write-Host "`nTo push (manual step - never run automatically):" -ForegroundColor Cyan
        if ($examplePackage) {
            Write-Host "  Invoke-NuGetPackagePush -PackagePath `"$($examplePackage.FullName)`"" -ForegroundColor Gray
        }
        Write-Host "  Invoke-NuGetPackagePush -PackagePath `"$($directories.Packages)`" -SkipDuplicate" -ForegroundColor Gray
        Write-Host ""

        return
    }
    catch {
        Write-Host "`nError occurred:" -ForegroundColor Red
        Write-Error $_.Exception.Message
        throw
    }
}

<#
.SYNOPSIS
Pushes NuGet packages to a NuGet feed using the NuGet CLI.

.DESCRIPTION
Pushes .nupkg/.snupkg to nuget.org or another feed. Kept verbatim from sign.ps1.
This is always a manual, human-initiated step - it is never called automatically
by the signing flow.

.PARAMETER PackagePath
Path to a single package or a directory containing packages.

.PARAMETER ApiKey
API key. Can also be provided via NUGET_API_KEY.

.PARAMETER Source
Optional. Feed URL. Defaults to nuget.org.

.PARAMETER Timeout
Optional. Push timeout in seconds. Defaults to 300.

.PARAMETER SkipDuplicate
Optional switch. Skip packages that already exist instead of failing.

.PARAMETER NuGetPath
Optional. Path to nuget.exe. Defaults to "nuget.exe".
#>
function Invoke-NuGetPackagePush {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackagePath,

        [Parameter(Mandatory = $false)]
        [string]$ApiKey,

        [Parameter(Mandatory = $false)]
        [string]$Source = "https://api.nuget.org/v3/index.json",

        [Parameter(Mandatory = $false)]
        [int]$Timeout = 300,

        [Parameter(Mandatory = $false)]
        [switch]$SkipDuplicate,

        [Parameter(Mandatory = $false)]
        [string]$NuGetPath = "nuget.exe"
    )

    try {
        Write-Host "`nInitializing NuGet package push process..." -ForegroundColor Cyan

        if ([string]::IsNullOrWhiteSpace($ApiKey)) {
            $ApiKey = $env:NUGET_API_KEY
        }

        if ([string]::IsNullOrWhiteSpace($ApiKey)) {
            throw "ApiKey is required. Provide via -ApiKey parameter or NUGET_API_KEY environment variable."
        }

        if (-not (Get-Command $NuGetPath -ErrorAction SilentlyContinue)) {
            throw "NuGet CLI not found at path: $NuGetPath"
        }
        Write-Host "OK NuGet CLI found at: $NuGetPath"

        if (-not (Test-Path $PackagePath)) {
            throw "Package path not found: $PackagePath"
        }

        $isDirectory = (Get-Item $PackagePath).PSIsContainer

        if ($isDirectory) {
            Write-Host "`nSearching for package files in: $PackagePath" -ForegroundColor Yellow
            $packages = Get-ChildItem -Path "$PackagePath\*" -Include "*.nupkg", "*.snupkg" -File

            if ($packages.Count -eq 0) {
                throw "No .nupkg or .snupkg files found in directory: $PackagePath"
            }

            $nupkgCount = ($packages | Where-Object { $_.Extension -eq ".nupkg" }).Count
            $snupkgCount = ($packages | Where-Object { $_.Extension -eq ".snupkg" }).Count
            Write-Host "OK Found $($packages.Count) package(s) to push ($nupkgCount .nupkg, $snupkgCount .snupkg)"
        }
        else {
            if (-not ($PackagePath.EndsWith(".nupkg") -or $PackagePath.EndsWith(".snupkg"))) {
                throw "Package file must have .nupkg or .snupkg extension: $PackagePath"
            }
            $packages = @(Get-Item $PackagePath)
        }

        Write-Host "`nPush Configuration:" -ForegroundColor Cyan
        Write-Host "  Target Source: $Source"
        Write-Host "  Timeout:       $Timeout seconds"
        Write-Host "  Skip Existing: $($SkipDuplicate.IsPresent)"

        $successCount = 0
        $failCount = 0

        foreach ($package in $packages) {
            Write-Host "`nPushing: $($package.Name)" -ForegroundColor White

            $pushArgs = @(
                "push",
                $package.FullName,
                $ApiKey,
                "-Source", $Source,
                "-Timeout", $Timeout,
                "-NonInteractive"
            )

            if ($SkipDuplicate) {
                $pushArgs += "-SkipDuplicate"
            }

            $output = & $NuGetPath $pushArgs 2>&1

            if ($LASTEXITCODE -eq 0) {
                Write-Host "  OK Successfully pushed" -ForegroundColor Green
                $successCount++
            }
            else {
                Write-Host "  FAILED to push" -ForegroundColor Red
                $output | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
                $failCount++
            }
        }

        Write-Host "`nPush Summary:" -ForegroundColor Yellow
        Write-Host "  Total Packages:      $($packages.Count)"
        Write-Host "  Successfully Pushed: $successCount" -ForegroundColor Green
        if ($failCount -gt 0) {
            Write-Host "  Failed:              $failCount" -ForegroundColor Red
            throw "$failCount package(s) failed to push"
        }

        Write-Host "`nPackage push process completed successfully." -ForegroundColor Green
    }
    catch {
        Write-Host "`nError occurred:" -ForegroundColor Red
        Write-Error $_.Exception.Message
        throw
    }
}
