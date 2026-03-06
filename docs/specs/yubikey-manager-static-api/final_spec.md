# PRD: YubiKeyManager Static API Redesign - FINAL

**Status:** APPROVED  
**Author:** spec-writer agent  
**Approved:** 2026-02-07T09:52:00Z  
**Feature Slug:** yubikey-manager-static-api

---

## 1. Problem Statement

### 1.1 The Problem

Developers using the Yubico.NET.SDK must create and manage YubiKeyManager instances through dependency injection or manual instantiation, even for simple device discovery scenarios. This creates friction for users who want a straightforward "find my YubiKey" experience without configuring services, managing lifecycles, or understanding the internal architecture of background monitoring services.

The current architecture requires:
1. Calling `AddYubiKeyManagerCore()` to wire up DI services
2. Resolving `IYubiKeyManager` from the service provider
3. Understanding the relationship between `DeviceMonitorService`, `IDeviceChannel`, and `DeviceListenerService`
4. Managing instance lifetimes for proper cleanup

This complexity is appropriate for enterprise applications with existing DI infrastructure, but creates unnecessary barriers for:
- Command-line tools that need quick device enumeration
- Interactive applications that want simple device arrival/removal notifications
- Developers learning the SDK who encounter infrastructure setup before API usage
- Test scripts that need on-demand device discovery without service hosting

### 1.2 Evidence

| Type | Source | Finding |
|------|--------|---------|
| Quantitative | Codebase Analysis | Two separate background services (`DeviceMonitorService` + `DeviceListenerService`) with async channel pattern for managing <10 devices |
| Quantitative | Architecture Review | `IDeviceChannel` exists solely to connect two services that could be merged |
| Qualitative | Research Document | "Over-engineered" producer/consumer pattern identified as unnecessary for this domain |
| Qualitative | API Design Analysis | Zero static entry points - every operation requires instance creation |
| Qualitative | Developer Feedback (Inferred) | Research doc's "Target API" section shows desired simple usage patterns not currently available |

### 1.3 Impact of Not Solving

**Developers** will continue to:
- Write boilerplate DI setup code for simple device discovery
- Create unnecessary service lifetimes for one-off operations
- Experience steep learning curve when starting with the SDK
- Build custom wrapper classes to hide the complexity

**SDK Adoption** will:
- Remain slower for command-line and scripting scenarios
- Require more example code to demonstrate basic operations
- Create perception of "enterprise-only" API design

**Codebase Maintenance** will:
- Retain unnecessary architectural complexity (two services + channel pattern)
- Maintain unused abstractions (`IDeviceChannel`) that add no value
- Continue managing separate service lifecycles that could be unified

---

## 2. User Stories

### Story 1: Simple Device Discovery
**As a** developer building a command-line tool,  
**I want to** call a static method to find all YubiKeys without configuring services,  
**So that** I can enumerate devices in 2-3 lines of code.

**Acceptance Criteria:**
- [ ] `YubiKeyManager.FindAllAsync(CancellationToken)` returns all connected YubiKeys without prior setup
- [ ] `YubiKeyManager.FindAllAsync(ConnectionType, CancellationToken)` filters by connection type
- [ ] Method works without calling any initialization or DI setup
- [ ] Returns `IReadOnlyList<IYubiKey>` consistent with existing instance API
- [ ] Thread-safe for concurrent calls from multiple threads
- [ ] No background monitoring is started by `FindAllAsync()` calls
- [ ] Each `IYubiKey` represents a transport-specific device endpoint
- [ ] Cancellation token cancels in-flight device scan

### Story 2: Device Monitoring Lifecycle
**As a** developer building an interactive application,  
**I want to** explicitly start and stop device monitoring,  
**So that** I have clear control over background resource usage.

**Acceptance Criteria:**
- [ ] `YubiKeyManager.StartMonitoring()` begins background device scanning
- [ ] `YubiKeyManager.StartMonitoring(TimeSpan interval)` allows custom scan intervals
- [ ] `YubiKeyManager.StopMonitoring()` stops background scanning and releases resources
- [ ] `YubiKeyManager.IsMonitoring` property indicates current monitoring state
- [ ] Multiple `StartMonitoring()` calls are idempotent (no duplicate tasks)
- [ ] `StopMonitoring()` waits for in-flight scan to complete before returning
- [ ] Starting monitoring does not block the calling thread

### Story 3: Device Change Events
**As a** developer building a device management UI,  
**I want to** subscribe to an observable that emits device arrival/removal events,  
**So that** I can update my UI when YubiKeys are connected or disconnected.

