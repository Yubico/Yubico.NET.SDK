---
name: AgentDesign
description: Agent types, mindsets, model selection, language profiles, and orchestration patterns for DocsAudit skill workflows.
type: reference
---

# Agent Design

DocsAudit uses specialized agents for different phases. Each agent has a defined mindset, scope, and recommended Claude model tier. The system auto-detects project characteristics — no configuration required.

---

## Model Selection Strategy

| Tier | Model | Use For | Cost/Speed |
|------|-------|---------|------------|
| **Haiku** | `haiku` | Discovery, bulk scanning, pattern matching, grep-heavy work | Fastest, cheapest |
| **Sonnet** | `sonnet` | Cross-referencing, signature comparison, prose review | Balanced |
| **Opus** | `opus` | Judgment calls, security review, final synthesis | Slowest, highest quality |

**Principle:** Use the cheapest model that can reliably perform the task. Escalate only when judgment or nuance is required.

---

## Language Profiles

Built-in profiles for auto-detection. The DiscoveryAgent selects the correct profile based on source file counts.

### C# (.cs)
```
extensions: .cs
deprecation_pattern: \[Obsolete\(
message_format: [Obsolete("message")] — extract quoted string
replacement_hint: Look for "Use X instead" in message
categories: class, interface, method-overload, property, constructor, command
code_fence: csharp, cs
doc_link_formats: xref:Namespace.Type.Member (DocFX)
```

### Java (.java)
```
extensions: .java
deprecation_pattern: @Deprecated
message_format: @deprecated tag in Javadoc comment above declaration
supplemental: @Deprecated(since = "version", forRemoval = true)
replacement_hint: Look for @see or "Use X instead" in Javadoc
categories: class, interface, method, field, constructor
code_fence: java
doc_link_formats: {@link ClassName#method} (Javadoc)
```

### TypeScript / JavaScript (.ts, .tsx, .js, .jsx)
```
extensions: .ts, .tsx, .js, .jsx
deprecation_pattern: @deprecated (JSDoc/TSDoc tag)
message_format: /** @deprecated Use X instead */
replacement_hint: Text following @deprecated tag
categories: class, function, method, property, type, interface
code_fence: typescript, ts, javascript, js
doc_link_formats: {@link ClassName} (TSDoc), [text](url) (markdown)
```

### Python (.py)
```
extensions: .py
deprecation_pattern: warnings.warn(*, DeprecationWarning) OR @deprecated decorator
message_format: First argument to warnings.warn() or decorator message
replacement_hint: Look for "Use X instead" in warning message
categories: class, function, method, property, module
code_fence: python, py
doc_link_formats: :class:`Name`, :func:`Name`, :meth:`Name` (Sphinx), [text](url)
```

### Go (.go)
```
extensions: .go
deprecation_pattern: // Deprecated: (godoc convention)
message_format: Text following "// Deprecated:" comment
replacement_hint: Look for "Use X instead" in comment
categories: function, type, method, variable, constant
code_fence: go, golang
doc_link_formats: [Name] (godoc linking)
```

### Rust (.rs)
```
extensions: .rs
deprecation_pattern: #[deprecated(
message_format: #[deprecated(since = "version", note = "message")]
replacement_hint: Text in note field
categories: struct, enum, trait, function, method, type, module
code_fence: rust, rs
doc_link_formats: [`Name`](path) (rustdoc intra-doc links)
```

### Multi-Language Projects
When multiple languages are detected, the skill:
1. Uses the dominant language (most source files) as primary
2. Runs deprecation scans for all detected languages
3. Matches code blocks to the correct language profile by fence tag
4. Reports findings grouped by language

---

## Agent Types

### 0. DiscoveryAgent (NEW — runs first)
**Purpose:** Auto-detect project structure, language, and documentation layout.
**Model:** Haiku
**Mindset:** Investigator. Fast, thorough, no assumptions.
**Input:** Repository root
**Output:** Project configuration:
```
{
  language: "csharp",
  source_dirs: ["Yubico.YubiKey/src/", "Yubico.Core/src/"],
  docs_dir: "docs/",
  exclude_docs: ["whats-new.md"],
  exclude_source: ["*Tests*", "*examples*"],
  deprecation_profile: <selected language profile>,
  doc_link_format: "docfx-xref",
  security_guidelines: "docs/.../sensitive-data.md" | null,
  code_fence_languages: ["csharp"]
}
```
**Method:**
1. **Language detection:**
   - Glob for source files by extension: `**/*.cs`, `**/*.java`, `**/*.ts`, `**/*.py`, `**/*.go`, `**/*.rs`
   - Count files per extension (exclude `node_modules/`, `vendor/`, `bin/`, `obj/`, `.git/`)
   - Select dominant language; note secondaries if >10% of total
