# CLAUDE.md - Core Module

This file provides module-specific guidance for working in **Yubico.YubiKit.Core**.
For overall repo conventions, see the repository root [CLAUDE.md](../../CLAUDE.md).

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** (e.g., new protocol handlers, connection types) should include usage examples
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below

## Module Context

Core is the **foundational library** for the entire SDK. It provides:
- **Device Management**: Discovery, monitoring, and lifecycle management
- **Connection Layer**: SmartCard (PC/SC) and HID connection abstractions
- **Protocol Layer**: ISO 7816-4 APDU processing, SCP (Secure Channel Protocol) support
- **Platform Interop**: Cross-platform native library loading (Windows, macOS, Linux)
- **Cryptography**: Concrete key identities, shared metadata and COSE registry identifiers, ASN.1 utilities
- **TLV Processing**: Tag-Length-Value parsing and construction

**Key Directories:**
```
src/
├── Abstractions/        # Public device/connection contracts; internal protocol contracts
├── Devices/             # Physical YubiKey model, discovery, monitoring, metadata
├── Sessions/            # ApplicationSession and ApplicationIds
├── Transports/          # HID and SmartCard device/connection/listener implementations
│   ├── Hid/             # HID transport, platform implementations, keyboard translation
│   └── SmartCard/       # PC/SC transport, device discovery, connection factory
├── Protocols/           # Protocol layers over transports
│   ├── SmartCard/Apdu/  # ISO 7816-4 APDU pipeline
│   ├── SmartCard/Scp/   # Secure Channel Protocol implementations
│   ├── Fido/Hid/        # FIDO/CTAP HID protocol binding
│   └── Otp/Hid/         # OTP HID protocol binding
├── Cryptography/        # Key types, COSE, ASN.1
│   └── Cose/            # COSE registry metadata; active codecs live in Ctap2
├── Native/              # Native interop per platform
│   ├── Desktop/SCard/   # PC/SC interop
│   ├── Windows/         # Windows-specific (HidD, Cfgmgr32)
│   ├── MacOS/           # macOS-specific (IOKit, CoreFoundation)
│   └── Linux/           # Linux-specific (udev, libc)
├── Credentials/         # Secure credential reading helpers
└── Utilities/           # TLV, CRC, byte, buffer utilities
```

## Logging

Core modules use `Microsoft.Extensions.Logging` via the global `YubiKitLogging.LoggerFactory`.

### Configure Logging

Configure once at application startup:

```csharp
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
YubiKitLogging.Configure(loggerFactory);
// Keep this outer scope alive through all SDK operations and resource disposal.
```

If using DI, configure logging explicitly from the DI-provided `ILoggerFactory` once during startup: `YubiKitLogging.Configure(serviceProvider.GetRequiredService<ILoggerFactory>())`.

The application owns the supplied factory. Configure before first SDK use: static logger instances
retain the factory's earlier loggers after replacement. Temporary configuration is process-global,
not async-flow or parallel-test isolation. Ordinary APDU trace templates log headers/lengths, while
SELECT logs supplied identifier bytes and exception objects carry their own text. There is no central
redactor; review payload provenance and provider destinations rather than promising global secrecy.

## Critical Patterns

### Cryptographic catalog

`CryptographicDefinitions` is the single entry point: `.Curves`, `.Keys`, `.Algorithms`. Each nested route
publishes named immutable records, an ordered immutable `All`, and frozen indexes with deliberate duplicate
identity rejection. `Get`/`GetBy` return one entry or a contextual exception, `TryGet`/`TryGetBy` annotate
nullable flow, and `Find` returns an immutable collection (possibly empty). There is no registration or DI.
Curve relationships are one-to-many: `Keys.FindByCurve` preserves catalog order; `GetByCurve` requires
exactly one construction and fails ambiguity. Ordinary EC parameter callers select `id-ecPublicKey`
explicitly through `GetNamedEcByCurve`, never an arbitrary first construction sharing the curve.
Initialization is curves → keys; algorithms reference structural constraints, never the keys initializer.

Keep concrete `KeyType`, mathematical `CurveInfo`, operation `AlgorithmInfo` and protocol numeric IDs distinct.
`KeyInfo` navigates to optional `.Curve`, `.Asn1`, `.Cose`. Classification is data-driven; `None` and invalid
key types are not asymmetric. `KeySizeBits`/`KeySizeBytes` retain nominal/stored-width meanings, not security
strength. In particular 25519 has 32 stored bytes, a 255-bit field and a 253-bit subgroup order. Scalar/seed,
coordinate and encoded point widths are separate. See README for caller examples.

`Asn1KeyInfo.AlgorithmOid` identifies a key representation: id-ecPublicKey plus named-curve parameters,
RFC 8410 X25519/Ed25519 with absent parameters, or RSA with NULL parameters. Decoders fully consume the
AlgorithmIdentifier: one curve OID for EC, no parameters for Ed/X, and absent or single NULL for RSA input
compatibility (encoding remains NULL). Check before allocating decoded private data. A shared key algorithm
OID cannot identify a curve or RSA modulus size. Only `.Curves.GetByOid` resolves named curves. Operation OIDs
belong to `AlgorithmInfo`, including the named AES/Triple DES CBC constants. `Oids` contains constants only.
Never size an ordinary EC coordinate from a key/operation OID. EC constructors, ASN encoders and decoders
all use the same short-Weierstrass/named-curve boundary. Host providers can still reject modeled curves;
private/public consistency and owned-secret cleanup must never be bypassed to make a catalog test pass.

`AlgorithmInfo` includes generic and parameterized operations; `FindForKey` means structurally applicable,
not supported by a provider, device, applet, slot, or profile, and not recommended/default. COSE ES256 does
not itself require P-256; EdDSA is Edwards, not ECDSA or intrinsically Ed25519-only. RSA-PSS parameters
include the hash, MGF1 hash, salt width and trailer. Its OID lookup deliberately returns multiple entries.
Nested record equality/hash are value-based, including algorithm applicability collections.
Within `KeyApplicability`, all fields are AND constraints; `AlgorithmInfo.Applicability` is OR alternatives.
Fully specified Ed25519/X25519 constrain concrete `KeyType`; generic families/forms stay generic.
An absent `HashAlgorithm` is not proof of a generic operation: Ed25519 has intrinsic hashing.

`CoseKeyInfo` in `Core.Cryptography.Cose` contains kty/crv, never an intrinsic alg. Only verified IANA mappings
are present (secp256k1 = 8; Brainpool P256/P384/P512 = 256/258/259). Active `Ctap2.Cose.CoseKey` remains open-valued and preserves
unknown wire metadata/opaque bytes. PIN/UV key agreement has a separate required protocol profile:
the current peer helper checks byte-array coordinates and their 32-byte lengths, forces P-256 import,
and delegates point acceptance to the provider. It does not validate the peer's required kty/crv/alg
labels or optional-label restrictions. This is a validation gap, not intended forward compatibility;
correct labels on the platform-generated key do not validate the peer. OpenPGP-specific legacy 25519 OIDs,
RSA sizes and slot choices live in OpenPgp; Core must never depend on that module.

To add a variant, append an explicit `KeyType` ID, add curve/key data and relationships, and registered COSE
metadata only when authoritative. Do not edit stable lookup/classification switches. Add independent exact
ASN/key vectors and provider-backed private consistency checks. Add protocol representation/capability support
in its owning module separately. New operation definitions change algorithm data/applicability, not key defaults.

### Applet session initialization

