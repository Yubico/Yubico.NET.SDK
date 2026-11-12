"""Fresh independent-of-assembly static joins; not an independent content reviewer.

Adapted from CTAP2's checker shape. Only this packet's receipt is written.
"""
import collections
import hashlib
import json
import pathlib
import re
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
RUN = HERE.parents[1]
ROOT = RUN.parents[6]
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
failures = []


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(path):
    return json.loads(path.read_text())


def check(condition, message):
    if not condition:
        failures.append(message)


def check_file(path, digest, label):
    try:
        check(sha(path.read_bytes()) == digest, label)
    except OSError as error:
        failures.append(f'{label}: {error}')


packet = load(RUN / 'inventory/packets/BOUNDARY-WebAuthn.json')
units = load(HERE / 'unit-dispositions.json')
files = load(HERE / 'file-dispositions.json')
inherited = load(HERE / 'inherited-dispositions.json')
summary = load(HERE / 'verification.json')
check(len(units) == len({u['id'] for u in units}) == 265, 'unit count/duplicates')
check({u['id'] for u in units} == set(packet['unit_ids']), 'exact assigned IDs')
check({f['path'] for f in files} == set(packet['disposition_paths']), 'exact candidate population')
check(len(packet['paths']) == 95 and len(files) == 129, 'explicit path counts')
check(summary['disposition_counts'] == dict(collections.Counter(u['disposition'] for u in units)), 'disposition counts')
inventory = {u['id']: u for u in load(RUN / 'inventory/semantic-units.json')}
for row in units:
    original = inventory[row['id']]
    for key in ['path', 'first_line', 'last_line', 'start_character', 'end_character_exclusive', 'source_sha256', 'range_sha256']:
        check(row[key] == original[key], f'inventory identity {row["id"]}: {key}')
    data = (ROOT / row['path']).read_bytes()
    text = data.decode('utf-8-sig')
    check(sha(data) == row['source_sha256'], f'raw source {row["id"]}')
    check(sha(text[row['start_character']:row['end_character_exclusive']].encode()) == row['range_sha256'], f'range {row["id"]}')
    check(row['disposition'] in ['READ-CLEAN', 'READ-CANDIDATE', 'READ-RELATED-CANDIDATE-CONTEXT', 'READ-EXCLUSION'], f'disposition {row["id"]}')
    check(row['independent_review'] == 'PENDING', f'review {row["id"]}')
for row in files + load(HERE / 'dependency-identities.json'):
    data = (ROOT / row['path']).read_bytes()
    pinned = subprocess.check_output(['git', 'show', f'{PIN}:{row["path"]}'], cwd=ROOT)
    check(sha(data) == row['source_sha256'], f'file {row["path"]}')
    check(data.decode('utf-8-sig').replace('\r\n', '\n') == pinned.decode('utf-8-sig').replace('\r\n', '\n'), f'pin {row["path"]}')
for row in load(HERE / 'source-identities.json'):
    check_file(ROOT / row['path'], row['sha256'], f'input source {row["path"]}')
for row in load(HERE / 'sources/captures.json'):
    check(row.get('status') == 200, f'original HTTP {row["path"]}')
    check_file(HERE / 'sources' / row['path'], row['sha256'], f'original bytes {row["path"]}')
route = load(RUN / 'inventory/inherited-packet-inputs.json')['BOUNDARY-WebAuthn']
check({r['key'] for r in inherited} == set(route['provenance_keys']) and len(inherited) == 31, 'inherited source-run keys')
registry = {r['key']: r for r in load(RUN / 'inherited-findings/registry.json')['rows']}
for row in inherited:
    check(row['registry_state_preserved'] == registry[row['key']]['current_state'], f'registry state {row["key"]}')
    check(row['original_definition'] == registry[row['key']]['historical_definition'], f'original definition {row["key"]}')
