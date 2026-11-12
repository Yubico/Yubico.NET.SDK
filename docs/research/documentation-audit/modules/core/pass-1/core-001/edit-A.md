# Packet A C# documentation edit return

## Outcome and boundary

The released, source-supported packet-A C# documentation batch is ready for independent review in
`/home/rupicola/Code/y/yubikit-core-docs-edit-discovery`, branch
`yubikit-core-docs-edit-discovery`. **36 C# files edited; 54 retained; all 21 local candidates explicitly
dispositioned.** This is an editor return, not independent acceptance, whole-Core publication, runtime
repair, or a claim that the nine applet boundaries are closed.

Worktree base/HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`. Source comparison pin:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. All 90 packet-A C# files have identical product bytes at
those two commits. Only full-line C# comments/XML changed; the patch tool also added final newlines to
some previously unterminated files, recorded per path. Every non-comment line, including blank lines,
directives, imports, declarations, defaults, runtime strings, inline comments and assertions, is preserved.

No primary-checkout, other-worktree, shared guide, applet, project/configuration, API snapshot, original
investigation/triage/coverage/archive, or historical integration record was edited. No build, test,
formatter, compiler, device/native/crypto execution, index/ref write, commit, push or subagent ran.
No runtime scratch was needed. No Pass-2 plan or implementation was started.

## Transfer and review artifacts

All artifact paths below are relative to this run directory.

| Artifact | Meaning |
| --- | --- |
| `editing/A/product.patch` | Complete transferable product diff, with conventional `a/` and `b/` prefixes; excludes editor records. |
| `editing/A/changed-path-sha256.json` | Full 36-path source/base/current SHA-256 manifest, candidate links, preservation result and final-newline state. |
| `editing/A/path-dispositions.json` | Every one of the 90 permitted C# paths: edit/retain, exact hashes and candidate associations. |
| `editing/A/candidate-dispositions.json` | All 21 original IDs → unchanged FPT identity/facets/inherited aliases, every associated permitted path, out-of-scope dependencies and exact original evidence units/range hashes. |
| `editing/A/example-bodies.json` | All three current packet-A C# code bodies, exact current line boundaries, text, hashes and classifications. |
| `editing/A/xml-checks.json` | Well-formed XML-block checks for each edited C# file; no compiler/cref-binding claim. |
| `editing/A/input-identities.json` | Exact immutable release/map/investigation/packet/source-cache input identities. |
| `editing/A/verification.json` | Fresh static-only verification receipt and product patch hash. |
| `editing/A/verify_static.py` | Small records-only reproduction of existing full-line-comment preservation comparison, manifest joins, XML parsing and diff check; no SDK invocation. |

`editing/A/output-identities.json` additionally hashes the new editor records, excluding itself to avoid
a self-hash cycle. The coordinator should transfer/review the exact product diff, not alter baseline
readers' checkout first. No staged or committed transfer is present.

## Local candidate → actual change / retained or held facet

The prefix `D/` below means `src/Core/src/Devices/`; `U/` means
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/`; `I/` means
`src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/`. These are table abbreviations only;
the JSON manifests contain full paths. `Cnnn` refers to the original `CORE-A-Cnnn`, never a new accepted ID.

