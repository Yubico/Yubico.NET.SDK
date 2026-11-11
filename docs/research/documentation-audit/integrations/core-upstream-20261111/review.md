# Independent integration source review

**Verdict: PASS for the resolved integration source. No required corrections. No optional changes recommended.**

Fresh independent GPT-6.1 Sol review, November 11, 2026. This reviewer authored neither the resolution nor
the validation preparation. Astra may assign the planned validation lanes against the reviewed source.
This verdict establishes integration-review acceptance, not validation success, publication, Core Pass-1
completion, or hardware/platform qualification.

## Reviewed identity and authority

| Input | Exact identity |
| --- | --- |
| Checkout / branch | `/tmp/opencode/core-upstream-20261111`, `yubikit-core-docs-integration` |
| Campaign / current HEAD | `f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa` |
| Incoming / MERGE_HEAD | `5746e72ed9a848d08b5782ddde025aa4a4b522ef` |
| Common ancestor | `b1362127203beda5e9c797e8648b28e2716c496c` |
| Reviewed pending product tree | `899dade50586c55efad0031e0a001498a11f5a26` |
| Product index entries | 7,196; exact recursive mode/path/blob equality with that tree |

Read root instructions from both parents and the merged checkout, the applicable module/test guidance,
the campaign workflow/style, both planning records, coordination/progress, resolution/deletion decisions,
validation preparation/supplement, and their source/evidence references. Both-parent intent was compared
through the overlap inputs and actual Git/source differences. The 45 changed-path overlaps were
independently recomputed from the common ancestor. They remain distinct from the captured 17 actual
conflicts: 14 content and three modify/delete. The current unmerged index is empty, and there are no
unstaged tracked changes.

The PR #745/common-ancestor association remains as adjudicated in planning. A different description in
the Git commit message supplies no reason to rewrite that recorded GitHub association.

The review writes only this file. It ran read-only Git/file/hash checks and the inspected read-only
`validation/check-preserved.py`. It ran no build, test, formatter, native host, device operation, image
generation, staging, commit, push, fetch, or subagent. Integration records remain outside the product-tree
identity until Astra stages them.

## Consequential acceptance decisions

### OATH shared selection and the friend-assembly exception

Accept `src/Oath/examples/OathTool/Cli/Prompts/DeviceSelector.cs` and the deliberate
`InternalsVisibleTo Include="OathTool"` addition in
`src/Cli.Shared/src/Yubico.YubiKit.Cli.Shared.csproj`.

The incoming selector migration removes `DeviceSelectorBase` and the duplicated tool-local discovery,
metadata and picker implementations. Applying the incoming OATH adapter verbatim would also remove the
campaign's injected console/discovery parameters while its surviving `OathToolServices` and real
cancellation tests still call them. Keeping the old local implementation would instead bypass the new
shared production selection policy. The resolved adapter reconciles these two real consumers:

- Normal selection uses the existing public shared constructor with SmartCard eligibility and
  `FirstEligible`; discovery observes All interfaces before filtering, retaining full composite views.
- Injected discovery uses the existing internal constructor, keeping filtering, metadata handling,
  retry and the real Spectre picker in the same shared implementation. The metadata delegate still
  calls the actual Management convenience API with the named cancellation argument.
- The friend grant is **assembly-wide**, not constructor-only. Its actual use is confined to this
  adapter's existing seam. `Cli.Shared` is explicitly non-packable, and neither shared selector public
  signatures nor shipping Core APIs are widened. This is a justified integration/testability tradeoff,
  not permission for additional friend grants or a new SDK injection framework.

Reviewed the complete shared selector, OATH adapter/services/session helper, the affected dispatch
lifetime, and the complete `OathToolDeviceSelectionTests`, `OathToolCancellationTests`,
`OathToolDispatchTests` and shared `DeviceSelectorTests`, rather than treating compilation or a grep
match as proof of preserved behavior.

The cancellation paths reconcile as follows:

1. Discovery checks cancellation before and after the supplied call. Retry and selection use
   `ShowAsync` with the supplied token; an `OperationCanceledException` from a metadata read escapes
   the best-effort exception filter. A different metadata failure rechecks the token before becoming
   missing display information.
