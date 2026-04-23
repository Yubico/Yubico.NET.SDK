# Phase 9.6 — `WithCredBlob` Length Validation Hardening

**Created:** 2026-04-23 (late session)
**Status:** Tracker — non-blocking, post-PR-#466
**Owner:** TBD
**Source:** Phase 9.4 DevTeam Ship Reviewer finding (2026-04-23)
**Priority:** Low (DX improvement, not a security or correctness bug)

## Context

While shipping Phase 9.4 unit-test coverage gaps, the new `Build_WithCredBlobOversized_AllowsOversizedInput` test discovered that `ExtensionBuilder.WithCredBlob(ReadOnlyMemory<byte> blob)` at `src/Fido2/src/Extensions/ExtensionBuilder.cs:76-80` performs **no length validation**:

```csharp
public ExtensionBuilder WithCredBlob(ReadOnlyMemory<byte> blob)
{
    _credBlob = blob;
    return this;
}
```

The doc comment at line 74 mentions "max 32 bytes typically" but enforces nothing. CTAP 2.1 §11.1 limits credBlob to 32 bytes (or `maxCredBlobLength` from `AuthenticatorInfo`, ≥32). When a caller passes an oversized blob, the SDK silently builds the request; the YubiKey then rejects it with `CTAP2_ERR_INVALID_LENGTH`, surfacing as a generic CTAP exception rather than a clean `ArgumentException` at the SDK boundary.

This is **DX debt**, not a security or correctness defect:
- The wrong outcome (request rejected) IS achieved
- But the error surface is poor — the caller learns about the limit only after a round-trip to the device
- A clean `ArgumentOutOfRangeException` at the builder call site would let callers discover the constraint without device interaction

## Proposed change

Add length validation in `ExtensionBuilder.WithCredBlob`:

```csharp
public ExtensionBuilder WithCredBlob(ReadOnlyMemory<byte> blob)
{
    if (blob.Length > MaxCredBlobLength)
    {
        throw new ArgumentOutOfRangeException(
            nameof(blob),
            blob.Length,
            $"credBlob length must not exceed {MaxCredBlobLength} bytes (CTAP 2.1 §11.1).");
    }

    _credBlob = blob;
    return this;
}

private const int MaxCredBlobLength = 32;
```

**Better still** (if the API can carry it): take `AuthenticatorInfo.MaxCredBlobLength` from a constructor or a context parameter, since the spec allows authenticators to advertise larger limits.

## Side effect

The current `Build_WithCredBlobOversized_AllowsOversizedInput` test in `ExtensionBuilderTests.cs:193` would need to be either:
- Renamed to `Build_WithCredBlobOversized_ThrowsArgumentOutOfRange` and rewritten to assert the throw, OR
- Replaced by a new positive test (`Build_WithCredBlob32Bytes_Succeeds`) plus a negative test (`Build_WithCredBlob33Bytes_ThrowsArgumentOutOfRange`)

Either way, the new test name will reflect the new behavior.

## Out of scope for this tracker

- Other extension input length validation (e.g., `WithMinPinLength` boundary checks) — file separately if those have similar gaps
- Authenticator-info-driven dynamic limits (`maxCredBlobLength`) — could extend this work but would expand scope

## Unblocking criteria

- A contributor with bandwidth to make a small Fido2 production change + corresponding test update
- Or: a future bug-hunt cycle that wants to harden the SDK boundary

## Closes

When validation lands and the corresponding `Build_WithCredBlob*` test is updated, this tracker can be deleted.
