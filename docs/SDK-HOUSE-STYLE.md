# SDK House Style

This document defines the intended v2 SDK style. It is the working standard for module consolidation and future refactors.

The goal is not to make every module abstract in the same way. The goal is to make every module readable in the same way.

## Core Principles

- Keep protocol flow visible.
- Prefer flat code over deep helper chains.
- Prefer plain data carriers over operation-specific command objects.
- Use shared Core primitives instead of reimplementing transport, APDU, TLV, logging, or security infrastructure.
- Extract helpers only when they make the session method easier to read without hiding the wire behavior.
- Do not penalize essential protocol complexity. Remove accidental complexity.

## Flat Protocol Flow

Most public session methods should follow this rhythm:

```text
Session method
  -> validate inputs
  -> ensure feature support
  -> build payload/APDU/DTO visibly
  -> transmit through Core protocol/backend
  -> parse response
  -> zero sensitive buffers in finally
```

This is preferred:

```csharp
public async Task<KeyInfo[]> GetKeyInfoAsync(CancellationToken cancellationToken = default)
{
    ThrowIfDisposed();

    var command = new ApduCommand(
        cla: 0x80,
        ins: Ins.GetData,
        p1: 0x00,
        p2: 0x00,
        data: GetKeyInfoTag);

    var response = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);

    return ParseKeyInfo(response.Data.Span);
}
```

This is discouraged:

```csharp
var command = new GetKeyInfoCommand();
return await command.ExecuteAsync(_protocol, cancellationToken).ConfigureAwait(false);
```

## What We Avoid

Do not introduce operation-specific command classes such as:

- `AuthenticateCommand`
- `PutKeyCommand`
- `GetDataCommand`
- `VerifyPinCommand`
- `GenerateKeyCommand`
- `CalculateCommand`

Avoid command classes that contain protocol logic, own execution, or require readers to jump across files to understand one APDU.

The v1 SDK made flows harder to follow by hiding behavior inside special command classes. v2 should preserve the lessons from the Android and Python SDKs: keep protocol code flat where flat code is clearer.

## Allowed Abstractions

Use these freely when they clarify behavior:

- Plain `ApduCommand` and `ApduResponse`.
- Plain CTAP/CBOR request or response DTOs when they are data carriers, not executors.
- Protocol constants and enums for INS, tags, slots, algorithms, policies, and status values.
- Small pure helpers such as `EncodeX`, `ParseY`, and `ValidateZ`.
- TLV builders/parsers and Core utility types.
- Fake protocol/test helpers that capture transmitted APDUs.
- Sensitive-buffer lifecycle wrappers, if the session method still makes the transmitted command visible.

Allowed helpers should be boring and local. They should not create a new architecture layer.

## Helper Depth

Helper depth should be minimal.

Prefer:

```text
Session method -> small private Encode/Parse helper
```

Avoid:

```text
Session method -> command object -> request builder -> executor -> response object -> parser service
```

A helper is justified when:

- The same protocol encoding appears repeatedly.
- The parse logic is independently testable.
- The helper has no side effects.
- The helper name states a protocol fact, not an architectural role.
- The session method remains understandable without opening the helper.

A helper is not justified when:

- It exists only to make modules look more layered.
- It hides which APDU or CTAP command is sent.
- It combines validation, encoding, transmission, parsing, and state mutation.

## Session Shape

Application modules should converge on this shape when possible:

- Public `IYubiKey` extension method creates the session.
- Session has a static `CreateAsync` factory.
- Session derives from `ApplicationSession` when it owns an applet/application protocol.
- Session accepts and propagates `CancellationToken` on async operations.
- Session owns protocol disposal consistently.
- Session uses `IsSupported(...)` / `EnsureSupports(...)` for firmware and feature gates.
- Session keeps public API operations readable and close to the wire behavior.

For multi-transport modules, a backend adapter can be appropriate when it removes transport branching from every operation. The backend should stay narrow and mechanical.

## SmartCard And APDU Style

SmartCard modules should use Core APDU infrastructure directly:

- Use plain `ApduCommand` for APDU construction.
- Keep CLA, INS, P1, P2, and payload shape visible in the session method or nearby private helper.
- Use Core `Tlv`, `TlvHelper`, `BerLength`, and APDU status utilities instead of local clones.
- Use app-specific INS and tag constants, preferably near the session or protocol model.
- Prefer private static `ParseX(ReadOnlySpan<byte>)` helpers for response parsing.
- Add fake-protocol tests for byte-level command behavior when hardware is not required.

