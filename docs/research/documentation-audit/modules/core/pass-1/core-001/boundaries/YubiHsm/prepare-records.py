"""Serialize this boundary's authored dispositions; perform static identity checks only."""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[9]
RUN = ROOT / "docs/research/documentation-audit/modules/core/pass-1/core-001"
OUT = Path(__file__).resolve().parent
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(path):
    return json.loads(path.read_text())


def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n")


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def normalize(data):
    return data.decode("utf-8-sig").replace("\r\n", "\n")


# Authored after complete file reads. These observations are not generated from hashes.
NOTES = {
    "CLAUDE.md": "Applet/HSM actors, gate table, borrowed inputs/prompt, key response variant, notification terminal and historical qualification are accurate bounded context. Reset transaction/auth and whole-workflow/copy scope need localized follow-up; no whole module rewrite.",
    "PACKAGE_README.md": "Package manages YubiKey credentials and returns session keys; external HSM library completes enrollment/handshake. Alpha warning and transitive Core/install/documentation links retain scope; no new HSM qualification claim.",
    "README.md": "Read complete listing/enrollment/HSM adapter/rotation/prompt blocks and enclosing cleanup. Application callbacks have declared typed contracts, owned resources and borrowed-secret responsibilities. Preserve supplied-cryptogram limitation; add local channel-history/full-workflow distinction without replacing truthful qualifications.",
    "src/Backend/HsmAuthBackend.cs": "Thin SmartCard select/send wrapper. SELECT finds tag79 with TryFindValue, exact width3 or5.4.3 fallback; malformed TLV errors still propagate. Public version copies differ from sensitive material; no result crypto performed.",
    "src/Backend/IHsmAuthBackend.cs": "Two backend operations expose initial version and APDU send/throw policy; no independent resource/disposal owner. Concrete Select remains internal helper, not missing public API.",
    "src/HsmAuthAlgorithm.cs": "Open byte enum38/39, local width switches16/32 and public point65; unknown identifiers deliberately throw only when algorithm-specific properties are queried. HSM KDF profile is separate from Core catalog support.",
    "src/HsmAuthCredential.cs": "Detached LIST snapshot, nullable touch, metadata-only init properties, all-field record equality versus case-insensitive label ordering. Retry restoration/deletion claims explicitly attributed to vendor overview.",
    "src/HsmAuthRetryException.cs": "Retry count holder derives from APDU exception and documents method coverage. XML catch-every-protocol-failure assurance exceeds APDU status scope; Parse/transport failures can have other exception types.",
    "src/HsmAuthSession.cs": "Complete 987-line source read: factory binding/prepare/select/version/config/SCP/backend, all operations, validation, retry/presence/result helpers and finally cleanup. Preserve independent lower copies and reset graph/flags, per-exchange versus public workflow; see analysis tables for exact lines.",
    "src/IHsmAuthSession.cs": "All public method XML read, including actor/borrowed-secret roles, exact context lengths, cryptogram56 qualification, challenge/public point limits, retry policy and reset policy. Local inherited channel/auth/full-task guidance is the focused gap; existing response-profile warnings remain useful.",
    "src/IYubiKeyExtensions.cs": "Only SmartCard transport resolved once; referenced options shallow-copy, direct Create awaited and OwnConnection after success. List convenience encloses session disposal. Failure cleanup belongs outer Core helper and source-based native release distinction.",
    "src/Properties/AssemblyInfo.cs": "Friend declaration exposes internals to actual HSM unit project, not integration applet protocol. Context-only declaration, unchanged.",
    "src/PublicAPI.Shipped.txt": "Nullable header only; no shipped signatures to semantically disposition. Preserve exact file, no API edit.",
    "src/PublicAPI.Unshipped.txt": "Complete84-line snapshot reconciled with eight public types, inherited interface/session machinery and current operations/feature fields. Snapshot lists48 parser constant, not complete wire-response universal grammar. No compiled API binding performed.",
    "src/SessionKeys.cs": "Strict48 admission, three independently allocated16-byte arrays, span views, idempotent clear/disposed refusal. Final response and copied keys have independent lifetimes; caller/connector copies and exceptional partial allocations remain separate. Existing56 remarks retain uncertainty.",
    "src/Yubico.YubiKit.YubiHsm.csproj": "Core-only shipping reference and descriptive applet-not-connector package boundary. Shared NativeAOT opt-in is analyzer/metadata participation rather than runtime qualification.",
    "tests/CLAUDE.md": "Actual reset-before/plain versus optional-SCP-body resource distinction, persistent cleanup and assertion-accurate shape/touch/dummy rejection explanations. Existing required-testing link retained; no old rejected link finding reintroduced.",
    "tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthAsymmetricTests.cs": "Complete four methods plus NewToken/default/password helpers: scalar export cleanup, algorithm metadata, nonempty challenge, multiply invalid dummy handshake and exact ApduException/nonzeroSW, touch metadata only. CTS/input/persistent cleanup limitations retain HSM-IMP-006.",
    "tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthSessionTests.cs": "Complete nine methods: reset/LIST, symmetric roundtrip, derived algorithm only, new-management-key PUT, retries8, omission-only three lengths, generated point width/prefix, replacement-password SEnc width and touch metadata. No authenticated external HSM; helper leaves persistence and NewToken owners unscoped.",
    "tests/Yubico.YubiKit.YubiHsm.IntegrationTests/TestExtensions/HsmAuthTestStateExtensions.cs": "Shared connection owner surrounds separate plain reset session then body session with optionalSCP. Configuration forwarded both; SCP only body. Full action awaited, no post-body reset/readback. No automatically qualified secure reset.",
    "tests/Yubico.YubiKit.YubiHsm.IntegrationTests/Yubico.YubiKit.YubiHsm.IntegrationTests.csproj": "net10, xUnitv2/traditional runner, hardware parallelism config and actual Tests.Shared/Core/HSM references. Metadata filtering and runner participation are not actual device execution.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/CredentialPasswordTests.cs": "Helper/input tests check raw-byte length, null padding, nonalias under source zeroing, UTF-8 encoded fixtures and public ParamName. Session helper uses borrowed recorder with high override and one SELECT; no operation bytes/native lifetime proof for rejected inputs.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthCredentialTests.cs": "Ordinal-ignore-case sort examples, null comparison and selected record retry difference. Actual record equality covers all fields; selected fixtures do not independently vary every property.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthRetryExceptionTests.cs": "Nine command paths use queued 63Cx and compare count/SW and selected messages; synthetic 63C9 is explicitly beyond the documented initial eight. Asymmetric calculation's expected Invalid credential password substring matches the actual credential helper. GET_CHALLENGE remains outside this suite; recorder proves no device retry transitions.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs": "Exact selected request headers/TLV bytes, context guard command counts,560/571 password inclusion, password-selector tag positions, borrowed factory failure and reset override. Fake queues supply crypto data without validation. Reset test carries no SCP handshake or post-reset protected operation.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/LabelValidationTests.cs": "UTF-8 byte-width encoding/bound checks distinguish21/22CJK characters; null/empty guards, no malformed label roundtrip or normalization policy assertion.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/Pbkdf2DerivationTests.cs": "Same BCL primitive with separate constants checks parameter wiring; nonzero/distinct halves and deterministic/different-password comparisons. Existing comment correctly limits KnownVector name. No crypto recomputation executed here.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/RetryExtractionTests.cs": "Explicit retry nibble0/1/5/8/15 and unrelated status vectors. Pure decoding scope, distinct from device max retry semantics.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/SessionKeysTests.cs": "Three fixture-region content checks; selected non48 rejection; reflection observes all actual arrays before/after Dispose; helper zeros actual RawData.56 absent from fixture cases, lower transport/callback/decrypted copies outside oracle.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/TouchNotificationTests.cs": "Full policy/order/lifecycle tests and prompt implementation read. Fake command-count2 before CALCULATE; same context/basis/operation/tokenNone; prompt-cancel before wire, request/no-resolution failure, success-resolution identity and primaryAPDU retention. Source disposes produced keys but tests lack actual-array callback-failure witness.",
    "tests/Yubico.YubiKit.YubiHsm.UnitTests/Yubico.YubiKit.YubiHsm.UnitTests.csproj": "net10/xUnitv3 executable/MTP runner and actual shared/helper references; no direct traditional test SDK. Test-source inspection only.",
    "examples/HsmAuthTool/HsmAuthTool.csproj": "Actual HSM/Core/Management/Cli.Shared/Cli.Commands references and Spectre/Hosting package participation. Compilation needs full project/helper context, no build conducted.",
    "examples/HsmAuthTool/Program.cs": "Whole interactive/noninteractive dispatch, owned CTS lifetimes and error output read. Diagnostic secret-output warning is accurate. CLI CancelKeyPress handler captures disposed CTS without unregister; one-process top-level lifetime is distinct from reusable helper policy. No runtime strings edited.",
    "examples/HsmAuthTool/Cli/Commands/AccessCommand.cs": "Current/new keys parsed and length-checked then selector before clearing scope; early wrong-length/selection exits leave copies. Owned session inside try, finally clears only after entry. Recoverable key storage remains operator responsibility.",
    "examples/HsmAuthTool/Cli/Commands/CommandArgs.cs": "Option/flag/positional parser reads label before options, hex FormatException mapsnull, default management key fresh16zero bytes. No secure-string or command-line secrecy claim.",
    "examples/HsmAuthTool/Cli/Commands/CredentialsCommand.cs": "All list/add/delete/generate dispatch and helpers read. Management key finally includes credential resolution in main paths, but keyEnc/keyMac second allocation and invalid private/management length exits strand copies. Session awaited/owned; emitted keys and visible arguments remain diagnostic; no HSM provisioning.",
    "examples/HsmAuthTool/Cli/Commands/InfoCommand.cs": "Owned convenience session reads retries/LIST and prints session.FirmwareVersion. This is effective HSM policy rather than an independent Management/raw revision fact. No hidden authentication or prompt.",
    "examples/HsmAuthTool/Cli/Commands/ResetCommand.cs": "Confirmation before selection, owned session then awaited destructive reset. Cancellation of confirmation differs from admitted device operation. Printed success follows method success, no independent persistent readback.",
    "examples/HsmAuthTool/Cli/Menus/CredentialMenu.cs": "Five complete selected-device/await-using session/example calls. Session encloses synchronous prompts and all awaited operations; example borrows it. Eligibility is SmartCard only, not applet support.",
    "examples/HsmAuthTool/Cli/Menus/ManagementKeyMenu.cs": "Rotation and retry menu each owns one session through awaited example; storage/secret acquisition is example/operator-owned. No extra lifetime owner or retries loop.",
    "examples/HsmAuthTool/Cli/Menus/ResetMenu.cs": "Owns selected-device session through confirmation/reset example, then disposes. Does not add rollback, SCP restoration or physical postcondition.",
    "examples/HsmAuthTool/Cli/Menus/SessionKeyMenu.cs": "Owns session without prompt, invokes symmetric-only calculation example. Accurate comment preserves physical policy versus absent notification service.",
    "examples/HsmAuthTool/Cli/Output/OutputHelpers.cs": "Complete delegation to shared output/confirmation including WriteHex borrowed spans. Formatting can create immutable output independently of cleared input; no local secret redaction.",
    "examples/HsmAuthTool/Cli/Prompts/DeviceSelector.cs": "Shared selector configured SmartCard eligibility and FirstEligible noninteractive choice. Shared helper still performs All discovery/enrichment; selected transport prerequisite is narrower than enabled applet or historical failure explanation.",
    "examples/HsmAuthTool/HsmAuthExamples/AddDerivedCredential.cs": "Using credential owners encompass derivation/credential prompts and PUT; management hex string persists independently; management byte array finally clear. Derivation enrollment parity/HSM object remains external.",
    "examples/HsmAuthTool/HsmAuthExamples/AddSymmetricCredential.cs": "Generated or parsed pair before touch prompt/management parse and clearing try; later failures can strand pair. WriteHex exposes long-lived keys. Existing comments accurately warn; owned credential disposal is separate.",
    "examples/HsmAuthTool/HsmAuthExamples/CalculateSessionKeys.cs": "Lists every algorithm but symmetric CALCULATE always; prompts visible context/optional cryptogram, parses under finally, checks8+8, owns context/parts and keys through diagnostic output. Preserves56 warning and external authenticated HSM role.",
    "examples/HsmAuthTool/HsmAuthExamples/ChangeManagementKey.cs": "Both visible hex strings and two parses precede try; second parse can strand first key. Active key recovery/communication uncertainty external; finally clears both only after entry.",
    "examples/HsmAuthTool/HsmAuthExamples/DeleteCredential.cs": "Lists/chooses label, visible management string and destructive confirmation before allocation; management array clear surrounds awaited DELETE. Session is menu-borrowed; public labels need no secret cleanup.",
    "examples/HsmAuthTool/HsmAuthExamples/GenerateAsymmetricCredential.cs": "Credential owner and management array enclose generate then public-key export; on-device private generation and external HSM enrollment distinct. GetPublicKey width-only validation inherited.",
    "examples/HsmAuthTool/HsmAuthExamples/GetManagementKeyRetries.cs": "One borrowed-session read and low-count diagnostic warning; no secret input, decrement, guessing loop or attempt reset.",
    "examples/HsmAuthTool/HsmAuthExamples/ListCredentials.cs": "Borrowed session LIST snapshots -> shared table; unknown algorithm/touch presentation stays metadata. No authentication, key copy or physical selection guarantee.",
    "examples/HsmAuthTool/HsmAuthExamples/ResetHsmAuth.cs": "Explicit destructive confirmation then borrowed session Reset; success messages follow method return. Does not independently read back defaults/empty list or restore SCP.",
}

