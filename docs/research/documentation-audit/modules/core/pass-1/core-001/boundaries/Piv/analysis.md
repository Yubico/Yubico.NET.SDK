# PIV caller and resource boundary

Source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Metadata checkpoint:
`da66f5d01ba594993974ddd26723e8c300c0a454`. All observations below are source/assertion
inspection. No SDK, native, cryptographic or device operation ran in this packet.

## Creation and transport call graph

```text
IYubiKey.CreatePivSessionAsync(options, token)                 Piv/IYubiKeyExtensions:38–63
  ResolveSessionTransport(preference, "PIV", SmartCard)       Core/YubiKeyConnectionExtensions:180–220
  WithPreferredConnectionType(SmartCard)                     SessionCreationOptions:71–79
  CreateSessionOverTransportAsync                            YubiKeyConnectionExtensions:246–288
    ConnectAsync<ISmartCardConnection>                       :291–303
    PivSession.CreateAsync(connection, copied options)        PivSession:102–112
      ConstructInitializedAsync                             ApplicationSession:247–270
        validate preferred type; construct; attach           :227–258
        InitializeSessionAsync                              :307–321
          PrepareProtocolAsync -> ProtocolFactory            :323–348
          SmartCardBackend.ReadInitialStateAsync             SmartCardBackend:32–41
            SELECT ApplicationIds.Piv; FD GET VERSION
          ConfigureProtocolAsync(effective version, SCP)     ApplicationSession:359–397
            PcscProtocol.Configure                          PcscProtocol:162–205
            optional InitializeScpAsync                     PcscProtocol.Scp:21–42
              ExchangeGuard -> ScpInitializer                B6 owns handshake/profile details
          rebind backend if protocol wrapped                PivSession:146–151
          ResolveManagementKeyTypeAsync                     :153–155,226–240
        publish IsInitialized only after callback succeeds
    OwnConnection() after successful direct creation         Piv/IYubiKeyExtensions:59
```

The convenience factory selects SmartCard once. An invalid HID preference throws
`ArgumentException` in the resolver; a supported preference absent from the device throws
`NotSupportedException`. A directly supplied connection's preference is a type assertion.
Neither route retries another transport after open or initialization failure. USB CCID/NFC are
SmartCard transport instances, rather than separate PIV backend choices.

The direct factory borrows the connection, binds before SELECT, and disposes/detaches the
failed session. The outer convenience helper disposes its hidden connection on failure and
preserves the original initializer exception if cleanup also fails. Native release can remain
unproven even when that original exception is the only one delivered; diagnostic logs and the
registry's unrecovered state retain their own significance.

## Creation and ownership table

| Resource | Creator / owner | PIV use and end of lifetime | Limit / reopening |
| --- | --- | --- | --- |
| Grouped physical registration | `YubiKeyDevice` acquires known member IDs before open | Built-in PC/SC owner receives the registration; successful checked release releases it | B1: process-local overlapping known IDs; disjoint scoped views and other processes remain separate |
| Direct connection | Application | PIV borrows it; dispose session before next session, then close connection in its enclosing `await using` | Detach is bookkeeping; uncertain exchanges still require connection disposal/reopen |
| Hidden convenience connection | Core helper, then returned PIV session | `OwnConnection` transfers ownership only after initialization; session disposes protocol then connection | A leaked session has no session-finalizer recovery guarantee |
| Protocol / SCP wrapper | Session preparation/configuration | Base `Protocol` is disposal owner during incomplete setup and success; backend borrows effective protocol | SCP wrapper and underlying PC/SC share one exchange guard |
| `SessionCreationOptions` | Caller | Factory reads values, convenience path creates a settings copy; session retains no options object | Referenced objects are not deep-cloned |
| `ProtocolConfiguration` | Caller | Consumed during Configure | Configuration is policy input, not a physical-firmware observation |
| SCP03 parameters / static keys | Caller owns parameter lifetime; parameter retains exact `StaticKeys` | `StaticKeys` clones input arrays; parameter Dispose disposes that contained owner | Record clones alias contained objects; keep one explicit disposal owner. Session does not dispose caller parameters |
| SCP11 parameters / certificates / OCE private key | Caller owns parameter lifetime; parameter copies list membership and retains element objects | Dispose clears/disposes the exact OCE key and certificate objects; SD public key is retained separately | Copying the list is not cloning certificates. Trust selection/provisioning and B6's P-256/AES128 profile are separate |
| Presence prompt | Application | Session retains service reference; one notification per operation; service is never disposed by session | Keep service alive through final callback and complete public operation |
| PIN/PUK/management-key inputs | Caller | Borrowed through actual awaited operation; helpers clear their encoded/working copies | `WaitAsync` timeout supplies no right to clear the original operation's live input |
| Imported `IPrivateKey` | Caller | Helper borrows key/parameter arrays while encoding; clears private encodings and appended predecessors | Keep key alive and unchanged through import; caller disposes concrete disposable private-key owners and separately clears exported source material |
| Crypto/raw-object/temporary-PIN output | PIV helper until successful transfer, then caller | Mutable `Memory<byte>` at SDK boundary; clear caller-owned sensitive output after use | Session or connection disposal leaves already transferred output alone |
| Returned certificate / extracted RSA/ECDsa | Caller | Independent disposable host object | Retrieval is not attestation trust or key binding; preserve existing F9 material |
| Metadata / CHUID / CCC / ADMIN / KeyHistory | Public/nonsecret model storage | Decoders copy; CHUID/CCC/Create and ADMIN salt construction can borrow supplied memory | Public metadata needs ownership accuracy, not automatic secret classification |

