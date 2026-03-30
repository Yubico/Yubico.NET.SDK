---
name: Report
description: Findings report generation workflow — merges Audit and Review results into a structured, deterministic report.
---

# Report Workflow

Combines Audit (T1-T6) and Review (Q1-Q8) findings into a single report using a fixed template. All agent outputs follow the structured schema in `FindingsSchema.md`. The report is rendered from `ReportTemplate.md` — no freestyle formatting.

## Success Criteria

| # | Criterion | Verified |
|---|-----------|----------|
| SC-1 | All findings from Audit and Review workflows are included (none dropped) | ☐ |
| SC-2 | Every finding has severity assigned per FindingsSchema.md severity rules (not judgment) | ☐ |
| SC-3 | Systemic issues (3+ files with same entity) are called out separately | ☐ |
| SC-4 | Remediation plan follows fixed priority order from ReportTemplate.md | ☐ |
| SC-5 | Report output exactly matches ReportTemplate.md structure (no added/removed sections) | ☐ |
| SC-6 | Config suggestion was presented to user (if no .docsaudit.yaml exists) | ☐ |

## Prerequisites

Load on demand:
- `FindingsSchema.md` — structured output schema for all agents
- `ReportTemplate.md` — fixed markdown template for rendering
- `ErrorTaxonomy.md` — for category descriptions and severity reference

## Algorithm (4 Phases)

### Phase 1: Collect and Validate Findings

**Model:** Haiku (mechanical aggregation)

1. Collect agent output envelopes from Audit and Review workflows
2. Validate each finding object against FindingsSchema.md:
   - Required fields present (`id`, `file`, `line`, `summary`, `severity`, `evidence`, `suggested_fix`)
   - Severity matches the rules table in FindingsSchema.md for that category
   - If severity doesn't match → override to the correct value (log the override)
3. Merge all findings into a single array
4. Deduplicate: same `file` + `line` with overlapping `id` → keep the most specific

### Phase 2: Analyze and Structure

**Model:** Sonnet

1. **Sort findings** by file path, then by line number within each file
2. **Group by file** for the "Findings by File" section
3. **Identify systemic issues:** Any entity (`evidence` text or `replacement.type`) appearing in 3+ findings across different files → extract into systemic issues array with:
   - `name`: the repeated entity
   - `file_count`: number of affected files
   - `files`: list of file paths
   - `description`: what the pattern is
   - `fix_strategy`: single approach to fix all instances
4. **Build remediation plan** using the fixed priority order from ReportTemplate.md:
   - Count findings per category
   - Calculate effort per ReportTemplate.md effort estimates
   - Calculate total estimated hours
5. **Build merged output** per FindingsSchema.md → Merged Output Schema

### Phase 3: Render Report

**Model:** Haiku (mechanical template fill)

1. Load `ReportTemplate.md`
2. Fill template placeholders with data from the merged output
3. **Do not add any content not in the template.** No "Observations", no "What worked", no "Summary" prose. The template is the complete report.
4. Write to `docs-audit-report-[DATE].md` in project root

### Phase 4: Config Suggestion (MANDATORY)

**This phase is not optional. It must execute after every report generation.**

1. Check if `.docsaudit.yaml` exists in repo root
2. If it exists → skip, add `"config_saved": true` to report metadata
3. If it does NOT exist → **present the detected configuration to the user**:

```
┌─────────────────────────────────────────────┐
│ DocsAudit — Save Configuration?             │
├─────────────────────────────────────────────┤
│ Language:    C# (847 .cs files)             │
│ Source:      Yubico.YubiKey/src/,            │
│              Yubico.Core/src/               │
│ Docs:        docs/                          │
│ Excluded:    whats-new.md (changelog)       │
│ Security:    docs/.../sensitive-data.md     │
│ Doc links:   DocFX xref                     │
│                                             │
│ Save as .docsaudit.yaml?                    │
│ This speeds up future runs by skipping      │
│ auto-detection. Delete the file to          │
│ re-detect.                                  │
└─────────────────────────────────────────────┘
```

4. If user confirms → write `.docsaudit.yaml`:

```yaml
# DocsAudit configuration
# Auto-generated on [DATE]
# Delete this file to re-run auto-detection

language: {{language}}
source_dirs:
{{#each source_dirs}}
  - {{this}}
{{/each}}
docs_dir: {{docs_dir}}
exclude_docs:
{{#each exclude_docs}}
  - {{this}}
{{/each}}
exclude_source:
  - "*Tests*"
  - "*Test*"
  - "*examples*"
  - "*sample*"
security_guidelines: {{security_guidelines}}
```

5. If user declines → note in output: "Config not saved. Will auto-detect on next run."

---

## Invocation

```
User: "Generate docs report"
User: "Show findings"
User: "Do a full docs audit and generate a report"
```

For "full audit + report": chain Audit → Review → Report workflows.

---

## Output Options

- **File:** Always save to `docs-audit-report-[DATE].md` in project root
- **Terminal:** Also print a summary table to stdout (the Executive Summary section only)
- **Structured:** If user requests, also output the merged JSON to `docs-audit-report-[DATE].json`

---

## Verification Protocol

1. **SC-1:** Count findings in merged output. Compare against sum of all agent `findings.length`. If mismatch, identify which findings were dropped and why.
2. **SC-2:** For every finding, check `severity` against the FindingsSchema.md severity rules table. The category determines severity — no judgment involved. If any finding has wrong severity, it's a bug.
3. **SC-3:** Scan findings for any `evidence` text or `replacement.type` appearing in 3+ different files. If found and NOT in Systemic Issues section → fail.
4. **SC-4:** Verify remediation plan rows are in the exact order specified in ReportTemplate.md. T5 first, Q3-Q7 last.
5. **SC-5:** Compare output file sections against ReportTemplate.md. Every section in template must be in output. No extra sections allowed.
6. **SC-6:** Check Phase 4 executed. If no `.docsaudit.yaml` existed before the run, the config suggestion MUST have been presented. Check output for the suggestion box or the "Config not saved" note.

**If any criterion fails:** Fix and re-render. Do not deliver a non-conforming report.
