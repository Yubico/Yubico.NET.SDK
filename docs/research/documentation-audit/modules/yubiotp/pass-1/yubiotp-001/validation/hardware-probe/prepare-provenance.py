"""Record source/config provenance and inspect syntax/layout only; never run dotnet or hardware."""
from pathlib import Path
import ast
import hashlib
import json
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
BASELINE = '95e5ccaeda33954c21f2a38058d4f66b5f208787'
TEMPLATE = 'docs/research/documentation-audit/modules/security-domain/pass-1/security-domain-001/validation/examples/v1/hardware/'
INPUTS = {
    'Directory.Build.props': 'Canonical language, test analyzer and warning policy imported without relaxation.',
    'Directory.Build.targets': 'Canonical common analyzer packages imported without relaxation.',
    'Directory.Packages.props': 'Canonical central package versions: xUnit v2 and VSTest packages.',
    '.editorconfig': 'Canonical source style/analyzer context.',
    'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/Yubico.YubiKit.YubiOtp.IntegrationTests.csproj': 'Actual integration package/reference template; project references retargeted to root paths, no existing tests linked.',
    'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/xunit.runner.json': 'Runner JSON copied exactly: disabled assembly/collection parallelism, one thread and live output.',
    'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/UserPresenceProbeTests.cs': 'Public prompt interface/lifecycle and known-answer reference, not linked or run.',
    'src/YubiOtp/src/IYubiOtpSession.cs': 'Public slot/configuration/challenge API signatures.',
    'src/YubiOtp/src/IYubiKeyExtensions.cs': 'Explicit transport and owned convenience-session lifetime.',
    'src/YubiOtp/src/YubiOtpSession.cs': 'Module API implementation for configuration, response ownership and cached status; no Core implementation read.',
    'src/YubiOtp/src/ConfigState.cs': 'Public observed status fields; disputed touch indicators logged, not preassumed.',
    'src/YubiOtp/src/HmacSha1SlotConfiguration.cs': 'Non-touch/RequireTouch/short challenge provisioning.',
    'src/YubiOtp/src/YubicoOtpChallengeResponseSlotConfiguration.cs': 'AES key provisioning with zero UID and six-byte challenge contract.',
    'src/YubiOtp/src/StaticPasswordSlotConfiguration.cs': 'Raw-scan-code constructor, never triggered by this probe.',
    'src/YubiOtp/src/KeyboardSlotConfiguration.cs': 'AppendCr(false) on the raw-static measurement configuration.',
    'src/YubiOtp/src/SlotConfiguration.cs': 'Owned cloned configuration buffers disposed; API-visible defaults.',
    'src/YubiOtp/src/Backend/HidBackend.cs': 'Module-layer terminal mapping/returned buffer handling; timeout identified by module catch name only.',
    'src/YubiOtp/src/Backend/SmartCardBackend.cs': 'Module-layer APDU errors and notification lifecycle; no transport-internals prediction.',
    'src/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj': 'Real discovery/allow-list library reference and output appsettings source.',
    'src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs': 'Real lazy device-binding/custom-filter attribute, no fixture provisioning.',
    'src/Tests.Shared/IYubiKeyFilter.cs': 'Serial103Filter contract.',
    'src/Tests.Shared/YubiKeyTestState.cs': 'Authorized device/metadata wrapper.',
    'src/Core/src/PublicAPI.Unshipped.txt': 'Public signatures only: session transport, identity, APDU SW, version qualifier and prompts; no Core implementation.',
    'scripts/toolchain/Discovery.cs': 'Recursive tests/src path and unique selector discovery.',
    'scripts/toolchain/Context.cs': 'Buildable src path plus integration tests path discovery.',
    'scripts/toolchain/TestTargets.cs': 'Real xUnit-v2/VSTest invocation and normal-verbosity output retention.',
    TEMPLATE + 'Directory.Build.props': 'Adapted canonical-import/isolated-output host pattern, output path changed to YubiOTP probe.',
    TEMPLATE + 'Directory.Build.targets': 'Adapted compiler input/policy capture pattern, target names changed; not SD example bodies.',
}

def sha(raw):
    return hashlib.sha256(raw).hexdigest()

def row(path, role=None):
    raw = path.read_bytes()
    result = dict(path=str(path.relative_to(ROOT)), bytes=len(raw), sha256=sha(raw))
    if role is not None:
        result['role'] = role
    return result

