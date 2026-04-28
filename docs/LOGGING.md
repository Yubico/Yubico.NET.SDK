# Logging Configuration

YubiKit uses Microsoft.Extensions.Logging for all diagnostic output. This guide covers logging setup for different scenarios.

## Quick Start

```csharp
// No logging (default) - just use the SDK
var manager = host.Services.GetRequiredService<IYubiKeyManager>();

// With logging - one line before using SDK
YubiKitLogging.Configure(loggerFactory);
```

## Default Behavior

By default, YubiKit produces **no log output**. This is intentional:
- No surprises for production deployments
- No performance overhead if logging isn't needed
- Developer explicitly opts in to logging

## Configuration Methods

### 1. No Logging (Default)

Simply don't configure logging. YubiKit uses `NullLoggerFactory` internally.

```csharp
using var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services => services.AddYubiKeyManagerCore())
    .Build();

var manager = host.Services.GetRequiredService<IYubiKeyManager>();
// No logging output
```

### 2. Explicit Static Setup

Best for: Console apps, scripts, or when you want full control.

```csharp
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;

// Create your logger factory
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Configure YubiKit before using SDK
YubiKitLogging.Configure(loggerFactory);

```

### 3. DI with Explicit Setup (Recommended)

Best for: Applications using dependency injection where you want explicit control.

```csharp
using var host = Host.CreateDefaultBuilder()
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices(services => services
        .AddYubiKeyManagerCore()
        .AddYubiKeyManager())
    .Build();

// Explicit setup - recommended for clarity
YubiKitLogging.Configure(host.Services.GetRequiredService<ILoggerFactory>());

await host.StartAsync();
var manager = host.Services.GetRequiredService<IYubiKeyManager>();
```

### 4. DI Auto-Wire (Convenience)

Best for: Quick prototypes or when you trust DI defaults.

If you register `ILoggerFactory` in DI but don't call `YubiKitLogging.Configure()`, 
YubiKit will automatically use the DI logger factory when `IYubiKeyManager` is first resolved.

```csharp
using var host = Host.CreateDefaultBuilder()  // Registers ILoggerFactory automatically
    .ConfigureServices(services => services.AddYubiKeyManagerCore())
    .Build();

// No explicit Configure() call - auto-wires from DI
var manager = host.Services.GetRequiredService<IYubiKeyManager>();
// Logging now active via DI's ILoggerFactory
```

**Note:** Explicit `YubiKitLogging.Configure()` always takes precedence over auto-wire.

### 5. ASP.NET Core

Best for: Web applications and APIs.

```csharp
var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core already configures logging
builder.Services.AddYubiKeyManagerCore();
builder.Services.AddYubiKeyManager();

var app = builder.Build();

// Optional: Explicit setup if you want control
// YubiKitLogging.Configure(app.Services.GetRequiredService<ILoggerFactory>());

// Or let auto-wire handle it when IYubiKeyManager is resolved
```

### 6. appsettings.json Configuration

Best for: Production deployments with environment-specific settings.

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Yubico.YubiKit": "Debug",
      "Yubico.YubiKit.Core": "Trace"
    },
    "Console": {
      "LogLevel": {
        "Default": "Warning",
        "Yubico.YubiKit": "Information"
      }
    }
  }
}
```

**Program.cs:**
```csharp
using var host = Host.CreateDefaultBuilder(args)  // Loads appsettings.json
    .ConfigureServices(services => services.AddYubiKeyManagerCore())
    .Build();

YubiKitLogging.Configure(host.Services.GetRequiredService<ILoggerFactory>());
```

### 7. Serilog Integration

Best for: Structured logging, file output, or log aggregation services.

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Yubico.YubiKit", Serilog.Events.LogEventLevel.Verbose)
    .WriteTo.Console()
    .WriteTo.File("yubikit.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

using var host = Host.CreateDefaultBuilder()
    .UseSerilog()
    .ConfigureServices(services => services.AddYubiKeyManagerCore())
    .Build();

YubiKitLogging.Configure(host.Services.GetRequiredService<ILoggerFactory>());
```

