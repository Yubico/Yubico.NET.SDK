"""Capture/check final integration records and verify exact staged bytes, read-only Git.

Snapshot writes only a new manifest under this packet. Check/staged modes read only.
Retain raw source captures with -text before snapshotting when byte-exact archive
identity is required. Report LF normalization separately from raw byte identity.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

HERE = Path(__file__).resolve().parent
PACKET = HERE.parent
ROOT = HERE.parents[5]
PREFIX = PACKET.relative_to(ROOT).as_posix() + "/"


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def git(*args, raw=None):
    return subprocess.check_output(["git", *args], cwd=ROOT, input=raw)


def attrs(path, cached=False):
    args = ["check-attr", "-z"] + (["--cached"] if cached else [])
    fields = git(*args, "text", "eol", "filter", "working-tree-encoding", "--", path).decode().split("\0")[:-1]
    return {fields[i + 1]: fields[i + 2] for i in range(0, len(fields), 3)}


def files(manifest):
    entries = list(PACKET.rglob("*"))
    require(not any(path.is_symlink() for path in entries), "Packet contains a symlink")
    return sorted(path for path in entries if path.is_file() and path != manifest
                  and path != HERE / "coordinator-command.lock")


def record(path):
    require(not path.is_symlink() and path.resolve().is_relative_to(PACKET), f"Escaping/symlinked record: {path}")
    relative = path.relative_to(ROOT).as_posix()
    forbidden = {"bin", "obj", "node_modules", "__pycache__", ".git"}
    require(not forbidden.intersection(path.relative_to(PACKET).parts), f"Generated directory: {path}")
    require(not any(suffix.lower() in {".dll", ".exe", ".pdb", ".so", ".a", ".o", ".dylib", ".pyc", ".nupkg", ".zip"}
                    for suffix in path.suffixes), f"Binary/cache artifact: {path}")
    raw = path.read_bytes()
    raw.decode("utf-8-sig")
    require(b"\0" not in raw and not raw.startswith((b"MZ", b"\x7fELF")), f"Runtime/binary content: {path}")
    attributes = attrs(relative)
    require(attributes["filter"] in ("unspecified", "unset") and attributes["working-tree-encoding"] in ("unspecified", "unset"),
            f"Review custom clean/encoding policy: {path}")
    normalized = raw.replace(b"\r\n", b"\n")
    expected = raw if attributes["text"] == "unset" else normalized
    captured_source = path.name.endswith(".snapshot") or "/resolver-inputs/blobs/" in relative
    require(not captured_source or expected == raw, f"Captured source needs byte-preserving attributes: {path}")
    # hash-object applies the repository's actual clean policy without writing objects.
    actual_oid = git("hash-object", "--path=" + relative, "--stdin", raw=raw).decode().strip()
    expected_oid = git("hash-object", "--stdin", raw=expected).decode().strip()
    require(actual_oid == expected_oid, f"Unmodeled clean transform: {path}")
    return dict(path=relative, bytes=len(raw), raw_sha256=sha(raw), normalized_sha256=sha(normalized),
                staged_sha256=sha(expected), staged_oid=actual_oid, attributes=attributes,
                crlf_count=raw.count(b"\r\n"), raw_preserved_in_git=expected == raw)


def execute(mode, manifest):
    manifest = manifest.resolve()
    require(manifest.parent == HERE, "Keep the manifest directly under validation/")
    current = [record(path) for path in files(manifest)]
    if mode == "snapshot":
        require(not manifest.exists(), "Use a fresh manifest name; preserve earlier snapshots")
        with manifest.open("x") as output:
            json.dump(dict(scope=PREFIX, files=current), output, indent=2)
            output.write("\n")
    else:
        data = json.loads(manifest.read_bytes())
        require(data["scope"] == PREFIX and data["files"] == current, "Final record set/bytes/attributes changed")
    if mode == "staged":
        require(not git("ls-files", "--unmerged", "--", PREFIX), "Resolve packet index conflicts first")
        indexed = set(git("ls-files", "-z", "--", PREFIX).decode().split("\0")) - {""}
        expected_paths = {row["path"] for row in current} | {manifest.relative_to(ROOT).as_posix()}
        require(indexed == expected_paths, "Staged packet set differs from complete final manifest")
        for row in current + [record(manifest)]:
            path = row["path"]
            require(attrs(path, cached=True) == row["attributes"], f"Index/worktree attributes differ: {path}")
            staged = git("show", ":" + path)
            require(sha(staged) == row["staged_sha256"], f"Staged byte mismatch: {path}")
            require(git("rev-parse", ":" + path).decode().strip() == row["staged_oid"], f"Staged object mismatch: {path}")
    print(f"{mode}: {len(current)} integration records; raw, normalized and Git-clean identities verified")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("snapshot", "check", "staged"))
    parser.add_argument("--manifest", type=Path, default=HERE / "final-record-manifest.json")
    args = parser.parse_args()
    execute(args.mode, args.manifest)
