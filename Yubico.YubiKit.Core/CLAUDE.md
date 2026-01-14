# CLAUDE.md - Core Module

This file provides module-specific guidance for working in **Yubico.YubiKit.Core**.
For overall repo conventions, see the repository root [CLAUDE.md](../CLAUDE.md).

## Logging

Core modules use `Microsoft.Extensions.Logging` via the global `YubiKitLogging.LoggerFactory`.
This keeps session constructors and factories consistent across modules.

### Configure logging

Configure the logger factory once at application startup:

```csharp
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;

YubiKitLogging.LoggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
```

If your application uses DI, calling `services.AddYubiKey()` will initialize `YubiKitLogging.LoggerFactory`
from the DI-provided `ILoggerFactory` (via `YubiKitLoggingInitializer`).

## Session base class

`ApplicationSession` centralizes shared session state:
- `FirmwareVersion`
- `IsInitialized`
- `IsAuthenticated`
- `Protocol` ownership/disposal

Prefer using `IsSupported(feature)` / `EnsureSupports(feature)` on `IApplicationSession` rather than duplicating firmware gates in each module.
