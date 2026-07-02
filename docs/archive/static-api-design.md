# YubiKeyManager Static API Design - Completion Summary

**Completed:** 2026-02-11  
**Branch:** `yubikit-static-api-design`  
**Commits:** 600+ (including Ralph Loop autonomous execution)  
**Last Updated:** 2026-02-11 (device monitoring architecture refactor)

## Reference Documents

| Document | Path | Description |
|----------|------|-------------|
| PRD Specification | [`docs/specs/yubikey-manager-static-api/final_spec.md`](../specs/yubikey-manager-static-api/final_spec.md) | Product requirements document |
| Implementation Plan | [`~/.copilot/session-state/.../plan.md`] | Context-based refactor plan |
| Progress File | [`docs/ralph-loop/yubikey-manager-static-api-progress.md`](../ralph-loop/yubikey-manager-static-api-progress.md) | Ralph Loop task tracking |

---

## Executive Summary

Implemented a **static-only API** for `YubiKeyManager` that provides simple, zero-configuration device discovery and monitoring. The design eliminates dependency injection complexity while maintaining thread safety and proper resource management.

### Key Design Decisions

1. **Static-only API** - No DI support. Physical USB devices are global state; the API reflects this.
2. **Encapsulated lifecycle** - All mutable state lives in `YubiKeyManagerContext`, enabling proper disposal and test isolation.
3. **Event-driven monitoring** - Combines polling with hardware event listeners for responsive device detection.
4. **Breaking changes accepted** - Removed `IYubiKeyManager`, `AddYubiKeyManagerCore()`, DI infrastructure.

---

## Final API Surface

```csharp
public static class YubiKeyManager
{
    // Device Discovery (with caching and optional rescan)
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken ct);
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type = ConnectionType.All,
        bool forceRescan = false,
        CancellationToken ct = default);
    
    // Monitoring Lifecycle
    public static void StartMonitoring();
    public static void StartMonitoring(TimeSpan interval);
    public static void StopMonitoring();
    public static bool IsMonitoring { get; }
    
    // Device Events (IObservable<DeviceEvent>)
    public static IObservable<DeviceEvent> DeviceChanges { get; }
    
    // Shutdown
    public static Task ShutdownAsync(CancellationToken ct = default);
    public static void Shutdown();
}
```

### FindAllAsync Behavior

| Scenario | forceRescan | Monitoring | Behavior |
|----------|-------------|------------|----------|
| First call | `false` | Off | Scan hardware, cache results |
| Subsequent | `false` | Off | Return cached (may be stale) |
| Any call | `true` | - | Always scan hardware |
| While monitoring | `false` | On | Return cache (kept fresh) |

---

## Architecture

### Before (Complex)

```
Application
    ↓
IServiceCollection.AddYubiKeyManagerCore()
    ↓
IYubiKeyManager (interface)
    ↓
YubiKeyManager (instance class)
    ↓
DeviceMonitorService (IHostedService)
    ↓
DeviceRepository + DeviceListenerService + DeviceChannel
```

### After (Simple) - Updated Architecture (Post-Refactor)

```
Application
    ↓
YubiKeyManager (static class)
    ↓
YubiKeyDeviceManager (internal, composition root)
    ├── YubiKeyDeviceRepository (pure cache + diff + events)
    └── YubiKeyDeviceMonitorService (Rx-based event coalescing)
            ├── HidDeviceListener (explicit Start/Stop, lazy)
            ├── DesktopSmartCardDeviceListener (explicit Start/Stop, lazy)
            └── IFindYubiKeys
```

**Listener Lifecycle:**
- Listeners created but NOT started in constructor
- `Start()` establishes baseline (existing devices) without firing events
- Only changes after `Start()` trigger `DeviceEvent` callback
- `Stop()` halts monitoring (can be restarted)

### Separation of Concerns

| Component | Responsibility |
|-----------|----------------|
| `YubiKeyManager` | Public static API, lazy initialization |
| `YubiKeyDeviceManager` | Composition root, owns lifecycle |
| `YubiKeyDeviceRepository` | Pure cache, diff logic, Rx events (NO discovery) |
| `YubiKeyDeviceMonitorService` | Owns listeners, calls IFindYubiKeys, updates repository |

### Event Flow (Post-Refactor)

```
Hardware Event (USB insert/remove)
    ↓
[HidDeviceListener / SmartCardDeviceListener]
    │ (explicit Start() establishes baseline - no events for existing devices)
    ↓ (DeviceEvent callback - only fires for changes AFTER Start())
_rescanTrigger.OnNext(Unit.Default)
    ↓
Rx Throttle (200ms) - coalesces rapid events
    ↓
RescanAsync() → IFindYubiKeys.FindAllAsync()
    ↓
_repository.UpdateCache(devices)
    ↓ (diff: detect added/removed)
Subject<DeviceEvent>.OnNext()
    ↓
DeviceChanges observable (public API)
```

