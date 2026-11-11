"""One declared comparison-Core copy owner; no package exemption or general HintPath evaluator."""
from pathlib import Path
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').is_file())
PROJECT = ROOT / 'verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj'
WRAPPER = HERE / 'mac-comparison/src/Yubico.YubiKit.AuditIntegration20261111.MacComparison.csproj'
CORE_PROJECT = ROOT / 'src/Core/src/Yubico.YubiKit.Core.csproj'
CORE = CORE_PROJECT.parent / 'bin/Release/net10.0/Yubico.YubiKit.Core.dll'
VARIANT = 'audit-integration-20261111'


def require(value, message):
    if not value:
        raise ValueError(message)


def is_declared_comparison_copy(project, path):
    return project.resolve() == PROJECT and path in (CORE, CORE.with_suffix('.pdb'), CORE.with_suffix('.xml'))


def declared_comparison_copy_owner(project, policy, path, destination, compiler_bindings, retain):
    require(is_declared_comparison_copy(project, path), 'Copy is outside the exact comparison/Core output context')
    require(Path(policy['MSBuildProjectFullPath']).resolve() == PROJECT and
            Path(policy.get('BoundaryCorePath', '')).resolve() == CORE,
            'Comparison copy does not use the actual prepared current-Core property')
    require(policy.get('BoundaryNativeVersion') == '1.18.1' and policy.get('BoundaryComparisonVariant') == VARIANT,
            'Comparison copy policy differs from its prepared profile')
    tree = ET.parse(PROJECT).getroot()
    references = [node for node in tree.iter('Reference') if node.attrib.get('Include') == 'Yubico.YubiKit.Core']
    require(len(references) == 1, 'Comparison must declare exactly one direct Core reference')
    reference = references[0]
    parents = {child: parent for parent in tree.iter() for child in parent}
    require(reference.attrib == dict(Include='Yubico.YubiKit.Core') and not parents[reference].attrib.get('Condition') and
            reference.findtext('HintPath') == '$(BoundaryCorePath)' and
            reference.findtext('Private', 'true').lower() == 'true' and
            not reference.find('HintPath').attrib.get('Condition'),
            'Comparison Core HintPath/CopyLocal declaration changed')
    wrapper_refs = list(ET.parse(WRAPPER).iter('ProjectReference'))
    require(len(wrapper_refs) == 1 and
            (WRAPPER.parent / wrapper_refs[0].attrib['Include']).resolve() == PROJECT and
            wrapper_refs[0].attrib.get('ReferenceOutputAssembly') == 'false', 'Prepared comparison traversal changed')
    fields = wrapper_refs[0].attrib.get('AdditionalProperties', '').split(';')
    pairs = [value.split('=', 1) for value in fields]
    require(all(len(value) == 2 for value in pairs) and len({value[0] for value in pairs}) == len(pairs),
            'Prepared comparison property identity is ambiguous')
    require(dict(pairs) == dict(BoundaryCorePath=str(CORE), BoundaryNativeVersion='1.18.1', BoundaryComparisonVariant=VARIANT),
            'Prepared comparison current-Core/native/variant binding changed')
    target_dir = Path(policy['TargetDir']).resolve()
    require(target_dir.is_relative_to(PROJECT.parent / 'bin' / VARIANT / 'build') and
            destination == target_dir / path.name, 'Direct Core copy destination differs from the actual comparison output')
    inputs = [row['binary'] for row in compiler_bindings if row.get('role') == 'declared-comparison-Core-DLL' and
              row.get('binary', {}).get('path') == str(CORE)]
    require(len(inputs) == 1, 'Direct Core copy lacks its actual Csc DLL input identity')
    current = retain.keep(CORE, 'comparison-copy-current-Core-input', True)
    require(current['sha256'] == inputs[0]['sha256'] and current['bytes'] == inputs[0]['bytes'],
            'Current direct Core DLL differs from its actual Csc input identity')
    return dict(role='declared-comparison-Core-copy' if path == CORE else 'declared-comparison-Core-related-copy',
                core_compiler_input=inputs[0], current_core_input=current,
                source_project=str(CORE_PROJECT), source_output=str(path),
                related_extension=None if path == CORE else path.suffix,
                declaration_inputs=[retain.keep(value, 'comparison-copy-declaration') for value in
                                    (PROJECT, WRAPPER, CORE_PROJECT, PROJECT.parent / 'Directory.Build.props')],
                boundary='Only the exact declared DLL and its project-local .pdb/.xml output paths; '
                         'each actual copied source/destination still receives the unchanged v4 byte-equality check. '
                         'No symbol-file semantic association or runtime behavior is inferred.')
