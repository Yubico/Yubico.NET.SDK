# F snippet inventory

Baseline 5b09a9b6; 2026-11-03. No snippet was compiled or executed by F.
`complete` means self-contained for the stated project/working-directory context, not an RP integration
or hardware qualification. `fragment` needs the listed typed setup/scope; `sketch` intentionally or
accidentally lacks enough behavior/setup to run meaningfully; `historical` is excluded from current
syntax validation. All blocks below were read in full, not just module-keyword lines.

## Typed prerequisite keys

- **D**: `IYubiKey device/yubiKey`, Core.Abstractions/Core.Devices and Ctap2 namespaces, async enclosing
  scope, reachable supported authenticator, device chosen by application; session owns connection.
- **S**: one live `Ctap2Session session` (or `ICtap2Session` where accepted), no concurrent owning factory;
  operation cancellation token `CancellationToken`, await call to completion before clearing inputs.
- **MC**: `byte[32] clientDataHash`, `PublicKeyCredentialRpEntity rp`, `PublicKeyCredentialUserEntity user`,
  credential parameter list and MakeCredentialOptions; protocol demonstration unless explicitly RP-fed.
- **P**: borrowed UTF-8 `byte[]/ReadOnlyMemory<byte> pin`, `IPinUvAuthProtocol protocol`, `ClientPin clientPin`,
  permission/RP-bound caller-owned `byte[] pinToken`, string `rpId`; finally zero token/PIN after use.
- **E**: `ReadOnlyMemory<byte> blobData`, typed `HmacSecretInput` or protocol/shared-secret COSE agreement
  `IReadOnlyDictionary<int,object?>`, 32-byte salts; caller owns/clears secret input/output arrays.
- **CM**: `CredentialManagement credentialManagement/credMgmt`, `ReadOnlyMemory<byte> rpIdHash` SHA-256,
  `PublicKeyCredentialDescriptor credentialId`; dispose CredentialEnumerationResult, keep views within scope.
- **UT**: CTAP UnitTests project, xUnit v3/MTP, System.Formats.Cbor, NSubstitute where used, imports,
  runner cancellation, fake ICtap2Session, ReadOnlyMemory<byte> mockResponse and actual awaited action.
- **IT**: CTAP IntegrationTests project, xUnit v2/Xunit.SkippableFact/Tests.Shared, authorized YubiKeyTestState
  or device field, known device/PIN state, exact human approval for UP/UV/reset; no v3 TestContext assumptions.
- **WA**: WebAuthnClient client, required WebAuthnOrigin/clientOptions.RpIdValidator, typed creation/request
  options, input/output extension namespaces; one client owns underlying session; RP handoff/verification separate.

## All module fenced blocks (39)

Paths relative to `src/Ctap2/`; line is opening fence at baseline.

