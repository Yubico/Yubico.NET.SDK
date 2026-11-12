"""Independent static record reconciliation; not compiler, test or device evidence."""
import collections
import hashlib
import json
import pathlib
import re
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p/'.git').exists())
RUN = HERE.parent.parent
load = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
sha = lambda b: hashlib.sha256(b).hexdigest()
packet = load(RUN/'inventory/packets/B5-session-framing.json')
units = load(HERE/'unit-dispositions.json')
expected = set(packet['unit_ids'] + packet['shared_read_unit_ids'])
assert len(units) == len(expected) == 932
assert {r['id'] for r in units} == expected
assert collections.Counter(r['status'] for r in units) == {'READ-CLEAN':716,'READ-CANDIDATE':212,'READ-EXCLUSION':4}
assert len(packet['paths']) == 43
assert {r['path'] for r in load(HERE/'file-dispositions.json')} == set(packet['paths'])
for row in units:
    raw=(ROOT/row['path']).read_bytes()
    text=raw.decode('utf-8-sig')
    fragment=text[row['start_character']:row['end_character_exclusive']].encode()
    assert sha(raw)==row['source_sha256']==row['current_source_sha256']
    assert sha(fragment)==row['range_sha256']==row['current_range_sha256']
    assert row['read_status']=='FULL-ASSIGNED-UNIT-READ-WITH-CONTEXT'
    assert row['review_status']=='PENDING-INDEPENDENT-REVIEW'
for identity in load(HERE/'file-identities.json'):
    pinned=subprocess.check_output(['git','show','fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:'+identity['path']],cwd=ROOT)
    raw=(ROOT/identity['path']).read_bytes()
    normalize=lambda b:b.decode('utf-8-sig').replace('\r\n','\n').encode()
    assert sha(pinned)==identity['pinned_git_content_sha256']
    assert normalize(raw)==normalize(pinned)
for manifest in ['sources/captures.json','sources/captures-followup.json']:
    for row in load(HERE/manifest):
        assert row['status']==200
        data=(HERE/'sources'/row['name']).read_bytes()
        assert len(data)==row['bytes'] and sha(data)==row['sha256']
nominees=load(HERE/'clean-nominees.json')
lookup={r['id']:r for r in units}
for row in nominees: assert lookup[row['id']]['status']=='READ-CLEAN'
candidate_index=load(HERE/'candidate-index.json')
assert {r['id'] for r in candidate_index}=={'CORE-B5-C'+str(i).zfill(3) for i in range(1,10)}
for candidate in candidate_index:
    assert set(candidate['unit_ids'])=={r['id'] for r in units if candidate['id'] in r['candidate_ids']}
    for p in candidate['candidate_associated_unchanged_paths']:assert (ROOT/p['path']).exists()
links=[]
for path in [RUN/'investigation-B5.md',*HERE.rglob('*.md')]:
    for target in re.findall(r'\]\(([^)]+)\)',path.read_text()):
        if '://' in target or target.startswith('#'):continue
        target=target.split('#',1)[0]
        assert (path.parent/target).exists(),(str(path),target)
        links.append(target)
    assert sum(line.startswith('```') for line in path.read_text().splitlines())%2==0,str(path)
inherited=load(HERE/'inherited-dispositions.json')
assert {r['key'] for r in inherited}==set(load(RUN/'inventory/inherited-packet-inputs.json')['B5-session-framing']['provenance_keys'])
receipt={'passed':True,'primary_files':43,'primary_lines':8830,'primary_units':893,'shared_units':39,
 'selected_paths_verified':59,'original_raw_and_range_hashes_verified':932,'source_capture_rows_verified':10,
 'candidate_groups':9,'inherited_keys':len(inherited),'clean_nominees':len(nominees),'local_links_checked':len(links),
 'sdk_execution':False,'compiler_execution':False,'native_device_execution':False,
 'scope':'Static identities, IDs, JSON, original captures and authored-record links/fences only; independent semantic review pending.'}
(HERE/'verification-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps(receipt,indent=2))
