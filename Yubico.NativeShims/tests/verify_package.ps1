param(
    [Parameter(Mandatory = $true)]
    [string]$PackageSource,

    [Parameter(Mandatory = $true)]
    [string]$PackageVersion,

    [Parameter(Mandatory = $true)]
    [string]$RuntimeIdentifier,

    [Parameter(Mandatory = $true)]
    [ValidateSet('windows', 'linux', 'macos')]
    [string]$TargetOS,

    [Parameter(Mandatory = $true)]
    [string]$PackagesPath,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactsRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$project = Join-Path $PSScriptRoot 'NativeAotProbe/NativeAotProbe.csproj'
$packageSource = (Resolve-Path $PackageSource).Path
$nupkgs = @(Get-ChildItem -LiteralPath $packageSource -File -Filter '*.nupkg')

if ($nupkgs.Count -ne 1) {
    throw "Expected exactly one current-run package, found $($nupkgs.Count)."
}

$expectedPackageName = "Yubico.NativeShims.$PackageVersion.nupkg"
if ($nupkgs[0].Name -cne $expectedPackageName) {
    throw "Expected package '$expectedPackageName', found '$($nupkgs[0].Name)'."
}

$sharedName = switch ($TargetOS) {
    'windows' { 'Yubico.NativeShims.dll' }
    'linux' { 'libYubico.NativeShims.so' }
    'macos' { 'libYubico.NativeShims.dylib' }
}
$executableName = if ($TargetOS -eq 'windows') { 'NativeAotProbe.exe' } else { 'NativeAotProbe' }

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

function Assert-SharedConsumerOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Mode
    )

    $sharedLibraries = @(Get-ChildItem -LiteralPath $Path -Recurse -File | Where-Object {
        $_.Name.Equals($sharedName, [StringComparison]::OrdinalIgnoreCase)
    })
    if ($sharedLibraries.Count -ne 1) {
        throw "Expected one $Mode NativeShims shared library, found $($sharedLibraries.Count)."
    }

    $archives = @(Get-ChildItem -LiteralPath $Path -Recurse -File | Where-Object {
        $_.Extension -in @('.a', '.lib')
    })
    if ($archives.Count -ne 0) {
        throw "Static archives leaked into $Mode output: $($archives.FullName -join ', ')"
    }

    $executable = Join-Path $Path $executableName
    & $executable
    if ($LASTEXITCODE -ne 0) {
        throw "$Mode probe failed with exit code $LASTEXITCODE."
    }
}

$restoreArguments = @(
    'restore', $project,
    '-r', $RuntimeIdentifier,
    '--packages', $PackagesPath,
    '--no-cache',
    '--source', $packageSource,
    '--source', 'https://api.nuget.org/v3/index.json',
    "-p:NativeShimsVersion=$PackageVersion"
)
Invoke-DotNet 'Package restore' $restoreArguments

