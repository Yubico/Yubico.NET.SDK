# Windows refactor intake and integration preparation

**The resolved integration has passed its bounded software checks. Final evidence review and publication remain pending.**

- Audited campaign parent: `d5d01ef99f1478e4d49911194c17667b2450d2fd`.
- Incoming v2 parent: `e322712d609aaf03b9bd1cf2964a99326728866a` (`origin/yubikit` after fetch).
- Merge base: `5746e72ed9a848d08b5782ddde025aa4a4b522ef`.
- Incoming delivery: PR771 Windows HID ownership and stacked PR774 adapter consistency, both merged.
- Isolated branch/worktree: `yubikit-windows-audit-integration-20261113` under `/home/rupicola/Code/y/`.

The incoming delta includes Windows HID implementations, macOS owner extraction, Linux seam-file
splits and PC/SC renames, plus tests, registries, documentation and verification tooling. Current PR
state and historical evidence claims must be distinguished: merged delivery does not automatically
qualify every source revision or close the reported first-open, AOT, OTP-permission and physical gaps.

## Completed receive review

The Sol [receive/readiness report](intake-review/report.md) and hash-bound evidence ledger are complete.
Both PRs deliver the same pinned ancestry; the merge tree equals the final PR774 head tree. Retained
Windows reports and both unsealed JIT bundles were inspected at their actual source identities.
The original first-open failure and 17/18 attended ceremony aggregate remain visible. Mac results are
PR-summary-only on this host because their raw artifacts remain on the Mac.

The integration plan includes the source-derived portable evidence gate and the three hardware-free
event-lifecycle cases in Core's integration assembly. The completed
[conflict map](merge-review/decision-map.md) covers all nine preview conflicts, ten clean overlap records,
extraction recipients and active reference updates. [Coordinator decisions](coordinator-decisions.md)
accept this preparation and clarify the delivered PIV rollback authority.

The user authorized execution through publication on 2026-11-13. Astra applied the pinned merge with
`--no-ff --no-commit`; it produced the nine expected conflicts. Separate GPT-6.1 Sol resolvers and fresh
reviewers completed source/documentation reconciliation; both reviews found no Critical or Important
issue. Astra executed the serialized software batch before the user assigned Sol execution through
publication and retained Astra as orchestrator only. Sol inspected the complete retained results,
verified source/inventory/API/package identities, and ran the scoped formatter. The final prose follows
`records/style.md` and the SDK house style. See the [software summary](validation/summary.md) and
[handoff](handoff.md) for the exact scope and remaining approval boundary.

The merge-start preservation check reproduced all three frozen subtree fingerprints (13,666 files)
and the four original user-file hashes. The original audit checkout remains at its published parent.
Execution receipts are under `execution/`; the preparation reports retain their original scope.

New records belong here. The published Core001 audit and prior integrations stay frozen. Original user
files and other worktrees remain untouched. PIV physical deferrals and private/attestation preservation
continue to apply. This intake authorizes preparing the integration and focused documentation delta;
the broader deferred-fix implementation pass remains a later task.
