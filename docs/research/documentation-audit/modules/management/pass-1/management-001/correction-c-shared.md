# Management C shared-review corrections

## Status and edit boundary

Editor: GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03. All required CSH-R1..R3 findings in
[review-c-shared.md](review-c-shared.md) were verified against actual source before editing. This round
changes only these existing files in `/tmp/opencode/management-001-edit-c`:

- Root `CLAUDE.md` — non-generic ManagementSession, caller-owned default SCP parameters in the real
  integration example, and the actual fakeable SCP initialization/testing boundary.
- `src/Tests.Shared/README.md` — owned default SCP parameters in Quick Start and the adjacent Filtered
  Test; precise actual helper/reference and reset prerequisites for the latter.

Updated PRIMARY `edit-c.md` mapping/final snippet manifest and wrote this separate correction report.
Original reviews, investigations, triage and historical archives remain unchanged. The four module
correction files retain exactly the hashes in `correction-c-module.md`; this shared round does not reopen
those edits. Both review correction sets are **ready for focused independent follow-up**, not claimed PASS.

Read the complete shared review, relevant root/Tests.Shared teaching blocks, the actual default-key
construction/disposal paths already verified for C-MOD-R1, current Management factories and the named
integration test. For the new narrow source dependencies, read Core test instructions, SecurityDomain
module/test instructions and the actual test-helper source/project. These are dependency reads, not a
Core or SecurityDomain audit. `review-receive`, `docs-module` and secret-ownership guidance remain loaded.

No C# source tokens, executable strings, API shapes, tests, project references or runtime behavior were
changed. No build, test, hardware operation, example execution, commit or delegation occurred. No
compile-source file or no-op helper was created. Build/test/commit skills were not actioned because
those operations remain prohibited. The report's references to test assertions are static reading,
not newly passing tests.

## Original required findings and evidence — preserved verbatim

The following is the original required-findings section of `review-c-shared.md`. Its locations refer
to the reviewed pre-correction bytes. The original reviewer file is preserved unchanged.

```text
### CSH-R1 — reconcile the remaining generic Management session claim

- **Location:** `CLAUDE.md:168`, Architecture / Application sessions.
- **Status:** required, unresolved accepted `MGMT-DOC-011` dependency (investigation C external manifest).
- **Observed:** the root still describes sessions as generic over connection type and gives
  `ManagementSession<TConnection>` as its current example.
- **Independent evidence:** `src/Management/src/ManagementSession.cs:30` declares
  `public sealed class ManagementSession : ApplicationSession, IManagementSession`; its actual factory
  at `:67-77` takes `IConnection` and returns `Task<ManagementSession>`. The public device extension at
  `IYubiKeyExtensions.cs:101-103` also returns that non-generic type.
- **Correction:** narrowly describe the actual non-generic Management session over supported connection
  types. Do not redesign root architecture or the generic-type policy. Update the edit/reconciliation
  record to include the missed dependency. The coordinator independently steered this candidate during
  review; the source declaration above confirms it rather than treating steering as evidence.

### CSH-R2 — keep caller-owned SCP parameter disposal visible in the complete changed examples

- **Locations:** root `CLAUDE.md:525-536`, integration test example;
  `src/Tests.Shared/README.md:37-46`, second Quick Start test; and the analogous full Filtered Test
  at README `:161-175` (inline default at `:173`). The last is an unchanged adjacent example in this
  changed document, explicitly included by the coordinator's cross-document ownership steering.
- **Status:** required ownership/example correction under `MGMT-DOC-006/011`.
- **Observed:** all three complete examples create `Scp03KeyParameters.Default` inline and never dispose the
  returned caller-owned object. Correct session disposal does not dispose these parameters. This is not
  a compile failure and must not be hidden by compiling a stub or deleting the SCP task.
- **Independent evidence:** `Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs:45-58`: every `Default`
  access constructs a new object; its `Dispose()` disposes `Keys`. The base
  `ScpKeyParameters.cs:20-28` implements `IDisposable`. Management passes parameters to initialization
  (`ManagementSession.cs:67-107`); it does not own them. `ApplicationSession.cs:509-565` disposes protocol
  state and only an owned connection, not the supplied key-parameter object. The actual named integration
  test, `ManagementSessionSimpleTests.cs:193-209`, uses `using var scpKeyParams = Scp03KeyParameters.Default`
  before creating its connection/session. `StaticKeys.cs:52-61,83-99` confirms separate owned ENC/MAC/DEK
  arrays and deterministic zeroing by disposal. The filtered example's real project-specific
  `SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs:31-66`
  forwards the supplied parameter through options and disposes its sessions/connection, **not** that
  parameter. Core guidance also explicitly calls for SCP key disposal. This narrow dependency read is
  not a SecurityDomain module audit. The module reviewer's analogous finding is supporting reconciliation;
  the independently read construction/disposal paths determine this judgement.
- **Correction:** introduce `using var scpKeyParams = Scp03KeyParameters.Default` before the owned session
  or helper call, and pass that local through the current options/named helper argument. Preserve the
  awaited real metadata query, assertion, cancellation handling and session scope. Keep the existing
  dedicated SmartCard/default-SCP-key execution prerequisite. These particular default bytes are public:
  this finding is about truthful ownership and deterministic cleanup, **not an allegation of exposed
  non-public key material**. No production lifecycle change is requested.

### CSH-R3 — the touched SCP limitation example still asserts an unsupported impossibility

- **Location:** root `CLAUDE.md:550-563`, When You Can't Write Meaningful Tests / Example (complete
  XML/comment teaching block, not only the renamed integration-test token).
- **Status:** required narrow factual correction; no broader testing-policy rewrite requested.
- **Observed:** after updating the integration-test reference, the block still says actual SCP
  initialization cannot be unit tested because `ScpState.Scp03InitAsync` is static, and the body says to
  test only routing. Static dispatch alone is not the current source's testing boundary.
- **Independent evidence:** `Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs:123-138`
  constructs a real `FakeApduProcessor`, queues an INITIALIZE UPDATE response, calls that exact static
  `ScpState.Scp03InitAsync`, and asserts `BadResponseException` for response lengths 0 and 28. The actual
  `ScpInitializer.cs:113-125` calls the same method through an APDU-processor argument. Thus initialization
  behavior already has a fakeable wire seam. The test read establishes malformed-response coverage, not
  a successful full handshake or a newly passing test run.
- **Correction:** preserve the surrounding policy, but make this small example distinguish fakeable
  initialization/protocol behavior from real-device interoperability that requires integration testing;
  alternatively use an explicitly illustrative limitation that does not make this false claim about
  current Core. Retain the verified current Management integration-test reference if relevant. Do not
  claim comprehensive unit coverage from the one test or open a Core refactor.
```

