# YubiOTP boundary candidates

Source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. These nine local concern groups use
`CORE-OTP-X001` through `CORE-OTP-X009`. They allocate no accepted Core IDs and are
not nine independent new bugs. The index supplies exact original unit identities and
candidate-associated unchanged paths. Documentation acceptance, runtime selection,
policy decisions and qualification remain separate coordinator gates.

## CORE-OTP-X001 — Keep creation and workflow ownership local

**Observed:** `IYubiKeyExtensions.cs:108–140` selects one transport, creates one hidden
connection and calls `OwnConnection` only after `YubiOtpSession.CreateAsync` succeeds.
Core's helper disposes a successfully opened connection if initialization fails.
The direct factory at `YubiOtpSession.cs:124–169` borrows the connection and binds
before reads. Backends borrow the effective protocol and retain no IDisposable state.
Core's platform owner controls native resources; the applet does not acquire a second
physical claim. Known member IDs coordinate process-local opens, with A/B1's partial
grouping/disjoint-view limits. Direct expert opens acquire no grouped-key registration.

`SmartCardBackend.WriteUpdateAsync:91–122` can make configuration and status calls in
two guard intervals. Sequence validation and `_status` publication occur after those
exchanges. HID response CRC/terminal resolution also occurs after Core releases its
exchange guard. ApplicationSession drains an admitted exchange, not the complete
public method, configuration object, callback task or provisioning workflow.

- **Documentation:** qualify README Constraints 324–329 and interface general remarks;
  retain complete session-before-connection scopes. Explain that callers await each
  complete operation and keep borrowed configurations/services alive through their use.
- **Runtime:** reuse B5 C006 and B1 ownership findings nonadditively. No new applet-wide
  lock or lifetime lease is authorized. The current backend shape needs no disposal hook.
- **Decision:** maintainers select any future whole-operation serialization/disposal policy.
- **Qualification:** Linux cross-layer borrowed reuse is a fake-native managed oracle;
  idle/sequential reuse and uncertain protocol recovery remain separate.
- **Reopen:** XML on factories/interface, README/CLAUDE, DeviceHelper and all complete
  task scopes; shared physical model, ownership guide, raw tiers and migration when
  connection claims, teardown or workflow admission changes. Keep unchanged accurate
  factory XML; it already distinguishes hidden and borrowed connections.

Aliases: `yubiotp-001:OTP-DEP-001`, `OTP-DEP-003`; B1 C005/C008, B5 C004/C006.

## CORE-OTP-X002 — Preserve HID initialization, polling and recovery classification

**Observed:** Core `OtpHidProtocol.cs:90–129,463–479` reads one report for protocol
firmware, performs the NEO refresh when that first major is three, then reads another
report for returned six-byte status. `HidBackend.cs:44–53` prefers that first protocol
version; `YubiOtpSession.cs:147–166` caches the second status and applies an override
only afterward. `Configure` then finds initialization already complete in this public
route. Its synchronous lazy wait remains reachable through other internal/raw paths.

Built-in Linux/macOS typed paths enforce eight-byte successful input; the Windows
compatibility adapter checks its returned report length. A caller-supplied
`IOtpHidConnection` reaches Core's indexing/slicing without a central length check.
Malformed custom input can therefore produce indexing/slicing exceptions before the
applet's six-byte write check. The interface contract says eight bytes; defensive
malformed-response behavior still needs a deliberate policy and meaningful test.

The NEO invalid-map probe catches every failure while `_resetFailure` remains null,
including readiness/transport failures after a successful abort. Python catches only
CommandRejectedError. This broad catch and tight three-second readiness loop are B5
C007 mechanisms, now traced into actual YubiOTP initialization. They are not new
independent applet bugs or a claim that the invalid map changed persistent settings.

- **Documentation:** add two-report/effective-version provenance and local timing
  conditions to initialization guidance. Describe 1 s write-ready polling with 5 ms
  busy delay, 3 s readiness with no explicit busy delay, 250 ms touch polling with
  the local 14 s threshold, and bounded packet count separately from native duration.
- **Runtime:** refer report-size admission, NEO catch and polling cadence to B5 C007;
  tests should use the public applet path, including malformed first and second reads.
- **Decision:** choose custom-provider malformed exception policy and classified NEO
  rejection behavior without copying Python's timing/ownership policy wholesale.
