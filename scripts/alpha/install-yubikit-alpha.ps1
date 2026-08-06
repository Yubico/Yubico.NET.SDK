# Yubico .NET SDK v2 - ALPHA feed bootstrap (Windows / PowerShell)
# Adds the anonymous public alpha NuGet feed and (best-effort) verifies build provenance.
$ErrorActionPreference = 'Stop'

$FeedUrl = 'https://yubico.github.io/Yubico.NET.SDK/alpha/index.json'
$SrcName = 'yubikit-alpha'
$Version = '2.0.0-alpha.1'

Write-Host '============================================================'
Write-Host ' Yubico .NET SDK v2 - ALPHA'
Write-Host ' Pre-release, subject to change, and NOT yet security-audited'
Write-Host ' by Yubico. No security guarantees. Package names/namespaces'
Write-Host ' may change. Evaluation / hackathon use only.'
Write-Host '============================================================'

# Confirmation. If not interactive (e.g. piped `iwr | iex`), refuse rather than
# consuming the piped script as input. Download and run instead.
if ([System.Console]::IsInputRedirected) {
    Write-Error 'Run this script from a terminal (download it first), not via a pipe (iwr | iex).'
    exit 1
}
$reply = Read-Host 'Add the alpha feed and continue? [y/N]'
if ($reply -ne 'y' -and $reply -ne 'Y') { Write-Host 'Aborted.'; exit 1 }

# Add the feed as an ADDITIONAL source (keep nuget.org for transitive deps like Yubico.NativeShims).
$existing = & dotnet nuget list source 2>$null
if ($existing -match [regex]::Escape($FeedUrl)) {
    Write-Host 'Feed already registered.'
} else {
    & dotnet nuget add source $FeedUrl -n $SrcName
}

# Best-effort provenance verification (requires GitHub CLI; skipped if absent).
if (Get-Command gh -ErrorAction SilentlyContinue) {
    Write-Host 'Verifying build provenance (best-effort)...'
    $tmp = New-Item -ItemType Directory -Path ([System.IO.Path]::GetTempPath()) -Name ([System.Guid]::NewGuid())
    $nupkg = Join-Path $tmp 'core.nupkg'
    $url = "https://yubico.github.io/Yubico.NET.SDK/alpha/flatcontainer/yubico.yubikit.core/$Version/yubico.yubikit.core.$Version.nupkg"
    try {
        Invoke-WebRequest $url -OutFile $nupkg
        & gh attestation verify $nupkg --repo Yubico/Yubico.NET.SDK
        if ($LASTEXITCODE -eq 0) { Write-Host 'Provenance verified.' }
        else { Write-Host 'WARNING: provenance verification failed or unavailable for this alpha. Continuing.' }
    } catch {
        Write-Host 'WARNING: provenance check could not run. Continuing.'
    } finally {
        Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
    }
} else {
    Write-Host 'NOTE: GitHub CLI (gh) not found - skipping provenance check. These are unsigned alpha packages.'
}

Write-Host ''
Write-Host "Done. Install a package with, for example:"
Write-Host "  dotnet add package Yubico.YubiKit.Core --version $Version"
Write-Host ''
Write-Host 'Teardown:'
Write-Host "  dotnet nuget remove source $SrcName"
