#!/usr/bin/env python3
"""Independent mechanical checker, not independent content review or SDK execution."""
import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess

here = Path(__file__).resolve().parent
run = here.parent.parent
root = next(parent for parent in here.parents if (parent/'.git').exists())
sha = lambda data: hashlib.sha256(data).hexdigest()
load = lambda path: json.loads(path.read_text())
pin = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
failures = []


def check(condition, message):
    if not condition:
        failures.append(message)


packet = load(run/'inventory/packets/BOUNDARY-Oath.json')
original = {unit['id']:unit for unit in load(run/'inventory/semantic-units.json')}
units = load(here/'unit-dispositions.json')
files = load(here/'file-dispositions.json')
notes = load(here/'file-notes.json')
summary = load(here/'coverage-summary.json')
check(collections.Counter(u['id'] for u in units) == collections.Counter(packet['unit_ids']), 'Exact unit ID population')
check({f['path'] for f in files} == set(packet['disposition_paths']) == set(notes), 'Exact candidate file dispositions')
check({u['path'] for u in units} == set(packet['paths']), 'Selected unit paths')
for unit in units:
    source = original[unit['id']]
    for key in ['path','source_sha256','range_sha256','first_line','last_line','start_character','end_character_exclusive']:
        check(unit[key] == source[key], f"Original {key}: {unit['id']}")
    raw = (root/unit['path']).read_bytes()
    text = raw.decode('utf-8-sig',errors='replace')
    body = text[unit['start_character']:unit['end_character_exclusive']]
    check(sha(raw) == unit['source_sha256'], f"Raw source: {unit['id']}")
    check(sha(body.encode()) == unit['range_sha256'], f"Character range: {unit['id']}")
    check(unit['status'] in ['READ-CLEAN','READ-CANDIDATE','READ-CONTEXT'], f"Final read outcome: {unit['id']}")
for file in files:
    raw = (root/file['path']).read_bytes()
    before = subprocess.check_output(['git','show',pin+':'+file['path']],cwd=root)
    norm = lambda value:value.removeprefix(b'\xef\xbb\xbf').replace(b'\r\n',b'\n')
    check(norm(raw) == norm(before), 'Product pin equality: '+file['path'])
    check(sha(raw) == file['source_sha256'] and sha(before) == file['pinned_raw_sha256'], 'File identity: '+file['path'])
    check(file['full_file_read'] and file['product_disposition'] == 'UNCHANGED', 'File disposition: '+file['path'])
check(summary['counts'] == dict(collections.Counter(u['status'] for u in units)), 'Summary counts')
check(summary['unread'] == 0 and summary['assigned_units'] == len(units), 'No unread IDs')
check(summary['complete_context_lines'] == sum(f['lines'] for f in files) == packet['lines'], 'Full file line population')
nominees = load(here/'clean-nominees.json')
by_id = {unit['id']:unit for unit in units}
for nominee in nominees:
    unit = by_id[nominee['unit_id']]
    check(unit['status'] == 'READ-CLEAN' and unit['last_line']-unit['first_line'] > 5, 'Substantive eligible nominee: '+unit['id'])
    check(nominee['source_sha256'] == unit['source_sha256'] and nominee['range_sha256'] == unit['range_sha256'], 'Nominee identity: '+unit['id'])
inputs = load(run/'inventory/inherited-packet-inputs.json')['BOUNDARY-Oath']
inherited = load(here/'inherited-dispositions.json')
check({row['key'] for row in inherited['rows']} == set(inputs['provenance_keys']), 'Exact inherited provenance keys')
check(inherited['registry_sha256'] == sha((run/'inherited-findings/registry.json').read_bytes()), 'Inherited authority unchanged')
source_manifest = load(here/'source-manifest.json')
capture = source_manifest['original_capture']
check(sha((here/capture['path']).read_bytes()) == capture['sha256'], 'Captured vendor original')
for reused in source_manifest['reused']:
    if reused['path'] == 'yubikit/oath.py':
        data = subprocess.check_output(['git','show',reused['revision']+':'+reused['path']],cwd='/home/rupicola/Code/y/yubikey-manager')
    else:
        data = (root/reused['path']).read_bytes()
    check(sha(data) == reused['sha256'], 'Reused authority/source: '+reused['path'])