## Fresh verification and applied corrections

| ID | Source verified by editor | Correction and remaining limit |
| --- | --- | --- |
| CSH-R1 | Management's actual non-generic declaration/factory in `src/Management/src/ManagementSession.cs:30,67–77`; device extension at `src/Management/src/IYubiKeyExtensions.cs:101–124` returns `Task<ManagementSession>` and passes a supported opened connection into that same factory. | Root Architecture / Application sessions now names the non-generic class and supported SmartCard/FIDO HID/OTP HID connections through IConnection. Other architectural/type-selection policy is unchanged. This reconciles the missed DOC-011 dependency, not an API redesign. |
| CSH-R2 | `Scp03KeyParameters.Default` creates a fresh object, `ScpKeyParameters` is IDisposable, and StaticKeys clones/clears its arrays. Management initialization borrows parameters, while ApplicationSession disposes protocol/owned connection. The real Management integration test declares a using local before session creation. Read all of the actual SecurityDomain `TestStateExtensions.cs`: supplied parameters flow into body-session options, while the helper disposes reset/body sessions and its WithConnectionAsync-owned connection, not input keys. | All three snippets now declare `using var scpKeyParams = Scp03KeyParameters.Default` before the session/helper and pass that local through current options/named arguments. Root retains tokenized discovery, actual factory, query and assertion. Quick Start retains its real metadata query/assertion. Filtered Test retains resetBeforeUse, real helper, ListKeyInformationAsync and NotEmpty. Root/readme prose explains caller ownership and session/helper completion before parameter disposal. No public-default secrecy allegation or production lifecycle change. |
| CSH-R3 | Read `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs:123–138`: InlineData 0/28, real FakeApduProcessor, queued response, real static Scp03InitAsync call and BadResponseException assertion. Read actual `src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs:113–125`: same static method receives an APDU processor. No static-mocking requirement is present at that seam. | Root's small XML/comment example now explains fakeable initialization wire behavior, names that exact malformed-length test, and distinguishes device interoperability handled by the actual Management integration test. The class shell is explicitly illustrative, not the Core implementation or a substitute used for compilation. Surrounding general testing policy is preserved. No successful full-handshake unit test, comprehensive coverage or newly executed result is claimed. |

