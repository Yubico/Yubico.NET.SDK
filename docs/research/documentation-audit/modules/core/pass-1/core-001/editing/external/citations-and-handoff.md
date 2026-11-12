# External edit: source authority and held comment needs

## Source-backed anchors

All product anchors below refer to `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`, not another editor's
uncommitted source. Exact shared units/ranges/hashes and family evidence coordinates are in
`path-decisions.json` and `candidate-decisions.json`. Complete family reports and all nine available
completed boundary reports were consumed read-only; old pending-B4/B6 text is interpreted through
completed follow-ups, not counted as current missing source. This supplies no independent boundary
acceptance or new assigned investigation coverage.

| Claim | Actual implementation/assertion anchor and limit |
| --- | --- |
| Fixture identification/authorization | `src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs`, complete `InitializeDevicesAsync` and `FilterDevices`; `Infrastructure/AllowList.cs`, constructor; `YubiKeyTestState.cs`, `EnsureBound`/`BindToRealDevice`. Live Management reads precede serial filtering. Empty authorized results lead to row-binding SkipException; empty configuration's exit is a separate constructor condition. |
| First transport is device preference | `src/Cli.Shared/src/Device/DeviceSelector.cs`, `SelectDeviceAsync`/`GetSelectionAsync`; `src/Cli.Commands/src/Infrastructure/YkCommandBase.cs`, `ExecuteAsync`. PreferFirstTransport chooses a view, while `PreferredConnection` receives only `requestedTransport`. |
| Form-factor and FIPS filtering | `src/Core/src/Devices/DeviceInfo.cs`, flagged full-byte cast and FIPS tags through FromApp; `src/Cli.Shared/src/Device/FormFactorFormatter.cs`, full switch; Tests.Shared full-value/any-bit predicates. No repaired decoder, normalization or certification inferred. |
| CLI input units and owners | `src/Cli.Shared/src/Output/SecureCredential.cs`, `Prompt`, `ReadMaskedConsoleInput`, `ReadWithPooledBuffer`, `ReadRedirectedInput`, `Dispose`; `PinPrompt.cs`, Resolve/PromptForCredential/ConfirmMatches. Raw redirected bytes are not a Core managed-string reader, interactive UTF-8 encoding is not normalization, and input is synchronous. |
| Recorder and credential-test oracles | `src/Tests.Shared/RecordingSmartCardConnection.cs`, full class; `src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/SecureCredentialTests.cs`, exact length, decline and tracking-pool tests. Recorded command copy/response alias and counter-only disposal are distinct from transport/native processing. A null result alone proves no clearing. |
| No-tests and translator limits | `scripts/toolchain/Results.cs:97–109`; `TestTargets.cs:97–117,158–257`; `Context.cs:44–50,71–78`; `Discovery.cs:10–26`. The gate rejects all-NO-TESTS, not each no-test project. Exit-zero/null-count or parsed zero/zero can pass. Separate same-kind arguments are source facts; pinned repeated-switch selection remains unrun. |
| Full-argv capture | `scripts/toolchain/RunLog.cs`, Start/BuildResult/WriteResultJson; `toolchain.cs:85`; Process command masking is a different carrier. No real key was supplied. Runtime redaction/plumbing is not fixed by warning in TOOLCHAIN. |
| Operator cue | `src/Tests.Shared/Infrastructure/OperatorCue.cs`, complete Announce/CreateSpeechStartInfo. Console/echo precede catch-protected speech. WaitForExit's 15-second wait neither kills the child nor confirms audible delivery. |
| Benchmark evidence | `benchmarks/Yubico.YubiKit.PerformanceBenchmarks/Program.cs:76–94`, fixture selection; `AsyncBoundaryBaseline.cs`, Sample/RunChildAsync/WriteDatasetAsync; `BoundarySelfTest.cs`, synthetic classifications. Read-only dependency anchors, outside the edit allowlist. No source fixture serial/FW assertion, loaded-library/producer attestation, native duration or all-schedule proof is manufactured. |
| Raw SCP and lifecycle | `src/Core/src/Devices/YubiKeyConnectionExtensions.cs`, raw factories and CreateSessionOverTransportAsync; `Sessions/RawSmartCardSession.cs`, CreateCoreAsync/SelectAsync; `ApplicationSession.cs`, configure/initialize/disposal; `Utilities/ExchangeGuard.cs`, admitted exchange. Session drain is not a whole-public-workflow lease. Plain SELECT retains old SCP graph. |
| SCP-contained owners | `src/Core/src/Protocols/SmartCard/Scp/StaticKeys.cs`, constructors/disposal; `Scp03KeyParameters.cs`, Keys/Dispose; `Scp11KeyParameters.cs`, constructors/Dispose. Array cloning, retained objects, copied collection and shared record clones have separate lifetimes. No full hidden-buffer erasure assertion. |
| OATH retry example | `src/Oath/src/OathSession.cs:844–865,880–890`; `src/Tests.Shared/YubiKeyTestStateExtensions.cs`, awaited Management helper. The retry helper derives/clears its key but borrows password memory. New migration method clears the owned password after actual work and session disposal, including factory failure. CORE-OATH-X006's original migration referral is reused, not automatically closed. |
| Provider/codecs and reflection | `src/Core/src/Cryptography/CryptographyProviders.cs`, public Rng/SHA/AesCreator versus internal Arkg/Ecdh/CmacPrimitivesCreator; actual Base32 source; complete PublicApi FactoryShape/AsyncSurfaceConvention tests. Reflection shape and factory substitution are not all-operation routing or runtime conformance evidence. |

