# Flat published-device model

## Assignment

Stages A and B are complete (commit `fa54aeea`). Stage C was approved on 2026-09-02 and executes on
branch `yubikit-flat-device-model-v2` through the autonomous Craftsman workflow. Stage D was resolved
as the stage D' device identity contract (branch `yubikit-device-identity-contract`), with all
preconditions and the bounded-metadata-retry policy completed on 2026-09-03. The feature slice is Core
device discovery, connection routing, repository correlation, its tests, and the corresponding
architecture documentation.

The public `IYubiKey` interface remains. It is a consumer contract and a critical testing seam with many
fake implementations. The goal is one production implementation returned by discovery, not the removal of
interfaces as a language feature.

## Invariants

- One returned `IYubiKey` represents one physical key with zero or one slot for each concrete connection
  type: SmartCard, HID FIDO, and HID OTP.
- The slots own live enumerated `IPcscDevice` / `IHidDevice` handles. Core cannot reopen HID interfaces by
  path, so a string-only Rust model is not viable.
- A typed connection and `DeviceConnectionRegistry.ResolveInterfaceId` select the same slot.
- One connection claims every known interface identifier of the physical key.
- Merger evidence tiers, ambiguity handling, conservation, and one-slot `DeviceId` values remain unchanged.
- `IYubiKey` stays public and fakeable. Applet modules remain coupled only to `IYubiKey` and
  `ConnectionType`.

## Stage A: one routing rule

Move connection-type-to-interface resolution behind one internal operation used by:

1. normal typed connection opening;
2. discovery connection opening; and
3. `DeviceConnectionRegistry.ResolveInterfaceId`.

Pin the invariant that registry identity and the interface actually opened cannot diverge. This stage is
behavior-preserving and establishes the tracer bullet for stage B.

## Stage B: one published device shape

Introduce an internal sealed `YubiKeyDevice : IYubiKey` with optional SmartCard, HID FIDO, and HID OTP
slots. It exposes sorted interface identifiers, an internal physical identity key, combined
`AvailableConnections`, best-effort `DeviceInfo`, and the shared routing operation.

Convert every merger publication path, not only `AddGroupedDevice`:

- topology / serial / PID groups;
- conservative serial-merger fallbacks;
- unknown-PID USB interfaces; and
- non-USB records.

Every path constructs `YubiKeyDevice`; a group of one is a one-slot instance rather than a different
runtime type. Preserve the one-slot transport-shaped `DeviceId` produced today. Whether a one-slot device
should instead receive a `ykphysical:*` value is explicitly deferred by
`docs/architecture/device-identity.md`.

Delete `CompositeYubiKey` and `IScopedConnectionProvider`. Keep `PcscYubiKey` and `HidYubiKey` only as
internal pre-merge slot adapters in this stage if that is the smallest safe implementation; they must never
be published. Make `IYubiKeyFactory` and `YubiKeyFactory` internal because their one-interface-to-one-key
shape is not a supported public extension point. Keep an internal static physical-identity accessor with a
single-`DeviceId` fallback for third-party/test `IYubiKey` implementations.

Populate metadata for all `YubiKeyDevice` instances, including one-slot USB and NFC records. Propagate a
later successful metadata read to the retained repository object or otherwise ensure it cannot remain
stale while the metadata cache has a newer value.

## Test migration

Replace runtime-type assertions with behavior assertions:

- slot/interface-id count;
- `AvailableConnections` shape;
- routing to each slot;
- preserved `DeviceId` value; and
- physical identity behavior.

Preserve these high-risk contracts rather than mechanically renaming them:

- connection lease scope remains stable across later regrouping;
- serial/PID evidence-tier flips do not churn an unchanged repository entry;
- transparent third-party `IYubiKey` implementations remain supported;
- every merger vector conserves interfaces and emits pairwise-distinct `DeviceId` values; and
- one-slot devices remain transport-named pending the deferred decision.

## Verification

After each stage:

```bash
dotnet toolchain.cs -- build --project Core
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- resilience --fast
dotnet toolchain.cs -- test --integration --project Core \
  --filter "FullyQualifiedName~CompositeDiscoveryIntegrationTests&Category!=RequiresUserPresence"
```