**Acceptance Criteria:**
- [ ] `YubiKeyManager.DeviceChanges` exposes `IObservable<DeviceEvent>`
- [ ] `DeviceEvent` contains `Action` (Added/Removed via `DeviceAction` enum) and `Device` (`IYubiKey`)
- [ ] Events are only emitted when monitoring is active
- [ ] Subscribing to `DeviceChanges` before `StartMonitoring()` does not start monitoring automatically
- [ ] Events fire on background thread (not UI thread)
- [ ] Multiple subscribers receive the same events
- [ ] Unsubscribing does not affect other subscribers or monitoring state

### Story 4: Clean Shutdown
**As a** developer managing application lifecycle,  
**I want to** shut down all YubiKeyManager resources in one call,  
**So that** I can cleanly exit my application without resource leaks.

**Acceptance Criteria:**
- [ ] `YubiKeyManager.ShutdownAsync(CancellationToken)` stops monitoring if active (async, primary API)
- [ ] `YubiKeyManager.Shutdown()` provides sync convenience (calls `ShutdownAsync().GetAwaiter().GetResult()`)
- [ ] `ShutdownAsync()` clears internal device cache
- [ ] `ShutdownAsync()` cancels any in-flight device scans and awaits completion
- [ ] After shutdown, calling `FindAllAsync()` performs fresh scan
- [ ] After shutdown, calling `StartMonitoring()` works correctly
- [ ] Both shutdown methods are idempotent (safe to call multiple times)
- [ ] `ShutdownAsync()` respects cancellation token for timeout scenarios

### Story 5: Clean Static API (Replaces DI)
**As a** developer with any application architecture,  
**I want to** use a simple static API without DI setup,  
**So that** I have one clear way to interact with YubiKeys.

**Acceptance Criteria:**
- [ ] `YubiKeyManager` is a static class with no instance members
- [ ] No `IYubiKeyManager` interface exists
- [ ] No `AddYubiKeyManagerCore()` DI extension exists
- [ ] All functionality available via static methods
- [ ] `IDeviceRepository` remains internal for advanced isolation scenarios

---

## 3. Functional Requirements

### 3.1 Happy Path

| Step | User Action | System Response |
|------|-------------|-----------------|
| 1 | Call `YubiKeyManager.FindAllAsync()` | Performs on-demand device scan, returns list of `IYubiKey` instances |
| 2 | Call `YubiKeyManager.StartMonitoring()` | Starts background task that scans devices at default interval (5 seconds) |
| 3 | Subscribe to `YubiKeyManager.DeviceChanges` | Receives `IObservable<DeviceEvent>` that emits Added/Removed events |
| 4 | Connect a YubiKey while monitoring | Emits `DeviceEvent` with `DeviceAction.Added` and device instance |
| 5 | Disconnect a YubiKey while monitoring | Emits `DeviceEvent` with `DeviceAction.Removed` and device instance |
| 6 | Call `YubiKeyManager.StopMonitoring()` | Stops background task, no more events emitted |
| 7 | Call `YubiKeyManager.ShutdownAsync()` | Stops monitoring, clears cache, releases resources |

### 3.2 Error States (Unhappy Paths)

| Condition | System Behavior | Error Type |
|-----------|-----------------|------------|
| `FindAllAsync()` called with no devices connected | Returns empty list | None (valid state) |
| `FindAllAsync()` fails due to platform API error | Throws `PlatformInteropException` with context | Exception |
| `StartMonitoring()` called when already monitoring | No-op, does not start duplicate task | None (idempotent) |
| `StopMonitoring()` called when not monitoring | No-op | None (idempotent) |
| `DeviceChanges` subscribed but monitoring not started | No events emitted | None (valid state) |
| Background scan throws exception | Exception logged, monitoring continues | Internal handling |
| `ShutdownAsync()` called while scan in progress | Awaits scan completion, then cleans up | None (async wait) |
| `ShutdownAsync()` cancelled via token | Stops waiting, may leave partial state | `OperationCanceledException` |
| `FindAllAsync()` called during active monitoring | Performs independent scan, does not affect monitoring | None (concurrent operation) |
| Thread A calls `FindAllAsync()` while Thread B calls `StartMonitoring()` | Both operations complete safely via lock | None (thread-safe) |

