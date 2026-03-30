---
name: Audit
description: Correctness scan workflow — finds obsolete code references, wrong signatures, and typos in documentation.
---

# Audit Workflow

Mechanical, deterministic scan for correctness errors (T1-T6). Produces verifiable findings with source citations.

## Success Criteria

Before starting, establish these testable criteria. Every criterion must be binary (true/false) and verified after execution.

| # | Criterion | Verified |
|---|-----------|----------|
| SC-0 | Project language, source dirs, docs dir, and deprecation profile are identified | ☐ |
| SC-1 | All deprecated items in source are extracted into deprecation map | ☐ |
| SC-2 | All `.md` files in docs (excluding changelogs) are scanned for code references | ☐ |
| SC-3 | Every finding includes source-code citation proving the deprecated status | ☐ |
| SC-4 | Every suggested fix references a replacement type that exists in current source | ☐ |
| SC-5 | Zero false positives remain after verification phase (each finding re-read from file) | ☐ |
| SC-6 | Findings are classified using T1-T6 taxonomy with correct category assignment | ☐ |

**Rule:** Do not produce the final report until all criteria are verified. If a criterion fails, loop back to the relevant phase.

## Prerequisites

Load on demand:
- `ErrorTaxonomy.md` — classification definitions
- `AgentDesign.md` — agent specs, language profiles, and model selection
- `FindingsSchema.md` — structured output format (all agents MUST emit findings in this schema)

## Algorithm (6 Phases)

### Phase 0: Discover Project
**Agent:** DiscoveryAgent | **Model:** Haiku

Auto-detect project characteristics. No configuration required.

1. **Check for existing config:** Look for `.docsaudit.yaml` in repo root. If found, load and skip to Phase 1.
2. **Detect language:** Glob for source files by extension (`*.cs`, `*.java`, `*.ts`, `*.py`, `*.go`, `*.rs`). Count per extension, excluding `node_modules/`, `vendor/`, `bin/`, `obj/`, `.git/`. Select dominant language.
3. **Find directories:**
   - Docs: Try `docs/`, `doc/`, `documentation/`, `manual/`, `guide/`. First match with `.md` files wins. Fallback: find directories with >5 clustered `.md` files.
   - Source: Try `src/`, `lib/`, `source/`, or find by project file patterns (`.csproj`, `pom.xml`, `package.json`, `Cargo.toml`, `go.mod`).
4. **Detect changelogs:** Find files matching `*changelog*`, `*whats-new*`, `*release-notes*`, `*history*` (case-insensitive). Add to exclusion list.
5. **Detect doc link format:** Grep docs for `xref:` (DocFX), `{@link` (Javadoc/TSDoc), `:class:` (Sphinx), intra-doc links (Rustdoc).
6. **Find security guidelines:** Search for files matching `*secur*`, `*sensitive*`, `*credential*` in docs.
7. **Output:** Project config object (see AgentDesign.md → DiscoveryAgent for schema).
8. **Present to user:** Show detected config and ask for confirmation before proceeding.

### Phase 1: Build Deprecation Map
**Agent:** DeprecationScanner | **Model:** Haiku

1. Using the language profile from Phase 0, grep source files for the deprecation pattern
2. For each match, extract:
   - Fully qualified name (namespace/module + identifier)
   - Simple name (just the identifier)
   - Category from the language profile's category list
   - Deprecation message text (contains replacement hints)
   - File path and line number
3. Parse replacement hints from deprecation messages (e.g., "Use X instead")
4. Output: `deprecationMap[]` — structured list, deduplicated by fully qualified name

**Exclusions:** Test files, example/sample projects (auto-detected or from config)

### Phase 2: Scan Documentation References
**Agent:** DocReferenceScanner | **Model:** Haiku

1. Find all `.md` files in the detected docs directory
2. For each file, extract:
   - **Code block references:** Parse fenced code blocks matching detected languages for type/function names, method calls, property accesses
   - **Prose references:** Find backtick-wrapped identifiers (`` `ClassName` ``)
   - **Doc links:** Extract links using the detected doc link format(s)