- **Qualification:** elapsed checks run between awaited native calls; a held GET/SET
  can exceed them. Only `OtpHidTouchTimeoutException` becomes TimedOut. NEO timeout
  swallowed during initialization emits no presence outcome because the probe uses None.
- **Reopen:** `HidBackend.ReadInitialStateAsync`, session initialization, CLAUDE/README,
  Core OTP comments, raw tiers, creation/recovery and Linux cross-layer test commentary
  after report admission, version source, NEO catch, polling or reset policy changes.

Aliases: `yubiotp-001:OTP-DEP-002/004`, `OTP-IMP-005/006`; B5 C001/C007/C008.

## CORE-OTP-X003 — Trace every secret-derived response owner

**Observed:** HID data responses travel from successful native GET arrays into Core's
cleared pooled accumulator, then an independent assembled array, then an applet prefix
view. Core clears payload/frame/output report and accumulator, while successful incoming
report arrays remain intact (`OtpHidProtocol.cs:327–390,484–489`). Linux and macOS
owners explicitly transfer those arrays. Windows adds another report-ID buffer on
GET and SET (`WindowsHidReportAccess.cs:111–148`) that is not cleared by the adapter's
outer copy cleanup. These are B5 C005/B4 C007, with actual HMAC/AES callers identified.

SmartCard serial/HMAC/AES calls pass through the APDU pipeline; `ApduResponse` clones
raw transport response. `SmartCardBackend.cs:153–166` returns a prefix alias of its
Data, not the response owner. Clearing the returned segment reaches that prefix,
leaving excess data/status and the transport source copy distinct. Failure cleanup
clears the whole Data view under ordinary callback/logging behavior. B6 owns raw,
fragment, decrypted/SCP and response-carrier cleanup; keep those shared mechanisms
under B6 C002 rather than allocating another applet defect for each copy.

`UserPresenceNotification.ResolveAsync` logs a callback failure when a primary failure
exists. A throwing logging provider can itself escape before each backend's subsequent
zeroing statement. This conditional all-path limit belongs with C C008/B5 C005, not a
claim that ordinary prompt-failure zeroing is absent. Existing backend tests do observe
their actual response arrays becoming zero on ordinary terminal callback failures.

- **Documentation:** retain caller source/result cleanup and avoid all-copy assurances.
  Clarify the result view versus complete owned response and lower staging scopes.
- **Runtime:** reuse shared copy/logging groups; define receive ownership before adding
  clearing to custom-provider results that may be borrowed or reused.
- **Decision:** select transfer/copy/clearable-owner contracts and cleanup precedence;
  do not apply MemoryMarshal writable conversion to arbitrary borrowed memory.
- **Qualification:** only semantically sensitive HMAC/AES/challenge/configuration data
  requires secret cleanup. Serial/status/firmware metadata alone needs no secret zeroing.
- **Reopen:** both backends, calculation XML, README security/result examples, Core raw
  tiers/APDU response docs, platform report comments, terminal-callback and retained-array
  tests after any owner/transfer/clear/logging behavior change. Existing accurate
  configuration-source and callback-before-transfer prose remains unchanged context.

Aliases: `yubiotp-001:OTP-DEP-002/003`, `OTP-IMP-003`; B5 C005, B4 C007, B6 C002, C C008.

## CORE-OTP-X004 — Retain a one-shot result until hidden-session teardown succeeds

**Observed:** `IYubiKeyExtensions.CalculateHmacSha1Async:83–94` evaluates a successful
calculation result inside `await using`, then awaits session disposal before its own
task can transfer that result. If protocol/owned-connection disposal throws, the helper
faults and gives the caller no result view to clear. The backend already considers
its output transferred. This is a distinct outer producer boundary, extending
`yubiotp-001:OTP-IMP-003`; ordinary successful disposal is unaffected. No native failure
was induced and no result was recovered experimentally.

- **Documentation:** qualify one-shot result ownership on exceptional teardown; keep
  source/input cleanup in caller finally even when session creation/disposal fails.
- **Runtime:** propose explicit result retention and failure clearing in later scoped
  work; avoid changing the public response type during this documentation investigation.
- **Decision:** maintainers choose primary-operation versus teardown exception policy
  and whether successful output may ever transfer alongside a teardown diagnostic.
- **Qualification:** use a deterministic fake connection with valid synthetic CRC and
  throwing/held DisposeAsync. Distinguish backend success, zeroing and outer task outcome.
