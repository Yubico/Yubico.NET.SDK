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
│  Channel<DeviceMonitorRescanRequest> (single reader)                                 │
│             │                                                                        │
│             ▼                                                                        │
│  Event coalescing loop                                                               │
│    ├─ initial RescanCoreAsync() at monitor startup                                   │
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
YubiKeyDeviceMonitorService ──TryWrite──► Channel<DeviceMonitorRescanRequest>
Single reader ──drain queued requests──► 200ms quiet period (re-arms on new hints)
Single reader ──cap reached or quiet period elapsed──► RescanCoreAsync()
RescanCoreAsync() ──FindAllAsync snapshot──► YubiKeyDeviceRepository.UpdateCache()
YubiKeyDeviceRepository ──diff──► DeviceChanges (DeviceAction.Added/Removed)
Application subscription ◄──DeviceEvent── YubiKeyManager.DeviceChanges
```

---

## Key Improvements

| Aspect | Before (Polling) | After (Event-Driven) |
|--------|------------------|----------------------|
| **Detection Latency** | Up to 500ms | ~5ms (HID) / 1000ms max (SmartCard) |
| **CPU Usage (Idle)** | Constant polling | Near-zero (waiting on OS) |
| **SmartCard Thread** | Task.Run (pool thread) | Dedicated background thread |
| **HID Windows** | ❌ Not implemented | ✅ CM_Register_Notification |
| **HID macOS** | Full enumeration | ✅ IOHIDManager callbacks |
| **HID Linux** | udev_enumerate | ✅ udev_monitor + poll() |
| **Event Coalescing** | None | Single-reader queue + 200ms quiet period capped at `MaxCoalesceInterval` |
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
