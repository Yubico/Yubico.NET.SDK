# Phase 0 conflict-resolution packet

## Handoff outcome

The isolated checkout has a resolved pending merge of incoming
`5746e72ed9a848d08b5782ddde025aa4a4b522ef` into campaign
`f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa`. The actual merge produced **17 conflicted paths:
14 content conflicts and three modify/delete conflicts**. All are resolved in the index. The earlier
45-path figure counts common-base changed-path overlaps, not conflicts.

This packet is ready for a different Sol review of both-parent intent and the exact resolved source.
It is a resolution handoff, not a completed Phase 0 release: compilation, tests, formatter, native
verification, independent acceptance, commit and push remain pending. No device operation or full
Core audit was performed. Astra retains those phase gates. The primary checkout and other worktrees
were left unchanged.

## Pins, ancestry and inventories

- Owned checkout: `/tmp/opencode/core-upstream-20261111`, branch `yubikit-core-docs-integration`.
- `HEAD` remains the campaign pin; `MERGE_HEAD` is the incoming pin. No commit, fetch, branch-tip or remote-tracking ref update,
  rebase, reset or push was performed by the resolver.
- Common ancestor: `b1362127203beda5e9c797e8648b28e2716c496c`; unique histories remain 30 / 74.
  GitHub identifies that common ancestor as PR #745's merge identity; its catalog-integration Git
  message is a separate description. This packet preserves the coordinator's fresh GitHub adjudication.
- Complete upstream-versus-base status: `evidence/resolver-inputs/incoming-delta.tsv`: 287 entries,
  comprising 164 modifications, 91 additions, 30 deletions and two detected renames. The complete
  campaign-versus-base delta is retained alongside it. Both-parent/base blobs and Git/SHA-256 identities
  for all 45 overlaps are in `evidence/resolver-inputs/overlaps.json` and `blobs/`.
- The pre-resolution three-stage index is `evidence/unmerged-index-before.tsv`; the initial worktree
  conflict contents are retained in `evidence/conflicted-worktree/`. `evidence/actual-conflicts.json`
  lists the actual 17 paths. `evidence/overlap-resolution-identities.json` binds all resolved overlaps.
- The pending product index tree is `899dade50586c55efad0031e0a001498a11f5a26`; new integration
  records are outside that source-tree identity until coordinator staging. `evidence/resolution-state.json`
  records the exact pending state. The merged Core inventory is **515 tracked files / 498 C# files**,
  adding the campaign Base32 source and test to incoming's 513 / 496. This is a file inventory, not
  semantic audit coverage.

The incoming test renames are retained: `YubiKeyManagerTests.cs` becomes `MonitorSnapshot.cs` in Core
integration tests (R050), and unit-test infrastructure `DeviceEventWatcher.cs` becomes
`DeviceChangeWatcher.cs` (R065). These are Git similarity observations, not compatibility aliases.
The deleted repository/manager/monitor-service/event-hub and legacy listener interfaces stay deleted.
The new change feed, scheduler, owned monitor and listener contracts replace their responsibilities.

## Preservation and API identities

`evidence/preservation-after-resolution.json` verifies all **5,366 manifest-listed artifacts**, all
**eight manifest identities**, all **114** prior PIV integration files and the **four** original user
files. User files are read only in the primary checkout; they were never copied into this merge.
Primary HEAD remains the campaign pin. The independent preparer's checks also cover both checkouts;
its tools and receipts are described in `validation-preparation.md`.

The shipping Core source comparison leaves every incoming C# file byte-identical after repository clean
filters except `Credentials/UserPresenceIntent.cs`, which is exactly the campaign blob. Campaign
`Utilities/Base32.cs` and `Utilities/Base32Tests.cs` are exact preserved blobs. Campaign OATH runtime
and tests remain intact; the narrow example-entry/selector migration is itemized below. The campaign's
OATH implementations are not replaced with the older incoming command parser or lifetime behavior.

Core API snapshot nonblank/non-directive line identities:

| Snapshot | Entries |
| --- | ---: |
| Common base | 1,410 |
| Campaign | 1,413 |
| Incoming | 1,439 |
| Resolved | 1,442 |

Incoming removes 54 old identities and adds 83. The resolved snapshot retains all 1,439 incoming
identities and adds exactly the campaign's Base32 type plus its two overloads, with zero duplicate
lines and zero missing incoming entries. This result was reconciled with actual source declarations:
the new five-method manager, owned monitor, nullable cached metadata, revisioned change-record family
and generated record members survive; old static lifecycle/token-only overload identities stay removed.
`UserPresenceIntent` changes implementation/documentation without a new snapshot identity. Base32's
two signatures agree with the retained implementation. This is source/snapshot reconciliation;
compiler/API-analyzer agreement remains a gate. Details are in `evidence/api-reconciliation.json`.

