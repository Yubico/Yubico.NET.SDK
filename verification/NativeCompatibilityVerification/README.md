# Bounded macOS compatibility matrix

Run from the repository root on macOS arm64 with the locally cached packages:

```sh
dotnet run --project verification/NativeCompatibilityVerification/NativeCompatibilityVerification.csproj -c Release -- verification/NativeCompatibilityVerification/baseline-exports.json "$HOME/.nuget/packages/yubico.nativeshims/1.18.0/runtimes/osx-arm64/native/libYubico.NativeShims.dylib" "$HOME/.nuget/packages/yubico.nativeshims/1.18.1-async.8/runtimes/osx-arm64/native/libYubico.NativeShims.dylib"
dotnet toolchain.cs -- test --project PublicApi --filter "FullyQualifiedName~HidConnectionCompatibilityTests"
```

The separately compiled runtime consumer references the **existing** Core binary from
the `65964966` baseline worktree, rather than compiling its source in the current tree.
Run each pairing in a separate process. Build the first row, reuse that same consumer
binary with `--no-build` for the second row, then compile against the current Core binary
for the third row (paths are local artifacts; neither worktree is changed).

The current Core binary is not pinned by a fixed hash: `src/Core/src/bin/Release/net10.0` changes whenever Core is rebuilt,
and it was rebuilt concurrently during this work (`9f8de622…` at first, then `afbc26cd…`, then `38f43b1c…`).
The third row therefore snapshots the current output directory once, hashes that snapshot, and compiles against the same snapshot and hash.
The SHA-256 printed by the last command is the identity of the "current Core" that was tested, and it is the value to record.
The first two rows keep their fixed historical hashes.

```sh
OLD_CORE=/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-baseline-65964966-async3/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
OLD_NATIVE="$HOME/.nuget/packages/yubico.nativeshims/1.18.0/runtimes/osx-arm64/native/libYubico.NativeShims.dylib"
NEW_NATIVE="$HOME/.nuget/packages/yubico.nativeshims/1.18.1-async.8/runtimes/osx-arm64/native/libYubico.NativeShims.dylib"
dotnet run --project verification/NativeCompatibilityVerification/RuntimeConsumer/RuntimeConsumer.csproj -c Release -p:CoreAssemblyPath="$OLD_CORE" -- "$OLD_NATIVE" 928a0b6fcc75e6940d385621065aebed716957ef7d5988d27c5e251e6e2550c6 96feaab51e8d8079d1bd0109b0479c750be09faa008361dc4f46dc1642009eac
dotnet run --project verification/NativeCompatibilityVerification/RuntimeConsumer/RuntimeConsumer.csproj -c Release --no-build -- "$NEW_NATIVE" 928a0b6fcc75e6940d385621065aebed716957ef7d5988d27c5e251e6e2550c6 65499e77151d483d8e04bd793c2d520b4f344478ffcacff91bff8e771ae9d8af
CURRENT_SNAPSHOT="$(mktemp -d)"
cp src/Core/src/bin/Release/net10.0/* "$CURRENT_SNAPSHOT"/
CURRENT_CORE="$CURRENT_SNAPSHOT/Yubico.YubiKit.Core.dll"
CURRENT_CORE_SHA="$(shasum -a 256 "$CURRENT_CORE" | cut -d' ' -f1)"
echo "current Core under test: $CURRENT_CORE_SHA"
dotnet run --project verification/NativeCompatibilityVerification/RuntimeConsumer/RuntimeConsumer.csproj -c Release -p:CoreAssemblyPath="$CURRENT_CORE" -- "$NEW_NATIVE" "$CURRENT_CORE_SHA" 65499e77151d483d8e04bd793c2d520b4f344478ffcacff91bff8e771ae9d8af
```

A fourth argument, `--control-release-failure`, turns any row into the cleanup negative control described under the SCard stage.
The run is expected to fail.

Observed locally on macOS arm64: each row passed its Core/native SHA-256 checks,
the P-256 generator on-curve and scalar-2 shared-secret vector, and the AES-128
CMAC empty-message known-answer vector. Each row recorded 17 SDK native resolver
calls (before the SCard stage below was added). A deliberately mismatched native hash failed before installing the resolver.
The consumer uses typed delegates to the actual Core internal OpenSSL primitive
methods because there is no Core public no-device native-crypto entry point; the
application-facing cryptography provider properties are managed-only. This is
**a local build of historical managed Core `65964966`** exercising native BN/EC/CMAC entry points,
but not a public-API consumer journey. Native imports are resolved explicitly
to the hash-checked library. The crypto stage does not invoke smart-card or HID paths;
the consumer never invokes HID paths. The separate, hardware-free SCard stage is described below.