Run formatting only over changed source files. Read the per-project `total:` value; the closing toolchain
summary counts projects, not tests.

## Deferred stages

### Stage C: raw interface candidates

Replace the pre-merge `IYubiKey` wrappers with raw interface candidates. Delete `PcscYubiKey`,
`HidYubiKey`, `IYubiKeyFactory`, and `YubiKeyFactory`. The soak gate was satisfied by the
Stage A/B verification runs against `fa54aeea` (unit, resilience, Core and PIV hardware
integration); approval is recorded in the Assignment.

#### Current shape

- `PcscYubiKey` / `HidYubiKey` (`src/Core/src/Devices/Implementations/`) implement
  `IYubiKeyConnectionSlot : IYubiKey` and `IDiscoveryConnectionProvider`. Production uses only three
  of their members: `DeviceId` (the stable `pcsc:{ReaderName}` / `hid:{ReaderName}:{Usage:X4}`
  strings), `AvailableConnections` (constructor validation in `YubiKeyDevice.ValidateSlot`), and
  `OpenRawConnectionAsync` (called from `YubiKeyDevice.ConnectAsync<T>` and
  `ConnectForDiscoveryAsync`). Their registered `ConnectAsync<T>` self-claim paths are
  production-dead since Stage B; only `ConnectionOwnershipContractTests` and
  `DeviceConnectionOwnershipTests` exercise them. `PcscYubiKey.Create` has zero callers.
- Candidate data already lives on records, not the wrappers: `FindYubiKeys.InterfaceCandidate`
  (private: slot, connection, IsUsb, PID, topology key) and `DeviceInterfaceDescriptor`
  (internal, `CompositeDeviceMerger.cs`: adds serial, `DeviceInfo`, identity-read budget flag).
- The pre-merge identity read (`ProtocolDeviceInfo.ConnectAndReadAsync`) type-tests
  `IDiscoveryConnectionProvider` and leases via `DeviceConnectionRegistry.ResolveInterfaceId`.
- `FindYubiKeys` receives `IYubiKeyFactory` by constructor; unit tests inject fake factories to
  script identity reads and fault injection.

#### Target shape

Two internal sealed slot types — `PcscConnectionSlot` and `HidConnectionSlot` — are constructed
directly from live enumerated `IPcscDevice` and `IHidDevice` handles behind
`IYubiKeyConnectionSlot`:

- carries the live handle, the unchanged interface-id string, and its single `ConnectionType`;
- absorbs the connection-creation logic of both wrappers (`SmartCard` via
  `ISmartCardConnectionFactory`; `FidoHidConnection` / `OtpHidConnection` with interface-type
  validation) behind `OpenRawConnectionAsync`;
- implements the discovery-read provider so `ProtocolDeviceInfo` keeps working;
- is not an `IYubiKey`. Pre-merge candidates never appear as devices anywhere.

`IYubiKeyConnectionSlot` is narrowed to the slot contract actually consumed by `YubiKeyDevice`:
interface id, slot connection type, `OpenRawConnectionAsync` (keeping the default-throw
`NonOpenableConnectionSlotException` seam). It no longer extends `IYubiKey`. Existing test fakes
migrate mechanically. `InterfaceCandidate` and `DeviceInterfaceDescriptor` keep their shapes with
`Device` retyped to the narrowed `IYubiKeyConnectionSlot` interface — not the sealed concrete
type — so scripted/fault-injection fakes keep flowing through the candidate records.

The pre-merge identity-read pipeline (`DiscoveryIdentityReader`, `ProtocolDeviceInfo`,
`CompositeMetadataReader` lease-key resolution) currently accepts `IYubiKey`. Stage C retypes the
pre-merge entry points to the narrowed slot contract (or a slot overload) so a non-`IYubiKey`
candidate flows through identity reads without an adapter; the post-merge `YubiKeyDevice` metadata
read path keeps its current shape.

#### Steps

1. Introduce `PcscConnectionSlot` and `HidConnectionSlot` implementing the narrowed
   `IYubiKeyConnectionSlot` and `IDiscoveryConnectionProvider`, absorbing their respective wrappers'
   raw-open and discovery-connect logic.
   Byte-for-byte identical interface-id strings are a hard invariant: in-flight
   `DeviceConnectionRegistry` leases correlate across scans by these strings. Add unit tests that
   pin the exact production formats — `pcsc:{ReaderName}` and `hid:{ReaderName}:{Usage:X4}`
   (upper-case hex, four digits) — against real `PcscConnectionSlot` and `HidConnectionSlot`
   instances, not fakes.
