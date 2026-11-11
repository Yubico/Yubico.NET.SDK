# Core Pass 1 inventory

**Source baseline:** `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`, `yubikit-docs-audit`.
The observed checkout HEAD during inventory was `d9cf31a17c7580ab580917ce201c4855c3cf89b4`.
That later coordinator checkpoint publishes campaign metadata; the source baseline stays fixed.
Every tracked Core file was compared with the pinned tree, with Git's CRLF/LF normalization recorded
explicitly. The active handoff is `docs/research/documentation-audit/core.md`; it is later metadata
rather than an input present in the source pin.

This inventory establishes scope and reading ownership. **Substantive units remain `UNREAD`.**
Instruction/orientation reads made to prepare dispatch establish no clean semantic dispositions.
Implementation, original-citation verification, compiler binding and device/platform execution retain
their separate gates.

## Reconciled populations

| Population | Fresh count | Meaning |
| --- | ---: | --- |
| Tracked Core files | 515 | Complete pinned `src/Core/` tree |
| Core C# files | 498 | 312 shipping + 186 test files |
| Shipping subtree files | 316 | 312 C#, Devices README, two API snapshots and project file |
| Native C# files | 57 | Inside `src/Core/src/Native/`, already included in shipping |
| Test subtree files | 196 | 186 C# plus instructions, runner/project files, registries and vectors |
| Core root guides | 3 | README, package README and CLAUDE |
| Public API snapshot entries | 1,442 | 160 types, 591 callable entries, 412 accessors and 279 enum/constant entries |
| Effective public source families | 160 | 171 public type declarations after combining partials |
| Indexed external files | 1,011 | Bounded support projects and applet/Core candidate paths, with partial-scope dispositions |

The earlier 311 shipping-C# count describes the incoming tree; this integrated tree also contains
the campaign's shared Base32 decoder. The earlier approximately 159-type estimate omitted the public
`DataEncryptor` delegate. Current source and snapshot type sets match exactly. The 160 families comprise
85 classes, 27 enums, 23 interfaces, 18 record classes, five record structs, one ordinary struct and one
delegate. Source containment excludes public-spelled declarations enclosed by internal native types.

[Public API reconciliation](inventory/public-api.json) records every snapshot entry, all defining
partial paths, source declaration-group candidates and its assigned family. A has 25 public families /
260 entries; B has 66 / 603; C has 69 / 579. These are surface counts rather than documentation-unit counts.

Accessors project onto their property contract. Positional-record members project onto the record's
parameter/type contract. The 154 record synthesis/positional candidates retain individual entries for
review; useful public fields such as `Revision`, `Device` and `DeviceInfo` metadata stay in scope.
Authored equality/hash overrides remain authored declarations. Standard generated equality, copying,
printing and deconstruction need a type-level disposition rather than repetitive invented XML.
The delegate's generated `Invoke` surface projects onto its declaration. The primary constructor of
`BadResponseException` likewise projects onto its type declaration. Every snapshot entry has a source
declaration-group candidate; precise overload resolution, generator participation and metadata shape
remain `VALID-NOT-CHECKED` for later compiler evidence.

## Inventory records and coordinates

| Record | Purpose |
| --- | --- |
| [coverage.json](coverage.json) | Compact denominator and explicit dimension statuses |
| [files.json](inventory/files.json) | All 515 Core files and 1,011 external candidate paths; raw SHA-256, pinned blob, owner, rationale and units |
| [semantic-units.json](inventory/semantic-units.json) | Disjoint declaration/context, narrative, map, test and support units with source/range hashes |
| [public-api.json](inventory/public-api.json) | Complete snapshot/source accessibility and partial/member projection reconciliation |
| [shared-sections.json](inventory/shared-sections.json) | Exact shared read ranges, one primary reader and exclusive future shared editor |
| [project-dispositions.json](inventory/project-dispositions.json) | Whole named project populations, indexed paths, omitted paths and reasons |
| [example-bodies.json](inventory/example-bodies.json) | 501 complete fenced-body candidates, including implicit calls and prerequisites |
| [specification-candidates.json](inventory/specification-candidates.json) | 1,658 lexical citation/standard candidate lines for contextual triage |
| [unfinished-markers.json](inventory/unfinished-markers.json) | 14 marker lines with their owning units and current scope statuses |
| [candidate-crossrefs.json](inventory/candidate-crossrefs.json) | Inventory questions and collision-safe key rules; separate from inherited-finding adjudication |
| [inherited-routing.json](inventory/inherited-routing.json) | All 70 inherited groups / 100 provenance rows mapped to packets and source ownership without changing their states |
| [inherited-packet-inputs.json](inventory/inherited-packet-inputs.json) | Reverse per-packet inherited IDs and exact current read paths |
| [parser-limits.json](inventory/parser-limits.json) | Two external toolchain files retained as complete units |
| [verification.json](inventory/verification.json) | Partition, source, API, ownership and protected-input checks |

