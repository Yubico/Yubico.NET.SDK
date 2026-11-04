# CTAP2 common claim and sequence matrix

## Authority and current state

Coordinator-owned reconciliation record, updated2026-11-04. All investigations and A–F documentation
reviews are complete; all three narrow integrated boundary corrections and their follow-ups passed.
Claims below are scoped source/specification conclusions, not live
device qualification or new intended behavior. The original boundary-map.md retains baseline findings;
boundary-final.md records resulting cross-layer dispositions. Baseline/ownership: inventory.json/plan.md.

## Required feature/condition rows

| Claim family | Primary investigator | Conditions/evidence to keep separate | Current disposition |
| --- | --- | --- | --- |
| Session/GetInfo/transport/reset | A | Protocol revision, exposed interface, device/family firmware, SDK effective version/override, metadata freshness, state/timing, caller ownership | A-M01–16; DOC-001/002/003/012 reviewed. Actual gate/snapshot/absence/validation limits remain IMP-001..004/020/023/025. |
| PIN/UV protocols and tokens | B | Advertised protocol/options, PIN/built-in UV configuration, permissions/RP binding, retries/power cycle, legacy fallback, SDK checks | B-S01–12/B-B01–14; DOC-004/005/006 reviewed. Command fallback is not protocol negotiation or requested-permission proof; IMP-005..010/021/022 deferred. |
| Credential creation/assertions | C with A | UP/UV/RK options, clientDataHash responsibility, allow/exclude list and counts, version-specific response omission, attestation/COSE/unknown fields | C claim/sequence/byte matrices; DOC-007/008/016 reviewed. Parsing/flags are not RP verification; IMP-011/026..030 and original WA-IMP-010 remain. |
| Credential management | D | Final versus preview commands, permissions, enumeration state/totals, deletion/update and owned result lifetime | D-S1–5/12,D-F1/7; DOC-009 reviewed. Standard-only commands, eager first-total-based sequences, borrowed auth and owned credential results; IMP-018/019/024 deferred; IMP-017 ordering inference rejected. |
| Biometrics and authenticator config | D | Advertised feature, biometric device capability/current enrollment state, authorization, sample sequence/cancel, per-setting requirements | D-S6–11,D-F2–6; DOC-006/009/010/011 reviewed. Standard bio wire/schema limitations candid; toggle isn't desired-state set; IMP-012..016/021/022 deferred. |
| Extensions/storage/crypto helpers | E | Standard/vendor/preview/experimental status, advertised support, credential state, request versus enforcement versus output, chunk integrity and sensitive views | E feature matrix/E-19; DOC-013/017 reviewed. Key/direct/output routes distinct; raw convenience/owner/algorithm/storage gaps IMP-027/030..043 and WA dependencies retained. |
| User journeys and executable samples | F | Complete protocol demonstration versus application integration, actual task reachability, typed prerequisites/lifetime/cleanup, help/cancellation behavior | F-01–12 and final fence classifications; DOC-014/018 reviewed. Real CLI subset/fixed-V2/reset target/cancel/PIN-string limits; IMP-044..048 and existing validation/lane dependencies deferred. |
| CTAP/WebAuthn/RP boundary | G | Legitimate higher-level orchestration/policy versus contradictions, exact cross-layer IDs/transformations/ownership, preserved WA findings | Fresh boundary-review-final and boundary-final accept29 baseline rows/G-001–011 plusBF-R1–R3 corrections. WA-XREF-001 satisfied as documentation; WA engineering/DEC-002 not closed. |

Each accepted row must identify exact source/documentation paths and reference revision/status/section,
actor and normative strength; advertised/configured/authorized conditions; actual SDK enforcement;
tests inspected versus executed; candidate/accepted/deferred disposition; and later re-review trigger.

## Consequential reconciled distinctions

