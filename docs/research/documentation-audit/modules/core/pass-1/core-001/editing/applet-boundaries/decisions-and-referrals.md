# Candidate decisions and deferred handoff

Documentation decisions below apply only to this edit batch and await independent review. All
engineering (P), maintainer-policy (M), unrun-evidence (E) and qualification (Q) facets remain held.
No accepted canonical Core finding is allocated. Original local IDs and source-run aliases remain
authoritative; shared mechanisms receive caller mappings, not duplicate findings.

## All local candidate groups

### Management — CORE-MGMT

| ID | Documentation decision | Retained hold / trigger |
| --- | --- | --- |
| X001 | Edit AdvancedTests fixture provenance, scoped selected-field/equality rationale and fresh serial-lookup explanation | `management-001:MGMT-XREF-003`, A-C003/C013/C017/C021: cache/storage/serial/fixture changes reopen exact callers; no fixture runtime bug allocated |
| X002 | Edit factory/read XML, README/CLAUDE, DeviceInfoResult and TimeoutsMenu provenance/alias scope | A-C003/C016 storage/raw representation; B4/B6 copy, phase and native qualifications stay open. Re-review after decoder/owner/initialization changes |
| X003 | Edit unsupported-connection test comment: directly constructed ProbeSession never binds and cannot detect a stale holder | Future test owner must use a genuinely bound probe and exact no-I/O/cleanup assertions; existing assertions/names unchanged |

Management's [B6 follow-up](../../boundaries/Management/followup-B6.md) and
[B4 completion](../../boundaries/Management/boundary-completion.md) supersede earlier pending-return
prose for source investigation only. Preserve `management-001:MGMT-IMP-004` undisposed Defaults and
fallible cleanup, `MGMT-IMP-008` unenforced read floor, `MGMT-IMP-009` malformed filter and
`MGMT-IMP-010` family/reset/configuration-lock lifecycle decisions. `MGMT-XREF-001/002` retain A-C004/C005
mapping/form-factor qualifications; no runtime output/filter/assertion changes were made.

### SecurityDomain — CORE-SD

| ID | Documentation decision | Retained hold / trigger |
| --- | --- | --- |
| X001 | Edit immediate SCP11 version prerequisite, typed README creation and reset/configuration provenance | `security-domain-001:SD-DEP-003`, `SD-IMP-005`: retained-reset options/version/parameter reuse and failed-reinit policy remain unresolved |
| X002 | Edit contained-object ownership table and factory/contributor scopes; retain accurate rotation/private-import examples | SD-DEP-002/SD-IMP-008 and B6-C007/C009/C-C020: record aliases, provider storage, KCV/encrypted temporaries and curve-byte engineering remain held |
| X003 | Edit GET DATA borrowed input/result/hidden-owner remarks and caller-copy distinction | SD-DEP-004/SD-IMP-008, B6-C002: owned-response API/parser cleanup requires a selected policy and real-owner oracles |
| X004 | Edit raw-reset completion rationale, failed/cancelled reset disposal guidance and local version failure | SD-IMP-001/005/B5-C006: raw attempts, old flags, full-workflow admission and actual restoration remain held |
| X005 | Edit diagnostic gate/status/nesting/provenance explanation; retain already precise selected catch XML | SD-DEP-005's old formatter-payload claim is not adopted. B6-C014/C-C008 own exception/provider logging policy; catch/runtime text unchanged |
| X006 | Edit a/c first-MAC phase and inherited flag meaning; narrow c-test comment to protected public listing | SD-DEP-001/002, SD-IMP-003/007, B6-C009/C010/C013: final/vendor permissions/trust/positive controls remain open |
| X007 | Edit GET DATA actual short/extended Le bounds and STORE logical-command fragmentation | B6-C001/C003/C011/C012: empty-command encryption, exact ISO/profile and protected continuations remain shared holds; backend forwarding retained |
| X008 | Correct c-test oracle only; retain existing bounded test-guide/fixture statements and useful parameter-owned examples | SD-IMP-007/008: undisposed test inputs/exported D, CTS/cleanup/fake limitations need separately authorized executable work |

No implicit a/c provisioning recipe was added: the role table and concrete contained-owner actions
remain safer than inventing certificate/trust helpers. `GetCertificatesAsync` ownership/trust and
source-resolved failure cleanup are retained, not reopened by the generic Core copy concerns.

### Oath — CORE-OATH

