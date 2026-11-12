# OATH/Core boundary analysis

Source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Investigator: GPT-6.1 Sol.
All statements below describe static source/assertion reading. Independent review, editing,
compilation and runtime qualification remain pending. Local IDs are candidates, with zero accepted
Core allocations. The historical OATH implementation exception remains historical authorization.

## Factory and creation trace

| Phase | Actual symbol and behavior | Ownership / failure boundary |
| --- | --- | --- |
| Discovery | README calls `YubiKeyManager.FindFirstAsync()` with All scope. OathTool's local `DeviceSelector.SelectDeviceAsync` constructs the shared selector with SmartCard eligibility and `FirstEligible`; its default discovery still calls All `FindAllAsync`. | Eligibility filters returned complete references, rather than restricting discovery I/O. A caller can explicitly use SmartCard-scoped Find; that avoids HID enumeration/probes in that call but can yield partial views with independent disjoint-member ownership. |
| Selection | `CreateOathSessionAsync` → `ResolveSessionTransport(options?.PreferredConnectionType, "OATH", SmartCard)` → `WithPreferredConnectionType` → `CreateSessionOverTransportAsync`. | OATH selects only SmartCard. Invalid/group/HID preference fails before open; missing exposed SmartCard is unsupported. No fallback follows an open/initialization error. |
| Open | Core's `OpenSessionConnectionAsync` calls `ConnectAsync<ISmartCardConnection>`. Built-in grouped `YubiKeyDevice`/slot registration and PC/SC owner are B1's mechanisms. | Claims cover known overlapping member IDs in this process. Strict Exclusive PC/SC is the default, with explicit Shared/fallback policy available. No cross-process or disjoint-view guarantee follows from the OATH extension's headline. |
| Direct construction | `OathSession.CreateAsync(connection, options, token)` → `ConstructInitializedAsync` → validate preferred type → `Construct` → attach exact holder. | Constructor completes before attach; attach precedes SELECT. Caller retains connection ownership. Refused construction disposes only its new session. |
| Prepare | `InitializeSessionAsync` → OATH `InitializeAsync` → `PrepareProtocolAsync` → `ProtocolFactory.Create`. | Core registers the protocol before setup I/O. Actual OATH input is SmartCard; HID channel initialization is not on this path. |
| SELECT | `OathBackend.ReadInitialStateAsync` → `SelectAsync(ApplicationIds.Oath)` → `PcscProtocol.SelectAsync`. | Required version is three bytes; ID is nonempty; present challenge is nonempty. DecodeList owners are disposed. Salt/challenge are copied before those TLVs die. Unknown SELECT tags are ignored; duplicate recognized fields retain last-value behavior. |
| Apply / gates | `ApplyInitialization` uses override or selected version, takes salt/challenge arrays, computes OATH DeviceId, sets IsLocked/IsPasswordProtected from challenge. Any non-null SCP parameters invoke the OATH effective 5.6.3 check. | An override leaves mandatory SELECT validation intact. The local name `FeatureScp03` also gates SCP11. No global session-open floor or touch/SHA-512 preflight is introduced. |
| Configure / SCP | OATH forces `InsSendRemaining = A5` while preserving other configuration; `ConfigureProtocolAsync` configures plain PC/SC, then initializes optional SCP. Core gates SCP03 at 5.3.0 and SCP11 at 5.7.2. | Returned SCP wrapper replaces Core Protocol and the OATH backend. OATH passes creation parameters through the initializer; it stores no `_scpKeyParams` for reset. |
| Ready | After backend assignment and debug logging, the full initializer returns; Core checks disposal and publishes IsInitialized. | Plain initialization leaves IsAuthenticated false. SCP success sets it true before final readiness; neither flag represents OATH access-key unlock. |
| Factory failure | Core `DisposeAfterInitializationFailure` clears/disposes the partial session, suppressing cleanup/logging exceptions to preserve the initializer failure. Convenience outer catch awaits hidden connection disposal with the same primary-failure rule. | Direct failure detaches but leaves caller connection open. Partial wire failure still requires caller recovery judgment; successful bookkeeping detach is not native-release or device-reuse proof. |