| Subject | Confirmed source/specification distinction | Evidence / later trigger |
| --- | --- | --- |
| Version and firmware | Selected CTAP operations are exposed; advertised revisions/options, current configuration, physical transport/AID and authorization are separate. Major-zero detected firmware passes an optimistic gate, not hardware qualification. | A-003/004, G-010, DEC-003; reopen after negotiation/gating/product evidence changes. |
| GetInfo state | Each call queries again; one-shot creation also probes. Session firmware is not refreshed by later reads. Missing required AAGUID is empty; parsed absent encrypted fields become present empty nullable memory, so HasValue is not presence/support evidence. | A-004/006, COORD-001, IMP-001/002; RawData retains encoding. |
| PIN crypto/ownership | V1 SHA256(z), V2 separate HKDF derivations; new PIN is right-zero-padded64, AES has no padding. ClientPin disposes supplied protocol, borrows session; direct returned token/crypto arrays are caller-owned. | B-01/02/08; re-review inputs/results/protocol/helper examples after ownership changes. |
| Authorization and retries | Requested permissions differ from device grants; legacy0x05 drops explicit permission/RP fields without selecting V1. Forced-change status differs between0x05 and0x09. Ordinary versus persistent tokens and device state differ from host-array lifetime. | B-03/06/09, BF-R1, WA-IMP-009, IMP-008/010; no blind retry or permanent-authority claim. |
| UP/UV and stateful operations | uv key is omitted beside token auth. Notifications resolve a device exchange before typed decode, not RP success. N−1 next assertions and administrative sequences need adjacency; an exchange guard is not a whole-sequence lease. | A/C/B state rows, G-003/009, IMP-011/019; callback/decode/cancel failure is not rollback. |
| Assertions and RP | CTAP2.0 omission requires exactly one actual allow-list descriptor; later reviewed revisions require it. Handle/display fields have different restrictions. SDK parses/compares selected fields; RP still verifies exact client-data bytes, credential/account binding and proofs. | C-001/002/005/013, WA-XREF-001/WA-IMP-010, DEC-002. |
| Administrative results | First reported totals are not recomputed; counts may be estimates. Bio defaults can fabricate completion; retain begin ID. Toggle outcome can be ambiguous, and configured PIN remains needed to change it under forced-change policy. | D matrices, IMP-012..016/019; exact wire/result tests and later hardware evidence needed. |
| Extension layers | credProtect enforcement is a platform obligation, not a wire flag. minPinLength discloses policy. hmac-secret UV variants differ from required verified WebAuthn PRF. Key-based blobs use outer response fields/storage, while direct blobs use a different unsigned route. | E/G originals and boundary-final; original WA enforcement/PRF/blob issues remain. |
| Bytes and raw views | Read-only access is not immutability or owned clearing. Response/extension copies lack a universal cleanup owner; LargeBlob unknown slices borrow serialized input. Unknown value support differs from reconstructive convenience decoding. | C-004/006,E-07..11,RE-R2/3,IMP-027/028/034/037; re-review each specific owner/view boundary. |
| Experimental profile | Draft-10 supplies seed-65700; actual SDK kty/wrapping/dkalg restrictions still differ. previewSign v4 is a vendor Editor's Draft, not CTAP4. Parsing inner attestation is not trust verification or production ARKG qualification. | RC-R1,E-12..16,IMP-029/038..041; pinned profile choice and verifier work deferred. |
| Evidence quality | Fake wire, nonempty signature, sensor metadata, conditional output and test names do not prove full ceremonies/security. Default helper PIN normalization precedes nominal read-only callbacks. | A/B/C/D/E test dispositions, IMP-020..022/042; actual unit run recorded separately from all not-run hardware. |

Full row details and exact paths/clauses remain in the named investigator matrices and focused original
evidence; they are incorporated by reference to avoid divergent duplicate ledgers. Rejected D-08/D-14
subclaims and corrected seed-placeholder interpretation remain visible through leading errata/reviews.

## Stateful sequence checklist

- GetAssertion → GetNextAssertion: sequence termination, interleaving, omitted descriptor/user fields.
- Credential enumeration: begin/next, reported versus collected count, interruption and result ownership.
- Biometric enrollment: begin/sample/status/completion/cancel and notification versus actual interaction.
- Large blobs: read/write chunks, authorization, integrity/encoding, partial failure and observable state.
- PIN set/change/reset: token and protocol-object lifetime, cached metadata versus fresh query, retry/error
  state and recovery claims; no new lifecycle policy inferred from another applet.

## Binary-data and error checklist

For every applicable input/output: representation/length/encoding, missing versus empty, borrowed input
versus owned copy/view, lifetime/disposal, sensitive classification, raw/unknown preservation and strict
malformed/security checks. For failures: CTAP status, APDU/transport, validation, cancellation or callback
provenance; distinguish request transmission, device-side effects and host-side reporting/cleanup.

No failed/cancelled call automatically proves the device made no change. Any ambiguity left after bounded
research belongs in the second-sweep ledger, with exact missing evidence rather than invented guarantees.
