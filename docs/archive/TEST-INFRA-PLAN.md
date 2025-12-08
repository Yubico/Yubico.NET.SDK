# Integration Test Infrastructure Implementation Plan

## ✅ COMPLETION STATUS

### Completed (2025-01-05)

**Phase 1: Critical Safety - AllowList Infrastructure** ✅
- ✅ AllowList core classes with console logging
- ✅ appsettings.json configuration
- ✅ YubiKeyTestBase with device filtering
- ✅ Test requirements system
- ✅ **Multi-device filtering** - Production keys filtered, not failed
- ✅ **AuthorizedDevices property** - Exposes all verified devices
- ✅ **SelectDevice() method** - Virtual method for device selection

**Phase 2: TestState Pattern for Management** ✅
- ✅ TestState base class
- ✅ ManagementTestState with WithManagementAsync callback
- ✅ ManagementTestFixture for xUnit integration
- ✅ Example integration tests (ManagementIntegrationTests.cs)

**Phase 3: Multi-Device Parameterized Tests** ✅ **NEW**
- ✅ **YubiKeyTestDevice** wrapper class with IXunitSerializable
- ✅ **YubiKeyDataAttribute** for parameterized tests
- ✅ **Device caching** for serialization
- ✅ **Declarative filtering** (MinFirmware, FormFactor, RequireUsb/Nfc, etc.)
- ✅ **Friendly test names** in output
- ✅ **Static lazy initialization** - Devices discovered once per test run

**Shared Test Infrastructure Project** ✅
- ✅ Created Yubico.YubiKit.Tests.Shared project
- ✅ All infrastructure moved to shared project
- ✅ Core/Management.IntegrationTests reference shared project
- ✅ Comprehensive README.md
- ✅ **Solution builds successfully (0 errors, 19 xUnit warnings)**

### Usage Example

```csharp
// Single device test (fixture-based)
public class SingleDeviceTests : ManagementTestFixture
{
    [SkippableFact]
    public async Task GetDeviceInfo()
    {
        RequireFirmware(5, 0, 0);
        await State.WithManagementAsync(async (mgmt, state) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.True(info.SerialNumber > 0);
        });
    }
}

// Multi-device test (Theory-based)
public class MultiDeviceTests
{
    [Theory]
    [YubiKeyData(MinFirmware = "5.7.2", RequireUsb = true)]
    public async Task Scp11_AllUsbDevices(YubiKeyTestDevice device)
    {
        // Runs on ALL USB devices with FW >= 5.7.2
        using var connection = await device.Device.ConnectAsync<ISmartCardConnection>();
        // Test logic...
    }
}
```

### Next Steps (Not Yet Implemented)

**Phase 4: Application-Specific TestState Classes**
- ⏸️ PivTestState (blocked - PIV not yet implemented)
- ⏸️ OathTestState (blocked - OATH not yet implemented)
- ⏸️ FidoTestState (blocked - FIDO not yet implemented)

**Phase 5: Documentation & Advanced Features**
- ⏸️ Update shared README.md with YubiKeyDataAttribute examples
- ⏸️ Device reconnection after destructive operations
- ⏸️ CI/CD integration documentation

---

## Phase 1: Critical Safety - AllowList Infrastructure (MUST HAVE)

**Goal**: Prevent any integration test from running on production YubiKeys

### 1.1 Create AllowList Core Classes
**Location**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/`

**Files to create**:
- `IAllowListProvider.cs` - Interface for reading allowed serials
- `AllowList.cs` - Core verification logic with hard fail on violation
- `AppSettingsAllowListProvider.cs` - Reads from appsettings.json
- `AllowListException.cs` - Custom exception for violations

**Key features**:
- Hard fail with `Environment.Exit(-1)` if no allow list configured
- Hard fail with `Environment.Exit(-1)` if device not in allow list
- Try SmartCard connection first, fallback to other connection types for serial
- Clear error messages with actionable guidance

### 1.2 Add appsettings.json Configuration
**Location**: `Yubico.YubiKit.Tests.IntegrationTests/appsettings.json`

```json
{
  "YubiKeyTests": {
    "AllowedSerialNumbers": [
      12345678,
      87654321
    ],
    "EnableHardFail": true
  }
}
```

### 1.3 Create Base Test Fixture with AllowList Check
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/YubiKeyTestBase.cs`

