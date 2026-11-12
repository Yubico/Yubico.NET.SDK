# WebAuthn/Core caller and ownership traces

Source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. This is the dedicated indirect boundary,
not a second whole-WebAuthn audit. Exact selected units and candidate-file outcomes are in the JSON
ledgers. Contextual dependencies keep their Core/CTAP2 owners.

## Direct and indirect call graph

```text
IYubiKey.CreateWebAuthnClientAsync (WebAuthn IYubiKeyExtensions:60–82)
  validate origin/options and force RpIdValidator getter before opening
  -> Ctap2.CreateCtap2SessionAsync (:140–170)
     -> Core ResolveSessionTransport / CreateSessionOverTransportAsync
     -> one typed ConnectAsync<IFidoHidConnection|ISmartCardConnection>
     -> Ctap2Session.CreateAsync -> ApplicationSession.ConstructInitializedAsync
        attach before I/O -> PrepareProtocolAsync -> ProtocolFactory
        HID: Core broadcast INIT -> correlated channel -> CTAP GetInfo
        SmartCard: SELECT ApplicationIds.Fido -> plain CTAP GetInfo
        detected firmware gate -> ConfigureProtocolAsync(override ?? detected, configuration, SCP)
        optional SCP wrapper -> CTAP backend rebind -> complete callback -> readiness
     -> OwnConnection after successful initialization
  -> new WebAuthnClient -> new WebAuthnBackend adopts session

MakeCredentialAsync / GetAssertionAsync
  validate options -> synchronous application RP validator (unchanged RP text)
  -> CeremonyDeadline.Start -> clientDataJSON/hash -> backend GetCachedInfoAsync
  -> extension permission union -> UvDecision -> CeremonyAuth token acquisition
  -> new extension session for each attempt -> optional PRF key agreement
  -> silent credential probes -> registration token re-mint where applicable
  -> final CTAP make/get -> eager GetNextAssertion collection where count requires
  -> extension outputs/storage and result construction, outside escalation catch
  -> clear ceremony auth/tag/prompt owner and processor shared secret
  -> return result / matches -> SelectAsync returns cached result, no device call
```

The shipping WebAuthn source directly imports Core only for `IYubiKey`, `ConnectionType`,
`SessionCreationOptions`, credential-prompt/context/kind, and the open `CoseAlgorithmIdentifier`
conversion in registration. Core's pooled owner is used by public prompt examples and test helpers.
`IUserPresencePrompt` and `UserPresenceIntent` reach ceremonies through the CTAP2 session, not a second
WebAuthn notification owner. There are no shipping WebAuthn logger calls, direct native calls, Core
catalog lookups, Core console-reader calls, raw-session calls, or discovery-metadata reads. Logging,
reader, cryptography and metadata statements in module guidance describe shared facilities or examples;
they do not establish adoption in every WebAuthn operation.

Indirect dependencies are substantial: CTAP request/extension builders and DTO decoders; ClientPin and
PIN/UV protocol 2; large-blob storage; CTAP COSE/AAGUID conversion; Core session/guard/transport/native
owners; APDU/SCP when SmartCard is selected. Experimental offline ARKG is reachable through CTAP helpers
used by the integration test's explicit bridge, not through an implicit WebAuthn ceremony derivation.
The bridge invokes a nonpublic constructor by reflection and is not a consumer SDK helper.

## Ownership table

