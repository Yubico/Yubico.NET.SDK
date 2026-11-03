# OATH-001 investigation dispatch

- Coordinator: GPT-6 Astra.
- Investigator: `github-copilot/gpt-6.1-sol` (resolved from the available model catalog; default variant).
- Investigator session: `ses_e629b0717ffedUbklz42fAb2hw`.
- Repository: `/home/rupicola/Code/y/Yubico.Yubikit.NET`.
- Branch: `yubikit-docs-audit`.
- Baseline: `ddf3cdc24984f598678b48f2a0f98d6be1a58bf9`, equal to local `origin/yubikit` at dispatch.
- Checkpoint: investigation findings and unresolved questions, before documentation edits.
- Records: this directory, outside the source repository.

## Task packet sent

Read root `AGENTS.md` and `CLAUDE.md`, OATH's module guidance, nested test guidance and its required testing document before test inspection. Read the approved `../../style.md`; do not load the full plan or role catalog.

Investigate authentication/password documentation in:

- `IOathSession.cs`: lock/password state; salt/device identity as needed; derive, validate, set/unset key, authenticate-and-retry.
- Corresponding implementation and comments in `OathSession.cs`.
- `Credentials/OathCredentialOptions.cs`.
- Module README, package README, and agent guidance: authentication/password/security material.
- OathTool `Commands/AccessCommand.cs` and `Cli/SessionHelper.cs`.

Use relevant authentication/password-change/session tests, exception definitions, backend/Core dependencies, and official YKOATH documentation as evidence. Discover external active/migration claims as necessary and disclose scope expansion.

Read every assigned semantic unit, including text without keyword hits. Separate documentation errors, implementation concerns, intended-contract ambiguity, and insufficient evidence. Record precise source locations and specification revision/section. Do not normalize implementation bugs by changing documentation to match them. Preserve valid device/firmware requirements despite the absence of an API backward-compatibility objective.

Use local read/search and read-only Git, official specification search/fetch, and Context7 for library/API questions. Do not modify files, run builds/tests/generators, switch branches, commit, or recursively delegate. Return findings in the response for the coordinator to persist.

Effort bound: initial focused investigation plus one targeted follow-up for unresolved claims; escalate consequential ambiguity. Prefer a concise set of distinct root causes without a finding-count quota.

Output: numbered findings with claim, severity/category, evidence, conclusion, disposition, confidence; coverage and clean claims; escalation packets; external sources and verification limits. No runtime, hardware, cost, or full-module-completion claims.

## Prospective remaining OATH units

These are coverage destinations, not completed or concurrently dispatched tasks. File/section ownership must be refined before each assignment.

| Unit | Scope |
| --- | --- |
| OATH-001 | Authentication, password state, access keys, retries, matching examples/tests |
| OATH-002 | Credential creation, URI parsing, identity, algorithms, secret processing and ownership |
| OATH-003 | Credential CRUD, firmware gates, device binding, reset |
| OATH-004 | Calculation, time/counter behavior, validity, bulk results, touch notifications |
| OATH-005 | Session creation, transport/connection ownership, backend protocol, errors and cancellation |
| OATH-006 | Example/test commentary and guidance not covered by the preceding units |
| OATH-007 | External active/migration/package claims, cross-module consistency and editorial consolidation |

The machine-readable coverage manifest lists all 38 tracked OATH Markdown/C# surfaces and 23 external reference files. File-level semantic status stays pending or partial until all relevant sections are reviewed; completion of OATH-001 alone cannot close a shared file.