### 3.3 Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Call `FindAllAsync()` 1000 times concurrently | Each call performs independent scan, protected by internal locking |
| Subscribe to `DeviceChanges`, never call `StartMonitoring()` | No events ever emitted, no resources consumed |
| Call `ShutdownAsync()` immediately after `StartMonitoring()` | Background task cancelled before first scan completes |
| Device disconnects during `FindAllAsync()` scan | Device may or may not appear in results (race condition, documented behavior) |
| Monitoring interval = 0ms | Throws `ArgumentOutOfRangeException` (minimum interval enforced) |
| Application exits without calling `ShutdownAsync()` | Background task stopped by OS process cleanup (not ideal, but safe) |
| Static API and advanced users need isolation | Use `IDeviceRepository` directly (internal API) |

---

## 4. Non-Functional Requirements

### 4.1 Performance

- **Device scan latency:** ≤ 500ms for up to 10 connected devices
- **Memory footprint:** ≤ 5MB additional for monitoring task and cache
- **Monitoring CPU usage:** ≤ 1% CPU when idle (no device changes)
- **Event dispatch latency:** ≤ 100ms from device connection to event emission

### 4.2 Thread Safety

- All static methods must be thread-safe
- Internal cache must use thread-safe collection or locking
- Lazy initialization of repository must use `Lazy<T>` with `ExecutionAndPublication` mode
- Background monitoring task must safely handle concurrent `StopMonitoring()` calls

### 4.3 Breaking Changes

This release includes intentional breaking changes to simplify the API:
- **Removed:** `IYubiKeyManager` interface
- **Removed:** `AddYubiKeyManagerCore()` DI extension method
- **Removed:** `DependencyInjection.cs`
- **Changed:** `YubiKeyManager` from instance class to static class

**Migration:** Replace `IYubiKeyManager` injection with direct `YubiKeyManager` static calls.

### 4.4 Compatibility

- **Platforms:** Windows, macOS, Linux (existing platform support unchanged)
- **.NET Version:** Same as existing SDK (currently .NET 10.0+)
- **Existing Code:** Migration required from `IYubiKeyManager` to static API

### 4.5 Testability

- Static API must be testable via:
  - `ShutdownAsync()` to reset state between tests
  - Documented test pattern for mocking device discovery
  - Internal static state that can be inspected (consider internal visibility)
- Unit tests must not interfere with each other through shared static state

---

## 5. Technical Constraints

### 5.1 Must Use

- **`DeviceRepositoryCached`** - Existing cache and event infrastructure
- **Same device discovery logic** - Reuse existing `SmartCardDeviceListener`, `HidDeviceListener` patterns
- **`IYubiKey`** - Transport-specific device references returned by discovery
- **CancellationToken** - For async operations and graceful shutdown

### 5.2 Must Not

- **Introduce new external dependencies** - Use existing SDK dependencies only
- **Require DI setup** - Static API works without any initialization

### 5.3 Dependencies

- **Internal SDK dependencies:**
  - `Yubico.Core` - Device listening infrastructure
  - `System.Reactive` - For `IObservable<DeviceEvent>` implementation
  - `System.Threading.Tasks` - For background monitoring task

---

## 6. Implementation Constraints

### 6.1 Architectural Changes

**Prerequisite:** The `yubikit-listeners` branch refactor is complete. This already accomplished:
- ✅ Merged `DeviceMonitorService` and `DeviceListenerService` into single service
- ✅ Deleted `IDeviceChannel` interface - `DeviceMonitorService` calls `UpdateCache()` directly
- ✅ Event-driven device discovery via `HidDeviceListener` and `DesktopSmartCardDeviceListener`

**Remaining Work:**

**Static Factory for Repository:** Add `DeviceRepositoryCached.Create()` static method for internal use by static API. This factory must instantiate:
- `IFindYubiKeys` (via `FindYubiKeys` which needs `IFindPcscDevices`, `IFindHidDevices`, `IYubiKeyFactory`)
- `ILogger<DeviceRepositoryCached>` (use `NullLogger<T>` for static API, or allow injection)

**Static Monitoring Loop:** The static API needs its own monitoring capability independent of DI-hosted `DeviceMonitorService`. Reuse:
- `HidDeviceListener.Create()` - factory method for platform-specific HID listener
- `new DesktopSmartCardDeviceListener()` - cross-platform PC/SC listener
- Event coalescing pattern (200ms delay via `SemaphoreSlim`)

### 6.2 Memory Safety

- Use `CancellationTokenSource` for stopping monitoring task
- Ensure background task is marked as background thread
- Dispose `CancellationTokenSource` on `ShutdownAsync()`
- No memory leaks from abandoned subscriptions to `DeviceChanges`

