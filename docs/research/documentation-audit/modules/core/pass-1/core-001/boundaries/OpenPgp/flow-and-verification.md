# OpenPGP caller, resource and oracle tables

## Factory and call flow

| Phase / actual caller | Core path / owner | Result and limit |
| --- | --- | --- |
| Device `CreateOpenPgpSessionAsync`, extensions 37–62 | ResolveSessionTransport(SmartCard) → CreateSessionOverTransportAsync → typed open | Only SmartCard; unsupported preference fails and failed open/init does not choose HID. Grouped known-member claims are process-local evidence, not cross-process isolation. |
| Direct `OpenPgpSession.CreateAsync`, 101–111 | ConstructInitializedAsync → ValidatePreferred → Construct → ConnectionSessionGuard.Attach | Caller keeps the supplied connection; attach precedes applet wire I/O. Failed construction/init disposes/detaches only this session. |
| Initialize, 126–139 | PrepareProtocolAsync → ProtocolFactory/PcscProtocol; backend SELECT/activate/version; ConfigureProtocolAsync | Base stores protocol before setup. Detected or overridden version configures framing before optional SCP; prompt is borrowed. |
| Secure creation, 133–150 | Pcsc.InitializeScpAsync → shared ExchangeGuard → ScpInitializer → PcscProtocolScp | Applet selection/version occurs plain; application cache read occurs through effective secure graph. Failed parse/logger setup still prevents ready publication. |
| Ready, InitializeSessionAsync | Complete callback → disposal check → IsInitialized true | IsAuthenticated comes from configured SCP, not User/Admin authorization. The first secure application-data command is relevant to SCP11 a/c host-auth phase; receipt success alone has narrower meaning. |
| Raw/data/certificate/private operation | OpenPGP APDU → backend Send → Pcsc/Scp guard → command/response processors → ISmartCardConnection | Core owns one complete logical exchange, not every surrounding applet workflow, parsing, callback or persistent mutation. |
| Native built-in SmartCard | UsbSmartCardConnection → PcscConnectionNativeState worker → native transmit | Pooled receive storage is cleared, independent returned transport array survives. Actual worker/native return precedes task completion. Checked disconnect/context release precedes claim release; uncertain release retains quarantine. B1/B4 own producer/platform proof. |
| Reset | C4 → invalid VERIFY loops → E6 →44 → same backend SELECT/version →6E | Statuses/postconditions incomplete; original effective version preserved, KDF reference dropped. Existing SCP keys/graph retained across plain SELECT with no new handshake. |
| Sync session Dispose | BeginDisposal → OpenPGP Dispose(bool) → KDF clear → base protocol drain → detach/owned close | Current KDF clear precedes drain. Full workflows have no admission lease. Borrowed connection survives session disposal unless separately closed by caller. |
| Async session Dispose | BeginDisposal → DisposeAsyncCore: protocol drain then detach/owned close → finally derived Dispose(bool) | Current KDF clears after asynchronous core teardown, even if it fails. Shared DisposalGate completion is terminal state, distinct from native release proof. |

## Resources and complete lifetime scopes