Context7 `/dotnet/docs` query on using declarations/await-using scope returned the current
[Implementing DisposeAsync](https://github.com/dotnet/docs/blob/main/docs/standard/garbage-collection/implementing-disposeasync.md)
guidance: cleanup declarations are scoped to the containing method/block and dispose in reverse declaration
order. This corroborates the language lifetime explanation; actual ownership follows the repository
construction/disposal source, not a framework assumption. No application was run to establish that order.

## Final snippet and actual-helper compilation handoff

`edit-c.md` now classifies all three corrected SCP examples as current compilation targets with real
calls and lifetimes. No fence is added or removed in this round; only their contents/prerequisites change.

- **Root integration member:** current complete xUnit **v3** member with actual Core/Management references,
  `Xunit`, Core.Devices/Sessions/Scp and Management imports. Preserve `TestContext.Current.CancellationToken`
  through discovery, factory and read. Preserve the real `Scp03KeyParameters.Default` using local and
  session declaration so async session teardown occurs before parameter disposal. Execution requires an
  authorized dedicated SmartCard key configured with those defaults; compilation must not execute it.
- **Tests.Shared Quick Start, fence 1:** current complete class under actual xUnit **v2**/SkippableFact,
  Tests.Shared/Core/Management dependencies. Keep both tests, both real reads and assertions. The second
  test's local parameters outlive the awaited helper; helper-owned session/connection end before the
  outer using local disposes. No inline disposable factory remains in that test.
- **Tests.Shared Filtered Test, fence 5:** now an **edited current composable test-member fragment**, not an
  unchanged/excluded sample. Its using directive belongs at compilation-unit scope; wrap the complete
  method in a real test class. It still supplies SCP03 defaults despite its historical Scp11 method name;
  new prose makes that explicit rather than claiming an SCP11 handshake. Keep the actual reset-before-use
  helper call and key-information task; never replace the helper with a no-op stub or remove reset/SCP.
- **Root SCP limitation/comment fence:** explanatory **sketch**, explicitly not the Core implementation.
  It states a source-backed testing boundary; compiling its empty illustrative class would not validate
  SCP. Real production initialization/test definitions remain unchanged and outside this editing scope.

### Exact Filtered Test references, types and prerequisites

The actual helper is public `TestStateExtensions` in
`Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions`, at:

`src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`

It is **not** shipped by Tests.Shared. For compile extraction, use either:

1. The actual project/assembly from
   `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Yubico.YubiKit.SecurityDomain.IntegrationTests.csproj`
   and its normal dependencies; or
2. Link that exact existing helper file as source, with real project references to
   `src/SecurityDomain/src/Yubico.YubiKit.SecurityDomain.csproj`,
   `src/Core/src/Yubico.YubiKit.Core.csproj` and
   `src/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj`.

The actual integration project targets net10.0, references xUnit v2 and `Xunit.SkippableFact`, and has
those SecurityDomain/Core/Tests.Shared project dependencies. Use C#14/nullable and normal System
Threading/Tasks imports. Import `Xunit`, `Yubico.YubiKit.Core.Devices`,
`Yubico.YubiKit.Core.Protocols.SmartCard.Scp`, `Yubico.YubiKit.Tests.Shared`,
`Yubico.YubiKit.Tests.Shared.Infrastructure` and the real helper namespace. The real helper signature is:

- Receiver: `YubiKeyTestState state`.
- Parameters: `bool resetBeforeUse`, `Func<Yubico.YubiKit.SecurityDomain.SecurityDomainSession, Task> action`,
  `ProtocolConfiguration? configuration = null`, `ScpKeyParameters? scpKeyParams = null`,
  `CancellationToken cancellationToken = default`.
- Return: `Task`.

The fragment itself obtains its actual `Scp03KeyParameters`; no fabricated key/provider prerequisite is
needed. Execution would reset SecurityDomain on an authorized dedicated SmartCard key matching the
firmware/capability row, requiring suitable device/key state for the real helper's reset/default-key
setup. Neither the filter nor compilation proves reset/interoperability. No operation is authorized or
executed by this handoff. The narrow helper read does not qualify SecurityDomain's broader reset behavior.

## Final hashes, diff check and follow-up

| Shared correction file | SHA-256 after correction |
| --- | --- |
| `CLAUDE.md` | `a04b35ebbbba9bb6b2ecb6ce5f95aa8c734b5635a0243e4b37642ba48374074b` |
| `src/Tests.Shared/README.md` | `f4524f77daf7e2d68e45e741bb95a247506a8f109ad289b0a4a753f9f5f29deb` |

Recomputed all four module-correction file hashes; all still match `correction-c-module.md` exactly:
`42eae875…`, `e5a631ff…`, `3143005a…`, `081220c3…` in that record's file order. No module file was changed
by the shared correction round.

`git diff --check da61d2e6` in the C worktree completed with exit 0 and no output after shared edits,
covering the entire current C checkout diff. This is whitespace checking only, not a build, test,
formatter, compiled-snippet or token/API-preservation result. Coordinator verification and both independent
reviewer follow-ups remain outstanding. All MGMT-IMP/MGMT-XREF behavior/device-evidence work remains
deferred. C-MOD-R1..R5 corrections and the C-10/triage help erratum request are recorded separately in
`correction-c-module.md`; their immutable original reviews/evidence remain intact.
