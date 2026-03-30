---
name: DocsAudit
description: Documentation consistency and correctness auditing for any codebase. USE WHEN docs audit, docs consistency, obsolete check, documentation scan, check docs for obsolete code, verify documentation accuracy, find stale code references in docs.
---

# DocsAudit

Scan documentation for correctness issues (deprecated code references, wrong API signatures, broken links) and quality issues (security anti-patterns, missing context, inconsistent terminology). Works with any language and documentation toolchain. Two modes: **Audit** (mechanical, deterministic) and **Review** (contextual, suggests improvements).

## Zero-Config Design

DocsAudit auto-detects everything it needs from the repository:

- **Language** — determined by counting source file extensions
- **Source/docs directories** — found by common naming patterns
- **Deprecation markers** — selected from built-in language profiles
- **Doc link format** — inferred from link syntax found in docs
- **Security guidelines** — discovered by searching for security/sensitive-data docs

No configuration file required. After the first run, the skill can suggest saving a `.docsaudit.yaml` to speed up future runs — but it's optional.

## Workflow Routing

| Workflow | Trigger | File |
|----------|---------|------|
| **Audit** | "audit docs", "check for obsolete", "scan docs" | `Workflows/Audit.md` |
| **Review** | "review docs quality", "improve docs", "docs quality" | `Workflows/Review.md` |
| **Report** | "generate docs report", "show findings" | `Workflows/Report.md` |

## Examples

**Example 1: Scan for deprecated code in documentation**
```
User: "Audit the docs for obsolete code references"
-> Auto-detects: C# project, docs/ directory, [Obsolete] attributes
-> Scans source for deprecation markers, builds obsolete map
-> Cross-references against docs code examples and prose
-> Reports findings categorized by T1-T6 taxonomy
```

**Example 2: Review documentation quality**
```
User: "Review the PIV docs for quality issues"
-> Invokes Review workflow
-> Reads docs through SDK Developer, SDK User, and Technical Writer lenses
-> Discovers and checks code examples against security guidelines
-> Reports suggestions categorized by Q1-Q8 taxonomy
```

**Example 3: Full audit with report**
```
User: "Do a full docs audit and generate a report"
-> Invokes Audit workflow, then Report workflow
-> Produces categorized findings with file paths, line numbers, and suggested fixes
-> Suggests saving .docsaudit.yaml for future runs
```

## Quick Reference

- **Error taxonomy:** See `ErrorTaxonomy.md` — T1-T6 correctness + Q1-Q8 quality categories
- **Security patterns:** See `SecurityPatterns.md` — SP1-SP6 anti-patterns (adapts to project's own guidelines)
- **Agent design:** See `AgentDesign.md` — agent types, model tiers, language profiles, orchestration
- **Findings schema:** See `FindingsSchema.md` — structured JSON output format for deterministic results
- **Report template:** See `ReportTemplate.md` — fixed markdown template, no freestyle formatting
- **Audiences:** SDK Developer (correctness), SDK User (usability), Technical Writer (consistency)
- **Modes:** Audit (facts/findings) vs Review (suggestions/opinions)

## How It Works

Each workflow follows a **criteria-driven** approach:

1. **Discover** the project structure, language, and documentation layout automatically.
2. **Define success criteria** before scanning — what does "done" look like? Each criterion is a binary-testable statement (true/false).
3. **Execute** the scan using specialized agents at appropriate model tiers (Haiku for bulk, Sonnet for analysis, Opus for judgment).
4. **Verify** every criterion mechanically after execution — no finding is reported without source-code evidence, no criterion is marked complete without verification.

This ensures reproducible, auditable results regardless of who runs the skill or what language the project uses.
