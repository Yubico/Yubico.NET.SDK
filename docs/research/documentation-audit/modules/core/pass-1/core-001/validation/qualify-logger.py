"""Coordinator-only adapter for the four reviewed disposable Python qualification cases."""
import argparse
import importlib.util
import re
import sys
from foundation import HERE, ROOT, RUNTIME, INTEGRATION, PINS, environment, pinned, require


def logger():
    spec = importlib.util.spec_from_file_location('core_logger_adapter', HERE / 'run-command.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.load()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--case', choices=('nonzero', 'orphan', 'retention', 'receipt'))
    parser.add_argument('--attempt', required=True)
    args = parser.parse_args()
    if re.fullmatch(r'[a-z0-9][a-z0-9-]*', args.attempt) is None:
        raise ValueError('Use a fresh simple qualification attempt')
    environment()
    original = INTEGRATION / 'validation/qualify-logger-v2.py'
    source = pinned(original, PINS['qualify-logger-v2.py']).decode()
    old = "(HERE / 'run-command-v2.py').read_bytes()"
    require(source.count(old) == 1, 'Reconcile the qualification logger identity expression')
    spec = importlib.util.spec_from_file_location('core_qualification_adapter', original)
    module = importlib.util.module_from_spec(spec)
    exec(compile(source.replace(old, "(HERE / 'run-command.py').read_bytes()"), str(original) + '[Core-logger-identity]', 'exec'), module.__dict__)
    module.HERE, module.ROOT, module.RUNTIME = HERE, ROOT, RUNTIME
    module.__file__ = str(HERE / 'qualify-logger.py')
    module.ATTEMPT = args.attempt
    module.WORK = RUNTIME / 'qualification' / ('logger-v2-' + args.attempt)
    module.load = logger
    sys.exit(module.case(args.case) if args.case else module.main())
