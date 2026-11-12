"""Read-only actual-argument regression checks; failed attempts retain their failed status."""
import copy
import importlib.util
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
B = HERE.parent
sys.path.insert(0, str(B.parent))


def load():
    spec = importlib.util.spec_from_file_location('compiler_file_binding', B / 'bind.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    binder = load()
    failed = B / 'attempts/checker-bound-build-second'
    before = json.loads((failed / 'before.json').read_bytes())
    lines = (failed / 'csc-arguments.txt').read_text().splitlines()
    descriptors = binder.compiler_file_arguments(lines)
    embedded = next(row for row in descriptors if '.NETCoreApp,Version=' in row['path'])
    assert embedded['path'] in before['items']['sources'], 'Quoted comma filename lost its original identity'
    missing = [row for row in descriptors if row['path'] not in set(before['items']['sources'] + before['items']['other'])]
    assert len(missing) == 1 and missing[0]['switch'] == '/sourcelink:'
    source_link = missing[0]['path']
    assert Path(source_link).is_file()
    try:
        binder.actual_arguments(failed, before['items'])
    except ValueError:
        pass
    else:
        raise AssertionError('The original incomplete before population was admitted')
    proposed = copy.deepcopy(before['items'])
    proposed['other'] = sorted({row['path'] for row in descriptors})
    parsed = binder.actual_arguments(failed, proposed)
    assert len(parsed) == len(lines) == 214
    # Explicitly model the collection the corrected hook will perform, without rewriting the attempt.
    hook = ET.parse(B / 'hook.targets').getroot()
    dependencies = hook.find("Target[@Name='CoreDocCheckProperties']").attrib.get('DependsOnTargets', '').split(';')
    assert 'GenerateSourceLinkFile' in dependencies and '_SetEmbeddedFilesFromSourceControlManagerUntrackedFiles' in dependencies
    fixture = '/embed:"/known/a,b with space.cs"'
    files = binder.compiler_file_arguments([fixture, fixture])
    assert len(files) == 2 and files[0]['path'] == files[1]['path'] == '/known/a,b with space.cs'
    resource = binder.compiler_file_arguments(['/resource:"/known/a,b with space.bin",identifier,public'])
    assert resource[0]['path'] == '/known/a,b with space.bin'
    preservation = json.loads((HERE / 'failure-preservation.json').read_bytes())
    mapped = {row['original']['path']: row['snapshot']['path'] for row in preservation['original_sources']}
    def historical_read(path):
        return Path(mapped.get(path, path)).read_bytes()
    binder.verify_rows(before['inputs'], historical_read)
    # Evaluate all remaining compile predicates on a clearly labelled proposed corrected view.
    # This neither replaces the historical before.json nor invokes capture()/save()/admit().
    properties = binder.props(failed)
    sdk = Path('/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.404')
    producers = {
        'SourceLinkGenerator': sdk / 'Sdks/Microsoft.SourceLink.Common/tools/net/Microsoft.SourceLink.Common.dll',
        'SourceLinkProvider': sdk / 'Sdks/Microsoft.SourceLink.GitHub/tools/net/Microsoft.SourceLink.GitHub.dll',
        'SourceControlTask': sdk / 'Sdks/Microsoft.Build.Tasks.Git/tools/net/Microsoft.Build.Tasks.Git.dll',
    }
    properties.update(SourceLink=source_link, **{key: str(path) for key, path in producers.items()})
    reviewed_profile = B / 'reviewed-inputs-third.json'
    observed_sdk = binder.profile(reviewed_profile, 'ceea8be4c6681c15c51f4611de9546c35f1428f3a6d44de893a434fdab7cd2f1')
    binder.validate_properties(properties, observed_sdk)
    origin = binder.source_link_origin(properties, observed_sdk)
    planned_before = copy.deepcopy(before)
    planned_before.update(items=proposed, source_link=origin)
    planned_before['inputs'] += [origin['input']]
    retained_calls = []
    def identity_only(path, destination):
        retained_calls.append(str(path))
        return binder.identity(path)
    observation = binder.compile_observation(failed, properties, planned_before, proposed, identity_only)
    assert not retained_calls and not observation['generated_sources'], 'This actual host emitted no source-generator C# output'
    assert 'actual_csc' not in observation
    assert not any((failed / name).exists() for name in ('compile.json', 'finish.json', 'sealed.json'))
    for row in preservation['retained_failed_records']:
        binder.verify_rows([row])
    for row in preservation['external_job_files']:
        preserved = row['preserved']
        binder.verify_rows([preserved])
    return dict(scope='Read-only actual second-attempt argument inspection and proposed before-population check; no capture replay or success admission.',
                literal_argument_count=len(lines), compiler_file_arguments=descriptors,
                genuinely_missing_before_input=source_link, corrected_comma_filename=embedded['path'],
                original_before_still_rejected=True, proposed_complete_population_passes=True,
                quoted_comma_spaces_and_duplicate_roles_preserved=True,
                original_before_hashes_unchanged_with_explicit_preserved_controller_mapping=True,
                source_link_observed_origin=origin,
                producer_profile=binder.identity(reviewed_profile),
                complete_compile_observation=observation,
                full_compile_predicate_passes_for_proposed_collection=True,
                historical_before_proof='Source Link lacked a before hash in the failed attempt; only a fresh build can establish the corrected pre-Csc proof.',
                hook_dependencies=dependencies, actual_third_build='pending independent review/coordinator')


if __name__ == '__main__':
    print(json.dumps(main(), indent=2))
