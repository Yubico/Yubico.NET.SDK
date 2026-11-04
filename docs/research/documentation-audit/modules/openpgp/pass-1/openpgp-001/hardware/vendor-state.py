#!/usr/bin/env python3
"""Serial-103 OpenPGP public-state evidence. Coordinator executes this file.

baseline performs SELECT and public reads only. cleanup requires explicit reset
authorization, preserves the baseline file, resets via installed ykman.safe_reset,
and opens independent connections for full readback and factory-PIN verification.
Only public metadata and certificate identities enter JSON; KDF hashes stay private.
"""

import argparse
import dataclasses
import datetime
import enum
import hashlib
import importlib.metadata
import inspect
import json
import logging
from collections.abc import Mapping
from pathlib import Path

from cryptography import x509
from cryptography.hazmat.primitives import serialization
from ykman.device import list_all_devices
from ykman.openpgp import safe_reset
from yubikit.core.smartcard import AID, ApduError, SmartCardConnection, SmartCardProtocol
from yubikit.openpgp import DO, INS, KEY_REF, UIF, KdfIterSaltedS2k, OpenPgpSession

SERIAL = 103


def public(value):
    if isinstance(value, enum.Enum):
        return {"name": value.name, "value": public(value.value)}
    if isinstance(value, bytes):
        return value.hex()
    if dataclasses.is_dataclass(value):
        return {f.name: public(getattr(value, f.name)) for f in dataclasses.fields(value)}
    if isinstance(value, Mapping):
        return {getattr(k, "name", str(k)): public(v) for k, v in value.items()}
    if isinstance(value, (tuple, list)):
        return [public(v) for v in value]
    if value is None or isinstance(value, (str, int, float, bool)):
        return value
    raise TypeError(f"Unmodeled public type: {type(value).__name__}")


def error_record(exc):
    # Exception messages can carry caller input; retain only type and APDU status.
    result = {"status": "error", "exception_type": type(exc).__name__}
    if isinstance(exc, ApduError):
        result["sw"] = f"{exc.sw:04X}"
    return result


def read(call):
    try:
        return {"status": "ok", "value": public(call())}
    except Exception as exc:
        # A failed read remains an explicit error and makes completeness false.
        return error_record(exc)


def connect():
    matches = [(device, info) for device, info in list_all_devices([SmartCardConnection])
               if info.serial == SERIAL]
    if len(matches) != 1:
        raise RuntimeError("Exactly one SmartCard device with serial 103 is required")
    device, info = matches[0]
    return device.open_connection(SmartCardConnection), info


def session_without_activation(connection):
    # The vendor constructor auto-activates a terminated applet. Preflight SELECT
    # guarantees baseline refuses that state before constructing the session.
    SmartCardProtocol(connection).select(AID.OPENPGP)
    session = OpenPgpSession(connection)
    if session.aid.serial != SERIAL:
        raise RuntimeError("OpenPGP AID serial differs from guarded management serial")
    return session


def kdf_metadata(session):
    kdf = session.get_kdf()
    result = {"type": type(kdf).__name__, "algorithm": kdf.algorithm}
    if isinstance(kdf, KdfIterSaltedS2k):
        # Salt bytes are public but unnecessary here. Initial hashes and derived
        # credential bytes are deliberately excluded from all evidence.
        result.update(hash_algorithm=public(kdf.hash_algorithm),
                      iteration_count=kdf.iteration_count,
                      distinct_salts=(len({kdf.salt_user, kdf.salt_reset,
                                           kdf.salt_admin}) == 3),
                      salt_lengths={name: len(getattr(kdf, name) or b"")
                                    for name in ("salt_user", "salt_reset", "salt_admin")})
    return result


def development_version_mismatch(session, info):
    qualifier = info.version_qualifier
    return (session.version < (5, 2, 0) and info.version >= (5, 2, 0)
            and qualifier.type.name in ("ALPHA", "BETA")
            and qualifier.version == info.version)


def public_apdu(session, trace, ins, p1, p2, data=b""):
    command = {"cla": "00", "ins": f"{ins:02X}", "p1": f"{p1:02X}",
               "p2": f"{p2:02X}", "data_hex": data.hex()}
    trace.append(command)
    try:
        response = session.protocol.send_apdu(0, ins, p1, p2, data)
    except Exception as exc:
        command.update(error_record(exc))
        raise
    command.update(status="ok", sw="9000", response_length=len(response))
    return response


