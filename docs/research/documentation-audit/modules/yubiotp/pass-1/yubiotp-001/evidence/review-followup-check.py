"""Read/hash-only bounded correction and hardware evidence reconciliation; never runs device/tests."""
import hashlib
import json
from pathlib import Path

RUN = Path(__file__).parent.parent
E = RUN / 'evidence'
original = json.loads((E / 'review-content-manifest.json').read_text())
expected = {
    'src/YubiOtp/src/ExtendedFlag.cs': '6ac9449167e736fe03b5475eafdf1364cd49ce1319b7a4607271578e5ca9f7c1',
    'src/YubiOtp/examples/OtpTool/Commands/YubiOtpCommand.cs': 'e129bcfbfc3ff12f64eb5f0d4c5fe688b8bd63078f4f26d50d9dba542ff7c45d',
    'src/YubiOtp/README.md': '5ba4557b704e023e231e09208f584cdf8bbf1dc8753f8d437d8558ef2ff32eba',
}
reverse = {
    'src/YubiOtp/src/ExtendedFlag.cs': [(
        '/// On legacy devices serial visibility merges across both slots. The personalization v1.20.0 manual documents\n/// button visibility for 2.x and USB serial unavailability on 3.0/3.1; these are legacy applicability notes, not universal',
        '/// On legacy devices serial visibility merges across both slots. Manual 3.3 documents button visibility\n/// for 2.x and USB serial unavailability on 3.0/3.1; these are legacy applicability notes, not universal')],
    'src/YubiOtp/examples/OtpTool/Commands/YubiOtpCommand.cs': [(
        "/// Parsed secret strings are immutable and parser diagnostics can echo input. The command's finally\n/// clears its acquired AES-key, private-ID and access-code arrays, not those strings or captured output.",
        "/// Parsed secret strings are immutable; arrays allocated before later parsing failures are not all\n/// protected by the command's finally cleanup. Use the SDK guide's ownership pattern for application code.")],
    'src/YubiOtp/README.md': [(
        'This application-dependent provisioning fragment generates a new key. Securely enroll that same key with\nyour verifier before it is cleared; a random key known only to this process is not a deployed verifier.\n\n', ''), (
        "    // Securely enroll hmacKey with your verifier here as part of your application's provisioning.\n", '')]
}

def sha(data):
    return hashlib.sha256(data).hexdigest()

def identity(path):
    data = path.read_bytes()
    return dict(path=str(path), bytes=len(data), sha256=sha(data))

files = []
for old in original['files']:
    path = old['path']
    data = Path(path).read_bytes()
    actual = sha(data)
    assert actual == expected.get(path, old['sha256']), path
    reconstruction = None
    if path in reverse:
        before = data.decode()
        for after, prior in reverse[path]:
            assert before.count(after) == 1, (path, after)
            before = before.replace(after, prior, 1)
        reconstruction = sha(before.encode())
        assert reconstruction == old['sha256'], path
    files.append(dict(path=path, bytes=len(data), sha256=actual, verdict='accepted-content',
                      original_review_sha256=old['sha256'],
                      unchanged_since_original_review=(actual == old['sha256']),
                      correction_reverse_sha256=reconstruction,
                      scope=old['scope']))
assert len(files) == 50
assert sum(f['unchanged_since_original_review'] for f in files) == 47
assert sha((RUN / 'review.md').read_bytes()) == '93305ccdd9b5c6b235e3e86fe367387b5a5c5d87369b61e5483f69d8eca16dae'

probe = RUN / 'validation/hardware-probe/tests/Yubico.YubiKit.AuditOtp001.Hardware.IntegrationTests/src/HardwareProbeTests.cs'
capture = RUN / 'validation/hardware-probe/evidence/v1-build/capture.json'
c = json.loads(capture.read_text())
compiled_source = next(r for r in c['dependency_identities'] if r['kind'] == 'compile-inputs.txt' and r['path'].endswith('/HardwareProbeTests.cs'))
assert identity(probe)['sha256'] == compiled_source['sha256'] == '1a9dd85f2a0856d0eb61581624e38737c217266d619dc988b7dd3367b3e9bdf6'
compile_inputs = capture.parent / 'compile-inputs.txt'
assert str(probe.resolve()) in compile_inputs.read_text()

logs = []
for pattern in ['integration-reader-released-*/01-test-YubiOtp.IntegrationTests.log',
                'integration-successful-touch-*/01-test-YubiOtp.IntegrationTests.log',
                'hardware-probe-v1-status-*/01-test-AuditOtp001.Hardware.IntegrationTests.log',
                'hardware-probe-v1-aes-*/01-test-AuditOtp001.Hardware.IntegrationTests.log',
                'hardware-probe-v1-cancel-*/01-test-AuditOtp001.Hardware.IntegrationTests.log',
                'hardware-probe-v1-timeout-*/01-test-AuditOtp001.Hardware.IntegrationTests.log']:
    matches = list((RUN / 'validation').glob(pattern))
    assert len(matches) == 1, pattern
    path = matches[0]
    text = path.read_text()
    assert 'Test Run Successful.' in text
    passed = next(line.strip() for line in text.splitlines() if line.strip().startswith('Passed:'))
    logs.append(dict(**identity(path), passed=int(passed.split(':')[1]), read_scope='full raw log'))
assert sum(r['passed'] for r in logs) == 20
other = [RUN / 'review-followup.md', Path(__file__), RUN / 'hardware/results.md', RUN / 'hardware/final-info.log', RUN / 'hardware/final-slots.log',
         RUN / 'hardware/initial-info.log', RUN / 'hardware/initial-slots.log',
         RUN / 'edit-corrections.md', E / 'sources.md', RUN / 'review.md',
         RUN / 'validation/integration-initial-20261104-111329-828-test/01-test-YubiOtp.IntegrationTests.log',
         probe, capture, compile_inputs,
         probe.parent / 'probe.runsettings', probe.parent / 'Yubico.YubiKit.AuditOtp001.Hardware.IntegrationTests.csproj',
         Path('src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/UserPresenceProbeTests.cs'),
         Path('src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/AuditV2/YubiOtpHmacChallengeKnownAnswerTests.cs')]
result = dict(date='2026-11-04', reviewer='github-copilot/gpt-6.1-sol',
              baseline=original['baseline'], verdict='PASS content and bounded hardware-impact review',
              accepted_paths=50, unchanged_since_original_review=47, corrected_paths=3,
              original_review_preserved=True, files=files,
              hardware_scope='serial103; Linux/Omarchy; metadata5.8.0.beta.0; raw/effectiveOTP0.0.1; explicit USB HID and USB SmartCard only',
              probe_compiler_source_sha256=compiled_source['sha256'], raw_execution_logs=logs,
              other_evidence_identities=[identity(p) for p in other],
              limits=['No test/build/device command executed by reviewer',
                      'No inference of dummy-reset bytes, native lifetime, same-session recovery, NFC read-back or universal firmware support',
                      'Shared document acceptance limited to five assigned YubiOTP sections',
                      'Final formatter/build/analyzer/token and record gates are separate'])
(E / 'review-final-content.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({k:result[k] for k in ['verdict','accepted_paths','unchanged_since_original_review','corrected_paths','original_review_preserved','probe_compiler_source_sha256']}, indent=2))
print('Retained raw log executed counts:', [r['passed'] for r in logs])
