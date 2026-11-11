# Core Documentation Pass 1 Implementation Plan

**Goal:** Complete the final whole-module documentation audit and all deferred applet/Core boundary reviews against a freshly reconciled `yubikit` baseline, then close the campaign's first sweep with evidence-backed implementation handoffs.

**Architecture:** Establish and publish a separate non-rewriting upstream integration checkpoint before auditing Core. Fresh Sol 6.1 sessions investigate, edit and independently review; Astra coordinates, adjudicates, samples, executes validation and publishes. Documentation completion, implementation completion and hardware/platform qualification remain separate outcomes.

**Tech Stack:** Current repository .NET/C# toolchain, managed/native device discovery and transport lifetimes, SmartCard/APDU/SCP and HID protocols, cryptographic and encoding utilities, original standards and OS API sources, pinned Python YubiKey Manager source/tests/history.

---

## 1. Authority and present state

The user selected Core as the final Pass-1 target on November 11, 2026 and requested this planning document with the rigor of the preceding successful cycles. **Planning is active; execution requires switching out of Plan mode.** The current session may write only beneath `/home/rupicola/.opencode/plan`. Reconciliation, repository records, audit edits, builds and device operations belong to execution.

The intended work is upstream reconciliation, whole-Core documentation, dedicated reviews of every applet's relationship with Core, and first-sweep closeout. Selected Pass-2 implementation work follows that closeout. OATH's earlier implementation exception supplies no general permission to fix new findings during Pass 1. Backward API compatibility is not a design goal for this breaking release; new behavior/API changes still require their own implementation scope.

### Fresh read-only observations

| Item | Observed value |
| --- | --- |
| Repository | `/home/rupicola/Code/y/Yubico.Yubikit.NET`, remote `Yubico/Yubico.NET.SDK` |
| Campaign local/GitHub tip | `f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa`, `yubikit-docs-audit` |
| Latest observed upstream tip | `5746e72ed9a848d08b5782ddde025aa4a4b522ef`, `yubikit` |
| Previous integration baseline | `727fe43afc430c5d8364d868a6436de50626e476`, containing PR #745 at `b1362127203beda5e9c797e8648b28e2716c496c` |
| Working tree | Clean tracked tree; four original untracked files remain |
| Frozen campaign evidence | All **5,366 manifest-listed artifact identities** verified, including PIV's 1,965 |
| Recent upstream examples | PR #770 Linux async HID ownership/lifetimes; PR #755 `ProtocolDeviceInfo` responsibility split; discovery/monitor and documentation changes |

Fresh Sol read-only upstream reconnaissance and Astra spot checks establish:

- Unique common ancestor: `b1362127203beda5e9c797e8648b28e2716c496c`; campaign/upstream-only commits: **30 / 74**.
- Common-base-to-upstream net delta: **287 name-status entries** (164 modified, 91 added, 30 deleted, two renamed); **146 Core entries**. These are net-diff entries, not audit units or merge conflicts.
- Whole upstream Core tree: **513 tracked files / 496 C# files**, including 315 shipping-subtree files and 195 test-subtree files. Campaign Core currently has 471 files / 455 C# files. Recount after integration.
- Upstream public snapshot: **1,439 nonblank/non-directive entries** and 159 simple type-declaration lines. Effective API families and generated members still require semantic reconciliation.
- **45 paths changed on both branches**, computed from common-base path-set intersections with rename detection disabled. Actual merge conflicts have not been determined.
- No upstream changed paths under `docs/research/documentation-audit/`, `docs/archive/` or `docs/completed/`; preservation still requires exact post-integration checks.

**PR #745 identity clarification:** a fresh GitHub query reports `mergeCommit.oid = b1362127…`, `state = MERGED`, `baseRefName = yubikit`, `mergedAt = 2026-11-04T23:27:12Z`. That Git commit itself merges catalog changes into the PIV lineage. Both descriptions can be true: use GitHub's PR association and actual Git ancestry separately. The read-only investigator's suggestion that it was not the PR merge identity was rejected after this API check; earlier frozen PIV provenance remains valid.

The observed upstream commit is an orientation point, **not the future integrated audit baseline**. Re-query at execution and record the chosen pin. Upstream objects were already available locally; planning performed no fetch, merge, checkout, build or hardware work.