- **Reopen:** the extension's XML/body, one-shot README example 164–185, interface output
  ownership, backend transfer explanation and extension lifecycle tests after a fix.
  GetConfigState/Put one-shots remain context because they return no sensitive calculation
  buffer; preserve their current resource scopes rather than labeling them defective.

## CORE-OTP-X005 — Keep configuration and parser lifetimes outside exchange drain

**Observed:** `SlotConfiguration.GetConfig:150–184` copies secrets before validating
new-code length; `YubiOtpSession.WriteConfigAsync:529–549` obtains config bytes and
validates/appends current code before its finally. These retained OTP-IMP-003 paths
remain unchanged. Normal source/config/assembled-frame clearing reaches separate owners.
The session copies config synchronously at call start, while config disposal is still
the caller's responsibility. A guard around one exchange does not own it.

Standalone ChalResp/Hotp/Static commands acquire secrets before parsing access-code
text and before cleanup try. The unified `OtpStaticCommand` correctly translates text
but builds its disposable config before parsing access code and entering try
(`OtpCommands.cs:531–571`); a parse failure can strand that config. This narrow CLI
handoff preserves OTP-IMP-003/010 and the correct historical N3 encoding fix.

- **Documentation:** retain existing preflight qualifications, separate mutable source,
  immutable string, config fields, GetConfig array and transport staging lifetimes.
- **Runtime:** later engineering should validate before secret acquisition or guard every
  acquired owner immediately. Do not repair sample runtime under Pass 1.
- **Decision:** define exceptional-path cleanup/diagnostic policy with the actual CLI owner.
- **Qualification:** actual-array observers and parse-failure/no-I/O vectors are meaningful;
  post-disposal refusal and no-I/O alone do not prove erasure.
- **Reopen:** base/config/session XML, sample setter comments, unified CLI static command,
  security task examples and migration ownership after validation/cleanup changes.

## CORE-OTP-X006 — Separate firmware policy, capability and presence certainty

**Observed:** SmartCard initialization probes Management on both USB/NFC, uses it when
parseable, and takes the greater version when raw OTP major is three. HID uses first
protocol report firmware. Override governs effective session gates and SmartCard
sequence-bypass interval, while cached ConfigState gates and SmartCard touch heuristic
use raw status. Major-zero policy admits features and configuration types; it is no
proof of actual firmware generation or operation support.

HID backend resolves after length/CRC; SmartCard resolves after APDU success/minimum
length. SmartCard PolicyRequires comes from keyboard-trigger cached bits, not read-back
of RequireTouch. A successful calculation after post-admission cancellation can resolve
Completed. A callback-induced pre-admission cancellation test is a different oracle.

- **Documentation:** preserve local source/override/transport conditions. Avoid deriving
  OTP support from Core algorithm catalog applicability, general SCP floors or FIPS bits.
- **Runtime:** retain OTP-IMP-004/005/006/012 and B5/C policy owners; no heuristic repair
  or support expansion is authorized. SmartCard status prefix/sequence gaps stay applet-owned.
- **Decision:** choose supported command/interface/version matrix and prompt certainty.
- **Qualification:** old serial 103 results include raw/effective 0.0.1, metadata
  5.8.0.beta.0, USB CCID HMAC rejection 6985 and successful HID calculations. Retain those
  facts without universalizing CCID rejection, major-zero support, NFC or old firmware.
- **Reopen:** firmware/ConfigState/session/configuration XML, both backends, requirement
  tables, shared interaction, migration and prompt/firmware tests after provenance,
  override, capability or certainty changes. Preserve existing HMAC marker/AES verifier
  qualifications and their original finding scopes.

## CORE-OTP-X007 — Route keyboard and credential responsibilities to actual callers

**Observed:** `StaticPasswordSlotConfiguration.cs:148–177` uses Core translator indexers
after a full validation pass; it avoids the generic GetHidCodes string→char-array helper.
Standalone OtpTool StaticCommand still sends UTF-8 bytes as scan codes and ignores
layout. Unified `OtpHelpers.CreateStaticPasswordConfiguration` uses the working string
overload. Its generator includes both cases from ModHex's supported map, yielding
32 non-whitespace letters, rather than its comment's 16-letter description.

Neither static caller uses ConsoleCredentialReader. That reader's redirected line
confirmation fix is a separate historical N3 scope. Its encoded credential bytes are
not HID translation. Correcting Core reader documentation cannot fix the standalone
sample; changing the sample cannot prove a locale table renders on every host.