Applet factories delegate binding and failure cleanup to `ApplicationSession.ConstructInitializedAsync`.
The initializer calls `PrepareProtocolAsync` before selecting its applet: Core registers the protocol for
disposal, rejects SCP on non-PC/SC protocols before device I/O, and initializes a FIDO HID channel when needed.
After applet selection and firmware discovery, `ConfigureProtocolAsync` configures the protocol and establishes
optional SCP. Backends do not initialize HID channels themselves. Applet firmware gates, backend rebinding,
and Security Domain exception translation remain in the applet. Raw FIDO sessions retain lazy initialization.

Protocol configuration does not mark the session ready. `ConstructInitializedAsync` delegates to
`InitializeSessionAsync`, which publishes `IsInitialized` only after the entire applet callback succeeds.
Security Domain reset clears readiness and uses this same completion boundary for reinitialization. Pass
creation-only SCP parameters to the callback rather than retaining them on the session; Security Domain keeps
its parameters for reset. Base `Protocol` remains the disposal owner even while applet setup is incomplete.

### Built-in macOS FIDO lifetime

The built-in macOS FIDO report connection opens asynchronously through
`MacOSFidoHidConnection` and its internal `IHidInputBridge`. A persistent native IOKit
input owner copies bounded reports; blocking output and checked shutdown run on the
connection's worker. Acknowledgment and accepted-report drain precede release; a failed
close retains the owner and physical claim rather than allowing unsafe reopen. Terminal
wake and registry/discovery claim transfer belong to this same connection lifetime.
`FidoHidProtocol.Configure` performs local setup only; `ApplicationSession` awaits FIDO
channel initialization. Public lower-level `IHidConnection` compatibility remains
synchronous; the typed macOS OTP path uses a separate worker. Linux has its own FIDO/OTP
owners described below; Windows/custom HID retains the compatibility adapters. See
[retained synchronous compatibility paths](../../docs/architecture/raw-access-tiers.md#retained-synchronous-compatibility-paths)
for public entry points and known limits.

### Linux native report boundary (L1)

`ILinuxHidReportApi` is the internal, synchronous borrowed-payload
seam used by Linux FIDO and OTP report connections; it does not itself supply a reactor or lifetime
worker. Direct IO metadata/read capacity follows bounded unnumbered descriptor sizes; direct IO
and feature SET preserve caller payload length. The L3 FIDO owner enforces typed64 at Send/Receive;
the L4 typed OTP owner enforces Send8/Receive8 separately.
`LinuxHidReportApi` adds native zero report-ID framing, rejects short writes/feature transfers,
and clears owned stack/pool framing in `finally`. Feature GET retains OTP8 payload / 9 native bytes.
Its parser retains the existing YubiKey last-main-item behavior, with bounded arithmetic; it is not
a general HID parser. See [L1 source evidence and limits](../../docs/research/linux-native-report-access.md).

libc descriptors are **int**, not pointer-width handles: `open`/`eventfd` imports return `int` and
initialize a preallocated SafeHandle; fd 0 is valid. Do not use the default SafeHandle return
marshaller here (its unmanaged representation is `IntPtr`). Native calls retain managed handles.
`LinuxFileSafeHandle.CloseOnce` disposes once and observes cached release evidence: pending native
references or an unexecuted release give **no release proof**. After reference drain, successful
close or Linux EINTR/EIO proves release (with diagnostic error preserved); EBADF and unexpected
errors do not. Never retry native close. Future owners must inspect this outcome before releasing
a physical claim. L3/L4 apply this evidence in separate FIDO and OTP owners.

### Linux read-reactor infrastructure (L2)

`ILinuxHidReadReactor` supplies internal readiness registration, not a typed connection or factory
migration. `LinuxHidReadReactor.Shared` lazily runs one poller while registrations exist and exits
when idle; instance disposal permanently shuts it down. Registrations retain SafeHandle references
through snapshot/callback drain and identify callbacks by registration identity plus generation.
Only bounded nonblocking reads belong in those internal callbacks; no application callbacks or
open/write/ioctl/close may run on the poller. Consumer TCS continuations must run asynchronously.
Every published Register returns a token, with immutable Activation outcome separate from actual
Quiescence. RequestRemoval reports wake failure but the same Quiescence remains observable for late
drain; neither terminal notification nor failed shutdown acknowledges it. Only after Quiescence may
the existing native owner worker synchronously ReleaseReference, then inspect L1 CloseOnce before
claim release. There are no registration/run cleanup tasks. The poller retires its private local
eventfd after poll exits; pending admissions cannot create replacements before that retirement
finishes. No timeout or cross-thread close pretends to recover a live snapshot. See
[L2 handoff and evidence](../../docs/research/linux-read-reactor.md).

### Linux FIDO owner seam (L3)

`LinuxFidoHidConnection.OpenAsync(path, reportApi, reactor, cancellationToken, physicalClaim)`
constructs/root-pins `LinuxFidoHidNativeOwner` before native acquisition. One dedicated background
worker owns open, descriptor ioctl, output, reactor activation wait, actual quiescence wait,
reference release and checked close. One ordinary raw operation is admitted; there is no backlog,
per-operation Task.Run or worker replacement. Send copies caller memory and cannot complete while
native output is held. Pending receive cancellation detaches only that reader, not native input.
Its TCS task is the actual public task (no async wrapper), so terminal disposal cannot overtake it.
The reactor only performs bounded nonblocking reads (32 attempts, four EINTR maximum); its
160-report queue faults on overflow, clears abandoned data and requests removal to avoid spinning.

Shutdown requests removal even during held output and waits for the exact token's true Quiescence
on the original worker before ReleaseReference/CloseOnce. EINTR/EIO close diagnostics are reported
after releasing the original claim; lack of release proof quarantines it and retains the GC root.
The facade finalizer only requests shutdown and never waits. A broken wake can leave shutdown
pending indefinitely; no replacement worker or timeout claims otherwise.

Unexpected worker-loop failure closes admission under the owner lock, terminalizes the public
reader and any accepted open/send completion, clears queued output and requests reactor removal.
The still-available original worker then performs true-quiescence/checked-close cleanup. Native
release proof still releases the original claim, but the first loop error remains in the shared
disposal outcome. Interrupted/failed cleanup quarantines uncertain state without retries. Event
retirement uses the same admission lock as all setters, and the finalizer request cannot throw.

`LinuxHidIOReportConnection` is now the synchronous expert facade over this same owner. It keeps
descriptor metadata, exact actual raw input lengths, arbitrary supported SET lengths and indefinite
reads (no imported macOS timeout). EOF is deliberately terminal rather than an empty success.
Public `IHidInterface`/`IHidConnection` signatures are unchanged. L5 routes typed opens and
discovery/registry transfer to this owner (below). No shared worker framework was added.
See [L3 contract, evidence and limits](../../docs/research/linux-fido-async-lifetime.md).

### Linux OTP owner seam (L4)

`LinuxOtpHidConnection.OpenAsync(path, reportApi, cancellationToken, physicalClaim)` implements
`IOtpHidConnection` over a separate `LinuxOtpHidNativeOwner`. Rooting precedes native acquisition;
one context-suppressed dedicated worker owns open, feature GET/SET and checked close. Exactly one
ordinary raw call is admitted; there is no backlog, per-call Task.Run, replay or reactor involvement.
Send owns a copy before publication and clears it before task completion. Receive allocates eight
bytes on the worker, clears failed/partial output, and transfers successful storage to the caller.
Pre-dispatch cancellation submits nothing; after native dispatch cancellation cannot detach GET/SET.

Shutdown stops admission and shares one outcome across sync/async callers, after any admitted native
call returns. EINTR/EIO release the original claim with diagnostics; unknown release retains and
quarantines the rooted owner/claim. Unexpected worker failure stops admissions, faults actual pending
open/send/receive tasks, clears staging and attempts checked cleanup on that original worker. The
initial error survives successful cleanup. Signal retirement shares the setter lock. Finalization
only requests nonblocking, nonthrowing shutdown, even after worker failure/event retirement.

`LinuxHidFeatureReportConnection` is the synchronous expert facade over the same OTP owner:
metadata/GET remain8; direct SET retains supported caller lengths, rather than imposing typed8.
L1 still supplies native framing, exact-count checking and scoped zeroing. No factory/discovery
routing or adapter-registry rows change in L4. L5 routing is described below.
See [L4 handoff](../../docs/research/linux-otp-async-lifetime.md).

### Linux public factory and discovery routing (L5)

`HidConnectionSlot.OwnsNativeLifetime` describes built-in macOS **or Linux** HID; it is not a
platform-wide async-provider abstraction. Linux slots call `LinuxFidoHidConnection.OpenAsync` /
`LinuxOtpHidConnection.OpenAsync`, bypassing the synchronous `IHidConnection` adapters. The internal
slot accepts optional `ILinuxHidReportApi` / `ILinuxHidReadReactor` seams, alongside its existing macOS
seams; `LinuxHidInterface` has an internal descriptor constructor for no-hardware route tests.
Direct `IHidInterface.ConnectTo*Reports` methods remain synchronous expert facades.

`YubiKeyDevice` claims every known physical member before open, then transfers that exact registration
to the native owner instead of wrapping it. The requested transport must still match the selected slot.
`SharedDeviceInfoRead` resolves the same owned slot for direct and published-composite providers and
transfers the per-interface discovery lease before invoking open (even if it later throws). Once
transferred, only the native owner releases it: neither a caller timeout nor a disposal exception is
release proof. EINTR/EIO report disposal diagnostics **after** releasing the original claim; unknown
release retains/quarantines it. Windows/custom adapters and PC/SC paths keep their prior behavior.
`DiscoveryDeviceInfoReader` bounds the caller's wait; `SharedDeviceInfoRead` owns admission, the lease,
worker, connection, and read pipeline. `ProtocolDeviceInfo.ReadAsync` only borrows an already-open
connection and owns the matching protocol, not discovery scheduling or connection teardown.

`LinuxHidRoutingTests` exercises real public typed connection and raw-session factories, grouped claims,
held open/report/close, canceled/failed opens, unknown release, late discovery cleanup, replacement evidence,
and borrowed idle/active session reuse under fake native seams. Preserve worker priority, evidence epochs,
one-connection/one-session admission, and no transport fallback. L6a adds managed public applet journeys
and bounded registry links (below); hardware/native acceptance remains L6b work. See
[L5 routing handoff](../../docs/research/linux-hid-routing.md).

### Bounded managed Linux acceptance (L6a)

`AdapterContractRegistry` independently requires 43 operations / 76 runnable profile links, including 16
Linux typed/direct operations / 27 links; `PublicSyncBoundaryRegistry` independently requires 23 public
reachability entries, including 8 retained Linux synchronous paths. These are links, not native attestation.
`LinuxDirectFacadeAcceptanceTests` checks sync facade failure, held output, true-quiescence and close drain.
Applet tests `Ctap2SessionLinuxCrossLayerTests` and `YubiOtpSessionLinuxCrossLayerTests` use actual
`LinuxHidInterface`/`HidConnectionSlot`/`YubiKeyDevice` and the production owners, faking only report/reactor
seams. They preserve borrowed idle reuse, cancellation recovery, terminal presence ownership, and an
owned pending CTAP exchange's claim through drain. OTP Configure is unchanged: public initialization
awaits status first. `PcscConnectionLifetimeTests.LiveRegisteredConnection_*` holds disconnect and context
release independently and isolates rooted live release-failure/quarantine probes using `PcscIsolatedProbe`.
L6a changed no shipping source sites; its historical 219-site snapshot remains documented/outstanding.
The current merged inventory incorporates upstream discovery/monitor changes; see the boundary-inventory README. See
[managed evidence and remaining L6b scope](../../docs/research/linux-managed-acceptance.md).

### Listener and Native Retry Loops

Background listeners and native/resource-manager retry loops must block, back off, exit, or throttle on every failure path. Do not ignore native return values inside loops unless another call in the same path provides a bounded wait. Persistent failures such as stale PC/SC handles must have no-hardware fault-injection tests that prove call cadence is backoff-bounded.

If a change touches Core runtime loops, polling paths, recovery logic, or listener lifecycle cleanup, run `dotnet toolchain.cs -- resilience --fast` in addition to the normal focused tests. Prefer adding or extending no-hardware `Category=RuntimeResilience` coverage before considering live diagnostics.

### User-presence notification ownership

SDK-created `UserPresenceContext` values set `Operation` from the applet's nested group in
`UserPresenceOperations` (for example `UserPresenceOperations.Piv.Decrypt`) and `Application` from
`UserPresenceApplications`. Operation values have the form `Group.Method` and must stay unique across
groups; a Core unit test enforces this. When a new applet method notifies presence, add a constant to its group.
`UserPresenceNotification.Create` is the only place that captures the ambient `UserPresenceIntent` into
`UserPresenceContext.Intent`. It runs in the caller's async flow at the start of the operation, so keep every
notification created through it, and never create one from a background listener or with suppressed
`ExecutionContext` flow.

`ApplicationSession` creates one non-null `UserPresenceNotification` for each operation. Pass that
same handle down every participating layer; use `UserPresenceNotification.None` for deliberately
silent raw, management, and device-information paths. A transport may request the handle when it
observes a live wait, but the layer that can classify the complete operation outcome owns terminal
resolution. In particular, Core FIDO and OTP HID protocols request only; their applet backends
resolve after CTAP status or CRC validation. Policy-driven SmartCard operations normally request
and resolve at the applet operation boundary. Do not pass nullable prompt/context pairs through
downstream APIs or create competing lifecycle owners.

### APDU Processing Pipeline

The APDU processing pipeline composes transmit and response processors. Short mode splits data into
255-byte chunks; extended mode uses `ApduTransmitter` with the configured complete-command ceiling.
The current formatters append Le, including zero; zero is not an absent-Le representation. Response
continuations use the configured remaining instruction and fixed zero CLA/P1/P2/Le. There is no
automatic `6Cxx` corrected-length retry.

```
ApduCommand
    ↓
[ChainedApduTransmitter]    ← Splits large commands into chained APDUs
    ↓
[ApduFormatterShort/Extended]  ← Formats for wire protocol
    ↓
ISmartCardConnection.TransmitAndReceiveAsync()
    ↓
[ChainedResponseReceiver]   ← Reassembles chained responses
    ↓
ApduResponse
```

**Key classes:**
- `PcscProtocol` - Main protocol implementation (`Protocols/SmartCard/Apdu/PcscProtocol.cs`)
- `ApduCommand` / `ApduResponse` - APDU representations
- `IApduProcessor` - Pipeline element interface
- `ChainedApduTransmitter` / `ChainedResponseReceiver` - Chaining handlers

**Configuration:**
```csharp
// Protocol auto-configures based on firmware version
protocol.Configure(firmwareVersion);

// Force short APDUs (for compatibility testing)
protocol.Configure(firmwareVersion, new ProtocolConfiguration { ForceShortApdus = true });
```

### Secure Channel Protocol (SCP)

Core owns SCP key-parameter types, handshake processing, and the secure PC/SC protocol decorator.
Applet session factories accept the key parameters and establish the channel through the internal
`PcscProtocol.InitializeScpAsync` capability; consumers cannot construct the decorator directly.

Parameters retain contained owners. `StaticKeys` clones its source spans; `Scp03KeyParameters` retains
and disposes that exact object. `Scp11KeyParameters` copies the certificate collection but retains its
key/certificate objects, disposing the OCE private key and certificates while leaving the SD public
wrapper intact. Record `with` copies share these owners. Keep parameters alive through initialization
and any reset reuse, and clear source buffers independently. See the complete scoped
[SCP example](README.md#use-a-secure-channel).

The implementation uses a fixed P-256/AES-128 SCP11 profile. Constructor admission alone establishes
neither a complete profile/trust check nor applet authorization. `IsAuthenticated` reflects protocol
setup; SCP11b is card-to-host authentication, and a/c setup completion is distinct from the device's
first protected-command phase. Keep cited editions exact: SCP03 1.1.2 Public Release is distinct from
1.1.2.6 Public Review, and the inspected SCP11 1.3.0.13 Public Review is distinct from final 1.4.
Synthetic wire tests do not close empty-command, protected-error/continuation, final-edition, or vendor
profile qualification. Raw SELECT after SCP remains plain while retaining the graph; use applet
factories for selection before channel setup, not raw reselect as recovery.

**Key files:**
- `Protocols/SmartCard/Scp/` - SCP implementations
- `Protocols/SmartCard/Scp/SessionKeys.cs` - Derived session keys
- `Protocols/SmartCard/Scp/ScpKid.cs` - Key identifiers

### TLV Processing

`ApduResponse` owns a private copied byte array in a sealed reference type. Consumers clear sensitive
response bytes through `response.Clear()` after parsing, never by casting a read-only view writable.
Clear invalidates `Data`/`RawData` aliases and status bytes, so inspect status first. Caller constructor
buffers are not owned by the response and remain unchanged. Do not reintroduce a struct owning cloned
sensitive arrays: copied structs cannot provide a single clear/lifetime owner.

The canonical usage examples for `DecodeList`, `TryFindValue`, `Tlv`, `EncodeList`, nested encoding, and
`EncodeAndDisposeList` are in [docs/usage/tlv-processing.md](../../docs/usage/tlv-processing.md). Keep the full examples there rather
than duplicating them in this contributor guide.

Contributor rules:
- Dispose every `Tlv` and `DisposableTlvList`. `EncodeAndDisposeList` disposes the `Tlv` inputs passed to that
  call; `EncodeList` does not.
- Neither encoding method clears caller-owned source buffers. `EncodeAndDisposeList` also does not clear its
  returned encoding or a returned encoding used as an intermediate value in nested TLV construction.
- Classify buffers by semantics. For sensitive nested data, clear source buffers plus returned and intermediate
  encodings in `finally`; public or nonsecret encodings do not require special clearing.

### Shared Base32 decoder

`Utilities.Base32` supplies the two public decode overloads used by OATH and CLI callers; keep it a small
codec, not an encoding framework. It accepts ASCII case, literal spaces, empty input, absent or complete
trailing padding, and permissive unused pad bits. Reject invalid symbols, impossible terminal symbol counts,
other whitespace, and incomplete/embedded padding. OATH, not the generic codec, requires nonempty material.

The array overload returns independent caller-owned bytes. The span overload borrows text and output;
`MemoryMarshal.AsBytes(encoded).Overlaps(destination)` rejects any byte-range overlap before writing or
entering failure cleanup. The BCL's checked byte-length conversion can throw `OverflowException` at the same
admission boundary. Either admission failure leaves both spans unchanged. On disjoint admitted storage,
decoding failure clears the entire destination; success preserves the unused tail. Callers clear successful
secret output and retain responsibility for encoded source storage. Do not clear borrowed source text.

### Platform Interop Pattern

Native methods are isolated in `Native/`:

```csharp
// Platform detection
if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
{
    // Windows-specific code
}

// Internal contributor fragment: load through the platform-specific library owner.
using var library = UnmanagedDynamicLibrary.Open(libraryPath);
// Resolve and finish all calls through its delegates before this scope ends.
```

#### `LibraryImport`, not `DllImport`

**`DllImport` is wrong for new code.** Every native entry point added to `Native/` must be declared
with `[LibraryImport]` on an `internal static partial` method inside a `partial` class.

```csharp
// ❌ WRONG — runtime-generated IL stub
[DllImport(Libraries.NativeShims, CharSet = CharSet.Ansi, EntryPoint = "Native_Foo", SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
internal static extern int Foo(string name, bool flag);

// ✅ RIGHT — source-generated, compile-time-visible marshalling
[LibraryImport(Libraries.NativeShims, EntryPoint = "Native_Foo",
    StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
internal static partial int Foo(string name, [MarshalAs(UnmanagedType.U1)] bool flag);
```

Why it matters here, beyond style:

Generated marshalling still depends on correct native declarations. Match the exact backend header,
architecture, pointer carrier, and Boolean width; source generation cannot infer the intended ABI.
Keep Windows LPGUID/PULONG/BOOLEAN issues and Linux LP64 PC/SC PCI qualification separate from the
choice of import mechanism. A successful build establishes neither installed-producer identity nor
successful native execution.

- The marshalling stub is **generated C# you can read and step through**, not IL synthesised by the
  runtime. Native AOT and trimming see real code — `docs/NATIVE-AOT.md` support depends on it.
- The generator **fails the build on signatures it cannot marshal**. `DllImport` accepts the same
  signature silently and corrupts memory at runtime instead. Interop bugs in HID/PC/SC paths are the
  most expensive class of bug in this module; move them to compile time.
- Marshalling is explicit at the call site, so reviewers can see the ABI rather than infer it from
  attribute defaults.

Conversion traps, in the order they bite:

| Trap | What to do |
|---|---|
| `extern` → `partial` | The method **and** its enclosing class both need `partial`. `NativeMethods` classes in `Native/` are already declared that way where converted. |
| `CharSet = CharSet.Ansi` | There is no direct equivalent. On Unix it meant UTF-8 → use `StringMarshalling = StringMarshalling.Utf8`. On Windows it meant the ANSI code page → marshal `byte*`/`ReadOnlySpan<byte>` yourself rather than pretending it is UTF-8. `CharSet.Unicode` → `StringMarshalling.Utf16`. |
| `bool` parameters/returns | Not blittable. Annotate `[MarshalAs(UnmanagedType.U1)]` (or `.I4` — match the C header), or change the signature to the native integer type. |
| Delegate callbacks | Not marshalled at all by the generator. Use `delegate* unmanaged[Cdecl]<...>` with an `[UnmanagedCallersOnly]` target, as `Native/MacOS/HidInput/HidInput.Interop.cs` does. A function pointer also avoids the per-instance thunk and the "keep the delegate alive" `GCHandle` dance. |
| `string` return values | Needs an explicit marshaller (`Utf8StringMarshaller`) or return `nint` and convert. Silent ownership bugs live here — say who frees the buffer in a comment. |
| `SafeHandle` | Supported for pointer-width native handles, including returns such as `LinuxUdevSafeHandle`. Its default unmanaged representation is `IntPtr`, **not** an `int` file descriptor. For libc `open`/`eventfd`, return native `int` into a preallocated managed SafeHandle, retaining SafeHandle ownership and references on caller APIs. |
| `SetLastError = true` | Keep it only where a caller actually reads `errno`/`GetLastError`. It is not free. |
| Array parameters | Delete `[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = n)]` outright. Do **not** replace it with `[MarshalUsing(CountElementName = ...)]` — leave the parameter as a bare `byte[]`/`IntPtr[]`. See the note below on why that is safe, not merely tidier. |

**Why bare arrays are safe, not just tidier.** A by-value array of a blittable element type is
*pinned* by the generated stub, not allocated-and-copied:

```csharp
// from the generated stub, with or without [MarshalUsing]
fixed (void* __output_native = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(output))
```

Native writes therefore land in the caller's own buffer, exactly as they did under `[DllImport]`.
This is load-bearing for native-written output buffers — `CmacEvpMacFinal`'s `output` and
`BnBigNumToBinaryWithPadding`'s `buffer` are read back by their wrappers after the call. Had the
generator copied in-only, every CMAC result and every EC coordinate would have come back silently
zeroed, with a clean build and no warning. Count metadata is only required for arrays marshalled
back **out** of unmanaged memory, which this repo has none of. Verify with
`EmitCompilerGeneratedFiles` rather than reasoning about it, as was done here.

Current state: **every** P/Invoke in the repository is `[LibraryImport]`. There is no remaining
`[DllImport]` or `static extern` anywhere in `src/`, `benchmarks/`, or `verification/`. Treat any
reappearance as a regression, not as legacy debt:

```bash
grep -rn "\[DllImport(\|static extern" --include="*.cs" src/ benchmarks/ verification/   # must be empty
```

The macOS listener passes static `[UnmanagedCallersOnly]` Cdecl addresses through the `nint` callback
parameters of the IOKit registrations. Its nonzero context identifies a rooted listener generation;
callbacks recover that generation through `GCHandle`, not retained per-instance delegate fields.
Generation cleanup requires actual run-loop/callback exit. A timed-out stop retains its generation;
partial startup without a worker has separate unresolved cleanup paths. Keep callback acknowledgment,
root lifetime, and native release distinct when changing this seam.

The `SYSLIB1054` analyzer that suggests this conversion is informational and is **not** configured as
an error in `.editorconfig`. A clean build does not mean you used `LibraryImport`.

### Connection Factory Pattern

Built-in PC/SC creation defaults to strict `SmartCardSharing.Exclusive`: sharing violations throw
`ConnectionInUseException` without a shared retry. Keep the `UsbSmartCardConnection`,
`PcscConnectionNativeState`, and `PcscNativeResources` constructor defaults consistent with the initial
`SmartCardConnectionFactory.DefaultSharing`. Parameterless factories (including `CreateDefault` behind
`FindYubiKeys.Create` / `YubiKeyManager`) snapshot the process-wide policy at construction; changing it
does not reconfigure existing factories. Migration/coexistence requires an explicit `Shared` or
`ExclusiveWithSharedFallback` selection at startup before discovery/monitoring, or on one factory.
Preserve `SCARD_UNPOWER_CARD` on release in every mode. `SmartCardSharingTests` covers selection,
contention/no-retry, opt-ins, policy snapshots, and release disposition without hardware; it does not
prove physical card isolation. See [Core usage and migration](README.md#pcsc-sharing-policy-and-migration).

Connections are created via factories:

```csharp
// SmartCard connection
var connectionFactory = new SmartCardConnectionFactory();
await using (var connection = await connectionFactory.CreateAsync(reader, cancellationToken))
{
    // Finish all borrowed SmartCard work before leaving this scope.
}

// Typed HID connection, after the SmartCard scope ends.
await using (var connection = await yubiKey.ConnectAsync<IFidoHidConnection>(cancellationToken))
{
    // Finish all borrowed FIDO work before leaving this scope.
}
```

This fragment assumes an enumerated `IPcscDevice reader` and `IYubiKey yubiKey`. Direct SmartCard
factory opens bypass grouped registration: coordinate discovery/monitoring and other connections for
that entire scope. Prefer typed `IYubiKey.ConnectAsync<TConnection>` for the managed member guard.

### Physical Device Model

`IYubiKey` represents **one physical YubiKey**, not a single transport handle. A composite USB key exposes
several interfaces at once (CCID, HID FIDO, HID OTP), and discovery returns one `IYubiKey` for it with those
interfaces in `AvailableConnections`. Use `SupportsConnection(...)` and the typed `ConnectAsync<TConnection>()`
to select an interface; the parameterless `ConnectAsync()` throws on a multi-interface device. One grouped
physical key admits one live connection across all known interfaces. Applet session extensions choose exactly
one transport via a documented default order plus an optional `SessionCreationOptions.PreferredConnectionType` override; connection or
initialization failure does not try another interface. Read-only metadata types (`DeviceInfo`, `FormFactor`,
`DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, `VersionQualifierType`) are Core-owned
(`Yubico.YubiKit.Core.Devices`); mutating operations stay in Management. Full reference:
[Physical Device Model](../../docs/architecture/physical-device-model.md). Read
[Device Discovery Guarantees](../../docs/architecture/device-discovery-guarantees.md) before changing
`CompositeDeviceMerger`, `FindYubiKeys`, or the topology resolver. It distinguishes managed evidence
rules, complementary-partial cross-key grouping, implementation-only topology choices, and actual
native/physical qualification. Repeated scans have no expiry that forces ambiguous grouping to heal.

The change feed retains a published object only while its interface and connection sets remain unchanged and
fresh known serial evidence does not contradict it. A different known serial on the same interface set proves
substitution and emits `DeviceRemoved` for the predecessor followed by `DeviceAdded` for a new successor object. Unknown
serials never trigger identity-based republication. If no listener event invalidates discovery caches and no scan
observes absence, same-interface substitution remains unknowable from cached evidence.

PID-completeness grouping also has a complementary-partials bound: two same-PID keys can contribute
different slots to one composite. Path-bound opening does not verify each slot against that composite's
serial. Do not equate the grouping heuristic with physical identity attestation.

`IYubiKey.CachedDeviceInfo` exposes the existing nullable Core `DeviceInfo` snapshot without I/O; its default
interface implementation returns `null` for custom implementations. `YubiKeyDevice` delegates directly to
`DeviceInfo`, whose getter reads the atomically published `DeviceInfoSnapshot` reference. Do not introduce a
second cache or infer freshness from this property. Discovery/change-feed updates replace the
snapshot and emit `DeviceUpdated` when metadata differs; null updates preserve the last successful value. Retained objects can
expose stale metadata after configuration changes or disconnect. `ManagementSession.GetDeviceInfoAsync` is
the live-read API and does not promise to update discovery metadata. The independent `SerialNumber` latch
and identity contract remain unchanged, even if a newer metadata snapshot omits its serial. No-hardware
coverage is in `YubiKeyDeviceTests` (no connection on access, snapshot replacement, default implementations,
serial independence) and `SnapshotDiffTests` (retention and replacement isolation).

Snapshot replacement preserves previously returned scalar fields, but `ChallengeResponseTimeout` currently
aliases an input TLV's memory. Read-only access is not independent ownership or post-clear stability.
Successful serial-less metadata can remain cached; unchanged scans need not retry it. Identity-cache
PID checks do not extend to the separate metadata cache's member-ID keyed entries.

Use `YubiKeyManager.FindFirstAsync(predicate, interfaces, cancellationToken)` when absence is exceptional and
`FindFirstOrDefaultAsync` when it is not. They select after one independent fresh `FindAllAsync` operation;
they do not wait for insertion, and callers must not treat discovery order as stable. Use the named
`cancellationToken:` argument; there are no token-only forwarding overloads.

`YubiKeyManager.FindAllAsync(interfaces, cancellationToken)` is always one-shot, not a monitor rescan.
It constructs `FindYubiKeys.CreateForInterfaces` per call with an immutable concrete scope; no manager/cache,
feed publication, watcher events, or monitor lifecycle participates. Keep `FindYubiKeys.Create()`
all-interface and `IFindYubiKeys.FindAllAsync(type, ...)` a post-merge result filter. Never reuse a finder
across different scopes: its cache eviction treats unobserved members as absent. Excluded backend
factories are not called. Filter HID by concrete type before slot creation/topology/protocol reads;
OTP and FIDO necessarily share OS descriptor enumeration. Ignore undefined scope bits like `Matches`.
Do not reinterpret partial scope as PID completeness or import sibling slots from prior scans.

One-shot exit terminally retires its read scope in `finally`, including cancellation and budget expiry.
This cancels queued identity admission and prevents later opens; already-open native work keeps its
worker and registry lease until teardown. Do not dispose admission/lease owners at the wait boundary.
Transport enumeration waits run concurrently with the shared `DiscoveryBudgets.Enumeration` budget
(five seconds). For one-shot discovery a timeout logs a warning and omits that transport for
this call; for the monitor's `IMonitoredDiscovery.FindAllAsync` it throws so a partial scan is never
published. Caller cancellation abandons the wait immediately. Abandoned enumeration completes independently and its result
or failure is observed at Debug level. The absence of post-cancellation slots/probes is pinned by
`FindAllAsync_CancellationDuringEnumeration_AbandonsEnumerationAndSkipsSlotsAndProbes`;
`FindYubiKeysEnumerationBudgetTests` pins timeout isolation and late completion for both transports.
No global registry identity aliases are added: all/scoped views exclude connections only when their
known member-ID sets overlap; disjoint CCID/FIDO views remain uncoordinated even with matching serials.
`FindYubiKeysScopeTests` pins filtering, partial same-PID grouping, evidence isolation, retirement, and
the ownership bound; `YubiKeyManagerStaticTests` pins no initialization/publication/monitor interference.
`FindAllAsync_ReturnedViewCanConnectSequentiallyAfterDiscoveryScopeRetires` additionally pins that retiring
one-shot discovery does not retire the returned view's public connection slots. The public hardware
companion is `ScopedDiscoveryReadOnlyTests` in Management's integration project (the appropriate place
for its live-read dependency), not Core's unit tests. It uses all attached authorized serial-bearing USB
fixtures, permits conservative fragments, and compares metadata over sequential concrete connections.
Keep native disjoint-view races out of hardware coverage; use the deterministic ownership vectors above.
Fixture requirements and the explicit read-only filter are in
[device discovery testing](../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks).

Pre-merge candidates are raw internal `PcscConnectionSlot`/`HidConnectionSlot` instances over live enumerated PC/SC or HID
handles; they are not `IYubiKey` objects. Only `YubiKeyDevice` is published by production discovery.

`IYubiKey` models the physical key; `IHidInterface` models one operating-system-exposed HID report
interface discovered by `FindHidInterfaces`. `IHidConnection` is an opened report connection, while
`IWindowsHidReportAccess` is the internal Windows report-handle seam. These names do not imply a
one-to-one mapping between operating-system interfaces and physical USB interfaces.

### ConnectionType Semantics

`ConnectionType` is a `[Flags]` enum with explicit values. `HidFido`, `HidOtp`, and `SmartCard` represent concrete discovered device interfaces. `Hid` is a group filter that includes both HID FIDO and HID OTP interfaces when used with discovery/cache filtering APIs. `Unknown` matches no devices.

### Device change delivery

manager watch creates a producer; monitor watch subscribes to an existing one.

`YubiKeyManager.WatchAsync(ct)` lazily creates a private monitor per enumeration and disposes it on exit.

`YubiKeyManager.CreateMonitorAsync(ct)` creates a new initialized `YubiKeyMonitor`, the sole
composition/teardown owner of its change feed, listeners, and sequential scan loop. There is no global manager/cache,
static Start/Stop/Shutdown, `ScanAsync`, or `forceRescan` alias. Short commands and most applet tests
use one-shot discovery; tracking consumers explicitly own/share a monitor. Old result filters must be
applied locally after All discovery where callers need full composite slots. The factory starts listeners
first and awaits the loop's own initial scan-and-publication signal, not a separate manual rescan.
Empty success is ready; initial scan or
publication failure is propagated and disposes partial ownership. Creation cancellation cancels the wait
and triggers cleanup; the monitor's lifetime token is independent of the creation token after return.
Each monitor is initialized once, runs once, and is disposed once by its public owner. There are no manual
rescans, stop/start modes, interval replacement, restarts, or monitor generations. Initial, listener-driven,
and fallback scans all await full feed publication inside one sequential loop. No scan semaphore or
publication gate is needed because no second producer exists. The production fallback interval
remains five seconds. Internal `RescanScheduler` owns initial, interval, and coalesced hotplug triggers
on `TimeProvider`; its 200 ms quiet window and one-second cap are tested with `FakeTimeProvider`.

`DeviceChangeFeed` holds one snapshot dictionary, a scan revision, and copy-on-write subscriptions.
Only the sequential producer calls `Apply`. `SnapshotDiff` computes a successor map outside the feed's
gate; published dictionaries are never mutated. It groups removals before additions before metadata
updates, retaining the original device unless connection flags or contradicting known serials prove
replacement. `DeviceInfo` has value equality, so equal metadata produces no update notification.

`YubiKeyMonitor.WatchAsync` delegates directly to `DeviceChangeFeed.Subscribe`. On first `MoveNextAsync`,
snapshot capture and subscription registration share the publication gate. Replay happens outside the
gate as `DeviceAdded` records followed by one `SnapshotComplete`, even for empty membership. Later
changes stream from that subscription's bounded channel. Revision increments once per applied scan;
changes sharing a revision came from one scan. Maintain a consumer
projection from the stream or use independent `FindAllAsync` discovery.

No consumer code or logging runs under the publication gate. Each subscription has a 256-change buffer
with asynchronous continuations; `TryWrite` overflow faults only that subscriber rather than dropping
changes. Recovery clears the consumer projection and re-enumerates the same monitor. Cancellation and
disposal of an enumeration unregister only that subscription. Replay does not occupy buffer capacity.

`Fault` records the first terminal error and faults current and later subscriptions with a wrapped
`InvalidOperationException`. `Complete` is idempotent and ends healthy subscribers normally; later
subscriptions end empty. Already captured replay and queued changes may drain after termination.
`Apply` after either terminal state throws. The owner must stop the producer before completing the feed.
Disposal stops listeners, cancels and awaits the loop without a drain timeout, retires discovery, then
completes the feed; native discovery budgets bound waits rather than abandoning the managed loop.
`SnapshotDiffTests`, `DeviceChangeFeedTests`, and monitor tests pin these contracts without hardware.

### Listener Event Semantics

Transport listeners implement `IDeviceChangeListener` with payload-less `Changed` callbacks. Set the
callback before `Start()` and never stop or dispose inside it. HID hint details stay internal to the
listener's Trace logging; callbacks are diagnostic rescan hints, never physical-device truth.
`YubiKeyMonitor.WatchAsync` remains snapshot-diffed output after a rescan. Unknown HID removals still
trigger a rescan rather than being suppressed: they may be the only native signal for a device diff.

`HotplugSignals` owns listener creation and teardown and carries transport hints in a capacity-one
drop-write channel into the monitor's single-reader loop. Each listener starts independently. A listener
that throws or reports a post-`Start()` status other than `Started` is logged, stopped/disposed, and skipped,
without aborting the other listener or loop. With no listeners the loop relies on its interval fallback.
These are optional latency accelerators; discovery plus snapshot diffs supply device truth.

A simply unavailable transport service (no PC/SC service/library/readers) can enumerate empty while other
transports are scanned. This is best-effort partial discovery, not proof that all OS interfaces were
accessible. If PC/SC enumeration itself throws (worker saturation or `SCardGetStatusChange` error), the
scan fails rather than committing false-empty removals. In-scope PC/SC and HID enumeration waits start
together with a five-second budget. One-shot discovery logs an enumeration timeout and omits that
transport for that call. Monitored discovery instead throws rather than publishing a timeout-induced
partial snapshot: initial creation propagates the failure and tears down partial ownership; later scans
retain the last publication and retry on the next trigger.

Creation tolerates listener unavailability but the public factory still requires the first
successful scan/publication. Hints during initialization remain pending for an ordered follow-up scan.
`RescanScheduler` consumes transport hints and combines them until the quiet window or coalesce cap.
The monitor globally invalidates finder evidence once, with the combined transport flags, immediately
before each hotplug-triggered scan. Discovery serializes evidence invalidation and retirement internally.
Disposing `HotplugSignals` clears all callbacks, stops all listeners, disposes all listeners, then completes
its channel. Read-scope retirement cancels internal queued admission waits without releasing native leases
early. The scheduler checks elapsed time after each occurrence so refilled signals cannot starve the cap.

## Session Base Class

### FIDO application and CTAP vocabulary

CTAP2 operation semantics live in `Yubico.YubiKit.Ctap2`; Core's FIDO HID classes, interfaces,
namespace, and `ConnectionType.HidFido` represent the shared physical transport. Internal
`IFidoHidProtocol.SendCommandAsync` accepts a CTAPHID command, not a CTAP2 operation code.
`RawCtapHidSession` replaces `RawFidoHidSession` and preserves application-agnostic CTAPHID framing.
`ApplicationIds.Fido` replaces `ApplicationIds.Fido2` and selects the shared CTAP1/U2F and CTAP2
applet; `FidoU2f` remains an explicitly same-AID interoperability alias. Keep the FIDO2 app identity
in `DeviceCapabilities.Fido2` (`0x200`) and `UserPresenceApplications.Fido2` (`"FIDO2"`), but use
`UserPresenceOperations.Ctap2` (`"Ctap2.Method"`) for CTAP2 operation contexts.

`ApplicationSession` centralizes shared session state:
- `FirmwareVersion`
- `ConnectionType`
- `IsInitialized`
- `IsAuthenticated`
- `Protocol` lifetime/disposal
- One-live-session attachment to the borrowed `Connection`

`ApplicationSession` does not dispose a caller-created connection. Only internal convenience entry
points that created a hidden connection call `OwnConnection()`. Whoever creates a connection must use
`await using` to dispose it; otherwise the physical-device lease can remain held for the
connection/process lifetime and block later opens. Sessions have no finalizer recovery; a built-in
connection's best-effort native finalizer cleanup is not a lifetime guarantee. Dispose one session
before creating the next session over the same connection; sequential reuse is supported.

`IsInitialized` and `IsAuthenticated` become `false` when sync or async disposal is admitted, not
only after teardown completes. `IsAuthenticated` describes application-protocol authentication (for
example, SCP); applet-specific authentication is exposed by the concrete applet session.

`IApplicationSession.IsSupported(feature)` is a public known-firmware compatibility query, not a runtime
success guarantee. `ApplicationSession.EnsureSupports(feature)` is protected enforcement machinery for
derived session operations and is not on the public interface. Operations enforce their own prerequisites;
consumers do not need to preflight them. Major-zero development versions retain permissive feature checks.

Applet factories snapshot `SessionCreationOptions` without retaining the options object. Secure-channel parameters
remain borrowed. A session retains the caller-owned `UserPresencePrompt` service reference for its lifetime but
never disposes it. Actual applet adoption still matters: Security Domain retains SCP parameters for
reset; Management and Security Domain do not retain their unused presence prompt. Do not infer the same retained state from the
shared options shape for every applet.

## Test Infrastructure

### Unit Test Structure

```
tests/
├── Yubico.YubiKit.Core.UnitTests/
│   ├── Devices/              # YubiKey model, discovery, metadata tests
│   ├── Protocols/
│   │   ├── SmartCard/Apdu/   # APDU protocol tests and fakes
│   │   ├── SmartCard/Scp/    # SCP protocol tests
│   │   └── Otp/Hid/          # OTP HID protocol tests
│   ├── Transports/           # HID and SmartCard transport tests
│   ├── Cryptography/
│   ├── Credentials/
│   └── Utilities/            # TLV, utility tests
└── Yubico.YubiKit.Core.IntegrationTests/
    ├── Devices/              # YubiKeyManager, device tests
    └── Transports/           # HID and SmartCard integration tests
```

### Faking Connections

Use `FakeSmartCardConnection` for unit tests:

```csharp
using var fakeConnection = new FakeSmartCardConnection();

// Queue expected responses
fakeConnection.EnqueueResponse(new byte[] { 0x90, 0x00 }); // Success

// Create protocol with fake
using var protocol = new PcscProtocol(fakeConnection);

// Test
var result = await protocol.SelectAsync(ApplicationIds.Piv, CancellationToken.None);

// Verify commands sent
Assert.Single(fakeConnection.TransmittedCommands);
```

### Discovery hardware contracts

Management's `OwnedMonitorReadOnlyTests` uses Core's public factory and stream with at least two
attached allow-listed serial-bearing USB fixtures. It reads initial replay through `SnapshotComplete`
after return, without polling away partial discovery, then checks object retention, subscriber
cancellation, disposal/recreate, convenience selection/no-match, and sequential live metadata comparisons
only after monitor disposal. `ScopedDiscoveryReadOnlyTests` covers concrete/combined scopes independently;
its complete serial-less-bystander readiness behavior must remain intact. No prompts, UV/UP, hotplug,
resets, or writes are in this unattended filter. Required attended removal/reinsertion/event-lifetime acceptance remains separate and pending until
the user is present. See the [explicit test filters](../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks).

### Integration Test Base

Integration tests inherit from `IntegrationTestBase`:

```csharp
public class MyTests : IntegrationTestBase
{
    [SkippableTheory]
    [WithYubiKey]
    public async Task MyTest_DoesX_Succeeds(YubiKeyTestState state)
    {
        // The placeholder row binds lazily to its first matching device.
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();
        // Test logic
    }
}
```

## Common Operations

### Creating a Raw Session

```csharp
// From a caller-owned connection
await using var connection = await connectionFactory.CreateAsync(reader, ct);
await using RawSmartCardSession raw = await RawSmartCardSession.CreateAsync(connection, ct);

// Configure for firmware
raw.Configure(firmwareVersion);
await raw.SelectAsync(ApplicationIds.Piv, ct);
```

### Sending APDUs

```csharp
// Non-sensitive command — no zeroing needed
var command = new ApduCommand
{
    Cla = 0x00,
    Ins = 0xA4,
    P1 = 0x04,
    P2 = 0x00,
    Data = applicationId
};
var responseData = await raw.TransmitAndReceiveAsync(command, cancellationToken: ct);

// Separate sensitive-command fragment: caller clears the source even on failure.
// ApduCommand is a readonly record struct: it stores a reference, not a clone.
try
{
    var verify = new ApduCommand(0x00, InsVerify, 0x00, 0x80, pinnedPin.AsMemory(0, 8));
    var response = await raw.TransmitAndReceiveAsync(verify, throwOnError: false, cancellationToken: ct);
    try { Console.WriteLine($"SW={response.SW:X4}"); }
    finally { response.Clear(); }
}
finally
{
    CryptographicOperations.ZeroMemory(pinnedPin);
}
```

### Error Handling

```csharp
try
{
    var response = await raw.TransmitAndReceiveAsync(command, cancellationToken: ct);
}
catch (ApduException ex) when (ex.SW == 0x6982)
{
    // Security status not satisfied - need to authenticate
}
catch (ApduException ex) when (ex.SW == 0x6A82)
{
    // Application/file not found
}
```

## Firmware Version Considerations

`PcscProtocol.Configure` combines firmware policy, the connection's extended-APDU support, and
`ProtocolConfiguration.ForceShortApdus`. It uses complete-command constants `Neo`, `Yk4`, and `Yk43`,
not a 254/2048-byte data rule. Session feature queries describe known-version compatibility; enabled
capabilities, authorization, provisioning, and actual command success remain applet-specific.

Major-zero versions, including `0.0.0`, pass optimistic feature checks. Raw HID sessions currently
retain inherited `0.0.0` even after protocol discovery. A detected, assumed, or overridden effective
version is not interchangeable with a hardware report, and overrides do not uniformly bypass applet probes.

## Concurrency Model

Behavior added by the discovery/session concurrency hardening (see `ExchangeGuard`, `DeviceConnectionRegistry`):

- **Protocols refuse overlapping logical exchanges.** `PcscProtocol` (+ SCP wrapper), `FidoHidProtocol`, and `OtpHidProtocol` run each full logical exchange through `ExchangeGuard`. A second operation throws `InvalidOperationException` immediately; sequential awaited operations are unchanged. Disposal atomically closes the guard to new admissions, drains an admitted exchange, and only then tears down protocol/SCP state and any owned connection. Prefer async disposal; synchronous disposal blocks for the same drain and must never be called from inside that operation.
- **Constituent I/O is shielded after admission.** SmartCard cancellation is checked at entry; admitted transmits use `CancellationToken.None`. FIDO can request CANCEL after a keep-alive and drain the original response; OTP touch-wait cancellation can attempt a reset. Neither supplies rollback, a completion deadline, or native release proof.
- **Whole workflows remain caller-owned.** Await complete public operations before another operation or disposal. The guard drains an exchange, not later exchanges, parsing/cache work, or detached callbacks. Never dispose from a callback that the operation must await.
- **The guard is protocol-instance scoped.** An SCP wrapper shares its base PC/SC guard, but independently created raw protocol instances over one connection are not coordinated. The supported contract prevents that shape by admitting one `ApplicationSession` per connection; connection-wide raw protocol ownership is a separate API decision.
- **Grouped connection claims are acquired incrementally before native open.** Member IDs are sorted and deduplicated, then claimed one at a time. An active discovery lease on an earlier member can delay checking a later member held by a live connection; cancellation can end that wait first. Encountering a live holder refuses acquisition with `ConnectionInUseException`, and failure rolls back earlier claims in reverse order. This is not a whole-set atomic preflight or an invocation-time refusal guarantee. Built-in PC/SC and macOS/Linux native owners require their route-specific release proof; Windows/custom compatibility disposal does not automatically prove physical teardown. Standalone records use one-element scopes. Discovery remains per-interface and nonblocking, with waiting connections retaining priority. `ConnectionSessionGuard` separately allows one live session per connection and sequential reuse. These are in-process claims, not cross-process exclusion.
- **Discovery caller waits are budgeted and reads are single-flight.** Identity reads use two seconds per attempt; composite metadata uses three seconds. Underlying work can continue after that wait. One finder owns an atomic evidence epoch containing identity cache, metadata cache, and read scope. Hotplug invalidation replaces the whole epoch and retires queued admissions; active work keeps its native ownership and writes only into the old caches. Identity entries validate the observed PID; successful metadata entries instead use member IDs and lack the equivalent PID check. Successful serial-less metadata can remain cached. Without listener hints, same-slot substitution can stay stale until observed absence. Public Find uses fresh per-call evidence and retires its scope on exit. See [discovery guarantees](../../docs/architecture/device-discovery-guarantees.md).
- **Potentially sibling discovery reads do not overlap.** Before worker admission, USB reads take a process-wide PID gate; an unknown USB PID takes all known PID gates atomically. Eligibility checks, complete grants, and releases share one short lock. Ineligible requests await a release notification without reserving any gates, so a pending unknown-PID request cannot spread a held PID's stall to unrelated PIDs, even in a live finder after its caller's budget expires. This conservative policy also serializes distinct same-PID keys, but known different PIDs and NFC remain independent. It changes no merge evidence or public connection claims. `FindYubiKeys` registers in-scope candidates in its read scope for both identity and metadata reads. The granted admission transfers to the discovery registry lease before native open and follows native release, including retained/quarantined built-in owners. Never release it at a bounded wait, cancellation, scope retirement, or merely because failed native disposal completed its task. Queued admission observes epoch retirement without occupying a native worker. `DiscoveryReadSchedulingTests` pins disposal, abandonment, retirement, and independent-PID progress with a live unknown-PID waiter; `PcscDiscoveryLifetimeTests` pins failed native-release retention.
- **The monitor is a single owned lifetime with one producer.** `YubiKeyMonitor` owns listeners and sequential
  scans/publications; `DeviceChangeFeed` owns membership, identity retention, and isolated subscriptions.
  Disposal stops listeners, cancels and awaits the loop without a timeout, retires discovery, then completes
  the feed; bounded native waits do not release native leases before actual teardown.
- **Registered connections dispose exactly once, and disposal shares one outcome.** `DisposalGate` gives the first `Dispose`/`DisposeAsync` caller the claim via one atomic compare-exchange. Every other caller observes that same completion — async callers await it and sync callers block on it. Built-in PC/SC connections create their lifetime owner before native open, run open/transmit/transaction/end/checked release on one lazy background worker, refuse overlapping ordinary raw calls, and release the registry lease only after successful disconnect plus context release. A release error retains and quarantines the claim; `UnrecoveredConnectionException` distinguishes it from ordinary live contention. External SmartCard implementations that fault disposal likewise retain their claim because Core has no native release proof. Built-in macOS/Linux HID transfer claims to their native owners before open; Windows/custom HID registered-wrapper release behavior is unchanged. Linux EINTR/EIO close diagnostics still release proven-closed descriptors' original claims.

## Known Gotchas

1. **APDU Size Limits**: complete-command constants are `Neo = 1390`, `Yk4 = 2038`, and `Yk43 = 3062`; short-mode data chunks are 255 bytes. These are different units. Transport support and configuration also select framing.
2. **Connection Ownership**: whoever CREATES a connection disposes it with `await using`. Protocols and sessions are pure users — `PcscProtocol`, `FidoHidProtocol`, `OtpHidProtocol`, and direct `Session.CreateAsync(connection)` never dispose a connection they were handed. The one exception is deliberate and internal: an `IYubiKey.Create<App>SessionAsync` convenience entry point opens a connection the caller never sees, so it calls `ApplicationSession.OwnConnection()` to hand that connection's lifetime to the session it returns. A leaked connection can retain the physical-device lease and block later opens; best-effort native finalization is not a substitute for deterministic disposal.
3. **SCP Key Zeroing**: Always zero SCP keys after use; `StaticKeys` implements `IDisposable`
4. **TLV Disposal**: `Tlv` and `DisposableTlvList` must be disposed
5. **Platform-Specific Behavior**: PC/SC APIs behave differently across platforms; test on all three
6. **Chained Response Assembly**: `INS_SEND_REMAINING` (0xC0) is used by default; some apps use custom values
7. **Access Tiers**: Applet sessions are the golden path. Raw sessions bypass applet checks but retain session ownership and overlap guards. Raw `IConnection` calls bypass session and exchange guards; built-in PC/SC additionally refuses overlapping native calls at the connection boundary. Do not interleave raw calls with sessions, and dispose/reopen after an interrupted exchange. See [Raw Access Tiers](../../docs/architecture/raw-access-tiers.md)

## Related Modules

- **Yubico.YubiKit.Management** - Uses Core for device info queries
- **Yubico.YubiKit.Ctap2** - Uses Core's shared FIDO HID framing and cryptography for CTAP2 operations
- **Yubico.YubiKit.Piv** - Uses Core's SmartCard protocol
