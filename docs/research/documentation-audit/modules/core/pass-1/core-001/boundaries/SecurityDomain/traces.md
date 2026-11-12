# Current Security Domain call-site traces

All anchors below refer to the fixed source pin. Core dependency reads are narrow and retain A/B1–B6/C primary ownership. The complete applet packet is the source-investigation population, not another whole-Core or whole-SD implementation audit.

## Factory and backend graph

```text
IYubiKeyExtensions.CreateSecurityDomainSessionAsync (48–73)
  ResolveSessionTransport(options.PreferredConnectionType, SmartCard only)
  WithPreferredConnectionType -> new options, copied configuration value, same parameters/prompt
  CreateSessionOverTransportAsync -> one ConnectAsync<ISmartCardConnection>, no fallback
    SecurityDomainSession.CreateAsync(connection, options) (137–147)
      ConstructInitializedAsync -> validate preferred type -> construct -> attach connection holder
      InitializeSessionAsync
        PrepareProtocolAsync -> ProtocolFactory -> PcscProtocol; base Protocol owns cleanup now
        SecurityDomainBackend.SelectApplicationAsync -> plain SELECT A000000151000000
        resolve override or 5.3.0; set explicit-version indicator
        ConfigureProtocolAsync -> configure PC/SC -> optional InitializeScpAsync
          shared ExchangeGuard -> ScpInitializer; feature gate -> SCP03 or SCP11 setup
          SCP03: INITIALIZE UPDATE -> verify card cryptogram -> MACed EXTERNAL AUTHENTICATE
          SCP11a/c: PSO certificate sequence -> authenticate -> receipt/key derivation
          SCP11b: INTERNAL AUTHENTICATE -> receipt/key derivation
          secure graph: ChainedResponseReceiver -> ScpProcessor -> extended transmitter
            or secure short command-chaining transmitter -> borrowed connection
        publish base effective protocol/version/authentication
        rebind SD backend if wrapper changed; assign local protocol/backend
      publish IsInitialized after callback
    OwnConnection only after successful direct factory
  on factory failure: dispose hidden connection, preserve original error even if cleanup/logging fails

Normal operation -> EnsureInitializedBackend -> backend.SendAsync -> protocol exchange guard
  -> status checking -> TransmitAndGetResponseDataAsync returns response.Data only
Reset -> key listing via normal graph -> raw connection attempts -> dispose old graph
  -> clear fields/flags -> InitializeSessionAsync with no configuration/version -> retained parameters
```

Direct creation borrows its already-open connection and never calls `OwnConnection`. Its failure cleanup disposes the prepared protocol and detaches the exact holder; the caller retains connection disposal responsibility. A detached slot proves bookkeeping release, not recovered device state. Convenience creation additionally disposes its hidden connection on failure. B1's post-open slot-logger failure remains a separate upstream caveat, not solved by the applet factory's later catch.

## Ownership and aliases