**Features**:
- Implements xUnit `IAsyncLifetime`
- Static `AllowList` instance (shared across all tests)
- `InitializeAsync()` acquires device and verifies against allow list
- `DisposeAsync()` releases device
- Helper methods for test requirements (see Phase 1.4 below)

### 1.4 Create Test Requirements System ⭐ NEW
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/TestRequirements.cs`

**Helper methods in `YubiKeyTestBase`**:
```csharp
public abstract class YubiKeyTestBase : IAsyncLifetime
{
    protected IYubiKeyDevice Device { get; private set; } = null!;
    protected DeviceInfo DeviceInfo { get; private set; } = null!;

    // ✅ Firmware version requirements
    protected void RequireFirmware(int major, int minor, int patch)
    {
        if (!DeviceInfo.FirmwareVersion.IsAtLeast(major, minor, patch))
        {
            throw new SkipException(
                $"Test requires firmware {major}.{minor}.{patch} or newer. " +
                $"Device has {DeviceInfo.FirmwareVersion}");
        }
    }

    // ✅ Form factor requirements (Bio, USB-A, USB-C, NFC)
    protected void RequireFormFactor(FormFactor formFactor)
    {
        if (DeviceInfo.FormFactor != formFactor)
        {
            throw new SkipException(
                $"Test requires {formFactor} form factor. " +
                $"Device is {DeviceInfo.FormFactor}");
        }
    }

    // ✅ Transport requirements (USB, NFC, etc.)
    protected void RequireTransport(Transport transport)
    {
        if (!DeviceInfo.AvailableTransports.Contains(transport))
        {
            throw new SkipException(
                $"Test requires {transport} transport. " +
                $"Device supports: {string.Join(", ", DeviceInfo.AvailableTransports)}");
        }
    }

    // ✅ Capability requirements
    protected void RequireCapability(Capability capability)
    {
        if (!DeviceInfo.EnabledCapabilities.HasFlag(capability))
        {
            throw new SkipException(
                $"Test requires {capability} capability to be enabled");
        }
    }

    // ✅ FIPS requirements
    protected void RequireFips(Capability capability)
    {
        if ((DeviceInfo.FipsCapable & capability.Bit) == 0)
        {
            throw new SkipException(
                $"Test requires FIPS-capable {capability}");
        }

        if ((DeviceInfo.FipsApproved & capability.Bit) == 0)
        {
            throw new SkipException(
                $"Test requires {capability} to be in FIPS approved mode");
        }
    }

    // ✅ Combination helper (commonly used together)
    protected void RequireDevice(
        FirmwareVersion? minFirmware = null,
        FormFactor? formFactor = null,
        Transport? transport = null,
        Capability? capability = null)
    {
        if (minFirmware is not null)
            RequireFirmware(minFirmware.Major, minFirmware.Minor, minFirmware.Patch);
        if (formFactor is not null)
            RequireFormFactor(formFactor.Value);
        if (transport is not null)
            RequireTransport(transport.Value);
        if (capability is not null)
            RequireCapability(capability.Value);
    }
}
```

**Usage Example 1: Class-Level Requirements** (skips entire fixture)
```csharp
public class FidoBioTestFixture : FidoTestFixture
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync(); // Acquires device + verifies allow list

        // ✅ Check requirements - throws SkipException if not met
        RequireFirmware(5, 7, 2);
        RequireFormFactor(FormFactor.Bio);

        // Build state (only runs if requirements met)
        State = new FidoTestState.Builder { Device = Device, SetPin = true }.Build();
    }
}
```

**Usage Example 2: Test-Level Requirements** (skips individual test)
```csharp
public class FidoIntegrationTests : FidoTestFixture
{
    [Fact]
    public void TestMakeCredentialWithBio()
    {
        // ✅ Check requirements at start of test
        RequireFormFactor(FormFactor.Bio);
        RequireFirmware(5, 7, 2);

        State.WithFido((fido, state) =>
        {
            // Test biometric credential creation...
        });
    }

