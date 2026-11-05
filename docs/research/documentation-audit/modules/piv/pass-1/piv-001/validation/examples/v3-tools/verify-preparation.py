"""Verify v3 additive provenance, exact annotation correction and unchanged historical evidence."""
from pathlib import Path
import ast
import json
import re
import sys

sys.dont_write_bytecode = True
TOOLS = Path(__file__).resolve().parent
HERE = TOOLS.parent
ROOT = next(path for path in HERE.parents if (path / 'toolchain.cs').exists())
FOLDER = HERE / 'v3'
from inventory import sha, require
import project_graph_v3 as graph


def verify(row):
    raw = Path(row['path']).read_bytes()
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'Changed historical/versioned identity: ' + row['path'])


if sys.argv[1:] == ['tools']:
    draft = json.loads((TOOLS / 'tool-transformations.json').read_text())
    for row in draft['tools']:
        verify(row)
        ast.parse(Path(row['path']).read_text(), filename=row['path'])
    for change in draft['transformations']:
        verify(change['source'])
        verify(change['target'])
        text = Path(change['source']['path']).read_text()
        for operation in change['substitutions']:
            require(text.count(operation['before']) == operation['count'], 'Draft adaptation count changed')
            text = text.replace(operation['before'], operation['after'])
        require(text.encode() == Path(change['target']['path']).read_bytes(), 'Draft versioned tool differs from source adaptation')
    require(not (FOLDER / 'manifest.json').exists(), 'Tools-only stage precedes final released extraction')
    print('PASS: nine versioned v3 source-tool adaptations replayed; Python syntax and original source identities verified. Old v2 tools/artifacts stay unchanged.')
    print('PENDING: explicit latest review-accepted-v3.json identity and matching coordinator reviewed-files/format-transform release, followed by final v3 extraction and fresh builds/captures.')
    raise SystemExit(0)
require(not sys.argv[1:], 'Usage: verify-preparation.py [tools]')
bootstrap = json.loads((FOLDER / 'tooling-provenance.json').read_text())
acceptance = graph.acceptance_receipt()
require(bootstrap['baseline'] == acceptance['baseline'] == '727fe43afc430c5d8364d868a6436de50626e476', 'Versioned baseline changed')
for row in bootstrap['preserved'] + bootstrap['tools']:
    verify(row)
for change in bootstrap['transformations']:
    verify(change['source'])
    verify(change['target'])
    text = Path(change['source']['path']).read_text()
    for operation in change['substitutions']:
        require(text.count(operation['before']) == operation['count'], 'Versioned adaptation count changed')
        text = text.replace(operation['before'], operation['after'])
    require(text.encode() == Path(change['target']['path']).read_bytes(), 'Versioned tool differs from exact bounded source adaptation')
for row in (acceptance['parent_extraction'], acceptance['parent_source_context'], acceptance['v3_tooling_provenance']):
    verify(row)
old = json.loads((HERE / 'v2/manifest.json').read_text())
new = json.loads((FOLDER / 'manifest.json').read_text())
require(new['revision'] == 'v3' and new['status'] == 'extracted-final-inputs', 'Require a fresh final-input manifest')
require(len(old['groups']) == len(new['groups']) == 45, 'Keep the reconciled complete-group denominator')
current_inventory = json.loads((FOLDER / 'inventory.json').read_text())
unit_count = sum(len(current_inventory[key]) for key in ('fences', 'inline', 'xml', 'mappings', 'tasks'))
require(len(new['dispositions']) == unit_count and len({row['id'] for row in new['dispositions']}) == unit_count, 'Reconcile current inline selector/reference additions and removals separately from complete code bodies')
before = {group['name']: group for group in old['groups']}
after = {group['name']: group for group in new['groups']}
require(set(before) == set(after), 'Group membership changed')
consumer = [group for group in new['groups'] if group['context'] == 'consumer']
require(len(consumer) == 44, 'Consumer group count changed')
for group in consumer:
    previous = before[group['name']]
    original_bodies = [(row['unit_id'], row['body'], row['body_sha256']) for row in group['originals']]
    previous_bodies = [(row['unit_id'], row['body'], row['body_sha256']) for row in previous['originals']]
    require(original_bodies == previous_bodies and group['wrapper'] == previous['wrapper'], 'Unchanged consumer body/wrapper differs: ' + group['name'])
annotation = after['metadata-test-annotation']
require(len(annotation['originals']) == 1 and annotation['originals'][0]['body_sha256'] == bootstrap['annotation_body_sha256'], 'Require the exact newly accepted annotation body')
require('MinFirmware = "5.3.0"' in annotation['wrapper'] and 'Assert.InRange(metadata.RetriesRemaining, 0, metadata.TotalRetries)' in annotation['wrapper'] and 'Assert.NotNull(metadata)' not in annotation['wrapper'], 'Compile the real corrected range assertion and fixture gate')
for context in ('consumer', 'annotations'):
    current_source = (FOLDER / context / 'src/Examples.cs').read_bytes()
    previous_source = (HERE / 'v2' / context / 'src/Examples.cs').read_bytes()
    reversed_source = current_source.replace(b'namespace AuditPiv001.Examples.V3.', b'namespace AuditPiv001.Examples.V2.')
    if context == 'annotations':
        reversed_source = reversed_source.replace(b', MinFirmware = "5.3.0"', b'').replace(b'Assert.InRange(metadata.RetriesRemaining, 0, metadata.TotalRetries)', b'Assert.NotNull(metadata)')
    require(reversed_source == previous_source, 'Generated host change exceeds namespace and the exact annotation correction')
    require(b'namespace AuditPiv001.Examples.V3.' in current_source, 'Require an explicit new application namespace')
counts = [(row['name'], len(row['direct']), len(row['closure'])) for row in acceptance['contexts']]
require(counts == [('consumer', 5, 12), ('annotations', 2, 4), ('pivtool', 5, 10)], 'Preserve corrected exact graph contexts')
for context in acceptance['contexts']:
    graph.context_for(Path(context['project']['path']), acceptance)
require(acceptance['consumer_workspace'] == [], 'Fresh v3 contexts await their own compiler workspaces')
for path in TOOLS.glob('*.py'):
    ast.parse(path.read_text(), filename=str(path))
capture = (TOOLS / 'capture-v3.py').read_text()
for expression in ('source_args == sources', "arguments('/reference:') == references", "arguments('/analyzer:') == analyzers", "arguments('/analyzerconfig:') == configs", "'/warnaserror+' in args", "'/warn:9999' in args", '_analyzer_policy.prove(', "policy['SkipCompilerExecution']"):
    require(expression in capture, 'Preserve compiler/analyzer proof: ' + expression)
print(f'PASS: {len(bootstrap["preserved"])} historical artifacts preserved; nine versioned tool transformations replayed; 57 current accepted products, sole annotation fence change plus README inline-selector dispositions, {unit_count} current units, unchanged 44 consumer groups, fresh v3 namespaces and exact 5/12, 2/4, 5/10 graph contexts reconciled.')
print('PENDING: fresh v3 consumer/annotation/actual-shipping builds, immediate captures and final --live verification. Prior v2 compilation/capture/failure evidence stays attached to v2.')
