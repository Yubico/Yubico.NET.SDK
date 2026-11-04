# F — fresh independent documentation review

**Verdict: REQUIRED CORRECTIONS; not PASS yet.** Four bounded factual/usefulness groups remain
below. The substantive protocol-demo/RP, UV/token, reset, ownership, test-setup and experimental
qualifications otherwise pass this scoped semantic review. Optional preferences are not blockers.

**Reviewer:** GPT-6.1 Sol, fresh independently assigned session. **Date/access date:** 2026-11-04.
**Reviewed checkout:** `/tmp/opencode/ctap2-001-edit-f`.
**HEAD and comparison baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
**Output:** this report in the PRIMARY packet only. No source/document corrections, builds, tests,
hardware/UP/UV/PIN/reset operations, CLI launches, commits, delegation, integration or image rendering.

## Required corrections

### RF-R1 — the active selector still attaches the USB firmware gate to NFC

**Required factual clarification; F-08/12, CTAP-DOC-001/018; medium reader impact.**

`src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs:27–37`, specifically line29, retains:

> SmartCard transport is available over NFC and USB on firmware 5.8.0+ when the FIDO AID is exposed.

This attaches the threshold to both transports, unlike the corrected module/tool/WebAuthn guides.
Actual `Ctap2Session.EnsureSmartCardTransportSupported`, lines436–452, returns immediately for
`Transport.Nfc`; the firmware comparison applies to the non-NFC path. Initialization still selects
the FIDO AID before GetInfo/gating (126–145). Exposed SmartCard interfaces alone do not prove AID support.

Separate the conditions locally: NFC needs the current connection to report `Transport.Nfc` and
the authenticator to expose the FIDO AID; USB SmartCard uses the SDK's 5.8.0+ gate. Preserve the
new interface-filter/noninteractive/duplicate-label/target-continuity qualifications and all executable
strings/selection code. This is an inherited sentence missed inside the edited whole remarks, not a
request to change transport policy or certify older NFC devices.

### RF-R2 — FidoTool's public guide omits its fixed V2 prerequisite and verify-PIN scope defect

**Required prerequisites/failure guidance; F-08/10, CTAP-DOC-005/018 and existing IMP-010/047.**

`src/Ctap2/examples/FidoTool/README.md`, **Quick Start / access / Feature Checks** (12–24,71–84,
178–189), describes PIN and token-authorized commands but never names the fixed protocol-2 choice.
A case-insensitive search of the complete guide found no V2/protocol-2 prerequisite or missing-RP
verify warning. These facts are present in the revised sample XML, not where CLI users find the tasks.

Actual `FidoExamples/PinManagement.cs` constructs `PinUvAuthProtocolV2` for set/change/verify/retry
paths; the credential/config/bio token paths likewise select V2 without negotiation. Permission-command
fallback in `ClientPin.cs:343–352` retains that supplied protocol, not V1 negotiation. Add a concise
tool prerequisite that PIN-changing/token-authorized flows require mutually supported protocol2;
inspect `GetInfo.PinUvAuthProtocols`. Do not impose this restriction on plain info/reset or claim all
PINless operations require V2.

The same guide's **access** task says `verify-pin` verifies the current PIN without its known caveat.
`PinManagement.VerifyPinAsync` requests `GetAssertion` permission without `rpId` (current lines168–171);
`ClientPin` does not supply one automatically. The permission-token route requires that scope and can
reject the request even with the correct PIN; legacy fallback drops requested permission/RP scope.
Add this failure qualification next to the command and direct users not to interpret such failure
as proof of a wrong PIN or blindly retry. Preserve the existing class/AccessMenu warnings and deferred
request fix. No new implementation bug/count or command redesign is requested.

### RF-R3 — use the actual protocol property name

**Required low-severity factual correction; CTAP-DOC-004/014.**

`src/Ctap2/CLAUDE.md:433` says `Version/TagLength remain readable`. The interface and V1/V2 classes
expose `AuthenticationTagLength`, not `TagLength` (`IPinUvAuthProtocol.cs:46`, V1:53, V2:60).
Replace the shorthand with the actual member name. The surrounding stateless/disposal/shared-secret
explanation is sound; no crypto or lifecycle change is needed.

### RF-R4 — the preceding L2 CTAP explanation still contradicts the corrected L3b flow

**Required adjacent active-reference reconciliation; F-11, CTAP-DOC-014/018.**

`docs/architecture/sdk-architecture-diagrams.md:117–125,174–180` still wires `Ctap2Session` to a box
labelled `CtapRequestBuilder → CBOR bytes / CtapResponseParser ← CBOR bytes` and states that CTAP2
parses replies through `CtapResponseParser`. The new L3b correctly names
`Ctap2SessionRequestEncoding.BuildMakeCredentialRequest` and `MakeCredentialResponse.Decode`.

