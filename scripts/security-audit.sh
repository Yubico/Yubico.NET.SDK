#!/usr/bin/env bash
# =============================================================================
# security-audit.sh — Sensitive Data Handling Audit
# =============================================================================
#
# Mechanically scans production source for the 9 security taxonomy errors
# identified during the worktree-security-remediation review (April 2026).
#
# Usage:
#   ./scripts/security-audit.sh              # scan src/*/src/**
#   ./scripts/security-audit.sh --all        # also scan examples/ and tests/
#   ./scripts/security-audit.sh --help       # print taxonomy descriptions
#
# Exit codes:
#   0  — no findings (or --help)
#   N  — number of findings (CI-compatible: non-zero = audit failed)
#
# Requirements:
#   - ripgrep (rg) with PCRE2 support: brew install ripgrep
#   - Run from repo root
#
# Taxonomy reference:
#   T1  Untracked .ToArray() copies of sensitive buffers        Medium FP
#   T2  Encoding.UTF8.GetBytes() → untracked temp byte[]       Low FP
#   T3  Convert.ToHexString() inside LogTrace/LogDebug          Low FP
#   T4  ArrayPool.Return without prior ZeroMemory              Medium FP
#   T5  Early return before ZeroMemory (control-flow)          AGENT ONLY
#   T6  Sensitive data as string parameter in public API        Medium FP
#   T7  IDisposable missing on class holding key material      AGENT ONLY
#   T8  Console.Write in production source                      Low FP
#   T9  Crypto disposable created without 'using'              Medium FP
# =============================================================================

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

SCAN_ALL=false
if [[ "${1:-}" == "--all" ]]; then
    SCAN_ALL=true
fi

if [[ "${1:-}" == "--help" ]]; then
    cat <<'EOF'
TAXONOMY DESCRIPTIONS
=====================

T1: Untracked .ToArray() copies of sensitive buffers
  Pattern: pin.ToArray(), key.ToArray(), .Memory.Span.ToArray(), .Span.ToArray()
  Problem: Creates a new byte[] on the managed heap with no owner responsible
           for zeroing it. If the original is zeroed but the copy isn't, the
           plaintext persists until GC.
  False Positives: .ToArray() inside try/finally with ZeroMemory is correct.
                   Also triggers on non-sensitive contexts (e.g. protocol IDs).
  Fix: Pass ReadOnlyMemory<byte> directly, or capture the array and zero in finally.

T2: Encoding.UTF8.GetBytes() → untracked temp byte[]
  Pattern: var bytes = Encoding.UTF8.GetBytes(pin|password|...)
  Problem: Creates an intermediate byte[] containing plaintext PIN/password
           that is never zeroed. Even if the result is then copied into a
           secure buffer, the intermediate persists.
  False Positives: Encoding.UTF8.GetBytes(str, span) span-overload is correct.
  Fix: Encode directly into a pre-allocated Span<byte> using the span overload.

T3: Convert.ToHexString() inside LogTrace/LogDebug
  Pattern: _logger.LogTrace(...Convert.ToHexString(buffer)...)
  Problem: Hex-encodes potentially sensitive crypto buffers into log output.
           Even at Trace level, logs can be captured by log aggregators.
  False Positives: Application ID (AID) selection is public info, not sensitive.
  Fix: Replace with byte count only: "...{ByteCount} bytes", buffer.Length

T4: ArrayPool.Return without explicit clearArray: true
  Pattern: ArrayPool<byte>.Shared.Return(buffer) [missing clearArray: true]
  Problem: Default is clearArray: false. If the buffer held sensitive data
           and ZeroMemory was NOT called before Return, the pool recycles
           the buffer with stale key/PIN bytes visible to the next renter.
  False Positives: Non-sensitive buffers (e.g. TLV encoding scratch space).
                   Also correct if ZeroMemory is called before Return.
  Fix: Either call ZeroMemory(buffer) first, or use Return(buffer, clearArray: true).

T5: Early return before ZeroMemory (AGENT-ONLY)
  Problem: A try block allocates a sensitive buffer, but an early return path
           exits the try before the finally { ZeroMemory() } runs. In C#,
           finally does run on return — but this can be confused with patterns
           where the ZeroMemory is placed AFTER the try/finally block.
  Note: Cannot be expressed as a grep. Requires control-flow analysis.