| Original / proposal | Actual C# disposition and remaining boundary |
| --- | --- |
| C001 / FPT-001 | **EDIT** `D/VersionQualifier.cs`: inclusive `0..uint.MaxValue`, preserved long API, actual exception conditions and formatted name. **RETAIN** `U/DeviceInfoFieldLengthTests.cs`: full-width/overflow assertions already accurate and unchanged. |
| C002 / FPT-002 | **EDIT** `D/DeviceInfo.cs`, `D/ConnectionType.cs`, `D/DeviceCapabilities.cs`, `D/Feature.cs`, `D/DeviceFlags.cs`, `src/Core/src/Transport.cs`: field purpose, missing-value defaults, supported/enabled distinction, timeout units/encoding, component versions, effective version, modeled masks, physical transport versus concrete connection. **RETAIN** field-length, physical-device and mapper tests; **HOLD** dependent Management/shared labels and cross-metadata policy. |
| C003 / FPT-003 | **EDIT** `D/DeviceInfo.cs`, `src/Core/src/Abstractions/IYubiKey.cs`, `U/DeviceInfoTests.cs`: timeout aliases source storage; keep it alive/unchanged; struct copying shares it; equality/hash read current bytes; replacing a cached snapshot preserves prior scalar fields. Existing comparison assertions run before input-list disposal. `D/DeviceInfoReader.cs` is **EDIT** for C015 precedence only: its storage ownership/cleanup remains unchanged. **RETAIN** `U/YubiKeyDeviceTests.cs` scalar/replacement oracle. **HOLD** stronger post-disposal stability wording, stable hash across mutation/disposal, independent ownership and automatic reader-list cleanup pending FPX-01/A-E01 plus ownership decision. No demonstration ran. |
| C004 / FPT-004 | `D/DeviceInfo.cs` is **EDIT** for independently correct field names/defaults; the ordinary `FromApp` integration and unused `FromFips` remain unchanged. **RETAIN** `U/CapabilityMapperTests.cs` and current fixture assertions. **HOLD** authoritative tags 0x14/0x15 mapping/width, integrated nonzero vectors and certification claims. Preserve `management-001:MGMT-XREF-001` and `audit-v2-20261022-20261031:deviceinfo-FIPS-residual` as one nonadditive inherited group. |
| C005 / FPT-005 | **EDIT** `D/FormFactor.cs`, `D/DeviceInfo.cs`: complete reported byte, manufacturing flags, unnamed 0x81 versus base 0x01, allow unnamed values. **RETAIN** current `U/DeviceInfoTests.cs` form-factor assertions (that file is edited for C003). **HOLD** normalization/raw representation and exact-match filter/CLI policy. Alias `management-001:MGMT-XREF-002` retained. |
| C006 / FPT-006 | **EDIT** `D/DeviceCapabilities.cs` modeled `All`; `D/DeviceInfo.cs` removes the implication that accepting a wide field proves whole-integer decoding. **RETAIN** mapper and field-length tests: wide all-zero vectors do not establish nonzero-tail or signed-high-bit behavior. **HOLD** width/raw preservation/truncation fix and A-E02 vectors. Alias `audit-v2-20261022-20261031:deviceinfo-wide-app-bits` retained. |
| C007 / FPT-007 | **EDIT** `IYubiKey.cs`: owned-monitor retention, DeviceUpdated on changed metadata/late serial, known-serial replacement, concrete HID bits and independent serial latch. Neighboring `D/DeviceChange.cs`, `D/YubiKeyMonitor.cs`, `D/YubiKeyManager.cs` explain mutable device references versus frozen payloads, subscription/publication and scoped identity. **RETAIN** `U/DeviceIdentityContractTests.cs`, `U/SnapshotDiffTests.cs`: existing retention/update assertions already supply the narrower oracle. |
| C008 / FPT-008 | **EDIT** `D/DiscoveryBudgets.cs`, `D/DiscoveryIdentityReader.cs`, `D/DiscoveryReadScope.cs`, `D/ProtocolDeviceInfo.cs`, `D/SharedDeviceInfoRead.cs`, neighboring `D/DiscoveryDeviceInfoReader.cs` and `D/CompositeMetadataReader.cs`: arithmetic budget sum, actual three-attempt exception branches/backoff, timeout versus busy-card inference, finder scope, admission/native ownership, remaining caller-wait budget and borrowed protocol cleanup. **EDIT** identity-reader/fault-injection test commentary and monitor disposal bounds. **RETAIN** scheduling/single-flight/scope/retry helpers' accurate assertions/commentary. **HOLD** logger-failure resilience A-E03, native release and historical root cause. |
| C009 / FPT-009 | **EDIT** `D/FindYubiKeys.cs`, `D/IDeviceTopologyResolver.cs`, `D/WindowsDeviceTopologyResolver.cs`, `D/WindowsTopologyNativeOps.cs`: current no-topology resolver on Linux/macOS, synchronous unbudgeted Windows calls, PnP evidence, null-buffer probe, Windows 8/Server 2012 and Remote Desktop fallback. Neighboring merger text scopes supplied topology keys. **RETAIN** `U/WindowsDeviceTopologyResolverTests.cs`: scripted resolver logic is not native ABI or physical ContainerId evidence. **HOLD** actual OS/native qualification and future ready Windows delta. |
| C010 / FPT-010 | **EDIT** `U/CompositeDeviceMergerTests.cs`, `U/CompositeDeviceMergerVectorTests.cs`, neighboring `D/CompositeDeviceMerger.cs`: transport-shaped single-slot identity, completed topology-vector reference instead of TODO, scan-scoped serial-shaped IDs, contested completeness heuristic, conditional convergence, one-order conservation and expected-shape fallback oracle. Assertions/method names/runtime failure text preserved. |
| C011 / FPT-011 | **EDIT** neighboring merger, IYubiKey and manager contracts: complementary partial same-PID observations can group across keys; slot routing faithfully follows paths but composite serial selection can still select another key's slot. Vector comments retain the accepted bound and identify untested later healing. **HOLD** shared headline/guide integration for the SHARED editor; no merge-policy change. |
| C012 / FPT-012 | **EDIT** `I/CompositeDiscoveryIntegrationTests.cs`, `I/DiscoveryUnattendedTests.cs`, `I/YubiKeyTests.cs`, `I/HotPlugIdentityContractTests.cs`, `U/DeviceInfoReaderTests.cs`: actual direct-device authorization scope, production-enumerator/local-PID oracle, counts versus membership, metadata SELECT, interface-bit assertions, replay versus new insertion, timeline versus native cause and sync-only fake disposal counter. **RETAIN** `I/DeviceEventHotPlugTests.cs` current full-cycle/success-path pump assertions. **HOLD** changed assertions/harness, failure-path pump drain and any new physical/native qualification. |
| C013 / FPT-013 | **HOLD OUTSIDE WRITE SCOPE**: shared live identification/allow-list/lazy binding documents and C# paths. No permitted primary C# correction needed. Alias `management-001:MGMT-XREF-003` remains a documentation dependency, not a new runtime finding. |
| C014 / FPT-014 | **RETAIN/HOLD OUTSIDE WRITE SCOPE**: existing malformed-firmware criterion policy is already documented in shared attribute XML; no Core behavior changed. Alias `management-001:MGMT-IMP-009`; strict handling/two-component Management criterion awaits its owner. |
| C015 / FPT-015 | **EDIT** `D/FirmwareVersion.cs`, `D/Feature.cs`, `D/DeviceInfo.cs`, `D/DeviceInfoReader.cs`: major-zero optimistic policy, including unknown 0.0.0; numeric ordering within major-zero group; actual firmware/default/non-final-qualifier precedence; format/byte-span contract. **RETAIN** `U/FeatureTests.cs` and reader precedence assertions; reader test commentary edit is C012. **HOLD** applet-specific support/gate decisions. |
| C016 / FPT-016 | `D/DeviceInfo.cs` **EDIT** preserves ignored unknown tags, states existing duplicate-tag ArgumentException and unnamed qualifier behavior without introducing a raw escape hatch or validation. **RETAIN** existing field-length tests. **HOLD** raw-response representation, duplicate-error normalization and equality/API design decisions; no complete unknown-field preservation claim. |
| C017 / FPT-017 | **EDIT** `D/FindYubiKeys.cs`: identity cache validates PID; metadata cache stores only info/member IDs and can reuse metadata under stable IDs after a PID change. **EDIT** fault-test description: two OTP instances force identity path and do not test metadata-only validity. **HOLD** A-E04 stable-ID/two-FIDO-PID vector, cache validity policy and fix. |
| C018 / FPT-018 | **HOLD OUTSIDE WRITE SCOPE**: `scripts/toolchain/TestTargets.cs`, TOOLCHAIN/testing guidance. No claim that installed xUnit 3.0.0 repeated switches implement AND or OR. A-E05 remains unexecuted. |
| C019 / FPT-019 | **HOLD OUTSIDE WRITE SCOPE**: toolchain layout and shared PermanentDeviceState reset/reinsertion instructions. Preserve executable traits and defer applet reset claims to their boundary. |
| C020 / FPT-020 | **EDIT** bounded local unsupported Rust-comparator language in merger/metadata-reader/qualifier comments and replace the public IYubiKey measurement anecdote with a source-supported example. **HOLD** original Rust repository/full `7d7a7455` pin and historical platform watcher claims for citation review; current pinned Python is not substitute evidence. Historical records remain immutable. |
| C021 / FPT-021 | **EDIT** finder cache and IYubiKey serial XML: successful metadata without a serial is cached, distinct from failed/timeout reads; unchanged scans do not promise another read. **RETAIN** existing failure-only assertions (fault-test file also edited for other facets). **HOLD** A-E06 serial-less successful-read counter/control and expiry policy. |

