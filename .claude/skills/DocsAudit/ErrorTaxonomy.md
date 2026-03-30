---
name: ErrorTaxonomy
description: Classification system for documentation correctness (T1-T6) and quality (Q1-Q8) issues found during DocsAudit scans. Language-agnostic.
type: reference
---

# Error Taxonomy

Two categories: **Correctness** (T-series, mechanical, verifiable) and **Quality** (Q-series, contextual, judgment-based).

## Correctness Errors (T1-T6)

These are **factual errors** — the documentation contradicts the current codebase. Every T-finding must include a source-code citation proving the inconsistency.

| ID | Name | Description | Detection Method |
|----|------|-------------|-----------------|
| **T1** | Deprecated type in code example | Code example uses a class/type marked as deprecated as if it's current API | Grep deprecation markers in source → cross-reference against doc code blocks |
| **T2** | Deprecated method/function overload in code example | Code example calls an overload/signature marked deprecated when a replacement exists | Check method signatures in source for deprecation on specific overloads |
| **T3** | Deprecated type in prose | Prose references a deprecated type/class name as if it's the current API surface | Grep type names from deprecation map against prose text (outside code blocks) |
| **T4** | Deprecated property/field access in code example | Code accesses properties/fields specific to a deprecated type; replacement has different member names | Compare member names between deprecated and replacement types |
| **T5** | Typo in identifier | Misspelled class/function/method/enum name in code example or prose | Fuzzy-match identifiers in docs against actual names in source |
| **T6** | Deprecated command/class in code example | Code uses a class/command replaced by an updated equivalent | Grep classes for deprecation markers and cross-reference docs |

### Severity

- **T1, T2, T6**: High — code examples won't compile/run or produce warnings
- **T3**: Medium — misleading prose but won't break compilation
- **T4**: High — code examples will fail (wrong member names)
- **T5**: Medium-High — may or may not compile depending on typo location

### Language-Specific Deprecation Markers

| Language | Marker | Example |
|----------|--------|---------|
| C# | `[Obsolete("message")]` | `[Obsolete("Use RSAPublicKey instead")]` |
| Java | `@Deprecated` + `@deprecated` Javadoc | `@Deprecated(since = "2.0")` |
| TypeScript/JS | `@deprecated` JSDoc/TSDoc | `/** @deprecated Use newMethod instead */` |
| Python | `warnings.warn(..., DeprecationWarning)` | `warnings.warn("Use X", DeprecationWarning)` |
| Go | `// Deprecated:` comment | `// Deprecated: Use NewFunc instead.` |
| Rust | `#[deprecated(note = "...")]` | `#[deprecated(since = "1.2", note = "Use X")]` |

---

## Quality Issues (Q1-Q8)

These are **judgment calls** — the documentation is technically not wrong but could mislead, confuse, or harm users. Quality findings include a rationale for why the issue matters.

| ID | Name | Description | Detection Method |
|----|------|-------------|-----------------|
| **Q1** | Non-compiling/non-running code example | Code example has syntax errors, missing imports, or type mismatches (beyond deprecation issues) | Static analysis of code blocks against known API signatures |
| **Q2** | Prose contradicts code example | Explanatory text says one thing, adjacent code does another | Read prose + code pairs and check alignment |
| **Q3** | Missing context | Code example assumes setup/state not shown and not linked | Check if variables/objects used are declared or referenced elsewhere |
| **Q4** | Unclear prerequisites | Document assumes knowledge or setup steps not mentioned | Review from Library User perspective — can a newcomer follow this? |
| **Q5** | Missing version gate | Feature or behavior is version-specific but doc doesn't mention which versions | Check if APIs used are version-gated in source |
| **Q6** | Inconsistent terminology | Same concept called different names across related docs | Compare terminology across docs in the same section |
| **Q7** | Broken or invalid link | Doc link, anchor, or URL that doesn't resolve | Validate link targets exist; check anchor slugs match headings |
| **Q8** | Security anti-pattern | Code example violates project or universal security guidelines | Check against SecurityPatterns.md checklist |

### Severity

- **Q1**: High — broken examples erode trust
- **Q2**: High — actively misleading
- **Q3, Q4**: Medium — frustrating but recoverable
- **Q5**: Medium — version-specific bugs are hard to diagnose
- **Q6**: Low — cosmetic but accumulates
- **Q7**: Medium — broken navigation
- **Q8**: High — security issues in official examples are dangerous

---

## Finding Format

Each finding should be reported as:

```
[ID] File:Line — Summary
  Evidence: <what the doc says/shows>
  Source: <what the code actually is> (with file:line citation)
  Suggested fix: <specific replacement text>
```

Example (C#):
```
[T1] cert-request.md:95 — Uses deprecated PivRsaPublicKey in code example
  Evidence: `PivRsaPublicKey rsaPublic = pivSession.GenerateKeyPair(...)`
  Source: PivRsaPublicKey marked [Obsolete] at Cryptography/PivRsaPublicKey.cs:12
         Replacement: RSAPublicKey (Cryptography/RSAPublicKey.cs)
  Suggested fix: `var rsaPublic = (RSAPublicKey)pivSession.GenerateKeyPair(...)`
```

Example (Python):
```
[T1] auth.md:42 — Uses deprecated authenticate() function in code example
  Evidence: `client.authenticate(username, password)`
  Source: authenticate() has DeprecationWarning at auth/client.py:88
         Replacement: login() (auth/client.py:95)
  Suggested fix: `client.login(username, password)`
```

Example (Java):
```
[T2] encryption.md:67 — Uses deprecated Cipher.getInstance("DES") overload
  Evidence: `Cipher cipher = Cipher.getInstance("DES");`
  Source: DES deprecated in favor of AES
  Suggested fix: `Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");`
```
