"""Retain original text only; no SDK imports, builds, tests or native operations."""
import concurrent.futures
import datetime
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).parent / "sources"
URLS = {
    "ubuntu-noble-libudev1.html": "https://packages.ubuntu.com/noble/amd64/libudev1/filelist",
    "ubuntu-noble-libudev-dev.html": "https://packages.ubuntu.com/noble/amd64/libudev-dev/filelist",
    "native-library-loading.md": "https://raw.githubusercontent.com/dotnet/docs/main/docs/standard/native-interop/native-library-loading.md",
    "glibc-2.40-close.c": "https://raw.githubusercontent.com/bminor/glibc/glibc-2.40/sysdeps/unix/sysv/linux/close.c",
    "69-yubikey.rules": "https://raw.githubusercontent.com/Yubico/yubikey-personalization/master/69-yubikey.rules",
    "linux-v6.12-open.c": "https://raw.githubusercontent.com/torvalds/linux/v6.12/fs/open.c",
    "linux-v6.12-file.c": "https://raw.githubusercontent.com/torvalds/linux/v6.12/fs/file.c",
    "linux-v6.12-eventfd.c": "https://raw.githubusercontent.com/torvalds/linux/v6.12/fs/eventfd.c",
    "linux-v6.12-hidraw.c": "https://raw.githubusercontent.com/torvalds/linux/v6.12/drivers/hid/hidraw.c",
    "linux-v6.12-usbhid.c": "https://raw.githubusercontent.com/torvalds/linux/v6.12/drivers/hid/usbhid/hid-core.c",
    "linux-v6.12-hid.h": "https://raw.githubusercontent.com/torvalds/linux/v6.12/include/linux/hid.h",
    "close.2.html": "https://man7.org/linux/man-pages/man2/close.2.html",
    "poll.2.html": "https://man7.org/linux/man-pages/man2/poll.2.html",
    "eventfd.2.html": "https://man7.org/linux/man-pages/man2/eventfd.2.html",
    "open.2.html": "https://man7.org/linux/man-pages/man2/open.2.html",
    "udev.html": "https://www.freedesktop.org/software/systemd/man/latest/udev.html",
    "libudev-monitor.c": "https://raw.githubusercontent.com/systemd/systemd/v257/src/libudev/libudev-monitor.c",
    "libudev-device.c": "https://raw.githubusercontent.com/systemd/systemd/v257/src/libudev/libudev-device.c",
    "SafeHandle.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/SafeHandle.cs",
    "SafeHandleMarshaller.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshalling/SafeHandleMarshaller.cs",
    "Thread.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/Thread.cs",
    "Task.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/Task.cs",
    "Device_Permissions.adoc": "https://raw.githubusercontent.com/Yubico/yubikey-manager/4ca60f706af930459138d8dc0f0f953480e1c7a4/doc/Device_Permissions.adoc",
    "python-linux.py": "https://raw.githubusercontent.com/Yubico/yubikey-manager/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/hid/linux.py",
}

def fetch(item):
    name, url = item
    result = {"name": name, "url": url, "accessed_utc": datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        with urllib.request.urlopen(url, timeout=40) as response:
            data = response.read()
            result["http_status"] = response.status
        (ROOT / name).write_bytes(data)
        result.update(sha256=hashlib.sha256(data).hexdigest(), bytes=len(data))
    except Exception as error:
        result["fetch_error"] = str(error)
    return result

if __name__ == "__main__":
    ROOT.mkdir(parents=True, exist_ok=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:
        records = list(pool.map(fetch, URLS.items()))
    (ROOT / "manifest.json").write_text(json.dumps(records, indent=2) + "\n")
    print(json.dumps(records, indent=2))
