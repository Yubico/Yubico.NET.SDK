# Final formatter reconciliation — unapplied proposal

## Decision for independent review

Approve only `eof-only.patch`, subject to before-hash preflight by the coordinator.
It removes exactly one terminal LF byte (`0a`) from each of 239 accepted C# files.
**No primary product file was written. No final formatter/build/test success is claimed.**

Patch SHA-256: `ee6f3b9e8198dcdcaea69c3016ba7de9352dc4788fe1a6058bb40d616220d516`.

The root `.editorconfig:34–36` specifies LF and `insert_final_newline = false` for C#.
The actual retained formatter report requests “Fix final newline. Delete 1 characters.”
at each proposed path's exact last-line/end-character position. Every proposed path
belongs to the current accepted map and the actual invocation's include list; no
out-of-map row was encountered. There are no nested `src/**/.editorconfig` overrides.

## Evidence and bounds

- `eof-identities.json`: all 239 exact before/after hashes, byte lengths, report sites,
  accepted receipts, last lines, and the sole deleted byte.
- `source-snapshots.tar.gz`: 239 before and 239 after source entries (478 total).
  Corresponding isolated copies are under
  `/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/format-final-reconciliation/{before,after}/`.
- `preparation-receipt.json`: accepted-map, policy, actual invocation/log/report input
  hashes and the proposal hashes. The external actual report is
  `/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/format-final-source-check.json`.
- `verification-receipt.json`: fresh proposal verification: all 239 exact byte proofs,
  all 478 archive entries, all 363 accepted primary hashes, and a system `patch`
  **dry-run only** against isolated before copies (exit 0; 239 paths; `--fuzz=0`).
- `prepare-evidence.py` is a bounded one-off evidence preparation record, not a
  tooling project or a formatter replacement. Its only Git subprocess is read-only
  `git show` at the supplied source pin. No Git index/ref/worktree operation occurred.

For every proposal, `after + b"\n" == before`. Thus all executable tokens, imports
and their order, namespaces, directives, ordinary comments, XML text and metadata,
and all nontrailing bytes are exactly preserved, rather than merely normalized.
There are no XML-comment or directive final lines requiring special EOF handling.
The final line is `}` in 236 files. The other three final compilation units were
reviewed separately and also retain every declaration byte, including the semicolon:

| Path | Final unit unchanged except following LF |
|---|---|
| `src/Core/src/Devices/DeviceChange.cs` | `public sealed record SnapshotComplete(long Revision) : DeviceChange(Revision);` |
| `src/Core/src/Devices/HotplugSignals.cs` | `internal readonly record struct ListenerSource(ConnectionType Transport, Func<IDeviceChangeListener> Create);` |
| `src/Core/src/Protocols/SmartCard/Scp/DataEncryptor.cs` | `public delegate byte[] DataEncryptor(ReadOnlySpan<byte> data);` |

No introduced ordinary-comment whitespace finding exists in this actual report.
No import, declaration, SDK behavior, public body text, documentation metadata,
analyzer suppression, or style configuration was changed to satisfy formatting.

## Ten preservation-conflicting baseline findings — leave unchanged

Source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
`baseline-findings.json` contains exact current and pinned ordered import lists,
reported sites, file hashes, and per-row classification. `baseline-context.json`
also records the matching namespaces and surrounding current/pinned source lines.
Every reported site is identical at the same line at the pin; every involved file's
import sequence and namespace is unchanged. For `IMPORTS`, the reported location
is the file-start sentinel, so the complete unchanged import sequence is the
relevant evidence rather than the copyright line alone.

| Diagnostic | Path and current/pinned line | Exact preserved site |
|---|---|---|
| IMPORTS | `src/Piv/src/IYubiKeyExtensions.cs:1` | Same six imports in the same order; `Core.Devices` still follows the protocol imports. |
| IDE1006 | `src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:19` | `internal enum kern_return_t` |
| IDE1006 | `src/Core/src/Native/Linux/Udev/Udev.Interop.cs:189` | `public static IntPtr udev_device_get_parent(LinuxUdevDeviceSafeHandle deviceObject) =>` |
| IDE1006 | `src/Core/src/Native/Linux/Udev/Udev.Interop.cs:211` | `public static IntPtr udev_device_get_syspath(LinuxUdevDeviceSafeHandle deviceObject) =>` |
| IDE0005 | `src/Core/src/Devices/CompositeDeviceMerger.cs:15` | `using Yubico.YubiKit.Core.Abstractions;` |
| IDE0005 | `src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:19` | `using Yubico.YubiKit.Core.Native;` |
| IDE0005 | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CompositeDeviceMergerVectorTests.cs:17` | `using Yubico.YubiKit.Core.UnitTests.Infrastructure;` |
| IDE0005 | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/ProtocolConnectionOwnershipTests.cs:15` | `using Yubico.YubiKit.Core.Abstractions;` |
| IDE0005 | `src/Piv/src/IYubiKeyExtensions.cs:17` | `using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;` |
| IDE0005 | `src/OpenPgp/src/IYubiKeyExtensions.cs:17` | `using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;` |