# Exact original-unit IDs selected by substantive investigation, not all neighboring accessors.
CANDIDATE_IDS = {
    "X001": "f4a1e21e6fa236ac c4d04ce56e188f14 46216fa4bb477d2d".split(),
    "X002": "3ceb4c49a09206e1".split(),
    "X003": "851afa54e70f73e4 04bbe6a58cdfc823".split(),
    "X004": "f4566399afef2281".split(),
    "X007": "d9d9f5602dca2c1b".split(),
}
RELATED_IDS = {
    "X001": "71bd62554fc11bca 261e37fb24419500 ac34af5219eeb53f 7142071d50d6d4b7 eac986726ec4b27c cbb38c36d4b5a688".split(),
    "X002": "bbb19744be9a1564 00e817cb60c87971 2f26c7ab9c51df33 6c71d3e46dddd93e 0ba7de36589f8d1c 92c1b5732f4ff9b8 36849dd60dafde94 512eeb739fe18a50 b1e69ebc4ce3b9be 084fa91d45ef9b3d b7d95c24d028842c".split(),
    "X003": "f719110786267801 1efcc215023da128 d2053b26d9b0f9c9 9a67b77c4103c979".split(),
    "X004": "0e2a6b8014b211f1 c143c8dcfc8723a2 3270b468d31dc8c9".split(),
    "X005": "1ae1a01ebf7f3e5d 229cbbf0c816393f bdf74ceb5f2099b0 29006ac1de1a54fe 328fa7631af38d2d 839fa04eb5965974".split(),
    "X006": "570cf77dfe20754d 66033149e18930bd a64f8e655d13ab4d ae7529f6ebd6a069 4c6a50aaddea40e8".split(),
    "X007": "45663117c7a6201c fa30412cc3d933a1 8f16d503ab482d17 bbd2f1c7724f4002 6f10e0219c81beab 99a7878f9ae10fdf 1000e7195b8cf769 4760ac6b0859b54d 1e8b86a55b54704e 44f88b2df5bc2e57 7490364af15b4c37 b6882212fe01e6f0 fad4dd2efec2dcf5 e8ca6f7526282022 3a4109de6d14d68b e685a68c7e261d51 5884e757b9dc6bcf e01b2c496b4b2231 208d789ca32e5779 bdf0b9fa2e22c201 0769b13e6234b944".split(),
}