- **Documentation:** qualify README's “layout-independent/neutral” wording to supported
  host layouts and selected characters; retain B4 C010's mapping/alias/locale limits.
- **Runtime:** keep OTP-IMP-009 under the standalone applet sample; refer generator prose
  to B4 C010 and early unified-CLI config lifetime to X005.
- **Decision:** select supported keyboard layouts/characters and production credential
  input policy; keep immutable CLI strings and printed secrets visible in that decision.
- **Qualification:** current literal en_US lowercase/uppercase and ModHex byte vectors
  prove encoding. Physical keyboard output, dead keys/host state and older capacities
  require a later coordinated editor/input-capture lane, never a shell or unattended touch.
- **Reopen:** static constructor/XML and README static example, Core translator docs,
  standalone layout/help comments, unified helper prose/tests and migration static row
  after mapping or caller routing changes. Preserve working string helper and exact
  literal vectors; do not imply all CLI paths share the broken standalone implementation.

Aliases: `audit-v2-20261022-20261031:N3`, `yubiotp-001:OTP-IMP-009/010`; B4 C010, C C001–C004.

## CORE-OTP-X008 — Resolve the migration relocation question by named surface

**Observed:** complete YubiOtp API and Management interface/DeviceConfig review finds
no typed QueryFipsMode, SetLegacyDeviceConfig or SetMode API. A complete current
public-snapshot name search corroborates those absences. Management **does** expose
GetDeviceInfoAsync, delegated to Core DeviceInfoReader. Its OTP path sends capabilities
command 0x13, validates the declared prefix/CRC and supports pages. Modern
SetDeviceConfigAsync serializes Management TLVs, with effective 5.0.0 write gate;
it is not the old four-byte mode/timeout/eject command. Raw session command access
remains an expert escape hatch rather than a relocated typed implementation.

- **Documentation:** update only active YubiOTP migration entries 246–254 and the
  outstanding-verification row 419 to split device-info relocation, absent typed
  FIPS-mode query and absent legacy mutator. Keep point-in-time historical surveys intact.
- **Runtime:** no new missing Core feature is allocated. Core FipsCapable/FipsApproved
  mapping concerns remain A C004; these metadata fields are not an OTP FIPS-mode query.
- **Decision:** maintainers select whether legacy query/mutation deserves a typed v2 API.
- **Qualification:** this is a source-surface inventory at the pin, not old-key device
  compatibility, equivalence of every v1 response or successful modern/legacy execution.
- **Reopen:** migration map/gaps/guide and YubiOtp/Management public reference if named
  APIs arrive, disappear or change gates. Retain README Related Management link, which
  already routes device-level enabling correctly. Library generation conveniences,
  TOTP, NDEF read-back and 38/45 scan-map findings keep their exact applet scopes.

Alias: `yubiotp-001:OTP-DEP-005`; Management boundary owns complete read/write enforcement.

## CORE-OTP-X009 — Keep actual assertion and qualification scopes

**Observed:** all assigned tests and helpers were read. Linux cross-layer tests exercise
actual production slot/device/native owner over fake report seams. Backend callback
tests inspect actual returned arrays; same-helper CRC generation is not an independent
known answer. Twelve HMAC device vectors have historical one-key execution, separate
from this packet's zero executions. The misleading high-byte/length/dispose test names
already have appropriately narrow comments and need no churn.

Held Linux creation releases its first-GET gate in finally but obtains/disposes its
first session only on the success path; an intervening assertion can skip disposal.
Protection cleanup calls are sequential, so first deletion failure skips slot one.
Programming often occurs before a cleanup try. These extend OTP-IMP-011 and B5 C008,
with no test assertion or helper changes here.

- **Documentation:** retain source/assertion versus executed-evidence language and
  per-device/firmware/transport limits. Correct the small stale CRC “no flags” comment
  at SlotConfigurationTests 1043–1049 if that unit is edited: defaults remain present.
- **Runtime:** strengthen meaningful failure-path release/drain and independent CRC,
  exact interval/zero rules, custom-report and one-shot output oracles separately.
- **Decision:** select each test's intended contract before changing assertions that
  deliberately pin erroneous current IMF behavior or permissive sequence rules.
- **Qualification:** preserve NFC read-back, old firmware, keyboard output, Windows
  late-handoff and PIV physical deferrals. No new build/test/hardware execution occurred.
- **Reopen:** named test units, nested instructions and module test/qualification guides
  after fixtures/assertions/cleanup change; remove later edited overlaps from clean sampling.
