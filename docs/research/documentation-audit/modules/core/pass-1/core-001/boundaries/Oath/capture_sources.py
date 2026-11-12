#!/usr/bin/env python3
"""Capture the public OATH original and hash reused evidence; no package imports/devices."""
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import subprocess
import urllib.request

here = Path(__file__).resolve().parent
run = here.parent.parent
root = next(parent for parent in here.parents if (parent/'.git').exists())
sha = lambda data: hashlib.sha256(data).hexdigest()
url = 'https://developers.yubico.com/OATH/YKOATH_Protocol.html'
request = urllib.request.Request(url, headers={'User-Agent':'Core-OATH-static-documentation-audit'})
with urllib.request.urlopen(request, timeout=40) as response:
    raw = response.read()
    capture = dict(url=url, accessed_utc=datetime.now(timezone.utc).isoformat(),
        status=response.status, sha256=sha(raw), bytes=len(raw),
        content_type=response.headers.get('Content-Type'),
        path='ykoath-protocol.html', revision='Unversioned public vendor page',
        role='Original public vendor protocol reference, not GP conformance/device qualification')
(here/'ykoath-protocol.html').write_bytes(raw)
paths = [
 'sources/python/manifest.json','sources/python/context.md',
 'sources/python/history/1b87964ba258e3ce39fd2959888dfe7cf81a6843.patch',
 'investigation/B6/sources/scp03-1.1.2.pdf','investigation/B6/sources/scp03-1.1.2.txt',
 'investigation/B6/sources/scp11-public-review-1.3.0.13.pdf',
 'investigation/B6/sources/scp11-public-review-1.3.0.13.txt',
 'investigation/B6/sources/final-source-register.md','investigation/B6/sources/final-manifest.json',
 'investigation/C/sources/rfc4648.txt','investigation/C/sources/final-source-register.md',
 'inherited-findings/registry.json','inventory/inherited-routing.json',
 'inventory/inherited-packet-inputs.json','inherited-findings/boundary-seeds.md'
]
reused = [dict(path=str((run/path).relative_to(root)),sha256=sha((run/path).read_bytes()),
    bytes=(run/path).stat().st_size,role='Read-only reused evidence; reading scope in sources.md') for path in paths]
pin = '4ca60f706af930459138d8dc0f0f953480e1c7a4'
data = subprocess.check_output(['git','show',pin+':yubikit/oath.py'],
    cwd='/home/rupicola/Code/y/yubikey-manager')
reused.append(dict(path='yubikit/oath.py', revision=pin, sha256=sha(data),bytes=len(data),
    url=f'https://raw.githubusercontent.com/Yubico/yubikey-manager/{pin}/yubikit/oath.py',
    role='Read complete file as source comparator; no import, test or crypto execution'))
manifest = dict(original_capture=capture,reused=reused,
    context7=dict(library_id='/dotnet/docs',query='Memory<T> ownership consumers async methods keep buffer alive until returned Task terminal; ReadOnlyMemory does not imply immutable backing storage',
        source='https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md',
        role='Current API/design guidance, unversioned query result; no runtime qualification'),
    research_bounds='Reuse completed initial+focused gaps from B6/C; no additional final-GP/ISO/hardware-rationale attempts.')
(here/'source-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(dict(vendor_sha256=capture['sha256'],vendor_bytes=len(raw),reused_sources=len(reused)),indent=2))
