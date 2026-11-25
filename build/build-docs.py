#!/usr/bin/env python3
"""Generate V1 and V2 metadata with their own SDKs, then render one DocFX site.

High-level build steps:
1. Check the existing V2 checkout and both .NET SDKs, and install the expected
   DocFX version if needed. This helper does not clone V2 or install .NET SDKs.
2. Resolve V2's source paths and clear old generated API YAML and website output,
   preserving handwritten Markdown.
3. Restore dependencies for both SDKs.
4. Generate V1 and V2 API metadata separately in their respective build environments,
   writing YAML directly into the documentation folders.
5. Check that the expected API metadata exists and render it with the handwritten
   pages into one website at docs/_site/.
6. Record the source commits and DocFX version in build-info.json (created before
   rendering so it is included in the site), and save logs in .docbuild/logs/.

This helper builds the site; publishing and deployment belong to the workflows.

After yubikit is merged into develop/main:
Once both SDK versions are in the same checkout and metadata generation has been
validated in one DocFX run, combine their metadata entries in docfx.json. 
Instead of running this helper, run `docfx docfx.json` to generate the documentation.
However, the branch merge alone does not make this helper redundant.
Before removing the helper, decide where to retain stale-file cleanup, metadata completeness
checks, and build-info.json generation, then update and validate both workflows.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / '.docbuild'
DOCFX_VERSION = '2.81.0'


def run(command, cwd=ROOT, env=None):
    print('+ ' + ' '.join(map(str, command)), flush=True)
    subprocess.run(list(map(str, command)), cwd=cwd, env=env, check=True)


def revision(path):
    return subprocess.check_output(['git', '-C', str(path), 'rev-parse', 'HEAD'], text=True).strip()


def sdk_environment(executable):
    resolved = shutil.which(executable)
    if not resolved:
        raise RuntimeError(f'.NET executable not found: {executable}')
    binary = Path(resolved).resolve()
    env = os.environ.copy()
    env['DOTNET_ROOT'] = str(binary.parent)
    env['PATH'] = str(binary.parent) + os.pathsep + env.get('PATH', '')
    env['DOTNET_ROLL_FORWARD'] = 'Major'
    return binary, env


def generate(args):
    WORK.mkdir(exist_ok=True)
    v2 = Path(args.v2_source).resolve()
    if not (v2 / 'global.json').is_file():
        raise RuntimeError(f'V2 checkout missing at {v2}. See contributordocs/documentation-system/building-docs-and-running.md.')
    v1_dotnet, v1_env = sdk_environment(args.dotnet)
    v2_dotnet, v2_env = sdk_environment(args.v2_dotnet or args.dotnet)
    # Check SDK availability before clearing previously generated output.
    run([v1_dotnet, '--version'], env=v1_env)
    run([v2_dotnet, '--version'], cwd=v2, env=v2_env)
    tools = WORK / 'tools'
    docfx = tools / ('docfx.exe' if os.name == 'nt' else 'docfx')
    if not docfx.is_file():
        run([v1_dotnet, 'tool', 'install', 'docfx', '--version', DOCFX_VERSION,
             '--tool-path', tools, '--allow-roll-forward'], env=v1_env)
    installed = subprocess.check_output([str(docfx), '--version'], env=v1_env, text=True).strip()
    if DOCFX_VERSION not in installed:
        raise RuntimeError(f'Expected DocFX {DOCFX_VERSION}, found {installed}. Update the tool in {tools}.')

    config = json.loads((ROOT / 'docfx.v2.json').read_text())
    entry = config['metadata'][0]
    projects = entry['src'][0]['files']
    for project in projects:
        if not (v2 / project).is_file():
            raise RuntimeError(f'Configured SDK project is missing: {project}')
    entry['src'][0]['src'] = str(v2)
    entry['output'] = str(ROOT / 'docs' / 'v2-api')
    entry['filter'] = str(ROOT / 'docs' / 'filterConfig.yml')
    resolved_config = WORK / 'docfx.v2.resolved.json'
    resolved_config.write_text(json.dumps(config, indent=2) + '\n')
    logs = WORK / 'logs'
    logs.mkdir(exist_ok=True)
    # Replace only generated YAML in API folders; preserve authored Markdown.
    for folder in ['core-api', 'yubikey-api', 'v2-api']:
        destination = ROOT / 'docs' / folder
        destination.mkdir(exist_ok=True)
        for old in destination.glob('*.yml'):
            old.unlink()
    # The site directory contains only generated output.
    site = ROOT / 'docs' / '_site'
    if site.exists():
        shutil.rmtree(site)

    # Restore all V1 frameworks to preserve the shared multi-framework lockfile.
    # docfx.json still selects netstandard2.0 for API metadata generation.
    for project in ['Yubico.Core/src/Yubico.Core.csproj', 'Yubico.YubiKey/src/Yubico.YubiKey.csproj']:
        run([v1_dotnet, 'restore', project], env=v1_env)
    for project in projects:
        run([v2_dotnet, 'restore', project, '-p:TargetFramework=net10.0'], cwd=v2, env=v2_env)

    def docfx_run(stage, config_path, cwd, env, log_name):
        run([docfx, stage, config_path, '--logLevel', 'warning', '--warningsAsErrors',
             '--log', logs / log_name], cwd=cwd, env=env)

    docfx_run('metadata', ROOT / 'docfx.json', ROOT, v1_env, 'v1-metadata.log')
    docfx_run('metadata', resolved_config, v2, v2_env, 'v2-metadata.log')
    # A successful process with missing projects must not publish a partial API reference.
    expected = [('core-api', 'Yubico.Core'), ('yubikey-api', 'Yubico.YubiKey')]
    expected.extend(('v2-api', Path(project).stem) for project in projects)
    for folder, namespace in expected:
        if not (ROOT / 'docs' / folder / (namespace + '.yml')).is_file():
            raise RuntimeError(f'Missing API namespace metadata: {namespace}')
    info = {'v1Commit': revision(ROOT), 'v2Commit': revision(v2),
            'v2Ref': 'yubikit', 'docfxVersion': DOCFX_VERSION}
    (WORK / 'build-info.json').write_text(json.dumps(info, indent=2) + '\n')
    docfx_run('build', ROOT / 'docfx.json', ROOT, v1_env, 'site.log')
    for folder in ['core-api', 'yubikey-api', 'v2-api']:
        if not (site / folder / 'index.html').is_file():
            raise RuntimeError(f'Missing API landing page: {folder}')
    summary = os.environ.get('GITHUB_STEP_SUMMARY')
    if summary:
        with open(summary, 'a', encoding='utf-8') as stream:
            stream.write(f"\n### Documentation sources\n\n- V1: `{info['v1Commit']}`\n- V2 (yubikit): `{info['v2Commit']}`\n- DocFX: `{DOCFX_VERSION}`\n")
    print(f'Combined documentation: {site}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--v2-source', default=str(WORK / 'yubikit'), help='Full checkout of yubikit')
    parser.add_argument('--dotnet', default='dotnet', help='V1 dotnet executable')
    parser.add_argument('--v2-dotnet', help='V2 dotnet executable, if installed separately')
    args = parser.parse_args()
    try:
        generate(args)
    except (RuntimeError, subprocess.CalledProcessError) as error:
        print(f'Documentation build failed: {error}', file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
