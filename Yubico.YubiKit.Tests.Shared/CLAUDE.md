# CLAUDE.md - Tests.Shared Module

This file provides AI agent guidance for working with the YubiKit Integration Test Infrastructure.  
**Read [README.md](README.md) first** for comprehensive user documentation.

## Module Context

Tests.Shared provides the foundation for all YubiKit integration tests. It implements:
- **Safety-first allow list** - Prevents accidental testing on production devices
- **Attribute-based test discovery** - xUnit integration for declarative device filtering
- **Device caching** - Single device discovery per test run for performance
- **Extension methods** - Fluent API for session management across applications

**Key Implementation Files:**
- `Infrastructure/WithYubiKeyAttribute.cs` - xUnit attribute with device filtering
- `Infrastructure/YubiKeyTestInfrastructure.cs` - Device discovery and caching
- `Infrastructure/AllowList.cs` - Security layer preventing unauthorized testing
- `YubiKeyTestState.cs` - Device wrapper implementing `IXunitSerializable`
- `YubiKeyTestStateExtensions.cs` - Application-specific session helpers

## Critical Design Patterns

### 1. xUnit Discovery Integration

The `[WithYubiKey]` attribute implements `ITraitAttribute` and `ITestCaseOrderer` to integrate with xUnit's discovery phase:

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class WithYubiKeyAttribute : Attribute, ITraitAttribute, ITestCaseOrderer
{
    // Filter properties
    public string? MinFirmware { get; init; }
    public FormFactor? FormFactor { get; init; }
    // ... more filters
    
    // xUnit integration
    public IEnumerable<IXunitTestCase> OrderTestCases<TTestCase>(
        IEnumerable<TTestCase> testCases) where TTestCase : IXunitTestCase
    {
        // Device filtering happens HERE during test discovery
        // Creates one test case per matching device
    }
}
```

**Why this pattern:**
- Filtering happens at discovery time, not runtime
- Each matching device generates a separate test case
- xUnit naturally parallelizes across test cases
- Test names include device serial number for clarity

### 2. Static Device Cache

```csharp
public static class YubiKeyTestInfrastructure
{
    public static IReadOnlyList<YubiKeyTestState> AllAuthorizedDevices { get; } =
        InitializeDevicesAsync().GetAwaiter().GetResult();
        
    private static async Task<IReadOnlyList<YubiKeyTestState>> InitializeDevicesAsync()
    {
        // 1. Discover all YubiKeys
        var devices = await YubiKey.FindAllAsync();
        
        // 2. Load allow list
        var allowList = await AllowList.LoadAsync();
        
        // 3. Verify each device (hard fail if not authorized)
        var authorized = new List<YubiKeyTestState>();
        foreach (var device in devices)
        {
            allowList.VerifyOrExit(device.SerialNumber);
            var state = await YubiKeyTestState.FromDeviceAsync(device);
            authorized.Add(state);
        }
        
        return authorized;
    }
}
```

**Why static initialization:**
- Device discovery is expensive (~500ms per device)
- Allow list verification must happen before ANY test runs
- CLR guarantees thread-safe initialization
- Fail-fast if unauthorized devices detected

**Tradeoff:**
- ✅ Fast test execution (discover once, use many times)
- ✅ Consistent state across all tests
- ❌ Devices plugged in after discovery won't be found
- ❌ Must restart test runner to discover new devices

### 3. Extension Method Pattern

```csharp
public static class YubiKeyTestStateExtensions
{
    public static async Task WithManagementAsync(
        this YubiKeyTestState state,
        Func<ManagementSession, DeviceInfo, Task> action,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Create connection
        await using var connection = await state.Device.OpenConnectionAsync<ISmartCardConnection>(cancellationToken);
        
        // 2. Create session with optional SCP
        await using var session = await ManagementSession.CreateAsync(
            connection,
            scpKeyParams: scpKeyParams,
            cancellationToken: cancellationToken);
        
        // 3. Get cached device info
        var deviceInfo = state.DeviceInfo;
        
        // 4. Invoke test action
        await action(session, deviceInfo);
        
        // 5. Automatic disposal via using statements
    }
}
```

**Why extension methods:**
- Composition over inheritance (no test base classes)
- Application-agnostic `YubiKeyTestState`
- Easy to add new applications (`WithPivAsync`, `WithOathAsync`, etc.)
- Clear resource management (using statements)

### 4. Allow List Security Layer

```csharp
public sealed class AllowList
{
    private readonly HashSet<int> _allowedSerials;
    
