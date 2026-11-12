# Static verification scope

Run `python3 docs/research/documentation-audit/modules/core/pass-1/core-001/editing/PGP-boundary/record-edit.py`
from this isolated worktree. The script records this patch; it does not compile or invoke SDK code.

- Every modified C# line is a standalone `//`/`///` comment. Every other complete source line is
  compared exactly, including strings, imports, directives, declarations, defaults, attributes,
  assertions and inline-comment-bearing executable lines. Missing EOF newlines added by the
  comment-edit tool are recorded separately and supply no token change.
- All 93 OpenPGP paths have decisions and hashes. The primary's raw source identities match the
  completed file ledger; explicit BOM/CRLF normalization reconciles Git checkout representations.
  Retained paths record their unchanged local raw identity separately from primary input identity.
- XML documentation blocks are checked for well-formed XML, not compiler binding or generated-doc
  resolution. Markdown relative targets in changed guides are checked for local existence, not
  remote reachability or anchor semantics. `git diff --check` checks patch whitespace.
- All seventeen Markdown example bodies are unchanged. Two additional existing XML protocol
  fragments in Kdf and PrivateKeyTemplate are separately inventoried. The Markdown inventory keeps
  complete top-level, composable typed-method, protocol fragment and run/install command classes.
  No new C# body or fake helper was introduced. Historical excluded examples stay in the archives.
  No new compilation, warnings/analyzer, consumer or runtime proof is claimed.

The first two record-export attempts stopped on Git-versus-working-tree raw-byte comparisons of
unchanged project files. The script was corrected to distinguish checkout BOM/CRLF identity from
normalized pinned content; those files were not edited. Later successful runs cover the full
listed scope. This is an editor-side preservation/XML/link check, not independent content/citation
review, an archive-wide validator or evidence of behavior fixes.

No build, SDK test, formatter, cryptographic/provider/native/device operation, Git staging/ref/commit/
push, subagent, primary-checkout write or other-worktree write is part of this assignment.
