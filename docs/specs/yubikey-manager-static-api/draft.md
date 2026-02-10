# PRD: YubiKeyManager Static API Redesign

**Status:** DRAFT  
**Author:** spec-writer agent  
**Created:** 2026-02-06  
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
- [ ] `YubiKeyManager.FindAllAsync()` returns all connected YubiKeys without prior setup
- [ ] `YubiKeyManager.FindAllAsync(ConnectionType)` filters by connection type
- [ ] Method works without calling any initialization or DI setup
- [ ] Returns `IReadOnlyList<IYubiKey>` consistent with existing instance API
- [ ] Thread-safe for concurrent calls from multiple threads
- [ ] No background monitoring is started by `FindAllAsync()` calls

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
- [ ] `DeviceEvent` contains `EventType` (Arrived/Removed) and `Device` (IYubiKey)
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
- [ ] `YubiKeyManager.Shutdown()` stops monitoring if active
- [ ] `Shutdown()` clears internal device cache
- [ ] `Shutdown()` cancels any in-flight device scans
- [ ] After `Shutdown()`, calling `FindAllAsync()` performs fresh scan
- [ ] After `Shutdown()`, calling `StartMonitoring()` works correctly
- [ ] `Shutdown()` is idempotent (safe to call multiple times)

### Story 5: DI Compatibility
**As a** developer with existing DI infrastructure,  
**I want to** continue using instance-based `IYubiKeyManager` via DI,  
**So that** my existing code is not affected by the static API addition.

**Acceptance Criteria:**
- [ ] Existing `AddYubiKeyManagerCore()` continues to work
- [ ] `IYubiKeyManager` instance API remains unchanged
- [ ] Static and instance APIs can coexist in the same application
- [ ] Instance-based API does not interfere with static API state
- [ ] All existing unit tests for instance-based API pass without changes

---

## 3. Functional Requirements

### 3.1 Happy Path

| Step | User Action | System Response |
|------|-------------|-----------------|
| 1 | Call `YubiKeyManager.FindAllAsync()` | Performs on-demand device scan, returns list of `IYubiKey` instances |
| 2 | Call `YubiKeyManager.StartMonitoring()` | Starts background task that scans devices at default interval (5 seconds) |
| 3 | Subscribe to `YubiKeyManager.DeviceChanges` | Receives `IObservable<DeviceEvent>` that emits Arrived/Removed events |
| 4 | Connect a YubiKey while monitoring | Emits `DeviceEvent` with `EventType.Arrived` and device instance |
| 5 | Disconnect a YubiKey while monitoring | Emits `DeviceEvent` with `EventType.Removed` and device instance |
| 6 | Call `YubiKeyManager.StopMonitoring()` | Stops background task, no more events emitted |
| 7 | Call `YubiKeyManager.Shutdown()` | Stops monitoring, clears cache, releases resources |

### 3.2 Error States (Unhappy Paths)

| Condition | System Behavior | Error Type |
|-----------|-----------------|------------|
| `FindAllAsync()` called with no devices connected | Returns empty list | None (valid state) |
| `FindAllAsync()` fails due to platform API error | Throws `PlatformInteropException` with context | Exception |
| `StartMonitoring()` called when already monitoring | No-op, does not start duplicate task | None (idempotent) |
| `StopMonitoring()` called when not monitoring | No-op | None (idempotent) |
| `DeviceChanges` subscribed but monitoring not started | No events emitted | None (valid state) |
| Background scan throws exception | Exception logged, monitoring continues | Internal handling |
| `Shutdown()` called while scan in progress | Waits for scan completion, then cleans up | None (blocking) |
| `FindAllAsync()` called during active monitoring | Performs independent scan, does not affect monitoring | None (concurrent operation) |
| Thread A calls `FindAllAsync()` while Thread B calls `StartMonitoring()` | Both operations complete safely via lock | None (thread-safe) |

