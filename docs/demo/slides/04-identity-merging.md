## Merging: one physical key, many interfaces

A single YubiKey appears as **several OS-level devices** — a PC/SC reader, a FIDO HID
device, an OTP HID device. The SDK merges them into one `IYubiKey`.

```
USB PC/SC reader  ─┐
FIDO HID device   ─┼─→  CompositeDeviceMerger  ─→  one IYubiKey
OTP HID device    ─┘
```

Merge inputs per interface: `Connection` · `IsUsb` · `Pid` · `Serial` · `DeviceInfo` ·
`TopologyKey` (Windows Container ID; `null` on macOS and Linux)

**NFC never merges.** Neither does a PC/SC reader of unknown kind — only USB-attached
interfaces are merge candidates. Correlation uses an internal, machine-local
interface-set key.

<!-- Anchors: src/Core/src/Devices/CompositeDeviceMerger.cs:19-30 (doc), :40-48 (descriptor);
     PhysicalIdentityKeyFor src/Core/src/Devices/YubiKeyDevice.cs:123,
     used YubiKeyDeviceRepository.cs:112 -->

---

## Identity: what is stable, what is not

| Surface | Guarantee |
|---|---|
| `DeviceId` | **Diagnostic only.** Not durable identity. |
| `SerialNumber` | Latched. `null` → value, **never** back to `null`. |
| Interface-set key | Internal, machine-local, never public |
| `Equals` / `GetHashCode` | **Referential** — same object, or not equal |

- `SerialNumber` may stay `null` **forever**: reads fail, budgets exhaust, and the
  Security Key series reports no serial at all.
- It can flip `null` → value **after** publication, with **no** device event.
- A key whose interface set changes is **republished as a new object** that inherits
  nothing from its predecessor.

> Full `DeviceInfo` stays internal: capabilities, flags and config are mutable via
> Management, so a cached copy goes stale. The serial is the one burned-in field.
> Need the rest? `await key.GetDeviceInfoAsync()` reads it live.

<!-- Anchors: device-identity.md D1 :61-65, D2 :67-98 (latch :76-77, null-forever :73-75,
     late arrival :80-81, republication :82-84), D6 :161, D7 :179;
     shortcut src/Management/src/PublicAPI.Unshipped.txt:33 -->
