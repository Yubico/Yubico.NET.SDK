# Phase 0 integration closeout — ready for publication with scoped qualification

## Current decision

**Integration validation is complete under the independently reviewed current-source/exact-retained-output
scope.** SDK compilation, repository/unit/native gates and the six individual compiler-capture/live
transactions passed at their recorded checkpoints. The fresh final reconciliation passed for all six
contexts, with all original retained bytes exact and zero unapproved drift. This explicitly accepted
scope amendment is **not a six-context strict-live PASS**: four contexts retain disclosed differences
at shared implementation DLL/PDB output paths. The original unqualified final-live condition remains
unsatisfied. See the final reconciliation section below for the exact qualification.

The checkpoint is ready for final evidence/record/index checks and publication. It has not been
committed or published; no published integration SHA is claimed. Whole-Core Pass 1 has not started.

This record supersedes earlier preparation-era “pending build/capture” summaries only for the completed
lanes listed here. It preserves those original records and failures. The previous publication-hold
version is retained byte-for-byte at
`closeout-history/pre-reconciliation-hold-0eba2185e28924a1.md.snapshot`, SHA-256
`0eba2185e28924a1b4d31f2463348f01711524c70bd77aacfe772e8179655ac8`.
This update changes only this closeout and adds that historical copy; `final-product-scope.json` and
products remain unchanged. The worker used read-only evidence inspection and ran no build, test,
formatter, native/device operation, index mutation, commit or subagent.

## Baseline and publication scope

- Active checkout: `/home/rupicola/Code/y/yubikit-core-docs-integration`, branch
  `yubikit-core-docs-integration`. The original `/tmp/opencode/core-upstream-20261111` is an inactive,
  unchanged evidence copy; use neither its files for new work nor its Git pointer for operations.
- Campaign/HEAD: `f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa`; incoming/MERGE_HEAD:
  `5746e72ed9a848d08b5782ddde025aa4a4b522ef`; common base:
  `b1362127203beda5e9c797e8648b28e2716c496c`; unique histories: 30 campaign / 74 incoming commits.
- Seventeen real conflicts (14 content, three modify/delete) were resolved among 45 changed-path
  overlaps. Independent source review passed at SHA-256
  `a39efdd36e08261d571457f42b1144f2522542df65dc40612f3b96348197a36d`, accepting product tree
  `899dade50586c55efad0031e0a001498a11f5a26`. See `resolution.md`, `deletion-review.md` and `review.md`.
- The current tracked product delta versus campaign is **289 name-status entries: 166 modified,
  91 added, 30 deleted and two renames**. Incoming versus common base had 287 entries. Two incoming
  OATH helper/Program changes now equal the campaign, while four additional integration paths are
  Cli.Shared's project, FidoTool/ManagementTool READMEs and OathToolApplication. Thus 287 is not the
  final campaign-diff count. Both rename endpoints produce **291 literal staging paths**.

`final-product-scope.json` is the exact current product allowlist and parent/current identity register.
It excludes this new integration packet from the tracked product diff; packet records require their
own final inventory after all writers finish. Its 27-path AUTO_MERGE comparison distinguishes manual
resolution/compatibility/formatter work from the 553 active-code/documentation differences relative to
incoming that also include inherited campaign history. Those populations are neither new bug counts
nor whole-Core coverage. Deletion provenance and both-parent intent remain in the accepted review.
The scope JSON's preparation-time publication-status field remains historical; the current validation
decision is this closeout's qualified acceptance. Its product identities and allowlist are unchanged.

The recorded 20-file formatter scope is whitespace-only with imports preserved; every current file
hash still matches `evidence/format-preservation.json`. Fourteen tracked files have unstaged formatter
differences. Current index **product entries**, independently compared by mode/path/blob, still match
the accepted pre-format tree. Raw index bytes can change independently of that content identity; the
original `bec87214…` recovery index hash is historical, not a new final-index assertion.

The in-memory Git-tree calculation for current product bytes, excluding the new packet, is
`5dda4f4bbedadd8bca23bb4eb0fed5bf65d841ae`; the encoder was checked against the accepted index tree.
This calculation writes no object and is not the eventual merge-commit tree, which will include records.
Scope-file SHA-256: `3f974a8216fbfa7690a62fca3cee035438816b004a15d8a0da715e0679d76420`.

## Runtime and source preservation

