# W1 Windows native fixture verifier

This non-packable host exercises the production `WindowsHidReportApi` and
`WindowsFidoHidConnection` / `WindowsFidoHidNativeOwner` against an owned Windows
**byte-mode named pipe**. The server is `NamedPipeServerStream`; the client goes
through the production `Kernel.CreateHidFile`, SafeHandle reference acquisition,
`ThreadPoolBoundHandle.BindHandle`, native ReadFile/WriteFile, exact-pointer
CancelIoEx, FreeNativeOverlapped and checked close paths.

The internal optional metadata constructor supplies FIDO64 descriptor values
(usage page `0xf1d0`, usage `1`, native input/output sizes `65`). Only descriptor
lookup is bypassed. Ordinary construction still invokes HidD/HidP; the
`metadata-default` case requires ordinary construction on the pipe to fail and
release the acquired handle. No core pipe imports or alternative native engine
are introduced. The observation decorator forwards actual native callbacks using
a verification-only operation carrier with the same owned report array.

## Invocation

Portable compilation, through the registered repository target:

```text
dotnet toolchain.cs windows-hid-verification-build
dotnet toolchain.cs windows-hid-verification-evidence-test
```

On the parent-owned Windows runner, invoke **one target** through the existing
windows-sdk toolchain lane:

```text
dotnet toolchain.cs windows-hid-verification-nohardware
```

The single target restores and publishes isolated `win-x64` self-contained JIT
and Native AOT trees, then executes both hosts. Do not combine target names with
`+`. The separate `windows-hid-verification-publish` target publishes without
executing. Windows publishing requires the runner's existing .NET 10 and native
AOT compiler/linker prerequisites; missing prerequisites fail the target.
Nothing installs or changes external tooling. Build/test/default do not depend
on these opt-in targets. `TargetRegistry.Define` automatically exposes them in
the existing help registry without modifying registry behavior.

Published host invocation is also explicit:

```text
artifacts/windows-hid-verification/jit/WindowsHidRouteVerification.exe --native jit artifacts/windows-hid-verification/evidence/jit
artifacts/windows-hid-verification/aot/WindowsHidRouteVerification.exe --native aot artifacts/windows-hid-verification/evidence/aot
```

Use the combined target for publish-to-run evidence binding. Standalone host
execution reports its current files but does not itself attest the source build.
Host invocation on Linux exits **2**, with no native checks. Runtime-kind mismatch
fails (JIT requires dynamic code supported; AOT requires it disabled).

## Finite native scenarios

Each scenario runs in its own child process, with a 45-second parent watchdog.
Expiry kills that process and fails the case: it is **not** drain/release evidence.
All scenarios are attempted even if another child fails. `RequiredNativeChecks`
independently specifies every scenario and its exact check names, separately from
the execution list. Missing, unexpected, duplicate, empty or false checks fail;
scenario identity, successful status and dynamic-code runtime identity must match.
The writer and process exit status reject incomplete children and summaries,
including a summary with only the runtime check and zero native cases.
The required successful run emits 44 scenario checks and 21 coordinator checks
per runtime (10 child processes, including the 100-iteration stress case).
Coordinator names are `requested-runtime-kind` and each required scenario's
`-exit` and `-required-checks` pair. Counts alone cannot satisfy this contract.

| Scenario | Required evidence |
|---|---|
| `pending-read-gc` | Read accepted with empty peer and no completion; force compacting GC while native storage is pending; real peer bytes complete with one 65-byte success callback; typed payload matches; retirement, clearing and checked close. |
| `output` | Production owner sends framed bytes to peer; one successful native callback; retirement and clearing; actual public task is terminal when close starts. |
| `early-callback` | Decorator holds Submit return after real submission; peer drives real callback; public read and retirement stay pending until Submit returns. |
| `cancel-retained` | Owner targets its actual pending request with CancelIoEx; native abort callback arrives but decorator holds delivery; public task, disposal, retirement and close remain pending; release callback and require caller cancellation before checked close. |
| `success-cancel-race` | Cancellation intent while Submit return is held; peer supplies real successful completion; success wins after return, without manufacturing an abort or invoking a fake cancel. |
| `cancel-not-found` | Direct production API, data ready before Submit; one successful callback; CancelIoEx on that completed but unretired request returns 1168; retire and close at the callback tail. |
| `reject-read`, `reject-write` | Disconnect owned server, retaining the production client's valid singly owned handle; require actual submission rejection, log Win32 error, free rejected runtime tracking, no callback, proven close. No double close or recycled-handle injection. |
| `reopen-stress` | 100 owner/connection lifetimes reopening the **same** owned pipe name; each pending read completes, retires, terminalizes and disposes binding/handle at callback tail. |
| `metadata-default` | Ordinary constructor still requests actual HID metadata; pipe descriptor lookup fails; partial-open handle cleanup is proven. |