Actual session lines251–277 and311–337 use the request-encoding helper and the make/get response
decoders; neither path uses `CtapResponseParser`. The parser is a real general utility, but that does
not make the displayed session call path true. Align the **L2 CTAP box and its teaching paragraph**
with the actual pure request-encoding/per-operation response-decoding boundary, or explicitly label
general utilities separately from the traced session path. Keep the existing backend/APDU convergence
and internal-access explanations; no unrelated PIV/Core diagram rewrite is requested.

**L3b itself passes:** request encoding precedes backend exchange; backend status/notification resolution
precedes typed response Decode; SmartCard uses the actual CLA80/INS10 APDU path. It is expressly a
main-flow sketch, not every validation/cleanup branch. Regenerating L2/L3b images/manifests after the
source correction remains coordinator work, not this reviewer's task or an already-passed gate.

## Optional suggestions — not blockers

- **RF-O1:** `Cli/Menus/InfoMenu.cs:55–57` has unindented XML summary lines amid indented comments.
  Align them when convenient; the XML parsed and this is not a factual/compilation diagnosis.
- **RF-O2:** `FidoExamples/CredentialManagement.cs` update remarks can say simply that the current
  canonical writer sorts completed maps. “Not a broken update-wire defect” is audit-adjudication
  narration rather than lasting consumer context. Preserve the correct automatic-sorting fact.
- **RF-O3:** README's discoverable alternative (117–126) can explicitly state that it requires an
  existing discoverable credential for `example.com`. The preceding created credential is nonresident;
  omitting its allow list intentionally searches a different population, not that prior credential.
  This is an alternative conditional task, not a no-op or a verified fresh-key success path.

## Scope, instructions and evidence actually read

Read root CLAUDE, the available-skill descriptions, CTAP module/test guidance, WebAuthn/Core guidance,
`docs/SDK-HOUSE-STYLE.md`, canonical audit workflow and approved style. Loaded `docs-module` and
`workflow-verify`; no build/test/commit/delegation skill action was applicable to this prohibited-work
packet. Read PRIMARY plan, triage/addendum, investigations A–G, F edit/correction, boundary map,
framework adjudication, F reference/snippet classification material, A correction and C's consequential
ARKG review. Reports supply questions/provenance, not inherited verdicts.

All five F module Markdown guides and all20 changed FidoTool C# files were read completely, including
every method/result/helper/comment and their unmodified executable statements. The two csproj files
were read completely. WebAuthn README, docs/TESTING and the architecture-diagrams guide were also
read completely; semantic acceptance for these external files is bounded to affected CTAP/WA
requirements, hardware-coordination context and CTAP diagram/teaching units. Migration review covers
the complete `webauthn-credential-prompt` mapping and adjoining entries, plus the exact one-line diff,
not every unrelated migration mapping or historical v1 API. Exact30 checkout hashes/ranges are below.

Actual technical dependency reads include session initialization/make/get/next/reset/raw operations,
both complete backends, complete request encoder and device factories, ClientPin fallback/UV/token/
retry/disposal units, complete ExtensionBuilder and all five XML dependency blocks, AuthenticatorInfo
typed locals/complete initializer, stored-credential owner and credential enumeration/disposal units,
PIN-normalizing test helper, bio request/array/sample/continuation units, config omission/toggle units,
complete ARKG seed-key encoder/decoder and experimental verifier. Narrow Core console-reader and
toolchain selection source establish character-count and command-selection claims. These are evidence
dependencies, not a new complete A–E/Core/WebAuthn implementation audit. A–E XML/comments in this
worktree are **stale baseline**, not the accepted final technical docs.

## Independently checked original citations and authority

Access date for the fresh retrievals below is **2026-11-04**. Direct stdlib retrieval of the three FIDO
originals returned HTTP403; Exa subsequently retrieved original publication content. Small publication
previews alone did not establish clauses: full extraction with focused sections/patterns supplied the
protocol passages. Direct HTTP returned200 for Yubico reset, IETF ARKG, vendor previewSign, W3C L3
and release-pinned framework source. No SDK or crypto experiment was executed to interpret these.

