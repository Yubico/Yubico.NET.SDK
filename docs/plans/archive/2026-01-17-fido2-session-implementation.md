# Ralph Loop: FIDO2 Session Implementation

**Goal:** Autonomously implement the complete FIDO2/CTAP2 functionality from the Yubico.NET.SDK plan, phase by phase, tracking progress persistently across iterations.

**Reference Plan:** `docs/plans/2026-01-16-fido2-session-implementation.md`  
**Progress File:** `docs/ralph-loop/fido2-progress.md`

---

## Initial Setup (Every Iteration)

1. **Read progress file** at `docs/ralph-loop/fido2-progress.md` to understand current status
2. **Find next incomplete task** (highest priority first: P0 phases before P1/P2)
3. **Resume from where you left off** - check git history and recent commits
4. **Execute one complete phase task** per iteration (or multiple small tasks if they form a unit)
5. **Update progress file** at the END of this iteration with completion status
6. **Commit changes** with clear message indicating which tasks completed

---

## Context & Architecture

This is a comprehensive port of Java `yubikit-android` FIDO2/CTAP2 to C#.

### Key Points
- **Session Pattern:** Derive from `ApplicationSession` (see `SecurityDomainSession.cs` as template)
- **CBOR Infrastructure:** Use `System.Formats.Cbor` with strongly-typed generic builders
- **COSE Integration:** Reuse existing `Yubico.YubiKit.Core.Cryptography.Cose.*` types
- **WebAuthn Extensions:** First-class support (hmac-secret, credProtect, credBlob, etc.)
- **YK 5.7/5.8 Features:** encIdentifier, encCredStoreState, PPUAT decryption
- **Testing:** NSubstitute (not Moq), `[WithYubiKey(Capability.Fido2)]` attribute

### Required References
- Read `CLAUDE.md` at repo root for C# 14 patterns, `Span<T>/Memory<T>`, security best practices
- Consult `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs` for session template
- Use `Yubico.YubiKit.Core/src/Cryptography/` for COSE and key type utilities
- Check `.claude/skills/` for available helpers

---

## Build & Test Commands

**Build:**
```bash
dotnet build.cs build
```

**Tests:**
```bash
dotnet build.cs test
```

**Coverage:**
```bash
dotnet build.cs coverage
```

---

## Task Selection Strategy

### Priority Order (Strict)
1. **Phase 1 (Foundation)** → Phase 2 (CBOR) → Phase 3 (Core Session) → Phases 4–11 → Phase 12–13
2. Within each phase: marked tasks in order
3. Skip/defer Phase 8–10 unless explicitly needed (P1/P2 lower priority)

### Per-Iteration Scope (SHORT ITERATIONS ENCOURAGED)
- **Target: 1–2 task checkboxes per iteration** — Maximizes context window availability, enables fresh context for next iteration
- **Do NOT attempt 3+ tasks in one iteration** — Risk of context compaction, reduced effectiveness
- Complete 1 task fully (code + tests + verification + commit) before moving to next
- If you complete 2 small related tasks, that's ideal
- **After completing and verifying 1–2 tasks:** Commit, update progress file, and **feel satisfied**—do not force continuation to next iteration

### Task Breakdown Example
For "Task 2.1: Create CTAP data models" (one full iteration):
1. Define CtapRequest/CtapResponse base classes
2. Add basic serialization
3. Add unit tests
4. Verify build passes
5. **Commit with message:** `feat(fido2): Task 2.1 - Create CTAP data models`
6. **Update progress file** - check `[x] Task 2.1`
7. **End iteration** (or continue if naturally flowing to Task 2.2 which is small)

---

## Implementation Requirements

### Test Strategy for Autonomous Execution

**IMPORTANT**: The agent runs without physical user interaction or user verification.

- **Unit tests with mocks (NSubstitute):** Always write and run these. They test logic without hardware.
- **Hardware integration tests** (optional): Can be written to exercise the plugged-in YubiKey, but:
  - Tests requiring **user presence, user touch, OR user verification (UV) logic** must be marked: `[Trait("RequiresUserPresence", "true")]`
  - Examples of features that require this trait:
    - Physical user touch/presence confirmation
    - User verification flows (`verifyUv`, PIN verification, biometric verification)
    - Any CTAP command with UV requirement that requires real user interaction
  - Agent will **skip these tests** using test runner filters: `--filter "RequiresUserPresence!=true"`
  - Tests that do NOT require user verification (e.g., `GetInfoAsync()`, status checks, non-UV credential creation) can run against real hardware
- **Expected behavior:** Build passes, all non-user-presence tests pass, user-presence tests are skipped (not run).

### Code Quality (MUST FOLLOW)
1. **No `object?` types** - use `CtapRequestBuilder<T>` with type constraints
2. **C# 14 idioms** - `Span<T>`, `Memory<T>`, `ArrayPool<T>` patterns per CLAUDE.md
3. **Async-first** - all I/O uses `*Async` methods
4. **Error handling** - specific exception types, meaningful messages
5. **EditorConfig compliance** - match existing codebase style

### Testing Requirements
1. **Unit tests per task** - at minimum: success case + error case
2. **Use NSubstitute** - mock YubiKey responses, not Moq
3. **Mark hardware tests** - `[Trait("RequiresHardware", "true")]` (optional; unit tests required)
4. **User Presence Tests** - **IMPORTANT**: Tests requiring user touch/presence must be marked with `[Trait("RequiresUserPresence", "true")]` or similar, and **MUST BE SKIPPED** during autonomous ralph loop execution. The agent cannot interact with physical hardware. However, a YubiKey IS plugged in, so tests that do NOT require user presence/verification can and should exercise real hardware features when available.
5. **Skip User Presence Tests** - Before running test suite, filter out tests marked as requiring user presence. Use test runner filter: e.g., `dotnet test --filter "RequiresUserPresence!=true"`
6. **>80% coverage** for new code (excluding skipped user-presence tests)