## SCard context and error stage

After the crypto vectors, the same process calls four of Core's own `LibraryImport`
methods on `NativeMethods` (`Yubico.YubiKit.Core.Native.Desktop.SCard`) by reflection,
because the methods and their `SafeHandle`/enum parameter types are internal.
`MethodInfo.Invoke` handles the `out SCardContext` and `out SCardCardHandle` parameters
directly, so the generated marshalling stubs are exercised as is.
Nothing is re-declared. In source, the whole `Native/Desktop/SCard` directory and `Native/Libraries.cs` are identical between the `65964966` baseline worktree and the current tree.
This is a source comparison. It does not prove that the tested binaries were compiled from those exact files.

1. `SCardEstablishContext(SCARD_SCOPE.USER, out SCardContext)`: must succeed.
2. `SCardCancel` with an `SCardContext` wrapping the deliberately invalid value `0x5A5A5A5A`:
   the result must be non-success. The wrapper is then marked invalid, so the bogus value never reaches `SCardReleaseContext`.
3. `SCardConnect(context, "YubiKit Verifier Nonexistent Reader 7d3f0c1e", SHARED, T0|T1, out card, out protocol)`:
   the result must be non-success, and the returned card handle must be invalid.
   A success, or a failure that still returns a valid handle, fails the run.
   Either way the handle is left to `using` disposal (`SCardDisconnect`, `LEAVE_CARD`) and is never suppressed.
4. `SCardReleaseContext` on the established context, explicitly, so its code is observed.
   Only a successful release marks the `SafeHandle` invalid.
   On failure the handle stays open, so its own `ReleaseHandle` remains the cleanup fallback, and the run fails.

Codes are printed with every matching `ErrorCode` constant name from the loaded Core.
The checks do not hard-code the observed error values.
Observed on macOS arm64 on 2026-09-25 (native imports resolved only through the hash-checked dylib):

| Core binary (SHA-256) | Native (dylib SHA-256) | Establish | Cancel(invalid) | Connect(nonexistent) | Release | Resolver calls |
| --- | --- | --- | --- | --- | --- | --- |
| historical `65964966` local build `928a0b6f…50c6` | 1.18.0 `96feaab5…9eac` | `0x00000000` S_SUCCESS | `0x80100003` E_INVALID_HANDLE | `0x80100009` E_UNKNOWN_READER, card handle invalid | `0x00000000` | 21 (17 crypto, 4 SCard) |
| same binary `928a0b6f…50c6` (`--no-build`) | 1.18.1-async.8 `65499e77…d8af` | `0x00000000` | `0x80100003` | `0x80100009`, card handle invalid | `0x00000000` | 21 (17, 4) |
| current-tree working build `afbc26cd496bb0b295c3a1abab753aded2c24456096db180dcae606e7b2dfded` (snapshot) | 1.18.1-async.8 `65499e77…d8af` | `0x00000000` | `0x80100003` | `0x80100009`, card handle invalid | `0x00000000` | 21 (17, 4) |

The current Core binary was a working-tree build (HEAD `70e35389` plus uncommitted changes by concurrent work) taken from
`src/Core/src/bin/Release/net10.0` at 21:04 local time. It was snapshotted before compiling, as in the command block above.
By the time these rows were re-run, the live output had been rebuilt as `38f43b1c4dc7e7efff69381554f190159b0c56f2ffa80ad28d1b6ead93e81417`.
Running the third-row command block verbatim against that newer binary gave the same four codes and 21 (17, 4) resolver calls, with exit code 0.
The recorded row remains `afbc26cd…`, and `38f43b1c…` only confirms that the command block works.
Its source revision is identified only by the binary hash `afbc26cd…`, not by a commit. The earlier crypto-only observation used a different current binary, `9f8de622…`.
The consumer refuses any Core or native binary whose SHA-256 differs from its arguments.
Current Core with native 1.18.0 remains **unsupported**, because the input-owner exports are missing. This stage was not run for that pairing, and passing SCard calls would not change it.

Negative controls observed on the same date:

- A wrong native hash (`96feaab5…` passed for the async.8 dylib) exits 134 with `SHA-256 mismatch` before the resolver is installed, and no SCard call is made.
- A wrong Core hash (`928a0b6f…` passed for the `afbc26cd…` build) exits 134 the same way.
- `missing-baseline-export.json` exits 134 with `legacy contract + native 1.18.0: missing Native_SCardMissingFixture`.
- `--control-release-failure` was run with the old Core and with the current `afbc26cd…` snapshot, both against async.8.
  The context is first released with a raw call (`0x00000000`), which bypasses the helper that marks the handle invalid.
  The explicit release then returns `0x80100003` (E_INVALID_HANDLE). The run exits 134 with
  `SafeHandle left for disposal fallback (IsInvalid=False, IsClosed=False)`, which shows that a failed release does not suppress the handle's own cleanup.
  The control does not observe `ReleaseHandle` running. Because the exception is unhandled, the runtime does not guarantee that `using`/`finally` disposal or finalizers run before the process exits.
