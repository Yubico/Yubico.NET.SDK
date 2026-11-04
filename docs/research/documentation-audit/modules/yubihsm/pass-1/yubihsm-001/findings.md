# YubiHSM triage and implementation handoff

Astra, 2026-11-04; baseline `a74792facb657413e98223e059876d8b213ffff3`. The investigator's full
12-candidate observations and original passages remain in investigation.md and evidence/sources.md.

## Documentation decisions

Accept HSM-C001–012 for supported documentation improvements, subject to fresh independent review.
The user-approved affirmative voice is an acceptance criterion: lead with actual behavior and actor,
use direct procedures, preserve meaningful qualifications and avoid repetitive negative contrasts.

- C001–002: correct success-reset and deletion/blocking distinctions; scope typed retry errors to actual
  methods. Astra read GetChallenge's complete send/cleanup path: it uses ordinary backend errors and
  bypasses the typed retry helper. Hardware probes will separately measure retry effects.
- C003–004: complete enrollment and secret-role explanations, recoverable management-key rotation,
  actual HSM key/challenge/cryptogram responsibilities and typed application-dependent sketches.
- C005: distinguish applet-resolved policy, metadata, caller override and development hardware; six
  feature fields include the misleading obsolete alias. Retain source-supported SDK gates.
- C006–008: precise ownership and Parse copying (Astra checked all SessionKeys implementation), prompt/
  LIST/cancellation limits, unknown metadata and ordinal comparison versus record equality.
- C009–011: diagnostic sample scope, symmetric-only calculation path, actual output/cleanup conditions,
  assertion-based test comments and present-tense backend explanations. Preserve all runtime strings,
  executable statements and test assertions. The proposed test-guide link correction was rejected:
  `../../../docs/TESTING.md` correctly resolves from src/YubiHsm/tests. See review.md R1 and the
  editor's independent target check; the original investigator observation remains historical.
- C012: narrow current shared-document consistency and clearly scoped clarification of hybrid historical
  gap entries. Preserve existing DOC-002 map parse error outside assigned entries and NativeAOT link-only
  qualification. Complete source/API behavior audit remains within YubiHSM.

S1–S5 establish descriptive vendor guidance/corroborating implementation; GAP-SPEC-1 records the bounded
search for versioned normative applet protocol, especially development firmware/asymmetric receipts.
Independent reviewer must verify original consequential passages and their exact applicability.

## Deferred engineering and verification

All rows: implementation **deferred-pass-2**, owner future module implementation coordinator after fresh
re-baselining. There are eight engineering groups after the late cryptogram finding; candidate and
engineering counts represent distinct overlapping classifications.

| ID / origins | Observed issue and next action | Documentation reopening trigger |
| --- | --- | --- |
| HSM-IMP-001 / C002 | GET_CHALLENGE generic errors bypass typed retry policy. Decide consistent handling and verify relevant command responses. | Error path/status policy changes → interface, retry exception, README, migration map and retry-test explanations |
| HSM-IMP-002 / C006/C009 | Sample exceptional paths strand owned secrets before cleanup; preserve precise Parse/source/copy lifetimes. Audit and repair allocation scopes with meaningful failure checks. | Ownership/allocation/disposal changes → SessionKeys XML, examples, README and security/internal explanations |
| HSM-IMP-003 / C008 | LIST skips short recognized records and decodes invalid UTF8 permissively; unknown algorithms/touch require deliberate response policy. | Parser/raw-metadata policy changes → LIST/DTO/algorithm XML, policy fallback and parsing-test comments |
| HSM-IMP-004 / C009 | UI selects all algorithms but calculation calls symmetric API; interactive tool supplies no presence prompt. | Dispatch/filter/prompt integration changes → sample guide/menu explanations and ceremony evidence |
| HSM-IMP-005 / C009 | Secret CLI/visible input and displayed key material require explicit diagnostic/safe-default product policy. | Input/output policy changes → tool overview, command/operation comments and credential guidance |
| HSM-IMP-006 / C010 | Weak length/metadata assertions, multiply invalid dummy handshake, exact exception mismatch, missing persistent cleanup and CTS/input lifetimes. Improve appropriate assertions/setup/teardown. | Test bodies/helpers/oracles change → all related test comments, test guide and qualification claims; preserve old outcomes |
| HSM-IMP-007 / C004/C008 | Asymmetric key/point/cryptogram and challenge/public-key response validation are partly delegated. Decide request/response validation contract. | Validation or raw escape-hatch changes → exception/format contracts, handshake guide, byte-level tests and HSM integration expectations |
| HSM-IMP-008 / hardware-v1-kat | On serial 103 development firmware, supplying a valid synthetic symmetric card cryptogram produces a 56-byte response rejected by the strict 48-byte parser. Establish response layout/version applicability and intended key/receipt API before fixing. | Response interpretation/cryptogram handling changes → symmetric calculation XML, SessionKeys parser contract, README handshake/sketch, examples and hardware tests |

## Core dependencies — final Core cycle

| ID | Precise inherited question | Reopen documentation when resolved |
| --- | --- | --- |
| HSM-DEP-001 | ApplicationSession/Feature/FirmwareVersion effective development/override semantics versus metadata filtering and applet SELECT | Feature table/XML, reset version policy, old-firmware and development qualification |
| HSM-DEP-002 | SmartCard admitted cancellation/drain and connection/prompt ownership; public outcomes versus native guarantees | Factory/README ownership, cancellation/presence guidance, shared interaction and touch-test explanations |
| HSM-DEP-003 | TLV/formatter/response copied-buffer ownership and reachable cleanup across transport | Qualified sensitive-buffer claims, ZeroApduResponse and SessionKeys/input lifetime |

Full candidate evidence and precise symbol/path questions remain in investigation.md:220–240. Core
source and snapshots remain outside this investigation. Breaking release permits future contract changes;
compatibility is not a reason to conceal a defect or preserve an unsuitable API.

## Editor ownership packet

Fresh editor owns documentation/comments in src/YubiHsm plus accepted YubiHSM sections of
docs/usage/user-interaction.md, docs/migration/v1-to-v2.md, docs/migration/v1-to-v2-map.yml,
docs/migration/v1-to-v2-gaps.md, docs/v1-to-v2-comparison.md and docs/v2-highlights.md. Other shared sections
receive coordinator scope before edits. Root README and architecture/NativeAOT passages were investigated
as currently consistent; target existence checks suffice for other modules. Editor saves edit-report.md
with mappings, paths/hashes and complete example inventory; coordinator owns status and findings.

## Hardware evidence state

Initial serial 103, metadata 5.8.0.beta.0/app 5.8.0, eight management retries, one asymmetric test
credential recorded. Investigator inspected all 13 methods/reset-before/no-reset-after effects.
Eleven non-UP-tagged methods are now being executed. Two UP-tagged methods only test metadata; their
separate execution will retain this narrower meaning. Actual touch/cancel probes require their own
recorded circumstances. All hardware results remain pending until raw outputs are inspected.

### Late hardware update

The eleven initial integration methods passed. The stronger KDF probe exposed HSM-IMP-008 after its
omitted-cryptogram calculation independently matched all three keys and a wrong-password request yielded
63C7/LIST seven. Retry restoration after the cryptogram-bearing response was not reached. Original probe
failure and cleanup are retained in corrections.md. There are now **eight deferred engineering groups**;
the original investigator's seven-group report is a historical checkpoint.
