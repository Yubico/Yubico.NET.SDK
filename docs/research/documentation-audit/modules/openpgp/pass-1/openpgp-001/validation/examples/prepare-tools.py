"""Prepare final-HSM-derived compiler tools and empty hosts; final extraction stays gated."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
SOURCE_RUN = ROOT / 'docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001'
SOURCE = SOURCE_RUN / 'validation/examples'
BASELINE = 'fe180282a2d77b15eaaa5868d6c8b00e251d1f05'
COMMON = {
    'AuditHsm001': 'AuditPgp001',
    'yubihsm-001': 'openpgp-001',
    'YubiHsm': 'OpenPgp',
    'YubiHSM': 'OpenPGP',
    'HsmAuthTool': 'OpenPgpTool',
    'hsmauthtool': 'openpgptool',
    'outside-hsm-': 'outside-openpgp-',
}
FILES = {
    'v1/consumer/Directory.Build.props': 'v1/consumer/Directory.Build.props',
    'v1/consumer/Directory.Build.targets': 'v1/consumer/Directory.Build.targets',
    'v1/consumer/src/Yubico.YubiKit.AuditHsm001.Examples.v1.consumer.csproj': 'v1/consumer/src/Yubico.YubiKit.AuditPgp001.Examples.v1.consumer.csproj',
    'v1/annotations/Directory.Build.props': 'v1/annotations/Directory.Build.props',
    'v1/annotations/Directory.Build.targets': 'v1/annotations/Directory.Build.targets',
    'v1/annotations/src/Yubico.YubiKit.AuditHsm001.Examples.v1.annotations.csproj': 'v1/annotations/src/Yubico.YubiKit.AuditPgp001.Examples.v1.annotations.csproj',
    'v1/hsmauthtool/HsmAuthTool.Capture.targets': 'v1/openpgptool/OpenPgpTool.Capture.targets',
    'dispatch/src/Yubico.YubiKit.AuditHsm001.Examples.Shipping.HsmAuthTool.csproj': 'dispatch/src/Yubico.YubiKit.AuditPgp001.Examples.Shipping.OpenPgpTool.csproj',
    'inventory.py': 'inventory.py',
    'capture.py': 'capture.py',
    'verify.py': 'verify.py',
}
OLD_SCOPE = '''def scope_for(path, headings):
    if path.startswith('src/OpenPgp/'):
        return 'module'
    if path == 'docs/usage/user-interaction.md':
        return 'shared-interaction'
    if path == 'docs/migration/v1-to-v2.md':
        return 'hsm-migration' if any('OpenPGP' in heading for heading in headings) else 'outside-openpgp-migration'
    if path == 'docs/migration/v1-to-v2-gaps.md':
        return 'hsm-gaps' if any('OpenPGP' in heading for heading in headings) else 'outside-openpgp-gaps'
    return 'shared-record-metadata'
'''
NEW_SCOPE = '''def scope_for(path, headings):
    if path.startswith('src/OpenPgp/'):
        return 'module'
    accepted = accepted_scope()
    if path in accepted.get('whole_shared_paths', []):
        return 'shared-openpgp'
    selectors = accepted.get('shared_sections', {}).get(path, [])
    if any(selector.casefold() in heading.casefold() for selector in selectors for heading in headings):
        return 'shared-openpgp'
    return 'outside-openpgp-section'
'''
EXTRA = {
    'inventory.py': {
        "relative.startswith('docs/') and Path(relative).suffix in ('.md', '.yml')":
            "(relative.startswith('docs/') or relative == 'README.md') and Path(relative).suffix in ('.md', '.yml')",
        OLD_SCOPE: NEW_SCOPE,
        "if 'yubihsm' in match[1].lower():": "if 'openpgp' in match[1].lower():",
    },
    'capture.py': {
        "require(policy['RunAnalyzersDuringBuild'] == 'true' and policy['SkipAnalyzers'].lower() != 'true', 'Analyzers skipped')":
            "require(policy['RunAnalyzersDuringBuild'] == 'true' and policy['SkipAnalyzers'].lower() != 'true' and policy['RunAnalyzers'].lower() != 'false' and policy['EnableNETAnalyzers'] == 'true', 'Analyzers skipped')",
        'from inventory import HERE, ROOT, sha, file_record, require':
            "from inventory import HERE, ROOT, sha, file_record, require, source_paths\nimport importlib.util\n_sdk_spec = importlib.util.spec_from_file_location('audit_pgp_sdk_policy', HERE / 'sdk-policy.py')\n_sdk_policy = importlib.util.module_from_spec(_sdk_spec)\n_sdk_spec.loader.exec_module(_sdk_policy)",
        "require(not policy['CustomBeforeMicrosoftCommonProps'].strip(), 'Remove early common-props hook from the shipping command')":
            "sdk_default = _sdk_policy.validate(policy)\n        require(Path(policy['CustomAfterMicrosoftCommonTargets']).resolve() == (folder / 'OpenPgpTool.Capture.targets').resolve(), 'Require exact active late capture hook')\n        for sdk_source in sdk_default['sdk_sources']:\n            retained.append(retain(Path(sdk_source['path']), 'sdk-source/' + Path(sdk_source['path']).name + '.snapshot'))",
        "require(len(sample_paths) == 22 and len(set(sample_paths)) == 22, 'Reconcile all 22 original sample sources')":
            "require(len(sample_paths) == len(set(sample_paths)) == 19 and sample_paths == source_paths(actual_project.parent), 'Reconcile all 19 original OpenPgpTool sources')",
        "native_shipping_capture=context == 'openpgptool', assets_identity=":
            "sdk_optional_hook=sdk_default if context == 'openpgptool' else None, native_shipping_capture=context == 'openpgptool', assets_identity=",
        "('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt')":
            "('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt', 'imported-projects.txt')",
        "assets = Path(policy['ProjectAssetsFile'])":
            "for name in sorted(set(lines(dest / 'imported-projects.txt'))):\n        path = Path(resolve(name))\n        require(path.is_file(), 'Imported project input missing: ' + str(path))\n        relative = 'imported-project/' + sha(path.read_bytes()) + '-' + path.name + '.snapshot'\n        captured = retain(path, relative)\n        retained.append(captured)\n        records.append(dict(kind='imported-project', path=str(path), bytes=captured['bytes'], sha256=captured['sha256'], retained=relative))\n    for argument in args:\n        if argument.startswith(('/resource:', '/linkresource:')):\n            path = Path(resolve(argument.split(':', 1)[1].split(',', 1)[0]))\n            raw = path.read_bytes()\n            records.append(dict(kind='resource', path=str(path), bytes=len(raw), sha256=sha(raw), compiler_argument=argument, retention='Resource identity; binary contents remain outside text archive.'))\n    assets = Path(policy['ProjectAssetsFile'])",
        'snippet/sample/HSM execution.': 'snippet/sample/device execution.',
    },
    'verify.py': {
        "len(sample['source_paths']) == 22": "len(sample['source_paths']) == 19",
        'all 22 current shipping source paths': 'all 19 current OpenPgpTool source paths',
        '22 shipping source paths': '19 shipping source paths',
        "name = f'Yubico.YubiKit.AuditPgp001.Examples.{args.revision}.{context}'\n    require(list(ROOT.rglob(name + '.csproj')) == [folder / context / 'src' / (name + '.csproj')], f'Project name collision: {name}')":
            "name = f'Yubico.YubiKit.AuditPgp001.Examples.{args.revision}.{context}'\n    selector = f'AuditPgp001.Examples.{args.revision}.{context}'\n    matches = sorted(path for path in ROOT.rglob('*.csproj') if selector.lower() in path.stem.lower())\n    require(matches == [folder / context / 'src' / (name + '.csproj')], f'Project name collision: {name}')",
        "require(bool(group['originals']) and raw ==":
            "require(bool(group.get('prerequisites')) and bool(group.get('resource_lifetimes')) and bool(group.get('transform')), 'Describe typed dependencies, resource ownership/lifetimes and whole-body wrapper transformation for every group')\n    require(bool(group['originals']) and raw ==",
        "require(data['native_shipping_capture'] is True, 'Require native-output actual shipping capture')":
            "require(data['native_shipping_capture'] is True, 'Require native-output actual shipping capture')\n            sdk = data['sdk_optional_hook']\n            require(sdk['sdk_version'] == data['policy']['NETCoreSdkVersion'] == '10.0.403' and sdk['target_exists'] is False, 'Require pinned SDK optional-hook evidence')\n            for source in sdk['sdk_sources']:\n                require(retained['sdk-source/' + Path(source['path']).name + '.snapshot']['sha256'] == source['sha256'], 'SDK source snapshot differs from optional-hook evidence')",
    },
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def retain(relative, raw):
    path = HERE / relative
    current = HERE
    for part in Path(relative).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked target: {current}')
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require(path.read_bytes() == raw, f'Refusing changed existing preparation: {path}')
    else:
        with path.open('xb') as output:
            output.write(raw)


archive = json.loads((RUN / 'prior-archive-baseline.json').read_text())[-1]
require(archive['checked'] == 477 and ROOT / archive['manifest'] == SOURCE_RUN / 'record-manifest.json', 'Wrong frozen source archive')
manifest_bytes = (ROOT / archive['manifest']).read_bytes()
require(sha(manifest_bytes) == archive['manifest_sha256'], 'Frozen source manifest changed')
rows = json.loads(manifest_bytes)['files']
require(len(rows) == 477, 'Wrong source record count')
frozen = {row['path']: row for row in rows}


def original(relative):
    path = SOURCE / relative
    raw = path.read_bytes()
    row = frozen[str(path.relative_to(SOURCE_RUN))]
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], f'Frozen tooling source changed: {path}')
    return path, raw


copies = []
for before, after in FILES.items():
    path, raw = original(before)
    text = raw.decode('utf-8')
    changes, counts = dict(COMMON), {}
    changes.update(EXTRA.get(after, {}))
    if after.endswith('.targets'):
        changes['<MakeDir Directories="$(DocumentationWorkspace)" />'] = '<MakeDir Directories="$(DocumentationWorkspace)" />\n    <WriteLinesToFile File="$(DocumentationWorkspace)/imported-projects.txt" Lines="$(MSBuildAllProjects)" Overwrite="true" />'
    for old, new in changes.items():
        counts[old] = text.count(old)
        if old in EXTRA.get(after, {}) or old.startswith('<MakeDir'):
            require(counts[old] == 1, f'Expected one correction fragment in {before}: {old}')
        text = text.replace(old, new)
    data = text.encode('utf-8')
    retain(after, data)
    copies.append(dict(source=str(path.relative_to(ROOT)), source_sha256=sha(raw), target=after,
                       target_sha256=sha(data), substitutions=changes, substitution_counts=counts))
lessons = []
for name in ('sdk-default.py', 'post-format.py'):
    path, raw = original(name)
    lessons.append(dict(source=str(path.relative_to(ROOT)), bytes=len(raw), sha256=sha(raw),
                        application='Integrated optional SDK-default/Exists guard; extract final post-format inputs directly, retaining current identities rather than replaying historical runtime overlays.'))
sample = ROOT / 'src/OpenPgp/examples/OpenPgpTool/OpenPgpTool.csproj'
tools = [dict(path=p.name, bytes=p.stat().st_size, sha256=sha(p.read_bytes())) for p in sorted(HERE.glob('*.py'))]
record = dict(baseline=BASELINE, status='skeleton-awaiting-editor-completion', source_manifest=archive,
              copies=copies, lesson_sources=lessons, tools=tools,
              sample_context=dict(path=str(sample.relative_to(ROOT)), bytes=sample.stat().st_size, sha256=sha(sample.read_bytes())),
              policy='Fresh compiler/inventory/capture/verification tools and empty hosts only. Final-HSM algorithms with exact integrated substitutions; SDK hook helper derived from the final capture correction. Native sample outputs and late-only observation retained. Final body extraction, typed groups, dispositions and compilation await coordinator checkpoint; historical source bodies/results/caches excluded.')
retain('tooling-provenance.json', (json.dumps(record, indent=2) + '\n').encode())
print('Prepared unique OpenPGP example host skeletons, actual-tool traversal/late capture and integrated final-HSM compiler tools; final extraction awaits coordinator release.')
