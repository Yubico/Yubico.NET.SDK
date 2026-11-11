#!/usr/bin/env python3
"""Fail-closed result/provenance regressions; no device operations or source mutations."""

import copy
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

import capture
import publish_contract
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

    def test_publish_input_difference_names_category_without_printing_values(self):
        after = self.inputs()
        after["dependencies"]["nativeArchiveSha256"] = "do-not-print-this-value"
        with self.assertRaisesRegex(ValueError, "INPUT dependencies/nativeArchiveSha256") as error:
            seal(self.root, self.before, after)
        self.assertNotIn("do-not-print-this-value", str(error.exception))
        self.assertFalse((self.root / STAMP).exists())


class EvaluatedImportTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="linux-hid-imports-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.props = self.root / "Fixture.nuget.g.props"
        self.targets = self.root / "Fixture.nuget.g.targets"
        self.props.write_text("<Project><PropertyGroup><DefineConstants>INITIAL</DefineConstants></PropertyGroup></Project>")
        self.targets.write_text("<Project><Target Name='Unused'/></Project>")
        (self.root / "Fixture.proj").write_text(
            "<Project><Import Project='Fixture.nuget.g.props'/><Import Project='Fixture.nuget.g.targets'/></Project>")
        os.utime(self.root / "Fixture.proj", (999999999, 999999999))
        for name, value in (("ROOT", self.root), ("PROJECT", "Fixture.proj"), ("consumed_projects", lambda: ("Fixture.proj",))):
            patcher = patch.object(publish_contract, name, value)
            patcher.start()
            self.addCleanup(patcher.stop)
        os.utime(self.props, (1000000000, 1000000000))
        os.utime(self.targets, (1000000001, 1000000001))

    def test_restore_timestamp_reordering_does_not_change_consumed_imports(self):
        before = publish_contract.effective_build_inputs()
        os.utime(self.props, (1000000002, 1000000002))
        self.assertEqual(before, publish_contract.effective_build_inputs())

    def test_non_newest_import_content_change_is_bound(self):
        before = publish_contract.effective_build_inputs()
        self.props.write_text(self.props.read_text().replace("INITIAL", "DIFFERENT"))
        os.utime(self.props, (1000000000, 1000000000))
        after = publish_contract.effective_build_inputs()
        for kind in ("jit", "aot"):
            self.assertNotEqual(before[kind]["Fixture.proj"]["importedFileHashes"], after[kind]["Fixture.proj"]["importedFileHashes"])

    def test_generated_restore_import_initialization_is_not_silently_accepted(self):
        before = publish_contract.effective_build_inputs()
        extra = self.root / "RestoredPackage.targets"
        extra.write_text("<Project><Target Name='FromPackage'/></Project>")
        self.targets.write_text("<Project><Import Project='RestoredPackage.targets'/></Project>")
        after = publish_contract.effective_build_inputs()
        self.assertNotEqual(before, after)
        for kind in ("jit", "aot"):
            self.assertIn(str(extra), after[kind]["Fixture.proj"]["importedFileHashes"])


class RestoreContextTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="linux-hid-restore-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        patcher = patch.object(publish_contract, "ROOT", self.root)
        patcher.start()
        self.addCleanup(patcher.stop)
        (self.root / "Directory.Packages.props").write_text("<Project><YubiKitVersion>fixture</YubiKitVersion></Project>")
        self.package = self.root / "packages/fixture/1.0"
        self.package.mkdir(parents=True)
        (self.package / "native.a").write_bytes(b"original native dependency")
        self.assets = []
        projects = [publish_contract.PROJECT] + [f"src/{name}/src/Yubico.YubiKit.{name}.csproj" for name in ("Core", "Ctap2", "Management")]
        for project in projects:
            value = {"libraries": {"Fixture/1.0": {"type": "package", "path": "fixture/1.0"}},
                     "packageFolders": {str(self.root / "packages"): {}}, "targets": {"fixture": {}}}
            for kind in ("jit", "aot"):
                path = self.root / f"artifacts/linux-hid-verification/build/{kind}/obj/{Path(project).stem}/project.assets.json"
                path.parent.mkdir(parents=True)
                path.write_text(json.dumps(value))
                self.assets.append(path)
            legacy = self.root / Path(project).parent / "obj/project.assets.json"
            legacy.parent.mkdir(parents=True)
            legacy.write_text(json.dumps(value))

    def test_both_prepared_restore_contexts_are_bound(self):
        actual = publish_contract.package_inputs()
        self.assertEqual({"jit", "aot"}, set(actual["resolvedClosure"]))
        self.assertEqual(4, len(actual["resolvedClosure"]["jit"]))
        self.assertEqual(4, len(actual["resolvedClosure"]["aot"]))

    def test_consumed_assets_change_without_identity_change_is_bound(self):
        before = publish_contract.package_inputs()
        assets = json.loads(self.assets[0].read_text())
        assets["targets"]["fixture"] = {"differentCompileAsset": {}}
        self.assets[0].write_text(json.dumps(assets))
        self.assertNotEqual(before, publish_contract.package_inputs())

    def test_real_package_content_change_is_bound(self):
        before = publish_contract.package_inputs()
        (self.package / "native.a").write_bytes(b"replaced native dependency")
        self.assertNotEqual(before, publish_contract.package_inputs())

    def test_unprepared_runtime_context_is_rejected(self):
        self.assets[0].unlink()
        with self.assertRaises(FileNotFoundError):
            publish_contract.package_inputs()


class ReferencedProjectImportTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="linux-hid-reference-imports-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        patcher = patch.object(publish_contract, "ROOT", self.root)
        patcher.start()
        self.addCleanup(patcher.stop)
        self.host = publish_contract.PROJECT
        self.references = [f"src/{name}/src/Yubico.YubiKit.{name}.csproj" for name in ("Core", "Ctap2", "Management")]
        (self.root / "Directory.Build.props").write_text(
            "<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>")
        (self.root / "Directory.Packages.props").write_text("<Project><PropertyGroup><YubiKitVersion>fixture</YubiKitVersion></PropertyGroup></Project>")
        package = self.root / "packages/fixture/1.0"
        package.mkdir(parents=True)
        (package / "native.a").write_bytes(b"unchanged package content")
        self.generated = []
        for project in [self.host] + self.references:
            path = self.root / project
            path.parent.mkdir(parents=True)
            references = self.references if project == self.host else self.references[:1] if project != self.references[0] else []
            items = "".join(f"<ProjectReference Include='{self.root / reference}'/>" for reference in references)
            path.write_text(f"<Project Sdk='Microsoft.NET.Sdk'><ItemGroup>{items}</ItemGroup></Project>")
            for kind in ("jit", "aot"):
                obj = self.root / f"artifacts/linux-hid-verification/build/{kind}/obj/{path.stem}"
                obj.mkdir(parents=True)
                (obj / "project.assets.json").write_text(json.dumps({
                    "libraries": {"Fixture/1.0": {"type": "package", "path": "fixture/1.0"}},
                    "packageFolders": {str(self.root / "packages"): {}}, "targets": {"fixture": {}}}))
                for suffix in ("props", "targets"):
                    imported = obj / f"{path.name}.nuget.g.{suffix}"
                    imported.write_text("<Project><PropertyGroup><DefineConstants>ORIGINAL</DefineConstants></PropertyGroup></Project>")
                    if project != self.host:
                        self.generated.append(imported)
        self.publish = self.root / "published"
        for kind in ("jit", "aot"):
            folder = self.publish / kind
            folder.mkdir(parents=True)
            binary = folder / "LinuxHidRouteVerification"
            binary.write_bytes(b"synthetic executable identity, never executed")
            binary.chmod(0o700)
        (self.publish / "jit/LinuxHidRouteVerification.dll").write_bytes(b"synthetic assembly identity")

    def inputs(self):
        return {"source": {"sourceHash": "unchanged-fixture-source"},
                "dependencies": publish_contract.package_inputs(), "effectiveBuild": publish_contract.effective_build_inputs()}

    def test_each_referenced_generated_import_change_rejects_seal_and_verification(self):
        before = self.inputs()
        for imported in self.generated:
            with self.subTest(imported=imported.relative_to(self.root)):
                seal(self.publish, before, before)
                original = imported.read_bytes()
                try:
                    # Preserve evaluated properties too: this changes only an imported target's content.
                    imported.write_bytes(original.replace(b"</Project>", b"<!-- changed consumed import --> </Project>"))
                    after = self.inputs()
                    self.assertEqual(before["source"], after["source"])
                    self.assertEqual(before["dependencies"], after["dependencies"])
                    with self.assertRaisesRegex(ValueError, "stale publish source/effective inputs"):
                        verify_publish(self.publish, after)
                    with self.assertRaisesRegex(ValueError, "INPUT effectiveBuild"):
                        seal(self.publish, before, after)
                    self.assertFalse((self.publish / STAMP).exists())
                finally:
                    imported.write_bytes(original)

    def test_referenced_import_timestamp_reordering_does_not_change_binding(self):
        before = publish_contract.effective_build_inputs()
        for index, imported in enumerate(self.generated):
            os.utime(imported, (1000000000 + index, 1000000000 + index))
        self.assertEqual(before, publish_contract.effective_build_inputs())

    def test_host_and_references_use_their_actual_sdk_build_context(self):
        actual = publish_contract.effective_build_inputs()
        for kind, aot in (("jit", "false"), ("aot", "true")):
            self.assertEqual({self.host, *self.references}, set(actual[kind]))
            self.assertEqual("linux-x64", actual[kind][self.host]["Properties"]["RuntimeIdentifier"])
            self.assertEqual("true", actual[kind][self.host]["Properties"]["SelfContained"])
            for project in self.references:
                properties = actual[kind][project]["Properties"]
                self.assertEqual("", properties["RuntimeIdentifier"])
                self.assertEqual("", properties["SelfContained"])
                self.assertEqual("Release", properties["Configuration"])
                self.assertEqual(aot, properties["PublishAot"])
                self.assertEqual(str(self.root / f"artifacts/linux-hid-verification/build/{kind}"), properties["ArtifactsPath"])


class CapturePackageProvenanceTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="linux-hid-capture-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        (self.root / "Directory.Packages.props").write_text(
            "<Project><YubiKitVersion>fixture</YubiKitVersion><PackageVersion Include='Yubico.NativeShims' Version='1.18.1'/></Project>")
        self.publish = self.root / "artifacts/linux-hid-verification"
        for kind in ("jit", "aot"):
            folder = self.publish / kind
            folder.mkdir(parents=True)
            binary = folder / "LinuxHidRouteVerification"
            binary.write_bytes(b"fixture executable identity, never executed")
            binary.chmod(0o700)
        (self.publish / "jit/LinuxHidRouteVerification.dll").write_bytes(b"fixture assembly")
        self.identity = "Yubico.NativeShims/9.8.7-fixture"
        self.package_files = {}
        for name in ("fixture.nupkg", "runtimes/linux-x64/native/libfixture.so",
                     "static/linux-x64/libfixture.a", "runtimes/osx-arm64/native/libfixture.dylib"):
            path = self.root / "non-default-package-cache" / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(f"fixture bound package file: {name}".encode())
            self.package_files[name] = sha(path)
        self.inputs = {"source": {"sourceHash": sha(self.root / "Directory.Packages.props")},
                       "dependencies": {"sdkPackageVersion": "fixture", "resolvedClosure": {
                           kind: {"src/Core/src/Yubico.YubiKit.Core.csproj": [self.identity]} for kind in ("jit", "aot")},
                           "packageFiles": {self.identity: self.package_files}}}
        self.reseal()

    def reseal(self):
        seal(self.publish, self.inputs, self.inputs)

    def run_capture(self):
        def child(args, **kwargs):
            kind = Path(args[0]).parent.name
            mode = args[1]
            record = json.loads((RECORDS / f"{kind}-{mode[2:]}.json").read_text())
            record["SourceHash"] = self.inputs["source"]["sourceHash"]
            Path(args[-1]).write_text(json.dumps(record))
            return capture.subprocess.CompletedProcess(args, 0 if mode == "--native" else 1, "", "")

        with patch.object(sys, "argv", ["capture.py", "--native-only"]), \
             patch.object(capture, "ROOT", self.root), \
             patch.object(capture, "verify_publish", lambda: verify_publish(self.publish, self.inputs)), \
             patch.object(capture, "command", return_value={"fixture": "diagnostic not executed"}), \
             patch.object(capture, "hardware_snapshot", return_value={}), \
             patch.object(capture.subprocess, "run", side_effect=child) as run, \
             patch("builtins.print"):
            try:
                outcome = capture.main()
            except ValueError:
                run.assert_not_called()
                raise
            self.assertEqual(0, outcome)
            self.assertEqual(6, run.call_count)
        records = next((self.publish / "evidence").iterdir())
        return json.loads((records / "provenance.json").read_text())

    def assert_bound_package_provenance(self, actual):
        self.assertEqual([self.identity], actual["resolvedNativeShims"])
        self.assertEqual("1.18.1", actual["nativeShimsPin"])
        expected = {f"packages/{self.identity}/{name}": value for name, value in self.package_files.items()
                    if not name.endswith(".dylib")}
        self.assertEqual(expected, {name: value for name, value in actual["artifacts"].items()
                                    if name.startswith("packages/")})
        self.assertNotIn(str(self.root / "non-default-package-cache"), json.dumps(actual))

    def test_standalone_capture_without_default_core_assets_uses_bound_package(self):
        self.assertFalse((self.root / "src/Core/src/obj/project.assets.json").exists())
        self.assert_bound_package_provenance(self.run_capture())

    def test_stale_default_core_assets_cannot_override_bound_package(self):
        legacy = self.root / "src/Core/src/obj/project.assets.json"
        legacy.parent.mkdir(parents=True)
        legacy.write_text(json.dumps({"libraries": {"Yubico.NativeShims/0.0.0-stale": {}}}))
        self.assert_bound_package_provenance(self.run_capture())

    def test_missing_runtime_package_closure_fails_before_child_execution(self):
        self.inputs["dependencies"]["resolvedClosure"]["jit"]["src/Core/src/Yubico.YubiKit.Core.csproj"] = []
        self.reseal()
        with self.assertRaisesRegex(ValueError, "NativeShims.*jit"):
            self.run_capture()

    def test_missing_bound_package_content_fails_before_child_execution(self):
        self.inputs["dependencies"]["packageFiles"] = {}
        self.reseal()
        with self.assertRaisesRegex(ValueError, "NativeShims.*content"):
            self.run_capture()


if __name__ == "__main__":
    if RECORDS is None:
        raise SystemExit("Usage: test_evidence_gates.py CAPTURE_DIRECTORY")
    unittest.main()