## Per-path overlap decisions

Every row below applies to the actual resolved source. “Content” and “delete” identify the 17 genuine
conflicts; the remaining rows are clean merges that also received integration-intent review.

| Overlap path | Merge state | Resolution intent |
| --- | --- | --- |
| `CLAUDE.md` | Clean | Adopt fresh scoped Find/owned monitor architecture and removed TestProject module. Retain campaign non-generic Management factory, real SCP options and caller cleanup examples. Replace the illustrative removed repository/manager shell with the supported owned-monitor route. |
| `README.md` | Clean | Retain task-oriented applet/session introduction and supported access tiers; adopt new discovery/monitor ownership. |
| `docs/TESTING.md` | Clean | Retain campaign operation-transport assertions, cached harness qualifications and PIN setup limits; adopt incoming discovery/native test guidance. |
| `docs/architecture/connection-ownership-and-contention.md` | Content | Keep explicit process-local grouped-member scope; retain incoming discovery admission/native-lifetime details. |
| `docs/architecture/device-discovery-guarantees.md` | Clean | Adopt fresh versus monitored evidence/lifetime guarantees; retain corrected OTP-major-three effective-version provenance. |
| `docs/architecture/images/image-manifest.txt` | Content | Keep campaign L2 and CTAP-sequence hashes/images; use incoming L4 discovery hashes/images and source. No regeneration or image editing performed. Architecture freshness remains a gate. |
| `docs/architecture/physical-device-model.md` | Clean | Adopt owned monitor/scoped discovery; retain options-based transport examples and finalizer qualification. Replace stale repository metadata terminology with change-feed updates. |
| `docs/architecture/raw-access-tiers.md` | Clean | Retain campaign named PIV cancellation argument; preserve incoming Linux typed/direct owner and synchronous expert boundaries. |
| `docs/architecture/sdk-architecture-diagrams.md` | Clean | Retain campaign protocol/CTAP corrections alongside the incoming owned-monitor discovery diagram. |
| `docs/migration/v1-to-v2-gaps.md` | Clean | Preserve scoped applet qualifications while adopting new discovery parity/remaining hardware limits. |
| `docs/migration/v1-to-v2-map.yml` | Clean | Preserve campaign applet/API findings and incoming discovery migration evidence. Existing unrelated YAML/DOC-002 investigation remains deferred; no speculative parser fix. |
| `docs/migration/v1-to-v2.md` | Clean | Keep campaign real options, secret ownership and reset qualifications; adopt fresh Find/interfaces, replay/revision and owned-lifetime migration recipes. |
| `docs/v1-to-v2-comparison.md` | Clean | Keep applet behavior qualifiers and adopt explicit initialized shared-monitor ownership. |
| `docs/v2-highlights.md` | Clean | Preserve applet/session facts and current producer/subscriber discovery distinction. |
| `src/Cli.Commands/src/Infrastructure/YkCommandBase.cs` | Content | Preserve incoming single selection-enrichment pass and context metadata reuse. Remove the campaign's now-redundant second live query; its old prose described architecture replaced upstream. |
| `src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs` | Content | Preserve incoming SessionOptions/live-read API and last-known display snapshot. Retain campaign FIPS/form-factor interpretation qualifications without claiming a second enrichment read. |
| `src/Cli.Commands/src/Management/ManagementConfigCommand.cs` | Clean | Keep incoming actual live read and campaign accurate displayed-field list. Correct the clean merge's obsolete snapshot-only summary to match the merged body. |
| `src/Cli.Commands/src/Management/ManagementResetCommand.cs` | Clean | Preserve incoming live prerequisite/transport behavior; retain campaign Bio MPE/F9/config-lock qualifications. |
| `src/Core/CLAUDE.md` | Clean | Preserve all incoming discovery/L1–L6 Linux contracts, int ABI/fd zero and request-only finalizer precision, alongside campaign Base32 and deterministic-disposal guidance. |
| `src/Core/README.md` | Clean | Preserve incoming fresh Find, owned watch and Linux owner guidance; retain Base32/secret contracts. Reconcile stale metadata prose with DeviceUpdated on the actual change feed. |
| `src/Core/src/PublicAPI.Unshipped.txt` | Clean | Source-checked 1,439 incoming identities plus three Base32 entries; remove old architecture identities through incoming delta, rather than textual union with campaign. |
| `src/Ctap2/CLAUDE.md` | Clean | Retain campaign complete API/protocol qualifications and examples plus incoming Linux cross-layer test explanation. |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs` | Content | Use incoming shared selection/policy; retain current AID, NFC/USB firmware eligibility and destructive target-binding guidance. Remove obsolete claims about the replaced selector's blocked prompts/duplicate-label behavior. |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoDeviceSelector.cs` | Delete | Accept upstream deletion of inactive alternative and its removed base; campaign inactive-path warning is preserved in parent blobs/history. |
| `src/Ctap2/examples/FidoTool/Program.cs` | Clean | Keep incoming monitoring removal and campaign actual parser/reset re-selection limitations. The existing destructive re-selection defect is qualified, not fixed here. |
| `src/Management/CLAUDE.md` | Content | Retain campaign Management truth and incoming owned discovery/hardware guidance. Resolve enumeration to `FindAllAsync(cancellationToken: cancellationToken)`; both old conflict alternatives were unsuitable. |
| `src/Management/README.md` | Clean | Keep campaign configuration/lifetime/metadata safeguards; retain new fresh-discovery reboot example. |
| `src/Management/examples/ManagementTool/Cli/Prompts/DeviceSelector.cs` | Content | Keep incoming SmartCard/RequireChoice tool policy over All discovery and shared selection; retain campaign flagged-form-factor formatting caveat. |
| `src/Management/examples/ManagementTool/Program.cs` | Content | Keep incoming one-shot dispatch/menu lifetime, removing old static shutdown scaffolding. |
| `src/Management/tests/CLAUDE.md` | Clean | Preserve campaign authorization/transport/metadata limits plus incoming scoped/owned-monitor test lanes. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ReconfigurationDiscoveryInvariantTests.cs` | Content | Keep incoming fresh Find calls; retain campaign comment that assertions establish identity invariants rather than interface completeness. Assertions and setup/cleanup remain unchanged from incoming. |
| `src/Oath/examples/OathTool/Cli/Prompts/DeviceSelector.cs` | Content | Migrate production selection to incoming shared policy while retaining campaign console/discovery test seam and cancellation path. Exact necessary seam adaptation is described below. |
| `src/Oath/examples/OathTool/Cli/SessionHelper.cs` | Content | Retain campaign IOathSession/service/credential cleanup flow; discard stale incoming imports that belong to its older helper. Only EOF newline changes relative to campaign. |
| `src/Oath/examples/OathTool/Program.cs` | Content | Retain campaign cancellable application dispatch and Ctrl+C handler. Incoming monitor removal is applied in the campaign's real `OathToolApplication.ExecuteAsync` owner. Only EOF newline changes in Program relative to campaign. |
| `src/OpenPgp/examples/OpenPgpTool/Cli/Commands/OpenPgpCommand.cs` | Clean | Retain incoming concrete shared-selector adapter path and campaign first-key mutation, prompt and credential-buffer/immutable-string qualifications. |
| `src/OpenPgp/examples/OpenPgpTool/Cli/Prompts/OpenPgpDeviceSelector.cs` | Delete | Accept upstream deletion of inactive alternative. Current static adapter remains on concrete shared selection. |
| `src/Piv/CLAUDE.md` | Clean | Keep campaign full PIV contracts/qualifications and incoming selector/discovery guidance. |
| `src/Piv/README.md` | Clean | Retain campaign current key/PIN/state/secret explanations and incoming discovery references; historical evidence untouched. |
| `src/Piv/src/Authentication/AuthenticationProtocol.cs` | Clean | Preserve incoming runtime exactly, including its newer authentication implementation; retain campaign corrected factory-key and retry/PIN-lifetime comments. |
| `src/Piv/src/Cryptography/PrivateKeyOperations.cs` | Clean | Preserve incoming runtime exactly; retain campaign full-width padded-block/modulus rationale. |
| `src/Tests.TestProject/Controllers/DiTestController.cs` | Delete | Accept complete upstream sample-project removal. Corrected campaign one-shot/request-token explanation remains in reachable Git/captured blobs; no dead project resurrected. |
| `src/YubiHsm/examples/HsmAuthTool/Cli/Prompts/DeviceSelector.cs` | Content | Use incoming shared SmartCard/FirstEligible policy; retain campaign distinction between interface eligibility and actual applet selection. |
| `src/YubiHsm/examples/HsmAuthTool/Program.cs` | Clean | Keep incoming one-shot lifetime and campaign diagnostic-key-output/secret-lifetime warning. |
| `src/YubiOtp/CLAUDE.md` | Clean | Retain campaign challenge/CRC/touch/secret qualifications and incoming Linux cross-layer assertions. |
| `src/YubiOtp/examples/OtpTool/DeviceHelper.cs` | Content | Use incoming shared selection, stderr policy and SmartCard preference; retain campaign OTP HID challenge recipe, no fallback, presence and metadata eligibility qualifications. Remove duplicated local DeviceSelection in favor of shared type. |

The explicit delete/modify review, upstream deletion commits and campaign-content disposition are in
`deletion-review.md`; named `git rm` followed that review. The complete upstream delta preserves
provenance for the remaining clean deletions, including the rest of Tests.TestProject.

## Necessary integration adaptations outside the overlap list

1. `src/Oath/examples/OathTool/OathToolApplication.cs`: remove obsolete static StartMonitoring/
   ShutdownAsync around the campaign's current dispatch, preserving parsing, command choices,
   cancellation propagation and secret-owner scopes. This applies incoming lifecycle intent at the
   actual surviving application entry point rather than restoring the old incoming parser.
2. `src/Cli.Shared/src/Yubico.YubiKit.Cli.Shared.csproj`: add `InternalsVisibleTo="OathTool"` so the thin
   OATH adapter can use the existing internal injected shared-selector constructor. No public selector
   API or shipping SDK behavior is added. This keeps the campaign's real prompt/metadata-cancellation
   tests on the shared production selection path rather than discarding tests or keeping duplicate
   production selection logic. Review this deliberate assembly-friend scope explicitly.
3. `src/Cli.Shared/src/Device/DeviceSelection.cs`: carry campaign FIPS/form-factor display qualification
   into the surviving shared metadata owner; executable source remains incoming.
4. `src/Ctap2/examples/FidoTool/README.md` and
   `src/Management/examples/ManagementTool/README.md`: reconcile active selection prose with the new
   shared path, removed alternative classes and absence of global tracking. Preserve unrelated
   operation/parser/reset limitations. These necessary dependent claims are separate from a Core audit.

`evidence/runtime-overlap-line-proof.json` compares 16 resolved runtime paths to incoming after removing
only whole `//` comment lines and normalizing line/EOF endings. Every remaining line matches, including
imports, directives and executable source. It covers both PIV overlaps, CLI context/base/Management
commands, surviving CTAP/OpenPGP/HSM Programs/command base, shared metadata and selector adapters,
OTP's exact diagnostic string, and the Management invariant fixture.
This narrow mechanical proof complements the path decisions; it is not represented as a Roslyn token
check or test result. `evidence/source-preservation.json` identifies the four OATH example C# paths
that differ from campaign (two substantive migrations, two EOF-only differences).

