# Consolidated v5: actual selected inputs and the declared direct-Core copy

## Decision and limits

One v5 receipt covers **two orthogonal collector corrections** over unchanged, accepted v4:

1. Recognize the pending comparison's exact declared Core DLL copy ownership, without treating it as
   a package or ProjectReference output.
2. Stop requiring every potential restored compile asset to appear in the actual compiler references.
   Every **actually consumed** reference remains subject to exact evaluated-item/Csc equality and
   selected package/project/framework ownership and content identity.

The original SDK builds passed with zero warnings. Consumers v4 capture/live already passed. Fixture
capture failed a false consume-all invariant after its successful build; Mac has not been built or
captured. No product/API/runtime fault is established. This packet is source/read-only preparation,
**not actual v5 capture/live acceptance**. It does not introduce another general NuGet/MSBuild interpreter.

## Preservation and failure classification

`fixture-preservation/manifest.json` preserves **2,079 current original identities** for the successful
`consumer-fixture-build-first` build, exact `fixture-first` compiler observations/output bytes and failed
`fixture-first-v4` capture. It also verifies **23,256 previously retained records**, including the accepted
v4 consumers capture and earlier immutable examples/annotations/probe evidence. It does not rebind
earlier proofs to later mutable Core bin/obj files. External binaries stay in the home runtime.

`preserve-fixture.py` is a source-hash-pinned routing-only adapter to the existing correct capture-failure
preserver. The failed-build-only function was not invoked. Earlier capture failures, preservation
receipts, namespace/AdditionalFiles fixes, independent reviews and the prior retention-invocation misuse
remain unchanged. The initial unsealed v5 copy-only draft is also preserved in this fixture receipt;
the subsequent user-directed consolidation happened before any v5 receipt or actual capture.

No SDK build, restore/test, actual capture, formatter, native/device operation, product edit, index
change, commit or delegation was performed by this owner.

## Fixture root cause — potential assets are not mandatory consumed inputs

`fixture-compile-difference.json` records the exact difference for all four actual contexts:

| Actual context | Potential selected package compile DLLs | Actual package Csc references | Unconsumed |
| --- | ---: | ---: | ---: |
| Core | 10 | 10 | 0 |
| Management | 10 | 10 | 0 |
| Tests.Shared | 23 | 23 | 0 |
| Management.IntegrationTests | 37 | 34 | 3 |

All three unconsumed candidates are from `Microsoft.TestPlatform.ObjectModel/18.0.1/lib/net8.0/`:

| DLL | Bytes | SHA-256, identical to the actually selected TestHost copy |
| --- | ---: | --- |
| Microsoft.TestPlatform.CoreUtilities.dll | 99,360 | `7837f9df4b11a9844143fc9419e5752bdd50305436adcbf84f82b8a52d5909d7` |
| Microsoft.TestPlatform.PlatformAbstractions.dll | 51,784 | `668c7f0df2c30feac9cea9dce808ccabb72ff575c1e820404124212b4eae1773` |
| Microsoft.VisualStudio.TestPlatform.ObjectModel.dll | 335,392 | `f6d6bc01a85fd68a0599533fcc0de7357922051bad90012b8ad15de31a4ef5e9` |

The actual references/Csc use `Microsoft.TestPlatform.TestHost/18.0.1/lib/net8.0/` paths. Their raw
ReferencePathWithRefAssemblies metadata records `ReferenceSourceTarget=ResolveAssemblyReference`,
assembly version **15.0.0.0**, neutral culture and token `b03f5f7f11d50a3a`. Exact byte equality establishes
content equivalence; no guessed package-version/assembly-version/SDK-version equality is used.

Selected SDK `Microsoft.NET.ConflictResolution.targets:40-64` explicitly filters Reference, Analyzer
and ReferenceCopyLocalPaths lists through ResolvePackageFileConflicts before later compiler inputs.
This establishes why restore candidates and final evaluated inputs need not be bijective. The exact
tie-break that chose TestHost rather than the byte-identical ObjectModel path is **not recorded in the
available logs**, so this packet does not assert which resolver step or preference chose it.

The correction is semantic, not a three-filename exemption: `compile-candidates.py` requires actual
package references to remain within selected compile candidates, while recording **every potential
candidate's package/target identity and consumed/unconsumed status**. Unconsumed materialized files
receive their own content identities and same-filename actual alternatives with byte-equality facts;
otherwise no filtering reason is invented. An unmaterialized unused declaration is not relabelled as
a missing compiler input. Actual inputs still must exist and satisfy the unchanged strict ownership
checks. No source project or generated library is changed to satisfy the verifier.

`fixture-preflight-first.json` runs all four complete compiler-project predicate paths and independent
graphs read-only: **four projects, zero failures**. A Python audit hook forbids write opens, mutation
and subprocesses except the explicitly named report. The same identity/text-only collector returns
`retained: null`, never a filesystem capture. The report includes full candidate dispositions, actual
winner metadata and all original Csc/policy/restore/framework/native/graph/XML/source/output predicates.

