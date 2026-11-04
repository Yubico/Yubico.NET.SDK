# Independent YubiOTP content and original-citation review

2026-11-04 · fresh `github-copilot/gpt-6.1-sol` reviewer · baseline
`95e5ccaeda33954c21f2a38058d4f66b5f208787` · current working-tree content, not a new commit.

## Verdict

**Required corrections before content approval: OTP-REV-001–003 below.** The other 47 authored paths
are accepted for the reviewed content scope at the exact hashes in
[`evidence/review-content-manifest.json`](evidence/review-content-manifest.json). All 50 current hashes
and byte lengths match the editor's handoff. The three affected paths are not file-level accepted until
corrected and re-reviewed. This is not a request for runtime changes or another general rewrite.

The batch substantially improves practical task flow, ownership, access-code retention, update masks,
NDEF limits, and assertion-based test explanations. It preserves the useful HMAC input / transmitted
payload / device-hashed-message distinction and openly discloses CLI defects without changing runtime
help strings. The 50 authored paths contain **42 C# comment-only paths, three module Markdown paths,
and five assigned shared-document paths**. No C# imports, runtime expressions, help strings, assertions
or API declarations changed in the reviewed diff. This is a content/diff inspection, not the coordinator's
Roslyn token/directive/API-snapshot gate.

**Hardware-dependent acceptance remains conditional.** I have not consumed or presumed results from the
retry that became active after the reader was released. The initial blocked integration attempt is not
device-operation evidence. The coordinator's later hardware review must reopen the dependencies listed
below; this report cannot stand in for that review or final validation.

## Required factual and usefulness corrections

### OTP-REV-001 — Attribute the serial firmware caveats to the source that actually states them

- **Path:** `src/YubiOtp/src/ExtendedFlag.cs:21–24`.
- **Current claim:** “Manual 3.3 documents button visibility for 2.x and USB serial unavailability on
  3.0/3.1.”
- **Independent original check:** Manual 3.3 §§5.6.1–5.6.3 describes these serial flags as introduced
  from 2.2, but does not state either of those exclusions. The freshly retrieved **personalization
  v1.20.0 manual**, `serial-btn-visible` and `serial-usb-visible` (lines 292–300), states both explicitly.
  The manual's device-wide merged serial-flag statement in §5.6 remains correctly attributed.
- **Required correction:** Keep the useful caveats and legacy/newer-device qualification, but attribute
  these two restrictions to the tagged personalization manual, not Manual 3.3. No new research or behavior
  change is needed. The investigator's `evidence/sources.md:23–24` repeats the same source misattribution;
  its owner should correct that record too, preserving the original capture.
- **Why required:** This is a demonstrably incorrect citation, not a preference about wording. An
  independent reader following the stated manual cannot find the cited exclusions.

### OTP-REV-002 — Do not assign other sample commands' pre-try cleanup gap to YubiOtpCommand

- **Path:** `src/YubiOtp/examples/OtpTool/Commands/YubiOtpCommand.cs:28–29`.
- **Current claim:** Arrays allocated before later parsing failures “are not all protected by the
  command's finally cleanup.”
- **Actual complete method:** `accessCode` is parsed immediately before `try`. Private-ID/AES parsing
  and generation, output, session creation, configuration construction and write are **inside** that
  try. Its finally clears `aesKey`, `privateId` and `accessCode`. For example, an invalid later AES-key
  parse still clears an already generated private-ID array. There is no intervening later parse outside
  the try that strands this command's successfully acquired secret arrays.
- **Required correction:** Remove or replace this command-specific allocation claim. Keep the true
  immutable-string/parser-diagnostic and generated-secret-output limitations. If explaining pre-try
  stranded arrays, place it on the actual `ChalRespCommand`/`HotpCommand`/`StaticCommand` paths, where
  key/scan-code acquisition precedes access-code parsing and the try. No executable change is requested.
- **Why required:** The new local comment incorrectly describes this method's exception/cleanup flow.
  The global sample security finding remains valid; it must not be copied indiscriminately to a safer path.

### OTP-REV-003 — Make generated HMAC key enrollment an explicit step before erasure

- **Path:** `src/YubiOtp/README.md:138–158`, HMAC provisioning task.
- **Current task:** Generates a fresh 20-byte key, writes slot 2, then clears the only shown caller copy.
  It never tells the reader to enroll that generated key with the verifier while it is available. The next
  calculation task correctly requires the verifier's key, but that cannot recover the just-erased random key.
