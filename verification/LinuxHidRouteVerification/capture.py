#!/usr/bin/env python3
"""Capture L6b evidence, without configuring the host or mutating a key.

Run the toolchain publish target first. Exit 2 means native evidence passed but
physical verification was blocked; it is deliberately not global acceptance.
Only positive uniquely enumerated serials are used for read-only hardware modes.
Operator modes are never run by this script.
"""

import datetime
import hashlib
import json
import os
from pathlib import Path
import stat
import subprocess
import sys
import xml.etree.ElementTree as ET
from evidence_gate import validate_result
from publish_contract import verify_publish, STAMP

ROOT = Path(__file__).resolve().parents[2]
BASELINE = "3de42bb54c5e8c986364f0f04398d4e85a66a596"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n")


def command(args):
    try:
        completed = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, check=False)
    except FileNotFoundError:
        return {"command": args, "exit": 127, "stdout": "", "stderr": "optional diagnostic command unavailable"}
    return {"command": args, "exit": completed.returncode, "stdout": completed.stdout, "stderr": completed.stderr}


def hardware_snapshot():
    usb = []
    for device in sorted(Path("/sys/bus/usb/devices").glob("*")):
        vendor = device / "idVendor"
        if vendor.exists() and vendor.read_text().strip() == "1050":
            item = {"sysfs": device.name}
            for name in ("idVendor", "idProduct", "bcdDevice", "serial", "product"):
                path = device / name
                if path.exists():
                    item[name] = path.read_text().strip()
            item["interfaces"] = {interface.name: os.path.realpath(interface / "driver")
                                  if (interface / "driver").exists() else None
                                  for interface in sorted(device.parent.glob(device.name + ":*"))}
            usb.append(item)
    return {"yubicoUsb": usb, "hidraw": [{"path": str(path), "mode": stat.filemode(path.stat().st_mode),
                                          "readable": os.access(path, os.R_OK), "writable": os.access(path, os.W_OK)}
                                         for path in sorted(Path("/dev").glob("hidraw*"))]}


def native_shims_provenance(dependencies):
    # The verified publish transaction owns the restore/package truth. Never consult
    # historical default obj files or infer a global-cache location from a version pin.
    identities = set()
    for kind in ("jit", "aot"):
        closure = dependencies["resolvedClosure"][kind]["src/Core/src/Yubico.YubiKit.Core.csproj"]
        resolved = {name for name in closure if name.startswith("Yubico.NativeShims/")}
        if not resolved:
            raise ValueError(f"missing bound NativeShims identity for {kind}")
        identities.update(resolved)
    artifacts = {}
    for identity in sorted(identities):
        files = dependencies["packageFiles"].get(identity)
        if not files:
            raise ValueError(f"missing bound NativeShims package content: {identity}")
        for name, value in sorted(files.items()):
            suffix = Path(name).suffix
            if (suffix == ".nupkg" or ("linux-x64/native/" in name and suffix == ".so") or
                    ("static/linux-x64/" in name and suffix == ".a")):
                artifacts[f"packages/{identity}/{name}"] = value
    return {"resolvedNativeShims": sorted(identities), "artifacts": artifacts}


