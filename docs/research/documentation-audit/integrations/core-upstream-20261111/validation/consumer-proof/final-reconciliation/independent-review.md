# Independent final reconciliation scope review

## Decision

**PASS for the explicitly qualified current-source/exact-retained-output scope. No required correction
found within that scope. This is not a six-context strict-live PASS.**

The reconciliation is suitable as the documentation compile-evidence gate's final, expressly reviewed
scope amendment: current bound sources/inputs remain exact, all original successful compiler artifacts
remain retained byte-for-byte, and the finite current output replacements are explicitly disclosed.
It does not establish equivalence between different implementation binaries or erase any original
strict-live failure. Original individual capture/live successes remain historical facts, not newly
relabelled final live successes.

Reviewer: independent Sol final-scope review, 2026-11-11. Only this review file was written. No SDK
build, capture, test, native/device operation, formatter, product edit, index/ref change, commit or
delegation occurred. No original manifest, receipt, output, tool list, failed record or scope was changed.
The coordinator's fresh final logged reconciliation remains to be executed.

## Exact reviewed identities

Paths below are relative to this integration's `validation/consumer-proof/` directory.

| File | SHA-256 |
| --- | --- |
| `final-reconciliation/scope.json` | `d6b559e263ed217de91bb9f0dd6a92a9fd0b1d51f198d7a26566fcfde30ce491` |
| `final-reconciliation/reconcile.py` | `dff0d4dad280a81cebd721e5baf2c9839d9dc208def68cef197930bf80c20a11` |
| `final-reconciliation/matrix.py` | `6f83352efc537d354dd3827c7f157eab80476c44bc2603bfebc00d48ebb697bd` |
| `final-reconciliation/preparation-receipt.json` | `012afd0c91d02b91af07c9c5a14ad6cfae5a72d5deeac66aa35c6ae0d42c0b23` |
| `final-reconciliation/preflight-first.json` | `8e112eaf2bdeadbe93e37679d47755461d429f83973c83785a030d8b309ffbd6` |
| `final-reconciliation/matrix-first.json` | `fae5e5687c4639a8ff41506ff7ed1989d9aef094a20bc1a6e483f5f48255a07f` |
| `final-reconciliation/output-drift-evidence.json` | `c047e602479f05cce1f8f81d408f0fc0430cdde928c78dac20b3a6e0fbb6b6fe` |

Independently checked every preparation-receipt file identity and all 71 scope source/record bindings.
The exact six manifests, original revision source/receipt/load chains, successful build/capture/live
receipts, final-probe failure records/snapshots and actual Mac caller provenance remain pinned.
The approval applies to these exact bytes, not an automatically enlarged scope or later changed inputs.
The coordinator report's scope identity must match the reviewed scope hash above.

## Independently reproduced matrix

Called `evaluate(scope)` directly without running its report-writing main. The complete returned
context results exactly equal `preflight-first.json`. Separately reproduced `matrix.py`'s exhaustive
report, adapting only report persistence into an in-memory return; the entire report, including its
8,251 unique file-identity count, exactly equals `matrix-first.json`.

All verification ran read-only under an audit hook forbidding write opens, mutation and subprocesses.
Original versioned retained and strict-live verifier paths were actually invoked; no SDK/capture
entry point or filesystem Retainer was used.

| Context / revision | Retained record occurrences | Retained predicates | Current drift occurrences | Strict-live byte result after explicit tool-population reconciliation | Qualified scope |
| --- | ---: | --- | ---: | --- | --- |
| Probe / v3 | 887 | pass | 7 | **fail** | pass |
| Examples / v3 | 1559 | pass | 17 | **fail** | pass |
| Annotations / v3 | 1416 | pass | 12 | **fail** | pass |
| Consumers / v4 | 14234 | pass | 108 | **fail** | pass |
| Fixture / v5 | 2846 | pass | 0 | pass | pass |
| Mac comparison / v5 | 1023 | pass | 0 | pass | pass |
| **Total** | **21965** | **6/6** | **144** | **2/6 pass** | **6/6** |

These are retained **record occurrences**, not 21,965 distinct artifacts. No retained artifact was
missing or changed. No captured tool changed or vanished. No current source, generated source,
configuration, analyzer, restored-input or reference-assembly drift was found. All original successful
build/capture/live receipts in the scope are passed/exit 0, complete-retained and quiescent. The original
`consumer-final-probe` failure is separately retained, not converted to success.

## Finite tool-population projection is not a source/tool bypass

The actual same-version tool inventories reproduce exactly:

- Probe/Examples/Annotations v3: captured 16, current 18; additions are only pinned `proof-v4.py` and
  `proof-v5.py`.
- Consumers v4: captured 21, current 22; addition is only pinned `proof-v5.py`.
- Fixture/Mac v5: captured/current 28, no additions.

The adapter checks unique tool paths, exact equality between actual population and captured-plus-
approved population, and every original/additive row and actual file identity. Only then does its
in-memory inventory function return the unchanged historical captured array to the original verifier.
That projection is deliberately explicit: later helpers are not said to have run in earlier captures.
An absent/changed original, altered approved helper or additional unlisted root tool fails the scope.
The nested final helper/review do not enlarge the old root Python glob.

Each original revision's receipt and constructor are used, including the accepted namespace,
AdditionalFiles and asset-collector adaptations appropriate to that historical capture. The adapter
does not reconstruct a new successful compilation or retroactively apply v5 collector semantics to an
older capture. Original seals/current prepared-input checks, retained bytes and graph-identity checks
still run. Earlier exact Csc membership/policy/ownership evidence remains the immutable capture, not
an invented fresh compiler-project execution by this final check.

