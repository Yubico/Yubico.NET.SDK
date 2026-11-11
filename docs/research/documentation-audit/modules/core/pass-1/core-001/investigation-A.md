# Packet A investigation

**Source:** `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. **Investigator:** GPT-6.1 Sol.
**State:** complete primary and assigned-shared reading; candidate triage and fresh independent review
remain pending. This is an investigation return, not accepted findings, an editing authorization, or
module closeout. `investigation/A/unit-results.json` keys every exact assigned ID. Primary reading covers
all 91 files / 16,728 lines, including complete test bodies and local helpers. All assigned shared ranges
were read with their enclosing ownership/helper context; unrelated external behavior retains its owner.
Narrow dependency reads have no disposition authority over B/C.

The actual packet contains 1,630 primary IDs and 376 shared IDs. The prose assignment's 374 shared count
is two lower; the packet also includes an SVG/PNG derived pair. The exact IDs govern this return.
No central inventory, coverage, source index, product file, assertion, or historical record was changed.
No SDK build, test, formatter, native/device operation, package import, commit, push, or subagent ran.
Python work captured originals and analyzed record/source identities only.

## 00. Find, watch, update, and troubleshoot

A short command uses one fresh Find operation. `YubiKeyDiscovery` constructs a scope-specific finder,
awaits discovery, and retires that read scope in `finally`. Returned connection slots remain usable.
`interfaces` limits backend construction, descriptor processing, topology queries, protocol probes, and
returned slots. `IFindYubiKeys.FindAllAsync(type, ...)` instead filters the merged result of that finder's
fixed scope. First-result methods finish that whole scan before evaluating the predicate. They select
the first matching result in unspecified order and leave insertion waiting to a watch stream.

A tracking application owns one `YubiKeyMonitor`, or uses the manager's enumeration-owned convenience
stream. The monitor constructs listeners before launching its sole scan/publication loop. Its factory
awaits the loop's first publication, including loaded-empty success. A creation token governs that wait
and partial-lifetime cleanup; a successfully returned monitor has its own lifetime token. Each watcher
captures membership and registers under the publication gate on its first move. It receives additions,
one `SnapshotComplete`, and its independent live tail. Revisions group scans, including unchanged scans;
device properties remain last-known state on retained references rather than frozen event payloads.

The producer never waits for a watcher. A full 256-change delta buffer terminates that subscription;
captured replay and buffered history can drain before the fault surfaces. A consumer clears its
projection and subscribes again to the same healthy monitor. Canceling or disposing a subscription
unregisters it. Manager enumeration disposal also disposes its private producer. The tests explicitly
exercise a throwing consumer body and verify private-monitor cleanup. Sharing a monitor makes these
ownership boundaries useful for several views without several sets of listeners.

When a scan retains the same member/connection sets and sees no contradictory known serial, it retains
the published object. Changed non-null metadata replaces its snapshot and produces `DeviceUpdated`;
null preserves the previous successful snapshot. A different known serial replaces the object with a
removal followed by an addition. A changed member set, changed connection set, or scan-observed absence
also ends retention. Independent Find calls neither update nor force-refresh a monitor's publication.

| Symptom | Supported action and evidence boundary |
| --- | --- |
| No match now | Handle null/exception, or watch for later membership; Find does not wait for insertion. |
| Metadata missing or historical | Use an explicit live Management read and handle contention; the getter performs no I/O. |
| One-shot transport timeout | Inspect the warning and repeat fresh discovery when the service recovers; that call can return other interfaces. |
| Later monitor scan fails | Keep the prior projection while the loop logs and retries on a later trigger. |
| Watcher overflow | Clear that projection and re-enumerate the same monitor. |
| Terminal loop error | Dispose the monitor and create another owned lifetime. |
| Cancellation returned but key remains owned | Await actual native cleanup/recovery; cancellation and timeout retire waits rather than prove native release. |

This journey is source-backed. Fresh execution and physical qualification remain separate. The cached
snapshot's buffer-ownership exception in CORE-A-C003 needs adjudication before strengthening stability prose.

## Local candidates and reopening map

These IDs are packet-local candidates, not accepted `CORE-001` or `CORE-DOC/IMP` allocations. Inherited
aliases remain run-qualified and nonadditive. Every candidate requires a fresh editor and independent
full-unit/citation review. Candidate-associated unchanged source/test paths retain explicit dispositions
in the packet records; none are silently counted as edits or resolved by a rename.

### CORE-A-C001 — Qualifier iteration limit (documentation)

`VersionQualifier.cs:35–48` repeatedly specifies `int.MaxValue`. Its constructor at 51–60 accepts the
inclusive unsigned 32-bit range. `DeviceInfoFieldLengthTests.CreateFromTlvs_QualifierIteration_UsesFullUnsignedWidth`
asserts `uint.MaxValue`; the overflow vector supplies five nonzero-leading bytes. Document `0..uint.MaxValue`
while retaining the public `long` parameter/property shape. Reopen that XML and the metadata field
description whenever the accepted numeric range or wire decoding changes. No runtime fix is needed for this mismatch.

### CORE-A-C002 — Explain public metadata and flag carriers (documentation)

`DeviceInfo`'s type and most properties have no XML describing purpose, units, defaults, provenance, or
interpretation. `ConnectionType`, `Transport`, and `Feature` similarly leave important public values or
semantics implicit. Existing guide summaries cannot substitute for usable member reference. Provide
focused contracts: supported versus enabled applications, manufacturing flags versus reported approval,
seconds for both timeouts, missing-field defaults versus observed false/zero, component versions,
effective firmware and qualifier precedence, unknown enum values, and the borrowed-buffer caveat below.
Keep `DeviceCapabilities.All` explicitly the union of currently modeled application bits, rather than a
claim to every future bit or every enabled application. Preserve open numeric values; do not turn docs
work into validation changes. `ConnectionTypeExtensions`, `PhysicalYubiKeyTests`, mapper tests, and
field-length tests provide current behavioral oracles. Reopen the public XML, Core guides, shared filters,
CLI labels, and Management reference after any decoding/default/carrier change.

### CORE-A-C003 — Returned metadata retains caller/TLV-owned bytes (implementation + documentation decision)

`DeviceInfo.CreateFromTlvs` stores `ReadFixedField(..., 0x07, 1)` directly as `ChallengeResponseTimeout`
(`DeviceInfo.cs:137,165,193`). `Tlv.Value` returns a view into its owned array; `Tlv.Dispose` clears that
array (`Utilities/Tlv.cs:65,81–88`). Therefore disposing the input TLVs changes the returned DeviceInfo's
timeout bytes. Equality and the content-based hash then depend on the input's lifetime. The public init property can likewise
receive caller-owned mutable memory. This is metadata ownership, not a secret-zeroing defect.

`DeviceInfoTests.Equals_SnapshotsDecodedFromSeparateBuffers_CompareByContent` checks before its using
declarations dispose inputs. It does not test returned-value stability after input disposal. `DeviceInfoReader`
currently leaves decoded page lists undisposed, so adding `using` mechanically would change successful
results. Decide an independently owned scalar/copy or an explicitly borrowed input lifetime in separately
scoped implementation work. Coordinate with C's TLV owner; A makes no disposition of C units.
Reopen `CreateFromTlvs`, equality remarks, `IYubiKey.CachedDeviceInfo`, Core README, discovery guide,
physical model, Management return/reference examples, and any proposed reader cleanup when ownership is
settled. Preserve the supported narrower fact that replacing a device's snapshot does not mutate the
previous struct's scalar fields. A pure managed disposal/hash vector is proposed below, unexecuted.

### CORE-A-C004 — FIPS application masks use the ordinary app decoder (inherited implementation candidate)

Aliases: `management-001:MGMT-XREF-001` and
`audit-v2-20261022-20261031:deviceinfo-FIPS-residual`, one nonadditive group.
`DeviceInfo.cs:153–154,232–239` routes 0x14/0x15 through `FromApp`; its separate `FromFips` maps
01→FIDO2, 02→PIV, 04→OpenPGP, 08→OATH, 10→HsmAuth. A one-byte 02 presently becomes U2F.
The pinned Python parser uses its FIPS-specific mapping for both fields. `CapabilityMapperTests`
tests the unused helper directly using two-byte buffers; `DeviceInfoTests` supplies ordinary application
encodings for these FIPS fields. Those oracles do not verify the actual FIPS-tag integration.
Simply switching helpers would still mishandle nonzero one-byte values because `FromFips` reads Int16.

The public vendor configuration reference has no 0x14/0x15 definition. Initial research and one focused
follow-up did not establish authoritative current tag widths/mapping. Keep this precise wire-source gap;
the Python implementation and local dead helper are strong implementation evidence, not certification
authority. Reopen Core metadata, shared FIPS filters, CLI display qualifications, and Management FIPS
tests/docs after mapping/width work and integrated nonzero vectors. Physical FIPS approval remains a
device/profile/operation question even after decoding is fixed.

### CORE-A-C005 — Flagged form factor remains an unnamed base-enum value (inherited implementation/representation decision)

Alias: `management-001:MGMT-XREF-002`. At `DeviceInfo.cs:142–146`, 0x81 produces `IsFips=true` and
`FormFactor=(FormFactor)129`, whereas the named base enum's USB-A keychain is 1. The manufacturer's
configuration table explicitly assigns 0x41/0x81 to Security Key/FIPS USB-A keychains on firmware 5.4+;
the technical manual independently lists the FIPS values. Python masks the low nibble. No current
DeviceInfo fixture verifies a nonzero product-flag byte. Shared exact enum filters can miss these keys;
labels may fall back to Unknown. Decide normalized plus preserved raw representation, then reopen
FormFactor/DeviceInfo XML, CLI formatters, shared fixture filters, Core guides, and Management examples.
Preserve unknown values deliberately instead of silently adopting Python's unknown-to-zero policy.

### CORE-A-C006 — Wide capabilities accepted but decoded from the first Int16 (inherited implementation candidate)

Alias: `audit-v2-20261022-20261031:deviceinfo-wide-app-bits`.
`ReadCapabilities` accepts nonempty variable widths; `FromApp` reads the first two bytes whenever length
exceeds one (`DeviceInfo.cs:232–239,353–359`). Thus 00 00 10 loses the trailing PIV bit; 80 00 sign-extends
through the signed Int16 into the int-backed enum. The current wide fixture contains only zeros, while
ordinary mapper vectors use at most two bytes. Python consumes the whole unsigned big-endian integer.
The vendor table establishes bit values but not an unbounded future-width promise. Decide supported
numeric width and raw preservation, and test trailing nonzero and high-bit vectors. Reopen metadata
XML, `All` wording, shared capability filters, CLI labels, and Management capability examples after that
decision. This is distinct from fixed-field guards and FIPS remapping.

### CORE-A-C007 — Interface XML still describes pre-feed events/ownership (documentation)

`IYubiKey.cs:49–52,90–97,122–124,155–158` describes a live repository/manager and metadata/serial
replacement without any device event. Current `SnapshotDiff` emits `DeviceUpdated` when retained
metadata changes, including learning a serial. `DeviceIdentityContractTests.Apply_LateSerialArrival_PopulatesRetainedObjectAndEmitsUpdate`
asserts the event. Scope retention to one owned monitor and describe updates through its change feed.
At 70–74, explain that the separate Hid group bit and All sentinel are absent, while both concrete HID
bits may be present together. A reader should not infer that FIDO+OTP cannot coexist. Reopen interface
XML and the physical-model summary after any update/serial/retention change; the newer identity guide
already describes the event and is a consistency reference, not a reason to preserve the stale XML.

### CORE-A-C008 — Read-budget/retry/internal rationale overclaims (documentation)

`DiscoveryBudgets.ScanUpperBound` is the arithmetic 5+2+3 second sum, not a scan completion bound;
identity retries, backoffs, scan-lock queueing, synchronous topology, and logging lie outside that sum.
Its XML should name the sum precisely. `DiscoveryIdentityReader` retries every ordinary exception up to
three attempts, not just classified transient PC/SC failures; cancellation propagates, skips return null,
and a timeout ends the attempt sequence. A timeout can reflect worker/PID admission or slow teardown,
so it does not prove a busy card. `SharedDeviceInfoRead` comments at 145–151 narrate old Phase-0 history;
`DiscoveryReadScope` says replacement manager; `ProtocolDeviceInfo` comments narrate an upstream change.
Describe current finder/admission/borrowed ownership and the retained release constraint instead.
The old Task.Run/SCardTransmit explanation in `DiscoveryIdentityReaderTests:62–67` predates the current
owned PC/SC lifetime. `FindYubiKeysFaultInjectionTests:252–254` says invalidation happens at event ingress;
the actual monitor does it after coalescing immediately before the scan.

`DiscoverySingleFlightTests`, `DiscoveryReadSchedulingTests`, `FindYubiKeysScopeTests`, and the actual
retry helper establish different contracts; keep their oracles distinct. Reopen local comments/XML and
the budget/monitor architecture sections when scheduling, retry classification, or cleanup changes.

### CORE-A-C009 — Topology claims need implementation and OS qualification (documentation/qualification)

`FindYubiKeys.ResolveTopologyKey` says the call never blocks meaningfully. The Windows path performs
synchronous context establishment, reader-instance queries, property resolution, and context disposal
without an SDK wait budget. Catching errors provides fallback, not a latency bound. The Windows reader
API documents the null-buffer length probe and Windows 8 minimum; it also explicitly fails under Remote
Desktop redirection with `SCARD_E_READER_UNAVAILABLE`. Container-ID documentation supports matching
devnodes in a container, not fresh hardware attestation on this host.

The null resolver and its test establish that this SDK supplies no topology keys on Linux/macOS.
Claims that no supported OS mapping exists anywhere, or that those platforms cannot group HID siblings
at all, exceed that oracle; the guarantees guide itself acknowledges HID-parent topology as unimplemented.
Qualify the current implementation and supported evidence instead. Reopen topology XML/comments, G4/G9,
platform troubleshooting, and Windows test summaries after actual native/OS evidence or implementation
changes. Preserve the Windows readiness handoff and existing hardware gap; no other branch was inspected.

### CORE-A-C010 — Merge/test explanations carry stale TODOs and stronger oracles than assertions (documentation)

`CompositeDeviceMergerVectorTests:338–347` still requests the topology vector already implemented at
414–442. Retain the completed test's real reason, rather than an unfinished Phase-3 instruction.
The same file calls DeviceId durable at 768–770, treats a complete contested census as necessarily live
at 629–632, and repeatedly narrates Phase/WAS/orchestrator decisions. The implementation expressly calls
that census a heuristic that can choose stale membership. The mixed-evidence test verifies conservation
and membership for one input order, not permutation determinism. The absent-topology test compares
specific expected shapes rather than serializing byte-identical old/new implementations. Preserve method
names/assertions and narrow explanatory prose to what is actually asserted. `CompositeDeviceMergerTests:67`
also says the single-interface SKY key is keyed by PID despite asserting its transport-shaped ID.
Reopen full merge test units, the guarantees pin table, and internal merge rationale after any rule/oracle change.

### CORE-A-C011 — Global grouping assurances conflict with the accepted partial-evidence bound (documentation)

`device-discovery-guarantees.md:36–37` says incomplete grouping is never wrong, while its G2 section and
the full complementary-partial vectors deliberately demonstrate cross-key composites. The introductory
hierarchy should state evidence-based conservative behavior with the G2 condition nearby. At 372–375,
path-bound slot routing is true, but saying the exposure excludes connection misdelivery can mislead a
consumer selecting by composite serial: a slot can faithfully open key B while the composite metadata
identified key A. Explain both routing and caller-selection consequences without reopening the accepted
merge-policy decision. `physical-device-model.md`'s one-key headline needs its existing common-case
qualification near the first claim. Reopen these narrative units and selection guidance if grouping,
serial-to-slot binding, or post-merge verification changes.

### CORE-A-C012 — Hardware-test descriptions exceed their actual oracles (documentation + test adequacy)

The integration files were read in full; none ran. `CompositeDiscoveryIntegrationTests:34–36` still
expects pre-Phase-2 red results. Its independent enumeration uses production transport enumerators and
a copied PID/name policy: it is independent of the merger, not an independent hardware census. Counts
by type cannot detect an equal-count substitution. The test claims allow-listed fixtures, yet several
cases enumerate/open every returned key directly. Describe the actual idle USB rig and ownership scope.

`DiscoveryUnattendedTests` says no applet session is opened. Its discovery still opens internal metadata
connections, selects Management for PC/SC, and can affect selection state on a shared/direct handle.
Its subset assertions constrain interface bits rather than prove membership inclusion; its initial
revision=1 checks assume no intervening publication. `YubiKeyTests.WaitForChangeAsync<DeviceAdded>`
can succeed on initial replay, so the current description does not prove a new insertion after a prompt.
The two stronger attended protocols assert removal/reinsertion plus final healthy stream completion,
but no execution proves a physical/native lane here. Their success-path pump waits also do not establish
failure-path pump drain. Preserve these as oracle/qualification limits, with any assertion/harness changes
in a separately authorized scope. Reopen test XML/comments, discovery hardware instructions, guarantee
test names, and historical evidence wording after harness changes or a new exact OS/device run.

### CORE-A-C013 — Shared fixture instructions still claim pre-operation hard failure (documentation)

Shared read: `YubiKeyTestInfrastructure.InitializeDevicesAsync` performs fresh All discovery, then a
live `device.GetDeviceInfoAsync()` before applying the allow list; it snapshots that separate live result
in `YubiKeyTestState`. It does not reuse `CachedDeviceInfo`. Nonmatches and read failures are filtered;
no authorized results return an empty list. `AllowList` exits only for an empty configured list with
unknown-serial authorization disabled. The README and contributor examples claiming exit on every
unauthorized serial and no hardware operation before authorization need precise distinctions between
read-only identification and test-body authorization. This observation does not authorize a new gate.

`IYubiKeyFilter` still shows nonexistent `YubiKeyTheory`/`YubiKeyTestDevice`; `YubiKeyTestState.Placeholder`
still describes re-enumeration rather than first-match lazy binding. The fixture cache remains run-local
and independent of one-shot manager evidence. Alias `management-001:MGMT-XREF-003` is a reopened
documentation dependency, not a newly counted runtime bug. Reopen these exact shared units and any
selected hardware authorization prose with the SHARED editor. Preserve earlier resolved applet prose.

### CORE-A-C014 — Malformed firmware filter policy remains an inherited decision

Alias: `management-001:MGMT-IMP-009`. `FirmwareVersion.FromString` requires three components;
`FilterDevices` silently leaves filtering unchanged if parsing fails. The current WithYubiKey XML
already documents malformed strings being ignored. C# source confirms this policy; no test execution
or decision to change it occurred. Keep the specific Management two-component criterion for its later
boundary owner, and settle strict handling separately. Reopen filter XML/examples and the caller's
coverage claims after a policy or criterion change. Avoid duplicating this group as a new accepted bug.

### CORE-A-C015 — Development firmware behavior is SDK policy, not hardware fact (documentation)

`FirmwareVersion.cs:46–74` says every alpha/beta key is based on 5.8.0 or later and tells readers to
suspect the key rather than command encoding. The code only knows `Major == 0`; `FeatureTests` pins
optimistic acceptance even for 0.0.0 and arbitrary major-zero values. `Default` also represents unknown
0.0.0. State that compatibility policy, numeric ordering, and its inability to prove command support.
DeviceInfo may replace the behavioral version using a non-final qualifier; the supplied default version
otherwise wins over a well-encoded firmware TLV in C#, while the pinned Python parser prefers the TLV.
That cross-SDK precedence difference is implementation policy, not a wire violation by itself.
Reopen firmware/feature XML, metadata reference, and applet gate guidance after a policy or effective-version change.

### CORE-A-C016 — Metadata raw preservation requires a scoped design decision

`CreateFromTlvs` ignores unknown tags and preserves some unnamed enum values, but publishes no complete
raw response or unknown-TLV collection. Duplicate tags fail through `ToDictionary` with an ordinary
argument exception; the declared BadResponse condition is therefore not exhaustive. Unknown qualifier
types are retained numerically and treated as non-final. The root forward-compatibility doctrine asks
for raw response escape hatches where practical. Decide this metadata-specific representation and error
contract separately; do not add fields or reject open values during Pass 1. Reopen metadata construction
XML, Management returns, diagnostic examples, and cache equality after a decided representation change.

## Primary-source verification

`investigation/A/sources/captures.json` retains exact URL, UTC access, HTTP metadata, SHA-256, byte count,
and original bytes. `sources/notes.md` identifies the consequential passages and their authority limits.
The public manufacturer reference is unversioned and includes stale material (0x0100 listed unused and
the NFC write tag typo); it is used only for the identified response fields, timeout units, and
flagged-form-factor table. The technical manual corroborates FIPS form factors, not the undocumented
FIPS response-mask layout. Neither publication is an RFC-style MUST/SHOULD conformance profile.

Context7 was used for Channels and Windows property/API orientation. The .NET runtime source is pinned
to tag `v10.0.0`; its `TryWrite`/completion implementation supports the feed's bounded-channel reasoning.
Microsoft API reference explains the native reader probe and supported platforms; this is API contract
evidence rather than an executed Windows result. Python metadata comparison reads the locally retained
`4ca60f706af930459138d8dc0f0f953480e1c7a4` source identified by C's shared manifest. No duplicate repository
fetch or Python import occurred. Older Rust/canonical claims in shared narratives still need their own
exact original verification; a current architecture claim alone does not close that source dimension.

### CORE-A-C017 — Metadata cache does not carry the identity cache's PID qualification

`FindYubiKeys.ReadIdentityAsync` checks the cached PID before using identity. Its metadata cache instead
stores only `Info` and member interface IDs, and `PopulateMetadataAsync:443–446` reuses it without a PID
check. With a stable single FIDO interface ID changing from 0120 to 0402, neither PID needs serial
disambiguation, and the same metadata key can therefore reuse the old value without hotplug/absence.
The existing PID-change fault test deliberately forces the identity path with two OTP instances; it
does not check this metadata-only path. Normal platform reconfiguration often changes interface IDs,
so this is a source-visible conditional limitation, not a demonstrated hardware failure.

Classify this as an implementation candidate and a documentation qualification: the physical-model
statement that successful identity/metadata evidence expires on PID change needs to distinguish the
two caches. Reopen finder cache comments, physical-model discovery, cached-metadata troubleshooting,
and the PID-change regression description after a cache-validity decision or stable-ID test.
Coordinator experiment A-E04 can use one scripted interface, two known FIDO PIDs, a constant interface
ID, changed serial/capabilities, and no activity notification; add a hotplug invalidation control.
No experiment ran.

## Test adequacy and proposed bounded experiments

### CORE-A-C018 — Toolchain same-kind filter translation needs a precise contract

The assigned `TranslateToMtpFilter` unit says ANDed method patterns are combined into one wildcard.
Its implementation instead appends a separate `--filter-method` argument for every term. The same
comment says repeated same-kind switches are ORed. These two source statements and the implementation
cannot all support the advertised same-kind AND semantics. Treat the prose correction as a candidate
and actual execution selection as a separate implementation question. Context7's current xUnit
documentation confirms simple versus query filters and multiple same-kind input, but did not settle
the exact repeated-switch behavior of the repository's xUnit 3.0.0 package in bounded follow-up.
Do not silently rewrite the command or run a runner. Coordinator A-E05 can test a pure translation
vector and list a tiny standalone runner's matches, using pinned package identity. Reopen the method
comment, TOOLCHAIN filtering instructions, and testing examples if the actual accepted grammar changes.

### CORE-A-C019 — Shared support instructions need current scope

`TOOLCHAIN.md:246–254` still describes packable paths as `Yubico.YubiKit.*/src/*.csproj`. Current libraries
sit under `src/<Module>/src/*.csproj`, and the assigned native-AOT discovery code enforces that depth;
ordinary `ProjectDiscovery.DiscoverProjects` searches recursively with substring predicates, rather
than those literal old-layout globs. Describe the actual current layout and direct callers to the
toolchain's discovered project names. Test projects include integration selection through the relevant
target; the unit-only example should not be read as the whole project population.

`TestCategories.PermanentDeviceState` at 83–90 says only factory reset, with physical reinsertion within
ten seconds, undoes permanent state. Its examples concern CTAP settings, while the shared category name
is broader. Qualify the application/profile and reset sequence with the later CTAP boundary owner;
Core's monitor and disposal supply no generic device-reset/reinsertion contract. Preserve executable
trait strings and the existing user coordination rules. Reopen these units after discovery-layout or
owning applet-reset policy changes. This is documentation/qualification work, not permission to run reset.

### CORE-A-C020 — Comparator and historical evidence need explicit provenance boundaries

The identity narrative cites `yubikey-manager-rust-auto` abbreviated commit `7d7a7455` and paths under
`crates/` and `packages/`. Those are not the paths of C's independently pinned public Python source.
No verified original repository URL/full commit was established. The exact named local reference path
was unavailable; keep the passage-level claims unverified rather than replacing that comparator with
current Python. This is a source gap attached to read narrative units, not an unread unit or a rejection
of the C# retention policy. A fresh citation reviewer needs the original repository/full pin and those
passages before retaining the cross-language comparison as verified authority.

The AOT data's Windows/Linux discovery sections describe older physical runs; the macOS section names
host `839e3c05...`, old observable sinks, and a static restart that the current monitor has removed.
Their descriptions were read, but this packet did not reacquire the original binaries/logs or establish
missing Windows/Linux source identities. Keep these records historical. In particular, the text that
the old fan-out invariants are "now measured across watchers" needs a new exact watcher-host run or a
prospective qualification. It cannot inherit execution credit from the old observable host. The L5
handoff's explicit managed/fake-native scope remains appropriate. Reopen source/cross-language and
qualification wording when original artifacts or a new exact run arrive; preserve old failed/skipped
outcomes rather than rewriting the historical result.

### CORE-A-C021 — Successful serial-less identity reads are cached despite retry prose

`FindYubiKeys.cs:84–86` says a serial-disabled read is absent from the identity cache and retried next
scan. `ReadIdentityAsync:380–381` actually caches every non-null DeviceInfo, including one whose
SerialNumber is null, and reuses it at 371–372 when the PID is unchanged. The guarantees guide at
39–44 likewise promises retries for null serials. Successful metadata without a serial is different
from an exception/timeout; the latter really is retried and has dedicated fault-injection assertions.
Those fake response helpers always encode a serial and do not test this successful-serial-less arm.

Record a documentation correction and an implementation-policy decision rather than assuming every
serial-less device should be reprobed forever. One-shot Find already uses fresh evidence. For an owned
monitor/finder, decide when a successful serial-less observation should expire, preserving conservative
grouping and native admission. Coordinator A-E06 can supply two same-PID scripted interfaces with
successful metadata omitting tag 02, count opens across scans, then provide serial-bearing responses
with and without an activity invalidation control. Reopen finder cache comments, guarantee convergence,
serial-latch XML, and troubleshooting when the caching policy or that oracle changes. No experiment ran.

## Shared caller and support results

The CLI selector discovers All once per attempt, filters complete device references, and consults the
independent serial latch before metadata. Each selection invocation has a reference-keyed enrichment
cache, including failed enrichment; a new invocation observes a new cached snapshot and can try live
fallback again. The tests use real prompt/filter code with substituted discovery/live reads and explicitly
check sequential enrichment, duplicate labels, transport policy, cancellation, and no extra live read.
They supply no physical selection/native evidence. `ConnectionTypeFormatter` prints concrete combined
bits; `FormFactorFormatter` maps exact named base values and falls back for flagged/unknown values,
which is a concrete C005 reopening dependency. Its model-like labels are presentation policy, not
independent proof of product family.

`YkCommandBase` carries `selection.Info` and the requested transport into `YkDeviceContext` without an
extra read. The context returns fresh SessionCreationOptions and explicitly live Management data,
returning null on ordinary failure rather than substituting the selection snapshot. The complete
CommandMetadataTests helper limits responses to SELECT and device-info instructions, verifies disposal,
and checks live-read failure/cancellation, stale-output avoidance, requested FIDO, and reset's live
firmware prerequisite. These are command-path assertions under a SmartCard fake, not hardware claims.

All assigned command bodies were read, including implicit factory calls and enclosing `await using`,
credential/configuration scopes, and `finally` cleanup. FIDO and OTP bodies pass the context options;
the selected SmartCard-only OATH/OpenPGP/HsmAuth and most PIV bodies use their single-transport
convenience factory. Each uses one session at a time. PIV info uses the selection snapshot only for
firmware/identity display; Management info/config/reset explicitly read live first. A's clean outcome
for those caller ranges is limited to selection, metadata, session ownership, and transport propagation.
It does not accept application reset wording, PIN policy, protocol command semantics, or secret-handling
claims outside that boundary. In particular, applet boundaries should retain review of early parsing
before `finally` in PIV/OTP setters and certificate/private-key helper lifetimes; no source fix or extra
applet finding allocation was made here.

The PublicApi helper ranges were read with their full reflection-helper context. They enumerate applet
IYubiKey extensions and check factory/one-shot shape or marked assembly identities. Those tests are
source-defined conventions, not this packet's executed compiler/API-binding evidence. The architecture
Mermaid source, map entry, full PNG and SVG labels agree on fresh scoped Find and owned-monitor
composition. Historical migration sections retain historical status; current migration examples use
named cancellation and explicitly owned monitoring. The historical gap survey's claimed full metadata
parity is not fresh validation of C004–C006.

- **Replay/overflow/consumer failure:** full feed/monitor/manager-watch assertions establish isolated
  managed subscription behavior under scripted discovery. The 50-iteration race accepts either whole
  publication boundary; it is bounded race sampling rather than a proof of all schedules.
- **Clock scheduling:** `ObservedTimeProvider` records actual timer registration before tests advance
  fake time. Initial trigger, quiet window, combined consumed hints, coalescing cap, completion,
  cancellation, and interval reset are asserted. The capacity-one listener channel deliberately drops
  additional writes; consumed flags are combined, and global invalidation makes dropped flag detail safe.
- **Initial/later failure:** tests stage initial scan failure and identity-access publication failure;
  later scan, fake timeout, and pre-publication diff failure preserve membership and retry on the next
  hint. Invalidation failure is terminal. These tests do not establish arbitrary logger-failure recovery.
- **Admission/native ownership:** fake gates hold open/disposal; scope tests prove no queued opens after
  retirement and leases through late completion. `ScopeRig` returns SmartCard fake connections even for
  HID slots, so these assertions prove scheduling/routing scope, not HID framing or native close semantics.
  Platform-specific release proof remains with B and the qualified historical integration evidence.
- **Borrowed reader cleanup:** the full FakeSmartCardConnection helper increments DisposeCount in sync
  Dispose, while DisposeAsync is a completed no-op. DeviceInfoReaderTests' zero-count assertions therefore
  detect synchronous disposal, but would miss an accidental asynchronous disposal call against that fake.
  The current production borrowed-reader path does not dispose the connection; strengthening this oracle
  belongs to separately authorized test work, with B retaining helper ownership.
- **Privacy:** field-length malformed-qualifier test checks the sentinel `DE` is absent from that exception.
  Completion logging tests check observer count/device/connection text. Neither is a general log-privacy audit.
- **Coordinator experiment A-E01, unexecuted:** construct a one-byte timeout TLV plus firmware, decode,
  capture bytes/hash, dispose inputs, compare again; include independent caller-memory mutation control.
  This should be a standalone managed host referencing the reviewed Core output, with source/output pin.
- **A-E02, unexecuted:** compare nonzero FIPS 02, flagged 81/41, application 00 00 10 and 80 00, and
  unknown qualifier 7f against explicit expected/current results. Record policy versus original-source
  authority; no hardware is needed and the result cannot certify physical FIPS operation.
- **A-E03, unexecuted:** failing logger at scan-applied or listener-teardown boundaries to determine
  whether publication/cleanup guarantees need narrower wording. Keep diagnostics failure separate from
  physical enumeration failure; acceptance of a changed behavior belongs to a later scope.

No experiment was executed. Compiler/consumer shape remains `VALID-NOT-CHECKED`. Baseline Linux
integration evidence retains its recorded 38 native checks, JIT/AOT lanes, and zero physical-device
qualification; this packet adds no new execution. Earlier PIV source evidence predates the Core changes.
The user-deferred PIV physical work and Windows late-handoff checkpoint remain untouched.

## Inherited outcomes and remaining work

- `audit-v2-20261022-20261031:#21`: present fixed-field widths, nested qualifier fields, missing-firmware
  handling, and unsigned qualifier range have current source/assertion guards. Preserve exact-scope source
  resolution, separate from mapping and memory-ownership candidates.
- `audit-v2-20261022-20261031:25b`: the page=255 continuation guard prevents wrap. The packet's reader
  tests exercise ordinary continuation and duplicate markers, not the 256-page endpoint; this is a source
  resolution with an explicit regression-oracle gap, not a claim of fresh test execution.
- `fixes-wave-001:CORE-001` and `audit-v2-20261022-20261031:multi-key-exclusions`: current scope retirement,
  admission, and native-owner transfer change the mechanism. The serial-9681620 failure remains unexplained;
  neither fakes nor architecture changes identify a physical cause. Keep its original historical identity.
- `management-001:MGMT-IMP-008`: Core's Feature remains optimistic for major-zero versions; actual
  Management legacy-read/factory enforcement is for the later boundary packet. No applet disposition here.
- The other inherited aliases are linked above without adding duplicate bug counts.

Primary clean sampling nominees: the complete `ConnectionTypeMapperTests` unit (mapping/filter truth
table); `RescanSchedulerTests.RunAsync_ContinuousSignals_YieldsAtCoalesceCap` plus complete
`ObservedTimeProvider`; `DeviceChangeFeedTests.Apply_Overflow_FaultsOnlySlowSubscriberAndAllowsReplayRecovery`;
`SnapshotDiffTests.Apply_DifferentKnownSerials_RepublishesWithoutMutatingPredecessor`; and
`FindYubiKeysScopeTests.FindAllAsync_ExitRetiresQueuedIdentityReadsBeforeLaterWorkerAdmission` plus
complete ScopeRig. The coordinator chooses actual unit IDs/strata from the keyed result record, excluding
candidate-associated edits. These are nominations, not completed independent sampling.

Every assigned primary/shared unit now has a read-clean or candidate disposition. The record's original
164-unread checkpoint remains a documented intermediate state; its successor contains no unread IDs.
Original Rust and historical platform source gaps remain attached to C020; current xUnit selection
semantics remain C018's bounded gap. Astra should triage the local candidates, reconcile boundary-owned
questions and inherited aliases, and assign fresh editing/review before accepting changes. The new
observations add no execution or physical qualification and do not close the old two-key blocker.