### Explicit candidate-associated retained C# files

The following paths are retained, not omitted. Together with the 36 edited paths they cover all 49
candidate-associated primary/neighboring files. All remaining packet C# paths retain their existing
prose and behavior; `path-dispositions.json` lists them individually.

| Full path | Retain rationale / held stronger interpretation |
| --- | --- |
| `src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/DeviceEventHotPlugTests.cs` | Current removal/arrival, non-churn and healthy success-path pump checks remain accurate. Failure-path pump drain and executed physical qualification remain held. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CapabilityMapperTests.cs` | Direct helper vectors retained; they do not prove actual FIPS-tag integration or wide nonzero input decoding. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs` | Late serial arrival/update and replacement assertions already agree with corrected interface XML. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoFieldLengthTests.cs` | Fixed-field, optional-default, qualifier-width and all-zero wide-capability assertions retained. No raw preservation or nonzero wide-integer inference. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoveryReadSchedulingTests.cs` | Existing managed admission/scheduling and late-cleanup assertions retained, separate from native release proof. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoverySingleFlightTests.cs` | Existing shared-read/waiter behavior remains accurate; no logger or native guarantee inferred. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FeatureTests.cs` | Existing major-zero policy vectors retained; they are compatibility policy, not hardware feature evidence. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysScopeTests.cs` | Existing scope, filtering, queued retirement and returned-slot usability assertions retained. SmartCard fake routing is not HID framing/native evidence. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PhysicalYubiKeyTests.cs` | Existing flags/connection-shape assertions retained; no broader hardware census inferred. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/SnapshotDiffTests.cs` | Existing retention, equal metadata, substitution and predecessor scalar isolation assertions retained. Timeout backing-memory stability remains separately held. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/WindowsDeviceTopologyResolverTests.cs` | Existing scripted PnP resolver/fallback checks retained; actual native APIs and OS property equality remain unqualified. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs` | Existing no-I/O cache access, replacement and serial-latch assertions retained; no disposal-stable timeout claim added. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/TransientScanRetryTests.cs` | Existing bounded managed retry-helper expectations retained; distinct from discovery's per-attempt retry and native lifetime. |

