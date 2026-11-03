# OATH pilot coverage index

## Scope and evidence boundaries

**Current cycle status:** The OATH pilot and its explicitly approved implementation follow-up are complete. The
original inventory below is baseline coverage, not a count of today's expanded tree. New and changed wave-one/
wave-two material received implementation review and targeted documentation-impact re-review; see the run records.
The subsequent WebAuthn public-first/comment-inclusive cycle is complete and pushed at `9399b94b`.
Its 131 module files and 30 external-reference files are recorded in
[runs/webauthn-001/closeout.md](runs/webauthn-001/closeout.md); the counts below remain the historical OATH
inventory. Core and its eventual scope are deferred. See [webauthn-plan.md](webauthn-plan.md).

The baseline inventory contains **38 tracked OATH Markdown/C# surfaces** (four Markdown, 34 C#), **23 external reference files**, and eight supporting project/API-snapshot/runner files. Build outputs, untracked user research, and non-OATH claims elsewhere are excluded. The canonical active-document list is consumed from the existing toolchain; source comments and module package README are supplemental scope.

All 38 local surfaces have now received semantic reading through the four investigations below. All 23 externally indexed files received OATH-specific contextual review. This is coverage of the planned source/documentation surfaces, not proof every claim is correct, every standard requirement was tested, or runtime defects have been resolved.

## Local coverage ownership

| Surfaces | Investigations |
| --- | --- |
| OATH README and CLAUDE | 001 authentication; 002 creation; 003 operations; 004 setup/consistency |
| OATH PACKAGE_README and tests/CLAUDE | 004, with authentication spot checks in001 |
| OathSession.cs | 001 authentication/state; 002 PUT/creation; 003 stored operations/calculation/reset; 004 setup/disposal |
| IOathSession.cs | 001 authentication; 002 PUT; 003 operation contracts |
| IYubiKeyExtensions.cs | 003 operation wrappers; 004 session creation |
| Backend/IOathBackend.cs and OathBackend.cs | 004; dependencies in001/003 |
| Credential.cs, CredentialData.cs, HashAlgorithm.cs, OathType.cs | 002 |
| OathConstants.cs | 002 creation/constants; 003/004 operation/setup values |
| Credentials/OathCredentialOptions.cs; OathException.cs | 001 |
| Code.cs | 003 |
| OathTool Program.cs, InfoCommand.cs, both device selectors | 004 |
| OathTool AccessCommand.cs and Cli/SessionHelper.cs | 001 |
| OathTool AccountsCommand.cs | 002 creation/decoder; 003 stored operations; Program/dispatch dependencies004 |
| OathTool ResetCommand.cs and OutputHelpers.cs | 003 operations; 004 help path and whole-tool output qualifications |
| Unit CodeTests.cs | 003 |
| Unit CredentialDataTests.cs, CredentialTests.cs, CredentialIdTests.cs | 002 |
| Unit OathAuthenticationTests.cs | 001 |
| Unit OathSessionTests.cs | 001 authentication; 002 PUT; 003 calculations; 004 setup/ownership/fixtures |
| Unit AuditV2/OathAuditReproTests.cs | 001 password ownership; 002 identities; 003 operation handling |
| Integration OathSessionTests.cs | 001 authentication; 002 creation; 003 operations; 004 scaffolding |
| Integration OathPasswordChangeTests.cs | 001 |
| Integration OathHashAlgorithmTests.cs | 001 authentication; 002 creation evidence; 003 calculations; 004 scaffolding |
| Integration AuditV2/OathAuditReproTests.cs | 002/003 |
| Integration TestExtensions/OathTestStateExtensions.cs | 003 reset evidence; 004 complete helper |

License headers and closing syntax do not require a separate semantic unit. Complete-file reads are not independent certification of every contained behavior. Detailed ranges and claims are in each investigation's coverage record.

## External coverage

See [OATH-004 external coverage](runs/oath-004/coverage.md) for all 23 paths/ranges/dispositions. Historical reports were checked for explicit snapshot status and active misuse; they were not rewritten as present-day product documentation. The lexical discovery boundary remains a limitation: future changed symbols or documentation can expose new references and should invalidate affected coverage.

## Reviews and unresolved work

- OATH-001 and002 accepted edits passed fresh Sol review and repository validation. Astra sampling added three corrections in002; later packets explicitly check table associations and sibling-operation exceptions.
- OATH-003/004 accepted edits passed independent review and final validation, including architecture checks. Deferred parts of findings remain open.
- Implementation and intended-contract questions are tracked in [implementation-followups.md](implementation-followups.md). Several still affect release documentation and need another focused review after resolution.
- The original documentation-only checkpoint did not run hardware or consumer-package installation checks.
  Subsequent OATH follow-up runs did exercise hardware, with limits recorded in the wave-one and wave-two
  completion records. Final combined results were3,998passing unit tests and32passing integration cases on125
  (19live,13offline), with explicit skips and no live SCP reset proof. Consumer-package installation remains unverified.

## Subsequent upstream reconciliation

Upstream `089b2393` was integrated after the documentation checkpoint. [The delta review](runs/upstream-001/reconciliation.md) revisited secret ownership/processing, parser cleanup, changed unit/integration source, and the newly added `CredentialOrderingTests.cs`. Old aliasing and natural-display-order claims were invalidated and updated. The original 38-file inventory and hashes remain baseline evidence; they are not current-tree fingerprints after this import. The existing Base32-shape follow-up now explicitly includes the third decoder in Cli.Commands. No blanket re-certification of unrelated incoming code is claimed.
