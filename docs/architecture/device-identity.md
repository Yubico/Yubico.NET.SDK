# Device identity and physical-device correlation

**Status:** accepted, with deferred identity-policy questions

## Context

`IYubiKey.DeviceId` is public, but it names the evidence tier that discovery used to group interfaces.
It can therefore change while the physical key remains attached. The repository avoids false removals by
diffing on `YubiKeyDevice.PhysicalIdentityKeyFor`, an internal fingerprint of the observed interface
identifiers. Consumers cannot use that fingerprint, and `DeviceInfo.SerialNumber` currently requires a
separate Management call.

These values answer different questions:

| Value | Scope | Suitable use |
|---|---|---|
| `DeviceId` | one published object / discovery evidence | diagnostics and event correlation during one uninterrupted presence |
| physical identity key | observed interface paths on one machine | internal repository correlation while the interface set is unchanged |
| `DeviceInfo.SerialNumber` | hardware-reported serial, when present | durable identity across processes and reinsertions |

The physical identity key is not intrinsic hardware identity. It is a length-prefixed encoding of sorted
PC/SC reader names and HID paths. Moving a key to another port may change it. A fully enumerated composite
also encodes a different interface set from a temporarily single-interface record published by merger rule
G6. Publishing this encoding would expose driver strings as API and would not satisfy a durable identity
contract.

## Canonical yubikit evidence

The comparison below was made against `yubikey-manager-rust-auto` commit `7d7a7455`.

- Rust represents a discovered key as one flat `LocalYubiKeyDevice` with optional `reader_name`,
  `hid_path`, and `fido_path` slots (`crates/yubikit/src/platform/device.rs:67-78`). It does not publish
  separate interface-device and composite-device implementations.
- The Rust monitor wraps that flat device in `YubiKey { id, device, nodes }`. `YubiKeyId` is a
  process-local counter assigned at first sight and retained across `Added`, `Changed`, and `Removed`
  events (`crates/yubikit/src/platform/monitor.rs:1029-1088`). A removal carries the complete last-known
  device, including cached metadata.
- Rust correlates monitor snapshots by serial when both sides have one, otherwise by any shared transport
  path (`crates/yubikit/src/platform/monitor.rs:1243-1255,1526-1534`). This tolerates an interface being
  added or removed. The C# repository instead requires equality of the complete interface set, which is
  stricter and helps prevent same-slot key substitution but causes a discontinuity during partial
  enumeration.
- Python's fingerprint is the first non-empty value of `reader_name or hid_path or fido_path`
  (`packages/yubikit/yubikit/device.py:201-231`). Its public contract explicitly says the fingerprint is
  not stable between sessions or after unplugging and reinserting a device
  (`packages/yubikit/yubikit/core/device.py:52-57`). It is not a durable physical descriptor.
- Canonical discovery eagerly opens a transport, reads `DeviceInfo`, and stores it on the device. Its
  consumer code uses the serial for cross-scan deduplication. Neither Rust nor Python provides a
  connection-free durable identity for a serial-less Security Key.
- The Rust polling merger has a `(version, serial)` fallback match
  (`crates/yubikit/src/platform/device.rs:988-1027`). C# deliberately excludes firmware from identity;
  this is especially important for development keys whose USB descriptor reports an alpha/beta `0.x`
  version while Management metadata supplies the effective firmware version.

Canonical behavior informs this decision but does not override the C# substitution-safety requirements.

## Decisions

### D1: keep the interface-set key internal

Do not promote `PhysicalIdentityKeyFor` or its string encoding to public API. It remains an internal,
machine-local repository key. `DeviceId` remains unchanged and documented as diagnostic rather than
durable identity.

### D2: expose discovery-read metadata after prerequisites are met

Core should eventually expose the best-effort `DeviceInfo` already read during discovery. This avoids a
second Management connection, makes cached serial and firmware metadata available without a Management
package dependency, and lets a `Removed` event retain metadata obtained while the key was connected.

