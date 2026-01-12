# Session API Code Review

## Executive Summary

This document analyzes `ManagementSession` and `SecurityDomainSession` as foundational templates for future YubiKey application sessions (PIV, OTP, FIDO, OATH, YubiHSM, OpenPGP).

**Overall Assessment:** The current implementations are functional but have **divergent patterns** that will create maintenance burden and inconsistent developer experience if propagated to future sessions.

---

## Transport Requirements by Application

Understanding which transports support which applications is critical for session design:

| Transport | Applications | Notes |
|-----------|--------------|-------|
| **OTP (HID)** | OTP only | Single application |
| **FIDO (HID)** | U2F, FIDO2 | FIDO-specific transport |
| **SmartCard (CCID/PCSC)** | OATH, PIV, OpenPGP, HSMAUTH, FIDO2 | Multi-application, FIDO2 optionally supported |

**Multi-Transport Sessions:**
- `ManagementSession`: SmartCard + FIDO (uses Backend pattern)
- `FidoSession` (future): FIDO HID + SmartCard (will need Backend pattern)

**Single-Transport Sessions:**
- `SecurityDomainSession`: SmartCard only
- `OtpSession` (future): OTP HID only
- `PivSession` (future): SmartCard only
- `OathSession` (future): SmartCard only
- `OpenPgpSession` (future): SmartCard only
- `YubiHsmSession` (future): SmartCard only

---

## Part 1: Structural Comparison

### Class Signatures

| Aspect | ManagementSession | SecurityDomainSession |
|--------|-------------------|----------------------|
| **Inheritance** | `ApplicationSession` | `ApplicationSession` |
| **Sealed** | Yes | Yes |
| **Generic** | No (was previously generic) | No |
| **Lines of Code** | ~254 | ~1184 |
| **Connection Types** | SmartCard, FIDO | SmartCard only |

### Constructor Pattern

**ManagementSession:**
```csharp
private ManagementSession(
    IConnection connection,          // Abstract base
    ILoggerFactory loggerFactory,
    ScpKeyParameters? scpKeyParams = null)
```

**SecurityDomainSession:**
```csharp
private SecurityDomainSession(
    ISmartCardConnection connection,  // Concrete type
    ILoggerFactory loggerFactory,
    ScpKeyParameters? scpKeyParams = null)
```

**Issue:** ManagementSession accepts `IConnection` (polymorphic), while SecurityDomainSession is tightly coupled to `ISmartCardConnection`.

### Factory Method Pattern

Both use the same factory method pattern:

```csharp
public static async Task<XxxSession> CreateAsync(
    IConnection connection,
    ProtocolConfiguration? configuration = null,
    ILoggerFactory? loggerFactory = null,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default)
```

**Consistency:** The parameter order and types are **identical**. This is good.

### Initialization Pattern

**ManagementSession:**
```csharp
private async Task InitializeAsync(ProtocolConfiguration? configuration, CancellationToken ct)
{
    _version = await GetVersionAsync(ct);           // Step 1: Get version
    _protocol.Configure(_version, configuration);   // Step 2: Configure

    if (_scpKeyParams is not null && _protocol is ISmartCardProtocol sc)
    {
        _protocol = await sc.WithScpAsync(_scpKeyParams, ct);  // Step 3: SCP
        _backend = new SmartCardBackend(_protocol, _version);   // Step 4: Recreate backend
    }
}
```

**SecurityDomainSession:**
```csharp
private async Task InitializeAsync(ProtocolConfiguration? configuration, CancellationToken ct)
{
    var protocol = EnsureBaseProtocol();
    await protocol.SelectAsync(ApplicationIds.SecurityDomain, ct);  // Step 1: Select app
    protocol.Configure(FirmwareVersion.V5_3_0, configuration);      // Step 2: Configure (hardcoded version!)

    if (_scpKeyParams is not null)
    {
        _protocol = await protocol.WithScpAsync(_scpKeyParams, ct); // Step 3: SCP
        IsAuthenticated = true;                                      // Step 4: Track state
    }
}
```

