"""Preserve an existing failed CAPTURE and its successful build; never executes compiler proof or SDK."""
import hashlib
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').is_file())
RUNTIME = Path('/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof')


def require(value, message):
    if not value:
        raise ValueError(message)


def main():
    require(len(sys.argv) == 1, 'This bounded preservation owns only the existing probe-first failure')
    destination = HERE / 'correction-v2/failure-preservation'
    destination.mkdir(parents=True, exist_ok=False)
    binary_root = RUNTIME / 'preserved-capture-failure-probe-first-v2'
    binary_root.mkdir(exist_ok=False)
    records = []

    def keep(path, kind, binary=False):
        path = path.resolve()
        require(path.is_file() and not path.is_symlink(), 'Missing/unsafe original input: ' + str(path))
        raw = path.read_bytes()
        digest = hashlib.sha256(raw).hexdigest()
        target = binary_root / (digest + '-' + path.name) if binary else destination / 'snapshots' / (digest + '-' + path.name + '.snapshot')
        if not binary:
            raw.decode('utf-8-sig')
            require(b'\0' not in raw, 'Binary in text retention: ' + str(path))
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists():
            require(target.read_bytes() == raw, 'Content-address collision')
        else:
            with target.open('xb') as output:
                output.write(raw)
        records.append(dict(path=str(path), kind=kind, bytes=len(raw), sha256=digest, binary=binary,
                            retained=str(target) if binary else str(target.relative_to(destination))))

    commands = HERE.parent / 'commands-v2'
    for label in ('consumer-probe-build-first', 'consumer-probe-capture-first', 'consumer-probe-failure-retention-first'):
        for path in sorted((commands / label).rglob('*')):
            if path.is_file():
                keep(path, 'original-command-' + label)
    build = json.loads((commands / 'consumer-probe-build-first/receipt.json').read_bytes())
    capture = json.loads((commands / 'consumer-probe-capture-first/receipt.json').read_bytes())
    misuse = json.loads((commands / 'consumer-probe-failure-retention-first/receipt.json').read_bytes())
    require(build['status'] == 'passed' and build['exit_code'] == 0, 'This capture has no successful original build')
    require(capture['status'] == misuse['status'] == 'failed', 'Original failure classification changed')
    for path in sorted((HERE / 'evidence/probe-first').rglob('*')):
        if path.is_file():
            keep(path, 'original-partial-capture')
    for row in json.loads((HERE / 'preparation-seal.json').read_bytes())['files']:
        path = Path(row['path'])
        require(hashlib.sha256(path.read_bytes()).hexdigest() == row['sha256'], 'Original sealed source changed')
        keep(path, 'original-sealed-input')
    keep(HERE / 'preparation-seal.json', 'original-static-seal')
    for folder in sorted((RUNTIME / 'attempts/probe-first/raw').iterdir()):
        policy = dict(line.split('=', 1) for line in (folder / 'policy.txt').read_text(encoding='utf-8-sig').splitlines() if line.strip())
        project = Path(policy['MSBuildProjectFullPath'])

        def resolve(value):
            return (project.parent / value.strip('"').replace('\\', '/')).resolve()

        for path in sorted(folder.rglob('*')):
            if path.is_file():
                keep(path, 'original-compiler-observation')
        for name, binary in (('sources.txt', False), ('configs.txt', False), ('additional-files.txt', False),
                             ('imports.txt', False), ('references.txt', True), ('analyzers.txt', True),
                             ('copy-local.txt', True), ('native-assets.txt', True)):
            for value in (folder / name).read_text(encoding='utf-8-sig').splitlines():
                if value.strip():
                    keep(resolve(value.strip().split('|', 1)[0]), 'original-' + name, binary)
        extensions = resolve(policy['MSBuildProjectExtensionsPath'])
        for suffix in ('.nuget.g.props', '.nuget.g.targets', '.nuget.dgspec.json'):
            keep(extensions / (project.name + suffix), 'original-generated-nuget')
        keep(resolve(policy['ProjectAssetsFile']), 'original-restored-assets')
        keep(project, 'original-compiled-project')
        for path in sorted(resolve(policy['TargetDir']).rglob('*')):
            if path.is_file():
                keep(path, 'original-successful-output', True)
        compiler = Path(policy['MSBuildSDKsPath']).parent / 'Roslyn/bincore'
        for path in sorted(compiler.iterdir()):
            if path.is_file() and (path.suffix == '.dll' or path.name.startswith('csc.')):
                keep(path, 'original-compiler-binary', True)
    package = Path('/home/rupicola/.nuget/packages/microsoft.codeanalysis.netanalyzers/9.0.0')
    for name in ('Microsoft.CodeAnalysis.NetAnalyzers.props', 'DisableNETAnalyzersForNuGetPackage.props',
                 'Microsoft.CodeAnalysis.NetAnalyzers.targets'):
        keep(package / 'buildTransitive' / name, 'original-canonical-analyzer-import')
    for name in ('Microsoft.CodeAnalysis.NetAnalyzers.dll', 'Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll'):
        keep(package / 'analyzers/dotnet/cs' / name, 'original-canonical-analyzer-binary', True)
    keep(Path(__file__), 'preservation-tool')
    with (destination / 'manifest.json').open('x') as output:
        json.dump(dict(status='existing failed capture preserved; no new compiler proof or SDK operation',
                       actual_compiler_attempt='probe-first', successful_build='consumer-probe-build-first',
                       capture_failure='consumer-probe-capture-first',
                       coordinator_misuse='retain-failure was invoked against a successful BUILD; its rejection is not a product/SDK failure',
                       records=records), output, indent=2)
        output.write('\n')
    print(f'Preserved {len(records)} original text/binary identities; no build or compiler-proof execution.')


if __name__ == '__main__':
    main()
