# Applet public API consolidation

**Date:** 2026-09-04  
**Status:** Ready for implementation  
**Scope:** Core application-session contract and the Management, PIV, FIDO2, OATH, OpenPGP, Security Domain, YubiOTP, and YubiHSM Auth applet facades

## Executive summary

Consolidate the public applet APIs around one recognizable product grammar without forcing different applet protocols into identical domain models. Every applet keeps its own meaningful vocabulary, but session creation, lifecycle state, interface parity, asynchronous methods, memory ownership, collections, options, and compatibility enforcement follow shared rules.

The implementation uses the 2.0 alpha window for one coordinated breaking normalization. After the surface is accepted, compile-time public API declarations and .NET package validation prevent accidental breaking changes. The design deliberately rejects request objects everywhere, command/executor layers, interface fragmentation, and speculative abstractions.

## Ideal state

A consumer can move between applets and immediately recognize how to create, inspect, use, and dispose a session. Required domain inputs remain direct and readable; proven growth axes use small options objects; low-level protocol escape hatches remain available; and every public surface change is explicit, reviewed, and mechanically checked.

## Ideal state criteria

1. Every applet exposes one sealed `XSession`, one public `IXSession` testing contract, one `IYubiKey.CreateXSessionAsync` golden path, and one borrowed-connection `XSession.CreateAsync` path.
2. Every applet factory accepts `SessionCreationOptions?` followed by a defaulted `CancellationToken`.
3. `SessionCreationOptions` contains only cross-cutting creation concerns and can grow through additive properties.
4. Every application session exposes its effective `FirmwareVersion`, `ConnectionType`, initialization state, protocol-authentication state, and feature checks through `IApplicationSession`.
5. Public instance members on each concrete applet session are represented by its interface.
6. Public device input/output methods use `Task` or `Task<T>`, end in `Async`, and accept a final defaulted cancellation token.
7. Borrowed asynchronous byte input uses `ReadOnlyMemory<byte>`; textual secret parameters use an `Utf8` suffix; ownership and zeroing responsibilities are documented.
8. Public collections use read-only interfaces unless mutability is an intentional part of the contract.
9. Options objects appear only for demonstrated growth axes, boolean ambiguity, or policy groups; required domain payloads do not become command/request objects by default.
10. Applet dependency-injection helpers and delegates are removed before general availability.
11. Existing one-shot `IYubiKey` convenience operations remain during this round but receive normalized signatures; their broader taxonomy is deferred.
12. Public API declarations and package validation detect accidental surface changes.
13. No applet wire behavior, transport fallback behavior, session ownership rule, or security lifecycle changes as part of this refactor.

## Design principles

- Same rhythm, not forced domain symmetry.
- Sealed session classes are the product surface; interfaces are complete testing and composition seams implemented by the SDK.
- External implementations of applet-session interfaces are not an initial compatibility promise.
- Required operation inputs stay positional.
- A trailing options object is used only when independent optional policies already exist or protocol growth is expected.
- One intrinsic boolean is acceptable when it reads naturally and does not select structurally different operations.
- Multiple booleans or mode-switch booleans become options, flags, an enum, or separate methods.
- Domain protocol terms may differ when they express materially different semantics.
- Raw sessions remain the general forward-compatibility escape hatch; no generic command/executor abstraction is introduced.
- Existing public signatures never gain optional parameters after general availability. Add an overload or add a property to an existing options type.

## Change taxonomy

This taxonomy is the checklist for this pass and future audit rounds. The durable copy belongs in `docs/architecture/applet-public-api.md`.

| ID | Category | Round-one decision | Status |
|---|---|---|---|
| CRE | Session creation | Shared `SessionCreationOptions`; uniform factories | Accepted |
| CON | Common contract | Shared `ConnectionType`; documented version semantics | Accepted |
| PAR | Interface parity | Concrete session members mirrored by testing interfaces | Accepted |
| EXT | Extensibility | Options only for proven growth axes | Accepted |
| MEM | Memory ownership | Borrowed bytes use `ReadOnlyMemory<byte>` | Accepted |
| ASY | Asynchronous shape | `Task`, `Async` suffix, final cancellation token | Accepted |
| COL | Collections | Return read-only collection interfaces | Accepted |
| NAM | Naming | Normalize equivalent concepts without erasing protocol terms | Accepted |
| SUR | Surface minimization | Remove applet dependency-injection APIs | Accepted |
| CMP | Compatibility | Public API declarations and package validation | Accepted |
| CNV | Convenience layer | Design a common one-shot operation taxonomy | Deferred |
| VER | Firmware override | Retain `FirmwareVersionOverride` in the initial design | Provisional; revisit before final baseline |

