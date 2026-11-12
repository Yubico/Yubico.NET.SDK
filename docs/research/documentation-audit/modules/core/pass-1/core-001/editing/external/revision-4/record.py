"""Bounded R1/R2 successor; existing body extraction only, no product imports or older assemblers."""
import copy
import difflib
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT=Path('/home/rupicola/Code/y/yubikit-core-docs-edit-external')
RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
INITIAL=ROOT/RUN/'editing/external'
R2=INITIAL/'revision-2'
R3=INITIAL/'revision-3'
OUT=INITIAL/'revision-4'
BASE='da66f5d01ba594993974ddd26723e8c300c0a454'
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
os.environ['GIT_OPTIONAL_LOCKS']='0'

def sha(b): return hashlib.sha256(b).hexdigest()
def norm(b): return b.decode('utf-8-sig').replace('\r\n','\n')
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT,stderr=subprocess.PIPE)
def save(name,value): (OUT/name).write_text(json.dumps(value,indent=2,ensure_ascii=False)+'\n')
def identity(p): return dict(path=str(p.relative_to(ROOT)),bytes=p.stat().st_size,sha256=sha(p.read_bytes()))

history=[]
for directory in [INITIAL,R2,R3]:
    entries=json.loads((directory/'output-identities.json').read_text())
    for entry in entries:
        assert sha((ROOT/entry['path']).read_bytes())==entry['sha256'],entry['path']
    history.extend(entries)
    history.append(identity(directory/'output-identities.json'))
reviews=[identity(p) for p in sorted(R3.glob('review*')) if p.is_file()]
assert len(reviews)==2
review_identity=json.loads((R3/'review-identities.json').read_text())
assert sha((ROOT/review_identity['report']['path']).read_bytes())==review_identity['report']['sha256']

readme='src/Tests.Shared/README.md'
allow='src/Tests.Shared/Infrastructure/AllowList.cs'
provider='src/Tests.Shared/Infrastructure/AppSettingsAllowListProvider.cs'
targets={readme,allow,provider}
prior=json.loads((R3/'path-decisions.json').read_text())
rows={f['path']:copy.deepcopy(f) for f in prior}
assert len(rows)==164 and targets<=set(rows)
mapping=json.loads((ROOT/RUN/'triage/full-family/map.json').read_text())
group=next(g for g in mapping['groups'] if g['proposal_id']=='FPT-013')
local=next(l for l in group['local_records'] if l['local_id']=='CORE-A-C013')
shared=json.loads((ROOT/RUN/'inventory/shared-sections.json').read_text())

# Reverse only the two exact reviewed README passages to recover the prior raw bytes.
# Equality with the frozen revision-3 hash establishes the reviewed before image without a patch parser.
replacements=[(
    '- **Safety-first allow list** - hard-fails when no device is explicitly authorized.',
    '- **Allow-list authorization** - exits on empty configuration unless unknown serials are enabled;\n  filters rejected devices and raises `SkipException` at row binding when no authorized fixture remains.'),(
    '- Allow-list violations call `Environment.Exit(-1)` by design.',
    '- An empty configured serial list exits with `Environment.Exit(-1)` unless `AllowUnknownSerials` is\n  enabled. Rejected devices and failed identification reads are filtered; an empty fixture list raises\n  `SkipException` at row binding, rather than exiting the process.')]
before_readme=(ROOT/readme).read_bytes()
for old,new in replacements:
    assert before_readme.count(new.encode())==1
    before_readme=before_readme.replace(new.encode(),old.encode())
assert sha(before_readme)==rows[readme]['current_raw_sha256']
notes={
    readme:'EDIT R1 completion of CORE-A-C013/FPT-013: overview and Known Limitations now distinguish empty-configuration exit with unknown serials disabled, filtered rejected/read-failed views, and empty-fixture SkipException at binding. No authorization policy or executable output change.',
    allow:'EDIT R1 completion of CORE-A-C013/FPT-013: class and constructor XML reflect empty serials AND !AllowUnknownSerials as the only hard-exit condition; IsDeviceAllowed is Boolean. Harness filtering/binding and pre-authorization identification are separate. Attach this formerly generic retained path explicitly to the existing facet.',
    provider:'EDIT R1 completion of CORE-A-C013/FPT-013: XML JSON example replaces unread EnableHardFail with real AllowUnknownSerials=false and explains actual keys/serial-less scope. Runtime error-message raw string still advertises EnableHardFail and is an explicit non-D output handoff. No configuration value, runtime literal or constructor changed.'}
