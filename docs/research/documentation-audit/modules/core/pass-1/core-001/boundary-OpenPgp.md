# OpenPGP/Core boundary investigation

**Outcome:** complete source/assertion investigation of the exact **328 assigned
semantic units**: **209 read-clean, 34 read-candidate and 85 read-related-context**,
with zero unread. All **93 disposition paths / 12,927 lines** were read in their
complete local caller, helper, ownership and assertion context. Eight local proposal
groups, `CORE-PGP-X001` through `CORE-PGP-X008`, are ready for coordinator triage.
They allocate no accepted Core finding IDs and add no execution evidence.

The source pin remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed HEAD
`da66f5d01ba594993974ddd26723e8c300c0a454` is the metadata checkpoint. Every assigned
raw file/range SHA-256 matches inventory, and every disposition path matches pinned
Git content after explicit UTF-8 BOM/CRLF normalization. Hash identity is separate
from the authored source/assertion reading and from later independent review.

**Inventory qualification:** the literal packet `paths` array contains 91 entries;
`disposition_paths` and `file_count` contain 93. The additional paths are
`src/OpenPgp/src/PublicAPI.Shipped.txt` and `src/OpenPgp/tests/CLAUDE.md`. They received
explicit file dispositions without fabricated semantic IDs. The packet intentionally
selects only some units from complete files; this return preserves those exact IDs.
For example, the interface's complete contracts were read, while its assigned unit
set contains its preamble, declaration and PutKey contract rather than forty invented
method units. Related declarations/project/API context remain outside the clean
sampling denominator.

## Durable records

All boundary-owned records are under [boundaries/OpenPgp](boundaries/OpenPgp/).

| Record | Purpose / limit |
| --- | --- |
| [Unit dispositions](boundaries/OpenPgp/unit-dispositions.json) | Every original ID, full character/line coordinates, source/range hashes, localized concerns and pending independent review/compiler/runtime gates |
| [File identities](boundaries/OpenPgp/file-identities.json) / [file dispositions](boundaries/OpenPgp/file-dispositions.json) | All 93 paths, exact raw/Git/normalized identities, complete-file observations and retain reasons |
| [Authored semantic notes](boundaries/OpenPgp/semantic-notes.json) | Source, actual helper/assertion and ownership reasoning; not an inference from hashes |
| [Candidates](boundaries/OpenPgp/candidates.md) / [index](boundaries/OpenPgp/candidate-index.json) | X001–X008, separate documentation/implementation/policy/qualification facets and actual source anchors |
| [Unchanged paths](boundaries/OpenPgp/unchanged-paths.json) | Candidate-associated paths retained with reason, future owner and reopening trigger |
| [Call flow and verification](boundaries/OpenPgp/flow-and-verification.md) | Factory/connection/SCP/PIN/KDF/APDU/prompt/reset resource tables, actual test oracles and bounded unexecuted proposals |
| [Inherited outcomes](boundaries/OpenPgp/inherited-dispositions.json) | All 15 routed run-qualified provenance keys, original registry states/conclusions and narrow boundary outcomes |
| [Documentation dependencies](boundaries/OpenPgp/documentation-dependencies.json) | All 21 original PGP-DEP definitions/reopening maps; zero additional Core bugs |
| [Applet findings](boundaries/OpenPgp/applet-findings-dispositions.json) | All fourteen original engineering groups, historical confirmation and precise remaining source scope |
| [Example dispositions](boundaries/OpenPgp/example-dispositions.json) | Current complete/composable/protocol/command fragments retained; no new compiler or runtime credit |
| [Sources](boundaries/OpenPgp/sources.md) / [source identities](boundaries/OpenPgp/source-identities.json) | Reused original Card/Python/vendor evidence, exact authority and remaining SCP/ISO/native qualifications |
| [Dependency reads](boundaries/OpenPgp/dependency-reads.json) | Actual Core/helper/shared ranges and routing seeds outside OpenPGP's SmartCard call path; no extra denominator credit |
| [Clean nominees](boundaries/OpenPgp/clean-nominees.json) | Eight substantive original units for later independent sampling; no completed sampling implied |
| [Coverage](boundaries/OpenPgp/coverage-summary.json) / [static verification](boundaries/OpenPgp/preservation-verification.json) | Exact populations, pin/range accounting and explicit unrun gates |

## Current supported journey

The application selects its intended key, then creates a SmartCard-only OpenPGP
session. The convenience extension owns its hidden connection after successful
creation; a connection-taking factory borrows the caller's connection. Core binds
before SELECT and registers the protocol before setup. OpenPGP selects/activates,
reads applet version, configures the effective version and optional SCP, rebinds the
backend and parses application data before readiness is published. Failed creation
disposes/detaches the session; hidden-connection cleanup remains with its helper.

The application supplies PIN bytes and separately owns their collection/clearing.
Reader presets count UTF-16 code units before UTF-8 encoding, while card profile and
C4 describe credential format/limits. The direct PIN methods add no general profile
validation. Successful SCP setup sets inherited channel authentication; the card
still enforces PW1/PW3 access conditions. Both User modes address PW1, while the
current extended derivation/retry path incorrectly selects Reset Code identity.

The application awaits complete public methods sequentially and keeps inputs, keys,
KDF aliases and prompt services alive through actual use. Core guards one logical
exchange, including command/response chaining and SCP state; it supplies no lease
for certificate occurrence selection plus read/write, key mutation plus refresh,
KDF lookup plus derivation, or reset. After admission the exchange uses None; an
outer canceled wait is separate from native completion, protocol drain and release.

