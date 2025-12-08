# SecurityDomainSession (Java) Learnings

## Overview
- Java implementation lives under `com.yubico.yubikit.core.smartcard.scp` and extends `ApplicationSession<SecurityDomainSession>`.
- Source path: `/home/dyallo/Code/y/yubikit-android/core/src/main/java/com/yubico/yubikit/core/smartcard/scp/SecurityDomainSession.java`.
- Session wraps a `SmartCardProtocol`, optionally bootstrapped with SCP during construction or on-demand via `authenticate`.
- Assumes firmware >= 5.3.0, selects the Security Domain AID, and applies default protocol configuration.

## Construction And SCP Lifecycle
- Constructors accept either a `SmartCardConnection` or an already constructed `SmartCardProtocol`.
- Optional `ScpKeyParams` allows SCP setup during initialization; otherwise `authenticate` can be called later.
- `authenticate` updates the cached `DataEncryptor` so writes (e.g. PUT KEY) can encrypt user supplied key material.

## Core Commands
- `getData`, `storeData`, `deleteKey`, `generateEcKey`, `putKey`, and `reset` send raw APDUs through the wrapped protocol.
- Uses constants for INS codes (GET DATA, PUT KEY, STORE DATA, DELETE, GENERATE KEY, SCP negotiation opcodes).
- TLV helpers encode parameters; e.g. `Tlv`/`Tlvs` build payloads for certificate bundles and allowlists.

## Read Operations
- `getCardRecognitionData` unwraps TLV tag 0x73 from a GET DATA call.
- `getKeyInformation` parses TLV list to map each `KeyRef` to available key components.
- `getCertificateBundle` fetches TAG_CERTIFICATE_STORE and decodes one or many X.509 certificates.
- `getSupportedCaIdentifiers` optionally merges KLOC/KLCC identifier blocks into a map of `KeyRef` -> raw identifier bytes.

## Write Operations
- `storeCertificateBundle`, `storeAllowlist`, and `storeCaIssuer` build TLV envelopes targeting specific tags.
- `deleteKey` applies SCP rules: wildcards via zeroed KID/KVN, SCP03 deletion semantics, and safety checks.
- `generateEcKey` requests SD to create a new SCP11 key pair and returns the public point.

## Key Import Helpers
- `putKey` overloads handle SCP03 static keys (AES) and SCP11 private/public EC keys.
- Validates expected KIDs (SCP03 requires KID 0x01; SCP11 variants require SECP256R1 curve).
- Uses session `DataEncryptor` (derived DEK) to encrypt sensitive key bytes before transmission.
- Generates and validates KCVs (3-byte truncation of CBC encrypt) for SCP03 keys.

## Reset Logic
- `reset` iterates all known key references and intentionally blocks each by exhausting authentication attempts.
- Chooses opcode based on KID (SCP03 uses INITIALIZE UPDATE with KVN wildcard, SCP11 variants use internal/external authenticate).

## Error Handling Patterns
- Wraps SCP initialization failures in `IllegalStateException` during constructor path.
- Distinguishes SW codes (e.g. REFERENCED_DATA_NOT_FOUND) to return empty collections.
- Validates preconditions (non-null encryptor, curve checks, wildcard rules) and throws descriptive exceptions.

## Reuse Ideas For .NET
- Mirror optional SCP bootstrapping plus on-demand authenticate to keep session flexible.
- Preserve separation between read and write helpers, leveraging shared TLV utilities.
- Carry forward explicit validation and error messaging to surface misconfiguration early.
- Reuse KCV calculation, DEK-based encryption, and reset-by-blocking mechanics for parity.

## Portability Assessment
- API shape maps cleanly to C# generics/async patterns; command methods translate to async Task-returning wrappers.
- TLV handling relies on helper classes; existing .NET `TlvHelper` can satisfy the same role with minor tweaks.
- Java `MessageDigest`, `ByteBuffer`, and certificate utilities have direct .NET counterparts (`CryptographicOperations`, `Span`/`BinaryPrimitives`, `X509Certificate2`).
- Exception model aligns—can surface similar custom exceptions or wrap in `InvalidOperationException`/`BadResponseException` equivalents.
- Logging via SLF4J in Java should map to `ILogger` usage; maintain structured logging for parity.
- Pay attention to memory hygiene: Java relies on GC/zeroing via `Arrays.fill`; .NET version should use `CryptographicOperations.ZeroMemory` and `using` for disposables.

## Maintainability Notes
- **Strengths**: Centralizes APDU constants, reuses TLV helpers, and keeps SCP lifecycle isolated in one session type, making behavior easy to audit. Extensive validation and logging improve diagnostics.
- **Strengths**: Overloaded `putKey`/`store*` methods cover distinct scenarios cleanly, and optional SCP bootstrapping supports multiple usage patterns without code duplication.
- **Weaknesses**: Heavy reliance on mutable byte arrays/`ByteBuffer` with manual zeroing increases risk of future regressions; abstracting repetitive encryption/KCV logic could reduce duplication.
- **Weaknesses**: `reset` routine depends on hard-coded attempt counts and opcode selection; encapsulating policy decisions would make future firmware changes safer to adopt.
- **Weaknesses**: Error handling wraps some exceptions into `IllegalStateException`, which obscures root causes; more granular exception types could help downstream callers.