## Exact original-reference qualifications

No protocol reference URL, section or edition was upgraded in the product diff. No new normative
card/platform requirement is published. The following retained authorities were checked through their
qualified registers and clarifications for applicability; that reuse is not independent revalidation
of every original passage.

| Retained authority | Revision/status/section/actor/strength and applicability |
| --- | --- |
| B1 clarification | NIST SP 800-73-5 Part 2, July 2024 final, §2.4.2 describes card-application security status, not per-process/handle isolation. Direct PDF 403; only Exa-attributed passage retained. Independent original-PDF review remains held. PC/SC Part 5 revision 2.01.01, September 2005, §3.2.5.2 is a separately retained original: desired disposition/if-possible conditions are not observed power-off or PIN clearing. No revised normative PIV wording added here. |
| B6 final register | SCP03 1.1.2 **Public Release**, March 2019, PDF SHA-256 `973a10834f7fe8befd98da9af65393622234b80745092bd85248804c57b648dd`; code-cited 1.1.2.6 is **Public Review**, December 2019, whose raw clauses remain uninspected. SCP11 1.3.0.13 **Public Review**, August 2023, PDF SHA-256 `5db47b3df3c4e565a5ad91257395604f3482ed5db70c64853544c3b0e289fde0`; final 1.4/profile clauses remain unverified. Draft a/c device-side first-correct-MAC authentication is distinct from host receipt/setup flags; b is card-only, c uses static SD material. Product edits describe source containment/reselect, not final conformance. |
| C final register/addendum | .NET runtime **v10.0.0** AsnWriter Encode copies while retaining writer storage; Reset clears the written prefix. Supplied-Q macOS import skips the checked absent-Q extraction route. These are pinned implementation facts, not installed-runtime/native erasure. X.690 **02/2021** and exact RFC/profile distinctions stay in the original C register; no generic DER/provider acceptance is inferred. |
| B4 native register | NativeShims **1.18.1**, release source commit `dedba95b94946907112a7953cd82df8a15c22655`, is available. The target's conditional Linux LP64 pointer-forwarded PCI discrepancy and separate Windows/macOS layouts stay qualified. Package/source/hash identity is not installed-producer/header identity. AOT static archive/link reachability is separate from JIT shared loading and actual native/device execution. |
| A source register | Manufacturer form-factor/configuration tables are descriptive unversioned vendor references, not FIPS operation certification. The null topology resolver is current SDK behavior, not proof of absent OS APIs. The original Rust `7d7a7455` comparator/full source identity is still missing; current pinned Python is not a replacement. |
| FIDO completed editor referral | CTAP 2.1 PS/2022 errata §6.5/6.5.1 distinguishes platform/code-point/UTF-8/NFC/final-NUL responsibilities. No CTAP rule is imposed on the general CLI reader. CTAPHID cooperative cancellation is not a native completion deadline. WebAuthn RP authorization and selected-native-output actor/recovery decisions retain their actual boundary owners. The FIDO batch is under independent review, not blanket accepted. |

