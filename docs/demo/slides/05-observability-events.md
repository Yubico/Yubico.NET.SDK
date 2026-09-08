## Observability: the whole device-event API

```csharp
YubiKeyManager.StartMonitoring();                 // events don't flow until this

await foreach (var e in YubiKeyManager.WatchAsync())
{
    Console.WriteLine($"{e.Action}: {e.Device.DeviceId}");
}
```

```csharp
public enum DeviceAction { Added, Removed }
public class DeviceEvent { IYubiKey Device; DeviceAction Action; DateTime Timestamp; }
```

The monitoring **control** surface is five members: `StartMonitoring()`,
`StartMonitoring(TimeSpan)`, `StopMonitoring()`, `WatchAsync(ct)`, `IsMonitoring`.
`Shutdown()` / `ShutdownAsync()` also stop monitoring as part of tearing the manager down.

<!-- Anchors: src/Core/src/PublicAPI.Unshipped.txt:986-992 (incl. Shutdown :987, ShutdownAsync :988);
     ShutdownAsync stops monitoring src/Core/src/Devices/YubiKeyManager.cs:216;
     src/Core/src/DeviceEvent.cs:19-30; docs/usage/device-discovery.md:126 -->

---

## How the peers surface device changes

| SDK | Attach / detach |
|---|---|
| **.NET v2** | `await foreach WatchAsync()` — OS-notification driven |
| `yubikey-manager` (Python) | **Poll only** — `scan_devices()` in a `sleep` loop |
| `yubikey-manager` (rust) | `monitor_yubikeys(cb)` — udev / `WM_DEVICECHANGE` / `IOHIDManager` |
| `yubikit-android` | Callbacks — `Callback<T>.invoke` attach, `setOnClosed` detach |
| `yubikit-swift` | **Connect-on-demand** — `makeConnection()` polls; `waitUntilClosed()` |
| `python-fido2` | **None** — `list_devices()` enumerates on call |

Two things worth noting: rust has a third variant, `YubiKeyEvent::Changed`, that .NET
does not. And Swift's `makeConnection()` is a 1-second poll loop internally, not an
OS notification.

<!-- Anchors: ykman ykman/device.py:108; rust monitor.rs:1113, examples/monitor_yubikeys.rs:87;
     android YubiKitManager.java:82-84, UsbYubiKeyDevice.java:154;
     swift@1.4.0 Connection.swift:32, USBSmartCardConnection.swift:53-55;
     python-fido2 fido2/hid/__init__.py:275-278 -->
