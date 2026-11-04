# Whole-module YubiOTP investigation

**2026-11-04 · GPT-6.1 Sol · baseline95e5ccaeda33954c21f2a38058d4f66b5f208787**

Report-only investigation complete. All63 tracked module paths were read fully:23 library C#,14 OtpTool
C#,9unit/5integration C#,4Markdown,4projects,2API snapshots and2runner configs. Inventory hashes still
match baseline. Generated `obj/` C# is excluded, not mistaken for tracked library source. No hardware,
build, test, formatter, consumer-content edit, commit or delegation performed. Independent citation
captures and failed access attempts are under `evidence/`; hardware execution belongs to coordinator.

Candidates below are **not adjudicated findings**. IDs OTP-C001 onward are stable across triage; coordinator
can map accepted behavioral/dependency items to OTP-IMP/DEP without losing these origins. Observed code,
vendor intent, proposed documentation and pass2 work are deliberately separate. No runtime fix is authorized
by this report. Source references are baseline line numbers. Common path root is `src/YubiOtp/`.

## Scope and evidence

Read root `CLAUDE.md`, module/test instructions, run plan/status/inventory, campaign workflow and approved
style. Read `docs/TESTING.md`; relevant house-style principles/session/ownership/error/testing/documentation
sections. Narrow active shared surfaces: usage interaction; YubiOTP migration recipe/module section and map
entries; YubiOTP gaps; comparison/highlights; backend/creation/selection architecture and YubiOTP NEO claim.
No Core or other-module source/docs were investigated. Other-module links were considered targets only.
Research/historical documents found by search were **not** treated as active consumer documentation or
proof of present implementation. No prior audit conclusions were substituted for this read.

`evidence/sources.md` carries exact original title/edition/status/section/actor/strength/applicability and
focused independently checked clauses. S1=vendor Manual3.3; S2=personalization1.20.0 header/encoder/manual;
S3=manager5.8.0 implementation; S4/S5=NFC URI/Text RTD1.0 originals via identified mirror; S6=RFC4226;
S7=.NET Context7. Source capture manifests retain URLs/HTTP failures/hashes. Initial research plus one
focused follow-up is exhausted: unresolved modern firmware, marker-minimum and status meaning go to precise
device/dependency questions, not another speculative search round.

## API reconciliation

24 source-declared effective-public top-level types exactly match the snapshot type rows:

`ConfigFlag`, `ConfigFlagMasks`, `ConfigSlot`, `ConfigState`, `ExtendedFlag`, `ExtendedFlagMasks`,
`HmacSha1SlotConfiguration`, `HotpSlotConfiguration`, `IYubiKeyExtensions`, `IYubiOtpSession`,
`KeyboardSlotConfiguration`, `NdefType`, `Slot`, `SlotConfiguration`, `SlotExtensions`, `SlotOperation`,
`StaticPasswordSlotConfiguration`, `StaticTicketSlotConfiguration`, `TicketFlag`, `TicketFlagMasks`,
`UpdateConfiguration`, `YubiOtpSession`, `YubiOtpSlotConfiguration`, `YubicoOtpChallengeResponseSlotConfiguration`.

`PublicAPI.Shipped.txt` contains only nullable directive. Unshipped has193non-directive/nonblank entries,
**not193 callable methods**.24 are type rows;18 represent protected subclass surface (including implicit
protected base constructor), one is compiler-provided parameterless ConfigState constructor, one implicit
public UpdateConfiguration constructor, one extension-block grouping and four static spellings duplicate
the four C#14 instance-extension declarations. Remaining144 include public methods/constructors/properties,
enum fields and constants, not144 independent user tasks. Interface/concrete session declarations are both
public contract surfaces but share ten operation families; the interface has substantive XML while most
implementations do not repeat/inherit it explicitly. There are protected getters/setters/flags despite
snapshot rows not printing accessibility. Public members of internal `YubiOtpConstants`, internal backends
and internal `YubiOtpInitialization` are not effective-public APIs. Test/OtpTool public classes are separate
nonshipping project surfaces. No prose is needed for invented generated types or repeated extension rows.

**API families read:** creation/transport/ownership; serial/status; replace/update/delete/swap; map/NDEF;
two calculations; configuration base/protected encoder/disposal; six concrete modes; keyboard flag family;
update masks; slot mapping; raw mode/command enums. `coverage.json` provides semantic dispositions/clean
units; final editor/reviewer must remove edited-overlap clean nominations before sampling.

## Candidate findings and edit/dependency map

### OTP-C001 — Make replace/update/access-code tasks explicit

