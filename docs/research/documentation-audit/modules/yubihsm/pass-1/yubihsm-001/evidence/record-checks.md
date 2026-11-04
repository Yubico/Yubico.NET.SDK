# Investigator record checks

2026-11-04, Python standard-library read-only reconciliation (not product build/tests):

```text
coverage paths 56 missing [] extra []
baseline hash matches 56 / 56
API non-directive 83
clean units 20 {'api': 8, 'example': 6, 'internal-test': 6}
```

Compared `coverage.json.files` to `git ls-files src/YubiHsm`, current file SHA256 values to RUN
`inventory.json`, and nonempty nondirective PublicAPI.Unshipped entries. JSON parsed successfully.
Baseline module bytes were unchanged at this checkpoint. The 20 units are investigator nominations;
coordinator removes editorial overlaps before final sampling. No runtime or hardware result is implied.
