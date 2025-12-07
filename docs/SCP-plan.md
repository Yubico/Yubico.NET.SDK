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

### Phase 6: Session Classes (1 file)
- [ ] SecurityDomainSession.cs - Security Domain operations (see `docs/security-domain-meta.md` for design notes)

### Phase 7: SCP Integration with Protocol and Sessions (CURRENT PHASE)
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

## File Locations
All files created in: `/home/dyallo/Code/y/Yubico.NET.SDK/Yubico.YubiKit.Core/src/SmartCard/Scp/`

## Notes
- Based on Java implementation from yubikit-android
- Implements SCP03 (symmetric keys) and SCP11 (EC keys with certificates)
- Thread-safe where applicable
- Comprehensive XML documentation for public APIs