This integration includes substantial **incoming runtime changes**, rather than a documentation-only
delta. It preserves the fresh scoped Find API, last-known nullable CachedDeviceInfo, explicitly owned
single-producer monitor, revisioned DeviceChange replay/SnapshotComplete stream, subscriber-local
cancellation/overflow recovery and removal of global restart/generation/repository architecture.
DiscoveryDeviceInfoReader bounds caller waits; SharedDeviceInfoRead owns admission/read/native lifetime;
ProtocolDeviceInfo borrows an open connection. Linux descriptor-int ABI/fd zero, separate FIDO/OTP
owners/reactor, checked release and retained uncertain claims remain incoming runtime. Finalizers
request shutdown; deterministic disposal observes its outcome. Incoming PIV/runtime and package/native
verification changes retain their lineage; they are not new documentation-audit implementation fixes.

Fresh source comparison covers all **311 incoming shipping Core C# paths**: the sole differing file is
campaign-exact `Credentials/UserPresenceIntent.cs`. Added Base32 source and its test are campaign-exact,
as are all 13 campaign OATH shipping C# files. Necessary OATH example migration removes obsolete global
monitoring at the surviving application owner and uses the incoming shared selector while preserving
the cancellation-test seam. The reviewed **assembly-wide** `InternalsVisibleTo="OathTool"` grant in
non-packable Cli.Shared supports that constructor use; it is not a constructor-only permission or a
shipping public-API expansion. Other required active-guide/fixture migrations are individually recorded.

Current Core inventory is **515 tracked files / 498 C# files**. Core's snapshot has **1,442 unique
nonblank/non-directive entries**, preserving all 1,439 incoming entries plus the Base32 type/two
overloads. These are source/file/API identities, not semantic audit-unit counts. Current compiler/API
gates and captures establish their recorded compilation scope; inherited findings and hardware
qualifications still require the later Core audit.

## Completed validation evidence

All paths below are relative to this integration record. These are retained coordinator executions,
not fresh executions by this closeout writer.

| Lane | Actual result and authority |
| --- | --- |
| Main clean integration | `validation/commands-v2/main-gates-home-first/toolchain/20261111-195127-739-build+test+docs-qa+docs-architecture+complexity+resilience+pack/result.txt`: build PASS, zero warnings/errors; 13 canonical unit projects, **4,442 executed / five existing skips**. Core's ordinary row is **2,065 / four skips**; the older 1,787 figure is historical unit evidence, not hardware coverage. |
| Separate resilience | Same run: **197 executed / zero skipped**. Combined total **4,639 executions** includes reruns, not 4,639 unique tests. |
| Documentation/architecture/packages | Same run: **74 active docs / ten package pages**, **six map entries / seven images**, **ten locally created packages**, all passing. |
| Docs patch guards | `validation/commands-v2/docs-patch-guards-home-first/`: **six tests PASS**. |
| Complexity | Two new-relative-to-campaign findings retained with source-backed rationale in `complexity-disposition.md`; include its two exact `Complexity-Justification:` lines in the eventual commit message. No threshold or runtime refactor was used. |
| Linux external host | `validation/commands-v2/linux-publish-home-first/`: JIT/AOT publish PASS, **zero warnings**, **223 bound artifacts**. `linux-native-home-first/`: **38 native checks per runtime**, four intended-failure controls matched expected exits. `linux-evidence-gates-home-first/`: **31 tests PASS**. Physical modes: **zero**. |

Linux capture is `artifacts/linux-hid-verification/evidence/20261111T185446Z`, retained through the
native command's text-record chain. Sealed source hash:
`90b0e9ab7582316099d37d9bc8e93beca1dd64fe8ce9c44e7edb32042b7533aa`;
publish input hash: `a509bb8ab2cb61c1b27e27af7f4a27dc893fcf66106fb4ddd423307005a021f7`.
Original binary identities remain external; the package/restore/publish bindings and text captures are retained.

### Six individual compiler proofs

Each linked manifest has its original successful zero-warning build receipt, exact Csc/input/analyzer/
policy/graph/output bindings and a passing individual LIVE receipt under `validation/commands-v2/`:

