# R1: checker input/output binding

**Both bound builds remain failed; the third-attempt correction awaits independent review.**
No checker selfcheck, token or API mode ran. The [scope correction](correction-csc-scope/handoff.md)
preserves the first failure; the [file-input correction](correction-file-inputs/handoff.md) preserves the
second and verifies the entire compile predicate read-only before another build. Checker C# and logger
inputs stay unchanged.
This additive correction addresses [R1](../../validation-review.md) without changing the accepted
foundation/logger/qualifier sources, original review, original manifests or historical tool records.
The original unbound commands were never executed. Their preparation is retained byte-for-byte in
`pre-r1-validation-preparation.md.snapshot`.

## Minimal binding

| Record | Role |
| --- | --- |
| `reviewed-inputs-third.json` | Corrected prebuild profile submitted for independent review. Pins checker `Program.cs`, project/local props, root policy/configuration, actual canonical toolchain includes, binder/hook, selected SDK/compiler/task binaries including Source Link producers, dotnet host and runtime. Metadata HEAD and fixed source baseline are separate. |
| `reviewed-inputs-third-inputs/*.snapshot` | Exact textual reviewed source/config snapshots. Binary files remain external and contribute path/size/SHA-256 identities only. |
| `attempts/<label>/before.json` | Immediately before Csc: exact sources/references/analyzers/configs and other compiler-file inputs, actual preprocessed import closure, restore assets/package identities, effective project policy/version and named non-secret hook/temp properties. |
| `attempts/<label>/compile.json` | Actual Csc arguments and matching item membership, verified unchanged input hashes, generated-source snapshots and original implementation DLL/PDB/reference output identities. The corrected hook reads committed task outputs with `AfterTargets="CoreCompile"`, matching the proven integration mechanism. Empty arguments, compiler-skip policy or an incomplete clean build prevent a seal. |
| `attempts/<label>/finish.json` | After Build: exact runtime output directory, final DLL/PDB equality to actual Csc outputs, runtimeconfig/deps metadata and copied Roslyn DLL equality to their selected SDK owner. |
| `commands-v2/<label>/receipt.json` | Original accepted v2 receipt: canonical clean build command, completed zero-warning RESULT, actual exit, full retention/streams and quiescence. Its existing fields remain unchanged. |
| `attempts/<label>/sealed.json` | Admits those captures against that successful build receipt and exact reviewed profile. Identifies the sole checker DLL and every original output; emits its own SHA-256 for coordinator acceptance. |
| `invocation-<label>-started.json` and `invocation-<label>.json` | Exact bound command/path/runtime and pre/post input/output checks. The final sidecar supplies checker admission only when its status is `passed` and the original command receipt also passed. |

The profile SHA-256 is:

`ceea8be4c6681c15c51f4611de9546c35f1428f3a6d44de893a434fdab7cd2f1`

The static profile selects installed SDK **10.0.404** and runtime **10.0.15** by explicit directory.
The assigned build must independently report that SDK in evaluated properties. The compiler source,
task binaries and Roslyn references are hashed before build and checked through completion; actual
framework references, canonical analyzer package/DLLs, generated inputs and imports are captured at
compilation. `Version`, `AssemblyVersion`, SDK version, restore-library identity and runtime version are
separate observations. NuGet asset hash declarations and cache/archive identities retain separate
fields; the binder assumes no equality across unrelated hash domains.

`hook.targets` is selected through the named `DirectoryBuildTargetsPath` environment property and
explicitly imports the actual root targets at this directory's nine-level depth. Its extra targets
and compiler controls apply only to `Audit20261111.DocCheck`. Root analyzers and warnings-as-errors
remain enabled. Restore and build inherit the same hook/profile/temp properties. The before hook's
`dotnet msbuild -preprocess` performs import evaluation only; its exact command, exit and streams are
retained. It starts no second build. Missing hook markers, actual Csc arguments, source/input changes,
unexpected membership, skipped analyzer policy or incomplete builds prevent a seal.

The invocation wrapper calls the same accepted v2 adapter directly, so the actual dotnet child retains
its process ownership, full streams, durable fallback receipts and quiescence proof. It derives the DLL
from the accepted seal instead of accepting an arbitrary `$C`. Before and after invocation it verifies
the reviewed source/configuration, actual compiler inputs, selected SDK/runtime, original build receipt,
capture records, generated sources and sealed outputs. The exact runtime is selected with
`dotnet exec --roll-forward Disable --fx-version 10.0.15`. Runtime injection variables and inherited
compiler-policy overrides require explicit reconciliation. New sidecars record only the named
non-secret hook/temp settings; credentials and user NuGet configuration are outside the snapshots.
Its entry point maps SIGTERM to `KeyboardInterrupt`, matching the accepted logger entry point so the
unchanged supervisor retains its cleanup path. Static verification calls that exception adapter without
sending a signal; actual process qualification remains the coordinator's already retained lane.

