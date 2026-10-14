# Native AOT support

This document defines Native AOT support for the v2 SDK libraries. The supported set is not a list
maintained here: it is every published SDK library, which `dotnet toolchain.cs
native-aot-contract-qa` derives from the `src/<Module>/src/*.csproj` layout and reports on each
build. At the time of writing that is `Core`, `Management`, `Piv`, `Fido2`, `WebAuthn`, `Oath`,
`OpenPgp`, `SecurityDomain`, `YubiOtp`, and `YubiHsm`. Command-line tools, example applications,
verification hosts, and test projects are not part of this support surface.

## Support statement

Every SDK library publishes Native AOT compatibility metadata and enables the AOT, trimming,
single-file, and reference-compatibility analyzers. A verification host references all of them and
has been published successfully for macOS Apple Silicon, Windows x64, and Linux x64.

The evidence is not equally deep for every platform or module:

- Every SDK library is analyzer-checked during normal builds.
- Every SDK library is linked into the Native AOT verification host, and the contract target fails
  the build if one is missing the opt-in, the host project reference, or a rooted entry type.
- Core device discovery has run against physical YubiKeys on macOS, Windows, and Linux.
- Management device-information reads, a PIV session, and device monitoring have run against
  physical YubiKeys on macOS.
- Fido2, WebAuthn, Oath, OpenPgp, SecurityDomain, YubiOtp, and YubiHsm are link-verified but have not
  had representative protocol operations runtime-exercised under Native AOT.
- The continuous-integration workflow is configured to publish and run the host on macOS Apple
  Silicon, Windows x64, and Linux x64 with no hardware attached. Historical manual results are
  recorded separately; workflow configuration alone is not evidence of a successful run.

The research records preserve the environment, commands, and observations behind these claims:

- [`native-aot-readiness.md`](research/native-aot-readiness.md)
- [`native-aot-readiness-data.md`](research/native-aot-readiness-data.md)

## Evidence terms

| Term | Meaning |
|---|---|
| Analyzer-checked | The library builds with `IsAotCompatible=true` and `VerifyReferenceAotCompatibility=true`, with AOT-related warnings treated as errors. |
| Link-verified | The Native AOT compiler processes a reachable type from the library and produces the native executable. This does not prove that a protocol operation works at runtime. |
| Runtime-exercised | The published native executable executes the named code path successfully. |
| Hardware-exercised | Runtime evidence includes communication with a physical YubiKey. |
| Recurring coverage | The repository workflow repeats the evidence on supported branch or pull-request changes. |

## Evidence matrix

| Platform | Runtime identifier | Analyzer-checked | All libraries link | Core discovery on hardware | Deeper hardware evidence | Recurring coverage |
|---|---|---|---|---|---|---|
| macOS Apple Silicon | `osx-arm64` | yes | yes | yes | Management device information, PIV session, device monitoring | publish and Core discovery entry point |
| Windows x64 | `win-x64` | yes | historical evidence | yes | none recorded | publish and Core discovery entry point configured |
| Linux x64 | `linux-x64` | yes | yes | yes | none recorded | publish and Core discovery entry point configured |

Only macOS has device-monitoring evidence. Windows and Linux monitoring and protocol-session paths
remain unverified under Native AOT.

A separate Linux experiment also published and ran a packaged consumer. That is package-resolution
evidence, not deeper protocol or hardware evidence.

At revision `df1ec06d`, NativeShims 1.18.1 exposed a Linux AOT link regression: reachable macOS HID
bridge calls required five `Native_HidInput*` exports that correctly do not exist in the Linux
archive. Explicit `OperatingSystem.IsMacOS()` guards at those native call sites resolve the Linux
link failure. The corrected host published and discovered one attached YubiKey on Linux x64 with
.NET SDK 10.0.401 / runtime 10.0.12. Windows and macOS still require fresh runner evidence for this
correction; the earlier research records predate it.

## Project configuration

Each supported library sets:

```xml
<IsYubiKitSdkLibrary>true</IsYubiKitSdkLibrary>
```