anchors={readme:['Reviewed README9,333-334; current corrected overview/Known Limitations',
    'YubiKeyTestInfrastructure.cs:203-206,216-260,263-290; YubiKeyTestState.cs:315-334'],
    allow:['AllowList.cs reviewed25-27,44-45; constructor47-66 condition55; Boolean predicate84-85'],
    provider:['AppSettingsAllowListProvider.cs reviewed24-32; keys37-39; GetList58-73; AllowUnknownSerials76-91; protected runtime example98-134']}
checks=[]
delta=[]
for path,row in rows.items():
    current=(ROOT/path).read_bytes()
    if path not in targets:
        assert sha(current)==row['current_raw_sha256'],('unexpected product change',path)
        continue
    before=before_readme if path==readme else git('show',BASE+':'+path)
    assert sha(before)==row['current_raw_sha256'],('reviewed before mismatch',path)
    if path.endswith('.cs'):
        def protected(b): return [l for l in norm(b).splitlines() if not l.lstrip().startswith('//')]
        assert protected(before)==protected(current),('executable change',path)
        count=0
        for block in re.findall(r'(?:^[ \t]*///[^\n]*\n?)+',norm(current),flags=re.M):
            ET.fromstring('<root>'+re.sub(r'^[ \t]*/// ?','',block,flags=re.M)+'</root>')
            count+=1
        checks.append(dict(path=path,protected_lines='equal including raw-string contents and directives/imports/branches/assertions',xml_blocks=count))
    else:
        assert sum(1 for l in norm(current).splitlines() if l.startswith('```'))%2==0
        checks.append(dict(path=path,markdown='only the two exact R1 reviewed passages changed; before raw hash verified'))
    delta.extend(difflib.unified_diff(norm(before).splitlines(True),norm(current).splitlines(True),fromfile='a/'+path,tofile='b/'+path))
    prior_identity=dict(decision=row['decision'],reason=row['reason'],current_raw_sha256=row['current_raw_sha256'])
    row.update(decision='EDIT',reason=notes[path],review_correction='revision-3 review R1',
        revision4_prior_disposition=prior_identity,revision4_exact_anchors=anchors[path],
        current_raw_sha256=sha(current),current_lf_sha256=sha(norm(current).encode()),
        current_final_newline=current.endswith(b'\n'),current_utf8_bom=current.startswith(b'\xef\xbb\xbf'))
    if 'base_current_lf_equal' in row: row['base_current_lf_equal']=norm(git('show',BASE+':'+path))==norm(current)
    if not any(c.get('local_id')=='CORE-A-C013' for c in row['candidates']):
        row['candidates'].append(dict(local_id='CORE-A-C013',proposal_id='FPT-013',facets=group['facets'],
            relation='R1 explicit retained-path completion, not a claim that the original index named this path',
            inherited_association_keys=group['inherited_association_keys'],original_definition=local['original_definition'],
            source_anchors=anchors[path],original_shared_units=[u for u in shared if u['path']==path]))

# Reuse the initial receipt's exact XML-code extraction expression; only the named bodies are checked.
def xml_bodies(content):
    return [re.sub(r'^\s*/// ?','',m[1],flags=re.M) for m in re.finditer(r'<code(?:\s[^>]*)?>(.*?)</code>',content,re.S)]
