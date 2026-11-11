#!/usr/bin/env python3
"""Route the parallel owner's registry into packets, retaining its finding states."""
import collections
import hashlib
import json
from pathlib import Path

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
OUT = RUN / 'inventory'
registry_path = RUN / 'inherited-findings/registry.json'
registry_bytes = registry_path.read_bytes()
registry = json.loads(registry_bytes)
file_by_path = {f['path']: f for f in json.loads((OUT / 'files.json').read_text())}
assert registry['source_baseline'] == 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
A = set('discovery-abandonment metadata-fips-bits metadata-form-factor fixture-cache-docs metadata-firmware-read metadata-field-length metadata-pagination metadata-wide-app-bits'.split())
C = set('base32-failure-cleanup base32-shape credential-length-units ctap-crypto-cleanup runtime-scalar-copy ber-length-safety private-key-consistency runtime-rng-substitution runtime-hkdf-substitution tlv-encoder-sizing credential-redirect-policy credential-confirm-cleanup logging-payload-policy'.split())
B1 = set('pcsc-pin-isolation pcsc-reader-name-cleanup'.split())
B6 = set('scp-parameter-ownership scp-auth-readiness apdu-tlv-ownership scp-short-response scp-response-authentication certificate-bundle-partial-cleanup'.split())
module_packets = {'oath': 'BOUNDARY-Oath', 'management': 'BOUNDARY-Management', 'ctap2': 'BOUNDARY-Ctap2',
                  'webauthn': 'BOUNDARY-WebAuthn', 'security-domain': 'BOUNDARY-SecurityDomain',
                  'yubiotp': 'BOUNDARY-YubiOtp', 'yubihsm': 'BOUNDARY-YubiHsm', 'openpgp': 'BOUNDARY-OpenPgp', 'piv': 'BOUNDARY-Piv'}


def primary(group):
    if group in A: return 'A'
    if group in C: return 'C'
    if group in B1: return 'B1-portable-lifetimes'
    if group in B6: return 'B6-wire-scp'
    if group == 'native-stall-evidence': return 'B2-linux-lifetimes'
    if group in ['migration-yaml', 'toolchain-host-selection']: return 'B4-windows-support'
    if group.startswith('wa-') or group == 'native-selected-user-actor': return 'BOUNDARY-WebAuthn'
    if group.startswith('ctap-') or group == 'assertion-user-docs': return 'BOUNDARY-Ctap2'
    if group in ['oath-auth-failure-state', 'oath-raw-access-key']: return 'BOUNDARY-Oath'
    if group == 'management-reset-lifecycle': return 'BOUNDARY-Management'
    if group == 'otp-relocation-evidence': return 'BOUNDARY-YubiOtp'
    if group == 'hsm-supplied-cryptogram-shape': return 'BOUNDARY-YubiHsm'
    return 'B5-session-framing'


routing = []
inputs = collections.defaultdict(lambda: dict(group_ids=[], provenance_keys=[], read_paths=set()))
for group, keys in registry['groups'].items():
    rows = [r for r in registry['rows'] if r['key'] in keys]
    assert len(rows) == len(keys)
    packet = primary(group)
    assert (OUT / 'packets' / (packet + '.json')).exists(), packet
    mentioned, reopening, boundaries = set(), set(), set()
    for row in rows:
        mentioned.update(row['current_implementation_paths'])
        mentioned.update(row['current_test_sources_to_read_or_reverify'])
        if row['module'] in module_packets: boundaries.add(module_packets[row['module']])
        for field in ['reopen_paths', 'sample_test_reopening_paths', 'module_xml_reopening_paths']:
            reopening.update(row['documentation'].get(field, []))
    expanded = sorted({p for target in mentioned for p in file_by_path if p == target or p.startswith(target.rstrip('/') + '/')})
    secondary = sorted(({file_by_path[p]['stage'] for p in expanded} | boundaries) - {packet, 'SHARED', 'EXTERNAL'})
    routing.append(dict(group=group, primary_packet=packet, provenance_keys=keys, secondary_read_packets=secondary,
                        mandatory_boundary_packets=sorted(boundaries), current_read_paths=expanded,
                        source_primary_owners={p: file_by_path[p]['stage'] for p in expanded},
                        reopening_targets=sorted(reopening), status='UNREAD',
                        qualification='Routing retains inherited states. Questions are nonadditive; consume original evidence before current family/boundary review.'))
    inputs[packet]['group_ids'].append(group)
    inputs[packet]['provenance_keys'].extend(keys)
    inputs[packet]['read_paths'].update(expanded)
    for boundary in boundaries:
        if boundary != packet:
            inputs[boundary]['group_ids'].append(group)
            inputs[boundary]['provenance_keys'].extend(keys)
            inputs[boundary]['read_paths'].update(p for p in expanded if file_by_path[p]['assigned_family'] == boundary or p.startswith('src/Core/'))
for value in inputs.values():
    value['read_paths'] = sorted(value['read_paths'])
    value['group_ids'] = sorted(set(value['group_ids']))
    value['provenance_keys'] = sorted(set(value['provenance_keys']))
assert len(routing) == 70
assert sum(len(r['provenance_keys']) for r in routing) == 100
result = dict(source_baseline=registry['source_baseline'], registry_path=str(registry_path),
              registry_sha256=hashlib.sha256(registry_bytes).hexdigest(), provenance_rows=100,
              nonadditive_groups=70, current_states='Inherited unchanged; see authoritative registry.',
              separately_preserved_pgp_documentation_labels=[r['key'] for r in registry['openpgp_documentation_dependencies']],
              groups=routing)
(OUT / 'inherited-routing.json').write_text(json.dumps(result, indent=2) + '\n')
(OUT / 'inherited-packet-inputs.json').write_text(json.dumps(dict(inputs), indent=2) + '\n')
print(json.dumps(dict(groups=len(routing), provenance_rows=100, pgp_labels=21,
                     primary_packet_groups=dict(collections.Counter(r['primary_packet'] for r in routing))), indent=2))