ALIASES = {
    "X001": ["yubihsm-001:HSM-DEP-001", "yubihsm-001:HSM-DEP-002", "CORE-B5-C001", "CORE-B5-C006", "CORE-B6-C011", "CORE-B6-C012", "CORE-B6-C015"],
    "X002": ["yubihsm-001:HSM-DEP-003", "yubihsm-001:HSM-IMP-002", "CORE-C-C005", "CORE-C-C006", "CORE-C-C007", "CORE-C-C020", "CORE-B6-C002", "CORE-B6-C006", "CORE-B6-C007"],
    "X003": ["yubihsm-001:HSM-DEP-002", "CORE-B5-C006", "CORE-C-C008"],
    "X004": ["yubihsm-001:HSM-DEP-001", "CORE-A-C015", "CORE-B5-C001"],
    "X005": ["yubihsm-001:HSM-IMP-008", "yubihsm-001:HSM-IMP-007"],
    "X006": ["yubihsm-001:HSM-IMP-001", "yubihsm-001:HSM-IMP-003", "yubihsm-001:HSM-IMP-007"],
    "X007": ["yubihsm-001:HSM-IMP-002", "yubihsm-001:HSM-IMP-004", "yubihsm-001:HSM-IMP-005", "yubihsm-001:HSM-IMP-006"],
}

