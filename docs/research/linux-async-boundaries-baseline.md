# Linux async-boundary baseline and held-report probes

Recorded 2026-11-11 in the isolated `yubikit-linux-async` worktree, branch
`yubikit-linux-async`, recorded HEAD `3de42bb54c5e8c986364f0f04398d4e85a66a596`.
The checkout was clean before L0. L0 does not edit production source, public
APIs, package pins, boundary manifests/registries, or master acceptance documents.
Concurrent orchestrator edits to the master ISA and `00-status.md` appeared
during this run and were left untouched.
This is managed baseline evidence, not Linux native/hardware acceptance or a
performance budget. No production migration is implemented here.

## Runtime and dependency provenance

Read-only queries: `git rev-parse HEAD`, `dotnet --info`, `uname -a`, `lscpu`,
`ldd --version`, `pacman -Q linux-omarchy glibc systemd pcsclite openssl`,
the Core `obj/project.assets.json`, cached-package SHA-256, and `ldd` on the
cached Linux shim.

| Item | Observed value |
|---|---|
| OS / RID | Omarchy 4.5.0 / `linux-x64` |
| Kernel | `7.2.15-1-omarchy`, x86_64, SMP PREEMPT_DYNAMIC |
| CPU | Intel Core i7-12700H; 20 online logical CPUs |
| SDK / MSBuild | .NET `10.0.404` (`2ab2854867`) / `18.9.15+2ab285486` |
| Executing runtime | .NET `10.0.15`, x64 (`8e0f8ed231`); confirmed by test runner |
| SDK package version | `2.0.0-alpha.2` in `Directory.Packages.props` |
| NativeShims | Pin **and resolved assets**: `Yubico.NativeShims/1.18.1`; September `.8` preview references are historical |
| Test framework | `xunit.v3` 3.0.0; Microsoft.Testing.Platform runner `v3.0.0+d0213fc4e5` |
| Native packages | `linux-omarchy 7.2.15-1`; `glibc 2.44+r50+g1848099f063e-1`; `systemd 262.1-1`; `pcsclite 2.5.3-1`; `openssl 3.6.5-1` |
| Shim dynamic dependencies | Resolved `libpcsclite.so.1`, `libpthread.so.0`, `libc.so.6`, `libdl.so.2`, and `/usr/lib64/ld-linux-x86-64.so.2`; none missing |

Cached `~/.nuget/packages/yubico.nativeshims/1.18.1/` hashes:

```text
yubico.nativeshims.1.18.1.nupkg
243958f7e87d96e22b33b1d9b469287ea9d6dd3461cd904a292d3342d27d5155
runtimes/linux-x64/native/libYubico.NativeShims.so
e59ad90d6bcca4bd1849d964f7cef276f40d2388b0abde19b3ed8400c4835a3e
```

These identify local cached bytes, not a fresh-feed restore, producer-commit
attestation, Native AOT execution, or hardware compatibility result.

## Hardware remains blocked

The task supplied an earlier observation that USB `3-1.3` interfaces were bound
to `usbfs`, with no Yubico hidraw nodes. L0's read-only sysfs snapshot instead
found:

```text
3-1.3:1.0 -> /sys/bus/usb/drivers/usbhid
3-1.3:1.1 -> /sys/bus/usb/drivers/usbhid
3-1.3:1.2 -> no driver symlink observed
hidraw0 -> .../3-1.3:1.0/0003:1050:0407.0034
hidraw5 -> .../3-1.3:1.1/0003:1050:0407.0035
```

This discrepancy does **not** authorize hardware work or establish usable
permissions/exclusive access. No device node was opened, no SDK discovery or
device operation was run, no interfaces were unbound, no processes were killed,
and no system configuration was changed. Firmware, selected-key lifecycle,
read/write/ioctl duration, cancellation/removal, idle behavior and hardware
reopen measurements are unavailable in L0.

## What the baseline actually demonstrates

Unchanged production source has the following call chain:

* `src/Core/src/Protocols/Fido/Hid/FidoHidConnection.cs` calls `GetReport` or
  `SetReport` before returning `Task.FromResult` / `Task.CompletedTask`.
* `src/Core/src/Protocols/Otp/Hid/OtpHidConnection.cs` has the equivalent
  synchronous feature-report adapter.
* `src/Core/src/Transports/Hid/Linux/LinuxHidIOReportConnection.cs` performs
  `open(O_RDWR)`, descriptor ioctls, `read`, and `write` synchronously.
* `src/Core/src/Transports/Hid/Linux/LinuxHidFeatureReportConnection.cs`
  performs synchronous open and feature GET/SET ioctls.
* `src/Core/src/Devices/HidConnectionSlot.cs` evaluates the non-macOS report
  open and adapter construction inside `Task.FromResult<IConnection>(...)`.