Names in JSON are qualified by each scenario's file. Exactly-once assertions
observe the real completion count. No fake immediate completion is generated.
The short post-rejection observation interval is a delayed-callback regression
check, not mathematical proof that an arbitrarily late callback cannot occur.

## Evidence and limits

`artifacts/windows-hid-verification/evidence/publish.json` records source commit,
dirty status, SDK identity, source and resolved-assets SHA-256 hashes, resolved
package identities (only assets entries of type `package`) and both complete
published-file manifests. Before either restore, publishing deletes only this
verifier's `build/jit` and `build/aot` trees so newer intermediate timestamps cannot
reuse compilation from older source. Final output trees are also recreated.
Inputs must be stable across publish. Before each execution, the combined target
loads the sealed `publish.json` and compares its complete input/artifact maps with
current files, plus source commit and dirty status. After each execution it compares
the summary's `PublishedFileSha256` map against the sealed output map, normalizing
slashes and relative versus output-prefixed names. Membership must be complete and
unique; runtime identity and the exact coordinator contract must match too.
Source, output and manifest stability are checked again after execution.
Toolchain logs capture commands and compiler output.

This is local content binding, not a signed build attestation. The input map covers
Git-listed tracked/unignored files that exist and both restored assets closures;
it does not hash arbitrary ignored external imports, SDK installations or package
cache contents. Resolved package names are identity records, not proof of package
origin/authenticity. Published package/native bytes are covered by the complete
output maps. Snapshot comparisons do not detect a transient mutation restored
between checks; exclusive source/build workspace ownership remains required.

Each runtime has `summary.json`, ten scenario JSON files and separate child
stdout/stderr logs. JSON records runtime/OS/architecture, machine/executable,
Core assembly identity, dynamic-code capability, requested thread-pool switch
presence/value and environment override, all published-file hashes, checks,
native error observations and failures. An absent switch is **not** a claim
about the runtime's effective default implementation. No credentials or host
configuration are checked into source control.

This is **runtime-only synthetic-pipe evidence, not HID-driver/device evidence**.
HID metadata/access permissions, actual report-driver behavior, selected-device
GetInfo, removal and hardware close/reopen require separate future Windows
hardware acceptance. No workflow is added by this change.

Production `InterpretSubmission` normalizes immediate success and pending
acceptance. Therefore the empty-peer case proves pending native work and the
data-ready case is only an immediate-success **candidate**. The raw BOOL/997 split
is not exposed, and immediate-success coverage is explicitly **unattested**.
The controlled success race exercises success against caller cancellation
intent; the completed-request case separately exercises real ERROR_NOT_FOUND.
It does not promise both outcomes from an uncontrolled CancelIoEx race.

No allocator/preparation-failure injection is claimed to reproduce BCL failure
unwind. The frozen preparation-retention decision and existing managed tests
remain the evidence for that policy. Watchdogs never replace native barriers.

## Implementation validation status

The portable `windows-hid-verification-evidence-test` target compiles this host and
runs **21 child/coordinator gate cases and 19 publication gate cases** without
invoking native APIs. It uses real temporary source/output/manifest files under
`artifacts/w1-reviewfix/`, including timestamp-preserved source mutation, missing
and extra outputs, stale manifest identity, runtime digest/identity disagreement,
and newer stale intermediate fixtures. The reset test checks the exact cleanup
helper used before restore; it does not claim a Windows MSBuild/AOT execution.
Only the explicit `--evidence-self-test` mode precedes the Windows-only gate.

Review-fix evidence is retained under `artifacts/w1-reviewfix/`: the original child
predicate produced six failures in nine cases; a reverted publication negative
control (fresh hashes trusted against themselves and intermediate cleanup omitted)
produced five failures in eighteen publication cases. These are portable gate
red/green results, not native-runtime passes.

The host was authored before the metadata seam and its initial registered
compile failed on the missing constructor. That is compile-contract evidence,
**not a Windows runtime red/green result**. Native tests, including the meaningful
default-metadata regression, require the parent-owned Windows execution lane.
Linux compilation and existing focused Core tests cannot establish native
acceptance.
