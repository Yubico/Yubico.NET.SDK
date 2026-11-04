"""Bounded read-only PR gate. Exit 1 waits quietly; exit 0 wakes for ready/error/blocker."""
import datetime
import fcntl
import json
from pathlib import Path
import re
import subprocess
import time

HERE = Path(__file__).resolve().parent
REPO = 'Yubico/Yubico.NET.SDK'
FIELDS = 'number,title,url,state,baseRefName,headRefOid,mergedAt,mergeCommit,updatedAt'


def sha(value):
    return isinstance(value, str) and re.fullmatch(r'[0-9a-f]{40}', value) is not None


def classify(pr):
    if pr.get('number') != 745 or pr.get('state') not in ('OPEN', 'CLOSED', 'MERGED'):
        raise ValueError('Missing or invalid PR identity/state')
    if not isinstance(pr.get('baseRefName'), str):
        raise ValueError('Missing PR base')
    if pr['baseRefName'] != 'yubikit':
        return 'WRONG_BASE'
    if pr['state'] == 'OPEN':
        return 'WAITING_FOR_MERGE'
    if pr['state'] == 'CLOSED':
        return 'CLOSED_UNMERGED'
    if not pr.get('mergedAt') or not sha((pr.get('mergeCommit') or {}).get('oid')):
        return 'MERGE_UNVERIFIED'
    return 'CHECK_CONTAINMENT'


def contained(merge, tip, comparison, fresh_tip):
    return (sha(merge) and sha(tip) and tip == fresh_tip
            and comparison.get('status') in ('ahead', 'identical')
            and comparison.get('merge_base_commit', {}).get('sha') == merge)


def observe():
    deadline = time.monotonic() + 60
    result = {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'repository': REPO}

    def query(*args):
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            raise TimeoutError('Overall 60-second deadline exceeded')
        run = subprocess.run(['gh', *args], text=True, capture_output=True, timeout=remaining)
        if run.returncode:
            # Retain status without copying authentication diagnostics or credentials.
            raise RuntimeError(f'gh {args[0]} returned exit {run.returncode}')
        return json.loads(run.stdout)

    try:
        pr = query('pr', 'view', '745', '--repo', REPO, '--json', FIELDS)
        result['pr'] = pr
        result['status'] = classify(pr)
        if result['status'] == 'CHECK_CONTAINMENT':
            merge = pr['mergeCommit']['oid']
            tip = query('api', f'repos/{REPO}/git/ref/heads/yubikit')['object']['sha']
            if not sha(tip):
                raise ValueError('Invalid branch tip SHA')
            comparison = query('api', f'repos/{REPO}/compare/{merge}...{tip}')
            fresh_tip = query('api', f'repos/{REPO}/git/ref/heads/yubikit')['object']['sha']
            result.update(merge_commit=merge, branch_tip=tip, rechecked_tip=fresh_tip,
                          comparison=comparison)
            result['status'] = 'MERGED_READY' if contained(merge, tip, comparison, fresh_tip) else 'MERGE_UNVERIFIED'
    except Exception as error:
        result.update(status='UNKNOWN', error_type=type(error).__name__, error=str(error))
    return result


def main():
    with (HERE / 'check.lock').open('a') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            print(json.dumps({'status': 'CHECK_ALREADY_RUNNING'}))
            return 1
        result = observe()
        with (HERE / 'observations.jsonl').open('a') as output:
            output.write(json.dumps(result) + '\n')
        print(json.dumps(result))
        return 1 if result['status'] == 'WAITING_FOR_MERGE' else 0


if __name__ == '__main__':
    raise SystemExit(main())