The tests invoke the **actual FIDO/OTP production adapters** over controlled
`IHidConnection` implementations. Both GET and SET are withheld. In all four
legacy cases, the report call runs on the invoking thread and the invocation
has not returned its task when the fake barrier is reached. The responsiveness
assertion rejects them deterministically on thread identity, without waiting
for a latency threshold. These are compatibility-adapter observations, not
execution of the Linux report implementations or public factory/discovery
end-to-end proof. Linux open has source evidence only at L0; its controlled
native seam is later work.

The off-caller control dispatches these same adapters with test-only `Task.Run`.
All four controls return an **incomplete** operation task while the report call
is withheld. This is a probe control, not a proposal for a shipping scheduler,
connection owner, lifetime policy, or worker-count bound.

`CompletedTaskWhileReportIsHeld_IsRejectedByTheResponsivenessGate` deliberately
dispatches the real OTP adapter but discards its task and returns
`Task.CompletedTask`. The old probe incorrectly accepted this fixture. The new
pending-operation assertion rejects it, distinguishing invocation return from
terminal operation completion. The detached control task is separately drained.

## Reusable helper contract

`src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/WithheldNativeProbe.cs`
is extracted from the existing suite's private probe, not a second framework.
Its contract is **one invocation and one held native-seam call per instance**:

* `Start(Func<Task>)` invokes on a dedicated background thread with a non-null
  `SynchronizationContext`. It does not unwrap the operation. `CallerThread`
  identifies this caller; `Invocation.Task` is a `Task<Task>` whose outer
  completion publishes the operation task. `Returned` separately marks exit
  from invocation/capture, including synchronous failure.
* A controlled seam calls `EnterNative()` at the actual held portion of the
  operation. It records thread/timestamps, signals entry, and waits for
  `Release` or runner cancellation. It is not a real native instrumentation hook.
* `AssertResponsive()` runs **before release**: wait for seam entry, reject
  caller-thread execution, wait for invocation return, require the barrier to
  remain held, and require the returned operation task to be incomplete.
  Five-second waits prevent hangs; they are not latency-success assertions.
* In `finally`, `ReleaseAndDrainAsync()` releases the barrier, awaits the outer
  invocation, then awaits the operation. Its finite cleanup waits deliberately
  use `CancellationToken.None` so runner cancellation does not abandon work
  still accessing the fake's gates. Dispose the connection after draining.
  For `Task<TConnection>` opens, the test must additionally await the typed task
  and dispose the returned connection inside cleanup, even after assertion failure.
* `Dispose()` releases and joins the caller; it does not replace operation
  drain or dispose returned connections. New tests must retain the `finally`
  drain pattern. No pumping UI context, starvation resistance, native quiescence,
  overlap policy or resource-release guarantee follows from this helper alone.
* `DescribeDurations()` is diagnostic-only after successful drain. It reports
  invocation-start to task-return and managed-fake entry to release-return,
  separately; neither value is a native-call duration or performance gate.

Migration reuse:

```csharp
using var probe = new WithheldNativeProbe();
// Construct the real migrated adapter over its controlled native seam.
probe.Start(() => connection.ReceiveAsync(TestContext.Current.CancellationToken));
try { probe.AssertResponsive(); } // while the seam's EnterNative is held
finally { await probe.ReleaseAndDrainAsync(); }
```

Do **not** copy `Assert.Throws<NotEqualException>(probe.AssertResponsive)` or
`Assert.False(probe.Returned.IsSet)` into migrated-route acceptance tests. Those
assertions pin only the explicitly named retained synchronous compatibility
adapters. If these adapters themselves migrate, replace/remove their legacy
expectations and require responsiveness instead. Keep the deliberate blocking
negative fixture to prove the gate still detects synchronous execution, and
the discarded-completion negative fixture to reject premature completion.

The existing `FindHidInterfacesBoundaryTests` already separates outer invocation
from inner completion and drains in `finally`; it remains unchanged and is
included in the regression selection. The macOS held-open/receive controls now
use the extracted probe and also require pending operation completion; their
cleanup drains/disposes even after an assertion failure.

## Diagnostic no-hardware timings

One observation per scenario, no warmup/statistical claim, measured with
`Stopwatch` on the untouched production adapters. Values are milliseconds.
The barrier is released by test coordination, not held for a fixed duration;
its elapsed time includes assertions/scheduling and is intentionally not a
device or native latency. Even a small legacy number remains a failure of the
ordering contract.

| Scenario | Caller return | Fake held call |
|---|---:|---:|
| Legacy OTP receive | 3.736 | 3.304 |
| Legacy OTP send | 0.244 | 0.088 |
| Legacy FIDO receive | 0.109 | 0.037 |
| Legacy FIDO send | 0.027 | 0.023 |
| Off-caller OTP receive control | 0.002 | 0.009 |
| Off-caller OTP send control | 0.007 | 0.065 |
| Off-caller FIDO receive control | 0.006 | 0.062 |
| Off-caller FIDO send control | 0.072 | 0.057 |

