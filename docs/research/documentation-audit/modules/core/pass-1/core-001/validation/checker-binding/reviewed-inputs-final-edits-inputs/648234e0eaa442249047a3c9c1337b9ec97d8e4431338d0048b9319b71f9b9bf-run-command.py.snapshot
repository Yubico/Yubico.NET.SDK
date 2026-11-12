"""Core adapter for the reviewed v2 process/session, stream and atomic receipt algorithm."""
import signal
import sys
from foundation import HERE, ROOT, RUNTIME, baseline, environment, frozen, participating, require


def load():
    baseline()
    module = frozen('run-command-v2.py')
    # Core validation/ is two directories deeper than the integration validation/.
    # Set every participating module's globals after loading its immutable source.
    module.HERE, module.ROOT, module.RUNTIME = HERE, ROOT, RUNTIME
    module.__file__ = str(HERE / 'run-command.py')

    def original():
        helper = frozen('run-command.py')
        helper.HERE, helper.ROOT = HERE, ROOT
        return helper

    module.original = original
    atomic = module.atomic_json

    def atomic_json(path, value):
        require(path.resolve().is_relative_to(HERE / 'commands-v2') or
                path.resolve().is_relative_to(RUNTIME), 'Core logger receipt destination escaped')
        if 'command' in value:
            value = dict(value, reuse=participating(['run-command-v2.py', 'run-command.py'], ['run-command.py']))
        return atomic(path, value)

    module.atomic_json = atomic_json
    return module


if __name__ == '__main__':
    def interrupted(signum, frame):
        raise KeyboardInterrupt(f'Core logger received signal {signum}')
    signal.signal(signal.SIGTERM, interrupted)
    try:
        environment()
        logger = load()
        sys.exit(logger.capture(logger.parser().parse_args()))
    except (Exception, KeyboardInterrupt) as error:
        print(f'Core capture blocked: {type(error).__name__}: {error}', file=sys.stderr)
        sys.exit(1)
