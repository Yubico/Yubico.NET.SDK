# Independent bounded review — v4 selected-target asset collector

## Decision

**PASS for the existing `consumers-first` source/predicate correction. One required narrow correction
remains before treating this packet as ready for the pending Mac-comparison lane.**

The independent read-only check exactly reproduces all 22 current compiler-project results and 21
independent graph results. The catalog/selected-target correction is justified, the previous strict
guards remain intact, and no new blocker was found for the proposed consumers recapture itself.
However, the collector recognizes the pending comparison's declared direct Core compiler reference
without recognizing the corresponding copy-local asset. See **R1** below. Do not interpret the passing
consumers matrix as readiness of that uncompiled lane or execute the entire handoff unconditionally.

Reviewer: independent Sol consumer-proof review, 2026-11-11. Only this review file was written.
No SDK build/restore/test, real capture, formatter, native/device execution, product change, index/ref
change, commit or delegation was performed. No compiled workspace, original record, earlier proof,
receipt or tool was modified. Actual v4 filesystem capture/live acceptance remains pending.

## Exact reviewed identities

Paths below are relative to this integration's `validation/consumer-proof/` directory.

| File | SHA-256 |
| --- | --- |
| `proof-v4.py` | `0ca001505c2c3b121687146a0660f78639b78d7c5680c6a034bdd47f14719fff` |
| `correction-v4/assets.py` | `f0432cbe6a8a88312c97b29c8f7982ebe62afc8ea45dbf476fbbddbfc3abd7ab` |
| `correction-v4/receipt.json` | `ac7a9dcc15510dc0dd8814ec5e79b54378c8b03280aff51cdb0b23ebed2d6325` |
| `correction-v4/handoff.md` | `8b5083486773c1298c4542ed6da51715026bbe387db2b3390321716dd463465f` |
| `correction-v4/preflight-v4-final.json` | `2c125d641f167313e5c03998ac7305e2748f8f906cc480a705f75a8b76285609` |
| `correction-v4/consumer-preservation/manifest.json` | `05dc14bd6be66a1f021cd25337cee44d0c6a2ff2debd6c6986d8d6e71f07cc88` |
| `correction-v4/lane.sh` | `e4f1937a52e38aa660ebaee6bfe71dcb349bab7ab391b1ac046ad3590dd3bfb1` |

The effective transformed base-source identity is
`53ba554a3b08d9ddc923b85df9f944b948d69ed0f5589392dfe0ca7dfedfd27f`.
Executed the v4 receipt check independently; all protected-original/new-file hashes, exact counted
transformations, prior v3/v2 receipts and effective-source identity passed. Earlier independent review
pins remain unchanged, including v3 review
`be8ee285e203c68d68b26e0a965f7e06d0c8a75b72fd6fe30ca49e8db51ec794`.

## R1 — missing direct Core copy ownership in the pending comparison lane

**Required before accepting the handoff's Mac-comparison scale-up as source-ready. Not an observed
Mac build/capture failure; that lane has not been compiled or captured in this assignment.**

Evidence and exact gap:

1. `verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj:17-19` declares
   `<Reference Include="Yubico.YubiKit.Core">` with `<HintPath>$(BoundaryCorePath)</HintPath>`.
   It has no ProjectReference and no `Private=false` metadata.
2. The sealed prepared `mac-comparison/src/...MacComparison.csproj` traverses that project with
   `ReferenceOutputAssembly=false`, setting BoundaryCorePath to the actual active Core `bin/Release/net10.0`
   DLL, native version 1.18.1 and the integration variant. This does not turn the child's direct assembly
   reference into a child ProjectReference output.
3. MSBuild's ResolveAssemblyReference CopyLocal rules default an ordinary local reference with no
   Private override to copy-local. Context7 `/dotnet/msbuild`, `ResolveAssemblyReference.md` and
   `Controlling-Dependencies-Behavior.md` confirm this distinction. The selected installed SDK's
   `Microsoft.Common.CurrentVersion.targets:2509` emits CopyLocalFiles into ReferenceCopyLocalPaths;
   `_CopyFilesMarkedCopyLocal` at lines 5086-5098 copies those inputs to output. The unchanged capture
   hook records ReferenceCopyLocalPaths.
4. `assets.py:149-153` has a declared-comparison-Core-DLL **compiler-reference** branch, but
   `assets.py:170-187` has no corresponding **copy** branch. A non-package copy is admitted only by
   matching `resolved-project-references.txt` output ownership at lines 180-183. The comparison's
   direct Core DLL is neither a package asset nor a resolved ProjectReference output.
