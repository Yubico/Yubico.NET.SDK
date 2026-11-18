# Copyright 2026 Yubico AB
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

<#
.SYNOPSIS
Verifies packages signed by Scribe before they are published. Windows only.

.DESCRIPTION
For every .nupkg and .snupkg under -SignedPath:
  * the NuGet author signature is valid, timestamped and made by the expected certificate;
  * apart from the signature, the package is identical to the unsigned package of the same name under -UnsignedPath;
  * every Windows binary in a .nupkg has a valid, timestamped Authenticode signature by the expected certificate.
#>
param(
    [Parameter(Mandatory)] [string] $SignedPath,
    [Parameter(Mandatory)] [string] $UnsignedPath,
    [Parameter(Mandatory)] [string] $CertificateFingerprint,
    [Parameter(Mandatory)] [int] $ExpectedCount
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Security.Cryptography.Pkcs

$timestampTokenOid = '1.2.840.113549.1.9.16.2.14'

$fingerprint = $CertificateFingerprint.Replace(':', '').ToUpperInvariant()
if ($fingerprint -notmatch '^[0-9A-F]{64}$') {
    throw 'CertificateFingerprint must be a SHA-256 fingerprint (64 hexadecimal characters).'
}

function Get-EntryHashes([string] $package) {
    $hashes = @{}
    $archive = [IO.Compression.ZipFile]::OpenRead($package)
    try {
        foreach ($entry in $archive.Entries) {
            if ($entry.FullName -eq '.signature.p7s') { continue }
            $stream = $entry.Open()
            try {
                $hashes[$entry.FullName] = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream))
            } finally {
                $stream.Dispose()
            }
        }
    } finally {
        $archive.Dispose()
    }
    return $hashes
}

# dotnet nuget verify accepts an untimestamped signature with a warning, so check the primary signature itself.
function Assert-TimestampedSignature([string] $package) {
    $archive = [IO.Compression.ZipFile]::OpenRead($package)
    try {
        $entry = $archive.GetEntry('.signature.p7s')
        if ($null -eq $entry) { throw "Package is not signed: $package" }
        $buffer = [IO.MemoryStream]::new()
        $stream = $entry.Open()
        try { $stream.CopyTo($buffer) } finally { $stream.Dispose() }
    } finally {
        $archive.Dispose()
    }
    $cms = [Security.Cryptography.Pkcs.SignedCms]::new()
    $cms.Decode($buffer.ToArray())
    $signer = $cms.SignerInfos[0]
    if ($signer.Certificate.GetCertHashString([Security.Cryptography.HashAlgorithmName]::SHA256) -ne $fingerprint) {
        throw "Unexpected NuGet signer for ${package}: $($signer.Certificate.Subject)"
    }
    if (-not ($signer.UnsignedAttributes | Where-Object { $_.Oid.Value -eq $timestampTokenOid })) {
        throw "NuGet signature is not timestamped: $package"
    }
}

$signed = @(Get-ChildItem -LiteralPath $SignedPath -Recurse -File | Where-Object { $_.Extension -in '.nupkg', '.snupkg' })
$unsigned = @{}
Get-ChildItem -LiteralPath $UnsignedPath -Recurse -File | Where-Object { $_.Extension -in '.nupkg', '.snupkg' } |
    ForEach-Object { $unsigned[$_.Name] = $_.FullName }

if ($signed.Count -ne $ExpectedCount) {
    throw "Expected $ExpectedCount signed packages, found $($signed.Count)."
}

foreach ($package in $signed) {
    Write-Host "=== $($package.Name)"

    & dotnet nuget verify $package.FullName --all --certificate-fingerprint $fingerprint --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw "NuGet signature verification failed: $($package.Name)" }
    Assert-TimestampedSignature $package.FullName

    if (-not $unsigned.ContainsKey($package.Name)) { throw "No unsigned package to compare with: $($package.Name)" }
    $before = Get-EntryHashes $unsigned[$package.Name]
    $after = Get-EntryHashes $package.FullName
    $changed = @(Compare-Object @($before.GetEnumerator() | ForEach-Object { "$($_.Key) $($_.Value)" }) `
                               @($after.GetEnumerator() | ForEach-Object { "$($_.Key) $($_.Value)" }))
    if ($changed.Count -ne 0) { throw "Package contents changed during signing: $($package.Name)`n$($changed | Out-String)" }

    if ($package.Extension -ne '.nupkg') { continue }

    $contents = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
    [IO.Compression.ZipFile]::ExtractToDirectory($package.FullName, $contents)
    $binaries = @(Get-ChildItem -LiteralPath $contents -Recurse -File -Include '*.dll', '*.exe')
    if ($binaries.Count -eq 0) { throw "No Windows binaries found in $($package.Name)" }
    foreach ($binary in $binaries) {
        $relative = [IO.Path]::GetRelativePath($contents, $binary.FullName)
        $signature = Get-AuthenticodeSignature -LiteralPath $binary.FullName
        if ($signature.Status -ne 'Valid') { throw "Authenticode signature is $($signature.Status): $relative" }
        if ($signature.SignerCertificate.GetCertHashString([Security.Cryptography.HashAlgorithmName]::SHA256) -ne $fingerprint) {
            throw "Unexpected Authenticode signer for $relative`: $($signature.SignerCertificate.Subject)"
        }
        if ($null -eq $signature.TimeStamperCertificate) { throw "Authenticode signature is not timestamped: $relative" }
        Write-Host "Authenticode OK: $relative"
    }
    Remove-Item -LiteralPath $contents -Recurse -Force
}

Write-Host "Verified $($signed.Count) signed packages."
