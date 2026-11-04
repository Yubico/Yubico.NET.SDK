# OpenPGP triage and return-visit ledger

Astra, 2026-11-04; baseline fe180282a2d77b15eaaa5868d6c8b00e251d1f05. Whole-module investigator read
96 paths, 39 public types/423 API entries and 40 interface operations. The 21 PGP-C candidates and
original passages remain in investigation.md and evidence/source-analysis.md. Source observations,
desired contracts and recorded hardware outcomes retain separate dispositions.

## Documentation decisions

Accept supported C001–C020 documentation improvements for fresh editing and independent review; C021
remains a version-provenance qualification and Core boundary question. No runtime/assertion/API change
is authorized in this Pass 1. The next release is breaking; compatibility is not a reason to conceal
a defect or preserve an unsuitable future API.

- C001–003: explain logical User PIN versus VERIFY mode; current distinct-salt/retry mismatch and KDF
  persistent transition/alias lifetime. Astra read the full KDF properties/derivation paths and prepared
  probes; salts are cloned by their init setters, while the session aliases the KDF object. Preserve
  existing test's actual deriving calls; new hardware evidence will settle the observed transition.
- C004–005: reset ordinary application material with explicit ATT exception; method return lacks a
  complete reset postcondition. Fixed ATT policy can require irreversible key replacement. Public
  certificate backup preserves certificate bytes, not the erased factory private key/manufacturer trust.
- C006–009: algorithm-specific message/hash/signature formats, raw RSA/ECDH inputs and caller-owned
  outputs, current X25519 framing gap, partial persistent mutation and stale cached attributes.
- C010–014: current SDK thresholds versus vendor introduction/capability checks; ROCA security rationale
  and exact unaffected/imported boundaries; evidence-scoped attestation; malformed ATT CRT; NEO import/
  generation/deletion differences. Ordinary and extended attestation routes are evaluated separately;
  source examples alone establish no exclusive-mode rule.
- C015–020: task-complete examples, tool limitations/secret strings/unused PIN, precise assertions,
  public/internal accessibility, model encoding/parse behavior and scoped shared-document consistency.
  Proposed chronology discrepancy stays a historical evidence gap; no speculative v1 narrative rewrite.

The Card specification at the URL named 3.4 is actually **Version 3.4.1, March 18, 2020**. Cite its real
title/revision and exact clauses. RFC 9580 establishes the S2K primitive, not a blanket newer-card/packet
profile. Python revision 4ca60f706af930459138d8dc0f0f953480e1c7a4 and retained history corroborate
specific gates/behavior; original vendor advisory/extension/manual establish their respective scopes.
Commercial 5.2.3 language versus 5.2.0/5.2.1 source gates and historical NEO execution remain bounded gaps.

### PGP-C022 — sampled generation-time semantics

Accepted required documentation correction, independently confirmed as PGP-REV-005. Card3.4.1§4.4.1/CD
defines zero as unspecified, and RFC4880§3.5 defines unsigned four-octet seconds since1970UTC. Current
DiscretionaryDataObjects parser combines bits into signed int and the setter writes that int's bits.
High-bit values therefore appear negative, requiring raw-bit interpretation rather than a pre-1970 date.
Add the sentinel/carrier qualification to GenerationTimes, its property, Get/Set APIs and metadata guide.
Implementation representation/range/sentinel policy extends **PGP-IMP-018**, without another bug count.
Reopen these five semantic surfaces after representation/validation changes. Expanded API sampling read
four further units after this miss; accepted clean hashes are in sampling-accepted.json.

### Independent review corrections

PGP-REV-001–005 are accepted after checking exact surviving source statements and original clauses:
reported zero count is not a universal blocking proof; signature policy belongs to card access state;
6A82 fake responses show file/application failure and broad KDF fallback; reset summary describes its
attempted sequence; timestamp metadata preserves the unsigned wire and zero-sentinel meaning. Fresh
editor corrects seven paths, including newly edited GenerationTimes. Original58-path acceptance and
required feedback remain in review.md/evidence; corrected hashes require independent re-review.

## Deferred engineering groups

All rows: **deferred-pass-2**, future implementation coordinator re-baselines evidence first. Preserve
the investigator's stable IDs, including intentional numeric gaps for documentation-only candidates.

