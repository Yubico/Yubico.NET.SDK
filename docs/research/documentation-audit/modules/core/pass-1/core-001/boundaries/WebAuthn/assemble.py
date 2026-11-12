"""Join authored boundary annotations to frozen inventory identities; static records only.

Shape follows the CTAP2 boundary's records, not its semantic conclusions. No product writes.
"""
import collections
import hashlib
import json
import pathlib
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
RUN = HERE.parents[1]
ROOT = RUN.parents[6]
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(path):
    return json.loads(path.read_text())


def save(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)


def identity(path):
    data = (ROOT / path).read_bytes()
    pinned = git('show', f'{PIN}:{path}')
    normalized = lambda b: b.decode('utf-8-sig').replace('\r\n', '\n')
    return dict(path=path, source_baseline=PIN, source_sha256=sha(data),
                pinned_git_bytes_sha256=sha(pinned), git_blob=git('rev-parse', f'{PIN}:{path}').decode().strip(),
                raw_matches_pin=data == pinned, bom_crlf_normalized_matches_pin=normalized(data) == normalized(pinned))


# Authored observations describe the selected range plus explicitly read boundary context.
# Header-only selection never becomes a claim of whole-file semantic acceptance.
NOTES = {
    'CLAUDE.md': 'Current module instructions separate protocol-2 availability, credential versus presence prompts, RP callback, output lifetimes and human-coordinated tests. Retain accurate applet limitations; add whole-ceremony lifetime condition when editing.',
    'PACKAGE_README.md': 'Package overview routes client/RP responsibilities and draft/alpha limitations to module guide; dependency is transitively Core through CTAP2. No native or hardware support claim inferred.',
    'README.md': 'Consumer examples have one owned client and explicit PIN finally; RP upload/serializer remains application-owned. Constraints membership headline overstates observed-member bound; whole-ceremony disposal condition should accompany ownership.',
    'WebAuthnAttestationObject.cs': 'Decode borrows outer CBOR and attStmt slices while materializing authData. Create/Encode allocate a new modeled map and retain component objects. Parsing is no trust or signature verification.',
    'AuthenticationResponse.cs': 'Result retains CTAP credential/signature/raw bytes and full modeled User. Descriptor omission is accurately qualified, but selected request association is not recovered. User policy remains separate DEC-002.',
    'CredentialMatcher.cs': 'Eager first/next collection passes User and optional descriptor through; omission becomes Empty twice across match/result construction. No sequence lease or output privacy filter.',
    'MatchedCredential.cs': 'Lazy task wrapper returns prebuilt response; Id/User are retained views/entity. WaitAsync cancels a wait, not already completed ceremony or side effects.',
    'PublicKeyCredentialRequestOptions.cs': 'Caller RP/challenge/allow-list inputs, required application validator, cooperative timer and cross-origin checks are accurately distinguished from server verification.',
    'CeremonyDeadline.cs': 'CTS timer/linked token signal begins after validation; Expired excludes caller cancellation. Dispose only timer resources, with no operation join or native abort.',
    'CredentialFilter.cs': 'Silent up:false zero-hash chunks shrink RequestTooLarge and advance NoCredentials. Singleton response recovers chunk descriptor; multi-entry response chooses returned matching ID. Final response omission is separate.',
    'IWebAuthnBackend.cs': 'Internal DTOs and backend seam hold borrowed request views/options, protocol and token/storage operations. Public serializer/operation-wide lifetime is absent.',
    'PinUvAuthTokenSession.cs': 'Adopts exact plaintext token array; Dispose zeroes it and leaves backend protocol alive. Scope is this array; no claim that provider/response copies vanish.',
    'PublicKeyCredentialCreationOptions.cs': 'RP/user/challenge/algorithm and attestation/UV/RK options feed current ceremony validation. Timeout/Enterprise limitations remain truthful applet-owned qualifications.',
    'RegistrationResponse.cs': 'Returned key/attestation/raw bytes are not RP-verified; transports alone receive independent sorted/read-only snapshot. Ordinary result views retain their original ownership.',
    'UvDecision.cs': 'Configured-option checks and PIN-first/token choice are SDK policy. UV capability check omits token capability; no PRF verified override. Core firmware/catalog supplies neither.',
    'WebAuthnBackend.cs': 'Owns supplied session and lazy V2 protocol/cache. Temporary ClientPin borrows shared protocol; disposing each would invalidate backend. Options auth-tag copies clear in finally; whole-workflow disposal is unjoined.',
    'WebAuthnClient.Authentication.cs': 'Probe narrows final request; all next assertions and outputs finish before matches return. Final builder loses selected descriptor on omission and passes User unchanged.',
    'WebAuthnClient.Ceremony.cs': 'CeremonyAuth clears token/tag/prompt owner; escalation at most once; registration caches probe and remints. Output preparation stays outside retry, preserving no-repeat creation boundary.',
    'WebAuthnClient.PinUvAuth.cs': 'Prompt result ownership/retry and detached late-result cleanup are explicit. Context code-point/byte limits are descriptive, not local validation; direct PIN rejection does not trigger prompt fallback.',
    'WebAuthnClient.Registration.cs': 'Effective RK and permission union precede attempts; final statement preference and transport snapshot reuse cached info. Output failure can follow creation; algorithm conversion uses Core numeric carrier only.',
    'WebAuthnClient.Validation.cs': 'Strict UTF-8 RP wire check then synchronous application authorization precedes deadline/backend. Selected CTAP status mapping covers attempt only; protocol/backend failures retain own types.',
    'WebAuthnClient.cs': 'Public constructor adopts session on success, retains application prompt and leaves returned buffers intact. Dispose bool changes after backend completion; no ceremony-wide admission/join.',
    'WebAuthnClientOptions.cs': 'Required RP validator, positive prompt cap and optional credential prompt. Service lifetime belongs to application; record carrier does not confer ownership on service.',
    'Aaguid.cs': 'Explicit bytes/Guid construction works through CTAP converter; default empty span/nonreflexive equality remains WA-owned. Public identifier bytes are not a secret-cleanup finding.',
    'CredBlobCodec.cs': 'Delegates builder/typed response parsing; get blob copies can hold sensitive application data and have no owner cleanup. Support/policy remains applet-level.',
    'CredProtectCodec.cs': 'Forwards integer policy plus ignored enforcement request to CTAP builder, parses only policy 1..3. Missing platform enforcement is not supplied by Core.',
    'MinPinLengthCodec.cs': 'Always requests true when called; factory ignores Requested false on nonnull input. Output reports authorized current policy in code points, not mutation.',
    'PreviewSignCodec.cs': 'Delegates canonical CTAP encoder/decoder; inner attestation supplies key/handle, outer signed extension algorithm. Repacking is no trust verification; selected exceptions propagate/suppress separately.',
    'PrfCodec.cs': 'Decrypts 32/64-byte aggregate, clears aggregate in finally but range temporaries/returned arrays survive. Protocol-specific crypto delegated to CTAP.',
    'CredProtectInput.cs': 'Public enforcement flag limitation is accurately disclosed. Input does not add a Core enforcement policy.',
    'CredProtectOutput.cs': 'Reports actual policy after creation; output check is no substitute for preventing insufficient-policy creation.',
    'GeneratedSigningKey.cs': 'Handle/public key/selected algorithm and decoded required inner attestation; empty handle permitted by draft but blocked by signing params. No private signing key export.',
    'PreviewSignErrors.cs': 'Selected CTAP statuses map to WebAuthn vocabulary. This is SDK policy and no universal DOMException equivalence.',
    'PreviewSignRegistrationInput.cs': 'Retains algorithm list; effective flags derive from ceremony UV preference, not stored internal Flags. Capability-gated request may be omitted.',
    'AuthenticationExtensionOutputsBuilder.cs': 'Mutable internal sink builds record with exact output references; no clone/clear owner or public extension plugin contract.',
    'RegistrationExtensionOutputsBuilder.cs': 'Mutable internal sink forwards optional output references into record. Container existence differs from each result presence.',
    'AuthenticationExtensionSession.cs': 'Fresh per-attempt processors; permission union/preparation/output phases and selected error suppression. Dispose reaches processor shared secrets, not returned output arrays.',
    'ExtensionContext.cs': 'Retains cached GetInfo/backend/protocol references for ceremony. Advertisement/enabled checks and available V2 object are not guarantees of successful authorization or old-device support.',
    'IAuthenticationExtensionProcessor.cs': 'Per-attempt interface supplies selected descriptor and live token for each output; underlying Core exchange lease does not cover this phase.',
    'IRegistrationExtensionProcessor.cs': 'Per-attempt preparation occurs after token, before command; output follows successful device exchange. Borrowed response/token lifetimes stay with ceremony.',
    'CredBlobProcessor.cs': 'Registration forwards supplied bytes; auth request asks credBlob:true only when chosen processor runs. Retrieved bytes remain separate sensitive-result lifetime.',
    'CredPropsProcessor.cs': 'Client-only bool reports effective RK parameter; no authenticator input or second capability query.',
    'CredProtectProcessor.cs': 'Forwards policy and parses response, with accurate missing-enforcement disclosure; no Core responsibility implied.',
    'LargeBlobProcessor.cs': 'Uses largeBlobKey/storage-array route; Required effective-RK/key validation and original write cardinality missing. Post-assertion read errors and write false/cancellation differ; nullable array conversion remains.',
    'MinPinLengthProcessor.cs': 'Presence constructs processor without carrying Requested. Reports current authorized policy; no PIN policy setter.',
    'PreviewSignProcessor.cs': 'Validates one sign entry per original allowed credential and selects by bytes. Final selected input translation differs from final descriptor recovery. Output inner-attestation proof remains unverified.',
    'PrfProcessor.cs': 'Per-attempt key agreement/secret disposal; salts cleared after builder, no token permission/UV override. Source concat/builder/results keep separate cleanup gaps.',
    'RegistrationExtensionSession.cs': 'Capability-filtered processors, aggregated permissions and per-attempt disposal. Error isolation and applet enforcement gaps already disclosed.',
    'WebAuthnExtensionInputs.cs': 'Typed optional input containers distinguish unsupported omission and code-point disclosure. Requests do not prove support, enforcement or credential availability.',
    'WebAuthnExtensionOutputs.cs': 'Container/result absence versus false is explicit; derived secrets/plaintext have no result cleanup owner. Client disposal clears neither copies nor results.',
    'IYubiKeyExtensions.cs': 'Validates client prerequisites then delegates session creation/options/transport unchanged to CTAP. On successful construction adopts session; no hidden second fallback or reset logic.',
    'WebAuthnAuthenticatorData.cs': 'CTAP parser makes independent raw/field copies; parsed extension values slice ED copy. Parses received flags/extensions without RP verification.',
    'Yubico.YubiKit.WebAuthn.csproj': 'Direct ProjectReference is CTAP2; Core transitively supplies public carriers. InternalVisibleTo supports test seam; package/draft/AOT opt-in is no native acceptance.',
    'WebAuthnAuditHardwareReproTests.cs': 'Selected hardware scenarios use real owned CTAP sessions/known PIN; timeout observes bounded assumed slack and GetInfo reuse; no run or full RP verification here.',
    'WebAuthnAuditHardwareTests.cs': 'PRF repeat/actual UV variants/held-ID association and blob register-write-read assert scoped hardware behavior when run; mutable PIN setup and output cleanup remain qualifications.',
    'PreviewSignTests.cs': 'Reflection-only ARKG bridge, extension signature offline verification and dual-ID nonmembership limitation. All selected user-presence/UV/stateful setup lanes are unrun.',
    'WebAuthnClientFactoryTests.cs': 'Explicit SmartCard creation/credential fields; broad NotSupported skip inherits SELECT wrapper uncertainty. Disposable result existence is not native release proof.',
    'WebAuthnClientTests.cs': 'Basic/full/discoverable result assertions use fresh sessions after client adoption; discouraged-UV test lacks capability guards and cannot qualify every PIN-set key.',
    'WebAuthnExcludeListStressTests.cs': 'Seventeen persistent credentials/exclusion and destructive RP-wide cleanup. Separate sessions follow client ownership; best-effort cleanup gives no restoration or secret-erasure proof.',
    'WebAuthnTestHelpers.cs': 'Fixed known test PIN; normalization may set/change/request token and cleanup deletes all RP credentials. Helper existence is not unattended authorization; success-only temporary clearing is scoped.',
    'AttestationObjectTests.cs': 'Selected preamble with parser-fixture context; no new whole-file conformance or cryptographic trust disposition.',
    'WebAuthnAuditReproTests.cs': 'Selected fake assertions cover absent uv/rk, attestation None/self views, crossOrigin, deadline signal and chunk limits. Hanging fake is cancellation-aware, not native abort.',
    'WebAuthnExtensionAuditReproTests.cs': 'Selected getCredBlob assertion checks request encoding and typed returned bytes only when requested. Fake outputs have no device/trust/cleanup qualification.',
    'WebAuthnLargeBlobAuditReproTests.cs': 'Selected import/namespace context for synthetic blob audit fixtures; no assigned full test body and no whole-file acceptance.',
    'CredentialFilterTests.cs': 'Selected complete tests assert chunk shape/retry/selection/no-token/length skip. Standard fixture includes descriptor; no final omission-recovery regression.',
    'CredentialPromptRetryTests.cs': 'Selected scripted/late/recording prompt units plus actual backing-owner assertions. SDK context/retry/clearing is distinct from fake ASCII validity and production profile checks.',
    'CtapStatusMappingTests.cs': 'Selected declaration preamble with mapper context; mapper table is not coverage of GetInfo/token/output or transport failure normalization.',
    'PinUvAuthTokenSessionTests.cs': 'Actual handed-in sentinel token array clears and repeated Dispose getter rejects. No provider/transport/input-copy erasure oracle.',
    'RegistrationTransportTests.cs': 'Actual source-list mutation and readonly-list guard assert independent sorted unique hints; caller-constructed response defaults empty. No host-reachability assertion.',
    'RpAuthorizationTests.cs': 'Both ceremonies forward arbitrary faithfully encodable RP text unchanged when callback approves and block backend on rejection/cancellation/wire-invalid Unicode. No built-in browser syntax/binding policy.',
    'UserPresenceIntentTests.cs': 'Real CTAP/Core SmartCard protocol over fake connection checks request intent/operation/RP. No terminal outcome recorder or native/physical oracle; broad layer headline should be scoped.',
    'UvDecisionLogicTests.cs': 'Selected truth-table summary with implementation branch context: configured versus available verification and policy overrides remain distinct from actual device token support.',
    'WebAuthnBackendTests.cs': 'Selected shared-secret test checks shape/nonzero/copy/access guard, not retained-view zeroing. Contextual tag tests capture actual options copy and in-call snapshot.',
    'WebAuthnClientCeremonyFrameTests.cs': 'Selected complete fake attempt/probe/remint tests assert call counts/options/permissions. Cached probe and output-after-success are separate; 5.8 comment lacks immutable measurement here.',
    'WebAuthnClientConstructionTests.cs': 'Selected tests check successful sequential disposal forwarding and failed-constructor nonadoption, not concurrency/drain or native release.',
    'WebAuthnClientGetAssertionTests.cs': 'Selected response/idempotent tests compare bytes and one backend call; incomplete internal factory tests wait cancellation and later completion, not device cancellation.',
    'WebAuthnClientMakeCredentialTests.cs': 'Selected AAGUID/key type and backend disposal tests plus ceremony sentinel context. Returned objects/counts do not establish end-to-end response ownership or RP acceptance.',
    'WebAuthnClientOptionsTests.cs': 'Selected NullPrompt implements declined-result contract; unselected option guards are not new boundary-wide acceptance.',
    'WebAuthnClientPinRetryTests.cs': 'Selected retry-test summary/import context; no Core credential prompt acquisition added to CTAP itself.',
    'WebAuthnSurfaceTests.cs': 'Reflection source checks internal backend/types and public session constructor shape, not compiled consumer/analyzer or native semantics.',
    'CoseAlgorithmTests.cs': 'Selected import context uses CTAP algorithm carrier, not duplicate WebAuthn crypto implementation or provider capability proof.',
    'CoseKeyTests.cs': 'Selected synthetic unknown-key/raw metadata and EC2/crv/coordinate lengths. Pinned FluentAssertions 7.0.0 byte-array rule compares exact order; no provider-validity claim.',
    'ExtensionSessionTests.cs': 'Selected exact encoded credBlob/canonical key order and no-extra-permission getCredBlob fixture. No blanket absence of permission-using processors or full device support.',
    'LargeBlobProcessorTests.cs': 'Selected mocked write CtapException maps Written=false. No original allow-list cardinality, read-error/cancel, whole-array transaction or restoration oracle.',
    'PreviewSignCodecTests.cs': 'Selected synthetic inner-attestation extraction asserts ID/key/algorithm/views; extra inner algorithm fixture leaves minimal inner shape untested. No trust verification.',
    'PreviewSignProcessorTests.cs': 'Selected processor summary describes original list map validation/selected input. No assigned full method body and no hardware/crypto disposition.',
    'PrfCodecTests.cs': 'Selected vector summary checked with complete codec-test bodies; exact output/salt comparisons for V1/V2 codec do not negotiate V1 or enforce verified PRF.',
    'PrfProcessorTests.cs': 'Selected processor-summary/vector context; permissions/UV enforcement remain absent in actual source. No full module test acceptance.',
    'IYubiKeyExtensionsTransportTests.cs': 'Selected fake Connect throws at routing boundary. Type-selection assertions establish exposed order/SCP implication, not native initialization or universal logic-absence proof.',
    'MockCtap2Responses.cs': 'Selected helper preamble with response-shape context; normal final fakes include descriptors and remain synthetic, not device-valid cryptographic responses.',
    'TestExtensionContext.cs': 'Selected helper preamble; supporting-all backend is test capability context, not production negotiation or a public extension seam.',
    'TestPinUvAuthProtocol.cs': 'Selected preamble with complete sentinel helper context: Version=2/tagLength=16 and unused throwing crypto methods are intentional noncrypto fake, not protocol-2 conformance.',
    'PinUvSharedSecret.cs': 'Supporting DTO adopts exact derived secret array and zeroes on Dispose; KeyAgreement retains public dictionary. No finalizer or returned-result cleanup is implied.',
    'RpIdValidator.cs': 'Supporting public delegate gives application RP validity and complete origin binding; sync callback precedes timer/device calls, cancellation propagates, false/ordinary throw maps Security.',
    'WebAuthnClientData.cs': 'Supporting builder owns JSON/hash arrays but manual control escaping is not exact CCDToString; low-level accepts arbitrary nonblank type and does not enforce crossOrigin/topOrigin relationship.',
    'WebAuthnOrigin.cs': 'Supporting origin representation accepts HTTPS or HTTP localhost/.localhost, strips URL path/query/fragment and default port; native/opaque origins remain outside this representation.',
    'CredPropsInput.cs': 'Supporting parameterless presence requests client-derived effective RK property; no authenticator input or native/permission side effect.',
    'LargeBlobInput.cs': 'Supporting input retains borrowed write bytes through post-assertion storage; documented exact-one original allow-list and Required support limitations remain applet-owned.',
    'LargeBlobSupport.cs': 'Supporting Required enum correctly discloses unsupported resulting-credential edge; caller requests RK Required and inspects actual result, without treating that as repaired enforcement.',
    'PrfInput.cs': 'Supporting input retains caller memory/dictionary; ComputeSalt returns hash but leaves concatenation containing input uncleared. Verified-PRF and temporary-cleanup limitations stay explicit.',
    'PrfInputValues.cs': 'Supporting per-credential input borrows First/Second backing memory through ceremony output; init properties do not transfer ownership or clear source.',
    'CredPropsOutput.cs': 'Supporting nullable bool permits unknown; active processor supplies effective RK bool. No independent authenticator-state attestation follows.',
    'LargeBlobOutput.cs': 'Supporting records retain read plaintext memory; missing byte[] becomes present empty memory. Written false, cancellation, read failure and absent result have separate meanings.',
    'PrfOutput.cs': 'Supporting nondisposable result views expose SDK-owned derived secret arrays without cleanup; caller-constructed results may borrow caller memory. No writable-result workaround is available.',
    'ByteArrayKeyComparer.cs': 'Supporting internal comparer uses byte equality/full-content hash. Normalized dictionary retains backing key buffers, which must stay unchanged; not a public helper.',
    'PreviewSignAuthenticationInput.cs': 'Supporting constructor normalizes dictionary comparer by copying entries, not memory. Requires nonempty map; processor separately checks original allow-list membership.',
    'PreviewSignAuthenticationOutput.cs': 'Supporting raw algorithm-specific signature view is passed through; selected algorithm rules and RP verification remain caller responsibilities.',
    'PreviewSignFlags.cs': 'Supporting enum defines 000/001/101; public registration has no flag setter and derives UP/UV from ceremony preference, so enum alone is no unattended signing capability.',
    'PreviewSignRegistrationOutput.cs': 'Supporting generated-key record carries handle/public key/attestation, not private key. Decoded inner attestation still needs selected RP trust verification.',
    'PreviewSignSigningParams.cs': 'Supporting constructor borrows keyHandle/TBS/optional args unchanged; rejects empty handle/TBS locally. Empty handle draft permission and algorithm-specific args validation remain separate.',
    'AttestationPreference.cs': 'Supporting None/self versus pass-through preferences; Enterprise does not send CTAP enterprise parameter and Indirect has no anonymization service.',
    'ResidentKeyPreference.cs': 'Supporting enum expresses discouraged/preferred/required; ceremony resolves against cached rk capability and rejects required when unavailable.',
    'UserVerificationPreference.cs': 'Supporting enum expresses preference, not absolute actual-UV guarantee; token/profile/alwaysUv and extension policy stay in client/authenticator.',
    'PublicAPI.Shipped.txt': 'Directive-only analyzer input (#nullable enable); excluded from explanatory prose denominator, retained byte-exact.',
    'Base64Url.cs': 'Supporting client-data/credential-key helper emits unpadded URL alphabet; decode restores padding and uses permissive BCL conversion. It is not a new strict canonical input policy.',
    'SensitiveMemory.cs': 'Supporting internal helper clears actual owned auth-tag views via MemoryMarshal.AsMemory, with null/empty no-op. It is not a public authority to mutate borrowed result views.',
    'WebAuthnClientError.cs': 'Supporting error vocabulary distinguishes client timeout/NotAllowed, authenticator timeout/Constraint, Security RP rejection and caller OCE; not every layer failure is normalized.',
    'WebAuthnTransport.cs': 'Supporting open string carrier preserves future transport hints and distinguishes contact smart-card from SDK SmartCard route; hints do not assert active host reachability.',
    'WebAuthnClientDataTests.cs': 'Supporting tests assert fixed JSON/order/hash consistency and quote escaping; intentionally invalid low-level topOrigin combination is marked as such. Controls/surrogates exact CCDToString remain uncovered.',
    'WebAuthnOriginTests.cs': 'Supporting selected scheme/host/default-port/stripping/equality cases; no document ancestry/name-resolution/browser Secure Contexts oracle.',
    'AaguidTests.cs': 'Supporting constructed-value Guid/string/equality tests; byte-array BeEquivalentTo preserves order under pinned library rule, without source mutation control. Default/reflexive equality and big-endian runtime absent.',
    'Fixtures.cs': 'Supporting synthetic canonical COSE EC/OKP/RSA encodings; RSA modulus deliberately zero, coordinate literals not validated as mathematical keys. Generator is retained source, not a native producer.',
    'PreviewSignSigningParamsTests.cs': 'Supporting tests assert exact raw args/null and local empty-handle/TBS errors; arbitrary args fixture is explicitly pass-through, not semantic CBOR validity.',
    'PrfInputTests.cs': 'Supporting tests assert length/repeat equality/different inputs, not independent domain-separated KAT or cleanup; codec vector tests supply stronger separate input/output comparison.',
    'TokenBufferAssert.cs': 'Supporting nonempty/every-byte Zeroed observer and any-nonzero NotZeroed observer; sentinel source tracked explicitly. Does not prove every original sentinel byte remains intact.',
    'WebAuthnAuthenticatorDataTests.cs': 'Supporting synthetic UP/ED/RP-hash/CBOR extension fixtures assert names/values/count, not signatures/trust/complete-envelope malformed rejection or independent snapshot mutation.',
}

