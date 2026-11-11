# Checkpoint verification scope

This verifies record structure and source binding, not audit completion, C# compilation, API binding,
or runtime behavior. The final JSON counts are in `checkpoint-verification.json`.

`python3 -B investigation/A/record_checkpoint.py`, using the full run path from the repository root,
checks the exact packet populations, disjoint IDs, existence in the canonical semantic inventory, and
current selected-file bytes against the pinned Git blobs with explicit CRLF/LF normalization. It
emits a keyed result for every primary/shared ID and leaves unfinished shared reads `UNREAD`.
Candidate associations use the investigator's cited ranges; hashes never infer a semantic read.
The source hashes retain both original Git bytes and current checkout raw bytes.

The first invocation failed before writing output because the remaining-shared list comprehension's
closing square bracket was missing (`record_checkpoint.py`, initial line 301). The `SyntaxError` was
corrected; this was a record-tool preparation failure, not an SDK/compiler/test result. The subsequent
successful invocation reported 1,630 primary and 376 shared IDs, with 164 shared IDs still unread.
No original failure has been reclassified as a passing SDK gate.

The source-capture script fetched five bounded originals, writing only new `investigation/A/sources/`
files. Captures contain access time, exact URL, content hash, HTTP status, and Last-Modified metadata.
The Python comparator was reused from C's pinned local source without imports or a duplicate fetch.

A local reference-name glob surfaced filenames in unrelated worktrees. Their source contents were
neither opened nor used; no Windows handoff work or other worker path was modified. A later attempted
report patch failed its expected-context check and made no change; the correction was applied with a
narrower exact patch. These are preparation observations, not SDK verification outcomes.

The investigator continued through all 164 remaining shared IDs after that checkpoint, reading complete
assigned sections/methods and enclosing relevant helper context. The successor record emits an empty
`remaining-shared.json`; its exact final counts are in the adjacent JSON. Original Rust/platform and
version-specific xUnit questions remain explicit source gaps rather than unread units. Local candidate
records are not accepted defects or additive inherited bug counts. Reconcile inherited provenance and
boundary questions and assign fresh independent review. Compiler, runtime, native, physical, review,
sampling, and publication dimensions remain pending.

## Final structural verification

The final record generation and a separate read-only Python verification both exited 0. They checked:

- 2,006 exact unique IDs: 1,630 primary and 376 shared; zero unread units.
- All 91 primary-file dispositions and all assigned shared-range dispositions are present.
- Primary: 1,501 read-clean / 129 candidate-associated units; shared: 354 read-clean / 22 candidate-associated.
- 21 distinct packet-local candidates, zero accepted allocations, with in-range original-source citations.
- Current selected-file content matches the pinned Git source after recorded CRLF/LF normalization.
- Five retained original captures and both reused Python-source hashes match their recorded SHA-256 values.
- Proposed sampling nominees still have read-clean unit dispositions; no independent sample was performed.

The second check printed: `PASS: 2006 exact unique read-unit IDs; 91 primary-file dispositions; 21 unique
local candidates; citations in range; 5 original captures and 2 reused Python hashes match; sampling
nominees remain clean; zero unread units.` This remains record/source validation only.
