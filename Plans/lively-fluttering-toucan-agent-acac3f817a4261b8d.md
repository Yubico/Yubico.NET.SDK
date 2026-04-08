# Plan: Integration Tests for SecurityDomain, Management, and Cross-Module

## Summary

Write new integration test files for SecurityDomain and Management modules. Cross-module tests (PIV+FIDO2) cannot be compiled in the Management test project because it only references Management, Core, and Tests.Shared -- not PIV or FIDO2. Those tests will be documented as needing a separate cross-module test project.

## Files to Create

### 1. SecurityDomain: `SecurityDomainSession_Scp03KeyLifecycleTests.cs`

**Location:** `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03KeyLifecycleTests.cs`

**Tests:**

1. **`DeleteKeyAsync_AfterImport_RemovesKey`**
   - Session 1 (reset=true): Import custom SCP03 key at KVN 0x01 using `PutKeyAsync`
   - Session 2 (reset=false): Authenticate with new keys, verify key present in `GetKeyInfoAsync`, then delete via `DeleteKeyAsync`
   - Session 3 (reset=false): Verify key is gone from `GetKeyInfoAsync` (authenticate with default keys since after delete, default keys should be restored by reset logic -- actually, deleting non-default key means default still works)
   - MinFirmware: "5.4.3"
   - Note: Deleting a custom key doesn't restore defaults. After import, default key (KVN=0xFF) is replaced. So the flow is: reset (gives defaults), import at KVN=0x01 (new key), verify with new key, delete KVN=0x01, verify default keys work again.
   - Actually: `PutKeyAsync` with `replaceKvn=0` adds a NEW key alongside defaults. With `replaceKvn=0xFF` it replaces default. Let's import at KVN=0x01 with replaceKvn=0 to ADD alongside defaults.
   - Wait -- looking at the existing test `PutKeyAsync_WithStaticKeys_ImportsAndAuthenticates`, it does `PutKeyAsync(newKeyReference, newStaticKeys, 0, ...)` where `newKeyReference = new KeyReference(0x01, 0x01)`. Then default keys stop working. So the KVN=0x01 replaces the defaults.
   - For the delete test: import key, verify it exists, delete by KVN, verify gone. After delete, we need to be careful about what key we authenticate with for the verify session.
   - Safest approach: Session 1 (reset=true, default keys): import custom key at KVN=0x02 alongside defaults. Session 2 (reset=false, custom keys): get key info, verify KVN=0x02 exists, delete it. Session 3 (reset=false, no auth or default auth): verify KVN=0x02 is gone.
   - Actually, from the code: `PutKeyAsync` with keyReference KID=0x01 is required for SCP03. And replaceKvn=0 means "add new" while replaceKvn=N means "replace KVN N". When we import KVN=0x01 with replaceKvn=0, it adds alongside the default KVN=0xFF.
   - But the existing test shows that after importing KVN=0x01, default keys stop working. This might be because SCP03 selects by KID, not KVN, and there's key precedence.
   - Let me simplify: Follow the pattern from the existing test. Reset, import at KVN=0x01 (replacing nothing), then delete KVN=0x01, then verify key gone.

2. **`ReplaceKeyAsync_AtSameKvn_UpdatesKey`**
   - Session 1 (reset=true, default keys): Import key A at KVN=0x01
   - Session 2 (reset=false, key A): Replace with key B at KVN=0x01 (using replaceKvn=0x01)
   - Session 3 (reset=false): Verify old key A doesn't work, key B does
   - MinFirmware: "5.4.3"

### 2. SecurityDomain: `SecurityDomainSession_Scp11cTests.cs`

**Location:** `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11cTests.cs`

**Tests:**

1. **`Scp11c_Authenticate_Succeeds`**
   - MinFirmware: "5.7.2"
   - Use the same `LoadKeys` pattern from Scp11Tests but with `ScpKid.SCP11c`
   - Session 1 (reset=true, SCP03 default): Generate key and load OCE certs for SCP11c
   - Session 2 (reset=false, SCP11c params): Verify `session.IsAuthenticated` is true
   - Reuse the test data helpers from `Scp11TestData` and the `LoadKeys` method pattern

