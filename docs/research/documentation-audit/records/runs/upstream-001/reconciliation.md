# Upstream OATH reconciliation

## Revisions

- Audit baseline: `ddf3cdc24984f598678b48f2a0f98d6be1a58bf9`.
- Reviewed documentation checkpoint: `7c4153f8549bcc4b89a304b5ca4f1502cde1dd64`.
- Imported branch: `origin/yubikit` at `089b2393`.
- Merge on the audit branch: `361567eb`.
- Relevant OATH fixes: [#726](https://github.com/Yubico/Yubico.NET.SDK/pull/726), commit `6ca266db` (secret ownership); [#728](https://github.com/Yubico/Yubico.NET.SDK/pull/728), commit `a65af969` (credential ordering).

Sol investigated immutable upstream Git objects while Astra resolved overlapping documentation. A fresh Sol reviewer then checked the resolved tree. No additional implementation fix was made beyond importing upstream.

## Issue disposition

| Follow-up | Current disposition |
| --- | --- |
| IMP-001 | Unchanged: production VALIDATE raw-response cleanup still missing; fake-response disposal is not a production fix |
| IMP-002 | Unchanged: OathTool cancellation omissions/handling remain |
| **IMP-003** | **Closed by upstream and verified**: input cloning and independent processing prevent PUT from consuming the owned seed |
| **IMP-004** | **Code addressed; regression verification incomplete**: failure cleanup paths corrected, but no meaningful assertion observes partial-decoder output clearing |
| IMP-005 | Unchanged: impossible Base32 terminal lengths remain accepted in SDK, OathTool, and Cli.Commands |
| IMP-006 | Unchanged: ambiguous creation identifiers and invalid direct periods remain |
| IMP-007 | Unchanged: numeric narrowing/negative counter omission remain |
| IMP-008 | Unchanged: full CALCULATE still returns the digit header and lacks full-value validation |
| IMP-009 | Unchanged: LIST/aggregate required-structure gaps remain |
| IMP-010 | Unchanged: reset refresh failure can leave stale session state after erasure |
| IMP-011 | Unchanged: aggregate raw-response cleanup still missing |
| IMP-012 | Unchanged: help can enter the reset path |
| IMP-013 | Unchanged: boolean flags can consume positionals |
| IMP-014 | Unchanged: required SELECT metadata is not enforced |
| DEC-001 through DEC-007 | All remain open: auth provenance, raw-key lengths, URI policy, padding provenance, timestamp domain, HOTP operation details, SCP gate provenance |
| DOC-001 | Unchanged: verified distribution-specific PC/SC setup reference remains absent |

### IMP-003 closure evidence

At imported `089b2393`, `CredentialData.Secret` clones initialization input into private storage and returns a borrowed `ReadOnlyMemory<byte>` view. `HmacShortenKey` now creates an independent copy even below the block size. `GetProcessedSecret` returns independent storage and clears its short intermediate when padding. PUT's existing cleanup therefore clears temporaries rather than the credential seed.

Meaningful tests in the executed OATH suite include:

- `CredentialDataTests`: input isolation, disposal/view/copy ownership, exact preprocessing bytes and 13/14, 64/65, 128/129 boundaries (incoming lines24–131).
- `OathAuthenticationTests.AuthenticateAndRetryAsync_PutLocked_PreservesSeedThroughValidateAndRetry`: actual PUT→Locked→VALIDATE→PUT sequence, both payloads, and surviving input/seed, including a long-length SHA512 payload (incoming lines187–283).
- Exact successful PUT payload cases in `OathSessionTests` (incoming lines465–574).

Post-disposal or concurrent use remains unsupported. This closure does not claim getters/operations throw `ObjectDisposedException`; no such guard was added.

### IMP-004 qualification

Non-secret validation now precedes decoding. Parsed temporary output is cleared in `finally` after cloning into the credential. The SDK and both CLI decoder paths clear partially written output when decoding throws. These source corrections address the identified paths.

Incoming tests do not observe failed partial-decoder output clearing. Successful disposal/view tests exercise a different lifetime. Keep a focused verification follow-up, preferably coordinated with Base32 validity work, rather than reimplementing already-correct cleanup or adding trivial tests that merely mirror `finally`.

Cleanup and encoding-shape validation are separate: input `A` still follows a source path to an empty decoded array, then potentially all-zero storage padding. No malformed-length runtime reproduction was performed in this reconciliation.

## Ordering contract and documentation

Natural ordering is now ordinal `DeviceId`, then unsigned lexicographic raw `Id`. It is not display ordering with an extra tie-breaker. Comparison zero matches identity equality regardless of display metadata. New `CredentialOrderingTests` cover sorted collection retention, equality consistency, unsigned-byte/ordinal ordering, and comparison laws; `CredentialTests` demonstrates explicit UI ordering.

This changes a previously documented/supported behavior rather than closing an existing implementation follow-up. The merge retains upstream identity-order documentation, removes transition narration from active API remarks, and adds a concise v2-alpha ordering migration note. It does not claim existing CLI display sorts adopted the explicit identity tie-breaker recipe.

## Merge and review

Three content conflicts were resolved in the OATH README, CredentialData, and IOathSession. Resolution preserves incoming runtime code and combines the new ownership contract with the audit's firmware, overwrite, padding, and authentication clarifications. Old direct-input-aliasing prose was replaced; historical baseline evidence is retained in the audit records.

Fresh Sol review (`ses_e621dadf5ffeadQj3R4X0XzL5D`): PASS. It confirmed current ownership/sorting contracts, no stale scoped statements or merge markers, intact upstream implementation, and 70 well-formed XML documentation blocks. Its review did not execute tests; execution results below are the coordinator's.

## Validation

- Full solution Release build: passed, **0 warnings**.
- Unit tests: **3,684 executed, 0 failed, 5 skipped** across13 projects; **167 OATH tests, 0 skipped**.
- Documentation QA: 73 active docs and10 package readmes passed.
- Architecture evidence map and image freshness: passed.
- Complexity against the updated integration base `origin/yubikit`: unchanged existing debt only, no new/worse method requiring action.
- Scoped formatting and staged/unstaged whitespace checks: passed.
- No hardware integration or consumer installation verification.

Command: `dotnet toolchain.cs -- build test docs-qa docs-architecture complexity --filter 'Category!=RequiresUserPresence' --complexity-args '--base origin/yubikit'`.

All22 entries were reviewed against incoming code and relevant test source. The remaining12 implementation concerns, seven decisions, and setup-reference task were not closed merely because unrelated upstream tests passed. The two commits remain local; no push was requested or performed.
