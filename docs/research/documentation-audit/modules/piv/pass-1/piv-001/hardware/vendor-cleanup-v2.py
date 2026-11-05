#!/usr/bin/env python3
"""Restore the captured serial-103 PIV baseline through a bounded vendor path."""

import argparse
from contextlib import contextmanager
import datetime
import hashlib
import importlib.util
import json
import logging
from pathlib import Path
import signal
import subprocess
import sys
import time

from ykman.pcsc import list_devices, ScardSmartCardConnection
from yubikit.core import InvalidPinError, Tlv
from yubikit.core.smartcard import ApduError, SmartCardConnection
from yubikit.management import ManagementSession
from yubikit.piv import DEFAULT_MANAGEMENT_KEY, MANAGEMENT_KEY_TYPE, PivSession


BASELINE_HASH = "3f160854e948cb01f3e535efe0e59cdf5df5eb67b94cdeb7658b0de9d1223718"
STATE_HELPER_HASH = "41aa7200cd912a5f7a1438ad693fff83ad36b32873540b8a70d1b578f976600a"
MANIFEST_HASH = "ebd4aa11daec37265b8c2b027d31bdfdd871d850b7b7b637a4a5ffb07fa96f5d"
READER = "Yubico YubiKey OTP+FIDO+CCID 00 00"
HERE = Path(__file__).resolve().parent


class GuardError(RuntimeError):
    """Expose helper-owned public diagnostics only."""


def require(condition, reason):
    if not condition:
        raise GuardError(reason)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def differences(expected, actual, path=""):
    if isinstance(expected, dict) and isinstance(actual, dict):
        result = []
        for key in sorted(expected.keys() | actual.keys()):
            child = path + "/" + key
            if key not in expected or key not in actual:
                result.append({"path": child, "baseline": expected.get(key), "actual": actual.get(key)})
            else:
                result.extend(differences(expected[key], actual[key], child))
        return result
    if expected != actual:
        return [{"path": path, "baseline": expected, "actual": actual}]
    return []


def stable(state):
    # Every captured field remains significant except observation time and capture label.
    return {key: value for key, value in state.items() if key not in ("utc", "mode")}


def load_vendor_review():
    helper = HERE / "vendor-state.py"
    require(digest(helper) == STATE_HELPER_HASH, "Frozen public-state helper hash mismatch")
    require(digest(HERE / "reference-source/installed-manifest.json") == MANIFEST_HASH,
            "Frozen installed-source manifest hash mismatch")
    spec = importlib.util.spec_from_file_location("piv_frozen_public_state", helper)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.provenance()


def validate_baseline(path, args):
    require(digest(path) == BASELINE_HASH, "Require the exact successful baseline-103.json")
    state = json.loads(path.read_text())
    require(state["status"] == "captured" and state["failures"] == [], "Baseline contains failures")
    require(args.serial == args.authorize_reset_serial == state["requested_serial"] == 103,
            "Both explicit serial arguments must authorize captured serial 103")
    require(args.reader == state["selected_reader"] == READER, "Exact captured reader guard failed")
    require(state["pin"]["value"] == state["puk"]["value"] == {
        "default_value": True, "total_attempts": 3, "attempts_remaining": 3}, "Baseline retry/default guard")
    require(state["management_key"]["value"] == {
        "key_type": 10, "default_value": True, "touch_policy": 1}, "Baseline AES192/default guard")
    require(state["provenance"]["helper_sha256"] == STATE_HELPER_HASH, "Baseline helper provenance guard")
    return state


