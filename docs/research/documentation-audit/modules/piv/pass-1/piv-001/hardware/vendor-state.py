#!/usr/bin/env python3
"""Capture public PIV state through the source-pinned installed vendor library."""

import argparse
import dataclasses
import datetime
import hashlib
import importlib
import importlib.metadata
import inspect
import json
import logging
from pathlib import Path
import sys

from cryptography.hazmat.primitives.serialization import Encoding, PublicFormat
from ykman.pcsc import list_devices, ScardSmartCardConnection
from ykman.piv import OBJECT_ID_PIVMAN_DATA, PivmanData
from yubikit.core import NotSupportedError, Tlv
from yubikit.core.smartcard import ApduError, SmartCardConnection
from yubikit.management import ManagementSession
from yubikit.piv import OBJECT_ID, SLOT, PivSession, SlotMetadata, KEY_TYPE, PIN_POLICY, TOUCH_POLICY


class GuardError(RuntimeError):
    """Carry a helper-owned, public-only diagnostic."""


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def provenance():
    manifest_path = Path(__file__).parent / "reference-source/installed-manifest.json"
    manifest = json.loads(manifest_path.read_text())
    actual = {"python": sys.version, "executable": sys.executable,
              "distribution_version": importlib.metadata.version("yubikey-manager"),
              "manifest_sha256": sha256(manifest_path.read_bytes()), "files": {}}
    if actual["distribution_version"] != manifest["version"]:
        raise GuardError("Installed vendor version differs from reviewed source")
    for name, expected in manifest["files"].items():
        path = Path(inspect.getsourcefile(importlib.import_module(name)))
        digest = sha256(path.read_bytes())
        actual["files"][name] = {"path": str(path), "sha256": digest}
        if digest != expected["sha256"]:
            raise GuardError("Installed vendor source differs: " + name)
    actual["reviewed_api_shape"] = manifest["api"]
    actual["helper_sha256"] = sha256(Path(__file__).read_bytes())
    return actual


class PublicTrace(SmartCardConnection):
    """Record headers/status; retain response bytes only for public metadata."""

    def __init__(self, connection, report):
        self.connection = connection
        self.report = report
        self.phase = "management-identity"

    @property
    def transport(self):
        return self.connection.transport

    def close(self):
        pass  # The enclosing real-connection context owns disposal.

    def send_and_receive(self, apdu):
        ins = apdu[1]
        if ins not in (0xA4, 0x1D, 0xC0, 0xFD, 0xF8, 0xF7, 0xCB, 0x00):
            raise GuardError("Read-only APDU guard rejected instruction")
        event = {"phase": self.phase, "header": apdu[:4].hex()}
        self.report["apdu"].append(event)
        try:
            data, sw = self.connection.send_and_receive(apdu)
        except Exception as error:
            event["transport_error_type"] = type(error).__name__
            raise
        event.update(sw=f"0x{sw:04X}", response_length=len(data))
        if self.phase.startswith("piv") and ins in (0xFD, 0xF8, 0xF7):
            event["public_response_hex"] = data.hex()
        return data, sw


def capture(report, label, action, absent=()):
    try:
        return {"state": "present", "value": action()}
    except ApduError as error:
        state = ("absent" if error.sw in absent else "unsupported"
                 if error.sw in (0x6D00, 0x6A81) else "failure")
        result = {"state": state, "sw": f"0x{error.sw:04X}",
                  "error_type": type(error).__name__}
    except NotSupportedError as error:
        result = {"state": "unsupported", "error_type": type(error).__name__}
    except Exception as error:
        # Exception messages can carry object contents; retain the type and operation.
        result = {"state": "failure", "error_type": type(error).__name__}
    if result["state"] == "failure":
        report["failures"].append(label)
    return result


def spki(key):
    encoded = key.public_bytes(Encoding.DER, PublicFormat.SubjectPublicKeyInfo)
    return {"spki_sha256": sha256(encoded), "spki_length": len(encoded)}


def certificate(session, slot):
    cert = session.get_certificate(slot)
    der = cert.public_bytes(Encoding.DER)
    return {"der_sha256": sha256(der), "der_length": len(der), **spki(cert.public_key())}


