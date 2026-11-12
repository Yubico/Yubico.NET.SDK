"""Mechanically separate record check, not independent semantic review or SDK execution."""
import collections
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[8]
RUN=OUT.parents[1]
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'

def load(p): return json.loads(p.read_bytes())
def sha(b): return hashlib.sha256(b).hexdigest()
def git(*args):
    return subprocess.check_output(['git',*args],cwd=ROOT,env={**os.environ,'GIT_OPTIONAL_LOCKS':'0'})

failures=[]
def check(condition,message):
    if not condition: failures.append(message)

packet=load(RUN/'inventory/packets/BOUNDARY-SecurityDomain.json')
units=load(OUT/'unit-dispositions.json')
inventory={u['id']:u for u in load(RUN/'inventory/semantic-units.json')}
files=load(OUT/'file-dispositions.json')
check(len(units)==len(packet['unit_ids'])==len({u['id'] for u in units}),'unique unit count')
check({u['id'] for u in units}==set(packet['unit_ids']),'exact assigned ID set')
check({f['path'] for f in files}==set(packet['disposition_paths']),'exact disposition path set')
for u in units:
    old=inventory[u['id']]
    for k in ['path','source_sha256','range_sha256','start_character','end_character_exclusive','first_line','last_line','category','label']:
        check(u[k]==old[k],f"inventory identity {u['id']}:{k}")
    raw=(ROOT/u['path']).read_bytes()
    text=raw.decode('utf-8-sig',errors='replace')
    body=text[u['start_character']:u['end_character_exclusive']]
    check(sha(raw)==u['source_sha256']==u['current_source_sha256'],f"raw hash {u['id']}")
    check(sha(body.encode())==u['range_sha256']==u['current_range_sha256'],f"range hash {u['id']}")
    check(u['status'] in ['READ-CLEAN','READ-CANDIDATE','READ-CONTEXT'],f"final disposition {u['id']}")
    check(bool(u['read_context']) and bool(u['localized_observations']),f"authored context {u['id']}")
for f in files:
    raw=(ROOT/f['path']).read_bytes()
    pinned=git('show',f"{PIN}:{f['path']}")
    norm=lambda b:b.decode('utf-8-sig').replace('\r\n','\n')
    check(sha(raw)==f['sha256'] and sha(pinned)==f['pinned_sha256'],f"file identity {f['path']}")
    check(norm(raw)==norm(pinned),f"source pin equality {f['path']}")
    check(bool(f['note']) and bool(f['candidate_associated_unchanged_reason']) and bool(f['handoff_owner']),f"file outcome {f['path']}")
for d in load(OUT/'dependency-identities.json'):
    check(sha((ROOT/d['path']).read_bytes())==d['sha256'],f"dependency identity {d['path']}")
for a in load(OUT/'localized-anchors.json'):
    check(1<=a['first_line']<=a['last_line']<=len((ROOT/a['path']).read_bytes().decode('utf-8-sig').splitlines()),f"bounded anchor {a}")
candidates=load(OUT/'candidate-index.json')
check({c['id'] for c in candidates}=={f'CORE-SD-X{n:03}' for n in range(1,9)},'eight stable local candidates')
for c in candidates:
    check(bool(c['anchors']) and bool(c['unit_ids']),f"candidate localization {c['id']}")
    for facet in ['documentation','implementation','decision','qualification','counting']:
        check(bool(c[facet]),f"candidate facet {c['id']}:{facet}")
for n in load(OUT/'clean-nominees.json'):
    u=next(u for u in units if u['id']==n['id'])
    check(u['status']=='READ-CLEAN' and not u['candidates'],f"clean nominee eligibility {n['id']}")
    check(n['source_sha256']==u['source_sha256'] and n['range_sha256']==u['range_sha256'],f"nominee identity {n['id']}")
inherit=load(OUT/'inherited-dispositions.json')
keys=set(load(RUN/'inventory/inherited-packet-inputs.json')['BOUNDARY-SecurityDomain']['provenance_keys'])
check({r['key'] for r in inherit['assigned']}==keys,'all sixteen inherited keys')
check(set(inherit['local_original_aliases'])=={f'SD-IMP-{n:03}' for n in range(1,9)},'all eight original SD aliases')
for r in inherit['assigned']:
    check(bool(r['owner']) and bool(r['boundary_status']) and bool(r['recommendation']),f"inherited outcome {r['key']}")
for r in load(OUT/'reopening-map.json'):
    check((ROOT/r['path']).is_file(),f"reopening path exists {r['path']}")
    check(all(r[k] for k in ['section','outcome','reason','trigger','owner']),f"explicit unchanged/reopen outcome {r['path']}")
sources=load(RUN/'investigation/B6/sources/final-manifest.json')['sources']
for s in sources:
    if s.get('retained_path') and s.get('name') in ['scp03-1.1.2.pdf','scp11-public-review-1.3.0.13.pdf','yubico-profile.html']:
        check(sha((ROOT/s['retained_path']).read_bytes())==s['sha256'],f"original cache identity {s['name']}")
mds=list(OUT.glob('*.md'))+[RUN/'boundary-SecurityDomain.md']
links=0
for p in mds:
    text=p.read_text()
    check(sum(line.startswith('```') for line in text.splitlines())%2==0,f"complete fences {p.name}")
    for target in re.findall(r'\]\(([^)]+)\)',text):
        if target.startswith(('https:','http:','#')): continue
        links+=1
        resolved=(p.parent/target.split('#')[0]).resolve()
        # This command creates its own receipt below; check other linked files now.
        check(resolved==(OUT/'verification.json').resolve() or resolved.exists(),
              f"local link {p.name}:{target}")
summary=load(OUT/'summary.json')
check(summary['source_pin']==PIN and summary['unread']==0 and summary['accepted_core_ids']==0,'scope summary')
check(summary['actual_lines_read']==5607 and summary['assigned_ids']==193 and summary['disposition_paths']==36,'fixed packet totals')
check(summary['dispositions']==dict(collections.Counter(u['status'] for u in units)),'summary count join')
result=dict(status='PASS' if not failures else 'FAIL',failures=failures,source_pin=PIN,
    observed_metadata_head=git('rev-parse','HEAD').decode().strip(),assigned_ids=len(units),
    source_files=len(files),line_count=summary['actual_lines_read'],local_links_checked=links,
    clean_nominees=len(load(OUT/'clean-nominees.json')),inherited_keys=len(keys),local_candidates=len(candidates),
    product_comparison='All assigned files equal pin after explicit BOM/newline normalization; raw identities preserved separately',
    independent_semantic_review='NOT PERFORMED; checker is same investigator, mechanically separate script',
    sdk_builds=0,sdk_tests=0,crypto_native_device_executions=0,
    write_scope='Only boundaries/SecurityDomain/* and boundary-SecurityDomain.md. No global preservation scan or acceptance of other workers claimed.')
(OUT/'verification.json').write_text(json.dumps(result,indent=2)+'\n')
check((OUT/'verification.json').is_file(),'verification self-output exists after publication')
print(json.dumps(result,indent=2))
raise SystemExit(bool(failures))
