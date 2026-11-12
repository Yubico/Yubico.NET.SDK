# Independent Sol review: explicit original-author schema adapter

## PASS — bounded record-authority correction

Accept additive `validation/check-records-v2.py` at SHA-256
`ec0f7c5c10f980f95227de98898b8c4a34b6f5784777c0c835f725540c18a7aa` for the separately coordinated
record snapshot/check/staged gates. It resolves the demonstrated B4/B6 authority mismatch without
loosening original-source payload admission. No further correction or new framework is required.

This is not final record freeze, staged-byte acceptance, compiler/checker qualification or normative
endorsement of the retained standards. Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`;
observed metadata HEAD was `da66f5d01ba594993974ddd26723e8c300c0a454`. Later record/status commits and
active writers require their own fresh coordinated checkpoint, not a fixed-HEAD assumption.

## Source/provenance findings

- The adapter pins the unchanged original checker at
  `e115552ca79243091ad160bddb0667e4bb1dc9d2d1cd86800b4152ac8ab2f552`, verifies its actual Core
  `HERE`/`ROOT`/`RUN` context and rebinds only `primary_sources()` before calling its unchanged `load()`.
  It does not duplicate or rewrite classification, format checking, actual-clean/attributes,
  enumeration, snapshot/check or staged-set/object verification.
- Name/status schemas are enabled only for the two exact known authority paths. B4's captures list
  uses `name` and `accessed_utc`; B6 requires the `FINAL-BOUNDED-SOURCE-REGISTER` object and `sources`,
  `name`, `requested_utc`, ROOT-relative `retained_path` and `byte_exact_original=true`.
- Recognized path/name aliases must identify the registered target; B6's required retained path must
  identify that exact same original. Absolute, parent-traversing, escaping or symlinked paths fail.
  Missing, unknown and ambiguous matching author populations fail rather than silently selecting the
  first row or accepting an unsupported name/status schema at an arbitrary authority path.
- B4/B6 require **integer** HTTP status 200; string `"200"`, boolean and error statuses fail. Explicit
  `error`/`fetch_error` fields fail. Both registry and author byte counts must be genuine positive ints;
  URL, timestamp, hash and size agree exactly. Prior B1/C list-or-files and path/file/time mappings are
  retained, with explicit failure statuses rejected. Original media-type checks remain effective.
- Exact registration remains mandatory for all **eight originals: seven PDFs and one encoding-qualified
  HTML**. The original HTTPS, byte/hash/size, PDF suffix/magic, HTML declaration/encoding/response media
  type and format checks remain in force. There is no wildcard binary admission, Latin-1 fallback,
  transcoding or content inference from a filename. Unregistered PDF, executable and non-UTF8 inputs
  still fail. All originals must exist; missing/changed/symlinked sources cannot vanish from enumeration.
- The original checker, primary-source addendum and old failures/history are retained. The changed
  authority interpretation does not convert an SCP11 public-review edition into a final standard or
  expand applicability of any source citation.

## Fresh read-only verification

Inspected the verifier's process-local mutations and absence of writing operations, then ran:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/record-authority-correction/verify.py
```

Exit **0**, reproducing the old unique-author failure, admitting all **eight** actual registered sources
with raw SHA-256 equal to expected staged/actual Git-clean SHA-256 and rejecting **56** negative cases.
These include per-source URL/time/hash/size disagreement; invalid typed status; missing/wrong/escaping
retained paths; name mismatch; duplicate author; failed fetch; wrong response format; missing/changed/
symlinked originals; unknown authority schema; and ordinary unregistered PDF/runtime/non-UTF8 payloads.
They are read-only record-boundary checks, not SDK tests or a process qualification run.

Independent byte checks matched all six registry/authority provenance identities (five author manifests
plus the source registry), the additive adapter's submitted hash and the original checker pin. Qualified
foundation/logger/binder and checker-profile identities remain unchanged. The adapter is outside the
example preparation descriptor and checker compiler profile; it invalidates neither qualified logger
evidence nor checker builds/seals nor the accepted example source/wrapper/capture participants.

The reported scan of **1,990 files** with no classification blocker is explicitly a retained point-in-time
observation while writers were active. It is not a stable final population, a new full scan by this
reviewer, a manifest/freeze or staged-byte proof. The separate authorized example output-scope delta
has its own preserved predecessor and review; no silent repin is accepted through this record adapter.

## Final coordinator gate

After all relevant writers stop, choose a fresh unoccupied manifest path. Use the additive checker:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
python3 -B "$V/check-records-v2.py" snapshot --manifest "$V/final-record-manifest-first.json"
python3 -B "$V/check-records-v2.py" check --manifest "$V/final-record-manifest-first.json"
# Coordinator stages only the exact reviewed run paths and manifest.
python3 -B "$V/check-records-v2.py" staged --manifest "$V/final-record-manifest-first.json"
```

The shown name is usable only if still fresh; retain earlier manifests and failures. Require the exact
complete run population, actual attributes/clean identities and staged set/object/bytes to pass. Later
source/attribute/population changes require a fresh checkpoint. No final gate above was executed here.
No checker rebuild, logger requalification or example body change follows from this correction.

## Reviewed identities and limits

| File, relative to `validation/` | SHA-256 |
| --- | --- |
| `check-records-v2.py` | `ec0f7c5c10f980f95227de98898b8c4a34b6f5784777c0c835f725540c18a7aa` |
| `check-records.py` | `e115552ca79243091ad160bddb0667e4bb1dc9d2d1cd86800b4152ac8ab2f552` |
| `record-authority-correction/handoff.md` | `2feca4969b08adbd1172424d0d7cd8a43f4e5518f84edcadeaaab51aa1381f8d` |
| `record-authority-correction/provenance.json` | `c4085fccd4daaf38ede2ac4c9c02a0a5d3486f67f54b4a82c8085414b111668d` |

Only this review and the separately assigned example output-scope review are written. No SDK build/test,
compiler capture/replay, compiled checker, native/device operation, formatter, Git mutation or subagent
ran. Frozen archives and other worktrees were not written. PIV physical deferral, Windows handoff and
remaining Core Pass-1 owners/gates retain their existing scope.
