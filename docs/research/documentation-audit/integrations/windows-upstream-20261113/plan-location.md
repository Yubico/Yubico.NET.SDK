# Integration plan locations

The editable plan is `docs/plans/2026-11-13-windows-refactor-integration.md`, the repository's standard
planning location. That directory is ignored by Git. A byte-exact preparation snapshot is retained in
`planning/2026-11-13-windows-refactor-integration.md.snapshot` so the received plan can accompany the
integration records when they are committed.

Relative links inside the snapshot retain the original plan's `docs/plans/` base. The original plan's
SHA-256 is recorded in `preparation-verification.json`. The subsequent validation-scope clarification
is retained as `planning/2026-11-13-windows-refactor-integration-bounded-validation.md.snapshot` with a
separate revision receipt. The source merge and its validation remain unexecuted.
