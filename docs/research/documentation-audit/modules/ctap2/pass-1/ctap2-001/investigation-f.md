# Investigator F — consumer journeys and shared references

Baseline: `5b09a9b6942ded3913b5c9340c3e126617e61a82`. Investigated 2026-11-03.
Status: candidates for Astra's triage, not an editing/review/validation closeout.

Read root CLAUDE, module README/CLAUDE, test CLAUDE, plan, campaign workflow/style and
docs/TESTING. Available skills were reviewed; security-guidelines was loaded for the sensitive-buffer
checklist, not used as authority for firmware/protocol claims. No build, test, hardware operation,
commit, delegation or source edit occurred. Only this report and `evidence/f/` are authored here.
All 33 owned files were read completely, including all 950 lines of the unshipped API snapshot.
External source reads are bounded dependencies, not another whole-module technical audit.
Final mechanical inclusion check: 44 module blocks and 133 external discovery paths are present in
the evidence inventories, with no missing path/line keys. Twenty-nine owned checkout hashes match
inventory.json; four csproj byte-identity mismatches with an empty git diff remain explicitly recorded
in evidence/f/references.md for coordinator reconciliation.

## Findings and dispositions

### F-01 — Identify the CTAP examples as protocol demos, not account registration/sign-in

**Candidate doc change; high usefulness/security importance.** README lines 68–159 hashes only
`{"challenge":"replace-me"}`, reuses that hash for creation and assertion, invents a new user handle,
and prints a signature without describing an RP handoff or verification. This is a usable device-operation
demonstration, not an application authentication journey. SHA-256 and a successfully decoded response do
not establish challenge freshness, ceremony type, origin, RP authorization, account binding, attestation
trust or signature validity. Explain that production input is the hash of the exact collected client-data
bytes for that ceremony; RP options/challenges/account handle come from the application's RP protocol.
Use separate ceremony inputs, explain persistent credential creation and dedicated-key prerequisites,
retain credential ID/public key for the verified RP record, and explicitly leave RP verification/upload
to the application. Recommend the WebAuthn package for client orchestration, without implying it verifies
server acceptance. README must also explain allow-list versus discoverable selection and consecutive
GetNextAssertion calls before intervening device commands.

Source: Ctap2Session.cs 207–344 checks hash length, builds/sends request and decodes response; it does
not construct client data or verify an assertion. FidoExamples/MakeCredential.cs 73–125 generates random
hash/user ID and requests ES256/rk; GetAssertion.cs 69–103 generates a random hash and returns only the
first discoverable assertion. Its hash is not even exposed by GetAssertionResult. Consequently the
sample cannot be converted into a verifiable RP response by relabeling its success as sign-in.
The menu's random-hash warning is useful but incomplete. Improve comments/README only, not executable
strings or result shapes. WebAuthn README 40–149 and 441–446 already supplies the appropriate boundary.
Original reference: WebAuthn L2, W3C Recommendation 2021-04-08, §§7.1/7.2; see evidence/f/references.md.
G owns the full standards/boundary reconciliation; this is not a duplicate protocol finding.

**Deferred IMP aspect:** supplying real client-data hashes and an application-selected stable account
handle, exposing assertion hash/context and handling multiple assertions requires a separately scoped
sample/API change, not Pass-1 executable-token editing.

### F-02 — Current examples append a second live session

**Candidate doc change; definite source-backed task failure.** IYubiKeyExtensions.cs XML 108–123 opens
`session`, then `secureSession` for the same key in the same using-declaration scope. README 35–52 keeps
`session` live; 180–183 opens `promptingSession`. README 54–55 also suggests calling the one-shot
device.GetCtap2InfoAsync while presenting an already-open session. These must be explicit alternatives,
replacement factory calls or separate completed scopes. Dispose the first session before another
connection-owning factory. The XML's `using var scpKeys = Scp03KeyParameters.Default` is correctly
caller-owned and ordered before secureSession; preserve it and state configured-key prerequisites.
CLAUDE 125–131 shows extension/direct creation as alternatives but needs typed connection/device and
non-overlapping scopes to prevent copy/paste ambiguity. Route XML edit to A.

