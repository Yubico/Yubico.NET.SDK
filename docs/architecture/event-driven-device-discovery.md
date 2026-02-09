@arh# Event-Driven Device Discovery Architecture

## Before vs After

### BEFORE: Timer-Based Polling (500ms)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DeviceMonitorService                                 │
│                         (BackgroundService)                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│    ┌──────────────────┐                                                      │
│    │  PeriodicTimer   │◄─── 500ms interval                                   │
│    │    (polling)     │                                                      │
│    └────────┬─────────┘                                                      │
│             │                                                                │
│             ▼ Every 500ms                                                    │
│    ┌────────────────────────────────────────────────────────────┐            │
│    │              Task.Run() - Async-over-sync 😞               │            │
│    ├────────────────────┬───────────────────────────────────────┤            │
│    │                    │                                       │            │
│    ▼                    ▼                                       │            │
│ ┌─────────────────┐  ┌─────────────────┐                        │            │
│ │ FindPcscDevices │  │  FindHidDevices │                        │            │
│ │                 │  │                 │                        │            │
│ │ SCardGetStatus  │  │ Full platform   │                        │            │
│ │ Change(0)       │  │ enumeration     │                        │            │
│ │ ▲               │  │ every cycle     │                        │            │
│ │ │ timeout=0     │  │                 │                        │            │
│ │ │ (immediate)   │  │ • udev_enum     │                        │            │
│ │ └───────────────┤  │ • IOHIDManager  │                        │            │
│ └─────────────────┘  │   CopyDevices   │                        │            │
│                      │ • Windows: N/A  │                        │            │
│                      └─────────────────┘                        │            │
│    └────────────────────────────────────────────────────────────┘            │
│             │                                                                │
│             ▼                                                                │
│    ┌─────────────────┐                                                       │
│    │  DeviceChannel  │◄─── Push all devices found                            │
│    │   (Channel<T>)  │                                                       │
│    └────────┬────────┘                                                       │
│             │                                                                │
└─────────────┼────────────────────────────────────────────────────────────────┘
              │
              ▼
     ┌─────────────────────┐
     │ DeviceListenerService│
     │  (consumes channel)  │
     └──────────┬──────────┘
                │
                ▼
     ┌─────────────────────┐
     │DeviceRepositoryCached│
     │   (IObservable<>)    │
     └──────────┬──────────┘
                │
                ▼
         Application
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
│                              DeviceMonitorService                                    │
│                              (BackgroundService)                                     │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌──────────────────────────────────────────────────────────────────────────────┐   │
│  │                         Platform Event Listeners                              │   │
│  │                         (Dedicated Background Threads)                        │   │
│  ├──────────────────────────┬──────────────────────────────────────────────────┤   │
│  │                          │                                                   │   │
│  │  ┌────────────────────┐  │  ┌─────────────────────────────────────────────┐ │   │
│  │  │DesktopSmartCard    │  │  │            HidDeviceListener                │ │   │
│  │  │DeviceListener      │  │  │            (abstract base)                  │ │   │
│  │  │                    │  │  │                                             │ │   │
│  │  │ ISmartCardDevice   │  │  │  ┌─────────────┬─────────────┬───────────┐ │ │   │
│  │  │ Listener (interface)│  │  │  │  Windows   │   macOS     │   Linux   │ │ │   │
│  │  │                    │  │  │  │            │             │           │ │ │   │
│  │  │ SCardGetStatus     │  │  │  │ CM_Register│ IOHIDManager│ udev_     │ │ │   │
│  │  │ Change(1000ms)     │  │  │  │ Notification│ + CFRunLoop │ monitor   │ │ │   │
│  │  │      │             │  │  │  │   ▲        │   ▲         │ + poll()  │ │ │   │
│  │  │      │ Blocks up   │  │  │  │   │callback│   │callback │   ▲       │ │ │   │
│  │  │      │ to 1000ms   │  │  │  │   │        │   │         │   │100ms  │ │ │   │
│  │  │      ▼             │  │  │  └───┼────────┴───┼─────────┴───┼───────┘ │ │   │
│  │  │ Checks _isListening│  │  │      │            │             │         │ │   │
│  │  │ flag, loops        │  │  │      │ OS notifies│ OS notifies │ fd ready│ │   │
│  │  └────────┬───────────┘  │  └──────┴────────────┴─────────────┴─────────┘ │   │
│  │           │              │                        │                       │   │
│  └───────────┼──────────────┴────────────────────────┼───────────────────────┘   │
│              │                                       │                           │
│              │  Arrived/Removed                      │  Arrived/Removed          │
│              │  events                               │  events                   │
│              ▼                                       ▼                           │
│         ┌─────────────────────────────────────────────────┐                      │
│         │              SignalEvent()                       │                      │
│         │         _eventSemaphore.Release()                │                      │
│         └───────────────────────┬─────────────────────────┘                      │
│                                 │                                                │
│                                 ▼                                                │
│         ┌─────────────────────────────────────────────────┐                      │
│         │           Event Coalescing Loop                  │                      │
│         │                                                  │                      │
│         │  await _eventSemaphore.WaitAsync()               │                      │
│         │  await Task.Delay(200ms) ◄── Coalesce rapid      │                      │
│         │  drain remaining signals    events               │                      │
│         │  PerformDeviceScan()                             │                      │
│         └───────────────────────┬─────────────────────────┘                      │
│                                 │                                                │
│                                 ▼                                                │
│    ┌────────────────────────────────────────────────────────────┐                │
│    │              Enumeration (only after event)                │                │
│    ├────────────────────┬───────────────────────────────────────┤                │
│    │                    │                                       │                │
│    ▼                    ▼                                       │                │
│ ┌─────────────────┐  ┌─────────────────┐                        │                │
│ │ FindPcscDevices │  │  FindHidDevices │  ◄── Only runs when    │                │
│ │   (snapshot)    │  │   (snapshot)    │      event received    │                │
│ └─────────────────┘  └─────────────────┘                        │                │
│    └────────────────────────────────────────────────────────────┘                │
│                                 │                                                │
│                                 ▼                                                │
│                     ┌─────────────────┐                                          │
│                     │  DeviceChannel  │                                          │
│                     │   (Channel<T>)  │                                          │
│                     └────────┬────────┘                                          │
│                              │                                                   │
└──────────────────────────────┼───────────────────────────────────────────────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ DeviceListenerService│
                    │  (consumes channel)  │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │DeviceRepositoryCached│
                    │   (IObservable<>)    │
                    └──────────┬──────────┘
                               │
                               ▼
                        Application
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
│ + Arrived event         │         │ + Arrived event         │
│ + Removed event         │         │ + Removed event         │
│ + Status { get; }       │         │ + Status { get; set; }  │
└────────────┬────────────┘         │ # OnArrived(device)     │
             │                      │ # OnRemoved(device)     │
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
│ Callback from OS │  │ CFRunLoop        │  │ poll(fd, 100ms)  │
│ on device change │  │ RunInMode(100ms) │  │     │            │
│     │            │  │     │            │  │     ▼            │
│     ▼            │  │     ▼            │  │ udev_monitor_    │
│ OnArrived() or   │  │ Matching/Removal │  │ receive_device   │
│ OnRemoved()      │  │ callbacks fire   │  │     │            │
│                  │  │     │            │  │     ▼            │
│ GCHandle pins    │  │     ▼            │  │ Check action:    │
│ callback delegate│  │ OnArrived() or   │  │ add → OnArrived  │
│                  │  │ OnRemoved()      │  │ remove→OnRemoved │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

