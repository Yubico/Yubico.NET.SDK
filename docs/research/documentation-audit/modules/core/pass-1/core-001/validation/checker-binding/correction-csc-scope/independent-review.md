# Independent Sol review: late Csc output capture

## Decision

**PASS — accept the bounded correction and exact second profile for the coordinator's new build.**
No additional code correction is required by this static review. Actual late-hook operation remains
unproved until that build. The first attempt remains failed and unsealed; neither its intermediate
outputs nor this review establish a successful checker build or checker execution.

Source baseline: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Observed metadata HEAD: `01a114d4e251e0f52b92f73393fbe1dca5d5c24e`.
Review scope is the Csc capture correction, not whole-Core investigation or a new framework review.

## Failure diagnosis and corrected placement

The retained first attempt has an empty `csc-arguments.txt`, matching before/actual source, reference,
analyzer and config item populations, and evaluated `ProvideCommandLineArgs=true` and
`SkipCompilerExecution=false`. Its original logger receipt reports exit 1 / failed, with complete
streams/retention and quiescence. It has no `compile.json`, `finish.json` or `sealed.json`.

The installed, profile-pinned SDK target passes `ProvideCommandLineArgs` into Csc and publishes its
`CommandLineArgs` output as `CscCommandLineArgs`. Neither the property nor output item name needed
correction. The old hook's `CallTarget` subscription read the calling target's initial lookup while
`CoreCompile` was still executing. Context7's current `/dotnet/msbuild` reference to
`ITargetBuilderCallback.LegacyCallTarget` documents precisely this isolation: called targets do not
see changes made after the calling target began. This supports the diagnosis alongside the actual
retained failure; it is not a claimed source-to-installed-binary mapping.

The new hook removes the `TargetsTriggeredByCompilation` subscription and attaches the existing
capture target with `AfterTargets="CoreCompile"`. The frozen integration
`validation/consumer-proof/Capture.targets:20–32` uses that same late target and output item. This is
the appropriate narrow correction for reading committed task outputs. The prior review's static
acceptance of the in-target hook was insufficient for this scope boundary; the retained actual failure
and this correction supersede that placement assessment without rewriting the earlier review.

**A late target firing is not, by itself, proof that Csc ran.** Admission still requires the canonical
clean toolchain command (whose build target supplies `--no-incremental`), a fresh capture attempt,
`SkipCompilerExecution=false`, nonempty actual Csc arguments, exact compiler item/argument membership,
strict analyzer/warning policy, actual outputs and a successful complete original build receipt.
The binder still rejects missing/empty arguments rather than inventing a command line or accepting
an AfterTargets marker. No manual capture replay or retroactive seal is part of the retry.

## Exact delta and independent read-only checks

Byte reads, SHA-256 checks, JSON/XML inspection, Python AST parsing and a textual source diff verified:

- `reviewed-inputs-second.json` has the submitted SHA-256 below and **242 input identities / 30 text
  snapshots**; every current original input and snapshot matches its recorded length/hash.
- Original input membership is identical to `reviewed-inputs-final.json`. Exactly **two** participant
  identities change: `hook.targets` and `bind.py`. Apart from `inputs` and `text_snapshots`, the profile's
  top-level fields are unchanged. Snapshot output paths change to the new profile's snapshot directory.
- The hook delta is limited to removing the old subscription, adding `AfterTargets="CoreCompile"` and
  correcting the comment. The binder delta changes **only** the compile-evidence description string.
  No executable Python admission behavior changed. The 22 existing negative guards remain intact;
  the separate empty-argument rejection at `bind.py:156` is also unchanged. Their reported results are
  retained static evidence, not tests rerun by this reviewer.
- Root import depth, selected SDK task parameter/output wiring and the integration late-reader mechanism
  match the correction. `ProvideCommandLineArgs`, compiler execution and shared-compilation controls
  are unchanged. The source/profile/output binding and paired original-receipt/sidecar requirements
  remain in force.
- All **192 retained failed-attempt/command records** match their preservation rows. All **19 original
  checker-job files and external preservation copies**, totaling **170,663 bytes**, match recorded
  identities. This includes the unsealed implementation DLL, PDB and reference DLL. Their external
  copies preserve failure evidence before cleaning, not executable admission.
- The original profile remains byte-identical at SHA-256 `65a53f340bd1496b259c98e4e15e20254cab61b394fb39e8f8f128b94a503e80`.
  Its two obsolete participant hashes cannot admit the changed current participants. The preserved
  hook/binder/handoff/provenance/preparation snapshots match the correction report's raw identities.
- Every `checked_records` raw hash in the supplied correction static report matches its current file.
  Accepted foundation/logger/qualifier/checker C# identities and both original review documents match
  their prior accepted hashes. No frozen integration/PIV bytes were written or historical receipt
  fields revised. This bounded review did not repeat the campaign-wide preservation scan.

No SDK/build/checker, capture operation, process qualification, test runner, native/device operation,
formatter, product/source edit or index/ref operation was performed. No writing verification/preparation
entry point was replayed. Only this review file is written.

## Accepted identities

Paths are relative to the parent `checker-binding/` directory.

| File | SHA-256 |
| --- | --- |
| `reviewed-inputs-second.json` | `51b733852da256846483d805fe54be3f9a4b2bc77ff50a95ba3d3dedaaac02e2` |
| `hook.targets` | `b0349f6a024743671a775294374d62dbeba43157b296dfbd4e10fd45b3fe0db1` |
| `bind.py` | `3fc6ef62901b40229f8e76fb6d8635e7c3ded4e370716ce8069e35d88ee1c90d` |
| `correction-csc-scope/failure-preservation.json` | `f65d01580b92abba3b535a38c3acb7ce976274125cdd183a4fb4b29730496795` |
| `correction-csc-scope/static-verification.json` | `dac74d77bb7d2f6c6c24b6d73cfa3ae6e3bacc88991428a7f0b40879de87f38a` |

## Exact next coordinator command

After exclusive execution release and loading the current build skill, run from the primary root:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
B="$V/checker-binding"
P=51b733852da256846483d805fe54be3f9a4b2bc77ff50a95ba3d3dedaaac02e2
python3 -B "$B/bind.py" build --label checker-bound-build-second \
  --profile "$B/reviewed-inputs-second.json" --profile-sha256 "$P"
```

Do not repeat logger qualification or `sdk-info-first`, and do not reuse the failed first build label.
Inspect the second build's actual nonempty Csc arguments, input membership, compiler execution,
zero-warning RESULT, output comparisons, original v2 receipt and new captures. Accept only the actual
newly emitted `attempts/checker-bound-build-second/sealed.json` SHA-256 after those checks pass.
Only then may separately released bound selfcheck/token/API invocations use this new profile and seal;
each still requires both its successful original receipt and passed binding sidecar. A failed retry
remains its own preserved failed attempt. No successful retry is asserted here.

The current source pin, active family investigations, final coverage/example work, PIV physical
deferral and later user Windows handoff remain unchanged.