Evidence: IYubiKeyExtensions.cs 62–64 opens a temporary session; 128–154 resolves one transport and
OwnConnection; 164–183 refuses fallback on connection failures. README's own constraints 196–199
document the one-live-connection boundary. No compiler/hardware reproduction is claimed.

### F-03 — README's UV and touch absolutes do not describe the low-level API

**Candidate doc change; coordinate B/C/G.** README 70 says creation asks for touch and nothing else;
117 says setting UserVerification alone is rejected; 165–166 says all listed methods require touch
and ClientPin commands are silent. The low-level request encoder writes a supplied `uv` boolean
without rejecting tokenless UV (Ctap2SessionRequestEncoding.cs 128–185); session validates makeCredential
up=false, hash length and duplicate parameters, not the asserted blanket UV rule. Authenticator
capability/configuration determines whether built-in UV/tokenless requests succeed. GetAssertion
accepts UserPresence=false. ClientPin.GetPinUvAuthTokenUsingUvAsync explicitly acquires biometric UV.
Presence is not invariably a new button gesture on NFC/contact transports; generic helper traffic
has no modeled notification even if the device needs interaction. Distinguish request options,
device requirements and SDK notification sites. Do not implement new UV rejection to match the prose.
Reset timing/scope and selection-version details remain A/G's authoritative technical findings.

### F-04 — Agent guide advertises obsolete APIs and extension semantics

**Candidate doc change.** CLAUDE directory tree 96 lists a PRF directory although the guide itself
correctly explains PRF belongs in WebAuthn (224–231). Tree also omits actual COSE/ARKG directories.
Extension table 195 lists parameterless WithHmacSecretMakeCredential, 197 lists WithLargeBlob and
198 says minPinLength requires a minimum rather than reporting existing authorized policy. Note 201
uses CTAP PreviewSignAuthenticationInput although the actual CTAP type is PreviewSignSigningParams;
the former is a real *WebAuthn* type, not a globally obsolete name. 187 says extensions are not
instantiated as classes, yet HmacSecretInput and PreviewSignRegistrationInput are real builder inputs.
Feature block 440 invents FeatureCtap2; actual field remains FeatureFido2 (Ctap2Session.cs 50, API 920).
Pitfall 461 says PIN padding is PKCS#7-style: ClientPin.PadPin 513–517 copies into a zero-initialized
64-byte array, not PKCS#7. Correct source-observed prose and classify schematic declarations explicitly.
No API rename is authorized. Firmware minima are SDK feature metadata, not sufficient proof of feature
advertisement, configured state or every model's support. Defer exact normative padding/version research
to B/G; the source contradiction is already decisive for this documentation.

### F-05 — Test guide examples have invalid shapes and weak assertion claims

**Candidate doc change.** tests/CLAUDE 131–145 reads `response.Extensions`; MakeCredentialResponse
has ExtensionOutputs, AuthenticatorData.Extensions and UnsignedExtensionOutputs, not that property
(snapshot 322–333). ExtensionOutput XML 29–35 has the same undefined `response.Extensions` shape;
give it an explicit AuthenticatorData prerequisite plus nullable guard or a real response property.
Route XML to E. tests/CLAUDE 43–55 omits RequiresUserPresence, has undefined setup values and no
assertion, and invents a 30-second timeout without supplying one. Module CLAUDE 411–428 combines an
undefined device field with an entirely empty creation test. Mark these sketches or replace their
Markdown/XML with meaningful test examples in the actual test-project context; do not advertise empty
methods as tests establishing behavior. CBOR versions-only examples at module 356 and test 117 are
parser fragments, not complete valid GetInfo responses: current Decode returns empty AAGUID when absent
(AuthenticatorInfo.cs 476–486). Preserve that source-observed distinction; A owns parser enforcement.
NSubstitute capture block 382–403 never actually invokes the helper between arrangement and parsing.
CapturedRequest remains default in the printed block. Typed mockResponse/setup and an awaited operation
are required to make it an executable test, rather than a mock-arrangement sketch.

### F-06 — Test command labels confuse default unit selection with integration execution

