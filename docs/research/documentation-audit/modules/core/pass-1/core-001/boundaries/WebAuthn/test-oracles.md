# Actual assertions and meaningful unexecuted verification

All statements below describe source assertions. This packet ran **zero SDK builds/tests, formatters,
native/device commands or cryptographic experiments**. Independent review, compilation and execution
remain later coordinator assignments. Existing 4,442 integration unit executions / 197 overlapping
reruns and Linux native-only evidence remain historical qualified inputs, not new ceremony acceptance.

| Current test/helper | Actual assertion and oracle limit |
| --- | --- |
| IYubiKeyExtensionsTransportTests | SelectionProbeYubiKey records requested generic connection type and throws immediately. Asserts pre-connect validation, default exposed-order, explicit selection and SCP implication. It opens no connection, initializes no CTAP session and proves no native failure/reopen path. |
| WebAuthnClientConstructionTests / MakeCredential_BackendDisposed | Substitute receives one DisposeAsync in sequential test; rejected origin/options construction leaves supplied session undisposed. No admitted-ceremony concurrent disposal, shared outcome or native drain observer. |
| RegistrationTransportTests | GetInfo metadata snapshot is ordinal-sorted/deduplicated; mutating source list leaves response unchanged; read-only list rejects mutation; absent data defaults empty. This is one specific owned collection, not an all-results snapshot or host reachability test. |
| CredentialPromptRetryTests | Scripted prompt/backend compare supplied ASCII PINs, refresh IsRetry/count; TrackingMemoryOwner.Dispose only counts/signals, so zero bytes observed before next prompt and after late completion come from production cleanup. Context min=8/max=63 is forwarded even with six-byte accepted fake PIN: metadata is not enforcement. Never-completing prompt is not drained; cancellation test deliberately completes late result. |
| PinUvAuthTokenSessionTests and TokenBufferAssert | Sentinel array handed to actual owner becomes zero after Dispose; repeated Dispose preserves zero and getter rejects. NotZeroed checks any nonzero byte, not full exact sentinel equality. Does not observe transport/provider copies. |
| WebAuthnBackendTests | Captures actual CTAP options owner and in-call snapshot; successful call leaves caller param live and backend copy zero. Encapsulate uses a real P-256 key when run, checks length/nonzero/property refusal; pre-disposal copied bytes stay nonzero and do not observe actual shared-owner zeroing (WA-IMP-008). No execution here. |
| UserPresenceIntentTests | Real Ctap2Session/SmartCard/Core protocol with fake connection; SELECT/GetInfo succeed, make times out. Request callback sees intent, Ctap2.MakeCredential and RP scope; no resolution recorder, native stack, HID keepalive or physical gesture oracle. |
| CredentialFilterTests | Empty/no-match, exact selected descriptor, request-too-large sizes 3→2→1→1→1, too-long-ID skip and no-token auth fields. Success singleton returns chunk[0]. Normal final-response fakes include descriptors, so final omission recovery remains untested. |
| CeremonyFrameTests | PUAT_REQUIRED twice gives two attempts then mapped error; exclude probe with/without token narrows/caches and re-mints mc after mc|ga; probe failure gets Unknown with no creation. CredProps after escalation proves output exists/effective false, not an independently observed processor identity. Re-mint test asserts permission sequence under fake; its 5.8 rationale is no new hardware evidence. |
| GetAssertion selection tests | Repeated Select compares credential/signature bytes and checks one backend call; production source also returns same cached object, but assertion itself is value equality. Internal incomplete factory test checks canceled wait then later success, not cancellation of authenticator work. |
| Audit timeout tests | Hanging backend uses cancellation-aware wait; expects timeout wrapper versus caller OCE, null no-timer token, unexpired success and pending prompt signal. No native cancellation or hard-deadline/release oracle. |
| ExtensionSession/PRF tests | Encoded extension keys/values/order, unsupported omission, optional outputs and published PRF vector transforms through selected real protocol codec paths. Version=2/tagLength=16 TestPinUvAuthProtocol is a noncrypto sentinel, not protocol-2 cryptographic correctness. No verified-PRF UV enforcement follows. |
| LargeBlobProcessorTests / audit blob tests | Mock storage write CtapException becomes Written=false; synthetic registration response-key support paths. Tests with null original allow-list do not establish WebAuthn write permission. Read cancellation/output-presence and real shared-array atomicity are separate. |
| CoseKeyTests / fixture helpers | Unknown kty retains RawCbor; ES256 fixture yields EC2/crv1/X32/Y32. Pinned FluentAssertions 7.0.0 compares byte collections in exact order, including BeEquivalentTo; provider-valid EC/RSA points are not established by structural lengths. |
| PreviewSignCodecTests | Extract inner generated ID/public key/attestation and outer selected algorithm. Fixture contains extra inner algorithm and tolerated outer flags; minimal flags-only inner case remains WA-IMP-020. No trust verification or vendor variant inference. |
| WebAuthnSurfaceTests | Reflection checks internal backend visibility/public constructors. It verifies API shape, not ceremony security, Csc analyzer participation or a consumer compilation. |
| Hardware basic/full ceremony tests | Assert nonempty credential/signature/raw bytes/objects, AAGUID or matching discoverable User as written. They do not independently verify standard assertion/registration with an RP. PIN normalization may set/change/consume retries; client/session double using relies on idempotent sequential CTAP disposal. |
| Hardware no-touch timeout repro | Expects five-second cooperative timeout within two-second slack, then same-session GetInfo. This is a future human-coordinated scenario and one transport/state-specific reuse assertion, not guaranteed HID/native deadline. |
| Hardware PRF/largeBlob | Stable 32-byte repeat output; two-credential PRF checks returned ID equals held ID and equal selected/default input output; actual UV flags and differing outputs test a nonconforming current behavior subject to device policy. Blob registers/writes/reads exact bytes. No cleanup/foreign-client transaction qualification. |
| PreviewSign hardware full/dual | Full test derives ARKG key via reflection bridge and verifies extension signature against raw message. It does not verify standard assertion or generated attestation trust. Dual test chooses second key for every non-first returned ID without membership assertion (WA-IMP-021). All are unrun here. |
| Exclude stress / WebAuthnTestHelpers | Seventeen persistent credentials; setup/cleanup delete all credentials for test RP; capacity/PIN tokens and inline PIN arrays lack comprehensive failure clearing. Cleanup swallows errors, so no restoration proof. NormalizePinAsync has fixed known PIN/no token parameter and can change state. Existence of slow/UP traits is no read-only guarantee. |

