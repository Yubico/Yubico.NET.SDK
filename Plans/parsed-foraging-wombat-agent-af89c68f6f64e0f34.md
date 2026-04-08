# Implementation Plan: Fix 3 Bugs (TLV Parse, Fingerprint Test, HsmAuth TLV Order)

**Date:** 2026-04-02
**Branch:** `yubikit-applets`
**Scope:** 3 bug fixes across Core, OpenPGP, and YubiHSM modules

---

## Bug 1: OpenPGP GetAlgorithmInformation TLV Parse Crash on FW 5.4.3

### Root Cause Analysis

The `Tlv.ParseData()` method at `src/Core/src/Utils/Tlv.cs:221` uses `if (length > 0x80)` to detect multi-byte length encoding. In BER-TLV:

- `0x00-0x7F`: short form (value IS the length)
- `0x80`: indefinite length (NOT a valid determinate length)
- `0x81-0xFF`: long form marker (lower 7 bits = number of subsequent length bytes)

When FW 5.4.3 returns a TLV with length byte `0x80`, the parser falls through the `> 0x80` check, treats it as short-form value 128, then tries to read 128 bytes from a buffer that does not have that many bytes remaining, causing `ArgumentOutOfRangeException`.

### Recommended Approach: Dual Fix (TLV parser hardening + application-level fallback)

**Why both:** The TLV parser has a genuine BER-TLV compliance bug (treating 0x80 as value 128 is incorrect per ISO 8825-1). Fixing this makes all callers safer. The application-level fallback matches ykman's proven resilience pattern for firmware quirks.

#### Step 1: Fix `Tlv.ParseData()` to reject indefinite length (0x80)

**File:** `src/Core/src/Utils/Tlv.cs` line 221

**Current:**
```csharp
if (length > 0x80)
```

**Change to:**
```csharp
if (length == 0x80)
{
    throw new ArgumentException("Indefinite length encoding (0x80) is not supported in determinate-length TLV parsing.");
}

if (length > 0x80)
```

**Rationale:** The class documentation already says "This class handles BER-TLV encoded data with determinate length." Indefinite length (0x80) is explicitly out of scope. Throwing immediately gives a clear error message instead of a confusing `ArgumentOutOfRangeException` downstream. This does NOT change behavior for any valid input -- it only changes the error for the specific 0x80 case from a downstream crash to an immediate, descriptive exception.

**Risk assessment:** Search of all `ParseData` and `Tlv.Create` callers (18 files) shows no caller expects 0x80 indefinite length. The `Encode` method (line 98) already uses `Length < 0x80` for short form, meaning it would never produce a 0x80 length byte for a 128-byte value (it would encode as `0x81 0x80`). So this change does not break any existing round-trip behavior.

#### Step 2: Add try-catch fallback in `GetAlgorithmInformationAsync`

**File:** `src/OpenPgp/src/OpenPgpSession.Config.cs` lines 104-108

**Current:**
```csharp
var rawData = await GetDataCoreAsync(DataObject.AlgorithmInformation, cancellationToken)
    .ConfigureAwait(false);
using var outerTlv = Tlv.Create(rawData.Span);
var innerSpan = outerTlv.Value.Span;
```

**Change to (matching ykman pattern at openpgp.py:1379-1382):**
```csharp
var rawData = await GetDataCoreAsync(DataObject.AlgorithmInformation, cancellationToken)
    .ConfigureAwait(false);

ReadOnlySpan<byte> innerSpan;
Tlv? outerTlv = null;
try
{
    outerTlv = Tlv.Create(rawData.Span);
    innerSpan = outerTlv.Value.Span;
}
catch (ArgumentException)
{
    // Firmware may return malformed TLV (e.g. indefinite length 0x80).
    // Match ykman fallback: pad with two zero bytes, re-parse, trim the padding.
    Span<byte> padded = stackalloc byte[rawData.Length + 2];
    rawData.Span.CopyTo(padded);
    padded[rawData.Length] = 0;
    padded[rawData.Length + 1] = 0;

    outerTlv = Tlv.Create(padded);
    // Trim the 2 padding bytes from the parsed value
    var fullValue = outerTlv.Value.Span;
    innerSpan = fullValue[..^2];
}
```

**Note:** The `outerTlv` must be disposed. Wrap in a `try-finally` or adjust the existing `using` scope. The implementer should ensure the `outerTlv` variable is disposed properly in all paths.