| Path:line | Class | Typed prerequisites / disposition |
|---|---|---|
| CLAUDE.md:51 | sketch | Architecture text tree, not C#; stale PRF folder/missing COSE, F-04 |
| CLAUDE.md:125 | fragment | D and borrowed IConnection connection; alternatives, not simultaneous sessions; F-02 |
| CLAUDE.md:135 | fragment | S, LINQ Contains on nonnull lists/dictionaries; no actual feature-operation authorization |
| CLAUDE.md:155 | fragment | S/P/MC; string rpId, CancellationToken, WithPinUvAuth options; caller PIN ownership must be explicit |
| CLAUDE.md:205 | fragment | E/MC; blobData and protocol/key agreement/salts; builders do not run either ceremony |
| CLAUDE.md:235 | fragment | S/P/CM; descriptor not raw credential ID; only result disposed, helper borrows token |
| CLAUDE.md:282 | fragment | S, protocol.Version, ClientPinSubCommand.GetRetries, Cbor namespace; raw response borrowed contract |
| CLAUDE.md:320 | fragment | byte[] pinBytes, System.Buffers/Cryptography; ArrayPool example complete within a containing method |
| CLAUDE.md:341 | fragment | PinUvAuthProtocolV2 import and enclosing using scope; stateless disposed marker |
| CLAUDE.md:356 | fragment | UT class/imports; versions-only parser test, not valid full GetInfo; F-05 |
| CLAUDE.md:382 | sketch | UT; needs mockResponse and an awaited helper action before parsing capturedRequest; F-05 |
| CLAUDE.md:411 | sketch | IT device field/traits; second method intentionally empty; not executed assertion evidence |
| CLAUDE.md:431 | complete | Repo root, toolchain workflow; default unit selection, not integration; F-06 |
| CLAUDE.md:439 | sketch | Class/static declaration context and Feature namespace; FeatureCtap2 nonexistent, F-04 |
| CLAUDE.md:450 | fragment | FirmwareVersion firmwareVersion, Ctap2Session.FeatureBioEnrollment; threshold not capability guarantee |
| PACKAGE_README.md:18 | complete | .NET10 CLI, existing consumer project, alpha feed/network; install only, not build proof |
| README.md:26 | complete | Same installation prerequisites; old verb-first form remains supported (Context7) |
| README.md:35 | complete | .NET10 console project with CTAP package, top-level async; hardware read-only discovery/GetInfo |
| README.md:72 | fragment | Prior S/imports and MC created here; locally fabricated client-data JSON; F-01 |
| README.md:86 | fragment | Prior S/clientDataHash; discoverable first result only, no RP verification; F-01 |
| README.md:99 | fragment | CM not established by earlier snippets; rpIdHash/credentialManagement explicitly supplied |
| README.md:122 | fragment | S/MC from prior blocks; known demo PIN must already be configured; try/finally scope retained |
| README.md:150 | fragment | Insert INSIDE preceding try, before its finally; protocol/clientPin are not in scope afterward |
| README.md:172 | complete | IUserPresencePrompt import; class declaration belongs outside top-level statement fragment; callback not touch proof |
| README.md:180 | fragment | D/TouchPrompt; replace original factory, not append while session lives; F-02 |
| README.md:213 | complete | Repo root .NET10 run project; compile/run is a ceremony-capable CLI, not an investigator command |
| examples/FidoTool/README.md:11 | fragment | Mixed root toolchain/sample-directory dotnet run context; fix explicit paths, F-07; PIN literals demo only |
| examples/FidoTool/README.md:29 | complete | Built FidoTool executable on PATH, selected supported device; read-only info |
| examples/FidoTool/README.md:37 | complete | Executable on PATH, dedicated selected key, human reset/touch timing; -f not CI-safe; F-09 |
| examples/FidoTool/README.md:49 | complete | Executable, existing known PIN/new PIN; irreversible retry/state effects and CLI string exposure |
| examples/FidoTool/README.md:61 | complete | Executable, configured PIN/capabilities; EA reset qualification; no full ykman compatibility |
| examples/FidoTool/README.md:73 | sketch | Replace <hex-credential-id> with full hex argument; shell angle syntax not runnable as printed; deletion approval |
| examples/FidoTool/README.md:86 | sketch | Replace <hex-fingerprint-id>, Bio hardware/configured PIN/human sampling; raw angle syntax not executable |
| examples/FidoTool/README.md:111 | complete | Executable on PATH, interactive terminal; launch only, not all API tasks |
| tests/CLAUDE.md:43 | sketch | IT/MC/options undefined, missing UP trait/assertions, no supplied timeout; F-05 |
| tests/CLAUDE.md:73 | fragment | IT/S/P, known test PIN constant and explicit human-authorized SetPin; using byte owner finally valid |
| tests/CLAUDE.md:96 | complete | Repo root toolchain; mislabeled all-tests command defaults unit; integration flags include both initially, F-06 |
| tests/CLAUDE.md:117 | fragment | UT imports/class; versions-only response payload not full GetInfo fixture |
| tests/CLAUDE.md:131 | sketch | UT or IT/S/MC/P/E; response.Extensions nonexistent, assertion only presence not extension semantics; F-05 |

## All module XML code blocks (5)

