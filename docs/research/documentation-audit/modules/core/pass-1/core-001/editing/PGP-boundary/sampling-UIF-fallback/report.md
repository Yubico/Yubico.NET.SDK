# SAMPLE-PGP-001 — bounded UIF fallback wording correction

**The two sampled passages now qualify fallback on diagnostic logging completing successfully.**
This focused successor is ready for owner/sampler review; it is not an independent accepted-review
verdict, runtime fix or new unique Core bug. Primary remains reserved and was not written.

## Patch baseline and scope

Both complete before files matched accepted primary bytes exactly before editing:

| Path | Accepted-primary / isolated-before SHA-256 |
| --- | --- |
| `src/OpenPgp/src/OpenPgpSession.cs` | `2ac58c16bc386cd362bf7c14343ffe3685e01616bf40406b6ab856d3c2dc855b` |
| `src/OpenPgp/CLAUDE.md` | `252b78ee37ae59fb2edfc9e9cafdff2f5d757874a782c3d93bcd235760cf57ba` |

Apply [focused.patch](focused.patch) on that accepted baseline, not on the original product pin
or on an already corrected copy. [Before/after identities and ranges](changed-files.json) and
[before snapshots](before-identities.json) bind those bytes. The original source pin still
identifies the sampled units; it is not the focused patch's accepted-before baseline.

Only the selected local source comment and contributor-guide passage changed. The catch still
logs before assigning `PolicyMayRequire`; cancellation is still rethrown. If logging completes,
the normal advisory fallback allows the caller to proceed. A logging exception propagates instead
and can prevent the cryptographic APDU. No exception handling, runtime tokens or policy changed.

## Exact sampled-unit miss mapping

| Sample item | Original selected unit | Miss and current correction |
| --- | --- | --- |
| SAMPLE-PGP-001 | `core-001:unit:8b2a95f01db9bf2c` — `GetUserPresenceContextAsync` | Unconditional local comment omitted the logging call's possible failure before fallback assignment. The comment now states the diagnostic-success condition and propagation. |
| SAMPLE-PGP-001 | `core-001:unit:710814862611f595` — CLAUDE user-presence section | Narrative repeated the unconditional fallback guarantee. It now distinguishes successful diagnostic logging, logging failure and cancellation propagation. |

[unit-miss-map.json](unit-miss-map.json) preserves both original pin coordinates/hashes and exact
accepted-before/current unit ranges/hashes. This is **one sampled issue affecting two selected
units**, not two bugs or additional investigation/sampling credit. The owner/sampler retains the
existing logging-related engineering/qualification handoff rather than allocating a duplicate
unique Core finding. No other sample or module statement was reopened by this assignment.

## Evidence and preservation

[source-evidence.md](source-evidence.md) identifies actual SDK call order, the configurable logging
boundary, the bounded Context7 orientation and retained .NET 10.0.2 implementation sources.
[source-manifest.json](source-manifest.json) binds the downloaded source bytes, not installed
binary identity or an executed throwing-provider experiment.

All 99 pre-existing OpenPGP/PIV patch, record, review and predecessor-product files bound in
[prior-record-identities.json](prior-record-identities.json) remain byte-exact. No old 17-file
patch, successor patch, review, report, ledger, example or PIV product file was rewritten. All
36 Markdown / 28 XML example bodies remain unchanged by this focused correction.

[static-verification.json](static-verification.json) records comment-only C# preservation, exact
two-path scope, primary-before identity, example-body and prior-record preservation. No SDK
build/test/formatter, crypto/provider/native/device execution, Git mutation or subagent ran.
No exception was injected or actual provider failure observed. Independent review and the
reserved primary compile/publication cycle remain with their current owners. Finish Core Pass 1,
provide the deferred handoff and stop; no broader audit, runtime policy change or Pass-2 work.
