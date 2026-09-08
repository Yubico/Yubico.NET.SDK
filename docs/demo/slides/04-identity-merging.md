## Merging: one physical key, many interfaces

A single YubiKey appears as **several OS-level devices** — a PC/SC reader, a FIDO HID
device, an OTP HID device. The SDK merges them into one `IYubiKey`.

```
USB PC/SC reader  ─┐
FIDO HID device   ─┼─→  CompositeDeviceMerger  ─→  one IYubiKey
OTP HID device    ─┘

NFC reader        ───→  stands alone, never merged
```

Merge inputs per interface: `Connection` · `IsUsb` · `Pid` · `Serial` · `DeviceInfo` ·
`TopologyKey` (Windows Container ID; `null` on macOS and Linux)

<!-- Anchors: src/Core/src/Devices/CompositeDeviceMerger.cs:19-30 (doc), :40-48 (descriptor);
     NFC/unknown-kind never merge :24-27, :119-124;
     PhysicalIdentityKeyFor src/Core/src/Devices/YubiKeyDevice.cs:123 -->

---

## What happens to an NFC key

NFC is **discovered and monitored exactly like USB** — the same
`SCardGetStatusChange` listener covers NFC readers, so tap and remove raise the
same `Added` / `Removed` events, and the same repository cache holds them.

What differs is **grouping**, and only grouping:

- Merging needs USB evidence — Product ID from the reader name, or a Windows
  Container ID. An NFC reader has neither.
- So an NFC-presented key is **published standalone**, with a transport-shaped
  `DeviceId` (`pcsc:*`), not a `ykphysical:*` one.
- Tap the same key over USB and NFC at once and you get **two `IYubiKey` objects**.
  The SDK will not claim they are one physical key, because nothing proved it.
- The honest correlator is `SerialNumber` — read it from both and compare.

This is conservative on purpose: a wrong merge is worse than no merge.

<!-- Anchors: NFC never merges src/Core/src/Devices/CompositeDeviceMerger.cs:24-27,
     standalone :119-124; no topology probe for non-USB
     src/Core/src/Devices/FindYubiKeys.cs:191;
     transport-shaped DeviceId docs/architecture/device-identity.md:179 (D7) -->

---

## Identity: what is stable, what is not

| Surface | Guarantee |
|---|---|
| `DeviceId` | **Diagnostic only.** Not durable identity. |
| `SerialNumber` | `null` → value, **never** back to `null`. |
| Interface-set key | Internal, machine-local, never public |
| `Equals` / `GetHashCode` | **Referential** — same object, or not equal |

- `SerialNumber` may stay `null` **forever**: reads fail, budgets exhaust, and the
  Security Key series reports no serial at all.
- It can flip `null` → value **after** publication, with **no** device event.
- A key whose interface set changes is **republished as a new object** that inherits
  nothing from its predecessor.

> **Why keep `DeviceId` public at all?** Its prefix encodes *which evidence tier
> produced it*: `pcsc:*` / `hid:*` for a lone interface, `ykphysical:*` only once
> grouping actually proved a physical key. That makes it genuinely useful in logs
> and bug reports. Removing it was considered and rejected for that reason — but
> it is the interface-set key, kept internal, that would be the wrong thing to expose.

<!-- Anchors: device-identity.md D1 :61-65, D2 :67-98 (latch :76-77, null-forever :73-75,
     late arrival :80-81, republication :82-84), D6 :161, D7 :179-184;
     "not as the current interface-set string" :188-190 -->
