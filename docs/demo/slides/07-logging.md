## Observability: logging

**Off by default.** No output, no logger, no overhead until you opt in.

```csharp
// Silent — the default
var keys = await YubiKeyManager.FindAllAsync();

// One line, before you touch the SDK
YubiKitLogging.Configure(loggerFactory);
```

Six documented setups: static, DI, ASP.NET Core, `appsettings.json`, Serilog, none.
Categories are class names — `Yubico.YubiKit.Piv.PivSession`,
`Yubico.YubiKit.Core.Transports.SmartCard.*` — so you can filter per applet or
per transport.

| Level | What lands there |
|---|---|
| `Trace` | Raw APDU / CBOR bytes, protocol steps |
| `Debug` | State transitions, cache updates |
| `Information` | Session creation, major operations |
| `Warning` / `Error` | Recoverable fallback / operation failure |

**Never logged:** PINs, PUKs, passwords, private keys, session keys.
Serial numbers and credential IDs are public identifiers and may appear at `Debug`.

<!-- Anchors: docs/LOGGING.md:5-14 (quick start), :17-20 (off by default),
     :22-137 (six methods), :139-151 (categories), :153-161 (levels),
     :201-212 (security), :219 (never inject ILogger) -->

---

## Logging: we already agree across the SDKs

| SDK | Default | Facade | Enable | Logger obtained |
|---|---|---|---|---|
| **.NET v2** | silent | `Microsoft.Extensions.Logging` | `YubiKitLogging.Configure(f)` | **static** |
| ykman (py) | silent | stdlib `logging` | `init_logging(level)` | per-module |
| ykman (rust) | silent | `log` crate | install a `log` impl | macros, per call site |
| yubikit-android | silent | **slf4j** | add a binding | per-class |
| yubikit-swift | silent | `OSLog` | on by platform | static, per domain |

Three conventions we already share, without ever having agreed them:

1. **A facade, never a concrete logger.** Every SDK binds to an abstraction and lets
   the host pick the sink.
2. **Silent until configured.** No SDK here prints anything out of the box.
3. **Raw protocol bytes go to `Trace`.** Android says so explicitly; .NET puts APDU
   and CBOR there too.

> **Nobody injects a logger into a session.** All five obtain one statically or
> per-module. .NET v2's "never inject `ILogger`" is not a .NET quirk — it is the
> house style everywhere, and it is what keeps the SDK usable without a DI container.
>
> Worth knowing: Android *used* to have a settable static `Logger.setLogger()` and
> deliberately moved off it to slf4j, calling the old approach "not scalable".

<!-- Anchors: .NET docs/LOGGING.md:219;
     python yubikit/core/__init__.py:31,44 (getLogger(__name__)), ykman/logging.py:57,67;
     rust crates/yubikit/Cargo.toml:30 (log crate), src/piv.rs:1210 (log::debug!);
     android doc/Logging_Migration.adoc:5-7 (slf4j move, "not scalable"), :10 (TRACE for raw data);
     swift@1.4.0 YubiKit/YubiKit/Utilities/Logger+Extensions.swift:17-39 (HasLogger, OSLog) -->
