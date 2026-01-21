# FIDO2 Post-Implementation Review Fixes (Ralph Loop)

**Goal:** Address all issues identified in the Product Owner's Post-Implementation Review of the FIDO2 session.

**Architecture:** Fix SmartCard connection selection, consolidate command interfaces into IFidoSession, rename confusing backend classes, standardize CBOR parsing with CtapResponseParser, remove dead code, and update test infrastructure to use WithYubiKey attributes.

**Tech Stack:** C# 14, xUnit v3, NSubstitute, CBOR, CTAP2

**Completion Promise:** FIDO2_POST_REVIEW_COMPLETE

**Phases:** 10 phases, one per iteration. Each phase outputs its own `<promise>PHASE_N_DONE</promise>`. Final phase outputs `<promise>FIDO2_POST_REVIEW_COMPLETE</promise>`.

---

## Phase 1: Fix SmartCard Connection Runtime Errors (MUST FIX)

**Problem:** `FidoSession.CreateAsync()` with SmartCardConnection fails at `SelectAsync()` with `SW=0x6A82` (File or application not found).

**Files:**
- Investigate: `Yubico.YubiKit.Fido2/src/FidoSession.cs` (lines 575-592)
- Investigate: `Yubico.YubiKit.Fido2/src/Backend/SmartCardFidoBackend.cs`
- Reference: `Yubico.YubiKit.Core/src/Protocols/ApplicationIds.cs` (line 22: `Fido2 = [0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01]`)
- Test: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/`

**Context:**
- The FIDO2 AID is `A0000006472F0001` (same as FIDO U2F)
- HID connection works fine; only SmartCard fails
- Other sessions (ManagementSession, SecurityDomainSession) successfully select their applications
- Compare how `ManagementSession` handles SmartCard selection in `Yubico.YubiKit.Management/src/ManagementSession.cs`

**Step 1: Reproduce the failure**
Run the failing integration tests:
```bash
dotnet build.cs test --filter "FullyQualifiedName~CreateFidoSession_With_SmartCard"
```
Expected: FAIL with SW=0x6A82

**Step 2: Debug the selection process**
- Check if FIDO2 application is available on the YubiKey via SmartCard interface
- Compare with how ManagementSession selects its application
- Verify the AID bytes are correct for CCID/SmartCard transport
- Check if YubiKey firmware requires a different selection sequence for FIDO2 over NFC/CCID

**Step 3: Implement the fix**
Based on debugging, fix the selection issue. Possible causes:
- FIDO2 may require a different AID for CCID vs HID
- Selection sequence may need additional steps
- The YubiKey may need FIDO2 enabled over NFC in configuration

**Step 4: Verify the fix**
```bash
dotnet build.cs test --filter "FullyQualifiedName~CreateFidoSession_With_SmartCard"
dotnet build.cs test --filter "FullyQualifiedName~CreateFidoSession_With_FactoryInstance"
```
Expected: PASS

**Step 5: Commit**
```bash
git status
git add Yubico.YubiKit.Fido2/src/<modified-files>
git commit -m "fix(fido2): resolve SmartCard connection selection error SW=0x6A82"
```

**Verification:**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~CreateFidoSession_With_SmartCard"
```
Expected: Build passes, SmartCard tests pass

→ Output `<promise>PHASE_1_DONE</promise>` when SmartCard tests pass

---

## Phase 2: Rename FidoBackend in Management to Avoid Confusion

**Problem:** `Yubico.YubiKit.Management.FidoBackend` naming conflicts with `Yubico.YubiKit.Fido2.FidoHidBackend`.

**Files:**
- Rename: `Yubico.YubiKit.Management/src/FidoBackend.cs` → `Yubico.YubiKit.Management/src/ManagementFidoHidBackend.cs`
- Update: All references in `Yubico.YubiKit.Management/`

**Step 1: Rename the class**
In `Yubico.YubiKit.Management/src/FidoBackend.cs`:
```csharp
// Before
internal sealed class FidoBackend(IFidoHidProtocol hidProtocol) : IManagementBackend

// After  
internal sealed class ManagementFidoHidBackend(IFidoHidProtocol hidProtocol) : IManagementBackend
```

**Step 2: Rename the file**
```bash
git mv Yubico.YubiKit.Management/src/FidoBackend.cs Yubico.YubiKit.Management/src/ManagementFidoHidBackend.cs
```

**Step 3: Update all references**
Search for `FidoBackend` in Management project and update to `ManagementFidoHidBackend`.

**Step 4: Verify build**
```bash
dotnet build.cs build
```
Expected: Build succeeds

**Step 5: Run tests**
```bash
dotnet build.cs test --filter "FullyQualifiedName~Management"
```
Expected: All Management tests pass