| Resource | Actual owner / borrow | End / consequential condition |
| --- | --- | --- |
| Direct connection | Caller; SD/base retain reference | Session teardown drains its admitted protocol exchange, detaches, leaves connection open; caller awaits connection disposal |
| Hidden convenience connection | Factory until success, then session via `OwnConnection` | Session teardown owns disposal; uncertain native release retains its platform claim under B1/B4 policy |
| Protocol and SCP state/session keys | Base `Protocol`, then effective wrapper; SD `_protocol`/backend are mirrors | Wrapper drains guard, disposes processor/state and base protocol; connection is separate |
| Options | Values accessed for factory initialization; no stored options field | Extension copies nullable `ProtocolConfiguration` value (readonly record struct) and firmware/key/prompt references; reset does not retain configuration/version |
| SCP parameters | Caller owns wrapper; SD retains exact `_scpKeyParams` through session lifetime, including reset | Keep alive until session and any operation are terminal; session disposal leaves wrapper alone |
| `StaticKeys` | Clones ENC/MAC/optional DEK source arrays; `Scp03KeyParameters` retains and disposes that exact object | Source bytes still caller-owned. Two wrappers/record `with` clones can share one contained owner; disposing one clears all aliases |
| `Scp11KeyParameters` | Retains SD public key and optional OCE private key; copies collection, retains certificate identities | Dispose clears private key and disposes every contained certificate, not SD public key. Construction failure leaves caller input ownership; sharing/record clones need one enclosing lifetime |
| Imported private key | Caller owns `ECPrivateKey`; SD reads its `Parameters.D` alias synchronously for inner DEK wrapping | Session never disposes/zeros caller key. Keep it alive until call terminal. Owned encrypted/encoded copies have separate cleanup scopes |
| Certificate store input | Caller certificate objects; SD reads DER, sends concatenation; no trust/order verification | Caller disposes objects after use; public DER bytes are not automatically secret |
| Certificate result | Caller owns each real loaded `X509Certificate2` | Partial failure disposes accumulated list; success survives session disposal. This source resolution is distinct from SCP parameter containment |
| Returned GET DATA bytes | Alias of hidden response-owned byte array; no borrowed transport/pool storage | Survives session teardown. Public facade returns no `ApduResponse.Clear` handle; secret-return policy needs X003 decision |
| Prompt | SD constructor calls `base(connection)`; never forwards `options.UserPresencePrompt` | No retained prompt and no notification callback here. Application owns any supplied unused service; generic retained-prompt prose needs applet condition |
| Reset raw command | Pooled 13-byte command, eight public zero bytes | Array returned with whole-rental clearing in finally after actual raw transmit completion; response result remains separate |

Static/session DEK copies are distinct: SCP03 session derivation copies the static DEK into `SessionKeys`, while SCP11 derives its own DEK. Parameter disposal during an established SCP03 exchange can leave derived keys usable yet break later reset derivation; that is no permission to shorten the documented parameter lifetime. Public-key wrapper parameter arrays remain mutable aliases under C's owner decision; retaining a public key is not immutable trust storage.

## Initialization, authentication and failure

| Phase / result | Readiness, protocol and auth | Translation / recovery responsibility |
| --- | --- | --- |
| Transport/preferred-type rejection | No applet wire I/O or returned session | Argument/unsupported/open errors propagate; no transport fallback |
| Prepared / SELECT pending | Base protocol registered, local backend temporary, not ready/authenticated | Preparation and SELECT are outside SD catch; dispose partial session on creation failure |
| Configured plain success | Effective override/assumed version, local graph assigned; callback then publishes ready | `IsAuthenticated=false`; reads can work without host authority |
| SCP03 success | EXTERNAL AUTHENTICATE succeeds, encryptor acquired, wrapper published; callback then ready | Inherited `IsAuthenticated=true` describes setup success; authorization still command/device dependent |
| SCP11 success | Receipt verified and wrapper published; then ready/auth flag | b authenticates SD only. Draft §4.3 a/c host authentication also needs first correct-MAC post-establishment APDU; flag precedes that device phase |
| Configure/SCP Apdu/BadResponse/NotSupported with parameters | No successful effective state publication | `SecureChannelException` preserves immediate inner; selected status only. CLA-not-supported first becomes Core NotSupported with nested APDU: SD StatusWord is null |
| Missing DEK / crypto-provider / other argument failure | Can occur after wire setup; partial state owns cleanup as available | InvalidOperation/Cryptographic/Argument exceptions escape SD wrapper; do not broaden XML to all failures |
| SCP03 derivation fails after SessionKeys allocation but before state return | No returned state for outer initializer guard | B6-C006 owns narrower leak/cleanup candidates; SD factory disposing base protocol does not reach that inaccessible local owner |
| Later operation failure | Outside initialization wrapper | APDU/status/parser/provider error keeps original type; partial accepted writes may persist. Current auth flag is setup history, not channel health |
| Disposal admitted | Public ready/auth getters false immediately | Core drains one admitted logical exchange, not every public workflow/parsing continuation; sync disposal can block |

SCP failure after Encrypt/Mac normally latches base recovery and future commands refuse. B6-C004's counter increments before `stateAdvanced` is set: a failure inside Encrypt can miss that notification. SD adds no retry/replay policy to compensate. Failure after initial SELECT or SCP setup can alter device selection/state even when direct creation leaves the borrowed connection open.