inv=json.loads((R2/'example-bodies.json').read_text())
initial_examples=copy.deepcopy(inv['initial_bodies_preserved'])
overlay=[]
new_provider=xml_bodies(norm((ROOT/provider).read_bytes()))
assert len(new_provider)==1
assert json.loads(new_provider[0])['YubiKeyTests']['AllowUnknownSerials'] is False
assert set(json.loads(new_provider[0])['YubiKeyTests'])=={'AllowedSerialNumbers','AllowUnknownSerials'}
for e in initial_examples:
    path=e['path'];body=e['body'];old=copy.deepcopy(e)
    assert sha(body.encode())==e['body_sha256']
    correction=None
    if path==provider:
        assert e['body_sha256']=='f44d4bb4843f4f6f73c6aa90a40ed69d8bbedaf1da0a26b1fd1276ef05dee888'
        assert body in xml_bodies(norm(git('show',BASE+':'+path)))
        correction='JSON configuration illustration, not C# or a C# compilation participant; old EnableHardFail setting is unsupported at the audited source pin.'
        e.update(language='json',classification=correction,compiler_status='NOT-APPLICABLE (configuration illustration); no configuration loaded',csharp_compilation_participant=False,
            resources='Configuration illustration only: no resource acquisition, C# compilation participant or actual configuration load.')
        if e['version']=='current':
            e.update(body=new_provider[0],body_sha256=sha(new_provider[0].encode()),classification='Current JSON configuration illustration using actual provider keys; not C# or a compilation participant',first_line=25,last_line=32)
    elif path=='src/Tests.Shared/Infrastructure/TestCategories.cs':
        assert e['body_sha256']=='5a25bdc655b19f6637001274cb1de027ee0aca3bff6f1e77eb4797cb5419a4d7'
        assert body in xml_bodies(norm((ROOT/path).read_bytes()))
        correction='Shell command fragments embedded in XML documentation, not C# code; no command executed.'
        e.update(language='sh (XML-embedded)',classification=correction,csharp_compilation_participant=False,
            compiler_status='NOT-APPLICABLE (shell recipes); commands NOT-RUN',
            resources='Command fragments, not C# resource scopes. Retain documented command prerequisites; no command or test executed.',
            xml_character_reference_provenance=dict(raw_body_sha256=e['body_sha256'],raw_body=body,
                rendered_body=body.replace('&amp;','&'),rendered_body_sha256=sha(body.replace('&amp;','&').encode()),
                transformation='Only the literal XML &amp; references are expanded to shell &; raw extracted XML body/hash retained, not a new parser or executed shell rendering.'))
    elif path=='src/Tests.Shared/CLAUDE.md' and 'public sealed class AllowList' in body:
        assert e['body_sha256'] in ['96d97d285b01486d97a7ce0e752b83f616ca98a7e926cb3631594edd31094813','4a89a27ab23bc7e2123eb74ffd192133757490d18bb829dbf1d0767862041126']
        if e['version']=='current': assert body in norm((ROOT/path).read_bytes())
        correction='Abbreviated AllowList implementation sketch, not a complete replacement; actual AllowUnknownSerials property/logging and complete implementation remain in source. Current CLAUDE32-33 explicitly marks this section as a sketch.'
        e.update(classification=correction,csharp_compilation_participant=False,compiler_status='NOT-RUN; implementation sketch, not complete replacement',
            resources='Abbreviated illustration omits actual property/logging/complete implementation; do not use as a replacement AllowList implementation.')
    elif path=='src/Tests.Shared/IYubiKeyFilter.cs' and e['version']=='base':
        assert e['body_sha256']=='ae523949c5098862ac6b754577eee04dd0731485e5a39cd027f9c08dbb6e93a5'
        assert body in xml_bodies(norm(git('show',BASE+':'+path)))
        correction='Superseded non-current invalid API example at the audited source pin: YubiKeyTheory/YubiKeyTestDevice do not match current APIs. Repaired by the current WithYubiKey/YubiKeyTestState body; not a usable context-dependent fragment.'
        e.update(classification=correction,csharp_compilation_participant=False,compiler_status='NOT-RUN; source-backed obsolete API classification, not observed compiler output',
            resources='Historical invalid-for-current-API body, not a usable fragment. Current corrected test/helper body retains its separate source-backed context and unrun compilation status.')
    if correction:
        ids=['CORE-A-C013','FPT-013'] if path!= 'src/Tests.Shared/Infrastructure/TestCategories.cs' else ['CORE-A-C019','FPT-019','CORE-B4-C008','FPT-055']
        overlay.append(dict(key=dict(path=path,version=old['version'],body_sha256=old['body_sha256']),review='R2',ids=ids,
            immutable_original=old,correction=correction,successor_entry=e,
            prior_current_body_superseded=path==provider and old['version']=='current'))
assert len(overlay)==7
assert len(initial_examples)==len(inv['initial_bodies_preserved'])
save('example-classification-overlay.json',dict(immutable_inventory=identity(R2/'example-bodies.json'),updates=overlay,
    rule='Apply by exact path/version/body hash. Only the prior provider current entry is replaced by the new current JSON body; every original body remains retained in the overlay and frozen inventory.'))
save('example-inventory.json',dict(initial_bodies=initial_examples,retained_primary_bodies=inv['added_primary_bodies'],
    total_body_records=len(initial_examples)+len(inv['added_primary_bodies']),new_csharp_example_bodies=0,
    changed_current_configuration_bodies=1,classification_overlay=identity(OUT/'example-classification-overlay.json'),
    compilation_and_execution='No C# example compiled, shell command executed or configuration loaded.'))