**Issues:**
1. SecurityDomainSession hardcodes `FirmwareVersion.V5_3_0` instead of detecting it
2. ManagementSession uses Backend pattern; SecurityDomainSession doesn't
3. Initialization order differs (version detection vs. app selection first)
4. SecurityDomainSession tracks `IsAuthenticated`; ManagementSession doesn't

---

## Part 2: Pattern Analysis

### 2.1 Backend Pattern (Multi-Transport Sessions)

ManagementSession uses an internal `IManagementBackend` abstraction:

```csharp
internal interface IManagementBackend : IDisposable
{
    ValueTask<byte[]> ReadConfigAsync(int page, CancellationToken ct);
    ValueTask WriteConfigAsync(byte[] config, CancellationToken ct);
    ValueTask SetModeAsync(byte[] data, CancellationToken ct);
    ValueTask DeviceResetAsync(CancellationToken ct);
}
```

With implementations:
- `SmartCardBackend` - ISO 7816 APDU encoding
- `FidoBackend` - CTAP vendor command encoding

**SecurityDomainSession does NOT use this pattern** - it embeds APDU construction directly in methods (acceptable since it's single-transport).

**Recommendation:** Sessions supporting multiple transports should **adopt the Backend pattern**:
- `ManagementSession`: Currently uses Backend (SmartCard + FIDO) ✅
- `FidoSession` (future): **Must use Backend** (FIDO HID + SmartCard)
- Single-transport sessions (PIV, OATH, OpenPGP, HSMAUTH, OTP): Backend pattern optional (can embed directly)

### 2.2 Protocol Management

| Aspect | ManagementSession | SecurityDomainSession |
|--------|-------------------|----------------------|
| **Base Protocol Field** | No | `_baseProtocol` |
| **Active Protocol Field** | `_protocol` | `_protocol` |
| **Protocol Creation** | In constructor | Lazy via `EnsureBaseProtocol()` |
| **Protocol Ownership** | Owns and disposes | Owns and disposes |

**Issue:** SecurityDomainSession keeps both `_baseProtocol` and `_protocol`, which is confusing. ManagementSession's approach of recreating the backend after SCP wrapping is cleaner.

### 2.3 Feature Detection

**ManagementSession:**
```csharp
private static readonly Feature FeatureDeviceInfo = new("Device Info", 4, 1, 0);
private static readonly Feature FeatureSetConfig = new("Set Config", 5, 0, 0);
private static readonly Feature FeatureDeviceReset = new("Device Reset", 5, 6, 0);

private void EnsureSupports(Feature feature)
{
    if (!IsSupported(feature))
        throw new NotSupportedException($"{feature.Name} is not supported on this YubiKey.");
}
```

**SecurityDomainSession:** No feature detection - assumes firmware 5.3.0+

**Recommendation:** All sessions should have feature detection for version-gated functionality.

### 2.4 Logging

Both use `ILogger<T>` correctly:

```csharp
// ManagementSession
_logger.LogDebug("Management session initialized with protocol {ProtocolType}", _protocol.GetType().Name);

// SecurityDomainSession
_logger.LogDebug("Sending GET DATA for object 0x{DataObject:X4}", dataObject);
_logger.LogInformation("CA issuer SKI stored (KeyReference: {KeyReference})", keyReference);
```

**Issue:** Logging levels are inconsistent:
- ManagementSession: Mostly `Debug`
- SecurityDomainSession: Mix of `Debug`, `Information`, `Warning`, `Error`

**Recommendation:** Standardize logging levels:
- `Trace`: Wire-level data (APDUs)
- `Debug`: Method entry/exit, internal state changes
- `Information`: Significant operations completed
- `Warning`: Recoverable errors, fallback paths
- `Error`: Unrecoverable errors before throw

### 2.5 Disposal

**ManagementSession:**
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _protocol?.Dispose();
    }
    base.Dispose(disposing);
}
```

**SecurityDomainSession:**
```csharp
protected override void Dispose(bool disposing)
{
    if (!disposing) return;

    _protocol?.Dispose();
    _protocol = null;
    _baseProtocol?.Dispose();
    _baseProtocol = null;
    _isInitialized = false;

    base.Dispose(disposing);
}
```

**Issue:** SecurityDomainSession disposes two protocols and resets fields. This suggests the lifecycle is more complex than necessary.

---

## Part 3: Developer Persona Analysis

### 3.1 SDK Developer (Internal)

**Current Experience:**

| Task | ManagementSession | SecurityDomainSession |
|------|-------------------|----------------------|
| Add new method | Simple - add to backend interface | Copy/paste APDU pattern |
| Support new transport | Add backend implementation | Major refactor required |
| Add SCP support | Works automatically | Works automatically |
| Add feature gating | Use `EnsureSupports()` | Manual firmware check |

**Pain Points:**
1. No shared base class logic for common patterns (SCP init, version detection, logging)
2. SecurityDomainSession is 5x larger - maintenance burden
3. No interface for sessions - can't mock in unit tests

**Suggested Improvements:**
- Extract `SessionBase<TProtocol>` with common initialization
- Define `IManagementSession`, `ISecurityDomainSession` interfaces
- Standardize on Backend pattern for multi-transport sessions

### 3.2 CLI Developer

**Typical Usage:**
```csharp
// Current: Multiple options, inconsistent patterns
var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
var device = devices.First();

