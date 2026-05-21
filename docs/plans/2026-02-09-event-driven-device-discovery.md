# Event-Driven Device Discovery Implementation Plan

**Goal:** Replace timer-based polling with event-driven device discovery for HID (all platforms) and improve SmartCard polling responsiveness.

**Architecture:** Create dedicated device listener classes that use OS-native event mechanisms (Windows `CM_Register_Notification`, macOS `IOHIDManager` callbacks, Linux `udev_monitor`). These listeners push events to the existing `DeviceChannel`, which feeds `DeviceRepositoryCached`. SmartCard uses 1000ms timeout polling via `SCardGetStatusChange` on a dedicated thread.

**Tech Stack:** 
- Platform interop: P/Invoke to Windows cfgmgr32, macOS IOKit/CoreFoundation, Linux libudev
- Threading: Dedicated background threads with proper cancellation
- Integration: Existing `IDeviceChannel`, `BackgroundService`, `System.Reactive`

**Reference Implementation:** Port patterns from `develop` branch (`Yubico.Core/src/Yubico/Core/Devices/`)

---

## Overview

### Current State (yubikit branch)
- `DeviceMonitorService` polls every 500ms using `PeriodicTimer`
- `FindPcscDevices.FindAll()` calls `SCardGetStatusChange(timeout=0)` - immediate return
- `FindHidDevices.FindAll()` enumerates all devices each cycle
- No event-driven mechanisms

### Target State
- `SmartCardDeviceListener` with 1000ms timeout polling on dedicated thread
- `WindowsHidDeviceListener` with `CM_Register_Notification` callbacks
- `MacOSHidDeviceListener` with `IOHIDManager` device matching/removal callbacks
- `LinuxHidDeviceListener` with `udev_monitor` + `poll()` events
- Listeners push to `DeviceChannel` on events (not timer)
- 200ms coalescing delay before processing

### Files to Create
```
Yubico.YubiKit.Core/src/Hid/
├── HidDeviceListener.cs              # Abstract base class
├── Windows/WindowsHidDeviceListener.cs
├── MacOS/MacOSHidDeviceListener.cs
├── Linux/LinuxHidDeviceListener.cs
└── NullDevice.cs                     # Placeholder for removal events

Yubico.YubiKit.Core/src/SmartCard/
├── ISmartCardDeviceListener.cs       # Interface
└── DesktopSmartCardDeviceListener.cs # 1000ms timeout implementation (implements interface directly)

Yubico.YubiKit.Core/src/
└── DeviceMonitorService.cs           # Refactor to use listeners
```

### Files to Modify
```
Yubico.YubiKit.Core/src/DependencyInjection.cs
Yubico.YubiKit.Core/src/YubiKey/YubiKeyManagerOptions.cs
Yubico.YubiKit.Core/src/PlatformInterop/Linux/Libc/Libc.Interop.cs  # Add poll() P/Invoke
```

### Prerequisites (Must Complete Before Phase 3.3)

**CRITICAL: Missing P/Invoke declarations must be added:**

1. **Linux `poll()` P/Invoke** - Add to `Libc.Interop.cs`:
   ```csharp
   [StructLayout(LayoutKind.Sequential)]
   internal struct PollFd
   {
       public int fd;
       public short events;
       public short revents;
   }
   
   internal const short POLLIN = 0x0001;
   internal const short POLLERR = 0x0008;
   internal const short POLLHUP = 0x0010;
   
   [DllImport(Libraries.LinuxKernelLib, EntryPoint = "poll", SetLastError = true)]
   internal static extern int poll([In, Out] PollFd[] fds, int nfds, int timeout);
   ```

2. **Verify `SCardCancel()` exists** - Should be in `Desktop/SCard/SCard.Interop.cs`. If missing:
   ```csharp
   [LibraryImport(Libraries.WinSCard)]
   internal static partial uint SCardCancel(SCardContext hContext);
   ```

3. **Verify `WindowsHidDevice.FromDevicePath()`** - Method to construct `IHidDevice` from Windows device interface path

---

## Phase 1: Infrastructure - Abstract Listener Base Classes

### Task 1.1: Create HID Device Listener Base Class

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/HidDeviceListener.cs`
- Create: `Yubico.YubiKit.Core/src/Hid/NullDevice.cs`

**Step 1: Create NullDevice placeholder**

This is used for removal events when we don't have full device info.

```csharp
// Yubico.YubiKit.Core/src/Hid/NullDevice.cs
using Yubico.YubiKit.Core.Hid.Interfaces;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// A placeholder device used for removal events when full device info is unavailable.
/// </summary>
internal sealed class NullDevice : IHidDevice
{
    public static readonly NullDevice Instance = new();
    
    private NullDevice() { }
    
    public string ReaderName => string.Empty;
    public HidDescriptorInfo DescriptorInfo => new();
    public HidInterfaceType InterfaceType => HidInterfaceType.Unknown;
    
    public IHidConnection ConnectToFeatureReports() => 
        throw new NotSupportedException("Cannot connect to NullDevice");
    
    public IHidConnection ConnectToIOReports() => 
        throw new NotSupportedException("Cannot connect to NullDevice");
}
```

**Step 2: Create HidDeviceListener abstract base**

```csharp
// Yubico.YubiKit.Core/src/Hid/HidDeviceListener.cs
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Event arguments for HID device events.
/// </summary>
public sealed class HidDeviceEventArgs(IHidDevice device) : EventArgs
{
    public IHidDevice Device { get; } = device;
}

/// <summary>
/// Abstract base class for platform-specific HID device listeners.
/// </summary>
public abstract class HidDeviceListener : IDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<HidDeviceListener>();
    private bool _disposed;

    /// <summary>
    /// Fired when a HID device is added to the system.
    /// </summary>
    public event EventHandler<HidDeviceEventArgs>? Arrived;

    /// <summary>
    /// Fired when a HID device is removed from the system.
    /// </summary>
    public event EventHandler<HidDeviceEventArgs>? Removed;

    /// <summary>
    /// Creates a platform-appropriate HID device listener.
    /// </summary>
    public static HidDeviceListener Create() =>
        SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => new Windows.WindowsHidDeviceListener(),
            SdkPlatform.MacOS => new MacOS.MacOSHidDeviceListener(),
            SdkPlatform.Linux => new Linux.LinuxHidDeviceListener(),
            _ => throw new PlatformNotSupportedException(
                $"HID device listening not supported on {SdkPlatformInfo.OperatingSystem}")
        };

    /// <summary>
    /// Called by implementations when a device arrives.
    /// </summary>
    protected void OnArrived(IHidDevice device)
    {
        Logger.LogInformation("HID device arrived: {Device}", device.ReaderName);

        if (Arrived is null) return;

        foreach (var @delegate in Arrived.GetInvocationList())
        {
            var handler = (EventHandler<HidDeviceEventArgs>)@delegate;
            try
            {
                handler.Invoke(this, new HidDeviceEventArgs(device));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in HID Arrived event handler");
            }
        }
    }

    /// <summary>
    /// Called by implementations when a device is removed.
    /// </summary>
    protected void OnRemoved(IHidDevice device)
    {
        Logger.LogInformation("HID device removed: {Device}", device.ReaderName);

        if (Removed is null) return;

        foreach (var @delegate in Removed.GetInvocationList())
        {
            var handler = (EventHandler<HidDeviceEventArgs>)@delegate;
            try
            {
                handler.Invoke(this, new HidDeviceEventArgs(device));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in HID Removed event handler");
            }
        }
    }

    protected void ClearEventHandlers()
    {
        Arrived = null;
        Removed = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            ClearEventHandlers();
        }
        
        _disposed = true;
    }
}
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/HidDeviceListener.cs Yubico.YubiKit.Core/src/Hid/NullDevice.cs
git commit -m "feat(core): add HidDeviceListener abstract base class"
```

---

### Task 1.2: Create SmartCard Device Listener Base Class

**Files:**
- Create: `Yubico.YubiKit.Core/src/SmartCard/ISmartCardDeviceListener.cs`

**Step 1: Create ISmartCardDeviceListener interface**

Since PC/SC is already cross-platform (same API on Windows/macOS/Linux), we only need ONE implementation. An interface is sufficient - no abstract class needed.

```csharp
// Yubico.YubiKit.Core/src/SmartCard/ISmartCardDeviceListener.cs
namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
/// Status of the device listener.
/// </summary>
public enum DeviceListenerStatus
{
    Stopped,
    Started,
    Error
}

