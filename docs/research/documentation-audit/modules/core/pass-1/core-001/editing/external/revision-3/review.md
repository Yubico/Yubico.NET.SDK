# Independent full external-batch review — revision 3

## Decision: bounded mandatory corrections

**Do not accept the full batch yet.** The 35-file successor is correctly identified and preserves
executable source, but two bounded documentation/record corrections remain below. These are not
requests to change runtime policy, assertions, configuration, historical evidence or validation tools.
No optional style changes are requested.

Reviewed worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-external`.
HEAD/base: `da66f5d01ba594993974ddd26723e8c300c0a454`.
Product source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Full patch: `final-successor-source.patch`, SHA-256
`76244276822b64598a78ab11b2db94d116da4bf021b555a73d4e02acaca836db`.
`review-identities.json` binds the reviewed product/record population and static checks.

This is a fresh full-batch review, not acceptance inherited from the initial or revision-2 reports.
The root, Tests.Shared and PublicApi instructions, approved editorial reference, house style,
testing instructions and primary checkout's current `triage/editing-release.md` were read. No more
specific CLI/verification CLAUDE instructions are present. The primary checkout was read-only;
its accepted integrated edits were not compared with this worktree's raw base as alleged drift.

## Mandatory corrections

### R1 — finish the existing authorization D facet on retained/unchanged passages

Original identity: **CORE-A-C013 / FPT-013**, including inherited
`management-001:MGMT-XREF-003`. This is the already released source-supported distinction, not a
new authorization policy or a new finding family.

| Current path/lines | Incorrect retained claim | Evidence and required bounded correction |
| --- | --- | --- |
| `src/Tests.Shared/README.md:9,333–334` | The overview says the harness hard-fails when no device is explicitly authorized; Known Limitations says allow-list violations call `Environment.Exit(-1)`. | `YubiKeyTestInfrastructure.cs:203–206,217–260,263–290` returns/caches an empty list and filters individual nonmatches/read failures. `YubiKeyTestState.cs:315–334` throws `SkipException` at binding. Replace these two claims with the same configuration-exit versus filtered/empty-binding distinction correctly given at README79–86. Do not change runtime output or the harness. |
| `src/Tests.Shared/Infrastructure/AllowList.cs:25–27,44–45` | Class XML says a disallowed device serial hard-exits; constructor XML says an empty list always hard-exits. | The complete constructor at47–66 exits only under55: `_allowedSerials.Count == 0 && !AllowUnknownSerials`. `IsDeviceAllowed:84–85` returns a Boolean and never exits. Correct the XML and attach this retained path to A-C013 rather than leaving its successor candidate list empty. Keep the executable constructor and predicate unchanged. |
| `src/Tests.Shared/Infrastructure/AppSettingsAllowListProvider.cs:24–32` | The current expected-configuration XML example advertises `EnableHardFail=true`. | Constants37–39, `GetList:58–73` and `AllowUnknownSerials:76–91` read only `AllowedSerialNumbers` and `AllowUnknownSerials`; there is no `EnableHardFail` switch. Remove the unsupported setting from the XML example or replace it with an accurately explained real setting without broadening authorization. Leave the executable error-message example at98–134 untouched and explicitly refer that protected output inconsistency to the existing non-D handoff. |

The original investigation states this distinction explicitly at `investigation-A.md:237–251`.
The final path ledger has all three paths, but gives the latter two generic RETAIN/no-candidate
dispositions. Thus population completeness does not establish D-facet completeness. Update only a
new successor's path/candidate/example dispositions to reflect these precise corrections. Preserve
the initial and revision-2 records and their recorded earlier product hashes as historical inputs.

### R2 — correct example-body classifications in the current successor inventory

Original associations: **CORE-A-C013 / FPT-013** (shared fixture examples),
**CORE-A-C019 / FPT-019** and **CORE-B4-C008 / FPT-055** (shared test/toolchain evidence).
This is an example-inventory acceptance correction, not new SDK behavior.

Revision 3 binds `revision-2/example-bodies.json` unchanged. The following concrete classifications
are false; a NOT-RUN compiler status does not make them accurate:

| Inventory coordinates / product body | Current record versus required classification |
| --- | --- |
| `revision-2/example-bodies.json:2489–2510`; `AppSettingsAllowListProvider.cs:25–32`, both base/current, body `f44d4bb4…dee888` | JSON configuration is labeled `csharp/xml-code` and a composable code/method/type example. Classify the actual JSON body as configuration, not a C# compilation participant. R1 changes only the successor current body; retain the old body/hash as history. |
| `revision-2/example-bodies.json:2537–2558`; `TestCategories.cs:23–35`, both base/current, body `5a25bdc6…19a4d7` | Shell command recipes are labeled composable C# examples. Classify them as command fragments; preserve literal XML `&amp;` versus rendered shell `&` provenance instead of presenting the raw XML body as C#. No command was executed. |
| `revision-2/example-bodies.json:2417–2426`; `src/Tests.Shared/CLAUDE.md:92–111`, current body `4a89a27a…041126` | The AllowList implementation sketch is labeled a composable example although product prose32–33 explicitly says section4 is an abbreviated implementation sketch, not a replacement. Classify it accordingly; its omitted real `AllowUnknownSerials` property/logging are not a complete replacement implementation. No product-body expansion is required. |
| `revision-2/example-bodies.json:2465–2474`; original `IYubiKeyFilter.cs:28–48`, body `ae523949…e93a5` | The superseded example with nonexistent `YubiKeyTheory`/`YubiKeyTestDevice` is labeled composable. Mark this base body as a superseded/non-current invalid API example repaired by the current body, not a currently usable fragment whose context supplies missing types. |

Provide a corrected successor inventory or a precise classification overlay keyed by
path/version/body hash, retaining the immutable earlier inventories. Rebind the current return to
that successor rather than rewriting the frozen histories. Retain all original bodies and distinguish
actual C#, typed composition context, configuration, command/output/map material and historical examples.
No new parser, compiler harness or whole-audit framework is needed.

## Full-batch content and evidence review

Apart from R1/R2, no mandatory correction was found in the changed units. The following describes
the review's actual scope; it is not runtime or whole-repository clean credit.

| Reviewed affected units and relevant retained dependencies | Source-supported conclusion |
| --- | --- |
| `YkCommandBase`, complete `DeviceSelector`, formatter, Otp helper units and retained CLI encoding assertions | Device preference is distinct from an explicit operation transport; reference-keyed enrichment and identity precedence remain intact. Exact form-factor comparison can fall through on product flags. ModHex's table/translator supports both cases; unified CLI scan-code conversion is distinct from the standalone tool defect. Protected any-host help text remains a non-D referral. |
| `PinPrompt`, complete `SecureCredential` readers/owner, affected credential assertions | Byte limits, surrogate validation, discarded embedded CR, raw redirected bytes and synchronous input are described accurately. View length/null-result/pool-clearing oracles remain distinct. No normalization, applet-profile, native-input cancellation or all-copy erasure guarantee was introduced. |
| Shared README/CLAUDE/filter XML, infrastructure/state binding, actual AllowList/provider and helpers, recorder | First-match binding, run-local cached metadata, connection filtering versus pinning, caller-owned SCP parameters and recorder command-copy/response-alias/counter-only disposal are supported. R1 is the remaining contradiction in this exact facet; a retained path is not automatically correct. |
| Retained HsmAuth commands, including complete add/generate scopes | Management-key cleanup comments match their enclosing try/finally. Generated keyEnc/keyMac allocations precede their inner try; no all-allocation/all-provider erasure claim is accepted. |
| Both PublicApi reflection test classes and factory/session lists | Finite reflected grammar and actual RP-ID validity/authorization option roles, not executed initialization/ownership/cancellation evidence. Assertions are unchanged. |
| Migration's raw SCP and retry methods, retained current examples and associated source APIs | Raw factory owns its hidden connection, supplied key owners remain external, callback work is awaited; the OATH method clears transferred password storage after actual operation/session lifetime, including creation failure. Real OATH retry signatures and key clearing support the prose. Retained PIV/OTP/OATH examples were source-checked, not accepted simply because unchanged. No example was compiled or executed. |
| Migration lifecycle/provider/codec prose, raw sessions, connection factories, ExchangeGuard and contained SCP key owners | Direct factories borrow connections; convenient device factories own hidden ones. Exchange drain is not a whole-workflow lease, and wait cancellation is not native completion. SCP record copies share contained owners; plain reselect retains the existing graph. Public provider delegates do not intercept direct runtime calls. Base32 and span-suspension qualifications are supported. |
| `TOOLCHAIN.md`, complete three changed scripts and relevant Discovery/Context/RunLog callers | Recursive selection, separate same-kind filter arguments, aggregate failed/all-NoTestsExecuted predicates and full-argv exposure match source. Actual pinned-runner boolean behavior remains unrun. Display masking is not log/output redaction. |
| All 26 primary verification/benchmark paths, including retained negative controls, expert adapters, CurrentProfile, self-tests and historical compatibility README | B2 contributes9, B3 contributes10, B4 contributes7, disjoint:12 edits,13 retains,1 historical hold. No primary path is omitted because absent from shared-sections. Changed prose matches actual assertions and timing/provenance boundaries, not filenames or printed PASS strings. |

In particular, the native/benchmark review checked real-libc socket fixtures versus hidraw hardware;
process-wide allocations versus native-only timing; invocation return versus terminal completion;
watchdog censoring versus SDK drain; selected fixture versus authorization/FW enforcement; GetInfo
status checks versus decoded maps; and package/deployed-file hashes versus loaded producer identity.
Listener CloseResult is recorded but not required zero. Late drain begins with an existing callback,
not ambiguous no-worker startup. AOT's ten type references are selected link reachability; three
WatchAsync sinks, the at-least-one Added/Removed oracle and live cross-sink snapshots do not establish
three physical cycles or an equal-prefix barrier. RuntimeConsumer's explicit release failure leaves
fallback eligible without observing ReleaseHandle before unhandled process termination.

Consequential authority was checked against the original qualified registers and source anchors,
not upgraded by a new generic reference: B1's NIST July2024 final §2.4.2 is card-application security
status, not per-process isolation; its independently unretained PDF remains qualified. PC/SC Part5
2.01.01 §3.2.5.2 retains desired/if-possible conditions. B6's SCP03 1.1.2 Public Release is distinct
from uninspected 1.1.2.6 Public Review clauses; SCP11 1.3.0.13 remains Public Review, not final1.4/profile
acceptance. NativeShims1.18.1 source commit `dedba95b94946907112a7953cd82df8a15c22655` identifies
published source, not a loaded producer/header build. Existing .NETv10 source/API qualifications
were reused; no new library API research or normative protocol requirement was needed.

## Independent static checks and exact script proof

Fresh read-only checks established:

- 164 unique path rows, all164 current raw SHA-256 values match the files. Final dispositions are
  35 EDIT,101 RETAIN,22 HOLD-HISTORICAL,6 HOLD; R1 identifies incorrect content dispositions, not missing rows.
- Actual Git diff contains35 product files and exactly equals the named87,278-byte patch when conventional
  `a/` and `b/` prefixes are requested. The checkout's default `i/`/`w/` prefixes explain the different
  default-rendered patch hash; this is not product drift.
- Original19 and prior32 edited-file raw identities remain equal to their frozen manifests. Both
 16-record history sets match; current revision3 output identities also match. Nothing was regenerated.
- All30 changed C# files retain protected source lines. Full-line comment removal preserves blank lines,
  imports, directives, declarations, assertions, literals and branch conditions; only the two known inline
  `return [];` comments in infrastructure require preserving their code prefix. This is text preservation,
  not Roslyn token/API binding. All111 XML documentation blocks in changed C# files parse.
- The supporting `.csproj` has identical normalized text outside XML comments and identical actual parsed
  element/attribute/text structure. CRLF/LF representation is disclosed; all configuration values remain equal.
- `git diff --check` exits0; HEAD remains the fixed base. No SDK build/test/formatter/native/device/crypto run,
  profile modification, Git index/ref update, commit, push or subagent occurred.

For these three files only, the non-comment digest is SHA-256 of exact raw lines after removing only
full-line comments whose left-trimmed bytes start with `//`, preserving all other bytes and blank lines:

