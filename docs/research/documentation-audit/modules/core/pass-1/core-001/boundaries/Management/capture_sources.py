"""Capture two focused original manufacturer pages; performs no SDK/native/device work."""
import datetime
import hashlib
import json
import urllib.request
from pathlib import Path

out = Path(__file__).resolve().parent / "sources"
out.mkdir(exist_ok=True)
records = []
for name, url in [
    ("bio-mpe.html", "https://docs.yubico.com/yesdk/users-manual/sdk-programming-guide/bio-mpe.html"),
    ("device-reset-command.html", "https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Management.Commands.DeviceResetCommand.html"),
]:
    with urllib.request.urlopen(url, timeout=30) as response:
        data = response.read()
        (out / name).write_bytes(data)
        records.append({"url":url,"final_url":response.url,"status":response.status,"access_utc":datetime.datetime.now(datetime.timezone.utc).isoformat(),"last_modified":response.headers.get("Last-Modified"),"sha256":hashlib.sha256(data).hexdigest(),"bytes":len(data),"retained_path":name,"authority":"Unversioned manufacturer product/legacy-SDK description; no current v2 API or physical acceptance inferred."})
(out / "captures.json").write_text(json.dumps(records,indent=2)+"\n")
print(json.dumps(records,indent=2))
