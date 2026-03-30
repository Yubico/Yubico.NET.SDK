---
name: Review
description: Quality review workflow — evaluates documentation from SDK Developer, SDK User, and Technical Writer perspectives.
---

# Review Workflow

Contextual, judgment-based review for quality issues (Q1-Q8). Produces suggestions with rationale.

## Success Criteria

Before starting, establish these testable criteria. Every criterion must be binary (true/false) and verified after execution.

| # | Criterion | Verified |
|---|-----------|----------|
| SC-1 | All scoped docs are reviewed through at least one audience lens | ☐ |
| SC-2 | Every Q1 finding includes the actual vs expected method signature | ☐ |
| SC-3 | Every Q8 finding references a specific SP pattern and the violated guideline | ☐ |
| SC-4 | No duplicate findings exist (same file:line, same category) | ☐ |
| SC-5 | Security review covers all code blocks that handle sensitive data variables | ☐ |

**Rule:** Do not produce the final report until all criteria are verified.

## Prerequisites

Load on demand:
- `ErrorTaxonomy.md` — Q1-Q8 definitions
- `SecurityPatterns.md` — SP1-SP6 anti-patterns for Q8 checks
- `AgentDesign.md` — agent specs and model selection
- `FindingsSchema.md` — structured output format (all agents MUST emit findings in this schema)

## Algorithm (5 Phases)

### Phase 0: Discover Project
**Agent:** DiscoveryAgent | **Model:** Haiku

Same as Audit workflow Phase 0. If running as part of a full audit, reuse the discovery output.

Auto-detect project structure, language, docs directory, security guidelines. Check for `.docsaudit.yaml` first.

### Phase 1: Scope Selection

Determine which docs to review:
- If user specifies files/directories → use those
- If "all" → enumerate all `.md` files in discovered docs dir, excluding detected changelogs
- If application-specific (e.g., "review PIV docs") → scope to that subdirectory

### Phase 2: Parallel Review (3 agents)

Launch three agents in parallel on the scoped doc set:

#### Agent A: SignatureVerifier (Sonnet)
Focus: Q1 (non-compiling code) + Q2 (prose contradicts code)

1. For each csharp code block in scoped docs:
   - Extract method calls, constructors, property accesses
   - Grep source for actual signatures
   - Compare parameter counts, types, return types
   - Flag mismatches as Q1
2. For each code block, read surrounding prose:
   - Does the prose describe what the code does?
   - Do they agree? Flag contradictions as Q2

#### Agent B: ProseReviewer (Opus)
Focus: Q3-Q6

Read each doc through three lenses:

**SDK Developer lens:**
- Q5: Are version-specific features gated? (e.g., "requires firmware 5.x")
- Are there assertions about behavior that depend on YubiKey version?

**SDK User lens:**
- Q3: Can a newcomer follow the code example without undeclared context?
- Q4: Are prerequisites (imports, setup, key state) mentioned or linked?

**Technical Writer lens:**
- Q6: Is terminology consistent within the doc and across related docs?
- Q7: Do all links (xref, anchors, URLs) resolve?

#### Agent C: SecurityReviewer (Opus)
Focus: Q8

1. Load SecurityPatterns.md
2. If DiscoveryAgent found a security guidelines doc → read it and derive project-specific anti-patterns
3. Find all code blocks that handle sensitive data:
   - Variable names: `pin`, `puk`, `password`, `key`, `managementKey`, `secret`, `privateKey`, `token`, `credential`
   - Method calls: common auth/key patterns (language-aware from discovery)
4. Check each against universal SP1-SP3 + language-specific patterns
5. Apply judgment notes (instructional focus vs. full security ceremony)
6. If no security guidelines found → skip project-specific checks, apply only universal SP1-SP3, note in output
7. Report Q8 findings with SP sub-classification

### Phase 3: Deduplicate and Merge

1. Collect findings from all three agents
2. Deduplicate: if same file:line flagged by multiple agents, keep the most specific finding
3. Sort by file, then by line number within file
4. Assign severity based on ErrorTaxonomy.md guidelines

### Phase 4: Format Output

```
## Quality Review Results — [DATE]

### Summary
- Files reviewed: X
- Findings: Y (Q1: a, Q2: b, ..., Q8: h)
- High severity: N
- Medium severity: M

### Findings by File

#### file.md

[Q3] file.md:45 — Missing context for connection object
  Issue: Code uses `connection` variable without showing where it comes from
  Audience: SDK User — newcomer can't follow this
  Suggestion: Add `using var connection = device.Connect(...)` before use

[Q8/SP2] file.md:103 — PIN buffer not zeroed after use
  Issue: `byte[] pin = ...` used in TrySetPin, never cleared
  Guideline: sensitive-data.md §2
  Suggestion: Wrap in try/finally with CryptographicOperations.ZeroMemory(pin)
```

---

## Invocation

```
User: "Review the PIV docs for quality issues"
User: "Review docs quality"
User: "Improve docs"
```

**Required inputs:**
- Docs directory (auto-detect)

**Optional inputs:**
- Scope (specific application or section)
- Focus (e.g., "just security" → only run SecurityReviewer)
- Audience filter (e.g., "from SDK User perspective" → only User lens findings)

## Verification Protocol

After Phase 4, walk through each success criterion:

1. **SC-1:** List scoped files and confirm each appears in at least one agent's output. Missing files = incomplete review.
2. **SC-2:** For each Q1 finding, confirm both `Expected:` and `Actual:` signatures are present. Spot-check 2 by grepping source.
3. **SC-3:** For each Q8 finding, confirm it names an SP pattern (SP1-SP6) and cites a section of sensitive-data.md.
4. **SC-4:** Sort findings by file:line — any consecutive duplicates? Remove them.
5. **SC-5:** Grep scoped docs for sensitive variable names (`pin`, `puk`, `password`, `key`, `managementKey`, `privateKey`). Every code block containing these must have been reviewed by SecurityReviewer.

**If any criterion fails:** Return to the relevant phase, fix, and re-verify.
