# Independent final integration evidence review

## Decision

**HOLD publication for one mandatory record-gate correction (FR-1). The integration product and
qualified compiler-evidence scope are accepted at the identities below.** No SDK source correction,
additional build/capture, or optional editorial change is requested.

The current packet contains seven legitimate retained text records that the required final
`validation/check-records.py` rejects. Consequently its snapshot/check/staged workflow cannot complete
as currently written. Preserve those records and correct the publication checker through a bounded,
independently reviewed additive version before publication. This is a tooling/publication issue,
not an SDK defect or a reason to withdraw the accepted current-source/exact-retained-output scope.

Fresh independent GPT-6.1 Sol review, November 11, 2026. This reviewer wrote only this file, using
read-only Python `-B`, file/hash inspection and Git verification in the active home checkout.
No product/tool edit, SDK build, test, capture, native/device execution, formatter, index/ref mutation,
commit, push, subprocess signaling or delegation occurred. Whole-Core investigation has not started.

## FR-1 — mandatory final record-checker correction

`validation/check-records.py`, SHA-256
`e287c3e35d6fec35d125c870125201816dc4d9822ae78ba94d7c0da167eeb3e4`, rejects:

- Two retained NuGet JSON inputs because their archived relative paths contain `obj`
  (`record`, lines 48–49).
- Five text metadata/hash snapshots because `Path.suffixes` includes `.nupkg`, despite their actual
  terminal `.snapshot` suffix and text contents (`record`, lines 50–51).

Imported the original checker and called its unchanged `record(path)` on **each** of the seven files.
All seven raised the expected `ValueError`: `Generated directory` or `Binary/cache artifact`.
Every execution mode first evaluates the same complete `current` list at line 74, so all three modes
are affected. No snapshot was attempted or written. The first failing sorted record is the dgspec
below; fixing only that first error would leave six further failures.

All paths in this table are relative to this integration packet:

| Rejected retained text record | Bytes | SHA-256 |
| --- | ---: | --- |
| `validation/commands-v2/consumer-mac-comparison-restore-profile-restore-isolated-first/extra/verification/MacOSHidBoundaryComparison/obj/audit-integration-20261111/MacOSHidBoundaryComparison.csproj.nuget.dgspec.json` | 24344 | `23caffbc15f5f4d313e2174f55866854b9b132327805310ab83cfaff6c6e7da0` |
| `validation/commands-v2/consumer-mac-comparison-restore-profile-restore-isolated-first/extra/verification/MacOSHidBoundaryComparison/obj/audit-integration-20261111/project.assets.json` | 118048 | `3850e6a352628733cfb3bb2ff2f8f400cbdd4ecb9c512f223f690cf9b688d399` |
| `validation/consumer-proof/correction-v4/hash-domain-diagnosis/runtime.linux-x64.microsoft.dotnet.ilcompiler.10.0.15.nupkg.sha512.snapshot` | 88 | `68d567d75f9427b9050a6c508c9cf834c0da69f4ee38d87f53cc271bff55e784` |
| `validation/consumer-proof/evidence/consumers-first-v4/snapshots/2df486b4e4685229e1b40378f459c1b582a062fe3d79272b4d3aae6c80dc666c-.nupkg.metadata.snapshot` | 178 | `2df486b4e4685229e1b40378f459c1b582a062fe3d79272b4d3aae6c80dc666c` |
| `validation/consumer-proof/evidence/consumers-first-v4/snapshots/68d567d75f9427b9050a6c508c9cf834c0da69f4ee38d87f53cc271bff55e784-runtime.linux-x64.microsoft.dotnet.ilcompiler.10.0.15.nupkg.sha512.snapshot` | 88 | `68d567d75f9427b9050a6c508c9cf834c0da69f4ee38d87f53cc271bff55e784` |
| `validation/consumer-proof/mac-restore-blocker/snapshots/79051e7e70ceadab418c5cf33df794e95c5f5301625919ae0e209c92e529ecf3-.nupkg.metadata.snapshot` | 178 | `79051e7e70ceadab418c5cf33df794e95c5f5301625919ae0e209c92e529ecf3` |
| `validation/consumer-proof/mac-restore-blocker/snapshots/927426e971d4f66595f2dfcf65f28eab9505270cc7f6d025036ab47d05b19e55-yubico.nativeshims.1.18.1.nupkg.sha512.snapshot` | 88 | `927426e971d4f66595f2dfcf65f28eab9505270cc7f6d025036ab47d05b19e55` |