Preserve these original files and their recorded hashes:

- `docs/research/audit-v2-closeout-review.md`
- `docs/research/audit-v2-execution.md`
- `docs/research/audit-v2-jira-traceability.md`
- `mise.toml`

Numerous other worktrees exist, including discovery, monitoring, Linux async and platform-verification work. Their branch names or recorded heads do not establish that their owners have finished. Inspect ownership/state before integration; leave their files and lifecycle alone. Existing prunable entries are not a cleanup task for this audit.

Read-only worktree checks found modified work in the historical CTAP/Management editing worktrees and `/tmp/opencode/pr770-clean-publish`, plus an untracked entry in the OATH SCP reference worktree. Use a new integration location rather than repurposing any of these.

### Concrete upstream reconciliation map

Treat these as source-derived planning observations, to revalidate at the chosen execution pin:

- **Discovery/monitor API:** fresh independently scoped `Find…Async` calls with `interfaces`; last-known nullable `CachedDeviceInfo`; owned `CreateMonitorAsync`/`YubiKeyMonitor`; `DeviceChange` records with snapshot completion and revisions. Global monitor lifecycle APIs and the old repository/event-hub/service architecture are removed. Review cached/fresh wording, named arguments, replay/empty-state examples and disposal end-to-end.
- **Admission and lifetime:** `DeviceChangeFeed`, `SnapshotDiff`, `HotplugSignals`, `RescanScheduler`, discovery budgets/read scopes/admission and workers. Document per-attempt waits separately from native work and total duration. Review one-shot transport omission versus failed monitored scans preserving the last publication; do not carry the old generation/restart/publication-gate explanation forward.
- **Metadata responsibility split:** `DiscoveryDeviceInfoReader`, `SharedDeviceInfoRead` and `ProtocolDeviceInfo` divide bounded waits, owned native/read lifetime and protocol reading over a borrowed connection. Start at upstream `src/Core/src/Devices/README.md` and trace actual ownership.
- **Linux native lifetime:** separate FIDO/OTP owner workers and readiness reactor, descriptor `int` ABI (including fd zero), reactor quiescence and checked close, retained claims on uncertain release, typed factory/discovery transfers and synchronous expert compatibility surfaces. Review Windows/custom/macOS boundaries separately.
- **Integration overlap priorities:** root/Core guides and instructions; Core API snapshot; discovery/ownership/physical-device/raw-tier architecture and diagram manifest; testing/migration/comparison/highlights; applet guides and selector callers; Management reconfiguration tests; PIV authentication/private-operation comments.
- **Delete/modify candidates:** upstream deletes `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoDeviceSelector.cs`, `src/OpenPgp/examples/OpenPgpTool/Cli/Prompts/OpenPgpDeviceSelector.cs`, and `src/Tests.TestProject/Controllers/DiTestController.cs` while campaign history modifies them. Reconcile corrected guidance into surviving shared CLI selection; do not resurrect deleted architecture by default.
- **Campaign-only Core work:** preserve and revalidate `Credentials/UserPresenceIntent.cs`, `Utilities/Base32.cs`, `Utilities/Base32Tests.cs` and their API/guide changes.
- **Instructions:** upstream still has some root Management generic/session and SCP examples already corrected by the campaign. Preserve valid campaign corrections while adopting newer Linux ABI guidance. Reconcile finalizer requests versus release guarantees and historical Linux verification status precisely.
- **Outside Core:** `verification/LinuxHidRouteVerification/` is a new 14-file host, outside the solution; nine Linux research records, AOT CI/build targets, macOS verification callers, cross-layer CTAP/YubiOTP tests, Management monitor tests and shared CLI/test infrastructure also changed. No new top-level native directory was introduced. Dependency changes include `System.Formats.Cbor` 10.0.2 → 10.0.15; do not restore obsolete package pins from the campaign parent.

These observations explain the integration scope; they are not completed Core contract or platform reviews.

## 2. Required reading and editorial contract

Read both existing and incoming instructions during reconciliation, then the actual integrated versions before assigning audit work:

