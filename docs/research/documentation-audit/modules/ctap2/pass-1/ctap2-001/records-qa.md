# CTAP2 final record and staging QA

Coordinator checks supplement review-closeout.md and its accepted CR-R1 follow-up:

- All165 final authored content hashes and five generated-output hashes reconcile with independently
  accepted source snapshots and explicit formatter/EOL transformations. All183 module path dispositions
  and seven edited external dependencies have authoritative final pointers.
- Current run/campaign/changed-document Markdown links resolve and fences balance. One old framework
  project link was repaired after preserving the original report bytes and original-path hash mapping.
  Historical snapshot navigation/execution commands remain provenance, not current rerun instructions.
- All784 immutable OATH/WebAuthn archive hashes and402 Management run artifact hashes match. No earlier
  published archive/run file was edited. Current handoff updates are deliberately outside those snapshots.
- Current retention overlay accounts for every original failure record. Generated runtime/cache copies
  are excluded with exact paths, sizes, hashes and reasons; all meaningful text/source/compiler records
  remain. One pinned xUnit nupkg is retained as an explicit offline package-provenance proof exception.
- Validation/evidence/patch attributes preserve exact bytes and historical whitespace. Staged bytes are
  compared with the run manifest rather than relying only on working-tree hashes.
- Only explicitly enumerated current-session paths are staged. Original untracked audit-v2 closeout,
  execution, Jira-traceability files and mise.toml remain outside the commit. Package/package-lock and
  SDK analyzer policy remain unchanged; node_modules and temporary build/browser outputs are not staged.
- Source/content checks include git diff whitespace, exact changed-path ownership, no private-key/GitHub
  credential patterns in retained text, declared binary-package exception, and final clean tracked status
  after publication. The actual generated diagram PNGs are content outputs, separately hashed.

`record-manifest.json` lists every retained run artifact except itself with byte length/SHA256 and kind,
and links the explicit generated-artifact exclusion overlay. It is generated after final record updates
and verified against both working-tree and index bytes before commit. Later source/remote publication
details are recorded in the campaign CTAP2 handoff, outside this immutable run snapshot.

Publication steps: explicit git add paths; git diff --cached --check; commit reviewed content/records;
non-force git push origin yubikit-docs-audit; git ls-remote origin refs/heads/yubikit-docs-audit equal to
local HEAD. Publication is not inferred from review or a local commit; its actual outcome belongs in the
handoff after those commands finish.
