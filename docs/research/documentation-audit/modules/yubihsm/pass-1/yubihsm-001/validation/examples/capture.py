"""Retain actual Csc/source/reference/analyzer/config/assets evidence after coordinator compilation."""
from pathlib import Path
import json
import re
import sys

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, sha, file_record, require

if len(sys.argv) != 4 or not re.fullmatch(r'v[1-9][0-9]*', sys.argv[1]) or sys.argv[2] not in ('consumer', 'annotations', 'hsmauthtool') or not re.fullmatch(r'[a-z0-9-]+', sys.argv[3]):
    raise SystemExit('Usage: capture.py <vN> <consumer|annotations|hsmauthtool> <new-attempt-name>')
revision, context, attempt = sys.argv[1:]
folder = HERE / revision / context
manifest_path = HERE / revision / 'manifest.json'
require(manifest_path.exists(), 'Final extraction manifest is required; skeleton hosts await editor completion')
extraction = json.loads(manifest_path.read_text())
require(extraction['status'] == 'extracted-final-inputs' and context in extraction['active_contexts'], 'Context has no accepted final snippet/sample input')
work = Path(f'/tmp/opencode/yubihsm-001-examples-{revision}-{context}')
dest = folder / 'evidence' / attempt
dest.mkdir(parents=True, exist_ok=False)


def lines(path):
    return [line.strip() for line in path.read_text().splitlines() if line.strip()]


def retain(source, relative):
    require(not source.is_symlink(), f'Symlinked capture input: {source}')
    raw = source.read_bytes()
    raw.decode('utf-8')
    require(b'\0' not in raw, f'Non-text capture: {source}')
    target = dest / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open('xb') as output:
        output.write(raw)
    return dict(path=str(relative), bytes=len(raw), sha256=sha(raw))


