# OATH boundary candidates and reopening map

All IDs are packet-local `CORE-OATH-X001+` candidates. They allocate no accepted Core IDs.
Each concern keeps documentation, runtime, maintainer decision and qualification separate.
All product/dependency files remain unchanged. Fresh independent content/citation review and an explicitly
assigned editor must adjudicate these proposals; source identity checks are a different gate.

## CORE-OATH-X001 — Explain state and borrowed metadata across the session lifetime

**Documentation:** `IOathSession` inherits Core readiness/auth without a local explanation separating
SCP setup from OATH unlock. Salt XML at lines 31–33 omits its session-owned alias lifetime. `OathSession` lines 49–55
returns cached applet state without a disposal guard; disposing zeroes salt/challenge but leaves
IsLocked, IsPasswordProtected, DeviceId and FirmwareVersion values. Public task guidance should say that
these are session observations, while operations refuse a terminal session. Describe Salt as borrowed
until disposal and direct callers to fresh creation after RESET. Avoid promising all getters throw or
that retained IsAuthenticated proves channel health after a secure-wire recovery latch.

**Runtime:** no new implementation defect allocated. Flags and metadata behavior are source facts;
B5-C001 owns common state/query semantics. **Decision:** changing terminal getter policy would require
separate API/compatibility work. **Qualification:** reset/plain/SCP fake assertions cover inherited flags
and actual old Salt views, not every cached getter or live secure reset.

**Reopen:** `src/Oath/src/IOathSession.cs` type and DeviceId/Salt/IsLocked/IsPasswordProtected members;
`src/Oath/src/OathSession.cs` properties, ApplyInitialization and Dispose; `src/Oath/README.md`
Getting started, User interaction, Constraints and Migrating reset callers; `src/Oath/CLAUDE.md`
Module Context and Reset and timestamp boundaries. Keep `src/Core/src/Abstractions/IApplicationSession.cs`,
`src/Core/src/Sessions/ApplicationSession.cs`, Core README session/query sections and
`docs/migration/v1-to-v2.md` OATH password/reset sections with the SHARED editor. Trigger: acceptance of
this local clarification or any changed readiness/getter/recovery/reset semantics. Existing terminal
reset guidance is retained, not reopened as a missing implementation.

## CORE-OATH-X002 — Scope workflow disposal to actual OATH calls

**Documentation:** OATH consumers need the actionable rule to await complete public workflows before
disposal and to keep callback/prompt resources alive. Core's guard protects one exchange, while
CalculateAll custom-period followups, AuthenticateAndRetry's provider/Validate/retry and calculation
callbacks extend beyond it. The README's cancellation paragraph correctly discusses an admitted exchange;
retain that precision and add the whole-task lifetime nearby rather than imply an operation-wide lock.

**Runtime:** secondary application of **CORE-B5-C006**, with B1 admission/drain/release distinction.
No duplicate lifetime bug allocation. Concurrent disposal may make followup fail or race post-exchange
state updates; callback-created detached work lies outside the awaited callback. **Decision:** a future
workflow lease/reentrancy policy belongs to Core and affected applets. **Qualification:** current OATH
reset held-LIST test checks overlap refusal and finally drain; it does not hold a post-wire callback or
an aggregate gap. Source race analysis is not an executed schedule or hardware failure.

**Reopen:** IOathSession type, CalculateAsync/CalculateCodeAsync/CalculateAllAsync and both retry overloads;
OathSession notification/aggregate/retry/dispose comments; README User interaction/Constraints/password
tasks; CLAUDE User-presence notifications; OathTool SessionHelper, AccessCommand and AccountsCommand
enclosing ownership. SHARED handoff: ApplicationSession/ExchangeGuard XML, Core README/CLAUDE concurrency,
`docs/usage/user-interaction.md`, `docs/architecture/raw-access-tiers.md` and migration session ownership.
Trigger: accepted clarification or actual workflow-admission/disposal change. Preserve borrowed
connection ownership and await real tasks before clearing inputs/results even if an outer wait ends.

