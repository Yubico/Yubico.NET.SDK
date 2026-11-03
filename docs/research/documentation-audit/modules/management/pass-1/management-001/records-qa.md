# Final record and staging checks

Coordinator checks on 2026-11-03 supplement the independent closeout/evidence review:

- All 50 final source/documentation content files match accepted review hashes, or have verified
  whitespace-only post-review formatting; four late changes have fresh explicit review hashes.
- Every current run/handoff local Markdown link resolves and fenced blocks balance. Changed external
  Markdown targets were checked too; the single erroneous extra-parent test-guide link was restored
  and independently re-reviewed. Historical archived navigation is preserved, not silently rewritten.
- All retained run artifacts are UTF-8 text. No generated bin, obj or Python-cache directory is retained.
  Helper/host generated outputs remain under `/tmp/opencode`; real compiler input identities are hashed.
- Validation inputs/projects/logs and source patches use byte-preserving Git attributes. This preserves
  exact hash evidence and original extracted snippet EOFs; active source/documentation formatting policy
  remains unchanged. Indexed validation bytes were compared with actual working-tree bytes.
- Explicit staging excludes the four original untracked user files. Staged whitespace checks pass;
  historical patches/raw input whitespace is evidence, not reformatted shipping source.
- No private-key block or GitHub token pattern was found in the explicitly staged text artifacts.
- The immutable OATH/WebAuthn archive has no changed paths; independent final review verified all 784
  archived hashes. New records do not rewrite historical reports or imply their old statuses are current.

The run's `record-manifest.json` lists every retained run artifact except itself, with byte length and
SHA-256. It is generated after final record updates and checked against both working-tree and index
bytes before committing. Current source/remote checkpoints are recorded in the campaign's Management
handoff after commit/push; that later publication entry is outside this immutable run snapshot manifest.

Required final commands/checks: `git diff --cached --check`; exact staged-path inventory; all manifest
byte/hash assertions; `git status --short --branch`; commit with explicit owned paths; non-force
`git push origin yubikit-docs-audit`; `git ls-remote origin refs/heads/yubikit-docs-audit` matching local HEAD.
Publication results belong to the handoff rather than being claimed before execution.
