"""Read-only original-source capture; writes only this investigation's sources."""
import datetime
import hashlib
import json
from pathlib import Path
import urllib.request
import urllib.error

ROOT = Path(__file__).parent

def capture(name, url):
    row = {"name": name, "url": url, "accessed_utc": datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        request = urllib.request.Request(url, headers={"User-Agent": "Core-documentation-audit", "Accept": "application/vnd.github+json"} if "api.github.com" in url else {"User-Agent": "Core-documentation-audit"})
        with urllib.request.urlopen(request, timeout=30) as response:
            data = response.read()
            row.update(status=response.status, sha256=hashlib.sha256(data).hexdigest(), bytes=len(data))
            (ROOT / name).write_bytes(data)
    except urllib.error.HTTPError as error:
        row.update(status=error.code, error=str(error))
    except Exception as error:
        row.update(error=str(error))
    manifest = ROOT / "captures.json"
    rows = json.loads(manifest.read_text()) if manifest.exists() else []
    rows.append(row)
    manifest.write_text(json.dumps(rows, indent=2) + "\n")
    print(row)
    return row

if __name__ == "__main__":
    capture("apple-device-class.m", "https://raw.githubusercontent.com/apple-oss-distributions/IOHIDFamily/777ccd9698845aadf711e32d843c8c9b777431d9/IOHIDLib/IOHIDDeviceClass.m")
    capture("iokit-head.json", "https://api.github.com/repos/apple-oss-distributions/IOKitUser/commits/main")
    capture("native-release-ref.json", "https://api.github.com/repos/Yubico/Yubico.NET.SDK/git/ref/tags/1.18.1")
    capture("native-historical-tree.json", "https://api.github.com/repos/Yubico/Yubico.NET.SDK/git/trees/71a23cd0269c968c1d9420eddb2e2fec71e0cc29?recursive=1")