## CORE-OATH-X003 — Qualify the secure terminal-reset oracle and actual SCP callers

**Documentation:** `OathScpResetTests.cs:76–77` says no encrypted-payload emulator is needed; explain why
that is a deliberate synthetic cleanup oracle, with no outbound encryption/MAC verification. The linked
csproj comment 27 identifies RFC 4493-validated primitive reuse; retain primitive versus whole-SCP scope.
The test observes actual Core key arrays, disposal state, command headers/order and borrowed connection
count. It is valuable even while wire/profile qualification remains outstanding.

**Runtime:** secondary handoff to **CORE-B6-C011/C012** for empty LIST/RESET/A5 commands and status/chaining;
**C002/C005/C006/C007/C009/C010** for response copies, phase, initializer cleanup and parameter/profile
questions. No duplicate wire ID or reopening of source-resolved IMP-010. **Decision:** final edition and
YubiKey profile targets must be settled by B6/coordinator, without silently treating code-cited 1.1.2.6
Public Review as final 1.1.2. **Qualification:** no live SCP terminal-reset proof. Serial 125's `6A88` prevented
the ceremony; no additional native/device/crypto execution occurred here.

**Reopen:** `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathScpResetTests.cs` complete test/fake/CMAC
assertions and UnitTests csproj lines 27–28; IntegrationTests `OathLifecycleTimeTests.ResetWithScp03_DefaultKeys_EndsSessionAndRequiresExplicitReopen`;
IOathSession ResetAsync/CreateAsync options XML; OathSession InitializeAsync/ResetAsync; README secure-channel
requirements/reset migration; CLAUDE Firmware Requirements/Reset boundaries; nested tests instructions;
TestExtensions/OathTestStateExtensions full reset/body/exception scopes. SHARED: Core SCP/response/key XML,
Core README SCP and response ownership, raw tiers, TLV/user-interaction guides, migration SCP/OATH reset.
Trigger: wire fix, changed standards/profile target, or new exact device lane. Keep first-successful-RESET
terminal behavior distinct from inability to reach that success on hardware.

## CORE-OATH-X004 — Give password-reader options their actual units and ownership

**Documentation:** `OathCredentialOptions.ForOathPassword` gives 1..128 without saying UTF-16 code units,
UTF-8 conversion, interactive filtering versus redirected rejection or that supplied byte buffers bypass
this UI preset. State those local conditions and distinguish UI choice from firmware/raw-key support.
OathTool uses Core ConsoleCredentialReader, including immutable redirected strings and retained first
confirmation entry. The prompt service/session and password owner are separate lifetimes.

**Runtime:** secondary C-C001–C004, not another Unicode/reader defect. OATH's wrappers already propagate
cancellation and clear owned prompted/derived storage; retain IMP-002 resolution. **Decision:** password
normalization/byte limit and raw access-key length stay maintainer policy, including source-run-qualified
`oath-pilot-and-fixes-wave-002:DEC-002`; CTAP-DEC-002 is unrelated. **Qualification:** fake reader tests
verify token/cleanup handoff, not real terminal responsiveness, full Unicode preservation or device limits.

**Reopen:** ForOathPassword XML; IOathSession DeriveKey/Validate/SetKey/retry contracts; OathTool
SessionHelper/AccessCommand and OathToolApplication EncodePassword descriptions; README password/security
sections and CLAUDE Access Key Derivation. SHARED: CredentialReaderOptions/ConsoleCredentialReader/reader
interface XML and `docs/usage/user-interaction.md`. Trigger: accepted unit clarification or changed
reader/normalization/raw-key policy. No new PIN/password input or reader execution authorized.

## CORE-OATH-X005 — Keep test descriptions and cleanup claims assertion-accurate

