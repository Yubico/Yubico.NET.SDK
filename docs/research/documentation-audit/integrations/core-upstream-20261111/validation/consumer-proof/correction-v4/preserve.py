"""Preserve the existing consumers-first failed capture after its successful build; no SDK/proof execution."""
import hashlib
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
PROOF = HERE.parent
ROOT = next(p for p in PROOF.parents if (p / 'toolchain.cs').is_file())
RUNTIME = Path('/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof')


def require(value, message):
    if not value:
        raise ValueError(message)


def main():
    destination = HERE / 'consumer-preservation'
    destination.mkdir(exist_ok=False)
    binaries = RUNTIME / 'preserved-consumers-first-v4'
    binaries.mkdir(exist_ok=False)
    records, previous = [], []

    def keep(path, kind, binary=False):
        require(path.is_file() and not path.is_symlink(), 'Missing or unsafe existing input: ' + str(path))
        raw = path.read_bytes()
        digest = hashlib.sha256(raw).hexdigest()
        target = binaries / (digest + '-' + path.name) if binary else destination / 'snapshots' / (digest + '-' + path.name + '.snapshot')
        if not binary:
            raw.decode('utf-8-sig')
            require(b'\0' not in raw, 'Unexpected binary in text input')
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists():
            require(target.read_bytes() == raw, 'Retained content-address collision')
        else:
            with target.open('xb') as stream:
                stream.write(raw)
        records.append(dict(path=str(path), kind=kind, bytes=len(raw), sha256=digest, binary=binary,
                            retained=str(target) if binary else str(target.relative_to(destination))))

    commands = PROOF.parent / 'commands-v2'
    for label, status in (('consumer-consumers-build-first', 'passed'), ('consumer-consumers-capture-first', 'failed')):
        receipt = json.loads((commands / label / 'receipt.json').read_bytes())
        require(receipt['status'] == status, 'Original consumer outcome differs')
        for path in sorted((commands / label).rglob('*')):
            if path.is_file():
                keep(path, label)
    # Earlier proofs already own their actual bytes. Check those retained bytes, never rebind them
    # to current obj/bin files that subsequent authorized compilations may legitimately replace.
    manifests = list((PROOF / 'evidence').glob('*/manifest.json')) + list((PROOF / 'evidence').glob('*/failure.json'))
    manifests.append(PROOF / 'correction-v2/failure-preservation/manifest.json')
    for manifest in sorted(manifests):
        value = json.loads(manifest.read_bytes())
        keep(manifest, 'unchanged-prior-evidence-manifest')
        checked = 0
        for row in value.get('records', []):
            if 'retained' not in row:
                continue
            path = Path(row['retained']) if row.get('binary') else manifest.parent / row['retained']
            raw = path.read_bytes()
            require(len(raw) == row['bytes'] and hashlib.sha256(raw).hexdigest() == row['sha256'],
                    'Prior retained proof bytes changed: ' + str(path))
            checked += 1
        previous.append(dict(path=str(manifest), verified_retained_records=checked,
                             sha256=hashlib.sha256(manifest.read_bytes()).hexdigest()))
    for path in sorted((PROOF / 'evidence/consumers-first-v3').rglob('*')):
        if path.is_file():
            keep(path, 'original-partial-consumer-capture')
    for path in sorted(PROOF.glob('*')):
        if path.is_file() and (path.suffix in ('.py', '.sh') or path.name in ('Capture.targets', 'inputs.json', 'preparation-seal.json', 'tooling-provenance.json')):
            keep(path, 'original-tool-or-sealed-input')
    for revision in ('correction-v2', 'correction-v3'):
        for name in ('receipt.json', 'handoff.md', 'independent-review.md'):
            path = PROOF / revision / name
            if path.is_file():
                keep(path, 'unchanged-prior-correction')
    for folder in sorted((RUNTIME / 'attempts/consumers-first/raw').iterdir()):
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
                    keep(resolve(value.strip().split('|', 1)[0]), 'current-' + name, binary)
        extensions = resolve(policy['MSBuildProjectExtensionsPath'])
        for suffix in ('.nuget.g.props', '.nuget.g.targets', '.nuget.dgspec.json'):
            keep(extensions / (project.name + suffix), 'original-consumer-restore')
        keep(resolve(policy['ProjectAssetsFile']), 'original-consumer-assets')
        keep(project, 'original-compiled-project')
        for path in sorted(resolve(policy['TargetDir']).rglob('*')):
            if path.is_file():
                keep(path, 'original-successful-consumer-output', True)
        for name in ('Microsoft.Common.targets', 'Sdks/Microsoft.NET.Sdk/targets/Microsoft.PackageDependencyResolution.targets'):
            keep(Path(policy['MSBuildToolsPath']) / name, 'selected-sdk-input')
    keep(Path(__file__), 'preservation-tool')
    with (destination / 'manifest.json').open('x') as stream:
        json.dump(dict(status='existing successful build and failed capture preserved; no new capture or SDK execution',
                       compiler_attempt='consumers-first', prior_retained_proofs=previous, records=records,
                       classification='Capture-model failure after a successful zero-warning build; failed-build-only retention was not called.'), stream, indent=2)
        stream.write('\n')
    print(f'Preserved {len(records)} current original identities; verified {sum(r["verified_retained_records"] for r in previous)} prior retained records.')


if __name__ == '__main__':
    main()
