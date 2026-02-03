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
- `Infrastructure/WithYubiKeyAttribute.cs` - xUnit DataAttribute for device-parameterized tests
- `Infrastructure/YubiKeyTestInfrastructure.cs` - Device discovery and caching
- `Infrastructure/AllowList.cs` - Security layer preventing unauthorized testing
- `YubiKeyTestState.cs` - Device wrapper implementing `IXunitSerializable`
- `YubiKeyTestStateExtensions.cs` - Application-specific session helpers

## Critical Design Patterns

### 1. xUnit DataAttribute Integration

The `[WithYubiKey]` attribute extends `DataAttribute` to provide test data for `[Theory]` tests:

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class WithYubiKeyAttribute : DataAttribute
{
    // Filter properties
    public string? MinFirmware { get; init; }
    public FormFactor FormFactor { get; init; }
    // ... more filters
    
    // DataAttribute implementation
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        // During discovery: returns a single placeholder
        // During execution: placeholder binds to real device via FilterCriteria
        var criteria = new FilterCriteria { /* from properties */ };
        yield return [YubiKeyTestState.CreateFilteredPlaceholder(criteria)];
    }
}
```

**Why DataAttribute:**
- Works with standard xUnit `[Theory]` tests
- Placeholders avoid device access during test discovery
- Filter criteria serialized with placeholder, resolved at execution time
- Multiple `[WithYubiKey]` attributes create multiple test cases

### 2. Discovery vs Execution Phases

**Discovery phase** (when test explorer enumerates tests):
- `WithYubiKeyAttribute.GetData()` returns a placeholder `YubiKeyTestState`
- Placeholder contains serialized `FilterCriteria`
- NO device access occurs

**Execution phase** (when tests actually run):
- Test receives placeholder `YubiKeyTestState`
- Accessing `Device` property triggers `BindToRealDevice()`
- `YubiKeyTestInfrastructure.FilterDevices()` finds matching device
- Placeholder is replaced with real device state

### 3. Lazy Static Device Cache

```csharp
public static class YubiKeyTestInfrastructure
{
    private static readonly Lazy<IReadOnlyList<YubiKeyTestState>> LazyAuthorizedDevices =
        new(() => InitializeDevicesAsync().GetAwaiter().GetResult());

    public static IReadOnlyList<YubiKeyTestState> AllAuthorizedDevices => LazyAuthorizedDevices.Value;

    private static async Task<IReadOnlyList<YubiKeyTestState>> InitializeDevicesAsync()
    {
        // 1. Discover all YubiKeys
        var devices = await TestDeviceDiscovery.FindAllAsync();

        // 2. Load allow list via provider
        var provider = new AppSettingsAllowListProvider();
        var allowList = new AllowList(provider);

        // 3. Filter to authorized devices only
        foreach (var device in devices)
        {
            if (allowList.IsDeviceAllowed(device.SerialNumber))
                authorizedDevices.Add(/* ... */);
        }

        return authorizedDevices;
    }
}
```

**Why Lazy<> pattern:**
- Device discovery is expensive (~500ms per device)
- Initialization happens only when first test accesses devices
- CLR guarantees thread-safe initialization
- `GetAwaiter().GetResult()` bridges async initialization to static context

### 4. Allow List Security Layer

```csharp
public sealed class AllowList
{
    private readonly HashSet<int> _allowedSerials;

    public AllowList(IAllowListProvider provider, ILogger<AllowList>? logger = null)
    {
        _allowedSerials = [..provider.GetList()];

        if (_allowedSerials.Count == 0)
        {
            Console.Error.WriteLine(provider.OnInvalidInputErrorMessage());
            Environment.Exit(-1); // Hard fail - cannot continue without allow list
        }
    }

    public bool IsDeviceAllowed(int? serialNumber) =>
        serialNumber is not null && _allowedSerials.Contains(serialNumber.Value);
}
```

**Why IAllowListProvider abstraction:**
- Allows testing AllowList without actual config files
- Production uses `AppSettingsAllowListProvider` (reads from appsettings.json)
- Tests can inject mock providers

**Why Environment.Exit:**
- Must prevent ANY test from running on unauthorized devices
- Clear, loud failure that cannot be caught
- Forces developer to explicitly authorize devices

### 5. FilterCriteria and Serialization

`FilterCriteria` stores all filtering parameters and handles xUnit serialization:

```csharp
public record FilterCriteria
{
    // Standard properties that serialize directly
    public string? MinFirmware { get; init; }
    public FormFactor FormFactor { get; init; }
    // ...

    // Type cannot be serialized, so we store the assembly-qualified name
    public Type? CustomFilterType { get; init; }
    public string? CustomFilterTypeName { get; init; }

    // Before serialization: convert Type to string
    public FilterCriteria PrepareForSerialization() =>
        this with { CustomFilterTypeName = CustomFilterType?.AssemblyQualifiedName };

    // After deserialization: resolve string back to Type
    public FilterCriteria ResolveCustomFilterType() =>
        this with { CustomFilterType = Type.GetType(CustomFilterTypeName!) };
}
```

## Testing the Test Infrastructure

**Problem:** How to test code that may call `Environment.Exit(-1)`?

**Solution:** Provider abstraction for testability:

```csharp
// Production
public sealed class AppSettingsAllowListProvider : IAllowListProvider
{
    // Reads from appsettings.json
}

// Tests
public class FakeAllowListProvider : IAllowListProvider
{
    private readonly List<int> _serials;

    public IReadOnlyList<int> GetList() => _serials;
    public string OnInvalidInputErrorMessage() => "Test error";
}
```

## Common Implementation Patterns

### Adding a New Application Extension

When adding support for a new application (e.g., OATH, PIV):

1. **Create extension method** in `YubiKeyTestStateExtensions.cs`:

```csharp
public static async Task WithOathAsync(
    this YubiKeyTestState state,
    Func<OathSession, DeviceInfo, Task> action,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default)
{
    await using var connection = await state.Device.OpenConnectionAsync<ISmartCardConnection>(cancellationToken);

    await using var session = await OathSession.CreateAsync(
        connection,
        scpKeyParams: scpKeyParams,
        cancellationToken: cancellationToken);

    await action(session, state.DeviceInfo);
}
```

### Adding New Filter Criteria

1. Add property to `WithYubiKeyAttribute`
2. Add corresponding property to `FilterCriteria`
3. Update `YubiKeyTestInfrastructure.FilterDevices()` to apply the filter
4. Update `FilterCriteria.GetShortDescription()` for test names
5. Document in README.md

## Performance Considerations

Device discovery has fixed costs:
- USB enumeration: ~100ms
- SmartCard enumeration: ~200ms
- Per-device `GetDeviceInfo`: ~100ms per device

Static cache eliminates per-test cost, making this worthwhile for test suites with >5 tests.

## Debugging Tips

### Test Not Running

**Check:**
1. Device serial in allow list? (`appsettings.json`)
2. Device matches filter criteria?
3. Device discovery succeeded?

### Environment.Exit During Discovery

**Causes:**
- Allow list is empty or missing
- Cannot read appsettings.json

**Fix:**
- Add serial to `appsettings.json`
- Verify appsettings.json copied to output directory

## Known Limitations

1. **Static device cache:** Devices must be connected before test runner starts
2. **Environment.Exit:** Cannot recover from allow list violations
3. **Single device per test case:** Each `[WithYubiKey]` attribute produces one test case per matching device

## Security Considerations

- **Never commit real serial numbers** - Use environment variables or .gitignored appsettings.json
- **Use separate test devices** - Never test on production or user devices
- **Verify allow list is configured** - Tests fail fast if misconfigured