**Candidate doc change.** tests/CLAUDE 97 labels a command without --integration "all CTAP2 tests".
ToolchainContext.FromArgs 44–50 selects only UnitTests unless --integration is set; adding that flag
includes both suites, not integration *only*. Lines 104/107 actually narrow the run using FQN/trait
filters. Module CLAUDE 430–433's command without --integration therefore does not demonstrate a hardware
smoke run. Say exactly which projects are selected and which traits are excluded; smoke excludes Slow
and RequiresUserPresence, not a semantic guarantee that all PIN/state-mutating tests are safe. Module
test instructions' blanket destructive prohibition conflicts with docs/TESTING 175–207's allow-listed
non-timing destructive policy. Record for coordinator policy adjudication; do not silently relax module
instructions. xUnit v3/MTP unit and xUnit v2 integration project/runner declarations are internally
consistent (Context7 corroborated runner opt-in and JSON semantics). No tests were executed.

### F-07 — FidoTool is a partial illustrative CLI, not the full API or ykman compatibility

**Candidate doc change with deferred executable help fixes.** README 3/18 and 115 promise full API/
ykman-compatible commands/all operations; there are no large-blob, hmac-secret/PRF, previewSign, raw-CBOR
or selection flows. Program 70–80 routes only info/reset/access/config/credentials/fingerprints/help.
Create/get operations, metadata/update/sensor/min-PIN are menu-only; set-pin is also a CLI branch
(211–219) although help and README call it only interactive. ykman officially supports additional
access force-change/set-min-length options and credential-ID substring matching; this program uses
Convert.FromHexString of the full supplied ID, positional args[2] and a tiny ParseOption routine.
Describe familiar command grouping and supported subset, not replacement compatibility. Explain working
directory: Quick Start combines root-only toolchain invocation with `dotnet run` requiring FidoTool's
directory; the scoped SDK build discovery prefix does not include AssemblyName FidoTool. Provide explicit
project paths and do not claim sample compilation from the module library build.

**Deferred IMP:** help omits set-pin/new-pin; accepts invalid/unknown subcommands as usage with exit 0;
subcommand help performs device selection before showing usage. Program 682–692 accepts the next flag
as an option value and lacks validation. Fixing executable strings/parser/control flow is Pass 2.
Source dependency: scripts/toolchain/Context.cs 45, project FidoTool.csproj 6, Program routing above.
Original ykman guide read 2026-11-03; no CLI/device invocation.

### F-08 — Selection/filtering, cancellation and error UI need honest qualification

**Candidate doc change plus deferred IMP.** DeviceSelector.cs 88–93 filters exposed HidFido OR SmartCard
connections, not actual FIDO AID, CTAP version or feature availability. SmartCard-only non-CTAP keys
can appear. Single candidates auto-select; multiple candidates auto-select HID-capable/first in a
noninteractive console (63–75), contradicting README 145's unconditional prompt claim. No-device paths
still call Confirm Retry (100–105), even in a noninteractive console. The newer FidoDeviceSelector
singleton is not the selector called by Program/menus; do not document its base class as active behavior.
Duplicate formatted choices can resolve via IndexOf to the first matching label (156–177).
GetDeviceInfoAsync catches cancellation/errors and prints Debug residue (126–131). Config/bio/credential
IsSupported catches all failures, including cancellation, and returns false, so UI "not supported"
can actually mean connection/read/cancellation failure. All example operation helpers catch Exception
and return failed results, erasing cancellation provenance. Synchronous Prompt/Ask/Confirm/ReadLine
does not pass cancellation tokens; Ctrl+C sets cts.Token but cannot promise prompt termination.
Context7 confirms cancellable async prompt APIs exist; no adoption is authorized in this pass.

**Deferred IMP source paths:** DeviceSelector.cs, Program.cs, all FidoExamples operation files,
menu prompt sites, shared CommandHelper.cs 29–38 (event registration retained until process exit).
Re-review README Device Selection/Global Options/Security Notes, selector/helper XML and CLI help after
fixing selection, cancellation or error normalization. Mark Debug wording as development residue;
it is an executable string and cannot be removed in Pass 1 by comment editing.

