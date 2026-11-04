"""Prepare current guide bodies; check extraction or retain actual compiler membership separately.

This integration-only host leaves all frozen module examples and evidence unchanged.
Only prepare/capture create generated artifacts, within this new validation directory.
Build and execution are deliberately absent from this script.
"""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').is_file())
WORK = Path('/tmp/opencode/piv-pr745-20261105-examples')
SELECTOR = 'Yubico.YubiKit.AuditPiv745Integration.Examples'
PROJECT = HERE / 'src' / (SELECTOR + '.csproj')
FROZEN = ROOT / 'docs/research/documentation-audit/modules'
PGP_FROZEN = FROZEN / 'openpgp/pass-1/openpgp-001/validation/examples'
SD_FROZEN = FROZEN / 'security-domain/pass-1/security-domain-001/validation/examples'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def record(path):
    raw = path.read_bytes()
    return dict(path=str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path),
                bytes=len(raw), sha256=sha(raw))


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=True).strip()


def retain(path, raw):
    assert path.is_relative_to(HERE), path
    assert not any(p.is_symlink() for p in [path, *path.parents] if p.is_relative_to(HERE))
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        assert path.read_bytes() == raw, f'Preserve existing artifact: {path}'
    else:
        with path.open('xb') as output:
            output.write(raw)


def json_bytes(value):
    return (json.dumps(value, indent=2) + '\n').encode()


def extraction():
    groups, outputs, documents = [], {}, []

    def scan(relative, count):
        path = ROOT / relative
        text = path.read_text()
        blocks = []
        for index, match in enumerate(re.finditer(r'^```csharp[^\S\n]*\n(.*?)^```[^\S\n]*$', text, re.M | re.S), 1):
            body = match[1]
            blocks.append(dict(path=relative, index=index, line=text[:match.start()].count('\n') + 2,
                               body=body, body_sha256=sha(body.encode()), source_sha256=sha(path.read_bytes())))
        assert len(blocks) == count, (relative, len(blocks), count)
        documents.append(record(path))
        return blocks

    def add(name, originals, wrapped, dependencies, transformation):
        groups.append(dict(name=name, originals=originals, wrapper=wrapped, dependencies=dependencies,
                           transformation=transformation,
                           lifetime='Complete original using/await/finally scopes, helpers, parameters and cleanup remain intact. Borrowed dependencies retain the original caller-owned lifetime.'))
        return wrapped

    def entry(block):
        match = re.match(r'(?:using [A-Za-z0-9_.]+;\n)+\n', block['body'])
        assert match, 'Expected original initial namespace directives'
        return match[0], block['body'][match.end():]

    pgp = scan('src/OpenPgp/README.md', 13)
    directives, statements = entry(pgp[0])
    code = add('OpenPgp.ReadOnlySession', [pgp[0]],
               'public static async Task InspectAsync()\n{\n' + statements + '}\n',
               'Real device discovery and OpenPGP applet; no supplied or stubbed device/session.',
               'Move exact original namespace imports to file scope; enclose all original entry statements in async method.')
    names = ['ChangeCredentials', 'RecoverUserPin', 'ConfigureInitialKdf', 'ShowKeyMetadata',
             'GenerateP256SigningKey', 'ImportRsaSigningKey', 'SignAndVerifyP256', 'DecipherAndConsume',
             'StoreAndReadCertificate', 'AttestGeneratedKey', 'ConfigureSigningPolicy', 'ResetAndInspect']
    for block, name in zip(pgp[1:], names, strict=True):
        code += '\n' + add('OpenPgp.' + name, [block], block['body'],
                           'Exact original typed method parameters and complete helper declarations; use shared original imports. ShowKeyMetadata requires real catalog APIs only.',
                           'Original method declarations/bodies remain byte-for-byte; add containing static class only.')
    outputs['src/OpenPgpExamples.cs'] = (directives +
        'namespace Yubico.YubiKit.AuditPiv745Integration.Examples;\n\npublic static class OpenPgpExamples\n{\n' + code + '}\n').encode()

    sd = scan('src/SecurityDomain/README.md', 5)
    directives, statements = entry(sd[0])
    auth = sd[1]['body']
    assert 'IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo>' in auth
    code = add('SecurityDomain.ReadOnly', [sd[0]],
               'public static async Task ReadOnlyAsync()\n{\n' + statements + '}\n',
               'Real device discovery and Security Domain session.', 'Move original namespace imports to file scope; wrap complete entry statements.')
    for name, originals, body in [('Authenticate', [sd[1]], auth),
                                  ('Import', [sd[1], sd[2]], auth + '\n' + sd[2]['body']),
                                  ('Generate', [sd[1], sd[3]], auth + '\n' + sd[3]['body']),
                                  ('Reset', [sd[4]], sd[4]['body'])]:
        code += '\n' + add('SecurityDomain.' + name, originals,
            f'public static async Task {name}Async(IYubiKey device)\n{{\n{body}}}\n',
            'Caller-selected real IYubiKey with no other live session. Import/Generate include entire original administrative-session setup; reset is a separate unauthenticated task.',
            'Wrap complete original statements; import/generation each repeat the full original authentication block in its own resource scope, matching frozen consumer host grouping.')
    expression = 'new Scp11KeyParameters(keyReference, devicePublicKey)'
    doc = ROOT / 'src/SecurityDomain/README.md'
    assert doc.read_text().count('`' + expression + '`') == 1
    original = dict(path=str(doc.relative_to(ROOT)), index='inline-constructor', body=expression,
                    body_sha256=sha(expression.encode()), source_sha256=sha(doc.read_bytes()))
    code += '\n' + add('SecurityDomain.OpenScp11', [original],
        'public static async Task OpenScp11Async(IYubiKey device, KeyReference keyReference, ECPublicKey devicePublicKey)\n{\n'
        'using var parameters = ' + expression + ';\n'
        'await using var session = await device.CreateSecurityDomainSessionAsync(\n'
        '    new SessionCreationOptions { ScpKeyParameters = parameters });\n}\n',
        'Caller supplies trusted provisioned KeyReference/ECPublicKey and real IYubiKey after disposing the administrative session.',
        'Reuse frozen consumer typed constructor wrapper; retain complete exact current inline expression and parameter/session lifetime scopes.')
    outputs['src/SecurityDomainExamples.cs'] = (directives +
        'namespace Yubico.YubiKit.AuditPiv745Integration.Examples;\n\npublic static class SecurityDomainExamples\n{\n' + code + '}\n').encode()
    for group in groups:
        raw = '\n'.join(item['body'] for item in group['originals']).encode()
        path = 'raw/' + group['name'] + '.cs.txt'
        outputs[path] = raw
        group.update(raw=path, raw_sha256=sha(raw))
    assert len(groups) == 19
    return documents, groups, outputs


