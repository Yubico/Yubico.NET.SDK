# Core001 publication staged-byte verification

The frozen Core001 manifest contains 5,225 records plus the manifest itself. Snapshot, check and staged
checks passed using the preserved `check-records-v2.py`. Manifest SHA-256:
`b9a4e4d8ca910cbb6f9cf9abe9c17feede6854e6011eecf14712ded4c3c7cb33`.

The coordinator staged only the 363 accepted product paths and the Core001 record/manifest paths.
There were 5,057 changed staged paths at that checkpoint. All 363 product index blobs match the exact
Git-clean object computed from their accepted working bytes. Of these, 362 also match raw bytes.

The initial stricter raw-byte check rejected one project file before commit:
`verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj`.
Its accepted working file has 40 CRLF and two LF endings. Existing Git attributes specify text/CRLF;
Git's clean filter stores all 42 lines with LF. Exact comparison proves that CRLF-to-LF conversion is
the only difference—no XML value, comment text, ordering or other byte changed.

| Identity | Value |
| --- | --- |
| Accepted working SHA-256 | `ce565faa26c8221ef8d3679a4f4ee6f0313fb720cf9208dc3b04c0d25a7b3298` |
| Staged SHA-256 | `63ce1db3c8c419c2a3bb2c8309f46d190dcd60208edf7353b6220c59f9e3e121` |
| Staged Git blob | `fc43ad4eaddfb782353bffa7749c615d2ff12ab6` |

The working file remains byte-exact to its reviewed identity; no attributes or Git-filter policy
changed. Core001 records use their existing `-text` rule and passed exact staged verification.
The staged diff check passed, no unmerged index entry or unstaged tracked change remained, and the four
original user files remained untracked and untouched.

This publication record is outside the frozen Core001 archive so that freeze and Git follow-through
can be recorded without modifying any frozen record byte. Commit/push/remote acceptance follows
separately; none is claimed by this pre-commit verification record.
