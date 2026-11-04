"""Reconcile retained six-host builds and immutable originals without any .NET execution."""
import hashlib
import json
from pathlib import Path
import runpy

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(p for p in HERE.parents if (p/'toolchain.cs').is_file())


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    runpy.run_path(str(HERE/'verify-inputs.py'))['main']()
    current = json.loads((HERE/'manifest.json').read_text())
    previous = json.loads((HERE/'failure-using-hoist/validation/compiled-examples/manifest.json').read_text())
    assert [(b['id'],b['extracted_text'],b['extracted_sha256'],b['verbatim_text']) for b in previous['blocks']] == [(b['id'],b['extracted_text'],b['extracted_sha256'],b['verbatim_text']) for b in current['blocks']]
    old = {g['name']:g for g in previous['groups']}
    assert all(old[g['name']]['raw_sha256'] == g['raw_sha256'] for g in current['groups'])
    assert [g['name'] for g in current['groups'] if old[g['name']]['source_sha256'] != g['source_sha256']] == ['guide-protocol']
    assert not list((HERE/'failure-using-hoist').rglob('*.csproj'))
    assert not list((HERE/'failure-xunit-package-content').rglob('*.csproj'))
    retention = runpy.run_path(str(HERE/'retention.py'))
    retention_summary = retention['verify']()
    overlay = json.loads((HERE/'retention-overlay.json').read_text())
    for archive_file in [HERE/'failure-using-hoist/archive-manifest.json', HERE/'failure-using-hoist/workspace-manifest.json', HERE/'failure-xunit-package-content/archive-manifest.json']:
        archive = json.loads(archive_file.read_text())
        for entry in archive['files']:
            resolved = retention['resolve'](Path(entry['archived']),overlay)
            assert sha(resolved) == entry['sha256']
    results = json.loads((HERE/'context-runs/after-using-hoist/results.json').read_text())
    final = json.loads((HERE/'context-runs/final-result.json').read_text())
    assert final['compiled_contexts'] == 6 and final['compiled_groups'] == 33 and final['compiler_analyzer_warnings'] == 0
    final_rows = {r['context']:r for r in final['results']}
    assert len(results) == 6 and sum(len(r['groups']) for r in results) == 33
    summary = []
    for record in results:
        build = next(s for s in record['steps'] if s['command'][0] == 'dotnet')
        assert build['exit_code'] == 0 and build['result_passed'] and build['warnings_zero']
        log = Path(build['retained_toolchain_run'])/'run.log'
        result_file = log.parent/'result.json'
        actual = json.loads(result_file.read_text())
        assert actual['status'] == 'passed' and not actual['warnings']
        context = RUN/'validation/checker-source/context'/record['context']
        evidence_path = context/'evidence/resolved-inputs.json'
        evidence = json.loads(evidence_path.read_text())
        assert final_rows[record['context']]['evidence_sha256'] == sha(evidence_path)
        assert evidence['prepared_manifest_sha256'] == sha(context/'context-manifest.json')
        snapshot = json.loads((context/'context-manifest.json').read_text())
        for entry in snapshot['inputs']:
            assert sha(Path(entry['path'])) == entry['sha256']
        for values in evidence['actual_compiler_inputs'].values():
            for entry in values:
                assert sha(Path(entry['path'])) == entry['sha256']
        for entry in evidence['captured_files']:
            assert sha(Path(entry['path'])) == entry['sha256']
        assert evidence['policy']['TreatWarningsAsErrors'] == 'true'
        assert evidence['policy']['RunAnalyzersDuringBuild'] == 'true'
        if record['context'] == 'unit-v3':
            assert evidence['package_content_proof']['exact_package_bytes_match']
            assert evidence['collector_source_sha256'] == sha(HERE/'collect-unit-package-source.py')
        summary.append(dict(context=record['context'],groups=len(record['groups']),build='PASSED',warnings=0,
                            input_collection='PASSED',retained_toolchain_run=str(log.parent),
                            evidence=str(evidence_path),evidence_sha256=sha(evidence_path)))
    print(json.dumps(dict(status='Six clean compile-only hosts PASSED/Warnings0; six input/policy collections verified',
                          groups=33,original_blocks_unchanged=95,readable_raw_unchanged=33,
                          changed_wrapper_only='guide-protocol',results=summary,retention=retention_summary,no_host_test_device_execution=True),indent=2))


if __name__ == '__main__':
    main()