**Documentation:** Unit `OathSessionTests.cs:115–186` labels DeriveKey tests, but these call BCL PBKDF2
directly; describe determinism/difference of that direct algorithm use, not session method coverage or an
independent known-answer vector. `CredentialDataTests.cs:610–617` calls an unpadded input StandardPadded
and asserts only nonempty. Keep comments at that oracle scope; stronger exact-byte vectors already live
in OathInputTests/Core Base32Tests. `OathSessionTests` factory-failure remarks at lines 628–632 should keep their
6A82 fake retry condition, rather than imply every failed factory leaves a device reusable. Generic
cleanup/reset prose in older integration helpers must not imply restoration after arbitrary failure.

**Runtime:** selected test-scaffolding debt, separate from the fourteen fixed product scopes: integration
NewToken helpers allocate undisposed CTS; password derivation helpers create untracked UTF-8 arrays;
AuditV2 raw PUT leaves seed/payload copies uncleared and raw DELETE ignores statuses with no verification;
PasswordChange finally can attempt new-password cleanup before rotation succeeded and replace the first
failure. Unit authentication fake owns retained snapshots/response arrays but several tests never dispose
the borrowed fake. These are not assertions of real-secret compromise or product regression.
**Decision:** separately scope harness cleanup/first-error policy; keep executable assertions unchanged
in this documentation pass. **Qualification:** no execution. A successful body return or disposal count
is not native close proof or restored application state. Two consecutive HOTP values differing is weaker
than an exact counter/failure-consumption oracle (historical DEC-006 remains).

**Reopen:** those exact full methods/helpers in UnitTests OathSessionTests/CredentialDataTests/
OathAuthenticationTests; IntegrationTests OathSessionTests.NewToken/access-key/HOTP tests,
OathHashAlgorithmTests.NewToken/LockedSession, OathPasswordChangeTests.WithFreshSessionAsync/
WithDerivedKeyAsync/password workflows, and AuditV2 OathAuditReproTests.PutAsync/DeleteAsync.
Retain the newer first-failure-aware OathLifecycleTimeTests/OathProtocolResponseTests and
TestExtensions/OathTestStateExtensions patterns as bounded examples, not claims that all helpers use them.
Trigger: accepted prose correction or separately authorized test cleanup change. No new global assertion or
hardware gate is proposed.

## CORE-OATH-X006 — Make selection and example resource prerequisites explicit

**Documentation:** IYubiKeyExtensions remarks at line 42 and README Constraints at line 230 describe one physical key's
one-live-connection headline without nearby process/known-member/disjoint-view conditions. Link and retain
the useful Core bound rather than promise global exclusivity. README Getting started's unfiltered
FindFirst can select a key without SmartCard; a task example can select by SupportsConnection or explicitly
choose SmartCard discovery, explaining different scope/census behavior. OathTool's selection really uses All
and first eligible in noninteractive mode; it is not an implementation of SmartCard-scoped discovery.

README reset migration at lines 252–259 uses undeclared `ct`, and shared migration's retry example at lines 706–745 leaves
callback-returned password ownership implicit. Classify them as composable fragments with explicit typed
prerequisites, or supply complete ownership including source/result cleanup and options/parameters live
through fresh creation. Do not copy the password provider as an expression that loses its buffer owner.

**Runtime:** secondary A/B1 selection/known-member limitation only; no new discovery bug. **Decision:**
choosing All/local filtering versus a scoped view is application policy; no change to default selection
authorized. **Qualification:** current selector tests substitute discovery/live metadata while retaining
real filtering/Spectre prompt. They prove cancellation/no OATH open after late metadata, not native census,
recovery of serial 9681620 or every host's console behavior. Examples remain VALID-NOT-CHECKED.

**Reopen:** IYubiKeyExtensions create/one-shot XML; README Getting started/Constraints/Migrating reset callers;
OathTool DeviceSelector/OathToolServices/Program descriptions and device-selection test comments. SHARED:
`docs/usage/device-discovery.md`, physical-device-model/discovery-guarantees, Core README selection/sharing,
`docs/migration/v1-to-v2.md` lines 463–578 and 706–745, map OATH/reset/password entries (DOC-002 parsing remains
its old identity). Trigger: accepted task/example correction or actual selection/ownership migration.
Package README remains a useful unchanged short entry point; no new broad API guarantees are needed there.