## Resource table

| Resource | Owner / actual OATH use | End of borrow and cleanup |
| --- | --- | --- |
| Hidden convenience connection | OATH extension opens it and calls `OwnConnection` only after successful direct creation. | Session teardown closes protocol then owned connection; native worker drains accepted calls, transaction end, disconnect and context release. Failed checked release retains the original claim. |
| Direct connection | Caller owns it; `CreateAsync` borrows. | Dispose session first, then connection. Healthy idle sequential reuse is supported; terminal OATH reset specifically requires close/reopen even though Core detaches the old session. |
| PC/SC command source | APDU formatter/transport borrows data for the actual exchange. OATH owns encoded command arrays/TLVs. | Keep inputs unchanged until the actual consuming task terminates. A separate `WaitAsync` timeout supplies no storage-release permission. Native task remains pending through native use. |
| Protocol and SCP session keys | ApplicationSession owns registered plain/wrapped protocol. SCP wrapper shares base exchange guard and disposes ScpState after drain. | RESET uses this same disposal owner. Creation parameters/static keys and their source arrays are different resources. |
| SCP03 parameters | Caller owns parameter object; it retains exact `StaticKeys`, whose own arrays clone caller sources. | Dispose parameter object after last intended handshake. OATH does not retain it after creation; the offline reset case deliberately disposes it early and observes derived state separately. Reopen requires live parameters again. Record clones share contained owners. |
| SCP11 parameters | Caller owns wrapper, private key and certificates as contained disposal scopes; constructor copies the list, not each object. SD public key is retained and not disposed by parameters. | Complete all handshakes before disposing shared objects. B6/C own exact containment/profile questions; OATH adds no independent trust-store or certificate-chain validation. |
| User-presence prompt | Application-owned service retained by ApplicationSession. Each calculation gets its own notification handle. | Keep service/callback resources alive through actual request/resolution tasks; session disposal never disposes the prompt and supplies no whole-workflow callback drain. |
| Credential seed | Caller clears original input. `CredentialData` owns a private clone; Secret views alias it. | Dispose only after all PUT/retry consumers finish. PUT uses independent processed material and clears it, keyValue, sensitive TLV copies and encoding, preserving the seed. |
| Base32 text and output | Core borrows text; OATH SDK/CLI each receive independently owned output. | Admission rejects overlap without changing either span. Admitted decode failure clears entire destination; success preserves tail. URI/CLI clone into CredentialData then clear decoded bytes in finally. Immutable URI/argv/Base32 strings remain separate, uneraseable sources. |
| SELECT salt/challenge | Backend transfers independent arrays into session. Salt getter aliases `_salt`. | Disposal zeroes those arrays and existing Salt views. Cached DeviceId/version/IsLocked/IsPasswordProtected getters survive as historical values; only inherited readiness/auth getters are forced false. |
| APDU response | Sealed ApduResponse copies transport bytes; Data/RawData alias its private copy. TLV decoder makes additional disposable owners. | VALIDATE and single/aggregate calculation finally blocks zero the actual RawData array; existing casts work for this owned array. Prefer owner `Clear()` in separately authorized maintenance. This reaches neither transport staging nor earlier SCP/decryption/chaining copies (B6-C002). |
| Calculation return | CalculateAsync resolves callback before making independent HMAC-only copy; caller owns it across session disposal. Code is immutable string metadata. | Caller clears HMAC via the supported returned-memory ownership contract. Code.Value and intermediate scalar values have no deterministic erasure API. Resolution failure prevents output transfer/string materialization for individual calculations. |
| Test fake bytes | RecordingSmartCardConnection snapshots commands and dequeues input arrays without clearing them on disposal. ResponseBackend retains the actual session response for zeroing assertions. | Tests must explicitly clean retained snapshots if sensitive; zeroing a different transport array is not an oracle for ApduResponse cleanup. |

