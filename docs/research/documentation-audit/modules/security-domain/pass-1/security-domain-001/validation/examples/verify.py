"""Check retained compiler evidence and exact current block bodies; do not require generated binaries."""
from pathlib import Path
import hashlib
import json
import re
import sys

HERE=Path(__file__).resolve().parent
ROOT=next(p for p in HERE.parents if (p/'toolchain.cs').exists())
folder=HERE/sys.argv[1]
manifest=json.loads((folder/'manifest.json').read_text())
def sha(data): return hashlib.sha256(data).hexdigest()
documents=['src/SecurityDomain/README.md','src/SecurityDomain/CLAUDE.md','src/SecurityDomain/PACKAGE_README.md',
           'src/SecurityDomain/tests/README.md','src/SecurityDomain/tests/CLAUDE.md']
actual_count=sum(len(re.findall(r'^```csharp\s*\n(.*?)^```\s*$',(ROOT/p).read_text(),re.M|re.S)) for p in documents)
assert actual_count==len(manifest['blocks']), 'New or removed blocks require inventory reconciliation'
for entry in manifest['inputs']:
    assert sha((ROOT/entry['path']).read_bytes())==entry['sha256'],entry['path']
for b in manifest['blocks']:
    text=(ROOT/b['path']).read_text()
    blocks=list(re.finditer(r'^```csharp\s*\n(.*?)^```\s*$',text,re.M|re.S))
    assert blocks[b['index']-1].group(1)==b['text'],(b['path'],b['index'])
    assert sha(b['text'].encode())==b['block_sha256']
selected={(b['path'],b['index']) for g in manifest['groups'] for b in g['blocks'] if isinstance(b['index'],int)}
excluded={(b['path'],b['index']) for b in manifest['exclusions']}
assert selected.isdisjoint(excluded)
assert selected|excluded=={(b['path'],b['index']) for b in manifest['blocks']}
for g in manifest['groups']:
    assert sha((folder/g['raw']).read_bytes())==g['raw_sha256'],g['name']
    assert g['wrapped'] in (folder/g['context']/'src/Examples.cs').read_text(),g['name']
inline=manifest['inline']
assert f'`{inline["text"]}`' in (ROOT/inline['path']).read_text()
for context in ['consumer','hardware']:
    evidence=folder/context/'evidence'
    data=json.loads((evidence/'manifest.json').read_text())
    for r in data['retained']:
        b=(evidence/r['path']).read_bytes()
        assert len(b)==r['bytes'] and sha(b)==r['sha256'],r['path']
    for r in data['records']:
        if 'retained' in r:
            b=(evidence/r['retained']).read_bytes()
            assert len(b)==r['bytes'] and sha(b)==r['sha256'],r['path']
    print(f'PASS: {context} retained compiler/source/configuration evidence')
print(f'PASS: {len(manifest["blocks"])} current fenced blocks; {len(selected)} selected, {len(excluded)} excluded; {len(manifest["groups"])} groups including inline constructor; raw/full wrapper bodies preserved.')
print('Current document hashes (prose may change independently of exact compiled blocks):')
for p in sorted({b['path'] for b in manifest['blocks']}): print(sha((ROOT/p).read_bytes()),p)