| Full source path:line | Class | Typed prerequisites / owner |
|---|---|---|
| src/Ctap2/src/IYubiKeyExtensions.cs:52 | fragment | D; one-shot factory only when no other session live; A |
| src/Ctap2/src/IYubiKeyExtensions.cs:109 | fragment | D/S, Scp03KeyParameters/ConnectionType/SessionCreationOptions imports, configured SmartCard key; second factory conflicts with first scope, F-02/A |
| src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs:31 | fragment | E `ReadOnlyMemory<byte> myBlobData`; builds extension bytes, not verified registration; E |
| src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs:29 | sketch | Explicit AuthenticatorData response with present nullable Extensions, not MakeCredentialResponse.Extensions; E/F-05 |
| src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs:34 | sketch | Binary layout notation sharedSecret[0..31]/[32..63], not assignments/executable C#; B |

No other `<code>` or `<example>` elements or C#-embedded Markdown fences were found in tracked module
source. Copyright headers/inheritdoc references are not examples. Executable sample source is one
complete FidoTool project containing 20 C# files, with internal method fragments wired by Program;
it is a protocol/administration demo, never classified as a complete RP application.

## External full-block inventory

The following exact current/historical reference blocks were read. These are CTAP-relevant blocks plus
implicit client/setup blocks needed to understand them, not unrelated PIV/OATH code in the same guide.
No keyword-only extraction is used as evidence of a complete current WebAuthn journey: its entire
README and CLAUDE were read, including blocks with only `client`, `matches` or callback variables.