if sys.argv[1:] not in (['write'], ['verify']):
    raise SystemExit('Usage: prepare-provenance.py write|verify')
for path in HERE.glob('*.py'):
    ast.parse(path.read_text(), filename=str(path))
for pattern in ('*.csproj', '*.props', '*.targets', '*.runsettings'):
    for path in HERE.rglob(pattern):
        ET.parse(path)
project = next(HERE.rglob('*.csproj'))
matches = [p for p in ROOT.rglob('*.csproj') if 'auditotp001.hardware.integrationtests' in p.stem.lower()]
if matches != [project] or 'tests' not in project.parts or 'src' not in project.parts:
    raise SystemExit('Host selector collision or incompatible toolchain discovery layout')
template = ROOT / 'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/Yubico.YubiKit.YubiOtp.IntegrationTests.csproj'
packages = lambda path: {node.attrib['Include'] for node in ET.parse(path).findall('.//PackageReference')}
if packages(project) != packages(template):
    raise SystemExit('Probe packages differ from the genuine YubiOtp integration template')
runner = project.parent / 'xunit.runner.json'
canonical_runner = template.parent / 'xunit.runner.json'
if json.loads(runner.read_text()) != json.loads(canonical_runner.read_text()):
    raise SystemExit('Probe runner settings differ from canonical serialized hardware runner')
settings = ET.parse(project.parent / 'probe.runsettings')
if settings.findtext('./RunConfiguration/TestSessionTimeout') != '90000':
    raise SystemExit('Missing 90-second VSTest host bound')
manual = RUN / 'evidence/vendor-technical-manual-v3.3.txt'
lines = manual.read_text().splitlines()
vendor = dict(source=row(manual), original_pdf=dict(
    url='https://downloads.yubico.com/support/yubico_YubiKey-Technical-Manual-v3.3_2014.pdf',
    sha256='0f4fb9224b1247a57d9b8b13c3bc370512cf17d9027e2013104217e9032a45ca'),
    title='The YubiKey Manual', revision='3.3, 17 September 2014',
    passages=[dict(section=section, lines=[first, last], text='\n'.join(lines[first-1:last]))
              for section, first, last in [('4.3', 926, 931), ('6.1.1', 1480, 1493),
                                           ('6.1.6', 1557, 1564), ('6.3', 1591, 1627)]],
    limits='Legacy manual timing motivates a longer outer token; modern timeout is measured. CRC verifier is an independent bitwise implementation, not SDK ChecksumUtils.')
inputs = [row(ROOT / path, role) for path, role in INPUTS.items()]
sources = []
for path in sorted(HERE.rglob('*')):
    if path.is_symlink():
        raise SystemExit(f'Symlinked probe source: {path}')
    if path.is_file() and path.name not in ('provenance.json', 'preparation-static.log') and 'evidence' not in path.relative_to(HERE).parts:
        if any(part.lower() in ('bin', 'obj', '__pycache__') for part in path.relative_to(HERE).parts):
            raise SystemExit(f'Generated outputs retained: {path}')
        path.read_bytes().decode('utf-8')
        sources.append(row(path))
result = dict(run='yubiotp-001', baseline=BASELINE, status='prepared-only-not-compiled-not-executed',
              identity='serial 103, user-reported firmware 5.8.0.beta.0; fresh read-only identity/status before writes',
              project=row(project), sources=sources, input_provenance=inputs, vendor=vendor,
              external_api_docs=[
                  'https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-10.0',
                  'https://xunit.net/docs/running-tests-in-parallel',
                  'https://xunit.net/docs/config-runsettings',
                  'https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=visualstudio'],
              policy='Canonical props/targets/packages; no warning relaxation, mocks, raw I/O, Core implementation or hardware execution. Optional access-code lane omitted.')
target = HERE / 'provenance.json'
data = (json.dumps(result, indent=2) + '\n').encode('utf-8')
if sys.argv[1] == 'write':
    with target.open('xb') as output:
        output.write(data)
elif target.read_bytes() != data:
    raise SystemExit('Probe provenance differs from current source/config inputs; retain originals before preparing a new revision')
print(f'PASS: static preparation only; {len(sources)} probe source/config records, {len(inputs)} original input hashes, verified vendor passages retained, one discoverable unique host.')
print('PASS: Python/XML syntax, canonical integration package set/runner policy, and declared 90-second VSTest bound inspected without executing tests.')
print('NOT RUN: C# build/analyzers, xUnit discovery/execution, device I/O, formatter, tests or hardware validation.')