- **Required correction:** State that this is application-dependent provisioning and securely enroll the
  same generated key with the verifier before clearing it (or use an already enrolled, caller-owned exact
  20-byte key). A brief prerequisite plus an enrollment placeholder inside the existing try is sufficient,
  as the AES recipe already does. Preserve the finally, using scopes, explicit HID and touch requirement.
- **Why required:** This is the main HMAC setup recipe, not an isolated encoder demonstration. The reader
  needs an actionable path to a usable verifier/device pair, rather than a slot whose random key was never
  shared. Do not solve this by printing the key or adding an SDK verifier/runtime feature.

## Optional preferences — not approval blockers

1. `IYubiKeyExtensions.CalculateHmacSha1Async` could make the interface reference an actual XML `see`
   link rather than plain text. The referenced contract exists; this is navigation polish only.
2. `SwapCommand`'s “original secrets are not recoverable” could say “cannot be read back.” Swapping
   changes placement rather than erasing the secrets, and the guide already explains attempted reverse
   swap and destructive combined swap/delete effects. This is optional precision, not another runtime issue.
3. Keep the current task-first paragraphs. Some firmware/transport cautions appear both locally and in
   Constraints, but removing those qualifications would be worse than the modest repetition. Do not turn
   this into another broad compression or forensic-detail expansion pass. Source evidence and pass-2 IDs
   belong in these run records, not in every public member's prose.

## Review coverage and semantic checks

Read root/module/test instructions, `docs/TESTING.md`, campaign workflow/approved style, and the run's
plan, findings, complete investigation, coverage manifest, evidence sources/capture manifests and complete
editor report. Compared the exact 50 authored paths against the stated baseline; read all affected module
files in full, including implementations behind public comments, complete example methods, helpers,
configuration constructors/encoders and test setup/assertions/cleanup. Reviewed all meaningful README and
module-instruction example bodies and the complete migration before/after block. Shared review stays in
the five authorized YubiOTP sections; surrounding prose was context, not a review of other applets.

Additional unchanged module semantic dependencies read include `Slot`, `ConfigSlot`, static ticket,
backend contract, sample delete/info/program entry point and the actual reflection zeroing tests. I did
not read Core or another module's source/docs to strengthen inherited guarantees. Link targets outside
the module are existence/navigation references only; this is not dedicated boundary review. No clean-unit
sampling completion is claimed on the coordinator's behalf.

| Area | Independent result / boundaries |
|---|---|
| Public interface and concrete session | Ten implemented operation families now use `inheritdoc`; factory docs separately state borrowed connection and owning extension distinction. Full interface remarks were checked with actual implementation, not just concrete additions. |
| Replacement/update/protection | New versus current code, omitted new zeros, default flags and whole masked values match `GetConfig`, `WriteConfigAsync` and `UpdateConfiguration`. Copy-before-invalid-code/try gaps are real. Flag protection is distinct from access-code authentication. |
| Config ownership and strings | Constructors copy fixed/UID/key/scan-code data; disposal clears private arrays, not source arrays or independent GetConfig results. String intermediate cleanup is real, while immutable source erasure is not. Calculation outputs are returned backend views; success transfers documented ownership. No claim of all-heap-copy erasure. |
| Async input and results | HMAC makes/clears its 64-byte padded copy; AES borrows exact six-byte input. README and migration clear owned source/output in finally and scope session/configuration lifetimes, including session-creation failure. The generated HMAC enrollment omission is REV-003, not a cleanup defect. |
| HMAC host/device boundary | SDK padding follows tagged manager 5.8.0. Short and explicit padded inputs collide at request encoding; no assertion that the session knows stored mode. Minimum-one behavior remains qualified pending target-device evidence; wire-only tests cannot establish it. |
| AES ticket | Exact key/challenge/output sizes, zero private ID and unchanged challenge request agree source. Legacy manual supports UID XOR challenge, variable fields and verifier CRC rejection. Documentation does not pretend the module decrypts, authenticates tickets or enforces replay policy. |
| Firmware/status | README minimum table is explicitly SDK-enforced, not all-device support. Raw status and effective/override firmware are distinct. Management probe and major-3 max logic match module code. Static-password length, IMF presets and per-flag capability checks remain qualified. No new universal 5.x restriction was inferred. |
| NDEF | URI 53 UTF-8 remainder bytes, text 51 bytes plus `02en`, longest ordinal matching, null zeros versus empty header, no URI/enum validation and no NFC read-back all match source. A 56-byte programming block is not the final NDEF record; old NEO concatenation is explicitly legacy behavior. |
| Backends | SmartCard's exact 5.0–<5.5 bypass, any-zero and short-fallback escape match code. HID CRC covers expected data plus two bytes; status writes only have the local minimum-length check. Neither CRC nor successful return is elevated to exhaustive validation/authenticity. |
| Transport/presence | Local selection order/no fallback and explicit HID recipes are preserved. Cached keyboard-trigger use is described as the current SmartCard PolicyRequires heuristic, not RequireTouch read-back. Core cancellation/drain/lease details remain inherited deferred dependencies, not newly verified guarantees. |
| Sample | Full parser/command paths confirm static UTF-8/raw-scan-code error, unused layout, required hex/no stdin, non-TOTP `--totp`, full settings masks/new-code omission, swap ignored code, JSON confirmation bypass and output differences. Help string defects intentionally remain unchanged. REV-002 corrects one overly broad local cleanup comment. |
| Shared docs | YubiOTP migration recipe, restored-versus-missing features, map notes, interaction exception and NEO helper path agree module source. V1 before block remains historical. FIPS/legacy-command relocation is explicitly unresolved; no external-module claim was independently endorsed. |

