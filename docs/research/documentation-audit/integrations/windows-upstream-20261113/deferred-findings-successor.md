# Windows integration: affected deferred findings

This is an additive disposition map for the merge of audited parent
`d5d01ef99f1478e4d49911194c17667b2450d2fd` and incoming
`e322712d609aaf03b9bd1cf2964a99326728866a`, with natural base
`5746e72ed9a848d08b5782ddde025aa4a4b522ef`. It does not replace the frozen Core001 findings,
accept a new defect count, or start their implementation pass.

**The integrated software scope is verified; platform closure is not claimed.** Independent source
and documentation reviews accepted product tree `05d4409729dd81c7717c99a15a2697d49eb10906` without
Critical or Important findings. The [software results](validation/summary.md) record actual Linux-hosted
build, unit, portable guard, lifecycle, formatter, complexity and package checks. Final narrative
reconciliation changes no C# source. Native, hardware and cryptographic qualification remain bounded
by the original source-specific evidence; final publication review is separate.

## Sources and disposition meanings

The original definitions and reopening conditions were read from the frozen
[candidate reconciliation](../../modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json),
alongside the [deferred handoff](../../modules/core/pass-1/core-001/deferred-findings.md), including
its protected applet, source and physical qualifications. The published baseline is
`core-001`, audit commit `bff374eb533f9ec03deecc04561aa286c050efa6`, product source pin
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. None of those frozen records was edited.

Delivery and historical execution evidence come from the
[intake review](intake-review/report.md), especially sections 2–5, and the
[coordinator decisions](coordinator-decisions.md). The [resolution record](execution/documentation-resolution.md)
identifies the active documentation changes and the ten clean-overlap decisions. The separate
[source resolution](execution/source-resolution.md), [source review](review/source-review.md) and
[software results](validation/summary.md) establish the integrated software scope. The tables preserve
their preparation-time descriptions of proposed source dispositions and pending tests; the completion
boundary below records the subsequent acceptance without converting managed tests into native closure.

- **Resolved** applies only to the named documentation mismatch in the candidate. It is not a runtime
  or platform closure and still requires independent content review.
- **Superseded** means the incoming implementation replaces the specific old mechanism. The historical
  observation remains valid for its source; the new mechanism needs its own validation.
- **Rescoped** means part of the original question moved or narrowed, while the named remainder stays open.
- **Open** means this delivery supplies no evidence that closes the original implementation, policy or
  qualification question. Renaming a file or linking a test does not close it.

## Windows lifetime, buffers and ABI

All IDs in the tables are qualified as `core-001:<ID>`. References identify existing candidates and
facets, not newly accepted defects.

| Original ID / affected facet | Disposition in this candidate | Evidence and remaining boundary |
| --- | --- | --- |
| `CORE-B4-C001` — built-in synchronous Windows route and early registry release | **Superseded**, proposed for the old built-in mechanism; its active documentation mismatch is **resolved** | Incoming removes `WindowsHidReportAccess` and routes built-in FIDO/OTP through separate rooted native owners. `HidConnectionSlot`, `YubiKeyDevice` and `SharedDeviceInfoRead` transfer the original claim before native acquisition. FIDO joins submission return with callback resource-last-use, then retirement and checked close; OTP drains its synchronous feature call before checked close. The intake names held-operation/open/close and preparation-failure tests. Those are available test assets, not a pass on this candidate. Actual Windows HID-driver races, unknown-release retention and separately scoped platform acceptance remain to be qualified. |
| `CORE-B1-C005` — registered HID disposal failure | **Rescoped** | Built-in Windows joins macOS/Linux owner-transfer routes rather than the registered compatibility wrappers. The arbitrary custom-provider failure-release policy remains unchanged and open; a custom disposal outcome is not native release proof. The retained external-disposal-failure assertion must not be reinterpreted as a new checked-release guarantee. |
| `CORE-B1-C007` — active ownership/wake/claim descriptions | **Resolved** for the obsolete Windows/custom wording; broader qualifications remain **open** | Root/Core guides, raw tiers and the ownership guide now name the Windows owners and their terminal-wake shutdown. They retain known-member/process bounds, incremental acquisition, whole-workflow sequencing and indefinite native waits. This is a documentation reconciliation, not evidence that every listener or custom implementation drains. |
| `CORE-B4-C007` — native Windows staging and successful short I/O | **Superseded** for the removed report-access implementation; **rescoped** to new owners/APIs | Incoming Windows owners/APIs clear owned staging after actual last use, return independent successful receive storage and check exact FIDO count/report-ID framing. The separate HidD feature API exposes no actual transfer count; do not invent one. Source-owner confirmation and execution of actual-buffer/failure/count tests are pending. Caller results, protocol copies, applet copies and native/BCL resources remain separate owners; this is no end-to-end erasure claim. |
| `CORE-B5-C005` — consumed received-report copies | **Open** | New Windows native staging cleanup does not clear the independently returned report arrays consumed by FIDO/OTP protocol accumulators. The connection-to-protocol transfer/clearing contract, external aliases and retained-array observations still need their own decision and evidence. Clearing a later result or clone does not establish source-report erasure. |
| `CORE-B4-C003` — Windows ABI | **Rescoped** | New `GetHid*`/`SetHid*`/`FreeHid*` report imports use byte returns and uint lengths, and the new APIs reinterpret signed HIDP_CAPS storage as ushort. Older Bool-return HidD imports, Cfgmgr32 Guid/PULONG declarations and other signed-length consumers remain open. No all-import repair or x86/x64/arm64 JIT/AOT acceptance follows. |
| `CORE-B4-C004` — access/elevation conditions | **Resolved** for the active unconditional platform-note wording; access qualification remains **open** | The ownership guide retains zero-desired-access OTP and read/write FIDO without promising universal permission sufficiency, keyboard denial or elevation requirements. ACLs, collection/driver behavior, token and existing sharing policy still govern success. Metadata is not feature-I/O proof; FILE_SHARE.ALL is not a grouped claim or an override of other handles' restrictions. Elevated direct-USB and standard-token RDP/SmartCard evidence are different paths. |
| `CORE-B4-C008` — harness and platform claims | **Rescoped** | The new Windows verifier and registries add bounded source/artifact and kernel/BCL pipe evidence. At `bdac2f4f4224fa0ccf92006971af8659736aa2b1` and `0f10775cecfb61f74d20851417746a5816863bb7`, each retained unsealed JIT bundle independently records 10 scenarios / 65 checks and a verified 207-file artifact map. Neither is sealed/AOT or HID-driver evidence. The unexplained first-open failure and original attended 17-pass/1-fail aggregate remain. Historical benchmark, loaded-producer and broader harness limitations are not closed. |