2. The picker uses integer indices so identical labels remain distinct. Its explicit Cancel sentinel
   is `-1`, mapped to a null selection; it introduces no sentinel exception. The campaign picker also
   returned null for explicit Cancel. Token cancellation and choosing Cancel remain separate paths.
3. `OathToolServices.OpenSelectedSessionAsync` checks the token again after selection and before
   `CreateOathSessionAsync`. Thus a metadata task that finishes or fails after cancellation cannot
   lead to the later OATH open. The retained fake-device tests exercise this path, plus blocked retry/
   selection and metadata cancellation. They still require actual execution by the validation owner.
4. The surviving `OpenSession` wrapper disposes a session returned after cancellation. Unsuccessful
   unlock disposes its untransferred session, clears the derived key and prompted password, and leaves
   caller-owned password storage alone. Credential-reader null still becomes the campaign's existing
   cancellation exception; that policy was not introduced by shared selection.
5. Cache reuse, sequential fallback reads, index-based labels and changed diagnostics come from the
   incoming shared selector lineage. They are accepted upstream behavior, not new Pass-1 fixes.

`OathToolApplication.ExecuteAsync` correctly removes only obsolete global `StartMonitoring`/
`ShutdownAsync` scaffolding around the campaign's real dispatch. Password owners still enclose the
awaited command, cancellation still reaches commands, parser/help gates remain before discovery, and
the Program Ctrl+C handler is unchanged except EOF termination. There is no reason to invent a
replacement application-global monitor lifetime for one-shot selection.

The session helper still returns `IOathSession?`, not the incoming older selection/session tuple.
Campaign services and command consumers remain consistent with that return shape. Its only integration
change is EOF termination. No test assertions were weakened or discarded to accommodate the adapter.

### Shipping Core, API and native lineage

Every incoming shipping Core C# file was independently compared using Git-clean blob identities. The
sole differing incoming path is `src/Core/src/Credentials/UserPresenceIntent.cs`, exactly the campaign
blob. The added Base32 source and its unit test are also exactly the campaign blobs. In particular,
Linux ownership workers/reactor, descriptor-int imports and fd-zero handling have not been edited by
the resolver. The discovery ownership split, fresh finder retirement, monitor and `DeviceChange` source
remain incoming. These are preservation conclusions, not a fresh whole-Core correctness audit.

The source/snapshot comparison agrees:

- Incoming: 1,439 nonblank/non-directive API entries.
- Resolved: 1,442 distinct entries, preserving every incoming entry.
- The only additions are public `Core.Utilities.Base32` and its two actual `Decode` overloads.
- The five public manager methods have the actual predicate/interfaces/token parameter order. Old
  token-only and global lifecycle APIs stay removed. Monitor watch/disposal and all four change-record
  types, revision/member shapes and generated snapshot identities remain incoming.

The 16-path noncomment comparison was independently replayed: removing only whole `//` lines and
normalizing line/EOF endings leaves every other line equal to incoming, including imports, directives,
literals, assertions and executable bodies. This supports the runtime-overlap preservation claim; it
is neither Roslyn-token verification nor a compiler/API-analyzer result. Incoming runtime changes,
including PIV authentication changes, must be attributed to the incoming lineage rather than reported
as documentation-pass implementation fixes.

### Active guide migration and preserved campaign contracts

The resolved discovery examples and contracts use the actual fresh Find API, named cancellation
arguments, `interfaces` scope, owned monitor factory and record-pattern flow. Source tracing through
`YubiKeyDiscovery`, `YubiKeyMonitor`, `DeviceChangeFeed` and `SnapshotDiff` supports the affected lifetime
explanations: one-shot finder retirement, manager-owned producer versus shared-monitor subscription,
initial replay/completion marker, subscriber-local cancellation, retained-object metadata updates,
overflow recovery and deterministic owner disposal. The complete generic-host example retains both
`MonitorLifetime` and its outer `await using` ownership; UI helpers identify real application callbacks.