The incoming central `System.Formats.Cbor` 10.0.15 pin is retained. The 14-file
`verification/LinuxHidRouteVerification` host, native publish target/CI wiring and new Linux research
records remain incoming. The source split still gives bounded caller waits to DiscoveryDeviceInfoReader,
native/read ownership to SharedDeviceInfoRead, and borrowed-connection protocol work to ProtocolDeviceInfo.
Linux fd-int ABI, valid descriptor zero, separate worker/reactor owners, checked close and retained
uncertain-release claims remain byte-preserved incoming runtime. Finalizers request shutdown; deterministic
disposal observes the outcome. No cross-platform release guarantee is inferred from this merge.

## Validation and deferred work

Read `validation-preparation.md` first, then `validation-plan.md` for source-specific consumer needs and
candidate host routing. Consumer compilation/capture is assigned after resolution acceptance. Exact
current source identities, caller inventory, API reconciliation and scopes are retained in evidence.
No compiled success, warning count, test count, device behavior or platform qualification is invented.

The resolver found no remaining merge-resolution blocker. Independent review must still accept the
OATH friend/seam adaptation, semantic API reconciliation, all automatic overlaps and deletion decisions.
Compilation can reveal further necessary fixture/consumer migrations; record them separately before
making a scoped fix. Unrelated defects, inherited finding dispositions, broad Core correctness and
style improvement belong to the subsequent Pass-1 investigation. Existing CTAP destructive re-selection,
Management metadata interpretation and other campaign-qualified limitations remain explicit; this
integration supplies no implementation fix or closure of their finding IDs.

The first input-capture preparation attempt used `parents[6]` instead of `parents[5]` for the checkout
root and exited 1 before writing evidence, with Git's `fatal: not a git repository` / exit 128. The
one-line path correction produced the complete retained input receipt. This was an evidence-tool path
failure, not a build, SDK or merge failure; it is retained here rather than counted as a product gate.
