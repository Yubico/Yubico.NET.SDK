# macOS verification handoff for adapter consistency

**Planned, not executed on macOS.** The C1/C2 extraction has Linux-hosted managed
characterization and source-equivalence evidence. macOS native/hardware testing
remains required before merge. Bind every run below to the final PR commit; the
[Windows parent checkpoint](windows-async-native-acceptance.md) has a different
source identity. Native AOT/linker verification is planned for a later VM.

## Source and managed checks

Run sequentially from the final checkout on the Mac. Retain source identity and
environment before running:

```bash
git rev-parse HEAD
git status --porcelain=v1
git diff HEAD --binary
sw_vers
uname -m
dotnet --info

dotnet toolchain.cs build
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~MacOS"
dotnet toolchain.cs -- resilience --fast --filter "FullyQualifiedName~MacOS"
dotnet toolchain.cs -- test --project Ctap2.UnitTests --filter "FullyQualifiedName~Ctap2SessionHidCrossLayerTests"
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~BoundaryInventory"
```

Hash untracked source inputs separately; `git diff` does not include them. Require
successful exit, `RESULT: PASSED`, zero warnings and nonzero executed tests. Inspect
all skips. Archive each complete toolchain run directory outside its five-run
rotation. These tests use controlled seams; registry links do not recursively run
the scenarios they name.

## Production-route JIT verifier

`verification/MacOSHidRouteVerification` calls the production bridge, report APIs,
owners and direct facades. There is no macOS verifier toolchain target in this
checkout. Use the project directly for local executable publication, with an
explicit `PublishAot=false` because its project defaults to AOT.

Set `RID` to the actual process/native-asset architecture (`osx-arm64` or `osx-x64`),
`ROUTE_PUBLISH` to a fresh absolute output directory, and `SERIAL` to the authorized
positive decimal serial before using these commands:

```bash
dotnet publish verification/MacOSHidRouteVerification/MacOSHidRouteVerification.csproj \
  -c Release -r "$RID" --self-contained -p:PublishAot=false -o "$ROUTE_PUBLISH"

"$ROUTE_PUBLISH/MacOSHidRouteVerification" --timing-selftest
```

The timing self-test exercises managed bookkeeping only. It is not native HID
acceptance. Retain the publish log and hash manifest of the fresh output.

### Physical-key checks

Use an already attached fixture authorized on this Mac. Confirm the attached-device
discovery scope, enabled interfaces and lack of competing clients first. Windows
serial authorization is not automatically Mac authorization. Expert modes require
one discovered key and one corresponding HID interface. IOKit keyboard/listener
access may require Input Monitoring for the actual launcher; record permission
failures as blocked rather than automatically elevating or changing settings.

Run one mode at a time and stop on failure, watchdog timeout or uncertain cleanup:

```bash
"$ROUTE_PUBLISH/MacOSHidRouteVerification" --probe --serial "$SERIAL"
"$ROUTE_PUBLISH/MacOSHidRouteVerification" --otp-get --serial "$SERIAL"
"$ROUTE_PUBLISH/MacOSHidRouteVerification" --otp-info --serial "$SERIAL"
"$ROUTE_PUBLISH/MacOSHidRouteVerification" --expert-io --serial "$SERIAL"
"$ROUTE_PUBLISH/MacOSHidRouteVerification" --expert-feature --serial "$SERIAL"
"$ROUTE_PUBLISH/MacOSHidRouteVerification" --smartcard --serial "$SERIAL"
```

| Mode | Required evidence |
|---|---|
| `--probe` | Three FIDO INIT/GetInfo/dispose cycles; borrowed-session reuse; pending-read disposal; reopen; pre-dispatch cancellation followed by successful reuse. |
| `--otp-get` | Three typed OTP open/feature-GET/dispose cycles and correct report length. |
| `--otp-info` | Three Management metadata queries explicitly over OTP HID, matching the serial. |
| `--expert-io` | Expected six-second unsolicited-read timeout, then same-connection INIT/GetInfo and dispose/reopen/GetInfo. |
| `--expert-feature` | Three synchronous feature-facade cycles, eight-byte feature GET and matching OTP metadata. |
| `--smartcard` | Three PC/SC open/begin/select/read/async-end/dispose cycles with stable identity. |

Require every expected phase/PASS message, exit zero and the parent's `child exit=0`.
A watchdog-killed child is OS cleanup, not SDK drain proof. Metadata queries can
send feature SET reports; these modes avoid persistent configuration mutation,
not all output reports. Discovery may inspect attached devices before selecting
the requested serial.

