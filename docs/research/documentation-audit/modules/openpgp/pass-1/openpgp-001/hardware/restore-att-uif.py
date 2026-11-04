"""Restore the measured, reversible Att On policy to the initial Off policy on serial 103."""
from pathlib import Path
import hashlib
import inspect
import json
import logging
import runpy
from yubikit.openpgp import INS, KEY_REF, UIF, OpenPgpSession

HERE = Path(__file__).resolve().parent
helper = runpy.run_path(str(HERE / 'vendor-state.py'), run_name='audit_vendor_state')
baseline = json.loads((HERE / 'vendor-baseline-v2.json').read_text())
assert baseline['success'] and baseline['state']['uif']['ATT']['value']['name'] == 'OFF'
logging.disable(logging.CRITICAL)
result = {'serial': 103, 'purpose': 'restore initial Att UIF Off after authorized On probe',
          'private_key_replacement': False, 'commands': [],
          'installed_set_uif_source': inspect.getsource(OpenPgpSession.set_uif),
          'installed_uif_encoding_source': inspect.getsource(UIF),
          'baseline_sha256': hashlib.sha256((HERE / 'vendor-baseline-v2.json').read_bytes()).hexdigest()}
connection, info = helper['connect']()
with connection:
    session = helper['session_without_activation'](connection)
    before = helper['read_uif'](session, KEY_REF.ATT, info)
    assert before['status'] == 'ok' and before['value']['name'] == 'ON', before
    result['before'] = before
    # Published factory TEST credential verified after the prior reset. Retain no credential bytes.
    session.verify_admin('12345678')
    encoded = bytes(UIF.OFF)
    assert encoded == bytes.fromhex('0020')
    # Mirror installed set_uif's put_data command after observing non-fixed On. The direct
    # client version remains 0.0.1; real development metadata and each APDU are recorded.
    helper['public_apdu'](session, result['commands'], INS.PUT_DATA, 0, KEY_REF.ATT.uif_do, encoded)
    result['version'] = {'direct_application': str(session.version), 'metadata': info.version_name}
connection, info = helper['connect']()
with connection:
    session = helper['session_without_activation'](connection)
    after = helper['read_uif'](session, KEY_REF.ATT, info)
    assert after['status'] == 'ok' and after['value']['name'] == 'OFF', after
    result['fresh_after'] = after
    certificate = helper['certificate_identity'](session, KEY_REF.ATT, info)
    assert certificate['der_sha256'] == baseline['state']['certificates']['ATT']['der_sha256']
    result['att_certificate_preserved'] = True
with (HERE / 'restore-att-uif.json').open('x') as output:
    json.dump(result, output, indent=2)
    output.write('\n')
print('PASS: actual Att On restored to initial Off on fresh readback; attestation certificate preserved.')
