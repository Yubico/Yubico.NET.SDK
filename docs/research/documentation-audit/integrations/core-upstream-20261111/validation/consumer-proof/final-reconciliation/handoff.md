# Final mixed-version reconciliation — exact scope, not relabelled live equality

## Decision for independent review

Use one bounded final read-only adapter for all six immutable captures. It validates each capture's
**exact original tool paths/hashes** and its original revision/receipt/load graph, then separately
checks a finite reviewed additive inventory. It invokes the original retained verifier and original
strict-live verifier. It never rewrites an old tool list, manifest, output or success record.

The proposed final acceptance scope is **current source/inputs + exact original retained compiled
outputs**. It is not “every historical mutable bin path is still identical.” Original strict-live
failures stay explicitly false. No SDK rebuild, recapture or bookkeeping replacement is required.
Independent review is still required before coordinator adoption of this final scope.

## Actual root cause and complete matrix

The original `consumer-final-probe` failure is preserved in `original-final-probe-failure/`. The v1
verifier compared a captured tool array against a newly enumerated `*.py` directory population.
Later version files expanded that metadata population; **no captured tool file changed or vanished**.

| Context / actual capture version | Captured tools | Current same-version enumeration | Exact additive files | Changed/missing captured tools |
| --- | ---: | ---: | --- | ---: |
| probe / v3 | 16 | 18 | proof-v4.py, proof-v5.py | 0 |
| examples / v3 | 16 | 18 | proof-v4.py, proof-v5.py | 0 |
| annotations / v3 | 16 | 18 | proof-v4.py, proof-v5.py | 0 |
| consumers / v4 | 21 | 22 | proof-v5.py | 0 |
| fixture / v5 | 28 | 28 | none | 0 |
| Mac comparison / v5 | 28 | 28 | none | 0 |

`scope.json` pins these exact additive paths and hashes. Another future addition, absent/changed
original, changed source/receipt or unapproved inventory fails. The later files are explicitly
external to earlier execution inventories, not claimed to have participated in old compilation.
The final helper stays in this subdirectory, so it does not itself enlarge the old root `*.py` glob.

The original verifier is called after the exact inventory checks with a captured-inventory projection.
This is not an unconditional tool-guard bypass: current population must equal captured paths **plus
exactly the reviewed additive set**, and every actual original/additive row and file identity must match.
Original versioned source/receipts, namespace imports and collector replacements remain hash-bound;
the actual original verifier still checks its source seal, current prepared inputs, all retained bytes
and graph uniqueness. No Csc/used-asset/analyzer/version or source-owner check is removed or fabricated.

## Retained versus current location results

All **21,965 original retained record occurrences** are present and byte-exact. No historical binary
was recreated, rebound to a replacement or skipped when missing. The six records remain their actual
v3/v4/v5 proofs and their actual successful build/capture/live receipts remain pinned.

| Context | Records | Retained predicates | Current live drift occurrences | Original strict all-current-path result | Proposed scoped result |
| --- | ---: | --- | ---: | --- | --- |
| probe | 887 | pass | 7 | **fail** | pass |
| examples | 1,559 | pass | 17 | **fail** | pass |
| annotations | 1,416 | pass | 12 | **fail** | pass |
| consumers | 14,234 | pass | 108 | **fail** | pass |
| fixture | 2,846 | pass | 0 | pass | pass |
| Mac comparison | 1,023 | pass | 0 | pass | pass |

The **144 occurrences are seven logical paths**, not 144 independent binaries:

- Core `bin/Release/net10.0/Yubico.YubiKit.Core.dll` and `.pdb`.
- Core `obj/Release/net10.0/Yubico.YubiKit.Core.dll`.
- Core DLL/PDB copies under Management's bin directory.
- Core DLL/PDB copies under Piv's bin directory.

Every replacement is binary, inside a captured project's declared build tree, and its **current exact
bytes are represented by another successful captured state** in this six-context packet. Nothing is
attributed merely by filename or assumed legitimate because it is under bin/obj. The finite scope
lists the old record identity, current byte identity and matching captured states. A new/unlisted
replacement, missing retained file, source/input change, or current bytes absent from the approved
captured states fails the scoped result. These are matched states, not filesystem-writer monitoring.

There is **zero** live source, analyzer, reference-assembly, config, restored-input or generated-input
drift. `output-drift-evidence.json` compares the original retained Core compiler observations:
the only recorded Core argument difference is `/generatedfilesout:<attempt-specific directory>`;
generated source hash maps and Core reference-assembly hashes are equal. Actual implementation/PDB
bytes nevertheless differ. This is not a claim of byte equality, proof that every runtime behavior
is equivalent, or permission to replace the older exact compiled artifacts. No source/API change was
observed; a future one would fail the unchanged source/input checks and cannot borrow this scope.

## Actual predicates exercised, not another compiler capture

`preflight-first.json` contains one full read-only pass through all six versions' original verifier
paths after the narrow tool-population correction:

- Each original revision receipt and complete captured context identity passes.
- Original source/preparation seals and current-input checks pass.
- Original retained-byte/graph predicates pass for all six.
- Original strict-live verification is invoked and preserved as four failures/two passes, as above.
- All live record mismatches are collected in one matrix pass, with zero unapproved drift.

`matrix-first.json` retains the exhaustive source/tool/retained/current comparisons, including every
live difference rather than only the original verifier's first error. `scope.json` and the final
adapter never invoke SDK build, capture, native/device, test or formatting entry points. An audit hook
forbids writes/process execution except the explicitly named new reconciliation report. This source
preflight is not relabelled as a new actual compilation/capture or runtime qualification.

The Mac caller provenance is explicitly pinned: the actual `restore-isolated-first` routing descriptor,
three-property admission record, P-enabled actual v3 launcher, unchanged creator and retained caller-delta
receipt. No credential/config/authentication state is accessed or changed. Earlier package/feed/import,
capture-helper and outside-logger failures remain untouched, including the first final-probe failure.

## Files and coordinator command after review

- `matrix.py`: read-only diagnostic enumeration and full record comparison.
- `reconcile.py`: the single final adapter; does not copy or replace the large prior proof framework.
- `scope.json`: exact six-capture/source/load-graph/addition/replacement binding.
- `output-drift-evidence.json`: original retained Core argument/generator/reference-API comparison.
- `preflight-first.json`: complete qualified matrix, not a new SDK/capture result.

After independent acceptance of this explicit final scope, run once through the canonical logger:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
python3 -B "$V/run-command-v2.py" --label consumer-final-reconciled-first -- \
  python3 -B "$P/final-reconciliation/reconcile.py" \
    --scope "$P/final-reconciliation/scope.json" \
    --output "$P/final-reconciliation/coordinator-final-first.json"
```

The report must be described as **current-source/exact-retained-output verification**, with strict-live
status reported separately. It is not an unqualified six-context “all historical paths still live” PASS.
The output name/label is fresh and never overwritten. No scope update is automatic if source, tool,
captured record or shared current binary state changes. Main gates, accepted integration source and
the retained hardware/native/platform/Core-audit boundaries remain unchanged.