### Test-comment verification

- Slot configuration tests pin raw offsets/key preservation, current incorrect IMF encoding and actual
  masks, not physical firmware behavior. The revised IMF `0003`/counter-48 explanation is correct.
- The low-byte `0x13`, high-byte-zero ConfigState vector does not establish nonzero high-byte parsing.
  The revised comment now says that, without changing its misleading historical method name or assertions.
- SmartCard sequence fixture is initial 3 → returned 5; fake 5.2 is one example within the implementation
  bypass, not a proof of every device firmware in that interval. Fake challenge APDUs are not hardware support.
- HID CRC fixture shares `ChecksumUtils`, not an independent CRC oracle. Backend tests genuinely assert
  selected callback failure/zeroing and timeout outcomes; reflection lifecycle checks only advertised shape.
- `Base_Dispose_ZerosKeyMaterial` checks rejection, not erasure of private arrays or its prior result.
  `SlotConfigurationEncapsulationTests` actually retains those arrays and asserts their bytes are zero.
- The HID integration method with “KnownKey/ExpectedResponse” in its name asserts only 20-byte length.
  The known-answer suite instead compares exact host HMAC for eight short-mode and four fixed-mode vectors
  when it executes. None was executed by this reviewer.
- Static/Yubico/HOTP programming tests check configured/cleared status, not keyboard output or digit count.
  NDEF is write-only. Swap's second call is not finally-protected and verifies no contents. Protection suite
  replaces both slots across the suite, attempts finally deletion, and its broad exception does not pin
  the rejected device reason or every protection ordering. Original secrets are not restored.

## Fresh original-citation verification

Initial bounded retrieval independently obtained ten relevant originals/publisher landing pages; all
matched pinned bytes exactly. The attempted Manual 3.4 original still returned 404. No additional research
round was needed and no binary duplicates were retained. This is **fresh retrieval followed by independent
passage reading**, not acceptance of an inherited excerpt as independent evidence.

[`review-retrieval.json`](evidence/review-retrieval.json) retains URLs, status/failure, byte counts and
SHA-256. [`review-original-clauses.md`](evidence/review-original-clauses.md) retains focused passages,
fresh PDF extraction hashes/diagnostics and the fresh Context7 documentation check. The two NFC extraction
warnings were retained verbatim; all three PDF extractions exited zero and match pinned extracted text.

