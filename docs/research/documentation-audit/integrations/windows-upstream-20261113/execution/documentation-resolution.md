# Documentation resolution receipt

The authorized documentation integration is written to the working tree. The three Markdown
conflicts are resolved in their file contents; the coordinator owns staging and the unmerged index.
Independent review, source-owner confirmation and all post-merge validation remain **pending**.
This receipt does not accept the merge, grant runtime closure or report platform acceptance.

## Inputs and ownership

- Audited parent: `d5d01ef99f1478e4d49911194c17667b2450d2fd`.
- Incoming parent: `e322712d609aaf03b9bd1cf2964a99326728866a`.
- Natural base: `5746e72ed9a848d08b5782ddde025aa4a4b522ef`.
- Worktree and intended publication branch: `yubikit-windows-audit-integration-20261113`.

The coordinator had applied the merge before this work began. The older preparation records describe
their original object-only preview; they were not edited to impersonate execution records. Guidance
was read from root `CLAUDE.md` at HEAD, the Core/Piv guides, the user-approved
[`records/style.md`](../../../records/style.md), SDK house style, the
[decision map](../merge-review/decision-map.md), [coordinator decisions](../coordinator-decisions.md)
and the editable integration plan. The intake review, frozen Core001 handoff and original candidate
definitions supplied evidence and continuity, not authorization for deferred implementation work.

This editor changed only the nine existing Markdown paths listed below and created this receipt plus
`../deferred-findings-successor.md`. `src/Piv/CLAUDE.md` was reviewed and retained as clean-merged.
No C# file, frozen historical record, status/coordinator/preparation record, Git index or ref was
changed by this editor. No merge, commit, build, test, formatter, native/hardware operation or subagent
was run. The original checkout stays untouched; publication of the new branch remains coordinator-owned.

## Conflict and active-documentation decisions

| Path | Written resolution |
| --- | --- |
| `CLAUDE.md` | Kept the audited guide. The HID vocabulary uses incoming `IWindowsHidReportApi` / `IWindowsHidFeatureReportApi`, subordinate to native lifetime owners. The adjacent audited APDU pipeline and non-generic Management session bullets remain. Logging, struct ownership/boxing, examples and test-oracle qualifications remain intact. |
| `docs/architecture/raw-access-tiers.md` | Kept all audited Tier 0/1 explanations and examples. Retained the original-task/borrowed-memory paragraph, narrowed synchronous compatibility to custom HID, and incorporated Windows FIDO overlapped and OTP blocking-worker ownership. Distinguished targeted CancelIoEx, terminal completion, callback resource-last-use, retirement and checked release. Kept native-success cancellation races, BCL preparation-failure retention, pre-open original claims, exact descriptor SET rules, macOS input/short-GET qualifications and terminal-wake shutdown. Replaced the stale Windows-registry-pending claim with explicitly source-bound incoming inventory claims and pending final-source validation. |
| `src/Piv/README.md` | Kept the complete audited guide and qualified `Yubico.YubiKit.Piv` selectors. Added the incoming focused PIN-only command with that selector and its owned-response-only oracle explanation. Preserved all examples, credential cleanup, direct/hidden ownership, firmware provenance, whole-workflow sequencing, physical deferrals and F9 retention. |
| `docs/architecture/connection-ownership-and-contention.md` | Replaced obsolete built-in Windows wrapper/deferred-SafeHandle-close and no-terminal-wake descriptions with owner-directed completion/retirement/checked release and terminal-wake shutdown. Kept custom-provider uncertainty, grouping bounds, caller workflow ownership and no-safe-reuse qualifications. Narrowed the OTP keyboard/access sentence to actual ACL, collection/driver, token and sharing conditions. |
| `docs/plans/2026-11-13-windows-refactor-integration.md` | Labeled preparation as a pinned historical checkpoint and added the actual applied-merge execution checkpoint, documentation receipt and successor-map pointers. Preserved the approved task scope and validation commands. No gate was marked passed. |
| `docs/research/documentation-audit/core.md` | Added the separately requested short pending Windows successor section, with exact parents, isolated worktree/new-branch publication, unchanged original checkout and pointers to the intake and finding map. Retained every Pass-1 baseline/count/result and prior integration milestone. Clarified that the old Windows handoff status belongs to that published checkpoint. |

The incoming inventory README is retained, not regenerated. Its claims at incoming
`e322712d609aaf03b9bd1cf2964a99326728866a` are 59 operations / 104 runnable links, 31 public entries
and 245 source sites, with 229 unchanged full site identities and 16 mapped through consistency
renames/extractions from `bdac2f4f4224fa0ccf92006971af8659736aa2b1`. Active prose now identifies those
as received static claims. Final-source scanning and linked-test execution are pending, and older
43-operation / 76-link L6a totals remain historical. Neither registry is native acceptance.

## All ten clean overlap decisions

The clean-merged hunks were compared with the audited parent and the accepted decision map. C# entries
below are **checked as proposed**, not accepted source resolutions: the source editor owns their final
receipt, and the coordinator must join that receipt and validation to the actual candidate. A clean
textual merge is not a runtime-equivalence result.