candidates = load(here/'candidate-index.json')
check([c['id'] for c in candidates] == [f'CORE-OATH-X{i:03}' for i in range(1,7)], 'Local candidate population')
for candidate in candidates:
    actual = {u['id'] for u in units if candidate['id'] in u['local_candidates']}
    check(actual == set(candidate['assigned_unit_ids']), 'Localized candidate IDs: '+candidate['id'])
check(summary['accepted_core_ids'] == 0, 'No accepted finding allocations')
# Check authored local links without treating code backticks or remote URLs as links.
markdowns = [here/'analysis.md',here/'candidates.md',here/'sources.md',run/'boundary-Oath.md']
for markdown in markdowns:
    text = markdown.read_text()
    check(text.count('```') % 2 == 0, 'Balanced code fences: '+str(markdown))
    for target in re.findall(r'(?<!!)\[[^\]]+\]\(([^)]+)\)',text):
        if target.startswith(('https://','http://','#')):
            continue
        check((markdown.parent/target.split('#',1)[0]).exists(), 'Local link: '+target)
dependencies = [
 'src/Core/src/Sessions/ApplicationSession.cs','src/Core/src/Utilities/ExchangeGuard.cs',
 'src/Core/src/Devices/YubiKeyConnectionExtensions.cs','src/Core/src/Devices/Feature.cs',
 'src/Core/src/Devices/FirmwareVersion.cs','src/Core/src/Protocols/SmartCard/Scp/PcscProtocolScp.cs',
 'src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs','src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs',
 'src/Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs','src/Core/src/Protocols/SmartCard/Scp/Scp11KeyParameters.cs',
 'src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs','src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs',
 'src/Core/src/Protocols/SmartCard/Apdu/ApduException.cs','src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs',
 'src/Core/src/Utilities/Base32.cs','src/Core/src/Credentials/UserPresenceNotification.cs',
 'src/Core/src/Credentials/ConsoleCredentialReader.cs','src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/Base32Tests.cs',
 'src/Tests.Shared/RecordingSmartCardConnection.cs','src/Cli.Shared/src/Device/DeviceSelector.cs',
 'src/Cli.Commands/src/Oath/OathHelpers.cs',
 'src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs',
 'docs/migration/v1-to-v2.md'
]
dependency_identities = []
for path in dependencies:
    data = (root/path).read_bytes()
    before = subprocess.check_output(['git','show',pin+':'+path],cwd=root)
    check(norm(data) == norm(before),'Narrow dependency unchanged: '+path)
    dependency_identities.append(dict(path=path,sha256=sha(data),pinned_raw_sha256=sha(before),
        role='Read-only dependency, no primary coverage credit; complete relevant caller/helper scope'))
result = dict(result='PASS' if not failures else 'FAIL',source_pin=pin,
    exact_assigned_units=len(units),candidate_files=len(files),vendor_capture=True,
    reused_sources=len(source_manifest['reused']),inherited_keys=len(inherited['rows']),
    substantive_clean_nominees=len(nominees),local_candidates=len(candidates),
    counts=summary['counts'],unread=0,accepted_core_ids=0,failures=failures,
    narrow_dependency_identities=dependency_identities,
    owned_record_hashes=[dict(path=str(path.relative_to(root)),sha256=sha(path.read_bytes()))
        for path in sorted(here.iterdir()) if path.is_file() and path.name != 'verification.json'] +
        [dict(path=str((run/'boundary-Oath.md').relative_to(root)),sha256=sha((run/'boundary-Oath.md').read_bytes()))],
    limits='Mechanical verification only. Independent content/citation review, edits, sample acceptance, compilation and runtime remain pending.',
    sdk_tests=0,builds=0,native_runs=0,crypto_runs=0,device_operations=0)
(here/'verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({key:value for key,value in result.items() if key not in ['narrow_dependency_identities','owned_record_hashes']},indent=2))
raise SystemExit(bool(failures))