## Authentication and authority

| Operation / state | Session responsibility | Card / application responsibility | Actual code and assertion context |
| --- | --- | --- | --- |
| `IsAuthenticated` | Records successful protocol/SCP setup; false on disposal admission | SCP variant and device phase determine actual authentication/authority | `ApplicationSession:380–394`; SCP11b device authentication grants no PIV administration permission |
| `IsManagementKeyAuthenticated` | Local observed PIV management success, hidden once disposal begins | Card owns current administration security status | `PivSession:73,293–304,907–914`; management and SessionConsistency tests cover failed/pre-cancel attempts and SET transitions |
| `AuthenticateAsync` | Clears prior recorded success before token check; two GENERAL AUTHENTICATE exchanges, witness/challenge verification, then records true | Card must validate witness; response mismatch fails locally | `AuthenticationProtocol:101–253`; mixed recovery fake validates submitted witness at second stage, not only canned success |
| PIN verify/change/unblock | Validates operation-specific byte lengths; maps retries/status; no public verified-PIN boolean | Card owns PIN state and key PIN policy | `AuthenticationProtocol:423–573,630–703`; PIN retry tests distinguish metadata and no-data VERIFY |
| SET management key | Requires management flag; successful SET updates type and preserves flag; 6982 clears it, unrelated status preserves it | Device applies key/type/touch policy; external changes can invalidate local observation | `PivSession:784–813`; public key-generation follow-up in ManagementKeyTests is stronger than flag alone |
| Privileged writes / key operations | Explicit local management check; key/policy gates at facade | Device still enforces PIN, touch, slot, product/profile and occupancy requirements | `PivSession:370–441,580–721,943–984`; fake reflected authentication is not a real authentication proof |
| Retry limits | Management prerequisite checked locally | Verified PIN prerequisite is card-enforced; successful command restores PIN/PUK defaults | `AuthenticationProtocol:753–779`; integration test actually attempts refusal without PIN and verifies changed/default credentials |
| PIN-only detection | Reads ADMIN records only; leaves authentication alone | ADMIN is software state, not independent active-key/PRINTED attestation | `PinOnlyProtocol:40–65` |
| Protected enable | Reauthenticates supplied active key, verifies PIN, stores PRINTED, blocks PUK, reads/preserves legacy ADMIN salt and writes flags | Device mutates each step separately | `PivSession:1077–1093`; WorkflowBackend records exact command order and selected failures |
| Recovery | PRINTED read; only 6982 triggers one PIN verify/re-read; protected candidate, optional derived candidate, restore earlier protected success after 6982 rejection | Actual active key determines which candidate authenticates | `PivSession:986–1074`; cancellation/unrelated errors propagate without restoration traffic; no candidate can preserve prior auth |
| Disable | Read ADMIN mode; replace active algorithm's default before deleting PRINTED then ADMIN | Writes are separate; PUK remains blocked | `PivSession:1096–1119`; default-array alias preservation and failure stopping counts are asserted |

The historical `piv-001:PIVCORE-001` (also described as PIV-CORE-001 in dispatch prose)
and `audit-v2-20261022-20261031:#22` / YESDK-1630 retain the same portable policy question.
Strict Exclusive mitigates competing PC/SC clients; explicit Shared/fallback accepts coexistence.
The NIST application-security-state rule includes deselection. It supplies neither a per-handle
PIN isolation guarantee nor a universal “until reset” lifetime. Borrowed-session disposal closes
no card handle. Owned connection cleanup requests UNPOWER and checks native release statuses;
neither the request nor a fake assertion establishes observed physical power or PIN state.

## Failure, cancellation, disposal and reset