Do not add a separate command-object hierarchy for SmartCard applets.

## CTAP And CBOR Style

FIDO2 and WebAuthn can use request builders or DTOs where CBOR structure is complex, but request construction should remain inspectable.

- Prefer one canonical CTAP request-building convention.
- Avoid hand-rolled CBOR copies when a shared pure helper already exists.
- Keep PIN/UV auth parameter construction explicit and zero sensitive buffers.
- Use data DTOs for request/response shape, not executor objects.
- WebAuthn should delegate CTAP/FIDO behavior to Fido2 and avoid duplicating protocol logic.

## Sensitive Data Style

Follow the root `CLAUDE.md` security rules as non-negotiable SDK practice:

- Zero PINs, PUKs, management keys, SCP material, tokens, credential secrets, and secret-derived buffers with `CryptographicOperations.ZeroMemory()`.
- Use `CryptographicOperations.FixedTimeEquals()` for secret-derived comparisons.
- Do not log PINs, keys, credentials, challenge-response secrets, or sensitive payloads.
- Use `Span<byte>` and `stackalloc` for small synchronous buffers.
- Use `ArrayPool<byte>.Shared.Rent()` with `try/finally` for larger temporary buffers.
- Use `Memory<byte>` / `ReadOnlyMemory<byte>` for data that crosses async boundaries.
- Avoid `.ToArray()` unless data must escape the current scope.
- Do not store a privately cloned sensitive `byte[]` in a struct.
- Use sealed disposable classes for owned sensitive buffers.

For sensitive APDU/CTAP payloads, the preferred lifecycle is:

```text
encode payload
transmit payload
zero encoded payload in finally
zero source/intermediate buffers in finally
```

If a public API accepts caller-owned sensitive data, document whether the SDK copies it and whether the caller must zero it.

## Error And Status Style

- Use Core APDU and CTAP status models where available.
- Convert retry status words into retry-aware exceptions or explicit retry metadata.
- Use `EnsureSupports(...)` for unsupported firmware/features instead of duplicating version checks.
- Do not use exceptions for ordinary control flow.
- Protocol-required probing via exceptions is acceptable only when the protocol provides no non-exception signal; keep it local and documented.
- Preserve cancellation. Do not swallow `OperationCanceledException` except at CLI/UI boundaries where it maps to a user-visible cancellation result.

## Testing Style

Tests should verify behavior that would catch real regressions.

- Prefer fake protocol/backend tests for exact APDU/CTAP bytes and parser behavior.
- Prefer integration tests only when real hardware behavior is essential.
- Do not write validation-only tests that only prove framework guard clauses work.
- Do not add skipped placeholder tests.
- Use `Tests.Shared` hardware filtering and traits for integration tests.
- Mark slow and user-presence tests with shared test categories.
- Keep destructive hardware tests behind explicit reset/setup helpers.

The best module tests make protocol flow safer without forcing command classes into production code.

## Documentation Style

Module documentation should match source reality.

- Keep module `CLAUDE.md` and README files aligned with actual APIs.
- Document module-specific security ownership rules.
- Document hardware reset/setup requirements for integration tests.
- Document protocol gotchas where they explain flat code that might otherwise look odd.
- Remove stale examples that refer to old v1 shapes or nonexistent APIs.

## Module Consistency Checklist

Before refactoring a module, check:

- Does the session expose a clear factory/extension creation path?
- Does the public method show validation, feature gate, command construction, transmit, parse, and cleanup?
- Are protocol constants named and local enough to read the method?
- Are helpers pure and shallow?
- Are sensitive buffers zeroed, including encoded command payloads?
- Are async methods accepting and propagating cancellation tokens?
- Are status/retry errors handled consistently?
- Are hardware-only behaviors tested through integration tests?
- Are byte-level protocol behaviors tested without hardware when feasible?
- Are README and module `CLAUDE.md` accurate?

## North Star

Same architectural rhythm, not more abstraction.

The SDK should feel like one developer with one plan wrote it, but that plan is deliberately flat: visible protocol flow, minimal helper depth, strong Core reuse, strong security hygiene, and tests that prove the bytes.