**Required disposition:** retain the original checker and all evidence bytes/paths. Prepare an additive,
source-bound checker/adapter that admits these exact reviewed text identities while preserving all
other file-population, binary/cache exclusion, UTF-8, symlink, raw/clean, attribute and staged-byte
checks. Exact path/size/hash exceptions are a sufficient bounded approach; blanket admission of `obj`
trees, arbitrary `.snapshot` binaries or arbitrary names containing `.nupkg` is not. Fail closed if an
approved file changes. Independently review the adapter and its negative boundaries, then run its
complete final snapshot/check/staged workflow after all writers finish. Do not remove these files from
the population, move retained paths, rewrite manifests, or regenerate compiler evidence to satisfy
the checker. Update the coordinator's authoritative publication commands to the accepted version.

The review's full read-only population scan found **8,317 physical packet files before this review**:
8,316 eligible records plus the excluded cooperative `validation/coordinator-command.lock`. These
are actual physical-file counts, not the 21,965 retained occurrences in compiler manifests. All 8,316
eligible files decode as UTF-8, have no NUL/runtime magic or binary/cache terminal payload, and have
no symlink. Bulk `git check-attr` and non-writing `git hash-object --stdin-paths` agree with independently
computed blob identities for every file; raw-to-clean changes are zero. The only seven conflicts with
the checker's additional pathname rules are listed above. The new review adds one record; the final
population remains coordinator-owned and must be recounted after the correction/review.

Two preliminary reviewer-only inventory assertions stopped during this read-only scan. The first
encountered the archived `obj` record; the labelled diagnostic reproduced it. An exhaustive diagnostic
then identified all seven cases, and unchanged original `record(path)` calls confirmed each rejection.
Those failed assertions wrote nothing and are not SDK, compiler or test outcomes. Their original tool
outputs remain in this session's transcript; this record preserves their cause and disposition.

## Accepted product identity and source-review continuity

Fresh checks establish:

- Active checkout: `/home/rupicola/Code/y/yubikit-core-docs-integration`.
- HEAD/campaign: `f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa`.
- MERGE_HEAD/incoming: `5746e72ed9a848d08b5782ddde025aa4a4b522ef`.
- Common ancestor: `b1362127203beda5e9c797e8648b28e2716c496c`; unique histories 30/74;
  empty unmerged index. The recorded GitHub PR #745 association is not reinterpreted from its Git message.
- All **7,196 whole-product index entries**, compared recursively by mode/path/blob, equal accepted
  pre-format tree `899dade50586c55efad0031e0a001498a11f5a26`. This is not a staged-change count or an
  assertion about historical raw index cache bytes.
- All 20 formatter paths match their after hashes and preserve whitespace-stripped bytes and exact
  import lines relative to the accepted index; exactly 14 have worktree overrides. The encoder was
  first checked against the accepted index tree, then recomputed current product tree
  `5dda4f4bbedadd8bca23bb4eb0fed5bf65d841ae` in memory without writing Git objects.
- Every current/deleted identity in `final-product-scope.json` matches. The complete actual tracked
  delta equals its **291 literal paths**, including both rename endpoints: **289 name-status entries**
  (166 M, 91 A, 30 D, two renames). The separately inventoried new packet is excluded from that product tree.
- Core inventory is 515 files/498 C# files. All 311 incoming shipping Core C# paths retain incoming
  blobs except campaign-exact `UserPresenceIntent.cs`; Base32 source/test and all 13 OATH shipping
  C# files are campaign-exact. The API snapshot has 1,442 unique entries: all 1,439 incoming plus
  exactly the Base32 type/two overloads.

The original source review remains authoritative for all 45 overlap dispositions, 17 actual conflicts
(14 content/three modify-delete), deletion intent and the OATH shared-selector integration. Its
assembly-wide Cli.Shared friend grant is explicitly accepted as an exception, not constructor-only
access. No fresh product change requiring semantic re-review was found. Incoming runtime changes are
part of this integration; subsequent Core documentation work begins against the published baseline.
The 27 AUTO_MERGE resolution/formatter paths are not the 553 inherited active differences versus incoming.

