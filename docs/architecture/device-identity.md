# Device identity and physical-device correlation

**Status:** accepted; stage D' identity contract decided and shipped, including the metadata-retry
policy (retry-every-scan, measured and pinned — see the stage D' section of
`docs/plans/flat-device-model.md`)

## Context

`IYubiKey.DeviceId` is public, but it names the evidence tier that discovery used to group interfaces.
It can therefore change while the physical key remains attached. The repository avoids false removals by
diffing on `YubiKeyDevice.PhysicalIdentityKeyFor`, an internal fingerprint of the observed interface
identifiers. Consumers cannot use that fingerprint; the durable identity they can use is
`IYubiKey.SerialNumber` (D2), when the hardware reports one.

These values answer different questions:

| Value | Scope | Suitable use |
|---|---|---|
| `DeviceId` | one published object / discovery evidence | diagnostics and event correlation during one uninterrupted presence |
| physical identity key | observed interface paths on one machine | internal repository correlation while the interface set is unchanged |
| `IYubiKey.SerialNumber` | hardware-reported serial, when present | durable identity across processes and reinsertions |

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
  added or removed. The C# repository instead requires equality of the complete interface set and treats
  different known serials as contradictory evidence. This is stricter during partial enumeration and
  republishes a same-slot substitution once discovery obtains the successor's serial.
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

### D2: expose the serial number only; full `DeviceInfo` stays internal

