# Static LoggerFactory Pattern Proposal

## Overview

Replace per-method `ILoggerFactory` parameters with a static, SDK-wide logging configuration.

## Proposed Implementation

### Core Logging Infrastructure

```csharp
// Yubico.YubiKit.Core/src/YubiKitLogging.cs
namespace Yubico.YubiKit.Core;

/// <summary>
/// Central logging configuration for all YubiKit components.
/// Configure once at application startup; all sessions will use this logger.
/// </summary>
public static class YubiKitLogging
{
    private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets or sets the LoggerFactory used by all YubiKit components.
    /// Default is NullLoggerFactory (no logging).
    /// </summary>
    /// <remarks>
    /// Set this once at application startup before creating any sessions.
    /// Thread-safe but not intended for runtime changes.
    /// </remarks>
    public static ILoggerFactory LoggerFactory
    {
        get
        {
            lock (_lock) return _loggerFactory;
        }
        set
        {
            lock (_lock) _loggerFactory = value ?? NullLoggerFactory.Instance;
        }
    }

    /// <summary>
    /// Creates a logger for the specified type.
    /// </summary>
    internal static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

    /// <summary>
    /// Creates a logger with the specified category name.
    /// </summary>
    internal static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
}
```

### Simplified Session Factory

```csharp
// ManagementSession.cs - AFTER
public sealed class ManagementSession : ApplicationSession
{
    private readonly ILogger<ManagementSession> _logger;

    private ManagementSession(IConnection connection, ScpKeyParameters? scpKeyParams = null)
    {
        _logger = YubiKitLogging.CreateLogger<ManagementSession>();
        // ... rest of constructor
    }

    public static async Task<ManagementSession> CreateAsync(
        IConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,  // loggerFactory removed!
        CancellationToken cancellationToken = default)
    {
        var session = new ManagementSession(connection, scpKeyParams);
        await session.InitializeAsync(configuration, cancellationToken);
        return session;
    }
}
```

### DI Integration

```csharp
// DependencyInjection.cs
public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds YubiKit services and configures logging from the DI container.
        /// </summary>
        public IServiceCollection AddYubiKit(Action<YubiKitOptions>? configure = null)
        {
            // Configure logging from DI container
            services.AddSingleton<IConfigureOptions<YubiKitOptions>>(sp =>
            {
                return new ConfigureOptions<YubiKitOptions>(options =>
                {
                    // Auto-wire LoggerFactory from DI if available
                    var loggerFactory = sp.GetService<ILoggerFactory>();
                    if (loggerFactory is not null)
                    {
                        YubiKitLogging.LoggerFactory = loggerFactory;
                    }
                });
            });

            // Register session factories
            services.AddSingleton<ManagementSessionFactory>(sp =>
                (conn, scp, ct) => ManagementSession.CreateAsync(conn, null, scp, ct));

            services.AddYubiKeyManagerCore(configure);
            return services;
        }
    }
}
```

---

## Developer Persona Analysis

### CLI Developer

**Before:**
```csharp
// Must understand ILoggerFactory, create one, pass it everywhere
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
using var session = await ManagementSession.CreateAsync(
    connection,
    loggerFactory: loggerFactory);  // Boilerplate
```

**After:**
```csharp
// Configure once at startup (optional)
YubiKitLogging.LoggerFactory = LoggerFactory.Create(b => b.AddConsole());

// Then forget about it
using var session = await ManagementSession.CreateAsync(connection);
```

**Verdict: BETTER** - Less boilerplate, simpler API.

---

### API/Service Developer (ASP.NET Core)

**Before:**
```csharp
// Program.cs
builder.Services.AddSingleton<ManagementSessionFactoryDelegate>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();  // Must capture
    return (conn, scp, ct) => ManagementSession.CreateAsync(
        conn, null, loggerFactory, scp, ct);  // Must forward
});

// Controller
public class YubiKeyController(ManagementSessionFactoryDelegate factory)
{
    public async Task<IActionResult> GetInfo()
    {
        using var session = await factory(connection, null, ct);
        // ...
    }
}
```

**After:**
```csharp
// Program.cs
builder.Services.AddYubiKit();  // Automatically wires ILoggerFactory from DI

// Controller
public class YubiKeyController(ManagementSessionFactory factory)
{
    public async Task<IActionResult> GetInfo()
    {
        using var session = await factory(connection, null, ct);
        // ...
    }
}
```

**Verdict: BETTER** - Auto-wiring eliminates manual threading.

---

### Web App Developer

**Before:**
```csharp
// Must remember to pass loggerFactory from DI
services.AddScoped(sp =>
{
    var loggerFactory = sp.GetService<ILoggerFactory>();
    return new YubiKeyService(loggerFactory);  // Forward to service
});

public class YubiKeyService(ILoggerFactory? loggerFactory)
{
    public async Task<DeviceInfo> GetInfo(IYubiKey device)
    {
        using var session = await device.CreateManagementSessionAsync(
            loggerFactory: loggerFactory);  // Must thread through
        return await session.GetDeviceInfoAsync();
    }
}
```

**After:**
```csharp
// Configure once
services.AddYubiKit();  // Done!

public class YubiKeyService
{
    public async Task<DeviceInfo> GetInfo(IYubiKey device)
    {
        using var session = await device.CreateManagementSessionAsync();
        return await session.GetDeviceInfoAsync();
    }
}
```

**Verdict: MUCH BETTER** - No parameter threading.

---

### Service Developer (Background Services)

**Before:**
```csharp
public class YubiKeyMonitorService(ILoggerFactory loggerFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var device in devices)
            {
                using var session = await ManagementSession.CreateAsync(
                    connection,
                    loggerFactory: loggerFactory);  // Every. Single. Time.
            }
        }
    }
}
```

