# Actual CTAP2/Core call paths and resource scopes

Source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Line numbers describe this source.
The unit/file ledgers retain original raw and character-range hashes. Narrow Core/WebAuthn reads retain
their primary owners; this record dispositions CTAP2 call sites only.

## Factories and initialization

```text
IYubiKeyExtensions.CreateCtap2SessionAsync (140–170)
  -> ResolveCtap2SessionTransport (178–206)
     -> Core YubiKeyConnectionExtensions.ResolveSessionTransport (180–221)
  -> WithPreferredConnectionType copies options carrier
  -> Core CreateSessionOverTransportAsync (246–303)
     -> typed ConnectAsync<IFidoHidConnection|ISmartCardConnection>
     -> Ctap2Session.CreateAsync (125–135)
        -> ApplicationSession.ConstructInitializedAsync (247–270)
           -> validate preferred type; construct; ConnectionSessionGuard.Attach
           -> InitializeSessionAsync; Ctap2Session.InitializeAsync (137–180)
              -> PrepareProtocolAsync: create/register protocol; reject non-PCSC SCP
                 -> HID InitializeAsync: guarded broadcast INIT, nonce/CID/vendor version
              -> CreateBackend
                 HID: no applet SELECT
                 SmartCard: SELECT ApplicationIds.Fido
              -> GetInfoCoreAsync: CTAP GetInfo via current backend
              -> detected GetInfo firmware or 0.0.0; SmartCard Transport.Nfc bypasses USB gate
              -> ConfigureProtocolAsync(override ?? detected, configuration, SCP)
                 -> local Configure; optional PcscProtocol.InitializeScpAsync
              -> rebind backend if protocol wrapped; assign backend; debug log
           -> readiness only after full initializer and disposal check
     -> OwnConnection after successful initialized factory
  failure -> Core helper awaits opened connection disposal; preserves original failure
```

`GetCtap2InfoAsync` creates an owned temporary session, performs its requested **second** fresh GetInfo,
then disposes the session/connection. It supplies no cache refresh of that session's effective firmware.
Direct creation first asserts supplied type; its `PreferredConnectionType` never opens another transport.
SCP parameters and prompt remain borrowed even when the connection is owned.

| Boundary | Success | Failure/cancellation and cleanup | Remaining mechanism owner |
| --- | --- | --- | --- |
| Transport resolution/open | One chosen typed connection | No transport retry; open's native owner keeps exact claim until release evidence | B1/platform families; B4 Windows open/cleanup |
| Construct/attach | One session attached before I/O | Constructor failure has no attachment; refusal disposes only unbound new session | B5 guard mechanics |
| Prepare HID | Core INIT precedes CTAP GetInfo | Pre-cancel is checked; admitted exchange uses uncancellable constituent I/O; no claim that malformed/partial INIT is recovered | B5 framing; B2/B3 native release; B4 |
| SCP requested on HID | Rejected before INIT/GetInfo | Protocol already registered for session cleanup; borrowed connection remains caller-owned | Existing fake Mac SCP-before-I/O test |
| SmartCard SELECT/GetInfo | Select shared FIDO AID then read CTAP info | Any SELECT ApduException is wrapped NotSupported with inner cause; this wrapper alone identifies no precise absence cause | B6 APDU status/error cleanup |
| USB SmartCard firmware gate | Detected major-zero or >=5.8 passes; NFC bypasses | Override cannot rescue detected older USB firmware; successful prior SELECT/GetInfo still happened | CTAP policy; B6 transport classification/config |
| Configure/SCP/rebind | Effective protocol and firmware assigned after configure/auth success | Base Protocol remains disposal owner during incomplete setup; factory suppresses/logs cleanup failure, original error survives | B6 handshake/intermediate/post-SCP applet state |
| Ready publication | Entire CTAP initializer returned | Backend assignment alone or SCP establishment alone is not public readiness | B5 shared completion boundary |

## Exchange, notification and outcome graphs

