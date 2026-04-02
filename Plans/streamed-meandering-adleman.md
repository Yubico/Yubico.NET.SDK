# Plan: Implement Remaining YubiKey Applets via Agate

## Context

The Yubico.NET.SDK is undergoing a **2.0 rewrite on `yubikit-*` branches** (not develop/main). Management, SecurityDomain, PIV (`yubikit-piv`), and FIDO2 (`yubikit-fido`) are implemented. Four applets remain skeleton-only: OATH, YubiOTP, HsmAuth, OpenPGP. The goal is to implement all remaining applets using `agate`, a non-interactive AI orchestrator. Each applet gets its own `yubikit-{applet}` branch, full tests, and a Spectre.Console CLI tool. A physical YubiKey is attached for integration testing.

**CRITICAL: Do NOT touch `develop` or `main`. All branches are `yubikit-*` off `yubikit`. This is a 2.0 effort.**

**Priority: Correctness and consistency over speed.** Code must look like it was written by the same developer who wrote Management/SecurityDomain/PIV.

## Strategy: One Agate Per Applet, Sequential

**Why not one big GOAL.md?**
- Each applet has distinct wire protocols; focused attention produces better protocol fidelity
- Sequential means later applets benefit from completed ones as additional reference
- Clean review checkpoint between each applet
- `.agate/` state cleaned between runs to avoid interference

**Execution order** (simplest → most complex):
1. **OATH** (566 lines canonical) — TOTP/HOTP, SmartCard only, simple TLV
2. **YubiOTP** (928 lines) — Dual transport, complex flags, builder pattern
3. **HsmAuth** (718 lines) — EC P256 crypto, session keys, security-critical
4. **OpenPGP** (1,793 lines) — Largest applet, partial classes required, complex BER-TLV
5. **FIDO2 CLI** — CLI tool only (applet already complete on feature branch)

## Step 1: Prepare GOAL.md Files

Create 5 GOAL.md files using the template below, customized per applet. Store them in `Plans/goals/` for reference.

### GOAL.md Template Structure

Each GOAL.md must contain these sections:

```
1. Context — What this is, what SDK it's for
2. MANDATORY READ LIST — Exact file paths to read before any coding:
   - Root CLAUDE.md (coding standards)
   - Yubico.YubiKit.Management/CLAUDE.md (session pattern, backend, DI, test infra)
   - Yubico.YubiKit.SecurityDomain/CLAUDE.md (reset pattern, SCP integration)
   - The canonical Python file for this applet
   - Existing C# session files (ManagementSession.cs, SecurityDomainSession.cs)
3. Architecture Requirements — Session pattern, backend, DI, extensions, partial classes, models
4. Wire Protocol Details — Extracted from Python: AID, TLV tags, INS bytes, all operations
5. CLI Tool Requirements — What the TUI should demonstrate
6. Coding Standards Checklist — Inline, not just "see CLAUDE.md"
7. Git Workflow — Branch name, commit conventions
8. Definition of Done — Build, test, format, integration test, CLI works
```

### Critical: What Makes the GOAL.md Effective

- **Explicit file paths** — Don't say "follow existing patterns"; say "read `Yubico.YubiKit.Management/src/ManagementSession.cs` and replicate its structure"
- **Wire protocol extracted** — List every INS byte, TLV tag, and enum value from the Python canonical
- **Anti-pattern list** — Explicitly forbid `== null`, `#region`, `.ToArray()`, injected ILogger, etc.
- **Structural mandates** — "Use partial classes if session exceeds 300 lines", "Use `extension()` syntax for IYubiKey extensions"

### Applet-Specific Customizations

| Applet | Python File | Java Dir | Transport | Special Notes |
|--------|-------------|----------|-----------|---------------|
| OATH | `yubikey-manager/yubikit/oath.py` | `yubikit-android/oath/` | SmartCard only | otpauth:// URI parsing, PBKDF2 key derivation |
| YubiOTP | `yubikey-manager/yubikit/yubiotp.py` | `yubikit-android/yubiotp/` | SmartCard + OTP HID | Dual backend pattern (like Management), flag enums, builder pattern |
| HsmAuth | `yubikey-manager/yubikit/hsmauth.py` | `yubikit-android/` (search) | SmartCard only | EC P256, ZeroMemory everywhere, management key auth |
| OpenPGP | `yubikey-manager/yubikit/openpgp.py` | `yubikit-android/openpgp/` | SmartCard only | Must use partial classes, complex BER-TLV, RSA+ECC keys |
| FIDO2 CLI | N/A (applet done) | N/A | N/A | CLI only, branch from `origin/yubikit-fido` |

## Step 2: Create All Branches and Worktrees

