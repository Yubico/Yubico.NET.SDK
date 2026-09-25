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
for the third row (paths are local artifacts; neither worktree is changed):

```sh
OLD_CORE=/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-baseline-65964966-async3/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
CURRENT_CORE=src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
OLD_NATIVE="$HOME/.nuget/packages/yubico.nativeshims/1.18.0/runtimes/osx-arm64/native/libYubico.NativeShims.dylib"
NEW_NATIVE="$HOME/.nuget/packages/yubico.nativeshims/1.18.1-async.8/runtimes/osx-arm64/native/libYubico.NativeShims.dylib"
dotnet run --project verification/NativeCompatibilityVerification/RuntimeConsumer/RuntimeConsumer.csproj -c Release -p:CoreAssemblyPath="$OLD_CORE" -- "$OLD_NATIVE" 928a0b6fcc75e6940d385621065aebed716957ef7d5988d27c5e251e6e2550c6 96feaab51e8d8079d1bd0109b0479c750be09faa008361dc4f46dc1642009eac
dotnet run --project verification/NativeCompatibilityVerification/RuntimeConsumer/RuntimeConsumer.csproj -c Release --no-build -- "$NEW_NATIVE" 928a0b6fcc75e6940d385621065aebed716957ef7d5988d27c5e251e6e2550c6 65499e77151d483d8e04bd793c2d520b4f344478ffcacff91bff8e771ae9d8af
dotnet run --project verification/NativeCompatibilityVerification/RuntimeConsumer/RuntimeConsumer.csproj -c Release -p:CoreAssemblyPath="$CURRENT_CORE" -- "$NEW_NATIVE" 9f8de622ff2488643167ca83c44476911dc688a918509f1df1cb12da85ce6ce1 65499e77151d483d8e04bd793c2d520b4f344478ffcacff91bff8e771ae9d8af
```

Observed locally on macOS arm64: each row passed its Core/native SHA-256 checks,
the P-256 generator on-curve and scalar-2 shared-secret vector, and the AES-128
CMAC empty-message known-answer vector. Each row recorded 17 SDK native resolver
calls. A deliberately mismatched native hash failed before installing the resolver.
The consumer uses typed delegates to the actual Core internal OpenSSL primitive
methods because there is no Core public no-device native-crypto entry point; the
application-facing cryptography provider properties are managed-only. This is
**a local build of historical managed Core `65964966`** exercising native BN/EC/CMAC entry points,
but not a public-API consumer journey. Native imports are resolved explicitly
to the hash-checked library. The consumer does not invoke smart-card or HID paths.

The JSON file pins the canonical 36-export NativeShims 1.18.0 macOS set (from the native producer's `tests/expected_symbols.txt`), **not** a list derived from managed Core imports. The pre-input-owner Core (`65964966cc21f431e16793264c899c30a5b957a3`) imports 28 of these symbols (CMAC 5, EC 8, BN 4, SCard 11); this fixture does not detect drift in Core's import declarations. The checker requires all 36 exports in both dylibs, plus all five `Native_HidInput*` symbols in the current macOS route (`3e928106ba51fd07df5d2a6edf3c47037213ef88`). It requires the five to be absent in the stable dylib. It loads and frees each dylib sequentially, calls `Native_BN_new` / `Native_BN_clear_free`, and checks the binary digests. The package labels are *inputs*, not inferred from exports: stable `Yubico.NativeShims` 1.18.0 arm64 dylib SHA-256 `96feaab51e8d8079d1bd0109b0479c750be09faa008361dc4f46dc1642009eac`; private-published `1.18.1-async.8` arm64 dylib SHA-256 `65499e77151d483d8e04bd793c2d520b4f344478ffcacff91bff8e771ae9d8af` (package SHA-256 `c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`, producer source `71a23cd0269c968c1d9420eddb2e2fec71e0cc29`). The stable package's exact producer commit is not established by its metadata.

| Native export requirement (managed source context) | Native package | Bounded result on macOS arm64 |
| --- | --- | --- |
| Legacy 36-export set (pre-input-owner Core `65964966`) | 1.18.0 | Present: 36/36 exports; locally built historical Core binary passed native BN/EC/CMAC success-path vectors in isolated process |
| Legacy 36-export set (pre-input-owner Core `65964966`) | 1.18.1-async.8 | Present: 36/36 exports; **same locally built historical Core binary** passed the same success-path vectors in isolated process |
| Input-owner requirement (current Core `3e928106`) | 1.18.1-async.8 | Present: 5/5 input-owner exports; current Core binary passed legacy BN/EC/CMAC vectors; no input callbacks or signatures exercised |
| Input-owner requirement (current Core `3e928106`) | 1.18.0 | **Unsupported**: 0/5 required input-owner exports; no missing entry point invoked |

The `PublicApi` compatibility tests compile a consumer implementation of the retained public `IHidConnection`, construct the existing internal FIDO and OTP raw adapters by reflection once each, and then call their public typed interfaces directly. They check report forwarding, SDK-owned send-copy clearing, caller-buffer retention, and disposal forwarding. This is an **adapter compatibility fixture**, not an external public factory or discovery end-to-end test: a consumer cannot directly construct these internal adapters. The `IHidDevice` → `IHidInterface` rename intentionally breaks alpha *source* compatibility; this matrix does not claim otherwise. The report connection interface is not the discovery interface. These checks do not prove all 28 managed import signatures or export behavior, exercise hardware or native input lifecycle, or establish Windows/Linux driver compatibility. The private-published package is not a hosted-driver compatibility certificate. Keep platform and device acceptance separate.

Substituting `missing-baseline-export.json` for `baseline-exports.json` is a negative control: the checker must fail reporting `Native_SCardMissingFixture` missing.