# Exact unit positions are frozen by the packet and checked against original IDs/hashes.
ANCHORS = {36: [1, 2], 69: [2], 70: [2], 54: [2], 194: [4], 216: [12], 250: [12]}
CLEAN_NOMINEES = {24, 33, 161, 172, 173, 185, 188, 192, 223, 225, 238, 239, 251, 254}
CLEAN_REASONS = {
    24: 'Registration hint task correctly separates independently owned sorted unknown-name snapshot from current host reachability and connection choice.',
    33: 'RP authorization narrative places callback before device work/timer, preserves RP text, and gives application complete validity/binding responsibility without browser conformance promise.',
    161: 'Nonpositive device list-limit vector checks refusal before any probe and explicit NotSupported result; not merely a framework null guard.',
    172: 'RequestTooLarge shrinking asserts exact 3,2,1,1,1 chunk sequence at same offset then correct third ID; concrete retry/no-match control.',
    173: 'Single-descriptor too-large propagates exact CTAP status instead of silently shrinking past one; actual protocol failure mapping boundary.',
    185: 'Disposal observes exact supplied nonzero sentinel token array becoming zero; no copy-based false zeroing claim.',
    188: 'Clears/mutates source advertised transport list after response then checks sorted unique independent read-only list and rejected mutation.',
    192: 'False/throwing application callback blocks all backend calls for both ceremonies while retaining exact original inner exception and count.',
    223: 'Repeated production match selection compares response byte values and exactly one backend call; commentary does not claim cryptographic validation.',
    225: 'Incomplete internal factory checks canceled waiting then real later completion; local comment explicitly separates production cached response from slow test factory.',
    238: 'Extension input pipeline output is decoded and actual credBlob bytes compared, protecting forwarding rather than just object construction.',
    239: 'Mixed standard/preview extension map checks count and concrete CTAP canonical key order across different lengths and same-length names.',
    251: 'Connect-throwing fake records SmartCard choice only when HID absent; commentary explicitly makes this selection fallback, not retry after failure.',
    254: 'SCP parameters are outer-owned/disposed and fake Connect records SmartCard implication; useful actual factory dependency without pretending handshake executes.',
}
RELATED = {
    'WebAuthnClient.cs': [2, 6], 'WebAuthnBackend.cs': [2, 3, 6, 9],
    'WebAuthnClient.Ceremony.cs': [2, 3, 4, 5, 6], 'WebAuthnClient.PinUvAuth.cs': [2, 3, 5, 6],
    'WebAuthnClient.Authentication.cs': [2, 5, 6, 7, 8, 11], 'CredentialMatcher.cs': [2, 7, 8],
    'AuthenticationResponse.cs': [6, 7, 8], 'MatchedCredential.cs': [5, 7, 8],
    'WebAuthnClient.Registration.cs': [6, 9, 10, 11], 'CeremonyDeadline.cs': [5],
    'WebAuthnAttestationObject.cs': [6], 'WebAuthnAuthenticatorData.cs': [6], 'Aaguid.cs': [10],
    'PrfProcessor.cs': [6, 11], 'PrfCodec.cs': [6, 11], 'LargeBlobProcessor.cs': [6, 11],
    'PreviewSignCodec.cs': [6, 10, 11], 'GeneratedSigningKey.cs': [6, 10],
    'CredProtectInput.cs': [11], 'CredProtectOutput.cs': [11], 'CredProtectProcessor.cs': [11],
    'MinPinLengthCodec.cs': [11], 'MinPinLengthProcessor.cs': [11],
    'RegistrationExtensionSession.cs': [2, 6, 11], 'AuthenticationExtensionSession.cs': [2, 6, 11],
    'ExtensionContext.cs': [9, 10], 'WebAuthnExtensionOutputs.cs': [6, 11],
    'WebAuthnExtensionInputs.cs': [11], 'PinUvAuthTokenSession.cs': [6],
    'UserPresenceIntentTests.cs': [4, 12], 'WebAuthnBackendTests.cs': [6, 12],
    'WebAuthnAuditHardwareTests.cs': [6, 11, 12], 'WebAuthnAuditHardwareReproTests.cs': [5, 12],
    'WebAuthnClientTests.cs': [12], 'PreviewSignTests.cs': [7, 10, 12],
    'WebAuthnExcludeListStressTests.cs': [2, 6, 12],
    'PinUvSharedSecret.cs': [6], 'WebAuthnClientData.cs': [11], 'PrfInput.cs': [6, 11],
    'PrfInputValues.cs': [6], 'PrfOutput.cs': [6], 'LargeBlobInput.cs': [6, 11], 'LargeBlobOutput.cs': [6, 11],
    'LargeBlobSupport.cs': [11], 'PreviewSignSigningParams.cs': [10, 11],
    'PreviewSignAuthenticationInput.cs': [7, 10], 'PreviewSignRegistrationOutput.cs': [6, 10],
    'AttestationPreference.cs': [11], 'WebAuthnClientDataTests.cs': [11, 12], 'AaguidTests.cs': [10, 12],
    'PreviewSignSigningParamsTests.cs': [10, 12], 'PrfInputTests.cs': [6, 12],
}