2. Narrow `IYubiKeyConnectionSlot` (drop the `IYubiKey` base) and update `YubiKeyDevice`
   (`ValidateSlot`, slot fields, `TryResolveSlot`) for the narrowed contract. Retype the pre-merge
   identity-read pipeline entry points per the target shape above. The `ResolveInterfaceId`
   single-`DeviceId` fallback for third-party/test `IYubiKey` implementations is unchanged.
3. Rework `FindYubiKeys.BuildInterfaces` to construct slots directly; delete the factory pair.
   Replace the factory constructor seam with the smallest seam that preserves the existing
   fault-injection and scripted-identity tests (a slot-construction delegate is acceptable; do not
   reintroduce an `IYubiKey`-shaped factory).
4. Delete `PcscYubiKey`, `HidYubiKey`, `YubiKeyFactory.cs`, and dead members. Migrate the two
   self-claim contract test files to assert the same ownership contracts at the `YubiKeyDevice`
   level; delete any test made redundant by an existing Stage B equivalent rather than porting it.
5. Update test fakes in the remaining files listed below.
6. Update documentation: `docs/architecture/sdk-architecture-map.yml`,
   `sdk-architecture-diagrams.md`, `device-discovery-guarantees.md` (wrapper mentions), the
   `CompositeDeviceMerger` doc comment, and `src/Core/CLAUDE.md` / `src/Core/README.md` wrapper
   references. If any mermaid source changed, regenerate images with
   `scripts/architecture/render-architecture.sh`, then validate with
   `dotnet toolchain.cs -- docs-architecture` (the toolchain target validates evidence and image
   freshness; it does not regenerate).

#### Invariants pinned for Stage C

- Interface-id strings, merger evidence tiers, ambiguity handling, conservation, one-slot
  `DeviceId` values, `PhysicalIdentityKey` encoding, and repository retention/metadata propagation
  are all byte-for-byte unchanged. Stage C deletes types, not behavior.
- The identity-read budget, metadata cache keying/eviction, and discovery lease scope are
  unchanged. Activity observed on any transport globally invalidates both identity and metadata
  caches; the reported transport is diagnostic context, not an eviction scope.
- No public API change. `IYubiKey` remains public and fakeable; applet modules stay coupled only to
  `IYubiKey` and `ConnectionType`.

#### Test migration inventory

Files with hard breakage (construct wrappers or implement `IYubiKeyFactory`), all under
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/` unless noted:
`ConnectionOwnershipContractTests.cs`, `DeviceConnectionOwnershipTests.cs`,
`DiscoverySingleFlightTests.cs`, `HeldExceptionPropagationTests.cs`, `FindYubiKeysTests.cs`,
`FindYubiKeysPidMergeTests.cs`, `FindYubiKeysFaultInjectionTests.cs`,
`DiscoveryIdentityReaderTests.cs` (exercises the retyped identity-read boundary), and
`../Transports/SmartCard/FindPcscDevicesTests.cs`. Files whose `IYubiKeyConnectionSlot` fakes need
mechanical retyping only: `YubiKeyDeviceTests.cs`, `CompositeDeviceMergerTests.cs`,
`CompositeDeviceMergerVectorTests.cs`, `YubiKeyDeviceRepositoryCompositeTests.cs`,
`DeviceConnectionRegistryTests.cs`. Preserve the intent of every ownership/lease contract test;
behavior assertions replace type assertions.

#### Stage C verification

```bash
dotnet toolchain.cs -- build --project Core
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- resilience --fast
dotnet toolchain.cs test
dotnet toolchain.cs -- test --integration --project Core \
  --filter "FullyQualifiedName~CompositeDiscoveryIntegrationTests&Category!=RequiresUserPresence"
dotnet toolchain.cs -- test --integration --project Piv \
  --filter "FullyQualifiedName~PivSessionContentionTests&Category!=RequiresUserPresence"