1. Root `AGENTS.md` and canonical `CLAUDE.md`; `src/Core/CLAUDE.md`, `src/Core/README.md` and every applicable nested instruction file.
2. `docs/research/documentation-audit/workflow.md` and `records/style.md`.
3. `docs/SDK-HOUSE-STYLE.md`; relevant ownership, logging, cryptography, testing and Native AOT guidance.
4. All nine module handoffs, their authoritative findings/dependency maps and relevant correction/closeout records.
5. PIV's `review-closeout.md`, `corrections.md`, `snippet-validation.md`, final compiler tools and preservation records under `modules/piv/pass-1/piv-001/`.
6. Current `TOOLCHAIN.md`, `docs/TESTING.md`, `docs/COMMIT_GUIDELINES.md`, CI and platform-verification entry points before choosing commands.

Every task packet repeats the editorial contract: **direct, calm, precise, approachable; affirmative task/actor/behavior first**. Retain useful rationale, full resource scopes and nearby firmware/transport/lifetime conditions. Use natural sentences and ordinary spacing, including research handoffs. Reserve negatives for consequential limits or likely misconceptions and pair them with a supported action. Avoid repeated “does not,” “cannot,” emphatic “NOT,” and rhetorical “X, not Y.” Optimize understanding rather than minimum word count. Keep private audit chronology in records rather than public reference comments.

Use Context7 for current framework/library/OS-wrapper API questions, then pin exact implementation sources when version-sensitive behavior matters. Planning consulted `/dotnet/docs` on AOT compatibility analysis: annotation/analyzer, publish/link and actual platform execution remain distinct verification dimensions. No new AOT setting or qualification follows merely from that consultation.

## 3. Phase 0 — reconcile upstream before investigation

**Owner: Astra, with a fresh Sol integration investigator/resolver and a different Sol reviewer.**

### 0A. Capture and protect the starting state

1. Re-check mode/release, branch, worktrees, index, tracked/untracked changes and live GitHub tips. Verify no other wake/session already owns a Core run.
2. Fetch `origin/yubikit` and `origin/yubikit-docs-audit` during execution. Record before/after ref values, chosen upstream SHA, merge base and both histories. If GitHub advances, decide and record the pin before merge; do not chase a moving branch silently.
3. Verify all finalized campaign manifests, manifest-file identities, the separate PIV integration records, original user files and any newer user work. Derive totals from actual manifests; 5,366 is the planning observation.
4. Record the active applet documentation and campaign-only OATH implementations that must survive reconciliation. Treat earlier fixes and upstream equivalents individually; neither lineage wins wholesale.

### 0B. Reconcile in a separately scoped integration checkpoint

5. Inspect the complete upstream delta, including already changed Core docs, public API snapshots, shared toolchain/native/verification files and affected applet consumers. Record renamed/deleted APIs and active-documentation fallout before resolving conflicts.
6. Prefer a non-fast-forward merge preserving published campaign and upstream ancestry. Use an isolated integration worktree if shared-checkout ownership or resolution complexity warrants it; bring the reviewed merge back without rewriting the campaign. Never reset/rebase the published campaign or force-push it for convenience.
7. Fresh Sol resolves explicitly assigned overlaps; another Sol reviews both parents' relevant intent. Preserve deletion/rename provenance, current upstream behavior and the campaign's still-applicable documentation and OATH changes. An overlap inventory is not an actual merge-conflict count.
8. Necessary fixture/API-consumer migrations caused by integration belong to this checkpoint. Diagnose, record and verify them narrowly. Unrelated behavioral fixes become findings; integration is not the start of the implementation sweep.
9. Verify the integrated checkout contains both pinned parents. Re-read its instructions. Run the current clean build/unit/docs/architecture/complexity and additional required upstream gates. Compile complete examples affected by API migration, using new evidence contexts rather than editing frozen examples. Review any remaining integration errors before release.
10. Commit/push the integration checkpoint, verify GitHub identity and ancestry, and recheck all protected bytes. Record the **actual integrated SHA** as the Core audit baseline. Audit dispatch requires this gate to pass.

Execution command skeleton, to be adapted to verified ownership and current instructions:

```bash
git status --short --branch
git worktree list --porcelain
git ls-remote origin refs/heads/yubikit refs/heads/yubikit-docs-audit
git fetch origin yubikit yubikit-docs-audit
git merge-base HEAD origin/yubikit
git rev-list --left-right --count HEAD...origin/yubikit
# Review and pin actual SHAs before the following integration action.
git merge --no-ff --no-commit <pinned-upstream-sha>
# After resolution/review, verify both parents and run current required gates.
```