## Reset state and cancellation

| Reset stage / exit | Source behavior | Supported caller action / evidence bound |
| --- | --- | --- |
| Initial readiness call | Already-ready session returns from InitializeSessionAsync without rebuilding | Does not reapply options or repair an unhealthy channel |
| GET DATA E0 empty | Logs skip; returns without clearing flags/reinitializing | Empty listing is no proof of defaults or reset |
| Enumeration / raw attempts | SCP03 KID 01 -> INS 50/reference 00/00; KIDs 02/03 skipped; 11/15 -> INS 82; 13 -> INS 88; others -> INS 2A | Up to 65 attempts each. 6983 or 6982 stops reference; 6A80/9000 continues; other statuses logged; no exhausted-result failure |
| Raw attempt ordinary exception | Warning with exception, continue | Device may have changed; reaching loop end is no restore verification |
| Cancellation before/between raw calls | Throws unchanged before teardown | Old protocol/local fields and ready/auth flags can remain despite preceding raw mutation; dispose/reopen |
| Cancellation after raw native dispatch | Connection/platform decides completion; caller memory remains borrowed until actual task terminal | A finite token is not a native interrupt or wall-clock bound; do not clear/reuse at outer WaitAsync boundary |
| Raw attempts all finish | Logs reset complete before reinit; synchronous Protocol.Dispose then nulls fields and clears flags | Logging/disposal failure before those assignments can also leave stale local state; message is progress narration, not verified factory restoration |
| Reinit starts | New protocol/SELECT; original parameters retained; configuration/version omitted | New graph can reauthenticate with deleted/static aliases, or fail before handshake at SCP11 firmware gate |
| Reinit failure | Ready remains false; base may retain newly prepared protocol; local graph null | Reset is not the Create factory catch: partial graph remains until disposal (or another attempted reset can replace it). Dispose rather than infer reusable state |
| Successful reinit | Effective version 5.3.0, explicit flag false, optional auth restored only if setup succeeds | Caller wanting original options uses a fresh session; no post-reset contents check |

Normal backend operations check cancellation on exchange entry; admitted SELECT, handshake, command/response chains run with `CancellationToken.None`. `GetCaIdentifiersAsync(Kloc|Klcc)` has two guarded exchanges; cancellation may stop the second after the first completed. Reset raw calls bypass the protocol guard and receive the caller token directly. Core's per-native-call admission can reject overlap on built-in PC/SC, but it does not give the entire raw reset an operation lease; custom SmartCard implementations retain their own contract. Keep all SD calls sequential and await the whole workflow before disposal or another call.

## Effective version / override provenance

| Input / path | Effective version and checks | Qualification |
| --- | --- | --- |
| No override, plain factory | 5.3.0 assumed; no firmware probe | Compatible configuration assumption, not detected hardware version |
| No override, GenerateKey | Local explicit-version gate skipped, request can reach device | Returning P-256 key does not update session FirmwareVersion |
| No override, SCP11 factory | Core sees 5.3.0 and rejects before PSO/INTERNAL/MUTUAL AUTHENTICATE | Earlier SD SELECT already occurred; wrapped NotSupported |
| Explicit 5.7.2+ | Configures with exact supplied value; Generate gate active; SCP11 initializer gate passes | Caller provenance must be trusted. Can obtain Management live version in an earlier, disposed session; cached metadata is last-known |
| Explicit below thresholds | Generate <5.7.2 throws locally; SCP03 <5.3/SCP11 <5.7.2 setup rejected | Feature query itself does not perform I/O or validate device support |
| Other feature descriptors | Plain creation selects SD without an independent FeatureSecurityDomain check; allowlist/attestation descriptors add no local enforcement to those operations | Only Generate's explicit-version branch and Core SCP setup gates enforce the version checks described above; per-command device acceptance remains separate |
| Major zero | Feature optimistic compatibility accepts; PC/SC alpha configuration special branch | ForceShortApdus ignored by that branch; InsSendRemaining still configured. A/B5/B6 own generic policy; no hardware guarantee |
| Reset reinit | Defaults to 5.3.0; clears explicit flag; default response instruction/format policy reapplied | SD-IMP-005 persists despite centralized completion boundary; fresh session workaround |
| Test helper | Every fresh reset/test factory receives state.FirmwareVersion and supplied config; reset's internal reinit still loses them | Success of the second fresh test session cannot prove reset retained options |