| Boundary | Source outcome | Safe caller task / qualification |
| --- | --- | --- |
| Pre-entry cancellation | Session/guard rejects before admitted APDU; Authenticate intentionally clears prior management success | Handle the specific failed operation, then decide retry; do not infer a device mutation |
| After exchange admission | `ExchangeGuard.RunAsync` passes None through all constituent transmits | Await actual exchange terminal completion. PIV has no CTAPHID_CANCEL or proven touch-timeout status |
| Between authentication APDUs | Each `backend.SendAsync` is a separate guarded exchange | Another public workflow/disposal may intervene; await and serialize the complete authentication workflow |
| Between PRINTED/PIN/ADMIN/default-key writes | No workflow lease or rollback | Preserve backups; inspect actual active key/objects/retries after failure before choosing recovery |
| Presence request | Policy-derived request just before crypto APDU, outside its exchange guard; callback failure prevents APDU | Keep prompt/input/session lifetime through callback; a callback that disposes session may cause later protocol refusal |
| Presence success | Parse crypto response, resolve Completed with None, then transfer output | Parsed completion is before RSA unpadding; local padding failure can follow Completed |
| Presence malformed response / device failure | Fallback resolution Failed; primary error preserved if resolution also throws | A successfully requested notification resolves once; failed request gets no matching resolution |
| Cancellation after successful request, before APDU admission | Guard sees canceled token; fallback resolves Cancelled | TouchNotificationTests cancels from request callback: this is pre-wire cancellation, not native abort |
| Cancellation after crypto APDU admission | Exchange ignores token until terminal response; success may still be returned | Source permits raced success; token alone gives no native deadline, drain or recovery proof |
| Session disposal | Readiness/protocol-auth and public management flag become false immediately; protocol drains admitted exchange; detach; close only owned connection | Session drain is narrower than complete helper parsing, callbacks and local crypto processing |
| Native PC/SC cleanup | Same owner checks disconnect and context-release success, then releases exact registration/root | Error completion without release proof retains quarantine. Check B1/B4 native artifact qualifications |
| PIV RESET | PIN blocking, PUK blocking, FB RESET, clear management flag, conservative default then metadata refresh | Retry consumption may precede cancellation/failure; refresh can fail after applied reset. No terminal session disposal/reselect/rehandshake is performed here |
| Fresh reopen | New convenience factory reselects/probes/configures and discovers management type | Reopen alone is not proof of PIN isolation, successful SCP reauthentication, restoration, touch recovery or physical release |

PIV's request callback occurs before the crypto exchange and its resolution callback occurs after
that exchange has left the guard. This differs from OTP's callback-inside-guard self-wait path.
PIV supplies no whole-workflow disposal lease: a held resolution callback or RSA unpadding can
outlive protocol drain. The implementation/policy question remains B5-C006/PIVCORE-002, with
this packet adding concrete PIV caller sites rather than another independent defect count.

## Memory and wire ownership

| Actual PIV caller | Copy / clear / transfer sequence | Scope of source/test evidence |
| --- | --- | --- |
| Management authentication | Pool key copy; witness `ApduResponse` -> disposable outer/inner TLVs -> witness copy; clear response; clear intermediate witness/challenge/encoded data in finally | Mixed recovery capture retains actual command/response memory; fake validates witness; no provider erasure or native staging proof |
| Sign/raw RSA/agreement | Own encoded command; Request; send; status; `ParseCryptoResponse` uses two disposable TLVs; copy parsed value; resolve; transfer; finally clear response/command | CapturingBackend retains actual response owner and command array; resolution-failure helper directly checks parsed array zeroing |
| High-level decrypt | Same raw operation, Completed before transfer to local unpadding; temporary dummy RSA object; ModPow re-wrap; clear raw and reencrypted arrays | BigInteger/provider hidden copies and output-size/content admission remain PIVIMP-007/C/B6 qualifications |
| Raw GET | Response -> wrapper TLV or independent raw copy -> caller result; clear original response in finally | Wrapped/unwrapped tests inspect exact response storage and retained result |
| PRINTED recovery | GET copy -> TryDecode takes zeroing ownership of raw bytes -> separate extracted key -> outer workflow finally clears key | Fixed-shaped direct TLV path avoids dictionary secret copies; wrong/malformed shapes clear input too |
| PUT / import | Borrow caller source/key; own encoded data; clear encoded data on send failure/success; append helper clears replaced private encodings | Local tests assert transmitted sentinel then actual owner zeroing; source key remains borrowed |
| Bio temporary PIN | VERIFY response copied into returned array, original Clear; temporary-PIN request encoding cleared | Fake fixtures establish mode bytes and memory behavior, not Bio hardware enrollment/ceremony |
| Metadata / generated public keys / certificates | Dictionary and independent raw/public copies, or host certificate object | These are ordinarily public metadata/public keys/certificates. Missing secret zeroing is not inferred merely from response direction |

