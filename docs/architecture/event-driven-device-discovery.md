# Event-Driven Device Discovery Architecture

## Before vs After

### BEFORE: Timer-Based Polling (500ms)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       Legacy timer-based polling                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  PeriodicTimer (500ms)                                                       │
│      │                                                                       │
│      ▼                                                                       │
│  Task.Run() async-over-sync scan                                             │
│      ├─ PC/SC scan: SCardGetStatusChange(timeout=0)                           │
│      └─ HID scan: full platform enumeration every cycle                        │
│      │                                                                       │
│      ▼                                                                       │
│  Cache update + observable device notifications                              │
│      │                                                                       │
│      ▼                                                                       │
│  Application                                                                 │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Problems:**
- ❌ 500ms latency for device detection
- ❌ CPU wasted polling when nothing changes
- ❌ Full device enumeration every cycle
- ❌ Windows HID not implemented
- ❌ Async-over-sync anti-pattern

---

### AFTER: Event-Driven Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                         YubiKeyDeviceMonitorService                                  │
│                         (plain async service)                                         │
├─────────────────────────────────────────────────────────────────────────────────────┤
│  Platform listeners                                                                  │
│    ├─ DesktopSmartCardDeviceListener: SCardGetStatusChange(1000ms)                   │
│    └─ HidDeviceListener: Windows/macOS/Linux OS notifications                        │
│             │                                                                        │
│             ▼                                                                        │
│  DeviceMonitorSignal: capacity-one occurrence (single reader)                        │
│             │                                                                        │
│             ▼                                                                        │
│  Event coalescing loop                                                               │
│    ├─ initial RescanCoreAsync() at monitor startup                                   │
│    ├─ consumes exactly one signal occurrence per wake-up                             │
│    ├─ listener hints wait for a 200ms quiet period                                   │
│    ├─ each new hint re-arms the quiet period                                         │
│    ├─ MaxCoalesceInterval = 5 × ThrottleInterval caps a hint storm                   │
│    └─ periodic interval fallback triggers a rescan when no listener hint arrives     │
│             │                                                                        │
│             ▼                                                                        │
│  RescanCoreAsync()                                                                   │
│    ├─ IFindYubiKeys.FindAllAsync(ConnectionType.All, ...)                            │
│    └─ YubiKeyDeviceRepository.UpdateCache(discoveredDevices)                         │
│             │                                                                        │
│             ▼                                                                        │
│  YubiKeyDeviceRepository.DeviceChanges                                               │
│    └─ repository-diffed DeviceEvent Added/Removed notifications                      │
│             │                                                                        │
│             ▼                                                                        │
│  Application                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Class Hierarchy

```
                          IDisposable
                              │
           ┌──────────────────┴──────────────────┐
           │                                     │
           ▼                                     ▼
┌─────────────────────────┐         ┌─────────────────────────┐
│ ISmartCardDeviceListener│         │   HidDeviceListener     │
│      (interface)        │         │    (abstract class)     │
├─────────────────────────┤         ├─────────────────────────┤
│ + DeviceEvent callback  │         │ + DeviceEvent : Action<HidDeviceRescanHint> │
│ + Status { get; }       │         │ + Status { get; set; }  │
└────────────┬────────────┘         │ # OnDeviceEvent(hint)   │
             │                      │ + Create() : static     │
             ▼                      └────────────┬────────────┘
┌─────────────────────────┐                      │
│DesktopSmartCardDevice   │         ┌────────────┼────────────┐
│       Listener          │         │            │            │
│  (concrete, internal)   │         ▼            ▼            ▼
├─────────────────────────┤    ┌─────────┐  ┌─────────┐  ┌─────────┐
│ Dedicated thread        │    │ Windows │  │  macOS  │  │  Linux  │
│ SCardGetStatusChange    │    │   Hid   │  │   Hid   │  │   Hid   │
│ 1000ms timeout          │    │ Device  │  │ Device  │  │ Device  │
│ Works on Win/Mac/Linux  │    │Listener │  │Listener │  │Listener │
└─────────────────────────┘    └─────────┘  └─────────┘  └─────────┘

Why different patterns?
━━━━━━━━━━━━━━━━━━━━━━━
• SmartCard: PC/SC API is cross-platform (same on all OSes)
  → Only ONE implementation needed
  → Interface is sufficient

• HID: Each OS has different APIs for device notifications
  → THREE implementations needed
  → Abstract class shares: events, status, safe invocation, disposal
```

---

## HID Listener Details by Platform