## Fresh final compiler-evidence reconciliation

Called the reviewed `reconcile.evaluate(scope)` directly under a Python audit hook forbidding write
opens, filesystem mutation and subprocesses. **The complete returned context objects exactly equal
both `preflight-first.json` and `coordinator-final-first.json`.** This invokes each original version's
retained and strict-live verifier after the explicit closed tool-inventory projection.

| Context / capture | Retained occurrences | Retained predicates | Strict current-path result | Reviewed drift occurrences | Qualified scope |
| --- | ---: | --- | --- | ---: | --- |
| Probe / `probe-first-v3` | 887 | pass | fail | 7 | pass |
| Examples / `examples-first-v3` | 1559 | pass | fail | 17 | pass |
| Annotations / `annotations-first-v3` | 1416 | pass | fail | 12 | pass |
| Consumers / `consumers-first-v4` | 14234 | pass | fail | 108 | pass |
| Fixture / `fixture-first-v5` | 2846 | pass | pass | 0 | pass |
| Mac / `mac-comparison-restore-isolated-first-v5` | 1023 | pass | pass | 0 | pass |

All **21,965 retained record occurrences** are exact; zero unapproved drift. The 144 reviewed occurrences
cover seven shared Core implementation DLL/PDB paths, with 40 exact old-record keys mapped to exact
current bytes from named successful captured states at the same paths. No original retained output
was substituted or recreated. No source/generated-input/configuration/analyzer/restored-input/reference-
assembly drift was admitted. Original captured tools are unchanged; only the explicitly pinned additive
proof versions are allowed. The original final-Probe failure remains failed and retained.

Fresh bounded negatives used process-local identity shadows only:

1. An unknown current Core DLL hash rejected the qualified result in all six contexts.
2. Making one original retained Probe binary unavailable rejected its retained and qualified results;
   the five unrelated contexts still passed. No disk input was changed.

The accepted amendment is **current-source/exact-retained-output verification**, not six strict-live
passes. The original unqualified final-live condition remains unsatisfied for four contexts. Earlier
individual live passes remain checkpoint-specific. Core implementation/PDB bytes differ despite the
recorded argument/generated-source/reference-assembly agreement; neither runtime equivalence nor an
unobserved cause of those differences is claimed. No unnecessary rebuild is required.

Independently checked actual manifest project counts **2/4/3/22/4/2**, SDK 10.0.404/net10.0 policies,
warnings-as-errors and compiler/analyzer execution settings. All **30 distinct generated complete-body
wrappers** match their input hashes and occur in their actual owner's Csc source set (29 Examples,
one real v3-derived Annotations context); Probe repeats body 01. All **20 formatted files** occur in
actual owner Csc inputs: 19 Consumers, one Fixture. Actual applet tools and verification hosts remain
compile evidence, not execution of their device paths. Fixture/Tests.Shared and the SCP method were
compiled only. Historical migration bodies remain outside current-body claims.

## Retained repository/native gates and recovery

Read the plan/review, coordination, resolution/source/deletion-review dispositions, current scope and
closeout, recovery handoff/independent review, complexity disposition, consumer preparation and final
reconciliation handoff/review. Freshly verified stream and retained-file identities for the actual
main, docs patch-guard, Linux publish/native/evidence-gate and final reconciliation v2 receipts. All
six receipts are passed/exit 0, complete-retained/stream-complete, error-free and owned-process quiescent.
These are retained coordinator executions, not tests/builds run by this reviewer.

- Main retained RESULT: 13 ordinary unit projects, **4,442 executions/five existing skips**, Core
  **2,065/four skips**; separate **197** resilience executions. Combined **4,639 executions** includes
  reruns. Clean build/docs/architecture/complexity/pack pass with zero warnings. The two complexity
  findings are new relative to campaign and require both exact commit-message justification lines
  from `complexity-disposition.md`; the successful command alone is not their disposition.
- Docs patch-guard stderr records six tests/OK. Linux publish binds 223 artifacts, zero warnings,
  source hash `90b0e9ab7582316099d37d9bc8e93beca1dd64fe8ce9c44e7edb32042b7533aa` and input hash
  `a509bb8ab2cb61c1b27e27af7f4a27dc893fcf66106fb4ddd423307005a021f7`. Native stdout records
  38 checks per JIT/AOT runtime plus four intended exit-1 sensitivity controls. Evidence-gate stderr
  records 31 tests/OK; the five temporary-directory substitutions retain the incoming assertions.