    public void VerifyOrExit(int? serialNumber)
    {
        if (serialNumber is null)
        {
            PrintError("Cannot read device serial number");
            Environment.Exit(-1);
        }
        
        if (!_allowedSerials.Contains(serialNumber.Value))
        {
            PrintError($"Device {serialNumber} not in allow list");
            Environment.Exit(-1);
        }
    }
}
```

**Why Environment.Exit:**
- Cannot throw exceptions during static initialization
- Must prevent ANY test from running on unauthorized devices
- Clear, loud failure that cannot be caught
- Forces developer to explicitly authorize devices

**Tradeoff:**
- ✅ Maximum security - impossible to bypass
- ✅ Fail-fast - no tests run on wrong devices
- ❌ Abrupt termination - no cleanup
- ❌ Harder to test the infrastructure itself

## Testing the Test Infrastructure

**Problem:** How to test code that calls `Environment.Exit(-1)`?

**Solution:** Abstraction for testability:

```csharp
// Production
public interface IAllowListProvider
{
    Task<HashSet<int>> GetAllowedSerialsAsync();
}

public class AppSettingsAllowListProvider : IAllowListProvider
{
    // Reads from appsettings.json
}

// Tests
public class FakeAllowListProvider : IAllowListProvider
{
    private readonly HashSet<int> _serials;
    
    public FakeAllowListProvider(params int[] serials)
    {
        _serials = new HashSet<int>(serials);
    }
    