PATHS = {
    "X001": ["CLAUDE.md", "README.md", "src/HsmAuthSession.cs", "src/IHsmAuthSession.cs", "src/IYubiKeyExtensions.cs", "tests/CLAUDE.md", "tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs", "tests/Yubico.YubiKit.YubiHsm.IntegrationTests/TestExtensions/HsmAuthTestStateExtensions.cs", "examples/HsmAuthTool/Cli/Commands/ResetCommand.cs", "examples/HsmAuthTool/Cli/Menus/ResetMenu.cs", "examples/HsmAuthTool/HsmAuthExamples/ResetHsmAuth.cs"],
    "X002": ["CLAUDE.md", "README.md", "src/HsmAuthSession.cs", "src/SessionKeys.cs", "tests/Yubico.YubiKit.YubiHsm.UnitTests/SessionKeysTests.cs"],
    "X003": ["CLAUDE.md", "README.md", "src/HsmAuthSession.cs", "src/IHsmAuthSession.cs", "src/IYubiKeyExtensions.cs", "tests/CLAUDE.md", "tests/Yubico.YubiKit.YubiHsm.UnitTests/TouchNotificationTests.cs", "examples/HsmAuthTool/Cli/Menus/SessionKeyMenu.cs", "examples/HsmAuthTool/HsmAuthExamples/CalculateSessionKeys.cs"],
    "X004": ["CLAUDE.md", "README.md", "src/Backend/HsmAuthBackend.cs", "src/HsmAuthSession.cs", "src/IYubiKeyExtensions.cs", "examples/HsmAuthTool/Cli/Commands/InfoCommand.cs", "tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs", "tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthSessionTests.cs"],
    "X005": ["CLAUDE.md", "README.md", "src/HsmAuthSession.cs", "src/IHsmAuthSession.cs", "src/SessionKeys.cs", "tests/Yubico.YubiKit.YubiHsm.UnitTests/SessionKeysTests.cs", "tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs", "examples/HsmAuthTool/HsmAuthExamples/CalculateSessionKeys.cs"],
    "X006": ["CLAUDE.md", "README.md", "src/HsmAuthSession.cs", "src/IHsmAuthSession.cs", "src/HsmAuthAlgorithm.cs", "src/HsmAuthCredential.cs", "src/HsmAuthRetryException.cs", "tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthRetryExceptionTests.cs", "tests/Yubico.YubiKit.YubiHsm.UnitTests/CredentialPasswordTests.cs", "tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthAsymmetricTests.cs"],
    "X007": [p for p in NOTES if p.startswith("examples/") or p.startswith("tests/")] + ["CLAUDE.md", "README.md", "src/HsmAuthRetryException.cs"],
}

# Keep generated prose readable while preserving exact inherited IDs, labels and hashes.
PROSE_SPACING = {
    "tag79": "tag 79", "width3": "width 3", "or5.4.3": "or 5.4.3",
    "enum38/39": "enum 38/39", "switches16/32": "switches 16/32", "point65": "point 65",
    "exact48": "exact 48", "Complete84-line": "Complete 84-line", "Snapshot lists48": "Snapshot lists 48",
    "universal grammar": "universal grammar", "Strict48": "Strict 48-byte", "allocated16-byte": "allocated 16-byte",
    "Existing56": "Existing 56-byte", "LIST79": "LIST 79", "read/SCP": "read/SCP",
    "optionalSCP": "optional SCP", "net10": ".NET 10", "xUnitv2": "xUnit v2", "xUnitv3": "xUnit v3",
    "explicitly beyond documented initial8": "explicitly beyond the documented initial eight",
    "queued63Cx": "queued 63Cx", "synthetic63C9": "synthetic 63C9", "documented initial8": "documented initial eight",
    "560/571": "5.6.0/5.7.1", "distinguish21/22CJK": "distinguish 21/22 CJK",
    "nibble0/1/5/8/15": "nibble 0/1/5/8/15", "non48": "non-48-byte",
    "fixture cases": "fixture cases", "command-count2": "command-count 2", "tokenNone": "token None",
    "primaryAPDU": "primary APDU", "fresh16zero": "fresh 16-zero", "checks8+8": "checks 8+8",
    "Preserves56": "Preserves 56-byte", "historical6982": "historical 6982", "Historical6982": "Historical 6982",
    "strict48 versus": "strict 48-byte versus", "cryptogram56": "cryptogram 56-byte",
    "result/receipt": "result/receipt", "algorithm AES128": "algorithm AES-128",
}


