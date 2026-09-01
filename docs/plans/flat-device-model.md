# Flat published-device model

## Assignment

Execute stages A and B through the autonomous Craftsman workflow. Stages C and D are recorded follow-ups
and require a separate approval. The feature slice is Core device discovery, connection routing, repository
correlation, its tests, and the corresponding architecture documentation.

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

Replace pre-merge `IYubiKey` wrappers with raw interface candidates carrying live handles, interface ids,
connection type, PID, serial metadata, and topology. Delete `PcscYubiKey`, `HidYubiKey`, and the factory.

This stage is soak-gated. Do not begin it automatically after stages A and B. Exercise the flat published
device model against the `yubikit` baseline first, including discovery grouping, routing, repository
retention, metadata propagation, and hot-plug or partial-enumeration behavior. Stage C requires separate
approval after that comparison and any resulting adaptations are complete.

### Stage D: identity policy

Resolve complete interface-set equality versus serial-first shared-path correlation; decide one-slot
`DeviceId`; then expose discovery metadata under an explicit nullability and lifetime contract.

## Hardware protocol for deferred hot-plug checks

Run one narrowly filtered test per invocation with a human ready. Never run the complete
`RequiresUserPresence` category. Capture the removed and added object, `DeviceId`, interface identifiers,
serial metadata, and the full event timeline for each individual cycle.
