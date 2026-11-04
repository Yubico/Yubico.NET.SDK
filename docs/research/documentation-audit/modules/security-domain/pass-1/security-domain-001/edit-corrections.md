# Security Domain — bounded edit corrections

2026-11-04; editor `github-copilot/gpt-6.1-sol`. Follow-up to the original edit report, which remains
unchanged. Read complete `review.md`, `evidence/review-sources.md` and `corrections.md`, the affected README
sections/interface contracts and the full certificate-store test semantic unit. Loaded `review-receive`.
No additional research or Core reading was performed. Independent follow-up acceptance remains pending.

## Correction mapping

| ID | Exact authored location | Correction |
| --- | --- | --- |
| SD-R1 | `src/SecurityDomain/README.md`, allowlist paragraph; `src/SecurityDomain/src/ISecurityDomainSession.cs`, StoreAllowListAsync XML | Changed the certificate-chain serial subsection from 7.5.4 to 7.5.1. Preserved draft version, Public Review status, August 2023 date and final/profile limitations. |
| SD-R2 | `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11Tests.cs`, Scp11b_StoreCertificates_CanBeRetrieved comment | Now says “Compare certificate thumbprints, not chain validity or association with the device key.” No byte-identity claim; setup, assertions and cleanup unchanged. |
| SD-QA-001 | `src/SecurityDomain/tests/README.md`, WithSecurityDomainSessionAsync paragraph | Replaced the CLAUDE link with human-facing `../../../docs/TESTING.md`. Kept argument order, optional parameters, caller-owned disposal scope and practical successive-session `resetBeforeUse: false` guidance local. No code example changed. |
| SD-COORD-001 | `src/SecurityDomain/README.md`, protocol introduction; `src/SecurityDomain/src/ISecurityDomainSession.cs`, interface type remarks | Replaced generic interchangeable SCP03/SCP11a/c administration wording with SCP03 as the examples' administrative route and command-specific SCP11 permissions. Explicitly identifies the August 2023 SCP11 v1.3.0.13 Public Review draft §4.6 prohibition of PUT KEY, DELETE [keys] and SET STATUS over SCP11c. Does not promote that draft restriction to final-specification or YubiKey-profile behavior. Retains SCP11b device-only authentication and no blanket authority from channel establishment. |

## Exact before/after hashes

Before values were measured immediately before this follow-up and agree with the original edit-report
manifest. After values were measured after the four bounded edits. No other product path changed during
this follow-up. The only new record is this `edit-corrections.md`; it has no self-referential hash.

| Path | Before SHA-256 | After SHA-256 |
| --- | --- | --- |
| `src/SecurityDomain/README.md` | `0a290632f4eb8db0eb6e051e69ad555e567009f8df4b888d93cefd922f9508f5` | `f211ae74ac5bf831ae9e566a82511c1fcb48a6277d319ad87f9a2419168410da` |
| `src/SecurityDomain/src/ISecurityDomainSession.cs` | `7862421d402c5bbeaefcaa523b5f65aa62968f69545cb5b716c4498a06009861` | `c9daab35f6388b8a711bcbe2df2881056a09c4babdcd8b18b294aa80241425d5` |
| `src/SecurityDomain/tests/README.md` | `658b60cae8657c4e0fe2d44fe5cbc7c174b606fde246b97bb4ad0c2ecd796394` | `83b15cd245f2eefafb833af830b25cbeed0f616b8845d81345e170541ed1c1f8` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11Tests.cs` | `f4b5cf41174b82a25d6b1c8e3a23a00cd02cd3062fb4a196506d6ebbb3a1d285` | `3bbbb90e48eea81924f49d539fa235af5d4ea3508cbd0d5a7bc0025b258f22a4` |

## Read-only checks

Python 3 through `functions.shell` completed with exit 0:

- Measured all four before/after SHA-256 values; all four retain LF line endings.
- Recomputed hashes for the other **22 original authored product paths** against the original edit-report:
  all unchanged. No shared-document follow-through was needed for these bounded corrections.
- Original `edit-report.md` hash remains
  `f2eec6b0ef8cb99e202ca3792a340cc1751d289fb12d811630c49359dea8f0be`.
- Compared both affected C# files against `git show HEAD:<path>` with the same comment/whitespace-excluding
  lexer retaining string/character literals: non-comment sequences equal. No executable source/assertion
  change; this is a read-only lexical check, not a Roslyn/build gate.
- Measured the five C# fences in module README and one in test README before/after: all **six block hashes
  unchanged**. Other example-bearing product paths are among the 22 unchanged paths. The original example
  classifications/prerequisites therefore remain applicable. The coordinator reports ten meaningful
  example groups compiled without warnings in two contexts; this editor did not rerun or independently
  verify that compilation, and these changes introduce no new code example.
- Verified both citation locations contain 7.5.1 rather than 7.5.4 and both authorization passages include
  command-specific permissions and the SET STATUS restriction.
- Verified test README has no agent-document link and its human testing link resolves by path existence.
  No linked Core/other-module contents or remote links were opened.

No formatter, build, tests, example-host execution, hardware, runtime/project/snapshot edits, commits,
delegation or coordinator-record edits. Original reports and citation evidence remain preserved; their
superseding corrections are coordinator-owned. No claim of final-GP conformance, device qualification,
runtime enforcement or campaign closeout follows from these edits.