**Step 6: Commit**
```bash
git status
git add Yubico.YubiKit.Management/src/ManagementFidoHidBackend.cs
git add Yubico.YubiKit.Management/src/<other-modified-files>
git commit -m "refactor(management): rename FidoBackend to ManagementFidoHidBackend for clarity"
```

**Verification:**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Management"
```
Expected: Build passes, all Management tests pass

→ Output `<promise>PHASE_2_DONE</promise>` when build and tests pass

---

## Phase 3: Remove Extra Command Interfaces

**Problem:** `IBioEnrollmentCommands` and `IClientPinCommands` were created for mocking but should be consolidated.

**Files:**
- Remove: `Yubico.YubiKit.Fido2/src/BioEnrollment/IBioEnrollmentCommands.cs`
- Remove: `Yubico.YubiKit.Fido2/src/Pin/IClientPinCommands.cs`
- Modify: `Yubico.YubiKit.Fido2/src/BioEnrollment/FingerprintBioEnrollment.cs`
- Modify: `Yubico.YubiKit.Fido2/src/Pin/ClientPin.cs`
- Modify: `Yubico.YubiKit.Fido2/src/IFidoSession.cs` (add required methods)
- Update: Related unit tests to mock `IFidoSession` instead

**Step 1: Add methods to IFidoSession if missing**
Ensure `IFidoSession` has `SendCborRequestAsync` or equivalent method that the command classes need.

**Step 2: Refactor FingerprintBioEnrollment**
```csharp
// Before
public class FingerprintBioEnrollment(IBioEnrollmentCommands commands)

// After
public class FingerprintBioEnrollment(IFidoSession session)
```

**Step 3: Refactor ClientPin**
```csharp
// Before
public class ClientPin(IClientPinCommands commands)

// After
public class ClientPin(IFidoSession session)
```

**Step 4: Remove the interfaces**
Delete `IBioEnrollmentCommands.cs` and `IClientPinCommands.cs`.

**Step 5: Update unit tests**
Update tests to mock `IFidoSession` instead of the removed interfaces.

**Step 6: Verify**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Fido2"
```
Expected: Build succeeds, all tests pass

**Step 7: Commit**
```bash
git status
git add Yubico.YubiKit.Fido2/src/BioEnrollment/FingerprintBioEnrollment.cs
git add Yubico.YubiKit.Fido2/src/Pin/ClientPin.cs
git add Yubico.YubiKit.Fido2/src/IFidoSession.cs
git add Yubico.YubiKit.Fido2/tests/<modified-test-files>
git commit -m "refactor(fido2): consolidate command interfaces into IFidoSession"
```

Then remove deleted files:
```bash
git rm Yubico.YubiKit.Fido2/src/BioEnrollment/IBioEnrollmentCommands.cs
git rm Yubico.YubiKit.Fido2/src/Pin/IClientPinCommands.cs
git commit -m "refactor(fido2): remove IBioEnrollmentCommands and IClientPinCommands"
```

**Verification:**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Fido2"
```
Expected: Build passes, all FIDO2 tests pass

→ Output `<promise>PHASE_3_DONE</promise>` when build and tests pass

---

## Phase 4: Create CtapResponseParser for CBOR Deserialization

**Problem:** CBOR deserialization logic is duplicated across CredentialManagementModels.

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Cbor/CtapResponseParser.cs`
- Modify: `Yubico.YubiKit.Fido2/src/Credentials/CredentialManagementModels.cs`
- Modify: `Yubico.YubiKit.Fido2/src/BioEnrollment/BioEnrollmentModels.cs` (if applicable)
- Test: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Cbor/CtapResponseParserTests.cs`

**Step 1: Create CtapResponseParser**
```csharp
namespace Yubico.YubiKit.Fido2.Cbor;

/// <summary>
/// Utility class for parsing CTAP2 CBOR responses.
/// Provides common deserialization patterns to reduce duplication.
/// </summary>
internal static class CtapResponseParser
{
    /// <summary>
    /// Reads an integer-keyed CBOR map and invokes the handler for each key-value pair.
    /// </summary>
    public static void ReadIntKeyMap(CborReader reader, Action<int, CborReader> fieldHandler)
    {
        var mapLength = reader.ReadStartMap();
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            fieldHandler(key, reader);
        }
        reader.ReadEndMap();
    }

    /// <summary>
    /// Reads a text-keyed CBOR map and invokes the handler for each key-value pair.
    /// </summary>
    public static void ReadTextKeyMap(CborReader reader, Action<string, CborReader> fieldHandler)
    {
        var mapLength = reader.ReadStartMap();
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            fieldHandler(key, reader);
        }
        reader.ReadEndMap();
    }

    /// <summary>
    /// Converts a nullable byte array to nullable ReadOnlyMemory.
    /// </summary>
    public static ReadOnlyMemory<byte>? ToNullableMemory(byte[]? data) =>
        data is not null ? new ReadOnlyMemory<byte>(data) : null;
}
```

**Step 2: Write unit tests for CtapResponseParser**
Create tests validating the parsing helpers work correctly.

**Step 3: Refactor CredentialManagementModels to use CtapResponseParser**
Replace duplicated map-reading loops with calls to `CtapResponseParser.ReadIntKeyMap()`.

**Step 4: Verify**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~CtapResponseParser"
dotnet build.cs test --filter "FullyQualifiedName~CredentialManagement"
```
Expected: All tests pass

