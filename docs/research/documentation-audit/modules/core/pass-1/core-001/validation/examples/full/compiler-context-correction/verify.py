"""Finite third-Csc context oracles and all-300-source preservation checks; never compile."""
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys

HERE=Path(__file__).resolve().parent
FULL=HERE.parent
ROOT=FULL.parents[9]
sys.path.insert(0,str(FULL))
from compiler_imports import key

PARAMETERS={
 'FULL-250':'IYubiKey yubiKey, ProtocolConfiguration customProtocolConfig, CancellationToken cancellationToken',
 'FULL-263':'IManagementSession session, CancellationToken cancellationToken',
 'FULL-268':'ISmartCardConnection connection',
 'FULL-279':'IManagementSession session, CancellationToken ct',
 'FULL-281':'IManagementSession session, Func<byte[]?> GetCurrentCode, Func<byte[]> GetNewCode, CancellationToken ct',
 'FULL-329':'IYubiKey device, byte[] accessCode',
}
IMPORTS={
 'FULL-163':'Yubico.YubiKit.Core.Protocols.Otp.Hid',
 'FULL-209':'Yubico.YubiKit.Core.Protocols.Fido.Hid',
 'FULL-232':'Yubico.YubiKit.Core.Protocols.Fido.Hid',
 'FULL-250':'Yubico.YubiKit.Core.Protocols.SmartCard.Apdu',
 'FULL-411':'Yubico.YubiKit.Management',
}
WATCH=('session','ct','cancellationToken','connection','customProtocolConfig','accessCode','GetCurrentCode','GetNewCode')


def sha(raw):return hashlib.sha256(raw).hexdigest()


def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module


def context_oracles():
    generator=load('third_context_generator',FULL/'contexts.py')
    inventory={row['id']:row for row in json.loads((FULL/'final-current-inventory.json').read_bytes())['entries']}
    failures=[]
    for id in sorted(set(PARAMETERS)|set(IMPORTS)|{'FULL-036'}):
        value=generator.select(dict(inventory[id]),inventory)
        if id in PARAMETERS and value['typed_parameters']!=PARAMETERS[id]:failures.append(id+': wrong borrowed parameter contract')
        if id in IMPORTS and 'using '+IMPORTS[id]+';\n' not in value['prefix']:failures.append(id+': missing genuine namespace')
        if id in ('FULL-263','FULL-268','FULL-279') and value['mode']!='async-statements':failures.append(id+': await in sync wrapper')
        if id=='FULL-036' and 'using Yubico.YubiKit.Ctap2.Extensions;\n' in value['prefix']:failures.append(id+': ambiguous added CTAP2 namespace')
    assert not failures,'; '.join(failures)
    return sorted(set(PARAMETERS)|set(IMPORTS)|{'FULL-036'})


def scan(row,data,code):
    body=data.get('body',row['decoded_body'])
    # Finite lexical checks for this failure family, not a C# parser or binding proof.
    lexical=re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"','',body)
    mode=data.get('mode','original-member-or-test-context')
    parameters=data.get('typed_parameters','')
    signature=re.search(r'(?m)^.*\b(async Task|void) CompileOnly\(([^\n]*)\)\n\{\n$',data['prefix'])
    if signature and mode=='original-member-or-test-context':
        mode='async-statements' if signature[1]=='async Task' else 'sync-statements'
        parameters=signature[2]
    statement=mode in ('sync-statements','async-statements','hosted-demo','return-client')
    watched={}
    for name in WATCH:
        if not re.search(r'(?<![\w.])'+name+r'\b(?!\s*:)',lexical):continue
        if re.search(r'\b'+name+r'\b',parameters):binding='typed wrapper parameter'
        elif re.search(r'\b(?:var|[A-Z]\w*(?:<[^;\n]+?>)?|byte\[\]\??)\s+'+name+r'\b',lexical):binding='original local or member/parameter declaration'
        elif not statement:binding='original member/test scope; not a generated statement parameter'
        else:raise AssertionError(row['id']+': unresolved watched prerequisite '+name)
        watched[name]=binding
    await_present=bool(re.search(r'\bawait\b',lexical))
    assert not (statement and mode=='sync-statements' and await_present),row['id']
    preview_names=sorted(set(re.findall(r'\bPreviewSign(?:RegistrationInput|AuthenticationInput|SigningParams)\b',lexical)))
    dual='using Yubico.YubiKit.Ctap2.Extensions;\n' in data['prefix'] and 'using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;\n' in data['prefix']
    assert not (preview_names and dual),row['id']
    for name,namespace in [('ChecksumUtils','Yubico.YubiKit.Core.Protocols.Otp.Hid'),('IFidoHidConnection','Yubico.YubiKit.Core.Protocols.Fido.Hid')]:
        if re.search(r'(?<![\w.])'+name+r'\b',lexical):assert 'using '+namespace+';' in code,row['id']
    delegates=re.findall(r'Func<byte\[\](\?)?>\s+(\w+)',data['prefix'])
    if 'GetNewCode' in watched:assert ('','GetNewCode') in delegates and ('?','GetNewCode') not in delegates
    nullable_locals=re.findall(r'\bbyte\[\]\?\s+(\w+)',lexical)
    guards={name:bool(re.search(r'\b'+name+r'\s+is\s+(?:not\s+)?null',lexical)) for name in nullable_locals}
    return dict(id=row['id'],path=row['path'],mode=mode,await_present=await_present,
                async_classification=('async statement wrapper' if statement and mode=='async-statements' else 'synchronous statement wrapper' if statement and mode=='sync-statements' else 'original enclosing members or specialized wrapper'),
                watched_prerequisites=watched,byte_array_delegate_contracts=delegates,
                original_nullable_byte_array_locals=nullable_locals,original_null_guard_presence=guards,
                preview_sign_short_types=preview_names,competing_preview_sign_imports=dual,
                qualification='Finite lexical fault-family classification, not general free-name/nullability/type binding or Csc acceptance.')


