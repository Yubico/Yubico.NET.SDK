# PIV piv-001 closeout

## Scope and baseline

This documentation-only cycle starts at **727fe43afc430c5d8364d868a6436de50626e476** on
`yubikit-docs-audit`, after PR #745 merged into `yubikit` at `b1362127`. The recurring watcher verified
merge and containment, then paused. The user supplied standing approval. A separately reviewed and
validated integration checkpoint preserved both histories before investigation; PIV initially matched
the merged PR exactly. Integration changes and failures remain separate from this documentation delta.

Whole-module reconciliation covers **123 paths, 23 public types, 370 API entries and 42 declared
interface operations**. Accepted changes cover **59 product paths: 53 module paths and six shared
sections**. The other 70 module paths retain their baseline bytes. All **944 original semantic units**
(927 module and 17 scoped shared units) have explicit final dispositions. API representation entries,
including generated record members, are distinct from task operations.

Fresh Sol sessions investigated, edited and independently reviewed content and original citations.
Astra adjudicated, sampled, coordinated device work and ran validation. Final source acceptance is
`evidence/review-accepted-v4.json`; prior acceptance checkpoints remain immutable. Fresh final evidence
review and publication remain pending.

## Consumer documentation

The guide follows discovery, PIN/PUK and management authentication/recovery, key generation/import,
algorithm-specific operations with host verification, certificates and attestation, typed/raw data,
PIN-only setup/reopen/recovery/disable, policies and deliberate reset. XML and concrete reference
inheritance explain encoded units, prerequisites, output formats, ownership, partial effects and errors.
Tool guidance covers actual commands, result fields and supported routes around current sample limits.
Internal and test comments describe current mechanisms and the assertions actually established.

Independent review corrected F9 result/JSON meaning, partial credential rotation and residual
SHA-256/Ed25519 test overclaims. Actual annotated-example compilation exposed a meaningless null
assertion on a value type; the example now checks retry coherence and declares its firmware prerequisite.
Canonical test selectors avoid retained audit hosts. Final coverage reconciliation corrected DER SPKI
result semantics and the deterministic RSA re-wrap invariant. All corrections received independent
re-review. The approved voice remains affirmative, task/actor first, direct and calm, with useful
rationale and conditions preserved.

Standards retain exact revision, publication status, actor and profile: combined SP 800-73-4 (May 2015,
updated 2016), SP 800-78-4 and FIPS 201-2 archival notices, applicable FIPS 186-5/RFC arithmetic and
encoding, and Yubico reset/attestation/policy extensions. Pinned Python code, tests and history establish
implementation comparisons and firmware-check origins. Sibling agreement supplies no certification or
automatic adoption of newer profiles. ISO edition-specific conformance remains a bounded evidence gap.

## Engineering return

**Ten deferred implementation/qualification groups and three Core questions** retain stable IDs,
source evidence, paths and reopening conditions in [findings](findings.md) and
[investigation](investigation.md). PR #745's fixed authentication, retry, ownership and recovery items
were re-baselined. No shipping runtime or assertion changes were made in this cycle.

Supplemental results establish:

- RFC 7748 X25519 agreement matched both the SDK and an independent vendor operation on the same
  imported key before cleanup.
- PIN Always rejected a second unverified operation; reverification and two PIN Once signatures passed
  host verification. Status attribution remains explicitly tied to SDK exception mapping.
- Key move/delete retained unrelated certificate DER independently of the moved key's verified SPKI/signature.
- Attestation verified the preserved issuer signature and generated-key binding; manufacturer-root trust
  remains outside that claim.
- **PIVIMP-005 is confirmed:** actual PivTool RSA output failed original-message verification despite
  helper success; Ed25519 output verified SHA256(message), rather than the original message. Direct SDK
  controls passed. Both desired-contract assertions remain failed; their fixes belong to Pass 2.

Other groups cover Bio reset preflight, PIN-only coherence, firmware/profile qualification, typed-object
generality, sample workflow/ownership/cancellation, crypto-response and padding handling, test oracles
and cleanup, AdminData timestamp interoperability, and raw object-ID range. Core's final cycle retains
physical PIN state, cancellation/serialization and shared ownership questions. A dedicated cross-module
boundary review was not opened during PIV.

