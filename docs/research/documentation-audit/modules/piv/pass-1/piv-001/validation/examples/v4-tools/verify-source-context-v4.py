"""Verify supplementary original/current body and full actual-tool source bindings; static only."""
from pathlib import Path
import ast
import collections
import json
import re
import sys

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, sha, require, source_paths

folder = HERE / 'v4'
record = json.loads((folder / 'source-context.json').read_text())
manifest = json.loads((folder / 'manifest.json').read_text())
require(record['baseline'] == manifest['baseline'] == '727fe43afc430c5d8364d868a6436de50626e476', 'Source context baseline mismatch')
require(record['extraction_manifest_sha256'] == sha((folder / 'manifest.json').read_bytes()) and record['release_sha256'] == sha((HERE / 'v4/extraction-scope.json').read_bytes()), 'Context release/extraction identity changed')


def verify(base, row):
    raw = (base / row['path']).read_bytes()
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'Changed source/input identity: ' + row['path'])


for key in ('module_sources', 'documents', 'preparation_tools'):
    for row in record[key]:
        verify(ROOT, row)
require(len(record['module_sources']) == 111 and len(record['documents']) == 10, 'Reconcile all module source and assigned document identities')
module_paths = {row['path'] for row in record['module_sources']}
require(module_paths == set(source_paths(ROOT / 'src/Piv')), 'Full module C# path set changed')
require(sum('/tests/' in path and '.UnitTests/' in path for path in module_paths) == 18 and
        sum('/tests/' in path and '.IntegrationTests/' in path for path in module_paths) == 24,
        'Test source context changed')
for key in ('original_editor_report', 'authoritative_corrections'):
    verify(ROOT, record[key])
require(len(record['original_bodies']) == 61, 'Original-body denominator changed')
for row in record['original_bodies']:
    require(sha(row['body'].encode()) == row['sha256'], 'Original full body changed')
require(len(record['current_bodies']) == len({row['id'] for row in record['current_bodies']}) == 60, 'Current complete-body denominator changed')
statuses = collections.Counter(row['disposition']['status'] for row in record['current_bodies'])
require(statuses == dict(compile=47, historical=3, **{'other-applet': 3, 'non-csharp': 5, 'non-executable': 2}), 'Current body dispositions changed')
require(len(manifest['groups']) == 45 and set(record['active_contexts']) == {'consumer', 'annotations', 'pivtool'}, 'Task/context counts changed')
for group in manifest['groups']:
    wrapper = group['template']
    for original in group['originals']:
        body = original['body']
        for change in group.get('body_changes', {}).get(original['unit_id'], []):
            require(body.count(change['before']) == change['count'] and bool(change['reason']), 'Body transformation identity changed')
            body = body.replace(change['before'], change['after'])
        marker = '{{unit:' + original['unit_id'] + '}}'
        require(wrapper.count(marker) == 1, 'Whole original body insertion changed')
        wrapper = wrapper.replace(marker, body)
    require(wrapper == group['wrapper'] == group['type_body'], 'Generated wrapper differs from exact complete-body transformation')
    require(not re.search(r'\bnamespace\s+Yubico\b', wrapper) and 'MemoryMarshal.AsMemory' not in wrapper,
            'Use real SDK dependencies and preserve read-only ownership boundaries')
    require(wrapper in (folder / group['context'] / 'src/Examples.cs').read_text(), 'Complete generated group missing')
require(len(record['native_sources']) == 36 and [row['path'] for row in record['native_sources']] == source_paths(ROOT / 'src/Piv/examples/PivTool'), 'Actual full shipping source set changed')
for row in record['native_sources'] + [record['native_project']]:
    verify(ROOT, row)
    verify(folder, row['retained'])
    require((ROOT / row['path']).read_bytes() == (folder / row['retained']['path']).read_bytes(), 'Original shipping compiler text changed')
f9 = next(row for row in record['current_bodies'] if row['path'].endswith('/Attestation.cs') and row['ordinal'] == 2)
require(f9['editor_normalized_sha256'] == 'c26c99a88224f73c91b9d2821843aec7abd049d7cd4fb36c9618be9a574a0ccc' and 'result.AttestationCertificate' in f9['body'], 'Reviewed F9 body changed')
for path in Path(__file__).resolve().parent.glob('*.py'):
    ast.parse(path.read_text(), filename=str(path))
print(f'PASS: 61 baseline originals, 60 current bodies, 47 selected bodies in 45 typed groups; {len(manifest["dispositions"])} explicit dispositions; 111 module sources / four module and six shared documents; all 36 original actual-PivTool sources/project retained; complete wrapper transforms and corrected F9 shape reconcile.')
print('PENDING: coordinator consumer/annotation/actual-shipping builds, actual Csc/source/reference/analyzer/config/assets captures and live dependency verification. Preparation establishes no new compilation or device result.')