// Option 1: Direct factory
using var connection = await device.ConnectAsync<ISmartCardConnection>();
using var session = await ManagementSession.CreateAsync(connection);

// Option 2: Extension method
using var session = await device.CreateManagementSessionAsync();

// Option 3: One-liner
var deviceInfo = await device.GetDeviceInfoAsync();
```

**Pain Points:**
1. SecurityDomainSession has no `IYubiKeyExtensions` - must create connection manually
2. No synchronous APIs for simple scripts
3. Connection type discovery is manual (`ConnectionType.Ccid` vs `ConnectionType.Hid`)

**Suggested Improvements:**
```csharp
// Unified extension pattern for all sessions
using var sdSession = await device.CreateSecurityDomainSessionAsync();
using var mgmtSession = await device.CreateManagementSessionAsync();
using var pivSession = await device.CreatePivSessionAsync();

// High-level convenience methods
await device.ResetSecurityDomainAsync();
var keyInfo = await device.GetSecurityDomainKeyInfoAsync();
```

### 3.3 API/Service Developer

**Typical Usage (ASP.NET Core):**
```csharp
services.AddYubiKeyManagerCore();
services.AddSingleton<ManagementSessionFactoryDelegate>(sp =>
    (connection, ct) => ManagementSession.CreateAsync(connection,
        loggerFactory: sp.GetService<ILoggerFactory>(),
        cancellationToken: ct));
```

**Pain Points:**
1. Factory delegate pattern is verbose
2. No DI-friendly interfaces
3. Session lifetime doesn't align with HTTP request lifetime (device reuse issues)
4. No built-in pooling or session management

**Suggested Improvements:**
```csharp
// Simplified DI registration
services.AddYubiKeyManagement();  // Registers ManagementSession factory
services.AddYubiKeySecurityDomain();  // Registers SecurityDomainSession factory