def object_digest(session, object_id):
    data = session.get_object(object_id)
    return {"length": len(data), "sha256": sha256(data), "empty": len(data) == 0}


def admin_metadata(session):
    data = session.get_object(OBJECT_ID_PIVMAN_DATA)
    admin = PivmanData(data)
    # Keep flags; salt/timestamp values and the protected object stay outside capture.
    return {"length": len(data), "puk_blocked_flag": admin.puk_blocked,
            "management_key_protected_flag": admin.mgm_key_protected,
            "has_derived_key": admin.has_derived_key,
            "has_stored_key": admin.has_stored_key,
            "pin_timestamp_present": admin.pin_timestamp is not None}


def metadata(session, device_version, report, reference, public_read, decode):
    try:
        return decode(public_read())
    except NotSupportedError:
        if tuple(session.version) != (0, 0, 1) or tuple(device_version) < (5, 3, 0):
            raise
        report["metadata_fallbacks"].append({
            "reference": f"0x{reference:02X}", "applet_version": str(session.version),
            "device_version": str(device_version),
            "source": "yubikit.piv.py:1069-1109,1466-1474",
            "operation": "public protocol.send_apdu(0,0xF7,0,reference)",
            "reason": "development sentinel fails installed public API version gate"})
        data = session.protocol.send_apdu(0, 0xF7, 0, reference)
        fields = Tlv.parse_dict(data)
        if reference in (0x80, 0x81):
            return {"default_value": fields[5] != b"\0",
                    "total_attempts": fields[6][0], "attempts_remaining": fields[6][1]}
        if reference == 0x9B:
            algorithm = fields.get(1, b"\x03")[0]
            return {"key_type": algorithm, "default_value": fields[5] != b"\0",
                    "touch_policy": fields[2][1]}
        slot = SlotMetadata(KEY_TYPE(fields[1][0]), PIN_POLICY(fields[2][0]),
                            TOUCH_POLICY(fields[2][1]), fields[3][0] == 1, fields[4])
        return decode(slot)


def slot_metadata(value):
    return {"algorithm_id": int(value.key_type), "algorithm": value.key_type.name,
            "pin_policy_id": int(value.pin_policy), "pin_policy": value.pin_policy.name,
            "touch_policy_id": int(value.touch_policy), "touch_policy": value.touch_policy.name,
            "origin": "generated" if value.generated else "imported-or-other",
            "public_key_encoded_sha256": sha256(value.public_key_encoded),
            **spki(value.public_key)}


