# CLAUDE.md - PIV Module

This file provides Claude-specific guidance for working with the PIV module. **Read [README.md](README.md) first** for user-facing module documentation and the repository root [CLAUDE.md](../../CLAUDE.md) for project-wide rules.

## Documentation Maintenance

When working on this module:

- Update both `CLAUDE.md` and `README.md` for notable API, behavior, test, or security-pattern changes.
- Keep examples async and source-backed.
- Do not add examples that require User Presence, reset, PIN/PUK changes, or persistent-state mutation unless clearly marked as human-coordinated.

## Module Context

The PIV module implements YubiKey PIV smart-card operations through a single public `PivSession` facade and the `IPivSession` contract.

Current structure:

- `PivSession.cs` - public facade, lifecycle, authentication state, touch notification, and one-hop delegation.
- `IPivSession.cs` - public session contract.
- `IYubiKeyExtensions.cs` - `IYubiKey.CreatePivSessionAsync(...)` convenience creation.
- `Authentication/` - PIN, PUK, and management-key protocol helpers.
- `Metadata/` - PIN/PUK/management-key metadata and retry-attempt helpers.
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

## Critical Security Requirements

PIV handles PINs, PUKs, management keys, private keys, and cryptographic operation payloads.

- Zero PINs, PUKs, management keys, and encoded sensitive APDU payloads with `CryptographicOperations.ZeroMemory()`.
- Use `ReadOnlyMemory<byte>` or `Memory<byte>` when sensitive data crosses async boundaries.
- Prefer `Span<byte>`/`stackalloc` for small synchronous temporary buffers.
- Use `ArrayPool<byte>.Shared.Rent()` with `try/finally` return for larger temporary buffers.
- Never log PINs, PUKs, management keys, private keys, plaintexts, or signatures. Log metadata only.
- Do not store privately cloned sensitive `byte[]` values in structs.

Example lifecycle:

```csharp
byte[]? managementKey = ArrayPool<byte>.Shared.Rent(24);
try
{
    FillManagementKey(managementKey.AsSpan(0, 24));
    await session.AuthenticateAsync(managementKey.AsMemory(0, 24), cancellationToken);
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey.AsSpan(0, 24));
    ArrayPool<byte>.Shared.Return(managementKey, clearArray: true);
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
  -> PivKeyProtocol.GenerateKeyAsync
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

## Test Infrastructure

Unit tests should use fake SmartCard protocol/connection seams where possible to assert APDU/TLV bytes and parser behavior without hardware.

Integration tests must use `[Theory]` plus `[WithYubiKey]` from `Tests.Shared`:

```csharp
[Theory]
[WithYubiKey(Capability = DeviceCapabilities.Piv)]
public async Task GetMetadata_ReadOnly_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    var metadata = await session.GetPinMetadataAsync();
    Assert.NotNull(metadata);
}
```

PIV reset, PIN/PUK changes, management-key changes, key generation/import/delete, certificate writes, and retry-counter manipulation mutate persistent applet state. Agents must not run those integration tests unless a human explicitly approves hardware coordination and reset expectations.

## Firmware And Feature Gates

Use `EnsureSupports(...)` / `IsSupported(...)` from the session base instead of duplicating version checks. Be careful to distinguish device firmware from applet-reported firmware when beta hardware reports sentinel applet versions.

## Known Gotchas

1. **Management key first**: key generation/import and some metadata changes require management-key authentication.
2. **PIN verification persists by session**: once verified, PIN state may remain until failure or disposal.
3. **Touch policy timing**: cached touch policy caches for a short window after first touch.
4. **Certificate size limits**: PIV certificates may need compression or careful object sizing.
5. **Default credentials**: never assume defaults are acceptable outside explicit test/reset flows.
6. **Retry counters**: PIN/PUK blocking is persistent and human-coordinated.

## Related Modules

- **Core.SmartCard**: APDU protocol, SCP, TLV utilities.
- **Core.Cryptography**: key material and algorithm helpers.
- **Tests.Shared**: YubiKey hardware filtering and integration helpers.
- **Management**: device information and firmware source of truth.
