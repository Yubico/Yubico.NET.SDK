"""Verify fresh v4 inputs and unchanged proven graph/order/policy algorithms; static only."""
from pathlib import Path
import ast
import json
import sys

sys.dont_write_bytecode = True
TOOLS = Path(__file__).resolve().parent
HERE = TOOLS.parent
FOLDER = HERE / 'v4'
from inventory import ROOT, sha, require
import project_graph_v4 as graph


def verify(row):
    raw = Path(row['path']).read_bytes()
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'Historical/current tool identity changed: ' + row['path'])


record = graph.acceptance_receipt()
require(record['revision'] == 'v4' and record['accepted_review']['sha256'] == '59a4be1b906b31aa95f7ea4a1fd77621c41f2e9af607719c713548dc05a329b9', 'Final accepted v4 identity changed')
for row in record['preserved'] + record['tools']:
    verify(row)
for change in record['transformations']:
    verify(change['source'])
    verify(change['target'])
    text = Path(change['source']['path']).read_text()
    for operation in change['substitutions']:
        require(text.count(operation['before']) == operation['count'], 'Bounded tool adaptation count changed')
        text = text.replace(operation['before'], operation['after'])
    require(text.encode() == Path(change['target']['path']).read_bytes(), 'Tool adaptation exceeds recorded transformations')
for row in (record['accepted_review'], record['reviewed_files'], record['formatter'], record['correction_review'], record['editor_report']):
    verify(row)
before = json.loads((HERE / 'v3/manifest.json').read_text())
current = json.loads((FOLDER / 'manifest.json').read_text())
require(current['revision'] == 'v4' and len(current['groups']) == len(before['groups']) == 45, 'Complete group denominator changed')
old_groups = {row['name']: row for row in before['groups']}
selected = []
for group in current['groups']:
    old = old_groups[group['name']]
    body = lambda rows: [(row['unit_id'], row['body'], row['body_sha256']) for row in rows]
    require(body(group['originals']) == body(old['originals']) and group['wrapper'] == old['wrapper'], 'Complete body/wrapper changed: ' + group['name'])
    selected.extend(row['unit_id'] for row in group['originals'])
require(len(selected) == len(set(selected)) == 47, 'Selected complete-body denominator changed')
require(sum(row['context'] == 'consumer' for row in current['groups']) == 44, 'Consumer group count changed')
inventory = json.loads((FOLDER / 'inventory.json').read_text())
unit_count = sum(len(inventory[key]) for key in ('fences', 'inline', 'xml', 'mappings', 'tasks'))
require(len(current['dispositions']) == len({row['id'] for row in current['dispositions']}) == unit_count, 'Reconcile every current prose/XML/code unit explicitly')
require(len(inventory['xml_documents']) == 111 and len(inventory['documents']) == 10 and len(inventory['edited_paths']) == 59 and len(inventory['edited_xml_paths']) == 50, 'Current source/document/authored scope changed')
for context in ('consumer', 'annotations'):
    source = FOLDER / context / 'src/Examples.cs'
    previous = HERE / 'v3' / context / 'src/Examples.cs'
    require(source.read_bytes().replace(b'namespace AuditPiv001.Examples.V4.', b'namespace AuditPiv001.Examples.V3.') == previous.read_bytes(), 'Generated host change exceeds its fresh namespace')
    expected_name = f'Yubico.YubiKit.AuditPiv001.Examples.v4.{context}.csproj'
    require(list((FOLDER / context / 'src').glob('*.csproj')) == [FOLDER / context / 'src' / expected_name], 'Require the unique fresh project filename')


def function_text(path, name):
    text = path.read_text()
    node = next(node for node in ast.parse(text).body if isinstance(node, ast.FunctionDef) and node.name == name)
    return ast.get_source_segment(text, node)


for name in ('project_closure_proof', 'canonical_graph_nodes'):
    require(function_text(TOOLS / 'project_graph_v4.py', name) == function_text(HERE / 'v3-tools/project_graph_v4.py', name), 'Preserve the exact proven graph/order-neutral algorithm: ' + name)
require([(row['name'], len(row['direct']), len(row['closure'])) for row in record['contexts']] == [('consumer', 5, 12), ('annotations', 2, 4), ('pivtool', 5, 10)], 'Exact graph context memberships changed')
for context in record['contexts']:
    graph.context_for(Path(context['project']['path']), record)
capture = (TOOLS / 'capture-v4.py').read_text()
for guard in ('source_args == sources', "arguments('/reference:') == references", "arguments('/analyzer:') == analyzers", "arguments('/analyzerconfig:') == configs", "'/warnaserror+' in args", "'/warn:9999' in args", '_analyzer_policy.prove(', "policy['SkipCompilerExecution']"):
    require(guard in capture, 'Preserve actual-Csc/compiler/analyzer policy guard: ' + guard)
for path in TOOLS.glob('*.py'):
    ast.parse(path.read_text(), filename=str(path))
print(f'PASS: {len(record["preserved"])} historical artifacts preserved; nine exact tool adaptations; accepted 59/50 path scope, 111 module sources/ten documents, 45 unchanged groups/47 selected bodies, {unit_count} actual unit dispositions, fresh v4 hosts and unchanged proven graph/order-neutral algorithms (5/12, 2/4, 5/10).')
print('PENDING: fresh v4 consumer/annotation/actual-shipping builds, immediate captures and final live verification. Earlier successful v3 compiler/binary evidence remains historical; this check runs no build, test, capture or device operation.')