| ID | Documentation decision | Retained hold / trigger |
| --- | --- | --- |
| X001 | Edit IOathSession state/Salt lifetime and local README/CLAUDE distinction | B5-C001 owns common query-state semantics; changing terminal getter policy is separate API work |
| X002 | Edit whole-task lifetime near aggregate/retry/presence callers and guides | B5-C006/B1 lifetime mechanisms; callback/aggregate gap experiments and future workflow leases remain unrun/unselected |
| X003 | Edit secure-reset fake commentary; retain terminal reset, borrowed connection rules and nested test qualifications | B6-C011/C012 and C002/C005/C006/C007/C009/C010; no live SCP terminal-reset proof, no reopening fixed IMP-010 |
| X004 | Edit console preset UTF-16 units/UI-versus-byte/firmware scope; retain correct borrowed password/key cleanup | C-C001–C004; `oath-pilot-and-fixes-wave-002:DEC-002` raw-key/normalization policy stays open, unrelated to CTAP DEC-002 |
| X005 | Edit direct-BCL PBKDF2, nonempty unpadded Base32 and exact 6A82 factory-reuse descriptions | Test CTS/raw PUT/DELETE/first-error/borrowed-fake cleanup debt remains separate from the fourteen fixed product IMP scopes; DEC-006 oracle limit remains |
| X006 | Edit SmartCard-capable selection, process/known-member exclusion and reset fragment's typed ct prerequisite | All versus scoped discovery is application policy; actual OathTool remains All/first eligible. Shared migration callback ownership is referred, not edited |

Preserve all fourteen historical OATH IMP resolutions, including input/seed/Base32/raw-response and
terminal-reset scopes. `oath-pilot-and-fixes-wave-002:DEC-001` broader verification-failure recovery,
DEC-002 raw-key scope and DEC-007 5.6.3 policy rationale retain their separate open dimensions. Generic
Core findings do not reopen source-fixed applet behavior or establish live SCP execution.

### YubiOtp — CORE-OTP

| ID | Documentation decision | Retained hold / trigger |
| --- | --- | --- |
| X001 | Edit full workflow/configuration/prompt scope and bounded process-local exclusion | OTP-DEP-001/003, B1-C005/C008 and B5-C004/C006 remain shared native/workflow holds |
| X002 | Edit first/second report and effective-version provenance; local polling versus native duration | OTP-DEP-002/004, OTP-IMP-005/006 and B5-C001/C007/C008: malformed custom provider admission, NEO catch and polling policy remain engineering decisions |
| X003 | Edit final/prefix/caller output versus lower-copy limits; retain accurate callback-before-transfer behavior | OTP-IMP-003, B5-C005/B4-C007/B6-C002/C-C008: report/staging/response/decrypt and throwing logger paths remain held |
| X004 | Edit one-shot HMAC XML/guide: output transfer waits for teardown; teardown failure can strand output | Extend `yubiotp-001:OTP-IMP-003` outer-producer facet. Result retention/zeroing and exception precedence require code/fault-injection scope |
| X005 | Retain existing accurate source/config/GetConfig/invalid-code cleanup qualifications and sample warnings | OTP-IMP-003/010: acquisition-before-parse/try and unified CLI config lifetime remain engineering work; no unrelated sample rewrite |
| X006 | Edit raw/effective/status/override provenance, preserve support and presence-heuristic conditions | OTP-IMP-004/005/006/012 and B5/C: no new support matrix, no inferred parity/FIPS/major-zero guarantee |
| X007 | Edit static-password layout qualification; retain standalone sample defect versus working SDK/unified translation | OTP-IMP-009/010, `audit-v2-20261022-20261031:N3`, B4-C010 and C input ownership stay distinct; physical keyboard output remains unqualified |
| X008 | Add named task guidance: Management device-info/modern TLV config versus absent typed FIPS query and legacy mutators | OTP-DEP-005; SHARED owns active migration/map/gaps updates. No typed API addition or raw recipe qualification |
| X009 | Correct default-flags/CRC test comment; retain assertion-accurate test descriptions and old-device/HID KAT scope | OTP-IMP-011/B5-C008: held-fake teardown/protection cleanup and independent CRC/custom-report/one-shot failure oracles remain held |

All `yubiotp-001:OTP-IMP-001–012` retain original applet scope. Historical serial 103 results do not
qualify older firmware, NFC, physical keyboard layouts, new Windows work or all devices. The one-shot
sample's finally clears only output actually returned to its caller; the newly documented exceptional
teardown limitation is not silently repaired or generalized to public metadata one-shots.

### YubiHsm — CORE-HSM