**Key Design Points:**
- Listeners have explicit `Start()`/`Stop()` - no auto-start in constructors
- `Start()` establishes baseline of existing devices **without** firing events
- Only subsequent device changes trigger events
- Rx `Throttle()` replaces semaphore-based coalescing for cleaner event debouncing
- `StartMonitoring()` enables event detection; `FindAllAsync()` triggers initial scan

### Context Pattern → Manager Pattern

The key architectural insight: **encapsulate all lifecycle state in a resettable manager with clear separation of concerns**.

```csharp
public static class YubiKeyManager
{
    private static YubiKeyDeviceManager? _manager;
    private static readonly object _managerLock = new();
    
    private static YubiKeyDeviceManager EnsureManager()
    {
        var mgr = Volatile.Read(ref _manager);
        if (mgr is not null) return mgr;
        
        lock (_managerLock)
        {
            mgr = _manager;
            if (mgr is not null) return mgr;
            _manager = YubiKeyDeviceManager.Create();
            return _manager;
        }
    }
    
    public static async Task ShutdownAsync(CancellationToken ct = default)
    {
        YubiKeyDeviceManager? mgr;
        lock (_managerLock)
        {
            mgr = _manager;
            _manager = null;  // Allow re-initialization
        }
        if (mgr is not null)
            await mgr.DisposeAsync();
    }
}

// Internal composition root
internal sealed class YubiKeyDeviceManager : IAsyncDisposable
{
    private readonly YubiKeyDeviceRepository _repository;
    private readonly YubiKeyDeviceMonitorService _monitorService;
    
    // Owns both, coordinates lifecycle, exposes unified API
}
```

**Benefits:**
- Clean disposal with correct ordering
- Test isolation via `ShutdownAsync()`
- Thread-safe lazy initialization
- No `Lazy<T>` limitations (can reset)
- **Clean separation**: Repository = pure cache, MonitorService = discovery engine
- **Dual-mode support**: One-off scans (lightweight) AND reactive monitoring (power users)

---

## Files Changed

### Created

| File | Description |
|------|-------------|
| `Yubico.YubiKit.Core/src/YubiKey/YubiKeyDeviceManager.cs` | Internal composition root, owns repository + monitor service |
| `Yubico.YubiKit.Core/src/YubiKey/YubiKeyDeviceRepository.cs` | Pure cache with diff logic and Rx events |
| `Yubico.YubiKit.Core/src/YubiKey/IYubiKeyDeviceRepository.cs` | Internal interface for repository |
| `Yubico.YubiKit.Core/src/YubiKey/YubiKeyDeviceMonitorService.cs` | Owns listeners and discovery service |
| `Yubico.YubiKit.Core/src/YubiKey/IYubiKeyDeviceMonitorService.cs` | Internal interface for monitor service |
| `Yubico.YubiKit.Tests.Shared/Infrastructure/TestCategories.cs` | Constants for test trait filtering |
| `Yubico.YubiKit.Core/tests/.../YubiKeyDeviceManagerTests.cs` | Unit tests for manager |
| `Yubico.YubiKit.Core/tests/.../YubiKeyDeviceRepositoryTests.cs` | Unit tests for repository |
| `Yubico.YubiKit.Core/tests/.../YubiKeyDeviceMonitorServiceTests.cs` | Unit tests for monitor service |

### Modified

| File | Changes |
|------|---------|
| `YubiKeyManager.cs` | Converted to static class, simplified to 2 FindAllAsync overloads with defaults |
| `YubiKeyTestInfrastructure.cs` | Uses `YubiKeyManager` directly |
| `CoreTests.cs`, `YubiKeyTests.cs` | Added test category traits |
| `YubiKeyManagerStaticTests.cs` | Updated for manager-based architecture |
| `DeviceSelector.cs` (ManagementTool) | Updated for new API with named parameters |
| `docs/TESTING.md` | Added test traits documentation |
| `.claude/skills/domain-test/SKILL.md` | Added trait filter patterns |
| `toolchain.cs` | Added trait filter documentation |
| `experiments/DeviceMonitor/Program.cs` | Fixed async patterns, adapted to new API |

### Deleted

| File | Reason |
|------|--------|
| `DeviceRepository.cs` | Replaced by `YubiKeyDeviceRepository.cs` |
| `YubiKeyManagerContext.cs` | Replaced by `YubiKeyDeviceManager.cs` + `YubiKeyDeviceMonitorService.cs` |
| `DeviceRepositoryTests.cs` | Replaced by `YubiKeyDeviceRepositoryTests.cs` |
| `YubiKeyManagerOptions.cs` | Unused |
| `DependencyInjection.cs` | DI support removed |
| `IYubiKeyManager.cs` | Interface removed (static API) |
| `DeviceMonitorService.cs` | Replaced by static monitoring |
| `DeviceListenerService.cs` | Consolidated into monitor service |
| `DeviceChannel.cs` | Consolidated into monitor service |
| `TestDeviceDiscovery.cs` | Redundant wrapper |