## Session, authentication and reset states

| Event | Readiness / authentication / applet state | Recovery and evidence limit |
| --- | --- | --- |
| Unprotected SELECT, plain | Ready true only after callback; inherited auth false; IsLocked false; IsPasswordProtected false. | No access-key ceremony needed. |
| Protected SELECT, SCP optional | Ready true after full callback; inherited auth tracks SCP; IsLocked true and IsPasswordProtected true. | SCP does not unlock OATH. Call Validate with derived access key. |
| Successful VALIDATE | Checks returned device HMAC using FixedTimeEquals; IsLocked false, challenge zeroed/replaced, IsPasswordProtected true. | Consumes the outstanding challenge. Repeating VALIDATE on this session raises InvalidOperationException. |
| SET CODE success | IsPasswordProtected true; existing IsLocked value unchanged. | Setting/changing key is persistent; a fresh session observes a new challenge. No inherited SCP-auth mutation occurs. |
| Unset success | IsPasswordProtected false; no generic Core auth change. | Caller first validates if locked, as documented. |
| Host HMAC mismatch / missing field | BadResponseException for missing response or failed comparison; malformed TLV may throw lower-level ArgumentException. IsLocked/challenge remain unchanged. | OATH DEC-001 retains post-verification-failure policy decision. Core's secure wire latch and OATH host-HMAC verification are different layers. |
| Device 6982 on ordinary Send | OathException Locked with raw status and original ApduException. | No automatic password acquisition or state refresh. AuthenticateAndRetry catches only that reason. |
| Device 6984 / 6A80 on VALIDATE | Compatibility WrongPassword category preserves raw status/inner cause and neutral message. | Vendor VALIDATE table calls these auth-not-enabled/wrong-syntax. A fake choosing 6984 for a wrong key supplies no firmware mapping proof. |
| SCP wire error | Core can latch recovery required after advanced secure state/failed continuation; later exchange refuses. | IsAuthenticated is a historical successful-setup flag, not a live channel-health probe. Dispose/reopen on uncertainty; no local OATH rehandshake is attempted. |
| Confirmed RESET success | `00 04 DE AD` with no application data → await SendAsync → await DisposeAsync without caller token. Readiness/auth false at disposal admission; derived state and salt/challenge cleared during cleanup. | No SELECT, metadata refresh or SCP handshake. Borrowed connection remains caller-owned; close/reopen. Owned connection is awaited closed. Disposal failure propagates with reset already completed and session terminal. |
| RESET canceled/refused before exchange | Pre-cancellation or overlap fails before confirmed-success boundary. | Healthy session survives; another admitted operation keeps ownership. No global connection poison is introduced. |
| RESET transport/device failure | Success not confirmed; code does not invoke terminal-success disposal. | Mutation may already have occurred. Caller owns inspection/recovery; helper/CLI close their session in finally. |
| New creation after reset | Fresh actual connection/session, mandatory SELECT, fresh identity/state, optional new handshake. | There is no `NewContext.DoesTokenAfterReset` symbol/API. No generic token-after-reset guarantee is inferred. OATH has access keys/challenges, not a CTAP token lifecycle. |

## Cancellation, prompts and workflow lifetime