## Coordinator sequence

The coordinator reports the existing disposable logger qualification passed. Preserve its 38 retained
text records and original participant hashes. After fresh R1 review, explicit SDK release and loading
the current build skill, run from the primary checkout root:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
B="$V/checker-binding"
P=ceea8be4c6681c15c51f4611de9546c35f1428f3a6d44de893a434fdab7cd2f1
python3 -B "$V/run-command.py" --label sdk-info-first -- dotnet --info
python3 -B "$B/bind.py" build --label checker-bound-build-third \
  --profile "$B/reviewed-inputs-third.json" --profile-sha256 "$P"
```

The build wrapper uses the unchanged v2 logger to execute exactly:

```bash
dotnet toolchain.cs -- build --project Audit20261111.DocCheck --clean
```

Read the actual captured RESULT and hook/capture records. The wrapper prints the path and SHA-256 of
`attempts/checker-bound-build-third/sealed.json` only after all admission checks pass. Accept that actual
seal; a failed/partial build supplies no substitute. Keep the seal and original outputs before another
build can replace them. Use the actual emitted SHA as `S`:

```bash
S=<accepted-actual-seal-sha256>
SEAL="$B/attempts/checker-bound-build-third/sealed.json"
python3 -B "$B/bind.py" run --label checker-selfcheck-first \
  --profile-sha256 "$P" --seal "$SEAL" --seal-sha256 "$S" selfcheck
python3 -B "$B/bind.py" run --label source-tokens-first \
  --profile-sha256 "$P" --seal "$SEAL" --seal-sha256 "$S" tokens
python3 -B "$B/bind.py" run --label api-first \
  --profile-sha256 "$P" --seal "$SEAL" --seal-sha256 "$S" api
```

Accept both the original v2 command receipt and its new passed invocation sidecar. A zero-exit raw
command whose binding sidecar fails remains unaccepted checker evidence. Preserve binder command
stdout/stderr on pre-admission failures as well. Choose fresh labels after corrections and retain the
original attempts. Use these same bound run commands with fresh labels for later final tokens/API
checks. Core documentation edits and metadata-only commits can advance while checker participants stay
exact; actual participant changes require a fresh profile/build review. Missing/replaced original output
bytes fail explicitly. Rebuilding for bookkeeping supplies no earlier live identity.

## Static verification and limits

`selfcheck.py` exercises independent metadata fixtures and a standard SHA-256 vector. It accepts a
complete fixture and rejects changed/missing source, absent actual Csc completion, replaced/missing or
wrong-path DLL, failed/incomplete/warning builds, live-child receipts, stale/missing external bytes,
Csc membership changes, hidden response inputs, skipped analyzers and weakened warnings. XML inspection
checks the actual root-target import. It runs no SDK process or compiled checker.

The first pre-implementation call failed because `bind.py` was absent. One initial fixture shared its
DLL dictionary between the old and changed states; independent copies corrected that test-oracle
aliasing. No product behavior or accepted logger code changed. Current static results are recorded in
`static-verification-final.json`. Compiler-hook participation, actual analyzer execution, produced binaries
and checker results still await their assigned build/run. Current whole-Core semantic review, final
examples, repository/native/platform/device gates, PIV physical deferral and Windows late handoff
remain with their existing owners.

The initial unaccepted `reviewed-inputs.json` and `reviewed-inputs-r1.json` profiles, their snapshots and
the first static report remain retained. Preparation added canonical dotnet-host resolution, relative
CLI profile/seal path handling and the matching SIGTERM entry adapter before any build. The final
source hash intentionally invalidates both prior profiles; static verification checks those rejections.
No earlier receipt or compiled output was rewritten.

The subsequent `reviewed-inputs-final.json` profile was used for the real failed build. It remains exact
at SHA-256 `65a53f340bd1496b259c98e4e15e20254cab61b394fb39e8f8f128b94a503e80`.
Only the hook timing and one binder evidence description change for the new second-attempt profile;
the failed attempt is never replayed into a successful capture.
