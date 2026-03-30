---
name: FindingsSchema
description: Structured JSON schema for agent findings output. Ensures deterministic, machine-parseable results that feed into the fixed report template.
type: reference
---

# Findings Schema

All agents MUST emit findings in this structured format. The Report workflow renders findings into the fixed template (ReportTemplate.md). This separation ensures deterministic output regardless of which model or agent produces the findings.

---

## Discovery Output Schema

The DiscoveryAgent emits this on completion. All subsequent agents receive it as input.

```json
{
  "discovery": {
    "language": "csharp",
    "language_display": "C#",
    "source_dirs": ["Yubico.YubiKey/src/", "Yubico.Core/src/"],
    "docs_dir": "docs/",
    "exclude_docs": ["whats-new.md"],
    "exclude_source": ["*Tests*", "*examples*"],
    "deprecation_pattern": "\\[Obsolete\\(",
    "doc_link_format": "docfx-xref",
    "security_guidelines": "docs/users-manual/sdk-programming-guide/sensitive-data.md",
    "code_fence_languages": ["csharp", "cs"],
    "config_source": "auto-detected",
    "timestamp": "2026-03-30T14:22:00Z"
  }
}
```

Fields:
- `config_source`: `"auto-detected"` or `"docsaudit.yaml"` — tracks whether config was discovered or loaded
- `security_guidelines`: path or `null` if none found

---

## Finding Object Schema

Every individual finding — from any agent — uses this shape:

```json
{
  "id": "T1",
  "file": "docs/users-manual/application-piv/cert-request.md",
  "line": 95,
  "summary": "Uses deprecated PivRsaPublicKey in code example",
  "severity": "critical",
  "evidence": "PivRsaPublicKey rsaPublic = pivSession.GenerateKeyPair(...)",
  "source": {
    "file": "Yubico.YubiKey/src/Cryptography/PivRsaPublicKey.cs",
    "line": 12,
    "detail": "PivRsaPublicKey marked [Obsolete]"
  },
  "replacement": {
    "type": "RSAPublicKey",
    "file": "Yubico.YubiKey/src/Cryptography/RSAPublicKey.cs",
    "verified": true
  },
  "suggested_fix": "var rsaPublic = (RSAPublicKey)pivSession.GenerateKeyPair(...)",
  "sub_id": null,
  "guideline": null,
  "audience": "developer"
}
```

### Required Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Category code: T1-T6 or Q1-Q8 |
| `file` | string | Doc file path (relative to repo root) |
| `line` | number | Line number in doc file |
| `summary` | string | One-line description of the issue |
| `severity` | enum | `"critical"` \| `"high"` \| `"medium"` \| `"low"` |
| `evidence` | string | What the doc shows (quoted text or code) |
| `suggested_fix` | string | Specific replacement text |

### Optional Fields

| Field | Type | Description | When Used |
|-------|------|-------------|-----------|
| `source` | object | Source code citation proving the issue | T1-T6 (required), Q1 (recommended) |
| `source.file` | string | Source file path | |
| `source.line` | number | Line in source | |
| `source.detail` | string | What the source shows | |
| `replacement` | object | The correct type/method to use | T1-T6 |
| `replacement.type` | string | Replacement identifier | |
| `replacement.file` | string | Where replacement lives in source | |
| `replacement.verified` | boolean | Was the replacement confirmed to exist? | |
| `sub_id` | string | Sub-classification (e.g., "SP2" for Q8) | Q8 only |
| `guideline` | string | Reference to violated guideline | Q8 only |
| `audience` | enum | `"developer"` \| `"user"` \| `"writer"` | Q2-Q7 |

### Severity Rules

Severity is NOT a judgment call — it's determined by the finding category:

| Category | Default Severity | Override Condition |
|----------|-----------------|-------------------|
| T1, T4 | critical | — |
| T2, T6 | high | — |
| T3 | medium | — |
| T5 | high | medium if typo doesn't affect compilation |
| Q1 | critical | high if example is clearly a snippet |
| Q2 | critical | — |
| Q3, Q4 | medium | — |
| Q5 | medium | — |
| Q6 | low | — |
| Q7 | medium | — |
| Q8/SP1, Q8/SP3 | high | medium if surrounding prose mentions cleanup |
| Q8/SP2 | medium | low if example is demonstrating non-security API |
| Q8/SP4-SP6 | low | — |

---

## Agent Output Schema

Each agent wraps its findings in this envelope:

```json
{
  "agent": "DeprecationScanner",
  "model": "haiku",
  "timestamp": "2026-03-30T14:25:00Z",
  "scope": {
    "files_scanned": 847,
    "directories": ["Yubico.YubiKey/src/", "Yubico.Core/src/"]
  },
  "findings": [
    { /* Finding objects */ }
  ],
  "metadata": {
    "deprecation_items_found": 162,
    "doc_references_checked": 1243,
    "false_positives_discarded": 3
  }
}
```

---

## Merged Output Schema

The Report workflow merges all agent outputs into:

```json
{
  "report": {
    "date": "2026-03-30",
    "discovery": { /* Discovery output */ },
    "agents": [
      { /* Agent output envelopes */ }
    ],
    "findings": [
      { /* Deduplicated, sorted findings */ }
    ],
    "summary": {
      "total": 12,
      "by_severity": {"critical": 2, "high": 4, "medium": 5, "low": 1},
      "by_category": {"T1": 0, "T2": 0, "T3": 0, "T4": 0, "T5": 2, "T6": 0, "Q1": 1, "Q2": 1, "Q3": 1, "Q6": 1, "Q8": 6},
      "files_with_findings": 8,
      "systemic_issues": ["OTP key cleanup pattern (4 files)"]
    },
    "config_saved": false
  }
}
```

This merged output feeds directly into ReportTemplate.md for rendering.
