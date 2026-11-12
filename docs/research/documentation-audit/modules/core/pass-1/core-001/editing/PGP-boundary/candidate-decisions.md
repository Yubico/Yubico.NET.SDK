# OpenPGP editing decisions

This is the fresh documentation edit at checkpoint `da66f5d01ba594993974ddd26723e8c300c0a454`,
product pin `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. The completed primary
`boundary-OpenPgp.md` and `boundaries/OpenPgp/*` are read-only inputs. This assignment reuses
their completed source investigation; it is not another 328-unit investigation or independent review.

| Candidate | Applied documentation facet | Retained / held facet |
| --- | --- | --- |
| CORE-PGP-X001 | Direct-factory borrowing, convenience hidden ownership/failure cleanup, backend borrowing, initialization readiness and actual synchronous/asynchronous KDF cleanup order in factory XML, README and contributor guidance. | Wire disposal comments remain accurate. PGP-IMP-003 alias/replacement/reset ownership, held derivation and full-workflow disposal policy stay open; no new lifetime lease is implied. |
| CORE-PGP-X002 | Exact UTF-16 reader units, UTF-8/card-byte distinction and supplied-option bypass in presets/tool XML; full-task input lifetime and channel versus PIN authorization in the interface/README. | Existing Pw/PwStatus/PIN/exception contracts accurately preserve credential identity/status caveats. Distinct-salt and Reset-counter defects remain PGP-IMP-001; validation, transition and error policy are engineering decisions. |
| CORE-PGP-X003 | Local KDF clone/serialization/parser gaps, import/template owner boundaries and returned-response versus transport/fragment/SCP owners. README preserves caller cleanup and states the limits of the existing KDF example. | Existing PIN/crypto finally scopes remain unchanged and accurately documented. Card absent-Le tables refer to B6-C001, not a local encoder fix. Source/transport/chaining/SCP cleanup remains with C/B6 and original applet engineering owners. |
| CORE-PGP-X004 | Complete-public-task versus logical-exchange exclusion, sequential certificate selection, caller-owned prompt/key/input lifetimes, admitted-exchange cancellation, signature/decrypt transfer order and Attest's abandoned-certificate callback risk. | Existing crypto/helper comments remain accurate. No workflow admission, callback policy, certificate cleanup or cancellation behavior changes. Stronger native interruption, reentrancy and complete-workflow drain claims are held. |
| CORE-PGP-X005 | Reset's retained effective version, readiness/authentication state and secure graph; plain SELECT followed by retained secure sends with no handshake. Helper XML distinguishes its separate plain reset from optional SCP action session. | Existing reset/XML/sample qualifications preserve unchecked E6/44 status, partial state, ATT persistence and independent readback. Reset terminality/restoration and physical SCP-reset acceptance remain unresolved. |
| CORE-PGP-X006 | Correct all-non-OK/short-OK/caught-APDU fallback, detected/effective/metadata/AID provenance, no skipped version probe and no inferred OpenPGP SCP firmware floor. | Existing BCD/workaround/feature contracts remain accurate. Broad-fallback policy, major-zero framing, old-family/NFC/firmware and SCP profile qualification remain separate gates. |
| CORE-PGP-X007 | PutKey XML now includes secp256k1 and Brainpool P256r1/P384r1/P512r1 as modeled representations, with separate provider/card/slot admission. | Current model/OID/CRT/KeyRef/wire comments already distinguish representation from capability. Catalog work fixes none of X25519 decipher, ATT read/provisioning, NEO CRT/deletion, ROCA or timestamp defects. |
| CORE-PGP-X008 | Unverify and unauthorized-generation comments name actual assertions; nested test guidance records initialization/disposal/signature/decrypt/cancellation/KDF/import/reset oracle scope and exclusions. | Other accurate test/tool comments are retained with individual reasons in path-decisions.json. Assertions, names, firmware early returns, provider skips, executable help/output and missing tool cleanup remain unchanged. No execution credit is added. |

## Path accounting and owner referrals

`path-decisions.json` contains all 93 original OpenPGP file dispositions, including every
candidate-associated eligible path. Each has an edit/retain decision, original substantive
observation and before/after hashes. Seventeen paths receive documentation edits; retained
source/test paths use the completed packet's observations rather than invented new reading credit.
Configuration and API snapshots remain ineligible and unchanged. `changed-files.json` gives exact
old/new ranges and candidate associations. Where the comment edit tool adds a missing EOF newline,
that whitespace-only difference is explicitly recorded; executable lines/tokens are preserved.

`outside-owner-handoffs.json` gives 25 exact external paths, candidate associations, source
applicability, owner and reopening trigger. Core C owns credential units and TLV copy scopes;
B5/B6 own effective versions, per-exchange guards, APDU response producers, SCP contained owners
and reselect; B1/B4 own SmartCard native lifetime/producer qualifications. SHARED owns its granted
narratives. Migration and Tests.Shared source changes need a separate coordinator assignment.
These are referrals, not changes to those owners' files or duplicate Core findings.

## Preserved identities and stopping point

`preserved-provenance.json` retains the 15 routed provenance keys, 21 PGP-DEP documentation
dependencies and fourteen engineering group identities. Their original definitions/statuses
remain in the primary records. The dependency labels are not new Core bugs. In particular,
PGP-IMP-001 (PIN/KDF identity), PGP-IMP-007 (X25519 framing) and PGP-IMP-013 (ATT public read)
remain confirmed engineering findings for their historical recorded scope. No documentation
change resolves their implementation, policy, source/provider or physical qualification facets.

PIV has not been released to this editor and is untouched. PIV physical touch/hotplug/recovery
deferrals and the user's unready/unreviewed Windows concurrency future handoff remain intact.
Next owner: coordinator and independent full-unit/citation reviewer, then the existing final
validation/publication process. Finish Core Pass 1, provide the deferred handoff, and stop.