| Path:opening lines | Class | Typed prerequisites |
|---|---|---|
| CLAUDE.md:144 | complete | Repo root toolchain; root instruction block, not proof of CTAP hardware execution |
| TOOLCHAIN.md:437 | historical negative example plus complete toolchain commands | Wrong direct-test path intentionally negative; do not update it into a recommended command |
| docs/LOGGING.md:221 | sketch | ILogger/YubiKitLogging imports and class shells; both classes illustrative, not shipping Ctap2Session |
| docs/TESTING.md:27,211,219 | complete commands | Repo root; exact flags/traits and human approval; selected project/execution counts still required |
| docs/TESTING.md:102 | historical negative direct-run example plus complete recommended command | Never executed by F |
| docs/TESTING.md:126 | sketch | Text project tree contains obsolete non-CTAP paths; not executable |
| docs/TESTING.md:279,343,383 | fragment | IT YubiKeyTestState and Tests.Shared imports; rows filter exposed interfaces, Management preference/actual ConnectionType assertion where stated |
| docs/TESTING.md:376 | historical | Removed DeviceId heuristic/old discovery; excluded from current compiler checks |
| docs/architecture/physical-device-model.md:51 | fragment | Core device/discovery imports; no CTAP operation, filtering exposed HidFido only |
| docs/architecture/physical-device-model.md:109 | fragment | D and typed connection interfaces; explicitly separate await-using scopes, clean lifetime unit |
| docs/architecture/raw-access-tiers.md:82 | fragment | D; byte ctapHidCommand, ReadOnlyMemory<byte> payload, CancellationToken; RawCtapHidSession adds framing not WebAuthn |
| docs/architecture/sdk-architecture-diagrams.md:20,47,92,241,287,319 | sketch | Mermaid conceptual topology/sequences; 241 contains obsolete Add/MakeCredentialData, F-11 |
| docs/architecture/webauthn-extension-processing.md:86 | sketch | Text internal type tree; not public plugin/compilable API |
| docs/usage/device-discovery.md:51 | fragment | Core.Devices imports; physical devices, not automatic per-interface/feature guarantees |
| docs/usage/user-interaction.md:15 | fragment | D and existing PIV namespace/CLI Shared reference; session setup context for shared prompt, not CTAP ceremony |
| docs/usage/user-interaction.md:75 | fragment | UserPresenceContext context; switch expression returns message, not authorization |
| docs/usage/user-interaction.md:99 | fragment | WA, PublicKeyCredentialRequestOptions requestOptions and CancellationToken; BeginScope disposed around awaited client call |
| docs/usage/user-interaction.md:106 | fragment | IUserPresencePrompt class method, Core.Credentials; promptly return, no gesture wait |
| docs/migration/v1-to-v2.md:148 | historical | Alpha2/alpha3 GetFidoInfo names explicitly qualified by current replacement immediately below |
| docs/migration/v1-to-v2.md:387 | historical | V1 Fido2Session sample; retain old namespace |
| docs/migration/v1-to-v2.md:396,404 | fragment | D/token; current one-shot versus reusable factory in separate recipes |
| src/Core/CLAUDE.md:25 | sketch | Core directory tree, Fido internal transport names legitimate |
| src/Management/CLAUDE.md:657 | fragment | DeviceCapabilities namespace; Fido2 capability flag remains correct; not renamed API |
| src/WebAuthn/README.md:31 | complete | Consumer project/package installation, .NET10 alpha feed |
| src/WebAuthn/README.md:53 | complete | .NET10 console/package context; one local dedicated-key hardware demo with known PIN; fresh separate challenges, explicit no RP integration |
| src/WebAuthn/README.md:195 | complete | PinPrompt class with Func<CredentialPromptContext,CancellationToken,Task<byte[]>>, IMemoryOwner imports; byte finally cleanup/ownership transfer |
| src/WebAuthn/README.md:208 | fragment | WA `clientOptions`, application `ReadPinAsync` delegate; insert BEFORE single factory |
| src/WebAuthn/README.md:225 | complete | TouchPrompt class/Core.Credentials; no waiting for gesture |
| src/WebAuthn/README.md:233 | fragment | WA/D/TouchPrompt; REPLACE original factory, not second live client |
| src/WebAuthn/README.md:393 | fragment | WA trusted immutable HashSet<(string Origin,string RpId)> configuration; exact pair policy, not browser default |
| src/WebAuthn/README.md:418 | sketch | Application rpValidationService synchronous bool(WebAuthnOrigin,string) contract supplied; not SDK-owned service |
| src/WebAuthn/README.md:486 | historical | Before/after migration shape; ctor results intentionally not owned runnable example; current ownership explained in main guide |
| src/WebAuthn/CLAUDE.md:28 | sketch | Internal architecture text tree |
| src/WebAuthn/CLAUDE.md:78 | fragment | ILogger/YubiKitLogging and processor context; not a current logger call claim |
| src/WebAuthn/CLAUDE.md:97 | fragment | WA/MC/application challenge/userId/prompts/extensions; explicit transfer of existing ctap2Session; both calls and zero-match branch read |
| src/WebAuthn/CLAUDE.md:143 | fragment | D/WA, application rpValidationService/prompt and caller-owned ScpKeyParameters; alternate constructor, not additional client |
| src/WebAuthn/CLAUDE.md:203 | sketch | Public interface declaration illustrates existing ICredentialPrompt contract, not new implementation |
| src/WebAuthn/CLAUDE.md:352 | fragment | WebAuthn PreviewSign namespaces/types, generatedKey.KeyHandle, ReadOnlyMemory<byte> toBeSigned/additionalArgs/credentialId; experimental and capability-conditioned |
| src/WebAuthn/CLAUDE.md:489 | sketch | WebAuthn IT project/shared helpers; setup session disposed BEFORE client, body intentionally omitted; not proof of signing assertion |
| src/WebAuthn/CLAUDE.md:516 | complete | Repo root toolchain/human coordination; default units versus integration flag |

## Later compile/review instructions

Use the real SDK console/test/sample references and analyzer policy; do not strip attributes or calls
to manufacture a green compile. Correct Markdown/XML syntax and typed setup before validation; preserve
executable samples' strings, assertions, attributes and control flow in Pass 1. Capture actual diagnostics
for fragment hypotheses; this inventory does not mark them verified. Compile XML alternatives in separate
lifetimes, insert README assertion PIN fragment inside its try, and configure prompt alternatives before
the single factory. Historical/sketch/layout blocks are explicitly excluded from runnable-example counts.