| ID | Documentation decision | Retained hold / trigger |
| --- | --- | --- |
| X001 | Edit reset/current-graph/no-transaction/no-handshake and historical ready/auth flag explanations | HSM-DEP-001/002, B5-C001/C006 and B6-C011/C012/C015: reset policy, post-reset protected use and actual recovery need separate selection/qualification |
| X002 | Edit final-response helper/guide cleanup scope; retain precise SessionKeys.Parse/caller-array XML and actual-owner tests | HSM-DEP-003/HSM-IMP-002 and C/B6: helper currently reaches real owner. Replacing it with Clear and fixing earlier copies requires code scope |
| X003 | Edit full workflow/input/prompt lifetime near LIST/CALCULATE and constraints | HSM-DEP-002/B5-C006/C presence/logging: held callback/gap schedules remain unrun; detached work is application-owned |
| X004 | Edit SELECT/fallback/override provenance, including comment beside the tool's unchanged version output label | HSM-DEP-001/A-C015/B5-C001: actual version/device/profile and reset-version semantics stay qualified |
| X005 | Retain all current strict-48 versus supplied-cryptogram-56 qualifications, typed application adapters and host/HSM actor guidance | `yubihsm-001:HSM-IMP-008` and HSM-IMP-007: tail meaning, production profile, accepted grammar and result/receipt ownership remain open; no truncation workaround |
| X006 | Retain accurate context/point/trust/LIST/GET_CHALLENGE/retry distinctions | HSM-IMP-001/003/007: independent valid/invalid profile vectors and actor-specific status evidence remain needed |
| X007 | Narrow exception XML to retry APDU-status failures; retain sample diagnostics and bounded test oracles | HSM-IMP-002/004/005/006 remain deferred. Final retry-message retraction honored: helper and asymmetric assertion both say `Invalid credential password`; no mismatch edit or historical reopening |

## Actual caller-to-shared-mechanism referrals

These are secondary uses of existing Core groups. The [path ledger](path-decisions.json) retains exact
original unit IDs and hashes; [outside-scope paths](outside-scope-paths.json) retains packet exclusions.

| Existing owner / groups | Actual caller sites and precise reopening trigger |
| --- | --- |
| A-C003/C004/C005/C016/C017/C021 | Management GetDeviceInfo/DeviceInfoResult/TimeoutsMenu/scoped equality and fixture descriptions. Reopen after timeout/raw/firmware/FIPS/form/cache representation changes; SHARED owns Core metadata XML and `docs/migration/v1-to-v2.md` metadata/configuration entries |
| B1/B4 native lifetime and C007 staging | Management initialization/C2/C3 and OTP/HMAC HID paths. Reopen Windows report/task/listener/registry/discovery statements only after the user-ready pinned delta and actual completion/drain/release evidence; no new native/device claim |
| [B5-C006](../../investigation-B5.md) | Management per-page reads; SD CA/reset raw work; OATH aggregate/retry/presence; OTP write/status/CRC; HSM LIST/CALCULATE/callbacks. Core/shared owner rechecks ApplicationSession/ExchangeGuard, raw tiers and `docs/usage/user-interaction.md` after any workflow admission/disposal change |
| [B6-C002/C005/C006/C007/C009/C010](../../investigation-B6.md), [C containment/input/logging](../../investigation-C.md) | Management borrowed factory parameters/encoding; SD retained parameters and hidden response owner; OATH cleared applet response plus earlier copies; OTP prefix/one-shot result; HSM final response versus three owned keys. Reopen exact borrower/owner/phase/copy/error docs after selected implementation changes |
| B6-C001/C003/C011/C012/C015 | Empty Management metadata/reset, SD GET DATA and STORE fragmentation, OATH LIST/RESET/A5, HSM reset/reselect. Core wire/profile decisions reopen caller XML/guides; no duplicate applet-level conformance defect allocated |
| C-C001–C004 / B4-C010 | OATH credential UI preset and actual CLI reader; OTP static-password translator versus standalone UTF-8 defect. SHARED/Core owners retain reader/keyboard XML, `docs/usage/user-interaction.md` and migration static/password examples |
| SHARED migration/editor | `docs/migration/v1-to-v2.md`, `docs/migration/v1-to-v2-map.yml`, `docs/migration/v1-to-v2-gaps.md`, `docs/v1-to-v2-comparison.md`, `docs/v2-highlights.md`: apply named OTP relocation resolution; SD SCP11 version/retained-reset/actor distinctions; OATH reset/retry owner scopes; HSM reset/profile limits. Existing DOC-002 YAML parsing issue stays separate |
| External C# owners | Core protocol/session/response/key/credential/native code, Tests.Shared helper options/fixture/disposal comments and unified CLI static/config/input code are read-only referrals. Apply the same actual-caller scopes after each selected mechanism change, not a second applet audit |

OpenPgp/Piv, Ctap2/WebAuthn and all Core/shared files were not edited. PIV physical touch/hotplug and
touch-cancellation deferrals remain. No queued hardware work, connector experiment, native validation,
Pass-2 implementation or extra tooling project is authorized by this handoff. Finish the coordinator's
Core Pass-1 publication/deferred-finding handoff, then stop.
