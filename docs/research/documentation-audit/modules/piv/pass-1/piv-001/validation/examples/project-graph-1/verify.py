"""Verify additive tool provenance and original checkpoint preservation; static preparation only."""
from pathlib import Path
import ast
import hashlib
import json
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
EXAMPLES = HERE.parent
sys.path.insert(0, str(EXAMPLES))
import project_graph_v3 as graph


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


record = graph.acceptance_receipt()
require(graph.identity(Path(record['parent_extraction']['path'])) == record['parent_extraction'] and
        graph.identity(Path(record['parent_source_context']['path'])) == record['parent_source_context'],
        'Original extraction/source-context receipt changed')
source = Path(record['hardware_source']['path']).read_text()
tree = ast.parse(source)
function = next(node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name == record['graph_reuse']['function'])
text = ast.get_source_segment(source, function)
require(sha(text.encode()) == record['graph_reuse']['original_function_sha256'], 'Working hardware graph helper identity changed')
for operation in record['graph_reuse']['substitutions']:
    require(text.count(operation['before']) == operation['count'], 'Hardware helper adaptation count changed')
    text = text.replace(operation['before'], operation['after'])
runtime = Path(record['graph_reuse']['runtime']['path']).read_bytes()
expected = record['graph_reuse']['header'].encode() + text.encode() + b'\n\n' + runtime
require(Path(record['graph_reuse']['target']['path']).read_bytes() == expected, 'Generated graph adapter differs from exact source transformation')
for row in record['transformations']:
    text = Path(row['source']['path']).read_text()
    for operation in row['substitutions']:
        require(text.count(operation['before']) == operation['count'], 'Capture/verify correction count changed')
        text = text.replace(operation['before'], operation['after'])
    require(Path(row['target']['path']).read_bytes() == text.encode(), 'Versioned tool differs from exact bounded transformation')
for row in record['contexts']:
    project = Path(row['project']['path'])
    current = set()
    for node in ET.parse(project).iter('ProjectReference'):
        value = node.attrib['Include'].replace('$(RepositoryRoot)', str(graph.ROOT)).replace('\\', '/')
        current.add(str((project.parent / value).resolve()))
    require(current == set(row['direct']), 'Exact declared project set changed')
    require(len(row['closure']) == len(set(row['closure'])) and set(row['direct']) <= set(row['closure']), 'Context graph ledger requires distinct reachable nodes')
    for source_row in row['graph_projects']:
        require(graph.identity(Path(source_row['path'])) == source_row, 'Recursive graph source metadata changed')
for row in record['tools']:
    path = Path(row['path'])
    ast.parse(path.read_text(), filename=str(path))
capture = (EXAMPLES / 'capture-v3.py').read_text()
for requirement in ('source_args == sources', "arguments('/reference:') == references", "arguments('/analyzer:') == analyzers", "arguments('/analyzerconfig:') == configs", "'/warnaserror+' in args", "'/warn:9999' in args", '_analyzer_policy.prove(', "policy['SkipCompilerExecution']", "len(sample_paths) == len(set(sample_paths)) == 36"):
    require(requirement in capture, 'Preserve original actual-Csc/source/analyzer/policy proof: ' + requirement)
proof_text = (EXAMPLES / 'project_graph_v3.py').read_text()
require('closure == expected == set(nodes)' in proof_text and 'actual == closure' in proof_text and
        'set(resolved) == closure' in proof_text and "matches[0].is_relative_to(path.parent / 'obj')" in proof_text and
        "resolved[path].is_relative_to(path.parent / 'bin')" in proof_text,
        'Preserve exact closure/late-item/resolved-output/obj-bin binding guards')
require("policy['Version']" in proof_text and "policy['YubiKitVersion']" in proof_text and "policy['Version'] == policy['YubiKitVersion']" not in proof_text, 'Keep host/SDK version metadata independent')
counts = ', '.join(f"{row['name']} {len(row['direct'])}/{len(row['closure'])}" for row in record['contexts'])
print(f'PASS: original v2 manifest/tool/source/failure identities preserved ({len(record["preserved"])} artifacts); exact hardware graph-source adaptation and two versioned capture/verify transformations replayed; Python syntax and direct/recursive metadata contexts reconcile: {counts}.')
print('PENDING: coordinator consumer second capture on its intact compiler workspace, annotation/shipping builds and first captures, corrected final --live verification. This static preparation performs no build, test, capture or device operation.')