| # | Path | Checked disposition |
| --- | --- | --- |
| 1 | `docs/SDK-HOUSE-STYLE.md` | **Documentation resolution written.** Retained incoming native-adapter roles/layout and the audited illustrative example, named-token, readiness/provenance and workflow-guard qualifications. Mechanical APIs remain subordinate to lifetime scheduling. Replaced blanket pending-Mac wording with source-bound PR-summary evidence and its permission/topology/AOT/listener gaps; Windows original failures, sealed/AOT and driver gaps remain. |
| 2 | `src/Core/CLAUDE.md` | **Documentation resolution written.** Kept both branches' guidance and live PC/SC/macOS/Linux/Windows names. Corrected the clean-merged concurrency bullet: built-in Windows requires owner release proof; custom HID retains wrapper semantics. Preserved incremental grouped admission and caller-budget/native-release distinctions. Source-bound inventory totals and Windows/Mac historical evidence now distinguish receipt from candidate validation. |
| 3 | `src/Core/README.md` | **Documentation resolution written.** Kept incoming owner/navigation/Windows sections, approved logger-category rename and intentional legacy disposed-object string. Preserved monitor/logger cleanup caveats, firmware and memory ownership, transaction-end oracle, raw/SCP examples and sharing qualifications. Replaced obsolete before-merge Mac wording with bounded PR-summary receipt and retained Windows first-open/mixed ceremony outcomes. |
| 4 | `src/Core/src/Native/Windows/HidD/HidD.Interop.cs` | **Checked as proposed; source-owner confirmation pending.** Current hunk keeps new byte-return/uint-length imports and a precise residual warning: older Bool-return imports remain open; new report APIs reinterpret HIDP_CAPS short storage as ushort; other consumers need separate review. No all-import/architecture repair is inferred. No C# edit here. |
| 5 | `src/Core/src/Transports/Hid/MacOS/MacOSHidFeatureReportConnection.cs` | **Checked as proposed; source-owner confirmation pending.** Incoming `IMacOSHidReportApi`/`MacOSHidReportApi` names coexist with the audited GET8/padded-short versus typed exact8, supported SET length, worker and checked-close remarks. No C# edit here. |
| 6 | `src/Core/src/Transports/Hid/MacOS/MacOSHidIOReportConnection.cs` | **Checked as proposed; source-owner confirmation pending.** Incoming input-bridge names coexist with audited descriptor metadata versus normalized input64/zero-prefixed65, terminal other lengths, supported SET lengths and six-second reader-detach remarks. No Linux/Windows timeout was introduced. No C# edit here. |
| 7 | `src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs` | **Checked as proposed; source-owner confirmation pending.** The hunk changes internal construction to `PcscConnection` / `connectionApi:` while preserving sharing/coexistence prose and scoped examples. Exclusive default, no implicit shared retry, factory snapshots and UNPOWER request qualifications remain. No C# edit here. |
| 8 | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscConnectionLifetimeTests.cs` | **Checked as proposed; source-owner confirmation pending.** Incoming construction names preserve audited XML and assertions. The scope-end oracle holds transmit, not native EndTransaction; failed end closes admission and later shutdown reports error separately from release proof. No renamed test or stronger assertion was added here. |
| 9 | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/SmartCardSharingTests.cs` | **Checked as proposed; source-owner confirmation pending.** Incoming connection/owner names and approved `NativeState_` → `NativeOwner_` test rename preserve audited policy/request-only summaries and assertion values. They prove no physical PIN, power or separate-process isolation. No C# edit here. |
| 10 | `src/Piv/CLAUDE.md` | **Reviewed; retained without additional edit.** The clean merge adds owned-response capture/no pool-runtime-oracle guidance to the audited guide. Direct/hidden ownership, complete-workflow lifetime, detected/effective firmware, F9 and physical deferrals remain. |

## PIV and deferred finding continuity

The [successor map](../deferred-findings-successor.md) reads the actual frozen candidate definitions,
keeps original IDs and separates documentation-resolved, superseded old mechanisms, rescoped facets
and open engineering/policy/qualification questions. Every source-dependent proposal and validation
result remains pending until the source-owner receipt and coordinator results are joined.

Coordinator decisions establish the PIV rollback through `030b2a448…` and incoming restored production
blob `526c5da214546c1655ab8f36e2f7e95831cfdbf0`. The incoming response-only assertions are retained;
no late pool-memory observation or withdrawn injection seam is reinstated. Read-only inspection here
also found current `AuthenticationProtocol.cs` byte-identical to audited HEAD blob
`eed0152966b89f8627c4749daa359d2c65b92f59`. That is a different documentation-bearing blob, not a
claim that the combined file equals incoming's raw blob or that a pooled-command runtime oracle exists.
Executable/source reconciliation belongs to the source owner. PIV physical deferrals and F9
preservation remain unchanged.

## Static checks actually performed

- `git diff --check` on the ten reviewed existing Markdown paths returned exit 0 with no output.
- A read-only script checked all ten paths for active merge markers and compared every fenced C#
  example with audited HEAD. All comparisons matched. The untracked editable plan has no HEAD object;
  its comparison uses the preserved bounded-validation planning snapshot instead.