    [Fact]
    public void TestNfcTransport()
    {
        RequireTransport(Transport.NFC);

        // Test NFC-specific functionality...
    }
}
```

**Usage Example 3: Combination Helper**
```csharp
[Fact]
public void TestScp11b()
{
    RequireDevice(
        minFirmware: new FirmwareVersion(5, 7, 2),
        transport: Transport.USB);

    // Test SCP11b functionality...
}
```

---

## Phase 2: TestState Pattern for Automatic Device Configuration

### 2.1 Create Base TestState Class
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/TestState.cs`

**Features**:
- Abstract base class for all application-specific states
- Properties: `CurrentDevice`, `DeviceInfo`, `ScpKeyParams`
- Builder pattern with modern C# `required` properties
- Helper methods: `OpenConnection<T>()`, `IsFipsCapable()`, `IsFipsApproved()`
- Device reconnection support
- Can call `RequireX()` methods in constructor to skip state initialization

### 2.2 Management TestState
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/ManagementTestState.cs`

**Features**:
- No destructive reset needed (read-only operations)
- Caches `DeviceInfo` on construction
- Simple state - mainly device capabilities

### 2.3 PIV TestState
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/PivTestState.cs`

**Features**:
- **Automatic reset** in constructor (destructive!)
- Default credentials: PIN="123456", PUK="12345678", Management Key=(default)
- Complex credentials for devices with PIN complexity requirements
- Properties: `Pin`, `Puk`, `ManagementKey`, `IsFipsApproved`
- `WithPiv(Action<PivSession, PivTestState>)` callback method

### 2.4 OATH TestState
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/OathTestState.cs`

**Features**:
- **Automatic reset** in constructor
- Default password handling
- Cleanup of existing credentials
- Properties: `Password`, `CredentialCount`
- `WithOath(Action<OathSession, OathTestState>)` callback method

### 2.5 FIDO TestState
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/FidoTestState.cs`

**Features**:
- Optional PIN setup via builder flag
- **Automatic credential cleanup** in constructor
- FIPS configuration (enable `alwaysUv` for FIPS devices)
- Properties: `Pin`, `IsFipsApproved`, `AlwaysUv`, `PinUvAuthProtocol`
- `WithFido(Action<Ctap2Session, FidoTestState>)` callback method

### 2.6 Specialized FIDO Bio State ⭐
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/FidoBioTestState.cs`

```csharp
public class FidoBioTestState : FidoTestState
{
    public class Builder : FidoTestState.Builder
    {
        public override FidoBioTestState Build()
        {
            // ✅ Verify Bio form factor before building state
            var deviceInfo = GetDeviceInfo(Device);
            if (deviceInfo.FormFactor != FormFactor.Bio)
            {
                throw new SkipException(
                    $"FidoBioTestState requires Bio form factor, device is {deviceInfo.FormFactor}");
            }

            if (!deviceInfo.FirmwareVersion.IsAtLeast(5, 7, 2))
            {
                throw new SkipException(
                    $"Biometric tests require firmware 5.7.2+, device has {deviceInfo.FirmwareVersion}");
            }

            return new FidoBioTestState(this);
        }
    }

    private FidoBioTestState(Builder builder) : base(builder)
    {
        // Additional bio-specific initialization
    }
}
```

---

## Phase 3: Application-Specific Test Fixtures

### 3.1 Management Test Fixture
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Fixtures/ManagementTestFixture.cs`

```csharp
public class ManagementTestFixture : YubiKeyTestBase
{
    protected ManagementTestState State { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync(); // Acquires device + verifies allow list
        State = new ManagementTestState.Builder { Device = Device }.Build();
    }
}
```

### 3.2 PIV Test Fixture
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Fixtures/PivTestFixture.cs`

- Inherits from `YubiKeyTestBase`
- Builds `PivTestState` in `InitializeAsync()` (which resets device)
- Exposes `State` property with known credentials

### 3.3 OATH Test Fixture
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Fixtures/OathTestFixture.cs`

