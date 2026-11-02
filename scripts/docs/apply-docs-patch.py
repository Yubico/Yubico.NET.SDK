#!/usr/bin/env python3
"""Safely apply a generated docs patch, validating before changing writer files.

Usage: python3 scripts/docs/apply-docs-patch.py {migration|architecture} PATCH
Requires only Python's standard library and Git on the Ubuntu writer runner.
"""

import argparse
import os
from pathlib import Path
import subprocess
import sys
import tempfile


REGULAR_MODES = (b"100644", b"100755")
MIGRATION_PATHS = {
    b"docs/migration/v1-to-v2.md",
    b"docs/migration/v1-to-v2-map.yml",
    b"docs/migration/v1-to-v2-changelog.md",
    b"docs/migration/.state.yml",
}


def git(*args, env=None, data=None):
    return subprocess.run(["git", *args], input=data, env=env, check=True, stdout=subprocess.PIPE).stdout


def check_patch_modes(patch):
    # Git normalizes modes such as 104644 to 100644. Check the original headers
    # too, including intermediate entries which a final tree diff could hide.
    # Hunk lines have a ' ', '+' or '-' prefix; binary payload has no spaces.
    for line in patch.split(b"\n"):
        if line.startswith((b"old mode ", b"new mode ")):
            raise ValueError(f"mode changes are forbidden: {line!r}")
        if line.startswith(b"new file mode ") and line[14:] != b"100644":
            raise ValueError(f"new files must have mode 100644: {line!r}")
        if line.startswith(b"deleted file mode ") and line[18:] not in REGULAR_MODES:
            raise ValueError(f"only regular files may be deleted: {line!r}")
        if line.startswith(b"index "):
            fields = line.split()
            if len(fields) > 2 and fields[-1] not in REGULAR_MODES:
                raise ValueError(f"only canonical regular-file modes are allowed: {line!r}")


def apply_patch(policy, patch):
    check_patch_modes(patch)
    with tempfile.TemporaryDirectory(prefix="docs-patch-") as tmp:
        env = {**os.environ, "GIT_INDEX_FILE": str(Path(tmp) / "index")}
        git("read-tree", "HEAD", env=env)
        # --cached writes only temporary index entries and Git objects. Never
        # apply untrusted contents to the working tree to discover their paths.
        git("apply", "--cached", "--whitespace=error", "-", env=env, data=patch)
        records = git("diff", "--cached", "--raw", "-z", "--no-renames",
                      "--no-ext-diff", "--no-textconv", "HEAD", env=env).split(b"\0")
        for header, path in zip(records[0:-1:2], records[1:-1:2]):
            old_mode, new_mode = header.split()[:2]
            old_mode = old_mode.removeprefix(b":")
            allowed = path in MIGRATION_PATHS if policy == "migration" else path.startswith(b"docs/architecture/")
            if not allowed:
                raise ValueError(f"path outside {policy} artifacts: {path!r}")
            current = Path(".")
            for component in path.split(b"/"):
                current /= os.fsdecode(component)
                if current.is_symlink():
                    raise ValueError(f"symlink in working-tree path: {current}")
            if old_mode == b"000000":
                safe_mode = new_mode == b"100644"
            elif new_mode == b"000000":
                safe_mode = old_mode in REGULAR_MODES
            else:
                safe_mode = old_mode in REGULAR_MODES and new_mode == old_mode
            if not safe_mode:
                raise ValueError(f"unsafe file type or mode: {path!r} ({old_mode!r} -> {new_mode!r})")
        # Regenerate from the validated tree, so duplicate/transient patch
        # entries cannot cause hidden path effects when materialized. --index
        # also requires the real files to match their index. It does NOT prevent
        # following untracked symlink ancestors; check those explicitly above.
        safe_patch = git("diff", "--cached", "--binary", "--full-index", "--no-renames",
                         "--no-ext-diff", "--no-textconv", "HEAD", env=env)
        git("apply", "--index", "--whitespace=error", "-", data=safe_patch)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("policy", choices=("migration", "architecture"))
    parser.add_argument("patch", type=Path)
    args = parser.parse_args()
    try:
        apply_patch(args.policy, args.patch.read_bytes())
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        print(f"Unsafe or invalid documentation patch: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