## Exact shared output replacements, not a path whitelist

There are **40 unique approved old-record keys**, covering the **144 occurrences** at exactly these
**seven logical paths** in the active checkout:

```text
src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.pdb
src/Core/src/obj/Release/net10.0/Yubico.YubiKit.Core.dll
src/Management/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
src/Management/src/bin/Release/net10.0/Yubico.YubiKit.Core.pdb
src/Piv/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
src/Piv/src/bin/Release/net10.0/Yubico.YubiKit.Core.pdb
```

For every permitted occurrence, the key includes original path, kind, size and hash. Acceptance also
requires binary classification, the exact reviewed current size/hash, location inside that captured
project's recorded build tree, and equality with the explicitly named matching successful captured
state set. Matching states are found by **same full original path and exact bytes**, not filename,
directory membership alone or any arbitrary known hash. All original retained bytes remain mandatory.

Independently checked every permit against its original record, current bytes and named capture
origins. None exempts a source, generated/config/analyzer input or reference assembly; no `ref`/`refint`
path is included. The full current-record scan still rejects drift outside the finite keys. Existing
current-state matches establish content provenance, not which filesystem writer performed a change.

Ran two bounded negative checks using process-local identity shadows only:

1. Returning an unknown current hash for the shared Core DLL caused all six affected contexts to fail
   the qualified result with unapproved drift, without changing the scope.
2. Making one original retained binary unavailable to the original verifier caused retained verification
   and that context's qualified result to fail, even though its current output-replacement scope remained
   approved. Other contexts still passed. No missing artifact was regenerated, skipped or accepted from
   a current substitute.

No disk input or record was modified for either check, and no new test framework was introduced.

## Original compiler inputs and permissible conclusions

Recomputed the five captured Core comparisons from the original **retained** Csc texts, generator
records and Core reference-output identities, rather than mutable latest raw/bin files:

- All Core arguments other than `/generatedfilesout:<attempt-specific directory>` are identical,
  preserving argument order.
- The single emitted generated-source content map matches across all five Core compiler states.
- Core reference-assembly SHA-256 is consistently
  `a0a6f20a6c6eadd8ca1085929d22bd47366691c0029f7e0f88b7f4fe64b0b624`.
- Implementation DLL/PDB bytes are nevertheless different and remain recorded as different.

These facts support unchanged observed source/compiler/reference-API inputs and the retained actual
documentation compile bindings. They do **not** prove binary or runtime equivalence, complete semantic
API equivalence beyond the recorded reference identity, or a particular unobserved cause of every
implementation/PDB byte difference. The report appropriately preserves that boundary. No compiler
reference-assembly drift or missing hardware-test input was discovered or excused by the scope.

The actual Mac proof is `mac-comparison-restore-isolated-first-v5`, not an earlier failed routing
attempt. Its build receipt retains the exact three-property admission descriptor and caller delta
receipt. The descriptor identifies the unchanged v2 creator; the actual v3 launcher adds safe-path
`-P` only to routing create/check and retains its delta. Scope pins the actual launcher, original
launcher, creator, preparation/delta receipts and descriptor. They are source/receipt provenance,
not an authentication or native-runtime qualification, and were not altered in this review.

## Relationship to the documentation compile plan

The sealed original preparation at `consumer-validation-preparation.md:209-220` requested final
strict current-byte checks and expressly disallowed weakening them. **That original unqualified
six-context final-live condition is not satisfied**; this review must not be used to say otherwise.
The original current-byte checks remain failed for the four contexts shown above.

This proposal is instead an explicitly reviewed **acceptance-scope amendment**: original exact
artifacts from every successful actual compile remain available and checked, current bound sources/
inputs remain exact, and later successful captured binary states explain only a finite set of shared
mutable output locations. It meets the documentation compile-evidence objective under that disclosed
scope without rebuilding merely to force simultaneous equality at overwritten paths. The PIV
historical verifier likewise distinguishes retained/current-body evidence from optional live binary
rechecking; that distinction is useful precedent, not authority to relabel this integration's original
strict-live results.

The retained actual successful compilations cover all 30 prepared bodies and required actual consumer
contexts; this review did not execute those bodies or add a device/test-runtime claim. Earlier individual
live passes remain valid **at their recorded time**. No SDK rebuild/recapture, synthetic replacement of
older artifacts or claim of current all-path identity is needed or authorized by this reconciliation.

## Coordinator command and correct completion wording

The fresh logger label/output were unused at review time. After coordinator reservation, use the
exact reviewed scope/tool bytes:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
python3 -B "$V/run-command-v2.py" --label consumer-final-reconciled-first -- \
  python3 -B "$P/final-reconciliation/reconcile.py" \
    --scope "$P/final-reconciliation/scope.json" \
    --output "$P/final-reconciliation/coordinator-final-first.json"
```

This command was reviewed, not run through the logger or used to create its report here. Do not
automatically update the scope if a source/tool/record/current binary changes; preserve and review it.

Admissible completion claim **after** the coordinator command succeeds with the reviewed scope hash:

> Current-source/exact-retained-output verification passed for all six documentation compile contexts.
> All 21,965 historical retained record occurrences verify exactly. Strict current-path verification
> remains failed for Probe, Examples, Annotations and Consumers because of 144 explicitly reviewed
> shared-output occurrences at seven paths; Fixture and Mac comparison pass strict-live verification.
> This does not establish equivalence of differing implementation binaries or native/device behavior.

Do not claim six strict-live passes, a new SDK/test run, runtime/API semantic equivalence, publication
of the integration, hardware/native acceptance, or completion of the still-pending whole-Core audit.
Late Windows concurrency work remains separate and was not audited or modified.
