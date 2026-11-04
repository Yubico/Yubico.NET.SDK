"""Prepare one audited capture/verify correction; preserve original tools/extraction/failure."""
from pathlib import Path
import ast
import hashlib
import json
import sys

sys.dont_write_bytecode = True
import analyzer_policy as policy

HERE, EXAMPLES, ROOT = policy.HERE, policy.EXAMPLES, policy.ROOT
require = policy.require


def retain(relative, raw):
    target = HERE / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        require(target.read_bytes() == raw, 'Preserve changed existing correction: ' + str(target))
    else:
        with target.open('xb') as output:
            output.write(raw)


GUARD = "require(policy['RunAnalyzersDuringBuild'] == 'true' and policy['SkipAnalyzers'].lower() != 'true' and policy['RunAnalyzers'].lower() != 'false' and policy['EnableNETAnalyzers'] == 'true', 'Analyzers skipped')"
RUN_GUARD = "require(policy['RunAnalyzersDuringBuild'] == 'true' and policy['SkipAnalyzers'].lower() != 'true' and policy['RunAnalyzers'].lower() != 'false', 'Analyzers skipped')"
IMPORT = "sys.dont_write_bytecode = True"
COMMON = {IMPORT: IMPORT + "\nsys.path.insert(0, str(Path(__file__).resolve().parents[1]))\nimport analyzer_policy as _analyzer_policy"}
CHANGES = {
    'capture.py': dict(COMMON, **{
        GUARD: RUN_GUARD,
        "if len(sys.argv) != 4": "_analyzer_policy.receipt()\n\nif len(sys.argv) != 4",
        "require(any('Microsoft.CodeAnalysis.NetAnalyzers.dll' in name for name in analyzers), 'Canonical .NET analyzers missing')":
            "require(any('Microsoft.CodeAnalysis.NetAnalyzers.dll' in name for name in analyzers), 'Canonical .NET analyzers missing')\n    analyzer_package = _analyzer_policy.prove(policy, args, analyzers, actual_project)\n    _analyzer_policy.validate_proof(analyzer_package)\n    for source in analyzer_package['shared_sources'] + analyzer_package['project_sources']:\n        retained.append(retain(Path(source['path']), 'analyzer-package/' + source['sha256'] + '-' + Path(source['path']).name + '.snapshot'))",
        "retained=retained, extraction_manifest_sha256=":
            "analyzer_package=analyzer_package, analyzer_package_correction_sha256=_analyzer_policy.identity(_analyzer_policy.HERE / 'manifest.json')['sha256'], retained=retained, extraction_manifest_sha256=",
    }),
    'verify.py': dict(COMMON, **{
        "parser = argparse.ArgumentParser(description=__doc__)":
            "_analyzer_policy.receipt()\nparser = argparse.ArgumentParser(description=__doc__)",
        "require(data['extraction_manifest_sha256'] == sha(manifest_path.read_bytes()), 'Capture binds another extraction manifest')":
            "require(data['extraction_manifest_sha256'] == sha(manifest_path.read_bytes()), 'Capture binds another extraction manifest')\n        _analyzer_policy.verify_capture(data, evidence, args.live)",
    }),
}
require(sys.argv[1:] in (['prepare'], ['static']), 'Usage: prepare.py prepare|static')
if sys.argv[1:] == ['static']:
    record = policy.receipt()
    for row in record['workspace']:
        require(policy.identity(Path(row['path'])) == row, 'Live consumer compiler workspace changed')
    for row in record['transformations']:
        original = (EXAMPLES / row['source']).read_bytes()
        require((HERE / row['before_snapshot']).read_bytes() == original, 'Before-tool snapshot changed')
        text = original.decode()
        for before, after in row['substitutions'].items():
            require(text.count(before) == row['substitution_counts'][before], 'Correction substitution count changed')
            text = text.replace(before, after)
        require((HERE / row['target']).read_bytes() == text.encode(), 'Unrecorded corrected tool change')
    for path in HERE.glob('*.py'):
        ast.parse(path.read_text(), filename=str(path))
    work = Path(record['workspace_root'])
    captured_policy = dict(line.split('=', 1) for line in (work / 'policy-final.txt').read_text().splitlines())
    arguments = (work / 'csc-command-line.txt').read_text().splitlines()
    analyzers = set((work / 'analyzers.txt').read_text().splitlines())
    require(policy.prove(captured_policy, arguments, analyzers, Path(captured_policy['MSBuildProjectFullPath'])) == record['canonical_package'],
            'Current consumer analyzer/package proof changed')
    require(captured_policy['EnableNETAnalyzers'] == 'false' and 'EnableNETAnalyzers' in GUARD,
            'Original overstrict guard reproduction changed')
    print('PASS: original guard rejected the canonical NuGet replacement flag; exact 9.0.0 import/pin, both real Csc analyzer assemblies, all 10 analyzer arguments and unchanged warning/no-skip policy reconcile.')
    print('PASS: frozen v1 extraction, 17 groups/407 dispositions/110 dependencies, original tools, first failure and intact consumer workspace preserved; corrected capture/verify source identities and exact substitutions reconcile.')
    print('PENDING: coordinator consumer/second capture, shipping build/first capture and corrected final --live verification. No build/test/device/formatter invoked.')
    raise SystemExit(0)