- Similar to PIV but for OATH application

### 3.4 FIDO Test Fixture (Base)
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Fixtures/FidoTestFixture.cs`

- Supports optional PIN setup via builder
- Handles FIPS configuration
- Base for specialized fixtures

### 3.5 FIDO Bio Test Fixture ⭐
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Fixtures/FidoBioTestFixture.cs`

```csharp
public class FidoBioTestFixture : YubiKeyTestBase
{
    protected FidoBioTestState State { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // ✅ Requirements checked here (skips entire test class if not met)
        RequireFirmware(5, 7, 2);
        RequireFormFactor(FormFactor.Bio);

        // Build specialized Bio state
        State = new FidoBioTestState.Builder { Device = Device, SetPin = true }.Build();
    }
}

// Usage in tests:
public class FidoBioIntegrationTests : FidoBioTestFixture
{
    [Fact]
    public void TestBioEnrollment()
    {
        // No need to check requirements - fixture already did it
        State.WithFido((fido, state) =>
        {
            // Test biometric enrollment...
        });
    }
}
```

### 3.6 Example - Security Domain Fixture ⭐
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Fixtures/SecurityDomainTestFixture.cs`

```csharp
public class SecurityDomainTestFixture : YubiKeyTestBase
{
    protected SecurityDomainTestState State { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // ✅ SCP requires firmware 5.3.0+ for SCP03, 5.7.2+ for SCP11
        RequireFirmware(5, 7, 2); // For SCP11 tests

        State = new SecurityDomainTestState.Builder { Device = Device }.Build();
    }
}
```

---

## Phase 4: Centralized Test Data

### 4.1 Create Test Data Constants
**Files**:
- `Infrastructure/PivTestData.cs` - Certificates, keys, slots
- `Infrastructure/OathTestData.cs` - Credential names, periods, algorithms
- `Infrastructure/FidoTestData.cs` - RP IDs, user entities, challenges

**Example**:
```csharp
public static class FidoTestData
{
    public static readonly ReadOnlyMemory<byte> DefaultPin =
        Encoding.UTF8.GetBytes("11234567");
    public static readonly string RpId = "example.com";
    public static readonly PublicKeyCredentialRpEntity Rp =
        new(RpId, "Example Company");
    public static readonly ReadOnlyMemory<byte> Challenge =
        new byte[] { 0x00, 0x01, 0x02, /* ... */ };
}
```

---

## Phase 5: Test Categories and Filtering

### 5.1 Create Category Attributes
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Infrastructure/CategoryAttributes.cs`

```csharp
public sealed class SmokeTestAttribute : TraitAttribute
{
    public SmokeTestAttribute() : base("Category", "Smoke") { }
}

public sealed class SlowTestAttribute : TraitAttribute
{
    public SlowTestAttribute() : base("Category", "Slow") { }
}

public sealed class ManualTestAttribute : TraitAttribute
{
    public ManualTestAttribute() : base("Category", "Manual") { }
}

// ⭐ Hardware-specific categories
public sealed class RequiresBioAttribute : TraitAttribute
{
    public RequiresBioAttribute() : base("Hardware", "Bio") { }
}

public sealed class RequiresNfcAttribute : TraitAttribute
{
    public RequiresNfcAttribute() : base("Hardware", "NFC") { }
}
```

**Usage**:
```csharp
[Fact, RequiresBio]
public void TestBioEnrollment() { ... }

[Fact, RequiresNfc, SlowTest]
public void TestNfcTransaction() { ... }
```

**Filtering**:
```bash
# Run only smoke tests
dotnet test --filter Category=Smoke

# Exclude manual tests
dotnet test --filter "Category!=Manual"

# Run only Bio hardware tests
dotnet test --filter Hardware=Bio

# Run tests that don't require special hardware
dotnet test --filter "Hardware!=Bio&Hardware!=NFC"
```

---

## Phase 6: Refactor Existing Integration Tests

### 6.1 Update PIV Integration Tests
**File**: `Yubico.YubiKit.Tests.IntegrationTests/Piv/PivIntegrationTests.cs`