**Observed:** `IYubiOtpSession.cs:44–108` calls accessCode “optional ... to set”; `SlotConfiguration.cs:48,
135–170` defaults SerialApiVisible+AllowUpdate and serializes **zero new code when omitted**.
`YubiOtpSession.cs:511–555` appends current code; it does not fetch/preserve any code. Update masks supply
the complete transmitted values, not a read-modify-write patch. `UpdateConfiguration` has no keyboard
constructor defaults: new instance carries ext24hex, ticket00/config00. S1§5.6.6 and S2 confirm update
flags include AllowUpdate and dormant has one-way activation constraints. S3 records4.3.2–4.3.5
access-code-update restriction absent locally. Put accepts even UpdateConfiguration because parameter is
base type, while update requires UpdateConfiguration; describe operation semantics, not “safe builder” magic.

**Proposed docs:** README practical mode table and replacement versus update workflow; interface XML and
base/update type XML state defaults, full-mask behavior, caller retains config ownership, six-byte current
authentication versus six-byte new setting, repeat new=current to retain protection, omitted/allzero removes
protection where firmware allows. No read-back of secrets/config flags; keep own intended settings. Explain
access code is **write protection**, not challenge authentication/transport encryption. Swap permissions
are device-enforced and may fail (vendor both-updatable condition); ProtectSlot2 is separate. Example with
code retained in both arguments and cleanup, not new runtime enforcement. Add dormant/AllowUpdate caveats.

**Pass2/decision:** firmware-specific update restriction and preventing accidental state loss/mode misuse
need re-baseline/product decision. A helper named Update must not silently become patch semantics in Pass1.
**Reopen:** access-code defaults, flag defaults/masks, update/put type validation, swap/protection enforcement
or hardware update results change. Dependents: README, IYubiOtpSession, SlotConfiguration, UpdateConfiguration,
IYubiKeyExtensions, OtpTool Settings/ChalResp/Hotp/YubiOtp comments, migration recipe/map.

### OTP-C002 — Correct flag meanings, aliases and firmware applicability

**Observed:** `KeyboardSlotConfiguration.cs:84–109` and `UpdateConfiguration.cs:97–129` incorrectly say
FastTrigger slot2-only and PacingChar10/20 add20/40ms. Bits actually04/08; S1§5.5.3/.4 says10/20ms,
combined30; §5.6.5 fast trigger applies with only one programmed configuration; S2 notes always effective
on3+. `ConfigFlag` aliases share bit meanings by mode, not freely combinable independent behaviors.
EXT serial flags merge device-wide (S1§5.6), while setters currently sound per-slot. Delay flags older
firmware only; serial button2.x, USB serial unavailable3.0/3.1; numeric-keypad restrictions are vendor
applicability, not enforced in local setters. `MinimumFirmwareVersion` checks configuration type only,
not every selected flag.

**Proposed docs:** fix pacing and fast-trigger XML; compact enum/member explanations distinguish mode
aliases and update-mask interpretation; explain actual defaults and global serial merge. State SDK checks
type/operation minimums, not all firmware/flag combinations. Preserve legacy flags with qualifications;
do not claim unsupported behavior removed. Use S1/S2 explicitly for legacy applicability, not universal5.x
rules. **Pass2:** per-flag support validation/product policy separate. **Reopen:** masks/defaults/feature
validation or firmware semantics change. Dependents: both flag-builder classes, base flags, enums/masks,
README requirements/defaults, OtpTool pacing XML and settings explanation.

### OTP-C003 — Public ID input is raw binary, not pre-encoded modhex

**Observed:** `YubiOtpSlotConfiguration.cs:23,34`, `SlotConfiguration.cs:30` and integration comment
`YubiOtpSlotConfigTests.cs:77` say modhex-encoded bytes. `InitializeKeys`/SetFixed copies raw bytes; no
modhex decoding; S2 `config_st.fixed` explicitly binary. OtpTool public-ID parser consumes hex **text**
and produces these bytes. **Proposed docs:** raw public ID up to16bytes; key emits two modhex characters per
byte. Distinguish AES/private identity from public prefix and no OTP verification performed by module.
**Pass2:** no demonstrated runtime defect here. **Reopen:** input representation/encoder changes.
Dependents: YubiOtpSlotConfiguration, base wire table, integration comment; relevant README mode task.

### OTP-C004 — Correct sensitive ownership claims; retain exceptional-path leaks

