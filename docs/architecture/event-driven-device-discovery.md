# Event-Driven Device Discovery Architecture

## Public ownership boundary

Applications choose one-shot `YubiKeyManager.Find…Async(interfaces: ..., cancellationToken: ...)` or an
explicitly owned `await YubiKeyManager.CreateMonitorAsync(ct)`. Find uses a fresh scoped finder per call,
without a repository, listener startup, or events. The initialized `YubiKeyMonitor` composes and tears down
its worker, finder/evidence, repository, and subscriptions. It scans All even without watchers. There is no
global manager/cache or static start/stop/shutdown lifecycle.

The factory registers listeners before the loop's single initial scan and awaits successful publication.
Empty discovery is a valid initialized snapshot; initial failure/cancellation tears down partial ownership.
The creation token applies only until return. `CurrentDevices` then reads the latest completed, read-only
membership snapshot without I/O. It tracks presence/interfaces with optional last-known metadata, not
continuously refreshed configuration. Use live Management reads for authoritative configuration.

| Component | Responsibility |
|---|---|
| `YubiKeyManager` | Fresh one-shot Find operations and the public monitor factory |
| `YubiKeyMonitor` | Public lifetime/access, composition, initialization cleanup, shared disposal completion |
| `YubiKeyMonitorWorker` | Listeners, evidence invalidation, one sequential scan/publication loop, one managed-loop drain; borrows the repository |
| `YubiKeyDeviceRepository` | Membership/identity retention, read-only snapshots, atomic replay registration and ordered event commits, failure/disposal and late-commit rejection |
| `DeviceEventHub` | Independent bounded watcher buffers, overflow, cancellation, and completion |
| `FindYubiKeys` / discovery admission | Evidence epochs/read scopes, identity/metadata reads, queued-read retirement, actual native ownership |

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
YubiKeyMonitor (public composition/lifetime owner)
    ├─ owns YubiKeyMonitorWorker (one initialize-run-dispose lifetime)
    │    ├─ owns optional DesktopSmartCardDeviceListener / platform HidDeviceListener
    │    │    └─ ingress: invalidate evidence → private capacity-one occurrence signal
    │    └─ one sequential loop
    │         ├─ initial ScanAndPublishAsync() → signal initialization success
    │         ├─ hints: 200ms quiet period, capped at 5 × ThrottleInterval
    │         └─ five-second fallback when no hint arrives
    │              └─ await FindAllAsync(All) AND repository.UpdateCache before next scan
    └─ owns YubiKeyDeviceRepository (borrowed by worker)
         ├─ CurrentDevices: read-only, no-I/O membership snapshot
         └─ atomic WatchAsync snapshot/replay registration + ordered diff events
              └─ owns DeviceEventHub → independent bounded watcher buffers → application
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
Platform listener ──rescan callback/hint──► YubiKeyMonitorWorker
Worker ──ingress lock: retirement check + global evidence invalidation + TrySignal──► private signal
Single reader ──consume one occurrence──► 200ms quiet period (re-arms on new hints)
Single reader ──cap reached or quiet period elapsed──► ScanAndPublishAsync()
ScanAndPublishAsync() ──FindAllAsync snapshot──► YubiKeyDeviceRepository.UpdateCache()
YubiKeyDeviceRepository ──diff──► DeviceEventHub (asynchronous fan-out)
                                        │
                                        ▼
                          one bounded buffer (256) per watcher
                                        │
                                        ▼
                              monitor.WatchAsync(ct)
                              (IAsyncEnumerable<DeviceEvent>)
```

On first `MoveNextAsync`, the repository captures membership and eagerly registers the hub subscription
under the same short `_stateLock` used to publish membership and enqueue a complete diff. It then yields
initial Added replay and ordered deltas outside that lock. Replay does not consume delta-buffer capacity;
changes during replay still can overflow it. No consumer code runs on the publication path. Never prepend
an independent snapshot to a lazy subscription: that loses changes between capture and registration.

Cancellation or overflow terminates only one watcher. Overflow recovery clears the consumer projection
and re-enumerates the same monitor to capture a new snapshot/tail boundary. An independent one-shot Find
cannot safely resynchronize an unrelated event stream. See `YubiKeyMonitorTests` for gated race, large
replay, cancellation, overflow, and disposal tests.

---

## Rescan Signalling: an Occurrence, Not a Queue

Listener hints are **not** queued and there is no `DeviceMonitorRescanRequest` payload type.
The worker's private nested `DeviceMonitorSignal` wraps a `Channel<bool>` bounded to capacity **1** with
`FullMode = DropWrite`, `SingleReader = true`. Consequences:

- A hint arriving while one is already pending is **coalesced**, not separately buffered.
  The pending occurrence requests reconciliation, not one scan per notification. Hints during the initial
  or a later blocked scan remain pending for an ordered follow-up. Device truth is the full `FindAllAsync`
  snapshot diffed by the repository, never the hint payload; a hint is not a successful-scan acknowledgment.
- The loop consumes **exactly one occurrence per wake-up** (`TryConsume`) before evaluating
  the debounce / max-coalesce deadline, so a continuously refilled signal cannot starve the
  `MaxCoalesceInterval` check.
- `Complete()` closes the writer during disposal. A captured late listener callback observes retirement
  at ingress and cannot invalidate evidence or enqueue work. Unexpected signal completion in a live worker
  is terminal failure, not a restart trigger.
- Hint details (`HidDeviceRescanHint`) are logged at ingress for diagnostics only. They never
  reach the loop and are never public device truth.

## Single-lifetime worker and shutdown

Each worker is initialized exactly once before public exposure, runs one loop, and is retired by its owner.
There is no manual-rescan entry point, stop/start mode, interval replacement, restart after failure, or
monitor-generation machinery. Recovery creates a new owned monitor with its own repository and finder.

```text
compose resources → start best-effort listeners → launch the one loop
    → initial discovery AND publication → report ready
    → listener/debounce or interval discovery AND publication → disposal