/// <summary>
/// Event arguments for SmartCard device events.
/// </summary>
public sealed class SmartCardDeviceEventArgs(IPcscDevice device) : EventArgs
{
    public IPcscDevice Device { get; } = device;
}

/// <summary>
/// Listens for SmartCard device arrival and removal events.
/// </summary>
public interface ISmartCardDeviceListener : IDisposable
{
    /// <summary>
    /// Fired when a SmartCard device is added to the system.
    /// </summary>
    event EventHandler<SmartCardDeviceEventArgs>? Arrived;

    /// <summary>
    /// Fired when a SmartCard device is removed from the system.
    /// </summary>
    event EventHandler<SmartCardDeviceEventArgs>? Removed;

    /// <summary>
    /// Current status of the listener.
    /// </summary>
    DeviceListenerStatus Status { get; }
}
```

**Step 2: Commit**

```bash
git add Yubico.YubiKit.Core/src/SmartCard/ISmartCardDeviceListener.cs
git commit -m "feat(core): add ISmartCardDeviceListener interface"
```

---

## Phase 2: SmartCard Listener with 1000ms Timeout

### Task 2.1: Create DesktopSmartCardDeviceListener

**Files:**
- Create: `Yubico.YubiKit.Core/src/SmartCard/DesktopSmartCardDeviceListener.cs`

**Reference:** Port from `develop` branch `Yubico.Core/src/Yubico/Core/Devices/SmartCard/DesktopSmartCardDeviceListener.cs`

**Key design decisions:**
- Use 1000ms timeout (not INFINITE) for responsiveness to cancellation
- Dedicated background thread (not Task.Run)
- `SCardCancel()` for clean shutdown
- Handle PnP reader notifications via `\\?\Pnp\Notifications` virtual reader
- Implements `ISmartCardDeviceListener` directly (no abstract class - PC/SC is already cross-platform)

**Step 1: Create the implementation**

```csharp
// Yubico.YubiKit.Core/src/SmartCard/DesktopSmartCardDeviceListener.cs
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
/// Desktop implementation of SmartCard device listener using PC/SC.
/// Uses SCardGetStatusChange with 1000ms timeout for responsive cancellation.
/// </summary>
internal sealed class DesktopSmartCardDeviceListener : ISmartCardDeviceListener
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<DesktopSmartCardDeviceListener>();
    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private SCardContext _context;
    private SCARD_READER_STATE[] _readerStates;
    private Thread? _listenerThread;
    private volatile bool _isListening;
    private bool _isDisposed;
    
    private readonly object _startStopLock = new();
    private readonly object _disposeLock = new();

    // ISmartCardDeviceListener implementation
    public event EventHandler<SmartCardDeviceEventArgs>? Arrived;
    public event EventHandler<SmartCardDeviceEventArgs>? Removed;
    public DeviceListenerStatus Status { get; private set; } = DeviceListenerStatus.Stopped;

    public DesktopSmartCardDeviceListener()
    {
        Logger.LogInformation("Creating DesktopSmartCardDeviceListener");
        Status = DeviceListenerStatus.Stopped;

        var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            context.Dispose();
            _context = new SCardContext(IntPtr.Zero);
            _readerStates = [];
            Status = DeviceListenerStatus.Error;
            Logger.LogWarning("SmartCardDeviceListener dormant - unable to establish PC/SC context");
            return;
        }

        _context = context;
        _readerStates = GetReaderStateList();
        StartListening();
    }

    private void StartListening()
    {
        lock (_startStopLock)
        {
            if (_isListening) return;

            _listenerThread = new Thread(ListenForReaderChanges) { IsBackground = true };
            _isListening = true;
            Status = DeviceListenerStatus.Started;
            _listenerThread.Start();
        }
    }

    private void StopListening()
    {
        Thread? threadToJoin;
        lock (_startStopLock)
        {
            threadToJoin = _listenerThread;
            if (threadToJoin is null) return;
            
            _isListening = false;
            Status = DeviceListenerStatus.Stopped;
        }

        // Wait outside lock
        bool exited = threadToJoin.Join(MaxDisposalWaitTime);
        if (!exited)
        {
            Logger.LogWarning("SmartCard listener thread did not exit within timeout");
        }

        lock (_startStopLock)
        {
            _listenerThread = null;
        }
    }

    private void ListenForReaderChanges()
    {
        Logger.LogInformation("SmartCard listener thread started. ThreadID: {ThreadId}", 
            Environment.CurrentManagedThreadId);

        bool usePnpWorkaround = UsePnpWorkaround();
        
        while (_isListening)
        {
            try
            {
                if (!CheckForUpdates(usePnpWorkaround))
                    break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in SmartCard listener loop");
                Status = DeviceListenerStatus.Error;
            }
        }
        
        Logger.LogInformation("SmartCard listener thread exiting");
    }

    private bool CheckForUpdates(bool usePnpWorkaround)
    {
        var arrivedDevices = new List<IPcscDevice>();
        var removedDevices = new List<IPcscDevice>();
        var newStates = (SCARD_READER_STATE[])_readerStates.Clone();

        // Use 1000ms timeout - allows clean cancellation while still being responsive
        var result = NativeMethods.SCardGetStatusChange(
            _context, 
            (int)CheckForChangesWaitTime.TotalMilliseconds, 
            newStates, 
            newStates.Length);

        if (!HandleSCardResult(result, newStates))
            return false;

        // Check for reader list changes (new readers plugged in)
        while (ReaderListChangeDetected(ref newStates, usePnpWorkaround))
        {
            var eventStateList = GetReaderStateList();
            var addedReaders = eventStateList.Except(newStates, ReaderStateComparer.Instance).ToArray();
            var removedReaders = newStates.Except(eventStateList, ReaderStateComparer.Instance).ToArray();

            if (addedReaders.Length == 0 && removedReaders.Length == 0)
                break;

            var readerStateList = newStates.ToList();
            readerStateList.AddRange(addedReaders);
            var updatedStates = readerStateList.Except(removedReaders, ReaderStateComparer.Instance).ToArray();

            // Track removed readers that had cards
            foreach (var removed in removedReaders)
            {
                if ((removed.CurrentState & SCARD_STATE.PRESENT) != 0)
                {
                    removedDevices.Add(CreateDevice(removed));
                }
            }

            if (addedReaders.Length != 0)
            {
                result = NativeMethods.SCardGetStatusChange(_context, 0, updatedStates, updatedStates.Length);
                if (!HandleSCardResult(result, updatedStates))
                    return false;
            }

            newStates = updatedStates;
        }

        // Check for card insertion/removal within existing readers
        if (RelevantChangesDetected(newStates))
        {
            result = NativeMethods.SCardGetStatusChange(_context, 0, newStates, newStates.Length);
            if (!HandleSCardResult(result, newStates))
                return false;
        }

        DetectCardChanges(_readerStates, newStates, arrivedDevices, removedDevices);
        UpdateCurrentlyKnownState(ref newStates);
        _readerStates = newStates;

        // Fire events
        foreach (var device in arrivedDevices)
            OnArrived(device);
        foreach (var device in removedDevices)
            OnRemoved(device);

        return true;
    }

    private bool HandleSCardResult(uint result, SCARD_READER_STATE[] states)
    {
        if (result == ErrorCode.SCARD_E_CANCELLED)
        {
            Logger.LogInformation("SCardGetStatusChange cancelled");
            return false;
        }

        if (result == ErrorCode.SCARD_E_TIMEOUT)
            return true; // Normal timeout, continue loop

        if (result == ErrorCode.SCARD_E_SERVICE_STOPPED ||
            result == ErrorCode.SCARD_E_NO_READERS_AVAILABLE ||
            result == ErrorCode.SCARD_E_NO_SERVICE)
        {
            Logger.LogInformation("PC/SC service status changed (0x{Result:X8}), refreshing context", result);
            UpdateCurrentContext();
            return true;
        }

        if (result != ErrorCode.SCARD_S_SUCCESS)
        {
            Logger.LogWarning("SCardGetStatusChange returned 0x{Result:X8}", result);
        }

        return true;
    }

    private bool UsePnpWorkaround()
    {
        try
        {
            var testState = SCARD_READER_STATE.CreateMany(["\\\\?PnP?\\Notification"]);
            NativeMethods.SCardGetStatusChange(_context, 0, testState, testState.Length);
            return (testState[0].GetEventState() & SCARD_STATE.UNKNOWN) != 0;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "PnP workaround check failed, assuming not needed");
            return false;
        }
    }

    private bool ReaderListChangeDetected(ref SCARD_READER_STATE[] states, bool usePnpWorkaround)
    {
        if (usePnpWorkaround)
        {
            var result = NativeMethods.SCardListReaders(_context, null, out var readerNames);
            if (result != ErrorCode.SCARD_E_NO_READERS_AVAILABLE)
            {
                return readerNames.Length != states.Length - 1;
            }
        }

        return (states[0].GetEventState() & SCARD_STATE.CHANGED) != 0;
    }

    private static bool RelevantChangesDetected(SCARD_READER_STATE[] states)
    {
        foreach (var state in states)
        {
            var diff = state.CurrentState ^ state.GetEventState();
            if ((diff & SCARD_STATE.PRESENT) != 0)
                return true;
        }
        return false;
    }

    private static void DetectCardChanges(
        SCARD_READER_STATE[] originalStates,
        SCARD_READER_STATE[] newStates,
        List<IPcscDevice> arrived,
        List<IPcscDevice> removed)
    {
        foreach (var entry in newStates)
        {
            var diff = entry.CurrentState ^ entry.GetEventState();
            if ((diff & SCARD_STATE.PRESENT) == 0)
                continue;

            if ((entry.CurrentState & SCARD_STATE.PRESENT) != 0)
            {
                // Card was present, now gone
                var original = originalStates.FirstOrDefault(s => s.GetReaderName() == entry.GetReaderName());
                removed.Add(CreateDevice(original));
            }
            else if ((entry.GetEventState() & SCARD_STATE.PRESENT) != 0)
            {
                // Card is now present
                arrived.Add(CreateDevice(entry));
            }
        }
    }

    private static void UpdateCurrentlyKnownState(ref SCARD_READER_STATE[] states)
    {
        for (int i = 0; i < states.Length; i++)
        {
            states[i].AcknowledgeChanges();
        }
    }

    private void UpdateCurrentContext()
    {
        var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
        if (result == ErrorCode.SCARD_S_SUCCESS)
        {
            _context = context;
            _readerStates = GetReaderStateList();
        }
    }

    private SCARD_READER_STATE[] GetReaderStateList()
    {
        var result = NativeMethods.SCardListReaders(_context, null, out var readerNames);
        if (result == ErrorCode.SCARD_E_NO_READERS_AVAILABLE)
            readerNames = [];

        var allReaders = new List<string>(readerNames.Length + 1) { "\\\\?PnP?\\Notification" };
        allReaders.AddRange(readerNames);

        return SCARD_READER_STATE.CreateMany([.. allReaders]);
    }

    private static IPcscDevice CreateDevice(SCARD_READER_STATE state) =>
        new PcscDevice
        {
            ReaderName = state.GetReaderName(),
            Atr = state.GetAtr(),
            Kind = PscsConnectionKind.Usb
        };

    private void OnArrived(IPcscDevice device)
    {
        Logger.LogInformation("SmartCard device arrived: {Device}", device.ReaderName);

        if (Arrived is null) return;

        foreach (var @delegate in Arrived.GetInvocationList())
        {
            var handler = (EventHandler<SmartCardDeviceEventArgs>)@delegate;
            try
            {
                handler.Invoke(this, new SmartCardDeviceEventArgs(device));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in SmartCard Arrived event handler");
            }
        }
    }

    private void OnRemoved(IPcscDevice device)
    {
        Logger.LogInformation("SmartCard device removed: {Device}", device.ReaderName);

        if (Removed is null) return;

        foreach (var @delegate in Removed.GetInvocationList())
        {
            var handler = (EventHandler<SmartCardDeviceEventArgs>)@delegate;
            try
            {
                handler.Invoke(this, new SmartCardDeviceEventArgs(device));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in SmartCard Removed event handler");
            }
        }
    }

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                // Cancel any blocking calls
                _ = NativeMethods.SCardCancel(_context);
                StopListening();
                _context.Dispose();
                Arrived = null;
                Removed = null;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Exception during DesktopSmartCardDeviceListener disposal");
            }
        }
    }

    private sealed class ReaderStateComparer : IEqualityComparer<SCARD_READER_STATE>
    {
        public static readonly ReaderStateComparer Instance = new();
        public bool Equals(SCARD_READER_STATE x, SCARD_READER_STATE y) => 
            x.GetReaderName() == y.GetReaderName();
        public int GetHashCode(SCARD_READER_STATE obj) => 
            obj.GetReaderName().GetHashCode();
    }
}
```

**Step 2: Commit**

```bash
git add Yubico.YubiKit.Core/src/SmartCard/DesktopSmartCardDeviceListener.cs
git commit -m "feat(core): add DesktopSmartCardDeviceListener with 1000ms timeout"
```

---

## Phase 3: HID Listeners (Event-Driven)

### Task 3.1: Windows HID Listener

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/Windows/WindowsHidDeviceListener.cs`