- There is no control for the connect branch that fails with a valid handle. Triggering it needs a reader, a real card, or a fake native layer, and all three are out of scope.

Limits:

- This proves that four SCard `LibraryImport` entry points bind and marshal against both dylibs on this macOS arm64 host.
  It covers the context `SafeHandle` in/out paths, a UTF-8 string, enum arguments, and the `out` card handle and protocol on failure.
  It also records how macOS PC/SC reports an invalid context and an unknown reader through each Core/native pairing.
- It does not exercise `SCardListReaders`, `SCardGetStatusChange`, transactions, `SCardTransmit`, `SCardReconnect`, a successful connect, or `SCardDisconnect`.
  It opens no reader or card. It does not show the SDK's `ISCardApi` / `PcscNativeResources` wrappers or public discovery behaving correctly.
- The error values are observations from one host's PC/SC service, not a Windows/Linux contract.
  A host without a running PC/SC service fails at step 1 and reports the code it received.
- The invalid handle is a fixed constant checked against the established context. It is not a stale or freed handle.

The JSON file pins the canonical 36-export NativeShims 1.18.0 macOS set (from the native producer's `tests/expected_symbols.txt`), **not** a list derived from managed Core imports. The pre-input-owner Core (`65964966cc21f431e16793264c899c30a5b957a3`) imports 28 of these symbols (CMAC 5, EC 8, BN 4, SCard 11); this fixture does not detect drift in Core's import declarations. The checker requires all 36 exports in both dylibs, plus all five `Native_HidInput*` symbols in the current macOS route (`3e928106ba51fd07df5d2a6edf3c47037213ef88`). It requires the five to be absent in the stable dylib. It loads and frees each dylib sequentially, calls `Native_BN_new` / `Native_BN_clear_free`, and checks the binary digests. The package labels are *inputs*, not inferred from exports: stable `Yubico.NativeShims` 1.18.0 arm64 dylib SHA-256 `96feaab51e8d8079d1bd0109b0479c750be09faa008361dc4f46dc1642009eac`; private-published `1.18.1-async.8` arm64 dylib SHA-256 `65499e77151d483d8e04bd793c2d520b4f344478ffcacff91bff8e771ae9d8af` (package SHA-256 `c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`, producer source `71a23cd0269c968c1d9420eddb2e2fec71e0cc29`). The stable package's exact producer commit is not established by its metadata.

| Native export requirement (managed source context) | Native package | Bounded result on macOS arm64 |
| --- | --- | --- |
| Legacy 36-export set (pre-input-owner Core `65964966`) | 1.18.0 | Present: 36/36 exports; locally built historical Core binary passed native BN/EC/CMAC success-path vectors in isolated process |
| Legacy 36-export set (pre-input-owner Core `65964966`) | 1.18.1-async.8 | Present: 36/36 exports; **same locally built historical Core binary** passed the same success-path vectors in isolated process |
| Input-owner requirement (current Core `3e928106`) | 1.18.1-async.8 | Present: 5/5 input-owner exports; current Core binary passed legacy BN/EC/CMAC vectors; no input callbacks or signatures exercised |
| Input-owner requirement (current Core `3e928106`) | 1.18.0 | **Unsupported**: 0/5 required input-owner exports; no missing entry point invoked |

The `PublicApi` compatibility tests compile a consumer implementation of the retained public `IHidConnection`, construct the existing internal FIDO and OTP raw adapters by reflection once each, and then call their public typed interfaces directly. They check report forwarding, SDK-owned send-copy clearing, caller-buffer retention, and disposal forwarding. This is an **adapter compatibility fixture**, not an external public factory or discovery end-to-end test: a consumer cannot directly construct these internal adapters. The `IHidDevice` → `IHidInterface` rename intentionally breaks alpha *source* compatibility; this matrix does not claim otherwise. The report connection interface is not the discovery interface. These checks do not prove all 28 managed import signatures or export behavior, exercise hardware or native input lifecycle, or establish Windows/Linux driver compatibility. The private-published package is not a hosted-driver compatibility certificate. Keep platform and device acceptance separate.

Substituting `missing-baseline-export.json` for `baseline-exports.json` is a negative control: the checker must fail reporting `Native_SCardMissingFixture` missing.