### 3.3 Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Call `FindAllAsync()` 1000 times concurrently | Each call performs independent scan, protected by internal locking |
| Subscribe to `DeviceChanges`, never call `StartMonitoring()` | No events ever emitted, no resources consumed |
| Call `Shutdown()` immediately after `StartMonitoring()` | Background task cancelled before first scan completes |
| Device disconnects during `FindAllAsync()` scan | Device may or may not appear in results (race condition, documented behavior) |
| Monitoring interval = 0ms | Throws `ArgumentOutOfRangeException` (minimum interval enforced) |
| Application exits without calling `Shutdown()` | Background task stopped by OS process cleanup (not ideal, but safe) |
| Static API and DI API used in same application | Both use separate internal state, no conflict |

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
- Lazy initialization of repository must use double-checked locking pattern
- Background monitoring task must safely handle concurrent `StopMonitoring()` calls

### 4.3 Compatibility

- **Platforms:** Windows, macOS, Linux (existing platform support unchanged)
- **.NET Version:** Same as existing SDK (currently .NET 8.0+)
- **Breaking Changes:** NONE - this is additive functionality
- **Existing Code:** All current instance-based code continues to work unchanged

### 4.4 Testability

- Static API must be testable via:
  - `Shutdown()` to reset state between tests
  - Documented test pattern for mocking device discovery
  - Internal static state that can be inspected (consider internal visibility)
- Unit tests must not interfere with each other through shared static state

---

## 5. Technical Constraints

### 5.1 Must Use

- **`DeviceRepositoryCached`** - Existing cache and event infrastructure
- **Same device discovery logic** - Reuse existing `SmartCardDeviceListener`, `HidDeviceListener` patterns
- **`IYubiKey` interface** - Return types must match existing API
- **CancellationToken** - For async operations and graceful shutdown

### 5.2 Must Not

- **Break existing DI API** - `IYubiKeyManager` interface and implementation unchanged
- **Change device discovery behavior** - Same device enumeration logic as instance API
- **Introduce new external dependencies** - Use existing SDK dependencies only
- **Make static API required** - Must remain optional for DI users

### 5.3 Dependencies

- **Internal SDK dependencies:**
  - `Yubico.Core` - Device listening infrastructure
  - `System.Reactive` - For `IObservable<DeviceEvent>` implementation
  - `System.Threading.Tasks` - For background monitoring task

---

## 6. Implementation Constraints

### 6.1 Architectural Changes

**Merge Services:** Combine `DeviceMonitorService` and `DeviceListenerService` into single internal monitoring loop within `YubiKeyManager`. The producer/consumer pattern via `IDeviceChannel` is unnecessary for this domain.

**Remove Abstractions:** Delete `IDeviceChannel` interface - direct calls to `UpdateCache()` on the repository.

**Static Factory for Repository:** Add `DeviceRepositoryCached.Create()` static method for internal use by static API.

### 6.2 Memory Safety

- Use `CancellationTokenSource` for stopping monitoring task
- Ensure background task is marked as background thread
- Dispose `CancellationTokenSource` on `Shutdown()`
- No memory leaks from abandoned subscriptions to `DeviceChanges`

### 6.3 Configuration

**Default Monitoring Interval:** 5 seconds (align with existing `DeviceMonitorService` default)

**Configuration Strategy:** 
- `StartMonitoring()` - uses default interval
- `StartMonitoring(TimeSpan interval)` - allows custom interval
- **Out of scope:** Global configuration file or environment variable-based settings

---

## 7. Open Questions

### Q1: Should static and DI APIs share the same cache?

**Options:**
1. **Shared cache** - Static API and DI instances all use same `DeviceRepositoryCached` singleton
2. **Separate caches** - Static API uses its own cache, DI instances use their own