Stages A and B closed the internal prerequisites: `PopulateMetadataAsync` now covers every flat
`YubiKeyDevice`, including one-slot USB and NFC records, and `YubiKeyDeviceRepository.UpdateCache` propagates
later successful metadata onto the retained object without device events. The deterministic
`UpdateCache_LaterScanHasMetadata_UpdatesRetainedPublishedObjectWithoutEvents` test pins that behavior. The
public member shape (`DeviceInfo?`, a `TryGet` method, or a narrower serial property) remains deferred until
nullability and lifetime semantics are settled; `IYubiKey` still does not expose metadata.

### D3: document object retention as the in-process correlation guarantee

Within one running `YubiKeyManager` repository and one uninterrupted physical presence, the repository
retains the previously published `IYubiKey` object while its interface identity and capabilities remain
unchanged. Reference equality or an ordinary `Dictionary<IYubiKey, T>` therefore correlates cached scans
and the eventual `Removed` event.

This guarantee does not survive `ShutdownAsync`, process restart, independent repositories, or a
capability/interface-set change represented as `Removed` plus `Added`. It does not satisfy the original
story's stronger requirement for independently created scan objects; serial remains the only durable
answer when the key reports one.

### D4: describe canonical behavior accurately

Documentation and source comments must not call canonical Python's fingerprint durable or stable across
reinsertion. The relevant narrow similarity is only that path identity is independent of the evidence tier
used to group a scan.

### D5: defer the repository correlation policy

No decision is made here between complete interface-set equality and canonical Rust's serial-first,
any-shared-path correlation. The former favors substitution safety; the latter preserves continuity while
interfaces appear or disappear. This deserves a separate decision with deterministic swap and partial-
enumeration tests.

The `DeviceId` assigned to a one-slot flat device is also deferred. Refactoring must preserve today's
transport-shaped value until that public behavior is explicitly reconsidered.

## Answers to the user-story questions

1. **Should physical identity be public?** Not as the current interface-set string. Use serial for durable
   identity and documented object retention for in-process event correlation.
2. **What stability contract is available?** Object retention is process- and presence-scoped; the internal
   path key is machine-, port-, and interface-set-scoped; serial is durable when present.
3. **Opaque type or fingerprint string?** Neither is approved. An opaque wrapper would still encode the
   wrong stability promise unless the underlying correlation policy changes.
4. **Must this agree across SDKs?** It should agree semantically where possible: serial is durable,
   path-based fingerprints are session-scoped, and event correlation may use a process-local handle. Exact
   encodings need not match.
5. **Is a PC/SC reader name stable enough?** Only for a bounded local session while that reader identity is
   unchanged. It is not a durable public identity.
6. **Do multi-slot and G6 one-slot flat records produce the same physical key?** No. The current set encoding
   differs because one contains all observed interface identifiers and the other contains one identifier.
7. **Can discovery cache serials for removal events?** Yes, and discovery already caches metadata
   internally. D2 describes the work required to expose it reliably.
8. **Was the untouched-key `Added` event a defect?** Not independently reproduced. A G6 one-slot record and
   a complete multi-slot record have different interface-set keys, so a legitimate `Removed` plus `Added` is a
   plausible explanation. Verification requires a human-coordinated hot-plug run.

## Hardware evidence

On 2026-09-01, macOS enumerated five allow-listed USB keys, all PID `0x0407`, with serials `20260533`,
`125`, `103`, `31683481`, and `31683268`. Six non-user-presence tests in
`CompositeDiscoveryIntegrationTests` passed: conservation, zero orphans, complete expected interface sets,
stable consecutive grouping, eventual metadata propagation across repeated scans, and typed connection
opens. This validates complete serial-tier grouping and retained-object metadata propagation on the current
five-key/four-worker rig; it does not validate hot-plug transitions.

## Consequences and follow-ups

- The flat published-device model and retained-object metadata propagation are implemented internally.
- Decide the public discovery-metadata member shape only after its null and lifetime contract is explicit.
- Decide interface-set equality versus serial-first shared-path correlation separately.
- Decide one-slot `DeviceId` behavior separately; preserve current behavior until then.
- Reproduce the hot-plug observations with one narrowly filtered test per invocation and a human ready to
  remove or insert hardware. Never run the whole user-presence category as a blocking lane.