The 25,728 partition records comprise **14,044 unread reading units**, 7,140 external out-of-scope units
and 4,544 administrative-context units. Core contributes 10,943 records: **9,020 unread units** and
1,923 administrative-context units. Administrative context covers structural punctuation, whitespace
and license context; all substantive comments and code-only declarations retain reading units.
Feature flags overlap: an API declaration can contain XML, internal rationale, a specification reference
and a complete implementation. Those flags create sampling strata, rather than duplicate audit units.

Offsets are zero-based, end-exclusive character positions after UTF-8 BOM removal, with original newline
characters retained. Whole-file SHA-256 covers raw bytes including BOM and CRLF. Adjacent units can share
a displayed line when declarations share that line; their character ranges remain disjoint. The verifier
checks full coverage without gaps or overlaps. Binary diagram renderings retain raw identities and a
derived-output disposition; their text-coordinate fields carry no line-level prose interpretation.

The source partitioner uses Pygments masking and balanced lexical declaration boundaries. It handles
record/delegate containment, partial declarations, default arguments, extension blocks and nested types
for this reading inventory. It supplies declaration groups, rather than a compiler symbol graph.
`scripts/toolchain/Process.cs` and `RunLog.cs` exceed its lexical string handling and are retained as
whole-file reading units. A later reviewer may refine them with Roslyn before clean sampling. All 498
Core C# files have balanced lexical partitions. Inventory generation used no existing generated-map
destination and created no C# host. **This inventory requests no build.**

The parallel [validation foundation](validation-preparation.md) has prepared
`Audit20261111.DocCheck` for coordinator review and later compilation. Its token/API-preservation work
is separate from precise symbol/consumer binding. Reuse that foundation for assigned validation rather
than creating another generic host. Its 1,608-file preservation population has a broader purpose than
this Core/external reading index; the foundation records the actual Git-clean explanation of 57 raw
checkout newline differences. Those differences supply no product-drift finding.

## Every Core file has an owner

| Owner | Core files | Scope |
| --- | ---: | --- |
| A | 91 | Discovery, metadata, identity, owned monitoring and their tests/support |
| B | 311 | Registry/slots, native/transports, session/framing/SCP, boundary registries and related tests |
| C | 104 | Cryptography, credentials/presence contracts, logging, encoding/buffer utilities and related tests |
| SHARED | 9 | Core root guides/instructions, project/runner configuration and test instructions |

The Devices README belongs to A. `IYubiKey` belongs to A, while its connection contract is a required
read-only dependency for B. `DeviceConnectionRegistry`, slot transfer, registered connections and
`DisposalGate` belong to B; discovery admission/evidence scopes belong to A. `ExchangeGuard` and its
tests belong to B; other utilities belong to C. Credential/presence declarations belong to C, while
session/protocol operation-lifetime use belongs to B. Native declarations have one B owner even when
discovery or crypto investigators need to read them.

Four tracked files consist of commented-out historical source: `Cryptography/Cose/CoseKeyOperations.cs`,
`Cryptography/RsaFormat.cs`, `Native/Linux/Udev/LinuxUdevScan.cs` and
`Utilities/DisposableTlvDictionary.cs` under `src/Core/src/`. Their substantive historical claims receive
applicability triage, and they contribute no active public families. They retain source hashes and owners.
The two API snapshots are analyzer records; test registries/vectors and project metadata receive support-
contract review. Generated library-import stubs, `bin/`, `obj/` and untracked code maps are outside the
tracked-source denominator. Their actual compiler participation belongs to later validation evidence.

## Precise external scope

The external population is a candidate/disposition index. Its size grants no repository-wide audit or
editing scope. Exact selected sections and exclusions are the ranges in `semantic-units.json`;
`project-dispositions.json` also names every omitted file in each selected source/support project.

