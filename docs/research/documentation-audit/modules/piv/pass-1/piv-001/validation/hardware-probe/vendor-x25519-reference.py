"""Coordinator-only same-key vendor agreement control for the P01 public RFC fixture."""
import argparse
import hashlib
import importlib.util
import json
import logging
from pathlib import Path
import sys

from cryptography.hazmat.primitives.asymmetric.x25519 import X25519PublicKey
from ykman.pcsc import list_devices, ScardSmartCardConnection
from yubikit.core import Tlv
from yubikit.management import ManagementSession
from yubikit.piv import PivSession, SLOT

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--serial', type=int, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
logging.disable(logging.CRITICAL)
report = dict(status='failed', requested_serial=args.serial)
report['helper_sha256'] = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
expected = bytearray.fromhex('4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742')
actual = bytearray()
with args.output.open('x') as output:
    try:
        if args.serial != 103:
            raise RuntimeError('Serial scope')
        helper_path = RUN / 'hardware/vendor-state.py'
        if hashlib.sha256(helper_path.read_bytes()).hexdigest() != '41aa7200cd912a5f7a1438ad693fff83ad36b32873540b8a70d1b578f976600a':
            raise RuntimeError('Frozen source helper changed')
        spec = importlib.util.spec_from_file_location('piv001_public_provenance', helper_path)
        helper = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(helper)
        report['provenance'] = helper.provenance()
        report['stage'] = 'source-checked'
        baseline = json.loads((RUN / 'hardware/baseline-103.json').read_text())
        readers = [d for d in list_devices() if d.fingerprint == baseline['selected_reader']]
        if len(readers) != 1:
            raise RuntimeError('Exact reader scope')
        with ScardSmartCardConnection(readers[0].reader.createConnection()) as connection:
            info = ManagementSession(connection).read_device_info()
            if info.serial != 103 or info.version_name != baseline['device']['version_name']:
                raise RuntimeError('Live Management identity')
            session = PivSession(connection)
            if tuple(session.version) != (0, 0, 1) or int.from_bytes(session.protocol.send_apdu(0, 0xF8, 0, 0), 'big') != 103:
                raise RuntimeError('Live PIV identity')
            report['stage'] = 'identity-checked'
            fields = Tlv.parse_dict(session.protocol.send_apdu(0, 0xF7, 0, SLOT.KEY_MANAGEMENT))
            point = Tlv.unpack(0x86, fields[4])
            report['metadata'] = dict(algorithm_id=fields[1][0], origin=fields[3][0],
                                      alice_public_matches=point.hex() == '8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a')
            if fields[1] != b'\xe1' or fields[3] != b'\x02' or not report['metadata']['alice_public_matches']:
                raise RuntimeError('P01 same-key fixture precondition')
            report['stage'] = 'fixture-checked'
            session.verify_pin('123456')
            report['factory_pin_verified'] = True
            report['stage'] = 'factory-pin-verified'
            peer = X25519PublicKey.from_public_bytes(bytes.fromhex('de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f'))
            actual = bytearray(session.calculate_secret(SLOT.KEY_MANAGEMENT, peer))
            import hmac
            report['shared_secret_matches_rfc7748'] = hmac.compare_digest(actual, expected)
            report['stage'] = 'agreement-completed'
            report['status'] = 'verified' if report['shared_secret_matches_rfc7748'] else 'mismatch'
    except Exception as error:
        report['error_type'] = type(error).__name__
    finally:
        actual[:] = b'\0' * len(actual)
        expected[:] = b'\0' * len(expected)
        json.dump(report, output, indent=2)
        output.write('\n')
print(json.dumps(dict(status=report['status'], output=str(args.output))))
sys.exit(0 if report['status'] == 'verified' else 1)
