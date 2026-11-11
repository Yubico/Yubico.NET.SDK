"""Retain the scoped recovery's fresh read-only checks and disposable qualification."""
import ast
import ctypes
import datetime
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]
VALIDATION = HERE.parent / 'validation'
RUNTIME = Path('/home/rupicola/Code/y/.core-docs-audit-runtime')
ENV = dict(os.environ, TMPDIR=str(RUNTIME / 'tmp'), TMP=str(RUNTIME / 'tmp'),
           TEMP=str(RUNTIME / 'tmp'), PYTHONDONTWRITEBYTECODE='1', GIT_OPTIONAL_LOCKS='0')


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def save(path, value):
    with path.open('x') as out:
        json.dump(value, out, indent=2)
        out.write('\n')


def capture(label, command):
    folder = HERE / label
    folder.mkdir(exist_ok=False)
    completed = subprocess.run(command, cwd=ROOT, env=ENV, capture_output=True)
    (folder / 'stdout.log').write_bytes(completed.stdout)
    (folder / 'stderr.log').write_bytes(completed.stderr)
    record = dict(command=command, cwd=str(ROOT), exit_code=completed.returncode,
                  stdout_sha256=sha(completed.stdout), stderr_sha256=sha(completed.stderr),
                  utc=datetime.datetime.now(datetime.timezone.utc).isoformat())
    save(folder / 'invocation.json', record)
    assert completed.returncode == 0, record
    return record


def quota():
    class Dqblk(ctypes.Structure):
        _fields_ = [(name, ctypes.c_uint64) for name in ('bhardlimit', 'bsoftlimit', 'curspace',
            'ihardlimit', 'isoftlimit', 'curinodes', 'btime', 'itime')] + [('valid', ctypes.c_uint32)]
    header = Path('/usr/include/asm/unistd_64.h')
    number = int(next(line.split()[-1] for line in header.read_text().splitlines()
                      if line.startswith('#define __NR_quotactl_fd ')))
    libc = ctypes.CDLL(None, use_errno=True)
    libc.syscall.restype = ctypes.c_long
    fd = os.open('/tmp', os.O_RDONLY | os.O_DIRECTORY)
    try:
        data = Dqblk()
        code = libc.syscall(ctypes.c_long(number), ctypes.c_int(fd), ctypes.c_uint(0x80000700),
                            ctypes.c_uint(os.getuid()), ctypes.byref(data))
        assert code == 0, ctypes.get_errno()
        return dict(operation='read-only quotactl_fd Q_GETQUOTA USRQUOTA', uid=os.getuid(),
                    syscall=number, syscall_header_sha256=sha(header.read_bytes()),
                    hard_bytes=data.bhardlimit * 1024, current_bytes=data.curspace,
                    remaining_bytes=data.bhardlimit * 1024 - data.curspace,
                    inode_hardlimit=data.ihardlimit, current_inodes=data.curinodes)
    finally:
        os.close(fd)


def main():
    tools = []
    for path in [VALIDATION / name for name in ('run-command-v2.py', 'check-preserved-relocated.py',
                                              'qualify-logger-v2.py', 'linux-evidence-gates-home.py')] + [HERE / 'verify-recovery.py', Path(__file__)]:
        raw = path.read_bytes()
        ast.parse(raw, filename=str(path))
        tools.append(dict(path=path.relative_to(ROOT).as_posix(), bytes=len(raw), sha256=sha(raw)))
    diagnostic_commands = [['findmnt', '-T', '/tmp', '-o', 'TARGET,SOURCE,FSTYPE,OPTIONS'],
                           ['findmnt', '-T', str(ROOT), '-o', 'TARGET,SOURCE,FSTYPE,OPTIONS'],
                           ['df', '-h', '/tmp', str(ROOT)], ['df', '-i', '/tmp', str(ROOT)], ['id']]
    diagnostics = []
    for command in diagnostic_commands:
        done = subprocess.run(command, capture_output=True, env=ENV)
        diagnostics.append(dict(command=command, exit_code=done.returncode,
                                stdout=done.stdout.decode(), stderr=done.stderr.decode()))
    save(HERE / 'environment-diagnosis.json', dict(uid_quota=quota(), diagnostics=diagnostics,
        original_error='Coordinator reported Python errno 122 (EDQUOT); actual build log records disk-quota copy failures.',
        source_error_stack='Unavailable in the partial original records; no stack is reconstructed.',
        original_build_pid_2898163_exists=Path('/proc/2898163').exists(),
        original_toolchain_pid_2897864_exists=Path('/proc/2897864').exists(),
        policy='Original source and all other tmpfs worktrees/evidence remain untouched. Quota/config/install changes: none.'))
    checks = [capture('preservation-relocated-first', [sys.executable, '-B', str(VALIDATION / 'check-preserved-relocated.py')]),
              capture('relocation-verified-first', [sys.executable, '-B', str(HERE / 'verify-recovery.py')]),
              capture('linux-home-adapter-static-first', [sys.executable, '-B', str(VALIDATION / 'linux-evidence-gates-home.py'), '--check-source'])]
    qualification = RUNTIME / 'qualification/logger-v2-first'
    report = json.loads((qualification / 'qualification.json').read_bytes())
    assert report['logger_sha256'] == sha((VALIDATION / 'run-command-v2.py').read_bytes())
    assert report['qualifier_sha256'] == sha((VALIDATION / 'qualify-logger-v2.py').read_bytes())
    shutil.copytree(qualification, HERE / 'logger-v2-qualification', symlinks=True)
    save(HERE / 'tooling-provenance.json', dict(tools=tools, checks=checks,
        preserved_v1_logger_sha256=sha((VALIDATION / 'run-command.py').read_bytes()),
        original_before_sha256=sha((HERE.parent / 'evidence/preservation-before.json').read_bytes()),
        linux_gate_source_sha256=sha((ROOT / 'verification/LinuxHidRouteVerification/test_evidence_gates.py').read_bytes()),
        qualification_report_sha256=sha((qualification / 'qualification.json').read_bytes()),
        runtime_root=str(RUNTIME), environment={name: ENV[name] for name in ('TMPDIR', 'TMP', 'TEMP', 'PYTHONDONTWRITEBYTECODE')},
        documentation_sources=['https://github.com/git/git/blob/v2.56.0/builtin/worktree.c',
            'https://github.com/git/git/blob/v2.56.0/Documentation/git-worktree.adoc',
            'https://github.com/python/cpython/blob/main/Doc/library/subprocess.rst',
            'https://github.com/python/cpython/blob/main/Doc/library/tempfile.rst'],
        scope='Storage recovery, read-only identity checks and disposable Python logger qualification only. No SDK/native/device gate.'))
    print('Retained recovery diagnostics, relocated preservation, full transfer/source binding, static Linux home adapter, and disposable logger qualification.')


if __name__ == '__main__':
    main()
