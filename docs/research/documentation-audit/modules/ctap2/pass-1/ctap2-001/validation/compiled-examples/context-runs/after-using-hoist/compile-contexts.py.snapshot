"""Exclusive serialized compile-only lane; never execute hosts/tests/device operations."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p/'toolchain.cs').is_file())
TOOLS = HERE.parent/'checker-source'
MANIFEST = TOOLS/'context/contexts-manifest.json'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--attempt',required=True)
    parser.add_argument('--context',action='append')
    args = parser.parse_args()
    assert re.fullmatch(r'[a-z][a-z0-9-]*',args.attempt)
    destination = HERE/'context-runs'/args.attempt
    assert not destination.exists(), 'Never overwrite previous attempt evidence'
    contexts = json.loads(MANIFEST.read_text())['contexts']
    if args.context:
        assert set(args.context) <= {c['id'] for c in contexts}
        contexts = [c for c in contexts if c['id'] in args.context]
    destination.mkdir(parents=True)
    snapshots = []
    for p in [Path(__file__),HERE/'extract.py',HERE/'selection.py',HERE/'verify-inputs.py',HERE/'manifest.json',MANIFEST,TOOLS/'prepare-project-context.py']:
        target = destination/(p.name+'.snapshot')
        shutil.copy2(p,target)
        snapshots.append(dict(path=str(p),snapshot=str(target),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    (destination/'inputs.json').write_text(json.dumps(snapshots,indent=2)+'\n')
    results = []
    for context in contexts:
        identifier = context['id']
        base = ['python3',str(TOOLS/'prepare-project-context.py'),'--repo',str(ROOT),'--manifest',str(MANIFEST),'--context',identifier]
        commands = [base,[*base,'--check'],['dotnet','toolchain.cs','--','build','--project',f'AuditCt001.Examples.{identifier}','--clean'],[*base,'--collect']]
        record = dict(context=identifier,groups=context['groups'],steps=[],status='pending')
        with (destination/f'{identifier}.log').open('x') as log:
            for command in commands:
                run = subprocess.run(command,cwd=ROOT,capture_output=True,text=True)
                output = run.stdout+run.stderr
                log.write('$ '+' '.join(command)+'\n'+output+f'\nEXIT={run.returncode}\n')
                log.flush()
                step = dict(command=command,exit_code=run.returncode)
                if command[0] == 'dotnet':
                    match = re.search(r'log: (artifacts/toolchain/runs/[^)]+/run\.log)',output)
                    if match:
                        source = ROOT/Path(match[1]).parent
                        target = destination/f'{identifier}-{source.name}'
                        shutil.copytree(source,target)
                        step['retained_toolchain_run'] = str(target)
                    step['result_passed'] = 'RESULT: PASSED' in output
                    step['warnings_zero'] = bool(re.search(r'Warnings:\s*0\b',output))
                record['steps'].append(step)
                print(f'{identifier}: {command[0]} {command[-1]} -> {run.returncode}',flush=True)
                if run.returncode or command[0] == 'dotnet' and not (step['result_passed'] and step['warnings_zero'] and 'retained_toolchain_run' in step):
                    record['status'] = 'failed'
                    print(output[-16000:],flush=True)
                    break
            else:
                record['status'] = 'compiled-warning-free-and-inputs-collected'
        results.append(record)
        (destination/'results.json').write_text(json.dumps(results,indent=2)+'\n')
    print(json.dumps(dict(attempt=args.attempt,compiled_contexts=sum(r['status']=='compiled-warning-free-and-inputs-collected' for r in results),
                          failed_contexts=sum(r['status']=='failed' for r in results),
                          results=str(destination/'results.json'),note='No host/test/device execution'),indent=2))
    raise SystemExit(1 if any(r['status']=='failed' for r in results) else 0)


if __name__ == '__main__':
    main()