| ID | Current issue / next implementation action | Documentation reopening trigger |
| --- | --- | --- |
| PGP-IMP-001 | Extended User VERIFY uses Reset identity for KDF/retry fallback; separate P2 from logical PW1. | PIN/Pw/KDF/retry XML, README, migration and byte-level test explanations after identity/status correction |
| PGP-IMP-002 | KDF provisioning/change/disable transition lacks complete high-level contract and meaningful same/fresh-session coverage. | SetKdf/Change PIN/KDF guide and Advanced test explanations after transition policy/evidence |
| PGP-IMP-003 | KDF cache aliases, replacement/reset disposal, serialized hash clearing and raw-write invalidation need an ownership contract. | Kdf/Get/Set/Reset/PutData XML, resource examples and internal disposal commentary |
| PGP-IMP-005 | Reset ignores TERMINATE/ACTIVATE statuses and assumes PIN blocking; establish status/postcondition policy. | Reset API/guide/tool/helper and reset-test explanations after verified lifecycle changes |
| PGP-IMP-007 | X25519 decipher currently omits the EC cipher DO wrapper; establish exact input/wire/result with independent agreement. | Decrypt/XML/README, format helpers, import/agreement tests and qualification claims |
| PGP-IMP-009 | Multi-command key mutations and direct config writes can leave partial state/stale cached algorithms. | Generate/import/delete/config/raw/cache task and failure guidance after policy changes |
| PGP-IMP-010 | Incomplete family/firmware/capability checks, advertised bounds and algorithm-info workaround behavior. | Feature fields/requirements/UIF/retries/challenge/algorithm discovery after gates or capabilities change |
| PGP-IMP-011 | Shipping ROCA guard absent; four test bodies early-return through corrected 4.3.5 and unnecessarily exclude imported keys. | RSA generation/import requirements and exact test execution commentary after applicability correction |
| PGP-IMP-013 | ATT CRT encodes B6 01 81 rather than vendor B6 03 84 01 81. | Crt/KeyRef/key operations and ATT provisioning limits after byte-level fix/read evidence |
| PGP-IMP-014 | NEO CRT-with-modulus format and key deletion/generation paths differ from vendor handling. | Old-family provisioning/key/delete/import guidance after wire and device qualification |
| PGP-IMP-015 | Tool ignores attestation PIN, misreports fingerprint presence, lacks prompt integration and has secret/certificate lifetime gaps. | Tool guide/comments/output qualifications after input/dispatch/selection/ownership changes |
| PGP-IMP-016 | Weak or overstated test oracles, missing final state cleanup, leaked temporary secrets/certificate results and early-return reporting. | Every affected test explanation and consumer qualification after assertions/setup/cleanup changes |
| PGP-IMP-018 | Parse/forward-compatibility policy for PW length bits, unknown IDs, fallback values and init-required algorithms. | Model/raw/factory contracts and parsing tests after policy changes |
| PGP-IMP-019 | VERIFY maps unrelated failures to PIN errors; change/reset paths use generic errors; retry uncertainty needs deliberate mapping. | PIN exception/API/recovery/migration and wire-test commentary after status policy changes |

These **14 overlapping engineering groups** are not 21 independent bugs. PGP-DEP-001–021 in the original
investigation are claim-to-document dependency labels, including documentation-only entries; preserve
their exact path/reopening descriptions rather than count them all as Core dependencies.

## Core dependencies — final Core cycle

| ID | Inherited question and source dependency | Reopening surfaces |
| --- | --- | --- |
| PGP-CORE-001 | Credential reader limits/units and credential-option factories versus device-profile requirements (C001/C018). | PIN/options XML and user-input examples |
| PGP-CORE-002 | Factory/connection ownership, async disposal dispatch, SCP applicability and KDF cache cleanup boundaries (C003/C021). | Factory/GetKdf/disposal/borrowed-connection guide |
| PGP-CORE-003 | Effective firmware/development/override policy, raw applet 0.0.1 versus metadata5.8beta and protocol framing (C010/C021). | Firmware constants/factory/requirements and old-family qualifications |
| PGP-CORE-004 | SmartCard admitted cancellation, session serialization, operation drain and prompt ownership/outcome (C008/C012). | Crypto/presence/cancellation/resource guidance and actual ceremony tests |

## Hardware checkpoint and editor ownership

Full vendor baseline succeeded; ATT certificate identity captured before mutation. Initial 48-method
batch was interrupted by coordinator's 120-second outer limit after 24 reported passes; independent
cleanup passed. Complete48+isolatedKDF invocations passed all49 methods, zero skips/warnings. Supplemental
probes confirmed PGP-IMP-001 (distinct-salt extended verification and wrong retry identity), PGP-IMP-007
(SDK X25519 returns6A80 while same-key Python derives the RFC secret) and PGP-IMP-013 (SDK ATT query
returns6A80 while correct-CRT public query matches issuer SPKI). Four successful touch operations and
measured6600/Failed cancellation with fresh-session cryptographic recovery passed. Earlier failed
ceremonies/setup/mode attempts remain retained. Final restoration/readback matches every initial public
state field, including Att UIF Off and attestation certificate identity. See hardware/results.md.

Fresh editor owns module documentation/comments and relevant OpenPGP sections in root README,
docs/v2-highlights.md, docs/v1-to-v2-comparison.md, docs/migration/v1-to-v2.md,
docs/migration/v1-to-v2-map.yml, docs/migration/v1-to-v2-gaps.md and docs/usage/user-interaction.md.
Architecture/NativeAOT investigation found current scoped claims consistent; other changes need
coordinator scoping. Preserve existing DOC-002 outside assigned map entries. Editor records full changed
paths/hashes, claim mapping, complete example inventory and clean-nominee overlaps in edit-report.md.