// Interface-based injection
public class MyController(IManagementSessionFactory sessionFactory) { }
```

### 3.4 Web App Developer

**Current Pain Points:**
1. Device enumeration is global - doesn't work well with multi-tenant scenarios
2. No session affinity helpers
3. Connection timeout/retry logic must be implemented by consumer
4. Reboot handling (after config change) is manual

**Suggested Improvements:**
- Add `DeviceId` to session for tracking
- Add `Reconnect()` method for post-reboot scenarios
- Add configurable timeout/retry policies

### 3.5 Service Developer (Background Services)

**Typical Usage:**
```csharp
public class YubiKeyMonitorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
            foreach (var device in devices)
            {
                using var session = await ManagementSession.CreateAsync(
                    await device.ConnectAsync<ISmartCardConnection>(ct),
                    cancellationToken: ct);
                // Process device...
            }
            await Task.Delay(5000, ct);
        }
    }
}
```

**Pain Points:**
1. No device event subscription in session API
2. No connection health checking
3. Device removal during operation throws cryptic errors
4. No graceful shutdown support

**Suggested Improvements:**
- Add `IObservable<SessionState>` for monitoring
- Add `IsConnected` property
- Better exception types for connection loss

### 3.6 PowerShell Developer

**Current Experience:** Must use .NET interop, no cmdlets

**Pain Points:**
1. No PowerShell module
2. Async-only APIs are awkward in PowerShell
3. `using` statements don't work naturally in PS

**Suggested Improvements:**
- Add sync wrappers: `GetDeviceInfo()` (blocking)
- Consider PowerShell module wrapper

### 3.7 IoT Developer

**Pain Points:**
1. Heavy dependencies (Microsoft.Extensions.*)
2. No trimming-friendly attributes
3. ArrayPool usage is good, but inconsistent between sessions
4. No AOT compilation support verification

**Suggested Improvements:**
- Mark classes with `[DynamicallyAccessedMembers]` where needed
- Add minimal/embedded build configuration
- Ensure consistent memory management patterns

---

## Part 4: Pain Points Summary

### Critical Issues (Must Fix)

| Issue | Impact | Affected |
|-------|--------|----------|
| **No session interfaces** | Can't mock/test consumer code | All developers |
| **Inconsistent initialization** | Hard to document, maintain | SDK devs |
| **SecurityDomainSession lacks IYubiKeyExtensions** | Friction for CLI/simple use | CLI/Script devs |
| **Hardcoded firmware version in SD** | Won't adapt to future versions | All |

### High Priority Issues

| Issue | Impact | Affected |
|-------|--------|----------|
| Backend pattern not universal | Multi-transport sessions inconsistent | SDK devs |
| Logging levels inconsistent | Hard to filter/debug | Service devs |
| No feature detection in SD | Runtime errors instead of clear messages | All |
| Two-protocol lifecycle in SD | Confusing, error-prone | SDK devs |

### Medium Priority Issues

| Issue | Impact | Affected |
|-------|--------|----------|
| No reconnect/retry helpers | Manual implementation required | Service devs |
| No sync APIs | PowerShell friction | PS devs |
| Factory delegate pattern verbose | DI boilerplate | API devs |

---

## Part 5: Recommended Template Pattern

### Proposed Base Class

```csharp
public abstract class YubiKitSession<TProtocol> : IDisposable
    where TProtocol : IProtocol
{
    protected readonly ILogger Logger;
    protected TProtocol Protocol { get; private set; }
    protected FirmwareVersion Version { get; private set; }

    public bool IsInitialized { get; private set; }
    public bool IsAuthenticated { get; protected set; }

    protected abstract byte[] ApplicationId { get; }
    protected abstract FirmwareVersion MinimumVersion { get; }

    protected YubiKitSession(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }

    protected async Task InitializeCoreAsync(
        TProtocol protocol,
        ProtocolConfiguration? config,
        ScpKeyParameters? scpParams,
        CancellationToken ct)
    {
        // 1. Select application
        await protocol.SelectAsync(ApplicationId, ct);

        // 2. Detect version
        Version = await DetectVersionAsync(ct);

        // 3. Validate minimum version
        if (Version < MinimumVersion)
            throw new NotSupportedException($"Requires firmware {MinimumVersion}+");

        // 4. Configure protocol
        protocol.Configure(Version, config);

        // 5. Establish SCP if requested
        if (scpParams is not null && protocol is ISmartCardProtocol sc)
        {
            Protocol = (TProtocol)(object)await sc.WithScpAsync(scpParams, ct);
            IsAuthenticated = true;
        }
        else
        {
            Protocol = protocol;
        }

        // 6. Post-initialization hook
        await OnInitializedAsync(ct);
        IsInitialized = true;
    }

    protected virtual Task<FirmwareVersion> DetectVersionAsync(CancellationToken ct)
        => Task.FromResult(new FirmwareVersion());

    protected virtual Task OnInitializedAsync(CancellationToken ct)
        => Task.CompletedTask;

    protected void EnsureSupports(Feature feature)
    {
        if (Version < feature.Version)
            throw new NotSupportedException($"{feature.Name} requires firmware {feature.Version}+");
    }

    protected void EnsureInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Session not initialized");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Protocol?.Dispose();
        }
    }
}
```

### Proposed Interface Pattern

```csharp
public interface IManagementSession : IDisposable
{
    Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken ct = default);
    Task SetDeviceConfigAsync(DeviceConfig config, bool reboot,
        byte[]? currentLockCode = null, byte[]? newLockCode = null,
        CancellationToken ct = default);
    Task ResetDeviceAsync(CancellationToken ct = default);
}