**Observed:** README172–176 calls keys/passwords/codes “borrowed” and tells readers to zero string, while
configuration constructors **copy** into private fields; strings cannot be zeroed through this API (S7).
Dispose clears fields, not independent arrays previously returned by GetConfig. Successful response
ownership transfers to caller; ReadOnlyMemory isn't erasure and SDK returns array-backed views. README
example92–94 never clears challenge/response. Backend clears received output when validation/resolution
prevents transfer, but broad “response buffer when an operation fails” overstates paths with no returned
buffer or failures before ownership. AES challenge is borrowed unchanged; only HMAC creates and zeroes
its padded copy. Keep buffers stable until awaited operation ends, including failure/drain.

**Exceptional-path implementation evidence:** `GetConfig` copies all secret fields into freshly allocated
config **before** checking invalid accCode length (`SlotConfiguration.cs:141–156`), then throws without
zeroing that array. `WriteConfigAsync` creates configBytes then `BuildPayloadWithAccessCode` **before try**
(`YubiOtpSession.cs:518–521`): invalid current code strands populated secret array. No I/O still occurs;
the documentation's no-I/O preflight guarantee does not imply secure cleanup. Config setter flags lack
disposed checks while encoder/data setters do check; do not promise every method throws after Dispose.

**Proposed docs:** configurations own copies and must be disposed; caller owns source buffers and every
GetConfig result; zero outputs once consumed using a suitable owned-buffer strategy, no promise of full
heap erasure. Immutable string tradeoff explicit; raw scan-code overload enables mutable-source cleanup.
Examples complete finally scopes. Narrow security absolutes to specific buffers/paths; no new lifecycle
policy or return type in Pass1. **Pass2 high:** zero exceptional preflight allocations, consistently validate
before copy/try; response ownership ergonomics and post-dispose mutation need policy. **Reopen:** allocation,
cleanup ordering, response carrier/owner, config lifecycle changes. Dependents: README security/examples,
IYubiOtpSession returns/parameters, config GetConfig/Dispose/XML, module CLAUDE security, migration recipe,
OtpTool secret-output notes. Core copies/drain remain D002/D003, not investigated.

### OTP-C005 — Cached state and “touch-triggered” are not fresh challenge policy

**Observed:** `GetConfigState` simply parses `_status` (session186–190), set at creation or successful
write; no fresh I/O. `ConfigState.FirmwareVersion` comes from **raw OTP status**, whereas session uses
effective management/OTP selection/override. `CreateUserPresenceContext` uses IsTouchTriggered to infer
SmartCard RequireTouch policy. S3's `is_touch_triggered` explicitly means touch-triggered **not
challenge-response**. Local unit tests just set fake status bits; live HID touch probe prints that bit
without asserting it. Thus XML “requires physical touch to trigger” is ambiguous, and README implies a
stronger mapping than evidence supports. Unknown/<3 status stays silent rather than PolicyMayRequire.

**Proposed docs now:** cached snapshot, when it refreshes, raw applet version distinction; status bits
are device-reported trigger indicators, **not readable secret/slot mode/short-mode/access code or proven
RequireTouch policy**. Precisely describe existing SmartCard notification decision without blessing it
as correct policy inference. Await hardware matrix before choosing final wording. **Pass2 high/decision:**
fix status interpretation/notification certainty if confirmed; this is module-local, not a Core bug by
assumption. **Reopen:** state refresh/version normalization or status/presence policy changes. Dependents:
ConfigState, IYubiOtpSession.GetConfigState, session presence comments, README, module CLAUDE, shared
user-interaction181–182/migration touch notes and related tests. Hardware plan probe1 is primary.

### OTP-C006 — Dual transport does not establish per-operation parity

**Observed:** factory defaults SmartCard then HID, explicitly no failed-open fallback; SCP without preference
forces SmartCard. Unit selection tests prove selection without initializing real sessions; module backend
supports both command encodings. Protection integration comments assert CCID challenge6985, but give no
device/firmware/USB-vs-NFC scope. Only HID known-answer challenge tests exist; fake SmartCard success isn't
device support evidence. README discovery example leaves session alive and then says later examples need
only device/directives: copying all snippets in one scope retains lease and later opens fail.

**Proposed docs:** explain selecting explicit HID for challenge examples if applicable hardware demonstrates
it; never change default order/fallback during docs pass. Distinguish supported session transport from
device support for chosen command, USBCCID from NFC. Use complete resource scopes so getting-started
session ends before one-shot examples. Direct factory borrows caller connection; convenience owns hidden
one; config remains caller-owned. Factory XML should document inputs/options/unsupported combinations,
not merely “creates.” **Pass2/decision:** actual SmartCard challenge support/capability behavior; no blanket
“works over both” claim from API structure. **Reopen:** transport selection/capability/init ownership
changes and scoped hardware findings. Dependents: README/CLAUDE, session/extension XML, OtpTool DeviceHelper
remarks, tests' CCID comment, migration HMAC recipe, shared architecture selection. Core specifics D001.

