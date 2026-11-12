# YubiOTP call flow, resources and verification handoff

## Factory and initialization

| Entry or phase | Actual actor and call flow | Failure and ownership boundary |
| --- | --- | --- |
| Device convenience factory | YubiOtp extension → ResolveSessionTransport → CreateSessionOverTransportAsync → typed ConnectAsync → direct CreateAsync → OwnConnection | SmartCard then HidOtp among exposed interfaces; explicit single preference wins. SCP without preference forces SmartCard. One selected transport; open/init failure propagates without fallback. |
| Direct factory | ConstructInitializedAsync validates preferred-type assertion; constructor validates supported interface; Construct binds connection after construction and before I/O | Caller owns connection on success and failure. Rejected construction leaves no attachment; initialization failure disposes protocol/session and detaches. |
| Prepare | ProtocolFactory chooses PcscProtocol or OtpHidProtocol from actual connection interface; ApplicationSession stores protocol before SCP validation | SCP on OTP HID fails before status I/O. Failed preparation leaves protocol available to session cleanup. |
| HID discover | ReadStatusAsync under guard → first GET for protocol version → optional raw-major-three invalid-map refresh → second GET for status slice | Typed native routes enforce eight bytes; arbitrary custom IOtpHidConnection still reaches unchecked index/slice. First version and second status may differ. NEO catch can hide a recovered non-rejection failure. |
| SmartCard discover | SELECT Management and parse optional version → SELECT OTP and require six bytes | Probe runs over USB and NFC. Non-cancellation Management failure falls through locally; required OTP failure propagates. No transport retry. |
| Configure | Override or detected effective version → protocol Configure → optional SCP initialize → effective protocol publication | HID Configure encounters already initialized protocol on the public applet path. SCP authentication is protocol authentication, separate from slot-write access codes and device command support. |
| Backend and readiness | Create SmartCard backend with effective protocol/version and SELECT prog_seq, or HID backend with protocol → callback returns → Core publishes IsInitialized | Applet backend is constructed once after configuration; no placeholder sequence. Logger/callback failure still fails creation. |
| Hidden creation cleanup | Core helper awaits connection DisposeAsync if direct factory fails | Native owner/wrapper determines release evidence. Primary creation error survives ordinary cleanup/logging failure; a faulted cleanup task alone proves no native release. |

## OS and access-tier ownership

| Route | Open/report execution | Native and registration owner | Release/cancel limit |
| --- | --- | --- | --- |
| Built-in typed Linux OTP | HidConnectionSlot directly opens LinuxOtpHidConnection; one context-suppressed worker performs hidraw open, GET/SET and close | LinuxOtpHidNativeOwner roots before acquisition and accepts original known-member/discovery token before open; no reactor | Before dispatch cancellation skips work; after dispatch actual task waits native return. Checked release precedes claim disposal; Linux EINTR/EIO can report diagnostics after proven release, unknown release retains/quarantines. |
| Built-in typed macOS OTP | Slot directly opens MacOSOtpHidConnection; separate OTP worker performs IOHIDDevice open/features/close | Its Owner roots itself and retains original token/device creation reference; no FIDO input backend | Feature dispatch cannot be detached by token. Checked CloseDevice then CFRelease precede token release; uncertainty retains root/token. Open option is non-seizing in the inspected lifetime seam. |
| Pinned Windows typed OTP | Synchronous IHidConnection feature facade → OtpHidConnection compatibility adapter → registered wrapper | WindowsHidReportAccess owns SafeFileHandle; wrapper owns process token | Calls may block before returning Task; adapter ignores token. Wrapper release follows managed Dispose, with B1 C005/B4 C001's deferred-native-borrow/custom-failure bounds. This pin excludes the user's later concurrency work. |
| Custom typed OTP | Caller-supplied IOtpHidConnection or custom interface adapter | Provider's contract supplies native/buffer ownership; Core session only borrows it | Do not transplant built-in native release or exclusive returned-array guarantees to arbitrary providers. |
| Direct expert reports | IHidInterface.ConnectToFeatureReports; Linux/macOS sync facades use separate owner instances of their platform owner types; Windows direct sync facade | Caller owns facade; direct construction has no grouped-key registration | Linux direct SET keeps supported variable length; GET is OTP eight bytes. macOS expert GET permits short successful length and returns zero-padded eight-byte array; typed GET requires exact eight. All direct calls remain synchronous. |
| RawOtpHidSession | Borrows typed connection, constructs protocol without wire I/O; lazy first exchange initializes | Session owns protocol; device convenience raw factory may own hidden connection | Supplies framing/guard/outbound CRC, leaves inbound command CRC/semantics to caller. It is not a typed YubiOTP configuration API. |
| Built-in SmartCard, each supported OS | Typed slot transfers claim to PC/SC rooted worker before native acquisition; APDU pipeline uses actual connection | PcscConnectionNativeState owns card/context and checked shutdown; session owns protocol/SCP graph | Admitted transmit is uncancellable. Native disconnect and context-release outcomes govern claim release. B1/B4 shim/ABI/encoding conditions remain separate from applet success. |

