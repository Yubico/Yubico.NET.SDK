# Native AOT support

This document defines Native AOT support for the v2 SDK libraries: `Core`, `Management`, `Piv`,
`Fido2`, `WebAuthn`, `Oath`, `OpenPgp`, `SecurityDomain`, `YubiOtp`, and `YubiHsm`. Command-line
tools, example applications, verification hosts, and test projects are not part of this support
surface.

## Support statement

The ten SDK libraries publish Native AOT compatibility metadata and enable the AOT, trimming,
single-file, and reference-compatibility analyzers. A verification host references all ten
libraries and has been published successfully for macOS Apple Silicon, Windows x64, and Linux x64.

The evidence is not equally deep for every platform or module:

- All ten libraries are analyzer-checked during normal builds.
- All ten libraries are linked into the Native AOT verification host.
- Core device discovery has run against physical YubiKeys on macOS, Windows, and Linux.
- Management device-information reads, a PIV session, and device monitoring have run against
  physical YubiKeys on macOS.
- Fido2, WebAuthn, Oath, OpenPgp, SecurityDomain, YubiOtp, and YubiHsm are link-verified but have not
  had representative protocol operations runtime-exercised under Native AOT.
- Recurring continuous-integration coverage publishes and runs the host on macOS Apple Silicon with
  no hardware attached. Windows and Linux results are recorded manual evidence, not recurring
  coverage.

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
| Windows x64 | `win-x64` | yes | yes | yes | none recorded | no |
| Linux x64 | `linux-x64` | yes | yes | yes | none recorded | no |

Only macOS has device-monitoring evidence. Windows and Linux monitoring and protocol-session paths
remain unverified under Native AOT.

A separate Linux experiment also published and ran a packaged consumer. That is package-resolution
evidence, not deeper protocol or hardware evidence.

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

1. References a public entry type from each of the ten SDK libraries so the Native AOT compiler
   processes all of them.
2. Runs `YubiKeyManager.FindAllAsync` through Core.
3. Succeeds with an empty device list when no YubiKey is attached.

The recurring workflow [`.github/workflows/native-aot.yml`](../.github/workflows/native-aot.yml)
publishes and runs this default mode on macOS Apple Silicon. It proves publish, link, process
startup, and successful execution of the ordinary Core discovery entry point with no attached key.
Because discovery can continue when a transport is unavailable, the empty result does not prove
that every native transport loaded. The workflow does not exercise hardware, device events, or
applet sessions.

The host also provides an operator-driven `--monitor` mode. Its recorded evidence is macOS-only and
belongs to the research record. It is not run by continuous integration.

## Native dependencies

Core uses `Yubico.NativeShims` for PC/SC and OpenSSL-backed ARKG operations. The package contains
native binaries rather than a managed assembly, so managed reference-compatibility analysis does
not inspect it. Runtime evidence therefore matters separately:

- PC/SC paths have run under Native AOT during the macOS Management and PIV exercises. Windows and
  Linux discovery did not record which transport produced each result.
- The OpenSSL ARKG path has run under Native AOT on macOS using locally generated P-256 keypairs.
- The OpenSSL CMAC implementation is not selected by the default provider and has no recorded
  Native AOT runtime evidence.

The stable `Yubico.NativeShims` package uses shared native libraries. A Native AOT deployment must
include the platform-specific shim next to the executable. Static-link experiments and prerelease
package candidates are research evidence, not part of the stable consumer contract.

## Consumer deployment

Publish once for each target runtime identifier. For example:

```bash
dotnet publish MyApplication.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained \
  -p:PublishAot=true
```

Deploy the complete publish output, including the `Yubico.NativeShims` native library. Run the
application from a layout that preserves the runtime's native-library resolution rules. Copying
only the executable or managed build output is not sufficient.

`InvariantGlobalization` is an application choice. Enable it only if the application does not need
culture data; the SDK does not set it for consumers.

## Verification gaps

The following work would strengthen support evidence without changing the current compatibility
metadata:

- Add recurring Windows x64 and Linux x64 publish-and-run jobs.
- Run the monitoring protocol on Windows and Linux.
- Runtime-exercise representative Fido2, WebAuthn, Oath, OpenPgp, SecurityDomain, YubiOtp, and
  YubiHsm operations under Native AOT.
- Add runtime evidence for any native interop implementation that becomes reachable through a
  default production provider.