def main():
    if sys.argv[1:] not in ([], ["--native-only"]):
        raise ValueError("Usage: capture.py [--native-only]")
    native_only = sys.argv[1:] == ["--native-only"]
    # Fail before any binary, SDK enumeration or device operation is started.
    stamp = verify_publish()
    package = native_shims_provenance(stamp["inputs"]["dependencies"])
    now = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    destination = ROOT / "artifacts/linux-hid-verification/evidence" / now
    destination.mkdir(parents=True, exist_ok=False)
    manifest = stamp["inputs"]["source"]
    save(destination / "source-manifest.json", manifest)
    save(destination / STAMP, stamp)
    provenance = {"utc": now, "sourceHash": manifest["sourceHash"], "baseline": BASELINE,
                  "environment": [command(["dotnet", "--info"]), command(["uname", "-a"]),
                                  command(["pacman", "-Q", "linux-omarchy", "glibc", "systemd", "pcsclite", "openssl"])],
                   "hardware": hardware_snapshot(), **package,
                  "publishManifestSha256": sha(destination / STAMP), "publishInputHash": stamp["inputHash"],
                  "sourceLabel": "Environment SourceHash is a label only; publish binding was independently verified before execution"}
    pins = ET.parse(ROOT / "Directory.Packages.props").getroot()
    provenance["sdkPackageVersion"] = stamp["inputs"]["dependencies"]["sdkPackageVersion"]
    provenance["nativeShimsPin"] = next(item.attrib["Version"] for item in pins.iter("PackageVersion")
                                       if item.attrib.get("Include") == "Yubico.NativeShims")
    for kind in ("jit", "aot"):
        binary = ROOT / "artifacts/linux-hid-verification" / kind / "LinuxHidRouteVerification"
        provenance["artifacts"][f"{kind}/LinuxHidRouteVerification"] = sha(binary)
        host_assembly = binary.with_suffix(".dll")
        if host_assembly.exists():
            provenance["artifacts"][f"{kind}/{host_assembly.name}"] = sha(host_assembly)
        for path in sorted(binary.parent.glob("Yubico.YubiKit.*.dll")):
            provenance["artifacts"][f"{kind}/{path.name}"] = sha(path)
        provenance["environment"].append(command(["ldd", str(binary)]))
    save(destination / "provenance.json", provenance)
    environment = os.environ.copy()
    environment["LINUX_VERIFY_SOURCEHASH"] = manifest["sourceHash"]
    runs = []

    def run(kind, mode, expected, serial=None):
        verify_publish()
        binary = ROOT / "artifacts/linux-hid-verification" / kind / "LinuxHidRouteVerification"
        label = kind + "-" + mode.removeprefix("--")
        output = destination / (label + ".json")
        args = [str(binary), mode]
        if serial is not None:
            args.extend(["--serial", str(serial)])
        args.extend(["--output", str(output)])
        # The host bounds its own verification child. This outer deadline is failure only.
        completed = subprocess.run(args, cwd=ROOT, env=environment, text=True, capture_output=True, timeout=90, check=False)
        (destination / (label + ".log")).write_text(completed.stdout + completed.stderr)
        result = json.loads(output.read_text()) if output.exists() else {}
        verify_publish()
        try:
            expected = validate_result(result, kind, mode, completed.returncode, stamp["sourceHash"])
            matched = True
            validation_error = None
        except ValueError as error:
            matched = False
            validation_error = str(error)
        item = {"kind": kind, "mode": mode, "expectedExit": expected, "exit": completed.returncode,
                "matched": matched, "validationError": validation_error, "checks": len(result.get("Checks", [])), "raw": output.name,
                "rawSha256": sha(output) if output.exists() else None}
        runs.append(item)
        print(json.dumps(item), flush=True)
        if not matched:
            save(destination / "gate-failure.json", item)
            raise ValueError(f"{label}: {validation_error}; stopping before any further operation")
        return completed, result

    lists = {}
    for kind in ("jit", "aot"):
        run(kind, "--native", 0)
        run(kind, "--sensitivity-blocking", 1)
        run(kind, "--sensitivity-quiescence", 1)
        if native_only:
            lists[kind] = None
            continue
        completed, result = run(kind, "--list", 0)
        serials = [line.partition("=")[2] for line in completed.stdout.splitlines()
                   if line.startswith("UNIQUE_SELECTED_SERIAL=")]
        lists[kind] = int(serials[0]) if len(serials) == 1 and serials[0].isdigit() and int(serials[0]) > 0 else None
    selected = lists["jit"] if lists["jit"] == lists["aot"] else None
    hardware_modes = ("--fido", "--otp", "--raw", "--expert-io", "--expert-feature", "--smartcard")
    native_passed = all(item["matched"] for item in runs if item["mode"] in ("--native", "--sensitivity-blocking", "--sensitivity-quiescence"))
    if selected is not None and native_passed:
        for kind in ("jit", "aot"):
            for mode in hardware_modes:
                run(kind, mode, 0, selected)
    summary = {"utc": now, "baseline": BASELINE, "sourceHash": manifest["sourceHash"], "runs": runs,
               "selectedSerial": selected, "physicalModesExecuted": 0 if selected is None else 12,
               "hardware": "BLOCKED: no single positive serial agreed by JIT/AOT enumeration" if selected is None else "see individual results",
               "operatorModes": "NOT EXECUTED: touch/removal/active-cancel require separate coordination",
               "budgetAcceptance": "NOT ESTABLISHED",
               "publishManifestSha256": sha(destination / STAMP), "publishInputHash": stamp["inputHash"],
               "nativePassed": native_passed,
               "allExpectedOutcomes": all(item["matched"] for item in runs)}
    save(destination / "summary.json", summary)
    save(destination / "sha256.json", {path.name: sha(path) for path in sorted(destination.iterdir()) if path.is_file()})
    print(f"Evidence directory: {destination.relative_to(ROOT)}")
    print(f"SOURCEHASH={manifest['sourceHash']}")
    if not summary["allExpectedOutcomes"]:
        return 1
    return 0 if native_only or selected is not None else 2


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, OSError, KeyError) as error:
        print(f"FAIL capture provenance/result gate: {error}", file=sys.stderr)
        sys.exit(1)
