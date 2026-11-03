# OATH-002 investigation coverage

Baseline: `ddf3cdc` plus validated OATH-001 documentation. Read-only Sol investigation, followed by Astra source/specification checks. No execution or hardware verification during investigation.

| File/unit | Investigator reading |
| --- | --- |
| CredentialData.cs | 1–335, entire source/comments |
| Credential.cs | 1–241, entire source/comments |
| OathType.cs / HashAlgorithm.cs | 1–31 / 1–36, entire source/comments |
| OathConstants.cs | Entire file; creation tags, masks, defaults, flags checked |
| OathSession.cs | 147–166, 185–321, 519–546: identity boundary, LIST/PUT, malformed-ID handling |
| IOathSession.cs | 62–75: listing/PUT documentation |
| README.md | 10–22, 58–67, 159–164: requirements, creation, ownership |
| CLAUDE.md | 19–64, 79–88: identity/key processing, firmware qualifications |
| CredentialDataTests.cs | 1–445 |
| CredentialTests.cs | 1–113 |
| CredentialIdTests.cs | 1–193 |
| Unit OathSessionTests.cs | 464–552: PUT payload tests |
| Unit AuditV2/OathAuditReproTests.cs | 16–172: periods/identity |
| Integration OathSessionTests.cs | 32–60, 102–139, creation evidence within 247–343 |
| Integration AuditV2/OathAuditReproTests.cs | 1–69 |
| AccountsCommand.cs | 49–122, 271–329, 432–473: creation, URI, decoder |

Source-read and test-read do not imply tests were executed or all claims in shared files are closed. Required root/module/test instructions and approved style reference were read.

## Supported claims

Credential identity/hash use device ID and raw ID bytes, excluding touch metadata; the constructor copies input IDs. Ordinary ID round trips and HOTP period zero agree with tests. Display ordering uses case-insensitive issuer/name, not identity; exact Unicode equivalence with Python was not established. Ordinary defaults and algorithm constants, RFC 2104 block-size shortening, and PUT payload/property encoding are supported. The normal README URI example is valid and disposes its creation object.

## Coordinator verification and sampling

Astra checked CredentialData, Credential, OathType, HashAlgorithm, OathConstants, PUT encoding/cleanup, both Base32 paths, relevant README/CLAUDE passages, and CALCULATE ALL's touch-tag handling. It checked the Google URI format and RFC 2104 directly; the already-fetched official YKOATH page supplied firmware/type/PUT evidence.

Three straightforward documentation misses were found during these checks: overly broad whitespace tolerance (F12), reversed type/value association in the guidance table (F13), and the claim that every calculation waits for touch (F14). This is evidence to strengthen subsequent reviewers' checks of tables, absolutes, and sibling-operation exceptions. It does not imply the eleven investigator findings are invalid.

## Gaps and deferred decisions

See findings F02–F06/F11 and the implementation-follow-up document for ownership, failed parsing cleanup, URI strictness, Base32 shape, identity ambiguity, and numeric range handling. Padding rationale remains unresolved. No hardware minimum-firmware behavior, arbitrary-input acceptance, full calculation behavior, transport internals, or general Core audit was performed.