| Surface | Selected review | Boundary |
| --- | --- | --- |
| Root README/package README and Core guides | Discovery/install/transport requirements, ownership, monitoring, raw tiers, SCP, credentials/crypto and linked task examples | Applet operations are read for Core responsibilities; applet-specific correctness remains in boundary packets |
| Root/Core instructions and SDK house style | Present Core contracts, examples, lifetime/security/logging/memory conventions | Instruction reads orient the task; substantive audit status stays unread |
| `docs/usage/` | All three guides: discovery, user interaction and TLV ownership; complete selected example scopes | Recheck callers, cleanup and current public signatures |
| `docs/architecture/` | Core-facing sections, diagram source blocks, structural map entries, image manifest and seven SVG/PNG pairs | WebAuthn extension-only behavior stays outside Core unless a concrete boundary dependency reopens it |
| Migration/comparison/highlights | Core discovery/session/ownership/shared-helper rows and sections, with neighboring conditions | Unrelated applet migration rows retain an out-of-scope disposition; `DOC-002` remains a registry input |
| Linux setup, AOT, logging, memory, crypto and C# guidance | Core-facing provider/platform prerequisites, API use, security ownership and execution qualifications | Platform/API questions go to current original documentation during investigation |
| `verification/LinuxHidRouteVerification/` | **All 14 files**: native/hardware/operator scenarios, report seams, publish/capture/evidence gates and host configuration | Distinguish native-only, device and attended lanes; source/capture logic supplies no new execution result |
| macOS verification | Whole named input host (2 files), route host (7) and comparison host (5) | Preserve current/legacy profile boundaries and held-native evidence limits |
| Native AOT verification | Whole 2-file host, linked-module anchors, default discovery and optional monitor flow | All-module linking, process startup, discovery, monitoring and applet execution remain separate |
| Native compatibility verification | Whole 7-file project including RuntimeConsumer, exports and negative fixture | Loader/export/package expectations; external native-shim internals retain their package-source boundary |
| Benchmarks | All 4 files in the one performance project | Invocation/completion, boundary baselines, self-check claims and prerequisite/result explanations |
| Main/AOT CI and shared build files | Two workflows, relevant toolchain/support declarations and properties | Review what gates select/execute and what their outputs prove; running them requires coordinator assignment |
| `Tests.Shared` | Whole 20-file candidate project, with per-unit scope | Authorized discovery, caching/filtering, recorder and helper connection/SCP lifetimes; applet operations remain applet-owned |
| `Cli.Shared` | 21-file candidate project; selected device selection, session, credential and presence units | Unrelated argument/menu/output mechanics are explicitly outside the Core contract review |
| `Cli.Commands` and `PublicApi` | Core-caller units and API-convention support, with project-level dispositions | Compile/run actual callers later; executable behavior/help strings remain preserved in Pass 1 |
| Nine applet projects | Actual Core-facing declarations, factory/backend/helper calls, test assertions and complete narrative/example units | Dedicated later boundary reviews; other applet behavior retains its earlier whole-module audit |
| Linux research handoffs | Current report/reactor/FIDO/OTP/routing/managed/native acceptance qualifications | Inspect claimed evidence at its recorded revision rather than rerunning or broadening it silently |
| Historical async plans and AOT readiness logs | Provenance, superseded architecture and current qualification pointers | Historical evidence stays historical; current contracts come from the integrated source |
| Active campaign Core handoff | Its current baseline and verification limits | Coordinator owns the metadata; frozen run contents remain inputs |

Native interop lives inside Core's 57-file subtree. There is no tracked top-level NativeShims source
project in this checkout. `Yubico.NativeShims` is a package dependency; its current integration and ABI
claims are covered through the Core project, native loaders, package/build context and compatibility host.
The current native implementation sources are C#. The current C/C++ lane is empty and explicitly
not applicable; Core's managed native source remains included in the 498-file C# population.
The inventory creates no replacement native directory.

## Consumer-journey coverage matrix

All cells start `UNREAD`. Paths below are entry points into the exact packet sets; investigators trace
the complete implementation, error paths, assertions and claim dependencies before dispositioning them.