## Shared session creation shape

Add `src/Core/src/Sessions/SessionCreationOptions.cs`:

```csharp
public sealed class SessionCreationOptions
{
    public ProtocolConfiguration? ProtocolConfiguration { get; init; }

    // Borrowed. The caller retains ownership and disposal responsibility.
    public ScpKeyParameters? ScpKeyParameters { get; init; }

    // Selects from IYubiKey or validates an existing connection.
    public ConnectionType? PreferredConnectionType { get; init; }

    // Explicitly overrides the version used for configuration and feature gates.
    public FirmwareVersion? FirmwareVersionOverride { get; init; }
}
```

The connection-owning golden path is:

```csharp
public static Task<PivSession> CreatePivSessionAsync(
    this IYubiKey yubiKey,
    SessionCreationOptions? options = null,
    CancellationToken cancellationToken = default);
```

The caller-owned connection path is:

```csharp
public static Task<PivSession> CreateAsync(
    ISmartCardConnection connection,
    SessionCreationOptions? options = null,
    CancellationToken cancellationToken = default);
```

Multi-transport applets use `IConnection` in the direct factory but otherwise retain the same shape. `PreferredConnectionType` selects a connection in an `IYubiKey` extension and acts as an assertion in a direct factory. SmartCard-only applets accept `null` or `ConnectionType.SmartCard`. Invalid combinations fail before wire input/output. Supplying secure-channel parameters without an explicit preference continues to force SmartCard where that behavior already exists.

The options object is consumed during initialization and is not retained. It does not own or dispose secure-channel parameters. Raw sessions keep specialized factories because their secure-channel and firmware requirements are materially different.

## Interface evolution policy

The public `IXSession` interfaces remain because they are useful for tests and higher-level SDK components. They are documented as SDK-implemented contracts, not third-party applet plug-in points.

The general-availability surface starts with exact concrete/interface parity. For a future minor release, a new interface operation should use a default interface implementation that returns a failed task containing `NotSupportedException`, while the SDK session supplies the real implementation. This avoids breaking existing test fakes without moving protocol logic into the interface. A large optional subsystem may instead use a companion type, following FIDO2 credential management, but capability-interface proliferation is not the default.

## Implementation tasks

### 1. Add the public API convention test project

**Create:**

- `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/Yubico.YubiKit.PublicApi.UnitTests.csproj`
- `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AppletSessionShapeTests.cs`
- `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AsyncSurfaceConventionTests.cs`
- `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/FactoryShapeTests.cs`
- `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/MemoryAndCollectionConventionTests.cs`

**Modify:**

- `Yubico.YubiKit.sln`

Reference Core and all eight applet assemblies. Write reflection tests that initially expose current violations: missing interface members, inconsistent factories, public `ValueTask` operations, mutable collection returns, raw array inputs, and inconsistent cancellation placement. Keep explicit allowlists for synchronous cached operations, `DisposeAsync`, and caller-owned secret outputs such as OATH key derivation.

Run:

```bash
dotnet toolchain.cs -- test --project PublicApi
```

Expected before normalization: targeted convention failures documenting the worklist.

### 2. Add the Core creation and session contracts

**Create:**

- `src/Core/src/Sessions/SessionCreationOptions.cs`

**Modify:**

