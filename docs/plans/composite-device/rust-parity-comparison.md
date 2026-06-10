# Composite Device: .NET SDK vs Rust `yubikey-manager` Parity Comparison

Comparison of the .NET SDK (after Phase 37.5) against the Rust reference `../yubikey-manager`
(branch `experiment/rust`) across connectivity, robustness, merging, and discovery.

Grounded in:

- Rust: `crates/yubikit/src/platform/device.rs` (`list_devices`, `merge_devices`, `open_single_usb`,
  `pid_from_reader_name`, `pid_from_interfaces`, `is_sky_pid`, `apply_device_info_fixups`,
  `reinsert_*`, `scan_usb_devices`) and `crates/yubikit/src/platform/pcsc.rs` (`open`/`open_inner`,
  `kill_pcsc_blockers`, `is_reader_usb`, retry logic).
- .NET: `src/Core/src/YubiKey/` (`FindYubiKeys`, `CompositeDeviceMerger`, `ReaderNamePidParser`,
  `CompositeMetadataReader`, `DiscoveryIdentityReader`, `ProtocolDeviceInfo`, `CompositeYubiKey`,
  `YubiKeyDeviceRepository`, `YubiKeyDeviceMonitorService`).

Status date: after Phase 37.5 (`feat(core): merge composite devices by USB Product ID`).

## Discovery — at parity

| Aspect | Rust | .NET (now) |
| --- | --- | --- |
| Per-transport enumeration (CCID / OTP HID / FIDO HID) | yes | yes |
| USB ↔ NFC classification | reader-name prefix `"yubico yubikey"` | ATR-derived `PscsConnectionKind` + reader-name PID parse |
| PID source | reader name (CCID) + HID descriptor | reader name (CCID) + HID descriptor |
| Hotplug detection | `scan_usb_devices` poll + fingerprint diff | Rx event listeners (HID + SmartCard), 200 ms throttle, repository diff |
| Add/remove events | — | repository `Added`/`Removed`, incl. remove+add on capability change |

Mechanically equivalent. .NET is event-driven (push); Rust is poll-fingerprint. .NET additionally
exposes a typed device-change event stream keyed by physical identity.

## Merging — at parity (Rust slightly richer; .NET more conservative)

Both use the same core model: PID from the reader name (CCID) + HID descriptor PID; a single key
(PID-count <= 1) merges with no opens; multiple same-model keys are disambiguated by serial; NFC is
never merged; SKY (`0x0120`) is handled.

- Rust extras: a PID-count==2 topology-free shortcut before the `(version, serial)` fallback; and
  DeviceInfo synthesis for OTP's bogus `3.0.0`, NEO, and CTAP1-only devices.
- .NET differences: PID-count > 1 always uses serial (no PID==2 shortcut) and refuses to collapse
  serial-less same-model keys (slightly more conservative; Rust cannot reliably merge those either).
  .NET's merge is a pure, deterministic function with a known-PID gate (no PID-0 over-merge), backed
  by unit tests.

## Connectivity (opening sessions) — .NET behind, by deliberate design

| Aspect | Rust | .NET |
| --- | --- | --- |
| Typed per-transport connect | yes | yes (`ConnectAsync<T>`) |
| Preferred-transport read (CCID -> OTP -> FIDO) | `open_single_usb` | `CompositeMetadataReader` |
| Open a CCID held by another process | yes — `kill_pcsc_blockers` (`pkill scdaemon`/`yubikey-agent`) + Exclusive->Shared->retry | no — never kills user processes (library-appropriate) |
| No-card retry on insert | 9x / 500 ms | serial path retries 3x on transient sharing violation |
| Reacquire handle after reboot/reset (`reinsert`) | yes, with `WrongDevice` safety (matches serial AND version) | not implemented |

Rust is a CLI tool and is more aggressive here. As an SDK library, .NET deliberately does not kill a
user's `scdaemon`, so it cannot open a held CCID — but Phase 37.5 means it still discovers and merges
that CCID. Applet transport fallback (prefer CCID, fall back to HID when held) is Phase 38.

## Robustness — at parity; .NET ahead on discovery, behind on edge devices

- .NET ahead: grouping is fully open-independent — the merge never fails because an interface is
  locked/unreadable (proven by a unit test where every `ConnectAsync` throws). Plus a known-PID gate,
  discovery serialization, identity + metadata caches with eviction, and a large deterministic test
  suite. Strict package boundary (Core has no dependency on Management) — a constraint Rust's single
  crate does not carry.
- .NET behind: no DeviceInfo synthesis for old/NEO/CTAP1-only keys when reads fail (Rust fabricates a
  best guess; .NET leaves metadata null); no `reinsert`/reconnect-after-reboot; 3x vs 9x insert retry.

## Net Assessment

After Phase 37.5, .NET reaches functional parity with Rust on discovery and merging — the two things
that motivated this work — and is arguably more robust in discovery (open-independent merge,
deterministic and heavily tested, no PID-0 footgun). The remaining gaps are all in
connectivity/lifecycle, and are either deliberate (no process-killing in a library) or already
scheduled.

### Remaining gaps to reach/exceed Rust

1. Held-CCID session fallback (prefer SmartCard, fall back to HID where the applet supports it) — Phase 38.
2. `reinsert` / reconnect-after-reboot with `WrongDevice` safety — deferred (recorded).
3. DeviceInfo synthesis for NEO / CTAP1-only / old firmware when reads fail — deferred.
4. Optional CLI-only `kill_pcsc_blockers` opt-in — deferred (never in the library).
5. PID-count==2 topology shortcut — minor; .NET uses serial, which is arguably safer.

All of the above are also captured in `phase-37_5-pid-merge-learnings.md` (Deferred Candidates).

## Summary Table

| Dimension | Verdict |
| --- | --- |
| Discovery | At parity (different mechanism: .NET push events vs Rust poll) |
| Merging | At parity (Rust slightly richer heuristics; .NET more conservative + deterministic) |
| Connectivity | .NET behind (no kill-blockers by design; no `reinsert`) |
| Robustness | At parity overall (.NET ahead on discovery; behind on edge-device info synthesis) |
