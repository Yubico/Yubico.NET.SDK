"""Read-only source acquisition for the documentation investigation; no SDK/device execution."""
import hashlib
import json
from pathlib import Path
import subprocess
import urllib.request

HERE = Path(__file__).resolve().parent
urls = {
    'openpgp-card-3.4.pdf': 'https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf',
    'rfc9580.txt': 'https://www.rfc-editor.org/rfc/rfc9580.txt',
    'rfc8017.txt': 'https://www.rfc-editor.org/rfc/rfc8017.txt',
    'rfc8032.txt': 'https://www.rfc-editor.org/rfc/rfc8032.txt',
    'rfc7748.txt': 'https://www.rfc-editor.org/rfc/rfc7748.txt',
}
records = []
for name, url in urls.items():
    data = urllib.request.urlopen(url, timeout=60).read()
    (HERE / name).write_bytes(data)
    records.append(dict(path=name, url=url, accessed='2026-11-04', bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
repo = Path('/tmp/opencode/openpgp-001-ykman')
revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=repo, text=True).strip()
for source in ['yubikit/openpgp.py', 'ykman/_cli/openpgp.py', 'tests/device/test_openpgp.py', 'tests/device/cli/test_openpgp.py']:
    data = (repo / source).read_bytes()
    name = 'ykman-' + source.replace('/', '-')
    (HERE / name).write_bytes(data)
    records.append(dict(path=name, url=f'https://github.com/Yubico/yubikey-manager/blob/{revision}/{source}', revision=revision, accessed='2026-11-04', bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
(HERE / 'source-manifest.json').write_text(json.dumps(records, indent=2) + '\n')
subprocess.run(['pdftotext', '-layout', str(HERE / 'openpgp-card-3.4.pdf'), str(HERE / 'openpgp-card-3.4.txt')], check=True)
print(json.dumps(dict(revision=revision, acquired=len(records)), indent=2))

# The selected commits preserve original changes as corroboration, including reasons in messages.
commits = [
    '0611c18f40122ad115829e1f59920bf69d1062db',
    'c089ec47b7b78573128602397fb2e3c42719571c',
    '11b6c36e85dc81b7e197986c664b6c6521687df9',
    'ab83e442496341e2634f87c88f1299e6705b96f5',
    '1e44cf75fc92368bfaadf9500aab046d0d972bfe',
    'c883b926a9ded48cb2dc8d6116d388124b537c81',
    '6870378ce2c4138a79598425182dfc70a247299a',
    '0ba0ad0e3fd23059367415b7e7c116cb3c05a175',
    '4a2d6abba44ffbb8d0a33639765beffc9506f475',
    '69b535096f60ae34e9a0d828e0239c8afe332000',
    '9654350803d6fcce2e6a0c432a063989fbce0922',
    'fe09b795f1ad858dc5770591e75adfee0fb61345',
    '31459ea393e1e3d603dea99152fbfbd2917f5280',
]
for commit in commits:
    (HERE / f'ykman-history-{commit[:12]}.patch').write_bytes(subprocess.check_output(['git', 'show', '--format=fuller', commit], cwd=repo))
for name, url in {
    'vendor-attestation.html': 'https://developers.yubico.com/PGP/Attestation.html',
    'vendor-openpgp-commands.html': 'https://docs.yubico.com/software/yubikey/tools/ykman/OpenPGP_Commands.html',
    'vendor-card-edit.html': 'https://developers.yubico.com/PGP/Card_edit.html',
    'vendor-technical-manual.html': 'https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-openpgp.html',
}.items():
    data = urllib.request.urlopen(url, timeout=60).read()
    (HERE / name).write_bytes(data)
    records.append(dict(path=name, url=url, accessed='2026-11-04', bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
(HERE / 'source-manifest.json').write_text(json.dumps(records, indent=2) + '\n')