### 0C. Establish durable audit records

After collision checks, proposed execution paths are:

```text
docs/research/documentation-audit/integrations/core-upstream-20261111/
docs/research/documentation-audit/modules/core/pass-1/core-001/
docs/research/documentation-audit/core.md
```

Use a new dated/run identity if work already exists. Copy this plan, the read-only planning observations and delegation/review decisions into version control during execution. Repository phase records and pushed checkpoints become the durable authority. Frozen earlier runs remain immutable.

Update only active campaign routing/status: Core is now selected; remove the obsolete duplicate PIV “reserved” row and reconcile the generic “other remaining applets” placeholder against the real module list. Preserve the existing PIV completion/publication facts and old frozen workflow history. Do not imply first-sweep completion before Core and all boundary reviews close.

## 4. Phase 1 — re-baseline inherited findings and build complete coverage

**Owner: fresh Sol inventory/re-baseline investigators; Astra reconciles ownership and denominators.**

1. Inventory every tracked Core source, guide, package page, test, example, project and native-support file at the integrated SHA. Include relevant material outside `src/Core/`: root/shared guidance, architecture diagrams and evidence maps, migration/comparison pages, platform setup, native shims, benchmark explanations, verification hosts and shared test/CLI usage. Give each external section a precise scope and owner.
2. Reconcile effective public types/members, partials/extensions/generated record members, public API snapshots and internal explanatory units. Generate a map only during execution. Count the complete source surface afresh; do not inherit counts from PIV or this upstream observation.
3. Partition into non-overlapping semantic units: full contracts with implementation context, narrative sections, complete examples including implicit calls/helpers, native lifetime explanations, test methods and their real assertions. Distinguish generated/historical/configuration context without using that classification to hide substantive claims.
4. Build a historical finding registry keyed by **source run + original ID**, preserving aliases and provenance. Map each item to current symbols/paths and relevant upstream commits. Use outcomes such as still reproducible, changed, resolved in upstream for a specified scope, superseded/duplicate, needs decision, or evidence unavailable. A rename, PR title or green suite is not proof of resolution.
5. Keep documentation disposition, implementation state and device/platform qualification in separate fields. Record the evidence that justifies each transition; close the original uncertainty only as far as that evidence reaches.
6. Finish an explicit inventory/disposition for every old Core dependency, including a rationale for candidates that remain unchanged. Carry both the fresh Core findings and the inherited IDs into a claim-to-document reopening map.

### Required inherited seeds

| Source | Starting questions and IDs to re-check |
| --- | --- |
| OATH | `CORE-001` abandoned HID identity read / missing multi-key device; shared Base32 ownership; terminal RESET/reopen and borrowed connection; historical SCP initialization `6A88` before RESET; remaining authentication/firmware decisions in `records/implementation-followups.md` and final OATH wave records |
| Management | `MGMT-XREF-001` FIPS metadata bit decoding; `MGMT-XREF-002` flagged form-factor representation; reopen the resolved `MGMT-XREF-003` fixture/cache explanations if upstream changes their assumptions; catalog/configuration/selection boundaries |
| CTAP2 | `CTAP-XREF-003` credential character/byte limits; Core HID framing/channel/cancel/keepalive; raw-response and COSE/catalog/ARKG boundaries; preserve original CTAP implementation groups and their reopening map |
| WebAuthn | Actual Core use directly and through CTAP2, prompt/connection lifetimes and result ownership; retain resolved `WA-XREF-001` and open `WA-IMP-*` provenance; selected-native-result privacy `DEC-002` is an actor/serializer decision, not permission to strip fields |
| Security Domain | `SD-DEP-001..005`: SCP/auth initialization, key/certificate ownership, reset/effective firmware, lease/cancel/APDU response and Le, logging policy |
| YubiOTP | `OTP-DEP-001..005`: selection/lease, OTP HID framing/CRC/progseq/cancellation, SmartCard drain and prompt ownership, raw/effective firmware, legacy/FIPS surface relocation |
| YubiHSM Auth | `HSM-DEP-001..003`: effective version policy, SmartCard/prompt lifetime and cancellation, TLV/APDU copied-buffer ownership; keep `HSM-IMP-008` response-shape uncertainty at its applet/profile boundary |
| OpenPGP | `PGP-CORE-001..004`: credential units, connection/SCP/cache disposal, development/override version provenance, serialization/drain/presence; `PGP-DEP-*` documentation links are not extra Core bugs |
| PIV | `PIVCORE-001..003`: physical PIN state/handle isolation, cancellation/workflow serialization, owned response/TLV and borrowed connection; retain `PIVIMP-*` identities and physical-test deferral |

