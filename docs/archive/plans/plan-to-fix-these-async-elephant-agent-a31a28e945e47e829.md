# Investigation: Failing AuthenticatorConfig Tests

## Summary
Two unit tests are failing because the production code was changed (commit 20d31cc9) to implement a different authentication message format than what the tests expect.

**Failing Tests:**
- `AuthenticatorConfigTests.AuthenticatesOverCorrectMessage_EnableEnterpriseAttestation`
- `AuthenticatorConfigTests.AuthenticatesOverCorrectMessage_ToggleAlwaysUv`

**Assertion:** Expected message length 2, Actual message length 34

---

## Root Cause Analysis

### What Changed in Commit 20d31cc9

The commit `20d31cc9` ("fix(fido2,openpgp,hsmauth,otp,core): fix SDK bugs discovered during integration test coverage") modified `src/Fido2/src/Config/AuthenticatorConfig.cs` line 177-185:

**OLD CODE (2-byte message):**
```csharp
// Build PIN/UV auth param over just the subcommand (0xff || subCommand)
Span<byte> message = stackalloc byte[2];
message[0] = 0xff; // Magic prefix for config command auth
message[1] = subCommand;
var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, message);
```

**NEW CODE (34-byte message):**
```csharp
// Build PIN/UV auth param: authenticate(pinUvAuthToken, 32*0xff || 0x0D || subCommand)
// Per CTAP 2.1 spec section 6.8
Span<byte> message = stackalloc byte[32 + 1 + 1];
message[..32].Fill(0xff);
message[32] = CtapCommand.Config; // 0x0D
message[33] = subCommand;
var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, message);
```

### What the Tests Expect

Both failing tests capture what message was authenticated:

**Test: `AuthenticatesOverCorrectMessage_EnableEnterpriseAttestation()` (line 344-361)**
```csharp
var capturedMessage = _testProtocol.LastAuthenticateMessage;
Assert.NotNull(capturedMessage);
Assert.Equal(2, capturedMessage.Length);           // ← EXPECTS 2
Assert.Equal(0xff, capturedMessage[0]);            // ← EXPECTS 0xff
Assert.Equal(0x01, capturedMessage[1]);            // ← EXPECTS 0x01 (EnableEnterpriseAttestation)
```

**Test: `AuthenticatesOverCorrectMessage_ToggleAlwaysUv()` (line 364-381)**
```csharp
var capturedMessage = _testProtocol.LastAuthenticateMessage;
Assert.NotNull(capturedMessage);
Assert.Equal(2, capturedMessage.Length);           // ← EXPECTS 2
Assert.Equal(0xff, capturedMessage[0]);            // ← EXPECTS 0xff
Assert.Equal(0x02, capturedMessage[1]);            // ← EXPECTS 0x02 (ToggleAlwaysUv)
```

The test mock (`TestPinUvAuthProtocol` at line 45-73) captures the message passed to `Authenticate()`:
```csharp
private sealed class TestPinUvAuthProtocol : IPinUvAuthProtocol
{
    public byte[]? LastAuthenticateMessage { get; private set; }
    
    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        LastAuthenticateMessage = message.ToArray();  // ← Captures what was passed
        return new byte[16];
    }
}
```

### Discrepancy

The production code now sends a **34-byte message** to `Authenticate()`:
- Bytes 0-31: 0xff (32 bytes)
- Byte 32: 0x0D (CtapCommand.Config)
- Byte 33: subCommand (0x01 or 0x02)

But the tests expect a **2-byte message**:
- Byte 0: 0xff
- Byte 1: subCommand

---

## Affected Code Locations

### Production Code
- **File:** `src/Fido2/src/Config/AuthenticatorConfig.cs`
- **Methods affected:**
  - `BuildCommandPayload()` (line 177) - affects:
    - `EnableEnterpriseAttestationAsync()` 
    - `ToggleAlwaysUvAsync()`
  - `BuildSetMinPinLengthPayload()` (line 207) - ALSO changed but not tested by failing tests

