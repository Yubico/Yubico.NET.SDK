# SCP Implementation Plan

## Overview
Port Java SCP (Secure Channel Protocol) implementation to C# with idiomatic patterns, modern .NET 8/10 crypto APIs, and performance-focused memory management.

## Progress Tracking

### Phase 1: Core Types (3 files)
- [x] ScpKid.cs - Static class with SCP key identifier constants
- [x] KeyRef.cs - `readonly record struct` for key references (Kid, Kvn)
- [x] DataEncryptor.cs - Delegate type for data encryption

### Phase 2: Key Management Classes (5 files)
- [x] SessionKeys.cs - `sealed class : IDisposable` for session keys (Senc, Smac, Srmac, Dek?)
- [x] StaticKeys.cs - `sealed class : IDisposable` for static keys + derivation methods
- [x] ScpKeyParams.cs - Base record exposing the key reference
- [x] Scp03KeyParams.cs - `sealed record` implementing ScpKeyParams
- [x] Scp11KeyParams.cs - `sealed record` implementing ScpKeyParams

### Phase 3: Cryptography Helper (1 file)
- [x] AesCmac.cs - `sealed class : IDisposable` for AES-CMAC (NIST SP 800-38B)

### Phase 4: Supporting Utilities (2 files)
- [x] PublicKeyValues.cs - Abstract base + nested Ec class for EC key handling
- [x] Tlvs helper usage - Reuse shared `Yubico.YubiKit.Core/src/Utils/TlvHelper.cs`

### Phase 5: Fix Existing Code (2 files)
- [x] ScpState.cs - Fix compilation errors, add missing logic
- [x] ScpProcessor.cs - Complete implementation, return ResponseApdu