### 3. SecurityDomain: `SecurityDomainSession_NegativeTests.cs`

**Location:** `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_NegativeTests.cs`

**Tests:**

1. **`Scp11a_BlockedAllowList_RejectsAuthentication`**
   - MinFirmware: "5.7.2"
   - Session 1 (reset=true): Set up SCP11a keys and store an allowlist with fake serials
   - Session 2 (reset=false): Attempt SCP11a auth with cert whose serial is NOT on the allowlist
   - Expect `ApduException` or similar
   - This is tricky -- we need a cert serial that differs from what's on the allowlist. The existing `Scp11a_WithAllowList_AllowsApprovedSerials` test stores specific serials. We can store a list with serials that DON'T match our OCE cert.

2. **`Scp11b_WrongPublicKey_ThrowsException`**
   - MinFirmware: "5.7.2"
   - Session 1 (reset=true): Generate SCP11b key
   - Session 2 (reset=false): Try to authenticate with a DIFFERENT (randomly generated) public key
   - Expect exception (ApduException or similar)

### 4. Management: `ManagementSessionCapabilityTests.cs`

**Location:** `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionCapabilityTests.cs`

**Tests:**

1. **`GetDeviceInfo_ShowsAllCapabilities`**
   - Use `[WithYubiKey]` with no filter
   - Get device info, verify UsbSupported/UsbEnabled/NfcSupported/NfcEnabled are populated
   - Verify UsbEnabled is a subset of UsbSupported
   - Verify NfcEnabled is a subset of NfcSupported (if NFC supported)

2. **`SetEnabledCapabilities_DisableOath_ThenReenable`**
   - This is a DANGEROUS configuration test that changes device state
   - Per Management CLAUDE.md: "NEVER write tests that modify device configuration in the shared test suite"
   - The `SetDeviceConfigAsync` API exists but toggling capabilities causes device reboot
   - I will write this as a `[SkippableFact]` gated by environment variable, following the "safe configuration testing" pattern from the CLAUDE.md
   - Save original capabilities, disable OATH, verify, re-enable, verify -- all in try/finally
   - Actually, this requires a reboot between each change (3+ seconds). The session dies after reboot.
   - This test is complex and risky. I'll implement it with proper safeguards.

### 5. Cross-Module Tests

**Cannot compile in Management test project.** The project only references:
- `Yubico.YubiKit.Management.csproj`
- `Yubico.YubiKit.Core.csproj`
- `Yubico.YubiKit.Tests.Shared.csproj`

PIV and FIDO2 are NOT referenced. Adding project references would create unwanted coupling.

**Recommendation:** Document that `MultiProtocol_PivPinSet_BlocksFidoReset` and `MultiProtocol_FidoPinSet_BlocksPivReset` require a dedicated cross-module test project (e.g., `Yubico.YubiKit.CrossModule.IntegrationTests`) that references PIV, FIDO2, and Management. These tests will be skipped for now.

## Implementation Steps

1. Create `SecurityDomainSession_Scp03KeyLifecycleTests.cs`
2. Create `SecurityDomainSession_Scp11cTests.cs`
3. Create `SecurityDomainSession_NegativeTests.cs`
4. Create `ManagementSessionCapabilityTests.cs`
5. Run `dotnet build.cs build` and fix any compilation errors

## Patterns to Follow

- Use `CancellationTokenSource` with 100-second timeout
- SD tests use `state.WithSecurityDomainSessionAsync(bool resetBeforeUse, ...)`
- Management tests use `state.WithManagementAsync(async (mgmt, deviceInfo) => { ... })`
- Multi-session SD tests: first session reset=true, subsequent sessions reset=false
- Use `[Theory]` + `[WithYubiKey(...)]` attributes
- Import usings matching existing test files
- Follow Apache 2.0 license header from existing files
