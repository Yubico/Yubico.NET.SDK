# Core001 publication verification

Published branch: `yubikit-docs-audit` in `Yubico/Yubico.NET.SDK`.

## Frozen audit commit

**`bff374eb533f9ec03deecc04561aa286c050efa6`**

Subject: `docs(core): complete Pass-1 documentation audit and deferred handoff`.
The commit contains 5,059 changed paths: the reviewed audit/product changes and the two staged-byte
publication records. The four original untracked user files were excluded and preserved.

The push advanced the remote from `da66f5d01ba594993974ddd26723e8c300c0a454` to the frozen audit commit.
After successful push, `git ls-remote origin refs/heads/yubikit-docs-audit` returned the exact full
`bff374eb…` identity. No force push or history rewrite was used.

## Freeze and staged verification

- Frozen packet: **5,225 records plus its manifest**.
- Manifest: `modules/core/pass-1/core-001/validation/record-manifest.json` under the audit directory.
- Manifest SHA-256: `b9a4e4d8ca910cbb6f9cf9abe9c17feede6854e6011eecf14712ded4c3c7cb33`.
- Snapshot, check and staged checks passed using the preserved record adapter.
- All 363 staged product blobs match path-aware Git-clean identities. The sole `.csproj` CRLF/LF
  normalization is byte-exactly explained and independently approved in
  [staged-byte verification](staged-byte-verification.md) and [staged review](staged-review.md).
- The independent final evidence review granted freeze/publication readiness; actual compiler,
  unit, package, sampling and preservation outcomes remain in the frozen archive.

The external [Core handoff](../../core.md) records the completed documentation milestone and links
the frozen deferred-findings handoff. Updating that external index does not modify frozen Core001
records. The index/publication follow-up is a separate documentation-only commit.

All implementation, source-authority, native/physical and Windows-readiness qualifications remain at
their documented scopes. The requested stopping point is reached with this publication and handoff.