---

## Critical Bugs Fixed

### 1. Static State Reset (CRITICAL)

**Problem:** `Lazy<DeviceRepository>` was `readonly`, never disposed in `ShutdownAsync()`.

**Solution:** Nullable `_context` field that can be set to null and re-created.

### 2. Event-Driven Monitoring (CRITICAL)

**Problem:** `_eventSemaphore.WaitAsync()` was never awaited—only timer-based polling worked.

**Solution:** 
```csharp
var delayTask = Task.Delay(interval, ct);
var eventTask = _eventSemaphore.WaitAsync(ct);
await Task.WhenAny(delayTask, eventTask);
```

### 3. Disposal Race Condition (HIGH)

**Problem:** Listeners disposed while callbacks might still execute.

**Solution:** Clear callbacks BEFORE disposing listeners, capture semaphore under lock:
```csharp
// In DisposeAsync:
// 1. Cancel CTS
// 2. Wait for monitoring task (with timeout)
// 3. Clear callbacks (prevent events during disposal)
if (_hidListener is not null) _hidListener.DeviceEvent = null;
if (_smartCardListener is not null) _smartCardListener.DeviceEvent = null;
// 4. Dispose listeners
// 5. Dispose primitives

// In SignalEvent - capture under lock:
private void SignalEvent()
{
    SemaphoreSlim? semaphore;
    lock (_monitorLock)
    {
        semaphore = _eventSemaphore;
    }
    if (semaphore is null) return;
    // ... use captured reference
}
```

### 4. DeviceEvent.Removed Had Null Device (MEDIUM)

**Problem:** Removed events passed `null` for the device, violating expected behavior.

**Solution:** Track devices in dictionary, pass actual device object on removal.

### 5. DevicesAreEqual Logic Broken (MEDIUM)

**Problem:** Only compared `DeviceId` (always equal for same device), so Updated events never fired.

**Solution:** Removed `DeviceAction.Updated` entirely (not in PRD, logic unfixable).

### 6. Repository Had Discovery Dependency (MEDIUM)

**Problem:** `DeviceRepository` mixed caching with discovery (`IFindYubiKeys` dependency), causing unclear separation of concerns and a bug where `FindAllAsync()` never rescanned after first call.

**Solution:** Split into:
- `YubiKeyDeviceRepository` - Pure cache, no discovery dependency
- `YubiKeyDeviceMonitorService` - Owns listeners and `IFindYubiKeys`, calls `repository.UpdateCache()`
- Added `forceRescan` parameter for explicit control

### 7. Duplicate Device Events on Startup (HIGH) - Fixed in Refactor

**Problem:** When a YubiKey was already plugged in at startup, duplicate ADDED events fired for each interface (6 events instead of 3).

**Root Causes:**
1. SmartCard listener started in constructor, fired events for existing readers on first poll
2. Race between `StartMonitoring()` initial scan and `FindAllAsync()` triggered parallel scans
3. Semaphore-based coalescing had timing edge cases

**Solution (Architectural Refactor):**
1. Listeners have explicit `Start()`/`Stop()` - no auto-start in constructors
2. `Start()` establishes baseline of existing devices **without** firing events
3. Rx `Throttle()` replaces semaphore-based coalescing for clean event debouncing
4. Removed sync-over-async blocking and sentinel value hacks
5. Clear separation: `StartMonitoring()` = listen for changes; `FindAllAsync()` = scan now

**Key Files Changed:**
- `ISmartCardDeviceListener.cs` - Added `Start()`/`Stop()` to interface
- `HidDeviceListener.cs` - Added abstract `Start()` method  
- All listener implementations - Lazy start pattern with baseline establishment
- `YubiKeyDeviceMonitorService.cs` - Rx-based throttling via `Subject<Unit>`

---

## Test Infrastructure Improvements

### Test Categories

Created `TestCategories` constants for trait-based filtering:

```csharp
using Yubico.YubiKit.Tests.Shared.Infrastructure;

[Fact]
[Trait(TestCategories.Category, TestCategories.RequiresHardware)]
public async Task MyHardwareTest() { }

[Fact]
[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
[Trait(TestCategories.Category, TestCategories.Slow)]
public async Task MyDeviceInsertionTest() { }
```

### Filter Commands

```bash
# Skip user presence tests (for CI/agents)
dotnet toolchain.cs test --filter "Category!=RequiresUserPresence"

# Run only fast unit tests
dotnet toolchain.cs test --filter "Category!=RequiresHardware&Category!=RequiresUserPresence&Category!=Slow"
```

