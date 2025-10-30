# SCP Implementation Plan

## Overview
Port Java SCP (Secure Channel Protocol) implementation to C# with idiomatic patterns, modern .NET 8/10 crypto APIs, and performance-focused memory management.

## Progress Tracking

### Phase 1: Core Types (3 files)
- [ ] ScpKid.cs - Static class with SCP key identifier constants
- [ ] KeyRef.cs - `readonly record struct` for key references (Kid, Kvn)
- [ ] DataEncryptor.cs - Delegate type for data encryption

### Phase 2: Key Management Classes (5 files)
- [ ] SessionKeys.cs - `sealed class : IDisposable` for session keys (Senc, Smac, Srmac, Dek?)
- [ ] StaticKeys.cs - `sealed class : IDisposable` for static keys + derivation methods
- [ ] ScpKeyParams.cs - Interface with GetKeyRef() method
- [ ] Scp03KeyParams.cs - `sealed record` implementing ScpKeyParams
- [ ] Scp11KeyParams.cs - `sealed record` implementing ScpKeyParams

### Phase 3: Cryptography Helper (1 file)
- [ ] AesCmac.cs - `sealed class : IDisposable` for AES-CMAC (NIST SP 800-38B)

### Phase 4: Supporting Utilities (2 files)
- [ ] PublicKeyValues.cs - Abstract base + nested Ec class for EC key handling
- [ ] Tlvs.cs - Static helper methods (EncodeList, DecodeList, UnpackValue)

### Phase 5: Fix Existing Code (2 files)
- [x] ScpState.cs - Fix compilation errors, add missing logic
- [x] ScpProcessor.cs - Complete implementation, return ResponseApdu

### Phase 6: Session Classes (1 file)
- [ ] SecurityDomainSession.cs - Security Domain operations

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