def read_state(session, info, report, trace):
    trace.phase = "piv-public-state"
    version_bytes = session.protocol.send_apdu(0, 0xFD, 0, 0)
    report["piv"] = {"public_api_version": str(session.version),
                     "raw_applet_version_hex": version_bytes.hex(),
                     "version_source": "PIV GET VERSION (FD), independent of Management DeviceInfo"}
    if version_bytes != bytes(session.version):
        raise GuardError("PIV version differs from public session version")
    if tuple(info.version) >= (5, 0, 3):
        def piv_serial():
            try:
                return session.get_serial()
            except NotSupportedError:
                if tuple(session.version) != (0, 0, 1):
                    raise
                report["serial_fallback"] = "source-pinned public GET SERIAL F8; development sentinel"
                return int.from_bytes(session.protocol.send_apdu(0, 0xF8, 0, 0), "big")
        report["piv"]["serial"] = capture(report, "piv.serial", piv_serial)
        if report["piv"]["serial"].get("value") != report["requested_serial"]:
            raise GuardError("PIV serial identity guard failed")
    for name, ref, reader in (
            ("pin", 0x80, session.get_pin_metadata),
            ("puk", 0x81, session.get_puk_metadata),
            ("management_key", 0x9B, session.get_management_key_metadata)):
        report[name] = capture(report, name, lambda r=ref, f=reader:
                              metadata(session, info.version, report, r, f, dataclasses.asdict))
    report["bio"] = capture(report, "bio", lambda: dataclasses.asdict(session.get_bio_metadata()))
    report["slots"] = {}
    for slot in SLOT:
        label = f"0x{int(slot):02X}"
        trace.phase = "piv-slot-" + label
        report["slots"][label] = {
            "name": slot.name,
            "metadata": capture(report, label + ".metadata", lambda s=slot:
                metadata(session, info.version, report, int(s),
                         lambda: session.get_slot_metadata(s), slot_metadata), absent=(0x6A88,)),
            "certificate": capture(report, label + ".certificate",
                lambda s=slot: certificate(session, s), absent=(0x6A82,))}
    trace.phase = "piv-public-objects"
    report["objects"] = {}
    for object_id in (OBJECT_ID.CHUID, OBJECT_ID.CAPABILITY, OBJECT_ID.KEY_HISTORY):
        report["objects"][object_id.name] = capture(report, object_id.name,
            lambda oid=object_id: object_digest(session, oid), absent=(0x6A82,))
    report["admin"] = capture(report, "admin", lambda: admin_metadata(session), absent=(0x6A82,))
    report["protected_management_data"] = {"state": "excluded",
        "reason": "Protected-key presence comes from public ADMIN flags; PRINTED is never requested"}
    report["attestation_preservation"] = {
        "slot": "0xF9", "observation": "public key metadata and certificate hashes",
        "private_key_possession": "requires separately coordinated attestation oracle",
        "cleanup_constraint": "retain existing F9 private key, certificate and policy"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("baseline", "final"))
    parser.add_argument("--serial", required=True, type=int)
    parser.add_argument("--reader", required=True,
                        help="Exact PC/SC reader name mapped to the authorized key by the coordinator")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    if args.serial <= 0:
        parser.error("--serial requires a positive device serial")
    # Reserve the evidence path before opening hardware; prior captures stay intact.
    output = args.output.open("x", encoding="utf-8")
    logging.disable(logging.CRITICAL)
    report = {"schema": 1, "mode": args.mode, "requested_serial": args.serial,
              "utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
              "apdu": [], "failures": [], "metadata_fallbacks": []}
    try:
        report["provenance"] = provenance()
        # list_devices enumerates reader descriptors without opening cards. Exact reader
        # selection keeps another attached key outside the capture's connection scope.
        devices = [device for device in list_devices() if device.fingerprint == args.reader]
        if len(devices) != 1:
            raise GuardError("Reader guard requires exactly one matching CCID reader")
        device = devices[0]
        report["selected_reader"] = args.reader
        # This public adapter opens exactly this reader. The convenience device opener
        # can terminate local agents when an open fails; direct construction preserves them.
        with ScardSmartCardConnection(device.reader.createConnection()) as connection:
            trace = PublicTrace(connection, report)
            management = ManagementSession(trace)
            info = management.read_device_info()
            if info.serial != args.serial:
                raise GuardError("Live Management serial guard failed")
            report["device"] = {"serial": info.serial, "firmware": str(info.version),
                "version_name": info.version_name, "management_applet_version": str(management.version),
                "version_qualifier": {"version": str(info.version_qualifier.version),
                    "type": info.version_qualifier.type.name, "iteration": info.version_qualifier.iteration},
                "provenance": "fresh ManagementSession.read_device_info on selected CCID connection",
                "transport": connection.transport.name, "form_factor": info.form_factor.name,
                "fips": info.is_fips, "reset_blocked": int(info.reset_blocked)}
            trace.phase = "piv-initialization"
            session = PivSession(trace)
            read_state(session, info, report, trace)
        report["status"] = "failed" if report["failures"] else "captured"
    except Exception as error:
        report["status"] = "failed"
        report["fatal_error_type"] = type(error).__name__
        if isinstance(error, GuardError):
            report["guard_failure"] = str(error)
        report["failures"].append("capture-or-identity-or-source-guard")
    finally:
        json.dump(report, output, indent=2)
        output.write("\n")
        output.close()
    print(json.dumps({"status": report["status"], "output": str(args.output),
                      "failures": report["failures"]}))
    return 1 if report["failures"] else 0


if __name__ == "__main__":
    sys.exit(main())