Do not conflate OATH's `DEC-002` with CTAP/WebAuthn's similarly numbered decision. The source-run key is mandatory. Also re-check the pre-existing migration YAML `DOC-002` and older audit-host selector collisions; resolve their current status explicitly rather than silently carrying or fixing stale assumptions.

## 5. Phase 2 — whole-Core investigation

Audit the entire integrated module, using recent commits to prioritize attention rather than restrict coverage. Proposed investigation families, adjusted to the actual inventory:

| Family | Required consumer contracts and internal evidence |
| --- | --- |
| Discovery, metadata and monitoring | Select one/many keys, serial/raw identity and grouping, unavailable versus failed transport enumeration, budgets/admission, cache generations, snapshot/event ordering, rescan hints, watcher overflow/recovery, start/stop/disposal and concurrent enumeration; new metadata split and platform-specific identity evidence |
| Connections and native ownership | Physical registry/lease lifetime, process boundaries, sharing modes, per-platform HID/PCSC open/read/write/close, async versus retained synchronous surfaces, abandoned operations, retained claims, worker admission, native callbacks and buffer/handle ownership |
| Sessions and interaction | Owned convenience versus borrowed connections, attachment, initialization/failure cleanup, effective firmware/overrides, readiness and applet/protocol auth distinctions, prompt borrowing/intent propagation, operation outcome owner, cancellation admission/terminal/drain and reuse |
| Raw protocols and framing | Guarded raw sessions versus direct raw I/O; APDU cases/Le/length/chaining/status handling, CTAPHID framing/channels/keepalive/cancel, OTP HID CRC/programming sequence/status; recovery and unsupported transports |
| Secure channels | SCP03 and applicable SCP11 variants/profiles, initialization/authentication actors, certificate/key validation, counters/MAC/chaining/padding, copied and borrowed secrets, failure teardown, reset/reopen and parameter ownership |
| Cryptography and encodings | Catalog identity/applicability versus operation support, key import/export/ASN.1/SPKI/PKCS formats, mathematical/encoded widths, provider/native availability, secret ownership and failure cleanup, TLV/BER/Base32/CRC and other utilities; inherited protocol-specific profiles remain owned by their applets |
| Cross-cutting support | Credential collection/encoding, logging/diagnostics, platform setup and native loading, AOT/trimming claims, actual tool/test/benchmark explanations, build/configuration examples and current migration guidance |

Each investigator reads whole units and the assertions behind tests. In particular, responsiveness/invocation is distinct from completion; a canceled managed wait is distinct from native interruption; shutdown return is distinct from native release; listener hints are distinct from physical-device truth; parsed output length is distinct from cryptographic correctness.

For each substantive specification/API claim record title, version/revision, publication status, section/anchor, actor, normative strength, applicability and the original passage. Candidate sources include ISO 7816, PC/SC, USB HID/FIDO transport specifications, GlobalPlatform Card/SCP amendments and vendor profiles, IANA/RFC/NIST crypto/encoding publications, Windows API documentation, Apple IOKit/CoreFoundation, Linux hidraw/udev/PCSC, and versioned .NET/native crypto provider sources. Determine applicability before relying on any source; research is claim-driven rather than an attempt to collect every standard.

Pin Python YubiKey Manager code, tests and relevant history for shared behavior and firmware origins. Prior pinned revisions are historical comparators; capture the revision actually used. Preserve vendor/profile statements separately from sibling policy and normative requirements. Initial research plus one focused follow-up bounds each unresolved issue; retain unavailable final GlobalPlatform originals and draft/final/vendor distinctions as explicit gaps if they remain unresolved.

## 6. Phase 3 — nine dedicated applet/Core boundary reviews