2. **Directory detection:**
   - Docs: Try `docs/`, `doc/`, `documentation/`, `manual/`, `guide/` — first match wins
   - If none: find directories with >5 `.md` files clustered together
   - Source: Try `src/`, `lib/`, `source/`, or language-specific patterns (`**/*.csproj` parent dirs)
   - Respect `.gitignore`
3. **Changelog detection:**
   - Find files matching: `*changelog*`, `*whats-new*`, `*release-notes*`, `*history*` (case-insensitive)
   - Add to exclude list (historical records, not instructional)
4. **Doc link format detection:**
   - Grep docs for `xref:` → DocFX
   - Grep for `{@link` → Javadoc/TSDoc
   - Grep for `:class:` or `:func:` → Sphinx
   - Grep for intra-doc `[`Name`]` → Rustdoc
   - Multiple formats possible in one project
5. **Security guidelines discovery:**
   - Search docs for files matching: `*secur*`, `*sensitive*`, `*credential*`, `*secret*`, `*handling*data*`
   - Read candidate files, check if they contain security practices/guidelines
   - If found → use as Q8 baseline
   - If not found → skip Q8, note in report
6. **Config file check:**
   - Look for `.docsaudit.yaml` in repo root
   - If found → load and use (skip auto-detection)
   - If not found → proceed with auto-detection (suggest saving after run)

### 1. DeprecationScanner (formerly ObsoleteScanner)
**Purpose:** Build the deprecation map — all deprecated items in source code.
**Model:** Haiku
**Mindset:** Mechanical collector. No judgment, just extraction.
**Input:** Source directories + language profile from DiscoveryAgent
**Output:** Structured list of deprecated items:
```
{type, name, file, line, deprecationMessage, replacementHint, language}
```
**Method:**
1. Grep for the language profile's `deprecation_pattern` across source files
2. For each match, extract: identifier name, deprecation message, replacement hint
3. Categorize using the language profile's category list
4. Deduplicate and sort by namespace/module

### 2. DocReferenceScanner
**Purpose:** Find all references to code entities in documentation.
**Model:** Haiku
**Mindset:** Pattern matcher. Extracts code references from markdown.
**Input:** Docs directory + code fence languages from DiscoveryAgent
**Output:** Structured list of doc references:
```
{docFile, line, referenceType (codeBlock|prose|xref), entityName, language, context}
```
**Method:**
1. Parse markdown files for fenced code blocks matching detected languages
2. Extract class/function names, method calls, property accesses from code blocks
3. Extract type names from prose (backtick-wrapped identifiers)
4. Extract doc links using the detected doc link format
5. Tag each reference with its surrounding context

### 3. CrossReferencer
**Purpose:** Match doc references against the deprecation map to produce T1-T6 findings.
**Model:** Sonnet
**Mindset:** Analytical comparator. Matches two datasets and classifies discrepancies.
**Input:** DeprecationScanner output + DocReferenceScanner output
**Output:** T1-T6 findings in standard format (see ErrorTaxonomy.md)
**Method:**
1. For each doc reference, check if the entity appears in the deprecation map
2. Classify the finding type (T1-T6) based on reference type and deprecated item category
3. Look up the replacement from the deprecation message
4. Generate suggested fix using the replacement type/method
5. Verify the replacement exists in source (grep for it)

### 4. SignatureVerifier
**Purpose:** Check that code examples use correct API signatures (beyond deprecation checks).
**Model:** Sonnet
**Mindset:** Compiler proxy. Validates that code examples would compile/run.
**Input:** Code blocks from docs + source API signatures
**Output:** Q1 findings (non-compiling examples)
**Method:**
1. For each code block, extract method/function calls with their argument types
2. Look up the actual signature in source
3. Check parameter count, types, and return type alignment
4. Flag mismatches as Q1

### 5. ProseReviewer
**Purpose:** Review documentation quality from three audience perspectives.
**Model:** Opus
**Mindset:** Three-lens reviewer (see Audiences below). Contextual judgment required.
**Input:** Documentation files + related source code
**Output:** Q2-Q7 findings
**Method:**
1. Read each doc through Library Developer lens → Q1, Q2, Q5 findings
2. Read each doc through Library User lens → Q3, Q4, Q6 findings
3. Read each doc through Technical Writer lens → Q6, Q7 findings
4. Deduplicate across lenses

