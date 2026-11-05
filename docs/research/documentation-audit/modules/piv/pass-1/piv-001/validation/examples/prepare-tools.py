"""Prepare final OpenPGP-derived compiler tools and empty PIV hosts with pinned provenance."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
SOURCE_RUN = ROOT / 'docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001'
SOURCE = SOURCE_RUN / 'validation/examples'
BASELINE = '727fe43afc430c5d8364d868a6436de50626e476'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def retain(relative, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    target = HERE / relative
    current = HERE
    for part in Path(relative).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked target: {current}')
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        require(target.read_bytes() == raw, f'Preserve changed prepared file: {target}')
    else:
        with target.open('xb') as output:
            output.write(raw)


archives = json.loads((RUN / 'prior-archive-baseline.json').read_text())
archive = next(row for row in archives if row['manifest'] == str((SOURCE_RUN / 'record-manifest.json').relative_to(ROOT)))
raw = (ROOT / archive['manifest']).read_bytes()
require(sha(raw) == archive['manifest_sha256'], 'Frozen source manifest changed')
frozen = {row['path']: row for row in json.loads(raw)['files']}
require(len(frozen) == archive['files'] == 607, 'Frozen source manifest count changed')
COMMON = {'AuditPgp001': 'AuditPiv001', 'openpgp-001': 'piv-001',
          'OpenPgpTool': 'PivTool', 'openpgptool': 'pivtool',
          'OpenPgp': 'Piv', 'OpenPGP': 'PIV', 'openpgp': 'piv'}
copies = []


def copy(old, new=None, replacements=None, cut=None, append=None):
    source = SOURCE / old
    raw = source.read_bytes()
    row = frozen[str(source.relative_to(SOURCE_RUN))]
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], f'Frozen tooling source changed: {source}')
    text = raw.decode()
    operations = []
    if cut:
        start, end = cut
        lo, hi = text.index(start), text.index(end)
        removed = text[lo:hi]
        text = text[:lo] + text[hi:]
        operations.append(dict(operation='remove-between-markers', start=start, end=end,
                               removed_sha256=sha(removed.encode())))
    for before, after in dict(COMMON, **(replacements or {})).items():
        count = text.count(before)
        if before in (replacements or {}):
            require(count > 0, f'Correction fragment absent: {old}: {before}')
        text = text.replace(before, after)
        operations.append(dict(operation='literal-replace', before=before, after=after, count=count))
    target = new or old
    if append:
        retain('inventory-extension.py.snapshot', append)
        text += append.decode()
        operations.append(dict(operation='append-file', path='inventory-extension.py.snapshot', sha256=sha(append)))
    retain(target, text.encode())
    copies.append(dict(source=str(source.relative_to(ROOT)), source_sha256=sha(raw), target=target,
                       target_sha256=sha(text.encode()), operations=operations))


inventory_extension = b'\n\ndef identities(inventory):\n    """Bind every complete current body, source/document identity and task unit."""\n    return inventory\n'
copy('inventory.py', append=inventory_extension, replacements={
    "scope['editor_complete'] is True and bool(scope['notification'])":
        "scope['baseline'] == '727fe43afc430c5d8364d868a6436de50626e476' and scope['editor_complete'] is True and scope['reviewer_complete'] is True and scope['formatter_complete'] is True and bool(scope['notification'])",
    "    return scope\n": """    for key in ('final_review', 'formatter_record'):
        row = scope[key]
        target = canonical(row['path'])
        require(target.is_relative_to(RUN) and sha(target.read_bytes()) == row['sha256'], 'Released record identity changed: ' + key)
    reviewed = json.loads(canonical(scope['final_review']['path']).read_text())
    require(reviewed['baseline'] == scope['baseline'], 'Final review baseline mismatch')
    final = {row['path']: row for row in reviewed['files']}
    require(len(final) == len(reviewed['files']) and bool(final), 'Require nonempty distinct final reviewed paths')
    for path in scope['edited_paths']:
        require(path in final and sha(canonical(path).read_bytes()) == final[path]['final_sha256'], 'Released final input changed: ' + path)
    return scope
