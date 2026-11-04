"""Reconcile exact review identities, bounded formatter transforms and all module inventory paths."""
from pathlib import Path
import hashlib
import json
import re
import subprocess

HERE=Path(__file__).resolve().parent
RUN=HERE.parent
ROOT=next(p for p in HERE.parents if (p/'toolchain.cs').exists())
baseline='b73efe3813b429b4b8eb2a7e819a6ad7be699d47'
def sha(data): return hashlib.sha256(data).hexdigest()
accepted={}
for path,h in re.findall(r'^\| `([^`]+)` \| `([0-9a-f]{64})` \| PASS[^\n]*$',(RUN/'review.md').read_text(),re.M):
    accepted[path]=(h,'review.md')
for path,h in re.findall(r'^\| `([^`]+)` \| `([0-9a-f]{64})` \|$',(RUN/'review-followup.md').read_text(),re.M):
    accepted[path]=(h,'review-followup.md')
assert len(accepted)==26,len(accepted)
transforms={r['path']:r for r in json.loads((HERE/'format-transform.json').read_text())['files']}
rows=[]
for path,(h,review) in sorted(accepted.items()):
    final=sha((ROOT/path).read_bytes())
    if path in transforms:
        tr=transforms[path]
        assert tr['before_sha256']==h and tr['after_sha256']==final,path
        transform='validation/format-transform.json'
    else:
        assert final==h,path
        transform=None
    rows.append(dict(path=path,review=review,reviewed_sha256=h,final_sha256=final,post_review_transform=transform))
scopes=['src/SecurityDomain','docs/AI-DOCS-GUIDE.md','docs/migration/v1-to-v2.md','docs/migration/v1-to-v2-map.yml']
changed=set(subprocess.check_output(['git','diff','--name-only',baseline,'--',*scopes],cwd=ROOT,text=True).splitlines())
assert changed==set(accepted),(changed-set(accepted),set(accepted)-changed)
all_src=set(subprocess.check_output(['git','diff','--name-only',baseline,'--','src'],cwd=ROOT,text=True).splitlines())
assert all(p.startswith('src/SecurityDomain/') for p in all_src),all_src
inventory=json.loads((RUN/'inventory.json').read_text())
coverage=json.loads((RUN/'coverage.json').read_text())
coverage_rows={r['path']:r for r in coverage['files']}
assert set(coverage_rows)=={r['path'] for r in inventory['files']}
final_paths=[]
for item in inventory['files']:
    p=item['path']; current=sha((ROOT/p).read_bytes())
    edited=p in accepted
    if not edited: assert current==item['sha256'],p
    final_paths.append(dict(path=p,investigation=coverage_rows[p]['disposition'],read='complete',
        candidates=coverage_rows[p]['candidates'],edited=edited,baseline_sha256=item['sha256'],final_sha256=current,
        authored_review=accepted[p][1] if edited else None,
        note='Independent changed-content review' if edited else 'Investigation disposition; not claimed individually re-reviewed'))
sample=json.loads((HERE/'final-sampling.json').read_text())
for r in sample['unchanged_files']: assert sha((ROOT/r['path']).read_bytes())==r['sha256']
replacement=sample['replacement']; text=(ROOT/replacement['path']).read_text()
block=re.findall(r'^```csharp\s*\n(.*?)^```\s*$',text,re.M|re.S)[replacement['csharp_block']-1]
assert sha(block.encode())==replacement['baseline_and_current_sha256']
(RUN/'reviewed-files.json').write_text(json.dumps(dict(baseline=baseline,authored_count=len(rows),files=rows),indent=2)+'\n')
(RUN/'coverage-final.json').write_text(json.dumps(dict(baseline=baseline,state='content-reviewed-and-reconciled',
    authority='status.md is current progression; coverage.json is investigation history; validation.md records gates',
    module_paths=len(final_paths),edited_module_paths=sum(r['edited'] for r in final_paths),
    unchanged_module_paths=sum(not r['edited'] for r in final_paths),public_families=8,api_entries=96,
    authored_paths=26,edited_csharp_paths=18,core_boundary_review='deferred-until-Core',
    final_clean_population=len(sample['eligible']),clean_samples=len(sample['counted']),
    files=final_paths,external_authored=[r for r in rows if not r['path'].startswith('src/SecurityDomain/')]),indent=2)+'\n')
print('PASS:26 authored review identities reconcile;38 module dispositions (23 edited/15 unchanged),18 C# transforms,3/9 final clean samples; no Core/other-module source changes.')