3. Tag each reference:
   - `referenceType`: `codeBlock` | `prose` | `docLink`
   - `entityName`: the referenced identifier
   - `language`: detected from code fence
   - `docFile`: file path
   - `line`: line number
   - `context`: surrounding 2 lines for reporting

**Exclusions:** Changelog files identified in Phase 0

### Phase 3: Cross-Reference
**Agent:** CrossReferencer | **Model:** Sonnet

1. For each doc reference, check if `entityName` appears in `deprecationMap`
2. Match by simple name first, then verify by context (namespace hints in surrounding code)
3. Classify the finding:
   - Code block + obsolete class → **T1**
   - Code block + obsolete method overload → **T2**
   - Prose + obsolete type → **T3**
   - Code block + obsolete property → **T4**
   - Code block + near-match to real class (edit distance ≤ 2) → **T5**
   - Code block + obsolete command class → **T6**
4. For each finding, look up replacement from obsolete message
5. Verify replacement type exists in source (grep for `class ReplacementName` or `interface ReplacementName`)
6. Generate suggested fix text

### Phase 4: Verify Findings
**Model:** Sonnet (same agent or inline)

For each finding:
1. Read the actual doc file at the cited line — confirm the reference is real
2. Read the source file at the cited line — confirm the deprecation marker is real
3. Check that the suggested replacement compiles conceptually (correct constructor/factory method, correct property names)
4. Discard false positives (e.g., type name appears in a comment explaining migration history)

### Phase 5: Format Output
Produce findings in the structured schema (see FindingsSchema.md → Finding Object Schema). Each finding MUST be a valid finding object with all required fields. Wrap all findings in an Agent Output envelope:

```
## Audit Results — [DATE]

### Summary
- Files scanned: X docs, Y source
- Deprecated items found: N
- Documentation references checked: M
- Findings: F (by category breakdown)

### Findings

[T1] file.md:line — Summary
  Evidence: ...
  Source: ...
  Suggested fix: ...

[T2] ...
```

Group by file, then by category within each file.

---

## Invocation

```
User: "Audit the docs for obsolete code references"
```

**Required inputs:**
- Source directories (auto-detect from project structure if not specified)
- Docs directory (auto-detect from project structure if not specified)

**Optional inputs:**
- Scope limiter: specific docs subdirectory (e.g., `application-piv/`)
- Exclude patterns: files to skip

---

## Parallelization

Phase 0 runs first (discovery). Phases 1 and 2 run in **parallel** (no dependency).
Phases 3-5 run **sequentially** (each depends on prior output).

```
Phase 0 (Haiku) ──→ Phase 1 (Haiku) ──┐
                ──→ Phase 2 (Haiku) ──┤
                                       ├──→ Phase 3 (Sonnet) → Phase 4 (Sonnet) → Phase 5
```

## Verification Protocol

After Phase 5, walk through each success criterion:

0. **SC-0:** Confirm discovery output includes: language name, at least one source dir, a docs dir, and the deprecation pattern. If any is missing, discovery failed.
1. **SC-1:** Count deprecated items found. If zero, warn — most codebases with docs have some. Re-check grep pattern against the language profile.
2. **SC-2:** Compare scanned file count against actual `.md` count in docs dir (minus exclusions). Discrepancies mean missed files.
3. **SC-3:** For each finding, confirm `Source:` field includes a real file path and line number. Spot-check 3 findings by reading the cited source line.
4. **SC-4:** For each suggested fix, grep for the replacement class/method in source. If not found, the fix is wrong — investigate.
5. **SC-5:** For each finding, re-read the doc file at the cited line. If the text doesn't match the evidence, discard the finding.
6. **SC-6:** Cross-check 3 random findings against ErrorTaxonomy.md T1-T6 definitions. Category must match.

**If any criterion fails:** Return to the relevant phase, fix, and re-verify. Do not output partial or unverified results.
