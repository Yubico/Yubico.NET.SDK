# Post-PR745 campaign integration

## Purpose and authority

This checkpoint integrates the merged PIV implementation and its upstream dependencies before PIV
documentation Pass1. User approved proceeding after PR745 merges into `yubikit`; this integration
preserves the published documentation campaign and the merged implementation in one non-rewritten history.
It is separate from the PIV documentation-only diff and from deferred implementation sweeps.

GitHub PR745 merged at2026-11-04T23:27:12Z (2026-11-05 local). The15-minute watcher and a fresh
coordinator check at23:40:27Z verified merge commit and current `yubikit` tip
`b1362127203beda5e9c797e8648b28e2716c496c`. Compare status was identical and the merge base matched.
Watcher `sch_1a8d3dff5001ZyIPND5SlbBZ2J` was paused after its first wake. No PIV investigation began
against pre-merge source. Planning/approval/monitor records are retained with this checkpoint.

## Lineage and preservation

- Campaign parent: `0c301252d7e8abfd1e84cb0cbc0ce17925f0b963`, `yubikit-docs-audit`.
- Incoming parent: `b1362127203beda5e9c797e8648b28e2716c496c`, fetched from `origin/yubikit`.
- Merge base: `089b2393ec90fa2c09434e7b897f635373b46c29`;27 campaign commits and14 upstream commits.
- Primary campaign checkout had a clean tracked tree and four original untracked user files.
- Other agent's `../yubikit-piv-session-consistency` worktree was clean atb1362127 and remains untouched.
- All3,401 prior archived artifact identities, their manifests and four original user files passed
  pre-integration preservation. Final checks repeat before commit/publication.

Upstream includes the new cryptographic catalog, response ownership changes and PIV session/API
consistency work. Existing campaign OATH fixes and audited prose remain. No reset/rebase/force push
was used. The coordinator invoked a non-fast-forward, no-commit merge for review and validation.

## Resolution and bounded adjustments

Fresh Sol resolved seven conflicts: Core README/API snapshot, CTAP CoseAlgorithm summaries, OpenPGP
README/AlgorithmAttributes, and two deliberate upstream deletions. The exact decisions and parent
blob identities are preserved in resolution.md. Core API combines the complete incoming catalog with
three campaign Base32 declarations. Both retained conflict C# implementations match incoming runtime
content; deliberate CurveOid and SDK_PAIN_POINTS deletions retain their historical Git provenance.

Coordinator qualified the SecurityDomain README's `KeyInfo` element type because the new Core catalog
introduces an identically named public type under its existing imports. Complete affected guides are
prepared for compilation in a new integration context; frozen example contexts remain historical.

The first clean integration build failed withCS8618 in the campaign's OATH ResponseBackend fixture.
Incoming `ApduResponse` changed from value type to reference type, making its unset `LastResponse`
state genuinely nullable. The fixture now models that absence and its existing zeroing assertion
first requires a non-null captured response. No shipping operation or existing zeroing check was
weakened. This necessary test-fixture migration belongs to the integration checkpoint, not PIV Pass1.
Original failed build/result remain retained. A fresh reviewer checks this change alongside resolutions.

PIV module source matches incomingb1362127 exactly at the integration checkpoint before the audit.
No device operations have been performed. Existing PR hardware results describe their own revisions;
the documentation cycle will establish identity/state and qualification evidence afresh.

## Gates and next action

Scoped formatter applied to the three integration-edited C# files; final verify-no-changes passed.
The clean combined solution passed4,124 unit tests, including289 PIV, five existing skips and zero
compiler/analyzer warnings or failures. Documentation/package and architecture checks passed.
Complexity reports four upstream-renamed PIV TryDecode methods as new relative to the campaign
parent. Their files match the incoming parent exactly; retain its explicit justifications rather
than redesign these parsers during integration.

Fresh independent integration review found no additional required product corrections. Its optional
OpenPGP introduction clarification was adopted: device examples open sessions, while the new catalog
example uses local metadata. All19 affected guide groups compiled with zero warnings:13 complete
OpenPGP fences and six SecurityDomain groups. Actual compiler evidence verified five sources,
180 references and10 analyzers. The first capture incorrectly equated host `Version` and late-defined
`YubiKitVersion`; retained import-order evidence shows the host and genuine SDK references receive
the observed1.0.0 default while YubiKitVersion is2.0.0-alpha.2. The versioned correction records those
actual values and preserves all warnings-as-errors/no-skip/membership checks. Original failed capture,
tool and manifest remain unchanged. No build/version setting was altered.

No new PIV run will dispatch until the integrated baseline is committed, backed up and verified to contain the incoming
merge commit. The frozen campaign archives remain immutable. The review's3,439 historical tracked
audit-file comparison and the manifest-based3,401 artifact count describe different inventories;
both preservation checks retain their original scope.

Initial staging exposed normal Git text conversion and whitespace diagnostics on exact compiler-input
snapshots. The campaign's existing evidence-preservation attributes were extended to integration
validation/evidence paths; their original CRLF, trailing whitespace and EOFs remain byte-identical.
Active product/documentation text retains the usual repository rules. Original capture and build
failures remain preserved rather than rewritten to match later success.
