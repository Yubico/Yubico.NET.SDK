"""Bounded failed-restore preservation and secret-free metadata/cache/config inspection. No SDK/network/auth action."""
import base64
import hashlib
import json
import os
from pathlib import Path
import urllib.parse
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
PROOF = HERE.parent
ROOT = next(p for p in PROOF.parents if (p / 'toolchain.cs').is_file())
PROJECT = ROOT / 'verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj'
WRAPPER = PROOF / 'mac-comparison/src/Yubico.YubiKit.AuditIntegration20261111.MacComparison.csproj'


def require(value, message):
    if not value:
        raise ValueError(message)


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def safe_url(value):
    parsed = urllib.parse.urlsplit(value)
    return urllib.parse.urlunsplit((parsed.scheme, parsed.hostname or '', parsed.path, '', ''))


def main():
    require(not (HERE / 'report.json').exists(), 'Preserve the existing restore diagnosis')
    records = []

    def preserve(path, kind):
        raw = path.read_bytes()
        raw.decode('utf-8-sig')
        require(b'\0' not in raw, 'Text preservation only')
        target = HERE / 'snapshots' / (hashlib.sha256(raw).hexdigest() + '-' + path.name + '.snapshot')
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists():
            require(target.read_bytes() == raw, 'Snapshot collision')
        else:
            with target.open('xb') as stream:
                stream.write(raw)
        row = dict(identity(path), kind=kind, retained=str(target.relative_to(HERE)))
        records.append(row)
        return row

    logger = PROOF.parent / 'commands-v2/consumer-mac-comparison-build-first'
    receipt = json.loads((logger / 'receipt.json').read_bytes())
    require(receipt['status'] == 'failed' and receipt['exit_code'] == 1 and receipt['retention_complete'] and receipt['quiescent'],
            'Original failed build/retention state differs')
    for path in sorted(logger.rglob('*')):
        if path.is_file():
            preserve(path, 'original-failed-build-record')
    restore = PROJECT.parent / 'obj/local'
    assets_path = restore / 'project.assets.json'
    assets = json.loads(assets_path.read_bytes())
    config_paths = [Path(p) for p in assets['project']['restore'].get('configFilePaths', [])]
    # NuGet does not put credentials in these generated metadata files. Reject URL userinfo/query
    # before preserving raw restore documents; never copy a NuGet.Config or credential store.
    sources = list(assets['project']['restore']['sources'])
    require(all(safe_url(url) == url for url in sources), 'Restore URL requires redaction before raw preservation')
    for path in sorted(restore.iterdir()):
        if path.is_file():
            preserve(path, 'original-failed-restore-metadata')
    host_obj = Path('/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof/hosts') / WRAPPER.stem / 'obj'
    for name in ('project.assets.json', WRAPPER.name + '.nuget.dgspec.json', WRAPPER.name + '.nuget.g.props', WRAPPER.name + '.nuget.g.targets', 'project.nuget.cache'):
        path = host_obj / name
        if path.is_file():
            preserve(path, 'original-traversal-restore-metadata')
    sdk = Path('/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.404')
    for path in (PROJECT, PROJECT.parent / 'Directory.Build.props', WRAPPER, ROOT / 'Directory.Packages.props',
                 sdk / 'NuGet.targets', PROOF / 'correction-v5/lane.sh', PROOF / 'correction-v5/receipt.json'):
        preserve(path, 'declaration-or-selected-tool-source')

    configs = []
    source_names = set()
    for path in config_paths:
        tree = ET.parse(path).getroot()
        relevant = []
        for node in tree.findall('./packageSources/add'):
            url = node.attrib.get('value', '')
            if urllib.parse.urlsplit(url).hostname != 'nuget.pkg.github.com':
                continue
            name = node.attrib['key']
            source_names.add(name)
            credentials = tree.find('./packageSourceCredentials/' + name)
            keys = {entry.attrib.get('key', ''): bool(entry.attrib.get('value')) for entry in credentials or []}
            relevant.append(dict(name=name, source=safe_url(url),
                                 config_credential_presence={key: keys.get(key, False) for key in
                                                             ('Username', 'Password', 'ClearTextPassword', 'ValidAuthenticationTypes')},
                                 disabled=any(entry.attrib.get('key') == name and entry.attrib.get('value', '').lower() == 'true'
                                              for entry in tree.findall('./disabledPackageSources/add'))))
        configs.append(dict(path=str(path), relevant_github_sources=relevant,
                            credential_values_read_into_tool_output=False, raw_config_copied=False))
    env_presence = {name: bool(os.environ.get(name)) for name in
                    sorted({'NuGetPackageSourceCredentials_' + name for name in source_names} |
                           {'GH_TOKEN', 'GITHUB_TOKEN', 'NUGET_PLUGIN_PATHS'})}

    cache_root = Path('/home/rupicola/.nuget/packages/yubico.nativeshims')
    versions = sorted(path.name for path in cache_root.iterdir() if path.is_dir())
    package = cache_root / '1.18.1'
    metadata = json.loads((package / '.nupkg.metadata').read_bytes())
    nuspec = next(package.glob('*.nuspec'))
    tree = ET.parse(nuspec).getroot()
    manifest_values = {node.tag.rsplit('}', 1)[-1]: node.text for node in tree.iter()
                       if node.tag.rsplit('}', 1)[-1] in ('id', 'version')}
    archives = list(package.glob('*.nupkg'))
    require(len(archives) == 1, 'Cached canonical archive missing/ambiguous')
    with archives[0].open('rb') as stream:
        signed_hash = base64.b64encode(hashlib.file_digest(stream, 'sha512').digest()).decode()
    sha_path = next(package.glob('*.nupkg.sha512'))
    require(signed_hash == sha_path.read_text().strip(), 'Cached canonical archive hash does not match archive metadata')
    consumer_manifest = PROOF / 'evidence/consumers-first-v4/manifest.json'
    consumer = json.loads(consumer_manifest.read_bytes())
    canonical = [row for project in consumer['projects'] for row in project['restored_binaries']['packages']
                 if row['package_id'] == 'Yubico.NativeShims/1.18.1']
    require(canonical and all(row['sha512'] == metadata['contentHash'] for row in canonical), 'Canonical cache content hash differs from accepted SDK bindings')
    for path in (nuspec, package / '.nupkg.metadata', sha_path):
        # Package metadata contains no credentials; preserve only the sanitized source in report.
        require(path.name != '.nupkg.metadata' or safe_url(metadata.get('source', '')) == metadata.get('source', ''),
                'Cached source URL requires redaction')
        preserve(path, 'canonical-cached-package-metadata')
    spec_path = restore / (PROJECT.name + '.nuget.dgspec.json')
    spec = json.loads(spec_path.read_bytes())['projects'][str(PROJECT)]
    report = dict(status='read-only restore blocker diagnosed; no retry or credential action',
        actual_failure='NU1301 / HTTP 401 during package-id index lookup before Mac Csc; no concrete NativeShims version resolved',
        original_build=dict(exit_code=receipt['exit_code'], quiescent=receipt['quiescent'], retained=receipt['retention_complete']),
        intended_wrapper=[dict(node.attrib) for node in ET.parse(WRAPPER).iter('ProjectReference')],
        actual_restore=dict(output_path=spec['restore']['outputPath'],
                            native_dependency={name: data['dependencies']['Yubico.NativeShims'] for name, data in spec['frameworks'].items()},
                            central_native_version={name: data['centralPackageVersions']['Yubico.NativeShims'] for name, data in spec['frameworks'].items()},
                            resolved_native_libraries=[key for key in assets.get('libraries', {}) if key.startswith('Yubico.NativeShims/')]),
        expected_variant_restore_exists=(PROJECT.parent / 'obj/audit-integration-20261111/project.assets.json').exists(),
        source_fact='SDK NuGet.targets _GenerateRestoreProjectPathItemsPerFramework converts ProjectReference to a property of absolute path strings, '
                    'then constructs fresh restore items; the wrapper AdditionalProperties are absent from this restore path walk. '
                    'Observed local variant and default VersionOverride confirm the mismatch.',
        cache=dict(available_versions=versions, intended_version='1.18.1', manifest=manifest_values,
                   canonical_content_sha512=metadata['contentHash'], accepted_sdk_catalog_matches=len(canonical),
                   cached_source=safe_url(metadata.get('source', '')), archive=identity(archives[0]), signed_archive_sha512=signed_hash,
                   exact_unintended_minimum_cached=(cache_root / '1.18.1-async.8').is_dir(),
                   caution='1.18.1 is newer than the prerelease lower bound; no concrete version resolved in the failed restore. '
                           'Warm cache for the intended baseline is not proof private-feed authentication works.'),
        redacted_nuget_configuration=configs, relevant_environment_presence_only=env_presence,
        authentication_limits='No credential values, NuGet.Config bytes, gh token/status, keyring/provider token, login or network request was performed. '
                              'Credential validity/scopes cannot be established from presence; 401 is the observed failure.',
        compilation='No Mac compiler/capture policy file or DLL produced by this attempt; clean/restore is not compilation.',
        preserved=records)
    preserve(Path(__file__), 'diagnosis-tool')
    with (HERE / 'report.json').open('x') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print('Preserved failed restore inputs/logs and wrote secret-free diagnosis; no SDK/network/auth operation.')


if __name__ == '__main__':
    main()