## Example-body decisions

Current boundaries, including text/hashes, are authoritative in `editing/A/example-bodies.json`.

| Current path / lines | Classification / decision |
| --- | --- |
| `src/Core/src/Devices/YubiKeyManager.cs`, tags 16–37, body 17–36 | **EDIT, COMPLETE-METHOD**: imports plus `TrackAsync(CancellationToken)` using actual manager/monitor/change-record APIs. `await using` encloses the complete watch enumeration. No fake Apply helper, unknown token/serial input, hidden resource owner, live Management dependency or read-only-buffer cast. It prints changes rather than pretending to maintain a projection. |
| `src/Core/src/Devices/ConnectionTypeExtensions.cs`, tags 31–35, body 32–34 | **RETAIN, ILLUSTRATIVE-INTERNAL-EXPRESSIONS**: actual internal scope-expansion expressions, not a public-consumer program. No resource acquisition. |
| `src/Core/src/Devices/ConnectionTypeExtensions.cs`, tags 66–70, body 67–69 | **RETAIN, ILLUSTRATIVE-INTERNAL-EXPRESSIONS**: actual internal Matches truth-table expressions, not a public-consumer program. No resource acquisition. |

All three are `VALID-NOT-CHECKED` for compilation and `NOT-RUN` for execution. No new complete metadata
decode example crosses TLV disposal; FPX-01 is not simulated with a clone or an unsupported writable cast.

## Source authority and consequential passages