def raw_certificate(session, slot, info, trace):
    # Mirror installed get_certificate/_select_certificate's public commands,
    # choosing only SELECT_DATA framing from fresh guarded management metadata.
    # No private session-version override or authentication is involved.
    if slot == KEY_REF.ATT:
        return public_apdu(session, trace, INS.GET_DATA, DO.ATT_CERTIFICATE >> 8,
                           DO.ATT_CERTIFICATE & 0xFF)
    data = bytes.fromhex("60045c027f21")
    if info.version <= (5, 4, 3):
        data = b"\x06" + data
    public_apdu(session, trace, INS.SELECT_DATA, 3 - slot, 0x04, data)
    return public_apdu(session, trace, INS.GET_DATA, DO.CARDHOLDER_CERTIFICATE >> 8,
                       DO.CARDHOLDER_CERTIFICATE & 0xFF)


def certificate_identity(session, slot, info):
    raw = development_version_mismatch(session, info)
    provenance = {"read_path": "explicit-public-apdu" if raw else "vendor-get_certificate",
                  "direct_session_version": str(session.version)}
    if raw:
        provenance.update(framing_version_source="fresh-serial-103-management-metadata",
                          framing_version=info.version_name, commands=[])
    try:
        if raw:
            der = raw_certificate(session, slot, info, provenance["commands"])
            if not der:
                return dict(provenance, status="absent", reason="successful GET DATA returned zero certificate octets")
            cert = x509.load_der_x509_certificate(der)
        else:
            cert = session.get_certificate(slot)
    except ValueError as exc:
        if not raw and str(exc) == "No certificate found!":
            return dict(provenance, status="absent", reason="vendor returned empty certificate data")
        return dict(provenance, **error_record(exc))
    except Exception as exc:
        return dict(provenance, **error_record(exc))
    der = cert.public_bytes(serialization.Encoding.DER)
    spki = cert.public_key().public_bytes(serialization.Encoding.DER,
                                         serialization.PublicFormat.SubjectPublicKeyInfo)
    return dict(provenance, status="ok", der_sha256=hashlib.sha256(der).hexdigest(),
                spki_sha256=hashlib.sha256(spki).hexdigest(),
                length=len(der), serial_number_hex=f"{cert.serial_number:X}")


def read_uif(session, slot, info):
    if not development_version_mismatch(session, info):
        return dict(read(lambda: session.get_uif(slot)), read_path="vendor-get_uif")
    # The same low reported version makes vendor get_uif synthesize OFF without
    # I/O. Use its installed parser on a real public DO read so cleanup never
    # mistakes that fallback for an observed card policy.
    trace = []
    result = read(lambda: UIF.parse(public_apdu(session, trace, INS.GET_DATA,
                                               slot.uif_do >> 8, slot.uif_do & 0xFF)))
    return dict(result, read_path="explicit-public-apdu-installed-UIF-parser", commands=trace)


def snapshot():
    connection, info = connect()
    with connection:
        session = session_without_activation(connection)
        state = {"device": public(info), "device_version_name": info.version_name,
                 "aid": session.aid.hex(), "openpgp_card_version": public(session.aid.version),
                 "application_version": str(session.version)}
        readers = {
            "pin_status": session.get_pin_status,
            "kdf": lambda: kdf_metadata(session),
            "extended_capabilities": lambda: session.extended_capabilities,
            "key_information": session.get_key_information,
            "supported_algorithms": session.get_algorithm_information,
            "signature_counter": session.get_signature_counter,
            "fingerprints": session.get_fingerprints,
            "generation_times": session.get_generation_times,
            "ca_fingerprints": lambda: session.get_application_related_data().discretionary.ca_fingerprints,
        }
        state.update({name: read(call) for name, call in readers.items()})
        state["algorithm_attributes"] = {slot.name: read(lambda slot=slot: session.get_algorithm_attributes(slot))
                                         for slot in KEY_REF}
        state["uif"] = {slot.name: read_uif(session, slot, info) for slot in KEY_REF}
        state["certificates"] = {slot.name: certificate_identity(session, slot, info) for slot in KEY_REF}
        state["version_interpretation"] = {
            "direct_session_version_preserved": str(session.version),
            "fresh_management_version": info.version_name,
            "development_mismatch_certificate_path": development_version_mismatch(session, info),
            "private_version_override": False,
            "uif_vendor_version_fallback_bypassed": development_version_mismatch(session, info),
        }
        state["complete"] = not has_error(state)
        return state


def has_error(value):
    if isinstance(value, dict):
        return value.get("status") == "error" or any(has_error(v) for v in value.values())
    if isinstance(value, list):
        return any(has_error(v) for v in value)
    return False


def verify_factory_pins():
    # These are published fixed factory TEST credentials, never user input. Python
    # strings are immutable: the helper makes no secure-erasure claim for them.
    connection, _ = connect()
    with connection:
        session = session_without_activation(connection)
        return {
            "user_sign": read(lambda: session.verify_pin("123456")),
            "user_extended": read(lambda: session.verify_pin("123456", extended=True)),
            "admin": read(lambda: session.verify_admin("12345678")),
            "pin_status_after": read(session.get_pin_status),
        }


