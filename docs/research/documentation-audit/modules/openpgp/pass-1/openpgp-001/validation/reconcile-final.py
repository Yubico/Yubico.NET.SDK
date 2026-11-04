"""Bind accepted content through whitespace-only formatting; reconcile all coverage and samples."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
BASE = 'fe180282a2d77b15eaaa5868d6c8b00e251d1f05'
def sha(raw): return hashlib.sha256(raw).hexdigest()
def save(path, data): path.write_text(json.dumps(data, indent=2) + '\n')
def git(path): return subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT)

accepted_path = RUN / 'evidence/review-accepted.json'
accepted = json.loads(accepted_path.read_text())['files']
assert len(accepted) == 59 and len({r['path'] for r in accepted}) == 59
before = json.loads((HERE / 'pre-format-corrected-content.json').read_text())
assert before['baseline'] == BASE and len(before['files']) == 49
by_path = {r['path']: r for r in before['files']}
transforms = []; reviewed = []
for row in accepted:
    path = row['path']; raw = (ROOT / path).read_bytes(); current = sha(raw)
    if path in by_path:
        prior = by_path[path]; old = prior['content']; new = raw.decode()
        assert sha(old.encode()) == prior['sha256'] == row['sha256'], path
        assert re.sub(r'\s', '', old) == re.sub(r'\s', '', new), path
        assert re.findall(r'^using .+;', old, re.M) == re.findall(r'^using .+;', new, re.M), path
        transforms.append(dict(path=path, before_sha256=prior['sha256'], after_sha256=current,
                               whitespace_only=True, imports_identical=True))
    else: assert current == row['sha256'], path
    reviewed.append(dict(path=path, reviewed_sha256=row['sha256'], final_sha256=current,
                         scope=row.get('scope', 'accepted product path')))
assert len(transforms) == 49
save(HERE / 'format-transform.json', dict(baseline=BASE, accepted_manifest_sha256=sha(accepted_path.read_bytes()), files=transforms))
save(RUN / 'reviewed-files.json', dict(baseline=BASE, files=reviewed))
project = 'src/OpenPgp/src/Yubico.YubiKit.OpenPgp.csproj'
old = ET.fromstring(git(project)); new = ET.fromstring((ROOT / project).read_bytes())
a = old.findall('.//Description'); b = new.findall('.//Description'); assert len(a) == len(b) == 1
description = dict(before=a[0].text, after=b[0].text)
assert description['before'] != description['after']; b[0].text = a[0].text
assert ET.tostring(old) == ET.tostring(new), 'Project changes exceed the accepted Description'
save(HERE / 'package-description.json', dict(path=project, baseline_sha256=sha(git(project)),
    final_sha256=sha((ROOT / project).read_bytes()), description=description, all_other_xml_identical=True))
inventory = json.loads((RUN / 'inventory.json').read_text())['files']
coverage = json.loads((RUN / 'coverage.json').read_text())
dispositions = {r['path']: r for r in coverage['files']}; authored = {r['path'] for r in reviewed}
assert {r['path'] for r in inventory} == set(dispositions)
module = []
for row in inventory:
    current = sha((ROOT / row['path']).read_bytes()); edited = row['path'] in authored
    if not edited: assert current == row['sha256'], row['path']
    module.append(dict(path=row['path'], baseline_working_sha256=row['sha256'], final_sha256=current,
        edited=edited, disposition='accepted-doc-change' if edited else dispositions[row['path']]['disposition']))
assert len(module) == 96 and sum(r['edited'] for r in module) == 52
assert len(authored - set(dispositions)) == 7
units = []
for unit in coverage['clean_semantic_units']:
    lo, hi = map(int, unit['lines'].split('-'))
    old = git(unit['path']).decode(); body = '\n'.join(old.splitlines()[lo-1:hi])
    whole_file = unit['id'].startswith('PGP-FILE-CLEAN-')
    eligible = unit['path'] not in authored if whole_file else body in (ROOT / unit['path']).read_text()
    units.append(dict(unit, eligible=eligible, baseline_body_sha256=sha(body.encode()),
                      reason='unchanged semantic unit' if eligible else 'edited overlap; accepted documentation path'))
eligible = [u for u in units if u['eligible']]
assert len(eligible) == 32, [(u['id'],u['path']) for u in eligible]
sampled = json.loads((RUN / 'sampling-accepted.json').read_text())['samples']
assert len(sampled) == 11
for sample in sampled:
    assert sample['id'] in {u['id'] for u in eligible}
    assert sha((ROOT / sample['path']).read_bytes()) == sample['checked_sha256']
save(RUN / 'sampling-final.json', dict(baseline=BASE, eligible=32, minimum_required=4,
    samples=sampled, units=units, expansion='Eight accepted API units plus narrative/example/internal-test; PinPolicy and timestamp misses corrected and excluded'))
save(RUN / 'coverage-final.json', dict(baseline=BASE, module_paths=96, public_types=39, api_entries=423,
    interface_operations=40, authored_paths=59, module_edited=52, module_unchanged=44,
    shared_edited=7, clean_eligible=32, clean_sampled=11, module=module,
    shared=[r for r in reviewed if r['path'] not in dispositions],
    correction='PGP-C022 generation time extends PGP-IMP-018; zero-sentinel/signed carrier docs corrected'))
print('PASS:59 accepted-to-final hashes,49 whitespace-only C# transforms,Description-only project change,96 module dispositions,11/32 final clean samples.')
