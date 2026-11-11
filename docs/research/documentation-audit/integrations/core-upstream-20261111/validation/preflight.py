"""Read-only project selector and host availability inventory; inspect pinned tooling.

No product source extraction, restore, build, test, service or device operations.
"""
import hashlib
import datetime
import json
from pathlib import Path
import platform
import shutil
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]
PIN = "5746e72ed9a848d08b5782ddde025aa4a4b522ef"
CAMPAIGN = "f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa"


def command(args):
    try:
        completed = subprocess.run(args, cwd=ROOT, capture_output=True, text=True)
        return dict(command=args, exit_code=completed.returncode, stdout=completed.stdout, stderr=completed.stderr)
    except FileNotFoundError as error:
        return dict(command=args, exit_code=127, stdout="", stderr=str(error))


def main():
    projects = sorted(path.relative_to(ROOT).as_posix() for path in ROOT.rglob("*.csproj") if ".claude" not in path.relative_to(ROOT).parts)
    # Exact Context.FromArgs + DiscoverProjects behavior at the pinned incoming revision:
    buildable = [path for path in projects if "/src/" in "/" + path and "Yubico.YubiKit." in path]
    unit = [path for path in projects if "/tests/" in "/" + path and ".UnitTests" in path and "Yubico.YubiKit." in path]
    integration = [path for path in projects if "/tests/" in "/" + path and ".IntegrationTests" in path and "Yubico.YubiKit." in path]
    selectors = {}
    for selector in ("Core", "Yubico.YubiKit.Core", "Yubico.YubiKit.Core.UnitTests", "Yubico.YubiKit.Core.IntegrationTests"):
        selectors[selector] = {kind: [path for path in candidates if selector.lower() in Path(path).stem.lower()]
                               for kind, candidates in (("build", buildable), ("unit", unit), ("integration", integration))}
    sources = []
    names = ["CLAUDE.md", "TOOLCHAIN.md", "global.json", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props",
             ".github/workflows/build.yml", ".github/workflows/native-aot.yml", "scripts/toolchain/Discovery.cs", "scripts/toolchain/Context.cs",
             "scripts/toolchain/BuildTargets.cs", "scripts/toolchain/TestTargets.cs", "scripts/toolchain/RunLog.cs", "scripts/toolchain/Results.cs",
             "scripts/toolchain/DocsTargets.cs", "scripts/toolchain/CoverageTargets.cs", "verification/LinuxHidRouteVerification/capture.py",
             "verification/LinuxHidRouteVerification/test_evidence_gates.py", "verification/LinuxHidRouteVerification/publish_contract.py",
             "scripts/architecture/check-architecture-map.sh", "scripts/architecture/check-architecture-images.sh", "scripts/docs/test-docs-patches.py"]
    for name in names:
        raw = subprocess.check_output(["git", "show", f"{PIN}:{name}"], cwd=ROOT)
        sources.append(dict(revision=PIN, path=name, bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest()))
    prior = []
    piv = "docs/research/documentation-audit/modules/piv/pass-1/piv-001/"
    for name in ("review-closeout.md", "snippet-validation.md", "corrections.md",
                 "validation/check-preserved.py", "validation/check-records.py",
                 "validation/examples/v4-tools/capture-v4.py", "validation/examples/v4-tools/compiler_policy.py",
                 "validation/examples/v4-tools/project_graph_v4.py", "validation/examples/v4-tools/verify-v4.py"):
        path = piv + name
        raw = subprocess.check_output(["git", "show", f"{CAMPAIGN}:{path}"], cwd=ROOT)
        prior.append(dict(revision=CAMPAIGN, path=path, bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest()))
    diagnostics = [command(args) for args in (["uname", "-a"], ["uname", "-m"], ["dotnet", "--info"],
                  ["clang", "--version"], ["ld", "--version"], ["objcopy", "--version"],
                  ["clang", "--print-file-name=crt1.o"], ["clang", "--print-file-name=libz.a"],
                  ["pkg-config", "--modversion", "zlib", "libpcsclite", "libudev"],
                  ["pacman", "-Q", "clang", "lld", "binutils", "zlib", "glibc", "pcsclite", "systemd-libs"],
                  ["git", "diff", "--name-only", "--diff-filter=U"])]
    print(json.dumps(dict(pin=PIN, observed_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                         machine=platform.machine(), cpu_count=__import__("os").cpu_count(),
                         available_commands={name: shutil.which(name) for name in ("dotnet", "clang", "ld", "objcopy", "python3", "bash", "pkg-config")},
                         selectors=selectors, discovered_unit_projects=unit, pinned_sources=sources, frozen_piv_reuse_sources=prior,
                         diagnostics=diagnostics, limits="Availability only; service readiness, AOT linking, tests and native execution await coordinator gates."), indent=2))


if __name__ == "__main__":
    main()
