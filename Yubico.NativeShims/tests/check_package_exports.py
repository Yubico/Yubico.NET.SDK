"""Check actual shared exports and static definitions in a NativeShims nupkg.

Usage: python3 check_package_exports.py /path/to/Yubico.NativeShims.*.nupkg
Requires LLVM llvm-nm and llvm-readobj (Homebrew llvm or on PATH).
Uses ELF dynamic exports, PE export directories and Mach-O exported symbols for
shared libraries; static archives use externally defined symbols. No device or
cross-platform execution is needed. Fixture coverage compiles Mach-O locally;
ELF and PE parsers are exercised against actual package assets.
"""

import hashlib
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from zipfile import ZipFile


HERE = Path(__file__).resolve().parent
RIDS = ("linux-arm64", "linux-x64", "osx-arm64", "osx-x64",
        "win-arm64", "win-x64", "win-x86")


def synthetic_symbols():
    # owner.h declares the nonshipping test entry points. The separately named
    # create_synthetic is a reserved synthetic factory name, not a declaration
    # in that header. Ordinary hidinput_* implementation symbols are legitimate.
    header = (HERE.parent / "hidinput/owner.h").read_text()
    declarations = header.split("/* Synthetic-only entry points in the nonshipping harness. */", 1)[1]
    return set(re.findall(r"\b(hidinput_test_\w+)\s*\(", declarations)) | {"create_synthetic"}


def tool(name):
    binary = shutil.which(name) or str(Path("/opt/homebrew/opt/llvm/bin") / name)
    if not Path(binary).is_file():
        raise RuntimeError(f"{name} not available; cannot inspect package")
    return binary


def run(*args):
    result = subprocess.run(args, capture_output=True, text=True, check=True)
    return result.stdout


def normalized(name, rid):
    if rid.startswith(("osx-", "win-")):
        name = name.removeprefix("_")
    if rid == "win-x86":
        name = re.sub(r"@\d+$", "", name)
    return name


def static_symbols(path, rid):
    output = run(tool("llvm-nm"), "--defined-only", "--extern-only",
                 "--format=posix", str(path))
    return {normalized(line.split()[0], rid) for line in output.splitlines()
            if len(line.split()) >= 3 and line.split()[1] not in ("U", "u")}


def shared_symbols(path, rid):
    if rid.startswith("win-"):
        output = run(tool("llvm-readobj"), "--coff-exports", str(path))
        return {normalized(name, rid) for name in re.findall(r"^\s*Name: (\S+)$", output, re.M)}
    flags = ("--dynamic",) if rid.startswith("linux-") else ("--export-symbols",)
    output = run(tool("llvm-nm"), *flags, "--defined-only", "--extern-only",
                 "--format=just-symbols" if rid.startswith("osx-") else "--format=posix", str(path))
    return {normalized(line.split()[0], rid) for line in output.splitlines()
            if (rid.startswith("osx-") and len(line.split()) == 1) or
            (len(line.split()) >= 3 and line.split()[1] not in ("U", "u"))}


def check_symbols(symbols, expected, macos):
    macos_expected = {line.strip() for line in (HERE / "expected_symbols.macos.txt").read_text().splitlines()
                      if line.strip() and not line.lstrip().startswith("#")} if macos else set()
    canonical = expected | macos_expected
    errors = [f"missing: {s}" for s in sorted(canonical - symbols)]
    errors += [f"extra Native_: {s}" for s in sorted(symbols - canonical)
               if s.startswith("Native_")]
    errors += sorted(symbols & synthetic_symbols())
    return errors


def inspect_artifact(archive, directory, rid, kind, entry, expected):
    try:
        # Read only the exact package member; never extract arbitrary zip paths.
        path = Path(directory) / f"{rid}-{kind}-{Path(entry).name}"
        path.write_bytes(archive.read(entry))
        symbols = shared_symbols(path, rid) if kind == "shared" else static_symbols(path, rid)
        errors = check_symbols(symbols, expected, rid.startswith("osx-"))
        canonical_count = len({s for s in symbols if s.startswith("Native_")})
        print(f"{rid} {kind}: {'FAIL ' + ', '.join(errors) if errors else 'PASS'} "
              f"({canonical_count} Native_* found)")
        return bool(errors)
    except (KeyError, OSError, subprocess.CalledProcessError, RuntimeError) as error:
        detail = error.stderr if isinstance(error, subprocess.CalledProcessError) else error
        print(f"{rid} {kind}: BLOCKED ({detail})")
        return True


def main(package):
    expected = {line.strip() for line in (HERE / "expected_symbols.txt").read_text().splitlines()
                if line.strip() and not line.lstrip().startswith("#")}
    failures = 0
    with open(package, "rb") as stream:
        print(f"SHA-256: {hashlib.file_digest(stream, 'sha256').hexdigest()}")
    with ZipFile(package) as archive, tempfile.TemporaryDirectory() as directory:
        for rid in RIDS:
            windows = rid.startswith("win-")
            filename = "Yubico.NativeShims" if windows else "libYubico.NativeShims"
            shared = f"runtimes/{rid}/native/{filename}{'.dll' if windows else '.dylib' if rid.startswith('osx-') else '.so'}"
            static = f"buildTransitive/static/{rid}/{filename}{'.lib' if windows else '.a'}"
            for kind, entry in (("shared", shared), ("static", static)):
                failures += inspect_artifact(archive, directory, rid, kind, entry, expected)
    return 1 if failures else 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("usage: python3 check_package_exports.py <nupkg>")
    sys.exit(main(sys.argv[1]))