#### Step 3: Add unit test for 0x80 length byte

**File:** `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utils/TlvTests.cs`

Add a test:
```csharp
[Fact]
public void ParseData_IndefiniteLength0x80_ThrowsArgumentException()
{
    // Tag 0x5A, length 0x80 (indefinite — not supported)
    byte[] data = [0x5A, 0x80];
    Assert.Throws<ArgumentException>(() => Tlv.Create(data));
}
```

#### Step 4: Add unit test for exactly 128 bytes (valid long form)

Verify that a legitimate 128-byte value (encoded as `0x81 0x80`) still parses correctly. The existing test at line 37-49 tests 130 bytes. Add one for exactly 128:

```csharp
[Fact]
public void Create_LongFormLength_128Bytes_RoundTrips()
{
    var value = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();
    using var tlv = new Tlv(0x5A, value);
    var encoded = tlv.AsSpan();

    // Should encode as 0x5A 0x81 0x80 <128 bytes>
    Assert.Equal(0x5A, encoded[0]);
    Assert.Equal(0x81, encoded[1]);
    Assert.Equal(0x80, encoded[2]);

    // Round-trip via Create
    using var parsed = Tlv.Create(encoded);
    Assert.Equal(0x5A, parsed.Tag);
    Assert.Equal(128, parsed.Length);
    Assert.True(parsed.Value.Span.SequenceEqual(value));
}
```

---

## Bug 2: OpenPGP Fingerprint Test Assumption

### Analysis

The test `GetFingerprints_DefaultState_AllZero` at `OpenPgpSessionTests.cs:410-424` uses `resetBeforeUse: true`. Looking at `OpenPgpTestStateExtensions.cs`, when `resetBeforeUse` is true:

1. Creates a session, calls `ResetAsync()`, disposes that session
2. Creates a fresh session, runs the test action

The `ResetAsync()` method (`OpenPgpSession.Reset.cs`) performs: block PINs -> TERMINATE -> ACTIVATE -> re-SELECT -> refresh cache. This should fully reset the applet state including fingerprints.

**Key finding:** `GetAlgorithmInformationAsync` is NOT called during session initialization. The session init only does: SELECT -> GET VERSION -> cache ApplicationRelatedData. So Bug 1 cannot cascade to cause Bug 2.

### Investigation Steps During Implementation

1. **Run the test in isolation on FW 5.4.3:**
   ```bash
   dotnet build.cs -- test --integration --project OpenPgp --filter "GetFingerprints_DefaultState_AllZero"
   ```

2. **If it passes in isolation but fails in suite:** The issue is test ordering / shared device state. xUnit does not guarantee test ordering within a class, but tests share the device. A prior test that crashes mid-operation (perhaps due to Bug 1 in another test like `GetAlgorithmInformation`) could leave the device in a bad state where RESET doesn't work properly.

3. **If it fails even in isolation:** The issue is with `ResetAsync()` on FW 5.4.3. Add diagnostic logging:
   - After `ResetAsync()`, read back fingerprints and log them
   - Check if TERMINATE + ACTIVATE on 5.4.3 actually clears fingerprint DOs

4. **If the root cause is test ordering due to Bug 1:** Fixing Bug 1 first may resolve this automatically. Run the full OpenPGP integration suite after fixing Bug 1 to check.

### Recommended Approach

**Priority: Fix Bug 1 first, then re-test.** If Bug 2 persists after Bug 1 is fixed:

- Add a defensive re-read after reset in the test helper to verify state is clean
- Or add `[Collection("OpenPgpSerial")]` to prevent test parallelism within the OpenPGP test class (though xUnit theory tests in a single class are already sequential)

If `ResetAsync()` on 5.4.3 genuinely does not clear fingerprints, the fix is to make the test firmware-aware:

```csharp
// If reset doesn't clear fingerprints on this firmware, skip assertion
if (session.FirmwareVersion < someThreshold)
{
    // Verify fingerprints are present but don't assert all-zero
    Assert.NotNull(fingerprints);
    return;
}
```

But this should only be done after confirming the root cause. The most likely scenario is that Bug 1 causes cascade failures.

---

## Bug 3: HsmAuth Admin Password Change TLV Ordering

### Confirmed Root Cause

**ykman (`yubikit/hsmauth.py:545-552`):**
```python
data = (
    Tlv(TAG_LABEL, _parse_label(label))          # 0x71
    + Tlv(TAG_MANAGEMENT_KEY, management_key)     # 0x7B
    + Tlv(TAG_CREDENTIAL_PASSWORD, ...)           # 0x73
)
```

