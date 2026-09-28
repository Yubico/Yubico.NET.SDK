"""Behavior checks for the package symbol gate (no device required)."""

import hashlib
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from zipfile import ZipFile

from check_package_exports import check_symbols, shared_symbols, static_symbols, synthetic_symbols


class PackageExportsTests(unittest.TestCase):
    @unittest.skipUnless(sys.platform == "darwin", "Requires a native Mach-O compiler")
    def test_macho_shared_rejects_synthetic_helper_even_with_complete_native_set(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "fixture.c"
            source.write_text(
                "void Native_Fixture(void) {}\n"
                "void Native_HidInputCreate(void) {}\n"
                "void Native_HidInputStart(void) {}\n"
                "void Native_HidInputCancel(void) {}\n"
                "void Native_HidInputWaitShutdown(void) {}\n"
                "void Native_HidInputDestroy(void) {}\n"
                "void hidinput_test_inject(void) {}\n"
                "void create_synthetic(void) {}\n"
            )
            library = root / "fixture.dylib"
            subprocess.run(["cc", "-dynamiclib", str(source), "-o", str(library)], check=True)
            symbols = shared_symbols(library, "osx-x64")
            self.assertEqual(["create_synthetic", "hidinput_test_inject"],
                             check_symbols(symbols, {"Native_Fixture"}, True))

    @unittest.skipUnless(sys.platform == "darwin", "Requires a native Mach-O compiler")
    def test_macho_static_rejects_synthetic_helper_even_with_complete_native_set(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "fixture.c"
            source.write_text(
                "void Native_Fixture(void) {}\n"
                "void hidinput_test_ack(void) {}\n"
                "void create_synthetic(void) {}\n"
            )
            obj = root / "fixture.o"
            library = root / "libfixture.a"
            subprocess.run(["cc", "-c", str(source), "-o", str(obj)], check=True)
            subprocess.run(["ar", "rcs", str(library), str(obj)], check=True)
            symbols = static_symbols(library, "osx-x64")
            self.assertEqual(["create_synthetic", "hidinput_test_ack"],
                             check_symbols(symbols, {"Native_Fixture"}, False))

    def test_synthetic_inventory_is_exactly_owner_header_declarations(self):
        self.assertEqual({"hidinput_test_create", "hidinput_test_create_with_terminal",
                           "hidinput_test_inject", "hidinput_test_ack", "hidinput_test_remove",
                           "hidinput_test_set_close_status", "hidinput_test_close_attempts"},
                         synthetic_symbols() - {"create_synthetic"})
        self.assertIn("create_synthetic", synthetic_symbols())

    def test_legitimate_hidinput_and_openssl_test_symbols_are_not_rejected(self):
        self.assertEqual([], check_symbols({"hidinput_start", "OPENSSL_test_symbol",
                                            "Native_Fixture"}, {"Native_Fixture"}, False))

    def test_missing_canonical_symbol_and_cli_package_identity(self):
        self.assertEqual(["missing: Native_Fixture"], check_symbols(set(), {"Native_Fixture"}, False))
        with tempfile.TemporaryDirectory() as directory:
            package = Path(directory) / "empty.nupkg"
            with ZipFile(package, "w"):
                pass
            result = subprocess.run([sys.executable, str(Path(__file__).with_name("check_package_exports.py")),
                                     str(package)], capture_output=True, text=True)
            self.assertEqual(1, result.returncode)
            self.assertIn(f"SHA-256: {hashlib.sha256(package.read_bytes()).hexdigest()}",
                          result.stdout)


if __name__ == "__main__":
    unittest.main()