5. Independently exercised just that copy-owner branch with an in-memory single direct-Core copy and
   no ProjectReference outputs. It deterministically raises:

   ```text
   Resolved non-package copy lacks its actual project-output identity: /home/rupicola/Code/y/yubikit-core-docs-integration/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
   ```

   This was an isolated source-branch check, not an actual or simulated successful Mac compiler proof:
   target selection was bypassed only to reach the copy-owner predicate, no disk inputs were changed,
   and no claim was made about unobserved future copy arrays.

Bounded disposition: recognize the same exact declared direct-Core source in the **copy** path, restricted
to the actual comparison context/BoundaryCorePath and its prepared declaration, with retained actual
source/output identities and byte equality. Any admitted corresponding sidecar must also be tied to
that precise source/output provenance, not accepted by filename alone. Preserve the existing rejection
of other unclassified copies and package ownership checks. Do not classify Core as a package, disable
CopyLocal, edit the product/project, or silently skip the copy. A narrow additive verifier revision can
address this without rebuilding the successful consumers attempt or mutating this reviewed packet.

The current packet can be used for a **consumers-only** recapture decision with this explicit limit;
R1 prevents an unqualified whole-handoff PASS. The pending fixture also remains actual-proof pending,
but no separate concrete fixture correction was established by this review.

## Selected-target model and actual ownership checks

The original v3 failure is correctly classified as a collector-model failure after a successful
zero-warning consumers build: catalog lookup assumed every global library had a neutral target node.
The runtime ILCompiler catalog entry belongs to a different restored target, so that assumption was
false. V4 does not mask it with `except KeyError`, a union of all targets, or host-platform inference.

Independently checked the collector and its actual full results:

- TFM selection requires one matching restored framework/alias and consistent restore metadata.
  Emitted dependency runtime-target name, the actual Csc-generated TargetFramework attribute and both
  output-path suffixes corroborate selection. Missing deps evidence is not silently allowed: it is
  opened and validated. Selected SDK ResolvePackageAssets wiring is source-bound and retained.
- **All 22 observed compiler contexts select neutral `net10.0`.** The two AOT-capable verification
  projects also restore `net10.0/linux-x64`, but no RID-bearing compilation is claimed. No multi-inner-
  build capture, RID fallback interpreter, AOT publication or platform qualification is inferred.
- Every selected node must have consistent catalog type metadata. Selected packages require exactly
  one installed root; ambiguous roots fail. An actual package-owned input outside the selected target
  fails. Actual package file membership and selected compile/copy asset roles are checked.
- Actual package Csc references equal the selected package compile-DLL set. Expected compile paths
  are resolved, and actual ownership is independently checked; an escaped/unclassified input does
  not become accepted merely because its spelling appeared in a catalog. Actual analyzers must be
  selected-package analyzer DLLs or admitted framework-manifest Analyzer entries.
- Non-package references are restricted to actual project-reference ownership, the explicitly declared
  comparison Core reference, or installed SDK framework-pack files. FrameworkList entries, restored
  framework reference and emitted TFM are checked. The unchanged graph proof separately binds real
  project closure/edges and actual reference/output identities.
- Actual package copies must belong to selected runtime/native/runtimeTargets/resource/compile nodes
  and catalog files. Non-package copies require actual project-output ownership, subject to R1 above.
  Copy destinations cannot escape TargetDir; conflicting destinations fail; source/output hashes must
  match. No observed current copy or compiler input is silently left unclassified.
- Complete restore catalogs are retained. Catalog-only rows, unused selected candidates and tool
  downloads have explicit dispositions; they are not automatically treated as actual application
  inputs. NativeShims remains required for real consumers, with only the two named non-reference-
  producing traversal hosts distinguished under their explicit project declarations.

Eight download declarations are explicitly reported, four in each AOT-capable host. Declarations with
no catalog/application input are honestly just declarations. Each runtime ILCompiler row has an exact
download declaration, `hasTools`, other-target membership, no observed current compiler/copy use and
an explicit restore-only disposition. No claim is made that every declared download was executed,
loaded, linked, or copied into the application.

## Distinct hash domains and preserved failures

The two AOT projects use the same installed runtime ILCompiler archive. Its actual values remain:

- Catalog / installed version-2 content metadata:
  `bxsHJDYsOBbOO/UnDDm6zG4Osn5EOW7bk8VBmCRH4r4RBkD5KerbNxYFFk2Pw7uRhqOaXACel0IoancPxgig4w==`.
- Streamed archive SHA-512 / `.nupkg.sha512`:
  `dCRuO3McIYAtevwTUa2sbCxAu0ckBHElMekY0gye4871a7/zySLD2lP2pUVHMvCSvT9WngI14uKWDkGwRQVsSQ==`.

The current collector verifies those domains independently; it does not force equality between them.
The archive is an unused tool-package identity, not a compiler/native execution artifact. No signing
trust or authenticity qualification is claimed. The draft's erroneous cross-domain equality, its
failed preflight, exact draft source and diagnosis snapshots remain receipt-bound. This offline
diagnostic failure is not another failed SDK build, and the original v3 capture failure is not erased.

