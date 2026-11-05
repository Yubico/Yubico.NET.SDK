"""Prepare/replay the additive graph-node-order verification correction; preserve all v3 evidence."""
from pathlib import Path
import ast
import copy
import hashlib
import importlib
import json
import sys

sys.dont_write_bytecode = True
TOOLS = Path(__file__).resolve().parent
HERE = TOOLS.parent
RUN = HERE.parent.parent
ROOT = next(path for path in HERE.parents if (path / 'toolchain.cs').exists())
RECEIPT = TOOLS / 'graph-order-v4.json'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def retain(path, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    with path.open('xb') as output:
        output.write(raw)


NORMALIZER = '''def canonical_graph_nodes(value):
    """Return a shallow comparison copy with unique graph nodes ordered by project path."""
    nodes = value['graph']
    paths = [node['project']['path'] for node in nodes]
    project_ids = [node['project_id'] for node in nodes]
    require(len(paths) == len(set(paths)) and len(project_ids) == len(set(project_ids)),
            'Graph comparison requires unique project paths and project IDs')
    return dict(value, graph=sorted(nodes, key=lambda node: node['project']['path']))


'''
RECEIPT_CHECK = '''ordering_receipt = json.loads((Path(__file__).resolve().parent / 'graph-order-v4.json').read_text())
for ordering_file in ordering_receipt['preserved'] + ordering_receipt['tools']:
    ordering_bytes = Path(ordering_file['path']).read_bytes()
    require(len(ordering_bytes) == ordering_file['bytes'] and sha(ordering_bytes) == ordering_file['sha256'],
            'Ordering correction source/evidence identity changed: ' + ordering_file['path'])
'''
TRANSFORMS = {
    'project_graph_v3.py': ('project_graph_v4.py', [
        ('def verify_project_proof(proof, evidence, live):', NORMALIZER + 'def verify_project_proof(proof, evidence, live):'),
        ("require(current == graph, 'Live project/restore/Csc/bin graph identities changed')",
         "require(canonical_graph_nodes(current) == canonical_graph_nodes(graph), 'Live project/restore/Csc/bin graph identities changed')"),
    ]),
    'verify-v3.py': ('verify-v4.py', [
        ('from project_graph_v3 import verify_project_proof, acceptance_receipt',
         'from project_graph_v4 import verify_project_proof, acceptance_receipt'),
        ('correction = acceptance_receipt()\n', 'correction = acceptance_receipt()\n' + RECEIPT_CHECK),
    ]),
}
require(sys.argv[1:] in (['prepare'], ['verify']), 'Usage: prepare-ordering-v4.py prepare|verify')
if sys.argv[1:] == ['prepare']:
    require(not RECEIPT.exists(), 'Preserve the existing ordering receipt')
    paths = {TOOLS / name for name in ('project_graph_v3.py', 'verify-v3.py', 'compiler_policy.py', 'inventory.py', 'capture-v3.py')}
    paths |= {HERE / 'v3' / name for name in ('manifest.json', 'source-context.json', 'tooling-provenance.json', 'project-graph-acceptance.json')}
    paths |= {HERE / 'v3' / context / 'evidence/first/manifest.json' for context in ('consumer', 'annotations', 'pivtool')}
    paths |= {RUN / 'validation' / name for name in ('examples-v3-compiled.log', 'examples-v3-graph-order-diagnosis.json')}
    transformations = []
    for source_name, (target_name, substitutions) in TRANSFORMS.items():
        source = TOOLS / source_name
        text = source.read_text()
        operations = []
        for before, after in substitutions:
            require(text.count(before) == 1, 'Expected one bounded ordering adaptation: ' + before)
            text = text.replace(before, after)
            operations.append(dict(before=before, after=after, count=1))
        ast.parse(text, filename=target_name)
        target = TOOLS / target_name
        retain(target, text.encode())
        transformations.append(dict(source=identity(source), target=identity(target), substitutions=operations))
    record = dict(id='graph-order-v4', baseline='727fe43afc430c5d8364d868a6436de50626e476',
                  policy='Verification-only correction: compare copied graph-node lists in canonical project-path order after rejecting duplicate paths and project IDs. Complete dictionaries retain exact value equality. Original closure, membership, deferred XML edge, source, DLL, assets, analyzer and policy checks remain unchanged. Preserve all original capture bytes and the failed verifier output; compilation/capture contexts stay v3.',
                  transformations=transformations, preserved=[identity(path) for path in sorted(paths)],
                  tools=[identity(path) for path in (TOOLS / 'project_graph_v4.py', TOOLS / 'verify-v4.py', Path(__file__).resolve())])
    retain(RECEIPT, (json.dumps(record, indent=2) + '\n').encode())
record = json.loads(RECEIPT.read_text())
for row in record['preserved'] + record['tools']:
    require(identity(Path(row['path'])) == row, 'Preserved/new ordering correction identity changed: ' + row['path'])
for change in record['transformations']:
    text = Path(change['source']['path']).read_text()
    for operation in change['substitutions']:
        require(text.count(operation['before']) == operation['count'], 'Ordering adaptation count changed')
        text = text.replace(operation['before'], operation['after'])
    require(text.encode() == Path(change['target']['path']).read_bytes(), 'Ordering correction exceeds its exact source transformation')
    ast.parse(text, filename=change['target']['path'])
graph = importlib.import_module('project_graph_v4')
acceptance = graph.acceptance_receipt()  # Original acceptance still checks original v3 tool identities.
summaries = []
for context_name in ('consumer', 'annotations', 'pivtool'):
    evidence = HERE / 'v3' / context_name / 'evidence/first'
    capture = json.loads((evidence / 'manifest.json').read_text())
    proof = capture['project_reference_proof']
    project = Path(proof['project']['path'])
    context = graph.context_for(project, acceptance)
    references = {row['path'] for row in capture['records'] if row['kind'] == 'reference'}
    _, current = graph.project_closure_proof(project, {Path(path) for path in context['direct']}, references,
                                            capture['policy'], evidence, {Path(path) for path in context['closure']})
    captured = proof['project_graph']
    before_bytes = json.dumps(captured, sort_keys=True)
    require(graph.canonical_graph_nodes(current) == graph.canonical_graph_nodes(captured), 'Strict normalized replay differs: ' + context_name)
    require(json.dumps(captured, sort_keys=True) == before_bytes, 'Comparison mutated original in-memory capture data')
    for duplicate_kind in ('path', 'project_id'):
        duplicate = copy.deepcopy(captured)
        node = copy.deepcopy(duplicate['graph'][0])
        if duplicate_kind == 'project_id':
            node['project']['path'] += '.duplicate-probe'
        else:
            node['project_id'] += '.duplicate-probe'
        duplicate['graph'].append(node)
        try:
            graph.canonical_graph_nodes(duplicate)
        except ValueError:
            pass
        else:
            raise ValueError('Duplicate graph-node rejection missing: ' + duplicate_kind)
    changed = copy.deepcopy(captured)
    changed['graph'][0]['project']['sha256'] = 'altered-identity-probe'
    require(graph.canonical_graph_nodes(changed) != graph.canonical_graph_nodes(captured), 'Node identity changes remain comparison failures')
    changed = copy.deepcopy(captured)
    changed['restored_assets']['sha256'] = 'altered-assets-probe'
    require(graph.canonical_graph_nodes(changed) != graph.canonical_graph_nodes(captured), 'Non-order metadata changes remain comparison failures')
    for row in capture['retained'] + proof['retained']:
        raw = (evidence / row['path']).read_bytes()
        require(len(raw) == row['bytes'] and hashlib.sha256(raw).hexdigest() == row['sha256'], 'Original capture text changed: ' + row['path'])
    summaries.append(context_name + '=' + str(len(captured['graph'])))
print('PASS: exact v3-to-v4 helper/verifier transformations and original evidence identities; read-only strict normalized graph replay ' + ', '.join(summaries) + '; duplicate path/ID rejection and identity/asset difference preservation.')
print('PENDING: coordinator full verify-v4 --live and existing v3 auxiliary checks. No build, recapture, test runner or device operation was invoked.')
