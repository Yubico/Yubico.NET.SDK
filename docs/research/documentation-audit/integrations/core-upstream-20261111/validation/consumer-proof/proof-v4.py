"""One bounded selected-target asset collector replacing v3's catalog/target-bijection assumption."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
V3_SHA = '3be9675df87e5fd8d742948efa8faf632df59ee6df752541d2841a4c624ccccb'


def require(value, message):
    if not value:
        raise ValueError(message)


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def accepted_v3():
    require(identity(HERE / 'proof-v3.py')['sha256'] == V3_SHA, 'Immutable v3 adapter changed')
    module = load('accepted_consumer_v3', HERE / 'proof-v3.py')
    module.check_receipt()
    return module


def effective_source():
    source, _ = accepted_v3().effective_source()
    before = ("restored_binaries = (restored_binary_bindings(project, references, folder, assets, retain)\n"
              "                         if relative in data['declared_graph'] else\n"
              "                         dict(disposition='aggregation host; raw assets retained, referenced real consumers proved separately'))")
    transformations = [(before, 'restored_binaries = restored_binary_bindings(project, references, folder, assets, retain)', 1),
                       ("proof_revision='v3'", "proof_revision='v4'", 1)]
    for old, new, count in transformations:
        require(source.count(old) == count, 'Exact selected-target collector invocation changed')
        source = source.replace(old, new)
    return source, [dict(before=old, after=new, count=count) for old, new, count in transformations]


def check_receipt():
    path = HERE / 'correction-v4/receipt.json'
    receipt = json.loads(path.read_bytes())
    for row in receipt['protected_originals'] + receipt['new_files']:
        require(identity(Path(row['path'])) == row, 'V4 source binding changed: ' + row['path'])
    source, transformations = effective_source()
    require(receipt['transformations'] == transformations and
            receipt['effective_source_sha256'] == hashlib.sha256(source.encode()).hexdigest(), 'V4 effective source binding differs')
    return identity(path)['sha256']


def source_namespace(capture_name, receipt_sha):
    accepted_v3()
    helper = load('unchanged_msbuild_namespace', HERE / 'nuget-imports-v2.py')
    additional = load('unchanged_additional_multiset', HERE / 'correction-v3/additional-files.py')
    assets = load('selected_target_assets', HERE / 'correction-v4/assets.py')
    namespace = dict(__name__='audit_consumer_v4_predicates', __file__=str(HERE / 'proof.py'),
                     generated_msbuild_imports=helper.generated_msbuild_imports,
                     additional_file_multiset=additional.additional_file_multiset,
                     CAPTURE_NAME=capture_name, CORRECTION_RECEIPT_SHA256=receipt_sha)
    source, _ = effective_source()
    exec(compile(source, str(HERE / 'proof.py') + '#v3-plus-selected-target-asset-collector', 'exec'), namespace)
    namespace['restored_binary_bindings'] = assets.restored_binary_bindings
    old_tools, old_seal = namespace['tool_identities'], namespace['check_seal']

    def tools():
        return old_tools() + [identity(HERE / path) for path in
            ('run-lane-v2.sh', 'correction-v2/receipt.json', 'run-lane-v3.sh', 'correction-v3/receipt.json',
             'correction-v3/additional-files.py', 'correction-v3/preflight.py',
             'correction-v4/assets.py', 'correction-v4/preflight.py', 'correction-v4/lane.sh', 'correction-v4/receipt.json')]

    def seals():
        old_seal()
        accepted_v3()
        check_receipt()

    namespace['tool_identities'], namespace['check_seal'] = tools, seals
    return namespace


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('static', 'capture', 'verify'))
    parser.add_argument('--context', choices=('probe', 'examples', 'annotations', 'consumers', 'mac-comparison', 'fixture'))
    parser.add_argument('--attempt')
    parser.add_argument('--capture-name')
    parser.add_argument('--build-label')
    parser.add_argument('--live', action='store_true')
    args = parser.parse_args()
    receipt_sha = check_receipt()
    if args.action == 'static':
        base = source_namespace('static-not-capture', receipt_sha)
        print(json.dumps(dict(base['static_check'](), proof_revision='v4', correction_receipt_sha256=receipt_sha), indent=2))
        return
    for value in (args.attempt, args.capture_name):
        require(value and re.fullmatch(r'[a-z0-9][a-z0-9-]*', value), 'Explicit compiler/capture identity required')
    require(args.attempt != args.capture_name, 'Keep original compiler and versioned capture identities distinct')
    base = source_namespace(args.capture_name, receipt_sha)
    if args.action == 'capture':
        require(args.context and args.build_label, 'Exact context and original successful build label required')
        base['capture'](args.context, args.attempt, args.build_label)
    else:
        manifest = json.loads((HERE / 'evidence' / args.capture_name / 'manifest.json').read_bytes())
        require(manifest['attempt'] == args.attempt and manifest['capture_name'] == args.capture_name and
                manifest['proof_revision'] == 'v4' and manifest['correction_receipt_sha256'] == receipt_sha,
                'V4 manifest binding differs')
        base['verify'](args.attempt, args.live)


if __name__ == '__main__':
    main()