| Actual workflow | Cancellation / lifetime observation | Consequence for callers |
| --- | --- | --- |
| Single PC/SC exchange | ExchangeGuard checks token before admission; delegate receives None through all constituent sends. Built-in native owner task remains pending through native use. | Await actual operation and disposal; caller cancellation cannot promise native interruption or a deadline. |
| Individual calculation request | Notification captures ambient intent, issues PolicyRequires for TouchRequired=true only, then sends CALCULATE. Request callback gets caller token. | LIST-derived TouchRequired=null stays silent even if device actually requires touch. Caller may obtain aggregate metadata first; no preflight touch enforcement is inferred. |
| Individual resolution | Parse structure/value before Completed; callback runs with None. Matching caller-token OCE maps Cancelled, other failures Failed. Request-callback failure has no resolution. | Callback failure after wire success can fail method; HOTP state may already advance. Timeout classification is not established by OATH statuses. |
| CalculateAll | One aggregate exchange plus individual custom-period CALCULATE followups with the original caller token. TLVs/raw aggregate stay inside finally through followups. | Cancellation between exchanges can stop followup, clear aggregate and fail whole method. A completed first exchange is not a lease for the whole method. |
| AuthenticateAndRetry | Original operation → catch Locked → cancellation check → password provider → DeriveKey → Validate helper finally clears key → retry operation once. | Caller owns callback-returned password, must await all callback work and clear it. No full-workflow lock/lifetime lease; no retry on unrelated error or failed validation. |
| OathTool prompt | ForOathPassword selects UTF-8 defaults with MinLength 1/MaxLength 128. Core measures UTF-16 code units, filters interactive control/excess input, rejects invalid redirected text, retains confirmation-first owner until comparison. | This is UI policy rather than an OATH firmware byte limit. Supplied password bytes bypass reader bounds. Redirected ReadLine creates immutable text and cannot be interrupted mid-read; OathTool checks before/after. |
| OathTool cleanup | Unlock clears owned derived key/prompted owner, borrows supplied password; failed setup disposes session. Commands await sequential body; ResetCommand preserves first reset error against disposal replay. | Generic await-using in other sample paths can let teardown failure replace an earlier failure; the reset/helper-specific precedence is not universal. |
| Concurrent disposal | Core closes/drains current exchange only. OATH parsing, state updates, requested/resolved callbacks and later workflow steps can lie outside that guard. | Await complete public workflows before non-reentrant disposal. Dispose in a calculation request callback can end session before a later Send, or resolution callback can precede returned-copy creation; no callback-wide drain is supplied. Sync disposal during an actual guarded/native callback can self-wait. B5-C006 owns mechanism. |

## Firmware provenance and policy

| Source / operation | Actual policy | Qualification |
| --- | --- | --- |
| OATH SELECT version | Required three raw bytes → new FirmwareVersion. No raw-version public property is retained separately after override. | Management's cached fixture firmware and physical qualifier are separate observations. |
| FirmwareVersionOverride | Effective value used for OATH rename/SCP gate and Core Configure/SCP gate. | Mandatory SELECT still executes and validates. Override describes host compatibility policy, not hardware identity. |
| Major-zero | Feature.IsSupportedByFirmware and FirmwareVersion compatibility helpers accept major-zero optimistically, including unknown 0.0.0. | Do not claim every such key physically implements >=5.8 features. A-C015/B5-C001 own generic wording; this is its actual OATH use. |
| Rename | Effective >=5.3.1 through EnsureSupports. | Device supports/rejects actual operation independently of the query. |
| Any SCP | Effective >=5.6.3 OATH gate, then Core SCP03 >=5.3.0 or SCP11 >=5.7.2. | Python pin rejects only 5.0.0<=v<5.6.3 and retains distinct pre-5 behavior. Shared history commit 1b87964ba258e3ce39fd2959888dfe7cf81a6843 introduces floor; earliest hardware/workaround rationale remains unresolved. |
| Touch / SHA-512 | Device requirements 4.2.4 / 4.3.1 in vendor page; OATH sends supported options without those SDK preflight gates. | Current public feature table already makes this useful distinction; retain it. |
| Reset/fresh reopen | Old FirmwareVersion/DeviceId remain historical on terminal object. Fresh call accepts caller-chosen options and parameters. | OATH does not restore prior override/configuration automatically; helper passes configuration/SCP anew but no firmware override. SecurityDomain's reinitialization-argument issue stays with its own boundary. |

