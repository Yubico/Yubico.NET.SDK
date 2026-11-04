#!/usr/bin/env python3
"""Coordinator-only references: same-key X25519 Decipher or read-only Att public-key CRT.

Neither mode resets, imports, generates keys or changes configuration. x25519 verifies
the published factory User TEST PIN in extended mode, then uses the existing Dec key.
Run it after the SDK case and before independent cleanup, even when that case fails.
"""

import argparse
import datetime
import hashlib
import hmac
import importlib.metadata
import importlib.util
import inspect
import json
import logging
from pathlib import Path

from cryptography import x509
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric import rsa, x25519
from yubikit.core import Tlv
from yubikit.openpgp import CRT, DO, INS, KEY_REF, OID, EcAttributes, KdfNone, OpenPgpSession, UIF

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
HELPER = RUN / 'hardware/vendor-state.py'
spec = importlib.util.spec_from_file_location('audit_openpgp_vendor_state', HELPER)
if spec is None or spec.loader is None:
    raise SystemExit('Cannot load the guarded vendor-state helper')
state_helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(state_helper)


def require(condition, label):
    if not condition:
        raise RuntimeError(label)


def spki(key):
    return key.public_bytes(serialization.Encoding.DER,
                            serialization.PublicFormat.SubjectPublicKeyInfo)


def att_reference(session, baseline, result):
    result['phase'] = 'att-certificate-read'
    der = state_helper.public_apdu(session, result['public_commands'], INS.GET_DATA,
                                   DO.ATT_CERTIFICATE >> 8, DO.ATT_CERTIFICATE & 0xFF)
    require(bool(der), 'Att certificate must be present for this identity-bound reference')
    certificate = x509.load_der_x509_certificate(der)
    certified = certificate.public_key()
    require(isinstance(certified, rsa.RSAPublicKey), 'This bounded reference expects the observed RSA Att issuer')
    recorded = baseline['state']['certificates']['ATT']
    certificate_hash = hashlib.sha256(der).hexdigest()
    certificate_spki_hash = hashlib.sha256(spki(certified)).hexdigest()
    require(recorded.get('status') == 'ok', 'Baseline Att identity must be readable')
    require(certificate_hash == recorded['der_sha256'], 'Current Att certificate differs from baseline')
    require(certificate_spki_hash == recorded['spki_sha256'], 'Current issuer SPKI differs from baseline')
    result['att_certificate'] = dict(status='ok', baseline_identity_match=True,
                                     der_sha256=certificate_hash, spki_sha256=certificate_spki_hash)
    result['phase'] = 'correct-att-public-key-crt-read'
    # Installed CRT.ATT and vendor Attestation extension specify B6 03 84 01 81.
    # P1=81 reads an existing public key. It neither generates nor exports private material.
    crt = bytes(CRT.ATT)
    require(crt == bytes.fromhex('B603840181'), 'Installed Att CRT differs from the pinned vendor profile')
    encoded = state_helper.public_apdu(session, result['public_commands'], INS.GENERATE_ASYM,
                                       0x81, 0x00, crt)
    fields = Tlv.parse_dict(Tlv.unpack(0x7F49, encoded))
    reported = rsa.RSAPublicNumbers(int.from_bytes(fields[0x82], 'big'),
                                    int.from_bytes(fields[0x81], 'big')).public_key()
    require(spki(reported) == spki(certified), 'Correct-CRT Att public key differs from certificate SPKI')
    result['att_public_key'] = dict(status='ok', structure='7F49/RSA-81-modulus-82-exponent',
                                    correct_crt='B603840181', certificate_spki_match=True,
                                    spki_sha256=hashlib.sha256(spki(reported)).hexdigest())