def main():
    corrected=context_oracles()
    preservation=json.loads((HERE/'preservation.json').read_bytes())
    retained=[preservation['profile'],preservation['generated_sources_snapshot']]+preservation['failed_attempt_records']+[pair['snapshot'] for pair in preservation['participants']]+preservation['prior_correction_records']
    for identity in retained:
        raw=Path(identity['path']).read_bytes();assert len(raw)==identity['bytes'] and sha(raw)==identity['sha256'],identity['path']
    snapshot=json.loads((HERE/'generated-sources.before.json.snapshot').read_bytes());saved={row['path']:row for row in snapshot['files']};assert len(saved)==300
    old=json.loads((FULL/'manifest-imports-fixed.json').read_bytes());current=json.loads((FULL/'manifest-contexts-fixed.json').read_bytes());old_rows={row['id']:row for row in old['records']}
    namespaces={match[1] for path in (ROOT/'src').rglob('*.cs') if not {'bin','obj'}.intersection(path.parts) for match in re.finditer(r'^\s*namespace\s+([A-Za-z0-9_.]+)\s*[;{]',path.read_text(),re.M)}
    checked=set();changed=[];scans=[]
    for row in current['records']:
        prior=old_rows[row['id']]
        assert {k:v for k,v in row.items() if k not in ('context','generated_source')}=={k:v for k,v in prior.items() if k not in ('context','generated_source')},row['id']
        if row.get('compilation_status')!='pending-canonical-build':continue
        consumer=row['disposition']=='prepared-consumer-body';data=row['context'] if consumer else row['compiled'];previous=prior['context'] if consumer else prior['compiled']
        if data.get('compilation_alias_of') is not None:continue
        path=ROOT/row['generated_source']['path'] if consumer else FULL/data.get('generated_path',data.get('path'));baseline=saved[str(path)];oldcode=baseline['content'].encode();assert sha(oldcode)==baseline['sha256'];code=path.read_bytes()
        if consumer:
            mutable={'prefix','mode','typed_parameters','context_key'}
            assert {k:v for k,v in data.items() if k not in mutable}=={k:v for k,v in previous.items() if k not in mutable}
            assert code==data['prefix'].encode()+oldcode[len(previous['prefix'].encode()):]
            assert row['generated_source']['path']==prior['generated_source']['path']
            assert row['generated_source']['bytes']==len(code) and row['generated_source']['sha256']==sha(code)
        else:assert code==oldcode
        original=previous.get('original_import_prefix',previous.get('preserved_import_prefix',''))
        if original:assert original in data['prefix']
        directives=[key(line) for line in code.decode().splitlines() if key(line)]
        assert len(directives)==len(set(directives)),row['id']
        assert all(static or not name.startswith('Yubico.YubiKit.') or name in namespaces for static,name in directives),row['id']
        checked.add(str(path));scans.append(scan(row,data,code.decode()))
        if code!=oldcode:changed.append(row['id'])
    assert checked==set(saved) and sorted(changed)==corrected
    actual=json.loads((HERE/'actual-diagnostics.json').read_bytes());identity=actual['source_log'];raw=Path(identity['path']).read_bytes();assert sha(raw)==identity['sha256']
    pattern=re.compile(r'(?P<path>[^\r\n]+?\.cs)\((?P<line>\d+),(?P<column>\d+)\): error (?P<code>\w+): (?P<message>.*?) \[')
    emitted={tuple(match.groupdict()[k].strip() for k in ('path','line','column','code','message')) for match in pattern.finditer(raw.decode())}
    assert len(emitted)==21 and emitted=={tuple(row[k] for k in ('path','line','column','code','message')) for row in actual['unique_diagnostics']}
    triage=json.loads((HERE/'diagnostic-triage.json').read_bytes());assert len(triage['diagnostics'])==21 and not triage['accepted_body_defects']
    assert emitted=={tuple(row[k] for k in ('path','line','column','code','message')) for row in triage['diagnostics']}
    evidence=json.loads((HERE/'context-evidence.json').read_bytes());evidence_ids={row['id'] for row in evidence['sources']}
    for row in triage['diagnostics']:
        assert row['classification']=='generated-context' and row['evidence_ids']
        assert set(row['evidence_ids'])<=evidence_ids
        assert saved[row['path']]['content'].splitlines()[int(row['line'])-1]==row['generated_line_text']
    for row in evidence['sources']:
        source=(ROOT/row['path']).read_bytes();assert sha(source)==row['sha256']
        assert source.decode().splitlines()[row['first_line']-1:row['last_line']]==row['lines']
    return dict(scope='Static finite remaining-context correction only; no compilation, capture replay or execution.',
                source_count=len(checked),consumer_sources=244,test_sources=56,changed_sources=changed,
                original_bodies_directives_and_nonprefix_remainders_exact=True,all_300_fault_family_classifications=scans,
                emitted_diagnostics=21,accepted_body_defects=[],failed_attempt_records_preserved=len(preservation['failed_attempt_records']),
                qualification='No general C# binding proof. Later compiler/body diagnostics remain possible and belong to the coordinator/editor.')


if __name__=='__main__':
    if '--context-oracles-only' in sys.argv:print(json.dumps(context_oracles(),indent=2))
    else:print(json.dumps(main(),indent=2))
