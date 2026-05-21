# Plan: Fix 3 Failing FIDO2 Unit Tests

## Context

Three unit tests broke after recent production code changes that were never paired with test updates:

1. `AuthenticatorConfig.BuildCommandPayload()` was updated (commit `20d31cc9`) to use the CTAP 2.1 spec-compliant 34-byte authenticated message format `[32×0xff || 0x0D || subCommand]` instead of the old 2-byte format `[0xff || subCommand]`. The two AuthenticatorConfig tests still assert the old 2-byte format.

2. `ClientPin.GetPinUvAuthTokenUsingPinAsync()` was updated (commit `c4c591be`) to call `_session.GetInfoAsync()` first for CTAP2.0 fallback detection. The test mocks only `SendCborRequestAsync`, so `GetInfoAsync` returns `null` from NSubstitute, causing a NullReferenceException at line 342 when accessing `info.Options`.

All fixes are in **test files only** — production code is correct.

---

## Fix 1: Update AuthenticatorConfig test assertions

**File:** `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Config/AuthenticatorConfigTests.cs`

### `AuthenticatesOverCorrectMessage_EnableEnterpriseAttestation` (line 358)

Replace:
```csharp
Assert.Equal(2, capturedMessage.Length);
Assert.Equal(0xff, capturedMessage[0]); // Magic prefix
Assert.Equal(0x01, capturedMessage[1]); // EnableEnterpriseAttestation
```

With (CTAP 2.1 spec: 32×0xff || 0x0D || subCommand):
```csharp
Assert.Equal(34, capturedMessage.Length);
// First 32 bytes are 0xff
for (var i = 0; i < 32; i++) Assert.Equal(0xff, capturedMessage[i]);
Assert.Equal(0x0D, capturedMessage[32]); // CtapCommand.Config
Assert.Equal(0x01, capturedMessage[33]); // EnableEnterpriseAttestation
```

### `AuthenticatesOverCorrectMessage_ToggleAlwaysUv` (line 378)

Replace:
```csharp
Assert.Equal(2, capturedMessage.Length);
Assert.Equal(0xff, capturedMessage[0]); // Magic prefix
Assert.Equal(0x02, capturedMessage[1]); // ToggleAlwaysUv
```

With:
```csharp
Assert.Equal(34, capturedMessage.Length);
for (var i = 0; i < 32; i++) Assert.Equal(0xff, capturedMessage[i]);
Assert.Equal(0x0D, capturedMessage[32]); // CtapCommand.Config
Assert.Equal(0x02, capturedMessage[33]); // ToggleAlwaysUv
```

---

## Fix 2: Add GetInfoAsync mock to ClientPin test

**File:** `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Pin/ClientPinTests.cs`

### `GetPinUvAuthTokenUsingPinAsync_WithPermissions_ReturnsToken` (line 216)

Add before `SendCborRequestAsync` mock setup (after line 221):
```csharp
var authenticatorInfo = new AuthenticatorInfo
{
    Options = new Dictionary<string, bool> { { "pinUvAuthToken", true } }
};
_mockSession.GetInfoAsync(Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(authenticatorInfo));
```

This makes the test simulate a CTAP2.1 device that supports permission-based tokens, so the code takes the happy path instead of the legacy fallback.

---

## Critical Files

- `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Config/AuthenticatorConfigTests.cs` — lines 356–381
- `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Pin/ClientPinTests.cs` — line 220–225
- `src/Fido2/src/Config/AuthenticatorConfig.cs` — lines 177–185 (reference only, no changes)
- `src/Fido2/src/Pin/ClientPin.cs` — lines 338–347 (reference only, no changes)

---

## Verification

```bash
dotnet toolchain.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Fido2.UnitTests"
```

All 345 tests should pass. The three previously failing tests:
- `AuthenticatesOverCorrectMessage_EnableEnterpriseAttestation`
- `AuthenticatesOverCorrectMessage_ToggleAlwaysUv`
- `GetPinUvAuthTokenUsingPinAsync_WithPermissions_ReturnsToken`