Synchronous OpenPGP disposal clears the cached KDF before base protocol drain.
Asynchronous disposal first awaits Core teardown, then reaches derived KDF cleanup
in finally. Both clear the current cache. Replaced/reset KDF references, F9 encoding
copies and parser intermediates retain the existing engineering ownership gap.
The current tests establish narrower idle salt-clear/detachment behavior.

Reset preserves the configured effective version, attempts the documented PIN/E6/44
sequence and refreshes app data without a full erasure postcondition. On an existing
SCP session, its plain reselect retains the secure graph and performs no handshake.
The test helper instead uses a separate plain reset session before an optional SCP
body session. Those are different journeys. Reopen after uncertainty and independently
inspect persistent state before retrying; retain the ATT reset exception.

## Triage summary

| Local group | Documentation proposal | Separate engineering / qualification |
| --- | --- | --- |
| X001 | Explain actual borrowed/owned creation and sync/async KDF cleanup ordering | PGP-IMP-003 ownership and B5 complete-workflow lifetime remain open |
| X002 | Name UTF-16 preset units, provided-string bypass and channel versus PIN auth | Preserve PIN/KDF identity, transition, parser and status-policy findings |
| X003 | Follow source, encoding, transport, response, fragment and SCP owners; link Card absent-Le tables | Reuse C/B6 copy/cleanup mechanisms at F9/PIN/import/decipher callers; no duplicate Core bug |
| X004 | Qualify per-exchange versus complete-public-task drain and notification lifecycle | Attest certificate can be abandoned on terminal callback failure; sensitive crypto paths retain explicit resolution-before-transfer |
| X005 | Document retained secure graph/plain reselect and no reset handshake | Reset terminality/restoration/status policy and actual SCP reset qualification remain unresolved |
| X006 | Correct broad version fallback explanation and preserve detected/effective/metadata sources | Broad fallback, development framing and firmware/profile support remain distinct policy/qualification facets |
| X007 | Update PutKey's incomplete EC representation list without promising card support | Core catalog/provider changes leave X25519, ATT, NEO, ROCA and parser findings open |
| X008 | Keep test/sample explanations within actual assertions and cleanup scopes | Unverify's signing-failure control can be confounded by an uncontrolled Always policy; existing tool/test ownership and physical gates stand |

The actual shared references are nonadditive: B5 C001/C006 for effective version and
whole-workflow lifetime; B6 C001/C002/C003/C007 and its secure-copy/profile findings
for Le, response producers, SELECT/recovery and contained key owners; C C001–C006
and C020 for credential units, TLV copies and SCP containment. The ownership table
names their actual OpenPGP caller sites rather than proposing generic duplicates.

## Inherited outcome and deferred handoff

`openpgp-001:PGP-CORE-001..004` now have complete current caller-source answers:
reader/card units, actual async KDF dispatch, effective-version provenance and
exchange-versus-workflow/prompt ownership. This finishes their bounded investigation
dimension. Fresh documentation acceptance and any implementation/policy/platform
closure remain separate. The other eleven routed keys receive only this shared
mechanism's OpenPGP applicability; their applet-specific work stays with its owner.

All fourteen original OpenPGP engineering groups retain their identity and deferred
status. In particular, historical `PGP-IMP-001` PIN/KDF identity, `PGP-IMP-007`
X25519 framing and `PGP-IMP-013` ATT public read remain confirmed engineering findings
for their recorded scope. Corrected prose, current Core metadata and source changes
do not silently close them. The 21 `PGP-DEP-001..021` labels remain claim-to-document
dependencies, with their original definitions and exact reopening maps preserved.
The historical timestamp correction continues to extend PGP-IMP-018.

Prior serial 103 results stay historical Linux USB SmartCard evidence: applet 0.0.1,
metadata 5.8.0.beta.0, CLI application 5.8.0, failed setup/ceremony attempts, confirmed
defects, successful same-key/correct-CRT controls and scoped touch/recovery outcomes.
The observed 6600/Failed cancellation and fresh-session recovery provide no native
interruption/timing causality. Prior restoration preserved ATT material and restored
all four UIFs Off; this boundary performed no device read or mutation.

## Verification, next owner and stopping point

Static checks passed for all exact IDs/ranges/raw/pinned identities, routed keys,
documentation dependency identities, candidate path reasons and eligible nominees.
These are same-investigator record checks, not independent semantic/citation review,
archive-wide revalidation, example compilation, SDK tests or hardware acceptance.
The source packet is complete; the remaining gates belong to the coordinator's
existing Core Pass-1 edit/review/validation assignments.

Writes are confined to `boundaries/OpenPgp/*`, this report and home-backed scratch
under `/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/OpenPgp/`.
Product XML/runtime/assertions/projects/API snapshots, central ledgers, Git index/
refs, frozen archives and other agents' work were left untouched. No SDK build/test,
formatter, native/device/crypto execution, new tooling project or subagent ran.
Static Python/Git reads and Context7 documentation orientation supplied no runtime
evidence. The unfinished Windows concurrency handoff remains unready and unreviewed;
PIV touch/hotplug/recovery deferrals stand.

Next action: coordinator triage, fresh bounded documentation editing and independent
review, then the existing final validation/publication gates. Finish Core Pass 1,
provide the deferred handoff and stop. This return requests no Pass-2 planning,
implementation project, new generic validation framework or physical action.
