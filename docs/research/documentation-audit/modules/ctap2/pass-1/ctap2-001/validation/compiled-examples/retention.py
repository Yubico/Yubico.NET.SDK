"""Prune only verified owned archive duplicates; explicit overlay, no .NET execution.

Original failure manifests/final compiler evidence stay immutable. --apply copies and
verifies every omitted file before unlinking any archived duplicate. --verify resolves
every original manifest record, including externally retained generated artifacts.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
EXTERNAL = Path('/tmp/opencode/ctap2-001-omitted-build-artifacts')
OVERLAY = HERE/'retention-overlay.json'
MANIFESTS = [HERE/'failure-using-hoist/archive-manifest.json',
             HERE/'failure-using-hoist/workspace-manifest.json',
             HERE/'failure-xunit-package-content/archive-manifest.json']


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def inventory():
    files = [p for p in RUN.rglob('*') if p.is_file()]
    return dict(files=len(files),bytes=sum(p.stat().st_size for p in files))


def disposition(source, data):
    extension = Path(source).suffix.lower()
    if extension == '.nupkg':
        assert Path(source).name == 'xunit.v3.core.3.0.0.nupkg'
        return 'retained-primary-package', 'PRIMARY-PACKAGE: pinned original NuGet package; offline source/targets byte proof, not generated build output'
    if extension in {'.dll','.pdb','.so','.dylib','.cache','.up2date'} or Path(source).name == 'apphost' or data.startswith(b'\x7fELF'):
        return 'externalized-generated-artifact', 'Generated compiled/native/apphost/PDB/cache or up-to-date marker duplicate; retained outside Git with exact path/hash/size, not required durable text evidence'
    text = data.decode('utf-8')
    assert '\0' not in text, f'Unclassified binary: {source}'
    return 'retained-text', 'Meaningful original text/metadata/source/input/provenance; historical archive path, not active build output'


def original_records():
    records, manifests = [], []
    for path in MANIFESTS:
        manifests.append(dict(path=str(path.relative_to(RUN)),sha256=sha(path)))
        for entry in json.loads(path.read_text())['files']:
            archived = Path(entry['archived'])
            assert archived.is_relative_to(HERE) and archived.is_file()
            assert sha(archived) == entry['sha256'], f'Original archive mismatch before moving: {archived}'
            data = archived.read_bytes()
            kind, reason = disposition(entry['source'],data)
            relative = str(archived.relative_to(RUN))
            current = EXTERNAL/relative if kind == 'externalized-generated-artifact' else archived
            records.append(dict(original_manifest=str(path.relative_to(RUN)),original_archived=relative,
                                source_provenance=entry['source'],sha256=entry['sha256'],size_bytes=len(data),
                                disposition=kind,reason=reason,current_location=str(current)))
    assert len(records) == 240 and len({r['original_archived'] for r in records}) == 240
    assert sum(r['disposition']=='retained-primary-package' for r in records) == 1
    return records,manifests


def protected_inputs():
    paths = [HERE/'extract.py',HERE/'selection.py',HERE/'manifest.json',HERE/'verify-inputs.py',
             HERE/'collect-unit-package-source.py',HERE/'context-runs/final-result.json']
    manifest = json.loads((HERE/'manifest.json').read_text())
    paths += [HERE/g[key] for g in manifest['groups'] for key in ('source','raw_source')]
    paths += [p for p in (RUN/'validation/checker-source/context').rglob('*') if p.is_file()]
    paths += [p for p in (HERE/'context-runs/after-using-hoist').rglob('*') if p.is_file()]
    return [dict(path=str(p.relative_to(RUN)),sha256=sha(p),size_bytes=p.stat().st_size) for p in sorted(set(paths))]


def totals(records):
    return {kind:dict(files=sum(r['disposition']==kind for r in records),
                      bytes=sum(r['size_bytes'] for r in records if r['disposition']==kind))
            for kind in ('retained-text','retained-primary-package','externalized-generated-artifact')}


def apply():
    assert not OVERLAY.exists() and not EXTERNAL.exists(), 'One-shot pruning; never overwrite evidence/external artifacts'
    records,manifests = original_records()
    protected = protected_inputs()
    before = inventory()
    # Copy and verify ALL omitted files first. No archive unlink happens in this phase.
    omitted = [r for r in records if r['disposition']=='externalized-generated-artifact']
    for record in omitted:
        original = RUN/record['original_archived']
        destination = Path(record['current_location'])
        assert destination.is_relative_to(EXTERNAL)
        destination.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(original,destination)
        assert sha(destination) == record['sha256'] and destination.stat().st_size == record['size_bytes']
    # Reverify the entire original set and every destination before deleting any copy.
    for record in records:
        original = RUN/record['original_archived']
        assert sha(original) == record['sha256'] and original.stat().st_size == record['size_bytes']
        destination = Path(record['current_location'])
        assert sha(destination) == record['sha256'] and destination.stat().st_size == record['size_bytes']
    overlay = dict(schema_version=1,status='Retention overlay only; original failure manifests and final six PASS evidence unchanged',
                   run_root=str(RUN),external_root=str(EXTERNAL),before_run_inventory=before,
                   original_set=dict(files=len(records),bytes=sum(r['size_bytes'] for r in records)),
                   original_manifests=manifests,records=records,totals=totals(records),
                   retained_text_relocations=[],protected_inputs=protected,
                   copy_verification='Entire original set and every copied destination hash/size verified before first unlink',
                   history='Full workspace originally archived in repository; generated duplicates now explicitly externalized. Historical obj/bin text is not active build output.')
    OVERLAY.write_text(json.dumps(overlay,indent=2)+'\n')
    for record in omitted:
        (RUN/record['original_archived']).unlink()
    print(json.dumps(verify(),indent=2))


def resolve(original, overlay):
    original = Path(original)
    relative = str(original.relative_to(RUN))
    rows = [r for r in overlay['records'] if r['original_archived']==relative]
    assert len(rows) == 1, f'Missing/ambiguous explicit retention disposition: {relative}'
    return Path(rows[0]['current_location'])


def verify():
    overlay = json.loads(OVERLAY.read_text())
    assert overlay['run_root'] == str(RUN) and overlay['external_root'] == str(EXTERNAL)
    indexed = {r['original_archived']:r for r in overlay['records']}
    assert len(indexed) == len(overlay['records']) == 240
    expected = set()
    manifest_hashes = {m['path']:m['sha256'] for m in overlay['original_manifests']}
    for path in MANIFESTS:
        relative = str(path.relative_to(RUN))
        assert sha(path) == manifest_hashes[relative], 'Original manifest mutated'
        for entry in json.loads(path.read_text())['files']:
            archived = Path(entry['archived'])
            key = str(archived.relative_to(RUN))
            expected.add(key)
            row = indexed[key]
            assert row['original_manifest']==relative and row['source_provenance']==entry['source'] and row['sha256']==entry['sha256']
            current = resolve(archived,overlay)
            assert current.is_file() and sha(current)==entry['sha256'] and current.stat().st_size==row['size_bytes']
            kind,reason = disposition(entry['source'],current.read_bytes())
            assert row['disposition']==kind and row['reason']==reason
            if kind=='externalized-generated-artifact':
                assert current==EXTERNAL/key and not archived.exists()
            else:
                assert current==archived
    assert expected==set(indexed), 'Unexplained missing/extra omission or disposition'
    assert totals(overlay['records'])==overlay['totals']
    assert not overlay['retained_text_relocations']
    for row in overlay['protected_inputs']:
        p=RUN/row['path']
        assert p.is_file() and sha(p)==row['sha256'] and p.stat().st_size==row['size_bytes'], f'Final compiler/body/host evidence changed: {p}'
    packages=[r for r in overlay['records'] if r['disposition']=='retained-primary-package']
    assert len(packages)==1 and packages[0]['size_bytes']==645594
    # Offline primary-package verification needs no NuGet cache or build runtime.
    source=next(r for r in overlay['records'] if Path(r['source_provenance']).name=='DefaultRunnerReporters.cs')
    targets=next(r for r in overlay['records'] if Path(r['source_provenance']).name=='xunit.v3.core.targets')
    with zipfile.ZipFile(packages[0]['current_location']) as package:
        assert package.read('_content/DefaultRunnerReporters.cs')==Path(source['current_location']).read_bytes()
        assert package.read('buildTransitive/xunit.v3.core.targets')==Path(targets['current_location']).read_bytes()
    nontext=[]
    for path in RUN.rglob('*'):
        if not path.is_file():continue
        data=path.read_bytes()
        try:text=data.decode('utf-8');is_text='\0' not in text
        except UnicodeDecodeError:is_text=False
        if not is_text:nontext.append(str(path))
    assert nontext==[packages[0]['current_location']], f'Unexpected durable nontext artifacts: {nontext}'
    assert not list((HERE/'failure-using-hoist').rglob('*.csproj')) and not list((HERE/'failure-xunit-package-content').rglob('*.csproj'))
    return dict(status='PASS: all 240 original archive records resolve/hash-match; every omission explicit; six PASS/compiler/body snapshots unchanged',
                original_set=overlay['original_set'],totals=overlay['totals'],protected_files=len(overlay['protected_inputs']),
                retained_text_relocations=0,offline_primary_package_proof='PASS',only_nontext_file=packages[0]['original_archived'],
                current_run_inventory=inventory(),external_root=str(EXTERNAL),no_build_test_hardware_execution=True)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    modes=parser.add_mutually_exclusive_group(required=True)
    modes.add_argument('--apply',action='store_true')
    modes.add_argument('--verify',action='store_true')
    modes.add_argument('--plan',action='store_true')
    args=parser.parse_args()
    if args.apply:apply()
    elif args.verify:print(json.dumps(verify(),indent=2))
    else:
        records,_=original_records()
        print(json.dumps(dict(original_files=len(records),totals=totals(records)),indent=2))


if __name__=='__main__':
    main()
