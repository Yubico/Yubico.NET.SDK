#!/usr/bin/env python3
"""Exercise the real writer run blocks with disposable Git patch fixtures.

Run: python3 scripts/docs/test-docs-patches.py
No .NET, network, credentials, or hardware are used.
"""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[2]
WRITERS = (
    ("docs-update.yml", "Apply documentation patches", "patches/migration/migration.patch", "migration"),
    ("docs-update.yml", "Apply documentation patches", "patches/architecture/architecture.patch", "architecture"),
    ("architecture-docs-monthly-synthesis.yml", "Apply architecture patch", "patches/architecture.patch", "architecture"),
    ("migration-docs-monthly-synthesis.yml", "Apply migration patch", "patches/migration.patch", "migration"),
)


def git(repo, *args):
    return subprocess.run(["git", *args], cwd=repo, check=True, capture_output=True).stdout


def writer_block(workflow, step):
    lines = (ROOT / ".github/workflows" / workflow).read_text().splitlines()
    start = lines.index(f"      - name: {step}")
    run = lines.index("        run: |", start) + 1
    end = run
    while end < len(lines) and (not lines[end] or lines[end].startswith("          ")):
        end += 1
    return textwrap.dedent("\n".join(lines[run:end]))


class WriterPatchTests(unittest.TestCase):
    def run_fixture(self, writer, fixture, second_fixture=None):
        workflow, step, artifact, policy = writer
        with tempfile.TemporaryDirectory(prefix="docs-patches-") as tmp:
            base = Path(tmp) / "base"
            base.mkdir()
            git(base, "init", "-q")
            git(base, "config", "user.name", "Patch fixture")
            git(base, "config", "user.email", "fixture@example.invalid")
            git(base, "config", "core.fileMode", "true")
            git(base, "config", "core.autocrlf", "false")
            directory = base / "docs" / policy
            directory.mkdir(parents=True)
            doc = "v1-to-v2.md" if policy == "migration" else "guide.md"
            executable = "v1-to-v2-map.yml" if policy == "migration" else "existing.sh"
            deleted = "v1-to-v2-changelog.md" if policy == "migration" else "obsolete.md"
            added = ".state.yml" if policy == "migration" else "new-image.png"
            (directory / doc).write_text("before\n")
            (directory / executable).write_text("existing executable\n")
            (directory / executable).chmod(0o755)
            (directory / deleted).write_text("obsolete\n")
            (base / "outside.txt").write_text("must not change\n")
            architecture_doc = "docs/architecture/guide.md"
            architecture_image = "docs/architecture/new-image.png"
            if second_fixture is not None:
                (base / architecture_doc).parent.mkdir(parents=True)
                (base / architecture_doc).write_text("architecture before\n")
            script = ROOT / "scripts/docs/apply-docs-patch.py"
            if script.exists():
                target = base / "scripts/docs/apply-docs-patch.py"
                target.parent.mkdir(parents=True)
                shutil.copyfile(script, target)
            git(base, "add", "docs", "outside.txt", *(["scripts"] if script.exists() else []))
            git(base, "commit", "-qm", "fixture base")
            writer_repo = Path(tmp) / "writer"
            shutil.copytree(base, writer_repo)
            # copytree changes inodes; a real checkout has fresh index stat data.
            git(writer_repo, "update-index", "--refresh")

            if fixture in ("ordinary", "quoted-path"):
                (directory / doc).write_text("after\n")
                (directory / executable).write_text("updated executable\n")
                (directory / deleted).unlink()
                (directory / added).write_bytes(b"state: updated\n" if policy == "migration" else b"\x00binary image\xff")
                if fixture == "quoted-path":
                    (directory / 'quoted "name"\twith\nnewline.md').write_text("ordinary documentation\n")
            elif fixture in ("symlink", "symlink-replacement"):
                destination = directory / (doc if fixture == "symlink-replacement" else added)
                if destination.exists():
                    destination.unlink()
                destination.symlink_to("../../outside.txt")
            elif fixture == "mode-change":
                (directory / doc).chmod(0o755)
            elif fixture == "new-executable":
                (directory / added).write_text("unexpected executable\n")
                (directory / added).chmod(0o755)
            elif fixture == "outside-path":
                (base / "outside.txt").write_text("unsafe change\n")
            elif fixture == "wrong-artifact-scope":
                other = base / "docs" / ("architecture" if policy == "migration" else "migration")
                other.mkdir(parents=True)
                (other / "v1-to-v2.md").write_text("wrong artifact\n")
            elif fixture == "unexpected-migration-file":
                (directory / "unexpected.md").write_text("not an allowed migration artifact\n")
            elif fixture in ("noncanonical-mode", "gitlink", "path-traversal", "symlink-ancestor"):
                (directory / added).write_text("unexpected mode\n")
            else:
                raise AssertionError(f"Unknown fixture {fixture}")
            git(base, "add", "-N", "docs", "outside.txt")
            patch = git(base, "diff", "HEAD", "--binary")
            if fixture == "noncanonical-mode":
                patch = patch.replace(b"new file mode 100644", b"new file mode 104644")
            elif fixture == "gitlink":
                sha = git(base, "rev-parse", "HEAD").strip()
                path = f"docs/{policy}/{added}"
                patch = (f"diff --git a/{path} b/{path}\nnew file mode 160000\n"
                         f"--- /dev/null\n+++ b/{path}\n@@ -0,0 +1 @@\n+Subproject commit ").encode() + sha + b"\n"
            elif fixture == "path-traversal":
                patch = patch.replace(f"docs/{policy}/{added}".encode(), f"docs/{policy}/../../escaped.txt".encode())
            elif fixture == "symlink-ancestor":
                patch = patch.replace(f"docs/{policy}/{added}".encode(), f"docs/{policy}/link/escaped.txt".encode())
                (writer_repo / "docs" / policy / "link").symlink_to(Path(tmp))

            artifact_path = writer_repo / artifact
            artifact_path.parent.mkdir(parents=True)
            artifact_path.write_bytes(patch)
            if second_fixture is not None:
                second_doc = base / architecture_doc
                if second_fixture == "symlink-replacement":
                    second_doc.unlink()
                    second_doc.symlink_to("../../outside.txt")
                else:
                    second_doc.write_text("architecture after\n")
                    if second_fixture == "mode-change":
                        second_doc.chmod(0o755)
                (base / architecture_image).write_bytes(b"\x00second artifact\xff")
                git(base, "add", "-N", "docs/architecture")
                second_artifact = writer_repo / WRITERS[1][2]
                second_artifact.parent.mkdir(parents=True)
                second_artifact.write_bytes(git(base, "diff", "HEAD", "--binary", "--", "docs/architecture"))
            status_args = ("status", "--porcelain=v1", "-z", "--untracked-files=all", "--", ".", ":(exclude)patches")
            before_status = git(writer_repo, *status_args)
            result = subprocess.run(
                ["bash", "--noprofile", "--norc", "-e", "-o", "pipefail", "-c", writer_block(workflow, step)],
                cwd=writer_repo, capture_output=True, text=True,
                env=os.environ.copy(),
            )
            output = result.stdout + result.stderr
            if fixture in ("ordinary", "quoted-path"):
                if second_fixture in (None, "ordinary"):
                    self.assertEqual(result.returncode, 0, output)
                    self.assertEqual(result.stdout, "", output)
                else:
                    self.assertNotEqual(result.returncode, 0, f"Unsafe second artifact was accepted\n{output}")
                    self.assertIn("Unsafe or invalid documentation patch:", result.stderr)
                output_dir = writer_repo / "docs" / policy
                self.assertEqual((output_dir / doc).read_text(), "after\n")
                self.assertEqual((output_dir / executable).read_text(), "updated executable\n")
                self.assertTrue((output_dir / executable).stat().st_mode & 0o111)
                self.assertFalse((output_dir / deleted).exists())
                self.assertEqual((output_dir / added).read_bytes(), (directory / added).read_bytes())
                if fixture == "quoted-path":
                    self.assertEqual((output_dir / 'quoted "name"\twith\nnewline.md').read_text(), "ordinary documentation\n")
                if second_fixture is not None:
                    expected_staged = {f"docs/migration/{name}".encode() for name in (doc, executable, deleted, added)}
                    self.assertEqual(git(writer_repo, "show", f":docs/migration/{doc}"), b"after\n")
                    self.assertEqual(git(writer_repo, "show", f":docs/migration/{executable}"), b"updated executable\n")
                    self.assertEqual(git(writer_repo, "show", f":docs/migration/{added}"), b"state: updated\n")
                    if second_fixture == "ordinary":
                        expected_staged.update(path.encode() for path in (architecture_doc, architecture_image))
                        for path in (architecture_doc, architecture_image):
                            expected = (base / path).read_bytes()
                            self.assertEqual((writer_repo / path).read_bytes(), expected)
                            self.assertEqual(git(writer_repo, "show", f":{path}"), expected)
                    else:
                        self.assertEqual(git(writer_repo, "status", "--porcelain", "--untracked-files=all", "--", "docs/architecture"), b"", output)
                        self.assertEqual((writer_repo / architecture_doc).read_text(), "architecture before\n")
                        self.assertEqual((writer_repo / "outside.txt").read_text(), "must not change\n")
                    self.assertEqual(set(git(writer_repo, "diff", "--cached", "--name-only", "-z").split(b"\0")[:-1]), expected_staged)
                    self.assertEqual(git(writer_repo, "diff", "--binary"), b"", output)
                    self.assertEqual(git(writer_repo, "diff", "--cached", "--check"), b"", output)
            else:
                self.assertNotEqual(result.returncode, 0, f"{fixture} patch was accepted\n{output}")
                # Rejection must precede ANY working-tree or real-index changes.
                self.assertEqual(git(writer_repo, "diff", "HEAD", "--raw"), b"", output)
                self.assertEqual(git(writer_repo, *status_args), before_status, output)
                self.assertFalse((writer_repo / "docs" / policy / added).is_symlink(), output)
                self.assertFalse((writer_repo / "docs" / policy / added).exists(), output)
                self.assertEqual((writer_repo / "outside.txt").read_text(), "must not change\n", output)
                self.assertFalse((Path(tmp) / "escaped.txt").exists(), output)
                self.assertFalse((writer_repo / "escaped.txt").exists(), output)

    def test_ordinary_patches_preserve_files_and_existing_modes(self):
        for writer in WRITERS:
            with self.subTest(writer=writer[0], artifact=writer[2]):
                self.run_fixture(writer, "ordinary")

    def test_unsafe_patches_are_rejected_before_application(self):
        fixtures = ("symlink", "symlink-replacement", "mode-change", "new-executable",
                    "gitlink", "noncanonical-mode", "outside-path", "wrong-artifact-scope", "path-traversal", "symlink-ancestor")
        for writer in WRITERS:
            for fixture in fixtures:
                with self.subTest(writer=writer[0], artifact=writer[2], fixture=fixture):
                    self.run_fixture(writer, fixture)

    def test_architecture_writers_accept_quoted_ordinary_paths(self):
        for writer in WRITERS:
            if writer[3] == "architecture":
                with self.subTest(writer=writer[0]):
                    self.run_fixture(writer, "quoted-path")

    def test_migration_writers_keep_the_four_artifact_boundary(self):
        for writer in WRITERS:
            if writer[3] == "migration":
                with self.subTest(writer=writer[0]):
                    self.run_fixture(writer, "unexpected-migration-file")

    def test_docs_update_preserves_both_valid_artifacts_in_index_and_working_tree(self):
        self.run_fixture(WRITERS[0], "ordinary", second_fixture="ordinary")

    def test_docs_update_rejects_second_artifact_without_reverting_first(self):
        for second_fixture in ("symlink-replacement", "mode-change"):
            with self.subTest(second_fixture=second_fixture):
                self.run_fixture(WRITERS[0], "ordinary", second_fixture=second_fixture)


if __name__ == "__main__":
    unittest.main(verbosity=2)
