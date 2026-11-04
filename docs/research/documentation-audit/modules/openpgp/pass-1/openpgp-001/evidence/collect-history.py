"""Focused follow-up: preserve history patches without contacting hardware or running SDK code."""
from pathlib import Path
import subprocess
import urllib.request

here = Path(__file__).resolve().parent
repo = '/tmp/opencode/openpgp-001-ykman'
for revision in [
    '9c0a0999d615859dfb195aac112775e266e48e10',
    '11b6c36e85dc81b7e197986c664b6c6521687df9',
    '31459ea393e1e3d603dea99152fbfbd2917f5280',
]:
    (here / f'ykman-history-{revision[:12]}.patch').write_bytes(subprocess.check_output(
        ['git', 'show', '--format=fuller', revision, '--', 'ykman/opgp.py', 'ykman/openpgp.py', 'yubikit/openpgp.py'], cwd=repo))
url = 'https://www.yubico.com/support/security-advisories/ysa-2017-01/'
(here / 'vendor-ysa-2017-01.html').write_bytes(urllib.request.urlopen(url, timeout=60).read())
print('Saved focused ROCA/NEO/status history and original YSA-2017-01.')