### 6. SecurityReviewer
**Purpose:** Check code examples against project security guidelines.
**Model:** Opus
**Mindset:** Security auditor. Applies discovered or universal security rules to code examples.
**Input:** Code blocks from docs + SecurityPatterns.md checklist + discovered security guidelines (if any)
**Output:** Q8 findings (with SP sub-classification)
**Method:**
1. If DiscoveryAgent found a security guidelines doc → read it and derive project-specific anti-patterns
2. Always apply universal SP1-SP3 checks (string storage of secrets, missing cleanup, missing try/finally)
3. Apply language-specific patterns (e.g., Python `getpass` usage, Java `char[]` for passwords)
4. Apply judgment notes (see SecurityPatterns.md) to filter noise
5. Generate findings with guideline references

---

## Audiences

Each quality review considers three perspectives:

### Library Developer
**Who:** Engineer building features on top of the library/SDK.
**Cares about:** API correctness, compile-time validity, version compatibility.
**Finds:** T1-T6, Q1, Q2, Q5 — "Does this code actually work?"

### Library User
**Who:** Developer following documentation to integrate the library into their app.
**Cares about:** Completeness, prerequisites, clarity.
**Finds:** Q3, Q4, Q6 — "Can I follow this without prior knowledge?"

### Technical Writer
**Who:** Documentation maintainer ensuring consistency and navigability.
**Cares about:** Terminology consistency, link integrity, structural coherence.
**Finds:** Q6, Q7 — "Is this consistent with the rest of the docs?"

---

## Orchestration Patterns

### Audit Workflow (Correctness)
```
DiscoveryAgent (Haiku) ──→ DeprecationScanner (Haiku) ──┐
                       ──→ DocReferenceScanner (Haiku) ──┤
                                                         ├──→ CrossReferencer (Sonnet) ──→ Findings
```
Discovery first, then parallel scan, then join for cross-referencing.

### Review Workflow (Quality)
```
DiscoveryAgent (Haiku) ──→ SignatureVerifier (Sonnet) ──┐
                       ──→ ProseReviewer (Opus)    ──────┼──→ Deduplicate ──→ Findings
                       ──→ SecurityReviewer (Opus)  ────┘
```
Discovery first, then all three reviewers in parallel.

### Full Audit
```
DiscoveryAgent (Haiku) ──→ Audit Workflow ──┐
                       ──→ Review Workflow ──┤
                                             ├──→ Report Workflow (merge + format)
```
Single discovery shared across both workflows.

---

## Agent Launch Guidelines

1. **Discovery runs once per invocation.** Share its output across all subsequent agents.
2. **Always scope agents narrowly.** Pass specific file lists from discovery, not "scan everything."
3. **Haiku agents get explicit instructions.** They follow patterns well but don't improvise.
4. **Opus agents get context + judgment latitude.** They decide what matters.
5. **Cross-referencing requires Sonnet minimum.** Matching two datasets needs reasoning.
6. **Parallelize independent agents.** DeprecationScanner and DocReferenceScanner have no dependency.
7. **Sequential where dependent.** CrossReferencer must wait for both scanners.

---

## Criteria-Driven Execution

Every workflow in DocsAudit follows a **criteria-first** pattern:

### Before Work: Define Success Criteria
Each workflow defines binary-testable criteria (true/false, no ambiguity). These describe the **end state**, not the steps to get there. Examples:
- "Every finding includes source-code citation proving deprecated status" (not "scan for deprecated items")
- "Zero false positives remain after verification" (not "verify findings")

### During Work: Execute Against Criteria
Agents execute their phases knowing what success looks like. This prevents scope creep and ensures completeness.

### After Work: Verify Every Criterion
Walk through each criterion mechanically:
1. Read the criterion statement
2. Check the output against it (grep, count, spot-check)
3. Mark verified or failed
4. If failed → loop back to the relevant phase, don't ship partial results

### Why This Matters
- **Reproducibility:** Different people running the same workflow get consistent results
- **No silent failures:** A criterion that can't be verified exposes a gap in the workflow
- **Self-improving:** If a criterion repeatedly fails, the workflow phase needs refinement

Each workflow file (Audit.md, Review.md, Report.md) contains its own criteria table and verification protocol.