| Original / exact revision and status | Independently inspected consequential meaning and limit |
| --- | --- |
| [CTAP2.0, Proposed Standard, 30 January2019](https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html) | §5.2 descriptor MAY be omitted if **allowList has exactly one credential**; not one match/non-discoverability generally. Relevant user table forbids identifying display fields without UV, not the opaque ID. No current2.1/2.3 exception invented. |
| [CTAP2.1 errata, actual heading Proposed Standard, 21 June2022](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html) | §6.1/6.2 platform MUST NOT include uv key with pinUvAuthParam; §6.6 receipt within10s for no-display authenticators, distinct from interaction. §12.2 sensitive credBlob RP MUST use level3+enforcement. minPinLength §12.4 and hmac-secret §12.5 numbering remains distinct from2.3. Existing references are not silently upgraded. |
| [CTAP2.3, Proposed Standard, 26 February2026](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html) | §6.3 entire algorithm/client logic: N−1 next calls, state/exhaustion/>30s NotAllowed, NFC timer steps OPTIONAL. §6.4 required versions/AAGUID16; option state semantics, encrypted identifier IV/cipher and longTouch≥5s. §6.5.5.7.3 UV+token capability, configured UV and permission-specific conditions; no built-in-UV guarantee from uv alone. §6.6 generated/U2F invalidation, discoverable erasure and named state/storage effects; receipt10s for no display; reset transport SHOULD, alternate vendor reset MUST if unsupported. §6.7 standard begin1/next2/cancel3/sensor7, timeout nested key3, template-array key7: current bio defects are not relabelled legitimate standard behavior. |
| CTAP2.3 extension clauses, same original | §12.1 platform enforcement **SHOULD NOT**, not a wire boolean/MUST; §12.2 PII/sensitive credBlob RP **MUST** level3+true enforcement remains stronger. §12.3 largeBlobKey top-level response route; §12.4 direct largeBlob standard creation-support map and mutually-exclusive authenticator routes. §6.10 conditional token/UV write rule, contrasted with stricter SDK writes. §12.5 creation-time authorized-RP minimum disclosure, not policy setting. §12.7 full processing: credential random WithUV/WithoutUV, encrypted salts/salt authentication, uv-mode output selection, up:false unsupported, protocol parameter required when not1. §12.8 MC requires companion hmac-secret=true. No broad YubiKey direct-route absence or earliest-firmware claim. |
| [Yubico FIDO2 Reset](https://docs.yubico.com/yesdk/users-manual/application-fido2/fido2-reset.html), living vendor legacy-SDK guide | Exact older-product window **5s before5.5.2 /10s on5.5.4+**, leaving5.5.2/5.5.3 unclassified. USB interaction wait30s, NFC stays in contact. Blocked Bio Multi-protocol FIDO requires device-wide reset. No v1 command API copied into v2. Retrieved SHA256 `3e7ac03068f09f24920919008043f18c0f782fe0f7210fcadf70623a894d2928`. |
| [WebAuthn L3, W3C Recommendation, 25 August2026](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/) | Publication heading/status freshly verified. §10.1.4 hmac-secret-backed PRF **MUST** use verified variant/override UV if needed. §§7.1/7.2 hash exact clientDataJSON/cData, RP ID/UP/UV/account/policy and stored-public-key signature/counter procedures are distinct from local parsing; counter mismatch is signal, not proof of cloning. This corroborates RP responsibility without rewriting every olderL2 citation. Retrieved SHA256 `157030c980d44a3ce4b1ec5bcfaa16790c7dfefac20709af6ed2b11c7120970b`. |
| [Yubico sign-extension draft v4](https://yubicolabs.github.io/webauthn-sign-extension/4/), Editor's Draft,26 August2025 | Temporary experimental publication, not CTAP4/adoptedW3C standard. §10.2.1 selected signing algorithm/TBS/COSE_Sign_Args and inner attestation/retrieval-after-verification distinction; generated handle may be zero-length. Generic bytes/parsed attestation are not full cryptographic/trust verification. Retrieved SHA256 `29359265d3b0f3dbc738482d4a33865beeb5c698e38651d2d12f14b7b5e6d86c`. |
| [ARKG Internet-Draft-10](https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html), published27 February2026, intended Informational, expires31 August2026 | §§5.1/5.2/8.3 explicitly contain seed placeholder**-65700**, BL=-1/KEM=-2 and example derivedESP256=-9. Separate signing placeholder-65539. SDK outerkty2/wrapped nested maps differ from draft-65537/direct maps; SDK required integer dkalg differs from optional int/tstr. F's late correction correctly states partial agreement, not full conformance, valid points or production authority. Retrieved SHA256 `1c931259f04fe0a79c219f0f1fadcb22a7b635dc36de5703a8209ac4dafab575`. |

### Framework/current-source adjudication, not a newly executed probe

- Fresh Context7 `/dotnet/docs` nullable query returned the general implicit T→T? rule, not the exact
  array/memory case. Fresh original runtime[v10.0.0 ReadOnlyMemory](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/ReadOnlyMemory.cs)
  independently shows null-array constructor→default memory, implicit operator→constructor and Length.
  SHA256 `c19b25038fdf0abf70658aed8979dd83e9ca11e27f5b3df68950e8ecb40d879e`.
  Together with actual AuthenticatorInfo locals/initializer this supports COORD-001:
  **decoded absent encrypted fields have HasValue=true/Length0**, not null; encoded empty has the same
  nullable presence/length. RawData preserves encoded presence. Missing required AAGUID separately
  becomes empty. Caller-constructed model defaults and backing-object identity are not generalized.
  The prior language probe belongs to archived provenance, not fresh CTAP execution here.
- Fresh Context7 CBOR query returned .NET11 depth material, **rejected as .NET10 sorting/reader proof**.
  Fresh original[v10.0.2 CborWriter.Map](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Writer/CborWriter.Map.cs)
  lines121–175 tracks/sorts completed key/value ranges. SHA256
  `14549e61adba45feb51295c5313ca251365ceaad93d35c6da282a33d6141be57`.
  The framework-adjudication probe's exact binary/package and source-identity limits remain intact;
  no probe rerun. D-08 insertion-order runtime defect stays rejected, not pending a wire fix.
- Framework Guid big-endian conclusion remains source-established inference with prior little-endian
  probe provenance, **not executed big-endian/platform qualification**. Trailing-CBOR map completion
  similarly is bounded prior probe/source evidence; no universal malformed corpus or SDK decoder was
  executed. F correctly retains parser/security-critical peer-key gaps rather than sanctioning bypass.

The live ykman guide/version compatibility was not independently re-qualified against a binary. Actual
FidoTool routing/parser/source is decisive for the documented subset, full-hex IDs and force semantics;
F's original vendor reference is retained as comparison context, not a tested compatibility contract.

## Scoped semantic acceptance and preserved boundaries

- **Useful reader tasks:** GetInfo actually queries; create/assert actually invoke the session; allow-list
  assertion targets the created ID. PIN demo creates another credential and authorizes a separate
  assertion with separate hashes/tokens. Mock retry method really invokes ClientPin and inspects command/
  protocol/subcommand/count; extension test checks actual policy/enablement bytes. No no-op helper or empty
  “successful test” was substituted. Compilation and device success remain unverified.
- **Client data/RP:** random hashes/handles are explicitly stand-ins; production uses fresh RP challenge/
  options/account handle, SHA256 of exact serialized collected bytes and independent RP handoff/verification.
  Printed flags/signatures/decoded response are not login, registration, trusted attestation or certificate
  qualification. WebAuthn supplies client orchestration, not RP acceptance or an upload/JSON serializer.
- **UP/UV/PIN:** Markdown token examples omit uv entirely, including false; built-in UV is distinct and
  conditional. P2 is caller-selected, fallback is a legacy command with the same protocol and lost scope.
  Actual FidoTool invalid uv+token calls remain conspicuously documented and unchanged. Retry defaults,
  blocked-state differences, PIN Unicode/NFC/byte-policy obligations and helper-owned protocol disposal
  are preserved. RF-R2 requires the same prerequisites locally for CLI readers.
- **Ownership:** direct sessions borrow connections; device factories own opened ones. One-shot/prompt
  factories are alternatives, not second live sessions. Caller PIN/token/auth buffers live through await
  and are finally-cleared where shown; ClientPin owns protocol disposal, not session/token lifetime.
  Administrative helpers borrow dependencies. StoredCredentialInfo owns one cleared envelope/key view,
  while decoded fields and borrowed transport input survive. No blanket result/client/session cleanup.
- **Stateful operations:** N−1 consecutive getNext calls precede account selection or other device I/O;
  no SDK timer/count/whole-sequence lease invented. Eager CM uses first total and returned RP hash, not
  potentially truncated display text; next failures throw. Toggle is not desired-state/idempotent setter.
  Bio retains beginID independently, count0 is not proof, token cancellation is not enrollmentCancel.
  Blob read-modify-write is shared/nontransactional and SDK writes use stricter token authorization.
- **CLI:** supported verbs/menu-only operations/set-pin omission/full-hex IDs, unknown/incomplete help
  exit quirks, next-flag-as-value, inactive selector, wrong-target reset reselection, swallowed errors/
  cancellation/default support, sync prompts, immutable CLI strings and no rollback are truthful.
  Program reset recomputes transport for the newly selected key; ResetMenu specifically reuses the old
  transport. Neither binds new selection to the confirmed physical target.
- **Reset:** no universal5s receipt or10s hold was accepted. Source has no local clock/power-cycle
  orchestration. Vendor window gap, separate USB30s/NFC contact, ≥5s enabled long-touch and conditional
  product/device-wide-reset limits are preserved, without weakening public standard clauses.
- **Extensions/proof:** direct vs key/storage route, minPIN disclosure, inert credProtect enforcement,
  sensitive-credBlob prevention prerequisite, two CTAP hmac variants/one verified WebAuthn PRF and
  experimental previewSign/ARKG/verifier limits remain distinct. No standards downgrading to hide bio,
  owner, malformed-input, peer-key validation or PRF defects.
- **Tests/policy:** default helper normalizes **after session initialization and before callback**, can
  mutate PIN/consume retries/leave temporary state; smoke traits do not make this read-only. Direct
  GetInfo example lacks normalization and does not gain a new approval gate. Existing CTAP human exact-
  command/physical-presence policy remains unchanged; the shared authorization policy was not relaxed.
  Real fixture/export/cleanup limitations remain, with no all-tests-restored/security proof.
- **External narrow changes:** WA requirement qualification preserves fixedV2/token/PRF and origin/RP
  limits. Shared TESTING added qualification without changing commands/approval tables. Migration changes
  only its selected notes scalar, correctly placing PIN in ClientPin and auth fields in credential options.
  **Existing migration YAML parse defect DOC002 remains unfixed**; no parse/schema gate passed here.

## Every39 module Markdown fence — independent complete-block coverage

Ranges below are in the reviewed hash snapshot. `complete` means complete in the indicated project/class
context, **not compiled/executed**. `fragment` lists real supplied context. `sketch` is explanatory only.

Typed contexts: **Console**=.NET10/C#14 async top-level consumer with Ctap2+transitiveCore, README
imports/implicit System collections/tasks; one supported dedicated-key session. **UT**=actual CTAP
UnitTests csproj, xUnitv3/NSubstitute/Cbor, methods inside class, required Ctap2/Cbor/Pin/Extensions
imports and runner token for async calls. **IT**=actual IntegrationTests csproj, xUnitv2/SkippableFact,
Tests.Shared and Infrastructure/TestExtensions plus Core Devices/Sessions/Ctap2 Credentials/Pin imports;
no v3TestContext, approved known device/state. **P**=live supported session, mutually supported protocol,
caller-owned PIN/token/auth buffers, RP scope/current policy. **CM**=actual CredentialManagement helper,
returned32-byte RpIdHash and descriptor. **E**=caller ROM blobData, supported protocol/owned sharedSecret,
keyAgreement dictionary and salt arrays32, stable through Build/send. **CLI**=built executable onPATH or
explicit repo-root sample project; actual reference graph, supported key/state, human timing/UP/UV.

| Exact path | Fence/range | Disposition / typed prerequisites |
| --- | --- | --- |
| `src/Ctap2/CLAUDE.md` | 1 /54–118 | sketch: actual directory tree, no invented PRF directory; no execution. |
| same | 2 /128–131 | fragment: IYubiKey yubiKey/async factory; direct IConnection alternative is borrowed/scoped separately. |
| same | 3 /141–153 | fragment: live session/LINQ; versions/extensions/options inspection only. |
| same | 4 /167–198 | fragment: P, PIN byte[], hash32, RP/user/options/ct; real make call, uvnull, finally token/tag; not RP registration. |
| same | 5 /247–266 | fragment: E plus MakeCredentialOptions; actual registration/assertion builders, sensitive-blob caveat adjacent. |
| same | 6 /293–310 | fragment: concrete session/P/CM, descriptor credentialId and returned hash; real metadata/enumerate/delete, owned result disposal. |
| same | 7 /356–363 | fragment: Cbor/Pin imports, session/protocol; actual command-prefixed raw retry send, not no-op. |
| same | 8 /407–419 | fragment: synchronous method, caller pinBytes byte[], crypto import; actual hash/finally, no span across await. |
| same | 9 /423–426 | fragment: V2 using scope; disposal marks flag, not secret ownership. RF-R3 surrounding name only. |
| same | 10 /439–459 | complete UT method: versions-only partial parser fixture, actual assertion; missing required AAGUID stated. |
| same | 11 /469–494 | complete UT method: ROM response/captured request, actual awaited helper/runner token, CBOR and retry assertions. |
| same | 12 /505–515 | complete IT method: HID attribute **and** factory preference, direct no-normalization GetInfo,16-byte assertion. |
| same | 13 /526–529 | complete root commands: default units vs includes-both/FQN-integration smoke; setup policy remains. |
| same | 14 /536–544 | sketch: real Feature field declarations/metadata, not a class to add or support matrix. |
| same | 15 /548–553 | fragment: Core FirmwareVersion/feature comparison; sentinel passes, no sensor/capability guarantee. |
| `src/Ctap2/PACKAGE_README.md` | 1 /24–27 | complete installation commands in existing consumer project/alpha feed; no runtime proof. |
| `src/Ctap2/README.md` | 1 /33–36 | complete installation commands, same consumer/feed context. |
| same | 2 /42–59 | complete Console entrypoint: discovery/one owned session/fresh GetInfo; no PIN/touch setup. |
| same | 3 /90–103 | fragment after Console imports/session: random demo hash/handle, actual nonresident ES256 creation, guarded attested ID/key. |
| same | 4 /107–112 | fragment: prior credentialId/session/imports; fresh authenticationHash and singleton allow list; public signature display only. |
| same | 5 /117–126 | alternative to4: session/hash, existing discoverable RP credentials/current policy; first total/default1 and real consecutive next calls. RF-O3 clarity. |
| same | 6 /148–155 | fragment: caller CM helper and returned hash, CM namespace/StoredCredentialInfo import; real enumeration/display/disposal. |
| same | 7 /185–228 | fragment: Console/P, prior RP/user, known configured evaluation PIN/support2; distinct creation/assertion hashes/tokens, returnedID allow list; finally PIN/token/auth cleanup. |
| same | 8 /267–273 | complete class: IUserPresencePrompt/Core.Credentials, ValueTask/threading imports; place after top-level statements. Informational callback. |
| same | 9 /275–279 | replacement Console factory, same device/prompt/session name; not simultaneous second session. |
| same | 10 /330–332 | complete repo-root CLI run syntax, actual sample project; not launched. |
| `src/Ctap2/examples/FidoTool/README.md` | 1 /14–24 | complete root build/run syntax; scoped SDK build excludes sample; actualCLI state required, RF-R2 missing publicV2 prerequisite. |
| same | 2 /36–38 | complete built-executable info command; actual exposed-interface selector may admit unsupportedSmartCard. |
| same | 3 /47–53 | complete reset syntax, human timing/target authorization; force bypasses instructions/confirmation only. |
| same | 4 /75–84 | complete set/change/verify syntax, noPIN vs existingPIN state; RF-R2 missing verifyRP/fixedV2 caveat. |
| same | 5 /95–101 | complete config command syntax, actual support/token prerequisites/state reconciliation. |
| same | 6 /109–116 | list complete; deletion fragment needs shell CREDENTIAL_ID_HEX actual fullID, known target/PIN/authorization. |
| same | 7 /128–140 | command fragment needs FINGERPRINT_ID_HEX; actual biometric hardware/PIN/human sampling and disclosed standardwire/schema limits. |
| same | 8 /158–160 | complete interactive launch syntax/terminal; no fullAPI/operation claim. |
| `src/Ctap2/tests/CLAUDE.md` | 1 /48–84 | complete IT method: pinnedHID/helper default normalization, supportedV2/knownPIN, actual creation/ID assertion/finally; persistent credential/PIN not restored. |
| same | 2 /104–117 | fragment: IT/P live ICtap2Session, noPIN set, supportV2, Encoding/KnownTestPinString; actual SetPin/finally. |
| same | 3 /131–147 | complete root command forms; exact project-kind/FQN/UP filters, approval/setup caveat; not run. |
| same | 4 /153–164 | UT fragment: Cbor imports/method scope; partial backend-status-stripped versions fixture, not conformingGetInfo. |
| same | 5 /171–187 | complete UT method: real nullableBuild guard, two-key CBOR policy/enablement assertions; not enforcement/PRF/device proof. |

### Five A/B/E XML dependencies — baseline classification only

| Exact source block | Independent classification / final owning dependency |
| --- | --- |
| `src/Ctap2/src/IYubiKeyExtensions.cs:52–56`, GetCtap2InfoAsync | fragment: IYubiKey/asyncConsole, one-shot without another live owning session. A owns final corrected advertisement/ownership XML. |
| same:109–122, CreateCtap2SessionAsync | baseline fragment has incompatible simultaneous sessions. FinalA must keep scoped alternatives; caller Scp03KeyParameters.Default owns keys outside secure-session scope on an authorized configuredSmartCard key. Not accepted/compiled as final here. |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs:34–37` | binary-layout sketch, HMAC32 thenAES32; inclusive indices are not executable C# ranges. B owns final contract. |
| `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs:31–36` | builder fragment with caller ROM myBlobData/CredProtectPolicy; E owns final sensitive-content/enforcement/capacity qualifications. No credential/enforcement operation in block. |
| `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs:29–35` | stale baseline sketch with undefined response.Extensions. E owns explicit AuthenticatorData/rawExtension guard/current reconstruction limits. Do not count this baseline as accepted compilation-ready output. |

These are all five XML code blocks found mechanically under module C# source. FidoTool's changed
source has none. All39 module Markdown fences were read in full even without CTAP keywords; opening/
closing counts match. External WA9/TESTING22/architecture8 fences were mechanically counted and read
in their full guide context; only the L3b Mermaid fence changed. They are not silently added to the
39 current F-module runnable-example total. Historical/before-after and unrelated external sketches
are not compiled-current examples. Coordinator compiles meaningful **final integrated** blocks in real
consumer/UT/IT/sample contexts; no stripping useful calls/traits/cleanup or analyzer policy for green.

## Fresh mechanical checks and exact snapshot manifest

Read-only checks in the F checkout:

- `git rev-parse HEAD`: baseline unchanged. `git diff --name-only 5b09a9b6` and `git status --short`:
  exactly30 changed paths,26 underCTAP/four external, no reviewer source edits or additional untracked paths.
- Python SHA256 over actual checkout bytes produced the30 hashes below. Diff baseline is Git-blob
  content; two csproj checkout LF→CRLF warnings are recorded separately, not unexpected runtime changes.
- Python comparison of all20 changed C# files, excluding only whole-line `//` comments/blank lines:
  **all remaining lines equal baseline**. Imports/directives/strings/calls/traits/assertions preserved.
  This is a text-preservation check, not Roslyn/API/build verification.
- Parsed both csproj XML, removed only Description, normalized whitespace: **all other elements equal
  baseline**. Sample diff changes only existing CA1852 explanatory comment; shipping diff onlyDescription.
  No analyzer/NoWarn/TreatWarningsAsErrors/reference/package/assembly policy was weakened.
- Parsed all89 contiguous F C# XML-comment blocks: no XML syntax errors. Not cref/compiler validation.
- All39 module Markdown fences close; all5 module source XML code openings accounted for above.
- Relative Markdown link-path scan over **every changed Markdown file**: zero missing filesystem targets.
  Changed csproj ProjectReference paths also exist after slash normalization. Remote URL and heading-anchor
  certification are not inferred; root command/example project paths were checked separately.
- `git diff --check 5b09a9b6`: exit0, no whitespace diagnostics, only the two expected csproj checkout warnings.

`1–N` in the table means whole file read; external **semantic** scope is qualified above. Comment
dispositions refer to complete affected remarks and methods, not merely added lines.

| Exact path | Read range / reviewed unit disposition | SHA256 of reviewed checkout bytes |
| --- | --- | --- |
| `docs/TESTING.md` | 1–586; hardware-coordination/setup/policy qualification passes; unrelated guidance not re-audited | `1db21b42b4a8b6443a872b2b0be37fcc0ced84202ec49a8c8db1c5210e6af401` |
| `docs/architecture/sdk-architecture-diagrams.md` | 1–431; L3b passes, L2RF-R4; render pending | `b5b5e75be060558d479441d281b35abaa198d92555a6ee3392a1d314830445cb` |
| `docs/migration/v1-to-v2-map.yml` | selected mapping517–531/adjoining486–537 + full diff; note correction passes, DOC002 not repaired | `e7f3b5d423bce9affd6f2d68bcd365d27cfdfd17cfa83472d5766f6efc0c3ad2` |
| `src/Ctap2/CLAUDE.md` | 1–574; all patterns/tree/security/tests/15fences, RF-R3 | `ea814d3ad1ceac8dbd13a51042205c940e1854444f8c82be1043f793edd5cccd` |
| `src/Ctap2/PACKAGE_README.md` | 1–35; scope/alpha/install/RP/bio/experimental qualifiers pass | `38094b395d2eb392765ea581fb6e0dfe49187a3bbd2905510b6a4be6ae37580b` |
| `src/Ctap2/README.md` | 1–340; complete journeys/10fences/ownership/sequence/reset/lateCOORD001 pass | `a4ce9582931bf8304888a74691e6683fe2cb4977b6ed72f51a384c809fa683e7` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/AccessMenu.cs` | 1–202; prompt/PINowner/retries/verifyRP warning pass | `d5a3969a2457e3e7d68802c760bd2899ed6031040594e4846916cb89d08f947f` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/ConfigMenu.cs` | 1–179; support/error/toggle/scope/code-point/force qualifiers pass | `a99f6b0b24b988f8bbdc10cf7ed3768f314317b312d0b74bc9e50b707ed05336` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/CredentialMenu.cs` | 1–197; whole create/get/display, RPdemo/firstonly/user/descriptor pass | `b651994c0fd97a1234460590b7f47920176c72a52455de20fa5e5cfa65b48472` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/CredentialsMenu.cs` | 1–289; full metadata/list/delete/update/display/ownedresult/hash route pass | `1d39b8c1e374ee9c1c40ecf4734c1cc7d1eb325771577b5b4c9007570d4edb20` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/FingerprintsMenu.cs` | 1–304; whole sensor/list/enroll/delete/rename and bio/provisioned-state/UI limits pass | `571d4f707af52a82fdc6af5731f9ca00e0fc3bf3f15734bbe4c44caace7c37bb` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/InfoMenu.cs` | 1–204; metadata/units/estimate/certification qualifier pass; RF-O1 only | `de9a43d65ec0ac5f355040b55259c2418defb2153420ee8e16bf6504b46b81b2` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/ResetMenu.cs` | 1–146; full preflight/confirmation/reselection/oldtransport/failure pass | `441b661cf450ee2a5f9a82132c05f59998ed3fedf3c8927dfeba2e855a9f4e06` |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs` | 1–203; full filter/retry/info/labels/selection, RF-R1 | `5d9f65f50d2cdc1f41452c69336cf50ee19be1e953e971ad580582e0f4140aa2` |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoDeviceSelector.cs` | 1–55; inactive alternative/sharedbase/auto preference pass | `b516c9c5159b634080a0f792adc07243cc1cf03c353d6d4b63f337dc5f303787` |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoPinHelper.cs` | 1–103; allinput methods/byteowners/CLIstrings/count/cancel qualifiers pass | `3a35d47981b9ab6186108b0c117faac027f21832abf5c9f366830c6d0f1cc41b` |
| `src/Ctap2/examples/FidoTool/FidoExamples/BioEnrollment.cs` | 1–424; full result/sensor/enroll/inventory/name/delete/support/status, retainedbeginID/knownwire limits pass | `f3d3ce51362352ab3415bd1efdddfc8e53941b7329b8417882d4b5c2ffa21ca0` |
| `src/Ctap2/examples/FidoTool/FidoExamples/ConfigManagement.cs` | 1–229; allops/results/support/borrowing/omission/state pass | `2d23b8461397a110ff8da45db25885f1351a3c3b47bf4dcc670a90f09773dba3` |
| `src/Ctap2/examples/FidoTool/FidoExamples/CredentialManagement.cs` | 1–409; full results/transfer/metadata/RP/credential/delete/update/support, owner/sorting pass; RF-O2 | `9a7a3a5687a255d479be1e4ffc9ecef687f529ad8c4608a666f217f8aedc63d8` |
| `src/Ctap2/examples/FidoTool/FidoExamples/GetAssertion.cs` | 1–148; randomhash/notreturned/firstonly/uvtoken/P2/catch/finally qualifiers pass | `18a05834d11e196cd6f96570eb9f904a9b3070234b2b9f41f138a25630c796fb` |
| `src/Ctap2/examples/FidoTool/FidoExamples/GetAuthenticatorInfo.cs` | 1–75; full query/result/init/freshness/nonnormalization/error pass | `247f4f51001b40a21981ea872ce20716862d5fffb8c4d2de464cdcd58f5ffc3e` |
| `src/Ctap2/examples/FidoTool/FidoExamples/MakeCredential.cs` | 1–173; full persistentdemo/hash/handle/result/uvtoken/failure/cleanup pass | `a4040a8f2617008bc625ee4aaeba52605ad34cdf9750f010fb7e7558c29f9c80` |
| `src/Ctap2/examples/FidoTool/FidoExamples/PinManagement.cs` | 1–277; allset/change/verify/retry/results/errors/P2/borrowedinput/verifyRP pass | `912e8a09871a83ccb3ac1ec2dc6a35b31ee5cded15d6fc9002e75a6998ce26eb` |
| `src/Ctap2/examples/FidoTool/FidoExamples/ResetAuthenticator.cs` | 1–158; full preflight/null/default/message/reset/errors/clock/productgap pass | `6e8284576f2fc1a17ca79793849434ced34e14a32962aa92126b4a2ca0105fe8` |
| `src/Ctap2/examples/FidoTool/FidoTool.csproj` | 1–26; existingCA1852 comment-only diff; policy/references unchanged | `24482e538235542bd6d5609c9e6ec06ab1687cc1ada93efe32f4f45a27b25c76` |
| `src/Ctap2/examples/FidoTool/Program.cs` | 1–729; everyverb/menu/parser/help/reset/PINscope/result lifetime read, commentary passes | `9ff0ac47971442c115aa7fbed71649c8c55220d08da1526bc65c5646cfb50866` |
| `src/Ctap2/examples/FidoTool/README.md` | 1–220; allcommands/8fences/support/selection/reset/security; RF-R2 | `b10f8dbaf238bb0f4ccc7683e1212930b73926f273c64d3ac4ee11874cc1e09f` |
| `src/Ctap2/src/Yubico.YubiKit.Ctap2.csproj` | 1–32; Description-only operation/RP correction; policy/references unchanged | `859c43a9e8e3254af21bd74b46c9174ec2c65a977a489624a0785d1d16ba7344` |
| `src/Ctap2/tests/CLAUDE.md` | 1–200; complete setup/lanes/commands/5fences/evidence/export; unchangedapproval pass | `b93c7a963573e93380ef82181baebce2b106b62ea4237d16efad1b0de602d266` |
| `src/WebAuthn/README.md` | 1–522; completeguide/9fences read; narrowlychangedRequirements pass, no fullWA re-audit | `cf64612066288c5845cd5d9892038abae2ac0cc17e746ab4fbc7e401069208f7` |

## Handoff and limits

Correct RF-R1–R4 with F/coordinator ownership, then fresh re-review those complete units and update hashes.
Optional suggestions need not delay factual acceptance. Seven unchanged F support/API/runner paths retain
the investigator's bounded dispositions; they are not asserted independently semantically re-audited here.

No build, compiler, unit/integration test, formatter, complexity/docs/architecture gate or image renderer
ran. No device/OS/firmware/SKU/timing, PIN/retry/UV/bio/storage/crypto/CLI operation was qualified.
Typed snippet inspection/XML/link checks are not compiler success, end-to-end secret erasure, standard
conformance or device interoperability. Executable sample defects/tests/assertions/runtime policy remain
deferred, not fixed by comments. Existing YAML DOC002 is deliberately unfixed and no parse claim is made.

The coordinator still owns meaningful final integrated example compilation and all normal gates,
image freshness/manifests, clean-unit sampling, record integration and remote checkpoint. The separate
fresh integrated CTAP↔WebAuthn boundary review remains required; this F checkout cannot accept stale
A–E source docs as final. Preserve WA-XREF-001 and WA-IMP-002/003/009/010/011/012/013/014–019,
COORD-001 decoder-family provenance, corrected ARKG partial agreement, rejected D-08 runtime diagnosis,
and G-011 native-result/serializer actor decision. Neither this review nor eventual F prose PASS starts
Pass2 or constitutes complete module closeout.