## Mac R1 — exact declared comparison Core copy ownership

The v4 independent review is SHA-256
`e2f0928dc50e756bf5b66f1c2052ba47b97c8f2a717668eb12ceeaf52afdf140`.
Its consumers PASS remains intact. R1 identifies the source-known pending comparison gap: ordinary
`Reference Include="Yubico.YubiKit.Core"` / `HintPath=$(BoundaryCorePath)` has default copy-local behavior,
but no ProjectReference output owner. The prior collector already admitted this compiler DLL and then
rejected its corresponding non-package copy.

The new copy branch admits only:

- The exact `verification/MacOSHidBoundaryComparison/...csproj` context and current active Core
  `bin/Release/net10.0/Yubico.YubiKit.Core.dll` property.
- Its unchanged, unconditional direct Core HintPath/CopyLocal declaration and the sealed wrapper's
  exact Core path, native version **1.18.1** and integration variant. The historical async default
  remains unchanged; the wrapper supplies the actual profile.
- The **actual Csc Core input identity** already recorded by the collector. The current source DLL
  must still match that identity; the unchanged copy tail then checks actual source/output hashes.
- If actually observed, only the precise sibling Core `.pdb` and `.xml` output paths. They are coupled
  to the exact declared DLL/project/context and their own source/output content hashes. They are not
  accepted by basename alone, and no PDB semantic/signature association is claimed. Selected SDK RAR
  related-file defaults explicitly include `.pdb` and `.xml`; no broader sidecar exemption is added.

Package ownership and all other unknown-copy rejection remain unchanged. The comparison's own project
is non-packable, non-SDK-library and has GenerateDocumentationFile=false; its local props separate the
variant's obj/bin output. Root's library-only IsAotCompatible block is not turned into a general expected
policy. Actual Csc/analyzer policy continues to be checked as captured. Its `CopyBoundaryCore` target
is AfterTargets=Publish, not a replacement for ordinary build CopyLocal ownership. No other direct
assembly reference is declared in this source packet.

`mac-source-cases-first.json` exercises the **exact transformed copy-loop AST**, not an approximation,
using only registered retained consumer DLL/PDB/XML bytes and stable project/wrapper source. Ten
isolated cases match their required outcomes: three positive DLL/precise-sidecar cases and seven
negative wrong-project, missing-HintPath, wrong-property, missing-Csc-input, changed-input-DLL,
wrong-output-bytes and unrelated-source cases. XML and wrong bytes are mutated in memory only.
The script never reads mutable current Core bin contents as an old consumer binding. This is a
source-contract check, **not an actual or simulated complete Mac compiler/copy qualification**.

## Versioning and unchanged guards

`proof-v5.py` pins accepted v4 and its collector. Three exact collector source edits add the direct-copy
branch, replace the false potential-equals-consumed requirement with explicit actual-subset/dispositions,
and add the disposition field. New manifests identify v5; all other base capture predicates remain.
Namespace handling, canonical NetAnalyzers DLLs/imports, warnings-as-errors, source/reference/analyzer/
config uniqueness and exact Csc/evaluated membership, PublicAPI occurrence multisets, selected-target
and framework identity, graph, output/source/XML and live-byte guards are not relaxed.

Old v4 source/receipt/captures remain immutable. No accepted 30-body/examples/consumer build is repeated
for bookkeeping. Fixture v5 must reuse `fixture-first`; only the uncompiled Mac lane needs a new build.
Actual Mac/native behavior, AOT linking, Windows concurrency work and whole-Core audit remain separate.

## Coordinator commands after one fresh consolidated review

First reuse the successful fixture build and existing raw compiler workspace:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
python3 -B "$P/proof-v5.py" static
python3 -B "$V/run-command-v2.py" --label consumer-fixture-capture-v5-first -- \
  python3 -B "$P/proof-v5.py" capture --context fixture --attempt fixture-first \
    --capture-name fixture-first-v5 --build-label consumer-fixture-build-first
python3 -B "$V/run-command-v2.py" --label consumer-fixture-live-v5-first -- \
  python3 -B "$P/proof-v5.py" verify --attempt fixture-first --capture-name fixture-first-v5 --live
```

After acceptance, reserve the checkout and compile/capture the sole unexecuted Mac context:

```bash
bash "$P/correction-v5/lane.sh" first
```

The wrapper uses the original hook/bodies, exact `Yubico.YubiKit.AuditIntegration20261111.MacComparison`
selector, `mac-comparison-first` actual compiler attempt and new `mac-comparison-first-v5` evidence.
It stops on any failed stage. No actual v5 capture/live or Mac build has been executed by this owner.
