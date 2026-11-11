#!/usr/bin/env python3
"""Fail-closed result/provenance regressions; no device operations or source mutations."""

import copy
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

import capture
from evidence_gate import REQUIRED_NATIVE_CHECKS, validate_result
from publish_contract import seal, sha, verify_publish, STAMP

RECORDS = Path(sys.argv.pop(1)) if len(sys.argv) > 1 else None


class RuntimeGateTests(unittest.TestCase):
    def setUp(self):
        self.jit = json.loads((RECORDS / "jit-native.json").read_text())
        self.aot = json.loads((RECORDS / "aot-native.json").read_text())

    def test_real_jit_record_cannot_be_substituted_for_aot(self):
        with self.assertRaisesRegex(ValueError, "DynamicCode"):
            validate_result(self.jit, "aot", "--native", 0, self.jit["SourceHash"])

    def test_real_aot_record_cannot_be_substituted_for_jit(self):
        with self.assertRaisesRegex(ValueError, "DynamicCode"):
            validate_result(self.aot, "jit", "--native", 0, self.aot["SourceHash"])

    def test_real_native_records_pass_their_own_runtime(self):
        for kind, record in (("jit", self.jit), ("aot", self.aot)):
            validate_result(record, kind, "--native", 0, record["SourceHash"])

    def test_missing_required_named_check_is_rejected(self):
        record = copy.deepcopy(self.aot)
        record["Checks"].remove(next(iter(REQUIRED_NATIVE_CHECKS)))
        with self.assertRaisesRegex(ValueError, "named checks"):
            validate_result(record, "aot", "--native", 0, record["SourceHash"])

    def test_invalid_positive_records_are_rejected(self):
        for field, value in (("Checks", []), ("Outcome", "FAILED"), ("DynamicCode", None),
                             ("DynamicCode", "false"), ("DynamicCode", 0), ("Mode", "--list"),
                             ("SourceHash", "unbound-label")):
            with self.subTest(field=field, value=value):
                record = copy.deepcopy(self.aot)
                record[field] = value
                with self.assertRaises(ValueError):
                    validate_result(record, "aot", "--native", 0, self.aot["SourceHash"])

    def test_duplicate_check_is_rejected(self):
        record = copy.deepcopy(self.aot)
        record["Checks"].append(record["Checks"][0])
        with self.assertRaisesRegex(ValueError, "duplicate"):
            validate_result(record, "aot", "--native", 0, record["SourceHash"])

    def test_real_intended_negative_assertions_and_cleanup_are_preserved(self):
        for kind in ("jit", "aot"):
            for mode in ("--sensitivity-blocking", "--sensitivity-quiescence"):
                result = json.loads((RECORDS / f"{kind}-{mode[2:]}.json").read_text())
                self.assertEqual(1, validate_result(result, kind, mode, 1, result["SourceHash"]))
                with self.assertRaises(ValueError):
                    validate_result(result, kind, mode, 3, result["SourceHash"])
                result["Failure"] = "System.InvalidOperationException: some other failure\n"
                with self.assertRaisesRegex(ValueError, "exact intended negative"):
                    validate_result(result, kind, mode, 1, result["SourceHash"])


class PublishBindingTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="linux-hid-gates-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.source = self.root / "Fixture.cs"
        self.source.write_text("// original synthetic source\n")
        for kind in ("jit", "aot"):
            folder = self.root / kind
            folder.mkdir()
            binary = folder / "LinuxHidRouteVerification"
            binary.write_bytes(b"synthetic executable artifact identity, not executed")
            binary.chmod(0o700)
        (self.root / "jit/LinuxHidRouteVerification.dll").write_bytes(b"synthetic assembly identity")
        self.before = self.inputs()
        seal(self.root, self.before, self.inputs())

    def inputs(self):
        return {"source": {"sourceHash": sha(self.source)}, "effectiveBuild": {"DefineConstants": "TRACE"},
                "dependencies": {"nativeArchiveSha256": "synthetic-package-identity"}}

    def assert_capture_refuses_before_operation(self, reason):
        # Exercise capture's real entry point, resolving a real stale/missing filesystem stamp.
        # Only the location provider is redirected to this isolated fixture. No source/artifact
        # in the working checkout is modified, and no verification child may start.
        with patch.object(sys, "argv", ["capture.py", "--native-only"]), \
             patch.object(capture, "verify_publish", lambda: verify_publish(self.root, self.inputs())), \
             patch.object(capture.subprocess, "run") as child, \
             patch.object(capture, "hardware_snapshot") as snapshot:
            with self.assertRaisesRegex(ValueError, reason):
                capture.main()
            child.assert_not_called()
            snapshot.assert_not_called()

    def test_unchanged_source_and_artifacts_pass(self):
        self.assertEqual(self.before, verify_publish(self.root, self.inputs())["inputs"])

    def test_stale_source_is_rejected_before_execution(self):
        self.source.write_text("// edited after publish\n")
        with self.assertRaisesRegex(ValueError, "stale publish source"):
            verify_publish(self.root, self.inputs())
        self.assert_capture_refuses_before_operation("stale publish source")

    def test_stale_binary_is_rejected(self):
        (self.root / "aot/LinuxHidRouteVerification").write_bytes(b"replaced executable")
        with self.assertRaisesRegex(ValueError, "stale published binary"):
            verify_publish(self.root, self.inputs())
        self.assert_capture_refuses_before_operation("stale published binary")

    def test_stale_jit_assembly_is_rejected(self):
        (self.root / "jit/LinuxHidRouteVerification.dll").write_bytes(b"replaced assembly")
        with self.assertRaisesRegex(ValueError, "stale published binary"):
            verify_publish(self.root, self.inputs())

    def test_missing_publish_manifest_is_rejected(self):
        (self.root / STAMP).unlink()
        with self.assertRaisesRegex(ValueError, "missing publish manifest"):
            verify_publish(self.root, self.inputs())
        self.assert_capture_refuses_before_operation("missing publish manifest")

    def test_source_change_during_publish_cannot_be_sealed(self):
        (self.root / STAMP).unlink()
        self.source.write_text("// changed during publish\n")
        with self.assertRaisesRegex(ValueError, "inputs changed during publish"):
            seal(self.root, self.before, self.inputs())
        self.assertFalse((self.root / STAMP).exists())

    def test_effective_build_change_is_rejected(self):
        current = self.inputs()
        current["effectiveBuild"]["DefineConstants"] = "DIFFERENT"
        with self.assertRaisesRegex(ValueError, "effective inputs"):
            verify_publish(self.root, current)

    def test_package_identity_change_is_rejected(self):
        current = self.inputs()
        current["dependencies"]["nativeArchiveSha256"] = "different-package-identity"
        with self.assertRaisesRegex(ValueError, "effective inputs"):
            verify_publish(self.root, current)

    def test_extra_publish_artifact_is_rejected(self):
        (self.root / "jit/unexpected.dll").write_bytes(b"unbound assembly")
        with self.assertRaisesRegex(ValueError, "stale published binary"):
            verify_publish(self.root, self.inputs())


if __name__ == "__main__":
    if RECORDS is None:
        raise SystemExit("Usage: test_evidence_gates.py CAPTURE_DIRECTORY")
    unittest.main()