The retained originals were reviewed before their corresponding text edits. Their exact original URLs,
access times, HTTP metadata, byte counts and SHA-256 identities remain in
`investigation/A/sources/captures.json`; this batch does not replace them with a current profile.

| Original / exact status | Passage, actor, strength and applicability used here |
| --- | --- |
| **YubiKey 5 Series Configuration Reference Guide**, unversioned manufacturer reference, retained 2026-11-11 (`vendor-config.html`) | Concepts → Form Factor/Auto-Eject Timeout/Challenge-Response Timeout/Device Flags; Reading Information → Device Information. Device-response/configuration descriptions establish reported bytes, seconds, 15-second hardware default, inactivity with MODE_FLAG_EJECT, manufacturing flags and ordinary response formats. Descriptive vendor text, not an RFC-style MUST/SHOULD profile. Family scope is YubiKey 5+/Security Key with separately described YubiKey 4 capabilities compatibility; no extrapolated modern FIPS-tag layout or future-width rule. |
| **YubiKey Technical Manual → YubiKey 5 FIPS Series Features → FIPS Form Factors**, unversioned manufacturer manual, retained 2026-11-11 | Manufacturing-set one-byte table, firmware 5.4+ values 0x81..0x85 corroborate raw flagged form factors only. No operation certification, FIPS-mask encoding or SDK normalization requirement follows. |
| **Microsoft DEVPKEY_Device_ContainerId**, content updated 2023-02-28, retained original HTML | Remarks: Windows 7+ PnP container grouping and matching GUIDs identify devnodes in the same container. Actor is OS PnP/property caller; descriptive API contract, not universal hardware attestation or measured SDK native correctness. |
| **Microsoft SCardGetReaderDeviceInstanceIdW (winscard.h)**, content updated 2024-11-20, retained original HTML | Parameters/Return value: null output buffer returns required character count including terminator with success; hContext comes from SCardEstablishContext. Remarks/Requirements: Remote Desktop call fails SCARD_E_READER_UNAVAILABLE; Windows 8 desktop / Server 2012 minimum. WinSCard caller contract, no completion-time guarantee. |
| **dotnet/runtime v10.0.0**, `System.Threading.Channels/BoundedChannel.cs`, retained original text | Complete TryComplete 305–361 and TryWrite 363–442: completed writer rejects, full Wait-mode TryWrite returns false immediately, buffered history can drain. Runtime implementation evidence supports the SDK's subscriber-local overflow policy; Core feed tests define that policy. No device norm or newly run concurrency result. |

Context7 was consulted for current .NET bounded-channel orientation; its unversioned channel guide is
orientation only. Consequential runtime reasoning stays pinned to the retained v10.0.0 source. The B1
and C triage clarifications were read: no NIST original-PDF verification, PIV physical-state closure,
provider erasure, profile upgrade or new native guarantee is imported into this A batch.

## Precise recommendations to the later SHARED editor

These are documentation handoffs within the current Pass-1 effort, not authorization to change these
files here or a new Pass-2 project.

1. **`docs/usage/device-discovery.md`, `src/Core/README.md`, `src/Core/CLAUDE.md`,
   `src/Core/src/Devices/README.md`:** align Find versus monitor ownership, first-move replay, retained
   device references, DeviceUpdated, five-second per-transport wait, individual rather than scan-total
   budgets, late native cleanup and explicit disposal. Keep live Management reads separate from cached
   state and preserve the timeout alias qualification; hold stronger post-disposal claims for FPX-01.
2. **`docs/architecture/device-discovery-guarantees.md`:** put the G2 complementary-partials/cross-key
   condition beside the introductory conservative-grouping claim. Explain that path-bound routing does
   not verify a slot against the composite serial selected by the caller. Replace successful serial-less
   retry promises with current cache lifetime; distinguish failed reads. Scope G4/G9 to implemented
   topology paths and qualify all expected-shape/count/scripted versus native/physical oracles.
3. **`docs/architecture/physical-device-model.md`, `docs/architecture/device-identity.md`,
   `docs/architecture/event-driven-device-discovery.md`:** qualify one-key headlines with evidence bounds;
   owned-monitor retention requires unchanged member/connection sets and no contradictory known serial.
   Identity cache validates PID; metadata-only cache does not. Explain last-known snapshots without
   claiming immutable backing memory. Source/citation review must obtain the original Rust full pin,
   not substitute the independently pinned Python source.
