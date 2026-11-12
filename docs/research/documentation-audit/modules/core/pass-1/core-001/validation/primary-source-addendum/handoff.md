# Registered original source formats

**Prepared for independent record-tool review.** This addendum is separate from checker compilation.
`check-records.py` is absent from the checker input profile, so this record-classifier correction
requires neither a checker rebuild nor logger requalification. Its original UTF-8-only version and
the intermediate PDF-only version are retained as source snapshots here.

The coordinator's `sources/primary-binaries.json` registers four exact originals: three PDFs and one
ISO-8859-1 HTML metadata page. The loader requires safe exact run-relative paths, unique registrations,
original-source kinds, HTTPS URLs, exact length/SHA-256 and matching successful author-fetch metadata.
Missing originals fail before enumeration. It checks the current B1 `path/access_utc/role` and C
`file/requested_at_utc/content_type` or successful-fetch schemas without changing those source records.

- **PDF:** registration kind `original-specification-pdf`, `.pdf` suffix, exact bytes and `%PDF-` magic.
- **Encoded HTML:** registration kind `original-source-html`, explicit `encoding=iso-8859-1`, exact
  byte identity, original response media type `text/html`, HTML markup and the actual Content-Type META
  declaration matching the registry's encoding evidence. This does not grant a Latin-1 fallback to
  unregistered files.

The ordinary record path retains UTF-8, binary magic, direct runtime/cache suffix and generated-directory
guards. The original source-attribute/Git-clean/index algorithms remain in place. Only the exact registered
source population bypasses UTF-8/NUL text interpretation. Symlinks and escaping paths fail; source kind,
format or identity changes fail. Both supported source kinds require `text=unset` and byte-exact actual
Git-clean identity. Their normalization field explicitly records `byte-exact-original-source`; HTML is
never silently transcoded and PDF internal bytes are never newline-normalized.

The coordinator's run-local `.gitattributes` preserves research sources and records with
`* -text -whitespace`. Current record manifests capture those actual attributes. This scope change leaves
shipping source, previous frozen archives and checker-profile input bytes unchanged. The source registry,
author manifests and this classifier are record-proof participants; they are outside the checker compiler
profile. Future originals require an explicit registry entry and matching source-role/format evidence.

`verify.py` checks all four real source identities and their actual Git-clean hashes, plus independent
negative cases for changed bytes/length/hash, unregistered paths, invalid kind/URL/format, escaping or
symlinked paths, missing originals, wrong HTML encoding/evidence, unregistered non-UTF8 HTML and wrong
original response media type. These are pure/read-only record-boundary checks, not SDK tests. Actual
staged-byte verification remains a coordinator checkpoint after all writers stop and explicit staging.

Run the bounded check from the checkout root:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/primary-source-addendum/verify.py
```

Final complete-run snapshot/check/staged commands stay those in `validation-preparation.md`, using a
fresh manifest. Earlier proof/manifests retain their original scope and attributes; this addendum does
not rewrite them or claim a final archive freeze.