## Extracted adapters and unchanged ownership questions

| Original ID / affected facet | Disposition in this candidate | Evidence and remaining boundary |
| --- | --- | --- |
| `CORE-B4-C002` — Windows listener unregister/start/callback lifetime | **Open** | Report owners do not repair the listener's unregister-result, partial-start, disposed-restart or callback-exception questions. New event-identity assertions are not successful unregister/drain evidence. A quiet test without confirmed Started status is not native registration acceptance. |
| `CORE-B3-C005` — macOS listener partial startup | **Open** | The FIDO/OTP report-owner extraction leaves listener manager/Generation/Thread/register/start/logger-failure scopes separate. Report-route passes do not prove no-thread cleanup or safe release of ambiguous callback roots. |
| `CORE-B3-C008` — native source/package/runtime identity | **Rescoped** | Internal names move to `MacOSHidInputBridge`, `MacOSHidReportApi` and the two native owners. PR774 reports macOS arm64 JIT FIDO/PCSC passes at `0f10775cecfb61f74d20851417746a5816863bb7`, macOS 15.7.7, NativeShims 1.18.1, authorized serial 125. Raw Mac artifacts are unavailable locally; the loaded image path was not observed. OTP permission and expert-facade permission/topology blocks, AOT, listener/touch/removal, producer/build correspondence and later-source qualification remain open. Source publication does not identify a loaded binary. |
| `CORE-B1-C006` — transaction-end reporting and test oracle | **Open** | `PcscConnection` / `PcscConnectionNativeOwner` replace internal names, not the policy. A failed native end closes admission and later connection disposal reports the stored error independently of release proof. The retained scope-end test holds transmit, not native EndTransaction itself. No stronger oracle or earlier error propagation is claimed. |
| `CORE-B1-C009` — post-open logging cleanup | **Open** | Renaming the PC/SC connection/logger category does not repair slot Information logging after a successful built-in or external open. Native cleanup and the original claim must still be considered separately from diagnostic failure. |
| `CORE-B1-C010` and `CORE-B3-C006` — ambient context policy | **Open** | PC/SC and macOS owner extraction is not an approved change to Thread/ExecutionContext policy or an AsyncLocal observation. Windows/Linux context suppression supplies no automatic resolution for these other workers or listeners. |
| `CORE-B2-C003` — libudev strings/unref | **Open** | Linux seam/event-source splits retain UTF-8 string-copy and borrowed-parent ownership qualifications. The source contracts checked against systemd v257 are not every installed libudev binary; no native return-type alignment or non-ASCII runtime probe was performed. |
| `CORE-B2-C004` — unversioned libudev prerequisite | **Open** | Moving Linux declarations does not change `libudev.so` resolution or prove a minimal runtime-only Ubuntu deployment. Stable-soname/resolver policy and the conditional empty-discovery outcome remain separate deferred work. |
| `CORE-B2-C005` — assertion-failure harness drain | **Open** | Existing successful held-native tests and new registry links do not repair every failing-assertion late-open/result-cleanup path. No red-path experiment, stress lane or harness repair is authorized by this map. |
| `CORE-B1-C012` — grouped admission precedence | **Open**, retained without policy change | Claims are still sorted, deduplicated and acquired incrementally. A discovery wait on an earlier member can precede checking a later live holder. Windows pre-open transfer must not be described as atomic whole-set preflight or immediate refusal at invocation. |
| `CORE-B4-C005`, `CORE-B4-C006`, `CORE-B4-C009` — loader, PC/SC ABI/string and backend errors | **Open**, adjacent dependencies only | The delivery does not repair dormant loader binding/search/lifetime concerns, installed PC/SC producer/string/LP64 PCI questions, or shared error constants/backend interpretation. PC/SC API extraction and successful bounded SmartCard scenarios do not establish those separate contracts. |