| Script | Old raw SHA-256 | New raw SHA-256 | Identical old/new non-comment SHA-256 |
| --- | --- | --- | --- |
| `scripts/toolchain/Process.cs` | `70fbc9d528f094fc098ff6aa58bdc2ca33817445b0d529393fabd971d53b4f90` | `84fe870fabdf44a6033c514722bcc256f73a5cf7ef9268c6b0e5c433be4cf133` | `7f96abb76db0ec0ad4aac597d6f54a76b5bff6db7da93183d82797d138ba410d` |
| `scripts/toolchain/Results.cs` | `80fe8ba0557664469545ea1e709e1a755cc6da546024c48f1e171088795b6279` | `90a595695e19a88a1f0e55a60708ffebcae8ee12a2753662b5444b3fecf48abe` | `efd56a36bc35d68165b4e7f50432f8d9d400f559b1d35a259bedd74620188440` |
| `scripts/toolchain/TestTargets.cs` | `4d6ce786a5fde41dbc66fafa6190daccc19f1bcd6c534cd789590413513cc1d5` | `b257b57172bbd9967b7ec46754deffdaa3f044f2288ea7d22ed3f3b3c7593337` | `fad7d9d694d6f73038b2b0639386020df6c24ff1416a37be6b16c46eaf8f6b69` |

The three released script D-comment changes themselves need no further correction. They are nevertheless
compiler/toolchain inputs: old qualified profile/input bindings do not become current merely because
non-comment bytes match. Coordinator reconciliation after acceptance remains necessary; no profile was
touched and this review grants no old binding current status.

## Return and stop

Return R1/R2 to the external editor for a bounded successor, preserving the original19/prior32 hashes and
both histories. Re-review changed documentation/classifications and fresh successor identities; do not
restart a whole audit. Runtime/filter/gate/logging/data, installed-producer, final-profile, Windows and
physical gaps remain their existing handoffs. Windows concurrency is UNREADY/UNREVIEWED; PIV touch/hotplug
and touch-dependent cancellation/recovery remain user-deferred. Serial9681620's cause remains unexplained;
serial125's setup6A88-before-RESET supplies no protected-reset result.

After the corrected batch's acceptance and the separately owned existing gates, publish Core Pass1,
provide the deferred-finding handoff, then stop. This review starts no Pass2 or new validation campaign.