## Verification and qualification

- Final clean solution gates passed **4,124 unit tests**, including **289 PIV**, five existing skips and
  zero compiler/analyzer warnings or failures. Documentation/package, architecture and complexity checks
  passed. The reported parser complexity is unchanged debt.
- **86/89 distinct canonical integration methods passed:** 83 single-key plus three two-key cases.
  The initial fixture skips remain recorded alongside their later successful dedicated execution.
- **Six supplemental methods executed:** four passed and two failed intended original-message contracts,
  with zero skips. Their compiler/runtime identity checks and independent cleanups passed.
- **Physical tests were explicitly deferred by the user.** Three canonical physical-lane methods,
  including that lane's read-only prerequisite printer, and P07 coordinated cancellation/recovery were
  not run. Compilation and unit evidence establish no touch/cache/hotplug/native-cancellation outcome.
- Final independent readbacks on **103 and 125** match every captured stable baseline field, excluding
  observation time and capture label. Both retain default PIN/PUK 3/3, AES192/default/Never management
  metadata, empty normal slots/certificates, captured object absence and distinct F9 identities/policies.
  Factory PIN and management authentication passed independently. Existing F9 private keys were never
  replaced. Protected PRINTED content was deliberately unread, so no separate byte-absence claim is made.
- The original tracked allowlist was restored byte-for-byte; all hardware jobs completed before final readback.
- **50 edited C# paths** preserve tokens, directives and imports. Formatting changed whitespace only;
  all 20 API snapshots remain unchanged. No package/project metadata changed in this cycle.
- Expanded sampling covers **64/385 clean regions**, exceeding the 39 minimum, including additional
  test and internal-prose selections after material misses. No further miss was found in the expansion.

Hardware scope is Linux USB SmartCard on development devices: serial 103 at **5.8.0.beta.0** and 125
at **5.8.0.alpha.2**, both Enhanced PIN. Their raw applets return `00 00 01`, distinct from CLI and
Management observations. Source-checked public fallbacks preserve that provenance. Bio/enrollment,
NFC, SCP-specific PIV operation paths, older releases/NEO, FIPS products, other OSes, manufacturer-root trust and Native AOT runtime
remain separate qualifications.

## Compiler evidence and records

The final example inventory contains **45 typed groups / 47 selected bodies**, **60 current bodies /
61 originals**, and **730 explicit dispositions**. It covers all 111 module C# files, four module
Markdown files, six shared documents and the actual **36-source PivTool**. The preceding v3 contexts
compiled warning-free and passed live verification. Fresh v4 contexts also compiled warning-free and
passed exact Csc/project-graph and live checks, binding the two final comment-only corrections. Their
619 reference/analyzer entries span three contexts. No example body changed in that final correction.

Original successful and failed compiler contexts remain attached to their own inputs. The six tested
runtime contexts retain their original binaries and source snapshots. All 2,166 captured Csc-input and
runtime-copy entries still matched after the final comment-only build. The two regenerated root bin
outputs are distinguished from those tested copies; all 24 original project obj/bin identities are
preserved externally with exact hashes. No runtime rerun is inferred or required by comment changes.

[Validation](validation.md), [examples](snippet-validation.md), [hardware results](hardware/results.md),
[coverage](coverage-final.json), [unit dispositions](unit-dispositions-final.json) and
[sampling](sampling.md) preserve exact scopes. Earlier 3,401 frozen artifacts, four original user files
and 114 integration files remain preservation gates. Four registered original PDFs, raw failures,
source history and corrected tooling provenance will be frozen; generated cache/runtime files stay external.

After final compiler and record checks, obtain fresh independent evidence review, freeze this run,
commit/push owned content and verify remote HEAD. The external [PIV handoff](../../../../piv.md) records
publication after freeze. Complete the remaining first sweep before implementation returns; Core stays
last, including dedicated per-applet boundary reviews and re-baselined dependencies.