- Change base class to `PivTestFixture`
- Use `State.WithPiv((piv, state) => { ... })` pattern
- Access known credentials from `state.Pin`, `state.ManagementKey`
- Remove manual device acquisition code
- Add `RequireX()` calls for tests with special requirements

### 6.2 Update Management Integration Tests
- Similar refactoring for Management tests

### 6.3 Update OATH Integration Tests
- Similar refactoring for OATH tests

### 6.4 Update FIDO Integration Tests
- Similar refactoring for FIDO tests
- Split into `FidoIntegrationTests` (general) and `FidoBioIntegrationTests` (bio-specific)

---

## Phase 7: Documentation and Safety Warnings

### 7.1 Update CLAUDE.md
Add integration testing section:
- Explain allow list requirement
- Warn about destructive operations (reset)
- Document TestState pattern
- Document test requirements system (`RequireX()` methods)
- Provide examples of writing new integration tests
- Examples of hardware-specific test suites

### 7.2 Create Integration Test README
**File**: `Yubico.YubiKit.Tests.IntegrationTests/README.md`

**Sections**:
- Setup instructions (appsettings.json)
- Safety warnings about test YubiKeys
- How to add device serials to allow list
- Test requirements system
  - Firmware version requirements
  - Form factor requirements (Bio, USB-A, USB-C, NFC)
  - Transport requirements
  - Capability requirements
- How to run specific test categories
- How to filter by hardware requirements
- How to write new integration tests using fixtures
- Example test suites for different hardware types

### 7.3 Add XML Comments
- All public infrastructure classes should have XML docs
- Warn about destructive operations in TestState constructors
- Document `RequireX()` method behaviors (throw SkipException)

---

## Implementation Order (Safety First)

1. **Phase 1.1-1.3** (Critical): AllowList infrastructure with hard fail
2. **Phase 1.4** ⭐ (Critical): Test requirements system
3. **Phase 2**: Base TestState and ManagementTestState (non-destructive)
4. **Test Phase 1**: Verify allow list + requirements work with existing tests
5. **Phase 2 continued**: PIV/OATH/FIDO TestStates (destructive resets)
6. **Phase 3**: Application-specific fixtures (including specialized Bio/NFC fixtures)
7. **Phase 6**: Refactor existing tests incrementally (one app at a time)
8. **Phase 4 & 5**: Polish (test data, categories)
9. **Phase 7**: Documentation

---

## Key Design Decisions

### Test Requirements Pattern ⭐
**Where to put requirements:**

1. **Class-level** (skips entire test class):
   - Put in `Fixture.InitializeAsync()` after `base.InitializeAsync()`
   - Best for test suites targeting specific hardware (Bio, NFC)
   - Example: `FidoBioTestFixture` requires Bio + firmware 5.7.2+

2. **Test-level** (skips individual test):
   - Put at start of test method
   - Best for occasional special requirements
   - Example: One test in `FidoIntegrationTests` needs NFC transport

3. **State-level** (prevents state construction):
   - Put in specialized `TestState.Builder.Build()`
   - Best for enforcing requirements when state is reused
   - Example: `FidoBioTestState` verifies Bio in builder

**Recommendation**:
- **Specialized hardware → dedicated fixture** (e.g., `FidoBioTestFixture`, `SecurityDomainScp11Fixture`)
- **Occasional requirement → call in test method**
- **Complex multi-requirement suites → call in fixture `InitializeAsync()`**

### Example: Organizing FIDO Tests

```
FidoIntegrationTests.cs          ← Base fixture (FidoTestFixture)
├─ TestMakeCredential()         ← Works on any FIDO2 key
├─ TestGetAssertion()
├─ TestClientPin()
└─ TestResidentKey()

FidoBioIntegrationTests.cs       ← Specialized fixture (FidoBioTestFixture)
├─ [Class requires Bio + FW 5.7.2+]
├─ TestBioEnrollment()
├─ TestBioAuthentication()
└─ TestBioCredentialManagement()

FidoNfcIntegrationTests.cs       ← Specialized fixture
├─ [Class requires NFC transport]
├─ TestNfcMakeCredential()
└─ TestNfcGetAssertion()

SecurityDomainIntegrationTests.cs
├─ [Class requires FW 5.3.0+]
├─ TestScp03()
└─ TestKeyManagement()

SecurityDomainScp11Tests.cs       ← Specialized for SCP11
├─ [Class requires FW 5.7.2+]
├─ TestScp11aInit()
├─ TestScp11bInit()
└─ TestScp11cInit()
```

