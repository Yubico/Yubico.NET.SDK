# Validate that a built Yubico.NativeShims.dll exports exactly the canonical
# set of symbols defined in expected_symbols.txt.
#
# Usage:  pwsh check_exports.ps1 <path-to-Yubico.NativeShims.dll>
#
# Requires: dumpbin.exe on PATH (provided by VC++ Build Tools / vcvars).
# Catches: symbols dropped from exports.msvc, drift between the .def file and
# the actual implementation. Works on cross-compiled binaries (arm64 DLLs
# inspected from x64 host) because dumpbin reads file metadata.
#
# Exits non-zero on any mismatch (missing or extra symbol).

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$LibraryPath
)

$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$expectedFile = Join-Path $scriptDir 'expected_symbols.txt'

if (-not (Test-Path $LibraryPath)) {
    Write-Error "shared library not found: $LibraryPath"
    exit 2
}
if (-not (Test-Path $expectedFile)) {
    Write-Error "expected_symbols.txt not found at $expectedFile"
    exit 2
}

# Load expected symbols (strip comments + blanks)
$expected = Get-Content $expectedFile |
    Where-Object { $_ -notmatch '^\s*#' -and $_.Trim() -ne '' } |
    ForEach-Object { $_.Trim() } |
    Sort-Object -Unique

# Extract exported names from the DLL via dumpbin /exports.
# Output format includes a header and a "name" column at the end of each
# export line. We grep for lines containing a Native_* token.
$dumpbinOutput = & dumpbin /exports $LibraryPath 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "dumpbin failed (exit $LASTEXITCODE). Make sure VC++ Build Tools are on PATH (run vcvars*.bat first)."
    exit 2
}

$actual = $dumpbinOutput |
    Select-String -Pattern '\bNative_\w+' -AllMatches |
    ForEach-Object { $_.Matches.Value } |
    Sort-Object -Unique

$missing = $expected | Where-Object { $actual -notcontains $_ }
$extra   = $actual   | Where-Object { $expected -notcontains $_ }

Write-Host "Library:  $LibraryPath"
Write-Host "Expected: $($expected.Count) symbols"
Write-Host "Actual:   $($actual.Count) Native_* symbols"

$status = 0
if ($missing) {
    Write-Host ""
    Write-Host "FAIL: symbols listed in expected_symbols.txt but NOT exported by the binary:"
    $missing | ForEach-Object { Write-Host "  - $_" }
    $status = 1
}
if ($extra) {
    Write-Host ""
    Write-Host "FAIL: Native_* symbols exported by the binary but NOT in expected_symbols.txt:"
    $extra | ForEach-Object { Write-Host "  - $_" }
    $status = 1
}

if ($status -eq 0) {
    Write-Host "PASS: export table matches expected symbol list"
}
exit $status
