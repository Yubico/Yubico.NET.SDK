# Plan: Merge DeviceListenerService + DeviceChannel into DeviceMonitorService

**Branch:** `yubikit-listeners`
**Date:** 2026-02-09
**Status:** Complete

## Problem

The device discovery pipeline has a redundant layer:

```
DeviceMonitorService  →  DeviceChannel  →  DeviceListenerService  →  DeviceRepositoryCached
    (scans)              (Channel<T>)       (pass-through)              (diff + notify)
```

`DeviceListenerService` (64 lines) does exactly one thing: read from `IDeviceChannel` and call `deviceRepository.UpdateCache()`. The `DeviceChannel` (70 lines) is a `Channel<T>` wrapper with exactly one producer and one consumer. This indirection adds:

- 3 types (`IDeviceChannel`, `DeviceChannel`, `DeviceListenerService`)
- ~134 lines of code
- An extra async hop per scan cycle
- Shutdown ordering complexity (channel must complete before service stops)
- A `static bool IsStarted` coupling between `DeviceMonitorService` and `DeviceRepositoryCached`

### Original reasoning

The separation existed to ensure a crashing background service cannot propagate errors to the main thread. However:

1. **`BackgroundService` already provides this isolation.** `ExecuteAsync` exceptions are caught by the host infrastructure - they don't crash the process.
2. **`DeviceMonitorService.PerformDeviceScan` already has its own try/catch** that prevents scan failures from killing the service loop.
3. **`UpdateCache` is a pure in-memory operation** (dictionary diffing + `Subject.OnNext`). It cannot throw in any way that isn't already caught by the existing error handling.
4. The `Channel<T>` adds no crash isolation that the existing try/catch blocks don't already provide.

## Target State

```
DeviceMonitorService  →  DeviceRepositoryCached
    (scans + updates)       (diff + notify)
```

`DeviceMonitorService` calls `deviceRepository.UpdateCache()` directly after each scan, wrapped in the existing try/catch. Same error isolation, fewer moving parts.

## Checklist

### Part A: Remove DeviceChannel + DeviceListenerService layer
- [x] 1. Modify `DeviceMonitorService.cs` — replace channel with direct repository call
- [x] 2. Modify `DependencyInjection.cs` — remove channel and listener registrations
- [x] 3. Modify `DeviceRepositoryCached.cs` — remove `IsStarted` static gate, remove `#region`
- [x] 4. Modify Core `IntegrationTestBase.cs` — remove `DeviceListenerService` references
- [x] 5. Modify Management `IntegrationTestBase.cs` — remove `DeviceListenerService` references
- [x] 6. Modify `YubiKeyManagerOptions.cs` — remove unused `ScanInterval`
- [x] 7. Delete `DeviceChannel.cs`
- [x] 8. Delete `DeviceListenerService.cs`

### Part B: Simplify listener events to bare signals
- [x] 9. Simplify `HidDeviceListener.cs` — replace `EventHandler<HidDeviceEventArgs>` with `Action? DeviceEvent`
- [x] 10. Simplify `ISmartCardDeviceListener.cs` — replace typed events with `Action? DeviceEvent`
- [x] 11. Simplify `DesktopSmartCardDeviceListener.cs` — replace typed events, remove device construction in `OnArrived`/`OnRemoved`
- [x] 12. Simplify `WindowsHidDeviceListener.cs` — remove `CreateDeviceFromPath` stub, just signal
- [x] 13. Simplify `LinuxHidDeviceListener.cs` — remove device construction in `HandleDeviceAdd`, just signal
- [x] 14. Simplify `MacOSHidDeviceListener.cs` — remove `MacOSHidDevice` construction in arrival callback, just signal
- [x] 15. Update `DeviceMonitorService.cs` — subscribe to simplified `DeviceEvent` action
- [x] 16. Delete `HidDeviceEventArgs.cs`
- [x] 17. Delete `SmartCardDeviceEventArgs.cs`
- [x] 18. Delete `NullDevice.cs`

