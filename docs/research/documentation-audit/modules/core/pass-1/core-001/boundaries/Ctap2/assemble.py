"""Join investigator-authored boundary annotations to immutable inventory coordinates.

This writes only this packet's records. Hashes establish identity, never semantic reading.
No SDK/Python package, native API, cryptographic implementation or device code is imported.
"""
import collections
import hashlib
import json
import pathlib
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
RUN = HERE.parents[1]
REPO = RUN.parents[6]
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(path):
    return json.loads(path.read_text())


def write(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n")


def git(*args):
    return subprocess.check_output(["git", *args], cwd=REPO)


def normalized(data):
    return data.decode("utf-8-sig").replace("\r\n", "\n")


# These are authored boundary observations from the complete selected ranges and contextual reads.
# Each file retains its exact selected ranges below; no whole-file read is inferred from this join.
RULES = [
    ("CLAUDE.md", [1, 2, 3, 4, 6, 7, 17, 18, 19, 20, 22], "Guide/instruction ranges distinguish session creation, PIN/response ownership, prompt terminal classification and qualified fake/device evidence. Preserve accurate existing limitations; localized membership and callback-failure wording needs review."),
    ("PACKAGE_README.md", [], "Package purpose, Core transitive dependency and alpha/device/RP/experimental boundaries are useful. Installation is documentation only; no CLI/package execution or API-binding verification."),
    ("src/Ctap2/README.md", [1, 2, 3, 4, 6, 7, 10, 17, 18, 19], "Complete assigned narrative/example sections read, including implicit same-session variables and enclosing PIN/token finally scopes. Second GetInfo, no-fallback and detected-versus-effective firmware are distinct; all-interfaces wording needs Core's known-member bound."),
    ("examples/FidoTool/README.md", [2, 3, 4, 17, 18, 21, 22], "Complete selected task/CLI/security sections read against actual menus/helpers/Program. They disclose independent reset reselection, synchronous PIN prompts, lost cancellation provenance and persistent demo operations; retain those qualifications."),
    ("IYubiKeyExtensions.cs", [1, 2, 17], "Actual factory chooses one typed transport, forces SmartCard for implicit-SCP choice, snapshots preference, delegates open/failure cleanup and transfers ownership only after initialization. One-shot helper adds a second fresh info read and encloses owned disposal."),
    ("Ctap2Session.cs", [2, 4, 17, 18, 20], "Complete session body read: eager HID prepare or SmartCard SELECT/GetInfo, detected USB gate before override, configure/SCP/rebind, full readiness, shared notification then backend status before typed decode. Make/Get clear final encoded requests but typed/output/workflow lifetimes remain separate."),
    ("Backend/HidBackend.cs", [4, 18, 19, 20], "Complete backend read: borrowed protocol, CBOR 0x10, CTAP status outcome, terminal resolution before status-stripped view transfer, finally clears only untransferred response after final Resolve. Native reports and decoder copies are separate owners."),
    ("Backend/SmartCardBackend.cs", [4, 17, 20], "Complete backend read: broad SELECT ApduException wrapper, policy request before CLA80/INS10/Le0, Core APDU status before CTAP status, Data slice transfer and ordinary failure clearing. B6 owns full chain/SW/SCP/intermediate cleanup."),
    ("Backend/ICtap2Backend.cs", [4, 20], "Narrow borrowed backend contract and response status/notification transfer checked against both implementations. Typed decode/RP validation and actual response ownership remain separate."),
    ("Credentials/Ctap2CredentialOptions.cs", [3], "Actual Core ConsoleCredentialReader call-site preset uses Min4/Max63 char counts before default UTF8. Current XML accurately distinguishes code units, encoded bytes, normalization and device policy; keep owner alive through final await."),
    ("Pin/ClientPin.cs", [2, 3, 5, 6, 16], "Complete helper implementation read for assigned type/disposal contracts: borrows session/PIN, owns supplied protocol disposal, fresh protocol exchanges, byte-only validation, no retry loop, permission fallback drops RP/scope, retry defaults and token output copies. No whole-workflow lease."),
    ("Pin/PinUvAuthHelpers.cs", [5, 6], "Complete assigned ECDH method read with V1/V2 callers: coordinate storage/width checks, forced BCL P256 import, missing required labels, import outside derivation catch, and Z before public-dictionary allocation."),
    ("Pin/PinUvAuthProtocolV1.cs", [5, 6], "Complete implementation context read: raw Z finally, direct BCL SHA256/AES/HMAC, same shared key, output arrays and unzeroed fullHash stack; Dispose only sets flag. Generic Core catalog is bypassed."),
    ("Pin/PinUvAuthProtocolV2.cs", [5, 6], "Complete implementation context read: two HKDF contexts, output partial-failure zeroing, AES half/random IV, MAC half, independent decrypt output and disposed flag. No provider-wide erasure or token capability inference."),
    ("Pin/IPinUvAuthProtocol.cs", [5, 6], "Assigned type contract separates synchronous borrowing, caller-owned outputs, V1/V2 structure and disposed flags. Actual implementation/provider/profile validation checked narrowly; constructing protocol does not negotiate."),
    ("Pin/PinRetryStatus.cs", [3], "Retry snapshot type remarks read with actual ClientPin parser. Count/false defaults are not safe-retry permission; temporary PIN_AUTH_BLOCKED and exhausted PIN_BLOCKED remain separate."),
    ("Pin/UserVerificationRetryStatus.cs", [3], "UV snapshot remarks read with ClientPin parser and original retry table: no standard UV power-cycle member; configured PIN/reset recovery differs from blindly trying again."),
    ("Pin/ClientPinSubCommand.cs", [], "Assigned command-reference context read for PIN helper boundary; numeric request semantics remain CTAP-owned and no Core retry/capability enforcement is implied."),
    ("Cose/CoseArkgP256SeedKey.cs", [7], "Assigned representation/Encode units and full Decode/reconstruction context read: alg dispatch, wrapped/direct nested encodings, width padding versus actual P256 points, distinct seed/derived identifiers and expired draft10 partial agreement."),
    ("Cose/CoseKey.cs", [6, 7, 11], "Assigned open carrier/Encode/dispatch units plus complete parser context read. Core constants supply readable kty values; finite catalog never coerces unknown responses. PIN peer validation is separate; modeled encoders omit unknown fields."),
    ("Cose/CoseAlgorithm.cs", [7], "Assigned open-algorithm type contract read; Core concrete KeyType and COSE numeric algorithm identity are separate. No support/default/profile inference from a constant."),
    ("Arkg/ArkgP256.cs", [7], "Complete thin wrapper read: internal Core ArkgPrimitivesCreator receives spans; native point/math implementations and CTAP wire/draft representation retain separate ownership and authority."),
    ("Crypto/EncryptedMetadataDecryptor.cs", [15], "Complete helper context read for assigned contract: direct BCL HKDF/AES128CBC, IV-only/block input tolerance, caller-owned plaintext/key, failed output and key derivation scopes. Generic bytes give no PPUAT issuance/authenticity proof."),
    ("Cbor/AaguidConverter.cs", [23], "Complete converter context read: mixed-endian first4/2/2 convention is host-independent; implementation reverses only on little-endian hosts. Existing source/old little-endian framework evidence, no new big-endian execution."),
    ("Cbor/CtapRequestBuilder.cs", [16], "Complete builder context read: immediate retained clones, deferred callbacks, canonical map completion, new command array and finally-cleared Encode intermediate. Captured copies/writer internal storage retain separate lifetimes."),
    ("Cbor/Ctap2SessionRequestEncoding.cs", [16], "Assigned pure encoder contract and duplicate-parameter method checked with session caller and actual encoding assertions; byte lifetime is separate from negotiation/coherence and ordered algorithm preference."),
    ("Cbor/CoseKeyWriter.cs", [6], "Shared COSE map-writing context read with ClientPin emitted platform-key dictionary; encoding does not validate the peer or prove profile negotiation."),
    ("Cbor/CtapResponseParser.cs", [11], "Assigned optional-reader contract identifies encoded-value handling rather than missing map-key semantics. Full-envelope and required/conditional checks remain consuming-model work."),
    ("Credentials/MakeCredentialResponse.cs", [4, 10, 11], "Assigned model contract checked against full decoder: copied authData/largeBlobKey, borrowed attestation/unsigned slices, key6 and incomplete outer preservation. Backend transfer precedes decode; no disposable model owner."),
    ("Credentials/GetAssertionResponse.cs", [4, 10, 11], "Assigned model contract checked against full decoder: independent arrays, key8 skipped, uint32-to-int count and null descriptor without request/version recovery. User opaque ID/display privacy actor conditions remain distinct."),
    ("Credentials/AuthenticatorData.cs", [4, 11], "Assigned structure/ownership contract checked against full parser: fixed37 prefix, independent raw/RP/ED copies, AT extraction, suffix without one-map validation, received bits versus verified events."),
    ("Credentials/AttestedCredentialData.cs", [11, 23], "Assigned layout/extraction context read: length framing and copied credential/key bytes, network-order AAGUID conversion and extracting one encoded value without complete COSE/profile verification."),
    ("Credentials/AttestationStatement.cs", [4], "Assigned statement contract checked against borrowed parent slices/materialized byte strings. Trust/RP validation and complete response ownership remain separate."),
    ("Credentials/GetAssertionOptions.cs", [2, 4], "Assigned options contract preserves borrowed mutable lists/bytes through awaited call; pinUvAuth and UV encoding is separate from authenticator token state or RP policy."),
    ("Credentials/MakeCredentialOptions.cs", [2, 4], "Assigned options contract preserves borrowed mutable lists/bytes through awaited call and describes caller cleanup; firmware/catalog metadata supplies no permission enforcement."),
    ("Credentials/PublicKeyCredentialTypes.cs", [4], "Assigned descriptor contract read for constructor borrowing versus parser materialization; advertised transport hints never choose a reachable Core interface by themselves."),
    ("Credentials/AttestationFormat.cs", [], "Assigned open format-reference context carries no Core mechanism; identifier recognition is separate from codec/trust support. Preserve existing applet audit scope."),
    ("AuthenticatorInfo.cs", [4, 17], "Assigned GetInfo/parser/firmware/algorithm units read with full session caller: raw envelope preservation, missing defaults, packed low24 version, readonly options and open numeric algorithms. Fresh GetInfo does not update effective session firmware."),
    ("ICtap2Session.cs", [2, 4, 18], "Assigned interface type contract read with full session implementation: exchange refusal versus caller workflow coordination, cooperative HID/SmartCard cancellation, response/presence versus RP outcome."),
    ("CredentialManagement/StoredCredentialInfo.cs", [4], "Assigned owner/public-key/constructor units checked against full Decode/Dispose: one private envelope clone, key view, failure/dispose clearing and independently decoded public fields; original response remains borrowed."),
    ("CredentialManagement/CredentialEnumerationResult.cs", [4], "Assigned owning-result contract/Dispose read with enumeration failure path and captured-view tests; clears entry clones, retains public fields/collection access and never clears decoder source."),
    ("CredentialManagement/CredentialManagement.cs", [2, 4, 16], "Assigned type and full credential-enumeration method read with helpers: eager begin/next, reported first total, borrowed token/protocol, entry-clone cleanup on continuation error and no whole-sequence guard."),
    ("CredentialManagement/CredentialManagementModels.cs", [4], "Assigned metadata model context is public device metadata; raw clone versus source lifetime checked in existing ownership pattern, without treating every response byte as secret by direction."),
    ("CredentialManagement/RelyingPartyEnumerationResult.cs", [2], "Assigned result contract read in caller context: readonly collection/report total, complete eager sequence remains caller-coordinated."),
    ("CredentialManagement/CredManagementSubCommand.cs", [], "Assigned standard-command citation context remains applet-owned; Core framing/backend transports numeric payload without credential-management capability/profile enforcement."),
    ("LargeBlobs/LargeBlobStorage.cs", [2, 12, 13, 16], "Complete storage implementation context read: borrowed services/token, cached limits, sequential fragments, full-array modification, discarded successful plaintext probes and separate auth/request copies. Individual Core exchanges supply no transaction/workflow reservation."),
    ("LargeBlobs/ILargeBlobStorage.cs", [2, 12, 13], "Assigned storage boundary contract read: borrowed dependencies and caller plaintext/secret lifetime, shared-array coordination, no conflict/rollback/resume. Conditional standard authorization differs from current SDK route."),
    ("LargeBlobs/LargeBlobData.cs", [12, 13], "Assigned format/cap/preservation contract and relevant compression/probe ownership context read; GCM/DEFLATE/plaintext owners are applet-level, separate from Core transport limits."),
    ("Extensions/Shared/ExtensionBuilder.cs", [14, 16], "Assigned builder contract read with complete hmac-secret helper and encode context: spans borrowed, saltData/encrypted/tag copies, deferred CBOR copying and missing failure cleanup. No Core catalog/transport layer grants policy enforcement."),
    ("Extensions/Shared/ExtensionOutput.cs", [4, 10, 11], "Assigned output-parser contract read for reconstructed value arrays/getter copies versus raw parent bytes; convenience parsing cannot substitute for complete signed/unsigned CTAP response preservation or disposal ownership."),
    ("Extensions/PreviewSign/PreviewSignRegistrationOutput.cs", [4, 7, 8], "Assigned accessors/constructor contract and implementation read: syntactic extraction and borrowed fields; TryGetGeneratedKey propagates seed marker where other extractor passes derived algorithm."),
    ("Extensions/PreviewSign/PreviewSignDerivedKey.cs", [7, 9], "Assigned VerifySignature/constructor/padding units read with DER converter: fixed P256/SHA256 raw-message verifier, missing exhaustion/INTEGER checks and separate borrowed/copied handle/context lifetimes."),
    ("Extensions/PreviewSign/PreviewSignGeneratedKey.cs", [7, 8], "Assigned generated-key contract read with full derivation body: Core mathematical delegation, borrowed device handle and copied returned context; property algorithm does not dispatch verifier/math."),
    ("Extensions/PreviewSign/PreviewSignCbor.cs", [7, 8, 16], "Assigned shared wire-encoder/registration units read with extraction context; explicit experimental vendor-v4 status, encoded copies and distinct seed/derived/signing roles, no trust verification."),
    ("Extensions/PreviewSign/", [4, 7, 16], "Complete assigned typed input/output/args contracts read: borrowed input memory, independently encoded/decoded copies and experimental vendor profile. TBS/prehashing and additionalArgs semantics remain caller/algorithm responsibilities."),
    ("Extensions/HmacSecret/", [4, 14], "Assigned hmac-secret DTO contracts read with actual builder/V1/V2 path: borrowed key-agreement inputs, independent encrypted/output storage, verified/unverified random credential variants, no cleanup owner or automatic token authorization."),
    ("Extensions/LargeBlob/", [4, 10], "Assigned legacy convenience-container contract read: synthetic combined map differs from top-level largeBlobKey/storage and optional direct unsigned extension; copies have separate lifetime."),
    ("Extensions/", [4, 16], "Complete assigned extension type/reference contracts read for borrowed/copy/profile ownership; Core framing supplies neither RP enforcement nor capability/shape negotiation. Previously investigated applet wire rules retain their original scope."),
    ("BioEnrollment/", [2, 16], "Assigned biometric type/reference contracts read for borrowed session/protocol/token, silent generic exchange and absent workflow/automatic cancel guarantees. Known applet wire/schema issues retain their prior audit IDs, rather than a new Core re-audit."),
    ("Config/", [2, 16], "Assigned administrative type/reference contracts read for borrowed session/protocol/token and persistent state: generic exchange, explicit capability/auth requirements, no rollback and no sequence-wide reservation. Existing applet subcommand questions retain original IDs."),
    ("src/Ctap2/src/Ctap/", [], "Assigned CTAP command/status/exception vocabulary read against backend classification: transport/APDU/CTAP/model errors retain distinct origin; protocol constants do not establish device support."),
]

TEST_NOTES = {
    "Ctap2BackendLifecycleTests.cs": "Complete backend tests/helper callbacks read: borrowed response arrays, CBOR0x10/no INIT, mapped terminal outcomes and actual untransferred response clearing on ordinary callback failure. Throwing diagnostic provider is untested.",
    "Ctap2SessionHidCrossLayerTests.cs": "Complete methods/fake bridge read: production Mac owner over IHidInputBridge fake, full request before one CANCEL, held original continuation before outcome, same CID reuse and pending borrowed terminal wake. No native Mac execution.",
    "Ctap2SessionLinuxCrossLayerTests.cs": "Complete file/fake report/reactor context read: production Linux public slot/device/owner, held invocation versus pending completion, borrowed reuse, cancellation correlation and owned claim through true drain. No hardware/native/AOT execution.",
    "Ctap2SessionTests.cs": "Complete667-line file/fakes read: owned async teardown, shared disposal failure, post-disposal refusal, borrowed sync-disposal failure assertion, policy/intent contexts and callback precedence. Versions-only fixtures omit AAGUID; no whole-workflow lease tested.",
    "IYubiKeyExtensionsTransportTests.cs": "Complete selected methods and all fake connection probes read: exact requested interface, no fallback after held HID, no-connect invalid requests and original failure preservation. Probes fail before protocol/app applet initialization.",
    "CredentialManagementWireTests.cs": "All selected assertions and complete wire helpers read: modeled request fields, totals/order/readonly results, actual successful clone-view zeroing, continuation exceptions and unchanged borrowed responses. FakeV2 tags16 and absent adjacency protection remain explicit.",
    "CredentialManagementModelsTests.cs": "Selected disposal method and fixture ownership context read: captured raw/key clone aliases zero, caller input preserved, independently decoded public fields usable. Preamble's broader parser assertions retain old applet-review scope.",
    "CoseArkgP256SeedKeyTests.cs": "Selected full decode/round-trip assertions read: synthetic coordinates, wrapped SDK-profile maps, exact role identifiers and fields. Neither valid points nor draft-wide/native interoperability is asserted.",
    "CoseKeyForwardCompatibilityTests.cs": "Complete selected unknown-key method read: unknownkty99/alg-70000 with array parameter preserved in RawCbor. No PIN peer-profile validation or arbitrary malformed acceptance oracle.",
    "PreviewSignGeneratedKeyTests.cs": "Selected KAT/finite-context methods read: one exact derived point and nonempty ticket, retained device handle/copied context, finite input comparisons. No accessor algorithm or universal uniqueness proof.",
    "Ctap2SessionRequestEncodingTests.cs": "Selected full encoding assertions read: ordered required map keys and mixed optional independent fields. Deliberately uv+token/short fake tags are encoding fixtures, not authorized profile examples.",
    "Ctap2RequestAuditReproTests.cs": "Selected unit/integration methods read: false-UP and duplicate-list rejection, distinct array order, actual missing-field failure branch. Integration setup can perform creation/GetInfo/PIN normalization before rejected target call.",
    "Ctap2AlgorithmSupportTests.cs": "Selected operations and common helper read: requested algorithm advertisement, optional early return, nonempty ID/key assertion and best-effort cleanup. No returned COSE curve/alg verification or Core catalog capability inference.",
    "Ctap2AuthenticatorConfigTests.cs": "Selected full workflows read: initial normalization, toggle compare then success-only toggle-back, fixed minimum target and forced-PIN legacy token status. Persistent minimum/recovery/token cleanup stay scoped; no full restoration proof.",
    "Ctap2BioEnrollmentTests.cs": "Sole selected full operation read: normalized PIN, capability presence, bio-token acquisition/finally and sensor kind/sample hint. No capture/enrollment/cancel interoperability is exercised.",
    "Ctap2CredBlobTests.cs": "Selected complete operations read: unconditional retrieval byte equality but conditional stored flag, broad stack-name transport skips and broad RP cleanup. Historical Mac diagnostic is not current producer/native failure proof.",
    "Ctap2CredProtectTests.cs": "Selected complete operations read: both level2 assertions use PIN auth, level3 positive UV and conditional policy echoes. No negative no-UV enforcement oracle; broad RP cleanup and secret residuals remain.",
    "Ctap2CredentialManagementTests.cs": "Selected complete operations read: normalized PIN and token helpers, enumeration ID containment, metadata nonnegative counts, delete test permits successful later assertion. Original oracle/cleanup gaps stay applet-owned.",
    "Ctap2CredentialManagementExtendedTests.cs": "Selected full update/enumeration workflows read: exact matching displayName and two created IDs, broad RP cleanup, management token cleared only after success. No global command lease or all-fields update oracle.",
    "Ctap2EncryptedMetadataTests.cs": "Selected full operations read: optional early returns, ciphertext inequality/plaintext equality/nonempty output and wrong-token inequality. Matching/persistent-token and capability proof remain outside those assertions.",
    "Ctap2EnhancedPinTests.cs": "Selected full operations read: firmware early return, configured clientPin after helper, optional scalar/count bounds and known-state normalization. No complete Unicode/device policy/cleanup oracle.",
    "Ctap2EnterpriseAttestationTests.cs": "Selected full operation read: ep gate, persistent config, statement presence and conditional epAtt, broad RP cleanup. No certificate/profile/trust or restored-config proof.",
    "Ctap2FipsComplianceTests.cs": "Selected complete metadata assertions read: optional/early-return cases, V2 advertisement and clientPin key presence. No certified-device/operation qualification; Core metadata/capability catalogs are not evidence of FIPS execution.",
    "Ctap2GetAssertionTests.cs": "Selected full operations read: nonempty signature, exact resident user ID and NoCredentials status, with normalization/token/cleanup context. Signatures/RP binding are not independently verified.",
    "Ctap2GetInfoTests.cs": "Selected complete metadata assertions read: advertisement/options/lengths and optional reported firmware, guarded extension disjunction. Default helper normalization is stateful; optional absence/early returns retain their limits.",
    "Ctap2HmacSecretTests.cs": "Selected full operations read: conditional registration output, decrypted32/nonzero and two same-salt secret comparisons, protocol key agreement and success-only cleanup. No all-UV/protocol or end-to-end buffer guarantee.",
    "Ctap2LargeBlobTests.cs": "Selected complete workflows read: largeBlobKey creation, storage write/read equality and delete/null; broad RP cleanup and success-only token clearing. No multichunk transaction, cancellation recovery or arbitrary foreign-client oracle.",
    "Ctap2LargeBlobAuditReproTests.cs": "Selected integration format/manual interop operations read: single-fragment hash/GCM/DEFLATE check and optional key export/environment input. No external client automatically runs; immutable secret text and incomplete cleanup stay explicit. Unit file assigned import context only.",
    "Ctap2MakeCredentialTests.cs": "Selected complete workflows read: nonempty IDs, attested fields, exclusion status and four accepted missing-auth errors. No attestation trust and AAGUID-length fallback does not prove presence; cleanup best effort.",
    "Ctap2MinPinLengthTests.cs": "Selected complete operations read: optional output/range only, no disclosure-RP configuration. PIN normalization and broad RP cleanup remain; no unauthorized-disclosure or policy-change oracle.",
    "Ctap2PinManagementTests.cs": "Selected complete operations read: change then new-PIN nonempty token, best-effort restoration and token-authorized creation with uv omitted. No old-PIN/old-token rejection or actual UV-bit oracle.",
    "Ctap2PreviewSignTests.cs": "Complete783-line operation/helper context read: registration outputs, one ARKG/prehash/sign/verify roundtrip; negative catches include setup. No generated attestation trust or actual execution; draft/source/provider/profile qualifications preserved.",
    "Ctap2PrfTests.cs": "Selected full operations read: hmac-secret registration and one ordinary assertion, no salts/PRF outputs/determinism oracle. Setup/cleanup and version gate remain sample assumptions.",
    "Ctap2SessionSimpleTests.cs": "Selected full methods read: direct borrowed SmartCard versus helper default PIN setup, advertised data, same-interface ownership refusal, selection completion and arbitrary reset CtapException. Existence supplies no fresh physical execution.",
    "Ctap2SmartCardTests.cs": "Selected full operations read: explicit SmartCard preference with PIN-normalizing helper, info/version/algorithm assertions, broad NotSupported skip. No precise SELECT-absence/native-SCP qualification.",
    "Ctap2TransportTests.cs": "Selected full workflows read: explicitly pinned SmartCard creation or singleton HID assertion, nonempty model/ID/signature and RP-wide cleanup. No returned ID/algorithm/signature verification oracle.",
    "Ctap2TestStateExtensions.cs": "Complete normalization and assigned helper extension block read: known PIN set/verify/two-change recovery before callback, default normalization, owned async session scope and Windows access diagnostic. B4 must qualify diagnostic/access advice.",
    "UserPresenceProbeTests.cs": "Selected complete operations/prompt read: requested/resolved identity/outcome/token and subsequent nonempty info. Requires human interaction and real platform lane; timer is signal rather than fixed completion bound.",
    "IntegrationTestBase.cs": "Complete empty base context read: WithYubiKey shared discovery is the real fixture entry; no hidden utility or safety/transport guarantee supplied by inheritance.",
    "Ctap2TestData.cs": "Selected constants/generators and ES256/EdDSA parameter contracts read as synthetic test data; open numeric identifiers do not prove advertised/provider/device support.",
    "Ctap2TestHelpers.cs": "Assigned helper preamble read and actual normalization/token helper dependencies traced for selected integration calls. Approved known-PIN utilities are not safe general production collection/restoration templates.",
}

CLEAN_LABELS = [
    "GetCtap2InfoAsync(",
    "ForCtap2Pin()",
    "SmartCardBackend_SendCborAsync_UsesBorrowedProtocol",
    "HidBackend_SendCborAsync_UsesBorrowedProtocol",
    "SmartCardBackend_WhenRequestThrows_DoesNotTransmitOrResolve",
    "MakeCredentialAsync_InsideIntentScope_ReportsIntentWithOperation",
    "CreateCtap2SessionAsync_ScpWithDefault_RoutesToSmartCard",
    "StoredCredentialInfo_Dispose_ZeroesOwnedViewsAndKeepsPublicFieldsUsable",
    "N1_MakeCredentialEncodesDistinctCredentialParametersInOrder",
]


def annotations(path):
    name = pathlib.Path(path).name
    if "/tests/" in path and name in TEST_NOTES:
        return [22], TEST_NOTES[name]
    if "/tests/" in path and name.endswith(".cs"):
        return [], "Complete assigned import/type-preamble or small disposal/encoding unit read for Core-facing applicability. Selected range is not a renewed audit of unassigned applet parser/cryptographic test bodies; no new execution, conformance or cleanup oracle inferred."
    if path.endswith(".csproj"):
        return [], "Complete project contract read: Core/project references, inherited build policy, package/test-runner or AOT opt-in remain distinct from compilation/execution. No build, restore, test or package load performed."
    if "PublicAPI." in path:
        return [], "Complete snapshot read as analyzer/declaration support, including Core firmware/options/algorithm carrier signatures and borrowed Memory/result APIs. Generated/accessor/equality/removal entries are not consumer prose or independent compiler verification. Preserve bytes; no API change authorized."
    if "/examples/" in path:
        if name == "DeviceSelector.cs":
            return [1, 17, 21], "Assigned selector contract/policy read with actual shared selector configuration and both reset caller bodies. HID/SmartCard eligibility is distinct from applet support; reset target-retention sentence conflicts with actual independent reselection."
        if name == "FidoPinHelper.cs":
            return [3, 16], "Assigned PIN entry/CLI encode methods read with Core reader/preset context. Byte owner retained through caller await; immutable CLI/redirected strings and encoding-failure ownership remain separate, synchronous prompts do not inherit operation cancellation."
        if name == "Program.cs":
            return [2, 3, 16, 21], "Complete723-line top-level program read including implicit helper calls: using PIN owners, sequential owned sessions inside helpers, reset pre/post independent selection, broad helper error results and parser/help behavior. Runtime output remains unchanged."
        if "Reset" in name:
            return [2, 17, 21], "Assigned reset/helper units and full reset caller context read: explicit concrete transport, preflight then fresh independent selection, borrowed old transport in menu, owned sessions and broad failure result. Human target/timing/state duties remain."
        if name in ("MakeCredential.cs", "GetAssertion.cs"):
            return [2, 4, 16], "Selected complete demo operation read: owned session, caller PIN borrowed, local V2/ClientPin disposal, finally-cleared token but auth-param copies remain. Random hash/user and conflicting uv+token sample calls retain prior applet findings."
        if "PinManagement" in name:
            return [2, 3, 16], "All assigned PIN sample operations read: owned session each call, selected V2, borrowed PIN, token finally and retry snapshot mapping; Verify asks GA without RP. General result catches flatten cancellation; existing scope issues preserved."
        if "CredentialManagement" in name or name == "CredentialsMenu.cs":
            return [2, 4, 16], "Assigned credential operations read with own helper scopes: token borrowed through eager sequence, result ownership transfers after session teardown, catch/finally disposes untransferred result and token. Menu uses returned RP hash and disposes credential results."
        if "Bio" in name or "Fingerprints" in name:
            return [2, 16], "Assigned biometric menu/helper units read: owned sessions, protocol/token scopes and progress callback across begin/next; thrown cancel/callback can skip explicit enrollment cancel. Known applet wire/schema issues remain independent from Core transport cancellation."
        if "Config" in name:
            return [2, 16], "Assigned admin operations read: owned sessions, borrowed PIN, ClientPin protocol lifetime and token finally; persistent toggle/policy mutations have no rollback/readback guarantee and general catches flatten cancellation."
        return [], "Complete assigned example context read for selection/metadata/ownership and helper boundaries. Displayed metadata and helper result success supply no device-support, certified-operation or RP-verification proof."
    for pattern, ids, note in RULES:
        if pattern in path:
            return ids, note
    raise AssertionError(("Missing authored scope annotation", path))


packet = load(RUN / "inventory/packets/BOUNDARY-Ctap2.json")
all_units = load(RUN / "inventory/semantic-units.json")
selected = [u for u in all_units if u["id"] in set(packet["unit_ids"])]
assert len(selected) == len(set(packet["unit_ids"])) == 590
inventory_files = {f["path"]: f for f in load(RUN / "inventory/files.json")}
head = git("rev-parse", "HEAD").decode().strip()
file_rows = []
unit_rows = []
for path in packet["disposition_paths"]:
    data = (REPO / path).read_bytes()
    text = data.decode("utf-8-sig")
    pinned = git("show", f"{PIN}:{path}")
    blob = git("rev-parse", f"{PIN}:{path}").decode().strip()
    assigned = [u for u in selected if u["path"] == path]
    candidate_numbers, note = annotations(path)
    candidate_ids = [f"CORE-CTAP-X{n:03}" for n in candidate_numbers]
    assert sha(data) == inventory_files[path]["sha256"], path
    assert normalized(data) == normalized(pinned), path
    for u in assigned:
        assert sha(data) == u["source_sha256"], u["id"]
        excerpt = text[u["start_character"]:u["end_character_exclusive"]]
        assert sha(excerpt.encode()) == u["range_sha256"], u["id"]
        status = "READ-CANDIDATE-CONTEXT" if candidate_ids else "READ-CLEAN"
        direct = ((path == "src/Ctap2/README.md" and u["label"] == "## Constraints")
                  or (path == "src/Ctap2/CLAUDE.md" and u["first_line"] == 14)
                  or (path.endswith("Cli/Prompts/DeviceSelector.cs") and u["first_line"] == 1)
                  or ("Backend/" in path and "SendCborAsync(" in u["label"]))
        if direct:
            status = "READ-CANDIDATE"
        is_nominee = any(label in u["label"] for label in CLEAN_LABELS) or (
            path == "src/Ctap2/README.md" and u["label"] == "## Getting started")
        if is_nominee:
            status = "READ-CLEAN"
        if "PublicAPI." in path:
            status = "READ-EXCLUSION"
        unit_rows.append({
            **{k: u[k] for k in ("id", "path", "label", "category", "first_line", "last_line", "start_character", "end_character_exclusive", "source_sha256", "range_sha256")},
            "source_baseline": PIN,
            "read": "complete-assigned-unit-with-boundary-context",
            "disposition": status,
            "candidate_ids": candidate_ids,
            "reason": note,
            "local_contract": u["label"],
            "scope": "CTAP2/Core call-site responsibilities; unassigned applet semantics retain original whole-module audit",
            "exclusion_reason": "Analyzer API support; generated/synthesized/declaration entries excluded from prose-clean denominator; compiler shape VALID-NOT-CHECKED." if "PublicAPI." in path else None,
            "independent_review": "PENDING",
            "compiler_and_runtime_validation": "VALID-NOT-CHECKED",
            "sampling": "NOMINATED" if is_nominee else ("INELIGIBLE-EXCLUSION" if status == "READ-EXCLUSION" else "ELIGIBLE-IF-CLEAN-AND-NO-LATER-EDITED-OVERLAP"),
        })
    file_rows.append({
        "path": path, "source_baseline": PIN, "git_blob": blob,
        "source_sha256": sha(data), "pinned_git_bytes_sha256": sha(pinned),
        "normalized_sha256": sha(normalized(data).encode()),
        "raw_matches_pinned_git": data == pinned, "bom_crlf_normalized_matches_pin": True,
        "assigned_unit_ids": [u["id"] for u in assigned],
        "selected_ranges": [[u["first_line"], u["last_line"]] for u in assigned],
        "read_scope": "complete selected ranges plus actual boundary caller/helper/assertion context; broader full-file reads named in observation" if assigned else "candidate-file disposition only; complete nullable directive inspected",
        "observation": note,
        "candidate_ids": candidate_ids,
        "candidate_file_disposition": "UNCHANGED-REOPEN-ON-LISTED-TRIGGER" if candidate_ids else "UNCHANGED-RETAIN-IN-BOUNDARY-SCOPE",
        "future_editor": "fresh CTAP2 boundary editor; shared/Core and WebAuthn paths retain their exclusive owners",
        "independent_review": "PENDING",
    })

assert {u["id"] for u in unit_rows} == set(packet["unit_ids"])
assert {f["path"] for f in file_rows} == set(packet["disposition_paths"])
write("unit-dispositions.json", unit_rows)
write("file-dispositions.json", file_rows)
write("clean-nominees.json", [u for u in unit_rows if u["sampling"] == "NOMINATED"])
write("candidate-index.json", [{
    "id": f"CORE-CTAP-X{number:03}",
    "definition": "candidates.md",
    "assigned_unit_ids": [u["id"] for u in unit_rows if f"CORE-CTAP-X{number:03}" in u["candidate_ids"]],
    "candidate_associated_unchanged_paths": [f["path"] for f in file_rows if f"CORE-CTAP-X{number:03}" in f["candidate_ids"]],
    "documentation": "Retain accurate qualifications or propose localized correction; precise action and reopening trigger in candidates.md; independent review pending.",
    "implementation": "Current-source observation or inherited question; no repair authorized or performed.",
    "policy": "Existing behavior is distinguished from desired contract; unresolved choices in candidates.md remain maintainer/coordinator work.",
    "qualification": "Source scope only; runtime/provider/native/platform/device lanes retain separate recorded limits.",
    "accepted_core_id": None,
} for number in range(1, 24)])

GROUP_OUTCOMES = {
    "credential-length-units": ([3], "Current CTAP preset already documents UTF16 versus bytes/code points; actual Core reader/ClientPin call path confirms the allocation decision. Preserve XREF003 and IMP006; OpenPGP retains its own profile owner.", "C plus CTAP2/BOUNDARY-OpenPgp and maintainer"),
    "ctap-response-ownership": ([4, 16], "Current factory/backend/raw/model trace confirms multiple independent response/request owners and successful slices/copies. StoredCredentialInfo private-clone cleanup has exact scope; WA callers receive a separate mandatory follow-up.", "CTAP2 plus B6/C; BOUNDARY-WebAuthn for WA aliases"),
    "ctap-crypto-cleanup": ([5, 13, 14, 15], "Specific local crypto output/Z/MAC, blob probe/compression and salt/metadata failure scopes remain separate. Core provider/native residuals retain C/B4 ownership; do not merge independent sites.", "CTAP2 and C/B4 for actual delegated helpers"),
    "ctap-peer-key-profile": ([6], "CTAP helper checks coordinate type/length and forces provider P256; required labels/optional restrictions remain missing. Core metadata/structural catalog offers no implicit fix.", "CTAP2 profile owner"),
    "ctap-aaguid-portability": ([23], "CTAP converter host-conditioned mixed-endian reversal and WA default-value/equality are distinct source-visible concerns. Existing little-endian probe remains historical; no new big-endian execution.", "CTAP2 converter; BOUNDARY-WebAuthn default-value owner"),
    "ctap-decoder-envelope": ([11], "Structural ED/AT/count/full-envelope/request-version validation remains CTAP-owned. Raw framing validates another domain; preserve full-parent versus nested-reader distinction.", "CTAP2; BOUNDARY-WebAuthn recovery separately"),
    "ctap-arkg-profile": ([7, 8], "Preserve draft10 -65700 erratum and remaining outer/nested/dkalg/profile questions; separate actual algorithm-propagation defect from profile choice and Core math. WA empty-handle policy receives its own boundary follow-up.", "CTAP2/maintainer plus C and BOUNDARY-WebAuthn"),
    "ctap-unsigned-outputs": ([10], "GetAssertion still skips unsigned key8; Make models key6 slices. Old #4 retained without additive bug count. Direct-extension support choice and WA route remain separate from transport framing.", "CTAP2 plus BOUNDARY-WebAuthn"),
    "ctap-blob-format": ([12], "Signed origSize/key ranges, read cap versus uncapped writes and unknown encoded-slice preservation retain separate policy/implementation facets; no Core transaction inferred.", "CTAP2 blob format/maintainer"),
    "ctap-preview-der": ([9], "Local signature converter exhaustion/INTEGER admission and raw-message fixed verifier remain distinct from Core key ASN1. Actual provider acceptance/impact remains unexecuted.", "CTAP2 experimental verifier"),
    "ctap-metadata-wrapper": ([15], "IV-only/generic blocks/nonempty token acceptance remains explicit wrapper-versus-generic choice, distinct from secret output cleanup and authenticator PPUAT state.", "CTAP2/maintainer"),
    "assertion-user-docs": ([], "Documentation resolution retained for User opaque-ID/display fields and version-specific credential descriptor. No automatic stripping and no closure of WA-IMP010 recovery or DEC002 actor policy.", "BOUNDARY-WebAuthn for distinct engineering/actor follow-ups"),
    "native-selected-user-actor": ([], "Native selected AuthenticationResponse.User is retained; actor/privacy/serializer choice remains open before any filtering decision. No field removed or vulnerability asserted.", "BOUNDARY-WebAuthn plus maintainer/Astra"),
    "ctap-profile-evidence": ([7, 17, 22], "Preserve exact SKU/earliest-support/reset5.5.2/5.5.3/vendor-profile/unsigned-introduction gaps; current Core version policy and published shim source add no missing hardware/producer evidence.", "Maintainer/vendor profile evidence; B4/B6 and BOUNDARY-WebAuthn as applicable"),
}
registry = load(RUN / "inherited-findings/registry.json")
inputs = load(RUN / "inventory/inherited-packet-inputs.json")["BOUNDARY-Ctap2"]
inherited = []
for row in registry["rows"]:
    if row["key"] not in inputs["provenance_keys"]:
        continue
    nums, outcome, owner = GROUP_OUTCOMES[row["dedup_group"]]
    inherited.append({
        "key": row["key"], "dedup_group": row["dedup_group"],
        "authority": row["authority"], "historical_definition": row["historical_definition"],
        "registry_current_state_preserved": row["current_state"],
        "registry_current_conclusion_preserved": row["current_conclusion"],
        "boundary_outcome": outcome,
        "local_candidates": [f"CORE-CTAP-X{n:03}" for n in nums],
        "documentation": "Retain accurate current qualifications; exact new/reopening paths in candidates.md; independent review pending.",
        "implementation": "Source-backed scoped observation or explicit policy gap; no repair authorized or executed.",
        "qualification": "No new runtime/native/device execution; source/provider/platform/hardware gaps retained.",
        "next_owner": owner,
        "independent_review": "PENDING",
    })
assert {r["key"] for r in inherited} == set(inputs["provenance_keys"])
write("inherited-dispositions.json", inherited)

sources = [
    "plan.md", "dispatch-map.md", "inventory.md", "inherited-findings.md",
    "inherited-findings/boundary-seeds.md", "inherited-findings/registry.json",
    "inventory/inherited-routing.json", "inventory/inherited-packet-inputs.json",
    "inventory/packets/BOUNDARY-Ctap2.json", "sources/python/context.md", "sources/python/manifest.json",
    "investigation-A.md", "investigation-B1.md", "investigation-B2.md", "investigation-B3.md", "investigation-C.md", "investigation-B5.md",
    "investigation/B5/sources/register.md", "investigation/B5/sources/captures.json", "investigation/B5/sources/captures-followup.json",
    "investigation/B5/sources/ctap20.html", "investigation/B5/sources/ctap21.html", "investigation/B5/sources/ctap23.html",
    "investigation/B5/sources/ctap21.txt", "investigation/B5/sources/ctap23.txt",
    "investigation/C/sources/final-source-register.md", "investigation/B3/sources/register.md",
]
old = REPO / "docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001"
source_paths = [RUN / p for p in sources] + [old / p for p in (
    "triage.md", "triage-addendum.md", "boundary-final.md", "boundary-review-final.md",
    "evidence/e/primary-excerpts.md", "evidence/g/boundary-originals.md",
    "evidence/framework/Guid-v10.0.14.cs", "evidence/framework/CborWriter-v10.0.2.cs",
)]
write("source-identities.json", [{"path": str(p.relative_to(REPO)), "sha256": sha(p.read_bytes()), "bytes": p.stat().st_size, "role": "reused immutable authority/original or completed family evidence; semantic scope in sources.md"} for p in source_paths])
dependency_scopes = {
    "src/Core/src/Sessions/ApplicationSession.cs": "complete factory/prepare/configure/prompt/disposal implementation",
    "src/Core/src/Devices/YubiKeyConnectionExtensions.cs": "180–305 resolution/open/failure helper and typed routing",
    "src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs": "complete protocol implementation for actual CTAP call path",
    "src/Core/src/Protocols/Fido/Hid/FidoHidConnection.cs": "complete retained synchronous compatibility adapter",
    "src/Core/src/Protocols/Fido/Hid/IFidoHidConnection.cs": "complete public raw connection contract",
    "src/Core/src/Utilities/ExchangeGuard.cs": "complete admission/drain implementation; no whole-workflow lease",
    "src/Core/src/Credentials/UserPresenceNotification.cs": "complete handle state/capture/request/resolution/error logging implementation",
    "src/Core/src/Credentials/UserPresenceIntent.cs": "complete scope/capture implementation",
    "src/Core/src/Credentials/ConsoleCredentialReader.cs": "complete interactive/redirected/confirmation/encoding implementation",
    "src/Core/src/Credentials/CredentialReaderOptions.cs": "complete generic units/preset contract",
    "src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs": "complete copy/Data/Raw/status/Clear owner",
    "src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs": "complete local prepare/configure/guard/SW boundary; full processor behavior remains B6",
    "src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.Scp.cs": "complete handshake call and wrapper transfer; internals remain B6",
    "src/Core/src/Devices/Feature.cs": "complete firmware compatibility policy",
    "src/Core/src/Devices/FirmwareVersion.cs": "1–120 development/default and comparison contract; A/B5 completed policy evidence reused",
    "src/Core/src/Cryptography/AlgorithmInfo.cs": "complete applicability/identifier metadata contract",
    "src/Core/src/Cryptography/Cose/CoseKeyInfo.cs": "complete kty/crv metadata contract",
    "src/Core/src/Cryptography/CryptographyProviders.cs": "complete provider declaration/reach contract, current CTAP BCL bypass traced",
    "src/Core/src/Cryptography/ArkgPrimitives.cs": "complete factory reference boundary",
    "src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs": "1–400 inspected native point/Z/delegated derivation scopes; C retains full mathematical/native helper audit",
    "src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs": "complete User/CredentialId result contract; native actor and recovery remain BOUNDARY-WebAuthn",
    "src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs": "complete borrowed CBOR/parsed copy decoder and encoder context",
    "src/WebAuthn/src/Extensions/Outputs/PrfOutput.cs": "complete result ownership contract",
    "src/WebAuthn/src/Extensions/Codec/PrfCodec.cs": "complete aggregate/range/result-copy cleanup context",
    "src/WebAuthn/src/Cose/Aaguid.cs": "complete default/equality/converter dependency",
    "src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs": "complete empty-handle and borrowed input policy; broader caller remains WA owner",
}
dependency_rows = []
for path, scope in dependency_scopes.items():
    data = (REPO / path).read_bytes()
    pinned = git("show", f"{PIN}:{path}")
    assert normalized(data) == normalized(pinned), path
    dependency_rows.append({"path": path, "source_baseline": PIN, "source_sha256": sha(data),
                            "pinned_git_bytes_sha256": sha(pinned), "read_scope": scope,
                            "primary_unit_disposition_authority": False,
                            "bom_crlf_normalized_matches_pin": True})
write("dependency-identities.json", dependency_rows)
counts = dict(collections.Counter(u["disposition"] for u in unit_rows))
write("verification.json", {
    "source_baseline": PIN, "observed_metadata_head": head,
    "packet_sha256": sha((RUN / "inventory/packets/BOUNDARY-Ctap2.json").read_bytes()),
    "assigned_units": len(unit_rows), "selected_files": len(packet["paths"]),
    "candidate_disposition_paths": len(file_rows), "disposition_counts": counts,
    "exact_unit_id_set": True, "exact_candidate_path_set": True,
    "all_source_and_range_hashes_match_inventory": True,
    "all_candidate_paths_bom_crlf_normalized_match_source_pin": True,
    "inherited_provenance_keys": len(inherited), "exact_inherited_key_set": True,
    "narrow_dependency_paths": len(dependency_rows),
    "clean_nominees": sum(u["sampling"] == "NOMINATED" for u in unit_rows),
    "sdk_builds": 0, "sdk_tests": 0, "formatter_runs": 0,
    "crypto_native_device_executions": 0, "commits_index_ref_writes": 0, "subagents": 0,
    "independent_content_review": "PENDING",
    "mandatory_followups": ["B4 Windows/support and later user-ready delta", "B6 APDU/SCP", "BOUNDARY-WebAuthn actor/recovery and selected WA aliases"],
    "completion": "Assigned static investigation complete; separate mechanism follow-ups, independent review and qualification remain open.",
})
print(json.dumps({"units": len(unit_rows), "files": len(file_rows), "dispositions": counts, "inherited": len(inherited)}, indent=2))
