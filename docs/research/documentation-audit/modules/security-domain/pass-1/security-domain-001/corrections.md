# Security Domain corrections and rejected inferences

2026-11-04. Original reports remain unchanged; this table records authoritative dispositions.

| ID | Issue / evidence | Decision / current state |
| --- | --- | --- |
| SD-ERR-001 | SD-C001 proposed changing the test-instructions testing link from three to four parent traversals. Editor's path check rejected four levels. | Rejected investigation inference. `src/SecurityDomain/tests/../../../docs/TESTING.md` is the correct repository path. Original three-level link retained. No product correction remains for this point. |
| SD-QA-001 | Initial docs-qa failed on `src/SecurityDomain/tests/README.md:87`: human-facing docs must not link agent instruction files. Full failure retained in validation/initial-docs-qa.log. | Corrected and independently accepted in review-followup.md: human testing link and practical successive-session guidance. Final repository gate still separately required. |
| SD-R1 | Independent original PDF check places certificate-chain serial filtering in §7.5.1, not §7.5.4. | Required two product citation corrections accepted; original investigation/source record pointers superseded by this entry and evidence/review-sources.md. Preserve draft/status qualifications. |
| SD-R2 | Certificate integration assertion compares Thumbprint strings, not RawData identity. Coordinator read the entire assertion/setup section. | Required comment correction accepted; assertions unchanged. |
| SD-COORD-001 | Review source note identifies draft §4.6 prohibiting SCP11c PUT KEY/DELETE keys/SET STATUS. Coordinator independently fetched original PDF and read the complete Session Replay passage, confirming the prohibition. Generic “SCP03 or suitably provisioned SCP11a/c” administrative wording can obscure command restrictions. | Clarify README and interface type remarks: shown administrative route is SCP03; SCP11 permissions are command-specific, and cited draft restricts SCP11c key import/deletion. Keep final/YubiKey applicability gap; no new runtime enforcement or blanket support assertion. Later profile verification joins SD-IMP-003 / SD-DEP-001. |

All four product corrections (SD-R1, SD-R2, SD-QA-001, SD-COORD-001) were applied in four files and
independently accepted in [review-followup](review-followup.md). Other 22 authored paths retain their
accepted hashes. Exact transformations are in [edit-corrections](edit-corrections.md). No runtime fix,
assertion change, Core investigation or hardware outcome is implied.

## Coordinator original-source check

On 2026-11-04, Exa fetched the original August 2023 SCP11 v1.3.0.13 Public Review PDF (same URL as
evidence/sources.md E2; 180000-character extraction). Read complete §4.6 Session Replay and the §7.5.1
definition / §7.5.4 processing-rule distinction. §4.6 says: “The usage of PUT KEY, DELETE [key(s)], and
SET STATUS commands shall not be allowed.” Actor: SD accepting an SCP11c session under this draft;
not an automatic claim of YubiKey runtime behavior or final-version applicability. It explains replay
is possible for SCP11c because key randomness depends on the OCE; SCP11a/b use fresh SD ephemeral keys.
§7.5.1 is the correct certificate-chain checking subsection; §7.5.4 discusses BF20 authorizations.

## Scope disclosure

The reviewer reports an incidental Core README heading read by its generic link-anchor script; no Core
behavior/source investigation or edits followed. This exceeded the intended document-read boundary;
subsequent checks use path existence for external targets and SD-local anchors only. Do not claim that
no Core document was opened anywhere in the run. Dedicated boundary reviews remain deferred.