## Log Categories

YubiKit uses the following log categories (class names):

| Category | Description |
|----------|-------------|
| `Yubico.YubiKit.Core.DeviceMonitorService` | Device arrival/removal events |
| `Yubico.YubiKit.Core.DeviceListenerService` | Background device cache updates |
| `Yubico.YubiKit.Core.DeviceRepositoryCached` | Device cache operations |
| `Yubico.YubiKit.Core.SmartCard.*` | Smart card/PCSC operations |
| `Yubico.YubiKit.Management.ManagementSession` | Management application commands |
| `Yubico.YubiKit.Piv.PivSession` | PIV application commands |
| `Yubico.YubiKit.Fido2.Fido2Session` | FIDO2 application commands |

## Log Levels

| Level | Use |
|-------|-----|
| `Trace` | Raw APDU bytes, protocol details |
| `Debug` | State transitions, cache updates |
| `Information` | Session creation, major operations |
| `Warning` | Recoverable errors, fallback behavior |
| `Error` | Operation failures |

## Testing with Logging

For unit tests, use `YubiKitLogging.UseTemporary()` to scope logging:

```csharp
[Fact]
public async Task MyTest()
{
    using var loggerFactory = LoggerFactory.Create(b => b.AddXUnit(output));
    using var _ = YubiKitLogging.UseTemporary(loggerFactory);
    
    // Test code - logging goes to xUnit output
    // Original logger restored when scope exits
}
```

## Troubleshooting

### No log output?

1. Verify `YubiKitLogging.Configure()` was called before SDK usage
2. Check minimum log level isn't filtering messages
3. Ensure logger provider is registered (e.g., `AddConsole()`)

### Too verbose?

Filter by category in your logging configuration:

```csharp
builder.AddFilter("Yubico.YubiKit", LogLevel.Warning);
```

### Performance concerns?

- Use `LogLevel.Information` or higher in production
- `Trace` level logs raw APDU bytes which adds overhead
- Logging is completely disabled with `NullLoggerFactory` (default)

## Security Considerations

YubiKit logging is designed to be safe:

- ❌ PINs, PUKs, and passwords are **never** logged
- ❌ Private keys are **never** logged  
- ❌ Session keys are **never** logged
- ✅ Public identifiers (serial numbers, credential IDs) may be logged at Debug level
- ✅ Operation types and durations are logged at Info level

If you have strict compliance requirements, use `LogLevel.Warning` or higher to minimize information disclosure.

---

## Logging Conventions for Contributors

> READ WHEN adding logging to a session class, choosing log level for a new operation, deciding what to log about credentials/keys.

### Use Static `YubiKitLogging` — NEVER inject `ILogger`

```csharp
// ✅ CORRECT: Static logger from YubiKitLogging
public class FidoSession
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<FidoSession>();
}

// ❌ WRONG: Injected logger (breaks consistency)
public class FidoSession(ILogger<FidoSession> logger) { }
```

Canonical logger factory: `YubiKitLogging.CreateLogger<T>()` at `src/Core/src/YubiKitLogging.cs:20`.

### Log Levels (when contributing new log calls)

| Level | Use for |
|---|---|
| `Trace` | Raw APDU/CBOR bytes, detailed protocol steps |
| `Debug` | Protocol-level operations, state transitions |
| `Info` | Session creation, major operations (enroll, authenticate) |
| `Warning` | Recoverable errors, fallback behavior |
| `Error` | Operation failures, exceptions |

### Logging Sensitive Data

- ❌ NEVER log PINs, keys, or credentials
- ✅ Log credential IDs as hex (public identifier)
- ✅ Log lengths, not contents, of sensitive buffers

```csharp
// ❌ NEVER
_logger.LogDebug("PIN: {Pin}", pin);
_logger.LogDebug("Key: {Key}", Convert.ToBase64String(privateKey));

// ✅ YES — metadata only
_logger.LogDebug("PIN verification for slot {Slot}", slotNumber);
_logger.LogDebug("Key operation completed, length: {Length}", privateKey.Length);
```
