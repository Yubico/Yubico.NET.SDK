"""Static retained-input/schema/source-body checks only; never prepare/build/run .NET."""
import ast
import json
from pathlib import Path
import runpy
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p/'toolchain.cs').is_file())
RUN = HERE.parent.parent


def main():
    for name in ('extract.py','selection.py','verify-inputs.py'):
        ast.parse((HERE/name).read_text())
    extractor = runpy.run_path(str(HERE/'extract.py'))
    digest = extractor['digest']
    manifest_path = HERE/'manifest.json'
    manifest = json.loads(manifest_path.read_text())
    assert manifest['primary_root'] == manifest['guide_root'] == str(ROOT), 'Final extraction must use PRIMARY for both roots'
    assert manifest['baseline'] == extractor['BASELINE']
    output, regenerated = extractor['generate'](ROOT,ROOT)
    for relative, text in output.items():
        assert (HERE/relative).read_bytes() == text.encode(), f'Stale generated input: {relative}'
    assert regenerated['counts'] == {'module_fences':39,'module_xml':5,'total_blocks':95,'selected_blocks':39,'excluded_blocks':56,'groups':33}
    blocks = {b['id']:b for b in manifest['blocks']}
    for group in manifest['groups']:
        complete = (HERE/group['source']).read_text()
        for extraction in group['extractions']:
            b = blocks[extraction['id']]
            _, body = extractor['relocate_imports'](b['extracted_text'])
            assert body in complete, f'Dropped/rewritten source body: {group["name"]}: {b["id"]}'
            assert digest(b['extracted_text']) == b['extracted_sha256']
            assert digest(b['verbatim_text']) == b['verbatim_sha256']
        assert digest((HERE/group['source']).read_bytes()) == group['source_sha256']
        assert digest((HERE/group['raw_source']).read_bytes()) == group['raw_sha256']
        assert digest(group['extracted_text']) == group['extracted_sha256']
        assert group['provenance'] and group['prerequisites']
    configuration_path = RUN/'validation/checker-source/context/contexts-manifest.json'
    configuration = json.loads(configuration_path.read_text())
    assert configuration['baseline'] == manifest['baseline']
    assert (configuration_path.parent/configuration['extraction_manifest']).resolve() == manifest_path
    assigned = [g for c in configuration['contexts'] for g in c['groups']]
    assert len(assigned) == len(set(assigned)) == len(manifest['groups']) == 33
    packages = {}
    for context in configuration['contexts']:
        pending, visited, names = [context['source_project']], set(), set()
        while pending:
            relative = pending.pop()
            if relative in visited:
                continue
            visited.add(relative)
            p = ROOT/relative
            project = ET.parse(p).getroot()
            assert project.get('Sdk') == 'Microsoft.NET.Sdk'
            names.update(item.get('Include') for item in project.findall('.//PackageReference'))
            for item in project.findall('.//ProjectReference'):
                child = (p.parent/item.get('Include').replace('\\','/')).resolve(strict=True)
                pending.append(str(child.relative_to(ROOT)))
        v2 = 'xunit' in names or 'xunit.core' in names
        v3 = any(n.startswith('xunit.v3') for n in names)
        assert not (v2 and v3), f'Mixed genuine source package closure: {context["id"]}'
        assert (context['kind'] == 'xunit2') == v2
        assert (context['kind'] == 'xunit3') == v3
        assert context['output_type'] in {'Library','Exe'}
        assert not v3 or context['output_type'] == 'Exe'
        assert all(word not in context['id'].lower() for word in ('ctap2','unittests','integrationtests'))
        if v3:
            assert not context['modules'] and not context['references']
            assert not any('Tests.Shared' in p for p in visited)
        packages[context['id']] = sorted(names)
    for entry in manifest['reference_inventory']:
        assert digest((Path(entry['root'])/entry['path']).read_bytes()) == entry['sha256']
    for entry in manifest['source_api_evidence']:
        assert digest((ROOT/entry['path']).read_bytes()) == entry['sha256']
    print(json.dumps(dict(status='STATIC ONLY: exact bodies/hashes/partition/package generation match; NOT compiled',
                          counts=manifest['counts'], contexts=len(configuration['contexts']),
                          source_evidence_paths=len(manifest['source_api_evidence']),
                          reference_paths=len(manifest['reference_inventory']), literal_url_records=len(manifest['external_references']),
                          genuine_package_closure=packages),indent=2))


if __name__ == '__main__':
    main()
