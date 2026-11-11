"""Bounded AdditionalFiles multiset correction; immutable original proof and accepted v2 namespace code."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
V2_SHA = '78024e05a44e82b309c3ec70a5814c13513f0a9436ccc345c224e0b19380b695'


def require(value, message):
    if not value:
        raise ValueError(message)


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def accepted_v2():
    path = HERE / 'proof-v2.py'
    require(identity(path)['sha256'] == V2_SHA, 'Accepted v2 adapter changed')
    module = load_module('accepted_consumer_v2', path)
    module.check_receipt()
    return module


def effective_source():
    source, _ = accepted_v2().effective_source()
    transformations = [
        ("additional = items('additional-files.txt')",
         "additional, additional_proof = additional_file_multiset(project, folder, args, retain)", 1),
        ("('/analyzerconfig:', configs), ('/additionalfile:', additional)):",
         "('/analyzerconfig:', configs)):", 1),
        ("return dict(project=str(project), policy=policy, graph=graph, canonical_analyzers=analyzer,",
         "return dict(project=str(project), policy=policy, graph=graph, canonical_analyzers=analyzer, additional_file_proof=additional_proof,", 1),
        ("proof_revision='v2'", "proof_revision='v3'", 1),
    ]
    for before, after, count in transformations:
        require(source.count(before) == count, 'Exact AdditionalFiles transformation site changed')
        source = source.replace(before, after)
    return source, [dict(before=b, after=a, count=n) for b, a, n in transformations]


def check_receipt():
    receipt_path = HERE / 'correction-v3/receipt.json'
    receipt = json.loads(receipt_path.read_bytes())
    for row in receipt['protected_originals'] + receipt['new_files']:
        require(identity(Path(row['path'])) == row, 'V3 binding changed: ' + row['path'])
    source, transformations = effective_source()
    require(receipt['transformations'] == transformations and
            receipt['effective_source_sha256'] == hashlib.sha256(source.encode()).hexdigest(), 'V3 transformation binding differs')
    return identity(receipt_path)['sha256']


def source_namespace(capture_name, receipt_sha):
    """Construct predicates only. Offline preflight calls compiler_project with a read-only collector."""
    v2 = accepted_v2()
    helper = load_module('accepted_namespace_helper', HERE / 'nuget-imports-v2.py')
    additional = load_module('consumer_additional_multiset', HERE / 'correction-v3/additional-files.py')
    namespace = dict(__name__='audit_consumer_v3_predicates', __file__=str(HERE / 'proof.py'),
                     generated_msbuild_imports=helper.generated_msbuild_imports,
                     additional_file_multiset=additional.additional_file_multiset,
                     CAPTURE_NAME=capture_name, CORRECTION_RECEIPT_SHA256=receipt_sha)
    source, _ = effective_source()
    exec(compile(source, str(HERE / 'proof.py') + '#accepted-v2-plus-additional-multiset', 'exec'), namespace)
    original_tools = namespace['tool_identities']

    def tools():
        return original_tools() + [identity(HERE / path) for path in
            ('run-lane-v2.sh', 'correction-v2/receipt.json', 'run-lane-v3.sh',
             'correction-v3/additional-files.py', 'correction-v3/preflight.py', 'correction-v3/receipt.json')]

    namespace['tool_identities'] = tools
    original_seal = namespace['check_seal']

    def seals():
        original_seal()
        v2.check_receipt()
        check_receipt()

    namespace['check_seal'] = seals
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
        base = source_namespace('static-not-a-compiler-attempt', receipt_sha)
        print(json.dumps(dict(base['static_check'](), proof_revision='v3', correction_receipt_sha256=receipt_sha), indent=2))
        return
    for value in (args.attempt, args.capture_name):
        require(value and re.fullmatch(r'[a-z0-9][a-z0-9-]*', value), 'Explicit compiler attempt/new capture name required')
    require(args.attempt != args.capture_name, 'Compiler attempt and evidence identities must remain distinct')
    base = source_namespace(args.capture_name, receipt_sha)
    if args.action == 'capture':
        require(args.context and args.build_label, 'Actual context and original successful build label required')
        base['capture'](args.context, args.attempt, args.build_label)
    else:
        manifest = json.loads((HERE / 'evidence' / args.capture_name / 'manifest.json').read_bytes())
        require(manifest['attempt'] == args.attempt and manifest['capture_name'] == args.capture_name and
                manifest['proof_revision'] == 'v3' and manifest['correction_receipt_sha256'] == receipt_sha,
                'V3 compiler/capture identity differs')
        base['verify'](args.attempt, args.live)


if __name__ == '__main__':
    main()