```text
MakeCredential/GetAssertion
  -> CTAP input checks + pure Build request
  -> CreateUserPresenceNotification captures caller-flow intent
  -> HidBackend.SendCborAsync
       Core FidoHidProtocol.SendCommandAsync(CBOR=0x10, CTAP request, same handle)
         -> ExchangeGuard.Enter; lazy INIT if needed; frame full request
         -> uncancellable Receive; keepalive 0x02 requests DeviceWaiting
         -> caller cancel or request callback failure: attempt one CTAPHID_CANCEL
         -> drain/validate original terminal response, including continuations
         -> rethrow request callback failure after valid drain;
            signal cancellation unless CBOR success won the race
         -> exit exchange guard; backend owns returned accumulation array
       CTAP status maps Completed/Cancelled/TimedOut/Failed
       resolve notification before successful response transfer
       strip status byte; typed Decode follows in Ctap2Session
  or SmartCardBackend.SendCborAsync
       request supplied policy notification
       Core APDU CLA=0x80 INS=0x10 P1=0 P2=0 Data=CTAP request Le=0
       PcscProtocol guard -> processor chain -> connection -> APDU status check
       CTAP status maps outcome; resolve; transfer status-stripped data view
  -> finally clear CTAP Make/Get encoded request
  -> typed response decode and completion logging
```

Wire hexadecimal values and decimal byte lengths are separate. CTAPHID on-wire CBOR is `0x90` after its
initialization mask; CTAP command `0x01` means MakeCredential inside that payload. CTAPHID CANCEL
`0x11`/on-wire `0x91` receives no separate acknowledgment. The original CBOR request's CTAP
KEEPALIVE_CANCEL `0x2d` is the response. A HID ERROR `0x3f` is a transport error, distinct from CTAP status.

| Event | Actual CTAP/Core behavior | Caller action / scoped limit |
| --- | --- | --- |
| Pre-canceled exchange | Guard refuses before wire admission | Await original task; earlier independent initialization/workflow steps can already have occurred |
| Cancel during no-report wait | Guard's constituent read uses None | Caller token supplies no immediate abort or elapsed deadline; hardware/native terminal behavior is separate |
| Keepalive processing after cancellation | One CANCEL attempt, then original terminal drain | Valid late CTAP success may return; device side effects are retained |
| HID request callback throws | Capture error, cancel/drain then rethrow when valid response drained | Notification becomes terminal without a matching resolved callback; partial/malformed drain retains recovery uncertainty |
| SmartCard request callback throws | No APDU sent by that backend call; no resolution callback | Earlier initialization and other workflow steps remain real |
| CTAP error | Backend classifies status and resolves successful prior request | Cancelled/TimedOut refer to CTAP/operation mapping, not native release or RP result |
| Success terminal callback throws | Callback error propagates; response untransferred and normally cleared | Existing tests observe actual response storage; X020 retains logger-failure exception path |
| Typed decode or post-decode log throws | Backend already resolved success and transferred its response | Failure is not rollback; independent typed copies need chosen cleanup ownership |
| Malformed CID/sequence/command/length | Core rejects the encountered packet | B5 retains distinct validation/recovery-latch gaps; dispose/reopen after uncertainty |
| Reset succeeds | CTAP command completes and logs; session has no reset reinitialization/cache/token registry | Device invalidation is separate from managed IsInitialized/FirmwareVersion snapshot; discard dependent token/helper assumptions and re-read state outside continuation sequences |

### Prompt and intent responsibility

ApplicationSession retains the caller's `IUserPresencePrompt` and never disposes it. Notification.Create
captures `UserPresenceIntent.Current` in the caller's async flow, preserving a preexisting explicit intent.
The same notification travels to Core; native owner workers never create it. Request success emits at
most once, and only a requested handle resolves; resolution uses CancellationToken.None. A primary
operation exception normally survives a resolution exception, with the latter sent to logging.

Make/Get carry RP scope and ordinary policy context. GetAssertion `up:false` uses None; Make rejects
`up:false` before its request/prompt. Selection/Reset use null-scope HID context and silent SmartCard
policy. Generic SendCborRequest uses None, so ClientPin/bio/admin traffic receives no inferred touch
notification, even where an authenticator performs UV. CTAP itself receives no ICredentialPrompt and
performs no automatic PIN retry loop. WebAuthn owns its separate credential-prompt/retry/timeout policy.

## Owned and borrowed resources

