"""Capture focused original sources; no SDK/native execution or external scratch writes."""
import datetime
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parent
SOURCES = ROOT / "sources"


def capture(name, url):
    row = {"name": name, "url": url, "accessed_utc": datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        request = urllib.request.Request(url, headers={"User-Agent": "Core-B4-static-documentation-audit"})
        with urllib.request.urlopen(request, timeout=40) as response:
            data = response.read()
            row.update(status=response.status, final_url=response.url, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
        (SOURCES / name).write_bytes(data)
    except Exception as error:
        row["error"] = str(error)
    return row


if __name__ == "__main__":
    SOURCES.mkdir(exist_ok=True)
    urls = {
        "cfgmgr32.h": "https://raw.githubusercontent.com/tpn/winsdk-10/master/Include/10.0.16299.0/um/cfgmgr32.h",
        "hidsdi.h": "https://raw.githubusercontent.com/tpn/winsdk-10/master/Include/10.0.16299.0/shared/hidsdi.h",
        "winnt.h": "https://raw.githubusercontent.com/tpn/winsdk-10/master/Include/10.0.16299.0/um/winnt.h",
        "cm-unregister.html": "https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_unregister_notification",
        "cm-interface-list.html": "https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_device_interface_listw",
        "cm-interface-property.html": "https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_device_interface_propertyw",
        "hidd-getfeature.html": "https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/hidsdi/nf-hidsdi-hidd_getfeature",
        "createfile.html": "https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew",
        "readfile.html": "https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile",
        "cancelioex.html": "https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-cancelioex",
        "getoverlappedresult.html": "https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-getoverlappedresult",
        "safehandle-marshaller.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshalling/SafeHandleMarshaller.cs",
        "interop-best-practices.md": "https://raw.githubusercontent.com/dotnet/docs/main/docs/standard/native-interop/best-practices.md",
        "official-cfgmgr32.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/um/cfgmgr32.h",
        "official-hidsdi.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/shared/hidsdi.h",
        "loadlibraryex.html": "https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw",
        "x64-calling-convention.html": "https://learn.microsoft.com/en-us/cpp/build/x64-calling-convention?view=msvc-170",
        "libraryimport.html": "https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation",
        "shim-pcsc.c": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/pcsc.c",
        "shim-native_abi.h": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/native_abi.h",
        "shim-CMakeLists.txt": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/CMakeLists.txt",
        "shim-exports.msvc": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/exports.msvc",
        "shim-nuspec.xml": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/Yubico.NativeShims.nuspec",
        "shim-header.in": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/Yubico.NativeShims.h.in",
        "shim-build-windows.ps1": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/build-windows.ps1",
        "shim-pcscd.cmake": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/cmake/pcscd.cmake",
        "official-winerror.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/shared/winerror.h",
        "pcsclite-wintypes.html": "https://pcsclite.apdu.fr/api/wintypes_8h_source.html",
        "pcsclite-types.html": "https://pcsclite.apdu.fr/api/pcsclite_8h_source.html",
        "dotnet10-WindowsIdentity.cs": "https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Security.Principal.Windows/src/System/Security/Principal/WindowsIdentity.cs",
        "official-devguid.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/shared/devguid.h",
        "official-devpkey.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/shared/devpkey.h",
        "official-hidclass.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/shared/hidclass.h",
        "shim-build-linux-amd64.sh": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/build-linux-amd64.sh",
        "shim-build-linux-arm64.sh": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/build-linux-arm64.sh",
        "shim-Aot.targets": "https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/msbuild/Yubico.NativeShims.Aot.targets",
        "hid-1.11.pdf": "https://www.usb.org/sites/default/files/hid1_11.pdf",
        "hut-1.5.pdf": "https://usb.org/sites/default/files/hut1_5.pdf",
        "official-ntddkbd.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/shared/ntddkbd.h",
        "official-usbiodef.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/shared/usbiodef.h",
        "official-winsmcrd.h": "https://raw.githubusercontent.com/microsoft/win32metadata/15fc870a5e96df7c560877ff30136c0fb5a21e9b/generation/WinSDK/RecompiledIdlHeaders/shared/winsmcrd.h",
        "sourcelink-CVE-2026-62900.html": "https://github.com/dotnet/sourcelink/security/advisories/GHSA-23fw-v26w-5fgq",
    }
    manifest_path = SOURCES / "captures.json"
    prior = json.loads(manifest_path.read_text()) if manifest_path.exists() else []
    prior_names = {row["name"] for row in prior}
    manifest = prior + [capture(name, url) for name, url in urls.items() if name not in prior_names]
    (SOURCES / "captures.json").write_text(json.dumps(manifest, indent=2) + "\n")
    for row in manifest[len(prior):]:
        print(row["name"], row.get("status", row.get("error")), row.get("sha256", ""))