| Resource | Acquired / borrowed | Last use, transfer or cleanup | Remaining condition |
| --- | --- | --- | --- |
| Hidden SmartCard connection | Device extension creates; OwnConnection after direct init succeeds | Returned session closes after protocol drain; generic helper closes failed creation | Keep returned session in await using and await every public task before leaving it. |
| Direct supplied connection | Caller-created, borrowed by direct factory/backend/protocol | Caller disposes after session teardown; idle healthy sequential rebind supported | Detachment does not repair uncertain card/SCP state. Reopen after interrupted/partial recovery failure. |
| Protocol/backend | Base session owns initial/effective protocol; backend borrows | Base sync/async drain and SCP key disposal | No backend disposal symmetry is needed; old plain protocol remains enclosed by SCP wrapper. |
| SCP03 parameters / StaticKeys | Caller owns parameter object; parameter retains exact StaticKeys owner | Keep through actual creation; session does not retain parameters | Parameter Dispose clears StaticKeys, not caller source arrays. Record cloning shares contained owners. B6-C007 owns that mechanism. |
| SCP11 parameters / keys / certificates | Parameter copies enumerable container, retains exact key/certificate objects | Dispose clears retained OCE private key and certificates, not SD public key | Keep all borrowed inputs alive through actual factory task. Parameter disposal may invalidate other aliases; trust selection and provider/profile validity remain separate. |
| Derived SCP keys | Initializer/state creates | Secure processor graph disposed after admitted exchange drain | Early construction/failure cleanup has B6's narrower guards; OpenPGP adds no repair or assurance. |
| User-presence service / intent | Application-owned service retained by session; notification captures ambient intent | Applet owns one request/resolution; application disposes service after all sessions and callback work end | Notification is UI policy evidence, never operation authorization or proof of touch/native interruption. |
| PIN / Reset Code bytes | Caller supplies borrowed ROM | Applet derives/copies and awaits actual exchange, clears owned derivatives in finally | Caller clears original bytes after complete task, including failure; an outer canceled wait is too early. |
| KDF input / cached result | Set retains exact caller object; Get returns exact cache | Current value clears during session teardown; caller retains outer cleanup for replaced/reset objects | No safe independent disposal while aliased session uses it; Process still works over cleared arrays after Dispose. |
| F9 wire/source/parsed arrays | KDF ToBytes and Core decoder allocate distinct encodings/dictionary fields | Individual TLVs and KDF backing arrays have cleanup; extra encoding, Set payload, dictionary fields and read response do not | Initial hashes are authentication data. Fix producer scopes separately from salts/public metadata and existing cache guidance. |
| Private-key input / import copies | Caller owns SDK IPrivateKey; import borrows and owns padded/reversed copies | Import Dispose clears its copies; template finally clears values/inner/TLVs; PutKey finally clears final encoding | Caller disposes private key afterward. PEM string/exported parameter arrays are additional CLI/test producers. |
| APDU source / formatted command | ApduCommand borrows source; transmitter allocates formatting | Applet source finally and Core formatted-payload finally are separate | Chaining/recovery must reach actual terminal state before borrowed memory reuse. |
| Transport / fragment / response / plaintext | Native facade returns independent array; ApduResponse clones; chaining/SCP add owners | Final response Clear or owned writable view reaches that owner only | Retain B6-C002/C006 producer cleanup issues. Plaintext may exist in prior fragments or decrypted temporary copies. |
| RSA/EdDSA signature | Successful raw response data slice | Transfers to caller after Completed resolution; caller clears actual returned storage | Status tail belongs to the underlying raw owner, while API output slice excludes it. ECDSA returns new DER and clears raw. |
| Decipher output | Applet resolves Completed then copies response.Data | Raw response always clears; caller owns copied plaintext/shared secret and clears it | Completed precedes copy allocation, not host content processing or trust verification. |
| Certificate input / Get/Attest result | Inputs borrowed; loader creates returned X509 owner | Caller using scope covers export/verification; ordinary6A88/empty return null | Attest result before outer resolution can be abandoned on callback failure; tool/test objects also have known missing scopes. Public certificate is not private material. |
| Public model dictionaries/OIDs/AID | Parsers copy; frozen OID arrays and CRTs are static metadata | Parser/model lifetime and read-only views | No automatic secret classification. HistoricalBytes remains independent of disposed outer Tlv; fingerprint parses have distinct backing arrays. |

## Whole workflow and cancellation boundaries

| Workflow | Exchanges and local stages outside one guard | Consequence |
| --- | --- | --- |
| Verify / Change / ResetPin | Optional F9 read; host KDF; credential command; optional C4 retry read | Concurrent calls can use/change KDF or device access state between exchanges. Core never allocates a PW1/PW3 workflow lease. |
| Certificate CRUD | SELECT DATA occurrence; subsequent GET/PUT | Sequential traffic can change selected occurrence between commands. Await complete public tasks. |
| Key generate/import/delete | Attributes; private operation; cache refresh | Partial persistent changes survive later cancellation/failure; refresh failure can follow actual key creation/import. |
| Sign/Decrypt/Authenticate | Host payload; optional UIF read; request callback; crypto exchange; format/resolve/transfer | Disposal can finish exchange drain while callback/result formatting is still in flight. Application lifetime must include full task and prompt. |
| Attest | Optional ATT UIF read; request; FB overwrite; certificate SELECT/read/parse; terminal callback | Certificate overwrite may precede failed retrieval/resolution. No host trust decision is supplied automatically. |
| Reset | Counter read; several VERIFY exchanges; unchecked E6/44; SELECT/version; cache read | No rollback, complete erasure proof, fresh channel setup or terminal readiness policy. |