Perform a distinct boundary packet for **OATH, Management, CTAP2, WebAuthn, Security Domain, YubiOTP, YubiHSM Auth, OpenPGP and PIV** after the relevant Core investigation is available. A shared mechanism can be described once, but every module receives its own checked call-site and documentation disposition.

Each packet must contain:

1. Current factory/connection path, selected transport, shared types/helpers and initialization sequence.
2. Ownership table for inputs, results, temporary buffers, protocols, native handles, options, credentials, SCP parameters and prompts.
3. Cancellation/failure/reset trace through the actual applet, Core and platform layers; distinguish existing regression assertions from hardware evidence.
4. Raw and effective version/capability provenance and actual enforcement owner.
5. Every inherited dependency's re-baselined status and any newly discovered boundary defect.
6. Exact public XML, task guide, sample/test, migration and shared architecture claims to correct or reopen.
7. Applicable scoped verification, limitations and an independent review verdict.

WebAuthn's indirect dependency through CTAP2 must be explicit; the earlier CTAP/WebAuthn review remains historical evidence. Cross-module findings can reopen active applet documentation during Core, with explicit per-path ownership and review, while applet implementations remain deferred. Preserve frozen applet evidence and add dated external handoff notes where new Core conclusions supersede old boundary qualifications.

## 7. Phase 4 — triage, fresh editing and independent review

1. Astra adjudicates documentation changes, current defects, product decisions and qualification gaps before editing. Retain original IDs; choose collision-free new Core IDs after registry reconciliation, without reusing `CORE-001` for a different finding.
2. Fresh Sol editors own coherent file sets. Keep one editor per path; a dedicated shared-document owner integrates boundary sections. Concurrent edit worktrees are isolated, with exact base and transfer hashes.
3. Improve task-oriented entry guides and contracts together: discovery/select/watch/dispose, open/reuse/close, choose raw access, establish SCP, handle interaction/cancellation, use key/encoding utilities and diagnose platform prerequisites. Preserve complete resource scopes and meaningful examples.
4. Shipping runtime bodies, tests/assertions, directives, API snapshots and executable help/output strings stay unchanged in the audit delta. Documentation code blocks may be corrected; descriptive package metadata is allowed when justified. Protocol or ownership defects are qualified truthfully without turning them into intended design.
5. Independent Sol content reviewers check full changed units and all consequential original citations. Correct factual/usefulness/style issues and re-review exact identities. Optional preference rewrites are not release requirements.
6. Astra samples at least 10% of eligible clean semantic units, minimum three where available, spanning API, narratives/examples and internal/test/native explanations. Stratify across major Core families/platforms and all boundary packets; one large pool of enum entries must not crowd out lifetime or native comments. Expand the affected category after a material miss and remove edited overlaps.
7. Before final compilation, explicitly close **every candidate-associated unchanged path**, not just the edited-file list. Reconcile all dependent claims after corrections. This incorporates PIV's late SPKI/rationale lesson earlier in the process.

## 8. Phase 5 — verification and device/platform evidence

### Compiler and repository gates