```

Read per-project `total:` values, not the closing project-count summary. Run `dotnet format` only
over changed files. No `RequiresUserPresence` tests.

### Stage D': device identity contract (shipped)

Stage D was resolved as stage D' — a documented, honest identity contract rather than new identity
machinery. Decided and shipped (see `docs/architecture/device-identity.md` for the decision records
and the full contract, each clause pinned by `DeviceIdentityContractTests` and the retention pins in
`YubiKeyDeviceRepositoryCompositeTests`):

- **Retention is contract (R1).** Equality of the complete interface and connection sets plus a known-serial
  contradiction guard is the repository correlation policy; serial-first shared-path correlation is
  rejected. One retained `IYubiKey` object per attached key with unchanged sets and no contradictory known
  serial; republication as a new object happens on exactly four triggers: interface-set change,
  connection-set change, reinsertion, or a different known serial proving same-slot substitution.
- **`IYubiKey.SerialNumber` (R2).** The discovery-read serial is public, session-free, and
  Management-free, under an explicit nullability/lifetime contract, added as a default interface
  member so external implementers do not break.
- **`IYubiKey.SameDeviceAs` (R3).** Tri-state physical correlation (`DeviceCorrelation`); unknown
  serial on either side of two distinct references answers `Unknown`, never a guess. Comparing a
  reference with itself always answers `Same`. No equality comparer ships (recorded decision).
- **Referential `Equals`/`GetHashCode` and transport-shaped one-slot `DeviceId` are decided, not
  deferred** — recorded with rationale in the architecture document.

**R4 — bounded metadata-read retries: decided 2026-09-03, retry-every-scan stands.** Both
preconditions were completed on hardware (macOS, two composite USB 0x0407 keys with serials 103 and
31683481, HID Global OMNIKEY 5022 NFC reader, YubiKey 5 NFC serial 125):

1. **Measured latencies.** Cold 3-key scan (2 USB composites + 1 NFC key, all serials resolved):
   260–580 ms. Warm scans with populated identity/metadata caches: 1–3 ms. Post-hotplug full cache
   eviction: serial re-read completed within the same scan (~250 ms; the serial was already present
   on the published object at the `Added` event). Cold-cache serial availability on a single-key
   rig: 79–264 ms.
2. **Persistent-failure exposure is narrower than feared.** A non-YubiKey card on an NFC reader
   (credit card) is rejected by the ATR allow-list in `FindPcscDevices` before any budgeted read —
   per-scan cost ~0. A YubiKey with all NFC applications disabled takes its NFC transport silent
   entirely and is not enumerated — cost 0. On current firmware the Management applet cannot be
   disabled while the NFC transport is up, so a published-but-persistently-unreadable NFC YubiKey is
   not manufacturable by configuration; cross-process reader contention fails fast with a sharing
   violation. An ambiguous USB interface whose identity read fails uses at most
   `DiscoveryIdentityReader.MaxAttempts` (3) connect attempts per scan, constant across scans. Each
   attempt has its own 2 s caller-wait budget; a timeout returns unknown immediately and is not
   retried. Other failures can retry after 150 ms and 300 ms delays. A published device that instead
   needs best-effort metadata makes one no-retry pass over its available transports with a single 3 s
   caller-wait budget shared across that pass; persistent failure repeats that constant work on later
   scans. A device that used the identity-read path does not also incur the metadata-read budget in
   that scan.

**Decision:** retry-every-scan remains the policy; no per-identity backoff ledger is added. The
attempt behavior is covered by
`FindYubiKeysFaultInjectionTests.FindAllAsync_PersistentIdentityFailureAcrossScans_PerScanAttemptsStayConstant`
(exactly three attempts per scan under the scripted failure, constant across scans, and healthy
siblings unaffected; no hardware required). The test counts attempts; it does not measure the two-second
per-attempt wait budget. If future hardware changes the numbers, the fallback design remains: a
per-physical-identity failure ledger in `FindYubiKeys` with a capped cross-scan backoff, reset by
`NotifyTransportActivity` and absence eviction.

## Hardware protocol for deferred hot-plug checks

Run one narrowly filtered test per invocation with a human ready. Never run the complete
`RequiresUserPresence` category. Capture the removed and added object, `DeviceId`, interface identifiers,
serial metadata, and the full event timeline for each individual cycle.
