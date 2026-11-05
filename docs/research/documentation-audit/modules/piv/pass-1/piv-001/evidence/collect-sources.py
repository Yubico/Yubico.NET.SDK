"""Read-only primary-source capture for the PIV documentation investigation."""
import concurrent.futures, datetime, hashlib, json, pathlib, subprocess, urllib.request

ROOT = pathlib.Path(__file__).parent
PIN = '4ca60f706af930459138d8dc0f0f953480e1c7a4'
SOURCES = {
    'nist-sp800-73-4-part1.pdf': 'https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73-4.pdf',
    'nist-sp800-73-4-part2.pdf': 'https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73-4pt2.pdf',
    'nist-sp800-78-4.pdf': 'https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-78-4.pdf',
    'nist-fips201-2.pdf': 'https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.201-2.pdf',
    'nist-fips186-5.pdf': 'https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.186-5.pdf',
    'vendor-reset.html': 'https://docs.yubico.com/yesdk/users-manual/application-piv/apdu/reset.html',
    'vendor-move.html': 'https://docs.yubico.com/yesdk/users-manual/application-piv/apdu/move-key.html',
    'vendor-attestation.html': 'https://docs.yubico.com/yesdk/users-manual/application-piv/attestation.html',
    'vendor-pin-puk.html': 'https://docs.yubico.com/yesdk/users-manual/application-piv/pin-puk-mgmt-key.html',
    'vendor-ysa2017-01.html': 'https://www.yubico.com/support/security-advisories/ysa-2017-01/',
    'rfc8017.txt': 'https://www.rfc-editor.org/rfc/rfc8017.txt',
    'rfc8032.txt': 'https://www.rfc-editor.org/rfc/rfc8032.txt',
    'rfc7748.txt': 'https://www.rfc-editor.org/rfc/rfc7748.txt',
    'rfc3279.txt': 'https://www.rfc-editor.org/rfc/rfc3279.txt',
}
for path in ['yubikit/piv.py', 'ykman/piv.py', 'ykman/_cli/piv.py', 'tests/test_piv.py', 'tests/device/test_piv.py', 'tests/device/cli/test_piv.py']:
    SOURCES['python-'+path.replace('/', '-')] = f'https://raw.githubusercontent.com/Yubico/yubikey-manager/{PIN}/{path}'
SOURCES['android-PivSession.java'] = 'https://raw.githubusercontent.com/Yubico/yubikit-android/76b89d35142d9a4937b6b72e5c840e057a280620/piv/src/main/java/com/yubico/yubikit/piv/PivSession.java'

def get(item):
    name, url = item
    entry = {'file': name, 'url': url, 'accessed_utc': datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        data = urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent':'PIV-documentation-audit'}), timeout=45).read()
        (ROOT/name).write_bytes(data)
        entry.update(bytes=len(data), sha256=hashlib.sha256(data).hexdigest(), status='captured')
        if name.endswith('.pdf'):
            subprocess.run(['pdftotext','-layout',str(ROOT/name),str(ROOT/name.replace('.pdf','.txt'))], check=True, capture_output=True)
    except Exception as exc:
        entry.update(status='gap', error=str(exc))
    return entry

with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
    manifest = list(pool.map(get, SOURCES.items()))
(ROOT/'source-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
print(json.dumps(manifest, indent=2))