| Accepted manifest in `validation/consumer-proof/evidence/` | Proof / projects | Individual LIVE receipt directory |
| --- | --- | --- |
| [probe-first-v3](validation/consumer-proof/evidence/probe-first-v3/manifest.json) | v3 / 2 | `consumer-probe-live-v3-first/` |
| [examples-first-v3](validation/consumer-proof/evidence/examples-first-v3/manifest.json) | v3 / 4 | `consumer-examples-live-first/` |
| [annotations-first-v3](validation/consumer-proof/evidence/annotations-first-v3/manifest.json) | v3 / 3 | `consumer-annotations-live-first/` |
| [consumers-first-v4](validation/consumer-proof/evidence/consumers-first-v4/manifest.json) | v4 / 22 | `consumer-consumers-live-v4-first/` |
| [fixture-first-v5](validation/consumer-proof/evidence/fixture-first-v5/manifest.json) | v5 / 4 | `consumer-fixture-live-v5-first/` |
| [mac-comparison-restore-isolated-first-v5](validation/consumer-proof/evidence/mac-comparison-restore-isolated-first-v5/manifest.json) | v5 / 2 | `consumer-mac-comparison-live-restore-isolated-first/` |

Examples compile 29 complete bodies; annotations compile the root SCP method in the real v3-derived
test context: **30 distinct bodies**, with the representative probe repeating body 01 rather than adding
a 31st. The 25 original raw extractions remain unchanged; derivatives fix three wrapper-local naming
collisions and add five complete bodies. All seven real applet tools, Mac HID route and all-module AOT
verification source hosts participate in the consumer traversal. The Management integration fixture
and Tests.Shared are **compile-only**; no integration test body, annotation or documented device example
was executed. Project counts repeat dependencies across proofs and are not unique coverage totals.

Mac's actual profile stage is
`validation/commands-v2/consumer-mac-comparison-restore-profile-restore-isolated-first/`; it verifies
the retained three-property descriptor, actual `[1.18.1, )` dgspec/assets, integration variant and
unchanged admission Core DLL before the two-project capture/LIVE. NativeShims remains canonical 1.18.1.
Compiling Mac hosts and cross-platform SDK code on Linux supplies no macOS/Windows runtime or certification proof.

The six individual checkpoint passes remain valid historical outcomes. Their final reconciliation
has now passed under the following disclosed scope, without rewriting the original strict-live results.

### Final current-source/exact-retained-output acceptance

Independent review:
`validation/consumer-proof/final-reconciliation/independent-review.md`, SHA-256
`6a7da05245711db4710dd53a79af62e7fd29f7bbea40cfb5c267ea9483af5567`.
Actual coordinator execution: `validation/commands-v2/consumer-final-reconciled-first/receipt.json`
is **passed / exit 0 / complete / quiescent**. Its report is
`validation/consumer-proof/final-reconciliation/coordinator-final-first.json`, bound to reviewed scope
SHA-256 `d6b559e263ed217de91bb9f0dd6a92a9fd0b1d51f198d7a26566fcfde30ce491`.

| Context | Exact retained predicates | Strict current-path result | Reviewed drift occurrences | Qualified scope |
| --- | --- | --- | ---: | --- |
| Probe | pass | fail | 7 | pass |
| Examples | pass | fail | 17 | pass |
| Annotations | pass | fail | 12 | pass |
| Consumers | pass | fail | 108 | pass |
| Fixture | pass | pass | 0 | pass |
| Mac comparison | pass | pass | 0 | pass |

All **21,965 retained record occurrences** verify exactly; this is not a unique-artifact count. The
**144 disclosed drift occurrences** are restricted to 40 exact old-record keys at seven shared Core
implementation DLL/PDB paths, including Management/PIV output copies. Every approved current value
matches an explicitly named successful captured state at the same full path. Original binaries remain
retained and mandatory. There is no source, generated-input, configuration, analyzer, restored-input
or reference-assembly drift; unapproved drift is zero.

No originally captured tool changed or vanished. The old root tool glob grew only by the closed,
hash-bound approved additions: proof-v4/proof-v5 for the v3 captures and proof-v5 for Consumers v4;
Fixture/Mac v5 need no additions. Every original tool identity and the exact additive population were
checked before projecting the historical inventory into its original versioned verifier. The review's
negative checks reject a missing original retained binary and an unknown current output hash.

Observed Core Csc arguments differ only in the attempt-specific `/generatedfilesout` directory;
retained generated-source content and reference-assembly identity agree. Implementation DLL/PDB
bytes nevertheless differ. Neither a cause for each difference nor binary/runtime equivalence is
asserted. Documentation bodies compiled against their exact recorded outputs and retain exact current
source/input bindings; this is the reviewed compile-evidence scope amendment, not a reconstruction
of simultaneous equality at overwritten paths. The original strict-live checks remain failed for
the first four contexts, and their earlier individual LIVE passes remain checkpoint-specific.