```
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ Windows          │  │ macOS            │  │ Linux            │
│ HidDeviceListener│  │ HidDeviceListener│  │ HidDeviceListener│
├──────────────────┤  ├──────────────────┤  ├──────────────────┤
│                  │  │                  │  │                  │
│ CM_Register_     │  │ IOHIDManager     │  │ udev_monitor_    │
│ Notification     │  │ Create()         │  │ new_from_netlink │
│     │            │  │     │            │  │     │            │
│     ▼            │  │     ▼            │  │     ▼            │
│ Callback from OS │  │ CFRunLoop        │  │ poll(monitor+fd) │
│ on device change │  │ RunInMode(100ms) │  │     │            │
│     │            │  │     │            │  │     ▼            │
│     ▼            │  │     ▼            │  │ udev_monitor_    │
│ Rescan hint      │  │ Matching/Removal │  │ receive_device   │
│ callback         │  │ callbacks fire   │  │     │            │
│                  │  │     │            │  │     ▼            │
│ GCHandle pins    │  │     ▼            │  │ Check action:    │
│ callback delegate│  │ Rescan hint      │  │ add/remove hint  │
│                  │  │ callback         │  │ stable identity  │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

---

## Event Flow Sequence

```
YubiKey/OS ──device notification──► Platform listener
Platform listener ──rescan callback/hint──► YubiKeyDeviceMonitorService
YubiKeyDeviceMonitorService ──TrySignal──► DeviceMonitorSignal (capacity-one occurrence)
Single reader ──consume one occurrence──► 200ms quiet period (re-arms on new hints)
Single reader ──cap reached or quiet period elapsed──► RescanCoreAsync()
RescanCoreAsync() ──FindAllAsync snapshot──► YubiKeyDeviceRepository.UpdateCache()
YubiKeyDeviceRepository ──diff──► DeviceEventBroadcaster (multicast)
                                        │
              ┌─────────────────────────┴─────────────────────────┐
              ▼                                                   ▼
   YubiKeyManager.DeviceChanges                    DeviceEventStream (bounded buffer)
   (IObservable<DeviceEvent>)                                     │
              │                                                   ▼
              ▼                                      YubiKeyManager.WatchAsync(ct)
   Application observer                              (IAsyncEnumerable<DeviceEvent>)
```

---

## Rescan Signalling: an Occurrence, Not a Queue

Listener hints are **not** queued and there is no `DeviceMonitorRescanRequest` payload type.
`DeviceMonitorSignal` wraps a `Channel<bool>` bounded to capacity **1** with
`FullMode = DropWrite`, `SingleReader = true`. Consequences:

- A hint arriving while one is already pending is **silently dropped**, not buffered.
  "At least one rescan happened after your hint" is the only guarantee — and the only one
  that matters, because device truth is the full `FindAllAsync` snapshot diffed by the
  repository, never the hint payload.
- The loop consumes **exactly one occurrence per wake-up** (`TryConsume`) before evaluating
  the debounce / max-coalesce deadline, so a continuously refilled signal cannot starve the
  `MaxCoalesceInterval` check.
- `Complete()` closes the writer. A reader observing completion (`DeviceMonitorWaitResult.Completed`)
  exits the loop; a late listener callback signalling a completed signal is logged at trace and ignored.
- Hint details (`HidDeviceRescanHint`) are logged at ingress for diagnostics only. They never
  reach the loop and are never public device truth.

## Generation (Epoch) Model

Monitor lifecycle is an **epoch model**, not a state machine. Each `StartMonitoring` builds an
immutable `MonitorGeneration` bundling `{ Id, ScanGate, Signal, Cts }`, held in a single field
`_current`. The loop and every `RescanAsync` capture that reference once; a generation's identity
*is* that reference, so a torn gate/generation pair is not representable.

- **Publication is gated, not coordinated.** All publications, from any generation, are mutually
  exclusive under the monitor service's single never-disposed `_publishGate`, held across the
  admission check and `UpdateCache`. This currently relies on `UpdateCache` publishing
  synchronously and finishing before it returns.
- **Admission is the linearization point.** Under the small `_publishLock`, a snapshot is applied
  only if its generation is still `_current` and the service is not disposed. A superseded
  generation can never publish — including a scan hung in native I/O that returns long after its
  generation was retired.
- **Lifecycle never blocks on subscribers.** `StartMonitoring` / `StopMonitoring` / `DisposeAsync`
  take only `_publishLock`, never `_publishGate`, so a device-event handler blocking inside
  `UpdateCache` cannot wedge start, stop, or dispose.
- **Nothing is disposed that anyone can still acquire.** Scan gates live inside their generation and
  are never disposed; abandoned generations are simply unreachable garbage. `DisposeAsync` drains
  `_publishGate` with the shutdown bound and, on timeout, warns and abandons — a publication already
  in flight may therefore complete after `DisposeAsync` returns. The manager disposes the repository
  afterwards, which silences any later emission. This is a documented contract, not an accident.

## Key Improvements

| Aspect | Before (Polling) | After (Event-Driven) |
|--------|------------------|----------------------|
| **Detection Latency** | Up to 500ms | ~5ms (HID) / 1000ms max (SmartCard) |
| **CPU Usage (Idle)** | Constant polling | Near-zero (waiting on OS) |
| **SmartCard Thread** | Task.Run (pool thread) | Dedicated background thread |
| **HID Windows** | ❌ Not implemented | ✅ CM_Register_Notification |
| **HID macOS** | Full enumeration | ✅ IOHIDManager callbacks |
| **HID Linux** | udev_enumerate | ✅ udev_monitor + poll() |
| **Event Coalescing** | None | Capacity-one occurrence signal + 200ms quiet period capped at `MaxCoalesceInterval` |
| **Cancellation** | Unreliable | Responsive (eventfd/run-loop stop/PCSC timeout) |

---

## Timeout Strategy: Why Not INFINITE?

```
INFINITE timeout (0xFFFFFFFF):
┌──────────────────────────────────────────────────────────────┐
│ SCardGetStatusChange(context, INFINITE, states)              │
│                                                              │
│ Thread blocks until:                                         │
│  • Device state changes, OR                                  │
│  • SCardCancel() called, OR                                  │
│  • PC/SC service stops                                       │
│                                                              │
│ Problem: SCardCancel() not reliable on all platforms!        │
│ Result: Dispose() may hang forever waiting for thread        │
└──────────────────────────────────────────────────────────────┘

