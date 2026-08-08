# Device Discovery Guarantees

What the SDK's composite device discovery does and does not guarantee, per platform, and why.

Every guarantee below cites the test that pins it. A guarantee with no pinning test is not a
guarantee — if you cannot find the citation, treat the behavior as unspecified and report it.

Scope: grouping multiple USB interfaces (CCID/smart card, HID FIDO, HID OTP) of one physical
YubiKey into one `IYubiKey`, as performed by `FindYubiKeys.FindAllAsync` and
`CompositeDeviceMerger`. See [Physical Device Model](physical-device-model.md) for the device model
itself, [Event-Driven Device Discovery](event-driven-device-discovery.md) for the monitoring
loop, and [Connection Ownership and Contention](connection-ownership-and-contention.md) for who may
hold which interface and what discovery does when one is already held.

## The core problem: same-PID keys

Two YubiKeys of the same model are **indistinguishable at the USB layer**. They share a VID, a PID,
and a product string, and YubiKeys deliberately expose **no USB `iSerialNumber` descriptor**. The
serial number and firmware version live inside the key and are readable only by opening a
connection and querying the Management application.

"Same-PID" is therefore the precise term throughout this document. Two keys with different serials
and different firmware are still same-PID if they are the same model with the same enabled
interface set — and that is the case discovery has to solve.

Discovery resolves same-PID keys using an **evidence hierarchy**, strongest first:

| Tier | Evidence | Cost | Availability |
|---|---|---|---|
| 1 | **USB topology** (Windows Container ID) | no device I/O | Windows only |
| 2 | **Serial number** read over each interface | one short-lived connection per interface | any key that reports a serial |
| 3 | **PID uniqueness + completeness** — merge on PID alone only when the observed interface set exactly equals what the PID promises | free | always |
| 4 | **Pigeonhole deduction** — an orphan whose type fills the one incomplete anchored composite, under type-count closure | free | always |
| 5 | **Conservative standalone** — publish the interface on its own rather than guess | free | always |

The hierarchy never guesses. When evidence runs out, interfaces are published separately, which is
incomplete but never wrong.