### 6.3 Configuration

**Default Monitoring Interval:** 5 seconds (align with existing `DeviceMonitorService` default)

**Configuration Strategy:** 
- `StartMonitoring()` - uses default interval
- `StartMonitoring(TimeSpan interval)` - allows custom interval
- **Out of scope:** Global configuration file or environment variable-based settings

---

## 7. Open Questions (RESOLVED)

### Q1: Should we keep DI support alongside static API?

**Decision:** No. Remove DI support entirely.

**Rationale:**
- Physical USB devices ARE global state - one API surface is simpler
- DI adds no value when static API provides identical functionality
- No naming conflicts between static and instance methods
- Cleaner codebase with fewer abstractions
- `IDeviceRepository` remains internal for advanced isolation scenarios

### Q2: Error handling when FindAllAsync() called during active monitoring

**Decision:** `FindAllAsync()` always performs fresh scan, even during monitoring.

**Rationale:**
- Most flexible for users
- Clear separation: `FindAllAsync()` = fresh scan, monitoring = background updates
- No surprising restrictions

### Q3: Configuration approach for scan interval, connection types

**Decision:** 
- `StartMonitoring()` - default 5 sec interval, all connection types
- `StartMonitoring(TimeSpan interval)` - custom interval, all connection types
- Connection type filtering happens at `FindAllAsync()` level, not monitoring

### Q4: Testing strategy for static API (mocking considerations)

**Decision:** Use `ShutdownAsync()` for test cleanup + `InternalsVisibleTo` for state inspection.

---

## 8. Out of Scope

- **Global configuration file** - No `appsettings.json` or environment variable support for static API
- **Automatic monitoring** - Monitoring only starts with explicit `StartMonitoring()` call
- **Lazy monitoring on subscription** - Subscribing to `DeviceChanges` does not auto-start monitoring
- **Per-connection-type monitoring** - Monitoring scans all connection types (filtering at `FindAllAsync()` level)
- **Device-specific filtering in monitoring** - Monitoring tracks all YubiKeys (filtering by serial number is user responsibility)
- **Hot-reload of configuration** - Changing monitoring interval requires stop/start
- **Metrics/telemetry** - No built-in logging or performance tracking for static API
- **Async disposal** - No `IAsyncDisposable` implementation (use `ShutdownAsync()` for cleanup)

---

## 9. Success Criteria

| Criterion | Target | Verification |
|-----------|--------|--------------|
| Static API implementation complete | All static methods + shutdown methods + properties implemented | Code review |
| INVEST compliance | All 5 user stories pass INVEST checklist | PRD audit |
| DI removed | `IYubiKeyManager` and `AddYubiKeyManagerCore()` deleted | Code review |
| Tests migrated | All tests updated to use static API | CI pipeline |
| Thread safety verified | Concurrent usage tests pass | Unit tests |
| Monitoring consolidation | ✅ COMPLETE via `yubikit-listeners` branch | Already merged |
| `IDeviceChannel` removed | ✅ COMPLETE via `yubikit-listeners` branch | Already merged |
| Documentation complete | API reference and usage examples added | Docs review |
| Performance targets met | Device scan ≤ 500ms, monitoring ≤ 1% CPU | Performance tests |

---

## 10. Related Documents

- **[Design Research](../../research/yubikey-manager-static-api-design.md)** - Architecture analysis and trade-off decisions
- **[Event-Driven Device Discovery](../../../architecture/event-driven-device-discovery.md)** - Architecture docs for the new listener-based discovery (from `yubikit-listeners`)
- **[Merge Device Services Plan](../../../plans/2026-02-09-merge-device-services.md)** - Plan that consolidated DeviceListenerService into DeviceMonitorService
- **[DX Audit Report](../../research/yubikey-manager-static-api-dx-audit.md)** - API design recommendations (referenced in research)
- **[Technical Feasibility Report](../../research/yubikey-manager-static-api-feasibility.md)** - Implementation validation (referenced in research)
- **[CLAUDE.md](../../CLAUDE.md)** - SDK patterns and conventions

---

## 11. Implementation Notes for Plan Agent

**PREREQUISITE:** The `yubikit-listeners` branch refactor is complete. This brought:
- Event-driven device discovery (OS notifications instead of 500ms polling)
- Merged `DeviceListenerService` + `DeviceChannel` into `DeviceMonitorService`
- Deleted: `DeviceChannel.cs`, `DeviceListenerService.cs`, `IDeviceChannel`, `DeviceRepositorySimple`
- Simplified listener events to `Action? DeviceEvent` callbacks (signal-only, no device construction)
- `DeviceRepositoryCached.DeviceChanges` is now always accessible (no `IsStarted` gate)