def prose(text):
    for old, new in PROSE_SPACING.items():
        text = text.replace(old, new)
    return text


NOTES = {p: prose(text) for p, text in NOTES.items()}

packet = load(RUN / "inventory/packets/BOUNDARY-YubiHsm.json")
all_units = {u["id"]: u for u in load(RUN / "inventory/semantic-units.json")}
assert len(set(packet["unit_ids"])) == len(packet["unit_ids"])
assert set(NOTES) == {p.removeprefix("src/YubiHsm/") for p in packet["disposition_paths"]}
head = git("rev-parse", "HEAD").decode().strip()

files = []
for path in packet["disposition_paths"]:
    raw = (ROOT / path).read_bytes()
    pinned = git("show", f"{PIN}:{path}")
    assert normalize(raw) == normalize(pinned), path
    rel = path.removeprefix("src/YubiHsm/")
    files.append({
        "path": path, "source_baseline": PIN, "source_sha256": sha(raw),
        "pinned_git_blob": git("rev-parse", f"{PIN}:{path}").decode().strip(),
        "pinned_sha256": sha(pinned), "normalized_lf_sha256": sha(normalize(raw).encode()),
        "pinned_equal_after_explicit_utf8_bom_crlf_normalization": True,
        "line_count": len(raw.decode("utf-8-sig").splitlines()),
        "read_status": "READ-COMPLETE", "outcome": "UNCHANGED-REFER-FRESH-EDITOR" if any(rel in ps for ps in PATHS.values()) else "UNCHANGED-RETAIN",
        "candidate_ids": ["CORE-HSM-" + k for k, ps in PATHS.items() if rel in ps],
        "authored_context": NOTES[rel],
        "unit_ids": [uid for uid in packet["unit_ids"] if all_units[uid]["path"] == path],
        "review_status": "INDEPENDENT-REVIEW-PENDING", "validation_status": "VALID-NOT-CHECKED",
    })

units = []
for uid in packet["unit_ids"]:
    u = dict(all_units[uid])
    raw = (ROOT / u["path"]).read_bytes()
    # Inventory hashes use UTF-8 decoded text with BOM preserved and newline bytes unchanged.
    text = raw.decode("utf-8")
    assert sha(raw) == u["source_sha256"], uid
    assert sha(text[u["start_character"]:u["end_character_exclusive"]].encode()) == u["range_sha256"], uid
    short = uid.rsplit(":", 1)[1]
    direct = ["CORE-HSM-" + k for k, ids in CANDIDATE_IDS.items() if short in ids]
    related = ["CORE-HSM-" + k for k, ids in RELATED_IDS.items() if short in ids]
    state = "READ-CANDIDATE" if direct else "READ-CONTEXT" if related else "READ-CLEAN"
    # Declarations, API snapshots and configuration are retained context rather than sampling padding.
    if not direct and not related and (u["last_line"] - u["first_line"] < 4 or u["path"].endswith((".csproj", ".txt"))):
        state = "READ-CONTEXT"
    u.update({"status": state, "read_status": "READ-COMPLETE", "candidate_ids": direct,
              "related_candidate_ids": related, "source_baseline": PIN,
              "context": NOTES[u["path"].removeprefix("src/YubiHsm/")],
              "unit_observation": "Full assigned unit and enclosing implementation/XML/helper/assertion context read: " + u["label"],
              "reason": "Localized proposed correction; see candidates.md and analysis.md." if direct else "Retain existing accurately qualified behavior/inherited candidate or declaration context; no new prose change required here." if state == "READ-CONTEXT" else "Retain current unit: contract/comment matches the read source and actual bounded oracle; no independent runtime acceptance inferred.",
              "review_status": "INDEPENDENT-REVIEW-PENDING", "sample_status": "ELIGIBLE-UNSAMPLED" if state == "READ-CLEAN" else "INELIGIBLE-CANDIDATE-OR-CONTEXT", "validation_status": "VALID-NOT-CHECKED"})
    units.append(u)

save("file-dispositions.json", files)
save("unit-dispositions.json", units)

candidate_index = []
for key in ALIASES:
    candidate_index.append({"id": "CORE-HSM-" + key,
        "state": "PROVISIONAL-LOCAL-CANDIDATE", "accepted_id": None,
        "documentation": "PROPOSED-LOCAL-CLARIFICATION" if key in {"X001", "X002", "X003", "X004", "X007"} else "RETAIN-EXISTING-QUALIFICATION",
        "runtime": "DEFERRED-SEPARATE-AUTHORIZATION", "decision": "MAINTAINER-POLICY-SEPARATE", "qualification": "UNRUN-PRESERVE-EXISTING-GAPS",
        "aliases_nonadditive": ALIASES[key],
        "unit_ids": [u["id"] for u in units if "CORE-HSM-" + key in u["candidate_ids"]],
        "related_unit_ids": [u["id"] for u in units if "CORE-HSM-" + key in u["related_candidate_ids"]],
        "unchanged_paths": ["src/YubiHsm/" + p for p in PATHS[key]],
        "reopening_map": "candidates.md#core-hsm-" + key.lower() + " (complete candidate section, XML/guide/sample/test/migration triggers)",
    })