## Fresh independent reproduction and preservation

Constructed v4 predicates directly and checked active-build exclusion, original/v2/v3/v4 seals,
current prepared inputs, original successful consumers build receipt and exact raw compiler closure.
Ran both complete compiler-project and independent graph paths using the same read-only identity/text
collector. A Python audit hook forbade write opens, filesystem mutation and external-process execution
during this full reproduction. No actual capture/verify entry point or filesystem Retainer was called.

Results exactly reproduce `preflight-v4-final.json`:

| Check | Independent result |
| --- | ---: |
| Complete compiler-project paths | 22/22, zero failures |
| Real project paths (excluding traversal host) | 21 |
| Independent graph paths | 21/21, zero failures |
| Identity/text-only collector records | 14,133, exact equality with sealed report |
| Pending declaration/available-restore descriptors | Exact equality with sealed report |

The membership is **9 declared consumer roots**, their **21-project real transitive closure**, plus
**one generated Consumers traversal host**. The report's `real_projects: 9` field is the root count
computed from `len(data['contexts']['consumers'])`, not the complete closure. The handoff's 21-real-plus-
one-host description is accurate; neither nine nor 22 should be reported as 21 real root selectors.
The matrix covers the seven real tools, Mac HID route verification, all-module NativeAotVerification,
their actual dependencies and traversal host. It does not include pending Mac comparison or fixture.

Independently hashed and checked:

- **7,713 current original/preserved record pairs**, including **5,778 binary records**: zero mismatches.
- **6,939 prior retained records** across six original proof/preservation manifests: zero mismatches.
- Included original probe preservation: **981 records / 411 binary records**, still intact.
- All **nine archived successful project restore snapshots**: neutral-only net10.0 shapes with no
  catalog rows outside their selected framework graph, matching the recorded historical inspection.

Earlier accepted proofs were checked against their **retained** identities, not rebound to mutable
obj/bin paths subsequently replaced by authorized compilations. They remain historical immutable v3
proofs. No newer tool list was inserted into them, no v4 label was retroactively applied, and no SDK
rebuild is required merely because the post-build collector changed. This does not assert a fresh
current live verification of those older mutable output paths under the expanded tool inventory.

## Original guard preservation and caller consistency

Textual/AST comparison of effective v3/v4 base sources changes only `compiler_project` and `capture`:
the asset collector now also runs for the traversal host, and new manifests identify v4. One
`restored_binary_bindings` function is explicitly replaced in the execution namespace. Original strict
Csc source/reference/analyzer/config uniqueness and equality, PublicAPI occurrence multiset, namespace
and exact import/Exists policy, warnings-as-errors/skip policy, source/body inclusion, SDK/compiler,
graph, emitted XML member/body, actual output and retained/live-byte guards remain unchanged.
New helper/preflight/wrapper/receipt identities join capture tools; earlier compilation tools are not
rewritten. `bash -n correction-v4/lane.sh` passed without executing the script.

The reviewed consumers-only caller commands consistently bind the original build and raw compiler
attempt to a new evidence destination:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
python3 -B "$P/proof-v4.py" static
python3 -B "$V/run-command-v2.py" --label consumer-consumers-capture-v4-first -- \
  python3 -B "$P/proof-v4.py" capture --context consumers --attempt consumers-first \
    --capture-name consumers-first-v4 --build-label consumer-consumers-build-first
python3 -B "$V/run-command-v2.py" --label consumer-consumers-live-v4-first -- \
  python3 -B "$P/proof-v4.py" verify --attempt consumers-first --capture-name consumers-first-v4 --live
```

These commands were reviewed, **not executed**. `consumers-first-v4` changes only evidence routing,
not the actual raw attempt, original successful build selector or strict context/closure checks.
Verify additionally binds attempt, evidence name, v4 revision and receipt hash before original live
identity checks. Any real failure must remain retained. Do not rebuild consumers for bookkeeping.

The remaining-lane wrapper correctly limits new compilation to mac-comparison/fixture, uses original
hook/environment routing and refuses existing attempts/evidence. Its syntax is valid, but **R1 must be
disposed before using that wrapper as an unconditional next-step readiness claim for Mac comparison**.
The fixture's available main-gate restore and xUnit build-content declarations are only source/shape
observations; actual fixture Csc/copy policy acceptance remains pending. Unrecorded build-content
copies and full output-tree bytes are not falsely called NuGet runtime assets.

No whole-Core/source-integration audit was repeated. Late Windows work, AOT linking, actual Mac/native
runtime execution and hardware qualification remain outside this bounded source/evidence review.