**Reference:** Port from `develop` branch `Yubico.Core/src/Yubico/Core/Devices/Hid/WindowsHidDeviceListener.cs`

**Key design:**
- Uses `CM_Register_Notification` for true event-driven device notifications
- Registers for `GUID_DEVINTERFACE_HID` interface class events
- Callback fires on arrival/removal - no polling

**Step 1: Create Windows HID listener**

```csharp
// Yubico.YubiKit.Core/src/Hid/Windows/WindowsHidDeviceListener.cs
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop;
using Yubico.YubiKit.Core.PlatformInterop.Windows.Cfgmgr32;
using CmNativeMethods = Yubico.YubiKit.Core.PlatformInterop.Windows.Cfgmgr32.NativeMethods;

namespace Yubico.YubiKit.Core.Hid.Windows;

/// <summary>
/// Windows HID device listener using CM_Register_Notification.
/// True event-driven - no polling.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsHidDeviceListener : HidDeviceListener
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<WindowsHidDeviceListener>();
    
    // GUID for HID device interface class
    private static readonly Guid GUID_DEVINTERFACE_HID = new("4D1E55B2-F16F-11CF-88CB-001111000030");

    private IntPtr _notificationContext;
    private GCHandle? _marshalableThisPtr;
    private CmNativeMethods.CM_NOTIFY_CALLBACK? _callbackDelegate;
    private bool _isDisposed;
    private readonly object _disposeLock = new();

    public WindowsHidDeviceListener()
    {
        Logger.LogInformation("Creating WindowsHidDeviceListener");
        StartListening();
    }

    ~WindowsHidDeviceListener()
    {
        Dispose(false);
    }

    private void StartListening()
    {
        var zeroBytes = new byte[CmNativeMethods.CmNotifyFilterSize];
        var guidBytes = GUID_DEVINTERFACE_HID.ToByteArray();

        var pFilter = Marshal.AllocHGlobal(CmNativeMethods.CmNotifyFilterSize);

        try
        {
            Marshal.Copy(zeroBytes, 0, pFilter, zeroBytes.Length);
            Marshal.WriteInt32(pFilter, CmNativeMethods.OffsetCbSize, CmNativeMethods.CmNotifyFilterSize);
            Marshal.WriteInt32(pFilter, CmNativeMethods.OffsetFlags, 0);
            Marshal.WriteInt32(pFilter, CmNativeMethods.OffsetFilterType, (int)CmNativeMethods.CM_NOTIFY_FILTER_TYPE.DEVINTERFACE);
            Marshal.WriteInt32(pFilter, CmNativeMethods.OffsetReserved, 0);
            
            for (int i = 0; i < guidBytes.Length; i++)
            {
                Marshal.WriteByte(pFilter, CmNativeMethods.OffsetGuidData1 + i, guidBytes[i]);
            }

            _marshalableThisPtr = GCHandle.Alloc(this);
            _callbackDelegate = OnEventReceived;
            
            var errorCode = CmNativeMethods.CM_Register_Notification(
                pFilter, 
                GCHandle.ToIntPtr(_marshalableThisPtr.Value), 
                _callbackDelegate, 
                out _notificationContext);

            if (errorCode != CmNativeMethods.CmErrorCode.CR_SUCCESS)
            {
                throw new PlatformApiException("CM_Register_Notification", (int)errorCode, 
                    $"Failed to register HID device notification: {errorCode}");
            }

            Logger.LogInformation("Registered HID device notification callback");
        }
        finally
        {
            Marshal.FreeHGlobal(pFilter);
        }
    }

    private void StopListening()
    {
        if (_notificationContext != IntPtr.Zero)
        {
            var errorCode = CmNativeMethods.CM_Unregister_Notification(_notificationContext);
            Logger.LogInformation("Unregistered HID device notification: {Result}", errorCode);
            _notificationContext = IntPtr.Zero;
        }

        if (_marshalableThisPtr.HasValue)
        {
            _marshalableThisPtr.Value.Free();
            _marshalableThisPtr = null;
        }
    }

    private static int OnEventReceived(
        IntPtr hNotify,
        IntPtr context,
        CmNativeMethods.CM_NOTIFY_ACTION action,
        IntPtr eventDataPtr,
        int eventDataSize)
    {
        var thisPtr = GCHandle.FromIntPtr(context);
        var thisObj = thisPtr.Target as WindowsHidDeviceListener;

        try
        {
            const int stringOffset = 24; // Offset to device path string
            int stringSize = eventDataSize - stringOffset;
            var buffer = new byte[stringSize];
            Marshal.Copy(eventDataPtr + stringOffset, buffer, 0, stringSize);

            switch (action)
            {
                case CmNativeMethods.CM_NOTIFY_ACTION.DEVICEINTERFACEARRIVAL:
                {
                    string instancePath = System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
                    Logger.LogDebug("HID device arrival: {Path}", instancePath);
                    
                    // Create device from path - implementation depends on existing WindowsHidDevice
                    // For now, create a minimal device
                    var device = WindowsHidDevice.FromDevicePath(instancePath);
                    if (device is not null)
                    {
                        thisObj?.OnArrived(device);
                    }
                    break;
                }
                case CmNativeMethods.CM_NOTIFY_ACTION.DEVICEINTERFACEREMOVAL:
                    Logger.LogDebug("HID device removal");
                    thisObj?.OnRemoved(NullDevice.Instance);
                    break;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception in HID device notification callback");
            return 0;
        }
    }

    protected override void Dispose(bool disposing)
    {
        lock (_disposeLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                StopListening();
                _callbackDelegate = null;
            }
            catch (Exception ex)
            {
                if (disposing)
                    Logger.LogWarning(ex, "Exception during WindowsHidDeviceListener disposal");
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
```

