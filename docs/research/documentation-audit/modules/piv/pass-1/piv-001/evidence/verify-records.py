"""Verify report inventory and indexes only; no build, test runner or device access."""
import datetime,hashlib,json,pathlib,re
ROOT=pathlib.Path(__file__).parent
RUN=ROOT.parent
c=json.loads((RUN/'coverage.json').read_text())
h=json.loads((ROOT/'hardware-methods.json').read_text())
a=json.loads((ROOT/'api-reconciliation.json').read_text())
errors=[]
for f in c['files']:
 if hashlib.sha256(pathlib.Path(f['path']).read_bytes()).hexdigest()!=f['sha256']:
  errors.append('PIV inventory changed: '+f['path'])
for path in dict.fromkeys(u['path'] for u in c['units']):
 us=[u for u in c['units'] if u['path']==path]
 if us[0]['start']!=1 or us[-1]['end']!=len(pathlib.Path(path).read_text().splitlines()):errors.append('incomplete region partition: '+path)
 if any(left['end']+1!=right['start'] for left,right in zip(us,us[1:])):errors.append('overlap/gap: '+path)
for manifest in ['source-manifest.json','followup-manifest.json']:
 for f in json.loads((ROOT/manifest).read_text()):
  if f['status']=='captured' and hashlib.sha256((ROOT/f['file']).read_bytes()).hexdigest()!=f['sha256']:
   errors.append('external source changed: '+f['file'])
links=[]
for f in c['files']:
 p=pathlib.Path(f['path'])
 if p.suffix=='.md':
  for target in re.findall(r'\]\(([^)]+)\)',p.read_text()):
   if '://' in target or target.startswith('#'):continue
   links.append({'source':str(p),'target':target,'exists':(p.parent/target.split('#')[0]).exists()})
if any(not x['exists'] for x in links):errors.append('missing module Markdown link target')
expected={'types':23,'api_entries':370,'operations':42,'methods':89,'hardware_files':24,'smoke':80,'unattended':86}
actual={'types':a['public_type_count'],'api_entries':a['api_entries'],'operations':a['interface_operations'],
 'methods':len(h),'hardware_files':len(set(x['path'] for x in h)),
 'smoke':sum(x['selected_by_smoke_traits'] for x in h),'unattended':sum(x['selected_by_unattended_traits'] for x in h)}
if actual!=expected:errors.append('count reconciliation mismatch')
if len(set((x['path'],x['method']) for x in h))!=len(h):errors.append('duplicate hardware method')
if not all('reviewed_effects' in x for x in h):errors.append('missing method effect annotation')
if pathlib.Path('src/Piv/tests/CLAUDE.md').exists():errors.append('test instruction assumption changed')
result={'checked_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'status':'pass' if not errors else 'fail',
 'scope':'Record/index/hash/link existence checks only. Investigator ran zero builds/tests/formatters/device operations.',
 'inventory_files':len(c['files']),'module_regions':len(c['units']),'shared_regions':len(c['shared_units']),
 'counts':actual,'sampling':c['sampling'],'module_local_link_existence':links,'errors':errors}
(ROOT/'record-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
if errors:raise SystemExit(1)