### Part C: Verify
- [x] 19. Build — `dotnet build Yubico.YubiKit.sln`
- [ ] 20. Test — `dotnet build.cs test` (skipped - some tests require device presence)
- [ ] 21. Commit

## Files to Delete

| File | Lines | Why |
|------|-------|-----|
| `Yubico.YubiKit.Core/src/DeviceChannel.cs` | 70 | No longer needed |
| `Yubico.YubiKit.Core/src/DeviceListenerService.cs` | 64 | No longer needed |

## Files to Modify

### 1. `DeviceMonitorService.cs` — Replace channel with direct repository call

**Changes:**
- Remove `IDeviceChannel deviceChannel` constructor parameter
- Add `IDeviceRepository deviceRepository` constructor parameter
- In `PerformDeviceScan`: replace `deviceChannel.PublishAsync(yubiKeys, ct)` with `deviceRepository.UpdateCache(yubiKeys)`
- In `StopAsync`: remove `deviceChannel.Complete()` call (no channel to complete)
- Remove the `IsStarted` static property (see step 4)

**Error isolation (critical constraint):**

The existing `PerformDeviceScan` already wraps everything in try/catch:

```csharp
private async Task PerformDeviceScan(CancellationToken cancellationToken)
{
    try
    {
        // ... scan + publish ...
    }
    catch (OperationCanceledException)
    {
        logger.LogDebug("Device scan was cancelled");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Device scanning failed");
        // Continue despite errors - don't crash the background service
    }
}
```

After the merge, `UpdateCache` sits inside this same try/catch. If `UpdateCache` throws (e.g., `Subject.OnNext` throws because a subscriber threw), the exception is caught, logged, and the service loop continues. This is identical behavior to the current setup where `DeviceListenerService` catches the same exception in its own `ExecuteAsync`.

**Before:**
```csharp
await deviceChannel.PublishAsync(yubiKeys, cancellationToken).ConfigureAwait(false);
```

**After:**
```csharp
deviceRepository.UpdateCache(yubiKeys);
```

### 2. `DependencyInjection.cs` — Remove channel and listener registrations

**Changes:**
- Remove `services.TryAddSingleton<IDeviceChannel, DeviceChannel>();`
- Remove the `DeviceListenerService` registration from `AddBackgroundServices()`
- Update the marker check from `typeof(DeviceListenerService)` to `typeof(DeviceMonitorService)`

**Before (AddBackgroundServices):**
```csharp
private IServiceCollection AddBackgroundServices()
{
    if (services.Any(s => s.ServiceType == typeof(DeviceListenerService)))
        return services;

    services.AddSingleton<DeviceListenerService>();
    services.AddHostedService<DeviceListenerService>(sp => sp.GetRequiredService<DeviceListenerService>());
    services.AddSingleton<DeviceMonitorService>();
    services.AddHostedService<DeviceMonitorService>(sp => sp.GetRequiredService<DeviceMonitorService>());

    return services;
}
```

**After:**
```csharp
private IServiceCollection AddBackgroundServices()
{
    if (services.Any(s => s.ServiceType == typeof(DeviceMonitorService)))
        return services;

    services.AddSingleton<DeviceMonitorService>();
    services.AddHostedService<DeviceMonitorService>(sp => sp.GetRequiredService<DeviceMonitorService>());

    return services;
}
```

### 3. `DeviceRepositoryCached.cs` — Remove `IsStarted` gate

