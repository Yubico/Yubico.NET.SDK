## Observability: what changed underneath

![w:1150](assets/observability-before-after.svg)

<!-- Anchors: docs/architecture/event-driven-device-discovery.md;
     ThrottleInterval=200ms src/Core/src/Devices/YubiKeyDeviceMonitorService.cs:61;
     MaxCoalesceInterval=5x :66; interval fallback :579-580 -->

---

## Observability: three contracts worth knowing

**1 — `WatchAsync` subscribes on first iteration, not when called.**
Start the `await foreach` *before* the action you expect to trigger an event, or you
will miss anything raised in the gap.

**2 — Overflow faults the stream; it does not drop.**
Each enumeration owns a 256-event buffer. Overflow throws `InvalidOperationException`
on *that* stream only. Device events are **deltas**, so a silent drop would
desynchronise you — recover by re-enumerating and calling `FindAllAsync`.

**3 — `StartMonitoring(interval)` while already running is a silent no-op.**
The new interval is ignored, not applied, and no error is raised. Stop and start to
change it. Deliberate: partial application would be worse, and throwing would make an
idempotent start unsafe to call defensively.

> Logging is opt-in and static: `YubiKitLogging.Configure(loggerFactory)`, one line,
> before you touch the SDK. **Never inject `ILogger`** — that house rule is what keeps
> the SDK usable without a DI container.

<!-- Anchors: WatcherBufferCapacity=256 src/Core/src/Devices/DeviceEventHub.cs:51,
     overflow :242; subscribe-on-first-iteration docs/usage/device-discovery.md:90-92;
     StartMonitoring no-op src/Core/src/Devices/YubiKeyDeviceMonitorService.cs:296-303;
     logging docs/LOGGING.md:5-14, :219 -->