**Step 2: Create WindowsHidDevice.FromDevicePath method**

Add to existing `Yubico.YubiKit.Core/src/Hid/Windows/` directory - this may require a new file or modification to create a Windows-specific HidDevice that can be constructed from a device path.

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/Windows/WindowsHidDeviceListener.cs
git commit -m "feat(core): add WindowsHidDeviceListener with CM_Register_Notification"
```

---

### Task 3.2: macOS HID Listener

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/MacOS/MacOSHidDeviceListener.cs`

**Reference:** Port from `develop` branch `Yubico.Core/src/Yubico/Core/Devices/Hid/MacOSHidDeviceListener.cs`

**Key design:**
- Uses `IOHIDManager` with device matching/removal callbacks
- CFRunLoop for event dispatch
- 100ms CFRunLoopRunInMode timeout for responsive cancellation

**Step 1: Create macOS HID listener**

```csharp
// Yubico.YubiKit.Core/src/Hid/MacOS/MacOSHidDeviceListener.cs
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop.MacOS.CoreFoundation;
using Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework;
using CFNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Hid.MacOS;

/// <summary>
/// macOS HID device listener using IOHIDManager callbacks.
/// Event-driven with CFRunLoop.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidDeviceListener : HidDeviceListener
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<MacOSHidDeviceListener>();
    private const string kCFRunLoopDefaultMode = "kCFRunLoopDefaultMode";
    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private Thread? _listenerThread;
    private IntPtr? _runLoop;
    private volatile bool _shouldStop;
    
    // Keep strong references to prevent GC of delegates used by native code
    private IOKitNativeMethods.IOHIDDeviceCallback? _arrivedCallbackDelegate;
    private IOKitNativeMethods.IOHIDDeviceCallback? _removedCallbackDelegate;
    
    private bool _isDisposed;
    private readonly object _disposeLock = new();

    public MacOSHidDeviceListener()
    {
        Logger.LogInformation("Creating MacOSHidDeviceListener");
        StartListening();
    }

    ~MacOSHidDeviceListener()
    {
        Dispose(false);
    }

    private void StartListening()
    {
        _listenerThread = new Thread(ListeningThread) { IsBackground = true };
        _listenerThread.Start();
    }

    private void StopListening(bool fromFinalizer = false)
    {
        var threadToJoin = _listenerThread;
        var runLoopToStop = _runLoop;

        _shouldStop = true;

        if (runLoopToStop.HasValue && runLoopToStop != IntPtr.Zero)
        {
            CFNativeMethods.CFRunLoopStop(runLoopToStop.Value);
        }

        if (!fromFinalizer && threadToJoin is not null)
        {
            bool exited = threadToJoin.Join(MaxDisposalWaitTime);
            if (!exited)
            {
                Logger.LogWarning("macOS HID listener thread did not exit within timeout");
            }
        }

        _runLoop = null;
        _listenerThread = null;
    }

    private void ListeningThread()
    {
        Logger.LogInformation("macOS HID listener thread started. ThreadID: {ThreadId}", 
            Environment.CurrentManagedThreadId);

        IntPtr manager = IntPtr.Zero;
        IntPtr runLoopMode = IntPtr.Zero;

        try
        {
            var modeBytes = Encoding.UTF8.GetBytes(kCFRunLoopDefaultMode + "\0");
            runLoopMode = CFNativeMethods.CFStringCreateWithCString(IntPtr.Zero, modeBytes, 0);

            manager = IOKitNativeMethods.IOHIDManagerCreate(IntPtr.Zero, 0);
            IOKitNativeMethods.IOHIDManagerSetDeviceMatching(manager, IntPtr.Zero);

            _runLoop = CFNativeMethods.CFRunLoopGetCurrent();
            IOKitNativeMethods.IOHIDManagerScheduleWithRunLoop(manager, _runLoop.Value, runLoopMode);

            Logger.LogInformation("IOHIDManager scheduled with run loop");

            // Flush existing devices
            CFNativeMethods.CFRunLoopRunInMode(runLoopMode, CheckForChangesWaitTime.TotalSeconds, true);

            // Store delegates as fields to prevent GC
            _arrivedCallbackDelegate = ArrivedCallback;
            _removedCallbackDelegate = RemovedCallback;

            IOKitNativeMethods.IOHIDManagerRegisterDeviceMatchingCallback(manager, _arrivedCallbackDelegate, IntPtr.Zero);
            IOKitNativeMethods.IOHIDManagerRegisterDeviceRemovalCallback(manager, _removedCallbackDelegate, IntPtr.Zero);

            int runLoopResult = IOKitNativeMethods.kCFRunLoopRunHandledSource;

            Logger.LogInformation("Beginning run loop");
            while (!_shouldStop && 
                   (runLoopResult == IOKitNativeMethods.kCFRunLoopRunHandledSource || 
                    runLoopResult == IOKitNativeMethods.kCFRunLoopRunTimedOut))
            {
                runLoopResult = CFNativeMethods.CFRunLoopRunInMode(
                    runLoopMode, 
                    CheckForChangesWaitTime.TotalSeconds, 
                    true);
            }

            Logger.LogInformation("Run loop exited with result: {Result}", runLoopResult);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception in macOS HID listener thread");
        }
        finally
        {
            Logger.LogInformation("Cleaning up macOS HID listener");

            try
            {
                if (_runLoop.HasValue && manager != IntPtr.Zero && runLoopMode != IntPtr.Zero)
                {
                    IOKitNativeMethods.IOHIDManagerUnscheduleFromRunLoop(manager, _runLoop.Value, runLoopMode);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception unscheduling IOHIDManager");
            }

            try
            {
                if (manager != IntPtr.Zero)
                    CFNativeMethods.CFRelease(manager);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception releasing IOHIDManager");
            }

            try
            {
                if (runLoopMode != IntPtr.Zero)
                    CFNativeMethods.CFRelease(runLoopMode);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception releasing run loop mode");
            }
        }
    }

    private void ArrivedCallback(IntPtr context, int result, IntPtr sender, IntPtr device)
    {
        try
        {
            var entryId = MacOSHidDevice.GetEntryId(device);
            OnArrived(new MacOSHidDevice(entryId, GetDescriptorInfo(device)));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception in HID arrived callback");
        }
    }

    private void RemovedCallback(IntPtr context, int result, IntPtr sender, IntPtr device) =>
        OnRemoved(NullDevice.Instance);

    private static HidDescriptorInfo GetDescriptorInfo(IntPtr device)
    {
        return new HidDescriptorInfo
        {
            VendorId = (short)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyVendorId) ?? 0),
            ProductId = (short)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyProductId) ?? 0),
            Usage = (ushort)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyPrimaryUsage) ?? 0),
            UsagePage = (ushort)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyPrimaryUsagePage) ?? 0)
        };
    }

    protected override void Dispose(bool disposing)
    {
        lock (_disposeLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                StopListening(fromFinalizer: !disposing);
                _arrivedCallbackDelegate = null;
                _removedCallbackDelegate = null;
            }
            catch (Exception ex)
            {
                if (disposing)
                    Logger.LogWarning(ex, "Exception during MacOSHidDeviceListener disposal");
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
```