Serial reads are conditional and on demand; discovery does **not** open every interface on every
scan. It requests serial evidence only when PID correlation is untrusted, more than one physical key
shares a PID, or a partial-PID shape is ambiguous. Only successful reads are cached, keyed by the
stable **interface identifier** (`hid:*`, `pcsc:*` — not the physical `ykphysical:*` identifier discussed
in [Device identity](#device-identity-what-deviceid-does-and-does-not-promise), which is a different
concept); failures and null serials are retried on later scans. This is pinned by
`FindAllAsync_ScriptedIdentityFailure_DeducedIntoAnchoredKey_AndRereadOnNextScan_Pin`,
`FindAllAsync_InterfaceDisappearance_EvictsIdentityCacheEntries_Pin`, and
`FindAllAsync_PcscReaderRenameBetweenScans_OldEntryMissesAndSuccessfulRereadHeals_Pin` in
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs`.

A cached identity is evidence about specific hardware in a specific configuration, and it expires with
either. **Hardware**: scan-time eviction only catches interfaces observed absent, and a same-slot swap
completing between scans reuses the slot-derived interface identifier — the old key's serial would be
attributed to its same-model successor (key substitution). A physical swap cannot happen without the OS
observing removal and arrival, so the device monitor forwards every listener event to
`IFindYubiKeys.NotifyTransportActivity` at ingress, which discards that transport's cached identities
before the rescan the event triggers. Eviction is per transport (HID activity does not discard PC/SC
evidence, and vice versa). **Configuration**: each entry records the PID observed at read time, and a
hit under a different PID is a miss. Pinned by
`FindAllAsync_SameSlotSwapWithTransportActivity_RereadsInsteadOfServingTheOldKeysSerial`,
`NotifyTransportActivity_HidOnly_LeavesPcscIdentityCacheIntact`, and
`FindAllAsync_PidChangeOnSameInterfaceId_IsACacheMissNotAHit`; the monitor wiring by
`ListenerEvents_NotifyTheFinderOfTransportActivity_PerTransport`.

Documented bound: without monitoring running there are no listener events, and staleness detection
degrades to scan-observed absence. Consumers driving `FindAllAsync` directly across a physical swap
they orchestrated themselves should force a rescan after replugging.

## Device identity: what `DeviceId` does and does not promise

The merge hierarchy above decides *which interfaces form one key*. It also decides *what that key is
called*, and the two are the same decision — which is why the identifier is evidence-dependent rather than
intrinsic. This section is the consumer-facing contract; `IYubiKey.DeviceId` carries the same statement in
XML docs.

### Two different identifiers, easily confused

| Identifier | Example | Names | Used by |
|---|---|---|---|
| **Interface identifier** | `hid:4367418413:0006`, `pcsc:Yubico YubiKey OTP+FIDO+CCID` | one USB interface | connection registry, identity cache |
| **Physical identifier** (`IYubiKey.DeviceId`) | `ykphysical:103` | one physical key | discovery results, `DeviceChanges` |

The interface identifier is stable for as long as the interface exists. The physical identifier is not, in
the way described below. Where this document says "stable interface `DeviceId`" it means the former.

### Shape follows evidence tier

| Tier used | `DeviceId` shape | Available on |
|---|---|---|
| 1 — topology (Container ID) | `ykphysical:topology:{key}` | Windows only |
| 2 — serial | `ykphysical:{serial}` | all platforms |
| 3/4 — PID uniqueness / pigeonhole | `ykphysical:pid:{PID:X4}` | all platforms |
| 5 — conservative standalone | the interface identifier, published alone | all platforms |

**At most one `ykphysical:{serial}` object exists per serial per scan.** A serial observed under more
than one PID in a single scan is one physical key caught mid-reconfiguration — serial is globally unique
per key, and PID changes with configuration — enumerated once staly and once fresh. A serial↔PID flip has
been observed on macOS hardware. Those enumerations are **not merged into one composite** (it would hold
two members of the same connection type, invisible in `AvailableConnections` and resolved arbitrarily at
connect time) and they do **not** both mint the id. Instead, the *winner rule* applies: the PID group
whose serial-anchored census exactly matches its PID's expected interface set keeps `ykphysical:{serial}`;
every other contested interface publishes standalone. Zero or multiple complete groups is ambiguity, and
ambiguity fragments conservatively — the next scan converges. The reasoning is recorded at
`CompositeDeviceMerger.ResolveContestedSerials`, and the rule is pinned by the `Merge_ContestedSerial_*`
vectors plus `Merge_AnyVector_ProducesPairwiseDistinctDeviceIds`.

macOS and Linux have no Container ID, so they never mint tier-1 identifiers and degrade to serial, then
PID. The same rig therefore yields different identifier shapes on different operating systems.

**The shape is an implementation detail. Do not parse it.**

### The same key can present different identifiers

Because the identifier reflects the evidence used, it changes when the available evidence changes — even
though the device did not. Measured on macOS hardware:

| Rig | Identifier for key 103 | Why |
|---|---|---|
| 103 alone | `ykphysical:pid:0407` | its PID is unique on the bus; no serial evidence needed |
| 103 + a same-PID sibling | `ykphysical:103` | serial evidence is now required to tell the two apart |

Inserting an unrelated second key therefore changes how the first one is named. Discovery absorbs this
without emitting spurious add/remove events for the incumbent, which is the guarantee that matters to
subscribers, but the identifier itself is not invariant.

### A live repository and a fresh scan can disagree

When an evidence-only tier change occurs, the repository keeps publishing the object it already handed to
subscribers rather than replacing it. A concurrent, independent scan computes the identifier from current
evidence and may return a different one. Both are correct: one preserves continuity for existing
subscribers, the other reports present truth. Observed simultaneously on macOS — live repository
`ykphysical:pid:0407`, fresh scan `ykphysical:103`.

### Use the serial as the durable key

For persistence, audit logs, allow lists, or anything surviving a process restart, use
`DeviceInfo.SerialNumber`, not `DeviceId`. Caveats:

- YubiKeys expose **no USB `iSerialNumber` descriptor**. The serial lives inside the key and is read by
  opening an interface, so obtaining it costs a connection and a Management exchange — it is not free the
  way `DeviceId` is.
- It is `null` on devices that do not report one (for example Security Key series, or when serial
  visibility is disabled). A null serial cannot be a durable key; such devices are only distinguishable by
  topology evidence, which exists on Windows alone (see G4 in the guarantee matrix).
- Discovery itself reads serials only on demand, for the reasons given above.

### Firmware version is deliberately not part of identity

It adds no uniqueness — the serial is already unique — and it is not dependable as a discriminator:

- It can differ per applet on one physical key. This SDK carries an explicit workaround taking the higher
  of the Management and OTP values on NEO (`src/YubiOtp/src/YubiOtpSession.cs`).
- Canonical yubikit sometimes *guesses* it (`version = Version(3, 0, 0); // Guess NEO`).
- Canonical uses it only as a tie-breaker for which metadata record to retain once serials already match,
  never as a match key.

This SDK's merger contains no firmware-version logic, and none should be added for identity purposes.

## Guarantee matrix

| # | Guarantee | Windows | macOS | Linux |
|---|---|---|---|---|
| G1 | No cross-key composite under complete same-PID visibility | yes | yes | yes |
| G2 | No cross-key composite during partial visibility | yes, with topology evidence | bounded — see [G2 bound](#g2-the-epistemic-bound) | same as macOS |
| G3 | Same-PID keys that report serials: complete grouping | first scan, with topology | first scan in practice; converges — see [G3](#g3-convergence) | same as macOS |
| G4 | Serial-less multi-interface keys: complete grouping | yes, with topology | **no — permanent split** | **no — permanent split** |
| G5 | Reconfigured key (different enabled interfaces) | yes | yes | yes |
| G6 | Single-interface keys are never wrapped in a composite | yes | yes | yes |
| G7 | Conservation: every enumerated interface appears exactly once | yes | yes | yes |
| G8 | Interfaces held in use since plug-in | attributed once idle and readable — see [G8](#g8-in-use-interfaces) | same | same |
| G9 | Topology-read failure degrades safely | yes — becomes macOS semantics | n/a | n/a |

`AvailableConnections` is the union of the concrete interfaces observed for the published device;
`CompositeYubiKeyTests.AvailableConnections_IsUnionOfMembers` pins that structural rule. It is
transport availability only. It does not prove that every applet or capability is enabled over every
interface, nor that interfaces or operations are safe to use concurrently. Applet capability and
connection-ownership rules remain separate contracts.

"with topology evidence" is the normal Windows case. Topology reads can fail (stale devnode during
hotplug, `CR_NO_SUCH_DEVNODE`, missing ContainerId, API unavailable before Windows 8); when they
do, Windows behaves exactly like macOS/Linux — see G9.

## Pinning tests

Unit vectors: `Core.UnitTests/Devices/CompositeDeviceMergerVectorTests.cs`,
`FindYubiKeysFaultInjectionTests.cs`, `WindowsDeviceTopologyResolverTests.cs`.
Hardware invariants: `Core.IntegrationTests/Devices/CompositeDiscoveryIntegrationTests.cs`.

| Guarantee | Pinned by |
|---|---|
| G1 | `Merge_Regression_CrossKeyShapeB_TwoTripleKeysDisjointHidNoCcidNoSerials_MustStayStandalone`, `Merge_TwoSamePidTripleKeysAllSerialsKnown_GroupsBySerial_Pin`, `Merge_TwoSamePidDualKeysAllSerialsKnown_GroupsBySerial_Pin` |
| G2 (bound) | `Merge_EpistemicBound_ComplementaryPartials_TwoDualKeysOneInterfaceEach_MergeIsRepresentable_Pin`, `Merge_ComplementaryPartialMasquerade_MisattributionIsRepresentableAndBounded_Pin` |
| G2 (Windows closes it) | `Merge_ComplementaryPartialsWithTopologyKeys_SplitByTopology_NotMergedByPid` |
| G3 | `Merge_Regression_TwoTripleKeysFiveOfSixSerialsKnown_OrphanIsAttributedByPigeonhole`, `Merge_Regression_TwoDualKeysThreeOfFourSerialsKnown_OrphanIsAttributedByPigeonhole`, `Merge_TwoTripleKeysBothMissingSameInterfaceTypeSerial_StaysConservativelySplit_Pin`, `Merge_TwoSameTypeOrphansExceedAnchoredKeys_StayStandaloneInsteadOfDoubleAttribution_Pin`, plus cache convergence/eviction vectors in `FindYubiKeysFaultInjectionTests` |
| G4 (Windows yes) | `Merge_SeriallessPairWithDistinctTopologyKeys_GroupsIntoTwoCompleteKeys` |
| G4 (mac/Linux no) | `Merge_TwoSamePidTripleKeysNoSerialsFullVisibility_ConservativeSplit_Pin`, `Merge_TwoSamePidDualKeysNoSerialsFullVisibility_ConservativeSplit_Pin` |
| G5 | `Merge_ReconfiguredKeyReenumeratedUnderNewPid_GroupsByCurrentPidTruth_Pin`, `Merge_OneOfTwoKeysReconfigured_DifferentPidsNoSerials_TriviallyDistinguishable_Pin`; hardware: Phase 4 Tier 1 reconfiguration matrix (ISA) |
| G6 | `Merge_SingleInterfacePid_StandsAloneWithoutCompositeWrapper_Pin`; hardware: Phase 4 Tier 1 CASE 2 |
| G7 | `Merge_MixedTopologyAndSerialEvidence_IsDeterministicAndConserving_Pin`, `FindAllAsync_Conservation_EveryEnumeratedUsbInterfaceAppearsExactlyOnce` |
| G8 | Cache convergence / eviction / reader-rename vectors in `FindYubiKeysFaultInjectionTests` |
| G9 | `Merge_TopologyAbsentForAllInterfaces_IsByteIdenticalToPreTopologyBehavior_Pin`, `Merge_PartialTopology_KeyedInterfacesGroup_UnkeyedFallThroughUnguessed_Pin`, and the 13 failure-mode vectors in `WindowsDeviceTopologyResolverTests` |
| Overall (hardware) | `FindAllAsync_ZeroOrphansWhenIdle_DeviceCountEqualsPhysicalKeyCount`, `FindAllAsync_CompletenessPerPid_EveryDeviceExposesItsFullExpectedInterfaceSet`, `FindAllAsync_TwoConsecutiveScansOnOneManager_ReturnStableGrouping`, `ConnectAsync_TypedTransports_OnEveryReturnedDevice_Succeed` |

## Bounds explained

### G2: the epistemic bound

Implemented by `CompositeDeviceMerger.CanMergeByPidWithoutSerial`.

When two same-PID keys are only **partially visible** and their visible interfaces **complement**
each other, the descriptors are mathematically indistinguishable from one fully-visible key.
Example: two OTP+FIDO keys where only key A's OTP and key B's FIDO have enumerated — the observed
set is exactly what the PID promises, so tier 3 merges them into one composite spanning two
physical keys.

No merge logic can resolve this. Only serial or topology evidence can, and in these windows
neither exists for the interfaces that have not appeared. The canonical Rust implementation shares
this bound.

**When it can occur:** staggered USB enumeration during hotplug, the ~3 second reboot window after
a configuration change, or an interface failing to enumerate.

**How it heals:** on the first scan with complete same-PID visibility, serial evidence, or topology
evidence. Healing is conditional on evidence, not on elapsed time — an interface that stays absent,
busy, or unreadable keeps the window open. Discovery keeps scanning, but no scan is guaranteed to
heal.

**Blast radius:** connections are path-bound — a `PcscYubiKey` connects by reader name, a HID key
by device path — so a misgrouped composite never routes a connection to a different physical
interface than the one it names. The exposure is composite-level metadata and capability-filter
truth, not connection misdelivery.

**Rejected alternatives**, recorded so they are not re-proposed: (a) always requiring serial
evidence for every multi-interface merge — imposes seconds of first-scan latency on every
single-key user, the overwhelmingly common case, to defend a transient window on multi-key rigs;
(b) post-merge dual-interface serial verification — still best-effort, converts the window rather
than closing it, and doubles metadata reads.

### G3: convergence

Successful serial reads are cached per interface and evicted only when the interface disappears, so
knowledge accumulates monotonically across scans. In practice a long-lived `YubiKeyManager` reaches
complete grouping on the first scan and stays there.

Convergence is conditional: it completes **provided each interface is eventually idle and
identity-readable**. An interface held in use forever, or persistently unreadable, stays
conservatively split (see G8).

Reader-name renames are safe: a PC/SC reader-name change (`"... 01"` → `"... 02"` as the reader set
changes) is a cache miss, costing one re-read. A stale serial is never served under a rename.

### G4: serial-less keys

A key that reports no serial number offers protocol-level identity nothing to work with. If two
such keys are connected and each exposes multiple interfaces:

- **Windows** groups them correctly using Container ID — no serial needed, no device I/O.
- **macOS and Linux cannot group them at all.** Each interface is published as a separate device,
  permanently. This is a platform bound, not an implementation gap: neither platform offers a
  supported mapping from a PC/SC reader name to a USB device. On macOS, `TKSmartCardSlot` and the
  PC/SC reader name expose no USB topology, and the `" 02"` suffix is a driver-local enumeration
  artifact. On Linux, the pcsc-lite reader-name serial component comes from the USB
  `iSerialNumber`, which YubiKeys do not expose, and `SCARD_ATTR_CHANNEL_ID` is not a documented
  bus/address contract.

The v1 SDK had the same bound and never attempted CCID topology correlation on any platform.

### G8: in-use interfaces

Discovery **never disturbs an open session**: if an interface has a live connection, discovery
skips its identity read rather than competing for it. That interface therefore has no serial
evidence while it stays busy.

The guarantee is: an in-use interface is attributed **once it first becomes idle AND a subsequent
scan successfully reads its identity**; the cache retains that attribution thereafter, including
across later periods of use. An interface that is in use from the moment of plug-in and never
becomes idle is never attributed by serial — on Windows, topology attributes it anyway.

### G9: topology-read failure

Implemented by `CompositeDeviceMerger.MergeUsbByTopology` over keys supplied by `IDeviceTopologyResolver`.

Topology evidence is optional by contract. The Windows resolver returns a key it actually read, or
nothing — it never infers one. When it returns nothing, those interfaces fall through to tiers 2–5
unchanged, so Windows degrades to exactly the macOS/Linux semantics rather than to a guess. Partial
topology is safe as well: keyed interfaces group, unkeyed interfaces fall through, and no unkeyed
interface is ever pulled into a keyed group.

### Untrusted PID correlation

If one enumerated USB CCID reader name cannot be parsed to a known YubiKey PID,
`FindYubiKeys` sets `pidCorrelationUntrusted` for the **entire USB portion of that scan**. Topology
evidence still groups first. Every remaining USB interface is then eligible only for serial grouping:
interfaces with the same successfully read serial may merge, while failed or null serial reads stay
standalone. PID completeness and pigeonhole deduction are not used for those remaining interfaces,
because a reader-name drift on one CCID means the PID evidence cannot safely be assumed consistent
for the scan. `CompositeDeviceMergerTests.Merge_ForceSerial_MergesAllUsbBySerial_RejoiningUnparsedCcid`
pins the successful-serial path, while `Merge_NullPidUsb_NotForceSerial_StandsAlone` pins the
conservative standalone behavior outside it.

## Firmware note: CCID is not independently switchable on 5.8.0+

Measured on firmware 5.8.0: FIDO2 and U2F are exposed over CCID as well as HID, so **disabling the
CCID-exclusive applications does not remove the CCID interface** while FIDO remains enabled. The
key continues to enumerate with its CCID interface present. Plan accordingly when reasoning about
reconfiguration: the enabled-capability set determines the USB PID, but the mapping is not one
application per interface.

## What is not covered

- **Cross-process contention.** All coordination is in-process. Another process holding a PC/SC
  reader appears to this SDK as an in-use interface (G8).
- **NFC readers.** NFC interfaces never merge into USB composites; they stand alone by design.
- **macOS/Linux HID-to-HID topology.** Both platforms can correlate HID interfaces to a parent USB
  device (IOKit `locationID`, udev parent walk), which the v1 SDK used. This is not implemented
  here because it cannot include the CCID interface, so it does not close G4 for FIDO+CCID-shaped
  hardware. It remains available as a future refinement for multi-HID serial-less keys.
- **Windows hardware validation of the topology tier.** The decision tree, every failure mode, and
  the merger's use of topology evidence are proven at seam level with scripted native operations.
  The `SCardGetReaderDeviceInstanceIdW` marshalling and real ContainerId matching across a physical
  YubiKey's interfaces await validation on Windows hardware; see the composite-merge ISA.