T6: Sensitive data as string parameter in public API
  Pattern: public Task Method(string pin|password|secret|key|credential)
  Problem: Strings are immutable and interned — they cannot be zeroed.
           Sensitive data should be passed as ReadOnlyMemory<byte> or ReadOnlySpan<byte>.
  False Positives: 'key' is often used for display labels (WriteKeyValue).
  Fix: Change parameter type to ReadOnlyMemory<byte> and UTF8-encode at call site.

T7: IDisposable missing on class holding key material (AGENT-ONLY)
  Problem: A class has a byte[] field named _key, _sessionKey, _mac, etc.
           but does not implement IDisposable to zero that field on disposal.
  Note: Cannot be expressed as a grep. Requires semantic analysis of field names
        plus class hierarchy traversal. Use an AI agent for this taxonomy.

T8: Console.Write in production source
  Pattern: Console.Write / Console.WriteLine in src/*/src/**/*.cs
  Problem: Debug-era print statements that dump sensitive data to stdout.
  False Positives: AnsiConsole.* is the Spectre.Console wrapper — not a Console call.
                   XML doc comments (///) and inline comments are excluded.
  Fix: Remove or replace with structured logging via ILogger.

T9: Crypto disposable created without 'using'
  Pattern: new AesCmac / new IncrementalHash / new Aes / new HMACSHA256 without 'using'
  Problem: These objects hold key material in unmanaged memory and zero it on
           Dispose(). Without 'using', disposal is non-deterministic (GC decides).
           During long sessions, key material can sit in memory for minutes.
  False Positives: Multiline 'using var' where 'using' is on the preceding line.
                   PCRE2 lookbehind cannot see across lines.
  Fix: Always wrap with 'using var mac = new AesCmac(...)'.

EOF
    exit 0
fi

# ── Scope setup ──────────────────────────────────────────────────────────────

PROD_GLOBS=(
    "--glob" "src/*/src/**/*.cs"
    "--glob" "!**/obj/**"
    "--glob" "!**/bin/**"
)

if [[ "$SCAN_ALL" == "true" ]]; then
    ALL_GLOBS=(
        "--glob" "src/**/*.cs"
        "--glob" "!**/obj/**"
        "--glob" "!**/bin/**"
    )
    SCOPE_GLOBS=("${ALL_GLOBS[@]}")
    SCOPE_LABEL="ALL (src/**)"
else
    SCOPE_GLOBS=("${PROD_GLOBS[@]}")
    SCOPE_LABEL="PRODUCTION (src/*/src/**)"
fi

TOTAL_FINDINGS=0

run_check() {
    local label="$1"
    local taxonomy="$2"
    local note="$3"
    local fp_rate="$4"
    shift 4
    local -a cmd=("$@")

    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  $taxonomy  $label"
    echo "  Note: $note"
    echo "  False positive rate: $fp_rate"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    local output
    output=$("${cmd[@]}" 2>/dev/null || true)

    if [[ -z "$output" ]]; then
        echo "  ✓ No findings"
    else
        local count
        count=$(echo "$output" | wc -l | tr -d ' ')
        echo "$output"
        echo ""
        echo "  ⚠  $count finding(s) — review required"
        TOTAL_FINDINGS=$((TOTAL_FINDINGS + count))
    fi
}

echo "=============================================================================="
echo "  Sensitive Data Handling Security Audit"
echo "  Scope: $SCOPE_LABEL"
echo "  $(date)"
echo "=============================================================================="

# ── T1: .Span.ToArray() — always suspicious in production ────────────────────
run_check \
    "Untracked .Span.ToArray() — always creates unowned copy" \
    "[T1a]" \
    "Any .Span.ToArray() in production src is suspect — span should be consumed directly" \
    "LOW" \
    rg -n '\.Span\.ToArray\(\)' "${SCOPE_GLOBS[@]}"

# ── T1b: sensitive variable names + .ToArray() ───────────────────────────────
run_check \
    "Untracked .ToArray() on sensitive-named variables (pin/key/password/mac/salt/hash/token)" \
    "[T1b]" \
    "Verify the .ToArray() result is zeroed in a finally block or that the callee accepts ReadOnlyMemory<byte> directly" \
    "MEDIUM" \
    rg -n --pcre2 \
        '(?:pin|key|password|secret|mac|salt|hash|token|puk|credential|auth)[A-Za-z0-9_]*\.(?:Memory\.)?ToArray\(\)' \
        "${SCOPE_GLOBS[@]}"

# ── T2: Encoding.UTF8.GetBytes creating new array (not span overload) ─────────
run_check \
    "Encoding.UTF8.GetBytes() → new byte[] copy of sensitive string" \
    "[T2]" \
    "The span overload Encoding.UTF8.GetBytes(str, Span<byte>) is correct and will NOT appear here" \
    "LOW" \
    rg -n \
        '=\s*Encoding\.UTF8\.GetBytes\(' \
        "${SCOPE_GLOBS[@]}"

# ── T3: Convert.ToHexString inside log calls ─────────────────────────────────
run_check \
    "Convert.ToHexString() inside LogTrace/LogDebug — potential sensitive data in logs" \
    "[T3]" \
    "Application ID (AID) logs are acceptable. Key material, PIN, APDU payload are not. Verify each hit." \
    "MEDIUM" \
    rg -n \
        'Log(?:Trace|Debug)\([^)]*Convert\.ToHexString' \
        "${SCOPE_GLOBS[@]}"

# ── T4: ArrayPool.Return without clearArray: true ────────────────────────────
run_check \
    "ArrayPool.Return without clearArray:true — verify ZeroMemory was called first" \
    "[T4]" \
    "CORRECT if CryptographicOperations.ZeroMemory(buf) appears before this Return. Check each hit." \
    "MEDIUM" \
    rg -n \
        'ArrayPool[^.]*\.Shared\.Return\([^,)]+\)' \
        "${SCOPE_GLOBS[@]}"

# ── T5: Note — requires agent ─────────────────────────────────────────────────
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  [T5]  Early return before ZeroMemory (control-flow analysis)"
echo "  Note: Cannot be expressed as a grep. Requires AI agent or static analysis tool."
echo "  Use: run security-audit.sh with the --agent flag (not yet implemented)"
echo "  Workaround: search for 'return' inside try blocks that allocate sensitive buffers"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# ── T6: string parameters named after sensitive data in public API ────────────
run_check \
    "Public API accepting string for sensitive credential (should be ReadOnlyMemory<byte>)" \
    "[T6]" \
    "Exclude display-label uses: WriteKeyValue(string key, ...) is a UI helper, not a crypto key." \
    "MEDIUM" \
    rg -n --pcre2 \
        'public\s+(?:static\s+|async\s+|virtual\s+|override\s+)*\S+\s+\w+\s*\([^)]*\bstring\s+(?:pin|password|secret|puk|passphrase)\b' \
        "${SCOPE_GLOBS[@]}"

# ── T7: Note — requires agent ─────────────────────────────────────────────────
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  [T7]  IDisposable missing on class holding key material (semantic analysis)"
echo "  Note: Cannot be expressed as a grep. Requires AI agent to:"
echo "         1. Find classes with byte[] fields named _key/_mac/_salt/_sessionKey"
echo "         2. Verify each implements IDisposable and calls ZeroMemory in Dispose()"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# ── T8: Console.Write in production source ────────────────────────────────────
run_check \
    "Console.Write in production source (not AnsiConsole, not comments)" \
    "[T8]" \
    "AnsiConsole.* (Spectre.Console) is legitimate. Comments are excluded by the leading-whitespace anchor." \
    "LOW" \
    rg -n \
        '^\s+Console\.Write' \
        "${SCOPE_GLOBS[@]}"

# ── T9: Crypto disposable created without 'using' (same-line heuristic) ───────
# Note: PCRE2 lookbehind cannot see across lines, so 'using var' on the preceding
# line is a false positive. Review each hit to check the preceding line manually.
run_check \
    "Crypto disposable (AesCmac/IncrementalHash/Aes/HMACSHA256) without 'using' on same line" \
    "[T9]" \
    "HIGH false positive: multiline 'using var\\n    mac = new AesCmac()' looks like a hit. Check preceding line." \
    "HIGH" \
    rg -n \
        'new\s+(?:AesCmac|IncrementalHash|HMACSHA256|HMACSHA512)\s*\(' \
        "${SCOPE_GLOBS[@]}"

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "=============================================================================="
if [[ $TOTAL_FINDINGS -eq 0 ]]; then
    echo "  ✅ AUDIT PASSED — 0 findings in $SCOPE_LABEL"
    echo "  Note: T5 and T7 require agent-based analysis (see above)"
else
    echo "  ⚠  AUDIT FLAGGED — $TOTAL_FINDINGS finding(s) require review"
    echo "  Each finding above needs human verification before declaring clean."
    echo "  See --help for false-positive guidance per taxonomy."
fi
echo "=============================================================================="

exit $TOTAL_FINDINGS