Captured in run `20261111-013337-267-test`, step
`01-test-Core.UnitTests.log`. To expose successful-test diagnostic output,
temporarily set `"showLiveOutput": true` in the existing Core unit
`xunit.runner.json`, run the focused wrapper command below, then restore that
setting. L0 restored it; no runner-config change remains. Default runs still
perform all ordering assertions but suppress successful-test output.

Native-only duration, pending native/ordinary worker counts, hardware operation
completion/recovery/disposal/reopen times, idle CPU/allocation, and matched
baseline/final hardware statistics are **unavailable/not measured**. The
existing `verification/MacOSHidBoundaryComparison/{Program,CurrentProfile}.cs`
was inspected: it already separates invocation/terminal phases and unavailable
native metrics, but its hardware routes are macOS-specific. L0 reuses the
existing managed suite rather than adding a Linux measurement application or
claiming a synthetic profile is a hardware baseline.

## Commands and exact result evidence

All .NET builds/tests used the repository wrapper, Release configuration.
No integration tests were executed. Run IDs below name directories under
`artifacts/toolchain/runs/`; full outputs are `run.log`, `result.json`, and
`01-test-Core.UnitTests.log` (build step names differ). That location keeps only
five runs. Available raw outputs were also copied to local-only
`/tmp/opencode/linux-async-l0-evidence/`; this document preserves the salient
results rather than depending on those ephemeral files.

```bash
# Before L0 edits: original 7-test responsiveness baseline
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~ResponsivenessProbeTests"
# 20261111-013115-662-test: exit 0, RESULT: PASSED
# Totals: projects: 1 passed, 0 failed, 0 with no tests executed; tests: 7 executed, 0 failed, 0 skipped
# Warnings: 0

# RED: new discarded-completion negative fixture against the old helper
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~CompletedTaskWhileReportIsHeld"
# 20261111-013153-397-test: exit 1, RESULT: FAILED
# Totals: projects: 0 passed, 1 failed, 0 with no tests executed; tests: 1 executed, 1 failed, 0 skipped
# Assert.Throws() Failure: No exception was thrown | Expected: typeof(Xunit.Sdk.FalseException)
# Warnings: 0

# GREEN after extracting/strengthening helper; includes diagnostic timing capture
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~ResponsivenessProbeTests"
# 20261111-013337-267-test: exit 0, RESULT: PASSED
# Totals: projects: 1 passed, 0 failed, 0 with no tests executed; tests: 14 executed, 0 failed, 0 skipped
# Warnings: 0

# Full-solution compilation at unchanged production baseline, with L0 tests
dotnet toolchain.cs build
# 20261111-013356-569-build: exit 0, RESULT: PASSED
# Built Yubico.YubiKit.sln in Release configuration; Warnings: 0

# Final boundary inventory/registry + existing withheld-enumeration regression
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~BoundaryInventory|FullyQualifiedName~FindHidInterfacesBoundaryTests"
# 20261111-013548-430-test: exit 0, RESULT: PASSED
# Totals: projects: 1 passed, 0 failed, 0 with no tests executed; tests: 65 executed, 0 failed, 0 skipped
# Warnings: 0
```

Additional checks/issues:

* Scoped Core build: `20261111-013153-389-build`, exit 0, 3 projects compiled,
  `Warnings: 0`. It inadvertently overlapped the RED test run, which emitted
  `! WARN Another toolchain run is active ...; parallel builds can collide`.
  Subsequent build/test runs were serialized and emitted no such warning.
* `dotnet format Yubico.YubiKit.sln --no-restore --include` with the two changed
  C# paths initially reported `FINALNEWLINE` errors. Scoped formatting corrected
  them. The final `--verify-no-changes` exits 0 but emits the existing workspace
  warning `Warnings were encountered while loading the workspace`. Diagnostic
  log `/tmp/opencode/linux-async-l0-format.log` identifies duplicate
  `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` workspace entries across
  SDK projects. These are not compiler/test warnings; no project/API files
  were changed to suppress them.
* `dotnet toolchain.cs complexity`: `20261111-013513-945-complexity`, exit 0,
  `Warnings: 0`, **No shipping C# methods in scope**. This does not claim that
  test-helper complexity was checked.
* `git diff --check` passes. `codemapper .` was attempted as required but is
  unavailable (`command not found`, exit 127); source was inspected directly.
* An initial package query for stock `linux` / `linux-lts` exited 1 because
  those packages are absent; querying the installed `linux-omarchy` succeeds.
* No unrelated baseline compilation failure was observed. No acceptance
  criteria/status, production source, or system configuration was edited.
