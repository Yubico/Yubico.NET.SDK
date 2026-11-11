"""Additive namespace correction and evidence-name adapter over the immutable v1 compiler proof."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
BASE_SHA = '54cb56e12e3b683d2c662c133dc6e23eadef355d7e515f478a87b36a87f25281'


def require(value, message):
    if not value:
        raise ValueError(message)


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def effective_source():
    path = HERE / 'proof.py'
    require(identity(path)['sha256'] == BASE_SHA, 'Immutable original compiler proof changed')
    source = path.read_text()
    transformations = [
        ("matches = [n for n in ET.parse(path).iter('Import') if n.attrib.get('Project') == needle]",
         "matches = [n for n in generated_msbuild_imports(path, package) if n.attrib.get('Project') == needle]", 1),
        ("destination = HERE / 'evidence' / attempt",
         "destination = HERE / 'evidence' / CAPTURE_NAME", 2),
        ("result = dict(status='captured-current-compiler-inputs', context=context, attempt=attempt,",
         "result = dict(status='captured-current-compiler-inputs', context=context, attempt=attempt, "
         "capture_name=CAPTURE_NAME, proof_revision='v2', correction_receipt_sha256=CORRECTION_RECEIPT_SHA256,", 1),
    ]
    for before, after, count in transformations:
        require(source.count(before) == count, 'Exact bounded transformation site changed')
        source = source.replace(before, after)
    return source, [dict(before=before, after=after, count=count) for before, after, count in transformations]


def check_receipt():
    path = HERE / 'correction-v2/receipt.json'
    receipt = json.loads(path.read_bytes())
    for row in receipt['protected_originals'] + receipt['new_tools'] + [receipt['preservation_manifest']]:
        require(identity(Path(row['path'])) == row, 'Versioned correction binding changed: ' + row['path'])
    source, transformations = effective_source()
    require(receipt['transformations'] == transformations and
            receipt['effective_base_source_sha256'] == hashlib.sha256(source.encode()).hexdigest(),
            'Versioned transformation receipt differs')
    return identity(path)['sha256']


def module(capture_name):
    receipt_sha = check_receipt()
    spec = importlib.util.spec_from_file_location('audit_nuget_imports_v2', HERE / 'nuget-imports-v2.py')
    helper = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helper)
    namespace = dict(__name__='audit_consumer_proof_v2_base', __file__=str(HERE / 'proof.py'),
                     generated_msbuild_imports=helper.generated_msbuild_imports,
                     CAPTURE_NAME=capture_name, CORRECTION_RECEIPT_SHA256=receipt_sha)
    source, _ = effective_source()
    exec(compile(source, str(HERE / 'proof.py') + '#v2-namespace-and-evidence-routing', 'exec'), namespace)
    original_tools = namespace['tool_identities']

    def tools():
        return original_tools() + [identity(HERE / 'run-lane-v2.sh'), identity(HERE / 'correction-v2/receipt.json')]

    namespace['tool_identities'] = tools
    original_seal = namespace['check_seal']

    def seals():
        original_seal()
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
    if args.action == 'static':
        base = module('static-not-a-compiler-attempt')
        result = base['static_check']()
        print(json.dumps(dict(result, correction_receipt_sha256=check_receipt(), proof_revision='v2'), indent=2))
        return
    for name in (args.attempt, args.capture_name):
        require(name and re.fullmatch(r'[a-z0-9][a-z0-9-]*', name), 'Explicit compiler attempt and new capture name required')
    require(args.capture_name != args.attempt, 'Keep original compiler and versioned evidence identities separate')
    base = module(args.capture_name)
    if args.action == 'capture':
        require(args.context and args.build_label, 'Exact context and original successful build label required')
        base['capture'](args.context, args.attempt, args.build_label)
    else:
        manifest = json.loads((HERE / 'evidence' / args.capture_name / 'manifest.json').read_bytes())
        require(manifest['attempt'] == args.attempt and manifest['capture_name'] == args.capture_name and
                manifest['proof_revision'] == 'v2' and manifest['correction_receipt_sha256'] == check_receipt(),
                'Compiler attempt / versioned capture identity mismatch')
        base['verify'](args.attempt, args.live)


if __name__ == '__main__':
    main()
