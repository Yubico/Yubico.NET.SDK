# Archive packaging correction — additive representation-only handoff

The original RUN path `editing/format-final/source-snapshots.tar.gz` contained a
generated gzip archive, not a registered original PDF/HTML source. It therefore
does not qualify for the unchanged text/original-source record-admission policy.
This addendum corrects packaging only; it does not change that policy or the EOF
proposal, and requires review separately from the EOF patch.

## Retention and relocation

The exact original gzip bytes are retained externally at:

```text
/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/format-final-reconciliation/source-snapshots.tar.gz
```

Original archive SHA-256:
`0f2bbee75e18f18e6ac46262c18a5e93fc3d01bef0c81080ab0ef5e9c774db14`
(931644 bytes). The prior RUN gzip path is removed **only after** verification of
the exact external copy and all entries in the replacement text bundle.

`source-snapshots.json` is the Git-backable replacement: 239 path records, each with
literal before/after UTF-8 source strings, byte counts, and SHA-256 values. Decoding
uses strict UTF-8, not UTF-8-with-BOM stripping; encoding each parsed string as
UTF-8 reproduces the original bytes, including terminal-newline presence/absence.
This is source text, not base64 or another binary encoding.

`archive-packaging-correction.json` records the text-bundle identity, explicit
prior-RUN-to-external gzip mapping, 478 archive-entry-to-JSON-pointer identity
mappings, and the unchanged predecessor identities. Every text entry is compared
byte-for-byte against the gzip archive, its isolated runtime source copy, and the
original `eof-identities.json` before the RUN gzip is removed.

## Predecessor preservation and admission bounds

The original `report.md`, `preparation-receipt.json`, `verification-receipt.json`,
all other predecessor text records, and `eof-only.patch` remain byte-identical.
Their archive references and hashes describe the original representation; resolve
the former RUN archive path through the relocation mapping above. Do not rewrite
those predecessor claims as though the JSON bundle existed at their capture time.
`prepare-evidence.py` is preserved as the historical preparation record; do not
rerun it to recreate the inadmissible RUN gzip.

The correction receipt includes a bounded static admission check of **every file
in `editing/format-final/`** using the exact, unchanged `classify` function from
`validation/check-records.py`, which `check-records-v2.py` retains. This checks text
encoding, content, filename/path restrictions, and JSON parsing without invoking
Git or the SDK. It is not the full repository record/index/staged admission gate;
that remains for coordinator execution through the existing qualified checker.
No checker, format-policy code, source registry, primary source, product file, or
EOF patch is modified. No SDK, Git, native/device, or subagent execution is used.
