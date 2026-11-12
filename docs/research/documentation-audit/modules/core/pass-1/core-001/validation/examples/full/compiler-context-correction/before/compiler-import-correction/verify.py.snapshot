"""All-source import-origin preservation and generated-context diagnostic regression checks."""
import json
from pathlib import Path
import re
import sys

HERE=Path(__file__).resolve().parent
FULL=HERE.parent
ROOT=FULL.parents[9]
sys.path.insert(0,str(FULL))
from compiler_imports import compose,correct_prefix,key


def sha(raw):return __import__('hashlib').sha256(raw).hexdigest()


def main():
    original='    using System;\nusing static System.Math;\n'
    added='using System;\nusing System.Text;\nusing System.Text;\nusing Yubico.YubiKit.Ctap2.Ctap;\n'
    assert compose(added,original)=='using System.Text;\n'+original
    assert compose('using System.Math;\nusing static System.Math;\n',original)=='using System.Math;\n'+original
    assert correct_prefix('using System;\n'+original+'\nnamespace Example;\n',original)==original+'\nnamespace Example;\n'
    assert correct_prefix(added+original+'using System.Text;\nnamespace Example;\n',original)=='using System.Text;\n'+original+'namespace Example;\n'
    try:compose('using var owner = Acquire();\n',original)
    except ValueError:resource_rejected=True
    else:raise AssertionError('Resource statement treated as namespace import')
    before=json.loads((HERE/'generated-sources.before.json.snapshot').read_bytes())
    preservation=json.loads((HERE/'preservation.json').read_bytes())
    retained=[preservation['profile'],preservation['generated_sources_snapshot']]+preservation['failed_attempt_records']
    retained += [pair['snapshot'] for pair in preservation['participants']]
    for identity in retained:
        raw=Path(identity['path']).read_bytes()
        assert len(raw)==identity['bytes'] and sha(raw)==identity['sha256'],identity['path']
    assert before['source_count']==len(before['files'])==300
    saved={row['path']:row for row in before['files']}
    old=json.loads((FULL/'manifest-admitted.json').read_bytes())
    current=json.loads((FULL/'manifest-imports-fixed.json').read_bytes())
    old_rows={row['id']:row for row in old['records']}
    namespaces={match[1] for path in (ROOT/'src').rglob('*.cs') if not {'bin','obj'}.intersection(path.parts)
                for match in re.finditer(r'^\s*namespace\s+([A-Za-z0-9_.]+)\s*[;{]',path.read_text(),re.M)}
    checked=set();changed=[]
    for row in current['records']:
        prior=old_rows[row['id']]
        fields={'context','compiled','generated_source'}
        assert {k:v for k,v in row.items() if k not in fields}=={k:v for k,v in prior.items() if k not in fields}
        if row.get('compilation_status')!='pending-canonical-build':continue
        data=row.get('context') if row['disposition']=='prepared-consumer-body' else row['compiled']
        previous=prior.get('context') if row['disposition']=='prepared-consumer-body' else prior['compiled']
        mutable={'prefix','generated_sha256'}
        if data.get('compilation_alias_of') is not None:mutable.add('generated_path')
        assert {k:v for k,v in data.items() if k not in mutable}=={k:v for k,v in previous.items() if k not in mutable},row['id']
        if 'generated_source' in row:
            assert row['generated_source']['path']==prior['generated_source']['path']
        if data.get('compilation_alias_of') is not None:
            owner=next(candidate for candidate in current['records'] if candidate['id']==data['compilation_alias_of'])['compiled']
            assert all(data[k]==owner[k] for k in ('prefix','suffix','generated_sha256'))
            assert data.get('generated_path',data.get('path'))==owner.get('generated_path',owner.get('path'))
            continue
        path=ROOT/row['generated_source']['path'] if row['disposition']=='prepared-consumer-body' else FULL/data.get('generated_path',data.get('path'))
        baseline=saved[str(path)];oldcode=baseline['content'].encode();assert sha(oldcode)==baseline['sha256']
        code=path.read_bytes();raw_original=previous.get('original_import_prefix',previous.get('preserved_import_prefix',''))
        assert data.get('original_import_prefix',data.get('preserved_import_prefix',''))==raw_original
        fixed=correct_prefix(previous['prefix'],raw_original)
        assert data['prefix']==fixed
        assert code==fixed.encode()+oldcode[len(previous['prefix'].encode()):]
        assert code[len(fixed.encode()):]==oldcode[len(previous['prefix'].encode()):]
        if raw_original:assert raw_original.encode() in fixed.encode()
        directives=[key(line) for line in code.decode().splitlines() if key(line)]
        assert len(directives)==len(set(directives)),row['id']
        assert all(static or not name.startswith('Yubico.YubiKit.') or name in namespaces for static,name in directives),row['id']
        checked.add(str(path))
        if code!=oldcode:changed.append(row['id'])
    assert len(checked)==300
    assert checked==set(saved)
    assert len(changed)==82
    actual=json.loads((HERE/'actual-diagnostics.json').read_bytes())
    # Canonical toolchain runs rotate; the qualified logger's retained copy does not.
    log=(FULL.parents[1]/'commands-v2/examples-full-consumer-second/toolchain/20261112-044456-975-build'/Path(actual['source_log']).name).read_bytes()
    assert sha(log)==actual['source_log_sha256']
    pattern=re.compile(r'(?P<path>[^\r\n]+?\.cs)\((?P<line>\d+),(?P<column>\d+)\): error (?P<code>\w+): (?P<message>.*?) \[')
    emitted={tuple(match.groupdict()[k].strip() for k in ('path','line','column','code','message')) for match in pattern.finditer(log.decode())}
    assert emitted=={tuple(row[k] for k in ('path','line','column','code','message')) for row in actual['unique_diagnostics']}
    diagnostics=json.loads((HERE/'diagnostic-triage.json').read_bytes())
    assert diagnostics['unique_diagnostics']==177 and not diagnostics['accepted_body_diagnostics_observed']
    assert diagnostics['root_cause_counts']=={'generated-wrapper-duplicates-added-or-original-directive':146,'generated-nonexistent-blanket-namespace':31}
    assert len(diagnostics['diagnostics'])==177
    for diagnostic in diagnostics['diagnostics']:
        baseline=next(value for path,value in saved.items() if Path(path).stem==diagnostic['id'])
        assert baseline['content'].splitlines()[diagnostic['line']-1]==diagnostic['diagnostic_line']
        assert diagnostic['code'] in ('CS0105','CS0234') and not diagnostic['actual_body_error']
    return dict(scope='Static all-source import/body origin checks only; no Csc/capture/build replay.',
                selected_source_count=300,consumer_source_count=244,test_source_count=56,
                changed_prefix_count=len(changed),changed_ids=changed,
                original_directives_order_content_and_remainders_exact=True,
                duplicate_or_nonexistent_repo_namespace_imports=0,
                namespace_vs_static_type_distinguished=True,resource_statement_rejected_as_import=resource_rejected,
                preserved_failed_attempt_records=len(preservation['failed_attempt_records']),
                observed_diagnostics=177,observed_accepted_body_diagnostics=[],
                qualification='A retry may expose later body/API errors; these static checks establish no compiler success.')


if __name__=='__main__':print(json.dumps(main(),indent=2))