### Phase 6: Session Classes (1 file) (CURRENT PHASE)
- [x] SecurityDomainSession.cs - Security Domain operations (see `docs/security-domain-meta.md` for design notes and source references to the Java and legacy C# implementations)

### Phase 7: SCP Integration with Protocol and Sessions 
**Goal**: Enable SCP (Secure Channel Protocol) initialization in PcscProtocol and ManagementSession

#### 7.1 Update PcscProtocol (2 methods)
- [x] Provide SCP initialization via `ISmartCardProtocol.WithScpAsync`
  - Extension builds the base processor and dispatches to `ScpInitializer`
  - `ScpInitializer` handles SCP03 and SCP11 flows and returns encryptor when available

**Key Points**:
- Base processor = `BuildBaseProcessor()` result (without SCP wrapping)
- ScpProcessor wraps the base processor + formatter + state
- Need to cast `_processor` to `IApduProcessor` interface
- EXTERNAL AUTHENTICATE for SCP03 should NOT use MAC/encryption (sendApdu with useScp=false flag)

#### 7.2 Update ISmartCardProtocol Interface
- [x] Handled via extension method to avoid interface change

#### 7.3 Update ManagementSession Constructor
- [x] Constructor accepts optional `ScpKeyParams? scpKeyParams` and wires it through

#### 7.4 Update ManagementSession.CreateAsync
- [x] Factory method forwards optional `ScpKeyParams?` into the session

#### 7.5 Update ManagementSession.InitializeAsync
- [x] Initialize SCP via `WithScpAsync` when parameters supplied before configuring

#### 7.6 Factory Pattern Consideration
- [x] **Decision**: Keep factories unchanged for now
  - `ScpKeyParams` passed directly to `ManagementSession`
  - Protocol initialization happens post-construction via extension helper
  - **Rationale**: Simpler, matches Java pattern, allows protocol reuse

#### Implementation Order:
1. Update ISmartCardProtocol interface
2. Implement InitScpAsync methods in PcscProtocol
3. Update ManagementSession constructor and CreateAsync
4. Update ManagementSession.InitializeAsync
5. Test with SCP03 parameters
6. Test with SCP11 parameters

#### Reference Java Implementation:
- `SmartCardProtocol.initScp()` (lines 271-289)
- `SmartCardProtocol.initScp03()` (lines 291-310)
- `SmartCardProtocol.initScp11()` (lines 312-322)
- `ManagementSession` constructor (lines 131-154)

## Design Decisions

### Type Choices
- **`record` or `record struct`**: Immutable value types (KeyRef, Scp03KeyParams, Scp11KeyParams)
- **`sealed class : IDisposable`**: Sensitive data (SessionKeys, StaticKeys, AesCmac)
- **`sealed class`**: Mutable state or complex logic (ScpState, SecurityDomainSession)

### Memory Management
- `Span<byte>` for stack-allocated buffers (≤512 bytes)
- `ArrayPool<byte>.Shared` for larger temporary buffers
- `Memory<byte>`/`ReadOnlyMemory<byte>` for storage
- `CryptographicOperations.ZeroMemory()` for sensitive data cleanup

### .NET Crypto APIs Used
- `Aes.EncryptEcb/EncryptCbc/DecryptCbc` with Span<byte> (.NET 8+)
- `CryptographicOperations.HashData()` for one-shot SHA-256 (.NET 9+)
- `ECDiffieHellman` for SCP11 key agreement
- `X509Certificate2` for certificate handling
- Custom AES-CMAC implementation (not in BCL)

### Async Patterns
- All I/O operations async with CancellationToken
- `ConfigureAwait(false)` throughout

### Nullability
- Nullable reference types enabled
- Explicit `?` for optional parameters

## Cryptography Migration Roadmap

1. **Pass 0 – Snapshot & Wiring**
  - Copy legacy `Yubico.YubiKey.Cryptography` sources into `Yubico.YubiKit.Core/src/Cryptography`, adjusting namespaces and project references only as needed for compilation.
  - Resolve immediate build breaks (e.g., resource strings, assembly internals) without altering behaviour.

2. **Pass 1 – Baseline Integration**
  - Hook `SecurityDomainSession` and other SCP components to the imported abstractions so SCP features compile end-to-end.
  - Validate core flows with existing unit/integration coverage where hardware allows.

3. **Pass 2 – Modernization Sweep**
  - Enable nullable/file-scoped namespaces, update to modern C# 14 idioms, simplify guard clauses, adopt `Span`/`Memory` where beneficial.
  - Replace bespoke ASN.1 handling with BCL helpers where they offer equivalent functionality while keeping public APIs stable.

4. **Pass 3 – Abstraction Pruning**
  - Audit which wrappers (`KeyDefinition`, zeroing handles, etc.) still add value; remove or shrink layers that simply forward to BCL types.
  - Introduce default interface implementations for shared behaviour we want to keep but not duplicate in each concrete key type.

5. **Pass 4 – Curve25519 & Extensibility**
  - Re-introduce X25519/Ed25519 support within the modernized abstractions, adding targeted tests.
  - Document extension points for future algorithms and ensure cleanup semantics are consistent across all key families.

6. **Pass 5 – Validation & Cleanup**
  - Run full test suites (including hardware-dependent paths when possible) and update docs/changelog with guidance on the new cryptography module shape.

  ## Notes for Next Contributor
  - Current state: `SecurityDomainSession` has API stubs for GET DATA, key management, certificate CRUD, etc. Implementation is blocked on bringing in the legacy cryptography abstractions (EC/Curve25519 key wrappers, ASN helpers, key definitions).
  - Legacy reference lives under `Yubico.NET.SDK-zig-glibc/Yubico.YubiKey/src/Yubico/YubiKey/Cryptography`. The plan is to copy these files verbatim in **Pass 0**, changing namespaces to `Yubico.YubiKit.Core.Cryptography` and fixing immediate compile issues without behaviour changes.
  - We already created an empty `Yubico.YubiKit.Core/src/Cryptography` folder and added initial interface stubs. Once the legacy files are copied, reconcile duplicates (keep legacy content, adapt our stubs accordingly).
  - Modern .NET (Core 3+/5+) provides `ImportPkcs8PrivateKey` / `ImportSubjectPublicKeyInfo`. During **Pass 2** we can delegate P-curve handling to those APIs, but Curve25519 still needs custom logic.
  - Curve25519 support is required long-term. Ensure the legacy `Curve25519PrivateKey/PublicKey` wrappers make it across in Pass 0 so future passes can integrate them cleanly.
  - Keep an eye on resource strings (`ExceptionMessages`). If the legacy files depend on `Resources/ExceptionMessages.*`, either port the resource or temporarily inline messages so Pass 0 builds.
  - When integrating with `SecurityDomainSession`, the SCP reset implementation already uses raw APDUs. Replacing the stubs will require parsing TLVs and using the new key wrappers—expect follow-up work after the cryptography module lands.
  - We also want to revisit whether the modern `ECDiffieHellman`/`ECDsa` implementations (e.g., via `ExportSubjectPublicKeyInfo`/`ImportPkcs8PrivateKey`) can replace or augment the wrapper types for SCP key import/export; capture findings during Pass 2 to confirm all Security Domain scenarios are covered before pruning wrappers.
  - Tests: no automated coverage yet for SCP due to hardware requirements. Capture manual validation steps whenever hardware testing occurs to help future contributors.

## File Locations
All files created in: `/home/dyallo/Code/y/Yubico.NET.SDK/Yubico.YubiKit.Core/src/SmartCard/Scp/`

## Notes
- Based on Java implementation from yubikit-android
- Implements SCP03 (symmetric keys) and SCP11 (EC keys with certificates)
- Thread-safe where applicable
- Comprehensive XML documentation for public APIs