- `src/Core/src/Abstractions/IApplicationSession.cs`
- `src/Core/src/Sessions/ApplicationSession.cs`
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs`

Add `ConnectionType ConnectionType { get; }` and implement it from `Connection.Type`. Add precise documentation for effective firmware version semantics, connection selection/assertion, option ownership, secure-channel ownership, and explicit firmware override behavior.

Do not add validation-only tests for property setters. Test observable factory/session behavior through the applet slices and enforce shape through the public API convention suite.

Run:

```bash
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- test --project PublicApi
```

### 3. Migrate applet factories one module at a time

**Modify session and extension files:**

| Module | Session | Extension |
|---|---|---|
| Management | `src/Management/src/ManagementSession.cs` | `src/Management/src/IYubiKeyExtensions.cs` |
| PIV | `src/Piv/src/PivSession.cs` | `src/Piv/src/IYubiKeyExtensions.cs` |
| FIDO2 | `src/Fido2/src/FidoSession.cs` | `src/Fido2/src/IYubiKeyExtensions.cs` |
| OATH | `src/Oath/src/OathSession.cs` | `src/Oath/src/IYubiKeyExtensions.cs` |
| OpenPGP | `src/OpenPgp/src/OpenPgpSession.cs` | `src/OpenPgp/src/IYubiKeyExtensions.cs` |
| Security Domain | `src/SecurityDomain/src/SecurityDomainSession.cs` | `src/SecurityDomain/src/IYubiKeyExtensions.cs` |
| YubiOTP | `src/YubiOtp/src/YubiOtpSession.cs` | `src/YubiOtp/src/IYubiKeyExtensions.cs` |
| YubiHSM Auth | `src/YubiHsm/src/HsmAuthSession.cs` | `src/YubiHsm/src/IYubiKeyExtensions.cs` |

For each module, first update its factory tests to describe the accepted shape and current transport behavior. Replace the public signatures, snapshot option properties at factory entry, update all module tests/examples/callers, then run the focused unit tests.

Preserve existing default transport order, secure-channel SmartCard forcing, one-connection ownership, initialization-failure cleanup, and no-fallback behavior. Remove the Phase-38 compatibility overloads in Management, FIDO2, and YubiOTP.

Where an applet reports a version during initialization, continue the required initialization exchange and let `FirmwareVersionOverride` determine the effective version afterward. Revisit this property before the final public API baseline is declared shipped.

Run after each module:

```bash
dotnet toolchain.cs -- test --project <Module>
dotnet toolchain.cs -- test --project PublicApi
```

### 4. Remove applet dependency-injection APIs

**Delete:**

- `src/Management/src/DependencyInjection.cs`
- `src/Fido2/src/DependencyInjection.cs`
- `src/Oath/src/DependencyInjection.cs`
- `src/OpenPgp/src/DependencyInjection.cs`
- `src/SecurityDomain/src/DependencyInjection.cs`
- `src/YubiOtp/src/DependencyInjection.cs`
- `src/YubiHsm/src/DependencyInjection.cs`
- `src/Management/tests/Yubico.YubiKit.Management.UnitTests/DependencyInjectionTests.cs`
- `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/DependencyInjectionTests.cs`
- `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_DependencyInjectionTests.cs`

Refactor test helpers and examples that resolve factory delegates to call the static factory or `IYubiKey` extension directly. Remove no-longer-needed dependency-injection package references from affected test projects. Do not add a PIV registration API.

### 5. Normalize common session parity

**Modify:**

- `src/Management/src/ManagementSession.cs`
- `src/Management/src/IManagementSession.cs`
- `src/Piv/src/PivSession.cs`
- `src/Piv/src/IPivSession.cs`
- `src/Fido2/src/FidoSession.cs`
- `src/YubiHsm/src/HsmAuthSession.cs`
- `src/YubiHsm/src/IHsmAuthSession.cs`

**Delete:**

- `src/Piv/src/TouchNotification.cs`
- `src/YubiHsm/src/TouchNotification.cs`

Remove `ManagementSession.Transport` in favor of inherited `ConnectionType`. Add `Action? OnTouchRequired` to the PIV and YubiHSM Auth interfaces and implementations. Keep the no-context security rule in property documentation. Remove redundant `IAsyncDisposable` declarations and make interface/implementation parameter names identical.

### 6. Normalize Management operation growth

**Create:**

- `src/Management/src/SetDeviceConfigOptions.cs`

**Modify:**

- `src/Management/src/IManagementSession.cs`
- `src/Management/src/ManagementSession.cs`
- `src/Management/src/IYubiKeyExtensions.cs`
- Relevant Management unit tests, examples, README, and module guidance

Replace the `reboot`, `currentLockCode`, and `newLockCode` tail with one sealed init-only options class. Use caller-owned `ReadOnlyMemory<byte>` for lock codes, do not retain the options object, and preserve zeroing of SDK-owned encoded configuration bytes. Use the non-destructive no-reboot behavior as the default.

Change the one-shot extension return from `ValueTask` to `Task`.

### 7. Normalize PIV policy surfaces

**Create:**

- `src/Piv/src/PivKeyCreationOptions.cs`
- `src/Piv/src/PivUserVerification.cs`
- `src/Piv/src/PivCertificateCompression.cs`

**Modify:**

- `src/Piv/src/IPivSession.cs`
- `src/Piv/src/PivSession.cs`
- Relevant PIV protocol helpers, tests, examples, README, and module guidance

Move PIN and touch policy modifiers for key generation/import into `PivKeyCreationOptions`. Replace positional user-verification booleans with the three-state `PivUserVerification` enum, preserving every genuinely valid mode; `(true, true)` was an accidental combination where check-only won. Replace the certificate compression boolean with `PivCertificateCompression.Automatic` and `PivCertificateCompression.Always`.

Rename encoded PIN and PUK parameter names consistently: `pinUtf8`, `currentPinUtf8`, `newPinUtf8`, `pukUtf8`, and `newPukUtf8`. Use the canonical YubiKit source comparison workflow before changing user-verification semantics.

### 8. Normalize OATH credential and collection shape

**Modify:**

- `src/Oath/src/CredentialData.cs`
- `src/Oath/src/IOathSession.cs`
- `src/Oath/src/OathSession.cs`
- `src/Oath/src/IYubiKeyExtensions.cs`
- Relevant OATH tests and module guidance

Move `RequireTouch` into `CredentialData`, where the rest of credential creation policy already lives. Remove the separate method boolean. Change `CalculateAllAsync` and its one-shot extension to return `IReadOnlyDictionary<Credential, Code?>`.

Keep `byte[] DeriveKey(ReadOnlyMemory<byte>)` as a documented exception: the returned key is caller-owned secret material and the caller must be able to zero it. Do not wrap it in a speculative sensitive-buffer abstraction during this pass.

### 9. Normalize OpenPGP mode and algorithm selection

**Modify:**

- `src/OpenPgp/src/IOpenPgpSession.cs`
- `src/OpenPgp/src/OpenPgpSession.Pin.cs`
- `src/OpenPgp/src/OpenPgpSession.Keys.cs`
- Relevant OpenPGP tests, examples, and module guidance

Replace `GenerateRsaKeyAsync` and `GenerateEcKeyAsync` with `GenerateKeyAsync(KeyRef, AlgorithmAttributes, CancellationToken)`. Reuse the existing `RsaAttributes` and `EcAttributes` hierarchy so adding another algorithm requires a new data type rather than another session signature.

Replace the `ResetPinAsync(..., bool useAdmin)` mode switch with two explicit methods: reset using a reset code, and reset using prior administrator authentication. No method should accept an input that another boolean causes it to ignore.

### 10. Normalize Security Domain selectors

**Create:**

- `src/SecurityDomain/src/CaIdentifierType.cs`

**Modify:**

- `src/SecurityDomain/src/ISecurityDomainSession.cs`
- `src/SecurityDomain/src/SecurityDomainSession.cs`
- Relevant Security Domain tests, examples, README, and module guidance

Introduce a flags enum for KLOC and KLCC selection and replace `includeKloc`/`includeKlcc`. Preserve wire encoding and result ordering. Keep the single intrinsic `deleteLast` boolean.

### 11. Normalize YubiOTP naming and asynchronous wrappers

**Modify:**

- `src/YubiOtp/src/IYubiOtpSession.cs`
- `src/YubiOtp/src/YubiOtpSession.cs`
- `src/YubiOtp/src/IYubiKeyExtensions.cs`
- Relevant YubiOTP tests, examples, and module guidance

Rename `GetSerialAsync` to `GetSerialNumberAsync`. Change the one-shot `PutConfigurationAsync` return from `ValueTask` to `Task`. Keep the polymorphic slot-configuration hierarchy and algorithm-specific challenge-response methods unchanged.

### 12. Keep FIDO2 and YubiHSM Auth operation shapes stable

FIDO2 remains the reference example for required positional inputs plus a trailing options object. Do not redesign `MakeCredentialAsync`, `GetAssertionAsync`, or the extension builder.

YubiHSM Auth retains algorithm-specific methods and their required positional secret inputs. Encourage named arguments in examples. Do not add request objects solely to shorten signatures. Only the common factory and callback/interface normalization applies in this round.

### 13. Adapt the deferred convenience layer without expanding it

Keep the existing one-shot `IYubiKey` operation extensions for now. Update them to delegate to normalized session methods, return `Task`, use read-only collection contracts, and accept new options types. Add no new one-shot operations in this pass.

Record `CNV` as deferred in `docs/architecture/applet-public-api.md`. A future pass should define which operation categories deserve device-level convenience methods and then add the chosen set consistently.

### 14. Add public API declarations and compatibility validation

**Modify:**

- `Directory.Packages.props`
- `Directory.Build.targets`
- All ten shipping SDK project directories

Add the current compatible `Microsoft.CodeAnalysis.PublicApiAnalyzers` package centrally and apply it privately to shipping SDK projects. Add reviewed `PublicAPI.Unshipped.txt` declarations for Core, all applet assemblies, and WebAuthn after normalization. Do not blindly accept generated declarations.

Before the 2.0 stable release, resolve the provisional firmware override decision and move the accepted surface into `PublicAPI.Shipped.txt`.

After a stable baseline package exists, enable .NET package validation and set `PackageValidationBaselineVersion` to the preceding stable package. Adding an optional parameter to an existing method is binary-breaking; add an overload or options property instead. Intentional major-version breaks require reviewed compatibility suppressions.

### 15. Update durable documentation

**Create:**

- `docs/architecture/applet-public-api.md`

**Modify:**

- `docs/SDK-HOUSE-STYLE.md`
- `docs/migration/v1-to-v2.md`
- Module `CLAUDE.md` files
- Existing module `README.md` files
- XML documentation and examples affected by renamed or reshaped APIs

Document the factory grammar, interface intent, options threshold, boolean rule, naming vocabulary, secret ownership, collection rule, compatibility process, taxonomy, and deferred work. Ensure examples use `await using`, named options, and named arguments where adjacent required byte-memory parameters remain.

## Execution order

Implement as independently reviewable slices:

1. Public API convention tests and taxonomy.
2. Core session contract and creation options.
3. Factory migration across all applets.
4. Dependency-injection surface removal and interface parity.
5. Management and PIV operation normalization.
6. OATH, OpenPGP, Security Domain, and YubiOTP normalization.
7. Documentation, examples, and deferred-convenience adaptation.
8. Public API declarations and final compatibility freeze.

For every production slice: write or update the focused test first, run it to observe the expected failure, make the smallest production change, rerun the module and public API tests, then inspect the public surface diff.

## Verification

Run focused tests during implementation:

```bash
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- test --project Management
dotnet toolchain.cs -- test --project Piv
dotnet toolchain.cs -- test --project Fido2
dotnet toolchain.cs -- test --project Oath
dotnet toolchain.cs -- test --project OpenPgp
dotnet toolchain.cs -- test --project SecurityDomain
dotnet toolchain.cs -- test --project YubiOtp
dotnet toolchain.cs -- test --project YubiHsm
dotnet toolchain.cs -- test --project PublicApi
```

Run final verification:

```bash
dotnet toolchain.cs build
dotnet toolchain.cs test
dotnet toolchain.cs -- pack --package-version 2.0.0-alpha.2
```

Scope formatting to changed or staged C# files; never format the whole solution indiscriminately. No hardware testing is expected because this migration changes public shape, delegation, and validation without changing wire behavior. If implementation reveals a wire-semantic change, stop that slice and use the canonical-source workflow before proceeding.

## Deferred decisions

1. Reassess whether `FirmwareVersionOverride` belongs in the general-availability public surface before moving declarations to `PublicAPI.Shipped.txt`.
2. Design a consistent taxonomy for one-shot `IYubiKey` convenience operations in a later audit round; do not remove or expand them in this round.

## Abbreviations

- API: Application Programming Interface, the public contract exposed by a library.
- FIDO: Fast Identity Online, the authentication standards family used by FIDO2.
- OATH: Initiative for Open Authentication, the standards family for one-time passwords.
- OTP: One-Time Password, a transient authentication code.
- PIN: Personal Identification Number, a user authentication secret.
- PIV: Personal Identity Verification, the smart-card application standard.
- PUK: Personal Unblocking Key, the secret used to unblock a PIN.
- SDK: Software Development Kit, the set of libraries and developer tooling in this repository.
- KLOC: Key Loading OCE Certificate identifier, a Security Domain certificate-authority identifier.
- KLCC: Key Loading Card Certificate identifier, a Security Domain certificate-authority identifier.