- Use `domain-build` and `domain-test` skills and the integrated `dotnet toolchain.cs` entry points. Re-derive selectors, targets, actual projects and required native/platform checks; short `Core` matching may include archived hosts. No direct `dotnet build`/`dotnet test` shortcut.
- Serialize build/test/capture jobs within a checkout. Capture full logs before retention rotation. Run meaningful selected checks while editing, then the required clean final solution/unit/docs/package/architecture/complexity gates with zero compiler/analyzer warnings or failures.
- Run current resilience gates when integration or selected work changes runtime loops, polling/recovery, monitoring or native lifetime code; follow incoming requirements. Documentation-only work need not manufacture additional tests, but existing relevant regression evidence must be read and used accurately.
- At observed upstream `5746e72e`, integration of these runtime changes requires planning `dotnet toolchain.cs -- resilience --fast`. The separate Linux no-hardware/native lane uses `dotnet toolchain.cs linux-hid-verification-publish`, then `python3 verification/LinuxHidRouteVerification/capture.py --native-only`, with the workflow's `test_evidence_gates.py` checks against the capture. Read actual arguments and prerequisites after integration; retain evaluated publish inputs/artifact binding. Ordinary solution/unit gates do not cover that external host. All-module AOT publish/run and selected-key hardware are separate lanes.
- Format only actual edited C# project documents. Verify the path list was processed; an empty formatter match is not a success. Compare executable tokens, directives, imports and current API snapshots against the integrated baseline, including relevant native-source preservation where comments are edited.
- Compile complete meaningful final examples and actual sample/verification projects. Give every current body an explicit complete/composable/application-dependent/historical disposition. Supply real typed prerequisites and full lifetime scopes, with no fake SDK methods or weakened assertions to make compilation pass.
- Reuse the proven final PIV capture algorithms selectively with original hashes and replayable transformations. Verify effective compiler policy, exact Csc membership, actual canonical analyzers, evaluated imports/assets and direct **and transitive** project output bindings. Canonicalize only unordered graph collections, reject duplicates and retain full identity/edge equality. Host `Version`, package version and SDK version are separate observations.
- The explicit analyzer package can disable duplicate SDK analyzer selection; prove actual participation instead of assuming `EnableNETAnalyzers=true`. Bind actual per-project policies rather than labeling a Recommended build AllEnabled. Preserve failures and corrected tools version-by-version.
- Validate a representative current host and its captures before scaling the harness. Establish the full three-context-style proof only where needed; avoid rebuilding a whole versioned framework for each small correction.
- After all source corrections, compile/capture current examples and run live dependency checks. Earlier hardware/compiler contexts retain their own snapshots and tested bytes. If later builds change outputs, preserve/map the original binaries externally before replacement; missing bytes make a requested live check fail explicitly.

### Hardware and platform matrix

1. Re-establish devices, transports, software/OS/native library versions and relevant applet state. Old serials 103/125 and development firmware are history, not guaranteed current fixtures.
2. Read every selected test's actual setup/body/cleanup, traits, early returns and mutation scope. Count attempted, admitted, executed, passed, failed, skipped, blocked and not-run separately. Verify exact selected serials and current harness configuration, including JSONC and tracked status.
3. Use deterministic fake/native-seam tests for ownership, call cadence, admission, ordering and failure cleanup. Add audit-only experiments only for consequential unresolved claims, with independent oracles; implementation regression fixes remain Pass 2.
4. Plan platform lanes for Linux, Windows and macOS separately: discovery/grouping, async and synchronous compatibility paths, PCSC/HID sharing, monitor start/stop/watch, contention, blocked calls, disposal and recovery, plus AOT publish/runtime where applicable. Inspect new upstream platform evidence at its original revision and harness; distinguish it from fresh execution. Available platform agents/machines require an explicit assignment, not an assumed environment.
5. Existing dedicated-key permission covers routine configurations/resets/ad hoc probes. Coordinate physical actions. **Retain the user's PIV touch/hotplug/cancellation deferral**; ask for an attended Core test window only when the matrix needs it, and record unrun work if deferred. A Core selection does not silently release PIV's physical lane.
6. Prefer read-only discovery/transport journeys where they answer the question. Before applet mutation, capture that applet's relevant state and verify independent cleanup. Preserve non-restorable attestation/private material and identify limits of state observability. Device-wide resets or mode changes require their full cross-applet effect to be explicit.
7. For native cancellation/lifetime claims, record operation admission, cancellation request, native interruption/terminal acknowledgment, drain, resource/claim release and fresh-session/device recovery separately. Use bounded waits with a recorded ownership outcome; never reset over an undrained operation to manufacture recovery evidence.
8. Keep same-key vendor controls before cleanup where identity matters. Finish with independent readback and restore temporary harness/configuration bytes exactly. Source/standards correctness, OS execution, firmware coverage and secure-channel interoperability remain separate matrix cells.

Historical SCP initialization failures do not count as reset execution; absent final standards remain source gaps. Linux evidence does not qualify Windows/macOS. Native AOT annotations, analysis, rooted publish/link and live platform operation are distinct checks.

## 9. Phase 6 — first-sweep closeout and publication

