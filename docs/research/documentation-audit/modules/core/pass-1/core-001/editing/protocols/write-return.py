"""Serialize this editor's bounded static return; not a validation framework or SDK runner."""
import difflib
import hashlib
import html
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[9]
RUN = ROOT / "docs/research/documentation-audit/modules/core/pass-1/core-001"
OUT = RUN / "editing/protocols"
BASE = "da66f5d01ba594993974ddd26723e8c300c0a454"
PRODUCT = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def load(path):
    return json.loads((RUN / path).read_text())


def sha(data):
    return hashlib.sha256(data).hexdigest()


def blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()


def emit(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + "\n")


assert git("rev-parse", "HEAD").decode().strip() == BASE
packets = {f: load(f"inventory/packets/{p}.json") for f, p in [
    ("B5", "B5-session-framing"), ("B6", "B6-wire-scp")
]}
eligible = {p for packet in packets.values() for p in packet["paths"]
            if p.startswith("src/Core/") and p.endswith(".cs")}
changed = set(git("diff", "--name-only", BASE, "--", "src/Core").decode().splitlines())
assert changed <= eligible
units = [u for f in packets for u in load(f"investigation/{f}/unit-dispositions.json")]
indices = [c for f in packets for c in load(f"investigation/{f}/candidate-index.json")]
groups = [g for g in load("triage/full-family/map.json")["groups"]
          if any(x.startswith(("CORE-B5-", "CORE-B6-")) for x in g["local_ids"])]
notes = {f: load(f"investigation/{f}/semantic-notes.json") for f in packets}
assoc = {}
for c in indices:
    paths = c.get("direct_paths", []) + [x["path"] for x in c.get("candidate_associated_unchanged_paths", [])]
    for p in paths:
        assoc.setdefault(p, set()).add(c["id"])
for u in units:
    assoc.setdefault(u["path"], set()).update(u.get("candidate_ids", []))

# These local comment corrections also touch related context units outside direct candidate anchors.
extras = {
    "CtapConstants.cs": ["CORE-B5-C002", "CORE-B5-C003"],
    "ApduResponse.cs": ["CORE-B6-C002"],
    "ApduException.cs": ["CORE-B6-C001"],
    "ApduFormatterTests.cs": ["CORE-B6-C001"],
    "ScpKid.cs": ["CORE-B6-C010"],
    "SessionKeys.cs": ["CORE-B6-C006", "CORE-B6-C007"],
    "RawSmartCardNativeBoundaryTests.cs": ["CORE-B5-C008"],
}
for p in eligible:
    assoc.setdefault(p, set()).update(extras.get(Path(p).name, []))

hold = {
    "AnswerToReset.cs": "FPT-082 has P/M/B facets only. Preserve ATR equality/hash/null behavior; no D correction is authorized.",
    "AesCmac.cs": "Hold failed-finalization offset/reset policy and stack initialization for separately selected implementation work. No existing explanatory claim needs rewriting here.",
    "FakeApduProcessor.cs": "Hold retained command aliases and nonproduction formatter behavior for harness work; using a production formatter in wire-sensitive tests requires executable changes.",
    "FakeSmartCardConnection.cs": "Hold async-disposal TODO and harness implementation. Correct stale guide API names through the shared-guide owner, not by changing this fake.",
    "OtpHidProtocolTests.cs": "Retain actual outgoing/pool observers; incoming-report zeroing is unobserved. Hold new ownership oracles and harness changes.",
    "OtpHidProtocolConcurrencyTests.cs": "Retain scripted overlap assertions; hold failure-safe gate release/drain and stronger NEO/physical oracles as harness work.",
    "ApplicationSessionDisposalTests.cs": "Retain disposal/query assertions. Hold finally-owned release/drain for failing assertions; no whole-workflow drain inference.",
    "RawCtapHidSessionTests.cs": "Retain existing lazy-INIT/borrowed-connection assertions; hold failure-safe held-send cleanup and raw firmware propagation probes.",
    "RawSessionYubiKeyExtensionsTests.cs": "Retain hidden-connection ownership assertions; hold task observation/drain on assertion failure even where release is in finally.",
    "RawSmartCardSessionTests.cs": "Retain current APDU/version/borrow assertions; hold finally-owned barrier release/drain and authenticated success vectors.",
    "UserPresenceNotificationTests.cs": "Retain notification assertions; hold failure-path pending callback release/drain. C owns implementation semantics.",
    "ExchangeGuardTests.cs": "Retain per-exchange assertions; hold finally-owned gate release and actual task drain. These are not whole-workflow or physical-release oracles.",
}
retain = {
    "ApduTransmitter.cs": "Retain current formatted-buffer finally cleanup. Transport-result clone cleanup is a separate implementation hold, documented on ApduResponse and receiver/processor owners.",
    "PcscProtocol.cs": "Retain actual configuration and logger implementation/comments. Below-4 reconfiguration retention, development ForceShort behavior, SELECT input classification and provider exception provenance remain qualified in the handoff.",
    "ScpExtensionsTests.cs": "Retain firmware-routing assertions; they admit non-NotSupported failures and do not establish setup success. Parameter disposal/harness improvements remain separate work.",
    "PcscProtocolConcurrencyTests.cs": "Retain per-exchange and no-replay assertions; stronger first-fragment/whole-workflow/native claims are held.",
    "X963KdfTests.cs": "Retain exact twenty SHA-256 vector equality cases and bounded length/consistency cases. Completed vector addendum supersedes the older pending-reading note; no new execution or NIST validation is claimed.",
}

