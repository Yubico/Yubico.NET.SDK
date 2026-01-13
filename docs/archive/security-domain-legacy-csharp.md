# Legacy C# SecurityDomainSession Learnings

## Overview
- Located in `Yubico.YubiKey/src/Yubico/YubiKey/Scp/SecurityDomainSession.cs` and derives from `ApplicationSession`.
- Source path: `/home/dyallo/Code/y/Yubico.NET.SDK-zig-glibc/Yubico.YubiKey/src/Yubico/YubiKey/Scp/SecurityDomainSession.cs`.
- Provides SCP03/SCP11 management operations plus helper APIs (store certificates, allowlists, reset, etc.).
- Heavily integrated with existing command classes (`PutKeyCommand`, `StoreDataCommand`, etc.) and logging infrastructure.

## Construction And SCP Lifecycle
- Constructors wire through to `ApplicationSession` (which likely handles device selection and optional SCP activation).
- Secure channel encryptor obtained lazily via `IScpYubiKeyConnection`; throws when secure messaging hasn’t been established.

## Core Commands & Patterns
- Uses dedicated command types per operation (e.g., `PutKeyCommand`, `DeleteKeyCommand`) instead of building APDUs inline.
- TLV handling via `TlvObject`, `TlvReader`, and `TlvObjects.EncodeList/DecodeList` utilities.
- KCV verification done client-side using `AesUtilities.AesCbcEncrypt` and fixed-time comparison helper.

## Read Operations
- `GetKeyInformation`, `GetCertificates`, `GetSupportedCaIdentifiers`, and `GetCardRecognitionData` wrap TLV parsing into strongly typed return values.
- `ExecuteGetDataCommand` centralizes raw `GET DATA` command execution.

## Write Operations
- `PutKey` overloads handle SCP03 static keys (AES) and SCP11 EC keys; rely on session encryptor for sensitive payloads.
- `StoreCaIssuer`, `StoreCertificates`, `StoreAllowlist`, and `StoreData` compose TLVs before sending `STORE DATA` APDU.
- `DeleteKey` enforces SCP03 deletion rules (KVN vs KID) and builds TLV filters dynamically.

## Reset Logic
- `Reset` enumerates key references, selects opcode per key type, and repeatedly authenticates to block keys (65 attempts) mirroring Java logic.

## Portability Assessment
- Aligns closely with .NET runtime conventions; async not used—calls are synchronous returning immediate results.
- Uses span-aware APIs (`ReadOnlySpan`, `ReadOnlyMemory`) for efficient byte handling; direct port to newer code should retain this pattern.
- Command classes encapsulate APDU details; integrating with new architecture may require adapters or refactoring to shared low-level layer.
- Crypto routines rely on BCL (`CryptographicOperations`, `ECCurve`) and custom helpers; mostly reusable with minimal adjustments.
- Exception model uses custom `SecureChannelException` plus standard `ArgumentException`/`InvalidOperationException`.

## Maintainability Notes
- **Strengths**: Command objects centralize APDU format logic; structured logging across methods; TLV utilities keep parsing consistent.
- **Strengths**: Extensive guard clauses and helper methods (`ValidateCheckSum`, `CreateECPublicKeyFromBytes`) reduce duplication.
- **Weaknesses**: Heavy reliance on `MemoryStream`/`BinaryWriter` inline; lacks pooling or span-based builders in some cases.
- **Weaknesses**: Synchronous API surface may limit scalability if future context requires async/await integration.
- **Weaknesses**: Reset loop hard-codes retry counts and status-handling; abstracting policy would ease updates.
