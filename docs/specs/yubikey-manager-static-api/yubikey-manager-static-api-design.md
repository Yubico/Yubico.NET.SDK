# YubiKeyManager Static API Design Research

**Date:** 2026-02-06  
**Status:** In Progress  
**Goal:** Redesign YubiKeyManager to use fully static API without requiring instance creation

## Current Architecture

```
┌─────────────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│ DeviceMonitorService│────▶│  IDeviceChannel  │────▶│DeviceListenerService│
│  (scans devices)    │     │ (async channel)  │     │ (consumes, updates) │
└─────────────────────┘     └──────────────────┘     └──────────┬──────────┘
                                                                 │
                                                                 ▼
                                                     ┌─────────────────────┐
                                                     │DeviceRepositoryCached│
                                                     │  (cache + events)   │
                                                     └──────────┬──────────┘
                                                                 │
                                                                 ▼
                                                     ┌─────────────────────┐
                                                     │   YubiKeyManager    │
                                                     │ (facade, discovery) │
                                                     └─────────────────────┘
```

### Current Issues

1. **Requires DI** - Components wired via `AddYubiKeyManagerCore()`
2. **Two background services** - `DeviceMonitorService` + `DeviceListenerService` with channel pattern (over-engineered)
3. **No clean standalone usage** - Must create instances, manage lifecycles

### Existing Components

| Component | Location | Role |
|-----------|----------|------|
| `YubiKeyManager` | `/src/YubiKey/YubiKeyManager.cs` | Facade for discovery |
| `DeviceRepositoryCached` | `/src/DeviceRepositoryCached.cs` | Thread-safe cache + events |
| `DeviceMonitorService` | `/src/DeviceMonitorService.cs` | Periodic device scanning |
| `DeviceListenerService` | `/src/DeviceListenerService.cs` | Consumes channel, updates cache |
| `IDeviceChannel` | `/src/DeviceChannel.cs` | Async channel between services |

---

## Design Trade-offs Analyzed

### 1. Factory Method Scope

| Option | Pros | Cons |
|--------|------|------|
| Single entry point | Simple API | Less flexible |
| Component factories | Testable, replaceable | Complex API |

**Decision:** Single entry point via static methods

### 2. Monitoring Services Consolidation

| Option | Pros | Cons |
|--------|------|------|
| **Merge into one** | Simpler, fewer moving parts | Less separation of concerns |
| Keep separate | Testable in isolation | Over-engineered for <10 devices |
| Callback/delegate | Decoupled | Still two services conceptually |

**Decision:** Merge into one - producer/consumer pattern unnecessary for this domain

### 3. Background Service Hosting

| Option | Pros | Cons |
|--------|------|------|
| Internal Task | Automatic | Resource leak risk |
| **Explicit Start/Stop** | Clear control | User must remember to call |
| Lazy on subscription | Pay-for-what-you-use | Magic behavior, complex ref-counting |

**Decision:** Explicit Start/Stop with static methods

---

## Proposed Design: Fully Static API

### Target API

```csharp
// Simple discovery (no monitoring, on-demand scan)
var devices = await YubiKeyManager.FindAllAsync();
var devices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);

// Monitoring lifecycle
YubiKeyManager.StartMonitoring();
YubiKeyManager.StopMonitoring();

// Event subscription (monitoring must be started)
YubiKeyManager.DeviceChanges.Subscribe(evt => {
    Console.WriteLine($"{evt.EventType}: {evt.Device.SerialNumber}");
});

// Cleanup
YubiKeyManager.Shutdown(); // Stops monitoring, clears cache
```

### Internal Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                    YubiKeyManager (static class)                   │
│                                                                    │
│  Static State:                                                     │
│  • _repository: DeviceRepositoryCached (lazy initialized)          │
│  • _monitorTask: Task? (background scanning)                       │
│  • _cts: CancellationTokenSource? (stop signal)                    │
│  • _lock: object (thread safety)                                   │
│                                                                    │
│  Static Methods:                                                   │
│  • FindAllAsync(ConnectionType?) → IReadOnlyList<IYubiKey>        │
│  • StartMonitoring(TimeSpan? interval)                            │
│  • StopMonitoring()                                                │
│  • Shutdown()                                                      │
│                                                                    │
│  Static Properties:                                                │
│  • DeviceChanges → IObservable<DeviceEvent>                       │
│  • IsMonitoring → bool                                             │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────┐
│                    DeviceRepositoryCached                          │
│                                                                    │
│  • Create() → DeviceRepositoryCached (static factory)             │
│  • UpdateCache(IReadOnlyList<IYubiKey>)                           │
│  • FindAllAsync(ConnectionType?)                                   │
│  • DeviceChanges → IObservable<DeviceEvent>                       │
└────────────────────────────────────────────────────────────────────┘
```

### Key Changes

1. **`YubiKeyManager` becomes static class** (or class with static members)
2. **Merge `DeviceMonitorService` + `DeviceListenerService`** into internal monitoring loop
3. **Remove `IDeviceChannel`** - direct calls to `UpdateCache()`
4. **Add `DeviceRepositoryCached.Create()`** - static factory for internal use
5. **Lazy initialization** - repository created on first access

---

## Implementation Considerations

### Thread Safety

```csharp
public static class YubiKeyManager
{
    private static readonly object _lock = new();
    private static DeviceRepositoryCached? _repository;
    private static Task? _monitorTask;
    private static CancellationTokenSource? _cts;

    private static DeviceRepositoryCached Repository
    {
        get
        {
            if (_repository is null)
            {
                lock (_lock)
                {
                    _repository ??= DeviceRepositoryCached.Create();
                }
            }
            return _repository;
        }
    }
}
```

### Monitoring Loop (Merged Service)

```csharp
private static async Task MonitorLoopAsync(TimeSpan interval, CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            var devices = await ScanDevicesAsync(ct);
            Repository.UpdateCache(devices);
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex) { /* log and continue */ }

        await Task.Delay(interval, ct);
    }
}
```

### DI Compatibility

Keep existing instance-based `IYubiKeyManager` for DI scenarios:

```csharp
// Static API (standalone)
var devices = await YubiKeyManager.FindAllAsync();

// DI API (existing)
services.AddYubiKeyManagerCore();
var manager = serviceProvider.GetRequiredService<IYubiKeyManager>();
var devices = await manager.FindAllAsync();
```

---

## Open Questions

1. **Should static and DI APIs share the same cache?** Or separate instances?
2. **Error handling** - What happens if `FindAllAsync()` is called while monitoring is active?
3. **Configuration** - How to configure scan interval, connection types for static API?
4. **Testing** - How to mock static API? (Consider internal instance that can be swapped)

---

## Related Documents

- **[DX Audit Report](./yubikey-manager-static-api-dx-audit.md)** - API design review with recommendations
- **[Technical Feasibility Report](./yubikey-manager-static-api-feasibility.md)** - Implementation validation and codebase analysis

---

## Next Steps

- [ ] Finalize static API surface
- [ ] Decide on DI compatibility strategy
- [ ] Create detailed implementation plan
- [ ] Address DX audit findings (see [audit report](./yubikey-manager-static-api-dx-audit.md))