Known-member process claims are narrower than physical/global exclusivity. A's grouping and
disjoint-view qualifications remain active. No direct report open, cached serial, generic Feature,
algorithm catalog lookup or FIPS metadata field grants applet capability or physical-state proof.

## Exchange framing and operation flow

| Operation | Request / response handling | Guard, cache and semantic responsibility |
| --- | --- | --- |
| HID request | Applet data copied/padded to 64; frame is payload 64 + command 1 + uncomplemented CRC16 2 little-endian + filler 3. Ten reports carry seven bytes plus 0x80 OR sequence 0–9 | Guard covers initial/status reads, all writes, polls, response assembly and reset. Core clears payload/frame/output staging. Sending all-zero middle packets is a permitted implementation choice, unlike Python's optimization. |
| HID status-only write | Pre-write prog_seq read; ready flags polled; no-data completion requires increment or reset-to-zero with prior nonzero and both configured bits clear | Core owns completion rule; HidBackend only checks returned six-byte minimum; session publishes status after backend success. This is separate from SmartCard's broader zero/bypass policy. |
| HID data | Sequence 0 begins data; ordered seven-byte chunks accumulate; repeated zero after data terminates; dummy 0x8F reset completes transfer | Maximum sequence space bounds accumulation to 224 bytes. Applet requires serial 4/HMAC 20/AES 16 plus two CRC bytes and validates only that expected prefix; it returns expected data view. CRC is transmission detection, not authenticity. |
| SmartCard write | INS 01, command in P1, data payload; response Data used as status. If short, INS 03 status read is a second exchange | Applet checks effective 5.0.0 ≤ version < 5.5.0 bypass, accepts any zero, and silently returns on still-short status. These retained OTP-IMP-005 limitations differ from Python. Sequence cache and session status mutate after guard exit. |
| SmartCard data | INS 01/P1 serial/HMAC/AES; Core validates APDU status, applet accepts minimum length and returns prefix | APDU formatting/chaining/SCP/response raw copies remain B6. Extra response data acceptance is a maintainer decision, not inferred ISO conformance. |
| HMAC | Public API permits 0–64 bytes; owns 64-byte padded copy, nonempty trailing zero selects pad one, other/empty selects zero | Finally clears padded copy. Slot mode is unknown to session. Existing short/fixed/marker qualifications remain OTP-IMP-012; historical 12 vectors cover only recorded fixture. |
| AES ticket | Public API borrows exactly six bytes; HID Core pads transport frame, SmartCard sends six | Backend returns 16 bytes. Application verifies decrypted challenge, ticket CRC and replay policy; Core catalog structural AES applicability establishes no device support. |
| Configure/update | Config copies keys, session GetConfig assembles 52 bytes and appends current six-byte code when supplied | Applet owns wire fields/defaults/full masks; caller owns source/config/GetConfig results. Session owns normal temporary cleanup, with pre-try gaps retained. No whole configuration lifetime lease exists. |
| Scan map/NDEF | Current scan-map method enforces 38; NDEF forms 56-byte programming block, optional current code, URI/text limits | Applet-owned 38/45 defect and NDEF validation/null/empty/readback limitations remain. Core frames bytes; no capability or NFC output guarantee follows. |

## Cancellation, failure and prompt outcome