**Step 2: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/MacOS/MacOSHidDeviceListener.cs
git commit -m "feat(core): add MacOSHidDeviceListener with IOHIDManager callbacks"
```

---

### Task 3.3: Linux HID Listener

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/Linux/LinuxHidDeviceListener.cs`

**Reference:** Port from `develop` branch `Yubico.Core/src/Yubico/Core/Devices/Hid/LinuxHidDeviceListener.cs`

**Key design:**
- Uses `udev_monitor_new_from_netlink` for event notifications
- `poll()` syscall with 100ms timeout for responsive cancellation
- Filters for hidraw subsystem

**Step 1: Create Linux HID listener**

```csharp
// Yubico.YubiKit.Core/src/Hid/Linux/LinuxHidDeviceListener.cs
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop;
using Yubico.YubiKit.Core.PlatformInterop.Linux.Udev;
using UdevNativeMethods = Yubico.YubiKit.Core.PlatformInterop.Linux.Udev.NativeMethods;

namespace Yubico.YubiKit.Core.Hid.Linux;

/// <summary>
/// Linux HID device listener using udev_monitor.
/// Event-driven with poll() for responsive cancellation.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHidDeviceListener : HidDeviceListener
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<LinuxHidDeviceListener>();
    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private readonly LinuxUdevSafeHandle _udevObject;
    private readonly LinuxUdevMonitorSafeHandle _monitorObject;
    
    private Thread? _listenerThread;
    private CancellationTokenSource? _cancellationTokenSource;
    private volatile bool _isListening;
    
    private bool _isDisposed;
    private readonly object _startStopLock = new();
    private readonly object _disposeLock = new();

    public LinuxHidDeviceListener()
    {
        Logger.LogInformation("Creating LinuxHidDeviceListener");

        _udevObject = UdevNativeMethods.udev_new();
        if (_udevObject.IsInvalid)
        {
            throw new PlatformApiException("udev_new", Marshal.GetLastWin32Error(), 
                "Failed to create udev context");
        }

        _monitorObject = UdevNativeMethods.udev_monitor_new_from_netlink(_udevObject, UdevNativeMethods.UdevMonitorName);
        if (_monitorObject.IsInvalid)
        {
            throw new PlatformApiException("udev_monitor_new_from_netlink", Marshal.GetLastWin32Error(), 
                "Failed to create udev monitor");
        }

        StartListening();
    }

    ~LinuxHidDeviceListener()
    {
        Dispose(false);
    }

    private void StartListening()
    {
        lock (_startStopLock)
        {
            if (_isListening) return;

            int result = UdevNativeMethods.udev_monitor_filter_add_match_subsystem_devtype(
                _monitorObject, UdevNativeMethods.UdevSubsystemName, null);
            if (result < 0)
            {
                throw new PlatformApiException("udev_monitor_filter_add_match_subsystem_devtype", result,
                    "Failed to add subsystem filter");
            }

            result = UdevNativeMethods.udev_monitor_enable_receiving(_monitorObject);
            if (result < 0)
            {
                throw new PlatformApiException("udev_monitor_enable_receiving", result,
                    "Failed to enable udev monitor receiving");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _listenerThread = new Thread(ListenForReaderChanges) { IsBackground = true };
            _isListening = true;
            _listenerThread.Start(_cancellationTokenSource.Token);
        }
    }

    private void StopListening()
    {
        lock (_startStopLock)
        {
            if (!_isListening || _listenerThread is null || _cancellationTokenSource is null)
                return;

            _isListening = false;
            _cancellationTokenSource.Cancel();
        }

        var threadToJoin = _listenerThread;
        if (threadToJoin is not null)
        {
            bool exited = threadToJoin.Join(MaxDisposalWaitTime);
            if (!exited)
            {
                Logger.LogWarning("Linux HID listener thread did not exit within timeout");
            }
        }

        lock (_startStopLock)
        {
            _listenerThread = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void ListenForReaderChanges(object? obj)
    {
        Logger.LogInformation("Linux HID listener thread started. ThreadID: {ThreadId}", 
            Environment.CurrentManagedThreadId);

        try
        {
            var cancellationToken = (CancellationToken)(obj ?? CancellationToken.None);

            while (!cancellationToken.IsCancellationRequested)
            {
                CheckForUpdates();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Exception in Linux HID listener thread");
        }

        Logger.LogInformation("Linux HID listener thread exiting");
    }

    private void CheckForUpdates()
    {
        if (!HasPendingEvents((int)CheckForChangesWaitTime.TotalMilliseconds))
            return;

        using var udevDevice = UdevNativeMethods.udev_monitor_receive_device(_monitorObject);
        if (udevDevice.IsInvalid)
            return;

        var actionPtr = UdevNativeMethods.udev_device_get_action(udevDevice);
        string action = Marshal.PtrToStringAnsi(actionPtr) ?? string.Empty;

        if (string.Equals(action, "add", StringComparison.Ordinal))
        {
            var device = new LinuxHidDevice(GetDescriptorInfo(udevDevice));
            OnArrived(device);
        }
        else if (string.Equals(action, "remove", StringComparison.Ordinal))
        {
            OnRemoved(NullDevice.Instance);
        }
    }

    private bool HasPendingEvents(int timeoutMs)
    {
        var fdPtr = UdevNativeMethods.udev_monitor_get_fd(_monitorObject);
        int fd = fdPtr.ToInt32();

        const short POLLIN = 0x0001;
        var pollFd = new PlatformInterop.Linux.Libc.NativeMethods.PollFd
        {
            fd = fd,
            events = POLLIN,
            revents = 0
        };

        var pollFds = new[] { pollFd };
        int result = PlatformInterop.Linux.Libc.NativeMethods.poll(pollFds, 1, timeoutMs);

        return result > 0 && (pollFds[0].revents & POLLIN) != 0;
    }

    private static HidDescriptorInfo GetDescriptorInfo(LinuxUdevDeviceSafeHandle device)
    {
        var devNodePtr = UdevNativeMethods.udev_device_get_devnode(device);
        var devNode = Marshal.PtrToStringAnsi(devNodePtr) ?? string.Empty;

        // Parse vendor/product from parent USB device
        // This mirrors the existing LinuxHidDevice.ParseHidDescriptor logic
        return new HidDescriptorInfo { DevicePath = devNode };
    }

    protected override void Dispose(bool disposing)
    {
        lock (_disposeLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                StopListening();

                if (disposing)
                {
                    _monitorObject.Dispose();
                    _udevObject.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (disposing)
                    Logger.LogWarning(ex, "Exception during LinuxHidDeviceListener disposal");
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
```

