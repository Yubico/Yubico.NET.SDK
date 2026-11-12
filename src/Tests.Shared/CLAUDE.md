# CLAUDE.md - Tests.Shared Module

This file provides AI agent guidance for working with the YubiKit Integration Test Infrastructure.
**Read [README.md](README.md) first** for comprehensive user documentation.

## Module Context

Tests.Shared provides the foundation for all YubiKit integration tests. It implements:
- **Allow-list filtering** - Authorizes test-body fixtures after discovery and live identification reads
- **Attribute-based test discovery** - xUnit integration for declarative device filtering
- **Device caching** - Single device discovery per test run for performance
- **Extension methods** - Fluent API for session management across applications

**Key Implementation Files:**
- `Infrastructure/WithYubiKeyAttribute.cs` - xUnit DataAttribute for device-parameterized tests
- `Infrastructure/YubiKeyTestInfrastructure.cs` - Device discovery and caching
- `Infrastructure/TransientScanRetry.cs` - Finite PC/SC worker-admission retry for integration scans
- `Infrastructure/AllowList.cs` - Security layer preventing unauthorized testing
- `YubiKeyTestState.cs` - Device wrapper implementing `IXunitSerializable`
- `YubiKeyTestStateExtensions.cs` - Application-specific session helpers
- `RecordingSmartCardConnection.cs` - xUnit-free SmartCard APDU recorder for byte-level unit tests

`Core.UnitTests` links `Infrastructure/TransientScanRetry.cs` as source so its xUnit v3 project does
not reference this xUnit v2-based utility assembly or the Management dependency. Keep the helper
xUnit-free, and do not add a Tests.Shared project reference to Core.UnitTests because that would
compile the same type twice.

## Critical Design Patterns

### 1. xUnit DataAttribute Integration

The structural snippets in sections 1, 4 and 5 are abbreviated implementation sketches, not replacement
implementations or copy-paste tests. Read the actual named source files before changing the harness.

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
- The attribute itself performs no device access; it serializes criteria into one placeholder.

**Execution phase** (when tests actually run):
- Test receives placeholder `YubiKeyTestState`
- Accessing `Device` property triggers `BindToRealDevice()`
- `YubiKeyTestInfrastructure.FilterDevices()` finds candidates; the placeholder binds to the first match
- The same placeholder stores the first matching authorized state; it does not produce one row per key.

### 3. Lazy Static Device Cache

`LazyAuthorizedDevices` invokes the synchronous `InitializeDevicesAsync` method once. That method bridges
the retry-wrapped `YubiKeyManager.FindAllAsync(interfaces: ConnectionType.All)` call with
`GetAwaiter().GetResult()`, then performs live `GetDeviceInfoAsync` reads before serial authorization.
It caches authorized views with the read metadata. This cache is local to the test process and is not a
Core monitor or a guarantee of stable device state across the run.

**Why Lazy<> pattern:**
- Device discovery and live identification involve interface opens and device I/O; no fixed latency is promised.
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

        if (_allowedSerials.Count == 0 && !provider.AllowUnknownSerials)
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

**Actual authorization outcomes:**
- The empty-list constructor exits only when `AllowUnknownSerials` is false.
- Initialization reads metadata before filtering disallowed or unreadable serials. It does not exit for
  each rejected device. Per-device read errors are also filtered.
- No authorized devices produces a cached empty list, then `SkipException` at lazy row binding;
  `[SkippableTheory]` handles that as a skip. Exhausted transient scan retries propagate separately.
