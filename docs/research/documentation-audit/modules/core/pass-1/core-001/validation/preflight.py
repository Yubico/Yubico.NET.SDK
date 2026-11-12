"""Read-only actual discovery, configuration and source/API membership prerequisites."""
import json
import shutil
from pathlib import Path
import xml.etree.ElementTree as ET
from foundation import HERE, ROOT, BASELINE, RUNTIME, baseline, git, require, sha


def inspect():
    baseline()
    paths = sorted(path.relative_to(ROOT).as_posix() for path in ROOT.rglob('*.csproj'))
    # Mirror Discovery.cs: recursive full-path predicates are case-sensitive;
    # MatchProjects then uses case-insensitive filename-stem substring matching.
    prefix = 'Yubico.YubiKit.'
    units = [path for path in paths if '/tests/' in '/' + path and '.UnitTests' in path and
             prefix in path and '/.claude/' not in '/' + path]
    short = lambda path: Path(path).stem.removeprefix(prefix)
    expected = json.loads((HERE / 'expected-unit-projects.json').read_bytes())
    require(['test:' + short(path) for path in units] == expected, 'Review changed canonical unit project membership')
    resilience = [path for path in units if 'core' in Path(path).stem.lower()]
    require(resilience == ['src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj'],
            'Core resilience selector acquired an audit/archived project')
    hosts = [path for path in paths if 'Audit20261111.DocCheck' in Path(path).stem]
    require(len(hosts) == 1 and 'core' not in Path(hosts[0]).stem.lower(), 'Require one collision-free checker filename')
    tree = ET.parse(ROOT / hosts[0])
    require(tree.getroot().findtext('PropertyGroup/IsPackable') == 'false', 'Checker enters packaging')
    props = ET.parse(HERE / 'Directory.Build.props').getroot()
    imported = (HERE / props.find('Import').attrib['Project'].replace('$(MSBuildThisFileDirectory)', '')).resolve()
    require(imported == ROOT / 'Directory.Build.props', 'Core-depth root policy import differs')
    require(props.findtext('PropertyGroup/AuditValidationWorkspace') == str(RUNTIME / 'jobs'), 'Compiler workspace escaped Core runtime')
    sdk_policy = json.loads((ROOT / 'global.json').read_bytes())
    dotnet = shutil.which('dotnet')
    require(dotnet is not None, 'dotnet executable is unavailable')
    sdk_root = Path(dotnet).resolve().parent / 'sdk'
    installed = sorted(path.name for path in sdk_root.glob('*') if path.is_dir())
    raw = git('ls-tree', '-r', '--name-only', '-z', BASELINE).decode().split('\0')
    apis = sorted(path for path in raw if path.startswith('src/') and
                  Path(path).name in ('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt'))
    require(apis, 'API snapshot inventory is empty')
    config = ['global.json', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props',
              '.editorconfig', 'toolchain.cs', 'scripts/toolchain/Discovery.cs', 'scripts/toolchain/TestTargets.cs',
              'scripts/toolchain/BuildTargets.cs', '.github/workflows/build.yml', '.github/workflows/native-aot.yml']
    return dict(source_baseline=BASELINE, metadata_head=git('rev-parse', 'HEAD').decode().strip(),
                canonical_unit_projects=units, resilience_projects=resilience, checker_project=hosts[0],
                api_snapshots=apis, api_snapshot_count=len(apis), sdk_policy=sdk_policy,
                installed_sdks=installed, sdk_root=str(sdk_root),
                sdk_selection='10.0.404 observed at integration; actual compiler gate re-observes selection',
                scratch=str(RUNTIME), configuration=[dict(path=path, sha256=sha((ROOT / path).read_bytes())) for path in config],
                limits='Filesystem/static membership only; no SDK invocation, compilation or device probe.')


if __name__ == '__main__':
    print(json.dumps(inspect(), indent=2))
