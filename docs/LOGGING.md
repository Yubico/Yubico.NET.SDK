# Logging Configuration

YubiKit uses Microsoft.Extensions.Logging for all diagnostic output. This guide covers logging setup for different scenarios.

## Quick Start

```csharp
// No logging (default) - just use the SDK
var devices = await YubiKeyManager.FindAllAsync();

// With logging - one line before using SDK
YubiKitLogging.Configure(loggerFactory);
```

## Default Behavior

By default, YubiKit produces **no log output**. This is intentional:
- No surprises for production deployments
- No configured output destination when logging isn't needed
- Developer explicitly opts in to logging

## Configuration Methods

### 1. No Logging (Default)

Simply don't configure logging. YubiKit uses `NullLoggerFactory` internally.

Configure before the first SDK use. Many SDK types retain static logger instances; replacing the
factory affects later logger creation, not loggers already captured by those types. Your application
owns the supplied factory and providers. Keep them alive through all SDK operations, monitor shutdown,
and connection/session disposal, then dispose them in the application's outer scope.

```csharp
var devices = await YubiKeyManager.FindAllAsync();
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

### 3. DI with Explicit Logging Setup

Best for: Applications using dependency injection where you want explicit control.

```csharp
using var host = Host.CreateDefaultBuilder()
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// Explicit setup from the DI-provided ILoggerFactory
YubiKitLogging.Configure(host.Services.GetRequiredService<ILoggerFactory>());

await host.StartAsync();
var devices = await YubiKeyManager.FindAllAsync();
```

### 4. ASP.NET Core

Best for: Web applications and APIs.

```csharp
var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core already configures logging

var app = builder.Build();

YubiKitLogging.Configure(app.Services.GetRequiredService<ILoggerFactory>());
```

### 5. appsettings.json Configuration

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
    .Build();

YubiKitLogging.Configure(host.Services.GetRequiredService<ILoggerFactory>());
```

### 6. Serilog Integration

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
    .Build();

YubiKitLogging.Configure(host.Services.GetRequiredService<ILoggerFactory>());
```

## Log Categories

YubiKit uses the following log categories (class names):

| Category | Description |
|----------|-------------|
| `Yubico.YubiKit.Core.Devices.*` | Device discovery, metadata, and cache operations |
| `Yubico.YubiKit.Core.Transports.SmartCard.*` | Smart card/PC/SC transport operations |
| `Yubico.YubiKit.Core.Protocols.SmartCard.*` | APDU and SCP protocol operations |
| `Yubico.YubiKit.Core.Transports.Hid.*` | HID device and transport operations |
| `Yubico.YubiKit.Management.ManagementSession` | Management application commands |
| `Yubico.YubiKit.Piv.PivSession` | PIV application commands |
| `Yubico.YubiKit.Ctap2.Ctap2Session` | CTAP2 authenticator commands |

## Log Levels

| Level | Use |
|-------|-----|
| `Trace` | Detailed protocol steps; ordinary APDU headers/lengths and SELECT identifier bytes |
| `Debug` | State transitions, cache updates |
| `Information` | Session creation, major operations |
| `Warning` | Recoverable errors, fallback behavior |
| `Error` | Operation failures |

## Testing with Logging

`YubiKitLogging.UseTemporary()` temporarily replaces the process-wide factory and restores the captured
factory on disposal. It is useful for serially coordinated diagnostics, not test-local or async-flow
isolation. Parallel or out-of-order scopes can restore the wrong factory, and existing static loggers
keep their earlier provider. Coordinate all participants before using it. This helper borrows an
application-owned factory; it restores the global factory reference without disposing the borrowed one:

```csharp
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;

static async Task DiscoverWithTemporaryLoggingAsync(
    ILoggerFactory loggerFactory, CancellationToken cancellationToken)
{
    using var _ = YubiKitLogging.UseTemporary(loggerFactory);
    await YubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);
}
```

Keep the factory and providers alive through all participating SDK work, including late enumeration
completion and later uses of static loggers created during this scope. Completion of the bounded Find
task is not a join for that work. Restoring the global factory neither replaces captured loggers nor
ends their provider lifetime; the application must choose an outer lifetime that covers those uses.

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
- `Trace` formatting and provider output add overhead; filter the categories you actually need
- Logging is completely disabled with `NullLoggerFactory` (default)

## Security Considerations

Choose log levels, destinations, retention, and access controls according to the data your application
handles. `YubiKitLogging` selects a factory; it has no central payload redactor. Ordinary APDU trace
templates record headers and lengths rather than complete bodies, but SELECT logs caller-supplied
application identifier bytes. Serial numbers and credential identifiers can also be privacy-sensitive.

Exception objects have independent provenance. Transport/provider exceptions and application callback
exceptions can reach configured providers, including text supplied by your application. Keep credentials,
keys, plaintext, and sensitive intent/scope text out of exception messages and diagnostics. A higher
minimum level reduces routine output but does not sanitize an exception at Warning or Error. Inspect
the actual templates and provider behavior before collecting or exporting logs.

---

## Logging Conventions for Contributors

> READ WHEN adding logging to a session class, choosing log level for a new operation, deciding what to log about credentials/keys.

### Use Static `YubiKitLogging` — NEVER inject `ILogger`

```csharp
// ✅ CORRECT: Static logger from YubiKitLogging
public class Ctap2Session
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<Ctap2Session>();
}

// ❌ WRONG: Injected logger (breaks consistency)
public class Ctap2Session(ILogger<Ctap2Session> logger) { }
```

Internal contributor entry point: `YubiKitLogging.CreateLogger<T>()` in
`src/Core/src/YubiKitLogging.cs`. Consumers configure the public factory rather than calling this internal helper.

### Log Levels (when contributing new log calls)

| Level | Use for |
|---|---|
| `Trace` | Detailed protocol steps and reviewed nonsecret metadata; never sensitive payloads |
| `Debug` | Protocol-level operations, state transitions |
| `Info` | Session creation, major operations (enroll, authenticate) |
| `Warning` | Recoverable errors, fallback behavior |
| `Error` | Operation failures, exceptions |

### Logging Sensitive Data

- ❌ NEVER log PINs, keys, or credentials
- Review identifier privacy before logging credential IDs, even when they are not secret key material
- ✅ Log lengths, not contents, of sensitive buffers

```csharp
// ❌ NEVER
_logger.LogDebug("PIN: {Pin}", pin);
_logger.LogDebug("Key: {Key}", Convert.ToBase64String(privateKey));

// ✅ YES — metadata only
_logger.LogDebug("PIN verification for slot {Slot}", slotNumber);
_logger.LogDebug("Key operation completed, length: {Length}", privateKey.Length);
```