| Boundary/event | Actual behavior | Required recovery or evidence |
| --- | --- | --- |
| Pre-entry cancellation | Guard rejects before exchange admission; SmartCard prompt can run before that admission | No admitted APDU/frame from that guard. A request callback can cancel before I/O, as the fake session test does. |
| Admitted SmartCard | Constituent APDUs use None; post-admission caller cancellation can still yield success/Completed | Await original task and public method. Cancellation cannot roll back a write; fallback status can fail entry if token canceled between exchanges. |
| HID before touch wait | Admitted writes/readiness/data assembly use exchange token None | Caller token has no generic interrupt at every await. Local poll thresholds are checked between calls, not native deadlines. |
| HID observed touch | Core requests the shared notification with DeviceWaiting, then checks caller cancellation in 250 ms touch loop | On attempted frame abandonment, one dummy reset is awaited before failure exits; ordinary Cancelled resolution follows in backend. A canceled outer WaitAsync is a different task. |
| Touch timeout | OtpHidTouchTimeoutException from the observed touch loop → backend TimedOut | Readiness/native timeout → Failed. A timeout alone supplies no close/release or rollback evidence. |
| First attempted write fails | wireAttempted is set before send; Core tries one abort even if send itself throws | A successful abort permits next exchange for that protocol; managed fake-native recovery does not prove physical device state. |
| Reset fails | Completion reset or abort records _resetFailure; later guarded exchanges refuse with dispose/reopen guidance | Dispose old session and connection, await actual teardown, then reopen. A fresh borrowed session alone is not recovery. |
| HID valid data | Backend length and CRC validation precede Completed resolution and response transfer | Core requests only. CRC failure resolves Failed and clears backend-owned assembled view under ordinary cleanup. |
| SmartCard prompt | Known raw keyboard-trigger bit → PolicyRequires before APDU; unknown/non-triggered state → None | Current heuristic is OTP-IMP-004. Silent prompt is no proof that RequireTouch is absent. |
| Request callback throws | Handle terminalizes without resolution. SmartCard sends no operation APDU; HID already attempted frame and aborts | Primary request failure propagates; no duplicate notification owner. |
| Terminal callback throws | On otherwise successful backend work, callback failure propagates and response remains backend-owned for clearing; on primary failure, callback error is logged | Throwing provider logging is a separate conditional cleanup concern (X003). Application owns prompt service and must keep it alive. |
| Concurrent disposal | Session closes admission, drains protocol exchange, detaches and disposes hidden connection | It can overtake post-exchange CRC/parsing/status publication or a second applet exchange. Await complete public tasks before disposal. |
| Reentrant prompt disposal | HID request callback runs within active exchange; awaiting disposal there can await itself | B5 C006 policy remains unresolved. Schedule external lifecycle cancellation rather than disposing from the operation being drained. |

## Version provenance and support

| Value | Source and consumer | Limit |
| --- | --- | --- |
| HID protocol FirmwareVersion | First initialization feature bytes 1–3; selects NEO refresh | Not overwritten by session override or second status. |
| Cached HID ConfigState firmware | Second feature report slice 1–6, parsed at bytes 0–2 | Can disagree with protocol/effective version; no metadata update. |
| SmartCard detected version | Optional parsed Management SELECT, required OTP SELECT; greater when raw OTP major is three | Applies on USB and NFC; fallback version is OTP status. It is different from Python's NFC-specific probe and condition. |
| Effective session FirmwareVersion | Override if supplied, else detected; Core Configure/SCP then backend construction | Drives operation/type gates and SmartCard sequence bypass. Override does not skip probes or rewrite raw status. |
| ConfigState gates/prompt heuristic | Raw OTP version, configured/trigger/LED bits | Cached output is no slot-mode, access-code or RequireTouch read-back. |
| Core major-zero policy | Feature and FirmwareVersion admit development/unknown sentinels optimistically | Generic policy cannot establish real command/interface support, physical generation or FIPS state. |
| Historical fixture | Serial 103, metadata 5.8.0.beta.0, raw/effective OTP 0.0.1, Linux USB HID/CCID | Retained prior-run evidence only. CCID HMAC 6985 was classified rejection; HID HMAC/AES/touch/cancel/timeout were scoped successes. |

## Buffer resource scopes

| Storage | Owner and clearing | Independent residual scope |
| --- | --- | --- |
| Caller key/code/challenge/raw scan-code arrays | Caller retains source; config copies, HMAC pads, AES borrows until actual operation terminal | Clearing config/padding never clears source; outer canceled wait never ends native borrow. |
| String password/options/generated string | Caller/CLI immutable storage | Config clears translated intermediate, not source/string builder/history/output. Core reader's redirected string is a separate route. |
| Configuration fixed/UID/key arrays | Disposable configuration, verified by actual reflection observers in test source | Every GetConfig result is independent caller-owned storage; guard disposal is not config disposal. |
| ConfigBytes/data append | Session normal finally clears both | Bad new/current code paths allocate before validation/try. NDEF URI/text is semantically separate nonsecret content unless an application places secrets there. |
| HID output payload/frame/report | Core finally clears; native owner/compatibility adapter clears their owned SET copies | Windows nested report-ID buffer remains separate; caller sources stay borrowed. |
| HID GET report arrays | Built-in owner transfers to Core consumer; protocol currently does not clear them | Accumulator/result/caller prefix clearing leaves incoming reports and Windows native-ID buffer distinct. |
| Pooled HID accumulator | Core clears full rented array then returns | Returned assembled array is independent, later applet prefix is a view. Extra bytes/CRC remain outside the returned prefix on success. |
| APDU formatted input | Core transmitter finally clears after actual connection task | Source/config input lifetime remains caller/session-owned; SCP intermediates retain B6 ownership. |
| APDU raw transport response / cloned owner | Native facade returns independent raw bytes; ApduResponse clones; applet returns Data prefix | Prefix zeroing leaves extra bytes/SW and original raw transport array; B6 owns shared cleanup design. |
| Backend successful result | Backend resolves before transfer; caller clears returned sensitive view | One-shot helper has another producer boundary: teardown failure before return can strand it (X004). |