| Resource | Current owner / borrow | End or transfer and consequence |
| --- | --- | --- |
| Factory-created connection | CTAP session owns Core-opened connection | Client → backend → session disposal drains admitted protocol exchange, detaches, then disposes connection. Actual native release follows its owner. |
| Caller-created connection | Caller; direct CTAP session borrows it | Handing that session to WebAuthn transfers session disposal, not connection ownership. Keep the connection's outer `await using` scope. |
| Supplied CTAP session | Caller until successful public client construction; backend afterward | Failed constructor leaves caller ownership. Factory catches client-construction failure and awaits session disposal; a disposal failure can replace that exception in this outer wrapper. |
| Backend PIN protocol | Backend lazily creates V2; token/shared-secret helpers borrow it | Backend disposes protocol before awaiting session disposal. Temporary `ClientPin` instances intentionally are not disposed: their Dispose would dispose this shared protocol. No whole-ceremony disposal gate exists. |
| Options, descriptor IDs, user IDs, PRF inputs, blob writes, signing memory | Caller retains backing storage; records/read-only views are not universal snapshots | Keep data unchanged through the actual ceremony, including probes, retries and output storage. List/property immutability does not make referenced arrays immutable. |
| Credential prompt service | Application; client retains reference | Service is not disposed by client. Its successful result transfers the exact-length `IMemoryOwner<byte>` to WebAuthn. |
| Prompted PIN owner | Prompt until successful task result; then WebAuthn | Rejection clears/disposes before next prompt; accepted PIN stays live for ceremony re-mint, then clears in CeremonyAuth.Dispose. Late successful result after wait cancellation is cleared/disposed by detached cleanup when it arrives. |
| Direct PIN memory | Caller, borrowed | WebAuthn leaves it unchanged and does not substitute prompt retry after rejection. Clear only after the actual operation has finished its last use. |
| PIN token and auth tag | TokenSession adopts returned plaintext array; CeremonyAuth owns last tag | Old token clears on acquisition/escalation/re-mint, final token/tag clear on disposal. Backend separately copies tag into CTAP options and clears that copy after its awaited call. This is scoped ownership, not proof of every provider/transport copy's erasure. |
| PRF key agreement | Per-attempt PinUvSharedSecret owns derived secret; keyAgreement is public metadata | Processor Dispose clears actual secret array. Failed Encapsulate/provider intermediates retain CTAP/C cleanup qualifications. |
| Transport/APDU/CTAP responses | Success transfers views/copies at several layers | Core report array, HID accumulation, raw PC/SC result, sealed ApduResponse clone, secure intermediates and typed arrays are distinct. Disposing client/session clears none of the successful WebAuthn results. |
| Attestation | Public Decode borrows original outer CBOR/statement slices; authData separately materialized | Input mutation can diverge from parsed fields (WA-IMP-006). Create allocates outer CBOR and retains component objects. Encode rebuilds modeled fields; unknown outer envelope is not preserved. |
| WebAuthn authData | CTAP AuthenticatorData.Parse makes another raw/field/ED copy; ParsedExtensions slices that ED storage | Clearing original CTAP input reaches neither independent copy. Parse is not signature, trust or RP validation. |
| User/credential result | CTAP decoder materializes ID arrays and optional name/displayName strings; WebAuthn passes the entity through | MatchedCredential and selected AuthenticationResponse reference the same User object. CTAP UserSelected is not copied into a public picker contract; absent/count interpretation remains lower-layer policy. |
| PRF/blob outputs | SDK-created arrays exposed through nondisposable result records | Aggregate PRF plaintext clears, range temporaries/results survive; blob plaintext and decoded credBlob have separate owners. Read-only output provides no supported caller clearing API. |
| Registration transport hints | Independently allocated read-only sorted/deduplicated snapshot from cached GetInfo | Unknown strings survive; it does not select or attest current host transport. This snapshot contract must not be generalized to all response memory. |
| SCP parameters/source keys/certificates | Caller owns creation parameters and their contained owners | Keep parameters outside session scope. Core derives separate session keys. Record cloning shares contained owners; B6 retains actual initialization/error cleanup and profile qualifications. |

## Creation, failure, cancellation, retry and reset

| Stage or event | Actual trace | Supported interpretation / action |
| --- | --- | --- |
| Default creation | HID FIDO when exposed, else SmartCard; supplied SCP with no override selects SmartCard | This is selection before open, not failover. Explicit HID+SCP is rejected during preparation before INIT/GetInfo. Neither open nor initialization failure tries another transport. |
| Physical admission | Core claims overlapping known member IDs before registered open | Process-local SDK scope; disjoint discoveries/direct opens/external processes retain coordination needs. A's partial grouping/evidence caveats remain. |
| Failure during CTAP creation | Core owns registered protocol cleanup; convenience helper awaits opened-connection disposal | Caller direct connection remains borrowed. Failed release may retain/quarantine physical claim. Session detachment alone proves no wire recovery. |
| Initial ceremony | First backend GetCachedInfo calls fresh CTAP GetInfo, even though CTAP factory already obtained one | Cache is lazy and backend-local; it is not IYubiKey.CachedDeviceInfo and does not refresh CTAP FirmwareVersion. No reset invalidation entry point exists on WebAuthn. |
| PIN rejection | Prompt path retries only PinInvalid, within MaxPromptAttempts; refresh retries after rejection; null declines | PinBlocked/PinAuthBlocked/PinAuthInvalid stop. Refresh errors become unknown count, except observed caller cancellation. Device retry state is independent of SDK cap. |
| Registration probe | up:false, zero hash; token optional; shrink RequestTooLarge at same offset; advance NoCredentials | Cache found exclude descriptor or empty list across UV escalation. If token exists, re-mint MakeCredential permission before final command. This is no general transport retry. |
| Authentication probe | Select recognized descriptor; miss sends dummy 00 descriptor in final command | Per-credential extension inputs use selected descriptor. Timing/prompt equivalence is unproven. Final request is singleton even when original list had many entries. |
| PUAT_REQUIRED | Attempt catch changes preference to Required once and creates fresh processors | A second such failure maps to error. GetInfo/token acquisition are outside this catch. Probe CTAP failures receive their own Unknown wrapper, including PUAT_REQUIRED. |
| Final response descriptor absent | Filter recovers singleton probe from chunk[0]; Matcher and result builder independently use final Credential?.Id ?? Empty | WA-IMP-010 remains. Carrying selected ID into request does not carry it into result. An original singleton supports application-side association; an original multi-entry list does not expose the selected descriptor to the caller. |
| Multiple discoverable assertions | First count N then N−1 GetNext calls, all before output work/account picker | Core guards each exchange only. Caller serializes whole ceremony; raw/intervening session use can invalidate continuation. No response-count reservation or transaction is created. |
| Deadline | Starts after options validation and RP callback, links caller/timer tokens | Expiry only wraps a resulting OperationCanceledException when caller token is not canceled. Successful result can win. No default, clamp, elapsed completion bound, side-effect rollback or native abort follows. |
| Prompt wait canceled | WaitAsync can end before prompt task; late result observer remains | Prompt must return promptly and honor token. A never-completing prompt is not drained by client disposal; keep application prompt resources alive for actual outstanding work. |
| HID cancellation | WebAuthn token → CTAP backend → Core, constituent read uses None; observed keepalive triggers at most one CANCEL; original terminal response drains | CANCEL has no own acknowledgement. No report/terminal result can leave operation pending. Malformed/partial response recovery is B5's separate question. Dispose/reopen after uncertainty; do not promise universal reuse. |
| SmartCard cancellation | Accepted APDU completes under Core guard; short/extended/chained/SCP route remains same native worker | No in-flight cancellation deadline. Status/error is handled before CTAP status. Core APDU-error response may never become reachable to backend cleanup. |
| Output/storage failure | Final assertion/creation has happened; outputs outside escalation catch | No creation retry or rollback. Blob write catches CTAP/InvalidOperation into Written=false; read CTAP errors propagate, selected parse/InvalidOperation errors can omit output. Cancellation propagates. |
| Client DisposeAsync during ceremony | Client bool changes only after backend await; backend disposes protocol first, then CTAP session; no operation counter/join | Core drains one admitted exchange only. Follow-up token crypto, parsing, presence resolution or blob calls can race teardown. Fully await ceremonies before outer disposal; a future shared lifetime design is separate engineering. |
| Reset | WebAuthn exposes no reset; separately retained session reference could call CTAP Reset, which sends command but does not refresh backend info/token state | That reference is no longer caller-owned after client adoption. Dispose client before administration; after approved reset start a fresh session/client and re-read state. No hidden automatic reset recovery is implied. |

