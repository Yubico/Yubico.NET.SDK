"""One focused source follow-up; retain initial failures unchanged."""
import datetime
import hashlib
import json
import pathlib
import urllib.request

here = pathlib.Path(__file__).resolve().parent / "sources"
urls = {
    "scp03-public-review-1.1.2.6.pdf": "https://globalplatform.org/wp-content/uploads/2019/12/GPC_2.3_D_SCP03_v1.1.2.6.pdf",
    "scp11-public-release-1.2.pdf": "https://globalplatform.org/wp-content/uploads/2017/09/GPC_2_3_F_SCP11_v1.2_PublicRelease.pdf",
    "arraybufferwriter-v10.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Buffers/ArrayBufferWriter.cs",
}
records = []
for name, url in urls.items():
    record = {"name": name, "url": url, "requested_utc": datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        with urllib.request.urlopen(url, timeout=40) as response:
            data = response.read()
            record.update(final_url=response.url, status=response.status, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
        (here / name).write_bytes(data)
    except Exception as error:
        record["error"] = str(error)
    records.append(record)
(here / "focused-followup-manifest.json").write_text(json.dumps(records, indent=2) + "\n")
print(json.dumps(records, indent=2))