INHERITED_WA = {
    1: ('Retained applet policy: Enterprise is not forwarded and Indirect has no anonymization service; None/self policy is separate.', [11]),
    2: ('Retained error/isolation policy: GetInfo/token acquisition/output failures lie outside attempt mapper; unsupported previewSign omitted.', [11]),
    3: ('Retained cooperative timer and oversized-delay policy; Core native completion/drain is a separate mechanism.', [5]),
    4: ('Retained test applicability: discouraged-UV hardware fixture lacks makeCredUvNotRqd/alwaysUv guards; existing comments disclose it.', [12]),
    5: ('Retained manual CCDToString control-escape/low-level builder gap; browser serializer compliance is not supplied by Core.', [11]),
    6: ('Confirmed borrowed attestation outer/statement lifetime; snapshot design remains applet-owned and documented.', [6]),
    7: ('Confirmed default Aaguid empty span/nonreflexive equality; separate CTAP endian conversion; registration uses explicit constructor.', [10]),
    8: ('Retained oracle limitation: shared-secret test checks pre-disposal copy/access guard, not retained actual owner view.', [6, 12]),
    9: ('Confirmed V2-only backend; no V1 negotiation and built-in UV selects without full token capability preflight.', [3, 11]),
    10: ('Confirmed final omission loss after known/narrowed singleton probe. Request association must survive both matcher and response construction; no first-original-entry guess.', [7]),
    11: ('Confirmed missing verified-PRF UV override; context/token/salt correctness is no replacement.', [11]),
    12: ('Confirmed credProtect enforcement ignored on supported/unsupported paths; returned actual policy check happens after creation.', [11]),
    13: ('Confirmed layered PRF/blob/input/builder/result ownership gaps with local token/shared-secret clears. No Core-wide or all-buffer erasure closure.', [6]),
    14: ('Confirmed Required blob can return unsupported created credential for effective non-RK or missing response key.', [11]),
    15: ('Confirmed original exactly-one allowed credential check absent for blob writes; singleton narrowing does not satisfy it.', [11]),
    16: ('Confirmed source null byte[] conversion produces present empty memory; read-error/presence policy remains distinct.', [6, 11]),
    17: ('Retained optional direct-largeBlob/pinless-write support decisions; CTAP key8 drop keeps original #4/CTAP-IMP-030 aliases.', [11]),
    18: ('Confirmed nonnull minPinLength input requests true regardless Requested; policy disclosure only.', [11]),
    19: ('Confirmed empty generated handle cannot round-trip through WebAuthn signing params; separate from CTAP ARKG wire/math.', [10, 11]),
    20: ('Retained minimal-inner fixture oracle gap; existing test uses extra algorithm and tolerated outer flags; no vendor variant proof.', [12]),
    21: ('Retained dual-preview test membership gap: non-first reported ID selects second verification key without explicit membership check.', [7, 12]),
}


