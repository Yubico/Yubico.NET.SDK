"""Home-backed, group-owned command capture. Original v1 evidence stays immutable.

Linux-only qualification: start one new session, adopt/reap orphan descendants,
stop only that proven group, and retain an external durable lease/receipt journal.
"""
import argparse
import ctypes
import datetime
import errno
import fcntl
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import selectors
import shlex
import signal
import subprocess
import sys
import time

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]
RUNTIME = Path('/home/rupicola/Code/y/.core-docs-audit-runtime')
ORIGINAL_SHA = '61bd7b3f218da8051c44ade033887ddbe598d9d9b2f95cc3fa7a8b8696838640'


def require(value, message):
    if not value:
        raise ValueError(message)


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def original():
    path = HERE / 'run-command.py'
    require(identity(path)['sha256'] == ORIGINAL_SHA, 'Preserved v1 logger identity changed')
    spec = importlib.util.spec_from_file_location('preserved_logger_v1', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def atomic_json(path, value):
    """Publish a new immutable JSON file; a failed write preserves its partial file."""
    require(not path.exists(), f'Preserve existing receipt: {path}')
    partial = path.with_name(path.name + f'.partial-{os.getpid()}')
    with partial.open('xb') as output:
        output.write((json.dumps(value, indent=2) + '\n').encode())
        output.flush()
        os.fsync(output.fileno())
    require(not path.exists(), f'Receipt appeared while writing: {path}')
    os.replace(partial, path)
    directory = os.open(path.parent, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(directory)
    finally:
        os.close(directory)


def proc_rows():
    rows = []
    for path in Path('/proc').glob('[0-9]*'):
        try:
            if path.stat().st_uid != os.getuid():
                continue
            raw = (path / 'stat').read_text()
            fields = raw[raw.rfind(')') + 2:].split()
            rows.append(dict(pid=int(path.name), state=fields[0], ppid=int(fields[1]),
                             pgid=int(fields[2]), sid=int(fields[3]), start_ticks=int(fields[19])))
        except (OSError, ValueError, IndexError):
            pass
    return rows


def group_state(child, leader_start):
    child.poll()  # Reap only the direct Popen child through Popen.
    rows = proc_rows()
    adopted = [row for row in rows if row['ppid'] == os.getpid() and row['pid'] != child.pid]
    for row in adopted:
        if row['state'] == 'Z':
            try:
                os.waitpid(row['pid'], os.WNOHANG)
            except ChildProcessError:
                pass
    rows = proc_rows()
    members = [row for row in rows if row['pgid'] == child.pid]
    require(all(row['sid'] == child.pid and row['start_ticks'] >= leader_start for row in members),
            'Process group ownership changed; refuse to signal')
    escaped = [row for row in rows if row['pgid'] != child.pid and
               (row['sid'] == child.pid or row['ppid'] == os.getpid())]
    return dict(members=members, escaped=escaped)


def stop_owned(child, leader_start, pump):
    record = dict(pgid=child.pid, sid=child.pid, leader_start_ticks=leader_start,
                  before=group_state(child, leader_start), signals=[])
    for sig, duration in ((signal.SIGTERM, 3.0), (signal.SIGKILL, 3.0)):
        state = group_state(child, leader_start)
        if not state['members']:
            break
        # start_new_session establishes this exclusive group; every current member
        # must still satisfy the captured session/start-time ownership bound.
        try:
            os.killpg(child.pid, sig)
            record['signals'].append(dict(signal=sig.name, proven_members=state['members']))
        except ProcessLookupError:
            pass
        deadline = time.monotonic() + duration
        while time.monotonic() < deadline:
            pump(0.05)
            if not group_state(child, leader_start)['members']:
                break
    record['after'] = group_state(child, leader_start)
    record['quiescent'] = not record['after']['members'] and not record['after']['escaped']
    return record


def retain(source, target):
    require(source.is_file() and not source.is_symlink(), f'Require regular text: {source}')
    raw = source.read_bytes()
    raw.decode('utf-8-sig')
    require(b'\0' not in raw and not raw.startswith((b'MZ', b'\x7fELF')), 'Text retention only')
    target.parent.mkdir(parents=True, exist_ok=True)
    partial = target.with_name(target.name + '.partial')
    with partial.open('xb') as output:
        output.write(raw)
        output.flush()
        os.fsync(output.fileno())
    require(not target.exists(), f'Preserve existing retained file: {target}')
    os.replace(partial, target)
    require(target.read_bytes() == raw, 'Retained bytes differ')
    return dict(source=identity(source), retained=identity(target))


def verify_result(root, run, retained_run, command, code, args, v1):
    require((retained_run / 'run.log').is_file(), 'Retained actual run.log missing')
    result = json.loads((retained_run / 'result.json').read_bytes())
    require(result['runDirectory'] == run.relative_to(root).as_posix(), 'Actual run identity mismatch')
    canonical = command if command[2:3] == ['--'] else command[:2] + ['--'] + command[2:]
    require(shlex.split(result['command']) == canonical, 'Actual command identity mismatch')
    text = (retained_run / 'result.txt').read_text()
    require('RESULT: PASSED' in text and 'Warnings: 0' in text, 'RESULT text disagrees')
    counts = v1.check_result(result, code, args.expect_target, args.expect_project)
    for project in result['projects']:
        log = (root / project['log']).resolve()
        require(log.is_relative_to(run.resolve()) and (retained_run / log.relative_to(run.resolve())).is_file(),
                'Actual project step log missing')
    if result['projects']:
        require(f"tests: {counts['executed']} executed, 0 failed, {counts['skipped']} skipped" in text,
                'RESULT text/JSON counts disagree')
    return counts


def capture(args, faults=()):
    v1 = original()
    root = args.cwd.resolve()
    command = args.command[1:] if args.command[:1] == ['--'] else args.command
    require(root == ROOT and command, 'Use the relocated checkout and supply a command')
    require(re.fullmatch(r'[a-z0-9][a-z0-9-]*', args.label), 'Use a fresh simple attempt label')
    output_root = (args.output_root or HERE / 'commands-v2').resolve()
    qualification = output_root.is_relative_to(RUNTIME / 'qualification')
    require(output_root == HERE / 'commands-v2' or qualification, 'Output stays in packet or disposable qualification scope')
    require(not faults or qualification and Path(command[0]).resolve() == Path(sys.executable).resolve(),
            'Fault injection is restricted to disposable Python qualification')
    if args.expect_projects_file:
        args.expect_project += json.loads(args.expect_projects_file.read_bytes())
    require(len(args.expect_project) == len(set(args.expect_project)), 'Duplicate expected project')
    if args.toolchain:
        require(command[:2] == ['dotnet', 'toolchain.cs'] and args.expect_target, 'Require canonical toolchain and expected targets')
    else:
        require(not args.expect_project and not args.expect_target, 'Expectations require --toolchain')
    require((RUNTIME / 'tmp').is_dir(), 'Dedicated home temporary directory is required')
    environment = dict(os.environ, TMPDIR=str(RUNTIME / 'tmp'), TMP=str(RUNTIME / 'tmp'),
                       TEMP=str(RUNTIME / 'tmp'), PYTHONDONTWRITEBYTECODE='1')
    os.environ.update({name: environment[name] for name in ('TMPDIR', 'TMP', 'TEMP', 'PYTHONDONTWRITEBYTECODE')})
    journal_root = output_root / 'journals' if qualification else RUNTIME / 'command-journals'
    journal_root.mkdir(parents=True, exist_ok=True)
    with (RUNTIME / 'coordinator-command-v2.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        for started in journal_root.glob('*/started.json'):
            final = started.parent / 'final.json'
            require(final.exists() and json.loads(final.read_bytes()).get('quiescent') is True,
                    f'Previous command has no durable quiescence: {started.parent}')
        v1.active_guard(root)
        destination = output_root / args.label
        destination.mkdir(parents=True, exist_ok=False)
        journal = journal_root / args.label
        journal.mkdir(exist_ok=False)
        invocation = dict(command=command, cwd=str(root), logger=identity(Path(__file__)),
            preserved_v1_sha256=ORIGINAL_SHA, started=datetime.datetime.now(datetime.timezone.utc).isoformat(),
            environment={name: environment[name] for name in ('TMPDIR', 'TMP', 'TEMP', 'PYTHONDONTWRITEBYTECODE')},
            expected_targets=args.expect_target, expected_projects=args.expect_project,
            primary_output=str(destination), external_journal=str(journal), qualification_faults=list(faults))
        atomic_json(journal / 'started.json', invocation)  # Durable admission before starting a process.
        atomic_json(destination / 'invocation.json', invocation)
        libc = ctypes.CDLL(None, use_errno=True)
        require(libc.prctl(36, 1, 0, 0, 0) == 0, 'PR_SET_CHILD_SUBREAPER failed')
        runs = root / 'artifacts/toolchain/runs'
        previous = {path.name for path in runs.iterdir()} if runs.exists() else set()
        errors, retained, stream_errors = [], [], []
        child, ownership, leader_start = None, None, None
        pipes_registered = False
        pipe_eof_reached = False
        selector = selectors.DefaultSelector()
        streams = {}

        def pump(timeout):
            for key, _ in selector.select(timeout):
                block = os.read(key.fd, 65536)
                if not block:
                    selector.unregister(key.fileobj)
                    key.fileobj.close()
                    continue
                name = key.data
                try:
                    streams[name].write(block)
                    streams[name].flush()
                except OSError as error:
                    stream_errors.append(f'{name}: {error}')
                    # Keep draining the child; the retained prefix remains a partial stream.
                try:
                    console = sys.stdout if name == 'stdout.log' else sys.stderr
                    console.buffer.write(block)
                    console.flush()
                except (BrokenPipeError, OSError):
                    pass

        try:
            streams = {name: (destination / name).open('xb') for name in ('stdout.log', 'stderr.log')}
            child = subprocess.Popen(command, cwd=root, env=environment, stdin=subprocess.DEVNULL,
                                     stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
            leader = next(row for row in proc_rows() if row['pid'] == child.pid)
            leader_start = leader['start_ticks']
            require(leader['pgid'] == child.pid and leader['sid'] == child.pid, 'New session ownership failed')
            atomic_json(journal / 'spawn.json', dict(pid=child.pid, pgid=child.pid, sid=child.pid,
                                                   start_ticks=leader_start, subreaper=True))
            for pipe, name in ((child.stdout, 'stdout.log'), (child.stderr, 'stderr.log')):
                os.set_blocking(pipe.fileno(), False)
                selector.register(pipe, selectors.EVENT_READ, name)
            pipes_registered = True
            while child.poll() is None and not stream_errors:
                pump(0.1)
            ownership = stop_owned(child, leader_start, pump)
            deadline = time.monotonic() + 2.0
            while selector.get_map() and time.monotonic() < deadline:
                pump(0.05)
            require(not selector.get_map(), 'Child pipe EOF was not reached within the drain bound')
            require(ownership['quiescent'], 'Owned process/session still has survivors')
            require(not stream_errors, f'Incomplete stream retention: {stream_errors}')
            require(child.returncode == 0, f'Command exited {child.returncode}')
        except (Exception, KeyboardInterrupt) as error:
            errors.append(f'{type(error).__name__}: {error}')
        finally:
            if child is not None and leader_start is not None:
                try:
                    final_state = group_state(child, leader_start)
                    if final_state['members'] or final_state['escaped']:
                        ownership = stop_owned(child, leader_start, pump)
                    if ownership is None:
                        ownership = dict(after=final_state, quiescent=not final_state['members'] and not final_state['escaped'])
                except (Exception, KeyboardInterrupt) as error:
                    errors.append(f'Ownership reconciliation failed: {error}')
            # Closing a descriptor locally is distinct from observing child EOF.
            pipe_eof_reached = pipes_registered and not selector.get_map()
            for key in list(selector.get_map().values()):
                selector.unregister(key.fileobj)
                key.fileobj.close()
            selector.close()
            for output in streams.values():
                try:
                    output.flush()
                    os.fsync(output.fileno())
                    output.close()
                except OSError as error:
                    errors.append(f'Stream finalization failed: {error}')
                    stream_errors.append(f'Stream finalization failed: {error}')
        quiescent = child is None or ownership is not None and ownership.get('quiescent') is True
        new_runs = sorted(path for path in runs.iterdir() if path.name not in previous) if runs.exists() else []
        try:
            if 'retention' in faults:
                raise OSError(errno.EDQUOT, 'Injected disposable retention failure')
            for run in new_runs:
                require(run.is_dir() and not run.is_symlink(), 'Unexpected actual run')
                for source in sorted(run.rglob('*')):
                    require(not source.is_symlink(), 'Symlinked actual run entry')
                    if source.is_file():
                        retained.append(retain(source, destination / 'toolchain' / run.name / source.relative_to(run)))
            for relative in args.retain_text:
                source = root / relative
                require(not Path(relative).is_absolute() and '..' not in Path(relative).parts and
                        source.resolve().is_relative_to(root) and source.exists(), f'Unsafe/missing retention: {relative}')
                require(not source.resolve().is_relative_to(output_root), 'Retention may not recursively copy itself')
                for path in sorted(source.rglob('*')) if source.is_dir() else [source]:
                    require(not path.is_symlink(), 'Symlinked extra retention')
                    if path.is_file():
                        retained.append(retain(path, destination / 'extra' / path.relative_to(root)))
        except Exception as error:
            errors.append(f'Retention incomplete: {type(error).__name__}: {error}')
        counts = None
        try:
            if args.toolchain:
                require(len(new_runs) == 1, 'Expected exactly one newly created actual toolchain run')
                counts = verify_result(root, new_runs[0], destination / 'toolchain' / new_runs[0].name,
                                       command, child.returncode if child else None, args, v1)
        except Exception as error:
            errors.append(f'Result unverified: {type(error).__name__}: {error}')
        # Receipt writes occur inside error handling. The external started journal
        # remains an admission blocker until a durable final quiescence record exists.
        receipt = dict(**invocation, exit_code=child.returncode if child else None,
            status='failed' if errors or not quiescent else 'passed', errors=errors,
            quiescent=quiescent, ownership=ownership, stream_complete=pipe_eof_reached and not stream_errors,
            retention_complete=not any('Retention incomplete' in error for error in errors),
            result_counts=counts, retained=retained,
            streams=[identity(destination / name) for name in ('stdout.log', 'stderr.log') if (destination / name).exists()],
            finished=datetime.datetime.now(datetime.timezone.utc).isoformat())
        primary_error = None
        try:
            if 'receipt' in faults:
                raise OSError(errno.EDQUOT, 'Injected disposable primary receipt failure')
            atomic_json(destination / 'receipt.json', receipt)
        except Exception as error:
            primary_error = f'Primary receipt incomplete: {type(error).__name__}: {error}'
            receipt = dict(receipt, status='failed', errors=errors + [primary_error])
        try:
            atomic_json(journal / 'final.json', receipt)
        except Exception as error:
            # started.json reliably blocks subsequent admissions even if every later write fails.
            print(f'BLOCKED: final external receipt failed; admission journal stays unresolved: {journal}: {error}', file=sys.stderr)
            return 1
        print(f"\nRetained command v2: {destination}; external receipt: {journal / 'final.json'}; status={receipt['status']}; quiescent={quiescent}")
        return 0 if receipt['status'] == 'passed' and primary_error is None else 1


def parser():
    result = argparse.ArgumentParser(description=__doc__)
    result.add_argument('--label', required=True)
    result.add_argument('--cwd', type=Path, default=ROOT)
    result.add_argument('--toolchain', action='store_true')
    result.add_argument('--expect-target', action='append', default=[])
    result.add_argument('--expect-project', action='append', default=[])
    result.add_argument('--expect-projects-file', type=Path)
    result.add_argument('--retain-text', action='append', default=[])
    result.add_argument('--output-root', type=Path, help='Disposable home qualification scope only')
    result.add_argument('command', nargs=argparse.REMAINDER)
    return result


if __name__ == '__main__':
    def interrupted(signum, frame):
        raise KeyboardInterrupt(f'Logger received signal {signum}')
    signal.signal(signal.SIGTERM, interrupted)
    try:
        sys.exit(capture(parser().parse_args()))
    except (Exception, KeyboardInterrupt) as error:
        print(f'Capture v2 blocked: {type(error).__name__}: {error}', file=sys.stderr)
        sys.exit(1)