| Source / status / actors | Consequential independently checked clauses and disposition |
|---|---|
| *The YubiKey Manual*, version 3.3, 17 September 2014; published legacy vendor manual, not a universal modern-device specification | §§5.5.3/.4: 10/20/combined30 ms; §5.6.5: fast trigger only with one configuration; §5.6: serial flags merged device-wide; §5.6.6: masks include AllowUpdate and dormant activation restriction; §5.7: provisioning access code for writes; §5.5.12: ordered protection semantics. Serial generation exclusions belong to personalization instead (REV-001). |
| Same manual; device and verifier, distinct actors | §2.5 variable Yubico challenge response; §6.1.1 private ID XOR challenge; §6.1.6 verifier rejects bad CRC; §7.2 existing OTP configuration and legacy URI+OTP NDEF construction. It does not settle minimum retained HMAC byte or SDK null-disable on-tap effect. |
| Personalization v1.20.0 tagged vendor manual/header/encoder; implementation evidence, not normative standard | `ykp_set_oath_imf` rejects >65535×16/nonmultiples16, writes IMF/16 big-endian to uid[4..5], getter shifts back four bits. Manual agrees range 0..1048560 step16. Scan-map manual requires 45 unique bytes for YubiKey3+, header mapping agrees; SDK's38 is a different field capacity. `hmac-lt64` describes final-byte backtracking but no minimum retained length. Header masks and protection bit corroborate public qualifications. |
| Manager 5.8.0 tagged vendor source; host implementation, not firmware specification | Exact `ljust` zero/one padding; HMAC builder initially enables LT64 (also has an lt64 toggle); IMF step16; `is_touch_triggered` explicitly “not challenge-response”; access-code update restriction 4.3.2–<4.3.6. None is substituted for actual SDK behavior or target-device execution. |
| NFC Forum URI RTD 1.0, 24 July 2006; original document obtained via identified CCC mirror | Original-publisher landing page is a purchase page, not publisher-hosted full clauses. URI appendix B explicitly says **Final**. §3.2.1 type U55 and identifier+UTF8; §3.2.2 Table3 mandates coding table, permits00, forbids producer RFU. Longest match is SDK choice, not NFC MUST. §3.2.3 byte-length/control/UTF8 reader rules do not become proof of full SDK URI validation. |
| NFC Forum Text RTD 1.0, 24 July 2006; same mirror provenance | Appendix B **status cell is blank**, unlike URI; do not promote it to Final. §3.2.1 type T54, status encoding/zero RFU/language length, §3.3 nonomitted language, and CRLF producer rule. `02en` is the SDK's English UTF8 choice, not a standard requirement for English. No full NDEF/device conformance claim is made. |
| RFC4226, December 2005, Informational; generator and verifier | Fresh RFC Editor bytes match retained original. Reference is relevant to HOTP/counter synchronization; it does not define Yubico IMF wire scaling, exact SDK key acceptance or slot/NDEF behavior. No contrary change required. |
| .NET current docs via fresh Context7 `/dotnet/docs` | String immutability and owner/consumer lifetime documentation support source-string and memory-view distinctions. SDK copying/output ownership conclusions come from local implementation, not generic documentation. No framework experiment was run. |

## Late hardware and integration reopening packet

Keep device identity, firmware, OS and actual USB-HID/USB-CCID/NFC transport attached to every result.
Distinguish setup-blocked, skipped and executed operations; do not turn a green configured-bit or length-only
test into stronger device proof. Current content does not claim a completed device run.

1. **Trigger status / SmartCard prompt heuristic:** reopen ConfigState/session comments, both module guides,
   shared interaction and migration claims after the keyboard/non-touch-HMAC/touch-HMAC status matrix.
   The tagged manager distinction already justifies not calling the bit RequireTouch read-back.
2. **HMAC marker:** reopen all README/CLAUDE/interface/config/known-answer wording if executed empty,
   all-equal and 64-distinct vectors disagree. Eight short plus four fixed expected messages are a test
   plan until those calculations actually execute; one firmware's success is not universal firmware proof.
3. **Transport support / AES:** explicit HID recipes do not establish CCID/NFC parity. Scope any challenge
   rejection code. AES ticket decryption/challenge/CRC/repetition probes can strengthen only the recorded
   device/transport scope. Do not infer AES verification from HMAC success.
4. **NDEF:** successful configure/zero write does not establish on-tap disable or final output. No NFC
   reader/read-back remains an explicit evidence gap, not failure of the local payload construction proof.
5. **Cleanup:** retain actual final slot state and failure paths. A second swap or finally deletion attempt
   is not verified restoration of unreadable original secrets. Hardware does not close IMF/scan-map/runtime
   enforcement findings merely because unrelated programming tests passed.

## Acceptance mechanics and limits

- The exact accepted 47 hashes and three pending hashes are in `review-content-manifest.json`, together
  with each baseline hash and per-path verdict. Shared-document hashes bind whole bytes for change detection,
  while acceptance applies **only to the authorized YubiOTP sections**. No other-module prose is approved.
- Editor/coordinator should correct REV-001–003, record new hashes and request focused re-review. Any other
  byte changes need explicit reconciliation; later hardware corrections reopen the claim dependency set,
  not automatically every already accepted style decision.
- No consumer-content edit, build, test, formatter, hardware operation, agent dispatch or commit was made.
  Only `review.md` and `evidence/review-*` were authored/generated by this reviewer. Prior user work and
  original evidence were not altered.
- Example compilation/analyzers, executable-token/directive preservation, package/docs gates, link QA,
  coordinator clean sampling, late hardware review and final evidence closeout remain separate gates.
  Content acceptance is not a claim that runtime defects were fixed, all platforms/firmwares work, or
  full specification conformance has been established.