save("candidate-index.json", candidate_index)

registry = load(RUN / "inherited-findings/registry.json")
inputs = load(RUN / "inventory/inherited-packet-inputs.json")["BOUNDARY-YubiHsm"]
outcomes = {
    "yubihsm-001:HSM-DEP-001": "Completed actual HSM SELECT/override/major-zero/reset retention trace; source mechanism answered. Documentation clarification/independent review and old-firmware/NFC/OS qualification remain separate. X001/X004; preserve metadata/raw/display distinctions.",
    "yubihsm-001:HSM-DEP-002": "Completed owned/borrowed creation/disposal, uncancellable admitted exchange, LIST/prompt/CALCULATE/result resolution and whole-workflow gaps. Historical6982/Failed plus fresh recovery retained without native-interruption/timeout cause. X001/X003; no whole-method lease or physical closure.",
    "yubihsm-001:HSM-DEP-003": "Completed caller/TLV/request/formatter/native-result/APDU/fragment/decrypted/result-copy graph. Current helper reaches actual response array; lower copies remain independent. X002 and B6/C aliases; no end-to-end erasure closure.",
    "yubihsm-001:HSM-IMP-008": "Still applet/profile parser candidate; strict48 versus supplied-cryptogram56 failure retained. Raw regions, firmware range, result/receipt contract and live HSM/connector remain open. No duplicate Core cryptographic defect. X005.",
}
inherited = []
for row in registry["rows"]:
    if row["key"] in inputs["provenance_keys"]:
        inherited.append({"key": row["key"], "original_id": row["original_id"],
            "dedup_group": row["dedup_group"], "registry_status_unchanged": row["current_state"],
            "registry_conclusion_unchanged": row["current_conclusion"], "authority": row["authority"],
            "boundary_outcome": outcomes.get(row["key"], "Shared mechanism comparison only: HSM actual call sites are traced in analysis.md; applet-specific version/reset/key/cache/workflow/native behavior retains its own boundary owner. No cross-applet closure or new bug count."),
            "implementation": "NO-CHANGE", "documentation": "COORDINATOR-TRIAGE-AND-FRESH-REVIEW-PENDING", "qualification": "NO-NEW-EXECUTION"})
assert {r["key"] for r in inherited} == set(inputs["provenance_keys"])
engineering_outcomes = {
    1: "GET_CHALLENGE still uses backend general APDU errors; nine other paths use typed retry translation. Desired consistency remains a decision. X006.",
    2: "Sample acquisition/second-allocation/early-exit cleanup gaps remain. SessionKeys and final response clearing have separate actual owners; lower pipeline copies are not erased by final-owner clear. X002/X007.",
    3: "LIST still skips short recognized records, uses permissive UTF-8 decode and preserves unknown algorithm/touch metadata. Parser/raw-preservation policy remains deferred. X006.",
    4: "Interactive calculation still lists all algorithms, always calls symmetric CALCULATE and configures no presence prompt. X007.",
    5: "Visible/argv strings and generated/session key output remain diagnostic product-policy choices. Current warning retained; no input/output changes authorized. X007.",
    6: "Actual assertion/CTS/secret-input/persistent-state scopes retained; byte/prompt oracles are fake-backed. Asymmetric retry-message assertion matches source; an investigator misreading was corrected and supplies no new finding. X007.",
    7: "HSM CALCULATE performs no generic Core EC point validation. Context widths and public-key response width checks are narrower than profile/trust validation. Challenge returns unchecked shape. X005/X006.",
    8: "Supplied cryptogram 56-versus-48 parser remains applet/profile uncertainty; raw regions/firmware/result grammar/live HSM authentication are open. X005; no duplicate Core crypto finding.",
}
save("inherited-dispositions.json", {"rows": inherited, "groups": inputs["group_ids"],
    "registry_sha256": sha((RUN / "inherited-findings/registry.json").read_bytes()),
    "module_engineering_aliases": [{"key": "yubihsm-001:HSM-IMP-" + f"{n:03}", "state": "DEFERRED-PASS-2-UNCHANGED", "outcome": engineering_outcomes[n]} for n in range(1, 9)]})

nominee_ids = "b0802a12b8e9559f 9b6e960fa937439b e496e59b826a88f7 52467afc34736ceb f25726f00414a916 2c00745a92a32066 93f15f446e18cfec d5c991345bf8977b 75649a8bd0ae3bb6 23c6b77c26150d0a ac8ed9a859c531f5 34d46c059c1d7d4e 32cf859f510562b0 56ac675d5c5a5b82 b6f2f47dc485e27f e7182c006c715df1 b3a4e2c9e4dc1f4e".split()
nominees = [u for u in units if u["id"].rsplit(":", 1)[1] in nominee_ids]
assert len(nominees) == len(nominee_ids)
assert all(u["status"] == "READ-CLEAN" for u in nominees)
save("clean-nominees.json", [{"id": u["id"], "path": u["path"], "first_line": u["first_line"], "last_line": u["last_line"], "source_sha256": u["source_sha256"], "range_sha256": u["range_sha256"], "label": u["label"], "context": u["context"], "sample_status": "NOMINATED-NOT-SAMPLED", "limit": "Complete method/narrative and helpers read; exclude later edited overlaps. Assertions are source-defined, not executed results."} for u in nominees])

