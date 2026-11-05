"""Prepare fresh v4 example contexts from the accepted late documentation corrections."""
from pathlib import Path
import ast
import copy
import hashlib
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
TOOLS = Path(__file__).resolve().parent
HERE = TOOLS.parent
PARENT = HERE / 'v3-tools'
FOLDER = HERE / 'v4'
RUN = HERE.parent.parent
ROOT = next(path for path in HERE.parents if (path / 'toolchain.cs').exists())
BASELINE = '727fe43afc430c5d8364d868a6436de50626e476'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=sha(raw))


def repo_record(path):
    return dict(identity(path), path=str(path.relative_to(ROOT)))


def retain(path, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    require(path.is_relative_to(HERE), 'Output escaped examples ownership')
    current = HERE
    for part in path.relative_to(HERE).parts:
        current /= part
        require(not current.is_symlink(), 'Symlinked extraction output')
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require(path.read_bytes() == raw, 'Preserve changed existing v4 preparation: ' + str(path))
    else:
        with path.open('xb') as output:
            output.write(raw)


def save(path, value):
    retain(path, (json.dumps(value, indent=2) + '\n').encode())


require(not (FOLDER / 'tooling-provenance.json').exists(), 'Preserve the existing v4 checkpoint')
preserved_paths = {path for path in HERE.rglob('*') if path.is_file() and not path.is_relative_to(TOOLS) and not path.is_relative_to(FOLDER)}
preserved_paths |= {path for path in (RUN / 'validation').glob('examples-v[23]-*') if path.is_file()}
preserved_paths |= {RUN / 'evidence' / name for name in ('review-accepted.json', 'review-accepted-v2.json', 'review-accepted-v3.json', 'review-accepted-v4.json')}
preserved = [identity(path) for path in sorted(preserved_paths)]
accepted_path = RUN / 'evidence/review-accepted-v4.json'
require(sha(accepted_path.read_bytes()) == '59a4be1b906b31aa95f7ea4a1fd77621c41f2e9af607719c713548dc05a329b9', 'Final v4 acceptance identity changed')
accepted = json.loads(accepted_path.read_text())
review_path = RUN / 'reviewed-files.json'
reviewed = json.loads(review_path.read_text())
format_path = RUN / 'validation/format-transform.json'
formatted = json.loads(format_path.read_text())
previous = {row['path']: row for row in json.loads((RUN / 'evidence/review-accepted-v3.json').read_text())['files']}
current = {row['path']: row for row in accepted['files']}
final = {row['path']: row for row in reviewed['files']}
new_cs = {'src/Piv/examples/PivTool/PivExamples/Results/KeyGenerationResult.cs', 'src/Piv/src/Cryptography/PrivateKeyOperations.cs'}
changed_md = 'src/Piv/examples/PivTool/README.md'
require(accepted['baseline'] == reviewed['baseline'] == formatted['baseline'] == BASELINE, 'Baseline mismatch')
require(len(current) == len(final) == 59 and set(current) == set(final) == set(previous) | new_cs, 'Final 59-path scope changed')
for path, row in current.items():
    require(sha((ROOT / path).read_bytes()) == row['sha256'] == final[path]['final_sha256'], 'Current accepted input changed: ' + path)
    if path in previous and path != changed_md:
        require(row['sha256'] == previous[path]['sha256'], 'Unchanged accepted v3 product differs: ' + path)
require(formatted['post_format_review_sha256'] == sha(accepted_path.read_bytes()) and len(formatted['files']) == 50 and
        {row['path'] for row in formatted['files']} == {path for path in current if path.endswith('.cs')} and
        all(row['whitespace_only'] and row['imports_identical'] for row in formatted['files']), 'Final 50-file formatting/import chain changed')
changes = []


def derive(source_name, target_name, substitutions):
    source = PARENT / source_name
    text = source.read_text()
    operations = []
    for before, after in substitutions.items():
        count = text.count(before)
        require(count > 0, 'Required v4 adaptation absent: ' + source_name + ': ' + before)
        text = text.replace(before, after)
        operations.append(dict(before=before, after=after, count=count))
    ast.parse(text, filename=target_name)
    target = TOOLS / target_name
    retain(target, text.encode())
    changes.append(dict(source=identity(source), target=identity(target), substitutions=operations))


derive('inventory.py', 'inventory.py', {"HERE / 'v3/extraction-scope.json'": "HERE / 'v4/extraction-scope.json'"})
derive('compiler_policy.py', 'compiler_policy.py', {"HERE / 'v3/tooling-provenance.json'": "HERE / 'v4/tooling-provenance.json'"})
derive('sdk-policy.py', 'sdk-policy.py', {})
derive('project_graph_v4.py', 'project_graph_v4.py', {"HERE / 'v3/project-graph-acceptance.json'": "HERE / 'v4/tooling-provenance.json'"})
derive('capture-v3.py', 'capture-v4.py', {
    'from project_graph_v3 import': 'from project_graph_v4 import',
    "HERE / 'v3/project-graph-acceptance.json'": "HERE / 'v4/tooling-provenance.json'",
    "HERE / 'v3/tooling-provenance.json'": "HERE / 'v4/tooling-provenance.json'",
})
ordering_guard = '''ordering_receipt = json.loads((Path(__file__).resolve().parent / 'graph-order-v4.json').read_text())
for ordering_file in ordering_receipt['preserved'] + ordering_receipt['tools']:
    ordering_bytes = Path(ordering_file['path']).read_bytes()
    require(len(ordering_bytes) == ordering_file['bytes'] and sha(ordering_bytes) == ordering_file['sha256'],
            'Ordering correction source/evidence identity changed: ' + ordering_file['path'])
'''
derive('verify-v4.py', 'verify-v4.py', {
    ordering_guard: '',
    "HERE / 'v3/project-graph-acceptance.json'": "HERE / 'v4/tooling-provenance.json'",
})
derive('prepare-final.py', 'prepare-final.py', {
    'AuditPiv001.Examples.V3.': 'AuditPiv001.Examples.V4.',
    "HERE / 'v3/tooling-provenance.json'": "HERE / 'v4/tooling-provenance.json'",
    "HERE / 'v3/extraction-scope.json'": "HERE / 'v4/extraction-scope.json'",
})
derive('bind-source-context-v3.py', 'bind-source-context-v4.py', {
    "folder = HERE / 'v3'": "folder = HERE / 'v4'", "revision='v3'": "revision='v4'",
    "HERE / 'v3/extraction-scope.json'": "HERE / 'v4/extraction-scope.json'",
    "RUN / 'edit-corrections.md'": "RUN / 'edit-final-coverage-corrections.md'",
    'final v3 verification': 'final v4 verification',
})
derive('verify-source-context-v3.py', 'verify-source-context-v4.py', {
    "folder = HERE / 'v3'": "folder = HERE / 'v4'",
    "HERE / 'v3/extraction-scope.json'": "HERE / 'v4/extraction-scope.json'",
})
# Prepare only project/configuration skeletons before pinning the single v4 tooling/graph receipt.
for context in ('consumer', 'annotations'):
    source = HERE / 'v3' / context
    for path in source.rglob('*'):
        if path.is_file() and path.suffix in ('.props', '.targets', '.csproj'):
            relative = str(path.relative_to(source)).replace('.v3.', '.v4.')
            retain(FOLDER / context / relative, path.read_bytes().replace(b'examples-v3-', b'examples-v4-'))
scope = json.loads((HERE / 'v3/extraction-scope.json').read_text())
scope.update(notification='Coordinator released final v4 inputs after independent acceptance of the DER SPKI result/factory contract, deterministic RSA guard rationale and operation-specific JSON clause. Complete example bodies/wrappers remain unchanged; all three v4 compiler contexts require fresh compilation/captures.',
             editor_report=dict(path=str((RUN / 'edit-final-coverage-corrections.md').relative_to(ROOT)), sha256=sha((RUN / 'edit-final-coverage-corrections.md').read_bytes())),
             final_review=dict(path=str(review_path.relative_to(ROOT)), sha256=sha(review_path.read_bytes())),
             formatter_record=dict(path=str(format_path.relative_to(ROOT)), sha256=sha(format_path.read_bytes())),
             accepted_review=dict(path=str(accepted_path.relative_to(ROOT)), sha256=sha(accepted_path.read_bytes())),
             final_coverage_review=repo_record(RUN / 'review-final-coverage-corrections.md'), edited_paths=sorted(current))
save(FOLDER / 'extraction-scope.json', scope)
parent_graph = json.loads((HERE / 'v3/project-graph-acceptance.json').read_text())
contexts = copy.deepcopy(parent_graph['contexts'])
for context in contexts:
    if context['name'] != 'pivtool':
        context['project'] = identity(FOLDER / context['name'] / 'src' / f'Yubico.YubiKit.AuditPiv001.Examples.v4.{context["name"]}.csproj')
    require(identity(Path(context['project']['path'])) == context['project'], 'Context project metadata changed')
    for row in context['graph_projects']:
        require(identity(Path(row['path'])) == row, 'Graph dependency project metadata changed')
    project = Path(context['project']['path'])
    declared = {str((project.parent / node.attrib['Include'].replace('$(RepositoryRoot)', str(ROOT)).replace('\\', '/')).resolve()) for node in ET.parse(project).iter('ProjectReference')}
    require(declared == set(context['direct']), 'Exact direct graph membership changed')
bootstrap = dict(id='final-v4-example-inputs', baseline=BASELINE, revision='v4',
                 parent_extraction=identity(HERE / 'v3/manifest.json'), parent_source_context=identity(HERE / 'v3/source-context.json'),
                 parent_ordering_receipt=identity(PARENT / 'graph-order-v4.json'),
                 accepted_review=identity(accepted_path), reviewed_files=identity(review_path), formatter=identity(format_path),
                 correction_review=identity(RUN / 'review-final-coverage-corrections.md'),
                 editor_report=identity(RUN / 'edit-final-coverage-corrections.md'), changed_product_paths=sorted(new_cs | {changed_md}),
                 preserved=preserved, transformations=changes, tools=[identity(path) for path in sorted(TOOLS.glob('*.py'))],
                 hardware_source=parent_graph['hardware_source'], contexts=contexts, consumer_workspace=[],
                 policy='Fresh v4 source/configuration/reference contexts. Reuse the proven graph closure and unique-ID/order-neutral full-value verification algorithms unchanged; all canonical compiler/analyzer/source/NoSkip/WAE proofs remain active. This single receipt registers new tool identities; the historical v3 ordering receipt remains preserved ancestry. No older build/capture is rebound to new source hashes.')
save(FOLDER / 'tooling-provenance.json', bootstrap)


def run_static(*args):
    subprocess.run([sys.executable, *args], cwd=ROOT, check=True)


run_static(str(TOOLS / 'prepare-final.py'), 'inventory', 'v4')
inventory = json.loads((FOLDER / 'inventory.json').read_text())
old_inventory = json.loads((HERE / 'v3/inventory.json').read_text())
old_sources = {row['path']: row for row in old_inventory['xml_documents']}
changed_sources = {row['path'] for row in inventory['xml_documents'] if row != old_sources[row['path']]}
require(changed_sources == new_cs and len(inventory['xml_documents']) == 111, 'Current C# input changes exceed the two accepted documentation paths')


def units(data):
    result = {}
    for kind, plural in [('fence', 'fences'), ('inline', 'inline'), ('xml', 'xml'), ('mapping', 'mappings'), ('task', 'tasks')]:
        for row in data[plural]:
            suffix = f"{row['chunk']}:{row['tag']}:{row['index']}" if kind == 'xml' else str(row.get('id', row.get('index')))
            result[f"{kind}:{row['path']}:{suffix}"] = (kind, row)
    return result


old_units, current_units = units(old_inventory), units(inventory)
code = lambda entries: {key: (row['body'], row['body_sha256']) for key, (kind, row) in entries.items() if kind == 'fence' or (kind == 'xml' and row['tag'] == 'code')}
require(code(old_units) == code(current_units), 'Every complete fenced/XML code body must remain unchanged')
plan = copy.deepcopy(json.loads((HERE / 'v3/final-plan.json').read_text()))
plan['inventory_sha256'] = sha((FOLDER / 'inventory.json').read_bytes())
previous_dispositions = {row['id']: row for row in plan['dispositions']}
dispositions, prose_changes = [], []
for key, (kind, row) in current_units.items():
    old = old_units.get(key)
    differs = old is None or old[1]['body'] != row['body'] or old[1]['body_sha256'] != row['body_sha256']
    if key in previous_dispositions:
        disposition = copy.deepcopy(previous_dispositions[key])
    else:
        require((kind == 'inline' or (kind == 'xml' and row['tag'] == 'c')) and row['path'] in new_cs | {changed_md}, 'Adjudicate new executable/body units explicitly')
        disposition = dict(id=key, status='symbolic', reason='Current accepted contract/encoding/ownership reference within prose; the unchanged complete task body compiles against the actual SDK/sample implementation.')
    if differs:
        require((kind == 'inline' or (kind == 'xml' and row['tag'] == 'c')) and row['path'] in new_cs | {changed_md}, 'Only accepted prose/XML reference units may change')
        prose_changes.append(dict(id=key, before=old[1] if old else None, after=row))
    dispositions.append(disposition)
removed = sorted(set(old_units) - set(current_units))
require(all(old_units[key][0] == 'xml' and old_units[key][1]['tag'] == 'c' and old_units[key][1]['path'] in new_cs for key in removed), 'Only changed prose/XML reference units may be removed')
plan['dispositions'] = dispositions
save(FOLDER / 'final-plan.json', plan)
save(FOLDER / 'input-change.json', dict(parent_inventory=identity(HERE / 'v3/inventory.json'), current_inventory=identity(FOLDER / 'inventory.json'),
                                      changed_source_paths=sorted(changed_sources), unchanged_complete_bodies=True,
                                      prose_changes=prose_changes, removed_prose_units=removed,
                                      current_unit_count=len(current_units), previous_unit_count=len(old_units),
                                      compilation_scope='All v4 contexts await fresh builds/captures; valid v3 records retain their historical source/binary context.'))
run_static(str(TOOLS / 'prepare-final.py'), 'extract', 'v4', '--plan', str((FOLDER / 'final-plan.json').relative_to(ROOT)))
run_static(str(TOOLS / 'bind-source-context-v4.py'))
print(f'Prepared fresh v4: 59 accepted product paths / 50 formatted C# paths; {len(current_units)} current dispositions; unchanged complete bodies/wrappers and fresh 111-source/ten-document identities. All three builds/captures remain coordinator-owned.')