### Use Modern C# Patterns
- `Span<byte>` for sensitive data (PINs, keys)
- `ReadOnlyMemory<byte>` for stored credentials
- `required` properties instead of complex generic builders
- `IAsyncLifetime` for xUnit fixtures
- File-scoped namespaces, nullable reference types

### Sensitive Data Handling
- Zero credentials with `CryptographicOperations.ZeroMemory()` in Dispose
- Never log PINs, keys, or passwords
- Use `Span<byte>` where possible to keep data on stack

### Error Messages
```csharp
// Good error message example for requirements
"Test requires firmware 5.7.2 or newer for SCP11 support. Device has 5.4.3"
"Test requires Bio form factor. Device is USB-C"
"Test requires NFC transport. Device supports: USB"
```

---

## Files to Create (23 new files)

**Infrastructure** (12 files):
1. `IAllowListProvider.cs`
2. `AllowList.cs`
3. `AppSettingsAllowListProvider.cs`
4. `AllowListException.cs`
5. `YubiKeyTestBase.cs` (with `RequireX()` methods)
6. `TestState.cs`
7. `ManagementTestState.cs`
8. `PivTestState.cs`
9. `OathTestState.cs`
10. `FidoTestState.cs`
11. `FidoBioTestState.cs` (specialized)
12. `CategoryAttributes.cs`

**Fixtures** (6 files):
13. `ManagementTestFixture.cs`
14. `PivTestFixture.cs`
15. `OathTestFixture.cs`
16. `FidoTestFixture.cs`
17. `FidoBioTestFixture.cs` (specialized)
18. `SecurityDomainTestFixture.cs` (example)

**Test Data** (3 files):
19. `PivTestData.cs`
20. `OathTestData.cs`
21. `FidoTestData.cs`

**Configuration** (1 file):
22. `appsettings.json` (new)

**Documentation** (1 file):
23. `README.md` (integration tests)

**Files to Modify**:
- Existing integration test classes (PIV, Management, OATH, FIDO) - split by hardware type
- `CLAUDE.md` (add integration testing section with requirements examples)
- `.csproj` file (add appsettings.json as content, copy to output)

---

## Estimated Effort
- Phase 1 (AllowList): ~2-3 hours
- Phase 1.4 (Requirements): ~2 hours
- Phase 2 (TestStates): ~5-7 hours (includes specialized states)
- Phase 3 (Fixtures): ~2-3 hours (includes specialized fixtures)
- Phase 4 (Test Data): ~1 hour
- Phase 5 (Categories): ~1 hour (includes hardware traits)
- Phase 6 (Refactor existing tests): ~4-5 hours (includes splitting by hardware)
- Phase 7 (Documentation): ~2 hours (includes requirements examples)

**Total**: ~16-23 hours of implementation work

---

## Success Criteria
✅ No integration test can run without valid allow list
✅ Hard fail (`Environment.Exit(-1)`) if device not in allow list
✅ All four application areas have TestState implementations
✅ Tests automatically reset devices to known state
✅ Tests have access to known credentials (PINs, keys, passwords)
✅ Tests gracefully skip when firmware version insufficient
✅ Tests gracefully skip when wrong form factor (Bio, USB, NFC)
✅ Tests gracefully skip when required transport unavailable
✅ Tests gracefully skip when required capability disabled
✅ Specialized fixtures for hardware-specific test suites (Bio, NFC, SCP11)
✅ Helper methods (`RequireX()`) for ad-hoc test requirements
✅ Test categories enable filtering (smoke, slow, manual, bio, nfc)
✅ Developers can easily write new integration tests using fixtures
✅ Clear documentation with safety warnings and requirements examples