| Resource | Owner / borrowing point | Release or transfer | Consequential limit |
| --- | --- | --- | --- |
| Convenience connection | Core opens; CTAP OwnConnection after factory success | Session protocol drain, detach, then await connection disposal | Claim release follows actual native proof on built-in owners; B4 Windows scope pending |
| Direct connection | Caller; borrowed by session/protocol/backend | Session detaches; caller disposes connection outside session scope | Detach permits bookkeeping reuse, without protocol/device recovery proof |
| Protocol/channel | ApplicationSession.Protocol; Core FIDO caches CID/vendor version | Dispose closes/drains exchange and clears CID; owned connection follows | Per-exchange, not whole workflow/post-exchange lease |
| Backend | CTAP session owns managed adapter; adapter borrows protocol | No independent native lifetime | SCP rebinding follows effective wrapper |
| Session options | Factory reads/copies carrier; configuration values passed into setup | Original carrier not stored for later reset | SCP object/prompt references still borrowed |
| SCP keys/certificates | Caller-owned parameters and contained owners, kept alive through creation | Caller disposes outer scope after secure session | Alias/record-clone/provider intermediates are B6/C responsibilities |
| Presence prompt/intent | Caller-owned service; operation captures text context | Terminal callback outcome; no service disposal | Callback must finish promptly; avoid reentrant disposal and detached work |
| PIN source | Caller or IMemoryOwner returned by Core console reader | Keep through complete ClientPin task, then zero/dispose | Reader UTF-16 units differ from CTAP input profile; redirected strings remain outside clearable ownership |
| ClientPin protocol | Supplied object, disposed by ClientPin | Stateless disposed flag; returned arrays survive | Other admin helpers borrow protocol instead; do not share with longer-lived use |
| Token | ClientPin returns caller-owned byte[]; admin helper borrows | Caller zeroes after last complete workflow | Authenticator token permissions/expiry/invalidation are separate |
| CTAP raw request | SendCborRequest borrows; Make/Get own encoded request | Make/Get finally clear final request; raw caller clears after actual task terminal | Builder captured clones/writer internals remain independent |
| HID outgoing/report/response | Core owns outgoing arrays; native owner owns its staging; returned reports escape; Core accumulates response | Outgoing arrays/failed accumulation clear; backend transfers success view | Returned report arrays remain B5-C005; session disposal leaves returned response intact |
| SmartCard response | Core ApduResponse copies raw connection result | Backend exposes Data slice; failure normally clears Data | Success loses a public Clear handle; underlying transport/APDU/SCP copies and error response need B6 answer |
| Make/Get typed models | Independent byte strings/authenticator copies plus make borrowed statement/unsigned slices | No general disposable owner | Original input clearing reaches only aliases, not copies; decode failure adds abandoned locals |
| StoredCredentialInfo | One private full-response clone; largeBlobKey aliases it | Dispose/failure clears clone; enumeration result disposes entries | Original response and independently decoded public fields remain unchanged |
| Extensions/PRF/blob plaintext | DTO/builders retain or allocate at their own stages | Caller owns returned arrays where exposed; many internal copies lack cleanup owner | Clearing another view/copy or disposing session supplies no end-to-end guarantee |
| ARKG mathematical/provider state | Core owns per-call native group/point/BN handles and managed intermediates | using scopes and selected explicit zeroing | Ordinary free/BigInteger/failure copies and deployed binary producer retain C/B4 qualification |

## Multi-exchange workflows and disposal

| Workflow | Actual exchanges and state | Public guarantee |
| --- | --- | --- |
| ClientPin permission token | Fresh GetInfo, getKeyAgreement, encapsulation, token request/decrypt | Individual exchanges; caller retains PIN/protocol/session until entire task terminal; legacy fallback drops permissions/RP rather than negotiating V1 |
| Set/change PIN | getKeyAgreement, encapsulation, encrypted set/change request | Failure/cancellation can follow mutation; named secret buffers finally clear with X005/X016 residuals |
| Credential RP/credential enumeration | Begin then reported-total next calls | Eager sequence, first total preserved; credential continuation failure disposes collected entry clones and throws; caller coordinates adjacency |
| GetAssertion/GetNext | First count then caller-issued N−1 next calls | No reserved sequence, tracked timer/count or account-picker lease in Ctap2Session |
| Blob read/write | Optional cached GetInfo; repeated read fragments; optional read-modify-write; authenticated write fragments | Per-exchange guard only; no conflict detection, transaction, resume or rollback; reads can fail after earlier writes |
| Bio loop | Generic begin/next/cancel/rename | No inferred presence owner, automatic cancel or whole-sequence lease; known CTAP wire/schema deficiencies remain original applet work |
| Session disposal | Readiness/auth false at BeginDisposal; protocol exchange drain; detach; owned connection close | New guarded session calls refused; already-running workflow can fail at next call or race later decode/helper state |