**.NET (`HsmAuthSession.cs:781-786`) -- WRONG:**
```csharp
new Tlv(TagManagementKey, managementKey.Span),   // 0x7B  <-- should be second
new Tlv(TagLabel, labelBytes),                    // 0x71  <-- should be first
new Tlv(TagCredentialPassword, newPwBytes)         // 0x73
```

**Cross-reference with other methods:**
- `PutCredentialSymmetricAsync` (line 317): `[ManagementKey, Label, ...]` -- but ykman's `put_credential` also uses `[ManagementKey, Label, ...]` so this is CORRECT
- `DeleteCredentialAsync` (line 388): `[ManagementKey, Label]` -- matches ykman
- `ChangeCredentialPasswordAsync` (user, line 739): `[Label, CurrentPw, NewPw]` -- matches ykman
- **`ChangeCredentialPasswordAdminAsync` (line 781): `[ManagementKey, Label, NewPw]` -- WRONG, should be `[Label, ManagementKey, NewPw]`**

The admin password change is the ONLY outlier.

### Fix

**File:** `src/YubiHsm/src/HsmAuthSession.cs` lines 781-786

**Change from:**
```csharp
var data = TlvHelper.EncodeList(
[
    new Tlv(TagManagementKey, managementKey.Span),
    new Tlv(TagLabel, labelBytes),
    new Tlv(TagCredentialPassword, newPwBytes)
]);
```

**Change to:**
```csharp
var data = TlvHelper.EncodeList(
[
    new Tlv(TagLabel, labelBytes),
    new Tlv(TagManagementKey, managementKey.Span),
    new Tlv(TagCredentialPassword, newPwBytes)
]);
```

This is a one-line swap. No other changes needed.

---

## Implementation Sequence

1. **Bug 1 (TLV parser + fallback)** -- highest priority, may resolve Bug 2
   - Fix `Tlv.cs:221` to reject 0x80
   - Add try-catch fallback in `OpenPgpSession.Config.cs`
   - Add unit tests in `TlvTests.cs`
   - Run: `dotnet build.cs test` (all unit tests must pass)

2. **Bug 3 (HsmAuth TLV order swap)** -- simple, independent
   - Swap lines in `HsmAuthSession.cs:782-783`
   - Run: `dotnet build.cs test` (all unit tests must pass)

3. **Bug 2 (Fingerprint test)** -- investigate after Bug 1 fix
   - Run OpenPGP integration tests on FW 5.4.3
   - If fingerprint test passes, done
   - If not, follow investigation steps above

## Verification Strategy

### Unit Tests (no hardware needed)
```bash
dotnet build.cs test
```
All 9 unit test projects must pass with 0 failures. Specifically:
- `Yubico.YubiKit.Core.UnitTests` -- new TLV tests for 0x80 rejection and 128-byte round-trip
- `Yubico.YubiKit.YubiHsm.UnitTests` -- existing tests unaffected

### Integration Tests (requires YubiKey 5.4.3)
```bash
# OpenPGP suite -- should go from 25/28 to 28/28
dotnet build.cs -- test --integration --project OpenPgp

# Specifically test the fixed methods
dotnet build.cs -- test --integration --project OpenPgp --filter "GetAlgorithmInformation"
dotnet build.cs -- test --integration --project OpenPgp --filter "GetFingerprints"

# HsmAuth -- cannot verify on alpha 5.8.0 (FW doesn't support INS 0x0B)
# Will need production 5.8.0 firmware to verify Bug 3
dotnet build.cs -- test --integration --project YubiHsm
```

### Regression Check
```bash
# Full unit test run
dotnet build.cs test

# PIV and OATH integration (should remain unaffected)
dotnet build.cs -- test --integration --project Piv
dotnet build.cs -- test --integration --project Oath
```

---

## Risk Assessment

| Change | Risk | Mitigation |
|--------|------|------------|
| TLV parser 0x80 rejection | Low | Only changes behavior for invalid input (0x80 as length); all valid encodings unaffected |
| GetAlgorithmInfo fallback | Low | Only triggers on parse failure; normal path unchanged |
| HsmAuth TLV order swap | Low | Matches canonical ykman; cannot test on current hardware (need FW 5.8.0 production) |
| Fingerprint test | Unknown | May self-resolve after Bug 1 fix; needs investigation |