`IYubiKey.SerialNumber` (stage D', R2) exposes the hardware serial read during discovery without a
session and without a Management package dependency. It is a default interface member, so external
`IYubiKey` implementations compile unchanged and inherit a `null` default. Its contract:

- `null` until a discovery metadata read has succeeded — and possibly forever: reads can fail
  persistently, the discovery read budget can be exhausted, and whole device classes (the Security Key
  series) report no serial at all.
- Once non-`null`, it never reverts to `null`. The value is latched independently of internal
  `DeviceInfo` churn: a later successful read whose metadata carries no serial cannot regress it.
- A manager-retained object's known serial does not change to another known value. A fresh scan reporting
  a different known serial for the same interface set proves substitution and republishes a new object.
- It may transition `null` → non-`null` after publication, without any device event
  (`UpdateCache_LateSerialArrival_PopulatesRetainedObjectWithoutEvents`).
- A republished object never inherits identity from its predecessor object. It exposes only what discovery
  established for that fresh object, which may already include a serial when the addition is published
  (`UpdateCache_ConnectionSetChangeRepublication_NewObjectDoesNotInheritSerial`).
- The object delivered with a removal event retains its last-known value
  (`UpdateCache_RemovalEvent_ObjectRetainsLastKnownSerial`).

The clauses are pinned by `DeviceIdentityContractTests` and
`YubiKeyDeviceRepositoryCompositeTests`.

Full `DeviceInfo` exposure remains rejected for now: its fields (enabled capabilities, flags,
configuration) are mutable via Management reconfiguration, so a cached copy can go stale. Canonical
Rust affords whole-struct exposure only by withholding publication until metadata is read, which
conflicts with this SDK's publish-first, degraded-state-tolerant discovery. Revisit only with an
explicit staleness/nullability design. The serial is the one field that cannot go stale — it is
burned-in hardware identity.

### D3: instance retention and republication are contract

Within one manager instance, an attached physical key whose interface and connection sets are unchanged
and whose known serial is not contradicted is represented by exactly one retained `IYubiKey` object across
scans (stage D', R1). Reference-keyed collections — `Dictionary<IYubiKey, T>`, `HashSet<IYubiKey>` — are
therefore a supported in-process pattern
(`UpdateCache_UnchangedInterfaceSet_ReferenceKeyedDictionaryRemainsValidAcrossScans`).

A device is instead republished as `Removed` + `Added` — delivering a **new object** that inherits
nothing from its predecessor — in exactly four cases, each pinned in
`YubiKeyDeviceRepositoryCompositeTests`:

1. **Interface-set change** — an interface appears or disappears, including merger rule G6 partial
   enumeration (`UpdateCache_InterfaceSetChanged_RepublishesAsNewObject`).
2. **Connection-set change** over an unchanged interface set (ISC-17;
   `UpdateCache_ConnectionSetChangedSameInterfaceSet_RepublishesOnce`).
3. **Reinsertion** observed across scans
   (`UpdateCache_ReinsertionObservedAcrossScans_PublishesNewObject`).
4. **Known-serial substitution** — unchanged interface and connection sets, but a fresh scan reports a
   different known serial
   (`UpdateCache_DifferentKnownSerials_RepublishesDifferentDevice`).

The guarantee is scoped to one manager instance and one uninterrupted physical presence. It does not
survive `ShutdownAsync`, process restart, or independent repositories.

**Correlation across those boundaries — the durable-correlation recipe.** Use
`IYubiKey.SameDeviceAs(other)` (stage D', R3), which answers the question "do these references
describe the same physical key?" honestly, with `DeviceCorrelation` tri-state semantics:

- the same object is always `Same`;
- two references with known, equal serials are `Same`; known, unequal serials are `Different`;
- if either serial is unknown, the answer is `Unknown` — never a guess.

When the serial is present it is the durable identity: use it to deduplicate rescans, key
cross-process state, and match audit records. When it is absent, physical identity is unknowable
without a live session — the same epistemic bound canonical Python (connection-comparing tuples,
elimination guessing) and Rust (process-local monitor ids, serial as the convenience accessor) live
with. The truth table is pinned by `DeviceIdentityContractTests`, including both-unknown and
one-unknown resolving to `Unknown`.

### D4: describe canonical behavior accurately

Documentation and source comments must not call canonical Python's fingerprint durable or stable across
reinsertion. The relevant narrow similarity is only that path identity is independent of the evidence tier
used to group a scan.

### D5: repository correlation policy — decided: stable sets without serial contradiction

Stage D' closes this question: equality of the complete interface and connection sets, guarded by
known-serial contradiction, is the retention contract (D3). Interface-set equality detects an observed
absence or changed interface shape; when neither is observed, different known serials are the substitution
backstop. Canonical Rust's serial-first, any-shared-path correlation is rejected for this SDK because it
deliberately tolerates interface churn and because mixed-evidence correlation (serial when available, path
fallback otherwise) is intransitive and cannot back the exact retention scope promised here. The
discontinuity during partial enumeration is the accepted, documented cost; it is exactly republication
trigger 1.

This backstop depends on fresh evidence. If no transport-activity notification invalidates discovery's
identity and metadata caches and no scan observes the slot empty, a same-slot successor can receive its
predecessor's cached serial. The repository then has no contradictory evidence and cannot distinguish the
swap. A direct caller orchestrating removal and insertion must scan while the slot is empty or restart the
manager; forcing only a post-insertion scan does not invalidate same-interface cache entries.

### D6: `Equals`/`GetHashCode` on published device objects remain referential — decided

Value equality on published device objects is **rejected**, not deferred:

- The serial arrives after publication (D2 allows `null` → non-`null` with no event), so any
  serial-derived hash is mutable while the object may already key a collection — the one corruption a
  hash contract must never allow.
- Any fallback for the serial-less case (paths, PID, timing) makes equality mixed-evidence and
  therefore intransitive: A=B by serial and B=C by path does not imply A=C.
- Whole device classes cannot honestly satisfy a cross-manager equality contract: serial-less keys
  (Security Key series) and platforms without topology evidence have no durable identity to compare.
  Neither canonical Python nor Rust attempts value equality on device objects.

For the same reason **no `IEqualityComparer<IYubiKey>` ships** for physical correlation: a comparer
whose hash could change under a live dictionary key is corrupted by late-arriving metadata, and a
comparer immune to that is reference equality in disguise. Key collections by reference (D3) and
correlate with `SameDeviceAs` (D3 recipe).

### D7: single-interface devices keep their transport-shaped `DeviceId` — decided

A one-slot flat device keeps its `pcsc:*` / `hid:*` identifier. A lone interface observation carries
only transport-level evidence; a `ykphysical:*` name would claim a physical-identity discovery never
established. The `DeviceId` prefix therefore remains truthful about the evidence tier that produced
it: `ykphysical:*` appears only when grouping evidence proved a physical key.

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
7. **Can discovery cache serials for removal events?** Yes — shipped: the object delivered with a
   removal event retains its last-known `SerialNumber` (D2).
8. **Was the untouched-key `Added` event a defect?** Did not reproduce during the 2026-09-03 human-coordinated
   hardware hot-plug run, which showed a clean `Added` → `Removed` → `Added` timeline. A G6 one-slot record
   and a complete multi-slot record have different interface-set keys, so a legitimate `Removed` plus `Added`
   remains the plausible explanation for earlier multi-key observations.

## Hardware evidence

On 2026-09-01, macOS enumerated five allow-listed USB keys, all PID `0x0407`, with serials `20260533`,
`125`, `103`, `31683481`, and `31683268`. Six non-user-presence tests in
`CompositeDiscoveryIntegrationTests` passed: conservation, zero orphans, complete expected interface sets,
stable consecutive grouping, eventual metadata propagation across repeated scans, and typed connection
opens. This validates complete serial-tier grouping and retained-object metadata propagation on the current
five-key/four-worker rig; it does not validate hot-plug transitions.

On 2026-09-03, the deferred hot-plug protocol ran on macOS with a human operator (single composite
USB key, PID `0x0407`, serial `103`, 1 s monitoring interval). One remove/reinsert cycle produced a
clean `Added` → `Removed` → `Added` timeline with no spurious intermediate events (the question-8
phantom `Added` did not reproduce). All stage D' clauses held on hardware: the `Removed` event
delivered the exact retained object with its last-known serial; reinsertion published a new object;
the serial was re-established from hardware after full hotplug cache eviction within the same scan;
and `SameDeviceAs` correlated the pre- and post-reinsertion objects as `Same` in both directions.
The same session ran the R4 latency measurements on a 3-key rig (two USB composites plus an OMNIKEY
5022 NFC reader) — the numbers and the resulting retry-policy decision are recorded in the stage D'
section of `docs/plans/flat-device-model.md`. The repeatable harness is
`HotPlugIdentityContractTests` (`RequiresUserPresence`; one filtered test per invocation, operator
present); it passed the full ceremony the same day. Open observation: one earlier ceremony where the
harness awaited only the event stream (no concurrent `FindAllAsync` polling) missed a
human-confirmed removal under the VSTest host, while the identical sequence in a console host
detected it. Not reproduced with diagnostics yet; the harness now polls the snapshot alongside the
event stream and records which layer observed the removal, so a recurrence will localize itself.

## Consequences and follow-ups

- The flat published-device model, retained-object metadata propagation, and the stage D' identity
  contract (D2, D3, D5, D6, D7) are implemented and pinned by deterministic tests.
- Metadata-read retry policy is decided with hardware evidence: retry-every-scan, bounded by the
  per-scan attempt cap and read budgets, pinned by the constant-attempts fault-injection test — see
  the stage D' section of `docs/plans/flat-device-model.md` for the measurements.
- A derived product name ("YubiKey 5C NFC"-style, per Rust's `name()` convenience) is deferred: it
  requires the PID/version/form-factor naming table.
- Run the repeatable `HotPlugIdentityContractTests` ceremony (`RequiresUserPresence`) as one narrowly
  filtered test per invocation with a human ready to remove or insert hardware. Never run the whole
  user-presence category as a blocking lane.