### F-09 — Reset flow does not bind its second selection to the confirmed key

**Deferred IMP; documentation warning needed now.** Interactive ResetMenu 35–47 selects/preflights one
key; 91–109 uses a fresh unbound device selection and the *old* reset transport. Program reset 135–180
likewise selects independently after reinsert. No serial/correlation check binds the destructive operation
to the originally confirmed physical key; a different key can be selected. Program's force branch skips
confirmation *and* remove/reinsert instructions, not touch/power-cycle restrictions. README 41–42 and
153 suggests reset -f for CI/automated testing, which conflicts with the required human timing/touch
lane. Explain exact behavior, dedicated-key/human coordination and manual target verification; do not
change the selection flow or executable prompt strings. GetPreflightInfoAsync catches all failures and
substitutes defaults; missing metadata is not proof of unrestricted transports/short touch. A/G owns
the normative reset restrictions and missing enforcement finding; this is the sample target-binding issue.

### F-10 — PIN cleanup claims exceed what command-line arguments can provide

**Candidate doc change; deferred IMP provenance remains separate.** FidoTool README 149 says PINs are
zeroed immediately and 151 says never logged. FidoPinHelper 64–95 borrows an immutable CLI string,
encodes into an owned DisposableArrayPoolBuffer, and using scopes dispose that byte owner after the
operation. This clears bytes, not args[], shell history, process-list exposure or the immutable string.
Prefer interactive entry; explain CLI secret exposure and avoid presenting sample literals as production
practice. XML 24–25's "secure ... proper memory zeroing" needs the same boundary. All operation token
arrays are cleared in finally, including verify; incoming ReadOnlyMemory PIN is caller-owned.

**Rejected candidate:** initially suspected a privately cloned CredentialManagement token. Full source
read disproved it: constructor 67 borrows the supplied memory, Dispose 223–231 only marks disposal.
Several sample paths lack `using` on this helper, but that does not retain a secret clone. Do not file
an invented token-leak defect. EnumerateCredentialsResult owns/disposes its nested result; both CLI
and menu use using and the helper's transfer/finally path disposes untransferred results correctly.

### F-11 — Shared architecture/API wording has remaining source contradictions

**Candidate doc change; external ownership to be assigned.** sdk-architecture-diagrams.md 241–276 uses
CtapRequestBuilder.Add, CtapResponseParser and MakeCredentialData in the makeCredential sequence; actual
session uses Ctap2SessionRequestEncoding and MakeCredentialResponse.Decode. Diagram should be labelled
conceptual or use present symbols. WebAuthn extension architecture 43 lists correct current request
invariants; module guide's stale extension table must be reconciled against it, not copied back into it.
Migration map 530 says ICtap2Session takes PIN bytes explicitly; interface actually exposes credential
commands with PinUvAuth options, with raw PINs acquired through ClientPin. Clarify the helper boundary.
Do not globally replace FIDO2: DeviceCapabilities.Fido2, FeatureFido2, UserPresenceApplications.Fido2,
FIDO2 PIN, Core FidoHidProtocol, ApplicationIds.Fido, fido-u2f and FIDO_2_* are legitimate current
application/protocol identities. Removed package/session/factory names are valid only in explicitly
historical before/after sections. The point-in-time migration gaps file labels its old baseline and is
not a current authority; preserve its old src/Fido2 paths and FidoSession assertions as historical.

### F-12 — Capability/package promises need scope and realistic prerequisites

**Candidate doc change; A/D/E/G reconcile exact feature claims.** README 3 says the FIDO application on
"a YubiKey" is CTAP2.1/2.3; CLAUDE 28 says module implements these revisions; test guide 25 says USB
supports all features and 153 asserts about 25–32 slots. These are not universal model/firmware/device-
state/conformance facts. Say operations modeled by the SDK, use advertised GetInfo features/options and
actual storage metadata, and distinguish SDK firmware Feature thresholds from sufficient support.
README 14's "NFC always" still requires a current NFC connection and exposed FIDO AID. Package Description
csproj 9 says biometric authentication/passwordless login where the package supplies protocol operations,
not a complete verified login application. minPinLength reports authorized existing policy, not a
requirement-setting extension. Keep alpha/security-audit warning and experimental ARKG warnings.
No new broad product survey or standards conformance research was attempted; G owns that matrix.

