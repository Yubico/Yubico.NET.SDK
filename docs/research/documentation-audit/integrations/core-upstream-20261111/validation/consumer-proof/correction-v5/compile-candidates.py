"""Describe potential selected-target compile candidates without inventing a consume-all invariant."""
from pathlib import Path


def compile_candidate_dispositions(project, raw, expected, actual, packages, retain):
    if not actual <= expected:
        raise ValueError('Actual Csc package reference lacks a selected-target compile asset')
    metadata = {}
    for line in (raw / 'references.txt').read_text(encoding='utf-8-sig').splitlines():
        if not line.strip():
            continue
        fields = line.strip().split('|')
        path = str((project.parent / fields[0].strip('"').replace('\\', '/')).resolve())
        if path in metadata:
            raise ValueError('Duplicate evaluated reference identity in candidate disposition')
        metadata[path] = dict(raw_line=line, reference_source_target=fields[2], fusion_name=fields[3])
    rows = []
    for value in sorted(expected):
        path = Path(value)
        owners = [row for row in packages if row['root'] and path.is_relative_to(Path(row['root'])) and
                  str(path.relative_to(Path(row['root']))) in (row['selected_target_node'] or {}).get('compile', {})]
        if len(owners) != 1:
            raise ValueError('Selected compile candidate lacks a unique declared package/target owner')
        declared = dict(package_id=owners[0]['package_id'], asset=str(path.relative_to(Path(owners[0]['root']))),
                        package_content_sha512=owners[0]['sha512'])
        if value in actual:
            rows.append(dict(declared, path=value, disposition='actually selected Csc package reference'))
            continue
        candidate = retain.keep(path, 'unconsumed-restored-compile-candidate', True) if path.is_file() else None
        alternatives = []
        for selected, info in metadata.items():
            if Path(selected).name != path.name:
                continue
            selected_record = retain.keep(Path(selected), 'actual-selected-candidate-alternative', True)
            alternatives.append(dict(reference=selected_record, metadata=info,
                                     byte_identical=candidate is not None and (candidate['bytes'], candidate['sha256']) ==
                                                    (selected_record['bytes'], selected_record['sha256'])))
        rows.append(dict(declared, path=value, disposition='potential restored compile asset not consumed by evaluated ReferencePathWithRefAssemblies/Csc',
                         candidate=candidate, same_filename_actual_references=alternatives,
                         materialization='current file identity retained' if candidate else 'unmaterialized declaration; not an actual compiler input',
                         reason=('byte-identical alternative path to an actual selected reference; exact resolver tie-break not recorded'
                                 if any(row['byte_identical'] for row in alternatives) else
                                 'unconsumed in actual evaluated references; exact filtering/conflict reason not established')))
    return dict(potential_compile_count=len(expected), actual_package_reference_count=len(actual),
                unconsumed_compile_count=len(expected - actual), candidates=rows,
                boundary='Actual item/Csc equality and per-input ownership remain strict. Restore candidates are not all required compiler inputs.')