**Step 5: Commit**
```bash
git status
git add Yubico.YubiKit.Fido2/src/Cbor/CtapResponseParser.cs
git add Yubico.YubiKit.Fido2/src/Credentials/CredentialManagementModels.cs
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Cbor/CtapResponseParserTests.cs
git commit -m "refactor(fido2): add CtapResponseParser to reduce CBOR deserialization duplication"
```

**Verification:**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~CtapResponseParser"
dotnet build.cs test --filter "FullyQualifiedName~CredentialManagement"
```
Expected: All tests pass

→ Output `<promise>PHASE_4_DONE</promise>` when build and tests pass

---

## Phase 5: Standardize CtapRequestBuilder Usage

**Problem:** `CtapRequestBuilder` is not used consistently; some places manually build requests.

**Files:**
- Audit: All files in `Yubico.YubiKit.Fido2/src/` that build CBOR requests
- Modify: Files that manually build CBOR requests to use `CtapRequestBuilder`

**Step 1: Find manual CBOR request building**
Search for `CborWriter` usage outside of `CtapRequestBuilder`:
```bash
grep -r "new CborWriter" Yubico.YubiKit.Fido2/src/ --include="*.cs" | grep -v CtapRequestBuilder
```

**Step 2: Refactor each instance**
Convert manual CBOR building to use `CtapRequestBuilder` fluent API.

**Step 3: Verify**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Fido2"
```
Expected: All tests pass

**Step 4: Commit**
```bash
git status
git add Yubico.YubiKit.Fido2/src/<modified-files>
git commit -m "refactor(fido2): standardize CBOR request building with CtapRequestBuilder"
```

**Verification:**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Fido2"
```
Expected: All tests pass

→ Output `<promise>PHASE_5_DONE</promise>` when build and tests pass

---

## Phase 6: Remove Dead Code

**Problem:** `GetKeyType()` and `GetAlgorithm()` in `AttestedCredentialData` are never used.

**Files:**
- Modify: `Yubico.YubiKit.Fido2/src/Credentials/AttestedCredentialData.cs`

**Step 1: Verify methods are unused**
```bash
grep -r "GetKeyType\|GetAlgorithm" Yubico.YubiKit.Fido2/ --include="*.cs"
```
Confirm only the definitions exist, no callers.

**Step 2: Remove the methods**
Delete `GetKeyType()` (lines 120-143) and `GetAlgorithm()` (lines 149-172) from `AttestedCredentialData.cs`.

**Step 3: Verify**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Fido2"
```
Expected: Build succeeds, all tests pass

**Step 4: Commit**
```bash
git status
git add Yubico.YubiKit.Fido2/src/Credentials/AttestedCredentialData.cs
git commit -m "refactor(fido2): remove unused GetKeyType and GetAlgorithm methods"
```

**Verification:**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Fido2"
```
Expected: Build passes, all tests pass

→ Output `<promise>PHASE_6_DONE</promise>` when build and tests pass

---

## Phase 7: Update FIDO2 Integration Tests to Use WithYubiKey Attribute

**Problem:** FIDO2 tests don't use existing test infrastructure patterns from SecurityDomainSession and ManagementSession.

**Files:**
- Reference: `Yubico.YubiKit.Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs`
- Reference: `Yubico.YubiKit.SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/`
- Modify: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/`

**Step 1: Study the existing pattern**
Review how `SecurityDomainSessionTests` and `ManagementSessionTests` use `[WithYubiKey()]` attributes.

**Step 2: Update FIDO2 integration tests**
Add `[WithYubiKey()]` attribute to FIDO2 integration tests that require hardware.

**Step 3: Verify**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Fido2.IntegrationTests"
```
Expected: Tests pass (hardware tests skipped if no device)

**Step 4: Commit**
```bash
git status
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/<modified-files>
git commit -m "test(fido2): update integration tests to use WithYubiKey attribute"
```

**Verification:**
```bash
dotnet build.cs build
```
Expected: Build passes (hardware tests skipped if no device)

→ Output `<promise>PHASE_7_DONE</promise>` when build passes

---

## Phase 8: Add IYubiKeyExtensions SCP Validation

**Problem:** Methods accepting `ScpKeyParameters` only work with SmartCard connections but don't validate this.

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Extensions/IYubiKeyExtensions.cs` (or equivalent)
- Test: Add validation tests

