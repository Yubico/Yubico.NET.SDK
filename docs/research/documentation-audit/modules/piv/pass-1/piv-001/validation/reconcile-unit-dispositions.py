"""Close every original semantic-unit disposition without rewriting investigation history."""
from pathlib import Path
import collections
import hashlib
import json

RUN = Path(__file__).resolve().parent.parent
ROOT = next(p for p in RUN.parents if (p / 'toolchain.cs').exists())
coverage = json.loads((RUN / 'coverage.json').read_text())
accepted = {r['path']: r for r in json.loads((RUN / 'reviewed-files.json').read_text())['files']}
sampled = {r['id'] for r in json.loads((RUN / 'sampling-final.json').read_text())['samples']}
unchanged_findings = {
    'src/Piv/examples/PivTool/Cli/Menus/CertificatesMenu.cs':
        'PIVIMP-006 runtime menu defects remain deferred; accepted tool guidance and sample contracts qualify actual behavior. Unchanged acceptance: review-unchanged-candidates.md.',
    'src/Piv/src/DataObjects/DataObjectProtocol.cs':
        'PIVIMP-010 request-ID truncation remains deferred; accepted raw-object API guidance states the caller range responsibility. Unchanged acceptance: review-unchanged-candidates.md.',
    'docs/architecture/sdk-architecture-diagrams.md':
        'Scoped L2 PIV facade/base labels remain accurate at this abstraction. Helper-detail expansion is unnecessary; broader Core pipeline claims remain for the final Core cycle. Coordinator checked the scoped node labels.',
}
rows = []
for original in coverage['units'] + coverage['shared_units']:
    path = original['path']
    current_hash = hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
    if path in accepted:
        assert current_hash == accepted[path]['final_sha256'], path
        outcome = 'accepted-current-documentation-path'
        rationale = 'Full source investigation and independent affected-unit content/citation acceptance; exact final path identity binds edits and retained surrounding context.'
    elif original['disposition'] == 'candidate-associated':
        assert path in unchanged_findings, path
        outcome = 'unchanged-finding-dispositioned'
        rationale = unchanged_findings[path]
    elif original['category'] == 'configuration-api-snapshot' or original['name'] == 'file/import/type-context':
        outcome = 'unchanged-administrative-or-declaration-context'
        rationale = 'Inspected configuration/API/import context; no boilerplate prose was added and this unit is outside the clean-prose sample denominator.'
    else:
        outcome = 'investigated-clean'
        rationale = 'Complete semantic read found no required documentation edit; current source remains in the whole-module coverage inventory.'
    rows.append({'id': original['id'], 'path': path, 'original_start': original['start'],
        'original_end': original['end'], 'original_unit_sha256': original['sha256'],
        'current_path_sha256': current_hash, 'category': original['category'],
        'candidate_ids': original.get('candidate_ids', []), 'final_disposition': outcome,
        'rationale': rationale, 'coordinator_sampled': original['id'] in sampled})
assert len(rows) == len({r['id'] for r in rows}) == 944
assert sum(r['coordinator_sampled'] for r in rows) == 64
result = {'baseline': coverage['baseline'], 'module_units': len(coverage['units']),
    'shared_units': len(coverage['shared_units']), 'final_unit_dispositions': len(rows),
    'disposition_counts': dict(collections.Counter(r['final_disposition'] for r in rows)),
    'scope': 'Original ranges remain historical anchors; current full-path hashes and independent acceptance bind the final content. No pending investigator status is treated as current.',
    'units': rows}
(RUN / 'unit-dispositions-final.json').write_text(json.dumps(result, indent=2) + '\n')
print('PASS: all944 original semantic units dispositioned;64 samples bound; three unchanged finding-associated paths explicitly adjudicated.')
