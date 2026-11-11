"""One nonsecret Mac routing descriptor and exact post-build profile check; no environment dump."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re

HERE = Path(__file__).resolve().parent
PROOF = HERE.parent
ROOT = next(p for p in PROOF.parents if (p / 'toolchain.cs').is_file())
PROJECT = ROOT / 'verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj'
CORE = ROOT / 'src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll'
VARIANT = 'audit-integration-20261111'


def require(value, message):
    if not value:
        raise ValueError(message)


def identity(path):
    require(path.is_file() and not path.is_symlink(), 'Require regular routing input: ' + str(path))
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def descriptor_path(suffix):
    require(re.fullmatch(r'[a-z0-9][a-z0-9-]*', suffix), 'Use a fresh simple suffix')
    return HERE / 'routing' / ('mac-comparison-' + suffix + '.json')


def create(args):
    require(Path.cwd().resolve() == ROOT, 'Use the active checkout')
    values = dict(BoundaryCorePath=args.core, BoundaryNativeVersion=args.native,
                  BoundaryComparisonVariant=args.variant)
    require(values == dict(BoundaryCorePath=str(CORE), BoundaryNativeVersion='1.18.1',
                           BoundaryComparisonVariant=VARIANT), 'Only the three approved routing values are admitted')
    originals = [(HERE / 'proposed-lane.sh', 'f2b645b8694c1aedcec8c7d779ffb8fd939fb288ee23e6de28ee4d6cdf73211a'),
                 (PROOF / 'correction-v5/lane.sh', '9fe65a29122d95eabcfafcc60e00cda53e4ae8f58d53386a8582b7902f17a807'),
                 (HERE / 'independent-review.md', 'c7b2e1e9ab925d2a13994d7379585a1c249590861f6baa829e65e56ba5fe20cf')]
    for path, digest in originals:
        require(identity(path)['sha256'] == digest, 'Reviewed original routing/lane identity changed')
    attempt = 'mac-comparison-' + args.suffix
    target = descriptor_path(args.suffix)
    require(not target.exists() and not target.is_symlink(), 'Preserve an existing routing descriptor; use a new suffix')
    data = dict(kind='nonsecret MacComparison restore/build routing admission', active_checkout_root=str(ROOT),
                compiler_attempt=attempt, build_label='consumer-mac-comparison-build-' + args.suffix,
                environment_properties=values, core_dll_at_admission=identity(CORE),
                launcher_original=identity(HERE / 'proposed-lane.sh'), sealed_lane_original=identity(PROOF / 'correction-v5/lane.sh'),
                launcher_derivative=identity(HERE / 'proposed-lane-v2.sh'), descriptor_creator=identity(Path(__file__)),
                approved_routing_review=identity(HERE / 'independent-review.md'))
    target.parent.mkdir(exist_ok=True)
    require(not target.parent.is_symlink() and target.parent.resolve() == HERE / 'routing', 'Routing directory must be local and regular')
    partial = target.with_name(target.name + '.partial-' + str(os.getpid()))
    # Same-directory link publication is atomic and refuses an existing destination. Any failure
    # stops before SDK admission; the available partial or published descriptor remains as evidence.
    with partial.open('xb') as stream:
        stream.write((json.dumps(data, indent=2) + '\n').encode())
        stream.flush()
        os.fsync(stream.fileno())
    os.link(partial, target)
    partial.unlink()
    fd = os.open(target.parent, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)
    print('Published nonsecret routing descriptor: ' + str(target.relative_to(ROOT)))


def check(args):
    require(Path.cwd().resolve() == ROOT, 'Use the active checkout')
    path = descriptor_path(args.suffix)
    descriptor = json.loads(path.read_bytes())
    require(descriptor['environment_properties'] == dict(BoundaryCorePath=str(CORE), BoundaryNativeVersion='1.18.1',
                                                         BoundaryComparisonVariant=VARIANT), 'Routing descriptor values differ')
    require(descriptor['active_checkout_root'] == str(ROOT) and
            descriptor['compiler_attempt'] == 'mac-comparison-' + args.suffix and
            descriptor['build_label'] == 'consumer-mac-comparison-build-' + args.suffix, 'Routing attempt identity differs')
    require(identity(CORE) == descriptor['core_dll_at_admission'], 'Core DLL changed after routing admission')
    folder = PROJECT.parent / 'obj' / VARIANT
    spec_path = folder / (PROJECT.name + '.nuget.dgspec.json')
    asset_path = folder / 'project.assets.json'
    spec = json.loads(spec_path.read_bytes())['projects'][str(PROJECT)]
    assets = json.loads(asset_path.read_bytes())
    for value in (spec, assets['project']):
        require(Path(value['restore']['outputPath']).resolve() == folder,
                'Actual restore did not use the integration variant')
        require(value['frameworks']['net10.0']['dependencies']['Yubico.NativeShims']['versionOverride'] == '[1.18.1, )',
                'Actual restore did not use the approved native override')
    require([key for key in assets['libraries'] if key.startswith('Yubico.NativeShims/')] == ['Yubico.NativeShims/1.18.1'],
            'Actual resolved native package differs from the canonical baseline')
    print(json.dumps(dict(status='actual restore profile and admission Core identity agree; Csc/copy proof still required',
                          descriptor=identity(path), dgspec=identity(spec_path), assets=identity(asset_path)), indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('create', 'check'))
    parser.add_argument('--suffix', required=True)
    parser.add_argument('--core')
    parser.add_argument('--native')
    parser.add_argument('--variant')
    args = parser.parse_args()
    create(args) if args.action == 'create' else check(args)


if __name__ == '__main__':
    main()