**Step 1: Find IYubiKeyExtensions methods accepting ScpKeyParameters**
```bash
grep -r "ScpKeyParameters" Yubico.YubiKit.Core/src/ --include="*.cs" -l
```

**Step 2: Add connection type validation**
Before accepting `ScpKeyParameters`, validate that the underlying connection is a SmartCard connection:
```csharp
if (scpKeyParameters is not null && connection is not ISmartCardConnection)
{
    throw new InvalidOperationException("SCP key parameters are only supported with SmartCard connections.");
}
```

**Step 3: Add tests for validation**
Write tests that verify the exception is thrown for non-SmartCard connections with SCP parameters.

**Step 4: Verify**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~IYubiKeyExtensions"
```
Expected: All tests pass

**Step 5: Commit**
```bash
git status
git add Yubico.YubiKit.Core/src/<modified-files>
git add Yubico.YubiKit.Core/tests/<test-files>
git commit -m "fix(core): validate connection type when ScpKeyParameters provided"
```

**Verification:**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~IYubiKeyExtensions"
```
Expected: All tests pass

→ Output `<promise>PHASE_8_DONE</promise>` when build and tests pass

---

## Phase 9: Document SDK-Wide Conventions

**Problem:** Property syntax and logging conventions need documentation.

**Files:**
- Modify: `CLAUDE.md` (add conventions section)
- Modify: `CONTRIBUTING.md` (if exists) or create

**Step 1: Document property conventions**
Add to CLAUDE.md:
```markdown
## Property Conventions

- Use `{ get; init; }` for immutable properties set at construction
- Use `{ get; private set; }` for properties modified only internally
- Use `{ get; set; }` sparingly, only for configuration objects
- Validation: Perform in constructor or via dedicated `Validate()` method
```

**Step 2: Document logging conventions**
Add to CLAUDE.md:
```markdown
## Logging Conventions

- Use static `LoggingFactory` - do NOT inject `ILogger` or `ILoggerFactory` in constructors
- Each class gets its logger: `private static readonly ILogger Logger = LoggingFactory.CreateLogger<ClassName>();`
- Log at appropriate levels: Debug for protocol details, Info for operations, Warning for recoverable errors, Error for failures
```

**Step 3: Commit**
```bash
git status
git add CLAUDE.md
git commit -m "docs: add property and logging conventions to CLAUDE.md"
```

**Verification:**
File saved and git status shows clean commit.

→ Output `<promise>PHASE_9_DONE</promise>` when documentation updated

---

## Phase 10: Document Extension Architecture

**Problem:** Missing documentation on WebAuthn/CTAP extensions architecture.

**Files:**
- Create or modify: `Yubico.YubiKit.Fido2/README.md` or `Yubico.YubiKit.Fido2/CLAUDE.md`

**Step 1: Document ExtensionBuilder pattern**
Add documentation explaining:
- Extensions are built via `ExtensionBuilder` fluent API, not separate extension classes
- Available extensions: credProtect, hmac-secret, hmac-secret-mc, prf, credBlob, largeBlob, minPinLength
- How to use each extension with examples

**Step 2: Clarify hmac-secret vs PRF**
Document the relationship between hmac-secret (CTAP2) and PRF (WebAuthn) extensions.

**Step 3: Commit**
```bash
git status
git add Yubico.YubiKit.Fido2/CLAUDE.md
git commit -m "docs(fido2): document ExtensionBuilder and WebAuthn extensions"
```

**Verification:**
File saved and git status shows clean commit.

→ Output `<promise>FIDO2_POST_REVIEW_COMPLETE</promise>` when documentation complete (final phase)

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** `dotnet build.cs build` (must exit 0)
2. **Unit Tests:** `dotnet build.cs test` (all unit tests must pass)
3. **Integration Tests:** Best-effort for hardware tests; document any that require specific device configuration
4. **No Regressions:** All existing tests pass

Each phase has its own verification. Final phase outputs `<promise>FIDO2_POST_REVIEW_COMPLETE</promise>`.

---

## On Failure

- If build fails: Read error messages, fix the code, re-run build
- If tests fail: Analyze failure, fix the issue, re-run ALL tests
- If hardware tests fail: Document the failure, check if device configuration is required, skip after 2-3 attempts
- Do NOT output phase completion until phase verification passes

---

## Git Commit Discipline

- **NEVER use `git add .` or `git add -A`**
- Only commit files YOU created or modified in this session
- Use `git status` before each commit to verify staged files
- Follow conventional commit format: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`
