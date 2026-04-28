# Plan: Trim CLAUDE.md (40KB → ~16KB target)

## Context

`CLAUDE.md` at the repo root is **1,395 lines / 40KB**. Every Claude Code session loads it into context — so every redundant line is paid on every turn, every agent spawn, every sub-task.

Audit findings:

1. **Heavy duplication between Quick Reference (lines 51–118) and downstream deep-dives.** The same rule is stated three times in three formats: a Quick Reference bullet, a deep-dive section with prose, and 3–5 code examples. For example:
   - Memory rules: bullets at 67–72, full section at 319–457 (138 lines), with 4 levels of `✅ GOOD / ❌ BAD` blocks.
   - Security rules: bullets at 74–80, full section at 810–888 (78 lines), audit checklist with bash one-liners.
   - Modern C# rules: bullets at 82–87, full section at 930–1034 (104 lines).
   - Crypto rules: bullets at 104–106, full section at 776–808 (32 lines).

2. **Skill files already cover most deep-dive content.** `.claude/skills/` has dedicated SKILL.md files for `domain-build` (163 lines), `domain-test` (211 lines), `domain-security-guidelines`, `domain-secure-credential-prompt`, `tool-codemapper`, `domain-yubikit-compare`. CLAUDE.md duplicates ~370 lines of this material verbatim.

3. **`docs/` directory has natural extraction targets** that already exist: `docs/LOGGING.md`, `docs/TESTING.md`, `docs/COMMIT_GUIDELINES.md`, `docs/DEV-GUIDE.md`, `docs/AI-DOCS-GUIDE.md`. Two new docs (`docs/MEMORY-MANAGEMENT.md`, `docs/CSHARP-PATTERNS.md`) would absorb the bulk of the deep-dive material.

**Intended outcome:** ~60% size reduction (40KB → ~16KB) using the user's three-pronged approach: conservative reword + JIT extraction to domain docs + delete pure duplicates of skill files. **Two sections preserved verbatim per user direction:** Type Selection (lines 459–731) and Test Philosophy (lines 1184–1304).

## Approach

Three lanes, applied per section:

- **REWORD** — keep in CLAUDE.md, shorten prose, drop redundant examples, keep mandate
- **EXTRACT** — move to JIT doc under `docs/`, leave a one-line reference in CLAUDE.md ("For X, read `docs/Y.md`")
- **DELETE** — pure duplicate of a skill file, replaced by a Quick Reference bullet pointing to the skill

CLAUDE.md keeps everything that is **a mandate, a project-specific override of common practice, or content that must be loaded every session**. Deep-dive examples that exist elsewhere become JIT.

## Per-Section Treatment

| Lines | Section | Current | Treatment | New size |
|---|---|---|---|---|
| 1–49 | Project Overview & Structure | 49 | **REWORD** — collapse module list to 2-column compact form | ~30 |
| 51–118 | Quick Reference - Critical Rules | 68 | **REWORD** — keep all mandates, dedupe redundant ✅/❌ pairs, tighten phrasing | ~50 |
| 120–208 | Build and Test Commands | 89 | **DELETE** — `domain-build` skill (163 lines) covers it. Replace with: "Build/test: invoke `domain-build` and `domain-test` skills. NEVER use `dotnet build` or `dotnet test` directly." | ~5 |
| 210–248 | Architecture - Core Components & Patterns | 39 | **REWORD** — derivable from `codemapper`. Keep only the non-obvious parts: factory pattern names, APDU pipeline order | ~15 |
| 249–275 | Property Conventions | 27 | **EXTRACT** → `docs/CSHARP-PATTERNS.md` (new) | ~3 (pointer) |
| 277–303 | Logging Conventions | 27 | **EXTRACT** → `docs/LOGGING.md` (existing — append) | ~3 (pointer) |
| 305–317 | Target Framework + Platform-Specific | 13 | **REWORD** — merge into Project Overview | merged |
| 319–457 | Memory Management Hierarchy (138) | 138 | **EXTRACT** → `docs/MEMORY-MANAGEMENT.md` (new). Keep the **decision tree** (lines 446–457) inline since it's the canonical reference table | ~18 (tree + pointer) |
| **459–731** | **Type Selection (readonly struct vs ...)** | **273** | **KEEP AS-IS** (per user) | 273 |
| 732–774 | Anti-Patterns (ToArray, LINQ, ArrayPool leak) | 43 | **EXTRACT** → `docs/MEMORY-MANAGEMENT.md` | ~3 (pointer) |
| 776–808 | Cryptography APIs | 33 | **EXTRACT** → `docs/CRYPTO-APIS.md` (new) | ~3 (pointer) |
| 810–888 | Sensitive Data Handling + Audit Checklist | 79 | **DELETE** — `domain-security-guidelines` + `domain-secure-credential-prompt` skills cover this. Quick Reference Security bullets (74–80) retain mandates | ~3 (pointer) |
| 890–917 | APDU and Protocol Buffers | 28 | **EXTRACT** → `docs/MEMORY-MANAGEMENT.md` (Span slicing examples) | ~3 (pointer) |
| 919–1034 | Code Style & Modern C# Language Features | 116 | **EXTRACT** → `docs/CSHARP-PATTERNS.md` (new). Quick Reference Modern C# bullets (82–87) retain mandates | ~3 (pointer) |
| 1036–1101 | What NOT to Do | 66 | **REWORD** — these are real mandates; condense from prose+examples to bullet list | ~20 |
| 1103–1166 | Additional Guidelines (immutable, readonly, ValueTask, validation, FixedTimeEquals) | 64 | **EXTRACT** → `docs/CSHARP-PATTERNS.md`. Move FixedTimeEquals mandate to Quick Reference Security (already there at line 78) | ~3 (pointer) |
| 1167–1183 | Integration Test Strategy table | 17 | **REWORD** — keep the table verbatim (it IS the policy), drop surrounding prose | ~12 |
| **1184–1304** | **Test Philosophy: Value Over Coverage** | **121** | **KEEP AS-IS** (per user) | 121 |
| 1306–1359 | Test Structure & Guidelines (descriptive names, cleanup) | 54 | **EXTRACT** → `docs/TESTING.md` (existing — append) | ~3 (pointer) |
| 1361–1382 | Git Workflow + Commit Discipline | 22 | **REWORD** — already lean, tighten by 30% | ~10 |
| 1384–1395 | Pre-Commit Checklist | 12 | **REWORD** — collapse to one-line bullets | ~10 |

