# Nine applet/Core boundary packets

**Preparation only. All nine packets remain unexecuted and unreviewed.** Source baseline:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. These questions hand off the registry to the
family investigators; they supply neither nine completed boundary audits nor nine new bugs.

Each packet follows the execution plan's factory, ownership, failure/reset/cancellation, effective
version, documentation-impact and independent-review contract. Read the actual module instructions
before its source. Trace the named operation through Core and the relevant OS owner. Preserve the
original ID and its source run, even when several modules share one mechanism. A Core change can
reopen an applet's active documentation while its frozen run stays unchanged.

## Seeds and distinct questions

| Module | Original candidates and current starting points | Questions the packet must answer | Documentation reopening map |
| --- | --- | --- | --- |
| OATH | `fixes-wave-001:CORE-001`; OATH `IMP-004/005/010`, `DEC-001/002/007`. `src/Oath/src/OathSession.cs`, `src/Oath/src/IYubiKeyExtensions.cs`; Core `DiscoveryDeviceInfoReader`, `SharedDeviceInfoRead`, `ProtocolDeviceInfo`, `ApplicationSession`, `Base32`. | Can current scoped SmartCard discovery avoid unrelated HID reads, with no sibling-interface overlap or early native lease release? How do actual same-PID grouping and scope retirement differ from the old two-key 9681620 failure? Does terminal RESET end readiness/derived keys while preserving caller-owned connection ownership? Which failure-state/raw-key/firmware policy questions remain OATH-owned? | `src/Oath/src/IOathSession.cs`, `src/Oath/README.md`, `src/Oath/CLAUDE.md`, `src/Oath/examples/OathTool/Program.cs`, `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathResetTests.cs`, `OathScpResetTests.cs` in that directory; `src/Core/src/Utilities/Base32.cs`, Core discovery XML, `docs/usage/device-discovery.md`, `docs/architecture/device-discovery-guarantees.md`, `docs/migration/v1-to-v2.md`. |
| Management | `MGMT-XREF-001/002/003`; related `MGMT-IMP-004/008/009/010`. `src/Management/src/ManagementSession.cs`, its extensions/backends; Core `DeviceInfo`, `FormFactor`, `CapabilityMapper` (inside DeviceInfo.cs), `IYubiKey.CachedDeviceInfo`; `src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs`. | Which FIPS bit/width and flagged form-factor cases still decode incorrectly? Does the current fixture bind live or cached metadata and one placeholder or per-device rows? How do last-known metadata and the serial latch behave after reconfiguration/substitution? Who enforces read/reset family and effective-version prerequisites? | `src/Core/src/Devices/DeviceInfo.cs`, `FormFactor.cs`, `src/Core/src/Abstractions/IYubiKey.cs`; `src/Management/src/IManagementSession.cs`, Management README/CLAUDE and `examples/ManagementTool/README.md`; `src/Tests.Shared/CLAUDE.md`; `docs/usage/device-discovery.md`, `docs/architecture/physical-device-model.md`, `docs/migration/v1-to-v2.md`. Reopen formerly resolved XREF-003 explanations when binding/cache assumptions change. |
| CTAP2 | `CTAP-XREF-003`, `CTAP-IMP-004/005/006/007/025/027/028/029/030/035/037/039/041/043`, `CTAP-DEC-002/003`; `src/Ctap2/src/Ctap2Session.cs`, `Backend/HidBackend.cs`, `Pin/`, `Cose/`, `Credentials/`; Core `FidoHidProtocol`, `RawCtapHidSession`, credential reader, mathematical/catalog helpers. | At which layer are UTF-16 counts, UTF-8 bytes, code points, NFC and authenticator minimum applied? Trace keepalive request, CTAPHID_CANCEL, terminal response/drain, raced success, malformed framing and raw/result ownership through Linux/macOS/Windows/custom routes. Which strict PIN-peer and experimental ARKG profile checks belong in CTAP rather than generic Core crypto? | Credential-option and PIN XML, `src/Core/src/Credentials/CredentialReaderOptions.cs`, `ConsoleCredentialReader.cs`, Core HID/raw-session XML; CTAP README/CLAUDE, `src/Ctap2/examples/FidoTool/README.md`, cross-layer tests and PIN/COSE fixtures; `docs/usage/user-interaction.md`, `docs/architecture/raw-access-tiers.md`, `docs/migration/v1-to-v2.md`. Preserve the ARKG draft-10 -65700 erratum. |
| WebAuthn | Original `WA-IMP-001..021`, resolved-doc `WA-XREF-001` alias `CTAP-XREF-001`, CTAP `DEC-002`. `src/WebAuthn/src/Client/`, backend/extension consumers, `src/Ctap2/src/Ctap2Session.cs`; Core prompt/session/connection machinery. | Identify every actual direct Core dependency and every indirect CTAP dependency. Which layer owns prompt intent, token negotiation, result snapshots, sensitive PRF/blob storage and ceremony timeout/outcome? Is selected native User output itself an RP/browser-equivalent serialization boundary? Preserve the actor decision before changing disclosure policy. | WebAuthn client/result/extension XML and README/CLAUDE; `docs/architecture/webauthn-extension-processing.md`, `docs/usage/user-interaction.md`, migration; current ceremony/retained-view/credential-membership test commentary. Recheck CTAP User/descriptor documentation without reopening it as an unfixed prose finding merely because WA descriptor recovery remains open. |
| Security Domain | `SD-DEP-001..005`, related `SD-IMP-005/008`; `src/SecurityDomain/src/SecurityDomainSession.cs`, backend and key material; Core `ApplicationSession`, `Scp03KeyParameters`, `Scp11KeyParameters`, `StaticKeys`, APDU formatter/response and global logging. | Separate channel establishment/readiness, device authentication and host management authority. Which supplied key/certificate objects are copied, borrowed or disposed through aliasing? Reset still calls reinitialization without original configuration/version: does the current callback preserve only readiness, or also effective options? Trace Le, raw reset bypass, failure cleanup and actual trace payload exposure. | Core session/SCP/key/formatter XML; `src/SecurityDomain/src/ISecurityDomainSession.cs`, README/CLAUDE, `src/SecurityDomain/tests/README.md`, SCP creation/reset/certificate test commentary; `docs/AI-DOCS-GUIDE.md`, `docs/LOGGING.md`, `docs/usage/tlv-processing.md`, migration. Preserve final-source gaps and draft/profile boundaries before a normative claim. |
| YubiOTP | `OTP-DEP-001..005`; related `OTP-IMP-003/004/005/006/009/012`. `src/YubiOtp/src/YubiOtpSession.cs`, `Backend/HidBackend.cs`, `Backend/SmartCardBackend.cs`; Core OTP framing/status/feature reports and native owners. | Establish exact report-length invariant for built-in versus custom connections, outbound CRC versus applet response CRC and programming sequence, cancellation/reset/failure latch/drain/reuse. Distinguish trigger metadata from challenge touch policy and raw status from effective firmware. Determine legacy/FIPS surface relocation from actual public APIs rather than a migration assumption. | YubiOTP interface/status/config/backend XML and README/CLAUDE; OtpTool commands and `src/YubiOtp/tests/CLAUDE.md`, `YubiOtpSessionLinuxCrossLayerTests` and backend tests; Core OTP/native XML; user-interaction, raw tiers, discovery guarantees and current versus historical migration passages. |
| YubiHSM Auth | `HSM-DEP-001..003`, `HSM-IMP-008`; `src/YubiHsm/src/HsmAuthSession.cs`, `SessionKeys.cs`, backend; Core session, prompt, Feature, TLV/formatter/response/crypto dependencies. | Trace SELECT fallback/effective version/reset retention. Which storage is copied and cleared before CALCULATE result transfer and callback completion? Preserve historical 6982/Failed plus fresh recovery without native-interruption claims. Does the supplied-cryptogram 56-byte receipt belong to an applet response/profile variant rather than Core's three-key derivation? | HSM session/SessionKeys XML, README/CLAUDE, tool and integration/probe explanations; Core response/TLV/session XML, shared user-interaction and TLV guide, migration. Keep response layout, raw-region identity, firmware range and real HSM/connector interop open until separately evidenced. |
| OpenPGP | `PGP-CORE-001..004`; related `PGP-IMP-003/010/016/018/019`; all `PGP-DEP-001..021` remain documentation-impact labels. `src/OpenPgp/src/OpenPgpSession.cs`, `.Pin.cs`, `.Config.cs`, `.Crypto.cs`, `.Reset.cs`; Core credential reader, Feature/session/SCP and SmartCard lifetime. | Distinguish card-profile PIN byte limits from generic reader character counts. Trace KDF cache alias/replacement/reset disposal through sync and async session teardown and SCP preparation. Establish per-exchange versus whole-workflow exclusion and prompt ownership. Keep raw 0.0.1, metadata 5.8 beta and override provenance separate. | PIN/options/KDF/factory/raw/crypto XML, OpenPGP README/CLAUDE, OpenPgpTool and `src/OpenPgp/tests/CLAUDE.md`, wire/ceremony assertions; Core reader/session XML; interaction, raw tiers and migration. The 21 exact original dependency sections are preserved in `registry.json:openpgp_documentation_dependencies`; they add zero Core bugs. |
| PIV | `PIVCORE-001..003`; retain `PIVIMP-001..010` identities and old `YESDK-1630/#22`; `src/Piv/src/PivSession.cs`, authentication/workflow code, `Cryptography/PrivateKeyOperations.cs`; Core PC/SC sharing/release, ExchangeGuard, session, response/TLV ownership. | What physical PIN state belongs to the card rather than session/handle? Which default/explicit sharing and power-off guarantees are source/assertion-backed, and which depend on OS/device evidence? Trace multi-command authentication/recovery/reset overlap and disposal admission beyond a single APDU. Distinguish response ownership from transferred crypto output. | PIV interface/operation/workflow XML, README/CLAUDE, PivTool README and affected integration/unit descriptions; Core sharing/session/response XML; user-interaction, physical-device model, raw tiers, applet-public-API and migration. Keep selected PIVIMP group triggers in the frozen investigation rather than renumbering them. |

