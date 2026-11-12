"""Read both complete fixture grammars and compare their actual fields. No KDF/hash operation.

SHA-256 below identifies source text only; published K values are never recomputed.
"""
import collections
import hashlib
import json
import pathlib
import re

HERE = pathlib.Path(__file__).resolve().parent
ROOT = next(parent for parent in HERE.parents if (parent / ".git").exists())
DIRECTORY = ROOT / "src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/Scp"

def parse(name):
    raw = (DIRECTORY / name).read_bytes()
    lines = raw.decode("utf-8-sig").splitlines()
    headers, records, current, options = [], [], None, {}
    for number, line in enumerate(lines, 1):
        value = line.strip()
        if not value:
            continue
        if value.startswith("#"):
            assert current is None
            headers.append(value)
            continue
        if value.startswith("["):
            assert value.endswith("]")
            field = value[1:-1]
            if field.startswith("SHA-"):
                options = {"sha": field, "header_line": number}
            else:
                key, text = field.split(" = ")
                options[key] = int(text)
            current = None
            continue
        key, text = value.split(" =", 1)
        text = text.strip()
        if key == "COUNT":
            current = {"options": dict(options), "count": int(text), "first_line": number, "fields": []}
            records.append(current)
        else:
            assert current is not None
            assert key in ["Z", "SharedInfo", "key_data", "Counter"] or re.fullmatch(r"(?:Hash input |K)\d+", key), (number, key)
            data = bytes.fromhex(text)
            current["fields"].append({"key": key, "hex": text, "bytes": len(data), "line": number})
            current["last_line"] = number
    assert headers == ["# CAVS 12.0", "# 'ANS X9.63-2001' information for sample", "# SHA sizes tested: SHA-1, SHA-224, SHA-256, SHA-384, SHA-512", "# Generated on Mon Mar 19 14:16:13 2012"]
    return {"path": str((DIRECTORY / name).relative_to(ROOT)), "sha256": hashlib.sha256(raw).hexdigest(), "lines": len(lines), "headers": headers, "records": records}

rsp = parse("ansx963_2001.rsp")
detail = parse("ansx963_2001.txt")
assert len(rsp["records"]) == len(detail["records"]) == 100
checks = []
hash_widths = {"SHA-1": 20, "SHA-224": 28, "SHA-256": 32, "SHA-384": 48, "SHA-512": 64}
for index, (r, d) in enumerate(zip(rsp["records"], detail["records"])):
    assert r["count"] == d["count"] == index % 10
    assert {k: v for k, v in r["options"].items() if k != "header_line"} == {k: v for k, v in d["options"].items() if k != "header_line"}
    rf = {f["key"]: f["hex"] for f in r["fields"]}
    df = {f["key"]: f["hex"] for f in d["fields"] if f["key"] in rf}
    assert list(rf) == ["Z", "SharedInfo", "key_data"]
    assert rf == df
    options = r["options"]
    z, info, output = (bytes.fromhex(rf[k]) for k in ["Z", "SharedInfo", "key_data"])
    assert len(z) == (options["shared secret length"] + 7) // 8
    assert int.from_bytes(z, "big").bit_length() <= options["shared secret length"]
    assert len(info) * 8 == options["SharedInfo length"]
    assert len(output) * 8 == options["key data length"]
    hash_width = hash_widths[options["sha"]]
    rounds = (len(output) + hash_width - 1) // hash_width
    intermediate = d["fields"][2:-1]
    assert len(intermediate) == rounds * 3
    blocks = []
    for round_index in range(1, rounds + 1):
        counter, hash_input, block = intermediate[(round_index - 1) * 3:round_index * 3]
        counter_bytes = round_index.to_bytes(4, "big")
        assert counter["key"] == "Counter" and bytes.fromhex(counter["hex"]) == counter_bytes
        assert hash_input["key"] == f"Hash input {round_index}"
        assert bytes.fromhex(hash_input["hex"]) == z + counter_bytes + info
        assert block["key"] == f"K{round_index}" and block["bytes"] == hash_width
        blocks.append(bytes.fromhex(block["hex"]))
    assert b"".join(blocks)[:len(output)] == output
    checks.append({"sha": options["sha"], "Z_bits": options["shared secret length"], "SharedInfo_bits": options["SharedInfo length"], "output_bits": options["key data length"], "count": r["count"], "rsp_lines": [r["first_line"], r["last_line"]], "detail_lines": [d["first_line"], d["last_line"]], "Z": rf["Z"], "SharedInfo": rf["SharedInfo"], "key_data": rf["key_data"], "intermediate_rounds": rounds, "structural_checks": "All field bytes compared; every Counter/Hash input/K length and concatenation/truncation checked. Published hash outputs not recomputed."})

test = ROOT / "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Scp/X963KdfTests.cs"
text = test.read_text()
vectors = re.findall(r'\{\s*"([0-9a-f]+)",\s*"([0-9a-f]*)",\s*"([0-9a-f]+)"\s*\}', text)
selected = [(c["Z"], c["SharedInfo"], c["key_data"]) for c in checks if c["sha"] == "SHA-256"]
assert len(vectors) == 20 and vectors == selected
single = ROOT / "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Scp/Scp11X963KdfTests.cs"
assert selected[0][0] in single.read_text() and selected[0][2] in single.read_text()

result = {"method": "Complete line grammar plus byte-field/counter/transcript/concatenation/truncation comparison and actual embedded test-row identity; no SDK run, hash/KDF computation or original-archive identity claim.", "source_files": [{k: v for k, v in source.items() if k != "records"} for source in [rsp, detail]], "all_records": checks, "embedded_assertion_rows": {"X963KdfTests": len(vectors), "Scp11X963KdfTests": "first SHA256 empty-SharedInfo vector"}, "scope": "Only SHA256 rows are consumed by the C# KDF tests; other SHA groups are retained fixture context, not modeled algorithm support or formal CAVP validation. Original NIST zip unavailable after bounded follow-up."}
(HERE / "vector-review.json").write_text(json.dumps(result, indent=2) + "\n")
for group in range(10):
    rows = checks[group*10:group*10+10]
    first = rows[0]
    print(first["sha"], "Z", first["Z_bits"], "info", first["SharedInfo_bits"], "out", first["output_bits"], "rounds", first["intermediate_rounds"], "counts", [r["count"] for r in rows], "rsp", [rows[0]["rsp_lines"][0], rows[-1]["rsp_lines"][1]], "txt", [rows[0]["detail_lines"][0], rows[-1]["detail_lines"][1]])
print("Every record field/transcript checked; 20 actual C# embedded vector tuples exactly match. No crypto operation executed.")