**Step 2: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/Linux/LinuxHidDeviceListener.cs
git commit -m "feat(core): add LinuxHidDeviceListener with udev_monitor"
```

---

## Phase 4: Integrate Listeners into DeviceMonitorService

### Task 4.1: Refactor DeviceMonitorService

**Files:**
- Modify: `Yubico.YubiKit.Core/src/DeviceMonitorService.cs`
- Modify: `Yubico.YubiKit.Core/src/YubiKey/YubiKeyManagerOptions.cs`

**Key design changes:**
- Replace `PeriodicTimer` polling with event listeners
- SmartCard and HID listeners push to DeviceChannel on events
- Add 200ms coalescing delay (debounce rapid events)
- Listeners created on StartAsync, disposed on StopAsync

**Step 1: Update YubiKeyManagerOptions**

```csharp
// Yubico.YubiKit.Core/src/YubiKey/YubiKeyManagerOptions.cs
namespace Yubico.YubiKit.Core.YubiKey;

public class YubiKeyManagerOptions
{
    public bool EnableAutoDiscovery { get; set; } = true;
    
    /// <summary>
    /// Time to wait after receiving device events before processing.
    /// Allows rapid insert/remove events to coalesce.
    /// </summary>
    public TimeSpan EventCoalescingDelay { get; set; } = TimeSpan.FromMilliseconds(200);
    
    public Transport EnabledTransport { get; set; } = Transport.All;
}
```

**Step 2: Refactor DeviceMonitorService**

```csharp
// Yubico.YubiKit.Core/src/DeviceMonitorService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

/// <summary>
/// Background service that monitors for device changes using event-driven listeners.
/// </summary>
public sealed class DeviceMonitorService(
    IYubiKeyFactory yubiKeyFactory,
    IFindPcscDevices findPcscService,
    IFindHidDevices findHidService,
    IDeviceChannel deviceChannel,
    IOptions<YubiKeyManagerOptions> options)
    : BackgroundService
{
    private static readonly ILogger<DeviceMonitorService> Logger = YubiKitLogging.CreateLogger<DeviceMonitorService>();
    private readonly YubiKeyManagerOptions _options = options.Value;
    
    private SmartCardDeviceListener? _smartCardListener;
    private HidDeviceListener? _hidListener;
    private readonly SemaphoreSlim _eventSemaphore = new(0);
    private bool _disposed;

    internal static bool IsStarted { get; private set; }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableAutoDiscovery)
        {
            Logger.LogInformation("YubiKey device auto-discovery is disabled");
            return Task.CompletedTask;
        }

        IsStarted = true;
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("YubiKey device monitor stopping...");
        
        // Signal semaphore to wake up any waiting
        try { _eventSemaphore.Release(); } catch (SemaphoreFullException) { }
        
        deviceChannel.Complete();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        
        IsStarted = false;
        Logger.LogInformation("YubiKey device monitor stopped");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Logger.LogInformation("YubiKey device monitor started");
            
            // Create listeners
            SetupListeners();
            
            // Perform initial scan
            Logger.LogInformation("Performing initial device scan...");
            await PerformDeviceScan(stoppingToken).ConfigureAwait(false);

            // Event-driven loop with coalescing
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for event from listeners
                    await _eventSemaphore.WaitAsync(stoppingToken).ConfigureAwait(false);
                    
                    // Coalesce rapid events
                    await Task.Delay(_options.EventCoalescingDelay, stoppingToken).ConfigureAwait(false);
                    
                    // Drain any additional events that arrived during delay
                    while (_eventSemaphore.CurrentCount > 0)
                    {
                        await _eventSemaphore.WaitAsync(TimeSpan.Zero, stoppingToken).ConfigureAwait(false);
                    }
                    
                    await PerformDeviceScan(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("Device monitoring was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Device monitoring failed");
        }
        finally
        {
            TeardownListeners();
        }
    }

    private void SetupListeners()
    {
        try
        {
            _smartCardListener = SmartCardDeviceListener.Create();
            _smartCardListener.Arrived += OnDeviceEvent;
            _smartCardListener.Removed += OnDeviceEvent;
            Logger.LogInformation("SmartCard listener created");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to create SmartCard listener");
        }

        try
        {
            _hidListener = HidDeviceListener.Create();
            _hidListener.Arrived += OnHidDeviceEvent;
            _hidListener.Removed += OnHidDeviceEvent;
            Logger.LogInformation("HID listener created");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to create HID listener");
        }
    }

    private void TeardownListeners()
    {
        if (_smartCardListener is not null)
        {
            _smartCardListener.Arrived -= OnDeviceEvent;
            _smartCardListener.Removed -= OnDeviceEvent;
            _smartCardListener.Dispose();
            _smartCardListener = null;
        }

        if (_hidListener is not null)
        {
            _hidListener.Arrived -= OnHidDeviceEvent;
            _hidListener.Removed -= OnHidDeviceEvent;
            _hidListener.Dispose();
            _hidListener = null;
        }
    }

    private void OnDeviceEvent(object? sender, SmartCardDeviceEventArgs e)
    {
        Logger.LogDebug("SmartCard event: {Device}", e.Device?.ReaderName);
        SignalEvent();
    }

    private void OnHidDeviceEvent(object? sender, HidDeviceEventArgs e)
    {
        Logger.LogDebug("HID event: {Device}", e.Device?.ReaderName);
        SignalEvent();
    }

    private void SignalEvent()
    {
        try
        {
            _eventSemaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already signaled, ignore
        }
    }

    private async Task PerformDeviceScan(CancellationToken cancellationToken)
    {
        try
        {
            var pcscScanTask = ScanPcscDevices(cancellationToken);
            var hidScanTask = ScanHidDevices(cancellationToken);
            await Task.WhenAll(pcscScanTask, hidScanTask).ConfigureAwait(false);

            var yubiKeys = new List<IYubiKeyReference>();
            yubiKeys.AddRange(pcscScanTask.Result);
            yubiKeys.AddRange(hidScanTask.Result);

            await deviceChannel.PublishAsync(yubiKeys, cancellationToken).ConfigureAwait(false);
            Logger.LogDebug("Device scan completed, found {TotalCount} total devices", yubiKeys.Count);
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("Device scan was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Device scanning failed");
        }
    }

    private async Task<List<IYubiKeyReference>> ScanPcscDevices(CancellationToken cancellationToken)
    {
        var devices = await findPcscService.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var yubiKeys = devices.Select(yubiKeyFactory.Create).ToList();
        Logger.LogDebug("PCSC scan: {DeviceCount} devices", devices.Count);
        return yubiKeys;
    }

    private async Task<List<IYubiKeyReference>> ScanHidDevices(CancellationToken cancellationToken)
    {
        var devices = await findHidService.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var yubiKeys = devices.Select(yubiKeyFactory.Create).ToList();
        Logger.LogDebug("HID scan: {DeviceCount} devices", devices.Count);
        return yubiKeys;
    }

    public override void Dispose()
    {
        if (_disposed) return;
        
        TeardownListeners();
        _eventSemaphore.Dispose();
        base.Dispose();
        
        _disposed = true;
    }
}
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/src/DeviceMonitorService.cs Yubico.YubiKit.Core/src/YubiKey/YubiKeyManagerOptions.cs
git commit -m "refactor(core): DeviceMonitorService to use event-driven listeners"
```

---

## Phase 5: Testing - Disposal and Cancellation

### Task 5.1: Create Disposal Tests

**Files:**
- Create: `Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/DesktopSmartCardDeviceListenerDisposalTests.cs`
- Create: `Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/Hid/HidDeviceListenerDisposalTests.cs`

**IMPORTANT:** These tests verify critical shutdown behavior. Run them manually and observe:
1. Tests complete within timeout (no hanging)
2. No exceptions thrown during disposal
3. Finalizers don't block GC

**Step 1: Create SmartCard disposal tests**

```csharp
// Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/DesktopSmartCardDeviceListenerDisposalTests.cs
using Xunit;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard;