The protocol checks the token before admitting each exchange. It passes None to all
constituent work after admission; a later workflow exchange can still observe the
original canceled token and refuse before submission. A custom SmartCard connection
can independently return cancellation despite receiving None. A native pending task,
an outer wait and the application operation task are different observations.

## Actual test oracles and bounded unexecuted proposals

| Existing source / oracle | Established by assertions if executed | Limit / bounded later proposal |
| --- | --- | --- |
| SessionWire initialization and borrowed-probe-failure | SELECT/version/6E membership; direct failed factory leaves recorder DisposeCount0 | No full activation/fallback matrix, secure setup or failed parse detachment/rebind proof. Queue exact failures and inspect claimed phase without hardware. |
| SessionWire cached-KDF and throwing-KDF | Retained salt zero after async Dispose; same connection can construct another session after throwing sync KDF | Add separately authorized held Process and async owned-close failure tests, observe exact aliases/order and drain; release barriers in finally. |
| SessionWire signature helper | Actual-array identity/zeroing and expected raw/DER bytes; resolution failure clears retained raw | Strong local owner oracle, not transport-fragment/SCP cleanup. Retain actual incoming/fragment/staging arrays in a fake producer chain, never only copied outputs. |
| SessionWire Decrypt callback failure | Propagates same exception and records Completed | No actual raw-array zero observer. Observe actual response owner and test abandoned result handling before making stronger comments. |
| SessionWire request cancel | Prompt cancels token before crypto transmission; Cancelled, no crypto APDU | No held admitted native cancellation test. Use one held exchange and distinguish late token, terminal result, public task and dispose drain. |
| InvalidPin custom status lookup | Own CTS canceled by provider; exception propagates, recorder last command VERIFY | Provider ignores passed token for this branch. It supplies no OS-native interruption proof. |
| Kdf byte-count/short-count/literal tests | Full SHA256 expected transcript or retained literal equality; unsigned count fields; SHA512 length only | Historical libgcrypt provenance is distinct. No crypto recomputation ran here. Future transition tests need distinct old/new inputs and same/fresh-session state. |
| PutKeyWire/Audit slot tests | Requested ordinary CRT and component bytes/chunk reconstruction; real named-curve scalar1 with provider admission | No ATT read or X25519 agreement; provider skips remain skips. Preserve confirmed historical failures. |
| PinManagement unverify | Signing succeeds before; after Unverify an APDU exception occurs | An Always policy could already consume authorization; this test does not explicitly control policy or demonstrate a second signature before Unverify. Use Once and an independent other-operation control if separately authorized. |
| Integration helper and body tests | Sequential reset/body borrowing, concrete metadata/crypto equality as asserted | Helper plain reset before SCP body is no SCP-reset test, leaves post-body state. Current run adds zero executions. |
| Supplemental historical serial103 | Distinct-salt/retry defect; same-key vendor X25519; correct-CRT ATT; selected touch/recovery | Exact old Linux USB/development device/source only; native causality, NFC, older families, other OS and AOT remain open. |
| Reset/version prospective check | None executed | A fake can record protected E6/44, plain SELECT and protected follow-up without claiming real SCP reset success. Device acceptance needs separately selected profile/firmware and independent state restoration. |

No new test project, runner, crypto computation or device task was started. Reuse the
existing validation foundation for later authorized checks. All meaningful README
examples retain their complete typed prerequisites, borrowed input and result scopes;
this boundary adds source review only, no new compilation credit.

## Precise shared-document disposition

`docs/usage/user-interaction.md:202–207,221–235` already preserves OpenPGP ATT scope,
UIF fallback,6600/Failed, caller-owned prompt and no-reentrant-callback guidance.
Retain it; reopen if X004 operation admission/classification or result-resolution
changes. `docs/migration/v1-to-v2.md:807–820` already distinguishes Admin/User/KDF/SCP
and broad VERIFY mapping. Retain it until X002/X005 changes those contracts.
`docs/architecture/raw-access-tiers.md:19–78` provides the independent raw diagnostic
route; B5/B6 own existing global guard/recovery wording. Typed GetData cannot recover
from an initialization failure caused by unknown mandatory algorithm data; open a
separate raw session after disposing the failed attempt. Reopen README “Open and
inspect” and model raw-inspection guidance if that practical path is expanded.

These are narrow dependency reads, outside the 328-unit denominator and outside this
worker's product edit ownership. Candidate-associated unchanged-path records name
future owners and triggers; no shared editor's work is duplicated.