## Owned-path disposition (33/33 fully read)

Paths are relative to `src/Ctap2/`; the baseline inventory carries byte counts and SHA-256 hashes.
"Clean" below means the named explanatory unit, not every behavioral branch or hardware qualification.

| Path | Disposition / semantic units |
|---|---|
| CLAUDE.md | F-02–06/F-12; all 15 fences classified |
| PACKAGE_README.md | F-12 scope; clean F-C01 alpha warning/install/transitive Core/naming |
| README.md | F-01–03/F-05/F-12; all 10 fences classified |
| examples/FidoTool/Cli/Menus/AccessMenu.cs | F-08 prompt/cancellation; F-C02 using PIN owner through awaited call; strings preserved |
| examples/FidoTool/Cli/Menus/ConfigMenu.cs | F-08 capability/error; F-12 every-operation alwaysUV string deferred; F-C03 enterprise confirmation |
| examples/FidoTool/Cli/Menus/CredentialMenu.cs | F-01 random hash boundary/first-only assertion; F-08 prompts; no signature verification claim |
| examples/FidoTool/Cli/Menus/CredentialsMenu.cs | F-08; F-C04 using credential enumeration owner and actual entry display |
| examples/FidoTool/Cli/Menus/FingerprintsMenu.cs | F-08; F-C05 per-sample progress and delete confirmation; unknown kind shown Swipe is deferred UI risk |
| examples/FidoTool/Cli/Menus/InfoMenu.cs | F-C06 displays advertised option/algorithm/limits rather than certification testing; no PIN output |
| examples/FidoTool/Cli/Menus/ResetMenu.cs | F-09 target continuity/timing/transport assumptions |
| examples/FidoTool/Cli/Output/OutputHelpers.cs | F-C07 forwarding XML/inheritdoc and ReadOnlyMemory hex facade consistent |
| examples/FidoTool/Cli/Prompts/DeviceSelector.cs | F-08 actual active selection, debug residue, cancellation |
| examples/FidoTool/Cli/Prompts/FidoDeviceSelector.cs | F-08 inactive alternative, not active selector; XML preference matches its override |
| examples/FidoTool/Cli/Prompts/FidoPinHelper.cs | F-10 immutable CLI argument qualification |
| examples/FidoTool/FidoExamples/BioEnrollment.cs | F-08 error/cancel masking; D follow-up enrollment-cancel skipped on thrown cancellation; F-C08 token finally |
| examples/FidoTool/FidoExamples/ConfigManagement.cs | F-08/F-12; F-C09 borrowed PIN and token finally |
| examples/FidoTool/FidoExamples/CredentialManagement.cs | F-08; XML 161 claims also enumerates credentials but method only enumerates RPs; F-C10 result transfer/disposal |
| examples/FidoTool/FidoExamples/GetAssertion.cs | F-01/F-08; first only, random hash not returned; F-C11 token finally |
| examples/FidoTool/FidoExamples/GetAuthenticatorInfo.cs | F-08 generic failure conversion; F-C12 query/lifetime and nonsecret GetInfo result |
| examples/FidoTool/FidoExamples/MakeCredential.cs | F-01/F-08; F-C13 defaults ES256/rk and returned demo hash; error strings are not edit-authorized |
| examples/FidoTool/FidoExamples/PinManagement.cs | F-08; verify GetAssertion permission without rpId at 155–158 (B deferred device-rejection check); F-C14 finally zero token |
| examples/FidoTool/FidoExamples/ResetAuthenticator.cs | F-09; preflight failure/default claims and destructive scope/timing to A/G |
| examples/FidoTool/FidoTool.csproj | F-07 explicit sample compile context needed; project references mechanically exist; CA1852-only suppression is explanatory, no analyzer relaxation |
| examples/FidoTool/Program.cs | F-07–09 executable help/parser/control-flow issues deferred; all PIN/credential-result using scopes read |
| examples/FidoTool/README.md | F-01/F-07–10/F-12; all eight fences classified |
| src/PublicAPI.Shipped.txt | F-C15 nullable marker only; no public prose/examples |
| src/PublicAPI.Unshipped.txt | F-C16 all 948 entries read; family reconciliation below; not edited |
| src/Yubico.YubiKit.Ctap2.csproj | F-12 description; F-C17 Core-only dependency and friend assemblies, no sample dependency |
| tests/CLAUDE.md | F-05/F-06/F-12; all five fences classified; policy conflict needs adjudication |
| tests/Yubico.YubiKit.Ctap2.IntegrationTests/Yubico.YubiKit.Ctap2.IntegrationTests.csproj | F-C18 v2 runner/reference declaration matches guide |
| tests/Yubico.YubiKit.Ctap2.IntegrationTests/xunit.runner.json | F-C19 no assembly/collection parallelism, max threads 1; configuration not device proof |
| tests/Yubico.YubiKit.Ctap2.UnitTests/Yubico.YubiKit.Ctap2.UnitTests.csproj | F-C20 executable/MTP v3 opt-in and NSubstitute/CBOR context |
| tests/Yubico.YubiKit.Ctap2.UnitTests/xunit.runner.json | F-C21 schema only; does not disable parallelism |