public interface ISecurityDomainSession : IDisposable
{
    bool IsAuthenticated { get; }
    Task<IReadOnlyDictionary<KeyReference, IReadOnlyDictionary<byte, byte>>>
        GetKeyInformationAsync(CancellationToken ct = default);
    Task PutKeyAsync(KeyReference keyRef, StaticKeys keys, int replaceKvn = 0,
        CancellationToken ct = default);
    Task ResetAsync(CancellationToken ct = default);
    // ... other methods
}
```

### Proposed Extension Pattern (All Sessions)

```csharp
// In each session module
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        // High-level convenience
        public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken ct = default)
        {
            using var session = await CreateManagementSessionAsync(ct);
            return await session.GetDeviceInfoAsync(ct);
        }

        // Session factory
        public async Task<ManagementSession> CreateManagementSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? config = null,
            ILoggerFactory? loggerFactory = null,
            CancellationToken ct = default)
        {
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(ct);
            return await ManagementSession.CreateAsync(
                connection, config, loggerFactory, scpKeyParams, ct);
        }
    }
}
```

---

## Part 6: Checklist for Future Sessions

### Session Implementation Checklist

- [ ] **Inherit from base class** (when created) or follow pattern exactly
- [ ] **Implement interface** for testability
- [ ] **Private constructor** with public `CreateAsync` factory
- [ ] **Accept `IConnection`** (not concrete type) if multi-transport
- [ ] **Detect firmware version** - never hardcode
- [ ] **Use Backend pattern** if supporting multiple transports:
  - Required for: `ManagementSession`, `FidoSession`
  - Optional for single-transport: `SecurityDomainSession`, `PivSession`, `OathSession`, `OpenPgpSession`, `YubiHsmSession`, `OtpSession`
- [ ] **Add feature detection** with `EnsureSupports()`
- [ ] **Consistent logging levels** per guidelines
- [ ] **Single protocol field** - no base/wrapped split
- [ ] **Proper disposal** - dispose protocol only

### Extension Method Checklist

- [ ] **Add `IYubiKeyExtensions`** with `extension(IYubiKey)` syntax
- [ ] **Provide `CreateXxxSessionAsync()`** factory extension
- [ ] **Provide high-level convenience methods** (e.g., `GetDeviceInfoAsync()`)
- [ ] **Match parameter order** with `CreateAsync`: connection, config, loggerFactory, scpKeyParams, ct

### Test Infrastructure Checklist

- [ ] **Add `XxxTestState`** class if session needs reset/state management
- [ ] **Add `WithXxxSessionAsync()`** test extension
- [ ] **Document reset behavior** clearly
- [ ] **Use `[WithYubiKey]` attribute** for device filtering

---

## Appendix: Code Metrics

### ManagementSession (254 lines)
- Public methods: 4 (`CreateAsync`, `GetDeviceInfoAsync`, `SetDeviceConfigAsync`, `ResetDeviceAsync`)
- Private methods: 9
- Fields: 7
- Constants: 1

### SecurityDomainSession (1184 lines)
- Public methods: 18
- Private methods: 16
- Fields: 8
- Constants: 23

### Complexity Assessment
- ManagementSession: Low complexity, good separation via Backend pattern
- SecurityDomainSession: High complexity, monolithic, needs refactoring

---

*Review completed: 2026-01-12*
*Reviewer: Claude Code*
