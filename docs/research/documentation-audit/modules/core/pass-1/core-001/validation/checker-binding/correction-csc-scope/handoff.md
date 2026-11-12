# Empty Csc argument capture: bounded correction

**The first build remains failed. The corrected late-item hook is statically ready for review; a new
coordinator build is pending.** No checker selfcheck/token/API command has run and no successful build
seal is claimed.

## Root cause and evidence

The actual first attempt recorded `NETCoreSdkVersion=10.0.404`,
`ProvideCommandLineArgs=true`, `SkipCompilerExecution=false` and `UseSharedCompilation=false`.
Its `csc-arguments.txt` is **zero bytes**, rather than a response-file argument. Before/actual item
files agree: **four sources, 169 references, eight analyzers and three analyzer configurations**.
The retained build log reports the binder's empty-argument rejection, then `MSB3073` at hook `(42,5)`;
it reports zero warnings and no C# compiler diagnostic. Overall build status and exit remain failed.

The selected SDK's `Roslyn/Microsoft.CSharp.Core.targets` passes `$(ProvideCommandLineArgs)` to
`Csc` at line 152 and writes the task's `CommandLineArgs` output into `CscCommandLineArgs` at line 182.
The original hook subscribed through `TargetsTriggeredByCompilation`, reached by `CallTarget` at
line 189 while `CoreCompile` is still in progress. That legacy call observes the calling target's
initial lookup, which lacks the task output just created in the ongoing target. Thus changing the
item name, enabling an already enabled property or inventing arguments would address the wrong cause.

The proven integration `validation/consumer-proof/Capture.targets` reads exactly the same item through
`AfterTargets="CoreCompile"` (lines 20–32). The correction uses that mechanism after the target commits
its output lookup. It retains the strict empty/response-argument rejection, item/argument equality,
compiler-skip checks, warning-free clean-build requirement and output binding.

Source support, accessed November 12, 2026:

- Context7 `/dotnet/msbuild` was consulted for target/task scope and ordering. Its general scope
  overview is insufficient for this legacy-call boundary.
- [Microsoft CallTarget task remarks](https://learn.microsoft.com/en-us/visualstudio/msbuild/calltarget-task?view=visualstudio)
  explicitly describe a new scope for called targets.
- Pinned [TargetBuilder.cs at `7af2a3bcbdb7a1463386562a99947be990fb2316`](https://github.com/dotnet/msbuild/blob/7af2a3bcbdb7a1463386562a99947be990fb2316/src/Build/BackEnd/Components/RequestBuilder/TargetBuilder.cs#L261-L267)
  states “Our lookup is the one used at the beginning of the calling target” and uses `_baseLookup`.
  This is a pinned upstream implementation comparator, rather than a claimed source mapping to the
  installed MSBuild binary. Installed SDK target bytes and retained actual failure are the local evidence.

## Preservation and exact delta

`failure-preservation.json` hashes all **192** retained failed-attempt/command records and preserves
the original hook, binder, handoff, provenance and preparation text as snapshots. It also maps all
**19** current checker-job files (**170,663 bytes**) to byte-exact external copies beneath:

`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/checker-binding-failures/checker-bound-build-first/`

Observed unsealed compiler outputs remain explicitly failed-build artifacts:

| Output | SHA-256 |
| --- | --- |
| Intermediate checker DLL | `6849f11ece46e7cee76195a885133cc7513f35b3882097efbe87ef2d3504849f` |
| Intermediate PDB | `62bce4329970486ee2d829b04bfd684dfc99a430b71c1b645687166178286fe1` |
| Intermediate reference DLL | `457886223960698438340c3644ca7c700e71f1f196539baeb63c8b169593c264` |

Those copies preserve evidence before a fresh clean build can remove the original paths. Their presence
supplies no successful compiler binding or permission to execute them. Binaries remain external.

Only two profile participants change:

1. `hook.targets`: remove `TargetsTriggeredByCompilation`; add `AfterTargets="CoreCompile"` to the
   existing capture target and correct its explanatory comment.
2. `bind.py`: correct the emitted compile-evidence description to match the late-hook contract.

The checker C#, project/local props, accepted foundation/logger/qualifier, original R1 review, old
profile `65a53f…`, first attempt and frozen integration/PIV records retain their bytes. The new profile
also records the current metadata HEAD separately from source baseline `fe342fb0…`.

| Current participant/record | SHA-256 |
| --- | --- |
| `../hook.targets` | `b0349f6a024743671a775294374d62dbeba43157b296dfbd4e10fd45b3fe0db1` |
| `../bind.py` | `3fc6ef62901b40229f8e76fb6d8635e7c3ded4e370716ce8069e35d88ee1c90d` |
| `failure-preservation.json` | `f65d01580b92abba3b535a38c3acb7ce976274125cdd183a4fb4b29730496795` |
| `../reviewed-inputs-second.json` | `51b733852da256846483d805fe54be3f9a4b2bc77ff50a95ba3d3dedaaac02e2` |

## Static checks and next command

`verify.py` first rejected the original hook's scope. After the narrow change it verifies the selected
SDK's actual task parameter/output item, the late hook and matching proven integration reader. It also
rechecks every retained failed record and external preservation copy. It never invokes a build or
calls the failed attempt's capture function to manufacture success. The existing binder selfchecks
remain applicable; the old profile rejects the changed participant as intended.

After fresh review and explicit SDK release, the coordinator runs the same accepted v2 logger through
the unchanged build wrapper. Logger requalification is unnecessary because its participants are exact:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
B="$V/checker-binding"
P=51b733852da256846483d805fe54be3f9a4b2bc77ff50a95ba3d3dedaaac02e2
python3 -B "$B/bind.py" build --label checker-bound-build-second \
  --profile "$B/reviewed-inputs-second.json" --profile-sha256 "$P"
```

That wrapper executes canonical `dotnet toolchain.cs -- build --project Audit20261111.DocCheck --clean`.
Inspect the new actual Csc argument capture, complete zero-warning RESULT and output comparisons before
accepting the newly emitted `attempts/checker-bound-build-second/sealed.json` SHA. Only then use the
bound `selfcheck`, `tokens` and `api` commands in the parent handoff with this new profile and seal.
Any second failure retains its own attempt and receives another bounded diagnosis; this preparation
claims neither a successful retry nor a checker result.
