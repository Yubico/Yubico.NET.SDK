"""One focused follow-up using exact original links; keep initial failures."""
import datetime
import hashlib
import json
import pathlib
import urllib.request

here = pathlib.Path(__file__).resolve().parent / "sources"
urls = {
    "nist-ansx963_2001.zip": "https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Algorithm-Validation-Program/documents/components/800-135testvectors/ansx963_2001.zip",
    "nist-askdfvs.pdf": "https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Algorithm-Validation-Program/documents/components/askdfvs.pdf",
    "iso-obp.html": "https://www.iso.org/obp/ui/en/#iso:std:iso-iec:7816:-4:ed-4:v1:en",
}
records = []
for name, url in urls.items():
    r = {"name": name, "url": url, "requested_utc": datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        with urllib.request.urlopen(url, timeout=40) as response:
            data = response.read()
            r.update(final_url=response.url, status=response.status, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
        (here / name).write_bytes(data)
    except Exception as error:
        r["error"] = str(error)
    records.append(r)
(here / "completion-followup-manifest.json").write_text(json.dumps(records, indent=2) + "\n")
print(json.dumps(records, indent=2))