## Workflow, examples and protected PIV scopes

| Original ID / affected facet | Disposition in this candidate | Evidence and remaining boundary |
| --- | --- | --- |
| `CORE-B5-C006` — exchange versus operation lifetime | **Open** | Windows applet cross-layer tests exercise selected Ctap2/YubiOtp journeys through actual slots/owners under controlled seams. They add no whole-operation lease, callback tracking or universal self-disposal policy. Await and serialize complete public tasks; exchange drain does not cover later parsing, cache updates, callbacks or later exchanges. |
| `CORE-B6-C015` — SCP task/reselect qualifications | **Open** for policy/profile/runtime facets; accepted documentation retained | Audited raw/SCP examples retain selection before SCP, parameter/source ownership and response-owner clearing. Plain SELECT after SCP retains its graph and is not recovery. Original Core001 example compilation remains historical; any changed or rename-dependent context needs a new source-bound result. Exact SCP03 Public Release/Public Review and SCP11 Public Review/final/vendor distinctions remain. |
| `CORE-PIV-X006` / `piv-001:PIVIMP-006` | **Open** | The PIN-only test-pool rollback does not repair PivTool/CLI pre-try PIN/management cleanup, generated/KDF source arrays, certificate/result ownership or cancellation/workflow boundaries. Frozen sample anchors `a579b6a5cf4242e4` and `8fc815177a95b970` keep their original aliases; no duplicate implementation finding is created. |
| `piv-001:PIVIMP-005`, carried by `CORE-PIV-X006` | **Open**, confirmed/unfixed at its historical scope | Tool RSA original-message verification and Ed25519 SHA256(message) framing findings are not fixed by response-only tests or connection renames. Direct SDK controls and their original fixture/source limits remain distinct. |

The PIV rollback is a separate received test correction, not a new cleanup implementation finding.
Coordinator authority identifies committed rollback `030b2a448…` and restored production
`AuthenticationProtocol.cs` blob `526c5da214546c1655ab8f36e2f7e95831cfdbf0`. Incoming tests retain
independent owned-response clearing, recovery candidates and authentication/witness outcomes.
They deliberately do not read returned pool memory, and there is no replacement pre-return pooled-command
clearing runtime oracle. No withdrawn pool seam is reinstated. Missing byte-original private discussion
is not an extra execution prerequisite under the coordinator's decision.

PIV physical PIN isolation, observed power cycles, separate-process access/release, touch/hotplug and
touch-dependent cancellation/recovery remain separately qualified or deferred. Normal PIV reset retains
the F9 attestation key and certificate; this integration authorizes no replacement or experiment on that
material. Serial 9681620's historical two-key failure remains unexplained, and serial 125's two `6A88`
results remain pre-EXTERNAL-AUTHENTICATE/RESET observations.

All fourteen scoped OATH IMP resolutions, resolved User/descriptor prose, retracted HSM retry-message
claim and unrelated crypto/profile/Rust/producer gaps retain their existing dispositions. The PIV CLI
and OpenPGP certificate anchors under SAMPLE-A004 and the OTP pre-try cleanup anchor `4089aedab94d4e23`
remain separate caller evidence, not closures produced by this delivery.

## Coordinator completion boundary

The incoming inventory README at `e322712d609aaf03b9bd1cf2964a99326728866a` reports 245 source
sites, 59 operations / 104 links and 31 public entries; its consistency mapping preserves 229 full
identities and migrates 16 from `bdac2f4f4224fa0ccf92006971af8659736aa2b1`. These received static
counts are not a prescribed result for this combined tree. L6a's 43 operations / 76 links and older
219-site snapshot remain historical. No inventory status is promoted from outstanding to verified.

The source-owner receipt and independent reviews confirm the new Windows mechanisms and retained
audit contracts. The ordinary suite passed the managed owner/routing/facade, actual-buffer/failure/count
and applet cross-layer tests. Thus `CORE-B4-C001`'s old built-in mechanism is superseded and the
`CORE-B4-C007` source/managed-test prerequisite is satisfied. Neither disposition closes native
Windows HID-driver races, unknown-release platform acceptance or end-to-end erasure questions.

The actual Core scan and registry tests passed in the ordinary suite. Static reconciliation counted
245 classified sites, 59 operations / 104 unique profile links and 31 public entries without adding
links to executed-case totals. Every family retains `outstanding`, not `verified`. Unchanged example
bodies required no new snippet compilation. See the [software summary](validation/summary.md).
All other open/rescoped and protected PIV facets above remain as recorded. Platform acceptance stays
a separately authorized lane; no historical Windows or Mac result is relabeled as execution on merged
source. Publication belongs only to the new integration branch, not the original checkout or frozen audit.
