"""Read-only reviewer checks; prints JSON and never changes inputs or invokes .NET/device tools."""
import hashlib
import html
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

BASE = "727fe43afc430c5d8364d868a6436de50626e476"
RUN = Path("docs/research/documentation-audit/modules/piv/pass-1/piv-001")


def sha(data):
    return hashlib.sha256(data).hexdigest()


def original(path):
    return subprocess.check_output(["git", "show", f"{BASE}:{path}"])


def bodies(path, text):
    if path.endswith(".md"):
        return [(m.group(1).strip(), m.group(2)) for m in re.finditer(
            r"^```([^\n]*)\n(.*?)^```", text, re.M | re.S)]
    result = []
    for m in re.finditer(r"^[ \t]*///.*(?:\n[ \t]*///.*)*", text, re.M):
        xml = "\n".join(re.sub(r"^[ \t]*/// ?", "", line) for line in m.group().split("\n"))
        for code in re.finditer(r"<code>\n(.*?)^[ \t]*</code>", xml, re.M | re.S):
            result.append(("xml-csharp", html.unescape(code.group(1))))
    return result


report_bytes = (RUN / "edit-report.md").read_bytes()
report = report_bytes.decode()
rows = re.findall(r"^\| `([^`]+)` \| `([0-9a-f]{64})` \| `([0-9a-f]{64})` \|$", report, re.M)
files = []
xml_blocks = 0
errors = []
blocked = {
    "src/Piv/examples/PivTool/PivExamples/Attestation.cs": "PIV-REV-001",
    "src/Piv/examples/PivTool/PivExamples/Results/AttestationResult.cs": "PIV-REV-001",
    "src/Piv/examples/PivTool/README.md": "PIV-REV-001",
    "src/Piv/src/IPivSession.cs": "PIV-REV-002",
    "src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSigningAlgorithmTests.cs": "PIV-REV-003",
    "src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs": "PIV-REV-003",
}
for path, before_hash, after_hash in rows:
    before, after = original(path), Path(path).read_bytes()
    entry = {"path": path, "baseline_sha256": sha(before), "review_sha256": sha(after),
             "editor_hashes_match": sha(before) == before_hash and sha(after) == after_hash,
             "verdict": blocked.get(path, "accepted-content")}
    if path.endswith(".cs"):
        def noncomments(data):
            return [line for line in data.decode().splitlines() if not re.match(r"^\s*//", line)]
        entry["noncomment_lines_identical"] = noncomments(before) == noncomments(after)
        for m in re.finditer(r"^[ \t]*///.*(?:\n[ \t]*///.*)*", after.decode(), re.M):
            xml = "\n".join(re.sub(r"^[ \t]*/// ?", "", line) for line in m.group().split("\n"))
            xml_blocks += 1
            try:
                ET.fromstring("<root>" + xml + "</root>")
            except ET.ParseError as ex:
                errors.append({"path": path, "error": str(ex)})
    files.append(entry)

original_section = report.split("## Original complete-body hashes")[1].split("## Changed-path")[0]
snippet_paths = []
expected_originals = {}
path = None
for line in original_section.splitlines():
    if line.startswith(("src/", "docs/")):
        path = line
        snippet_paths.append(path)
    elif re.match(r"^\d+@", line):
        expected_originals[(path, int(line.split("@")[0]))] = line.split()[1]

snippets = []
for path in snippet_paths:
    old = bodies(path, original(path).decode())
    current = bodies(path, Path(path).read_text())
    for revision, collection in [("baseline", old), ("review", current)]:
        for ordinal, (language, body) in enumerate(collection, 1):
            if path == "docs/migration/v1-to-v2.md" and ordinal not in (9, 10, 11, 12, 21, 22):
                continue
            status = "composable-fragment"
            if path == "docs/migration/v1-to-v2.md" and ordinal in (9, 11, 21):
                status = "historical-v1"
            elif path == "docs/architecture/raw-access-tiers.md" and ordinal in (4, 5, 6):
                status = "other-applet-excluded"
            elif path == "docs/architecture/raw-access-tiers.md" and ordinal == 8:
                status = "historical-before-current-after"
            elif language not in ("csharp", "xml-csharp"):
                status = "non-csharp"
            elif path == "src/Piv/CLAUDE.md" and ordinal == 4:
                status = "application-dependent-store-contract"
            elif path == "src/Piv/README.md" and ordinal == 2:
                status = "complete-discovery-read"
            if path == "src/Piv/examples/PivTool/PivExamples/Attestation.cs" and ordinal == 2 and revision == "review":
                status = "required-correction-PIV-REV-001"
            digest = sha(body.encode())
            entry = {"path": path, "revision": revision, "ordinal": ordinal,
                     "body_sha256": digest, "classification": status}
            if revision == "baseline":
                entry["matches_editor_original"] = expected_originals.get((path, ordinal)) == digest
            snippets.append(entry)

sources = []
for manifest in ("source-manifest.json", "followup-manifest.json"):
    for entry in json.loads((RUN / "evidence" / manifest).read_text()):
        if entry.get("status") == "captured":
            actual = sha((RUN / "evidence" / entry["file"]).read_bytes())
            sources.append({"file": entry["file"], "sha256": actual, "matches_capture": actual == entry["sha256"]})

print(json.dumps({"baseline": BASE, "edit_report_sha256": sha(report_bytes),
                  "counts": {"files": len(files), "csharp_files": sum(p.endswith('.cs') for p, _, _ in rows),
                             "xml_blocks": xml_blocks,
                             "baseline_bodies": sum(s['revision'] == 'baseline' for s in snippets),
                             "review_bodies": sum(s['revision'] == 'review' for s in snippets)},
                  "xml_parse_errors": errors, "files": files, "snippets": snippets,
                  "captured_original_sources": sources}, indent=2))
