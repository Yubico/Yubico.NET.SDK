"""Independent static record checker; not independent content review or SDK execution."""
import hashlib
import json
import re
import subprocess
from pathlib import Path

root = Path.cwd()
out = Path(__file__).resolve().parent
run = out.parents[1]
read = lambda p: json.loads(p.read_text())
sha = lambda b: hashlib.sha256(b).hexdigest()
packet = read(run / "inventory/packets/BOUNDARY-Management.json")
units = read(out / "unit-dispositions.json")
files = read(out / "file-dispositions.json")
assert len(units) == len({u["id"] for u in units}) == 331
assert {u["id"] for u in units} == set(packet["unit_ids"])
assert {f["path"] for f in files} == set(packet["disposition_paths"])
assert {u["path"] for u in units} == set(packet["paths"])
inventory = {u["id"]:u for u in read(run / "inventory/semantic-units.json")}
for u in units:
    raw = (root / u["path"]).read_bytes()
    text = raw.decode("utf-8-sig")
    assert sha(raw) == u["source_sha256"] == inventory[u["id"]]["source_sha256"]
    assert sha(text[u["start_character"]:u["end_character_exclusive"]].encode()) == u["range_sha256"]
    assert u["status"] in {"READ-CLEAN","READ-CANDIDATE-CONTEXT"}
for f in files:
    assert f["normalized_pin_equal"]
    current = (root / f["path"]).read_bytes().decode("utf-8-sig").replace("\r\n","\n")
    pin = subprocess.check_output(["git","show",f"fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:{f['path']}"],cwd=root).decode("utf-8-sig").replace("\r\n","\n")
    assert current == pin
candidates = read(out / "candidates.json")
for c in candidates:
    assert set(c["unit_ids"]) == {u["id"] for u in units if c["id"] in u["candidate_ids"]}
    assert all((root / p).is_file() for p in c["paths"])
inputs = read(run / "inventory/inherited-packet-inputs.json")["BOUNDARY-Management"]
assert {r["key"] for r in read(out / "inherited-dispositions.json")} == set(inputs["provenance_keys"])
assert sha((run / "inherited-findings/registry.json").read_bytes()) == "da49c371613b2ddc2c9ec59e1aa87b0e68b5b4611f2b795d935aaaf23922a481"
for c in read(out / "sources/captures.json"):
    assert sha((out / "sources" / c["retained_path"]).read_bytes()) == c["sha256"]
nominees = read(out / "clean-nominees.json")["units"]
assert len(nominees) >= 3
links = []
for p in [run / "boundary-Management.md", out / "sources.md"]:
    text = p.read_text()
    assert sum(line.startswith("```") for line in text.splitlines()) % 2 == 0
    for link in re.findall(r"\]\(([^)]+)\)",text):
        if "://" not in link and not link.startswith("#"):
            target = p.parent / link.split("#")[0]
            assert target.exists(), (p,link)
            links.append(link)
receipt = {"kind":"static record verification, not independent semantic review","report_sha256":sha((run / "boundary-Management.md").read_bytes()),"exact_primary_ids":331,"candidate_files":55,"inherited_keys":len(inputs["provenance_keys"]),"local_candidates":len(candidates),"clean_nominees":len(nominees),"local_links_checked":len(links),"source_range_pin_capture_checks":"PASS","registry_identity":"UNCHANGED","sdk_build_test_native_device_executions":0,"records":{str(p.relative_to(out)):sha(p.read_bytes()) for p in sorted(out.rglob("*")) if p.is_file() and p.name != "verification-receipt.json"}}
(out / "verification-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n")
print(json.dumps({k:v for k,v in receipt.items() if k != "records"},indent=2))