```bash
cd /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK
git checkout yubikit

# Create all feature branches from yubikit (NEVER from develop or main)
for applet in oath yubiotp hsmauth openpgp fido2-cli; do
  git branch yubikit-$applet yubikit 2>/dev/null || true
  git worktree add /Users/Dennis.Dyall/Code/y/agate-$applet yubikit-$applet
done

# Place GOAL.md in each worktree
cp Plans/goals/goal-oath.md /Users/Dennis.Dyall/Code/y/agate-oath/GOAL.md
cp Plans/goals/goal-yubiotp.md /Users/Dennis.Dyall/Code/y/agate-yubiotp/GOAL.md
cp Plans/goals/goal-hsmauth.md /Users/Dennis.Dyall/Code/y/agate-hsmauth/GOAL.md
cp Plans/goals/goal-openpgp.md /Users/Dennis.Dyall/Code/y/agate-openpgp/GOAL.md
cp Plans/goals/goal-fido2-cli.md /Users/Dennis.Dyall/Code/y/agate-fido2-cli/GOAL.md
```

## Step 3: Launch All Agate Instances in Parallel

Launch each agate as a background process with output logged to files for monitoring.

```bash
# Launch all agate workflows in parallel as background processes
for applet in oath yubiotp hsmauth openpgp fido2-cli; do
  (cd /Users/Dennis.Dyall/Code/y/agate-$applet && \
   agate auto --agent claude \
   > /Users/Dennis.Dyall/Code/y/agate-$applet/agate.log 2>&1) &
  echo "Launched agate for $applet (PID: $!)"
done

# Save PIDs for monitoring
```

From this Claude session, use `Bash` with `run_in_background` for each agate invocation.

## Step 4: Monitor All Running Instances

```bash
# Check status of all instances
for applet in oath yubiotp hsmauth openpgp fido2-cli; do
  echo "=== $applet ==="
  (cd /Users/Dennis.Dyall/Code/y/agate-$applet && agate status 2>&1) || true
  echo
done

# Tail logs
tail -f /Users/Dennis.Dyall/Code/y/agate-*/agate.log

# If agate exits 255 (needs input), re-run for that applet:
cd /Users/Dennis.Dyall/Code/y/agate-{applet} && agate auto --agent claude
```

## Step 5: Post-Completion Review

When each agate completes:
```bash
cd /Users/Dennis.Dyall/Code/y/agate-{applet}
dotnet build.cs build     # Zero warnings
dotnet build.cs test --filter "Category!=RequiresUserPresence"  # All pass
dotnet format --verify-no-changes
```

Worktrees are **kept** for CLI-driven E2E testing with the physical YubiKey.
Branches are **never merged to develop or main** — this is a 2.0 effort on `yubikit-*` branches.

## Security Requirements (from CLAUDE.md)

Every applet GOAL.md must emphasize:
- `CryptographicOperations.ZeroMemory()` on ALL sensitive buffers (PINs, keys, passwords, session keys, challenges)
- `CryptographicOperations.FixedTimeEquals()` for any crypto comparisons
- `using var` for all crypto objects (Aes, RSA, HMAC, etc.)
- `ArrayPool` buffers zeroed in `finally` blocks before return
- Never log PINs, keys, or sensitive payloads — only log metadata (slot numbers, lengths)
- Security audit checklist from CLAUDE.md must pass

## Testing Constraints (from docs/TESTING.md)

- **Always use `dotnet build.cs test`** — never `dotnet test` directly (xUnit v2/v3 differences)
- **`[WithYubiKey]` + `[InlineData]` is incompatible** — use separate test methods per parameter
- **User-presence tests will fail** — any test requiring touch/insertion must use `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]` and agents must skip them: `--filter "Category!=RequiresUserPresence"`
- **Touch policy tests** — set appropriate touch policies to avoid requiring user presence
- Integration tests use `[WithYubiKey]` attribute with `YubiKeyTestState` injection
- Use `ConnectionType` filtering (e.g., `ConnectionType.Ccid`) not device ID parsing

## CLI Tools as E2E Verification

Each CLI tool must support **command-line parameters** (not just interactive menus) so Claude can drive automated end-to-end testing against the physical YubiKey. This is how we verify the API actually works — the same pattern used in the PIV branch (`PivTool`).

**Worktrees are NOT removed after agate completes.** They stay for post-implementation E2E testing.

## Step 3: Monitoring and Quality Gates

### During Execution

- `agate status` — check sprint/task progress
- `agate suggest "..."` — steer the agent (e.g., "use partial classes", "check CLAUDE.md memory management rules")
- Tail `.agate/` logs for real-time output

### Post-Sprint Verification

After each agate completes, run these checks:

```bash
# Pattern adherence
grep -rn "== null" Yubico.YubiKit.{Module}/src/     # Must be zero (use "is null")
grep -rn "#region" Yubico.YubiKit.{Module}/src/      # Must be zero
grep -rn "\.ToArray()" Yubico.YubiKit.{Module}/src/  # Review each occurrence
grep -rn "LoggingFactory" Yubico.YubiKit.{Module}/src/ # Must exist (not injected ILogger)
grep -rn "ConfigureAwait" Yubico.YubiKit.{Module}/src/ # Must exist on all awaits
grep -rn "CancellationToken" Yubico.YubiKit.{Module}/src/ # Must exist on async methods
```

### Dev Team Review

After all applets complete, run a dev-team review pass to catch cross-cutting issues.

## Step 4: What to Implement Now (in this session)