def discovery():
    # Exact current toolchain discovery/filter predicates; no MSBuild or toolchain run.
    found = sorted(str(p.relative_to(ROOT)) for p in ROOT.rglob('*.csproj')
                   if '/src/' in str(p) and 'Yubico.YubiKit.' in str(p) and '/.claude/' not in str(p)
                   and SELECTOR.lower() in p.stem.lower())
    assert found == [str(PROJECT.relative_to(ROOT))], found
    refs = [row.attrib['Include'].replace('$(RepositoryRoot)/', '')
            for row in ET.parse(PROJECT).findall('.//ProjectReference')]
    assert refs == [f'src/{module}/src/Yubico.YubiKit.{module}.csproj' for module in ['Core', 'OpenPgp', 'SecurityDomain']]
    assert all((ROOT / path).is_file() for path in refs)
    return found, refs


def prepare_or_check(mode):
    documents, groups, outputs = extraction()
    found, refs = discovery()
    inputs = [ROOT / path for path in ['Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig',
                                      'scripts/toolchain/Discovery.cs', 'scripts/toolchain/Context.cs', *refs]]
    inputs += [HERE / path for path in ['Directory.Build.props', 'Directory.Build.targets', 'examples.py', 'src/' + SELECTOR + '.csproj']]
    historical = [PGP_FROZEN / 'prepare-final.py', PGP_FROZEN / 'v1/consumer/src/Examples.cs',
                  SD_FROZEN / 'prepare.py', SD_FROZEN / 'v1/consumer/src/Examples.cs', SD_FROZEN / 'v1/manifest.json']
    manifest = dict(status='prepared-awaiting-coordinator-compilation', context='current integrated guides, separate from frozen module runs',
        project=found[0], selector=SELECTOR, documents=documents, groups=groups,
        counts=dict(openpgp_readme_fences=13, security_domain_readme_fences=5, security_domain_groups=6, total_groups=19),
        inputs=[record(p) for p in inputs], historical_wrapper_sources=[record(p) for p in historical],
        frozen_security_domain_scope='Original ten groups = six README groups + contributor Inspect + three hardware-test snippets. Only the six affected README groups belong to this guide compilation.',
        generated=[dict(path=path, bytes=len(raw), sha256=sha(raw)) for path, raw in sorted(outputs.items())],
        limits='Preparation validates complete current source extraction only. Coordinator builds and captures actual Csc membership separately. The library host has no entry point; no examples, tests or hardware operations execute. Frozen historical examples retain their original API dependencies.')
    if mode == 'prepare':
        manifest['parents'] = dict(head=git('rev-parse', 'HEAD'), merge_head=git('rev-parse', 'MERGE_HEAD'))
        for path, raw in outputs.items():
            retain(HERE / path, raw)
        retain(HERE / 'manifest.json', json_bytes(manifest))
    else:
        old = json.loads((HERE / 'manifest.json').read_text())
        assert {k: v for k, v in old.items() if k != 'parents'} == manifest, 'Current source/provenance changed; use a fresh extraction context'
        for path, raw in outputs.items():
            assert (HERE / path).read_bytes() == raw, 'Generated wrapper changed: ' + path
    print(f'{mode}: 13 OpenPGP fences + 5 Security Domain fences/inline constructor = 19 full groups; unique project discovery and current input hashes verified. Compilation pending.')