## Evidence and ownership handoff

The discovery family receives the old abandoned-read/multi-key and metadata questions. The lifetime
family receives native admission, cancel/terminal/drain/release, session initialization/teardown,
workflow-versus-exchange and raw framing questions. The crypto/credential family receives SCP key and
certificate aliases, response/TLV/Base32 storage, credential units and provider/profile boundaries.
Coordinate these three families before completing the per-applet packets; a shared Core answer still
needs nine checked applet call-site and documentation dispositions.

Every packet records actual source and regression assertions, source authority, observed versus desired
behavior, source/hardware/OS limits, exact affected XML/guides/examples/tests and an independent reviewer.
The family owners may refine a seed after current inspection. Registry candidate statuses keep original
unknowns intact until that review.

## Reopening conditions for unavailable physical and late-platform work

- **Two-key discovery:** fresh matching serial/firmware/interface/topology/read path, caller budget,
  native task terminal state and actual claim release, followed by recovery, are needed to resolve the
  historical 9681620 abandoned-read question. Scoped discovery avoiding the old probe proves a narrower
  bypass/selection improvement when it is actually observed; it leaves the old native failure unexplained.
- **SCP:** the historical serial 125 FF and 00 INITIALIZE UPDATE responses were both 6A88. Record fresh
  key reference/state and another SCP-capable device before generalizing. INITIALIZE UPDATE success,
  EXTERNAL AUTHENTICATE and RESET are three separate executed boundaries.
- **PIV:** preserve the user's physical prerequisite/touch/hotplug and touch-dependent recovery deferral.
  Earlier Linux USB tests and deterministic sharing-policy assertions keep their recorded scope.
- **Windows:** wait for the user-provided CCID/HID concurrency handoff, then pin its commit, review changed
  claims/callers and define matching platform checks. Continue at this source baseline meanwhile; another
  worker's unfinished checkout supplies no qualification or inspection target.

## Identifier and count handoff

`registry.json` counts run-qualified provenance occurrences and nonadditive mechanism/question groups.
The complete original applet ledgers remain authoritative for applet-only findings. The registry includes
all 21 WA engineering IDs because the indirect boundary explicitly depends on their disposition; that
choice creates no new Core findings and completes no new whole-WebAuthn audit.

Reserve `CORE-DOC-001...` for accepted new documentation findings and `CORE-IMP-001...` for accepted new
implementation/verification groups **after Astra's collision check and family triage**. These are proposed
namespaces, with zero allocations in this preparation. Historical `CORE-001`, OATH `DEC-002` and CTAP
`CTAP-DEC-002` retain their source-run-qualified identities.