---

## Event Flow Sequence

```
         YubiKey              OS                  Listener            Service           App
           │                  │                      │                   │               │
           │ Insert device    │                      │                   │               │
           ├─────────────────►│                      │                   │               │
           │                  │                      │                   │               │
           │                  │ Device notification  │                   │               │
           │                  ├─────────────────────►│                   │               │
           │                  │                      │                   │               │
           │                  │                      │ Arrived event     │               │
           │                  │                      ├──────────────────►│               │
           │                  │                      │                   │               │
           │                  │                      │                   │ SignalEvent() │
           │                  │                      │                   │ semaphore++   │
           │                  │                      │                   │               │
           │                  │                      │                   │◄──────────────┤
           │                  │                      │                   │ Wait 200ms    │
           │                  │                      │                   │ (coalesce)    │
           │                  │                      │                   │               │
           │                  │                      │                   │ Drain sema    │
           │                  │                      │                   │               │
           │                  │                      │                   │ FindAll()     │
           │                  │                      │                   ├───────────────┤
           │                  │                      │                   │               │
           │                  │                      │                   │ DeviceChannel │
           │                  │                      │                   │ .Write()      │
           │                  │                      │                   ├──────────────►│
           │                  │                      │                   │               │
           │                  │                      │                   │               │ DeviceEvent
           │                  │                      │                   │               │ (Arrived)
           │                  │                      │                   │               ├──────────►
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
| **Event Coalescing** | None | 200ms debounce |
| **Cancellation** | Unreliable | Responsive (100-1000ms) |

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

HID: 100ms poll/CFRunLoop timeout for same reason
```

---

## Files Created/Modified

```
Yubico.YubiKit.Core/
├── src/
│   ├── DeviceMonitorService.cs              ◄── MODIFIED: Event-driven loop
│   ├── SmartCard/
│   │   ├── ISmartCardDeviceListener.cs      ◄── NEW: Interface
│   │   └── DesktopSmartCardDeviceListener.cs◄── NEW: Implementation
│   ├── Hid/
│   │   ├── HidDeviceListener.cs             ◄── NEW: Abstract base
│   │   ├── NullDevice.cs                    ◄── NEW: Placeholder for removals
│   │   ├── Windows/
│   │   │   └── WindowsHidDeviceListener.cs  ◄── NEW
│   │   ├── MacOS/
│   │   │   └── MacOSHidDeviceListener.cs    ◄── NEW
│   │   └── Linux/
│   │       └── LinuxHidDeviceListener.cs    ◄── NEW
│   ├── PlatformInterop/
│   │   └── Linux/Libc/
│   │       └── Libc.Interop.cs              ◄── MODIFIED: Added poll()
│   └── YubiKey/
│       └── YubiKeyManagerOptions.cs         ◄── MODIFIED: EventCoalescingDelay
```