**Estimated final size:** ~590 lines / ~16KB (60% reduction).

## Files to Create/Modify

**Modify:**
- `CLAUDE.md` (root) — primary target
- `docs/LOGGING.md` — append "Logging Conventions" content from CLAUDE.md L277–303
- `docs/TESTING.md` — append "Test Structure & Guidelines" from CLAUDE.md L1306–1359

**Create (JIT domain docs):**
- `docs/MEMORY-MANAGEMENT.md` — Memory Hierarchy (L319–457) + Anti-Patterns (L732–774) + APDU Span examples (L890–917)
- `docs/CRYPTO-APIS.md` — Cryptography APIs (L776–808). Cross-references `domain-secure-credential-prompt` skill
- `docs/CSHARP-PATTERNS.md` — Property Conventions (L249–275) + Code Style/Language Features (L919–1034) + Additional Guidelines (L1103–1166)

### JIT Doc Discoverability Header (REQUIRED)

Each JIT doc must open with a YAML frontmatter block mirroring the PAI skill format (`~/.claude/skills/CreateSkill/SKILL.md` line 3 is the canonical example). This gives agents a pattern-matchable trigger so they can decide to load the doc the same way they decide to invoke a skill — by intent, not by browsing.

**Mandatory header format:**

```markdown
---
name: <FileBasename>
description: <One-line purpose>. READ WHEN <trigger keywords/phrases that should pull this doc into context>. Cross-references: <related skills or docs>.
---

# <Title>
```

**Example (`docs/MEMORY-MANAGEMENT.md`):**

```markdown
---
name: MemoryManagement
description: Span/Memory/ArrayPool decision rules and APDU buffer patterns for the YubiKit SDK. READ WHEN allocating byte buffers, working with APDU data, choosing between Span and Memory, sensitive data lifetime, ArrayPool rent/return, stackalloc sizing, zero-allocation hot paths. Cross-references skills/domain-secure-credential-prompt, skills/tool-codemapper.
---

# Memory Management
```

