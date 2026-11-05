"""Inspect the pinned SDK's optional early-hook default without invoking MSBuild."""
from pathlib import Path
import hashlib
import xml.etree.ElementTree as ET

SDK_VERSION = '10.0.403'
SDK = Path('/home/rupicola/.local/share/mise/dotnet-root/sdk') / SDK_VERSION
DEFAULT = SDK / 'vCurrent/Custom.Before.Microsoft.Common.props'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def inspect_default():
    common = SDK / 'Current/Microsoft.Common.props'
    outer = SDK / 'Sdks/Microsoft.NET.Sdk/Sdk/Sdk.props'
    tree = ET.parse(common).getroot()
    node = tree.find('.//CustomBeforeMicrosoftCommonProps')
    require(node is not None and node.attrib['Condition'] == "'$(CustomBeforeMicrosoftCommonProps)'==''",
            'SDK optional-hook default condition changed')
    require(node.text == r'$(MSBuildExtensionsPath)\v$(MSBuildToolsVersion)\Custom.Before.$(MSBuildThisFile)',
            'SDK optional-hook expression changed')
    imports = [entry for entry in tree.findall('Import')
               if entry.attrib.get('Project') == '$(CustomBeforeMicrosoftCommonProps)']
    condition = "'$(CustomBeforeMicrosoftCommonProps)' != '' and Exists('$(CustomBeforeMicrosoftCommonProps)')"
    require(len(imports) == 1 and imports[0].attrib['Condition'] == condition,
            'SDK optional-hook Exists import changed')
    require(r'$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props' in outer.read_text(),
            'SDK common-props import context changed')
    expected = node.text.replace('$(MSBuildExtensionsPath)', str(SDK)).replace(
        '$(MSBuildToolsVersion)', 'Current').replace('$(MSBuildThisFile)', 'Microsoft.Common.props').replace('\\', '/')
    require(expected == str(DEFAULT), 'SDK default target changed')
    require(not DEFAULT.exists() and not DEFAULT.is_symlink() and not DEFAULT.parent.exists(),
            'Optional early-hook target appeared; inspect its imported bytes before capture')
    records = []
    for path in (common, outer):
        raw = path.read_bytes()
        records.append(dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest()))
    return dict(sdk_version=SDK_VERSION, sdk_root=str(SDK), default_target=str(DEFAULT),
                target_exists=False, default_expression=node.text, import_condition=condition,
                sdk_sources=records)


def validate(policy):
    evidence = inspect_default()
    require(policy['NETCoreSdkVersion'] == SDK_VERSION, 'Re-adjudicate optional-hook policy for the selected SDK version')
    actual = policy['CustomBeforeMicrosoftCommonProps'].strip()
    require(actual in ('', str(DEFAULT)), 'Early-hook value differs from empty or exact pinned absent SDK default')
    evidence['observed_value'] = actual
    evidence['conclusion'] = 'Empty override or exact SDK-built-in default; absent target and conditional Exists import verified.'
    return evidence