**Recommendation:** Separate caches. Rationale:
- Prevents surprising behavior where DI cache is affected by static API calls
- Allows independent lifecycle management
- Simpler testing (static tests don't interfere with DI tests)

**Decision:** Separate caches for static and DI APIs.

### Q2: Error handling when FindAllAsync() called during active monitoring

**Options:**
1. **Allow concurrent operation** - `FindAllAsync()` performs independent scan
2. **Return cached results** - `FindAllAsync()` returns current cache contents
3. **Throw exception** - Disallow `FindAllAsync()` when monitoring active

**Recommendation:** Allow concurrent operation. Rationale:
- Most flexible for users
- Clear separation: `FindAllAsync()` = fresh scan, monitoring = background updates
- No surprising restrictions

**Decision:** `FindAllAsync()` always performs fresh scan, even during monitoring.

### Q3: Configuration approach for scan interval, connection types

**Options:**
1. **Method parameters** - `StartMonitoring(TimeSpan interval, ConnectionType type)`
2. **Static properties** - `YubiKeyManager.DefaultInterval`, `YubiKeyManager.ConnectionType`
3. **Configuration object** - `StartMonitoring(MonitoringOptions options)`

**Recommendation:** Method parameters with sensible defaults. Rationale:
- Simple and explicit
- No global state to manage
- Easy to test different configurations

**Decision:** 
- `StartMonitoring()` - default 5 sec interval, all connection types
- `StartMonitoring(TimeSpan interval)` - custom interval, all connection types
- Connection type filtering happens at `FindAllAsync()` level, not monitoring

### Q4: Testing strategy for static API (mocking considerations)

**Options:**
1. **Internal testable state** - Use `InternalsVisibleTo` for test assembly
2. **Test-only reset method** - `ResetForTesting()` that fully resets static state
3. **Dependency injection for static** - Static methods delegate to injected instance (complex)

**Recommendation:** Combine options 1 and 2. Rationale:
- `Shutdown()` already provides reset mechanism
- `InternalsVisibleTo` allows inspecting internal state
- Document test pattern: call `Shutdown()` in test teardown

**Decision:** Use `Shutdown()` for test cleanup + `InternalsVisibleTo` for state inspection.

---

## 8. Out of Scope

- **Global configuration file** - No `appsettings.json` or environment variable support for static API
- **Automatic monitoring** - Monitoring only starts with explicit `StartMonitoring()` call
- **Lazy monitoring on subscription** - Subscribing to `DeviceChanges` does not auto-start monitoring
- **Per-connection-type monitoring** - Monitoring scans all connection types (filtering at `FindAllAsync()` level)
- **Device-specific filtering in monitoring** - Monitoring tracks all YubiKeys (filtering by serial number is user responsibility)
- **Hot-reload of configuration** - Changing monitoring interval requires stop/start
- **Metrics/telemetry** - No built-in logging or performance tracking for static API
- **Async disposal** - No `IAsyncDisposable` implementation (use `Shutdown()` for cleanup)

---

## 9. Success Criteria

| Criterion | Target | Verification |
|-----------|--------|--------------|
| Static API implementation complete | All 4 static methods + 1 property implemented | Code review |
| INVEST compliance | All 5 user stories pass INVEST checklist | PRD audit |
| No breaking changes | All existing tests pass | CI pipeline |
| Thread safety verified | Concurrent usage tests pass | Unit tests |
| Monitoring consolidation | `DeviceMonitorService` + `DeviceListenerService` merged | Code review |
| `IDeviceChannel` removed | Interface deleted from codebase | Code review |
| Documentation complete | API reference and usage examples added | Docs review |
| Performance targets met | Device scan ≤ 500ms, monitoring ≤ 1% CPU | Performance tests |

---

## 10. Related Documents

- **[Design Research](../../research/yubikey-manager-static-api-design.md)** - Architecture analysis and trade-off decisions
- **[DX Audit Report](../../research/yubikey-manager-static-api-dx-audit.md)** - API design recommendations (referenced in research)
- **[Technical Feasibility Report](../../research/yubikey-manager-static-api-feasibility.md)** - Implementation validation (referenced in research)
- **[CLAUDE.md](../../CLAUDE.md)** - SDK patterns and conventions

---

## 11. Implementation Notes for Plan Agent

When creating the implementation plan, consider:

1. **Phase 1: Repository Factory**
   - Add `DeviceRepositoryCached.Create()` static method
   - Ensure repository can be used without DI

2. **Phase 2: Monitoring Consolidation**
   - Merge `DeviceMonitorService` and `DeviceListenerService` into single loop
   - Remove `IDeviceChannel` and async channel dependency
   - Preserve all device scanning logic

3. **Phase 3: Static API Surface**
   - Add static methods to `YubiKeyManager` (or create new `YubiKeyManager` static class)
   - Implement thread-safe lazy initialization of internal repository
   - Add `IsMonitoring` property and `DeviceChanges` observable

4. **Phase 4: Testing**
   - Unit tests for each static method
   - Concurrency tests (multiple threads calling `FindAllAsync()`)
   - Monitoring lifecycle tests (start/stop/shutdown)
   - Static + DI coexistence tests

5. **Phase 5: Documentation**
   - XML doc comments for all public static members
   - Usage examples for common scenarios (discovery, monitoring, shutdown)
   - Migration guide for users of existing instance API

---

## 12. INVEST Verification

### User Story 1: Simple Device Discovery
- ✅ **Independent** - Can be implemented without other stories
- ✅ **Negotiable** - Focuses on WHAT (find devices), not HOW (implementation)
- ✅ **Valuable** - Enables command-line tools and scripts (end-user value)
- ✅ **Estimable** - Clear scope: single method with filtering
- ✅ **Small** - Single method implementation ≤ 2 days
- ✅ **Testable** - Can verify return value and filtering behavior

### User Story 2: Device Monitoring Lifecycle
- ✅ **Independent** - Monitoring lifecycle separate from events subscription
- ✅ **Negotiable** - Focuses on start/stop control, not internal task management
- ✅ **Valuable** - Gives users control over resource usage
- ✅ **Estimable** - Well-defined: start/stop methods + property
- ✅ **Small** - 3 members (StartMonitoring, StopMonitoring, IsMonitoring) ≤ 3 days
- ✅ **Testable** - Can verify monitoring state and idempotence

### User Story 3: Device Change Events
- ✅ **Independent** - Observable pattern independent of monitoring lifecycle (though depends on monitoring being active for events)
- ✅ **Negotiable** - Focuses on WHAT events are exposed, not HOW events are generated
- ✅ **Valuable** - Enables reactive UIs and device management applications
- ✅ **Estimable** - Single observable property + event model ≤ 2 days
- ✅ **Testable** - Can verify events fire on device connection/disconnection

### User Story 4: Clean Shutdown
- ✅ **Independent** - Cleanup logic independent of other features
- ✅ **Negotiable** - Focuses on resource cleanup, not internal implementation
- ✅ **Valuable** - Prevents resource leaks, enables clean application exit
- ✅ **Estimable** - Single method with clear behavior ≤ 1 day
- ✅ **Testable** - Can verify resources released and state reset

### User Story 5: DI Compatibility
- ✅ **Independent** - Existing instance API unchanged, no dependencies on other stories
- ✅ **Negotiable** - Focuses on coexistence requirement, not internal architecture
- ✅ **Valuable** - Protects existing users, enables gradual migration
- ✅ **Estimable** - Validation via existing test suite (no new code)
- ✅ **Small** - Verification only, no implementation ≤ 1 day
- ✅ **Testable** - Existing tests verify instance API unchanged

---

## 13. Error State Audit

**Every user action defined in Section 2 has corresponding error states in Section 3.2.**

| User Story | User Action | Error States Defined |
|------------|-------------|---------------------|
| US-1 | Call `FindAllAsync()` | No devices connected (returns empty list), Platform API error (exception) |
| US-1 | Call `FindAllAsync(ConnectionType)` | Same as above + filtering logic |
| US-2 | Call `StartMonitoring()` | Already monitoring (idempotent), Background scan exception (internal handling) |
| US-2 | Call `StartMonitoring(TimeSpan)` | Interval = 0ms (exception), Already monitoring (idempotent) |
| US-2 | Call `StopMonitoring()` | Not monitoring (idempotent) |
| US-3 | Subscribe to `DeviceChanges` | Monitoring not started (no events), Unsubscribe behavior (documented) |
| US-4 | Call `Shutdown()` | Scan in progress (blocking wait), Already shut down (idempotent) |
| US-5 | Use static + DI APIs together | Concurrent access (separate caches, no conflict) |

**Edge case coverage:**
- Concurrent access from multiple threads (Section 3.3)
- Device disconnects during scan (Section 3.3)
- Invalid configuration (interval = 0ms) (Section 3.3)
- Application exit without cleanup (Section 3.3)
- Extreme load (1000 concurrent calls) (Section 3.3)

✅ **All user actions have defined error states and edge cases.**
