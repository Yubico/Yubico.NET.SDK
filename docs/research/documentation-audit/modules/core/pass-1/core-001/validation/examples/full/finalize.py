"""Bind the actual finite full-example inputs after the accepted external transition; no build."""
import importlib.util
import json
from pathlib import Path
import re
import sys

HERE=Path(__file__).resolve().parent
V=HERE.parents[1]
RUN=HERE.parents[2]
sys.path.insert(0,str(V))
from foundation import ROOT,BASELINE,git,require,sha


def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module


def main():
    driver=load('fixed_full_example_driver',HERE/'run.py')
    k=driver.capture.kernel(HERE/'host/src/Yubico.YubiKit.Audit20261111.DocExamples.Full.csproj')
    manifest=json.loads((HERE/'manifest-current.json').read_bytes())
    plan=json.loads((HERE/'compiled-contexts.json').read_bytes())
    products_path=RUN/'editing/integration/current-products.json'
    products=json.loads(products_path.read_bytes())
    accepted={row['path']:row['sha256'] for row in products['product_files']}
    require(accepted and len(accepted)==len(products['product_files']) and
            all(sha((ROOT/path).read_bytes())==digest for path,digest in accepted.items()),'Current accepted product map membership/bytes differ')
    # Reconcile the old fixed toolchain/root-policy participants, admitting only the three reviewed comment edits.
    old=json.loads((HERE.parent/'output-scope-correction/preparation-identities.json.snapshot').read_bytes())
    controls=[row for row in old['files'] if row['path']=='toolchain.cs' or row['path'].startswith('scripts/toolchain/') or
              row['path'] in ('global.json','Directory.Build.props','Directory.Build.targets','Directory.Packages.props','.editorconfig','nuget.config')]
    transition=json.loads((V/'checker-binding/final-inputs/three-input-transition.json').read_bytes())
    permitted={row['path']:row for row in transition['deltas']}
    control_deltas=[]
    for row in controls:
        current=sha((ROOT/row['path']).read_bytes())
        if current!=row['sha256']:
            require(row['path'] in permitted and permitted[row['path']]['old']['sha256']==row['sha256'] and
                    permitted[row['path']]['current']['sha256']==current==accepted[row['path']], 'Unapproved toolchain/control repin')
            control_deltas.append(row['path'])
    require(set(control_deltas)==set(permitted),'Expected exactly the three accepted toolchain comment changes')
    files={ROOT/row['path'] for row in controls}
    names=('run.py','capture.py','capture.targets','Directory.Build.props','manifest-current.json','compiled-contexts.json',
           'qualification-decisions.json','verify.py','contexts.py')
    files.update(HERE/name for name in names)
    files.update(V/name for name in ('foundation.py','run-command.py','checker-binding/bind.py'))
    files.add(HERE.parent/'capture.py')
    files.update([products_path,RUN/'editing/integration/accepted-external.json',V/'checker-binding/reviewed-inputs-final-edits.json'])
    compile_sources=set()
    all_projects=set()
    for row in plan['contexts']:
        project=ROOT/row['project'];injections=[str(HERE/path) for path in row['injections']]
        compile_sources.update(driver.capture.expected_sources(project,injections))
        all_projects.update(driver.project_closure(project))
    for project in all_projects:
        files.add(project)
        for path in project.parent.rglob('*.cs'):
            if not {'bin','obj'}.intersection(path.relative_to(project.parent).parts):compile_sources.add(str(path))
    files.update(Path(path) for path in compile_sources)
    # Actual source/authority inputs for each documentation body; no global future-helper glob.
    for row in manifest['records']:
        files.update(ROOT/row[key]['path'] for key in ('authority','review') if key in row)
        if 'document' in row:files.add(ROOT/row['path'])
    for row in manifest['pilot'].values():
        if isinstance(row,dict) and 'path' in row:files.add(ROOT/row['path'])
    packages=Path('/home/rupicola/.nuget/packages')
    files.update([packages/'xunit.v3.core/3.0.0/buildTransitive/xunit.v3.core.targets',
                  packages/'xunit.v3.core/3.0.0/_content/tasks/netcore/xunit.v3.msbuildtasks.dll',
                  packages/'microsoft.testing.platform.msbuild/1.7.3/buildTransitive/Microsoft.Testing.Platform.MSBuild.targets',
                  packages/'microsoft.testing.platform.msbuild/1.7.3/buildMultiTargeting/Microsoft.Testing.Platform.MSBuild.targets',
                  packages/'microsoft.testing.platform.msbuild/1.7.3/_MSBuildTasks/netstandard2.0/Microsoft.Testing.Platform.MSBuild.dll'])
    files.update(HERE/'application-dependencies'/name for name in ('selection.json','serilog.extensions.hosting-10.0.0.nuspec.snapshot',
                'serilog.sinks.console-6.1.1.nuspec.snapshot','serilog.sinks.file-7.0.0.nuspec.snapshot'))
    checker=json.loads((V/'checker-binding/reviewed-inputs-final-edits.json').read_bytes())
    # Reuse actual selected SDK/compiler/runtime identities, without copying their binaries into records.
    files.update(Path(row['path']) for row in checker['inputs'] if not Path(row['path']).is_relative_to(ROOT))
    rows=[k.identity(path) for path in sorted(files)]
    result=dict(status='prepared-final-inputs',source_baseline=BASELINE,metadata_head=git('rev-parse','HEAD').decode().strip(),
                files=rows,compile_sources=sorted(compile_sources),projects=[str(path) for path in sorted(all_projects)],
                accepted_product_map=k.identity(products_path),control_deltas=control_deltas,
                accepted_product_paths=len(accepted),
                counts=manifest['counts'],release='Pending independent full-preparation/qualification review and explicit coordinator execution. This status binds accepted source inputs, not a compiler pass.')
    k.write(HERE/'reviewed-inputs-current.json',result)
    print(json.dumps(k.identity(HERE/'reviewed-inputs-current.json'),indent=2))


if __name__=='__main__':main()