## Coordinator proposals — unexecuted

Use the repository's current build/test skills and one serialized toolchain run per checkout after
separate assignment. Put all scratch/output under
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/BOUNDARY-WebAuthn`; propagate TMPDIR/TMP/TEMP.
No new helper API, fake example compilation or all-suite success is asserted here.

1. **Descriptor association:** feed CTAP2.0-shaped final omission after successful original-singleton
   and narrowed two-entry probes; inspect both MatchedCredential.Id and selected CredentialId plus
   selected PRF/preview parameters. Include no-list omission and later-edition mismatch as separate
   policy controls. Do not recover from the first of a multi-entry original list.
2. **Whole ceremony versus disposal:** deterministically hold token GetInfo/key agreement, the gap
   before GetNext, output parsing and blob storage separately; admit client disposal at each point.
   Observe protocol disposed flag, actual operation task, session attachment/claim and cleanup order.
   Add prompt callback/reentrant disposal and held terminal callback separately. Release gates in
   finally and await actual drain before disposing test primitives; a watchdog timeout is no SDK drain.
3. **PIN owner/profile:** observe rejected/accepted/re-mint/late result actual backing arrays and exact
   logical memory length, including faults. Separately test strict UTF-8, surrogate/code-point/UTF-16,
   NFC, final NUL and device-minimum policy under selected enforcement owner. Preserve existing
   credentials' exact bytes; no normalization experiment against hardware is proposed.
4. **Cleanup:** retain actual shared-secret view before Dispose; observe source/transport/typed/PRF
   result arrays at each transfer and exception. Inject provider/output/callback/logger failure only
   under a defined fake seam; never infer native/provider erasure from a second caller copy.
5. **Retry and side effects:** output-phase CTAP failure must not resend makeCredential; cancellation
   or terminal callback failure after successful creation retains side effect. Compare direct PIN
   rejection (one attempt) with prompt retry, escalation and registration re-mint. Test cached ceremony
   GetInfo versus ClientPin's fresh GetInfo without treating stale state as firmware mutation.
6. **Native actor decision:** once maintainer names output boundary, assert exact User handle/names
   retention at CTAP parser, native picker result and any new browser/RP serializer separately. Keep
   UV=false authenticator violation distinct from platform post-selection filtering. Do not strip now.

## Physical and late-platform matrix

| Later lane | Prerequisite and observations | Bound |
| --- | --- | --- |
| HID registration/authentication | Human-approved exact fixture/PIN/state; direct and prompted PIN, UP/UV required/preferred, exclusion/selection and chosen extensions | Record RP/offline verification separately from parsing and nonempty bytes. No execution here. |
| HID cancellation/touch race/removal | Human physically present, exact command and timing approved | Observe keepalive, cancel request, original terminal response, public task, drain and checked release/reopen separately. |
| Built-in UV | Capable configured fixture with applicable token capability/protocol | Separate biometric interaction from SDK touch prompt; no generic UV guarantee. |
| USB SmartCard/NFC and chosen SCP | Actual transport/FIDO AID/detected firmware/key parameters; vendor/profile authorization | Initial plain SELECT/GetInfo and post-SCP protected command are distinct. Retain ISO/GP/profile/producer gaps. |
| Ready Windows delta | User-announced ready commit/artifacts only | Compare selection, synchronous invocation versus pending task, accepted buffers, native cancel/completion, claim release/reopen, error/callback paths; recheck every X001/X002/X004/X005/X006 affected doc. |
| PIV physical | Existing user deferral | Unchanged; this WebAuthn record grants no device authorization. |

The clean nominees carry exact original IDs/hashes and substantial contracts/oracles in
`clean-nominees.json`. They are nominations only. The coordinator selects stratified 10%/minimum-three
sampling after removing any edited overlap; boilerplate/header units are ineligible substitutes.
