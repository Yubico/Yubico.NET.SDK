# Independent focused review — final-guide sampling corrections

**PASS for this exact three-guide patch.** No mandatory correction remains in the bounded return.

Accepted patch SHA-256:
`2e5a12d5c057f999a67c46a89836b899f29615943b278ee09a1d120d4eda30f9`.

| Accepted path | After SHA-256 |
| --- | --- |
| `docs/usage/device-discovery.md` | `fb4a7e2f3c6ebb50e404d2d9c41a3004e576b8956610054e3733d9e23e9d5d09` |
| `src/Core/README.md` | `61fc9fa641b9a2bd7210a6837fb09194331d47b46326187b3feb2ab0b24c830e` |
| `docs/TESTING.md` | `fe78686fa966a951ebd4134c3e9144cdecbd24ce2118378cb72b75d772a65339` |

This is the focused review of SAMPLE-A003 propagation (`core-001:unit:0b5f8163f489c670`,
`core-001:unit:c2de2d6f26065229`) and SAMPLE-TESTING001 (`core-001:unit:287e6410db16f0e7`).
Edited overlaps receive **zero clean-sampling credit**; exclusions, expansion and quota replacement
remain coordinator-owned. The architecture-map packet has a separate patch and verdict; neither
packet's acceptance is silently substituted for the other. Only this review and its identity record
were written for this packet.

## Content decisions

### Conditional lifecycle guidance — PASS

The affected complete tracking/hosted-adapter/startup/disposal/thread-safety units were read with
their surrounding task flow. `docs/usage/device-discovery.md:299–366,542–550` and
`src/Core/README.md:82–134` now preserve the established source conditions:

- `HotplugSignals.cs:50–80,83–109` attempts startup-failure cleanup and ordered teardown, but its
  warning/final diagnostic logging can throw. The prose no longer guarantees independent listener
  startup or successful cleanup irrespective of logging.
- `YubiKeyMonitor.cs:109–124,189–204` awaits constructed-monitor disposal on factory failure and caches
  a disposal task. Signals disposal precedes cancellation, loop join, token-source disposal, discovery
  retirement and feed completion, with no finally advancing later stages. Cleanup can replace the earlier
  creation error; later stages depend on earlier calls returning. Final logging can fault disposal
  after feed completion. The corrected prose does not promise total completion or a total wall-clock bound.
- `DeviceChangeFeed.cs:65–95,98–122,154–168` separates completed-feed subscription admission from
  existing channel outcomes. As established against retained runtime v10.0.0 channel completion source,
  a later normal TryComplete does not erase an earlier terminal/overflow error. Only healthy uncanceled
  tails can drain and end normally when cleanup reaches feed completion; later enumerations then end empty.
- The unchanged hosted adapter actually awaits `monitor.DisposeAsync().AsTask()` in StopAsync and has
  an outer await-using scope. Its prose now describes awaiting disposal, not unconditional successful
  cleanup, and retains consumer error/queued-work responsibility. No new lifetime broker or drain API
  is implied. The same conditions accompany the Core overview and thread-safety guidance.

These are source-order conclusions, not logger fault injection, resource-release proof or native/device
qualification. Actual guide-worktree source hashes and complete text match the packet evidence and
the previously reviewed lifecycle mechanism. No duplicate A C# edit or transfer is performed here.

### Manual runner filter and central package versions — PASS

`docs/TESTING.md:15–18,139–155` now correctly distinguishes this checkout's **v2/VSTest** mode from
**v3/Microsoft.Testing.Platform** mode. The MTP method argument is
`dotnet run ... -- --filter-method "*TestName*"`; native xUnit console `-method` is a different CLI.
The canonical toolchain recommendation is unchanged.

The retained official xUnit MTP page's option table explicitly maps `-method "name"` to
`--filter-method "name"`; it separately documents UseMicrosoftTestingPlatformRunner. A focused
independent Context7 `/websites/xunit_net` check confirms the distinct CLI experiences and argument
placement after `--`. Current moving documentation is not pinned-package execution. Newer query-filter
features or unrun repeated-switch semantics are not imported into the checkout contract.

Actual checkout evidence independently agrees:

- `Discovery.cs:29–36` checks the opt-in string defined in `Conventions.cs:15`.
- `TestTargets.cs:124–155` selects MTP `dotnet run ... --` versus VSTest `--filter`;
  `:195–242` emits `--filter-method` for the relevant terms. The existing complex-filter qualifications
  remain, rather than treating translation commentary as an executed selection result.
- `Directory.Packages.props:4,26–32` enables central package management and pins `xunit` **2.9.3**
  and `xunit.v3` **3.0.0**. Core unit-project opt-in, executable output and xunit.v3 reference contrast
  with Core integration's xunit/Microsoft.NET.Test.Sdk references and absent opt-in. Package identity,
  centrally selected version and runner mode are separate observations.

No package/project/config/translation implementation changed and no runner was invoked.

## Preservation and static checks

- All **three** complete before snapshots freshly match current PRIMARY accepted bytes and their
  recorded hashes. All after snapshots match the actual guide files. Reconstructing the three-document
  before-to-after diff gives the exact accepted patch.
- All **54** complete fences are unchanged; independently extracted text, language, boundaries and hashes
  match every before/after receipt. No new compilation eligibility or execution credit is granted.
- All **63** prior artifacts in this packet's preservation manifest remain byte-exact. All **12** source
  evidence entries match their actual source/capture bytes, including prior lifecycle evidence.
- The editor's other-23-path preservation receipt describes its pre-architecture-packet capture. In the
  current combined worktree, **22** of those paths remain unchanged; the sole additional diagram-guide
  delta is exactly the separately assigned architecture-map packet. No unrelated product delta was found.
  This distinction preserves both packet histories without claiming stale whole-worktree equality.
- `git diff --check` and read-only focused reverse-application check each exit **0**. No unclosed fence
  was found. The index is unchanged; no primary-checkout writes or Git mutations occurred.

The exact path, artifact and source identities are bound by [review-identities.json](review-identities.json).
Prior acceptance records remain predecessors, not relabeled seals for the new bytes. This PASS is not
whole-guide re-audit, compiler/test/formatter/native/device/crypto execution, sampled-clean credit or
publication. Existing qualifications and deferred Windows/PIV lanes remain unchanged. After the
coordinator's existing reconciliation/validation gates, finish Core Pass 1 publication and the precise
deferred-owner handoff, then stop.