try:
    names = ('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt')
    retained = [retain(work / name, name) for name in names]
    args = lines(dest / 'csc-command-line.txt')
    policy = dict(row.split('=', 1) for row in lines(dest / 'policy-final.txt'))
    require(policy['TreatWarningsAsErrors'] == 'true', 'Warnings-as-errors not effective')
    require(policy['RunAnalyzersDuringBuild'] == 'true' and policy['SkipAnalyzers'].lower() != 'true', 'Analyzers skipped')
    require(policy['SkipCompilerExecution'].lower() != 'true', 'Compiler execution skipped')
    require(policy['AnalysisMode'] == ('Recommended' if context == 'annotations' else 'AllEnabledByDefault'), 'Unexpected canonical analysis mode')
    require('/warnaserror+' in args and '/warn:9999' in args, 'Compiler warning policy mismatch')
    require(not any('/skipanalyzers' in argument.lower() for argument in args), 'Csc skipped analyzers')
    actual_project = Path(policy['MSBuildProjectFullPath'])
    expected_project = ROOT / extraction['sample']['project'] if context == 'hsmauthtool' else folder / 'src' / f'Yubico.YubiKit.AuditHsm001.Examples.{revision}.{context}.csproj'
    require(actual_project.resolve() == expected_project.resolve(), 'Capture belongs to another project')

    def resolve(text):
        return str((actual_project.parent / text.strip('"')).resolve())

    def arguments(prefix):
        return {resolve(argument[len(prefix):]) for argument in args if argument.startswith(prefix)}

    if context == 'hsmauthtool':
        def native_path(value):
            return (actual_project.parent / value.replace('\\', '/')).resolve()

        native_obj, native_bin = (actual_project.parent / 'obj').resolve(), (actual_project.parent / 'bin').resolve()
        require(native_path(policy['MSBuildProjectExtensionsPath']) == native_obj, 'Shipping extensions must use ordinary obj')
        require(native_path(policy['ProjectAssetsFile']) == native_obj / 'project.assets.json', 'Shipping assets must come from ordinary obj')
        require(native_path(policy['BaseIntermediateOutputPath']) == native_obj, 'Shipping base intermediate path overridden')
        require(native_path(policy['BaseOutputPath']) == native_bin, 'Shipping base output path overridden')
        require(native_path(policy['IntermediateOutputPath']).is_relative_to(native_obj), 'Shipping intermediate output escaped ordinary obj')
        require(native_path(policy['OutputPath']).is_relative_to(native_bin), 'Shipping output escaped ordinary bin')
        require(not policy['CustomBeforeMicrosoftCommonProps'].strip(), 'Remove early common-props hook from the shipping command')
        require(native_path(policy['_InitialMSBuildProjectExtensionsPath']) == native_obj, 'Initial/current shipping extensions paths differ')
    source_args = {resolve(argument) for argument in args if argument.endswith('.cs') and not re.match(r'^/[A-Za-z]+:', argument)}
    sources = {str(Path(path).resolve()) for path in lines(dest / 'compile-inputs.txt')}
    references = {str(Path(path.split('|', 1)[0]).resolve()) for path in lines(dest / 'resolved-references.txt')}
    analyzers = {str(Path(path).resolve()) for path in lines(dest / 'analyzers.txt')}
    configs = {str(Path(path).resolve()) for path in lines(dest / 'analyzer-configs.txt')}
    require(bool(sources) and source_args == sources, 'Actual Csc source arguments differ from Compile items')
    require(arguments('/reference:') == references, 'Actual Csc reference arguments differ from resolved references')
    require(arguments('/analyzer:') == analyzers, 'Actual Csc analyzer arguments differ from Analyzer items')
    require(arguments('/analyzerconfig:') == configs, 'Actual Csc analyzer-config arguments differ from EditorConfigFiles')
    require(any('Microsoft.CodeAnalysis.NetAnalyzers.dll' in name for name in analyzers), 'Canonical .NET analyzers missing')
    if context == 'annotations':
        require(any('xunit.analyzers' in name.lower() for name in analyzers), 'Genuine xUnit analyzers missing')
    if context == 'hsmauthtool':
        sample_paths = extraction['sample']['source_paths']
        require(len(sample_paths) == 22 and len(set(sample_paths)) == 22, 'Reconcile all 22 original sample sources')
        require({str((ROOT / name).resolve()) for name in sample_paths} <= sources, 'Actual shipping compilation omitted an original sample source')
    else:
        require(str((folder / 'src/Examples.cs').resolve()) in sources, 'Complete generated example source missing from Csc')
    records = []
    for kind, paths in [('source', sources), ('reference', references), ('analyzer', analyzers), ('analyzer-config', configs)]:
        for name in sorted(paths):
            path = Path(name)
            raw = path.read_bytes()
            row = dict(kind=kind, path=name, bytes=len(raw), sha256=sha(raw))
            if kind in ('source', 'analyzer-config'):
                relative = f'{kind}/{sha(raw)}-{path.name}.snapshot'
                retained.append(retain(path, relative))
                row['retained'] = relative
            else:
                row['retention'] = 'Binary dependency identity only; full compiler assets retained as text.'
            records.append(row)
    assets = Path(policy['ProjectAssetsFile'])
    retained.append(retain(assets, 'project.assets.json.snapshot'))
    for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig'):
        retained.append(retain(ROOT / name, name + '.snapshot'))
    retained.append(retain(actual_project, 'compiled-project.csproj.snapshot'))
    original = 'src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/Yubico.YubiKit.YubiHsm.IntegrationTests.csproj' if context == 'annotations' else 'src/YubiHsm/src/Yubico.YubiKit.YubiHsm.csproj'
    retained.append(retain(ROOT / original, 'source-context.csproj.snapshot'))
    if context == 'hsmauthtool':
        retained.append(retain(folder / 'HsmAuthTool.Capture.targets', 'capture-hook.targets.snapshot'))
    result = dict(revision=revision, context=context, attempt=attempt, policy=policy, records=records,
                  retained=retained, extraction_manifest_sha256=sha(manifest_path.read_bytes()),
                  native_shipping_capture=context == 'hsmauthtool', assets_identity=dict(path=str(assets), sha256=sha(assets.read_bytes())),
                  counts=dict(sources=len(sources), references=len(references), analyzers=len(analyzers), configs=len(configs)),
                  limits='Actual Csc membership/policy/assets and source text retained. Binary identities only. Matching build log supplies success/diagnostics; compilation establishes binding rather than snippet/sample/HSM execution.')
    with (dest / 'manifest.json').open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(f'PASS: {context}/{attempt}: {len(sources)} actual Csc sources, {len(references)} references, {len(analyzers)} analyzers; source/config/assets text retained.')
except Exception as error:
    with (dest / 'capture-failure.json').open('x') as output:
        json.dump(dict(type=type(error).__name__, error=str(error), scope='Capture/provenance failure; classify separately from matching compiler diagnostics. Earlier text captures retained.'), output, indent=2)
        output.write('\n')
    raise