FIDO protocol self-exchange checks refuse synchronous/asynchronous disposal from its own flow. That
protective check supplies no universal callback/workflow disposal guarantee. SmartCard policy request and
backend terminal callback lie outside Core's exchange guard. Keep final disposal in the outer awaited
owner scope, rather than relying on callback disposal to drain the callback that initiated it.

## Version and capability provenance

| Value | Producer / use | Scope |
| --- | --- | --- |
| INIT vendor version | Core extracts bytes 13–15; CID from bytes 8–11; protocol version byte 12 distinct | Shared HID state, not session's chosen GetInfo firmware or CTAP edition |
| GetInfo firmware | Authenticator unsigned field interpreted by AuthenticatorInfo.DecodeFirmwareVersion as packed low-24-bit major/minor/patch | Vendor interpretation; missing becomes null; CTAP session uses 0.0.0 fallback |
| Transport gate | Current ISmartCardConnection.Transport == Nfc skips USB gate; otherwise Feature threshold 5.8 | Detected GetInfo value before override; model NFC capability and advertised transport strings are different |
| Effective firmware | Override ?? detected fallback passed to Configure/SCP and session FirmwareVersion | Future GetInfo calls do not replace it; no override skips required probes |
| Development allowance | Feature.IsSupportedByFirmware accepts Major==0 through IsAlphaOrBeta | Optimistic SDK policy, including unknown 0.0.0; no earliest-device/hardware support proof |
| Management metadata | Independent discovery/live Management read | Raw 0.0.1 and Management 5.8.0 can coexist; neither silently replaces CTAP's snapshot |
| Algorithms/GetInfo options | Authenticator advertisement/configured state | Generic Core catalog is structural; permissions/feature/profile/current state still govern command acceptance |

B6 owns the complete Configure branch implications: current PcscProtocol's major-zero branch consults
SupportsExtendedApdu and returns before ordinary ForceShortApdus handling. This source fact merits an
explicit configuration follow-up without guessing B6's chosen correction or native interoperability.

## Crypto and representation call-site graph

```text
PIN V1/V2 Encapsulate -> CTAP PinUvAuthHelpers -> BCL ECDiffieHellman(P-256)
  -> raw Z -> V1 SHA256 or V2 two HKDF contexts -> AES/HMAC directly via BCL
  (Core algorithm catalog is not consulted)
Generic CTAP CoseKey -> Core CoseKeyType constants / Core numeric algorithm carriers
  -> open unknown carrier or modeled EC2/OKP/RSA; no finite-registry/profile coercion
PreviewSignGeneratedKey.DerivePublicKey -> CTAP ArkgP256 thin wrapper
  -> internal Core CryptographyProviders.ArkgPrimitivesCreator
  -> Core ArkgPrimitivesOpenSsl.Derive -> NativeShims point/BN calls + managed HKDF/hash/BigInteger
PreviewSignDerivedKey.VerifySignature -> CTAP signature-DER conversion -> BCL P256/SHA256 VerifyData
EncryptedMetadataDecryptor -> BCL HKDF/AES128CBC directly
LargeBlobEntry -> BCL GCM + raw DEFLATE; Storage -> raw CTAP exchange
```

Core catalog key kty/crv metadata contains no intrinsic alg. Algorithm applicability is structural;
CTAP peer labels, selected algorithm/curve, authentication parameters and experimental wire profile remain
consumer responsibilities. RFC9459's COSE CBC construction never imposes PKCS#7 on CTAP PIN or SCP CBC.
The Core provider delegates also have finite reach: CTAP's direct static/BCL calls bypass public hook
assurances. This is C's existing provider-documentation concern with an actual CTAP call-site trace.