## All B6 mechanisms reconciled at this caller

| Existing group | Actual SD reach / qualification; no new Core mechanism ID |
| --- | --- |
| C001 APDU grammar | GetData Le=0 still emits short 00 or extended 0000; positive short 256 rejected; facade only validates negative Le. Generic length/status semantics remain B6 |
| C002 response copies | Facade returns only hidden response.Data; KCV/generic raw result can be sensitive; fragments/decrypted temporary remain separate even if final owner cleanup is later added |
| C003 recovery | Backend uses normal graph except raw reset; no local 6C correction. New reset graph can discard a recovery latch without proving same connection recovered |
| C004 mutation notification | Every protected SD call uses same Encrypt/Mac sequence; no applet guard repairs missed notification inside failed Encrypt; no native failure executed |
| C005 INS 82 exemption | Normal post-establishment methods fix INS to CA/D8/E4/F1/E2. SD exposes no arbitrary INS setter. Reset 82 goes raw, handshake 82 uses initialization phase; no claimed normal SD post-establishment bypass |
| C006 SCP03 startup | SD wraps selected failures; direct factory disposes prepared protocol but cannot collect unreturned local SessionKeys. Missing DEK InvalidOperation escapes selected wrapper |
| C007 parameter aliases | Exact retained parameters reused by reset, wrappers/with-clones share owners, early disposal can break reinit. Public examples need enclosing lifetime and explicit contained identities |
| C008 KeyReference encoding | Public fields control APDU header/reset instruction; AsSpan cached bytes control cert/CA filters. `with` modification can make those diverge. Recommend fresh constructor in examples; mechanism stays B6 |
| C009 SCP11 admission | SD does no extra constructor/profile trust validation; generated/imported keys explicitly P-256, external parameters can be broader. a/c provisioning helpers leave OCE reference explicit; public constructor null semantics stay B6 |
| C010 editions/key role | Public draft restrictions already qualified; inherited auth flag still precedes a/c first-MAC phase. c SD key called ephemeral in Core is separate from SD Generate's persistent device key |
| C011 empty command | Common ListKeyInformation/card recognition/CA GET DATA use no requestData. Current padded-empty encryption is directly reached under SCP03; Python agrees but final 1.1.2 says increment without encrypting absent field |
| C012 protected chaining | Certificate GET can span responses and certificate STORE can span commands. Core wraps each follow-up and decrypts fragments separately; no SD test supplies independent status-only 61xx oracle. Synthetic fixture's single 9000 exchange cannot settle chaining |
| C013 test precision | Generated key test lists later without SCP11 auth; b rejection accepts any ApduException; c listing is discovery allowed without host auth; see test-oracles.md |
| C014 logging | Normal APDU trace headers/length only; fixed SD SELECT AID public. SD forwards opaque exception objects and exception Message; privacy cannot be promised end-to-end |
| C015 useful tasks | Complete typed SCP11/version and borrowed-connection proposals supplied; existing rotation advice escrow/fresh verify remains correct. Proposals are uncompiled, no new SDK helper invented |
| C016 ATR hash | SD never uses ATR as a key or compares ATR. Transport identity owner B6/B4; no local defect occurrence or new SD candidate |

Core command chaining protects the whole command before short fragmentation; response chaining calls the SCP processor per continuation. Default remaining instruction is C0, fixed CLA/P1/P2/Le zero, not SW2-derived length. Public SD StoreData's “one command” means one logical STORE DATA, not one physical APDU when Core chunks it. Normal status failures throw before facade returns; status-only error handling and plaintext-copy cleanup retain B6 ownership. Raw reset is the only explicit bypass and reads final two bytes itself, rejecting responses shorter than two bytes.