def reset_checks(state, baseline):
    checks = {}
    checks["device_configuration_preserved"] = state["device"]["config"] == baseline["state"]["device"]["config"]
    checks["supported_algorithms_preserved"] = state["supported_algorithms"] == baseline["state"]["supported_algorithms"]
    pins = state.get("pin_status", {}).get("value", {})
    checks["pin_attempts_3_0_3"] = [pins.get(f"attempts_{p}") for p in ("user", "reset", "admin")] == [3, 0, 3]
    checks["signature_pin_policy_once"] = pins.get("pin_policy_user", {}).get("name") == "ONCE"
    checks["kdf_disabled"] = state.get("kdf", {}).get("value", {}).get("algorithm") == 0
    checks["signature_counter_zero"] = state.get("signature_counter", {}).get("value") == 0
    for slot in ("SIG", "DEC", "AUT"):
        checks[f"{slot}_certificate_absent"] = state["certificates"][slot]["status"] == "absent"
        checks[f"{slot}_uif_off"] = state["uif"][slot].get("value", {}).get("name") == "OFF"
        checks[f"{slot}_key_absent"] = state["key_information"].get("value", {}).get(slot, {}).get("name") == "NONE"
        checks[f"{slot}_fingerprint_zero"] = state["fingerprints"].get("value", {}).get(slot) == "00" * 20
        checks[f"{slot}_generation_time_zero"] = state["generation_times"].get("value", {}).get(slot) == 0
        checks[f"{slot}_ca_fingerprint_zero"] = state["ca_fingerprints"].get("value", {}).get(slot) == "00" * 20
    old = baseline["state"]["certificates"]["ATT"]
    new = state["certificates"]["ATT"]
    if old.get("status") == "absent":
        checks["attestation_certificate_absence_preserved"] = new.get("status") == "absent"
    else:
        checks["attestation_certificate_identity_preserved"] = (
            old.get("status") == new.get("status") == "ok"
            and old.get("der_sha256") == new.get("der_sha256")
            and old.get("spki_sha256") == new.get("spki_sha256"))
    return checks


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("baseline", "cleanup"))
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--baseline", type=Path)
    parser.add_argument("--authorize-reset-serial-103", action="store_true")
    args = parser.parse_args()
    if args.output.exists():
        parser.error("Evidence output already exists; choose a fresh path")
    baseline = None
    if args.mode == "cleanup":
        if not args.authorize_reset_serial_103 or args.baseline is None:
            parser.error("cleanup requires --authorize-reset-serial-103 and --baseline")
        baseline = json.loads(args.baseline.read_text())
        if (baseline.get("mode") != "baseline" or baseline.get("serial_guard") != SERIAL
                or not baseline.get("success")):
            parser.error("Expected successful original serial-103 baseline JSON")
    logging.disable(logging.CRITICAL)
    result = {"mode": args.mode, "serial_guard": SERIAL, "helper_revision": 2,
              "helper_source_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
              "utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
              "vendor_version": importlib.metadata.version("yubikey-manager"),
              "vendor_sources": {name: {"path": inspect.getfile(obj),
                                         "sha256": hashlib.sha256(Path(inspect.getfile(obj)).read_bytes()).hexdigest()}
                                 for name, obj in (("OpenPgpSession", OpenPgpSession), ("safe_reset", safe_reset))}}
    try:
        if args.mode == "cleanup":
            connection, _ = connect()
            with connection:
                safe_reset(connection)
            result["safe_reset"] = {"status": "ok"}
        result["state"] = snapshot()
        if baseline is not None:
            result["baseline_file_sha256"] = hashlib.sha256(args.baseline.read_bytes()).hexdigest()
            result["reset_checks"] = reset_checks(result["state"], baseline)
            result["factory_pin_verification"] = verify_factory_pins()
            # A second full read is independent of the authentication connection.
            result["post_verification_state"] = snapshot()
            result["reset_exception_observations"] = {
                name: {"baseline": baseline["state"][name], "final": result["state"][name],
                       "unchanged": baseline["state"][name] == result["state"][name]}
                for name in ("algorithm_attributes", "uif", "key_information", "fingerprints", "generation_times", "certificates")}
        result["success"] = (not has_error(result) and result["state"]["complete"]
                             and all(result.get("reset_checks", {}).values())
                             and result.get("post_verification_state", {"complete": True})["complete"])
    except Exception as exc:
        result["fatal"] = error_record(exc)
        result["success"] = False
    with args.output.open("x") as output:
        json.dump(result, output, indent=2, sort_keys=True)
        output.write("\n")
    print(json.dumps({"output": str(args.output), "success": result["success"]}))
    return 0 if result["success"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