## OATH operations affected by B6 wire and ownership concerns

This is exact call-site application of existing mechanisms, not new wire issue IDs.

| OATH caller | B6 concern and bounded consequence |
| --- | --- |
| LIST A1 and RESET04 have empty Data | B6-C011: ScpProcessor currently pads/encrypts empty data. Final SCP03 1.1.2 §6.2.6 says counter increments but no encryption for absent data. Real OATH listing/reset use this arm, including reset as the first protected command in the fake. |
| Response continuation A5 | B6-C012: configured A5 followups are protected by the SCP processor per fragment; status 61xx treatment and assembly layer differ from pinned Python/final-edition profile discussion. Plain OATH tests prove A5 rather than C0; they supply no SCP 61xx interop proof. |
| RESET authenticated empty success | Fake derives R-MAC from transmitted prefix and returns R-MAC+9000; it neither checks outbound C-MAC nor decrypts/polices empty command data. The test proves accepted synthetic success drives actual state disposal, not final-profile outbound RESET correctness. |
| All OATH normal commands after creation | B6-C005's INS 82 exception is in a shared processor but OATH applet INS values are not 82. OATH cannot directly request that exception through its public methods. Factory EXTERNAL AUTHENTICATE uses encrypt=false in the handshake phase; no new OATH exploit claim. |
| VALIDATE / CALCULATE / aggregate secret-derived responses | B6-C002: OATH clears its returned ApduResponse copy plus decoded TLVs/extracted HMAC; encrypted wire, cloned decrypted temporary and previous fragment storage remain separate owners. Preserve exact IMP-001/008/011 source resolutions. |
| Bad device status | PcscProtocol/Scp wrapper throws before OATH receives a response. ApduException.FromResponse stores header/status, not response payload or command data. Caller-owned cleanup still occurs in OATH finally; a lower-layer response-cleanup concern belongs to B6. |
| SCP factory with any parameters | B6-C006/C007/C009/C010 and C-C020 retain initializer/contained-key/provider/trust qualifications. OATH does not add a generic validation or device-authorization guarantee. SCP11b setup is device authentication; a/c first-protected-command authentication phase in retained review is separate from factory flag. |

## Historical implementation resolutions retained

The fourteen `oath-pilot-and-fixes-wave-002:IMP-*` scopes were reconciled against current source and
the original implementation-followups ledger. IMP-001 raw VALIDATE response, IMP-002 OathTool cancellation,
IMP-003 seed ownership/retry, IMP-004 parser failure cleanup, IMP-005 common Base32 shapes,
IMP-006 outgoing identity round-trip/64-byte boundary, IMP-007 request numeric narrowing,
IMP-008 HMAC-only result/notification validation, IMP-009 required response structure,
IMP-010 terminal reset, IMP-011 aggregate response/followup cleanup, IMP-012 help-before-bootstrap,
IMP-013 CLI option arity and IMP-014 required SELECT metadata retain source-resolved outcomes.
This boundary adds zero new execution to their historical evidence. Generic Core buffer/workflow/SCP
questions reopen only the affected dependency claims, never these fixes wholesale. DEC-003 URI policy,
DEC-004 padding provenance, DEC-005 resolved timestamp policy and DEC-006 detailed HOTP semantics retain
their historical applet scope; this packet is not a second whole-OATH audit.

The original serial 9681620 two-key HID discovery failure remains unexplained. A SmartCard-scoped caller
can avoid an unrelated HID read, but OathTool currently uses All discovery and neither modern architecture
nor the narrower selection option diagnoses that failure. Serial 125's FF and 00 INITIALIZE UPDATE results
were `6A88` before EXTERNAL AUTHENTICATE/RESET. Plain reset and synthetic SCP cleanup remain separate from
live secure terminal-reset proof. User-ready Windows CCID/HID work is a future pinned delta, not included
in B4's source. PIV physical deferrals remain unchanged.