""",
    "    xml, xml_documents = xml_inventory(source_paths(module))": """    xml, xml_documents = xml_inventory(source_paths(module))
    tasks = []
    for item in accepted['task_units']:
        path = item['path']
        require(path.startswith('src/Piv/') or path in accepted['shared_paths'], 'Task unit escaped released scope')
        lines = canonical(path).read_text().splitlines(keepends=True)
        lo, hi = item['start_line'], item['end_line']
        require(1 <= lo <= hi <= len(lines) and bool(item['reason']), 'Require a bounded task unit and rationale')
        body = ''.join(lines[lo - 1:hi])
        tasks.append(dict(path=path, id=item['id'], body=body, body_sha256=sha(body.encode()), reason=item['reason']))
    require(len(tasks) == len({row['id'] for row in tasks}), 'Duplicate task unit identities')""",
    "inline=inline, xml=xml, xml_documents=xml_documents, mappings=mappings,":
        "inline=inline, xml=xml, xml_documents=xml_documents, mappings=mappings, tasks=tasks,",
    "suffix = f\"{item['chunk']}:{item['tag']}:{item['index']}\" if kind == 'xml' else str(item.get('id', item.get('index')))":
        "suffix = f\"{item['chunk']}:{item['tag']}:{item['index']}\" if kind == 'xml' else str(item.get('id', item.get('index')))",
    "def identities(inventory):": "def legacy_identities(inventory):",
})
copy('sdk-policy.py')
copy('analyzer-package-1/analyzer_policy.py', 'compiler_policy.py', cut=('def receipt():', 'def verify_capture('), replacements={
    '    record = receipt()\n': '',
    "data['analyzer_package_correction_sha256'] == identity(HERE / 'manifest.json')['sha256']":
        "data['tooling_provenance_sha256'] == identity(HERE / 'tooling-provenance.json')['sha256']",
    '    validate_proof(proof)':
        "    require(proof['package'] == 'Microsoft.CodeAnalysis.NetAnalyzers' and proof['version'] == VERSION, 'Canonical analyzer package identity changed')",
})
copy('analyzer-package-1/capture.py', 'capture.py', replacements={
    "sys.path.insert(0, str(Path(__file__).resolve().parents[1]))\nimport analyzer_policy as _analyzer_policy":
        "import compiler_policy as _analyzer_policy\nfrom provenance import project_reference_proof",
    '_analyzer_policy.receipt()\n': '',
    "('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt', 'imported-projects.txt')":
        "('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt', 'imported-projects.txt', 'project-references.txt', 'resolved-project-references.txt')",
    '    _analyzer_policy.validate_proof(analyzer_package)\n': '',
    "len(sample_paths) == len(set(sample_paths)) == 19": "len(sample_paths) == len(set(sample_paths)) == 36",
    'all 19 original PivTool sources': 'all 36 original PivTool sources',
    "    result = dict(revision=revision,":
        "    project_proof = project_reference_proof(actual_project, policy, references, dest)\n    result = dict(project_reference_proof=project_proof, revision=revision,",
    "analyzer_package_correction_sha256=_analyzer_policy.identity(_analyzer_policy.HERE / 'manifest.json')['sha256']":
        "tooling_provenance_sha256=_analyzer_policy.identity(HERE / 'tooling-provenance.json')['sha256']",
    "dest.mkdir(parents=True, exist_ok=False)":
        "require(not (HERE.parent.parent / 'record-manifest.json').exists(), 'Run is frozen')\ndest.mkdir(parents=True, exist_ok=False)",
})
copy('analyzer-package-1/verify.py', 'verify.py', replacements={
    "sys.path.insert(0, str(Path(__file__).resolve().parents[1]))\nimport analyzer_policy as _analyzer_policy":
        "import compiler_policy as _analyzer_policy\nfrom provenance import verify_project_proof",
    '_analyzer_policy.receipt()\n': '',
    "('mapping', 'mappings')": "('mapping', 'mappings'), ('task', 'tasks')",
    "len(sample['source_paths']) == 19": "len(sample['source_paths']) == 36",
    'all 19 current PivTool source paths': 'all 36 current PivTool source paths',
    '19 shipping source paths': '36 shipping source paths',
    "        _analyzer_policy.verify_capture(data, evidence, args.live)":
        "        _analyzer_policy.verify_capture(data, evidence, args.live)\n        verify_project_proof(data['project_reference_proof'], evidence, args.live)",
})

EXTRA_ITEMS = '''    <WriteLinesToFile File="$(DocumentationWorkspace)/project-references.txt" Lines="@(ProjectReference-&gt;'%(FullPath)')" Overwrite="true" />
    <WriteLinesToFile File="$(DocumentationWorkspace)/resolved-project-references.txt" Lines="@(_ResolvedProjectReferencePaths-&gt;'%(FullPath)|%(MSBuildSourceProjectFile)|%(OriginalProjectReferenceItemSpec)')" Overwrite="true" />'''
EXTRA_POLICY = '''      <DocumentationPolicy Include="Version=$(Version)" />
      <DocumentationPolicy Include="YubiKitVersion=$(YubiKitVersion)" />
      <DocumentationPolicy Include="AssemblyVersion=$(AssemblyVersion)" />
      <DocumentationPolicy Include="TargetPath=$(TargetPath)" />'''
for context in ('consumer', 'annotations'):
    copy(f'v1/{context}/Directory.Build.props')
    copy(f'v1/{context}/Directory.Build.targets', replacements={
        '    <MakeDir Directories="$(DocumentationWorkspace)" />':
            '    <MakeDir Directories="$(DocumentationWorkspace)" />\n' + EXTRA_ITEMS,
        '      <DocumentationPolicy Include="Configuration=$(Configuration)" />':
            '      <DocumentationPolicy Include="Configuration=$(Configuration)" />\n' + EXTRA_POLICY,
    })
    copy(f'v1/{context}/src/Yubico.YubiKit.AuditPgp001.Examples.v1.{context}.csproj',
         f'v1/{context}/src/Yubico.YubiKit.AuditPiv001.Examples.v1.{context}.csproj')
copy('v1/openpgptool/OpenPgpTool.Capture.targets', 'v1/pivtool/PivTool.Capture.targets', replacements={
    '    <MakeDir Directories="$(DocumentationWorkspace)" />':
        '    <MakeDir Directories="$(DocumentationWorkspace)" />\n' + EXTRA_ITEMS,
    '      <DocumentationPolicy Include="Configuration=$(Configuration)" />':
        '      <DocumentationPolicy Include="Configuration=$(Configuration)" />\n' + EXTRA_POLICY,
})
copy('dispatch/src/Yubico.YubiKit.AuditPgp001.Examples.Shipping.OpenPgpTool.csproj',
     'dispatch/src/Yubico.YubiKit.AuditPiv001.Examples.Shipping.PivTool.csproj')
tools = [dict(path=str(p.relative_to(HERE)), bytes=p.stat().st_size, sha256=sha(p.read_bytes()))
         for p in sorted(HERE.glob('*.py')) if p.name != 'analyzer_policy.py']
metadata = []
for relative in ('src/Piv/src/Yubico.YubiKit.Piv.csproj',
                 'src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/Yubico.YubiKit.Piv.IntegrationTests.csproj',
                 'src/Piv/examples/PivTool/PivTool.csproj',
                 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig'):
    path = ROOT / relative
    metadata.append(dict(path=relative, bytes=path.stat().st_size, sha256=sha(path.read_bytes())))
record = dict(baseline=BASELINE, status='skeleton-awaiting-reviewed-formatted-release', source_archive=archive,
              copies=copies, tools=tools, context_metadata=metadata,
              policy='Fresh tools and empty hosts only; corrected final OpenPGP analyzer-package-1 capture/verify algorithms integrated directly. Final extraction and current body reading require explicit reviewed/formatted release. Ordinary actual PivTool outputs retained; historical results/bodies excluded.')
retain('tooling-provenance.json', (json.dumps(record, indent=2) + '\n').encode())
print('Prepared final-algorithm PIV inventory/capture/verify tools and unique empty consumer/annotation/actual-shipping traversal; final inputs await reviewed/formatted release.')
