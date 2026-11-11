"""Fetch bounded original-source evidence into this packet's owned cache; no SDK execution."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import urlopen

ROOT = Path(__file__).resolve().parent
SOURCES = {
    "pcsc5-v2.01.01.pdf": "https://pcscworkgroup.com/Download/Specifications/pcsc5_v2.01.01.pdf",
    "SafeHandle-v10.0.0.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/SafeHandle.cs",
    "Task-v10.0.0.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/Task.cs",
    "Thread-v10.0.0.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/Thread.cs",
    "WaitHandle-v10.0.0.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/WaitHandle.cs",
    "SafeWaitHandle-v10.0.0.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/Microsoft/Win32/SafeHandles/SafeWaitHandle.cs",
    "pcsc-lite-api.html": "https://pcsclite.apdu.fr/api/group__API.html",
    "sp800-73-5-part2.pdf": "https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73pt2-5.pdf",
}
for api in ["scardconnecta", "scarddisconnect", "scardreleasecontext", "scardgetstatuschangea", "scardcancel", "scardbegintransaction", "scardendtransaction"]:
    SOURCES[f"win-{api}.md"] = f"https://raw.githubusercontent.com/MicrosoftDocs/sdk-api/docs/sdk-api-src/content/winscard/nf-winscard-{api}.md"


def main():
    records = []
    manifest = ROOT / "manifest.json"
    prior = {item["path"]: item for item in json.loads(manifest.read_text())} if manifest.exists() else {}
    for name, url in SOURCES.items():
        path = ROOT / name
        if name in prior:
            records.append(prior[name])
            continue
        try:
            data = path.read_bytes() if path.exists() else urlopen(url, timeout=40).read()
            if not path.exists():
                path.write_bytes(data)
            records.append({"path": name, "url": url, "sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data), "access_utc": datetime.now(timezone.utc).isoformat(), "role": "original-source; version/status and actor applicability in register.md"})
        except Exception as error:
            records.append({"path": name, "url": url, "fetch_error": str(error)})
    (ROOT / "manifest.json").write_text(json.dumps(records, indent=2) + "\n")
    print(json.dumps(records, indent=2))


if __name__ == "__main__":
    main()