### Documentation
1. **XML doc comments** on public APIs
2. **Architecture notes** in CLAUDE.md if subproject-specific
3. **Test class comments** explaining test scenarios

---

## Verification Checklist (MUST PASS BEFORE COMPLETION)

Before completing an iteration:

### Per-Iteration Verification
- [ ] ✅ **1–2 task checkboxes marked complete** in progress file
- [ ] ✅ **Committed your work** with message like `feat(fido2): Task X.Y completed`
- [ ] ✅ **Updated progress file** with completed tasks marked `[x]`

**After these are done, you may end the iteration.** The next iteration will resume from the next uncompleted task. Short iterations = fresh context window = better effectiveness.

### Before Outputting Completion Promise (Final Iteration Only)

Only after **ALL phases 1–13 are fully complete**, run the final verification:

### Build Verification
- [ ] Run `dotnet build.cs build` → exits 0
- [ ] No compiler errors or warnings (except pre-existing)

### Test Verification
- [ ] Run `dotnet build.cs test --filter "RequiresUserPresence!=true"` → all non-user-presence tests pass
- [ ] No test failures (user-presence tests are expected to be skipped/not-run)
- [ ] Coverage ≥80% for new code (if tooling available, excluding user-presence tests)
- [ ] If test filter syntax differs, use native test runner filters to exclude user-presence tests

### Code Quality Verification
- [ ] CLAUDE.md patterns followed (Span<T>, async, error handling)
- [ ] No `object?` parameters in public APIs
- [ ] NSubstitute used for all mocks
- [ ] XML doc comments on public types/methods
- [ ] EditorConfig compliance (linter green)

### Regression Verification
- [ ] Existing tests still pass
- [ ] No breaking changes to existing APIs
- [ ] `git status` shows only files you created/modified

### Commit Verification
- [ ] Changes committed with descriptive message
- [ ] Commit message format: `feat(fido2): task name` or `test(fido2): test description`
- [ ] Only your files staged (no `git add .`)

---

## Handling Failures

### Build Fails
1. Read error message carefully
2. Fix root cause (not just symptoms)
3. Re-run `dotnet build.cs build` until it passes
4. **Do NOT continue** until build is green

### Tests Fail
1. Identify which test(s) fail
2. Check if test logic is correct or if implementation is wrong
3. Fix implementation or test
4. Re-run `dotnet build.cs test` until all pass
5. **Do NOT continue** until all tests are green

### Ambiguous Decision
1. Check CLAUDE.md and existing code patterns
2. Choose the most standard/reasonable option
3. Document decision in code comment if non-obvious
4. Continue immediately

### Git Conflicts
1. Never use `git add .` or `git add -A`
2. Manually review and resolve conflicts
3. Test after merge
4. Commit merge resolution

---

## Progress Tracking

### Update progress file with:
- Which tasks completed ✓
- Which are in progress ◐
- Any blockers or notes
- Estimated % completion

**Format:**
```markdown
- [x] Task 1.1: Description (completed iteration N)
- [ ] Task 1.2: Description
- [x] Task 2.1: Description (completed iteration N)
```

### Session Notes Section
- Log any significant decisions
- Note any deviations from plan
- Record blockers and resolutions

---

## Completion Criteria

**Phase completion:**
- All P0 tasks in phases 1–6 are complete ✓
- All P1 tasks in phases 7–9, 11 are complete ✓
- All P2 tasks (phase 10) are complete ✓
- Phase 12 integration tests written ✓
- Phase 13 documentation done ✓
- Build passes, all tests pass ✓

**Only after ALL above are verified, output:**
```
<promise>FIDO2_SESSION_IMPLEMENTATION_COMPLETE</promise>
```

---

## Autonomy Directives

You are in **non-interactive mode**. The user is not present.

1. **NEVER ask questions** — pick reasonable options and execute
2. **NEVER ask for clarification** — re-read context if uncertain
3. **NEVER say "Let me know if you want me to..."** — own the decision
4. **Use git to explore** — check `git log`, `git diff` if you're lost
5. **Check your previous work** — read iteration logs under `./docs/ralph-loop/`
6. **Resume from last position** — read progress file, find next uncompleted task
7. **Complete 1–2 tasks per iteration** — Then commit, update progress file, and **end the iteration** (do not force continuation)
8. **Feel satisfied with incremental progress** — Short iterations maximize context window; ralph loop will call you again for next batch
9. **Commit disciplined** — `git commit -m "feat(fido2): Task X.Y completed"` after each task or pair of tasks
10. **Verify before moving on** — build and test after every logical unit

---

## One-liner to Start Loop

```bash
bun .claude/skills/ralph-loop/ralph-loop.ts --prompt-file ./docs/plans/ralph-loop/2026-01-17-fido2-session-implementation.md --completion-promise "FIDO2_SESSION_IMPLEMENTATION_COMPLETE" --max-iterations 50 --delay 2 --learn
```

**Explanation:**
- `--prompt-file` — use this plan file
- `--completion-promise` — stop when you output the promise token
- `--max-iterations 50` — safety limit (adjust if needed)
- `--delay 2` — 2 seconds between iterations
- `--learn` — generate learning analysis at the end