decisions = []
xml_errors = []
examples = []
preserved = []
for p in sorted(eligible):
    old = git("show", f"{BASE}:{p}")
    new = (ROOT / p).read_bytes()
    src = git("show", f"{PRODUCT}:{p}")
    a, b = old.decode("utf-8-sig"), new.decode("utf-8-sig")
    family = next(f for f, pk in packets.items() if p in pk["paths"])
    own_units = [u for u in units if u["path"] == p]
    ranges = [(i + 1, max(i + 1, j)) for tag, i, j, _, _ in
              difflib.SequenceMatcher(None, a.splitlines(), b.splitlines()).get_opcodes() if tag != "equal"]
    touched = [u["id"] for u in own_units if any(lo <= u["last_line"] and hi >= u["first_line"] for lo, hi in ranges)]
    candidates = sorted(assoc.get(p, []))
    proposals = [g["proposal_id"] for g in groups if set(candidates) & set(g["local_ids"])]
    decision = "edit" if p in changed else "hold" if Path(p).name in hold else "retain"
    reason = ("Apply source-supported D corrections only; exact changed units and full patch are recorded. "
              "All runtime/assertion/profile changes remain held." if decision == "edit" else
              hold.get(Path(p).name, retain.get(Path(p).name,
              "Retain the source-supported contract and existing assertions; no independent documentation correction was identified in this path.")))
    decisions.append({"path": p, "family": family, "candidate_associated": bool(candidates),
        "decision": decision, "rationale": reason,
        "investigator_context": notes[family].get(Path(p).name),
        "local_ids": candidates, "proposal_ids": proposals,
        "association_basis": "Original index/unit associations plus explicit candidate-prose related-context handoffs; no new candidate IDs",
        "original_unit_ids": [u["id"] for u in own_units], "edited_overlap_original_unit_ids": touched,
        "base_raw_sha256": sha(old), "current_raw_sha256": sha(new),
        "base_git_blob": blob(old), "current_git_blob": blob(new),
        "product_source_raw_sha256": sha(src),
        "product_source_equal_after_bom_newline_normalization": src.decode("utf-8-sig").replace("\r\n", "\n") == a.replace("\r\n", "\n")})
    if p in changed:
        # A deliberately limited line check, not a C# lexer/token/API/compilation claim.
        def noncomments(s):
            return [x.split(" //", 1)[0].rstrip() for x in s.splitlines()
                    if x.strip() and not x.lstrip().startswith("//")]
        assert noncomments(a) == noncomments(b), p
        preserved.append(p)
        for m in re.finditer(r"(?:^[ \t]*///[^\n]*\n?)+", b, re.M):
            text = "\n".join(re.sub(r"^\s*/// ?", "", line) for line in m.group().splitlines())
            try:
                ET.fromstring("<root>" + text + "</root>")
            except ET.ParseError as ex:
                xml_errors.append({"path": p, "error": str(ex)})
    for version, text in [("base", a), ("current", b)]:
        doc = "\n".join(re.sub(r"^\s*/// ?", "", line) if line.lstrip().startswith("///") else line for line in text.splitlines())
        for i, match in enumerate(re.finditer(r"<code(?:\s[^>]*)?>(.*?)</code>", doc, re.S), 1):
            body = html.unescape(match.group(1)).strip("\n")
            examples.append({"path": p, "version": version, "ordinal": i,
                "classification": "complete composable helper; borrowed open connection and applet-specific header/payload are explicit prerequisites" if p.endswith("/ApduCommand.cs") and version == "current" else "context-dependent documentation fragment",
                "compilation": "NOT-RUN; coordinator compilation/token/API checks pending",
                "runtime": "NOT-RUN; command validity/authorization is caller/profile-specific",
                "body_sha256": sha(body.encode()), "complete_body": body})