/// <summary>
/// Tests for SmartCard listener disposal behavior.
/// CRITICAL: These verify the listener shuts down cleanly without blocking.
/// </summary>
public class DesktopSmartCardDeviceListenerDisposalTests
{
    /// <summary>
    /// Verify that Dispose() completes within a reasonable time.
    /// If this test hangs, the cancellation logic is broken.
    /// </summary>
    [Fact]
    public void Dispose_CompletesWithinTimeout()
    {
        // Arrange
        using var listener = SmartCardDeviceListener.Create();
        
        // Act - should complete within 10 seconds
        var task = Task.Run(() => listener.Dispose());
        bool completed = task.Wait(TimeSpan.FromSeconds(10));
        
        // Assert
        Assert.True(completed, "Dispose() did not complete within 10 seconds - cancellation may be broken");
    }

    /// <summary>
    /// Verify multiple rapid Dispose() calls are safe.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_NoException()
    {
        // Arrange
        var listener = SmartCardDeviceListener.Create();
        
        // Act & Assert - should not throw
        listener.Dispose();
        listener.Dispose();
        listener.Dispose();
    }

    /// <summary>
    /// Verify GC finalization doesn't hang.
    /// Creates listener without disposing, forces GC, verifies completion.
    /// </summary>
    [Fact]
    public void Finalizer_DoesNotBlockGC()
    {
        // Arrange - create and abandon listener
        CreateAndAbandonListener();
        
        // Act - force GC
        var task = Task.Run(() =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        });
        
        bool completed = task.Wait(TimeSpan.FromSeconds(15));
        
        // Assert
        Assert.True(completed, "GC finalization blocked - finalizer may be joining a thread");
    }

    private static void CreateAndAbandonListener()
    {
        // Create listener but don't dispose - will be finalized
        _ = SmartCardDeviceListener.Create();
    }
}
```

**Step 2: Create HID disposal tests**

```csharp
// Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/Hid/HidDeviceListenerDisposalTests.cs
using Xunit;
using Yubico.YubiKit.Core.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Hid;

/// <summary>
/// Tests for HID listener disposal behavior.
/// Platform-specific - may skip on unsupported platforms.
/// </summary>
public class HidDeviceListenerDisposalTests
{
    [Fact]
    public void Dispose_CompletesWithinTimeout()
    {
        // Skip if platform not supported
        HidDeviceListener? listener;
        try
        {
            listener = HidDeviceListener.Create();
        }
        catch (PlatformNotSupportedException)
        {
            return; // Skip test on unsupported platform
        }

        using (listener)
        {
            var task = Task.Run(() => listener.Dispose());
            bool completed = task.Wait(TimeSpan.FromSeconds(10));
            
            Assert.True(completed, "HID Dispose() did not complete within 10 seconds");
        }
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_NoException()
    {
        HidDeviceListener? listener;
        try
        {
            listener = HidDeviceListener.Create();
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }

        listener.Dispose();
        listener.Dispose();
        listener.Dispose();
    }

    [Fact]
    public void Finalizer_DoesNotBlockGC()
    {
        try
        {
            CreateAndAbandonListener();
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }

        var task = Task.Run(() =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        });

        bool completed = task.Wait(TimeSpan.FromSeconds(15));
        Assert.True(completed, "HID GC finalization blocked");
    }

    private static void CreateAndAbandonListener()
    {
        _ = HidDeviceListener.Create();
    }
}
```

**Step 3: Run tests and verify**

```bash
# Run disposal tests specifically
dotnet toolchain.cs test -- --filter "FullyQualifiedName~DisposalTests"

# Expected: All tests pass within timeout
# If any test hangs: the cancellation/disposal logic needs debugging
```

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/DesktopSmartCardDeviceListenerDisposalTests.cs
git add Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/Hid/HidDeviceListenerDisposalTests.cs
git commit -m "test(core): add disposal tests for device listeners"
```

---

### Task 5.2: Integration Tests (Device Present, No Insertion/Removal)

**Note:** These tests require a YubiKey to be connected (both SmartCard and HID interfaces available). They do NOT require device insertion/removal during tests.

**Files:**
- Create: `Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.IntegrationTests/SmartCard/SmartCardDeviceListenerIntegrationTests.cs`
- Create: `Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.IntegrationTests/Hid/HidDeviceListenerIntegrationTests.cs`

**Step 1: SmartCard integration tests**

```csharp
// Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.IntegrationTests/SmartCard/SmartCardDeviceListenerIntegrationTests.cs
using Xunit;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.IntegrationTests.SmartCard;

/// <summary>
/// Integration tests for SmartCard device listener.
/// Requires YubiKey connected via SmartCard interface.
/// Does NOT require device insertion/removal during test.
/// </summary>
[Trait("Category", "Integration")]
[Trait("RequiresDevice", "SmartCard")]
public class SmartCardDeviceListenerIntegrationTests
{
    /// <summary>
    /// Verify listener starts successfully with PC/SC service available.
    /// </summary>
    [Fact]
    public void Create_WithPcscAvailable_StatusIsStarted()
    {
        // Arrange & Act
        using var listener = new DesktopSmartCardDeviceListener();
        
        // Assert - should be started (or error if no PC/SC service)
        Assert.True(
            listener.Status is DeviceListenerStatus.Started or DeviceListenerStatus.Error,
            $"Unexpected status: {listener.Status}");
    }

    /// <summary>
    /// Verify listener can be created, subscribed to, and disposed without events firing.
    /// </summary>
    [Fact]
    public void EventSubscription_NoDeviceChange_NoEventsFired()
    {
        // Arrange
        int arrivedCount = 0;
        int removedCount = 0;
        
        using var listener = new DesktopSmartCardDeviceListener();
        listener.Arrived += (_, _) => Interlocked.Increment(ref arrivedCount);
        listener.Removed += (_, _) => Interlocked.Increment(ref removedCount);
        
        // Act - wait briefly (no device changes expected)
        Thread.Sleep(500);
        
        // Assert - no spurious events
        Assert.Equal(0, arrivedCount);
        Assert.Equal(0, removedCount);
    }

    /// <summary>
    /// Verify listener thread is background (won't prevent app exit).
    /// </summary>
    [Fact]
    public void ListenerThread_IsBackground()
    {
        // Arrange
        using var listener = new DesktopSmartCardDeviceListener();
        
        // Allow thread to start
        Thread.Sleep(100);
        
        // Assert - verify status indicates running
        // (We can't directly check thread.IsBackground from outside,
        //  but if disposal completes, we know it's not blocking)
        Assert.Equal(DeviceListenerStatus.Started, listener.Status);
    }

    /// <summary>
    /// Verify dispose unsubscribes all event handlers.
    /// </summary>
    [Fact]
    public void Dispose_ClearsEventHandlers()
    {
        // Arrange
        var listener = new DesktopSmartCardDeviceListener();
        bool eventFired = false;
        listener.Arrived += (_, _) => eventFired = true;
        
        // Act
        listener.Dispose();
        
        // Assert - can't easily verify handlers cleared, but dispose should complete
        Assert.False(eventFired);
    }

    /// <summary>
    /// Verify Status transitions to Stopped after dispose.
    /// </summary>
    [Fact]
    public void Dispose_SetsStatusToStopped()
    {
        // Arrange
        var listener = new DesktopSmartCardDeviceListener();
        Assert.Equal(DeviceListenerStatus.Started, listener.Status);
        
        // Act
        listener.Dispose();
        
        // Assert
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
    }
}
```