## Explicit held/out-of-scope work

`outside-owner-followups.json` enumerates exact candidate paths, IDs, original source anchors and
reopening triggers. Important actionable cases are:

- `scripts/toolchain/TestTargets.cs`: contradictory same-kind AND commentary, separate-argument
  translation and pinned runner selection need the A C018 wording/selection follow-up. No runner ran.
- `scripts/toolchain/RunLog.cs`, `Process.cs`, `Options.cs`, `PackageTargets.cs` and root `toolchain.cs`:
  full-argv persistence remains a distinct credential carrier despite displayed-command masking.
  Future comment/redaction work must not imply the warning fixed capture or tested real credentials.
- `scripts/toolchain/Results.cs`/`TestTargets.cs` and toolchain executable help/output: preserve
  aggregate/no-count gate limitations and do not silently tighten assertions/guards to make prose true.
- `.github/workflows/{build,native-aot}.yml`, `Directory.Build.props`, `global.json` and other project/config
  context: no configuration or SDK/SourceLink version assurance is added. Compiler configuration is
  not resolved SDK/producer/runtime evidence.
- `docs/migration/v1-to-v2-map.yml`: retain its existing parser/relocation/primitive-factory issues,
  including original `oath-pilot-and-fixes-wave-002:DOC-002`. No YAML parse repair is authorized.
- `docs/migration/v1-to-v2-gaps.md`, changelog, root async ISA, `docs/plans/**` and named Linux/AOT
  `docs/research/**` captures: preserve dated baselines, original source/log identities and failed,
  blocked/skipped outcomes. Current guidance lives in the active migration/toolchain surfaces instead.
- Primary `verification/**` and `benchmarks/**` paths named by B2/B3/B4: not in this shared ledger.
  Their source-supported comment needs include invocation versus terminal/native timing, killed-child
  censoring versus SDK drain, replay/snapshot assertions versus full races, compiled versus executed
  AOT support, selected fixture versus full authorization, historical Mac `.8` versus stable source,
  and asset hashes versus a process-loaded producer. They need a separately explicit owner/allowlist.
- `src/Cli.Commands/src/Otp/OtpCommands.cs` executable Description still says ModHex types the same on
  any host. Only its XML was corrected. Preserve the working unified CLI translation separately from
  standalone OtpTool's UTF-8-as-scan-code defect and CORE-OTP-X005's pre-parse config cleanup gap.
- `src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs` is retained. Its keyEnc/keyMac allocations occur
  before their inner try; do not extend local finally wording to every allocation failure. Likewise
  C's AES key/IV allocations before try qualify that helper's cleanup scope. No generic all-path or
  all-provider erasure claim follows from disposing a provider or clearing one observed array.

No FPX/A-E input/ABI/wire/provider experiment ran. FIPS mapping/width, capability width, metadata aliases,
cache validity, Unicode/profile policy, callback/logging exceptions, whole-workflow admission,
SCP phase/empty/chaining/profile/oracle, native producer/release and physical evidence gaps remain held.
Keep `fixes-wave-001:CORE-001` serial-9681620's physical cause unexplained. Serial 125's two 6A88 setup
failures were before EXTERNAL AUTHENTICATE/RESET and add no protected reset result. Windows concurrency
work remains unready and PIV physical deferrals stand. Finish Core Pass 1 and its deferred handoff, then stop.