    public Task<HashSet<int>> GetAllowedSerialsAsync() =>
        Task.FromResult(_serials);
}
```

**When testing infrastructure:**
- Mock `IAllowListProvider` to avoid `Environment.Exit`
- Use `FakeYubiKey` implementations for device simulation
- Test filter logic separately from device discovery
- Never commit real serial numbers in test code

## Common Implementation Patterns

### Adding a New Application Extension

When adding support for a new application (e.g., OATH, PIV):

1. **Create extension method** in `YubiKeyTestStateExtensions.cs`:

```csharp
public static async Task WithOathAsync(
    this YubiKeyTestState state,
    Func<OathSession, DeviceInfo, Task> action,
    ScpKeyParameters? scpKeyParams = null,
    bool resetBeforeUse = false,
    CancellationToken cancellationToken = default)
{
    await using var connection = await state.Device.OpenConnectionAsync<ISmartCardConnection>(cancellationToken);
    
    if (resetBeforeUse)
    {
        // Reset OATH app to factory defaults
        await using var resetSession = await OathSession.CreateAsync(connection);
        await resetSession.ResetAsync(cancellationToken);
        
        // Reconnect after reset
        connection = await state.Device.OpenConnectionAsync<ISmartCardConnection>(cancellationToken);
    }
    
    await using var session = await OathSession.CreateAsync(
        connection,
        scpKeyParams: scpKeyParams,
        cancellationToken: cancellationToken);
    
    await action(session, state.DeviceInfo);
}
```

2. **Follow established patterns:**
- `ScpKeyParameters?` parameter for optional SCP
- `resetBeforeUse` for applications that support reset
- `CancellationToken` for async cancellation
- Automatic disposal via `await using`
- Pass both session AND cached device info to action

3. **Document in README.md:**
- Add example under "Extension Methods" section
- Include common usage patterns
- Document reset behavior if applicable

### Adding New Filter Criteria

To add a new filter property (e.g., `ExcludeFirmware`):

1. **Add property to `WithYubiKeyAttribute`:**

```csharp
public sealed class WithYubiKeyAttribute : Attribute, ITraitAttribute, ITestCaseOrderer
{
    public string? ExcludeFirmware { get; init; }
}
```

2. **Implement filter logic in `OrderTestCases`:**

```csharp
public IEnumerable<IXunitTestCase> OrderTestCases<TTestCase>(
    IEnumerable<TTestCase> testCases)
{
    var devices = YubiKeyTestInfrastructure.AllAuthorizedDevices;
    
    var filtered = devices.Where(state =>
    {
        // Existing filters...
        
        if (!string.IsNullOrEmpty(ExcludeFirmware))
        {
            var excluded = FirmwareVersion.Parse(ExcludeFirmware);
            if (state.FirmwareVersion == excluded)
                return false; // Exclude this device
        }
        
        return true;
    });
    
    // Create test cases...
}
```

3. **Add helper method to `YubiKeyTestState` if needed:**

```csharp
public class YubiKeyTestState
{
    public bool HasFirmware(FirmwareVersion version) =>
        FirmwareVersion == version;
}
```

4. **Document in README.md** under "Available Filter Properties"

## Performance Considerations

### Device Discovery Cost

Device discovery has fixed costs:
- USB enumeration: ~100ms
- SmartCard enumeration: ~200ms
- Per-device `GetDeviceInfo`: ~100ms per device

**For 3 devices:**
- Total discovery: ~600ms
- Per-test overhead: ~0ms (cached)

**Optimization:**
- Static cache eliminates per-test cost
- Worth the upfront cost for test suites with >5 tests
- Break-even point: ~3 tests per device

### Parallel Test Execution

xUnit runs test cases in parallel by default:
- Each `[WithYubiKey]` test generates N test cases (N = matching devices)
- Test cases run in parallel across devices
- Safe because each test case uses a different physical device

**Constraint:**
- Tests on the SAME device run sequentially (xUnit collection constraint)
- Use `[Collection("YubiKey")]` if tests share device state

## Debugging Tips

### Test Not Running

**Check:**
1. Device serial in allow list? (`appsettings.json`)
2. Device matches filter criteria? (firmware, form factor, etc.)
3. Device discovery succeeded? (check test output)

### Environment.Exit During Discovery

**Symptoms:**
- Test runner terminates immediately
- No tests execute
- Error message printed to console

**Causes:**
- Device serial not in allow list
- Cannot read device serial
- Allow list is empty

**Fix:**
- Add serial to `appsettings.json`
- Check device is properly connected
- Verify appsettings.json copied to output directory

### Multiple Devices, Only One Running

**Check:**
- All devices in allow list?
- Filter criteria excluding some devices?
- xUnit test output shows all generated test cases?

## Future Enhancements

### Planned Features

1. **Per-test device isolation:**
   - Reset application to factory defaults before each test
   - Verify no state leakage between tests
   - Optional via `[WithYubiKey(ResetBefore = true)]`

2. **Conditional skip reasons:**
   - More informative skip messages ("Skipped: Requires firmware >= 5.7.0")
   - Include device info in skip message
   - Link to documentation for required features

3. **Device-specific test data:**
   - Store per-device test data (PINs, keys, certificates)
   - Avoid hardcoding test credentials
   - Support device-specific configuration in appsettings.json

4. **Test retry logic:**
   - Automatic retry on transient failures
   - Configurable retry count
   - Log retry attempts for debugging

## Related Modules

- **Core.Devices** - `IYubiKey`, device discovery
- **Management** - `ManagementSession`, `DeviceInfo`
- **All test projects** - Use this infrastructure

## Contributing

When modifying test infrastructure:

1. **Maintain backward compatibility:**
   - Existing tests must continue working
   - Add new features, don't break old ones
   - Deprecate before removing

2. **Update README.md:**
   - Document new extension methods
   - Add examples for new filter criteria
   - Update "Future Enhancements" section

3. **Test the infrastructure:**
   - Unit tests for filter logic
   - Integration tests for session helpers
   - Mock allow list in tests

4. **Consider performance:**
   - Minimize per-test overhead
   - Cache expensive operations
   - Profile test suite execution time

## Known Limitations

1. **Static device cache:**
   - Devices must be connected before test runner starts
   - Cannot detect devices plugged in during test run
   - Workaround: Restart test runner

2. **Environment.Exit:**
   - Cannot recover from allow list violations
   - Kills entire test process
   - Difficult to test infrastructure itself

3. **xUnit v2 constraint:**
   - Tests.Shared still uses xUnit v2 for compatibility
   - Cannot use xUnit v3 features
   - Will migrate when all test projects upgrade

4. **Single transport per test:**
   - Extension methods assume one connection type
   - Cannot test multi-transport scenarios easily
   - Workaround: Manual connection management

## Security Considerations

- **Never commit real serial numbers** - Use environment variables or .gitignored appsettings.json
- **Never log sensitive device info** - Serial numbers are public, but device IDs might be internal
- **Verify allow list before destructive operations** - Even within tests
- **Use separate test devices** - Never test on production or user devices
