# Bounded publication evidence rereview

**Ready for publication at the reviewed checkpoint. The Important omitted-log blocker is closed;
no Critical or Important blocker remains from this review.**

This is a bounded closure of [the predecessor review](final-publication-review.md), not a second
source/documentation audit or a new software/platform acceptance campaign. Its accepted factual,
complexity, PIV and platform qualifications carry forward unchanged.

## Exact checkpoint and reviewer

- Reviewer: fresh independent GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), continuing the independent
  final publication review on 2026-11-13; separate from the execution lead.
- Worktree: `/home/rupicola/Code/y/yubikit-windows-audit-integration-20261113`.
- Branch: `yubikit-windows-audit-integration-20261113`.
- Reviewed staged tree: `3a789e598180e5f3011e2fd4d0bd05878224fc70`.
- Predecessor reviewed tree: `7e8b4b40111b65231470a06207cf9700f12f7015`.
- Audited `HEAD`: `d5d01ef99f1478e4d49911194c17667b2450d2fd`.
- Incoming `MERGE_HEAD`: `e322712d609aaf03b9bd1cf2964a99326728866a`.
- Refreshed manifest blob: `5021900401ae1aeb56a2952b6bb2cfd38aef4414`.
- Refreshed manifest SHA-256:
  `88ac832da3ee1b75f745f47c1382e1517156a52fe51b9256fe5387a6f4e668cd`.
- Correction record SHA-256:
  `d9f1f03725a98eb04766c548747edff88dfcc76c8bbef1bd64c6120316c91b3d`.

Product and index remained read-only. Only this additive rereview record was created. No build,
test, formatter, native/device/hardware/credential operation, commit, push or subagent was run.
The correction scripts were read, not executed by this reviewer.

## Independent closure checks

1. **All receipt evidence is staged.** I enumerated every `files` entry from the actual staged
   `validation/*/receipt.json` records, independently of the correction's assertions. All 67 entries
   are unique paths and exist in the index. For every entry, staged bytes reproduce its original
   receipt byte count and SHA-256 and equal the on-disk captured bytes. The correction's 67-row
   ledger equals this independently derived set, including receipt path, evidence path, size and hash.
2. **The exact 40 logs are retained.** The 40 `.log` entries derived from those receipts equal the
   correction's explicit force-added list. Every log is in the refreshed manifest and staged tree.
   This includes the Core actual-source scan log and portable guard detail log that motivated the
   predecessor blocker. No receipt was rewritten to remove or relabel evidence.
3. **Manifest coverage is exact.** The refreshed manifest covers all 301 staged no-rename path
   spellings: 135 product/incoming-tooling paths and 166 RUN paths including the manifest. Its 300
   rows plus its documented self-exclusion equal the complete staged changed-path set. Every surviving
   raw working-file hash, filtered Git blob OID and current staged blob OID agrees with its row;
   deleted paths remain absent. The manifest reports 67 staged receipt entries, matching the direct
   verification rather than an ignore-aware path count.
4. **No product drift.** The predecessor-to-successor tree diff has 47 path spellings, all under RUN.
   It consists of the 40 retained logs and seven review/correction/publication paths; only the
   publication preparation script and current manifest modify predecessor entries. All product,
   source, final narrative, intended-message and original receipt identities therefore remain
   unchanged. The prior review is now staged at its original hash, and the predecessor manifest is
   preserved byte-for-byte as `execution/publication-manifest-before-log-retention.json`.
5. **No hidden evidence omission.** A fresh ignored-file enumeration under RUN finds exactly 29
   untracked ignored paths, each with `bin`, `obj` or `__pycache__` as a path component. None is a
   receipt entry or staged path. No such generated path is in the manifest. There are no unmerged
   entries, tracked unstaged changes or nonignored untracked files before adding this record.
   The staged diff check passes.
6. **Future preparation rejects this omission.** Static inspection of
   `execution/prepare-publication.py:18–26` confirms that it now reads every receipt entry from the
   index and checks byte count/hash before manifest preparation. A missing staged file causes
   `git show` failure; a changed staged file fails the assertion. It separately rejects ignored
   nongenerated paths. The ignored-path blind spot identified in the predecessor is addressed,
   without broad force-adding arbitrary ignored files.

The 301-path checkpoint is the predecessor's 256 paths plus 45 additive paths: 40 logs, the prior
review and four new correction/preservation/script/predecessor-manifest records. The two modified
paths do not increase the total. These are publication artifacts, not additional test executions.

## Source and preservation continuity

Fresh static verification reproduces the 1,363-file active C# fingerprint:
`c05c4010789edd3424ec62dc143c68bb3fa3cdc36cacc2eef4069c27d6e82b02`.
The source is unchanged from the accepted independent source review and retained software batch.
The intended message and its three exact incoming complexity justifications remain unchanged.

I also freshly reproduced all three frozen-root fingerprints covering 13,666 files, the hashes and
sizes of the four original user files, and all 33 preparation records excluding the authorized
status update. The original checkout remains at audited HEAD with no tracked or index delta.
The new preservation receipt agrees with those observations; it does not substitute for them.

The predecessor's bounded result accounting and Windows/Mac/PIV qualifications remain in force.
Log publication does not establish new native/HID-driver/physical acceptance or resolve deferred
implementation findings. No rerun is necessary for this evidence-only correction.

## Reviewed correction input hashes

All paths below are RUN-relative. These identities were checked against actual files and staged
blobs. The refreshed manifest supplies identities for the 40 logs and all original receipt inputs;
the predecessor review retains the detailed original input ledger.

| Input | SHA-256 |
| --- | --- |
| `execution/log-retention-correction.json` | `d9f1f03725a98eb04766c548747edff88dfcc76c8bbef1bd64c6120316c91b3d` |
| `execution/log-retention-preservation.json` | `feec539a45537ad8f02838e9239e116cccfa7429651a7659eda46862ba51410f` |
| `execution/prepare-publication.py` | `a02e36af8d238e7a3a1ce364acc4b4f22420e721f858841712eaa1029fa40042` |
| `execution/publication-manifest-before-log-retention.json` | `2e0651bca9cb2696929ba239ed250ffc940c0b4d043fa132dc9d97bc2df0f5a1` |
| `execution/publication-manifest.json` | `88ac832da3ee1b75f745f47c1382e1517156a52fe51b9256fe5387a6f4e668cd` |
| `execution/retain-receipt-logs.py` | `63a9109a26e66a20bf2f51945a6562c02dd614998aa98dae9af151952cea877c` |
| `review/final-publication-review.md` | `0517e6c69c6401bdb2ccabcb8dc548886bfdfc76742e8aea471ad27223bc50d3` |

## Execution handoff

Sol may proceed under the existing authorization; Astra orchestrates only. Publication target remains
**only** `origin/yubikit-windows-audit-integration-20261113`, using the normal two-parent commit with
the pinned parents and unchanged intended message. No force push, upstream/audit branch promotion,
original-checkout change or PR is authorized. Fresh post-push remote OID readback remains required;
this static verdict does not claim that a commit or push has happened.

This rereview is intentionally additive and unstaged at handoff. The execution lead may explicitly
stage it afterward and record the final tree. That review-only addition does not require a circular
self-hash or a manifest/commit to predict its future identity. The closure is valid provided the
reviewed product, source, receipt evidence and publication inputs remain unchanged.