- The first comparison attempt stopped when it requested that nonexistent plan HEAD object. It
  supplied no complete check result. The corrected full ten-path comparison above completed normally.
- The Core Markdown scan found no remaining `Windows/custom`, `Windows compatibility`, active conflict
  markers, stale before-merge wording or present-tense 43/23 registry requirements within that scope.
- Read-only Git diffs covered all ten clean overlap records; the two macOS facade files were also read
  in full to confirm their locally retained qualifications. No linked behavioral test was executed.
- Local links in both new records resolve. A first receipt-hash check also compared the provisional
  C# snapshots and observed that `HidD.Interop.cs` had changed during the separate source editor's
  work. This is not a Markdown identity failure or an accepted final-source fingerprint. The source
  table below deliberately records the earlier observations; final C# identities belong to that
  editor's receipt. The corrected receipt check scopes identity acceptance to the eleven Markdown
  hash rows, leaving all six C# decisions proposed.
- A whole-file whitespace scan reported fifteen inherited trailing-space lines in root `CLAUDE.md`.
  They remain unchanged, including inside the byte-preserved examples; no unrelated cleanup was
  applied. Changed-line `git diff --check` remains the applicable whitespace result, not a claim that
  every existing line is whitespace-clean.

These are content/identity checks, not docs-QA, compilation, inventory execution, source-independent
review or frozen-archive verification. Those coordinator gates remain pending. The coordinator must
also record final index state; resolving file text does not stage it.

## Returned Markdown identities

SHA-256 hashes below bind the reviewed working bytes. The plan was already an editable untracked
input; it was updated rather than newly created by this editor. `src/Piv/CLAUDE.md` is included as a
reviewed clean-merge identity, not an additional edit. This receipt's own final hash is returned to the
coordinator outside the file to avoid a self-reference.

| Path | SHA-256 |
| --- | --- |
| `CLAUDE.md` | `93b24692c2c9ad11e13f9c349a30eff650e6285430fb80d3b1aeb14f47050e5d` |
| `docs/architecture/raw-access-tiers.md` | `0b858ea6a107cb0de9c2675cc651fbb7d690714d712575dc6dc93e83970ceb94` |
| `src/Piv/README.md` | `f5571de62ffb8894eb12b828077ed219b8581ca575276b83088f0152955e2cae` |
| `src/Core/CLAUDE.md` | `df004bbc0467f1c52d5a032c001afe86bf8a4af211f5bd2d5e42bf245e5a2200` |
| `src/Core/README.md` | `c1094cdb9da28d7a9e234c48302b8c50e5554108de99973bde9ef12899f1b964` |
| `docs/SDK-HOUSE-STYLE.md` | `663e99227a44510ba41e53a6ebc4814a908e80dc7f71c7f1d677d3959e596344` |
| `src/Piv/CLAUDE.md` (retained) | `09e27044c705fe0c308617f100919c14d7d607261b020e99d20bdd559e4a5744` |
| `docs/architecture/connection-ownership-and-contention.md` | `0083ebf979adb94f867fd212f6c9939256508ba4e74ebb9cb4da47b45be1d6cc` |
| `docs/plans/2026-11-13-windows-refactor-integration.md` | `5bea09dc8e3ba4292edae846a0c383ae40bafb135dd1ecac7203f377c7d9061c` |
| `docs/research/documentation-audit/core.md` | `0005a8f685de25051697a1f1ccc9f0a68fde3775cdeb6461c4d76fed3039023f` |
| `docs/research/documentation-audit/integrations/windows-upstream-20261113/deferred-findings-successor.md` | `d214d3c5830b7b5cd5f0507df742b44b5432ce9db764a9238a6a5f272a8ff47f` |

The source overlap observations were read at these SHA-256 identities. They are provisional snapshots,
not an exclusive source-editor handoff or a requirement that later reviewed source corrections keep
these exact bytes:

| Path | Observed SHA-256 |
| --- | --- |
| `src/Core/src/Native/Windows/HidD/HidD.Interop.cs` | `8bdc36dc9d6c3d91dee40e53da899805f82783c044c9288118697d105b17e333` |
| `src/Core/src/Transports/Hid/MacOS/MacOSHidFeatureReportConnection.cs` | `61b294cc9ffb37faa03c3198014887de32222a1c672c678700a1d8ebdafc582a` |
| `src/Core/src/Transports/Hid/MacOS/MacOSHidIOReportConnection.cs` | `fdcdc90fa4d874dc7c99e38fb2aa3b6872e36a2e96662850d4de4f41dfb9ea24` |
| `src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs` | `e2150a83494f8afab98be940b26ee27cefd9efeba5742c13fba2022d30c8c1df` |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscConnectionLifetimeTests.cs` | `d3e803697bfaa8cbf89e6d1ccb1acb1a83dad0560fb80fe88c49f83c4b7f5d0f` |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/SmartCardSharingTests.cs` | `b7980de61ec9469a6a31cccdb1c43a6cd8a128344f82cfbde2de569c436845dc` |