### OTP-C007 — Keep HMAC three-layer distinctions; qualify evidence

**Observed:** current XML/README correctly distinguish input,64byte padded payload and slot-mode-hashed
message. Nonempty1–63 data preserve repetitions/trailing00 because marker differs;64bytes unchanged
request but short-mode marker strips data. Empty sends64zero. Unit public SmartCard captures demonstrate
collision equality and63/64 boundaries, not device hashing. Integration8short+4fixed vectors assert exact
host HMAC and minimum-one-byte behavior; not run by investigator. S2 establishes backtracking marker and
S3 exact host padding, neither explicitly establishes minimum retained one byte across firmware. “only
mode ykman programs” is too broad unless qualified to reviewed version/path. Do not call these request
encoding collisions a break of HMAC-SHA1 or promise SDK knows slot mode.

**Proposed docs:** retain useful current explanation, one convention per slot, empty/63/64 examples;
scope vendor and hardware claims. Add response ownership/presence reference rather than duplicating
protocol discussion in all extension docs. C007 is mostly preserve/qualify, **not** an invitation to rewrite
padding. **Pass2/decision:** mode-awareness/length policy if product wants it; no identified new padding
bug. **Reopen:** padding or accepted length/mode contract changes, or hardware disputes minimum-one
behavior. Dependents: Hmac config/UseShortChallenge, IYubiOtpSession, README, CLAUDE, PadHmacChallenge,
AuditRepro/known-answer commentary, migration recipe/map. Hardware evidence must record actual firmware.

### OTP-C008 — Explain Yubico OTP AES response as a ticket, not raw challenge encryption

**Observed:** six-byte exact challenge sent unchanged; config only sets16byte AES key, fixed/UID zero;
returned16byte ticket is not a deterministic six-byte block cipher call. S1§2.5/6.1.1 describes variable
fields, UID XOR challenge, CRC; zero UID makes first6 decrypted bytes equal challenge. Module does no
verification, decryption, replay policy or code formatting. Current API's “16-byte AES-128 response” can
mislead readers expecting response equality for repeated challenges.

**Proposed docs:** separate normal keyboard YubiOtpSlotConfiguration from explicit challenge config;
include a short complete provisioning/calculation example and verifier responsibilities, known-key
ticket/decryption explanation without adding verifier code product surface. Explain nondeterminism and
20byte HMAC versus16byte AES ticket. **Pass2:** real-device KAT missing (not proof method broken).
**Reopen:** UID support, payload format/validation or calculate return behavior changes. Dependents:
both challenge XML surfaces, README mode table/example, migration Yubico-OTP entry, backend/test comments.

### OTP-C009 — NDEF task/limits/null semantics need exact qualifications

**Observed:** session418–509 constructs56byte command `[length,type,54data]`, not full NDEF record;
URI reserves1byte for prefix→**53UTF8 remainder bytes**, longest **ordinal/case-sensitive** matching of
36table entries; text reserves`02en`→**51UTF8 text bytes**, no language/UTF16 option. Null produces56zero;
empty URI produces length1/type55/prefix0; empty text length3/type54/02en. Undefined NdefType takes text
branch with undefined type byte (no enum validation). No URI syntax validation or NFC readback. A3+
firmware gate does not imply NFC hardware or supported slot configuration. Vendor manual says URI prefix
plus generated OTP, not simply stand-alone URI. Null “disables” is a local request intent, not established
by existing write-only hardware test; Python's None means default URI, so cannot prove it.

**Proposed docs:** explicit53/51UTF8 limits, prefix selection, English/header, null versus empty;
configures prefix/content for existing slot, final on-tap record may append OTP. Name no readback and
firmware/hardware applicability; avoid full NFC conformance claim. Keep disable as intended request
with qualification until measured; decide exact final public wording after probe or vendor evidence.
**Pass2:** invalid enum/URI controls/UTF8 fallback normalization policy; arbitrary language/UTF16/NDEF
readback are feature gaps, not automatically defects. **Reopen:** NDEF format/type validation/null contract
changes or actual NFC output evidence. Dependents: IYubiOtpSession.SetNdef, NdefType, README/CLAUDE,
BuildNdef comments/tests, OtpTool Ndef options, migration gaps.

### OTP-C010 — State firmware checks as implementation checks, not complete capability guarantees