4. **`src/Tests.Shared/README.md`, `src/Tests.Shared/CLAUDE.md`, `src/Tests.Shared/IYubiKeyFilter.cs`,
   `src/Tests.Shared/YubiKeyTestState.cs`:** live identification precedes allow-list filtering; distinguish
   empty configuration's exit condition from filtered nonmatches/read failures. Explain first-match lazy
   row binding and run-local fixture metadata independent of one-shot manager evidence. Replace nonexistent
   YubiKeyTheory/YubiKeyTestDevice examples with actual APIs under that editor's scope. No new gate is proposed.
5. **`scripts/toolchain/TestTargets.cs`, `TOOLCHAIN.md`, `docs/TESTING.md`:** correct the translator's
   separate per-term switch description without claiming pinned repeated-switch selection semantics;
   A-E05 remains held. Correct current `src/<Module>/src/*.csproj` layout versus recursive ordinary
   discovery. **`src/Tests.Shared/Infrastructure/TestCategories.cs`:** qualify PermanentDeviceState reset
   and reinsertion instructions with the owning CTAP/profile boundary; preserve executable trait strings.
6. **CLI/shared filters and Management references:** `src/Cli.Shared/src/Device/FormFactorFormatter.cs`,
   shared capability/form-factor filters, and `src/Management/src/IManagementSession.cs`/README examples
   need their owners' current observed/raw-versus-normalized qualifications. FIPS mapping, form-factor
   representation and wide capability bits remain policy/implementation handoffs, not intended design.
   Applet-specific claims await the named boundary; independently correct Core field wording can proceed.
7. **Historical AOT/readiness/integration narratives:** preserve the old binaries/log identities,
   failed/skipped outcomes and observable-host status. Do not label present WatchAsync fan-out measured
   by an older host. Keep all historical 8325 integration, 5366 prior and 114 PIV populations immutable.

## Review needs and exact holds

- Independent content/citation review must inspect full affected units and their helpers, not just hunks.
  Review all 36 C# paths, the complete example and the 13 candidate-associated retained paths above.
  Original investigation units and hashes remain attached to every local candidate in the JSON return;
  this does not relabel old source ranges as current edited ranges.
- **FPX-01/A-E01 remains NOT RUN.** The source establishes direct aliasing. Stronger claims about timeout
  bytes/hash after disposing input TLVs, mutation independence or safe mechanical page-list cleanup are
  held. The coordinator decides scalar/copy/explicit borrowing only in separately authorized work.
- A-E02 (nonzero FIPS/flagged/wide vectors), A-E03 (logger-failure behavior), A-E04 (metadata PID validity),
  A-E05 (installed runner switches) and A-E06 (successful serial-less cache expiry) remain NOT RUN.
  Their exact triggers are preserved in `candidate-dispositions.json`; no apparent experiment outcome
  is inferred from this edit or from pre-existing assertions.
- Windows concurrency work remains unready and receives a later named delta review; current pinned
  source wording adds no execution on that branch. Native cleanup, producer/ABI, physical FIPS/profile,
  cross-process state and logger-exception behavior retain the original qualifications.
- Inherited fixed-field `audit-v2-20261022-20261031:#21` and continuation `25b` source fixes are preserved.
  This adds no page-255 endpoint execution. `fixes-wave-001:CORE-001` and the run-qualified multi-key
  exclusions remain unresolved as historical physical causes, including serial 9681620. Managed
  ownership changes do not identify that cause. `management-001:MGMT-IMP-008` remains its applet's gate
  dependency. PIV physical deferrals and original provenance populations remain unchanged.
- Fresh static verification passed: all 90 source/base product comparisons, every exact non-comment-line
  comparison, changed XML well-formedness and `git diff --check`. This is not a Roslyn token/API/cref check,
  build, test, example compilation or physical qualification. Those statuses remain explicitly unclaimed.

The bounded C# edit batch ends here. The coordinator owns transfer, independent review, shared/boundary
reconciliation and eventual Core Pass-1 publication; no additional pass is planned by this return.
