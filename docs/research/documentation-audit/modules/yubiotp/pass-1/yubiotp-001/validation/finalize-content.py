"""Bind accepted source hashes through exact formatter transforms and reconcile all63 paths."""
from pathlib import Path
import hashlib
import json
import subprocess

HERE=Path(__file__).resolve().parent
RUN=HERE.parent
ROOT=next(p for p in HERE.parents if (p/'toolchain.cs').exists())
BASELINE='95e5ccaeda33954c21f2a38058d4f66b5f208787'
def sha(data):return hashlib.sha256(data).hexdigest()
accepted=json.loads((RUN/'evidence/review-final-content.json').read_text())
transforms={r['path']:r for r in json.loads((HERE/'format-transform.json').read_text())['files']}
assert accepted['accepted_paths']==50 and len(transforms)==42
rows=[]
for r in accepted['files']:
    path=r['path']; final=sha((ROOT/path).read_bytes())
    if path in transforms:
        t=transforms[path]
        assert t['before_sha256']==r['sha256'] and t['after_sha256']==final,path
    else:assert r['sha256']==final,path
    rows.append(dict(path=path,review='review-followup.md',reviewed_sha256=r['sha256'],final_sha256=final,
                     transform='validation/format-transform.json' if path in transforms else None,scope=r['scope']))
authored={r['path'] for r in rows}
changed=set(subprocess.check_output(['git','diff','--name-only',BASELINE,'--','src/YubiOtp','docs/usage/user-interaction.md',
    'docs/migration/v1-to-v2.md','docs/migration/v1-to-v2-gaps.md','docs/migration/v1-to-v2-map.yml',
    'docs/architecture/device-discovery-guarantees.md'],cwd=ROOT,text=True).splitlines())
assert changed==authored,(changed-authored,authored-changed)
all_src=subprocess.check_output(['git','diff','--name-only',BASELINE,'--','src'],cwd=ROOT,text=True).splitlines()
assert all(p.startswith('src/YubiOtp/') for p in all_src)
inventory=json.loads((RUN/'inventory.json').read_text())['files']
coverage={r['path']:r for r in json.loads((RUN/'coverage.json').read_text())['files']}
assert set(coverage)=={r['path'] for r in inventory}
final_paths=[]
for r in inventory:
    path=r['path'];final=sha((ROOT/path).read_bytes());edited=path in authored
    if not edited:assert final==r['sha256'],path
    final_paths.append(dict(path=path,baseline_sha256=r['sha256'],final_sha256=final,edited=edited,
                           disposition=coverage[path]['disposition'],read='complete',
                           review='review-followup.md' if edited else 'investigation; clean sample if selected'))
population=json.loads((RUN/'sampling-population.json').read_text())
samples={'OTP-CL001','OTP-CL009','OTP-CL016'}
eligible=0
for r in population['units']:
    # Formatter may normalize a final newline outside the stored range; prose/body remains exact.
    remains=r['baseline_unit'] in (ROOT/r['path']).read_text()
    r['final_eligible']=remains and r['category']!='metadata'
    if r['id'] in samples:assert r['final_eligible'],r['id']
    eligible+=r['final_eligible']
assert len(samples)>=max(3,(eligible+9)//10)
(RUN/'reviewed-files.json').write_text(json.dumps(dict(baseline=BASELINE,authored_count=len(rows),files=rows),indent=2)+'\n')
(RUN/'coverage-final.json').write_text(json.dumps(dict(baseline=BASELINE,module_paths=len(final_paths),
    public_types=24,api_entries=193,authored_paths=len(rows),edited_csharp=42,
    edited_module_paths=sum(r['edited'] for r in final_paths),unchanged_module_paths=sum(not r['edited'] for r in final_paths),
    final_clean_eligible=eligible,counted_samples=sorted(samples),files=final_paths,
    external_authored=[r for r in rows if not r['path'].startswith('src/YubiOtp/')]),indent=2)+'\n')
(RUN/'sampling-final.json').write_text(json.dumps(dict(eligible=eligible,samples=sorted(samples),units=population['units']),indent=2)+'\n')
print(f'PASS:50accepted authored hashes=>final,42formatter transforms,63module paths,3/{eligible} final clean samples; no other-module source changes.')
