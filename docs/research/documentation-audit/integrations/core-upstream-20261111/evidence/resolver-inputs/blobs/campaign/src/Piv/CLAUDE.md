# CLAUDE.md - PIV Module

This file provides Claude-specific guidance for working with the PIV module. **Read [README.md](README.md) first** for user-facing module documentation and the repository root [CLAUDE.md](../../CLAUDE.md) for project-wide rules.

## Documentation Maintenance

Core's `CryptographicDefinitions.Keys` provides immutable `KeyInfo` on key objects. Keep the explicit
internal `KeyTypeEncoding` mappings and independent protocol values; catalog representation or algorithm applicability
never implies PIV operation support or broadens its supported curves. See
[Core catalog](../Core/README.md#cryptographic-metadata).

When working on this module:

- Update both `CLAUDE.md` and `README.md` for notable API, behavior, test, or security-pattern changes.
- Keep examples async and source-backed.
- Do not add examples that require User Presence, reset, PIN/PUK changes, or persistent-state mutation unless clearly marked as human-coordinated.

## Module Context

The PIV module implements YubiKey PIV smart-card operations through a single public `PivSession` facade and the `IPivSession` contract.

Current structure:

- `PivSession.cs` - public facade, lifecycle, authentication state, user-presence notification, and one-hop delegation.
- `IPivSession.cs` - public session contract.
- `IYubiKeyExtensions.cs` - `IYubiKey.CreatePivSessionAsync(...)` convenience creation.
- `Authentication/` - PIN, PUK, and management-key protocol helpers.
- `Metadata/` - read-only PIN/PUK/management-key/slot metadata and serial parsing.
- `DataObjects/` - PIV data-object GET/PUT helpers.
- `Certificates/` - certificate storage and retrieval helpers.
- `Keys/` - key generation, import, move, delete, and attestation helpers.
- `Cryptography/` - sign/decrypt/key-agreement helpers.
- `Bio/` - biometric and temporary-PIN helpers.

## Session Shape

Prefer the extension method when starting from an `IYubiKey`:

```csharp
await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);
```

Use direct creation only when a test or lower-level caller already owns the connection:

```csharp
await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
await using var session = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);
```

Keep public session methods easy to trace:

```text
PivSession public method
  -> validate arguments and session state
  -> ensure feature support when firmware-gated
  -> call one shallow internal protocol helper
  -> helper builds visible APDU/TLV payload
  -> transmit through Core SmartCard protocol
  -> parse response
  -> zero sensitive source/intermediate buffers in finally
```

User-presence notification is configured only through
`SessionCreationOptions.UserPresencePrompt`; the removed alpha `OnTouchRequired` property has no
compatibility adapter. Private-key operations map `Always` to `PolicyRequires`, `Cached` to
`PolicyMayRequire`, and leave `Never`, `Default`, and empty slots silent. Unknown or unavailable
metadata maps conservatively to `PolicyMayRequire`. Requests occur immediately before the
cryptographic APDU and are resolved after its successful response has been parsed, before transferring
the parsed output or performing local RSA padding removal, with `CancellationToken.None`. Malformed
success responses therefore resolve as `Failed`, and raw or untransferred parsed output is cleared. PIV does not report
`TimedOut` because these exchanges provide no confirmed touch-timeout status.

## Critical Security Requirements

Raw APDU responses have a Core-owned `Clear()` operation. Internal crypto/data-object results stay in owned
`byte[]` buffers until the public session transfers `Memory<byte>` to the caller. Never cast read-only memory
writable to perform cleanup. Clear only buffers owned by this scope; caller inputs and static defaults remain
borrowed and unmodified.

PIV handles PINs, PUKs, management keys, private keys, and cryptographic operation payloads.

- Zero PINs, PUKs, management keys, and encoded sensitive APDU payloads with `CryptographicOperations.ZeroMemory()`.
- Use `ReadOnlyMemory<byte>` or `Memory<byte>` when sensitive data crosses async boundaries.
- Prefer `Span<byte>`/`stackalloc` for small synchronous temporary buffers.
- Use `ArrayPool<byte>.Shared.Rent()` with `try/finally` return for larger temporary buffers.
- Never log PINs, PUKs, management keys, private keys, plaintexts, or signatures. Log metadata only.
- Do not store privately cloned sensitive `byte[]` values in structs.

Application-dependent lifecycle fragment: the caller supplies the real credential acquisition callback.
Keep its buffer alive through authentication and clear it before returning it to the pool.

```csharp
static async Task AuthenticateWithStoreAsync(
    IPivSession session,
    Action<Memory<byte>> loadManagementKey,
    CancellationToken cancellationToken)
{
    // This example store contract supplies a 24-byte Triple-DES or AES-192 active key.
    byte[] managementKey = ArrayPool<byte>.Shared.Rent(24);
    try
    {
        loadManagementKey(managementKey.AsMemory(0, 24));
        await session.AuthenticateAsync(managementKey.AsMemory(0, 24), cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(managementKey.AsSpan(0, 24));
        ArrayPool<byte>.Shared.Return(managementKey, clearArray: true);
    }
}
```

## Flat-Flow Rules

- Do not introduce operation-specific command classes such as `GenerateKeyCommand`, `VerifyPinCommand`, `GetDataCommand`, or `SignCommand`.
- Do not hide APDU construction behind broad executor/helper layers.
- Internal helpers are appropriate when they are feature-local, shallow, and keep payload shape inspectable.
- Keep `PivSession` as the public facade; do not split it into partial classes just to create architectural symmetry.
- Prefer module-local helpers until reuse with another module is proven.

Allowed helper pattern:

```text
PivSession.GenerateKeyAsync
  -> KeyProtocol.GenerateKeyAsync
  -> builds key-generation TLV/APDU visibly
  -> parses returned public key
```

## Current Public Operations

Common async operations include:

- `VerifyPinAsync(...)`
- `ChangePinAsync(...)`
- `ChangePukAsync(...)`
- `UnblockPinAsync(...)`
- `AuthenticateAsync(...)`
- `SetManagementKeyAsync(...)`
- `GenerateKeyAsync(...)`
- `ImportKeyAsync(...)`
- `MoveKeyAsync(...)`
- `DeleteKeyAsync(...)`
- `GetCertificateAsync(...)`
- `StoreCertificateAsync(...)`
- `DeleteCertificateAsync(...)`
- `SignOrDecryptAsync(...)`
- `CalculateSecretAsync(...)`
- `AttestKeyAsync(...)`
- `ResetAsync(...)`
- `SetPinAttemptsAsync(...)`

Check `IPivSession.cs` before adding or documenting public surface.

Key generation and import keep required domain inputs positional while policy modifiers live in
`KeyCreationOptions`. User-verification mode uses `UserVerification`; certificate storage uses
`CertificateCompression` instead of mode-switch booleans.

## Test Infrastructure

Unit tests should use fake SmartCard protocol/connection seams where possible to assert APDU/TLV bytes and parser behavior without hardware.
User-presence tests use a recording `IUserPresencePrompt` and fake SmartCard responses; they assert
policy basis, PIV slot scope, request/resolution pairing, callback failure behavior, and that
metadata already loaded by an operation is not fetched again.

`PivPinOnlyWorkflowTests` exercises a real protected-mode enable → connection close → recovery → disable
cycle with a randomized management key, verifies default-key authentication afterward, and resets PIV in
bounded independent cleanup. It requires an allowlisted dedicated key, but no physical touch ceremony.

Integration tests must use `[Theory]` plus `[WithYubiKey]` from `Tests.Shared`:

```csharp
[SkippableTheory]
[WithYubiKey(Capability = DeviceCapabilities.Piv, MinFirmware = "5.3.0")]
public async Task GetMetadata_ReadOnly_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    var metadata = await session.GetPinMetadataAsync();
    Assert.InRange(metadata.RetriesRemaining, 0, metadata.TotalRetries);
}
```

PIV reset, PIN/PUK changes, management-key changes, key generation/import/delete, certificate writes, and retry-counter manipulation mutate persistent applet state. That is expected: an allow-listed device is a dedicated test device, and the allow list is the authorization boundary. Run them.

What still needs a human is presence and timing, not destruction — touch-policy ceremonies and physical insert/remove. Mark those `Category=RequiresUserPresence` so `--smoke` excludes them. See [docs/TESTING.md](../../docs/TESTING.md#hardware-authorization) for the canonical policy.

## Firmware And Feature Gates

Helpers obtain metadata-only category loggers from `YubiKitLogging`, not logger parameters. `KeyTypeEncoding`
is the sole PIV wire-ID table; its frozen inverse is derived once. Request encoders enforce management vs
asymmetric subsets, so shared Core `KeyType` does not permit AES slot generation or P-521 requests. Management
key lengths come from `KeyInfo.KeySizeBytes`; EC points use `Curve.CoordinateLengthBytes`, private scalars/seeds
use `PrivateValueLengthBytes`, and ordinary EC curve identity comes from `Curve.NamedCurveOid`. Core's new
secp256k1/Brainpool identities remain unsupported by PIV generation/import/agreement until separately modeled
in the protocol map and capability rules. `KeyReference` holds only internal PIN/PUK,
management, and Bio references, separate from public asymmetric `Slot` values.

PIN/PUK parameters are named by role and carry caller-owned UTF-8 bytes, not ASCII-only credentials. Internal
working copies are cleared; shared factory-default key slices are borrowed read-only and never cleared.
Bio metadata uses tagged fields (retries 06, configured 07, temporary PIN 08); MATCH-policy probes check
capability, not fingerprint enrollment. Bio verification modes use the Python-confirmed APDU payloads.
No Bio-hardware coverage is implied by fake-protocol tests.

Use `EnsureSupports(...)` / `IsSupported(...)` from the session base instead of duplicating version checks. Be careful to distinguish device firmware from applet-reported firmware when beta hardware reports sentinel applet versions.

`Features` is a public standalone PIV catalog; `IsSupported` stays public and `EnsureSupports` is protected.
`Slot` replaces `PivSlot`; shared Core `KeyType` replaces `PivAlgorithm`. Only internal `KeyTypeEncoding`
maps Core key types to protocol bytes. Never cast `KeyType` to byte. Metadata preserves unknown algorithm
identifiers through `RawAlgorithmId`, nullable `KeyType`, and complete `RawData`; malformed required fields
still fail. The README records source-pinned support boundaries and Android/Python differences.

The session owns key/policy gates, management-authentication prerequisites, PIN-only workflows, and retry
fallback selection. Helpers take neither authentication booleans nor callbacks into the session. Metadata
fallback is limited to instruction-unavailable compatibility responses; cancellation, malformed TLVs, and
other device errors propagate. No-data VERIFY success without a retry count is explicitly unavailable,
never an invented count of three. PIN-only candidate rejection is specifically `0x6982`; other errors
do not initiate restoration traffic. Initialization and reset share management-key-type discovery using
detected firmware, independent of compatibility overrides.

## Known Gotchas

1. **Management key first**: key generation/import and some metadata changes require management-key authentication.
2. **PIN verification persists by session**: once verified, PIN state may remain until failure or disposal.
3. **Touch policy timing**: cached touch policy caches for a short window after first touch.
4. **Certificate size limits**: PIV certificates may need compression or careful object sizing.
5. **Default credentials**: never assume defaults are acceptable outside explicit test/reset flows.
6. **Retry counters**: PIN/PUK blocking is persistent; use only explicitly authorized dedicated devices. Physical presence and timing still require human coordination.
7. **PIN-only transitions**: enabling must authenticate the supplied active management key before PIN verification or writes; disabling restores the type-appropriate default key before deleting PRINTED, then ADMIN DATA.
8. **Mixed PIN-only recovery**: a returned success flag must match the session's final authenticated candidate; restore an earlier protected-key success after a stale derived candidate fails, or return no success.
9. **Management-key state**: `IsManagementKeyAuthenticated` reports PIV management-key authentication, distinct from the inherited protocol/SCP `IsAuthenticated`; successful SET updates the type and preserves card-session authentication without retaining key bytes; SET status `0x6982` clears recorded authentication while unrelated command failures preserve authentication and type; every failed authentication attempt and disposal clears prior authentication; initialization and successful RESET share the same conservative default-type fallback when metadata is unavailable, and RESET clears authentication before refresh.
10. **Reset boundaries**: normal PIV reset retains the F9 attestation key/certificate. PIN/PUK blocking occurs before RESET, so failure can consume retries; metadata refresh can fail after RESET has applied. Preserve the unfinished Bio preflight and route Bio MPE users to the product's device-reset procedure.
11. **Provisioning shapes**: typed CHUID/CCC readers require the SDK's fixed shape, while raw object access supports inspection of other encodings. ADMIN timestamps currently use trimmed little-endian Unix seconds, differing from Python's fixed big-endian field; preserve that interoperability follow-up.
12. **Evidence scope**: test names are not cryptographic oracles. Distinguish key generation/output shape, independent host verification, attestation trust and key binding. Hardware qualification is specific to device, firmware, transport and OS.

## Related Modules

- **Core.SmartCard**: APDU protocol, SCP, TLV utilities.
- **Core.Cryptography**: key material and algorithm helpers.
- **Tests.Shared**: YubiKey hardware filtering and integration helpers.
- **Management**: device information and firmware source of truth.