**Observed:** features2.2serial/HMAC/AES,2.3update/swap,3NDEF; ConfigState2.1configured/3trigger;
base configs2.0, HOTP2.1, HMAC/AES2.2, update2.3. `IsSupportedBy` accepts alpha/beta sentinels. Flag
requirements are not considered; static-password38byte config inherits2.0 although S1§5.3.3 gives2.1
16characters/2.2up to38. IMF preset comes2.2 while HOTP minimum2.1. NEO version path probes Management
regardless USB/NFC, compares max only when OTP major3, and raw state version remains separate. CLAUDE
“max ... NEO” is not a general max rule for all generations. HID initial status indexes first3bytes
without module validation (Core assumption D004).

**Proposed docs:** requirements table differentiates enforced minimum from device capability; qualify
static-password/IMF/flags instead of promising all selected settings validated. Exact alpha/beta logic
not production certification. Correct stale architecture path for NEO to SmartCardBackend. **Pass2:**
length/flag/feature gating and firmware5-family special cases. **Reopen:** feature gates, NEO normalization,
firmware override or supported config checks change. Dependents: README table, config XML, CLAUDE init,
ConfigState, shared NEO architecture path. No speculative newer-device restrictions invented.

### OTP-C011 — HOTP IMF scaling is inconsistent with vendor protocol

**Observed:** `HotpSlotConfiguration.cs:40–58` validates multiples0x10000 and stores imf/0x10000.
`SlotConfiguration.SetHmacKey` writes ushort big-endian into UID4–5. Local tests424–450 assert that same
mapping, not actual HOTP. S2 encoder and manual, independently S3, say step16, range0..0xFFFF0 and
storeimf/16. Example requested0x30000 encodes0003, device interpretation48 rather than196608. Current
XML's signed-int0x7FFF0000 discussion describes local accepted range but not device counter range.

**Proposed docs:** clearly mark current SDK nonzero-IMF limitation; recommend default0 until corrected;
do not present wrong scaling as intended protocol. Protected SetHmacKey parameter comment also affected.
HOTP generation is device keyboard output and verifier counter sync (RFC4226), not Oath credential store.
**Pass2 high:** correct API/range/scaling and behavior tests; breaking release permits clean contract
choice. **Reopen:** IMF implementation and hardware counter interpretation fixed. Dependents: Hotp XML,
protected helper XML, README HOTP guidance, OtpTool IMF help/comments, related unit tests/migration gaps.

### OTP-C012 — Scan-map API's38byte contract conflicts with45entry vendor map

**Observed:** IYubiOtpSession153/158 and session275 enforce38 (`ScanCodesSize=fixed+uid+key`), while S2
map is45entries and firmware3+. No feature check exists. Static password max38 is a different object.
**Proposed docs:** disclose current38byte API limitation and that it is not demonstrated support for full
vendor map; don't give actionable “custom38byte map” recipe or change constant in Pass1. **Pass2 high:**
confirm/fix command map length/default reset contract and gate, exact wire KAT. **Reopen:** map payload or
gate changes. Dependents: IYubiOtpSession.SetScanMap, README constraints, constants comment where useful,
module guidance. No full map hardware test exists; don't mutate device globally to manufacture evidence.

### OTP-C013 — Write status/CRC/sequence validation claims exceed actual checks

**Observed:** SmartCard write fallback when status<6, but `ValidateProgrammingSequence` returns silently
if fallback still short (backend211–214), so write may succeed/cache malformed state; API says malformed
short status causes BadResponse. GetConfigState then throws ArgumentException. Sequence check skips all
5.0–<5.5, despite comments saying5.0–5.4.3; zero accepted after **any** previous byte, not only rollover.
S3 has stricter conditional5.4.3 boundary and status behavior; vendor legacy rule is not sufficient to
override modern SDK policy. SmartCard SendAndReceive accepts excess data and truncates; HID accepts at
least expected+2, checks CRC on that prefix and returns expected prefix. HID write validates status length
but no backend CRC; don't claim **all responses** get CRC. Internal payload-appending comment says “for
SmartCard” though shared payload feeds HID too.

**Proposed docs:** describe exact interval as5.0≤version<5.5 and status/expected-prefix checks; distinguish
data response CRC from write status, CRC transmission integrity from cryptographic authenticity. Correct
misleading internal/comment exception guarantees without making malformed-status acceptance intended.
**Pass2 high:** malformed write status rejection, sequence exception rationale/range/zero handling,
strict-vs-minimum length choice. **Reopen:** backend validators/transport firmware policy change.
Dependents: backend XML/inline explanations, IYubiOtpSession write exceptions, README/CLAUDE architecture,
SmartCardBackendTests/HidBackendTests. Core framing/program-sequence enforcement D004.