- Unknown-serial authorization deliberately broadens the fixture set; keep unauthorized hardware off
  the rig. The allow list is not a pre-I/O barrier for discovery or identification.

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
    await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>(cancellationToken);

    await using var session = await OathSession.CreateAsync(
        connection,
        new SessionCreationOptions { ScpKeyParameters = scpKeyParams },
        cancellationToken: cancellationToken);

    await action(session, state.DeviceInfo);
}
```

### Sharing a SmartCard Connection Across Sessions

Pass the connection straight through. A session is a user of the connection it is handed, so disposing a reset session leaves the connection open for the test session that follows; the `WithConnectionAsync` owner disposes it. This used to need a non-owning `SharedSmartCardConnection` wrapper, which is gone — non-owning is the default now.

One live session per connection: dispose the reset session before creating the test session, or the second construction throws `ConnectionInUseException`. Scope the reset session to its own `using` block.

**OATH reset exception:** successful OATH RESET ends plain and SCP sessions. Close/reopen even a borrowed
connection before creating the body session; the OATH module's `WithOathSessionAsync` uses separate owned
reset/body connections. Do not copy same-connection reset setup into OATH. Other applets are unchanged;
see [OATH reset boundaries](../Oath/CLAUDE.md#reset-and-timestamp-boundaries).

### Recording SmartCard APDUs In Unit Tests

Use `RecordingSmartCardConnection` when a unit test needs to assert exact APDU bytes without hardware. The helper records every transmitted command and returns queued raw responses. Keep it narrow: do not turn it into an APDU DSL, protocol emulator, or replacement for `[WithYubiKey]` integration tests.

### Adding New Filter Criteria

1. Add property to `WithYubiKeyAttribute`
2. Add corresponding property to `FilterCriteria`
3. Update `YubiKeyTestInfrastructure.FilterDevices()` to apply the filter
4. Update `FilterCriteria.GetShortDescription()` for test names
5. Document in README.md

## Performance Considerations

The static cache avoids repeating the initial scan and metadata reads for each test row. Actual latency
depends on the attached devices, enabled interfaces, operating system and native calls. This is a source
description of reuse, not benchmark evidence or a per-test latency bound. Session helpers and explicit
live metadata reads still perform their own I/O.

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
2. **Authorization outcomes:** Empty configuration can exit; rejected devices are filtered and an empty
   fixture result causes row-binding skips. Discovery/metadata reads precede per-device authorization.
3. **Single device per attribute:** Each `[WithYubiKey]` attribute yields one placeholder bound to the
   first matching authorized device at execution. Multiple attributes bind independently and may choose
   the same key; this is not automatic execution over every matching device.
4. **Cached callback metadata:** `WithManagementAsync` passes `state.DeviceInfo` without refreshing it.
   A session initialization read is separate from the callback snapshot. Read explicitly for fresh settings.
5. **Filtering versus pinning:** Connection filters select eligible devices; pass a concrete
   `preferredConnection` and assert `mgmt.ConnectionType` when operation transport matters. The unfiltered
   state's multi-flag set is not a valid preference. Helper argument order is `action`, `configuration`,
   `scpKeyParams`, `preferredConnection`, `cancellationToken`; use names for policy options.
6. **Metadata filters:** Malformed firmware strings are ignored; use three components. Capability checks
   accept USB or NFC enabled bits independently of `RequireUsb`. Current Core FIPS-tag mapping is wrong,
   so FIPS masks/filters cannot establish application approval. Product flag bits can remain in
   `FormFactor`, causing exact base-enum filters to miss flagged devices.

## Security Considerations

The allow list **is** the authorization boundary. See [docs/TESTING.md](../../docs/TESTING.md#hardware-authorization) for the canonical policy; this section must not diverge from it.

- **The committed allow list is the mechanism, by design.** `AppSettingsAllowListProvider` reads exactly one source: `appsettings.json` in the output directory. There is no environment-variable override, no `appsettings.Local.json`, and no user secrets. Serials of dedicated team test devices belong in that committed file.
- **Only dedicated test devices.** Adding a serial authorizes destructive operations against that device — application resets, key overwrites, credential deletion. Never add a personal or production key, including one used for SSH, FIDO2/WebAuthn, PIV certificates, or OATH.
- **Destruction is not the gate.** Integration tests reset applications and overwrite keys on allow-listed devices; that is what the harness exists for. What requires a human is *presence and timing* — User Presence/touch, UV/PIN ceremonies, FIDO2 reset power-cycle windows, and physical insert/remove. Do not add a second config gate on top of the allow list; it only obscures the real boundary.
- **Verify the allow list is configured** - Tests fail fast if misconfigured (`Environment.Exit(-1)`).