These are explicit **source-pinned, preservation-conflicting baseline findings**:
their requested remediation would reorder/remove imports or rename interop symbols,
which this task forbids. Do not waive them silently or suppress them with `NoWarn`,
diagnostic configuration, or a blanket “formatter clean” claim. The formatter was
not replayed on the source pin: this is direct source-site preservation evidence,
not a claim that pinned diagnostic output was measured in the same environment.

## Actual workspace coverage and remaining qualification

The accepted map has **363 product paths, of which 310 are C#**. The retained
invocation supplies those 310 paths as separate `--include` arguments. The actual
log ends **“Formatted 241 of 1426 files.”** It is a verify-only run and no primary
bytes changed: “Formatted” identifies files for which changes were proposed, not
files actually written and not the full set of requested files processed.

`format-membership.json` records all 310 requested paths, the 42 solution projects,
static project-root/literal linked-Compile candidates, and each actual changed-file
log entry. There are 294 static candidates and **16 requested paths outside those
solution project roots and literal linked Compile includes**:

```text
benchmarks/Yubico.YubiKit.PerformanceBenchmarks/AsyncBoundaryBaseline.cs
benchmarks/Yubico.YubiKit.PerformanceBenchmarks/Program.cs
scripts/toolchain/Process.cs
scripts/toolchain/Results.cs
scripts/toolchain/TestTargets.cs
src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/SecureCredentialTests.cs
verification/LinuxHidRouteVerification/Evidence.cs
verification/LinuxHidRouteVerification/OperatorScenarios.cs
verification/MacOSHidBoundaryComparison/Program.cs
verification/MacOSHidInputVerification/Program.cs
verification/MacOSHidRouteVerification/ListenerDrainScenario.cs
verification/MacOSHidRouteVerification/Program.cs
verification/MacOSHidRouteVerification/SmartCardScenario.cs
verification/NativeAotVerification/Program.cs
verification/NativeCompatibilityVerification/Program.cs
verification/NativeCompatibilityVerification/RuntimeConsumer/Program.cs
```

Treat these 16 as **unmatched by the solution-scoped formatting gate**, not as
verified clean. Static candidate mapping does not prove runtime workspace membership
for the other 294, and absence from the changed-file log does not prove a skip.
No namespace, helper context, project inclusion, or solution configuration was
changed to manufacture membership.

If the coordinator needs stronger membership evidence, the minimal additional
read-only SDK operation for Astra is evaluation-only, once per existing solution
project, with the run's Debug/net10.0 context:

```text
dotnet msbuild <existing-solution-project.csproj> -nologo -p:Configuration=Debug -p:TargetFramework=net10.0 -getItem:Compile
```

Do not pass a target or restore flag; retain JSON output externally and compare
`Items.Compile[].FullPath` against the requested path list. This evaluates project
items without building/testing or introducing a helper project; it is still not an
enumeration of Roslyn's actual loaded workspace. This operation was **not run** here.
The API/CLI syntax was checked against current MSBuild documentation through
Context7 (`/dotnet/msbuild`, `XMake.cs` and `JsonOutputFormatter.cs`): no-target
`-getItem` returns evaluation items as JSON without executing build targets.

Alternatively retain the explicit unmatched/coverage qualification for Pass 1;
do not expand this task into new formatter projects or replay native/device work.

## Coordinator handoff

1. Independently review this EOF-only patch and preserved baseline findings.
2. Before any authorized apply, verify each current source against its exact
   `before_sha256`; stop on drift. Apply only the reviewed 239 EOF deletions and
   verify each `after_sha256`. Update acceptance records through the existing
   coordinator-owned mechanism, not this proposal's private evidence script.
3. Keep the actual failed formatter receipt and ten baseline conflicts explicit.
   A new final formatter result cannot be inferred from this byte-level proposal.
4. Any final clean build or approved formatter check belongs to Astra/coordinator
   execution after review/apply. None was run by this bounded reconciliation task.
5. Continue only the already authorized Pass 1 publish/deferred handoff, then stop.