Campaign corrections survive where incoming still had older examples: non-generic Management creation,
real `SessionCreationOptions`, named PIV cancellation, SCP-parameter ownership and disposal order,
process-local grouped-member bounds, OTP effective/raw-version distinctions, and qualified test assertions.
The primary OATH guide and shared user-interaction guide remain byte-identical to the campaign blobs.
Base32 cleanup/borrowed-input guidance remains consistent with the preserved decoder. This integration
does not reopen or repair unrelated frozen examples or claim those historical compilations cover the
new API.

The new/changed prose is acceptable for this bounded migration: it identifies actors, current behavior,
resource scopes and consequential limits. No optional voice rewrite is needed. Known CTAP destructive
reselection, Management metadata interpretation and sample cancellation limitations stay qualified in
their surviving owners; none receives implementation closure from this review.

## All 45 overlap dispositions

Every row is accepted against the reviewed tree and both-parent intent. “Accept” includes clean merges;
it does not imply that Git reported a conflict for that path. The exact per-path blobs are bound by
`evidence/overlap-resolution-identities.json` and the product-tree identity above.

| Path | Independent disposition |
| --- | --- |
| `CLAUDE.md` | Accept current discovery ownership while preserving Management/SCP corrections. |
| `README.md` | Accept new tracking ownership alongside campaign task/access-tier guidance. |
| `docs/TESTING.md` | Accept removed global monitoring and retained transport/cache/setup qualifications. |
| `docs/architecture/connection-ownership-and-contention.md` | Accept process-local/grouped scope and incoming discovery/native ownership. |
| `docs/architecture/device-discovery-guarantees.md` | Accept fresh/monitored distinctions and campaign OTP version provenance. |
| `docs/architecture/images/image-manifest.txt` | Accept campaign L2/CTAP and incoming L4 identities; static hash reconciliation agrees. |
| `docs/architecture/physical-device-model.md` | Accept owned monitoring, options-based examples and qualified disposal/cache language. |
| `docs/architecture/raw-access-tiers.md` | Accept named PIV token and incoming Linux typed/direct compatibility boundaries. |
| `docs/architecture/sdk-architecture-diagrams.md` | Accept combined campaign CTAP trace and incoming discovery diagram. |
| `docs/migration/v1-to-v2-gaps.md` | Accept retained applet qualifications and incoming discovery status. |
| `docs/migration/v1-to-v2-map.yml` | Accept combined migration evidence; unrelated historical YAML issue remains deferred. |
| `docs/migration/v1-to-v2.md` | Accept fresh Find/interfaces/replay migration with campaign options/lifetimes intact. |
| `docs/v1-to-v2-comparison.md` | Accept shared-monitor ownership and campaign applet/codec qualifications. |
| `docs/v2-highlights.md` | Accept producer/subscriber distinction and retained applet boundaries. |
| `src/Cli.Commands/src/Infrastructure/YkCommandBase.cs` | Accept incoming single enrichment and context metadata reuse. |
| `src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs` | Accept live-read/options API with retained metadata interpretation limits. |
| `src/Cli.Commands/src/Management/ManagementConfigCommand.cs` | Accept incoming live read and corrected displayed-field summary. |
| `src/Cli.Commands/src/Management/ManagementResetCommand.cs` | Accept live prerequisites and campaign product/reset qualifications. |
| `src/Core/CLAUDE.md` | Accept incoming discovery/Linux ABI guidance plus campaign Base32/disposal corrections. |
| `src/Core/README.md` | Accept fresh Find/owned tracking and campaign Base32/secret contracts. |
| `src/Core/src/PublicAPI.Unshipped.txt` | Accept exact incoming set plus three actual Base32 identities. |
| `src/Ctap2/CLAUDE.md` | Accept campaign protocol qualifications and incoming Linux/discovery context. |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs` | Accept shared policy and retained AID/firmware/target-binding guidance. |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoDeviceSelector.cs` | Accept deletion of obsolete alternative, together with removed shared base. |
| `src/Ctap2/examples/FidoTool/Program.cs` | Accept incoming lifecycle removal with campaign parser/reselection limits. |
| `src/Management/CLAUDE.md` | Accept corrected named-token enumeration and retained Management contract detail. |
| `src/Management/README.md` | Accept fresh reboot rediscovery with campaign recovery/metadata/lifetime safeguards. |
| `src/Management/examples/ManagementTool/Cli/Prompts/DeviceSelector.cs` | Accept SmartCard/RequireChoice tool policy and form-factor caveat. |
| `src/Management/examples/ManagementTool/Program.cs` | Accept incoming one-shot dispatch/menu lifetime, without global shutdown. |
| `src/Management/tests/CLAUDE.md` | Accept scoped/owned test lanes and campaign authorization/transport limits. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ReconfigurationDiscoveryInvariantTests.cs` | Accept incoming executable fixture and campaign precise assertion/restoration explanations. |
| `src/Oath/examples/OathTool/Cli/Prompts/DeviceSelector.cs` | Accept shared production selection with preserved console/discovery cancellation seam. |
| `src/Oath/examples/OathTool/Cli/SessionHelper.cs` | Accept campaign IOathSession ownership/cleanup flow; EOF-only integration difference. |
| `src/Oath/examples/OathTool/Program.cs` | Accept campaign dispatch/Ctrl+C; apply monitoring removal in surviving application owner. |
| `src/OpenPgp/examples/OpenPgpTool/Cli/Commands/OpenPgpCommand.cs` | Accept incoming active adapter and retained mutation/credential ownership qualifications. |
| `src/OpenPgp/examples/OpenPgpTool/Cli/Prompts/OpenPgpDeviceSelector.cs` | Accept deletion; useful active-path guidance survives in command/static adapter owners. |
| `src/Piv/CLAUDE.md` | Accept incoming selection context and campaign PIV contract/evidence boundaries. |
| `src/Piv/README.md` | Accept shared selection references while preserving campaign task/secret/state guidance. |
| `src/Piv/src/Authentication/AuthenticationProtocol.cs` | Accept incoming executable source and campaign factory-key/PIN comments. |
| `src/Piv/src/Cryptography/PrivateKeyOperations.cs` | Accept incoming executable source and campaign full-width padded-block rationale. |
| `src/Tests.TestProject/Controllers/DiTestController.cs` | Accept entire obsolete sample-project removal; retain historical corrected blob, not dead architecture. |
| `src/YubiHsm/examples/HsmAuthTool/Cli/Prompts/DeviceSelector.cs` | Accept shared SmartCard/FirstEligible policy and applet-versus-interface distinction. |
| `src/YubiHsm/examples/HsmAuthTool/Program.cs` | Accept incoming one-shot lifetime and campaign diagnostic-secret warning. |
| `src/YubiOtp/CLAUDE.md` | Accept incoming Linux cross-layer context and campaign challenge/CRC/secret qualifications. |
| `src/YubiOtp/examples/OtpTool/DeviceHelper.cs` | Accept incoming shared result/policy/stderr behavior and retained OTP HID/presence caveats. |

The three modify/delete histories were checked at the actual deletion commits:
`f1ee781dee565fd3d7c5116a707f14960cf6e588` deletes the FIDO/OpenPGP alternatives;
`e64eecdca1b867c039f2ff077a3bcbb6b094e1dc` removes Tests.TestProject. Current shared selection and scoped
Management examples retain the useful current guidance. Recreating these deleted classes or the sample
project would reintroduce removed architecture rather than preserve a necessary consumer.

Outside the overlap list, accept the narrowly described OATH application/friend adaptation, shared
`DeviceSelection.Info` interpretation guidance, and FidoTool/ManagementTool README migration. The
incoming Cbor 10.0.15 pin and external Linux verification host remain intact.

## Verification receipts and exact bindings

Fresh read-only checks returned exit 0:

- HEAD/MERGE_HEAD/common-base identities and 30/74 unique-commit counts.
- Exact recursive index equality with the reviewed product tree, empty unmerged index and no unstaged
  tracked changes; independent 45-path overlap-set reconciliation.
- Incoming shipping Core blob comparison, exact campaign Base32/source-test preservation, API set and
  uniqueness reconciliation, and independent replay of all 16 noncomment comparisons.
- `python3 -B docs/research/documentation-audit/integrations/core-upstream-20261111/validation/check-preserved.py`:
  both checkouts retain eight manifest identities, 5,366 frozen artifacts and 114 prior integration files;
  the primary retains its four original user files and campaign HEAD. No archive repair was performed.
- Independent read-only hashing of all seven Mermaid bodies and their SVG/PNG files against the merged
  manifest: every source/image digest agrees. This is static consistency evidence, not execution of the
  complete architecture gate or image regeneration.

| Reviewed binding | Identity |
| --- | --- |
| Overlap parent inputs, SHA-256 | `652957f035f25be75f247fceb31be43201be6a5c748d65d2ae5225f4681251fb` |
| Resolved overlap identities, SHA-256 | `5f7e11a6d7f28c42858ec1556c954ba732d0af84a3fb9b0fd849acf6f3884c72` |
| Actual conflict record, SHA-256 | `c97bf62622770ce7c805a93cf71f32d8d59253eab1dd499d1e83be2ae9c1ff85` |
| Pre-resolution index receipt, SHA-256 | `05c613c221fd228e0ade1554045c3914e7843ddb01087501739f32b3ee94d8ba` |
| `resolution.md`, SHA-256 | `960f84b742166f2d5545c1103bd79f43a065d72fbaa47ee2b960309de184372b` |
| `deletion-review.md`, SHA-256 | `fc2610fe6c7b680fbf4a452864619e8073373c7ff487e5efac2acc4c142a2cec` |
| OATH selector, Git-clean blob | `598ad5856e8da0ba5eff7dc8141375fd6e34aa5b` |
| Cli.Shared project, Git-clean blob | `a7e6eaa39bec4293bb1c8f59868b0bdeabe5440d` |
| OATH application, Git-clean blob | `6a5cdc781eb1027abc251c37d252ce8d575c0fae` |
| Shared selector, Git-clean blob | `3a599509c93e8957c38bb7f5af76906aa31d519c` |
| OATH services, Git-clean blob | `d8b06a43184d6954cce0b7b51a65f792161e6421` |
| OATH selection tests, Git-clean blob | `f765b9be9910e09d311047ec1ef8ceb8808a77aa` |
| OATH cancellation tests, Git-clean blob | `b9499cd342b4ea607f7fac50ce3bbdfffef23253` |
| OATH dispatch tests, Git-clean blob | `2c46d4e991119f5b937cce75dbac1a05f2e6cd44` |
| Core API snapshot, Git-clean blob | `e16b5c9b2127e5e48056919d361b0821b49b40af` |
| Campaign OATH guide retained, Git-clean blob | `85b2f1f51f7619d93acdf38be5f770fa8f5702c4` |
| Campaign user-interaction guide retained, Git-clean blob | `463b4ea474e81b7eb7880994c31b2b49ab3937fc` |

## Remaining release gates

Astra still owns the required clean solution/unit/docs/architecture/complexity/resilience and CI-parity
checks, separate Linux publish/native-only/evidence-gate transaction, formatter scope if assigned,
and final preservation/staged-byte/publication verification. Preserve initial failures before scoped
corrections and request re-review of any changed accepted integration source.

The 25 prepared current example bodies and candidate projects remain **provisional and uncompiled**.
They are not audit coverage, compiler provenance or proof that all affected examples have been captured.
The assigned follow-up must reconcile complete final bodies/XML/example consumers, compile the actual
sample projects and retain current compiler/dependency evidence. The root SCP example needs its real
test-project context in that follow-up.

No integration hardware lane was executed or authorized by this reviewer. Retain the user's PIV
physical deferral and distinguish planned attended discovery acceptance from the no-hardware lanes.
Whole-Core investigation starts only after Astra completes the separately validated, pushed integration
checkpoint and verifies ancestry/protected bytes. This source-review PASS supplies no broader closure.