**Current Architecture (post-rebase):**
```
HidDeviceListener ─────┐
                       ├──► SignalEvent() ──► DeviceMonitorService ──► DeviceRepositoryCached
SmartCardListener ─────┘         │                    │                        │
                                 │                    ▼                        ▼
                           (coalescing)        PerformDeviceScan()      IObservable<DeviceEvent>
                            200ms delay              │
                                                     ▼
                                              UpdateCache()
```

When creating the implementation plan, consider:

1. **Phase 1: Repository Factory** *(NEW WORK)*
   - Rename `DeviceRepositoryCached` to `DeviceRepository`
   - Add `DeviceRepository.Create()` static method for non-DI usage
   - Factory must create all dependencies: `IFindYubiKeys`, `ILogger<T>`
   - Consider using `NullLoggerFactory` for static API or allow optional logger injection

2. **Phase 2: Monitoring Consolidation** *(ALREADY COMPLETE via yubikit-listeners)*
   - ✅ `DeviceListenerService` + `DeviceChannel` merged into `DeviceMonitorService`
   - ✅ `IDeviceChannel` deleted
   - ✅ Event-driven architecture with `HidDeviceListener` and `DesktopSmartCardDeviceListener`
   - ✅ `DeviceChanges` no longer gated by `IsStarted` check

3. **Phase 3: Static API Surface** *(NEW WORK)*
   - Convert `YubiKeyManager` to static class (delete instance members)
   - Implement thread-safe lazy initialization using `Lazy<T>` with `ExecutionAndPublication` mode
   - Add `IsMonitoring` property and `DeviceChanges` observable
   - For static monitoring: instantiate `HidDeviceListener.Create()` and `DesktopSmartCardDeviceListener` directly
   - Reuse the event coalescing pattern from `DeviceMonitorService`

4. **Phase 4: Remove DI Support** *(NEW WORK)*
   - Delete `IYubiKeyManager` interface
   - Delete `AddYubiKeyManagerCore()` extension method
   - Delete `DependencyInjection.cs`
   - Update all tests to use static API

5. **Phase 5: Testing** *(NEW WORK)*
   - Unit tests for each static method
   - Concurrency tests (multiple threads calling `FindAllAsync()`)
   - Monitoring lifecycle tests (start/stop/shutdown)

6. **Phase 6: Documentation** *(NEW WORK)*
   - XML doc comments for all public static members
   - Usage examples for common scenarios (discovery, monitoring, shutdown)

---

## Audit Summary

All validators passed. This specification is approved for implementation.

| Audit | Result | Report |
|-------|--------|--------|
| UX | PASS | [ux_audit.md](./ux_audit.md) |
| DX | PASS | [dx_audit.md](./dx_audit.md) |
| Technical | PASS | [feasibility_report.md](./feasibility_report.md) |
| Security | PASS | [security_audit.md](./security_audit.md) |

### Key Findings Summary

**UX Audit (0 CRITICAL, 4 WARN):**
- WARN-001: Consider `CancellationToken` overload for `FindAllAsync()`
- WARN-002: Document UI thread marshaling for events
- WARN-003: Clarify behavior when changing interval while monitoring
- WARN-004: Document subscription behavior before monitoring starts

**DX Audit (0 CRITICAL, 3 WARN):**
- WARN-001: ~~Consider async versions of `StopMonitoring()` and `Shutdown()`~~ RESOLVED: Added `ShutdownAsync(CancellationToken)`
- WARN-002: Use existing `DeviceAction.Added`/`Removed` enum values
- WARN-003: Use `Lazy<T>` instead of double-checked locking

**Technical Feasibility (0 CRITICAL, 2 WARN):**
- WARN-001: Thread safety requires careful lock implementation
- WARN-002: Remove `DeviceChanges` access validation for static API

**Security Audit (0 CRITICAL, 2 WARN):**
- WARN-001: Use `Lazy<T>` for thread-safe initialization
- WARN-002: Define exception handling strategy for background monitoring

---

## Next Steps

1. Run `write-plan` skill with this spec to create implementation plan
2. Execute plan using TDD workflow
3. Request code review before merge

---

**Estimated Implementation Time:** 8 days (1.6 weeks for 1 developer) - reduced from 13 days since Phase 2 (Monitoring Consolidation) is already complete via `yubikit-listeners` rebase.
