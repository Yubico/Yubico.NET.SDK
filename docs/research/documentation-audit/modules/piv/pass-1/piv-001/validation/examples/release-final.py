"""Bind the coordinator's reviewed/formatted v2 release before current-body extraction."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
BASELINE = '727fe43afc430c5d8364d868a6436de50626e476'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def identity(path):
    return dict(path=str(path.relative_to(ROOT)), sha256=sha(path.read_bytes()))


review_path = RUN / 'reviewed-files.json'
accepted_path = RUN / 'evidence/review-accepted.json'
format_path = RUN / 'validation/format-transform.json'
reviewed = json.loads(review_path.read_text())
accepted = json.loads(accepted_path.read_text())
formatted = json.loads(format_path.read_text())
require(reviewed['baseline'] == accepted['baseline'] == formatted['baseline'] == BASELINE, 'Release baseline mismatch')
require(len(reviewed['files']) == len(accepted['files']) == 57 and len(formatted['files']) == 48, 'Release path counts changed')
require(formatted['accepted_manifest_sha256'] == sha(accepted_path.read_bytes()), 'Formatter binds another acceptance')
before = {row['path']: row for row in accepted['files']}
final = {row['path']: row for row in reviewed['files']}
changes = {row['path']: row for row in formatted['files']}
require(len(before) == len(final) == 57 and set(before) == set(final), 'Accepted/final path sets differ')
require(set(changes) == {path for path in final if path.endswith('.cs')}, 'Formatter C# path set differs')
for path, row in final.items():
    require(sha((ROOT / path).read_bytes()) == row['final_sha256'] and row['reviewed_sha256'] == before[path]['sha256'], 'Released source identity changed: ' + path)
    if path in changes:
        change = changes[path]
        require(change['before_sha256'] == row['reviewed_sha256'] and change['after_sha256'] == row['final_sha256'] and change['whitespace_only'] is True and change['imports_identical'] is True, 'Formatter boundary changed: ' + path)
    else:
        require(row['final_sha256'] == row['reviewed_sha256'], 'Unrecorded content transform: ' + path)
shared = sorted(path for path in final if not path.startswith('src/Piv/'))
require(len(shared) == 6, 'Assigned shared scope changed')
record = dict(baseline=BASELINE, editor_complete=True, reviewer_complete=True, formatter_complete=True,
              notification='Coordinator released final reviewed/formatted inputs for v2 extraction: all 57 accepted paths, 48 whitespace-only C# transforms with unchanged imports; PIV-REV-001–003 accepted. Product files stable; preparation and compiler execution remain separate.',
              editor_report=identity(RUN / 'edit-corrections.md'), final_review=identity(review_path),
              formatter_record=identity(format_path), accepted_review=identity(accepted_path),
              correction_acceptance=identity(RUN / 'review-corrections.md'), original_editor_report=identity(RUN / 'edit-report.md'),
              edited_paths=sorted(final), shared_paths=shared,
              whole_shared_paths=['docs/architecture/raw-access-tiers.md', 'docs/usage/user-interaction.md'],
              shared_sections={'docs/migration/v1-to-v2.md': ['PIV'], 'docs/migration/v1-to-v2-gaps.md': ['PIV'], 'docs/v1-to-v2-comparison.md': ['PIV']}, task_units=[])
target = HERE / 'extraction-scope.json'
require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
raw = (json.dumps(record, indent=2) + '\n').encode()
if target.exists():
    require(target.read_bytes() == raw, 'Preserve changed existing release')
else:
    with target.open('xb') as output:
        output.write(raw)
print('Bound coordinator release: 57 final accepted paths / 48 whitespace-only C# transforms / six assigned shared documents; final inventory may proceed.')