for row in load(HERE / 'candidate-index.json'):
    check(row['accepted_core_id'] is None, f'accepted ID {row["id"]}')
    check(bool(row['assigned_unit_ids']), f'unmapped candidate {row["id"]}')
for row in load(HERE / 'clean-nominees.json'):
    check(row['disposition'] == 'READ-CLEAN', f'clean nominee {row["id"]}')
    check(len(row['nomination_reason']) > 80, f'substantive nominee {row["id"]}')

baseline = load(RUN / 'baseline.json')
frozen_count = 0
for manifest in baseline['frozen_manifests']:
    path = ROOT / manifest['path']
    check_file(path, manifest['sha256'], f'frozen manifest {manifest["path"]}')
    base = path.parent / 'records' if path.parent.name == 'documentation-audit' else path.parent
    for row in load(path)['files']:
        check_file(base / row['path'], row['sha256'], f'frozen artifact {row["path"]}')
        frozen_count += 1
integration = baseline['integration_manifest']
path = ROOT / integration['path']
check_file(path, integration['sha256'], 'integration manifest')
integration_rows = load(path)['files']
for row in integration_rows:
    check_file(ROOT / row['path'], row['raw_sha256'], f'integration artifact {row["path"]}')
prior_piv = baseline['prior_piv_integration']['files']
for row in prior_piv:
    check_file(ROOT / row['path'], row['sha256'], f'prior PIV {row["path"]}')
user_files = load(RUN / 'user-work-baseline.json')
for row in user_files:
    check_file(ROOT / row['path'], row['sha256'], f'user file {row["path"]}')

for path in [*HERE.glob('*.md'), RUN / 'boundary-WebAuthn.md']:
    text = path.read_text()
    check(sum(line.startswith('```') for line in text.splitlines()) % 2 == 0, f'fences {path.name}')
    for link in re.findall(r'\]\(([^)]+)\)', text):
        if not re.match(r'(?:https?://|#)', link):
            check((path.parent / link.split('#')[0]).exists(), f'link {path.name}: {link}')

product_diff = subprocess.check_output(['git', '--no-optional-locks', 'diff', '--name-only', '--', 'src'], cwd=ROOT).decode().splitlines()
receipt = dict(kind='fresh-static-record-coverage-and-preservation-check', source_baseline=PIN,
    assigned_units=len(units), selected_paths=95, candidate_paths=len(files), inherited_keys=len(inherited),
    disposition_counts=summary['disposition_counts'], clean_nominees=len(load(HERE / 'clean-nominees.json')),
    preserved_frozen_artifacts=frozen_count, preserved_integration_artifacts=len(integration_rows),
    preserved_prior_piv_files=len(prior_piv), preserved_user_files=len(user_files),
    observed_product_diff_paths=product_diff,
    preparation_failures_preserved=['read_packet.py initial root calculation FileNotFoundError; corrected to file parents[9]',
        'assemble.py unmatched closing parenthesis at line232, then251; corrected before first successful assembly'],
    failures=failures, independent_content_review=False, sdk_native_crypto_device_execution=False,
    git_index_ref_commit_push=False, subagents=False)
receipt['owned_record_identities'] = [dict(path=str(p.relative_to(ROOT)), sha256=sha(p.read_bytes()), bytes=p.stat().st_size)
    for p in [*HERE.rglob('*'), RUN / 'boundary-WebAuthn.md']
    if p.is_file() and p.name != 'verification-receipt.json' and '__pycache__' not in p.parts]
prior_receipt = HERE / 'verification-receipt.json'
if prior_receipt.exists() and load(prior_receipt)['failures'] and not (HERE / 'verification-attempt-1.json').exists():
    (HERE / 'verification-attempt-1.json').write_bytes(prior_receipt.read_bytes())
prior_receipt.write_text(json.dumps(receipt, indent=2) + '\n')
print(json.dumps({k: v for k, v in receipt.items() if k != 'owned_record_identities'}, indent=2))
raise SystemExit(bool(failures))