### Test Code
- **File:** `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Config/AuthenticatorConfigTests.cs`
- **Failing tests:**
  - Line 343: `AuthenticatesOverCorrectMessage_EnableEnterpriseAttestation()`
  - Line 364: `AuthenticatesOverCorrectMessage_ToggleAlwaysUv()`
- **Mock test protocol:**
  - Line 45-73: `TestPinUvAuthProtocol` captures authenticate calls

---

## Additional Changes

The same commit ALSO modified `BuildSetMinPinLengthPayload()` (line 247-255):

**NEW CODE:**
```csharp
// Build PIN/UV auth param: authenticate(pinUvAuthToken, 32*0xff || 0x0D || subCommand || subCommandParams)
// Per CTAP 2.1 spec section 6.8
var subCommand = ConfigSubCommand.SetMinPinLength;
var messageLength = 32 + 1 + 1 + subCommandParams.Length;
var message = new byte[messageLength];
message.AsSpan(0, 32).Fill(0xff);
message[32] = CtapCommand.Config; // 0x0D
message[33] = subCommand;
subCommandParams.CopyTo(message.AsMemory(34));
```

This is similar to the BuildCommandPayload change but adds the subCommandParams to the end of the message.

---

## Questions Needing Resolution

1. **Is the new 34-byte format correct per CTAP 2.1 section 6.8?**
   - The comment claims this is per spec, but needs verification
   - The old 2-byte format might have been intentional/correct

2. **Should the tests be updated to match the new behavior, or should the production code revert?**
   - If the new behavior is correct: Update test assertions
   - If the old behavior was correct: Revert the commit changes
   - If uncertain: Verify against actual CTAP 2.1 specification

3. **Are there other similar tests that might be affected?**
   - `SetMinPinLengthAsync_*` tests don't explicitly check the authenticated message length
   - Need to verify they still pass

---

## Test Infrastructure

**Test Framework:** xUnit v3
**Mock Library:** NSubstitute
**Key Test Classes:**
- `AuthenticatorConfig` - the class being tested
- `IFidoSession` - mocked interface for device communication
- `TestPinUvAuthProtocol` - mock PIN/UV auth protocol that captures authenticate() calls

The tests do NOT currently validate the authenticated message for `SetMinPinLengthAsync` variants.

---

## Next Steps (PLANNING STAGE)

To fix these tests, we need to:

1. **Clarify the correct behavior** - Verify CTAP 2.1 spec section 6.8 for `authenticatorConfig` command:
   - What should be authenticated for the PIN/UV auth param?
   - Is it the 2-byte (0xff || subCommand) or 34-byte (32*0xff || 0x0D || subCommand) format?

2. **If the new format (34 bytes) is correct:**
   - Update test assertions in both failing tests:
     - Line 358: Change `Assert.Equal(2, ...)` to `Assert.Equal(34, ...)`
     - Line 378: Change `Assert.Equal(2, ...)` to `Assert.Equal(34, ...)`
   - May need to update assertions for message[0] and message[1] 
   - Consider adding tests for `SetMinPinLengthAsync` authenticated message too

3. **If the old format (2 bytes) is correct:**
   - Revert the message building logic in `BuildCommandPayload()` and `BuildSetMinPinLengthPayload()`
   - Restore comments about the old 2-byte format
   - Verify integration tests still pass

---

## Git Commit Info

**Introduced in:** `20d31cc9`
```
commit 20d31cc937b46d01ba2932bb92c638883ddcb267
Author: Dennis Dyall <dennis.dyall@yubico.com>
Date:   (earlier date)

    fix(fido2,openpgp,hsmauth,otp,core): fix SDK bugs discovered during integration test coverage
```

**Modified in:** `3c38d2804f7f235cced34ad3e017db7ef15f3b1c` (only style changes, no logic)
```
commit 3c38d2804f7f235cced34ad3e017db7ef15f3b1c
Author: Dennis Dyall <dennis.dyall@yubico.com>
Date:   Wed Apr 15 09:42:24 2026 +0200

    fix(fido2): SHA256 API, auth tag zeroing, DisposeAsync, dead code
```