- The actual final reconciled command receipt reports passed/exit 0/complete/quiescent and its entire
  context report matches this review's fresh evaluation. It does not erase the strict-live failures.
- Recovery remains an environment UID tmpfs-quota failure, not a Core defect. Original partial/zero-byte
  retention, helper/capture failures, wrong-profile 401 and pre-admission Python shadowing failures
  remain historical evidence. Mac's accepted corrected-profile proof supplies no private-feed
  authentication or native-platform success claim. The pre-snapshot unsupported coordinator argument
  attempt is an operational usage error, not an SDK outcome.
- Fresh `python -B .../validation/check-preserved-relocated.py` exited 0: **eight manifests, 5,366
  artifacts and 114 prior PIV integration files in each active/primary checkout**, plus all four
  original primary user files. Both HEADs remain the campaign pin. Its receipt explicitly confirms
  the active home root/backlink and original preservation-before hash; the inactive tmpfs copy was
  not used as a substitute. The initial recovery verifier was not misused as a post-build gate.

## Exact accepted anchors

Paths are relative to this integration packet; hashes are freshly computed SHA-256.

| Record | SHA-256 |
| --- | --- |
| Original source `review.md` | `a39efdd36e08261d571457f42b1144f2522542df65dc40612f3b96348197a36d` |
| Current `closeout.md` | `8e16d689ac1353fd0e4983493f0f7ee252b3cc2fc274435524c725cac8ba1d66` |
| `final-product-scope.json` | `3f974a8216fbfa7690a62fca3cee035438816b004a15d8a0da715e0679d76420` |
| `recovery/independent-review.md` | `cbd0e7f9801ec100574c03832c7a66838ea5d0dc3a0ded7528cf42b4941e312a` |
| `complexity-disposition.md` | `1e66b5123339856febffbcae3dbf2aefeb365439c8d3fb6ceda538b62c5f0d36` |
| `validation/consumer-proof/final-reconciliation/independent-review.md` | `6a7da05245711db4710dd53a79af62e7fd29f7bbea40cfb5c267ea9483af5567` |
| `validation/consumer-proof/final-reconciliation/scope.json` | `d6b559e263ed217de91bb9f0dd6a92a9fd0b1d51f198d7a26566fcfde30ce491` |
| `validation/consumer-proof/final-reconciliation/reconcile.py` | `dff0d4dad280a81cebd721e5baf2c9839d9dc208def68cef197930bf80c20a11` |
| `validation/consumer-proof/final-reconciliation/coordinator-final-first.json` | `8e112eaf2bdeadbe93e37679d47755461d429f83973c83785a030d8b309ffbd6` |
| `validation/commands-v2/main-gates-home-first/receipt.json` | `019c705d729fd1fe8f5f7639ee457d0687b8ec20b89147508eb70257e4735ad4` |
| `validation/commands-v2/consumer-final-reconciled-first/receipt.json` | `bd8fd97104b86ead43aac1c5eaa14139c959654e5c2604ecc2b5a47a021048f1` |

The superseded held closeout snapshot still hashes to
`0eba2185e28924a1b4d31f2463348f01711524c70bd77aacfe772e8179655ac8`.
Acceptance attaches to these product/source/scope bytes, not a future automatically enlarged scope.

## Coordinator release boundary

Resolve **FR-1**, retain its failure/review evidence, and complete final record snapshot/check/staged
verification plus final product/user-preservation checks after all writers finish. The current review
does not assert a final manifest, exact final staged packet, merge commit or remote publication.
Include both complexity justifications, preserve pinned-parent ancestry, and perform publication and
the user-safe non-rewriting campaign fast-forward only through the coordinator's checks.

Physical SDK device operations remain zero. Linux native evidence does not qualify macOS/Windows,
physical applet interoperability or all-module AOT execution. PIV's physical deferral stays in force.
The user-announced Windows CCID/HID concurrency handoff has no supplied ready pin here; continue this
pinned integration and review that later delta separately when instructed. No other branch discovery
or waiting is required. Fresh whole-Core semantic/boundary investigation begins only after the actual
integration checkpoint is published and ancestry/preservation are verified.
