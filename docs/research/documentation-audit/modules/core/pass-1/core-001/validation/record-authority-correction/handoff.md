# Explicit author-schema normalization

**Ready for bounded independent review before final record freeze.** The additive
`validation/check-records-v2.py` resolves the demonstrated B4/B6 author-schema prerequisite. It leaves
the original checker, primary-source addendum, pilot files and qualified compiler/logger participants
unchanged. No SDK command, compiler/checker/native/device execution or Git mutation ran.

## Exact change and retained boundary

The adapter pins original `check-records.py` SHA-256
`e115552ca79243091ad160bddb0667e4bb1dc9d2d1cd86800b4152ac8ab2f552`, loads it in its actual Core
context, and rebinds **only `primary_sources()`** before calling its unchanged `load()` implementation.
It reuses the original classification, exact registered PDF/HTML format checks, attributes, raw/Git-clean
identity, enumeration, snapshot/check and staged-set/object verification functions. Original source code
is neither rewritten nor copied into a replacement framework.

The adapter recognizes these original-author forms:

| Authority | Population/path/time/success evidence |
| --- | --- |
| Existing B1/C authorities | Preserve list or `files`, `path`/`file`, `access_utc`/`requested_at_utc`, original-source role/response type or typed `FETCHED…` status. Any explicit failed status or error rejects. |
| Exact `investigation/B4/sources/captures.json` | List; `name` relative to the authority directory; `accessed_utc`; **integer** HTTP status 200. |
| Exact `investigation/B6/sources/final-manifest.json` | `FINAL-BOUNDED-SOURCE-REGISTER` with `sources`; `name`; `requested_utc`; **integer** status 200; `byte_exact_original=true`; required ROOT-relative `retained_path`. |

Name/status forms are recognized only at those two actual author paths. A string `"200"`, boolean,
HTTP error, failed-fetch field, derivative or arbitrary object supplies no success proof. Each source
requires one unique matching author. URL, timestamp, integer byte count and hash must match exactly.
All name/path aliases and any retained path must resolve to the same actual source. Retained paths use
repository ROOT, while names use the authority's parent. Absolute/escaping/symlinked paths reject.

The registry remains mandatory: all **eight** originals (**seven PDFs and one encoding-qualified HTML**)
must exist with their registered bytes and original format. Unregistered PDF, executable and non-UTF8
HTML remain rejected. The adapter changes no source bytes or names and performs no transcoding.

`provenance.json` records the exact current registry and five author-manifest identities. Those records
remain owner-controlled inputs; a future new schema requires its own explicit review. Existing public-review
versus final-standard qualifications remain unchanged by archive admission.

## Read-only evidence

- `original-failure.stderr.txt` preserves a fresh reproduction of the old unique-author rejection.
- `verify.py` admits all eight actual sources with byte-exact actual Git-clean hashes and checks bounded
  process-local negative mutations. These include URL/time/hash/size disagreement for every source,
  typed status failures, missing/wrong/escaping retained paths, name mismatch, ambiguous authors,
  failed fetches, wrong format, missing/modified/symlinked originals and unregistered binary inputs.
- `current-run-scan.json` inspected **1,990 current files**, all accepted: **1,397 UTF-8 text**, **585 JSON**,
  **seven PDFs**, **one original HTML**. It found **no additional classification blocker**. Writers were
  active; this observation is neither a manifest/freeze nor staged-byte acceptance.
- `preservation-before.json` pins the unchanged original checker and all 145 supplied pilot files.
  Final verification checks those identities again. The later independently authorized EX-R1 scope
  correction, if applied, receives its own preserved predecessor/delta rather than silently repinning
  the pilot here.

## Coordinator commands after review and writer release

For bounded reproduction now:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
python3 -B "$V/record-authority-correction/verify.py"
```

After all writers finish, choose a **fresh** manifest and use the additive checker:

```bash
python3 -B "$V/check-records-v2.py" snapshot --manifest "$V/final-record-manifest-first.json"
python3 -B "$V/check-records-v2.py" check --manifest "$V/final-record-manifest-first.json"
# Coordinator stages only the exact reviewed run paths and manifest.
python3 -B "$V/check-records-v2.py" staged --manifest "$V/final-record-manifest-first.json"
```

These final operations were prepared, not run. Preserve earlier failures and manifests. A later source,
attribute or population change requires a fresh checkpoint, not a relabelled previous result. No checker
DLL rebuild or logger requalification follows from this record-only correction.