## Native paths and version provenance

HID chooses the pinned Core slot: built-in Linux FIDO owner uses its worker and read reactor; macOS
uses persistent native input/worker/ack-drain; Windows/custom uses compatibility adapters. Pinned
Windows opens and ReadFile/WriteFile execute synchronously with null OVERLAPPED and ignore adapter
tokens. A task-returning ceremony can therefore block its invoking thread. B4's raw concurrent-Dispose
registry/SafeHandle gap retains its exact path; it is not a claim that correctly sequenced session
disposal always releases early. Linux pending receive cancellation detaches a reader, while Core's
admitted protocol read deliberately passes None. Native terminal wake, device CANCEL, task terminal,
true quiescence, checked native release and permitted reopen are separate events.

SmartCard follows UsbSmartCardConnection → PcscConnectionNativeState worker → PcscNativeResources →
NativeSCardConnectionApi → SCard.Interop → NativeShims. Core sends CLA 80/INS 10/P1=P2=0/Le=0,
formats/chains, handles APDU status, and optionally wraps secure commands. B6's command/response copies,
61xx handling, no 6C resend, empty protected remaining command, uncertain first fragment, counter/phase
and SCP setup qualifications apply indirectly. SCP establishment has no protected CTAP acceptance probe
before ready publication. No new FIDO-over-SCP or native producer qualification follows from this trace.

Core HID INIT vendor version, CTAP GetInfo firmware (packed low 24 bits or missing → 0.0.0), effective
override/configuration, Management live firmware and discovery's last-known metadata are separate.
The USB SmartCard 5.8 gate uses detected GetInfo before override; NFC is the actual connection's Transport.
Major zero remains optimistic compatibility policy. ForceShortApdus does not govern earlier SELECT/GetInfo,
and B6 confirms major-zero configuration returns before its ordinary handling. Backend GetInfo cache may
be stale after external state change; ClientPin's permission-token path performs another fresh GetInfo.
None of these observations negotiates a CTAP edition, protocol 1, provider support or extension support.

## Crypto and profile boundaries

Registration casts requested CTAP CoseAlgorithm numeric value to Core's open CoseAlgorithmIdentifier.
It does not consult the Core catalog or choose a provider based on catalog applicability. CTAP PIN/UV
uses BCL P-256 ECDH/AES/HMAC/HKDF directly; generic catalog support cannot validate peer kty/crv/alg or
replace missing CTAP PIN Unicode/profile checks. WebAuthn prompt context supplies minimum code points
and maximum UTF-8 bytes but performs no such validation. Core ConsoleCredentialReader uses UTF-16 char
counts and configurable encoding, with redirected immutable-string limitations; an application adapter
must decide how to fulfill the prompt/profile contract without silently changing an existing PIN.

Experimental previewSign translates to the canonical CTAP encoder. Generated key algorithm, seed
algorithm and derived verification algorithm are distinct. CTAP accessor propagation/limited DER and
ARKG draft-10 wire questions remain CTAP-owned; Core draft-09 math/provider/native cleanup remains C-owned.
The integration bridge passes seed.Algorithm, so its successful offline VerifySignature assertion would
not settle either accessor's algorithm propagation. No crypto operation or example compilation ran here.