### OTP-C014 — Make test comments assertion-based, remove project-history residue

Complete assertions were read, not inferred from names. Required documentation corrections:

- `SlotConfigurationTests.cs:817–818` says AllowUpdate **not** in mask; it is, and test852–863 asserts it.
- `ConfigStateTests.cs:144–159` narrates high-byte0100, but actual status is low13/high00; checks only
  defined low flags. Don't change input/assertions in Pass1; say what this vector establishes.
- `SmartCardBackendTests.cs:100–109` comment says5→5 while setup is3→5; correct scope and5.x caveat
  interval in comment136. Backend byte-preservation tests don't prove real firmware support.
- `YubiOtpSessionTests.cs:323–336` removed-test history and nonexistent `ProcessHmacKey` reference in
  `SlotConfigurationTests.cs:510`; challenge-length tests do not prove invalid **key** lengths through
  session. Replace with current reason constructors are independently tested; keep legitimate no-I/O
  challenge assertions. `ISC-*`/Phase38 prose is development history, not present contract.
- `HidBackendTests.cs:40–54` says “Actually, let's compute it properly” and narrates overwritten interim
  CRC; concise complement/little-endian rationale, assertions untouched. No claim independent CRC oracle
  when fixture/helper uses same ChecksumUtils as implementation.
- `Base_Dispose_ZerosKeyMaterial` checks post-disposal GetConfig rejection, not actual zeroed fields;
  stronger reflection test in EncapsulationTests70–85 establishes zeros. Preserve names/assertions in
  Pass1, qualify surrounding explanations rather than claim erasure from the weaker test.
- HID integration method named ReturnsExpectedResponse asserts only length20. HOTP8 summary says
  verifies eight-digit option but body asserts configured flag only. Static/Yubico/HOTP programming
  tests don't capture keyboard output. Swap “restore original state” is an attempted second swap, not
  finally-guaranteed restoration; NDEF test is write-only. Hardware plan gives exact actual scopes.
- Protection test CCID6985 and empty-delete-timeout observations need device/transport scope; otherwise
  use local reason explicitHID tests don't establish SmartCard support. Broad rejection exception tests
  don't pin device reason. Lifecycle reflection proves absence of IDisposable, not all runtime teardown.

**Proposed docs:** local concise explanations and truthful suite README/CLAUDE guidance. No new skipped
tests or deleted TODOs just to claim cleanliness. Search found no actual TODO/FIXME/HACK/XXX/NotImplemented
or static skipped placeholders. Existing test-value/coverage gaps remain pass2, not “fixed” by prose.
**Reopen:** assertions, vectors, cleanup bodies or lifecycle tests change. Dependents: listed unit and
integration files, module/test instructions. Candidates C004/C005/C011/C013 map related behavioral gaps;
do not double-count them as independent new bugs.

### OTP-C015 — Align module/test guidance and links with actual suite

**Observed:** module CLAUDE diagram names nonexistent OtpHidBackend instead of HidBackend; claims tests
always clean in finally/use slot2, but suite swaps both and Protect test replaces slot1; currently does not
explain loss of original secrets. Minimal config example connection lifetime implicit. Tests/CLAUDE link
`../../../docs/TESTING.md` correctly resolves from the tests directory to repository docs. README and
module/test instruction link targets all exist (existence only; no reading external-module docs). Related source
architecture NEO path references YubiOtpSession while helper moved to SmartCardBackend.

**Proposed docs:** true ownership/backend names and precise suite effects with link to actual tests,
not “always” policy falsely describing old code. Separate guidelines for future tests from observations.
Keep the valid relative links and scope example prerequisites. **Pass2:** cleanup robustness under failures is not
fixed by guidance. **Reopen:** tests/setup/cleanup/backend shape change. Dependents: four module Markdown,
shared NEO architecture clause. README/PACKAGE task positioning largely sound; preserve useful app split.

### OTP-C016 — OtpTool static password is a real sample behavior defect

**Observed:** `StaticCommand.cs:36–54,73` encodes generated/provided string as UTF8 then passes bytes to
**raw scan-code** constructor; `KeyboardLayout` option parsed but never used. For text'a' byte61 is sent
where public string overload/test expects scan04. Modhex generated characters being layout friendly does
not make ASCII bytes into scan codes. `--length` is generated character count but UTF8 user password length
may differ from scan capacity. **Proposed docs:** prominent current sample limitation, use public
`StaticPasswordSlotConfiguration(string, KeyboardLayout)` in recommended SDK recipe; comment at raw-byte
handoff explains unresolved mismatch without endorsing it. **Pass2 high:** wire proper translation/layout
and test actual command generation. Don't silently fix sample executable behavior during Pass1.
**Reopen:** OtpTool StaticCommand/layout handling corrected. Dependents: OtpTool README section, StaticCommand
comment, CliOptions keyboard-layout description/help interpretation, migration static-password claims
which currently falsely say translator wired only into OtpTool.

