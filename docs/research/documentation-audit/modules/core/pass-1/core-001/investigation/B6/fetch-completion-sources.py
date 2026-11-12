"""One initial source pass for remaining vector/ISO provenance; HTTP only."""
import datetime
import hashlib
import json
import pathlib
import re
import urllib.request

here = pathlib.Path(__file__).resolve().parent / "sources"
urls = {
    "nist-component-testing.html": "https://csrc.nist.gov/projects/cryptographic-algorithm-validation-program/component-testing",
    "nist-askdfvs.pdf": "https://csrc.nist.gov/csrc/media/projects/cryptographic-algorithm-validation-program/documents/components/askdfvs.pdf",
    "iso7816-4-2020.html": "https://www.iso.org/standard/77180.html",
}
records = []
for name, url in urls.items():
    record = {"name": name, "url": url, "requested_utc": datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        with urllib.request.urlopen(url, timeout=40) as response:
            data = response.read()
            record.update(final_url=response.url, status=response.status, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
        (here / name).write_bytes(data)
        if name.endswith(".html"):
            record["relevant_links"] = [x for x in re.findall(r'href=[\"\']([^\"\']+)', data.decode(errors="replace")) if re.search(r'963|askdf|obp|preview|7816|77180|pdf', x, re.I)]
    except Exception as error:
        record["error"] = str(error)
    records.append(record)
(here / "completion-initial-manifest.json").write_text(json.dumps(records, indent=2) + "\n")
print(json.dumps(records, indent=2))
