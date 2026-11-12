"""Bounded original-source retention only; no SDK imports or execution."""
import datetime
import hashlib
import json
import pathlib
import re
import urllib.request

HERE = pathlib.Path(__file__).resolve().parent / "sources"
HERE.mkdir(exist_ok=True)
URLS = {
    "scp03-1.1.2.pdf": "https://globalplatform.org/wp-content/uploads/2014/07/GPC_2.3_D_SCP03_v1.1.2_PublicRelease.pdf",
    "scp11-public-review-1.3.0.13.pdf": "https://globalplatform.org/wp-content/uploads/2023/08/GPC_2.3_F_SCP11_v1.3.0.13_PublicRvw.pdf",
    "scp11-catalogue.html": "https://globalplatform.org/specs-library/secure-channel-protocol-11-amendment-f/",
    "scp03-catalogue.html": "https://globalplatform.org/specs-library/secure-channel-protocol-03-amendment-d-v1-2/",
    "yubico-profile.html": "https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-scp.html",
    "arraybufferwriter-v10.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Memory/src/System/Buffers/ArrayBufferWriter.cs",
}
records = []
for name, url in URLS.items():
    record = {"name": name, "url": url, "requested_utc": datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        request = urllib.request.Request(url, headers={"User-Agent": "Documentation-audit-source-reader"})
        with urllib.request.urlopen(request, timeout=40) as response:
            data = response.read()
            record.update(final_url=response.url, status=response.status, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
        (HERE / name).write_bytes(data)
        if name.endswith(".html"):
            record["download_links"] = sorted(set(re.findall(r'(?:href|data-url)=[\"\']([^\"\']*(?:pdf|download)[^\"\']*)', data.decode(errors="replace"), re.I)))
    except Exception as error:
        record["error"] = str(error)
    records.append(record)
(HERE / "initial-download-manifest.json").write_text(json.dumps(records, indent=2) + "\n")
print(json.dumps(records, indent=2))