def main():
    packet_path = RUN / 'inventory/packets/BOUNDARY-WebAuthn.json'
    packet = load(packet_path)
    wanted = set(packet['unit_ids'])
    units = [u for u in load(RUN / 'inventory/semantic-units.json') if u['id'] in wanted]
    results = []
    for i, unit in enumerate(units):
        path = unit['path']
        data = (ROOT / path).read_bytes()
        text = data.decode('utf-8-sig')
        assert sha(data) == unit['source_sha256'], unit['id']
        assert sha(text[unit['start_character']:unit['end_character_exclusive']].encode()) == unit['range_sha256'], unit['id']
        name = pathlib.Path(path).name
        observation = NOTES.get(name)
        if name.endswith('.csproj'):
            observation = observation or 'net10 test project reference/runner configuration; unit uses xUnit v3/MTP, integration xUnit v2/Tests.Shared. Source configuration supplies no run or compiler participation proof.'
        if name == 'PublicAPI.Unshipped.txt':
            observation = 'Whole analyzer declaration snapshot excluded from prose-clean denominator. Boundary public type/constructor/property inventory reconciled; no independent semantic approval of every signature.'
        assert observation, path
        candidates = ANCHORS.get(i, RELATED.get(name, []))
        if i in CLEAN_NOMINEES:
            candidates = []
        disposition = ('READ-EXCLUSION' if i == 112 else 'READ-CANDIDATE' if i in ANCHORS else
                       'READ-RELATED-CANDIDATE-CONTEXT' if candidates else 'READ-CLEAN')
        results.append({k: unit[k] for k in ['id', 'path', 'label', 'category', 'first_line', 'last_line', 'start_character', 'end_character_exclusive', 'source_sha256', 'range_sha256']} | dict(
            source_baseline=PIN, read='complete-assigned-unit-with-boundary-context' if i != 112 else 'declaration-inventory-reconciled-exclusion',
            disposition=disposition, candidate_ids=[f'CORE-WA-X{n:03}' for n in candidates], reason=observation,
            exact_unit_scope=unit['label'], exclusion_reason=observation if i == 112 else None,
            independent_review='PENDING', compiler_runtime_native_device_validation='UNRUN',
            sample_status='NOMINEE' if i in CLEAN_NOMINEES else 'NOT-NOMINATED'))
    save('unit-dispositions.json', results)

    file_results = []
    for path in packet['disposition_paths']:
        members = [r for r in results if r['path'] == path]
        ids = sorted({c for r in members for c in r['candidate_ids']})
        if not members:
            ids = [f'CORE-WA-X{n:03}' for n in RELATED.get(pathlib.Path(path).name, [])]
        note = NOTES.get(pathlib.Path(path).name)
        if not note:
            note = ('Additional candidate path: targeted declaration/ownership or prior finding context only; no assigned semantic unit. '
                    'Retain current product bytes and original applet finding; no whole-file clean or new engineering resolution claimed.')
        if members:
            note = members[0]['reason']
        file_results.append(identity(path) | dict(assigned_unit_ids=[r['id'] for r in members],
            selected_ranges=[[r['first_line'], r['last_line']] for r in members],
            read_scope='complete assigned ranges plus named caller/helper context' if members else 'candidate-population/context-only; zero assigned units',
            contextual_read_line_ranges=[] if members else [[1, len((ROOT / path).read_text().splitlines())]],
            observation=note, candidate_ids=ids,
            candidate_file_disposition='UNCHANGED-REOPEN-PROPOSED-LOCAL-DOC' if any(r['disposition'] == 'READ-CANDIDATE' for r in members) else
                'UNCHANGED-RETAIN-QUALIFIED-RELATED-CONTRACT' if ids else 'UNCHANGED-RETAIN-SCOPED-CONTRACT' if members else 'UNCHANGED-RETAIN-APPLET-OWNED-DEPENDENCY',
            reopening='candidates.md and original source-run inherited key; no blanket module reopening', independent_review='PENDING'))
    save('file-dispositions.json', file_results)

    routed = load(RUN / 'inventory/inherited-packet-inputs.json')['BOUNDARY-WebAuthn']
    registry = {r['key']: r for r in load(RUN / 'inherited-findings/registry.json')['rows']}
    inherited = []
    for key in routed['provenance_keys']:
        old = registry[key]
        if key.startswith('webauthn-001:WA-IMP-'):
            outcome, groups = INHERITED_WA[int(key[-3:])]
        elif 'XREF-001' in key:
            outcome, groups = ('Preserve prior resolved User/descriptor documentation for exact scope. WA-IMP-010 recovery and CTAP-DEC-002 actor decision remain separately open.', [7, 8])
        elif key.endswith('CTAP-DEC-002'):
            outcome, groups = ('Source actor trace complete; native result retains full modeled User, no response upload serializer exists. Maintainer/coordinator must name disclosure boundary before policy change. No field stripping.', [8])
        else:
            num = old['original_id']
            outcome, groups = {
                '#4': ('Retain original dropped key8 output optional-profile gap, not closed by API removal or Core framing.', [11]),
                'CTAP-IMP-004': ('Builder-held copies survive final encoded request clearing; actual WebAuthn tag copies and extension callers have separate owners.', [6]),
                'CTAP-IMP-025': ('CTAP endian converter remains independent from default WebAuthn Aaguid; no big-endian run.', [10]),
                'CTAP-IMP-027': ('Typed response/borrowed statement and independent authData/secret arrays survive session disposal; WebAuthn adds further copies.', [6]),
                'CTAP-IMP-029': ('CTAP draft-10 wire/profile remains separate from Core draft-09 math; WebAuthn delegates, no silent upgrade.', [10]),
                'CTAP-IMP-030': ('GetAssertion key8 unsigned outputs skipped; WebAuthn largeBlob storage route does not implement optional direct extension.', [11]),
                'CTAP-IMP-039': ('CTAP generated-key accessor algorithm propagation remains distinct; WebAuthn selected algorithm vs seed key roles and test reflection bridge do not settle it.', [10]),
            }[num]
        inherited.append(dict(key=key, source_run=old['source_run'], original_id=old['original_id'],
            registry_state_preserved=old['current_state'], original_definition=old['historical_definition'],
            original_aliases=old['aliases_or_related_ids'], nonadditive_group=old['dedup_group'],
            current_boundary_outcome=outcome, local_candidate_ids=[f'CORE-WA-X{n:03}' for n in groups],
            documentation_state='PRIOR-RESOLUTION-PRESERVED' if 'XREF-001' in key else 'QUALIFICATIONS-RETAINED; REOPEN-ON-EXACT-TRIGGER',
            implementation_state='NO-NEW-CLOSURE', decision_state='NEEDS-MAINTAINER-DECISION' if key.endswith('CTAP-DEC-002') else 'ORIGINAL-APPLET-OR-MECHANISM-OWNER',
            qualification='No fresh SDK/native/crypto/device execution; source boundary rebaseline only',
            current_implementation_paths=old['current_implementation_paths'],
            reopening_paths=old['documentation']['reopen_paths'],
            original_precise_trigger=old['documentation']['precise_trigger'],
            original_module_xml_reopening_paths=old['documentation'].get('module_xml_reopening_paths', []),
            independent_review='PENDING'))
    save('inherited-dispositions.json', inherited)
    save('clean-nominees.json', [r | dict(nomination_reason=CLEAN_REASONS[i],
         limits='Substantial complete unit checked in context; no runtime/independent acceptance. Remove if later edited.') for i, r in enumerate(results) if i in CLEAN_NOMINEES])
    save('candidate-index.json', [dict(id=f'CORE-WA-X{n:03}', accepted_core_id=None,
         assigned_unit_ids=[r['id'] for r in results if f'CORE-WA-X{n:03}' in r['candidate_ids']],
         candidate_associated_unchanged_paths=sorted({r['path'] for r in file_results if f'CORE-WA-X{n:03}' in r['candidate_ids']}),
         details='candidates.md', documentation='PROPOSED-OR-RETAINED-PENDING-REVIEW', runtime='NO-REPAIR', decision='SEPARATE-OWNER', qualification='UNRUN') for n in range(1,13)])

    reports = [f'investigation-{n}.md' for n in ['A', 'B1', 'B2', 'B3', 'B4', 'B5', 'B6', 'C']]
    reports += ['plan.md', 'boundary-Ctap2.md', 'boundaries/Ctap2/followup-B4-B6.md', 'boundaries/Ctap2/followup-B4-B6.json',
                'boundaries/Ctap2/traces.md', 'boundaries/Ctap2/candidates.md', 'boundaries/Ctap2/sources.md',
                'inherited-findings/registry.json', 'inherited-findings/boundary-seeds.md', 'inventory/inherited-routing.json',
                'inventory/inherited-packet-inputs.json', 'inventory/packets/BOUNDARY-WebAuthn.json',
                'investigation/C/sources/final-source-register.md', 'investigation/B4/sources/register.md',
                'investigation/B5/sources/register.md', 'investigation/B6/sources/final-source-register.md',
                'investigation/A/sources/notes.md', 'investigation/B1/sources/register.md',
                'investigation/B2/sources/register.md', 'investigation/B3/sources/register.md']
    source_paths = [str((RUN / p).relative_to(ROOT)) for p in reports]
    source_paths += [str(p.relative_to(ROOT)) for p in (RUN / 'investigation/B5/sources').glob('ctap*.html')]
    source_paths += [str(p.relative_to(ROOT)) for p in (HERE / 'sources').glob('*') if p.is_file()]
    source_paths += ['docs/research/documentation-audit/workflow.md', 'docs/research/documentation-audit/records/style.md',
                     'CLAUDE.md', 'Directory.Packages.props', 'src/Core/CLAUDE.md', 'src/Core/tests/CLAUDE.md', 'src/WebAuthn/CLAUDE.md',
                     'src/Ctap2/CLAUDE.md', 'src/Ctap2/tests/CLAUDE.md', 'docs/TESTING.md',
                     'docs/SDK-HOUSE-STYLE.md', 'docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/evidence/g/boundary-originals.md',
                     'docs/research/documentation-audit/records/runs/webauthn-001/investigation-a.md',
                     'docs/research/documentation-audit/records/runs/webauthn-001/investigation-b.md']
    source_identity_path = HERE / 'source-identities.json'
    if source_identity_path.exists() and not (HERE / 'source-identities-initial.json').exists():
        (HERE / 'source-identities-initial.json').write_bytes(source_identity_path.read_bytes())
    save('source-identities.json', [dict(path=p, sha256=sha((ROOT / p).read_bytes()), bytes=(ROOT / p).stat().st_size) for p in sorted(set(source_paths))])
    deps = {
        'src/Ctap2/src/Ctap2Session.cs': [[90, 225], [265, 478]],
        'src/Ctap2/src/IYubiKeyExtensions.cs': [[140, 206]],
        'src/Ctap2/src/Credentials/GetAssertionResponse.cs': [[19, 245]],
        'src/Ctap2/src/Credentials/MakeCredentialResponse.cs': [[19, 304]],
        'src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs': [[19, 439]],
        'src/Ctap2/src/Backend/HidBackend.cs': [[23, 117]],
        'src/Ctap2/src/Backend/SmartCardBackend.cs': [[25, 148]],
        'src/Ctap2/src/Pin/ClientPin.cs': [[298, 490], [571, 584]],
        'src/Core/src/Sessions/ApplicationSession.cs': [[202, 380], [461, 568]],
        'src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs': [[230, 433]],
        'src/Core/src/Credentials/UserPresenceNotification.cs': [[19, 204]],
        'src/Core/src/Credentials/ICredentialPrompt.cs': [[20, 116]],
        'src/Core/src/Credentials/CredentialPromptContext.cs': [[17, 68]],
        'src/Core/src/Credentials/ConsoleCredentialReader.cs': [[220, 330]],
        'src/Core/src/Utilities/DisposableArrayPoolBuffer.cs': [[20, 84]],
    }
    save('dependency-identities.json', [identity(p) | dict(read_line_ranges=rr, primary_owner='Core family / CTAP2 boundary; contextual read only') for p, rr in deps.items()])
    save('verification.json', dict(kind='investigator-static-record-assembly', source_baseline=PIN,
         packet_sha256=sha(packet_path.read_bytes()), assigned_units=len(results), selected_paths=len(packet['paths']),
         candidate_paths=len(file_results), inherited_keys=len(inherited), local_concern_groups=12,
         disposition_counts=dict(collections.Counter(r['disposition'] for r in results)),
         clean_nominees=len(CLEAN_NOMINEES), unread_units=0, independent_content_review=False,
         source_investigation='COMPLETE-WITH-QUALIFICATIONS', accepted_core_ids=0, product_edits=0,
         sdk_builds=0, sdk_tests=0, native_device_crypto_experiments=0, formatter=0,
         git_index_ref_commit_push=0, subagents=0,
         scratch='/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/BOUNDARY-WebAuthn',
         qualification='No new WebAuthn ceremony qualification; independent review/triage/sampling/validation remain pending'))
    print(json.dumps(load(HERE / 'verification.json'), indent=2))


if __name__ == '__main__':
    main()