`ApduResponse.Clear` zeroes only its owned copied bytes, including SW. It leaves constructor
source, Core transport staging, previous chained fragments and SCP decrypted temporaries under
their separate owners. `Tlv.Value` aliases its TLV owner; copying out before Dispose is essential.
PIV's cleared owners do not close B6-C002's underlying-copy question. Generic TLV dictionary/
TryFindValue residue remains C-C005/C006; public RSA/certificate metadata callers do not turn
that generic mechanism into an additional PIV secret leak. The explicit secret PRINTED path
uses scoped TLVs and is already fixed for its historical scope.

PIV reaches B6's empty-command and continuation mechanisms through FD/F7/F8, SET RETRIES,
RESET, move/delete/attestation and large object/key/certificate traffic. Actual protected PIV
callers therefore require B6-C011/C012 qualification; a plain-PIV hardware pass supplies no
SCP wire acceptance. `KeyProtocol:395`'s “Case 1 … no Le” comment should name actual formatter
behavior without asserting an unavailable ISO clause. Core formatters append Le; no 6C retry
or universal malformed-response recovery follows from the PIV helpers.

## Effective version and profile provenance

| Value / gate | Actual producer and use | Evidence limit |
| --- | --- | --- |
| Detected PIV version | SELECT then FD; first three bytes, at least three required | Raw applet observation, historically 00 00 01 on development keys |
| Session FirmwareVersion | Override when supplied, otherwise detected version | Effective Configure/Feature policy; not Management metadata or independently identified hardware firmware |
| `_detectedFirmwareVersion` | Saved before applying override | Initialization/reset default-key fallback uses this value; metadata still wins |
| Major-zero | Core `IsAtLeast`/Feature optimistic compatibility policy | The value does not establish a physical minimum 5.8.0; development tests' causal prose needs narrowing |
| PIV gates | P384/policies 4.0; cached/attest 4.3; serial 5.0; metadata 5.3; AES 5.4; move/delete/25519/large RSA 5.7 | SDK model; AES released-device 5.4.2 and Python serial 5.0.3 differences remain PIVIMP-003 |
| ROCA/FIPS | Generation exclusion 4.2.6–4.3.4; import independent; 4.4.x RSA1024/Never restriction | Product/profile support and modern FIPS/NFC/Bio qualifications are separate from Core catalog availability |
| Management fallback | Detected >=5.7 or major-zero -> AES192; earlier released -> TripleDes | Only NotSupported/6A88 fallback; malformed/unrelated errors propagate. Override does not redefine installed reset default |
| SCP | Core gates and caller-supplied key/reference/profile, after PIV FD and effective Configure | Borrowed SCP material; source setup flag is not PIV management/PIN authorization or full hardware conformance |

## Family mechanism routing

| Owner | PIV sites / consequence | Counting / gap |
| --- | --- | --- |
| B1 C001 / #22 | Convenience/direct lifetimes; PinPolicy.Once; AuditV2 isolation test | Preserve application deselection, requested UNPOWER, checked release and physical PIN-state distinctions |
| B4 C006 / Q001 | Every PIV SmartCard backend send reaches common PC/SC native imports/PCI carrier | Published producer/header layout differs by platform; exact installed producer/header correspondence remains unverified |
| B4 C001/C007 | Discovery can enumerate HID before SmartCard selection; PIV applet itself selects no HID backend | Keep pinned Windows compatibility shortcomings with their owner; no claim that PIV uses a FIDO cancellation path |
| B5 C001/C004/C006 | Factory readiness, borrowed inputs, auth/recovery/reset, callbacks and local processing | Actual PIV sites extend shared mechanism, zero duplicate bug credit |
| B6 C001/C002/C007/C009–C012 | Le/chaining, raw response copies, parameter aliases, SCP profile/phase | Exact public-review/final/ISO gaps remain; plain PIV evidence is not protected PIV qualification |
| C C001–C007/C011–C013/C020 | PinPrompt reader options, copied owner creation, TLV/private-key/BigInteger/provider scopes | UI digit filter is local policy; generic reader lengths are UTF-16 units. SDK byte checks/profile enforcement remain PIV responsibilities |
| A | Shared selector discovers once and filters SmartCard; independent Management reads occur before PIV sessions | Last-known selection metadata, fixture binding, flagged form factor/FIPS decoding and known-member grouping retain A's qualifications |

This completes the bounded source investigation. Editing, independent content/citation review,
clean sampling, compilation and platform/device qualification remain separate gates. The user-ready
Windows CCID/HID work is a future delta handoff, outside pinned B4 and this packet. PIV physical
touch, hotplug and touch-dependent cancellation/recovery remain DEFERRED.