### OTP-C017 — OtpTool help/parity/security/update behavior need honest boundaries

**Observed:** “ykman-compatible”/matching format claims overreach: calculate help says stdin fallback but
RequireHex rejects missing challenge; `--totp` just RequireTouch + already-default short flag, not Unix time,
period/truncation/code generation; swap access-code option ignored; all sessions defaultSmartCard and no
transport option, presence prompt or fallback; `settings` newUpdate resets every selected flag-mask field,
so `--no-enter` is effectively alreadyfalse by default and omitted current flags aren't preserved. Current
access-code passed only as authentication, new code omitted, so protected slots may be unprotected by
program/update. `--json` bypasses confirmation even absentForce. Generated keys/private IDs/passwords are
intentionally printed in human mode; JSON generation output omits secret material. Calculate prints
response; ParseHex failure echoes bad input into error, possibly a secret string; parsed CLI string values
cannot be zeroed, and some secret arrays are allocated before access-code parsing/try. Metadata helper
catch-all also catches cancellation; Program Shutdown isn't finally. Those are observations, not proof
every error path leaks device handles through Core.

**Proposed docs:** describe demonstrative CLI, destructive commands and confirmation bypass, generated
secret output/capture limitations, no usable TOTP convenience, required explicit hex challenge, actual
settings replacement/protection effects, default transport and static bug. Retain generated-secret output
as intentional provisioning behavior until product decides; don't claim SDK “never logs” means sample
never displays secrets. XML/comments and README are safe docs-only surfaces. **Do not edit help string
literals/runtime tokens under a strict executable-token-preservation gate without coordinator scope.**
**Pass2:** stdin/transport/TOTP/layout/sample error+cleanup and secret handling, settings-preservation
policy, JSON parity. **Reopen:** parser/commands/security output/session helper changes. Dependents: complete
14file OtpTool sample, README, migration map recipe; no external Cli.Shared investigation.

### OTP-C018 — Reconcile active migration guide/gaps/map

**Observed:** `docs/migration/v1-to-v2-gaps.md:202–233` still claims no string/layout constructor, no AES
challenge, no prefix compression and silent key normalization; current source/map/main migration guide
contradict these. It combines restored AES path and still-missing TOTP generation conveniences; separate
those. Serial-derived helper absent from library but **present in sample** YubiOtpCommand143–155, so
“no ... anywhere” false. NDEF UTF16/language/readback and CSPRNG helpers remain real library gaps.
FIPS/legacy device configuration relocation is inherited **unverified**; no other-module investigation
authorized, keep precise unresolved dependency, not confident restored/missing statement. Migration
HMAC recipe has proper key/challenge finally but response unhandled; resource failure before try and
explicit device/token prerequisites need snippet classification/compilation by editor/coordinator.

**Proposed docs:** update YubiOTP section only to current behavior and limitations C004–C012/C016–17,
retain genuine unresolved relocation questions scoped to missing YubiOTP surface. Map assisted/manual
statuses remain reasonable; HMAC note should link convention/length semantics, no new automatic migration
promise. v1 before-block is explicitly **historical**; don't compile againstv2 or verify by reading legacy
source now. Compare/highlights present restoration claims are locally supported; leave untouched unless
late evidence changes them. **Pass2:** generation/readback gaps, not a license to implement them.
**Reopen:** corresponding features/API contracts change. Dependents: migration guide534–590/718–725,
YubiOTP gaps197–243, map applet-yubiotp/recipe-HMAC/static/AES entries, shared touch notes whenC005 resolves.

## Precise inherited dependencies — Core remains final

These are questions to carry, **not conclusions from reading Core**. Applet-local callers/tests above
are enough to identify the boundary; no dedicated boundary audit was performed.