$assets = Get-Content (Join-Path (Split-Path $project) 'obj/project.assets.json') -Raw | ConvertFrom-Json
$resolvedPackages = @($assets.libraries.PSObject.Properties.Name | Where-Object {
    $_.StartsWith('Yubico.NativeShims/', [StringComparison]::OrdinalIgnoreCase)
})
$expectedPackage = "Yubico.NativeShims/$PackageVersion"
if ($resolvedPackages.Count -ne 1 -or
    -not $resolvedPackages[0].Equals($expectedPackage, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Expected exact package '$expectedPackage'; resolved '$($resolvedPackages -join ', ')'."
}

$expectedHash = [Convert]::ToBase64String(
    [Security.Cryptography.SHA512]::HashData([IO.File]::ReadAllBytes($nupkgs[0].FullName)))
$installedHashPath = Join-Path $PackagesPath "yubico.nativeshims/$($PackageVersion.ToLowerInvariant())/yubico.nativeshims.$($PackageVersion.ToLowerInvariant()).nupkg.sha512"
$installedHash = (Get-Content -LiteralPath $installedHashPath -Raw).Trim()
if ($installedHash -cne $expectedHash) {
    throw 'Restored NativeShims package does not match the current-run package artifact.'
}

$normalOutputPath = Join-Path $ArtifactsRoot "build/$RuntimeIdentifier"
Remove-Item -LiteralPath $normalOutputPath -Recurse -Force -ErrorAction SilentlyContinue
Invoke-DotNet 'Ordinary build' @(
    'build', $project,
    '-c', 'Release',
    '-r', $RuntimeIdentifier,
    '--no-restore',
    '-o', $normalOutputPath,
    "-p:NativeShimsVersion=$PackageVersion",
    "-p:RestorePackagesPath=$PackagesPath"
)
Assert-SharedConsumerOutput $normalOutputPath 'ordinary build'

$normalPublishPath = Join-Path $ArtifactsRoot "publish-framework-dependent/$RuntimeIdentifier"
Remove-Item -LiteralPath $normalPublishPath -Recurse -Force -ErrorAction SilentlyContinue
Invoke-DotNet 'Non-AOT publish' @(
    'publish', $project,
    '-c', 'Release',
    '-r', $RuntimeIdentifier,
    '--no-restore',
    '--self-contained', 'false',
    '-o', $normalPublishPath,
    "-p:NativeShimsVersion=$PackageVersion",
    "-p:RestorePackagesPath=$PackagesPath",
    '-p:PublishAot=false'
)
Assert-SharedConsumerOutput $normalPublishPath 'non-AOT publish'

$aotRestoreArguments = $restoreArguments + '-p:PublishAot=true'
Invoke-DotNet 'Native AOT restore' $aotRestoreArguments

# PublishAot also enables analyzers during ordinary builds. It must not remove
# the shared library needed by the CoreCLR development path.
$aotBuildOutputPath = Join-Path $ArtifactsRoot "build-aot-configured/$RuntimeIdentifier"
Remove-Item -LiteralPath $aotBuildOutputPath -Recurse -Force -ErrorAction SilentlyContinue
Invoke-DotNet 'AOT-configured build' @(
    'build', $project,
    '-c', 'Release',
    '-r', $RuntimeIdentifier,
    '--no-restore',
    '-o', $aotBuildOutputPath,
    "-p:NativeShimsVersion=$PackageVersion",
    "-p:RestorePackagesPath=$PackagesPath",
    '-p:PublishAot=true'
)
Assert-SharedConsumerOutput $aotBuildOutputPath 'AOT-configured build'

$aotPublishPath = Join-Path $ArtifactsRoot "publish/$RuntimeIdentifier"
Remove-Item -LiteralPath $aotPublishPath -Recurse -Force -ErrorAction SilentlyContinue
Invoke-DotNet 'Native AOT publish' @(
    'publish', $project,
    '-c', 'Release',
    '-r', $RuntimeIdentifier,
    '--no-restore',
    '-o', $aotPublishPath,
    "-p:NativeShimsVersion=$PackageVersion",
    "-p:RestorePackagesPath=$PackagesPath",
    '-p:PublishAot=true',
    '-p:ContinuousIntegrationBuild=true'
)

$aotExecutable = Join-Path $aotPublishPath $executableName
if (-not (Test-Path -LiteralPath $aotExecutable -PathType Leaf)) {
    throw "Native AOT executable is missing: $aotExecutable"
}

$nativeSidecars = @(Get-ChildItem -LiteralPath $aotPublishPath -Recurse -File | Where-Object {
    $_.Name -match '(?i)(Yubico\.NativeShims|libcrypto|libssl)'
})
if ($nativeSidecars.Count -ne 0) {
    throw "NativeShims or OpenSSL sidecars remain in AOT publish output: $($nativeSidecars.FullName -join ', ')"
}

if (Test-Path -LiteralPath (Join-Path $aotPublishPath 'NativeAotProbe.dll')) {
    throw 'Managed NativeAotProbe.dll was published; Native AOT compilation did not run.'
}

& $aotExecutable
if ($LASTEXITCODE -ne 0) {
    throw "Native AOT probe failed with exit code $LASTEXITCODE."
}