def capture():
    prepare_or_check('check')
    dest = HERE / 'compiler-evidence'
    assert not dest.exists(), 'Preserve prior compiler evidence; use a fresh capture directory'
    names = ['compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt']
    def lines(name):
        return [s.strip().strip('"') for s in (WORK / name).read_text().splitlines() if s.strip()]
    policy = dict(line.split('=', 1) for line in lines('policy-final.txt'))
    assert policy['TreatWarningsAsErrors'] == 'true' and policy['RunAnalyzersDuringBuild'] == 'true'
    assert policy['SkipAnalyzers'].lower() != 'true' and policy['SkipCompilerExecution'].lower() != 'true'
    assert policy['TargetFramework'] == 'net10.0' and policy['AnalysisMode'] == 'AllEnabledByDefault'
    assert policy['Version'] == policy['YubiKitVersion'] and policy['Version']
    args = lines('csc-command-line.txt')
    assert '/warnaserror+' in args and '/warn:9999' in args
    assert not any('/skipanalyzers' in arg.lower() for arg in args)
    def resolve(value):
        return str((PROJECT.parent / value.strip('"')).resolve())
    def arguments(prefix):
        return {resolve(arg[len(prefix):]) for arg in args if arg.startswith(prefix)}
    sources = {str(Path(s).resolve()) for s in lines('compile-inputs.txt')}
    assert sources == {resolve(a) for a in args if a.endswith('.cs') and not re.match(r'^/[A-Za-z]+:', a)}
    assert all(str((HERE / 'src' / name).resolve()) in sources for name in ['OpenPgpExamples.cs', 'SecurityDomainExamples.cs'])
    refs = {str(Path(s.split('|', 1)[0]).resolve()) for s in lines('resolved-references.txt')}
    analyzers = {str(Path(s).resolve()) for s in lines('analyzers.txt')}
    assert arguments('/reference:') == refs and arguments('/analyzer:') == analyzers
    assert any('Microsoft.CodeAnalysis.NetAnalyzers.dll' in name for name in analyzers)
    assert {f'Yubico.YubiKit.{m}.dll' for m in ['Core', 'OpenPgp', 'SecurityDomain']} <= {Path(p).name for p in refs}
    records = []
    for kind, paths in [('source', sources), ('reference', refs), ('analyzer', analyzers), ('analyzer-config', arguments('/analyzerconfig:'))]:
        for name in sorted(paths):
            path = Path(name)
            row = dict(kind=kind, **record(path))
            if kind in {'source', 'analyzer-config'}:
                retained = kind + '/' + row['sha256'][:12] + '-' + path.name + '.snapshot'
                retain(dest / retained, path.read_bytes())
                row['retained'] = retained
            records.append(row)
    for name in names:
        retain(dest / name, (WORK / name).read_bytes())
    retain(dest / 'project.assets.json', (WORK / 'obj/project.assets.json').read_bytes())
    for path in ['Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig']:
        retain(dest / (path + '.snapshot'), (ROOT / path).read_bytes())
    retain(dest / 'host.csproj.snapshot', PROJECT.read_bytes())
    retain(dest / 'manifest.json', json_bytes(dict(policy=policy, records=records,
        counts=dict(sources=len(sources), references=len(refs), analyzers=len(analyzers)),
        retained=[record(p) for p in sorted(dest.rglob('*')) if p.is_file()],
        limits='Actual Csc source/reference/analyzer membership and binary identities captured after coordinator build. Read the separate toolchain RESULT for build success. Compilation alone establishes no device behavior or conformance.')))
    print('Captured actual compiler membership and current dependency identities; no example execution.')


if __name__ == '__main__':
    assert sys.argv[1:] in [['prepare'], ['check'], ['capture']], 'Usage: examples.py prepare|check|capture'
    if sys.argv[1] == 'capture':
        capture()
    else:
        prepare_or_check(sys.argv[1])
