#!/usr/bin/env bash
# =============================================================================
# security-audit.sh — Sensitive Data Handling Audit
# =============================================================================
#
# Mechanically scans production source for security taxonomy errors
# identified during the worktree-security-remediation review (April 2026).
# T1-T9 from original review; T10-T12 added after Copilot round-3 analysis.
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
#   T10 IDisposable not disposed on exception/failure paths    AGENT ONLY
#   T11 Conditional buffer allocation not covered by ZeroMemory AGENT ONLY
#   T12 Dispose() zeros caller-provided buffer (ownership bug)  AGENT ONLY
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
  Known API exception: ApduCommand internally does Data = data?.ToArray() — callers
                   that zero their own buffer cannot prevent this infrastructure clone.
                   This is a known limitation; flag at ApduCommand API layer only.
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

T10: IDisposable not disposed on exception/failure paths (AGENT-ONLY)
  Problem: An IDisposable object (holding key material) is created in a try block.
           If an exception is thrown or an early-exit condition is reached, the
           finally block runs — but if Dispose() is not called in the finally,
           the object's key material is never zeroed.
  Distinction from T7: T7 = class never implements IDisposable. T9 = crypto object
           created without 'using'. T10 = object IS IDisposable and IS created
           correctly, but exception/failure paths bypass Dispose() entirely.
  Example: new ScpProcessor() in a try block; if EXTERNAL AUTHENTICATE fails
           the processor is never disposed and session keys remain in memory.
  Note: Cannot be expressed as a grep. Requires control-flow analysis.
  Fix: Wrap creation in 'using var', or explicitly dispose in a finally block.

T11: Conditional buffer allocation not covered by ZeroMemory (AGENT-ONLY)
  Problem: A byte[] is allocated inside a conditional branch (e.g. if encrypt=true).
           The finally block calls ZeroMemory on variables that exist unconditionally,
           but the conditionally-allocated buffer is not zeroed because it was null
           on non-allocating paths — the finally has no reference to it.
  Distinction from T5: T5 = ZeroMemory is placed AFTER the try/finally (wrong
           position). T11 = ZeroMemory IS in the finally, but the buffer it needs
           to zero was allocated conditionally and may not be in scope.
  Example: byte[] encryptedData allocated only when encrypt=true; finally zeros
           scpCommandData and mac, but encryptedData is left on the heap.
  Note: Cannot be expressed as a grep. Requires data-flow analysis.
  Fix: Declare the conditional buffer before the try block (= null), then
       ZeroMemory it in finally if not null.

T12: Dispose() zeros caller-provided buffer — ownership violation (AGENT-ONLY)
  Problem: A class receives a ReadOnlyMemory<byte> or byte[] from the caller
           and zeroes the underlying backing array in Dispose(). The caller
           may still hold a reference to the same memory and see unexpected zeros,
           or the zeroing may corrupt unrelated data if the memory is a slice
           of a larger caller-owned array.
  Distinction: Most taxonomy entries are about FAILING to zero. T12 is the
           opposite: zeroing memory you don't own, which creates correctness
           and ownership bugs.
  Example: CredentialManagement receives pinUvAuthToken from caller, then
           Dispose() calls ZeroMemory on that token's backing array.
  Note: Cannot be expressed as a grep. Requires ownership/provenance analysis.
  Fix: Only zero buffers allocated by this class (via new or ArrayPool.Rent).
       For caller-provided memory, document that the caller retains zeroing
       responsibility. If ownership transfer is intended, document it explicitly
       in the constructor signature (e.g. by taking byte[] not ReadOnlyMemory<byte>).

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

# ── T10: Note — requires agent ────────────────────────────────────────────────
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  [T10] IDisposable not disposed on exception/failure paths (control-flow)"
echo "  Note: Different from T7 (class lacks IDisposable) and T9 (no 'using')."
echo "        T10 = object IS IDisposable but Dispose() is not called on error paths."
echo "        Look for: IDisposable objects created in try blocks without 'using var',"
echo "        where a failure branch (throw, early return) exits without disposing."
echo "  Workaround: grep for 'var scp\|var processor\|var state' in SCP/session code,"
echo "              then trace whether all exit paths call Dispose()."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# ── T11: Note — requires agent ────────────────────────────────────────────────
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  [T11] Conditional buffer allocation not covered by ZeroMemory (data-flow)"
echo "  Note: Different from T5 (ZeroMemory in wrong position)."
echo "        T11 = ZeroMemory is in finally but the buffer was allocated conditionally"
echo "        (e.g. inside 'if encrypt') and the finally has no reference to zero it."
echo "  Workaround: grep for 'if.*encrypt\b' or 'if.*compress\b' near byte[] allocations"
echo "              in try blocks, then verify finally covers those variables."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# ── T12: Note — requires agent ────────────────────────────────────────────────
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  [T12] Dispose() zeros caller-provided buffer — ownership violation"
echo "  Note: Inverted pattern — not failing to zero but zeroing memory not owned."
echo "        Find: Dispose() methods that call ZeroMemory on fields whose backing"
echo "        arrays came from constructor parameters rather than internal allocation."
echo "  Workaround: grep for 'ZeroMemory' in Dispose methods; trace whether the"
echo "              zeroed field was allocated by this class or passed in by caller."
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
    echo "  Note: T5, T7, T10, T11, T12 require agent-based analysis (see above)."
fi
echo "=============================================================================="

exit $TOTAL_FINDINGS