### Categories

| Category | Constant | Description |
|----------|----------|-------------|
| `RequiresHardware` | `TestCategories.RequiresHardware` | Needs physical YubiKey |
| `RequiresUserPresence` | `TestCategories.RequiresUserPresence` | Needs user to insert/remove/touch |
| `Slow` | `TestCategories.Slow` | Takes >5 seconds |
| `Integration` | `TestCategories.Integration` | Multi-component test |

---

## Usage Examples

### Simple Device Discovery

```csharp
// Returns cached results (fast)
var devices = await YubiKeyManager.FindAllAsync();
foreach (var device in devices)
{
    Console.WriteLine($"Found: {device.SerialNumber}");
}

// Force fresh scan
var freshDevices = await YubiKeyManager.FindAllAsync(forceRescan: true);

// Filter by connection type
var smartCardOnly = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);
```

### Device Monitoring (Recommended Pattern)

```csharp
// 1. Subscribe to changes first
using var subscription = YubiKeyManager.DeviceChanges.Subscribe(e =>
{
    Console.WriteLine($"{e.Action}: {e.Device.SerialNumber}");
});

// 2. Start monitoring for future events
YubiKeyManager.StartMonitoring();

// 3. Get current devices (triggers scan, populates cache, fires events)
var devices = await YubiKeyManager.FindAllAsync();

// ... application runs, receives events for insert/remove ...

// 4. Cleanup
await YubiKeyManager.ShutdownAsync();
```

**Note:** `StartMonitoring()` only enables event detection for future changes. Call `FindAllAsync()` to trigger the initial device scan.

### Test Cleanup Pattern

```csharp
public class MyTests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    
    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();
    
    [Fact]
    public async Task MyTest()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        // ...
    }
}
```

---

## Breaking Changes

| Removed | Replacement |
|---------|-------------|
| `IYubiKeyManager` interface | Use `YubiKeyManager` static class directly |
| `AddYubiKeyManagerCore()` | No setup needed—just call static methods |
| `DependencyInjection.cs` | Deleted |
| `DeviceAction.Updated` | Only `Added` and `Removed` events |
| `TestDeviceDiscovery` | Use `YubiKeyManager` directly |

---

## Test Results

| Metric | Value |
|--------|-------|
| Unit Tests Passed | 221 |
| Unit Tests Skipped | 2 (require hardware) |
| Pre-existing Failures | 1 (unrelated OtpHidProtocol test) |
| Build Errors | 0 |
| Build Warnings | 4 (pre-existing, unrelated) |

---

## Lessons Learned

1. **Static classes need lifecycle management** - The manager pattern solves this elegantly.

2. **Double-checked locking with `Volatile.Read`** - Essential for thread-safe lazy initialization without `Lazy<T>`.

3. **Disposal order matters** - Cancel → Wait → Clear callbacks → Dispose, not Cancel → Dispose → Wait.

4. **Test trait constants** - Avoid magic strings, enable consistent filtering.

5. **Remove unused features** - `DeviceAction.Updated` was broken and not in PRD—deleting it was the right call.

6. **Separation of concerns** - Repository should be pure cache; discovery belongs in a separate service.

7. **Clear callbacks before disposal** - Prevents race conditions where callbacks fire on disposed resources.

8. **Capture references under lock** - In `SignalEvent()`, capture semaphore reference under lock to avoid TOCTOU race.

9. **Explicit Start/Stop > auto-start** - Listeners that auto-start in constructors cause initialization ordering issues and duplicate events. Explicit `Start()` with baseline establishment is cleaner.

10. **Rx for event coalescing** - `Throttle()` operator is more declarative and testable than manual semaphore drain loops.

11. **Lazy initialization for monitoring** - `StartMonitoring()` should just enable event detection; `FindAllAsync()` should trigger the scan. This gives callers explicit control.

---

## Related Documents

| Document | Path | Description |
|----------|------|-------------|
| Code Review | [`docs/ralph-loop/reviews/2026-02-11-device-monitoring-refactor.md`](../ralph-loop/reviews/2026-02-11-device-monitoring-refactor.md) | Review of the refactor |
| Progress File | [`docs/ralph-loop/device-monitoring-refactor-progress.md`](../ralph-loop/device-monitoring-refactor-progress.md) | Ralph Loop task tracking |

---

## Future Considerations

1. **Performance benchmarks** - Phase 10 tasks remain for measuring discovery latency.

2. **Additional transports** - Current implementation handles SmartCard and HID; NFC could be added.

3. **Configuration options** - If needed, could add `YubiKeyManagerOptions` parameter to `StartMonitoring()`.

4. **Smarter listeners** - Current listeners only signal "something changed"; future versions could report which device for targeted updates instead of full rescans.