```

- **One producer supplies ordering.** Only the loop calls the private `ScanAndPublishAsync` helper, and it
  awaits each complete scan/publication before requesting another. There is no worker scan semaphore or
  publication gate. Optional listener startup failures are cleaned up independently; fallback discovery
  still runs, but initial discovery/publication must succeed. Initial failure/cancellation cleans partial
  ownership and propagates the error. The factory token is not the worker's lifetime token.
- **One ingress lock orders invalidation against retirement.** Under `_ingressLock`, a callback checks
  retirement, globally invalidates finder identity/metadata evidence, and signals reconciliation. Disposal
  closes ingress and retires the built-in finder's read scope under the same ordering boundary. A callback
  either finishes before final retirement or observes retirement and does nothing; even a previously
  captured callback cannot install a fresh evidence epoch afterward. Lock order is ingress → finder
  evidence → read-scope gate. Retirement cancels internal queued admission waits; their callbacks do not
  wait for this loop or re-enter ingress. It is not a callback-free path. Worker-token cancellation,
  logging, listener Start/Stop/Dispose, discovery, diffing, and awaits stay outside ingress.
- **The repository supplies final commit protection.** `_cacheLock` serializes diffs that build a private
  successor dictionary and ordered event list. The short `_stateLock` coordinates membership/snapshot
  publication, complete event enqueue, atomic snapshot-plus-subscription capture, failure, and disposal.
  Snapshots are atomically replaced read-only lists; reads perform no I/O. Getters/logging during diffing
  and replay/consumer continuations run outside the state lock. A failed diff commits neither partial
  membership nor partial deltas. The hub only enqueues into isolated watcher buffers.
- **Disposal drains the actual loop once.** The public owner closes access, retires/stops the worker, then
  disposes its repository in `finally`. The worker waits once for its loop, including any in-progress
  publication, with the production **ten-second managed-drain budget**. This does **not** bound native
  listener cleanup, arbitrary native operations, or total wall-clock disposal. A late finder result is
  suppressed before diffing. An already-admitted diff may overlap teardown, so the repository checks
  disposed/failed state again at final commit: after repository disposal it cannot restore membership or
  emit new deltas. Commits during the teardown window are not retroactively erased; already-committed
  events may drain. The abandoned loop retains its timer-less cancellation source because its work can
  still register with that token; linked wait sources are disposed by the loop itself.

Healthy watcher buffers can drain events published before disposal and then complete normally; new
enumerations after disposal complete empty. `CurrentDevices` after disposal throws, while previously
obtained lists retain historical membership. Disposal is idempotent and does not dispose caller-owned
connections/sessions. Concurrent dispose callers share completion through repository teardown. A later
ordinary scan/publication failure retains the previous publication and retries on later hints/fallback;
unexpected terminal loop failure faults active/future watchers and rejects cached access **before**
best-effort diagnostic logging. Later disposal does not overwrite that fault.

### Finder evidence epochs are not monitor generations

Finder evidence epochs/read scopes remain necessary even though monitor generations are gone. Any
transport activity atomically replaces identity/metadata caches and retires their read scope; old native
completion can write only into unreachable old evidence. Retirement cancels queued reads and prevents
later opens, but **already-open native reads retain their actual admission and registry lease until
teardown**, even after the managed-loop drain expires. CCID/OTP sibling admission, same-PID serialization,
and unknown-PID atomic reservation remain unchanged. A pending unknown-PID request reserves no gates.
Neither a wait budget nor scope retirement proves native release. See
[native discovery scheduling](device-discovery-guarantees.md#native-discovery-scheduling).

`YubiKeyMonitorWorkerTests` pins sequential scans, coalescing, listener fallback, and ingress invalidation.
`YubiKeyMonitorTests` and `YubiKeyMonitorLifecycleTests` pin initialization, admitted/captured callbacks,
parked diffs, both late-publication disposal windows, shared disposal, native-read retention, and terminal
failure-before-logging. Repository tests separately pin the final commit and atomic replay boundaries.

## Key Improvements

| Aspect | Before (Polling) | After (Event-Driven) |
|--------|------------------|----------------------|
| **Detection Latency** | Polling cadence plus discovery | Native hint plus coalescing/discovery; five-second fallback, not an end-to-end bound |
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
│     // Bounded wait permits another stop check              │
│ }                                                            │
└──────────────────────────────────────────────────────────────┘

HID: macOS stops its run loop directly; Linux polls the udev monitor and an explicit shutdown event fd.
```

---

## Current source layout

```
src/Core/src/
├── Devices/
│   ├── YubiKeyMonitorWorker.cs               ◄── Single-lifetime sequential producer and listeners
│   ├── YubiKeyDeviceRepository.cs            ◄── Repository-diffed device events
│   ├── DeviceEventHub.cs                     ◄── Asynchronous fan-out behind WatchAsync
│   ├── YubiKeyMonitor.cs                     ◄── Initialized public ownership and snapshots
│   └── YubiKeyManager.cs                     ◄── One-shot Find and owned-monitor factory
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