**Changes:**
- Remove the `DeviceMonitorService.IsStarted` check from `DeviceChanges` getter
- Instead, make the `IObservable<DeviceEvent>` always accessible (the `Subject` works fine without the background service running — it just won't emit events until `UpdateCache` is called)
- Alternatively, add an internal `bool IsMonitoring` property set by `DeviceMonitorService` at start/stop to replace the static coupling. This is preferred since it preserves the user-facing validation.

**Before:**
```csharp
public IObservable<DeviceEvent> DeviceChanges
{
    get
    {
        if (!DeviceMonitorService.IsStarted)
        {
            throw new InvalidOperationException(
                "DeviceChanges requires background services to be running. ...");
        }
        return _deviceChanges.AsObservable();
    }
}
```

**After (option A — remove gate entirely):**
```csharp
public IObservable<DeviceEvent> DeviceChanges => _deviceChanges.AsObservable();
```

**After (option B — replace static coupling with instance state):**
```csharp
internal bool IsMonitoring { get; set; }

public IObservable<DeviceEvent> DeviceChanges
{
    get
    {
        if (!IsMonitoring)
        {
            throw new InvalidOperationException(
                "DeviceChanges requires background services to be running. ...");
        }
        return _deviceChanges.AsObservable();
    }
}
```

Then `DeviceMonitorService` sets `((DeviceRepositoryCached)deviceRepository).IsMonitoring = true/false` in `StartAsync`/`StopAsync`. This removes the static coupling while keeping the guard.

**Recommendation:** Option A (remove the gate). It's simpler. If the user subscribes before starting the host, they simply won't receive events until the monitor starts — no need to throw.

### 4. Integration test bases — Remove `DeviceListenerService` references

**Files:**
- `Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.IntegrationTests/IntegrationTestBase.cs`
- `Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/IntegrationTestBase.cs`

**Changes (both files):**
- Remove `DeviceListenerService = ServiceProvider.GetRequiredService<DeviceListenerService>();`
- Remove `DeviceListenerService.StartAsync(CancellationToken.None).Wait();`
- Remove `private DeviceListenerService DeviceListenerService { get; }`

### 5. `YubiKeyManagerOptions.cs` — Remove unused `ScanInterval`

**Change:** Remove `public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMilliseconds(500);`

This was from the old polling architecture. The event-driven architecture uses `EventCoalescingDelay` instead. Currently nothing references `ScanInterval` except the integration test bases (which set it but nothing reads it).

Also remove `ScanInterval` references from the `DefaultOptions` lambdas in both `IntegrationTestBase` files.

## Verification

### Build
```bash
dotnet build Yubico.YubiKit.sln
```

### Test
```bash
dotnet build.cs test
```

### Manual smoke test
1. Confirm `DeviceMonitorService` starts, scans, and updates the repository
2. Confirm `IObservable<DeviceEvent>` emits events when devices are inserted/removed
3. Confirm clean shutdown (no hangs, no exceptions)

## Summary of changes

| Action | Type/File | Lines removed | Lines added |
|--------|-----------|---------------|-------------|
| Delete | `DeviceChannel.cs` | 70 | 0 |
| Delete | `DeviceListenerService.cs` | 64 | 0 |
| Modify | `DeviceMonitorService.cs` | ~8 | ~3 |
| Modify | `DependencyInjection.cs` | ~5 | ~1 |
| Modify | `DeviceRepositoryCached.cs` | ~8 | ~1 |
| Modify | Core `IntegrationTestBase.cs` | 3 | 0 |
| Modify | Management `IntegrationTestBase.cs` | 3 | 0 |
| Modify | `YubiKeyManagerOptions.cs` | 1 | 0 |

**Net:** ~162 lines removed, ~5 lines added. 3 types eliminated (`IDeviceChannel`, `DeviceChannel`, `DeviceListenerService`).

## Risks

| Risk | Mitigation |
|------|------------|
| `UpdateCache` throws and kills the monitor loop | Already handled by existing try/catch in `PerformDeviceScan` |
| Subscriber to `IObservable<DeviceEvent>` throws in `OnNext` | Already handled by `Subject<T>` — exceptions propagate to `UpdateCache` caller, which is inside try/catch |
| Thread safety of `UpdateCache` called from `ExecuteAsync` | `UpdateCache` uses `ConcurrentDictionary` — already thread-safe. And there's only one caller (the monitor loop), so no contention |
| Breaking change if anyone depends on `IDeviceChannel` | `IDeviceChannel` is a public interface but not documented for external use. It's an internal implementation detail of the DI pipeline |