**After:**
```csharp
public class YubiKeyMonitorService : BackgroundService
{
    // No constructor injection needed for logging!
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var device in devices)
            {
                using var session = await ManagementSession.CreateAsync(connection);
            }
        }
    }
}
```

**Verdict: BETTER** - Cleaner code, less ceremony.

---

### PowerShell Developer

**Before:**
```powershell
# Must create LoggerFactory in PowerShell - painful!
$loggerFactory = [Microsoft.Extensions.Logging.LoggerFactory]::Create({
    param($builder)
    $builder.AddConsole()
})

$session = [ManagementSession]::CreateAsync($connection, $null, $loggerFactory, $null, [CancellationToken]::None).Result
```

**After:**
```powershell
# Optional: configure logging once
[YubiKitLogging]::LoggerFactory = [LoggerFactory]::Create(...)

# Or just skip logging entirely - works fine!
$session = [ManagementSession]::CreateAsync($connection).Result
```

**Verdict: MUCH BETTER** - Can ignore logging entirely or configure once.

---

### IoT Developer

**Before:**
```csharp
// Must pass NullLoggerFactory explicitly to avoid pulling in logging dependencies
using var session = await ManagementSession.CreateAsync(
    connection,
    loggerFactory: NullLoggerFactory.Instance);  // Explicit null
```

**After:**
```csharp
// Default is NullLoggerFactory - just don't configure anything
using var session = await ManagementSession.CreateAsync(connection);
```

**Verdict: BETTER** - Cleaner, no explicit null needed.

---

### SDK Developer (Internal)

**Before:**
```csharp
// Every session class must:
// 1. Accept ILoggerFactory parameter
// 2. Default to NullLoggerFactory.Instance
// 3. Store loggerFactory field
// 4. Create logger in constructor
// 5. Pass to internal components

private ManagementSession(IConnection connection, ILoggerFactory loggerFactory, ...)
{
    _loggerFactory = loggerFactory;
    _logger = loggerFactory.CreateLogger<ManagementSession>();
    _protocol = CreateProtocol(connection, loggerFactory);  // Must forward
}
```

**After:**
```csharp
// Just create logger - done!
private ManagementSession(IConnection connection, ...)
{
    _logger = YubiKitLogging.CreateLogger<ManagementSession>();
    _protocol = CreateProtocol(connection);  // Protocol gets its own logger
}
```

**Verdict: MUCH BETTER** - Less boilerplate, consistent pattern.

---

## Potential Concerns

### 1. Static State in Unit Tests

**Concern:** Static LoggerFactory could leak between tests.

**Mitigation:**
```csharp
// Test fixture setup
[Collection("YubiKit")]
public class ManagementTests : IDisposable
{
    private readonly ILoggerFactory _originalFactory;

    public ManagementTests()
    {
        _originalFactory = YubiKitLogging.LoggerFactory;
        YubiKitLogging.LoggerFactory = new TestLoggerFactory();
    }

    public void Dispose()
    {
        YubiKitLogging.LoggerFactory = _originalFactory;
    }
}
```

Or provide a test helper:
```csharp
public static class YubiKitLogging
{
    /// <summary>
    /// Temporarily replaces the LoggerFactory. Dispose to restore.
    /// Useful for testing.
    /// </summary>
    public static IDisposable UseTemporary(ILoggerFactory factory)
    {
        var original = LoggerFactory;
        LoggerFactory = factory;
        return new Disposable(() => LoggerFactory = original);
    }
}
```

### 2. Thread Safety

**Concern:** Concurrent access to static LoggerFactory.

**Mitigation:** Already handled with `lock` in the implementation. Additionally, LoggerFactory is typically set once at startup, never changed at runtime.

### 3. Multiple LoggerFactory Instances

**Concern:** What if different parts of an app want different logging?

**Response:** This is an edge case. Most apps have one logging configuration. For advanced scenarios, keep the parameter override option (see Hybrid approach below).

---

## Hybrid Approach (Optional Override)

For maximum flexibility, support both patterns:

```csharp
public static async Task<ManagementSession> CreateAsync(
    IConnection connection,
    ProtocolConfiguration? configuration = null,
    ScpKeyParameters? scpKeyParams = null,
    ILoggerFactory? loggerFactory = null,  // Optional override
    CancellationToken cancellationToken = default)
{
    // Use explicit parameter if provided, otherwise use static
    var effectiveLoggerFactory = loggerFactory ?? YubiKitLogging.LoggerFactory;
    // ...
}
```

This gives:
- **Simple case:** Don't pass loggerFactory, use static configuration
- **Override case:** Pass loggerFactory to use a specific one for this session

---

## Recommendation

**Adopt the static pattern with optional override.**

Benefits:
1. **Simpler API** - 4 parameters instead of 5 on every CreateAsync
2. **Less boilerplate** - No parameter threading through extensions/services
3. **Better DI integration** - Auto-wire from container
4. **PowerShell-friendly** - Can ignore logging entirely
5. **IoT-friendly** - Default is NullLoggerFactory
6. **Testable** - UseTemporary() helper for tests
7. **Backward compatible** - Optional override parameter for edge cases

---

## Migration Path

1. Add `YubiKitLogging` class to Core
2. Update `ApplicationSession` to use `YubiKitLogging.CreateLogger<T>()`
3. Move `loggerFactory` parameter to end of parameter list (optional override)
4. Update DI extensions to auto-wire from container
5. Update documentation with new pattern
6. Mark old explicit `loggerFactory` usage as legacy in docs

---

*Proposal created: 2026-01-12*