| ID | Boundary/path to inspect in final Core cycle | Precise question / why it matters | Documentation reopening trigger |
|---|---|---|---|
| OTP-D001 | `src/Core/src/Sessions/ApplicationSession.cs`, Core device session transport helpers reached by `ResolveSessionTransport`/`CreateSessionOverTransportAsync` (resolve exact helper file in Core cycle) | Confirm physical lease identity scope, snapshot timing, initialization-failure claim cleanup, connection disposal order and no fallback. Module extension owns hidden connection only; direct factory borrows. | Any owning/borrowing/transport option or single-connection contract change reopens README/creation XML/OtpTool/migration/architecture. |
| OTP-D002 | `src/Core/src/Protocols/Otp/Hid/` protocol `SendAndReceiveAsync` and `ReadStatusAsync`; exact files later | Which initial-status length invariant is guaranteed? Where are HID write programming-sequence checks and outbound frame CRC handled? Does cancellation after live touch wait attempt/reset/drain, with what reusable-state result? | Reopen HID backend summary, malformed-response exceptions, CLAUDE init/CRC, README cancellation and C013. |
| OTP-D003 | `src/Core/src/Protocols/SmartCard/` / `Sessions/ApplicationSession.cs` / credentials notification handle used by backends | What admitted SmartCard cancellation/drain guarantee and sensitive serialized-copy lifetime actually holds? Does callback failure always preserve primary exception and clear output if resolution throws? Module tests cover selected fake paths, not all Core intermediates. | Reopen README admitted-operation cancellation, session/output ownership XML, shared user interaction and C004/C006. |
| OTP-D004 | Core `FirmwareVersion`, protocol prepare/configure, and OTP HID status contracts used at backend initialization | Alpha/beta sentinel semantics, exact override precedence, HID status normalization/length; can operation effective version disagree with raw ConfigState by design? Avoid attributing guarantees to Core from module assumptions. | Reopen firmware/status/presence wording, C005/C010/C013 and test assumptions. |

Migration FIPS/legacy-config relocation is **not** a Core conclusion: missing from current module public
surface; relocation elsewhere unverified and deferred to whichever future module/boundary owner has scope.
Do not open/read Management source to settle it during this cycle.

## Meaningful clean units and example classification

Coverage records both findings and clean complete units, not only file counts. API clean nominations
include slot enum+mapping, exact key split/validation, HOTP token-ID mode explanation, raw static-password
scan-code distribution/string copy+cleanup, static-ticket fixed behavior, protect bit/mask distinction.
Narrative clean nominations include package app distinction/alpha/install, README transport order/no-fallback
and ownership paragraph, HMAC input/payload distinction (subject toC007 qualification), comparison restoration
and selected map notes. Internal/test nominations include borrowed-backend ownership rationale, constructor
validation no-I/O fake paths, valid CRC response-prefix test, reflection zeroing assertions, session initial
programming-sequence preservation, known-answer vector definitions and raw-field placement assertions.
Final clean nominations are listed with exact ranges in coverage, and overlapping edited regions must leave
the eligible pool. These are investigator nominations, **not independent sample acceptance**.

All meaningful README snippets require review as a **set**: imports/discovery/current-session example;
HMAC provisioning with finally; HMAC response fragment; static-password fragment; destructive swap/delete;
TouchPrompt class and creation fragment. Later snippets are composable alternatives, not an instruction
to keep first session alive. Compile complete wrappers including typedIYubiKey, mutable key/challenge,
tokens and full lifetime scopes; importing System.Threading/Tasks may be necessary for a genuine
consumer project without implicit usings. OtpTool is complete program, validate in its actual references.
Module CLAUDE snippets are connection/session/config fragments with explicit typed prerequisites. Migration
v2 HMAC block is application-dependent recipe with existing device/key/challenge/token, v1 block historical.
Shared usage interaction implicit `client` blocks belong to the already generic interaction explanation;
none is a hidden YubiOTP executable example. Do not compile a rewritten summary instead of original bodies.

## Handoff priorities and limits

1. Coordinator adjudicate high-impactC004/C005/C011/C012/C013 and hardware plan; current modern device can
   settle status interpretation/SmartCard command behavior/HMAC, not old firmware restrictions.
2. Fresh editor improve practical tasks and XML together, with truthful limitations and preserved rationale;
   no executable/sample fixes, assertion changes, snapshot edits or lifecycle-policy redesign in Pass1.
3. Independent reviewer checks original clauses and local source, not only this report. Sampling spans
   clean API, narrative/examples and internal/test units. Hash-bind edited/reviewed content after changes.
4. Coordinator compilation/gates/hardware results may reopen candidates. Save failed probes and cleanup
   state; successful docs review is not implementation/hardware qualification. Pass2 waits campaign sweep.

No claim of conformance, all-firmware/OS support, successful suite execution, completed documentation edits
or fixed implementation is made. Original63paths/193entries are reconciled; source lookup bounds honored;
actual hardware behavior, accepted dispositions and final report manifest remain coordinator-owned.