source_paths = [
    "docs/research/documentation-audit/yubihsm.md",
    "docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001/findings.md",
    "docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001/cryptogram-investigation.md",
    "docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001/evidence/sources.md",
    "docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001/evidence/cryptogram-sources.md",
    "docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001/hardware/results.md",
    "docs/research/documentation-audit/modules/core/pass-1/core-001/sources/python/manifest.json",
    "docs/research/documentation-audit/modules/core/pass-1/core-001/sources/python/context.md",
    "docs/research/documentation-audit/modules/core/pass-1/core-001/investigation/B6/sources/scp03-1.1.2.pdf",
    "docs/research/documentation-audit/modules/core/pass-1/core-001/investigation/B6/sources/scp03-1.1.2.txt",
    "docs/research/documentation-audit/modules/core/pass-1/core-001/investigation/B6/sources/final-source-register.md",
    "docs/research/documentation-audit/modules/core/pass-1/core-001/investigation/B6/ownership-and-handoffs.md",
]
source_paths += ["docs/research/documentation-audit/modules/core/pass-1/core-001/investigation-" + k + ".md" for k in ["A", "B1", "B2", "B3", "B4", "B5", "B6", "C"]]
source_identities = [{"path": p, "sha256": sha((ROOT / p).read_bytes()), "role": "REUSED-READ-ONLY-EVIDENCE-NOT-NEW-EXECUTION"} for p in source_paths]
pyroot = ROOT.parent / "yubikey-manager"
pymanifest = load(RUN / "sources/python/manifest.json")
for path in ["yubikit/hsmauth.py", "tests/test_hsmauth.py"]:
    raw = (pyroot / path).read_bytes()
    expected = next(x["sha256"] for x in pymanifest["files"] if x["path"] == path)
    assert sha(raw) == expected
    source_identities.append({"path": str(pyroot / path), "sha256": sha(raw), "revision": pymanifest["reference_revision"], "role": "COMPLETE-SOURCE-AND-ASSERTIONS-READ-COMPARATOR-NOT-EXECUTED"})
save("source-identities.json", source_identities)