def x25519_reference(session, result):
    # RFC 7748 section 6.1 published fixture: Python acquires no private scalar.
    alice = bytes.fromhex('8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a')
    bob = bytes.fromhex('de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f')
    expected = bytearray.fromhex('4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742')
    shared = bytearray()
    try:
        result['phase'] = 'same-key-preconditions'
        require(isinstance(session.get_kdf(), KdfNone), 'Expected KDF disabled before factory-User verification')
        attributes = session.get_algorithm_attributes(KEY_REF.DEC)
        require(isinstance(attributes, EcAttributes) and attributes.algorithm_id == 0x12
                and bytes(attributes.oid) == bytes(OID.X25519), 'Current Dec key must use X25519 ECDH attributes')
        policy = UIF.parse(session.get_data(KEY_REF.DEC.uif_do))
        require(policy == UIF.OFF, 'This unattended reference requires observed Dec UIF Off')
        public = session.get_public_key(KEY_REF.DEC)
        require(isinstance(public, x25519.X25519PublicKey), 'Current Dec public key must parse as X25519')
        encoded = public.public_bytes(serialization.Encoding.Raw, serialization.PublicFormat.Raw)
        require(encoded == alice, 'Current Dec public key differs from imported RFC Alice fixture; stop before Decipher')
        result['preconditions'] = dict(status='ok', fixture='RFC7748-section-6.1-Alice-Bob',
                                       alice_public_point_match=True, kdf='disabled', dec_uif='OFF')
        result['phase'] = 'factory-user-extended-verification'
        before = session.get_pin_status()
        result['pin_retries_before'] = dict(user=before.attempts_user, reset=before.attempts_reset,
                                            admin=before.attempts_admin)
        # Published fixed factory TEST credential; no caller secret is acquired/logged.
        session.verify_pin('123456', extended=True)
        result['user_extended_verification'] = dict(status='ok', credential_source='published-fixed-factory-test-credential')
        result['phase'] = 'installed-vendor-decrypt'
        result['decipher_profile'] = dict(api='OpenPgpSession.decrypt', header='00 2A 80 86',
            data_template='A6{7F49{86 peer-public-point}}', public_point_octets=32, command_data_octets=39,
            source='installed method source corroborates Card 3.4.1 section 7.2.11 and pinned Python profile')
        # Installed decrypt has no application-version gate and constructs the A6
        # wrapper itself. Retain its exact API rather than emulating SDK formatting.
        immutable_response = session.decrypt(bob)
        try:
            shared = bytearray(immutable_response)
        finally:
            del immutable_response
        matched = hmac.compare_digest(shared, expected)
        result['decipher'] = dict(status='ok', vendor_call_completed=True, rfc_shared_secret_match=matched)
        require(matched, 'Vendor Decipher must match the RFC shared secret')
    finally:
        shared[:] = b'\0' * len(shared)
        expected[:] = b'\0' * len(expected)
        result['owned_mutable_secret_buffers_cleared'] = True
        result['python_memory_qualification'] = 'Vendor immutable response bytes cannot be securely erased by this Python caller; no byte values or derived-output hashes are logged.'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('x25519', 'att'))
    parser.add_argument('--baseline', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        parser.error('Choose a fresh output path; existing evidence is preserved')
    baseline_bytes = args.baseline.read_bytes()
    baseline = json.loads(baseline_bytes)
    if baseline.get('mode') != 'baseline' or baseline.get('serial_guard') != 103 or not baseline.get('success'):
        parser.error('A successful serial-103 baseline is required')
    logging.disable(logging.CRITICAL)
    result = dict(mode=args.mode, serial_guard=103, utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                  baseline_sha256=hashlib.sha256(baseline_bytes).hexdigest(),
                  helper_source_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                  guarded_state_helper_sha256=hashlib.sha256(HELPER.read_bytes()).hexdigest(),
                  vendor_version=importlib.metadata.version('yubikey-manager'),
                  installed_openpgp_source_sha256=hashlib.sha256(Path(inspect.getfile(OpenPgpSession)).read_bytes()).hexdigest(),
                  installed_decrypt_source_sha256=hashlib.sha256(inspect.getsource(OpenPgpSession.decrypt).encode()).hexdigest(),
                  reset=False, configuration_write=False, key_import_or_generation=False,
                  public_commands=[], phase='guarded-open')
    try:
        connection, info = state_helper.connect()
        with connection:
            session = state_helper.session_without_activation(connection)
            result.update(device_version_name=info.version_name, direct_application_version=str(session.version))
            if args.mode == 'x25519':
                x25519_reference(session, result)
            else:
                att_reference(session, baseline, result)
            result.update(success=True, phase='complete')
    except Exception as exc:
        result.update(success=False, failure=state_helper.error_record(exc))
    with args.output.open('x') as output:
        json.dump(result, output, indent=2, sort_keys=True)
        output.write('\n')
    print(json.dumps(dict(output=str(args.output), success=result['success'], phase=result['phase'])))
    return 0 if result['success'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