| Journey | Primary owner | Source entry points under `src/Core/src/` | Reader-facing / external dependency |
| --- | --- | --- | --- |
| Discover and select one/many keys | A | `Devices/YubiKeyManager.cs`, `YubiKeyDiscovery.cs`, `FindYubiKeys.cs`, `Abstractions/IYubiKey.cs` | Core/root getting started; CLI device selection |
| Scope Find to selected interfaces | A | Finder/slot selection, discovery read scope/admission and identity readers | Discovery guide; Management scoped read-only integration companions |
| Own/share monitoring | A | `Devices/YubiKeyMonitor.cs`, `HotplugSignals.cs`, `RescanScheduler.cs` | Host-owned and per-enumeration guide examples; AOT optional monitor |
| Replay, snapshot completion and updates | A | `DeviceChange.cs`, `DeviceChangeFeed.cs`, `SnapshotDiff.cs`, `DeviceInfo.cs` | Projection/overflow/cached-metadata guidance and monitor tests |
| Dispose discovery/monitor lifetime | A | Monitor loop and disposal, finder retirement, `SharedDeviceInfoRead.cs` | Wait budgets versus worker/native lifetime; B reads transfer/teardown boundary |
| Lease and select PC/SC or HID | B1 | Registry, slots, registered connections, `Transports/SmartCard/` | Ownership/contention/physical-model guides; sharing policy and direct-factory callers |
| Cancel, reach terminal state and drain | B1–B6 | Connection owners, `Utilities/ExchangeGuard.cs`, sessions and protocols | Raw-tier guide; portable/native held-call assertions; per-platform qualification |
| Own native handles and report buffers | B1–B4 | `Native/`, Linux/macOS HID owners, Windows adapters, PC/SC native resources | Route/input/comparison/compatibility hosts and current native handoffs |
| Exchange raw APDUs | B5/B6 | `Sessions/RawSmartCardSession.cs`, APDU format/chaining/response pipeline | Raw access and Core guide examples; response clear/status order |
| Exchange raw shared FIDO HID | B5 | `RawCtapHidSession.cs`, `Protocols/Fido/Hid/` | CTAPHID versus CTAP2 vocabulary, channel/cancel/recovery and borrowed lifetime |
| Exchange raw OTP HID | B5 | `RawOtpHidSession.cs`, `Protocols/Otp/Hid/` | CRC/status/programming-sequence, reset/reopen and caller result ownership |
| Establish/use SCP | B6 | `Protocols/SmartCard/Scp/`, `PcscProtocol.Scp.cs` | Caller parameter/key/certificate lifetime and Security Domain/app applet boundaries |
| Use caller crypto/encoding buffers | C | `Cryptography/`, `Utilities/` | Catalog/provider/profile distinctions, ASN widths, Base32 and TLV source/intermediate/result cleanup |
| Acquire credentials and request presence | C, with B session trace | `Credentials/`, `Sessions/SessionCreationOptions.cs` | User-interaction guide, CLI secure input and nine applet outcomes |
| Configure logging safely | C | `YubiKitLogging.cs` and actual logger templates across A/B/C | `docs/LOGGING.md`, sentinel tests, externally supplied exception/value provenance |
| Publish/run with AOT/native prerequisites | B4 | Native loading and Core project properties | Native AOT guide, build/AOT CI, compatibility and platform hosts |

## Handoff and limits

The inherited-findings investigator exclusively owns `inherited-findings/*` and the old-ID outcome
registry. This inventory reads the nine handoffs and completed registry for scope routing without
adjudicating their old findings again. All 70 groups / 100 provenance rows are mapped in the two routing
records above. The 21 PGP documentation labels remain separate and all nine boundary packets remain
unexecuted. New candidate IDs use `core-001:CORE-XREF-<number>` after coordinator allocation;
original findings retain `<source run>:<original ID>`. An inventory question is evidence to investigate,
rather than a confirmed defect. The registry's proposed `CORE-DOC-*`/`CORE-IMP-*` accepted-finding
namespaces remain subject to the coordinator's collision check and triage.

The integration's recorded 4,442 unit executions, 197 overlapping resilience executions, Linux native
lanes, 30 compiled bodies and retained-output qualifications remain exactly scoped in the Core handoff.
They supply no new semantic disposition here. Four strict-live historical output comparisons remain
failed with retained originals; current-source/retained-output evidence is the qualified successor.

Continue the pinned audit while awaiting the user's Windows CCID/HID concurrency readiness notice.
Record a late handoff/delta checkpoint before closeout. Other worktrees remain untouched. PIV physical
touch/hotplug and touch-dependent cancellation stay deferred. This inventory ran Python/Git reads only;
it ran zero builds, tests, formatters, device commands, commits, index/ref writes or subagents.

Reproduce the inventory from the repository root with
`python -B docs/research/documentation-audit/modules/core/pass-1/core-001/inventory/build_inventory.py`.
Run the adjacent `map_inherited.py`, then `verify_inventory.py`, with the same full path prefix.
All three tools write
only their run-owned records. Any future compiler scratch uses
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/` with `TMPDIR`, `TMP` and `TEMP` propagated;
historical tmpfs and frozen integration records remain preserved.