**Why this format:**
- Mirrors `description: ... USE WHEN <triggers>` from `~/.claude/skills/CreateSkill/SKILL.md:3` — proven discovery pattern
- `READ WHEN` (vs skill's `USE WHEN`) signals these are reference docs, not invokable skills
- Trigger keywords are concrete actions/intents, not generic topics — agents pattern-match on what they're about to do
- Cross-references chain agents to deeper or adjacent context

**Quick Reference pointers in CLAUDE.md must include the trigger context too**, e.g.:
> Memory: see `docs/MEMORY-MANAGEMENT.md` (load when allocating buffers, working with APDU data, or choosing Span vs Memory).

This double-reinforces discovery: the agent sees the trigger in CLAUDE.md (always loaded) AND in the doc's frontmatter (visible during file search/grep).

## CLAUDE.md New Skeleton (after trim)

```
# CLAUDE.md
[1-line project description + branch context]

IMPORTANT: subproject CLAUDE.md rule

## Project Overview (~30 lines: structure + tech)

## Quick Reference - Critical Rules (~50 lines)
   All mandates retained. Each section ends with a pointer:
   "Deep dive: docs/X.md or .claude/skills/Y/"

## Architecture (~15 lines)
   Non-obvious patterns only. "Run codemapper for the rest."

## Type Selection: readonly struct vs struct vs class (273 lines, KEEP)

## Testing
   ### Integration Test Strategy table (~12 lines)
   ### Test Philosophy: Value Over Coverage (121 lines, KEEP)
   ### Pointer to docs/TESTING.md for structure & guidelines

## What NOT to Do (~20 lines, condensed bullets)

## Git Workflow + Commit Discipline (~10 lines)

## Pre-Commit Checklist (~10 lines)
```

## Critical Files to Modify

- `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/CLAUDE.md` — root file
- `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/docs/LOGGING.md` — append section
- `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/docs/TESTING.md` — append section
- `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/docs/MEMORY-MANAGEMENT.md` — new
- `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/docs/CRYPTO-APIS.md` — new
- `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/docs/CSHARP-PATTERNS.md` — new

## Skill Improvement Opportunities (deferred — flagged for follow-up)

If during execution we find skill files have gaps that CLAUDE.md was filling, file follow-up tasks rather than expand CLAUDE.md back. Candidates:
- `.claude/skills/domain-build/SKILL.md` — verify it covers all flag examples currently in CLAUDE.md L157–183
- `.claude/skills/domain-test/SKILL.md` — verify integration strategy matrix is referenced

**Separate follow-up work item (out of scope for this trim, but noted):**
Audit all `.claude/skills/*/SKILL.md` frontmatter `description` fields against the PAI `USE WHEN <triggers>` pattern from `~/.claude/skills/CreateSkill/SKILL.md:3`. Several project skills (e.g., `domain-build`, `domain-test`, `tool-codemapper`) may have terse descriptions that don't expose the trigger keywords agents would naturally search for ("build the project", "run integration tests on PIV", "find a symbol in core"). Improving these would let agents discover skills without CLAUDE.md needing to enumerate them in the Quick Reference.

## Verification

### Tier 1 — Static checks (fast, mechanical)

1. **Mandate preservation** — `grep -E "(NEVER|ALWAYS|✅|❌|MUST)" CLAUDE.md.before` count vs combined (new CLAUDE.md + extracted JIT docs). Every mandate symbol must map to either: a Quick Reference rule, a section retained verbatim, or a JIT doc that includes it. **Acceptance: 100% mandate retention.**
2. **Pointer integrity** — `for f in $(grep -oE "docs/[A-Z-]+\.md" CLAUDE.md); do test -f "$f" || echo "MISSING: $f"; done`. **Acceptance: zero missing.**
3. **Skill reference integrity** — same check for `.claude/skills/X/SKILL.md` references.
4. **Size delta** — `wc -c CLAUDE.md` before/after; expect ~40KB → ~16KB. **Acceptance: ≥50% reduction.**
5. **Sub-CLAUDE.md compatibility** — module-level `src/*/CLAUDE.md` files are untouched and still authoritative for their domain (the rule at root L6 is preserved).

### Tier 2 — Agent A/B test harness (the real measurement)

**Question we need to answer:** does the trimmed system produce equivalent or better agent behavior on real tasks?

**Setup:**
1. Snapshot the current CLAUDE.md → `Plans/eval/baseline/CLAUDE.md`.
2. After trim, snapshot new structure → `Plans/eval/treatment/CLAUDE.md` + `Plans/eval/treatment/docs/*.md`.
3. Build a **task corpus** of 8–12 synthetic, discardable tasks designed to exercise specific mandates and JIT triggers. Each task lives in `Plans/eval/tasks/NN-task-name.md`.

**Task corpus (each must hit a specific mandate or JIT trigger):**

| # | Task prompt | What it probes | Expected agent behavior |
|---|---|---|---|
| 1 | "Add a method that accepts a PIN as `ReadOnlySpan<byte>` and verifies it" | Security: ZeroMemory, no logging | Agent uses `ZeroMemory`, validates length, never logs PIN |
| 2 | "Build the WebAuthn project and run its smoke tests" | Build/test skill discovery | Invokes `domain-build`/`domain-test`, uses `dotnet toolchain.cs --project WebAuthn --smoke`, NEVER `dotnet build` |
| 3 | "I need a 4KB buffer for an APDU response — what's the right type?" | Memory hierarchy JIT discovery | Loads `docs/MEMORY-MANAGEMENT.md`, recommends `ArrayPool<byte>.Rent` with try/finally |
| 4 | "Refactor `DeviceMetadata` (28 bytes, mutable) to be more efficient" | Type Selection (preserved verbatim) | Reaches for the Type Selection table, recommends class (>16 bytes) |
| 5 | "Write a unit test for `ScpInitializer.Route()`" | Test Philosophy (preserved verbatim) | Refuses validation-only tests, writes behavior test or documents limitation |
| 6 | "Implement SHA-256 of an APDU payload" | Crypto APIs JIT discovery | Loads `docs/CRYPTO-APIS.md`, uses `SHA256.HashData` with stackalloc, not `SHA256.Create()` |
| 7 | "Add a new `IConnection` implementation for testing" | Architecture awareness + logging | Uses static `YubiKitLogging.CreateLogger<T>()`, NOT injected `ILogger` |
| 8 | "Commit my changes" | Commit discipline | Uses `git add path/to/file`, NEVER `git add .` |
| 9 | "Add a method that returns processed APDU bytes" | Anti-pattern: `.ToArray()` | Uses `Span<byte>` parameters, avoids unnecessary allocation |
| 10 | "Compare the Java `Fido2Session.makeCredential` to our C# port" | yubikit-compare skill discovery | Invokes `domain-yubikit-compare` skill, does byte-level forensic analysis |

**Execution protocol:**

```bash
# Pseudo-script — actual orchestration runs via Agent tool

for task in Plans/eval/tasks/*.md; do
  # Baseline run
  cp Plans/eval/baseline/CLAUDE.md ./CLAUDE.md
  Agent({
    description: "A/B baseline run",
    subagent_type: "general-purpose",
    prompt: "<task content>. Report: (1) which docs/skills you loaded, (2) which mandates you applied, (3) the code/answer you produced.",
    isolation: "worktree"
  }) → capture transcript to Plans/eval/results/baseline-NN.json

  # Treatment run
  cp Plans/eval/treatment/CLAUDE.md ./CLAUDE.md
  cp Plans/eval/treatment/docs/*.md docs/
  Agent({...same prompt...}) → capture to Plans/eval/results/treatment-NN.json
done
```

**Note on isolation:** spawn each agent with `isolation: "worktree"` so file edits don't pollute the workspace. The agent's *transcript* (what it loaded, what tools it called, what it produced) is the data — its file changes are discarded.

**Scored metrics (per task, then aggregate):**

| Metric | How measured | Pass criterion |
|---|---|---|
| **Mandate adherence** | Did the agent's output follow the project-specific rule? (e.g., used `ZeroMemory`, used `toolchain.cs`) | Treatment ≥ baseline rate |
| **Discovery latency** | Tool calls before reaching the right doc/skill | Treatment ≤ baseline + 1 |
| **JIT doc load accuracy** | Did the agent load the doc whose trigger matched the task? | ≥80% on triggered tasks |
| **False loads** | Did the agent load JIT docs irrelevant to the task? | ≤1 per task |
| **Output equivalence** | Does the produced code/answer satisfy the same rubric as baseline? | Equivalent or better |
| **Total prompt tokens** | Initial system + on-demand reads, per task | Treatment average ≤ baseline |

**Acceptance bar:**
- **MUST PASS:** mandate adherence (no regression on any task), pointer integrity (Tier 1), zero missing JIT docs when their triggers fire
- **SHOULD PASS:** total token usage averaged across corpus is lower for treatment
- **NICE TO HAVE:** discovery latency improves (because agent reads less to find the relevant rule)

**Failure modes to watch for:**
- Agent in treatment skips a JIT doc because its trigger phrasing doesn't match the task — fix: improve `READ WHEN` triggers in the doc's frontmatter
- Agent in treatment violates a mandate that lived in a deleted section — fix: that mandate must move back to Quick Reference or its JIT doc, not be deleted
- Agent in treatment loads 4 JIT docs for a task that needed 1 — fix: trigger keywords are too broad

### Tier 3 — Reversibility

Keep `CLAUDE.md.before` (full backup) and a single revert commit hash for one week post-merge. If real-world sessions show degradation that the synthetic tasks missed, revert is one `git revert` away. Document the revert procedure in the merge commit body.

## Non-Goals

- No changes to `.claude/skills/` files (audit only — improvements deferred)
- No changes to subproject `src/*/CLAUDE.md` files
- No changes to user's `~/.claude/CLAUDE.md` (out of scope)
- No deletion of the two preserved sections (Type Selection, Test Philosophy)