`Directory.Build.props` defaults this property to `false`. `Directory.Build.targets` reads the
project-level opt-in and sets:

```xml
<IsAotCompatible>true</IsAotCompatible>
<VerifyReferenceAotCompatibility>true</VerifyReferenceAotCompatibility>
```

`IsAotCompatible` also enables the AOT, trimming, and single-file analyzers. The repository does
not suppress `IL3058`; a managed dependency without Native AOT compatibility metadata fails the
warnings-as-errors build.

The metadata and analyzer result are library compatibility evidence. They do not, by themselves,
prove that every runtime path has executed under Native AOT.

## Verification host

[`verification/NativeAotVerification`](../verification/NativeAotVerification) is an internal,
non-packable console host. Its default mode:

1. References a public entry type from each SDK library so the Native AOT compiler processes all of
   them. A project reference alone is not enough — without a rooted type the compiler trims the
   assembly out of the host's closure — so `native-aot-contract-qa` checks for both.
2. Runs `YubiKeyManager.FindAllAsync` through Core.
3. Succeeds with an empty device list when no YubiKey is attached.

The recurring workflow [`.github/workflows/native-aot.yml`](../.github/workflows/native-aot.yml)
is configured to publish and run this default mode on macOS Apple Silicon, Windows x64, and Linux
x64. A passing run proves publish, link, process startup, and successful execution of the ordinary
Core discovery entry point with no attached key.
Because discovery can continue when a transport is unavailable, the empty result does not prove
that every native transport loaded. The workflow does not exercise hardware, device events, or
applet sessions.

The host also provides an operator-driven `--monitor` mode. Its recorded evidence is macOS-only and
belongs to the research record. It is not run by continuous integration.

## Native dependencies

Core uses `Yubico.NativeShims` for PC/SC, macOS HID input, and OpenSSL-backed ARKG operations. The package contains
native binaries rather than a managed assembly, so managed reference-compatibility analysis does
not inspect it. Runtime evidence therefore matters separately:

- PC/SC paths have run under Native AOT during the macOS Management and PIV exercises. Windows and
  Linux discovery did not record which transport produced each result.
- The OpenSSL ARKG path has run under Native AOT on macOS using locally generated P-256 keypairs.
- The OpenSSL CMAC implementation is not selected by the default provider and has no recorded
  Native AOT runtime evidence.

The pinned `Yubico.NativeShims` 1.18.1 package automatically links its RID-specific static archive
when `PublishAot=true` and removes the shared shim from the publish output. Non-AOT deployments
continue to use the shared library. Static shim linking does not make the entire application
fully static: operating-system libraries such as Linux PC/SC remain runtime dependencies.

Core must guard macOS-only shim calls with `OperatingSystem.IsMacOS()` so Native AOT eliminates
them on Linux and Windows. This is an SDK responsibility, not a consumer linker configuration
requirement. The macOS native bridge remains unchanged on macOS; fake-backed connection tests
can still run on other platforms.

## Consumer deployment

Publish once for each target runtime identifier. For example:

```bash
dotnet publish MyApplication.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained \
  -p:PublishAot=true
```

Deploy the complete publish output and install the platform's required system libraries. With
NativeShims 1.18.1 and Native AOT, no separate `Yubico.NativeShims` shared library is required.
For non-AOT deployments, preserve the shared shim and the runtime's native-library resolution
layout. Copying managed build output is not a substitute for publishing.

`InvariantGlobalization` is an application choice. Enable it only if the application does not need
culture data; the SDK does not set it for consumers.

## Verification gaps

The following work would strengthen support evidence without changing the current compatibility
metadata:

- Confirm successful runs of the expanded Windows x64 and Linux x64 publish-and-run jobs.
- Run the monitoring protocol on Windows and Linux.
- Runtime-exercise representative Fido2, WebAuthn, Oath, OpenPgp, SecurityDomain, YubiOtp, and
  YubiHsm operations under Native AOT.
- Add runtime evidence for any native interop implementation that becomes reachable through a
  default production provider.