## Public snapshot reconciliation and generated exclusions

948 nonblank, non-directive API entries; 85 explicit type entries map to **84 top-level public types
plus public nested PreviewSignCbor.InnerAttestationObject**. All map to public source declarations;
the initial declaration search missed implicit `record` classes, resolved by a focused follow-up.
The extension receiver marker is a compiler/API-analyzer projection, not an additional public type.
Two logical device extension operations appear both as C#14 extension members (588–590) and lowered
static methods (892–893); audit their source documentation once, not as four unrelated APIs.
Record-generated Clone$, equality/operator/hash/ToString, EqualityContract, PrintMembers,
copy constructors and Deconstruct entries are excluded from invented prose coverage, while explicit
domain methods/properties remain owned by A–E. Three `~override` removed entries and the related
InnerAttestationObject ToString removed record at 931–934 are historical snapshot removals, not four
current APIs demanding documentation. No bin/obj/generated XML/AST output is part of the source inventory.
FeatureFido2 remains an actual public field, not a missed rename to repair mechanically.

## Evidence and later-documentation dependencies

- `evidence/f/snippets.md`: every module fence/XML-code classification, typed prerequisites and checks;
  external full-block inventory includes implicit WebAuthn client variables and alternative scopes.
- `evidence/f/external-paths.md`: exact external active/dependency paths and historical/generated exclusions.
- `evidence/f/references.md`: original reference and Context7 evidence, mechanical links, bounded gaps.

Reopen F-01 after client-data input/result/selection work; README common operations, CredentialMenu,
MakeCredential/GetAssertion comments and package/login claims must agree. Reopen F-03 after low-level
UV validation/negotiation/UP changes, including README PIN flow, CLAUDE PIN/extension patterns and shared
WebAuthn guide/architecture/migration. Reopen F-08/09 after selector/cancel/error/target-binding work,
including README selection/reset/security, active helper XML and all executable help/prompts. Reopen
F-10 after credential reader/CLI ownership changes; byte cleanup and string exposure remain distinct.
After D fixes helper state/disposal/enrollment cancellation, revisit administrative examples, their
capability probes and result ownership. After E fixes extension enforcement/output lifetime, revisit
all guide/XML builder/output snippets, security notes and WebAuthn architecture; no cleanup promise
may be inferred from clearing a caller copy. G reconciles full standards/product/version claims.

Limits: no compilation, runner invocation, device selection or ceremony execution. Framework/API syntax
was researched through Context7, not established by compilation. External manifest separates read
current units, discovery-only collateral and immutable archives. Consequential protocol ambiguities
are assigned to A–E/G or recorded as gaps rather than settled by sibling agreement. This report does
not claim final independent review or complete external-library/API compilation.