previous_products=set(json.loads((R3/'static-preservation.json').read_text())['final_product_paths'])
product=sorted(p for p in git('diff','--name-only',BASE).decode().splitlines() if not p.startswith(str(RUN)+'/'))
assert set(product)==previous_products|targets
assert len(product)==37 and len(previous_products)==35 and len(targets-previous_products)==2
allowlist=json.loads((R3/'allowlist.json').read_text())
assert targets<=set(allowlist['full_final_allowlist'])
assert git('rev-parse','HEAD').decode().strip()==BASE
patch=git('diff','--binary','--src-prefix=a/','--dst-prefix=b/',BASE,'--',*product)
(OUT/'final-successor-source.patch').write_bytes(patch)
(OUT/'R1-comment-delta.patch').write_text(''.join(delta))
subprocess.run(['git','--no-optional-locks','diff','--check'],cwd=ROOT,check=True,capture_output=True)
save('path-decisions.json',[rows[p] for p in sorted(rows)])
save('allowlist.json',dict(inherited=identity(R3/'allowlist.json'),full_final_allowlist=allowlist['full_final_allowlist'],
    revision4_product_paths=sorted(targets),new_owned_paths=0,base=BASE,source_pin=PIN))
save('candidate-decisions.json',dict(inherited=identity(R3/'candidate-decisions.json'),
    ids=['CORE-A-C013','FPT-013','management-001:MGMT-XREF-003'],
    reason='R1 completes the existing released authorization D facet on previously retained passages; no new family, canonical ID or authorization policy.',
    original_local_record=local,original_inherited_keys=group['inherited_association_keys'],
    current_path_updates=[dict(path=p,decision=rows[p]['decision'],reason=notes[p],source_anchors=anchors[p],candidates=rows[p]['candidates']) for p in sorted(targets)],
    R2_overlay=identity(OUT/'example-classification-overlay.json'),
    review_state='Corrections source-verified by editor; focused independent re-review pending, not full acceptance inherited from the earlier review.'))
referrals=json.loads((R3/'remaining-referrals.json').read_text())
referrals['R1_protected_runtime_output']=dict(path=provider,symbol='OnInvalidInputErrorMessage',reviewed_lines='98-134',
    ids=['CORE-A-C013','FPT-013','management-001:MGMT-XREF-003'],
    reason='Executable raw-string error example still advertises unread EnableHardFail=true; correcting that output is not a D-comment edit and remains deferred. No authorization policy change.',
    protected_source='Exact non-comment-line preservation includes the complete raw string.')
save('remaining-referrals.json',referrals)
save('history-preservation.json',dict(frozen_outputs=history,frozen_reviews=reviews,
    previous_35_product_manifest=identity(R3/'path-decisions.json'),
    unchanged_previous_product_files=sorted(previous_products-targets),accepted_toolchain_comments_unchanged=[p for p in previous_products if p.startswith('scripts/toolchain/')],
    intentional_prior_README_hash_supersession=dict(path=readme,old_sha256=sha(before_readme),new_sha256=sha((ROOT/readme).read_bytes()),review='R1',reason=notes[readme]),
    result='All earlier patches/receipts/review records byte-exact. Current README is the expressly authorized R1 supersession, not a claim that its old raw product hash remains current.'))
save('static-preservation.json',dict(base=BASE,head=BASE,source_pin=PIN,actual_final_product_count=len(product),final_product_paths=product,
    actual_previous_product_count=len(previous_products),revision4_changed_product_paths=sorted(targets),new_diff_paths=sorted(targets-previous_products),
    manifest_paths=len(rows),full_patch_sha256=sha(patch),checks=checks,git_diff_check='PASS',
    R2_corrected_keys=len(overlay),current_example_inventory=identity(OUT/'example-inventory.json'),
    limits='Exact non-comment/XML/Markdown/hash checks only; no build/test/formatter/native/device/crypto/provider execution, Git index/ref mutation or independent re-review. No new parser or consumer harness.'))
print(json.dumps(dict(product_files=len(product),manifest_paths=len(rows),R1_paths=len(targets),R2_overrides=len(overlay),
    example_records=len(initial_examples)+len(inv['added_primary_bodies']),review_records_preserved=len(reviews),patch_sha256=sha(patch)),indent=2))