assert not xml_errors, xml_errors
emit("path-decisions.json", decisions)
emit("candidate-decisions.json", [{
    "proposal_id": g["proposal_id"], "local_ids": g["local_ids"], "facets_preserved": g["facets"],
    "documentation": "D correction applied where eligible; retained and held paths remain explicit" if "D" in g["facets"] else "HOLD; no released D facet",
    "proposal_and_rationale": g["proposal_and_rationale"],
    "precise_follow_up": g["implementation_reopen_trigger_or_needed_probe"],
    "edited_paths": [x["path"] for x in decisions if x["decision"] == "edit" and g["proposal_id"] in x["proposal_ids"]],
    "retained_or_held_paths": [{"path": x["path"], "decision": x["decision"], "reason": x["rationale"]} for x in decisions if x["decision"] != "edit" and g["proposal_id"] in x["proposal_ids"]],
    "original_indices": [c for c in indices if c["id"] in g["local_ids"]],
    "inherited_association_keys": g.get("inherited_association_keys", []),
} for g in groups])
emit("examples.json", {"xml_code_bodies": examples,
    "removed_internal_prototype": {"path": "src/Core/src/Protocols/SmartCard/Scp/Scp11X963Kdf.cs", "classification": "obsolete commented-out provider experiment fragment removed; complete original bytes retained in full-product.diff/base Git object", "compilation": "NOT-RUN; never executable in source"},
    "other_primary_paths": "No other XML <code> bodies in either complete primary C# population; unchanged executable test bodies and runtime fixture strings are not documentation examples.",
    "shared_and_prepared_examples": "B5/B6 prepared examples and shared-guide bodies remain unchanged, UNCOMPILED/context-dependent as recorded by their original owners; later guide owner must classify and compile complete bodies."})
outside = []
for p, ids in sorted(assoc.items()):
    if not ids or p in eligible:
        continue
    outside.append({"path": p, "decision": "hold-for-owner", "local_ids": sorted(ids),
        "proposal_ids": [g["proposal_id"] for g in groups if ids & set(g["local_ids"])],
        "follow_up": "Reconcile the mapped candidate's exact caller/guide claim, ownership, firmware/profile and evidence conditions; retain historical bytes. No edit authorized to this editor."})
emit("outside-owner-handoffs.json", {"direct_candidate_paths": outside,
    "inherited_boundary_records_preserved": {f: load(f"investigation/{f}/inherited-dispositions.json") for f in packets}})
diff = git("diff", "--binary", "--full-index", BASE, "--", *sorted(changed))
(OUT / "full-product.diff").write_bytes(diff)
receipt = {"worktree": str(ROOT), "branch": git("branch", "--show-current").decode().strip(),
    "base_commit": BASE, "current_commit": git("rev-parse", "HEAD").decode().strip(),
    "product_source_commit": PRODUCT, "eligible_primary_cs_count": len(eligible),
    "edited_count": len(changed), "decision_counts": {s: sum(x["decision"] == s for x in decisions) for s in ["edit", "retain", "hold"]},
    "candidate_associated_eligible_count": sum(x["candidate_associated"] for x in decisions),
    "full_product_diff_sha256": sha(diff), "limited_noncomment_line_comparison_matched": len(preserved),
    "xml_well_formedness_errors": xml_errors, "compiler_cref_token_api_status": "NOT-RUN; existing coordinator checker owns this gate",
    "build_test_formatter_crypto_native_device_status": "NOT-RUN as instructed",
    "git_index_ref_commit_push_subagents": "NONE",
    "independent_content_citation_review": "PENDING; this is the editor return, not independent review"}
git("diff", "--check", BASE)
receipt["git_diff_check"] = "PASS; exit 0, no whitespace diagnostics"
emit("receipt.json", receipt)
emit("artifact-manifest.json", [{"path": str(p.relative_to(ROOT)), "sha256": sha(p.read_bytes())}
    for p in sorted([*(p for p in OUT.iterdir() if p.is_file() and p.name != "artifact-manifest.json"), RUN / "edit-protocols.md"])])
print(json.dumps(receipt, indent=2))
