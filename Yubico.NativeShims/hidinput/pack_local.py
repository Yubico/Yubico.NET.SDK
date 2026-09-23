#!/usr/bin/env python3
"""Build both pinned macOS architectures and assemble an unsigned local-only package.

Usage: python3 hidinput/pack_local.py SIGNED_1_18_0.nupkg PINNED_VCPKG_ROOT
The original signed package and all global NuGet caches remain untouched.
"""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile

NATIVE = Path(__file__).resolve().parents[1]
ROOT = NATIVE.parent
VERSION = "1.18.1-async.7"
BASELINE = "04a9d8e5212d01ee1dd9478eadd9caade4f8b0d4"
SOURCE_BASE = "f8c974f785d96dfc654606b573c6840968bb8220"
SOURCE_SHA256 = "ddcbda0bbdaba70d1b1dbd646f85575b017efbe0a4e05c20b71d76af0524ee4d"
OUTPUT = NATIVE / "hidinput/artifacts/native-feed" / f"Yubico.NativeShims.{VERSION}.nupkg"


def run(*command, cwd=ROOT):
    subprocess.run(command, cwd=cwd, check=True)


def output(*command, cwd=ROOT):
    return subprocess.check_output(command, cwd=cwd, text=True).strip()


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    if len(sys.argv) != 3:
        raise SystemExit("usage: pack_local.py SIGNED_1_18_0.nupkg PINNED_VCPKG_ROOT")
    if OUTPUT.exists():
        raise SystemExit(f"refusing to overwrite existing local package: {OUTPUT}")
    signed, vcpkg = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve()
    if digest(signed.read_bytes()) != SOURCE_SHA256:
        raise SystemExit("refusing unsigned/unrecognized 1.18.0 input package")
    if output("git", "rev-parse", "HEAD", cwd=vcpkg) != BASELINE:
        raise SystemExit("vcpkg checkout is not the pinned 04a9d8e5 baseline")
    if subprocess.run(("git", "merge-base", "--is-ancestor", SOURCE_BASE, "HEAD"),
                      cwd=ROOT, check=False).returncode != 0:
        raise SystemExit("native source is not descended from the approved input-owner base")

    replacements = {}
    for arch, triplet, name in (("arm64", "arm64-osx-12", "arm64"),
                                ("x64", "x64-osx-12", "x86_64")):
        build = NATIVE / f"build-hidinput-{arch}"
        run("cmake", "-S", str(NATIVE), "-B", str(build), "-DCMAKE_BUILD_TYPE=Release",
            f"-DCMAKE_TOOLCHAIN_FILE={vcpkg / 'scripts/buildsystems/vcpkg.cmake'}",
            f"-DVCPKG_OVERLAY_TRIPLETS={NATIVE / 'triplets'}",
            f"-DVCPKG_TARGET_TRIPLET={triplet}", f"-DCMAKE_OSX_ARCHITECTURES={name}",
            "-DCMAKE_OSX_DEPLOYMENT_TARGET=12.0", "-DPROJECT_VERSION=1.18.1",
            "-DHIDINPUT_DIAG_CLOSE=OFF")
        run("cmake", "--build", str(build), "-j", "4")
        shared = build / "libYubico.NativeShims.dylib"
        static = build / "static/libYubico.NativeShims.a"
        for artifact in (shared, static):
            run("bash", str(NATIVE / "tests/check_exports.sh"), str(artifact))
        if "minos 12.0" not in output("vtool", "-show-build", str(shared)):
            raise SystemExit(f"macOS 12 deployment target missing from {shared}")
        replacements[f"runtimes/osx-{arch}/native/libYubico.NativeShims.dylib"] = shared.read_bytes()
        replacements[f"buildTransitive/static/osx-{arch}/libYubico.NativeShims.a"] = static.read_bytes()
    targets = (NATIVE / "msbuild/Yubico.NativeShims.Aot.targets").read_bytes()
    replacements["build/Yubico.NativeShims.targets"] = targets
    replacements["buildTransitive/Yubico.NativeShims.targets"] = targets

    diff = output("git", "diff", "HEAD", "--binary")
    untracked = output("git", "ls-files", "--others", "--exclude-standard").splitlines()
    provenance = {
        "purpose": "unsigned local-only experimental prerelease; not a Yubico-signed release",
        "source_package_sha256": SOURCE_SHA256,
        "source_git_commit": output("git", "rev-parse", "HEAD"),
        "source_base_git_commit": SOURCE_BASE,
        "source_tracked_diff_sha256": digest(diff.encode()),
        "source_untracked_sha256": {name: digest((ROOT / name).read_bytes()) for name in untracked},
        "vcpkg_git_commit": BASELINE,
        "vcpkg_openssl": "3.6.4",
        "macos_close_diagnostic": "disabled; no failed-close logging in packaged native binaries",
        "replaced_assets_sha256": {name: digest(data) for name, data in sorted(replacements.items())},
        "unchanged_assets": "Windows/Linux and net472 targets copied byte-for-byte from signed 1.18.0 input",
    }
    replacements["docs/local-provenance.json"] = (json.dumps(provenance, indent=2, sort_keys=True) + "\n").encode()

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    temporary = OUTPUT.with_suffix(".tmp")
    with zipfile.ZipFile(signed) as original, zipfile.ZipFile(temporary, "w") as pkg:
        names = set(original.namelist())
        if not set(replacements).difference({"docs/local-provenance.json"}) <= names:
            raise SystemExit("signed input package lacks expected macOS or target assets")
        if ".signature.p7s" not in names:
            raise SystemExit("signed input has no signature entry")
        metadata = [name for name in names if name.endswith(".psmdcp")]
        if len(metadata) != 1:
            raise SystemExit("expected exactly one core-properties metadata asset")
        for info in original.infolist():
            if info.filename == ".signature.p7s":
                continue  # Signing a modified package would misrepresent its provenance.
            data = replacements.get(info.filename, original.read(info.filename))
            if info.filename == "Yubico.NativeShims.nuspec" or info.filename in metadata:
                old = b"<version>1.18.0</version>"
                tree = ET.fromstring(data)
                version = tree.find(".//{*}version")
                if version is None or version.text != "1.18.0" or data.count(old) != 1:
                    raise SystemExit(f"unexpected source version format: {info.filename}")
                data = data.replace(old, f"<version>{VERSION}</version>".encode())
            if info.filename == "[Content_Types].xml":
                data = data.replace(b"</Types>",
                    b'<Default Extension="json" ContentType="application/json" />\n</Types>')
            pkg.writestr(info, data)
        pkg.writestr(zipfile.ZipInfo("docs/local-provenance.json", (2026, 9, 23, 0, 0, 0)),
                     replacements["docs/local-provenance.json"])
    with zipfile.ZipFile(temporary) as pkg:
        for name in names - {".signature.p7s", "Yubico.NativeShims.nuspec",
                              "[Content_Types].xml"} - set(metadata) - replacements.keys():
            if pkg.read(name) != original_asset(signed, name):
                raise SystemExit(f"unchanged asset drift: {name}")
    temporary.replace(OUTPUT)  # Only this generated local artifact; never a NuGet cache entry.
    print(f"LOCAL ONLY unsigned {OUTPUT} sha256={digest(OUTPUT.read_bytes())}")


def original_asset(signed, name):
    with zipfile.ZipFile(signed) as package:
        return package.read(name)


if __name__ == "__main__":
    main()
