"""Faithful AdditionalFiles multisets; only the source-proven PublicAPI pair may repeat."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').is_file())
NS = 'http://schemas.microsoft.com/developer/msbuild/2003'


def require(value, message):
    if not value:
        raise ValueError(message)


def identity(path):
    require(path.is_file() and not path.is_symlink(), 'Additional-file input must be a regular file')
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def source_origin(project, folder, retain):
    policy = dict(line.split('=', 1) for line in (folder / 'policy.txt').read_text(encoding='utf-8-sig').splitlines() if line.strip())
    assets_path = Path(policy['ProjectAssetsFile'])
    if not assets_path.is_absolute():
        assets_path = project.parent / assets_path
    assets = json.loads(assets_path.read_bytes())
    key = 'Microsoft.CodeAnalysis.PublicApiAnalyzers/5.6.0'
    library = assets['libraries'][key]
    require(library['type'] == 'package' and library['path'] == 'microsoft.codeanalysis.publicapianalyzers/5.6.0',
            'Repeated PublicAPI files require the exact restored analyzer package')
    packages = [Path(p) / library['path'] for p in assets['packageFolders']]
    packages = [p for p in packages if p.is_dir()]
    require(len(packages) == 1, 'PublicApi analyzer package root is ambiguous')
    package = packages[0]
    package_targets = package / 'buildTransitive/Microsoft.CodeAnalysis.PublicApiAnalyzers.targets'
    root_targets = ROOT / 'Directory.Build.targets'
    root_tree = ET.parse(root_targets).getroot()
    groups = [g for g in root_tree.findall('ItemGroup') if g.attrib.get('Condition') == "'$(IsYubiKitSdkLibrary)' == 'true'"]
    require(len(groups) == 1, 'Canonical library-scoped item-group structure changed')
    api_groups = [g for g in groups if g.findall('AdditionalFiles')]
    require(len(api_groups) == 1, 'Canonical AdditionalFiles owner is ambiguous')
    group = api_groups[0]
    names = ('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt')
    require([n.attrib for n in group.findall('AdditionalFiles')] ==
            [dict(Include='$(MSBuildProjectDirectory)/' + name) for name in names],
            'Canonical root PublicAPI Include expressions changed')
    require([n.attrib.get('Include') for n in group.findall('PackageReference')] == ['Microsoft.CodeAnalysis.PublicApiAnalyzers'],
            'Canonical PublicAPI package declaration changed')
    require([n.text for n in ET.parse(project).iter('IsYubiKitSdkLibrary')] == ['true'],
            'Repeated PublicAPI pair is restricted to the actual SDK-library context')
    additions = [g for g in ET.parse(package_targets).getroot().findall('ItemGroup') if g.findall('AdditionalFiles')]
    require(len(additions) == 2, 'Package AdditionalFiles declarations changed')
    for addition, name in zip(additions, names):
        require(addition.attrib.get('Condition') == f"Exists('$(MSBuildProjectDirectory)\\{name}')" and
                [n.attrib for n in addition.findall('AdditionalFiles')] == [dict(Include=name)],
                'Package PublicAPI Include/Exists declaration changed')
    generated = assets_path.parent / (project.name + '.nuget.g.targets')
    generated_tree = ET.parse(generated).getroot()
    require(generated_tree.tag == '{' + NS + '}Project', 'Unexpected NuGet XML namespace')
    needle = '$(NuGetPackageRoot)microsoft.codeanalysis.publicapianalyzers/5.6.0/buildTransitive/Microsoft.CodeAnalysis.PublicApiAnalyzers.targets'
    imports = [node for node in generated_tree.iter('{' + NS + '}Import') if node.attrib.get('Project') == needle]
    require(len(imports) == 1 and imports[0].attrib.get('Condition') == f"Exists('{needle}')", 'PublicAPI package import guard changed')
    parents = {child: parent for parent in generated_tree.iter() for child in parent}
    require(parents[imports[0]].attrib.get('Condition', '').strip() == "'$(ExcludeRestorePackageImports)' != 'true'",
            'PublicAPI restore import-group guard changed')
    sdk_common = Path(policy['MSBuildToolsPath']) / 'Microsoft.Common.targets'
    text = sdk_common.read_text()
    early = '<Import Project="$(MSBuildProjectExtensionsPath)$(MSBuildProjectFile).*.targets"'
    late = '<Import Project="$(DirectoryBuildTargetsPath)"'
    require(early in text and late in text and text.index(early) < text.index(late), 'SDK extension/root target import ordering changed')
    csharp = Path(policy['MSBuildSDKsPath']).parent / 'Roslyn/Microsoft.CSharp.Core.targets'
    require('AdditionalFiles="@(AdditionalFiles)"' in csharp.read_text(), 'Actual Csc AdditionalFiles input wiring changed')
    sources = [project, root_targets, package_targets, generated, sdk_common, csharp, assets_path]
    return dict(package=key, package_sha512=library['sha512'],
                origin='Package adds the relative pair before root Directory.Build.targets adds the absolute pair.',
                root_include_expressions=['$(MSBuildProjectDirectory)/' + name for name in names],
                package_include_expressions=list(names),
                sources=[retain.keep(p, 'additional-file-origin') for p in sources])


def additional_file_multiset(project, folder, args, retain):
    item_path, csc_path = folder / 'additional-files.txt', folder / 'csc.txt'

    def row(value, raw_line, ordinal, line_number):
        canonical = (project.parent / value.strip('"').replace('\\', '/')).resolve()
        return dict(ordinal=ordinal, line_number=line_number, raw_line=raw_line, path_spelling=value,
                    canonical_path=str(canonical), content=identity(canonical))

    items, arguments = [], []
    for number, line in enumerate(item_path.read_text(encoding='utf-8-sig').splitlines(keepends=True), 1):
        value = line.strip()
        if value:
            require('|' not in value, 'AdditionalFiles metadata column changed')
            items.append(row(value, line, len(items) + 1, number))
    selected_args = []
    for number, line in enumerate(csc_path.read_text(encoding='utf-8-sig').splitlines(keepends=True), 1):
        argument = line.strip()
        if argument.startswith('/additionalfile:'):
            selected_args.append(argument)
            arguments.append(dict(row(argument[len('/additionalfile:'):], line, len(arguments) + 1, number), argument=argument))
    require(selected_args == [a for a in args if a.startswith('/additionalfile:')], 'Raw Csc spelling/order differs from actual parsed argument stream')
    item_counts = Counter(r['canonical_path'] for r in items)
    arg_counts = Counter(r['canonical_path'] for r in arguments)
    require(item_counts == arg_counts, 'AdditionalFiles/Csc canonical path MULTISETS differ')

    def content_multiset(rows):
        return Counter((r['canonical_path'], r['content']['bytes'], r['content']['sha256']) for r in rows)

    require(content_multiset(items) == content_multiset(arguments), 'AdditionalFiles/Csc full-content MULTISETS differ')
    duplicates = {path: count for path, count in item_counts.items() if count > 1}
    origin = None
    if duplicates:
        expected = {str((project.parent / name).resolve()): 2 for name in ('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt')}
        require(duplicates == expected, 'Only the source-proven two-per-PublicAPI pair may repeat')
        origin = source_origin(project, folder, retain)
    proof = dict(comparison='canonical path and content identity MULTISETS; order/spelling also retained verbatim',
                 total_items=len(items), total_csc_arguments=len(arguments), unique_paths=len(item_counts),
                 multiplicities=dict(sorted(item_counts.items())), captured_fullpath_items=items,
                 csc_as_passed_arguments=arguments, origin=origin,
                 item_text=identity(item_path), csc_text=identity(csc_path),
                 boundary='Item spellings are the hook-recorded FullPath values, not uncaptured MSBuild Identity metadata. '
                          'No claim that Csc or analyzers ignored/deduplicated any occurrence.')
    return [r['canonical_path'] for r in items], proof