### Bounded xUnit companions

After checking the effective `YubiKeyTests:AllowedSerialNumbers` configuration:

```bash
dotnet toolchain.cs -- test --integration --project Core.IntegrationTests --smoke \
  --filter "FullyQualifiedName~FidoHidOwnershipIntegrationTests"
dotnet toolchain.cs -- test --integration --project Core.IntegrationTests --smoke \
  --filter "FullyQualifiedName~PcscLifetimeIntegrationTests"
dotnet toolchain.cs -- test --integration --project Ctap2.IntegrationTests --smoke \
  --filter "(FullyQualifiedName~CreateCtap2Session_With_ExtensionMethod|FullyQualifiedName~GetInfoAsync_With_YubiKeyExtensionMethod|FullyQualifiedName~CreateCtap2Session_SecondExplicitHidCtap2Session_IsRefused)"
dotnet toolchain.cs -- test --integration --project Ctap2.IntegrationTests --smoke \
  --filter "FullyQualifiedName~CreateCtap2Session_With_SmartCard_CreateAsync"
```

Expected source selections: three FIDO ownership methods, two PC/SC lifetime
methods, three CTAP HID methods and one explicit CTAP SmartCard method. Actual
executed counts and fixture selection must be recorded. The SmartCard case can
skip unsupported firmware/AID; that is not acceptance of that route. PC/SC lifetime
tests select the highest-serial eligible authorized USB fixture.

## Separately authorized scenarios

Default scope excludes touch, topology changes, two-key tests, PIN operations,
credential ceremonies, reset, slot-one mutation and slot swapping.

Optional OTP slot-two programming, HMAC and deletion may be selected later using
`CalculateHmacSha1_WithKnownKey_ReturnsExpectedResponse`,
`YubiOtpHmacChallengeKnownAnswerTests` and `YubiOtpSlotConfigTests`. Confirm explicit
Mac fixture authorization first: cleanup deletes the test configuration and cannot
restore overwritten secrets. Some initial programming occurs before cleanup's
`try/finally`; a programming failure can leave uncertain state.

Do not select the whole `YubiOtpProtectSlot2Tests` class: it includes a case that
writes/deletes slot one. Do not select whole CTAP classes merely because their names
mention GetInfo. `WithCtap2SessionAsync` defaults to PIN normalization, which can
set/change a PIN or request a token. `--smoke` removes only Slow and
RequiresUserPresence cases; it does not remove PIN normalization or mutations.

The route verifier's `--listener-drain` and `--listener-late-drain` modes require
separate topology/permission approval: matching callbacks are not serial-filtered.
Its `--active-cancel` executes CTAP Selection and waits for user-presence notification,
so it is outside GetInfo-only scope. `--touch`, `--removal` and `--listener-remove`
require attended physical actions.

## Experimental native lane and evidence custody

`verification/MacOSHidInputVerification` is a distinct five-scenario synthetic
native harness. It needs externally built `libhidinput_experiment.dylib` and calls
`hidinput_test_*`/`hidinput_*`, not the packaged production bridge exports. This
checkout has no native producer or macOS verifier toolchain target. Without a
source-bound artifact it is blocked. It must not substitute for production-route
checks. Its console label says AOT even with JIT publication; classify runtime from
the actual build inputs and artifacts.

For each native/device run retain SDK/verifier commit and source snapshot, exact
command, timestamps, logs/exits, macOS build, machine/process architecture and any
Rosetta use, SDK/runtime/RID, fixture identity/firmware/interfaces, launch/permission
context, and phase counts. Record resolved `Yubico.NativeShims` package identity
(currently 1.18.1), package integrity metadata, RID asset hash/architecture and
deployed dylib hashes. Independently compare deployed bytes with their package
assets. A deployed path/hash does not prove the loaded image path; record loader
observation or mark that binding unobserved.

Retain full publish manifests and toolchain run/step logs. The route verifier has
no Windows-style publication seal; manually collected JIT evidence is unsealed.
Keep managed, experimental-native, production-device and later AOT results distinct.

On the later prepared Windows VM, `dotnet toolchain.cs windows-hid-verification-nohardware`
is the existing combined fresh JIT/AOT synthetic-pipe gate. It neither proves
physical HID-driver behavior nor macOS AOT behavior. All deferred evidence remains
open until its actual source-bound execution is recorded.
