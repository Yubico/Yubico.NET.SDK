Audit the project documentation for correctness and quality issues.

## What this does

Auto-detects the project language, source directories, documentation layout, and security guidelines. Then scans documentation for deprecated code references, wrong API signatures, broken links, security anti-patterns, and quality issues. Works with any language (C#, Java, TypeScript, Python, Go, Rust). No configuration required.

## Instructions

Read `.claude/skills/DocsAudit/SKILL.md` and follow the workflow routing table to determine which workflow to execute.

**Default behavior (no arguments):** Run the full Audit workflow — auto-detect project structure, then scan for deprecated code references (T1-T6 findings).

**With arguments:**
- `/docs-audit review` — Run the Review workflow (Q1-Q8 quality findings)
- `/docs-audit report` — Run both Audit + Review, then generate a combined report
- `/docs-audit review piv` — Review only a specific subdirectory
- `/docs-audit review security` — Focus only on security anti-pattern checks (Q8)

## Workflow files

- Audit (correctness): `.claude/skills/DocsAudit/Workflows/Audit.md`
- Review (quality): `.claude/skills/DocsAudit/Workflows/Review.md`
- Report (combined): `.claude/skills/DocsAudit/Workflows/Report.md`

## Reference files (load on demand)

- Error taxonomy (T1-T6, Q1-Q8): `.claude/skills/DocsAudit/ErrorTaxonomy.md`
- Security patterns (SP1-SP6, multi-language): `.claude/skills/DocsAudit/SecurityPatterns.md`
- Agent design, language profiles, model selection: `.claude/skills/DocsAudit/AgentDesign.md`

## Optional configuration

If a `.docsaudit.yaml` exists in the repo root, it will be used instead of auto-detection. The skill will offer to create this file after the first run.