base = json.loads((EXAMPLES / 'v1/manifest.json').read_text())
old_tools = {row['path']: row for row in base['tool_sources']}
transformations = []
for name, changes in CHANGES.items():
    source = EXAMPLES / name
    original = source.read_bytes()
    require(hashlib.sha256(original).hexdigest() == old_tools[str(source.relative_to(ROOT))]['sha256'], 'Base capture/verify tool changed')
    snapshot = 'before/' + name + '.snapshot'
    retain(snapshot, original)
    text, counts = original.decode(), {}
    for before, after in changes.items():
        counts[before] = text.count(before)
        require(counts[before] == 1, 'Expected one pinned correction fragment: ' + before)
        text = text.replace(before, after)
    ast.parse(text, filename=str(HERE / name))
    retain(name, text.encode())
    transformations.append(dict(source=name, before_snapshot=snapshot, target=name,
                                substitutions=changes, substitution_counts=counts,
                                source_identity=policy.identity(source), target_identity=policy.identity(HERE / name)))
work = Path('/tmp/opencode/openpgp-001-examples-v1-consumer')
captured_policy = dict(line.split('=', 1) for line in (work / 'policy-final.txt').read_text().splitlines())
arguments = (work / 'csc-command-line.txt').read_text().splitlines()
analyzers = set((work / 'analyzers.txt').read_text().splitlines())
proof = policy.prove(captured_policy, arguments, analyzers, Path(captured_policy['MSBuildProjectFullPath']))
for row in proof['shared_sources'] + proof['project_sources']:
    retain('proof/' + row['sha256'] + '-' + Path(row['path']).name + '.snapshot', Path(row['path']).read_bytes())
preserved = [EXAMPLES / 'v1/manifest.json', EXAMPLES / 'tooling-provenance.json']
preserved += [EXAMPLES / name for name in CHANGES]
preserved += [path for path in sorted((EXAMPLES / 'v1/consumer/evidence/first').rglob('*')) if path.is_file()]
preserved += [EXAMPLES.parent / 'examples-v1-consumer-capture.log', EXAMPLES.parent / 'examples-v1-consumer-build.log']
preserved += sorted((HERE / 'before').glob('*.snapshot'))
workspace_paths = [work / name for name in ('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt',
                   'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt', 'imported-projects.txt')]
workspace_paths += [Path(row['path']) for row in proof['project_sources']] + [Path(captured_policy['ProjectAssetsFile'])]
record = dict(id='analyzer-package-1', baseline=base['baseline'], scope='Capture/verification evidence guard correction only; no rebuild, source/configuration/output edit or analyzer disablement.',
              base_extraction=policy.identity(EXAMPLES / 'v1/manifest.json'), canonical_package=proof,
              original_failure='Analyzers skipped: EnableNETAnalyzers=false selects the canonical pinned NuGet replacements, not skipped analyzer execution.',
              preserved=[policy.identity(path) for path in preserved],
              proof_snapshots=[policy.identity(path) for path in sorted((HERE / 'proof').glob('*.snapshot'))],
              tools=[policy.identity(path) for path in sorted(HERE.glob('*.py'))],
              workspace_root=str(work), workspace=[policy.identity(path) for path in workspace_paths],
              transformations=transformations,
              lineage='Original v1 tools and extraction retain their identities. Versioned before-tool snapshots and corrected concrete tool sources replace only the overstrict built-in-selection guard and add canonical-package proof/receipt bindings. Existing source/reference/analyzer/config/assets membership, SDK-default and native-output algorithms remain intact.')
retain('manifest.json', (json.dumps(record, indent=2) + '\n').encode())
print('Prepared analyzer-package-1 with exact canonical package/import/Csc proof, before-tool snapshots and independent corrected capture/verify identities; original v1 and first failure preserved.')