1. Reconcile current whole-Core and external-scope inventories, every inherited item, all nine boundary packets, accepted hashes, source preservation, sample populations, complete examples and original citations.
2. Verify the final claim-to-document map after integration changes, actual tests, hardware results and all late corrections. Active handoffs supersede old status; frozen archives preserve their historical text.
3. Obtain a **fresh final Sol evidence reviewer**, separate from authors and content reviewers. Review raw outcomes, compiler provenance, device/platform scope, restored state, current status and every required gap/disposition. Correct required issues before freeze.
4. Preserve all source/log/tool failures. Register original binary sources by exact identity; keep generated caches/runtime artifacts external. Verify prior archives and user work, final record/link checks, manifests and exact staged bytes.
5. Commit/push Core documentation and run evidence, verify remote HEAD and ancestry, then publish an external handoff/index checkpoint without modifying frozen bytes. Checkpoint phase records during long work and before pauses so GitHub remains a usable recovery source.
6. Publish campaign Pass-1 closeout only when Core and all nine boundary reviews have explicit outcomes. Confirm all actual modules are accounted for and eliminate obsolete active status placeholders.
7. Prepare a **selected Pass-2 planning backlog** with re-baselined IDs, priorities/dependencies, intended contracts or outstanding decisions, implementation scope, meaningful verification matrix and exact documentation re-review triggers. Do not automatically implement the entire backlog or call unqualified dimensions complete.

### Completion criteria

Core Pass 1 is complete when every in-scope unit and inherited finding is dispositioned; all nine boundary packets are reviewed; accepted documentation and consequential citations have independent acceptance; final examples and required gates pass at recorded identities; remaining implementation/platform gaps have precise handoffs; user work/device state are preserved; and the records are committed and backed up. First-sweep completion is a documentation milestone, not a universal hardware or implementation claim.

## 10. Delegation and phase handoffs

Resolve the exact available **Sol 6.1** model before dispatch (`github-copilot/gpt-6.1-sol` was available during planning). Report a real availability blocker rather than silently changing the requested model. Astra remains coordinator. Use fresh editors and independent reviewers; execution workers receive explicit paths, baseline, allowed actions and return artifacts.

| Stage / packet | Owner and independence | Required return / transition gate |
| --- | --- | --- |
| Integration reconnaissance/resolution | Fresh Sol; Astra owns checkout/actions | Exact ancestry, overlap/rename decisions, preservation risks and scoped necessary migrations |
| Integration review | Different fresh Sol | Both-parent intent and exact resolved source reviewed; Astra validates/publishes before audit |
| Coverage and inherited registry | Fresh Sol, coherent inventory owner | Complete unit population, old-to-current finding map, no unowned paths |
| Core family investigation | Fresh Sol per independent family as size warrants | Complete semantic reads, clean/findings ledger, original sources, tests/oracles and dependencies |
| Nine boundary packets | Fresh Sol owners, coordinated with relevant family evidence | Per-module call-site/ownership/failure/version traces and exact reopening map |
| Shared docs and module editing | Fresh Sol, exclusive path ownership | Task-complete prose, body classifications, changed-path hashes, preserved executable state |
| Content/citation review | Fresh Sol independent of authors | Required/optional verdicts, original-passage checks and exact accepted identities |
| Validation preparation | Sol; no concurrent toolchain/device execution | Small verified extraction/capture tools, collision-free project names, explicit command/evidence matrix |
| Sampling/execution/integration | Astra | Stratified clean samples, actual command results, current compiler/device identities, restoration |
| Final evidence review | Fresh Sol independent of prior author/reviewer roles | Cross-record/raw-evidence reconciliation, remaining gaps and release verdict |

Start with at most three independent investigation packets; scale only after the integrated inventory identifies genuinely independent work. Assign one shared-file integrator, serialize toolchain/hardware operations, and avoid duplicate standards research by sharing a pinned source register. A failed validation gate causes diagnosis and a scoped correction rather than parallel retries or relaxed proof.

Every packet includes: goal/pass; actual integrated SHA; exact owned/read-only paths; user style; required instructions; upstream re-baseline context; complete-unit requirement; inherited IDs; source/evidence bounds; implementation deferral; allowed device/build actions; expected records; independence constraints; and explicit blocker/handoff conditions.

## Next action

Review this plan in conversation. After the user switches to execution with this scope, start Phase 0 by rechecking live branch/worktree ownership and upstream tips. Publish the integrated baseline before assigning Core audit investigation. The last PIV checkpoint remains the campaign starting point; it is not the Core source baseline.
