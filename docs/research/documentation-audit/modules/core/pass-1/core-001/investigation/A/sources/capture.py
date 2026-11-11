"""Capture the bounded packet-A originals; no SDK or device execution."""
import datetime
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parent
SOURCES = [
    ("vendor-config", "https://developers.yubico.com/yubikey-manager/Config_Reference.html"),
    ("vendor-fips-form-factor", "https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-fips-specifics.html"),
    ("windows-container", "https://learn.microsoft.com/en-us/windows-hardware/drivers/install/devpkey-device-containerid"),
    ("windows-reader-instance", "https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scardgetreaderdeviceinstanceidw"),
    ("dotnet10-bounded-channel", "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Threading.Channels/src/System/Threading/Channels/BoundedChannel.cs"),
]

if __name__ == "__main__":
    records = []
    for key, url in SOURCES:
        target = ROOT / (key + (".cs.txt" if key.startswith("dotnet") else ".html"))
        if target.exists():
            raise SystemExit(f"Preserve existing capture: {target}")
        with urllib.request.urlopen(url, timeout=45) as response:
            body = response.read()
            record = {"id": key, "requested_url": url, "final_url": response.url,
                      "access_utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                      "http_status": response.status, "last_modified": response.headers.get("Last-Modified"),
                      "sha256": hashlib.sha256(body).hexdigest(), "bytes": len(body),
                      "retained_path": target.name}
        target.write_bytes(body)
        records.append(record)
    (ROOT / "captures.json").write_text(json.dumps(records, indent=2) + "\n")
    print(json.dumps(records, indent=2))