1000ms timeout (our approach):
┌──────────────────────────────────────────────────────────────┐
│ while (_isListening)                                         │
│ {                                                            │
│     SCardGetStatusChange(context, 1000, states);             │
│     // Returns after 1000ms even if no change                │
│     // Loop checks _isListening flag                         │
│     // Dispose() sets _isListening = false                   │
│     // Thread exits within 1000ms guaranteed                 │
│ }                                                            │
└──────────────────────────────────────────────────────────────┘

HID: macOS stops its run loop directly; Linux polls the udev monitor and an explicit shutdown event fd.
```

---

## Files Created/Modified

```
src/Core/src/
├── Devices/
│   ├── YubiKeyDeviceMonitorService.cs        ◄── Event-driven monitor loop
│   ├── YubiKeyDeviceRepository.cs            ◄── Repository-diffed DeviceChanges
│   ├── IYubiKeyDeviceMonitorService.cs       ◄── Monitor contract
│   ├── IYubiKeyDeviceRepository.cs           ◄── Cache contract
│   └── YubiKeyManager.cs                     ◄── Static public entry point
└── Transports/
    ├── SmartCard/
    │   ├── ISmartCardDeviceListener.cs       ◄── Listener interface
    │   └── DesktopSmartCardDeviceListener.cs ◄── PC/SC implementation
    └── Hid/
        ├── HidDeviceListener.cs              ◄── Abstract listener base
        ├── HidDeviceRescanHint.cs            ◄── Diagnostic rescan hint
        ├── Windows/WindowsHidDeviceListener.cs
        ├── MacOS/MacOSHidDeviceListener.cs
        └── Linux/LinuxHidDeviceListener.cs
```

---

## Deferred design decisions

Recorded when `System.Reactive` was removed from the SDK. These were considered and consciously
**not** done. Each lists the trigger that should reopen it, so they are revisited on evidence rather
than on taste.

| # | Deferred | Why not now | Reopen when |
|---|---|---|---|
| 1 | `WaitForDeviceAsync(...)` convenience helper | Purely additive; the existing `WatchAsync` examples already cover the pattern | Consumers repeatedly hand-roll the same wait loop |
| 2 | Filtering overloads, e.g. `WatchAsync(DeviceAction)` | Additive; consumers filter inside the loop | The filter-in-loop boilerplate shows up across several consumers |
| 3 | Classic `event EventHandler<DeviceEventArgs>` surface | Weakest of the three primitives: no cancellation, no composition, `async void` handlers, and a static event roots subscribers that forget `-=`. v1's `YubiKeyDeviceListener` had to hand-roll per-handler invocation and swallow exceptions to protect its background thread | v1 migrators ask for `Arrived`/`Removed` parity. Additive over the broadcaster; no door is closed |
| 4 | Partial-delivery-on-throw for `OnNext` | A throwing observer aborts delivery to later observers. Inherited from Rx's `Subject<T>` and pinned by monitor-service tests; changing it is a behaviour change beyond the scope of a dependency removal | Someone deliberately revisits the delivery contract, with the pinning tests updated together |
| 5 | Guaranteeing no `OnNext` after `OnCompleted` under concurrent publish/complete | The broadcaster is state-safe under concurrent publish/complete, but enforcing strict observer grammar inside it requires holding a lock across observer callbacks, which lets a blocking subscriber wedge start/stop/dispose. Implemented, deadlocked the blocking-subscriber tests, reverted. The `IObservable` contract puts serialisation on the producer, and the monitor's publish gate already provides it | The monitor stops serialising publications, or a non-blocking way to gate delivery is found |
| 6 | Generic `Broadcaster<T>` / `ToAsyncEnumerable<T>` | One event stream, one call site — generalising it would be speculative surface. `DeviceEventStream` is concrete for the same reason | A second independent observable stream appears in the SDK. Two call sites is duplication; one is preference |
| 7 | Re-adopting `System.Reactive` if it ever ships AOT metadata | The dependency is gone and nothing needs it back; consumers who want Rx add it themselves and it composes unchanged | Never, most likely — recorded so the decision is not relitigated. Rx would first need a `net10.0+` target, since `IsAotCompatible` was introduced in .NET 10 |