dependencies = {
    "src/Core/src/Sessions/ApplicationSession.cs": (1, 569, "B5", "Complete factory/readiness/auth/presence/disposal methods; borrowed versus owned connection and no whole-workflow lease."),
    "src/Core/src/Sessions/SessionCreationOptions.cs": (1, 80, "B5", "Referenced option values and retained caller-owned prompt; no deep key clone or ownership transfer."),
    "src/Core/src/Devices/Feature.cs": (1, 42, "A", "Major-zero optimistic compatibility policy."),
    "src/Core/src/Devices/FirmwareVersion.cs": (1, 277, "A", "Raw fields, major-zero ordering, parser and optimistic IsAtLeast policy versus device fact."),
    "src/Core/src/Devices/YubiKeyConnectionExtensions.cs": (140, 305, "B5", "Complete relevant transport resolution/open/create/failure-cleanup methods, no fallback."),
    "src/Core/src/Credentials/UserPresenceNotification.cs": (1, 205, "C", "Single-owner handle, intent, request/terminal callback state and primary exception preservation."),
    "src/Core/src/Utilities/ExchangeGuard.cs": (1, 140, "B5", "Pre-entry cancellation and admitted uncancellable exchange/drain; no whole-HSM operation scope."),
    "src/Core/src/Utilities/Tlv.cs": (1, 275, "C", "Owned encoded buffer, borrowed Value views and parsing copies; dispose clears actual storage."),
    "src/Core/src/Utilities/TlvHelper.cs": (1, 269, "C", "Encode/dispose/result ownership; DecodeList temp cleanup; TryFindValue extra copies, public version use."),
    "src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs": (1, 207, "B6", "Actual plain SELECT/configuration/logging/guard/recovery and protocol-only disposal; no native transaction."),
    "src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.Scp.cs": (1, 44, "B6", "SCP initializer receives admitted token and shares guard before wrapper creation."),
    "src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs": (1, 100, "B6", "Sealed copying owner, Data/RawData aliases/status and explicit Clear API."),
    "src/Core/src/Protocols/SmartCard/Apdu/ApduTransmitter.cs": (1, 49, "B6", "Formatted request clear versus transport result cloned into another owner."),
    "src/Core/src/Protocols/SmartCard/Apdu/ChainedApduTransmitter.cs": (1, 66, "B6", "Short 255-byte chunks, offset-dependent recovery callback and secure intermediate refusal."),
    "src/Core/src/Protocols/SmartCard/Apdu/ChainedResponseReceiver.cs": (1, 77, "B6", "61xx fixed followups, dropped fragment owners and success-only writer Clear."),
    "src/Core/src/Protocols/SmartCard/Scp/PcscProtocolScp.cs": (1, 144, "B6", "Protected sends, plain SELECT using secure graph, no reconfigure, shared close/drain."),
    "src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs": (1, 198, "B6", "useScp false bypass, empty-data padding/state advancement, response/decrypt copies and recovery callbacks."),
    "src/Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs": (1, 60, "B6", "Retains/disposes exact StaticKeys owner; record cloning shares contained reference."),
    "src/Core/src/Protocols/SmartCard/Scp/Scp11KeyParameters.cs": (1, 121, "B6", "Retains private/public key and certificate objects; copies list, optional-reference discrepancy and exact object disposal."),
    "src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs": (1, 182, "B1", "Pooled output clear and exact transport-result copy, admitted native worker lifetime and shutdown release qualification."),
    "src/Tests.Shared/RecordingSmartCardConnection.cs": (1, 84, "SHARED", "Copies command bytes, dequeues response without crypto, no-op transaction and disposal counters."),
    "src/Tests.Shared/YubiKeyTestStateExtensions.cs": (140, 198, "SHARED", "Actual WithConnectionAsync enclosing connection disposal and full callback await."),
    "src/Cli.Shared/src/Device/DeviceSelector.cs": (1, 245, "A", "Fresh All discovery, SmartCard eligibility, per-invocation cached/live metadata enrichment and first-eligible noninteractive policy."),
    "src/Cli.Shared/src/Output/SecureCredential.cs": (1, 365, "C", "CLI byte/Rune interactive input versus redirected raw bytes, pool lifetime and owned disposal; no HSM credential ownership transfer."),
    "src/Cli.Shared/src/Output/PinPrompt.cs": (1, 113, "C", "CLI supplied string/prompt resolution and IMemoryOwner lifetime; separate from generic Core reader policy."),
    "src/Cli.Shared/src/Output/OutputHelpers.cs": (1, 161, "SHARED", "Actual hex formatting creates immutable output; escaped metadata table/header text is separate from semantic redaction."),
    "src/Cli.Shared/src/Output/ConfirmationPrompts.cs": (1, 58, "SHARED", "Explicit double-confirmation/RESET word, before HSM mutation; no rollback or device-postcondition guarantee."),
    "src/Cli.Shared/src/Cli/CommandHelper.cs": (1, 39, "SHARED", "Console cancellation handler retains CTS; HSM sample owns returned CTS but helper has no unregister scope."),
    "src/Cli.Shared/src/Cli/InteractiveMenuBuilder.cs": (1, 181, "SHARED", "Full action await and error/cancellation presentation around repeated selection; no detached application-operation drain."),
    "src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs": (94, 151, "SHARED", "Complete HsmAuthHelpers type, especially escaped label/known-or-numeric algorithm/nullable-touch metadata table; broad CLI command bodies remain outside packet."),
}
dependency_records = []
for path, (first, last, owner, context) in dependencies.items():
    raw = (ROOT / path).read_bytes()
    text = raw.decode("utf-8-sig")
    selected = "".join(text.splitlines(keepends=True)[first - 1:last])
    pinned = git("show", f"{PIN}:{path}")
    assert normalize(raw) == normalize(pinned), path
    dependency_records.append({"path": path, "source_baseline": PIN, "source_sha256": sha(raw),
        "pinned_git_blob": git("rev-parse", f"{PIN}:{path}").decode().strip(), "first_line": first, "last_line": last,
        "selected_line_text_sha256": sha(selected.encode()), "read_scope": "DEPENDENCY-ONLY-COMPLETE-RELEVANT-CONTEXT",
        "original_owner": owner, "context": context, "new_coverage_credit": 0})
save("dependency-dispositions.json", dependency_records)

summary = {"packet": "BOUNDARY-YubiHsm", "source_baseline": PIN, "observed_metadata_head": head,
    "packet_sha256": sha((RUN / "inventory/packets/BOUNDARY-YubiHsm.json").read_bytes()),
    "assigned_units": len(units), "selected_paths": len(packet["paths"]), "complete_candidate_files": len(files),
    "complete_candidate_lines": sum(f["line_count"] for f in files), "shared_assigned_units": len(packet["shared_read_unit_ids"]),
    "dispositions": dict(collections.Counter(u["status"] for u in units)), "unread": 0,
    "provisional_candidate_groups": len(candidate_index), "accepted_ids_allocated": 0,
    "inherited_provenance_keys": len(inherited), "inherited_nonadditive_groups": len(inputs["group_ids"]),
    "clean_nominees": len(nominees), "semantic_reading": "COMPLETE-ASSIGNED-STATIC-BOUNDARY-WITH-EXPLICIT-QUALIFICATIONS",
    "independent_review": "PENDING", "product_editing": "NOT-AUTHORIZED-HERE", "compiler_validation": "VALID-NOT-CHECKED",
    "sdk_builds": 0, "sdk_tests": 0, "crypto_native_device_connector_executions": 0,
    "windows_handoff": "WAITING-USER-READY-DELTA", "piv_physical_deferral": "UNCHANGED"}
save("coverage-summary.json", summary)
save("verification.json", {"status": "STATIC-RECORD-IDENTITY-CHECKS-PASSED", "source_baseline": PIN,
    "observed_metadata_head": head, "checked_unit_ids": len(units), "checked_source_and_range_hashes": True,
    "checked_complete_candidate_files_against_pin": len(files), "candidate_path_exhaustion": True,
    "inherited_key_equality": True, "python_original_hashes": True,
    "independent_semantic_review": False, "sdk_or_runtime_execution": False,
    "limits": "Hash checks establish source/record identities, not semantic quality, passing SDK tests or physical release."})
print(json.dumps(summary, indent=2))
