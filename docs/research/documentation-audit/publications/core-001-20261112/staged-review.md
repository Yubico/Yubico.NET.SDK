# Independent bounded staged-byte review

## PASS — staged Core001 products and frozen records approved

**Approve the bounded staged publication state.** All **363 accepted product index blobs** match the
path-aware Git-clean objects calculated from their exact reviewed working bytes. **362 are also
raw-byte identical.** The one documented project-file normalization is exactly the existing Git text
conversion below, with no other byte difference. No blocker or product/attribute correction is required.

Reviewer: independent GPT-6.1 Sol, continuing the already accepted final evidence review. This is a
bounded pre-commit staged-byte confirmation, not a repeat of SDK/content gates or a claim that commit,
push, remote verification or external publication has happened. Only this publication record is written;
the frozen Core001 archive and its earlier reviews remain unchanged.

## Exact single normalization

Path: `verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj`.

| Identity | Independently verified value |
| --- | --- |
| Reviewed working SHA-256 | `ce565faa26c8221ef8d3679a4f4ee6f0313fb720cf9208dc3b04c0d25a7b3298` |
| Index SHA-256 | `63ce1db3c8c419c2a3bb2c8309f46d190dcd60208edf7353b6220c59f9e3e121` |
| Index Git blob | `fc43ad4eaddfb782353bffa7749c615d2ff12ab6` |
| Working newline population | **40 CRLF + two LF** |
| Index newline population | **42 LF**, no CR |

Exact byte comparison proves **`working.replace(b"\r\n", b"\n") == index`**. Every non-newline
byte remains identical: XML values, comment text, order and all other source content are preserved.
The working SHA-256 remains the accepted product-map identity, not a silently replaced after hash.

Actual effective attributes are `text: set`, `eol: crlf`, `filter: unspecified` and
`working-tree-encoding: unspecified`. Root `.gitattributes` is byte-identical in HEAD, index and working
tree; there is no staged out-of-run attribute change. Git's clean-side LF storage is therefore expected,
even though checkout policy requests CRLF. **Do not force mixed raw bytes into an unclean index, change
attributes, or rewrite the reviewed working file to make raw equality appear universal.** The initial
stricter raw-check rejection remains preserved in `staged-byte-verification.md` as a failed check,
followed by this exact canonicalization proof.

## Entire staged scope and frozen archive

Read-only Git/Python checks independently confirmed:

- Every accepted working product still matches its recorded raw hash. For each of all **363 paths**,
  `git hash-object --path=<path> --stdin` on those bytes equals its actual stage-0 index object.
  Index blob reads found exactly the one raw difference above, with no unexpected product difference.
- Frozen manifest SHA-256 is
  **`b9a4e4d8ca910cbb6f9cf9abe9c17feede6854e6011eecf14712ded4c3c7cb33`**.
  All **5,225 distinct records** match manifest raw sizes/hashes, manifest staged hashes and actual
  index object IDs; each record's raw bytes also reproduce that Git blob object. The manifest itself
  is byte-identical in the index. The run's raw-preservation/`-text` policy remains intact.
- The checkpoint contains **5,057 changed staged paths**, all within the exact accepted product set
  or frozen record/manifest population. Records already present unchanged in HEAD are not invented as
  additional changed paths. No unmerged entry or unstaged tracked change exists; `git diff --cached
  --check` exits **0**.
- All **four original user files** match their original raw hashes and remain absent from the index
  and staged set. Their untracked state is not an omission to fix.

This publication directory is outside the frozen manifest. Its companion verification note and this
review are publication-only follow-through records; any explicit staging of these records must preserve
the frozen run and accepted product identities. No new SDK or whole-campaign gate is required for the
proved Git normalization. Astra may proceed with owned commit/push, remote-identity verification and
the external handoff, then stop after Core Pass-1 publication/deferred handoff.

No Git mutation/staging, SDK/compiler/test, formatter, product/attribute edit, subagent, polling or sleep
occurred in this review. Read-only Git commands included index/object/attribute inspection, path-aware
hashing without `-w`, and diff/status checks. The index and all reviewed working bytes were left intact.