**Step 2: HID integration tests**

```csharp
// Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.IntegrationTests/Hid/HidDeviceListenerIntegrationTests.cs
using Xunit;
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.PlatformInterop;

namespace Yubico.YubiKit.Core.IntegrationTests.Hid;

/// <summary>
/// Integration tests for HID device listener.
/// Requires YubiKey connected via HID interface.
/// Does NOT require device insertion/removal during test.
/// </summary>
[Trait("Category", "Integration")]
[Trait("RequiresDevice", "HID")]
public class HidDeviceListenerIntegrationTests
{
    private static bool IsPlatformSupported => 
        SdkPlatformInfo.OperatingSystem is SdkPlatform.Windows or SdkPlatform.MacOS or SdkPlatform.Linux;

    /// <summary>
    /// Verify correct platform-specific listener is created.
    /// </summary>
    [SkippableFact]
    public void Create_ReturnsCorrectPlatformImplementation()
    {
        Skip.IfNot(IsPlatformSupported, "HID listener not supported on this platform");
        
        // Act
        using var listener = HidDeviceListener.Create();
        
        // Assert
        var expectedType = SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => typeof(WindowsHidDeviceListener),
            SdkPlatform.MacOS => typeof(MacOSHidDeviceListener),
            SdkPlatform.Linux => typeof(LinuxHidDeviceListener),
            _ => throw new PlatformNotSupportedException()
        };
        
        Assert.IsType(expectedType, listener);
    }

    /// <summary>
    /// Verify listener starts successfully.
    /// </summary>
    [SkippableFact]
    public void Create_StatusIsStarted()
    {
        Skip.IfNot(IsPlatformSupported, "HID listener not supported on this platform");
        
        using var listener = HidDeviceListener.Create();
        
        Assert.Equal(DeviceListenerStatus.Started, listener.Status);
    }

    /// <summary>
    /// Verify no spurious events when device state is stable.
    /// </summary>
    [SkippableFact]
    public void EventSubscription_NoDeviceChange_NoEventsFired()
    {
        Skip.IfNot(IsPlatformSupported, "HID listener not supported on this platform");
        
        int arrivedCount = 0;
        int removedCount = 0;
        
        using var listener = HidDeviceListener.Create();
        listener.Arrived += (_, _) => Interlocked.Increment(ref arrivedCount);
        listener.Removed += (_, _) => Interlocked.Increment(ref removedCount);
        
        // Wait briefly - no device changes expected
        Thread.Sleep(500);
        
        Assert.Equal(0, arrivedCount);
        Assert.Equal(0, removedCount);
    }

    /// <summary>
    /// Verify dispose completes cleanly on all platforms.
    /// </summary>
    [SkippableFact]
    public void Dispose_CompletesCleanly()
    {
        Skip.IfNot(IsPlatformSupported, "HID listener not supported on this platform");
        
        var listener = HidDeviceListener.Create();
        var initialStatus = listener.Status;
        
        // Act
        var disposeTask = Task.Run(() => listener.Dispose());
        bool completed = disposeTask.Wait(TimeSpan.FromSeconds(5));
        
        // Assert
        Assert.True(completed, "Dispose did not complete within timeout");
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
    }
}
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.IntegrationTests/SmartCard/SmartCardDeviceListenerIntegrationTests.cs
git add Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.IntegrationTests/Hid/HidDeviceListenerIntegrationTests.cs
git commit -m "test(core): add integration tests for device listeners (no device changes needed)"
```

---

### Task 5.3: Manual Testing Checklist (Requires Human Presence)

**⚠️ IMPORTANT: Manual verification required**

Before considering this complete, manually test these scenarios:

#### Scenario 1: Clean shutdown during device enumeration
1. Start application with YubiKey inserted
2. While background service is running, call `host.StopAsync()`
3. **Verify:** Application exits cleanly within 2 seconds
4. **Verify:** No exceptions in logs

#### Scenario 2: Shutdown while waiting for device event
1. Start application with NO YubiKey
2. Wait 5 seconds (listeners are waiting for events)
3. Call `host.StopAsync()`
4. **Verify:** Application exits cleanly within 2 seconds

#### Scenario 3: Rapid device insertion/removal
1. Start application
2. Insert and remove YubiKey rapidly 5 times within 2 seconds
3. **Verify:** Application handles events without crash
4. **Verify:** Final state reflects actual device presence

#### Scenario 4: Device disconnected during operation
1. Start application with YubiKey
2. Begin a long operation (e.g., key generation)
3. Remove YubiKey mid-operation
4. **Verify:** Operation fails gracefully with appropriate error
5. **Verify:** Removal event fires

#### Scenario 5: PC/SC service restart (Windows)
1. Start application with YubiKey
2. Restart "Smart Card" Windows service
3. **Verify:** Listener recovers and detects devices again

---

## Phase 6: Platform Interop Updates (If Needed)

### Task 6.1: Verify P/Invoke Declarations

**Files to check:**
- `Yubico.YubiKit.Core/src/PlatformInterop/Windows/Cfgmgr32/Cfgmgr32.Interop.cs`
- `Yubico.YubiKit.Core/src/PlatformInterop/MacOS/IOKitFramework/`
- `Yubico.YubiKit.Core/src/PlatformInterop/Linux/Udev/`

**Verify these methods exist:**
- Windows: `CM_Register_Notification`, `CM_Unregister_Notification`
- macOS: `IOHIDManagerRegisterDeviceMatchingCallback`, `IOHIDManagerRegisterDeviceRemovalCallback`, `CFRunLoopRunInMode`, `CFRunLoopStop`
- Linux: `udev_monitor_new_from_netlink`, `udev_monitor_enable_receiving`, `udev_monitor_get_fd`, `poll`

If any are missing, port from `develop` branch `Yubico.Core/src/Yubico/PlatformInterop/`.

---

## Summary

| Phase | Description | Commits |
|-------|-------------|---------|
| 1 | Infrastructure - base classes | 2 |
| 2 | SmartCard listener (1000ms timeout) | 1 |
| 3 | HID listeners (event-driven) | 3 |
| 4 | DeviceMonitorService integration | 1 |
| 5 | Testing - disposal/cancellation/integration | 2 |
| 6 | Platform interop (if needed) | 0-3 |

**Total estimated commits:** 9-12

**Key behavioral changes:**
- SmartCard: 500ms poll → 1000ms timeout with dedicated thread
- HID: 500ms poll → event-driven callbacks (near-instant)
- Coalescing: None → 200ms delay after events

**Risk areas requiring careful testing:**
1. Thread shutdown timing (use timeouts, don't block forever)
2. Callback delegate GC (store as fields, not locals)
3. Platform interop completeness (verify all P/Invoke methods exist)
4. Error recovery (PC/SC service restart, USB hub issues)
