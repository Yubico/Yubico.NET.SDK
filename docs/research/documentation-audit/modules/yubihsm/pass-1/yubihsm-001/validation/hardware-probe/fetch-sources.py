"""Retain focused original-source excerpts and whole-response identities; no vendor code runs."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
from urllib.request import Request, urlopen

HERE = Path(__file__).resolve().parent
COMMIT = '44a5ab52e0697c67b36c6dec953db08a6b7b183a'
SOURCES = [
    dict(name='python-yubihsm-core', url=f'https://raw.githubusercontent.com/Yubico/python-yubihsm/{COMMIT}/yubihsm/core.py',
         revision=COMMIT, sections=[('KEY_ENC =', '\ndef _unpad_resp('), ('class SymmetricAuth:', '\n\nclass AsymmetricAuth:')]),
    dict(name='rfc4493', url='https://www.rfc-editor.org/rfc/rfc4493.txt', revision='RFC 4493, June 2006, Informational',
         sections=[('2.3.  Subkey Generation Algorithm\n', '\n2.5.  MAC Verification Algorithm'),
                   ('4.  Test Vectors\n', '\n5.  Acknowledgement')]),
]
destination = HERE / 'sources'
destination.mkdir(exist_ok=False)
rows = []
for source in SOURCES:
    with urlopen(Request(source['url'], headers={'User-Agent': 'YubiHsm001-source-preparation'}), timeout=30) as response:
        raw = response.read()
        resolved_url = response.url
    text = raw.decode('utf-8')
    excerpts = []
    for index, (start_marker, end_marker) in enumerate(source['sections'], 1):
        start = text.index(start_marker)
        end = text.index(end_marker, start)
        data = text[start:end].encode('utf-8')
        name = f"{source['name']}-{index}.txt"
        with (destination / name).open('xb') as output:
            output.write(data)
        excerpts.append(dict(path=name, bytes=len(data), sha256=hashlib.sha256(data).hexdigest(),
                             start_line=text[:start].count('\n') + 1, end_line=text[:end].count('\n') + 1,
                             start_marker=start_marker, exclusive_end_marker=end_marker))
    rows.append(dict(url=source['url'], resolved_url=resolved_url, revision=source['revision'],
                     accessed_utc=datetime.now(timezone.utc).isoformat(), response_bytes=len(raw),
                     response_sha256=hashlib.sha256(raw).hexdigest(), excerpts=excerpts))
with (destination / 'provenance.json').open('x') as output:
    json.dump(dict(sources=rows, policy='Original-source excerpts only; full response hashes identify fetches. Vendor implementation corroborates the local test KDF; RFC supplies independent CMAC primitive vectors. No HSM conformance claim.'), output, indent=2)
    output.write('\n')
print('Retained pinned vendor KDF/key/cryptogram mapping and RFC 4493 CMAC algorithm/vector excerpts; zero code execution.')