class Receipt:
    def __init__(self, path):
        self.path = path
        self.output = path.open("x", encoding="utf-8")
        self.journal = None
        self.data = {"schema": 1, "status": "in-progress", "reset_completed": False,
                     "utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                     "cleanup_helper_sha256": digest(Path(__file__)), "events": []}

    def start_journal(self):
        self.journal = self.path.with_suffix(".events.jsonl").open("x", encoding="utf-8")

    def event(self, operation, **public):
        event = {"operation": operation, **public}
        self.data["events"].append(event)
        if self.journal:
            self.journal.write(json.dumps(event) + "\n")
            self.journal.flush()

    def close(self):
        json.dump(self.data, self.output, indent=2)
        self.output.write("\n")
        self.output.close()
        if self.journal:
            self.journal.close()


class HeaderTrace(SmartCardConnection):
    def __init__(self, connection, receipt):
        self.connection = connection
        self.receipt = receipt
        self.phase = "identity"

    @property
    def transport(self):
        return self.connection.transport

    def close(self):
        pass  # The real connection's context owns disposal.

    def send_and_receive(self, apdu):
        ins, p1, p2 = apdu[1:4]
        reads = ins in (0xA4, 0x1D, 0xC0, 0xFD, 0xF8, 0xF7, 0x00)
        public_certificate = (self.phase == "attestation-public-certificate" and
                              (ins, p1, p2) == (0xCB, 0x3F, 0xFF) and
                              apdu.endswith(bytes.fromhex("5c035fff01")))
        mutation = ((self.phase == "block-pin" and (ins, p1, p2) == (0x20, 0, 0x80)) or
                    (self.phase == "block-puk" and (ins, p1, p2) == (0x2C, 0, 0x80)) or
                    (self.phase == "reset" and (ins, p1, p2) == (0xFB, 0, 0)))
        auth = ((self.phase == "factory-pin" and (ins, p1, p2) == (0x20, 0, 0x80)) or
                (self.phase == "factory-management" and ins == 0x87 and p1 == 10 and p2 == 0x9B))
        require(reads or public_certificate or mutation or auth, "Cleanup APDU scope guard rejected instruction")
        header = apdu[:4].hex()
        self.receipt.event("apdu-start", phase=self.phase, header=header)
        data, sw = self.connection.send_and_receive(apdu)
        self.receipt.event("apdu-finish", phase=self.phase, header=header, sw=f"0x{sw:04X}")
        return data, sw  # Payloads, hashes and lengths of authentication material stay unlogged.


def raw_metadata(session, reference):
    # Device/version/source guards have already qualified this exact development key.
    return Tlv.parse_dict(session.protocol.send_apdu(0, 0xF7, 0, reference))


@contextmanager
def live_session(args, baseline, receipt, authenticate=False):
    devices = [d for d in list_devices() if d.fingerprint == args.reader]
    require(len(devices) == 1, "Require one exact selected reader")
    with ScardSmartCardConnection(devices[0].reader.createConnection()) as connection:
        trace = HeaderTrace(connection, receipt)
        management = ManagementSession(trace)
        info = management.read_device_info()
        expected = baseline["device"]
        require(info.serial == args.serial == 103, "Live Management serial mismatch")
        require(str(info.version) == expected["firmware"] and info.version_name == expected["version_name"]
                and str(management.version) == expected["management_applet_version"], "Live firmware mismatch")
        require(info.form_factor.name == expected["form_factor"] and not info.is_fips
                and int(info.reset_blocked) == 0 and connection.transport.name == "USB",
                "Device scope/reset capability mismatch")
        session = MetadataTypedSession(trace) if authenticate else PivSession(trace)
        require(tuple(session.version) == (0, 0, 1), "Require captured PIV development sentinel")
        require(session.protocol.send_apdu(0, 0xFD, 0, 0) == b"\0\0\1", "Raw applet version mismatch")
        serial = int.from_bytes(session.protocol.send_apdu(0, 0xF8, 0, 0), "big")
        require(serial == info.serial, "Live PIV serial mismatch")
        receipt.event("live-identity-verified", serial=serial, firmware=info.version_name,
                      applet_version=str(session.version))
        yield session, trace
    receipt.event("connection-closed")


class MetadataTypedSession(PivSession):
    @property
    def management_key_type(self):
        # Installed authenticate() reads this public property. Actual metadata supplies
        # the algorithm while the public applet version and private caches stay untouched.
        fields = raw_metadata(self, 0x9B)
        require(fields[1] == b"\x0a" and fields[5] == b"\x01" and fields[2] == b"\0\1",
                "Post-reset management metadata differs from captured AES192 defaults")
        return MANAGEMENT_KEY_TYPE(fields[1][0])


def retry_count(session, reference):
    fields = raw_metadata(session, reference)
    retries = fields[6]
    require(len(retries) == 2 and 0 <= retries[1] <= retries[0] <= 255,
            "Malformed or unbounded retry metadata")
    return retries[1]


def block_reference(session, trace, receipt, reference):
    phase = "block-pin" if reference == 0x80 else "block-puk"
    trace.phase = phase
    initial = retry_count(session, reference)
    remaining = initial
    for attempt in range(initial):
        receipt.event("blocking-attempt", reference=f"0x{reference:02X}",
                      attempt=attempt + 1, remaining_before=remaining, maximum_attempts=initial)
        try:
            if reference == 0x80:
                session.verify_pin("")
            else:
                session.unblock_pin("", "")
        except InvalidPinError:
            pass
        else:
            raise GuardError("Invalid-reference blocking attempt unexpectedly succeeded")
        observed = retry_count(session, reference)
        require(observed == remaining - 1, "Blocking retry count failed to decrease by exactly one")
        remaining = observed
    require(remaining == 0, "Reference remains unblocked after bounded attempts")
    receipt.event("reference-blocked", reference=f"0x{reference:02X}", attempts=initial)


def reset_piv(args, baseline, receipt):
    with live_session(args, baseline, receipt) as (session, trace):
        try:
            raw_metadata(session, 0x96)
        except ApduError as error:
            require(error.sw == 0x6A88, "Unexpected biometric metadata status")
        else:
            raise GuardError("Biometric metadata present: dedicated non-Bio cleanup scope changed")
        # Read F9 public metadata and certificate immediately before retry mutation.
        spec = importlib.util.spec_from_file_location("piv_state_public_checks", HERE / "vendor-state.py")
        helper = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(helper)
        from yubikit.piv import SLOT, SlotMetadata, KEY_TYPE, PIN_POLICY, TOUCH_POLICY
        fields = raw_metadata(session, 0xF9)
        # The baseline's decoded value used the complete chained response. Its F7
        # transport trace retained only the first fragment, so compare captured fields.
        live_f9 = SlotMetadata(KEY_TYPE(fields[1][0]), PIN_POLICY(fields[2][0]),
                              TOUCH_POLICY(fields[2][1]), fields[3][0] == 1, fields[4])
        require(helper.slot_metadata(live_f9) == baseline["slots"]["0xF9"]["metadata"]["value"],
                "Captured live F9 public metadata changed")
        require(fields[1] == b"\x07" and fields[2] == b"\x02\x01" and fields[3] == b"\x02",
                "Live F9 algorithm/policy/origin changed")
        require(helper.sha256(fields[4]) == baseline["slots"]["0xF9"]["metadata"]["value"]["public_key_encoded_sha256"],
                "Live F9 public key changed")
        # Certificate read uses a separate public trace permission within this phase.
        trace.phase = "attestation-public-certificate"
        require(helper.certificate(session, SLOT.ATTESTATION) == baseline["slots"]["0xF9"]["certificate"]["value"],
                "Live F9 certificate changed")
        receipt.event("attestation-public-identity-preserved-before-reset")
        block_reference(session, trace, receipt, 0x80)
        block_reference(session, trace, receipt, 0x81)
        trace.phase = "reset"
        receipt.event("reset-issued", serial=args.serial)
        session.protocol.send_apdu(0, 0xFB, 0, 0)
        receipt.data["reset_completed"] = True
        receipt.event("reset-completed", serial=args.serial)


def snapshot(args, baseline, receipt, suffix, deadline):
    output = args.receipt.with_suffix("." + suffix + ".json")
    command = [sys.executable, str(HERE / "vendor-state.py"), "final", "--serial", str(args.serial),
               "--reader", args.reader, "--output", str(output)]
    receipt.event("public-capture-start", label=suffix, command=command)
    result = subprocess.run(command, capture_output=True, timeout=max(1, min(45, deadline - time.monotonic())))
    receipt.event("public-capture-exit", label=suffix, returncode=result.returncode)
    require(result.returncode == 0, "Public capture failed: " + suffix)
    state = json.loads(output.read_text())
    require(state["status"] == "captured" and state["failures"] == [], "Public capture contains failures")
    require(state["requested_serial"] == 103 and state["selected_reader"] == args.reader,
            "Public capture identity mismatch")
    require(state["provenance"] == baseline["provenance"], "Public capture source/runtime identity changed")
    delta = differences(stable(baseline), stable(state))
    receipt.data.setdefault("readbacks", []).append({"label": suffix, "path": str(output),
        "sha256": digest(output), "baseline_differences": delta})
    for key in ("device", "piv", "slots"):
        expected = baseline[key]["0xF9"] if key == "slots" else baseline[key]
        actual = state[key]["0xF9"] if key == "slots" else state[key]
        require(actual == expected, "Public identity/F9 mismatch: " + suffix + "/" + key)
    if suffix != "before":
        require(delta == [], "Public state differs from baseline: " + suffix)
    receipt.event("public-capture-verified", label=suffix, serial=103, differences=len(delta))
    return state


def factory_auth(args, baseline, receipt):
    require(receipt.data["reset_completed"], "Factory authentication requires successful authorized reset")
    with live_session(args, baseline, receipt, authenticate=True) as (session, trace):
        for reference in (0x80, 0x81):
            fields = raw_metadata(session, reference)
            require(fields[5] == b"\x01" and fields[6] == b"\x03\x03", "Post-reset PIN/PUK defaults mismatch")
        algorithm = session.management_key_type
        require(algorithm == MANAGEMENT_KEY_TYPE.AES192 and len(DEFAULT_MANAGEMENT_KEY) == algorithm.key_len == 24,
                "Actual algorithm and vendor public factory key width mismatch")
        trace.phase = "factory-pin"
        session.verify_pin("123456")
        receipt.event("factory-pin-verified", serial=103)
        trace.phase = "factory-management"
        session.authenticate(DEFAULT_MANAGEMENT_KEY)
        receipt.event("factory-management-authenticated", serial=103, algorithm=algorithm.name,
                      algorithm_id=int(algorithm), credential_source="installed public DEFAULT_MANAGEMENT_KEY")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--serial", type=int, required=True)
    parser.add_argument("--authorize-reset-serial", type=int, required=True)
    parser.add_argument("--reader", required=True)
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    logging.disable(logging.CRITICAL)
    receipt = Receipt(args.receipt)
    def interrupted(signum, frame):
        raise GuardError("Cleanup interrupted or exceeded 180-second process deadline")
    signal.signal(signal.SIGALRM, interrupted)
    signal.signal(signal.SIGTERM, interrupted)
    deadline = time.monotonic() + 180
    signal.alarm(180)
    try:
        receipt.start_journal()
        receipt.data["authorization"] = {"serial": args.serial, "authorize_reset_serial": args.authorize_reset_serial,
                                         "reader": args.reader}
        receipt.data["command"] = [sys.executable, str(Path(__file__).resolve()), *sys.argv[1:]]
        baseline = validate_baseline(args.baseline, args)
        receipt.data["baseline"] = {"path": str(args.baseline), "sha256": digest(args.baseline)}
        receipt.data["vendor_provenance"] = load_vendor_review()
        require(receipt.data["vendor_provenance"] == baseline["provenance"], "Current vendor/runtime provenance mismatch")
        for suffix in ("before", "after-reset", "after-auth"):
            require(not args.receipt.with_suffix("." + suffix + ".json").exists(), "Evidence path already exists")
        snapshot(args, baseline, receipt, "before", deadline)
        reset_piv(args, baseline, receipt)
        snapshot(args, baseline, receipt, "after-reset", deadline)
        factory_auth(args, baseline, receipt)
        snapshot(args, baseline, receipt, "after-auth", deadline)
        receipt.data["status"] = "restored-and-factory-authenticated"
        receipt.data["comparison_exceptions"] = ["utc", "mode"]
    except (Exception, KeyboardInterrupt) as error:
        receipt.data["status"] = "failed"
        receipt.data["error_type"] = type(error).__name__
        if isinstance(error, GuardError):
            receipt.data["guard_failure"] = str(error)
        receipt.event("cleanup-stopped", reset_completed=receipt.data["reset_completed"])
    finally:
        signal.alarm(0)
        receipt.close()
    print(json.dumps({"status": receipt.data["status"], "receipt": str(args.receipt),
                      "reset_completed": receipt.data["reset_completed"]}))
    return 0 if receipt.data["status"] == "restored-and-factory-authenticated" else 1


if __name__ == "__main__":
    sys.exit(main())
