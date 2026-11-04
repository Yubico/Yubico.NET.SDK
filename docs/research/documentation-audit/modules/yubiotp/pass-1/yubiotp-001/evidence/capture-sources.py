"""Read-only HTTP evidence capture; writes only this run's evidence directory."""
import hashlib
import json
from pathlib import Path
import urllib.request
import urllib.error

root = Path(__file__).parent
sources = {
    'ykman-5.8.0-yubiotp.py': 'https://raw.githubusercontent.com/Yubico/yubikey-manager/5.8.0/yubikit/yubiotp.py',
    'personalization-1.20.0-ykdef.h': 'https://raw.githubusercontent.com/Yubico/yubikey-personalization/v1.20.0/ykcore/ykdef.h',
    'personalization-1.20.0-manual.txt': 'https://raw.githubusercontent.com/Yubico/yubikey-personalization/v1.20.0/ykpersonalize.1.adoc',
    'vendor-ykpersonalize.html': 'https://developers.yubico.com/yubikey-personalization/Manuals/ykpersonalize.1.html',
    'vendor-otp-explained.html': 'https://developers.yubico.com/OTP/OTPs_Explained.html',
    'vendor-technical-manual-v3.3.pdf': 'https://downloads.yubico.com/support/yubico_YubiKey-Technical-Manual-v3.3_2014.pdf',
    'vendor-manual-v3.4.pdf': 'https://www.yubico.com/wp-content/uploads/2015/03/YubiKeyManual_v3.4.pdf',
    'rfc4226.txt': 'https://www.rfc-editor.org/rfc/rfc4226.txt',
    'failed-otp-related.html': 'https://developers.yubico.com/OTP/Specifications/OTP_related_commands.html',
    'failed-challenge-response.html': 'https://developers.yubico.com/OTP/Challenge-Response.html',
}
records = []
for name, url in sources.items():
    try:
        with urllib.request.urlopen(url, timeout=40) as response:
            data = response.read()
            (root / name).write_bytes(data)
            record = dict(file=name, url=url, status=response.status, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
    except Exception as error:
        record = dict(file=None, attempted_file=name, url=url, error=str(error))
    records.append(record)
    print(json.dumps(record))
(root / 'source-capture.json').write_text(json.dumps(dict(access_date='2026-11-04', sources=records), indent=2) + '\n')