## Recovery, corrections and evidence authority

`recovery/handoff.md` and `recovery/independent-review.md` bind the quota diagnosis, all **10,558**
copied entry identities and the private-admin change limited to this worktree's backlink. They retain
the original partial failed build/logger data. The reviewed home-backed logger is
`validation/run-command-v2.py`; it records actual owned-process quiescence. Frozen preservation passed
for **eight manifests, 5,366 artifacts and 114 prior PIV integration files in each active/primary
checkout**, plus the primary's four original user files and campaign HEAD. Fresh final preservation
and staged-byte proof remain publication checks, not implied by those earlier receipts.

`consumer-validation-preparation.md` maps complete bodies, real project ownership and proof bounds.
The correction handoffs and independent reviews under `validation/consumer-proof/correction-v2/`
through `correction-v5/` preserve namespace lookup, PublicAPI occurrence multiplicity, selected-target
versus catalog ownership, explicit unused potential assets and precise direct-Core copy classification.
They retain strict actual Csc membership/content, canonical analyzers, policy, graphs and used-input
ownership. No old capture is relabelled with later tools or rebuilt merely to repair bookkeeping.

`validation/consumer-proof/mac-restore-blocker/` preserves the original 401/wrong historical restore
profile, cache/content evidence, receipt-linkage review (`routing-v2-review.md`) and the separate
pre-admission `inspect.py` shadowing correction (`launch-import-correction.md`). Its v3 caller applies
`-P` only to the two routing-helper invocations and retains the caller correction receipt. The actual
correct-profile compilation/capture then passed. No credential/feed/product setting was changed.
Every original failed attempt remains separate from the successful outcome and its scope. The original
`validation/commands-v2/consumer-final-probe/` failure and its preserved inputs remain failed evidence;
the reviewed reconciliation accounts for additive tools and finite shared-output replacements without
relabeling that failure or claiming that all six original strict-live conditions now pass.

## Remaining release path — coordinator only

1. Carry the accepted final scope qualification and original failures into the final integration
   evidence review and publication summary. The six-context current-source/exact-retained-output gate
   is complete; the original six-context strict-live condition remains unsatisfied as disclosed above.
   Preserve the reviewed scope/report and exact retained outputs; any later unapproved identity change
   requires a new disposition, rather than automatically enlarging that scope or rebuilding evidence.
2. After all writers finish, recheck current source/scope identities, relocated protected bytes and
   the four primary user files. Use `validation/check-preserved-relocated.py` and the final record
   snapshot/check/staged workflow in `validation/check-records.py`. Keep UTF-8 text/snapshots and
   byte-preserving attributes in records; keep binaries, caches and private admin backups external.
3. Use the **291 literal product paths** in `final-product-scope.json` as the staging allowlist,
   including both rename endpoints and already-reviewed deletions. Stage the 14 formatter worktree
   updates by name; retain and verify already-staged removals rather than blindly adding absent paths.
   Add only the separately inventoried integration packet. Verify final staged product bytes and all
   record identities; the old pre-format index/tree is insufficient. Leave unrelated user/worktree
   files outside staging.
4. Create the non-rewriting merge checkpoint with campaign and incoming as its two pinned parents,
   including both complexity justification lines. Verify commit ancestry and remote identity after
   publication; record the **actual published integration SHA**. This report invents no commit SHA.
5. Recheck primary ownership/user bytes, then fast-forward the campaign to that published checkpoint
   and publish/verify campaign remote ancestry. Preserve the original four user files. Use no rebase,
   force push, branch reset or other-worktree cleanup. Stop on competing campaign work rather than
   replacing it.
6. Only after that published baseline, re-read actual root/Core/nested instructions and establish the
   fresh whole-Core semantic inventory and inherited/boundary registry. The 515/498 merged file counts
   above replace the 513/496 upstream orientation counts; neither is semantic coverage. PIV's physical
   deferral remains in force. User-announced Windows CCID/HID work continues separately; no new ready
   pin was supplied. Continue this pinned integration and assign a separately pinned delta review when
   that handoff arrives, without touching its workers or waiting for an unspecified branch.