1. **Create the 5 GOAL.md files** in `Plans/goals/`
2. **Set up the first worktree** for OATH
3. **Launch agate** for OATH and monitor
4. Iterate through remaining applets

## Key Reference Files

| File | Purpose |
|------|---------|
| `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/CLAUDE.md` | All coding standards |
| `Yubico.YubiKit.Management/CLAUDE.md` | Session architecture reference (29KB) |
| `Yubico.YubiKit.SecurityDomain/CLAUDE.md` | Reset/SCP patterns (10KB) |
| `Yubico.YubiKit.Management/src/ManagementSession.cs` | Session class pattern |
| `Yubico.YubiKit.Management/src/DependencyInjection.cs` | DI pattern |
| `Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs` | Extension pattern |
| `Yubico.YubiKit.Management/examples/ManagementTool/` | CLI tool pattern |
| `Yubico.YubiKit.Piv/examples/PivTool/` | CLI tool pattern (on yubikit-piv branch) |
| `yubikey-manager/yubikit/oath.py` | OATH canonical (566 lines) |
| `yubikey-manager/yubikit/yubiotp.py` | YubiOTP canonical (928 lines) |
| `yubikey-manager/yubikit/hsmauth.py` | HsmAuth canonical (718 lines) |
| `yubikey-manager/yubikit/openpgp.py` | OpenPGP canonical (1,793 lines) |

## Step 6: Final Quality Pass — Dev Team Review + Cross-Branch Consistency

After all 5 agate runs complete, dispatch **independent dev-team review agents** on each branch:

```bash
# For each worktree, run dev-team review in parallel
for applet in oath yubiotp hsmauth openpgp fido2-cli; do
  # Launch review agent per branch
done
```

The review agents should:
1. **Compare patterns across branches** — ensure consistency (same DI pattern, same extension style, same test helpers)
2. **Fix recurring anti-patterns** — if one branch uses `== null` and others use `is null`, fix the outlier
3. **Cross-check security** — verify ZeroMemory usage on ALL branches
4. **Normalize naming** — ensure consistent naming conventions across all applets

After reviews complete, run **autonomous CLI testing** against the physical YubiKey:

```bash
# For each applet, run the CLI tool with automated commands
# OATH: list, add, calculate, delete
# YubiOTP: status, configure HMAC, calculate
# HsmAuth: list, add symmetric, delete, reset
# OpenPGP: status, generate key, sign, reset
# FIDO2: info, PIN set, make credential
```

## Verification

After all 5 agate runs + dev-team review + CLI testing:
1. Each applet builds with zero warnings
2. All unit tests pass
3. Integration tests pass with physical YubiKey (skip user-presence)
4. Each CLI tool runs and demonstrates all operations via command-line parameters
5. `dotnet format --verify-no-changes` passes
6. Pattern compliance checks pass (no `== null`, `#region`, etc.)
7. Each applet has its own CLAUDE.md
8. All branches pushed to origin
9. Cross-branch consistency verified by dev-team review
10. Autonomous CLI E2E tests pass on all applets

## Future Work (Post 2.0 Initial Delivery)

### 1. Management Session as Authoritative Firmware Version Source
Alpha/beta YubiKey firmware reports placeholder version `0.0.1` from each applet's
SELECT response. The true firmware version is only available via the Management session.
**Current workaround:** `Major == 0` sentinel in `ApplicationSession.IsSupported()` and
`PcscProtocol.Configure()` treats the device as modern (5.x). This works for internal
alpha/beta hardware but is not a production-quality solution.
**Proper fix:** At `ApplicationSession.InitializeCoreAsync()`, if the applet-reported
version has `Major == 0`, open a short-lived ManagementSession to read the true firmware
version. Cache it for the session lifetime. This requires careful design to avoid PCSC
transaction conflicts with the caller's open session.

### 2. FIDO2 over SmartCard on 5.8+ Devices
YubiKey 5.8 adds FIDO2 over SmartCard (CCID) transport, not just HID FIDO.
FidoTool currently unconditionally prefers HID FIDO in non-interactive mode.
**Fix:** Detect firmware >= 5.8.0 via Management session and allow SmartCard FIDO.
Blocked by #1 (need true firmware version to make this decision).

### 3. CLI Shared Infrastructure Extraction
All 5 CLI tools now follow the canonical DeviceSelector pattern but still contain
copy-paste code. A shared project `Yubico.YubiKit.Examples.Shared` could contain:
- `DeviceSelector.cs` (canonical implementation)
- `OutputHelpers.cs` (error → stderr, data → stdout)
- `SessionHelper.cs` patterns

### 4. OpenPGP Integration Test Edge Cases (7/28 failing)
The 7 remaining failures are:
- `GetAlgorithmAttributes_DefaultState_ReturnsRsa2048` — SW=0x6B00 on alpha firmware
- `VerifyPin_WrongPin_ThrowsWithRemainingAttempts` — error message format mismatch
- Key generation tests (4) — ordering/state dependencies
- `GetAlgorithmInformation` — algorithm information query format
These require further investigation on production firmware hardware.
