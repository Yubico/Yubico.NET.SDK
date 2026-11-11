"""Read-only owning-project/default-glob check; actual Csc membership remains a later gate."""
import fnmatch
import json
from pathlib import Path
import xml.etree.ElementTree as ET
import prepare

ROOT, HERE = prepare.ROOT, prepare.HERE


def main():
    scope = HERE.parent.parent / 'evidence/format-scope.txt'
    names = scope.read_text().splitlines()
    prepare.require(len(names) == len(set(names)) == 20, 'Accepted formatter scope changed')
    rows = []
    for name in names:
        source = ROOT / name
        candidates = []
        for parent in source.parents:
            candidates = list(parent.glob('*.csproj'))
            if candidates or parent == ROOT:
                break
        prepare.require(len(candidates) == 1, 'Formatted source has no unique owning project: ' + name)
        project = candidates[0]
        relative = str(source.relative_to(project.parent))
        tree = ET.parse(project)
        defaults = [node.text for node in tree.iter('EnableDefaultCompileItems')]
        prepare.require('false' not in defaults, 'Explicit compile-item context needs separate review')
        removals = [node.attrib['Remove'].replace('\\', '/') for node in tree.iter('Compile') if 'Remove' in node.attrib]
        prepare.require(not any(fnmatch.fnmatch(relative, glob) for glob in removals), 'Formatted file excluded by its project')
        prepare.require(not any(part in ('bin', 'obj') for part in Path(relative).parts), 'Generated path in product format scope')
        rows.append(dict(source=name, project=str(project.relative_to(ROOT)),
                         source_identity=prepare.identity(source), project_identity=prepare.identity(project),
                         admission='SDK default Compile glob; no matching project removal. Actual Csc check pending.'))
    print(json.dumps(dict(status='static owning-project/default-glob reconciliation only',
                          scope=prepare.identity(scope), files=rows), indent=2))


if __name__ == '__main__':
    main()