## Actual test oracles and meaningful unexecuted proposals

All statements here describe source assertions; this packet ran zero SDK tests or crypto.
`semantic-notes.json` describes every test file and helper, including weaker/no-I/O
and actual-array observers. Historical execution is linked separately in the source register.

| Proposal | Deterministic setup and observations | Scope / next owner |
| --- | --- | --- |
| V01: custom report admission/provenance | Public direct factory over scripted IOtpHidConnection; vary first and second report lengths 0–9; valid control uses eight. Supply first version 3/5 and differing second version. Capture attempted writes, exception type, cached/effective version and no hidden Configure GET | Managed, no native/device. B5/app boundary test owner. Assert policy only after maintainers decide malformed-provider behavior. |
| V02: NEO classification | Script invalid-map rejection, timeout/transport failure with successful abort, and failed abort; follow with second status or next exchange | Observe actual swallowed/propagated outcomes and reset count/latch. Keep Classify failure separate from earliest NEO hardware support. |
| V03: all response owners | Fake native GET captures actual output array references; inject synthetic HMAC/AES bytes and literal independent CRC; retain native-ID/raw APDU/result owners separately | Check which exact storage changes after success, CRC failure, prompt failure and disposal. Do not clear a second copy and call it proof. Coordinate B4/B5/B6 shared mechanisms. |
| V04: one-shot teardown | Fake IYubiKey returns typed fake; supply valid framed result, hold/fault DisposeAsync after calculation. Retain actual result backing reference | Assert inner success, outer pending/failure, one cleanup attempt and desired failure erasure after selected fix. No close/device claims. |
| V05: workflow drain | Hold SmartCard write response, return short status, pause before second exchange or terminal callback; begin disposal | Distinguish exchange drain, second-call refusal, status publication and full public task. Keep caller config alive; every gate releases in finally and actual tasks drain before primitive disposal. |
| V06: configuration/parser exception cleanup | Observe actual config private arrays and serialized buffers for bad new/current-code lengths; unified CLI static config created then invalid code parse | Require zero/no abandoned owners after selected fix plus no-I/O and normal-success controls. Existing validation-only rejection is insufficient. CLI owner receives narrow referral. |
| V07: sequence policy | SmartCard effective 4.x/5.0/5.4.3/5.5/override, previous 0/1/255, new unchanged/increment/zero, short initial/fallback status; HID deletion mask vectors separate | Exact current versus desired behavior, no generic hardware acceptance. Maintain OTP-IMP-005 and B5 polling group, avoid rewriting assertions during docs phase. |
| V08: meaningful consumer compilation | Compile existing README direct/hidden-session/key/result/finally examples in coordinator's existing host with actual current references/analyzers | UNCOMPILED here. Reuse prepared validation foundation; no new generic harness. Include creation/teardown failure paths in source review. |
| V09: future physical matrix | Separately approved dedicated fixture, concrete OS/interface/firmware and known empty slots; HMAC short/fixed boundaries, AES verification, touch/cancel/timeout, exact output capture and NFC readback as separate lanes | Unexecuted proposals only. Preserve historical fixture, older firmware/NFC/keyboard-output limits; coordinate touch/input destination, never shell/reset/ykman now. PIV physical deferrals remain untouched. |

For later coordinator-assigned managed execution, use current build/test skills and
unambiguous YubiOtp.UnitTests selectors. Relevant classes are HidBackendTests,
SmartCardBackendTests, YubiOtpSessionTests, IYubiKeyExtensionsTransportTests,
YubiOtpSessionLinuxCrossLayerTests and SlotConfigurationEncapsulationTests. Read
actual RESULT counts; a compiled project or skipped/all-no-tests run is no behavioral
proof. Runtime-loop changes also need the scoped resilience gate. All execution is
unrun here, including formatter, compiler, native API, device and crypto work.
