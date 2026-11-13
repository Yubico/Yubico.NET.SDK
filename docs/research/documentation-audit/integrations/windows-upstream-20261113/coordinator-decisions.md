# Coordinator preparation decisions

**Integration preparation accepted. Source merge and validation remain unexecuted.**

Astra read the completed receive report, conflict map and proposed ownership against the pinned
intake. These decisions govern the next execution phase.

## Accepted reconciliation approach

1. Receive `e322712d609aaf03b9bd1cf2964a99326728866a` once. It contains both delivered PR heads and
   has the same tree as final PR774 head `13fdf4301…`. Preserve the audited parent and natural ancestry.
2. Adopt the nine path-specific recommendations in `merge-review/decision-map.md`, subject to fresh
   review of the actual resolved candidate. Preserve incoming Windows ownership/routing and adapter
   layout while retaining applicable audited contracts and the audit branch's runtime corrections.
3. Accept removal of the two obsolete Windows report-access files. Transfer relevant documentation to
   the real report/feature APIs, owners and facades; do not recreate dead classes.
4. Keep extracted Linux/macOS implementations and interfaces in their incoming files. Remove duplicate
   old declarations from conflict resolutions, and migrate applicable audit qualifications into the
   new `ILinuxHidEventSource` and `MacOSHidReportApi` recipients.
5. Review the ten clean overlap decisions and the named active followthrough paths as part of the merge.
   A clean textual merge is not a content review. Historical registry migration entries and the legacy
   disposed-object diagnostic remain intentional.
6. Preserve public signatures, named parameters, `PscsConnectionKind`, assertions and runtime output
   except for the actual approved incoming changes. Unrelated deferred fixes remain outside this task.

## PIV rollback authority

The captured public PR774 handoff, the committed rollback `030b2a448…`, and the restored production
blob `526c5da214546c1655ab8f36e2f7e95831cfdbf0` establish the delivered rollback and its source intent.
Retain the incoming response-only assertions and the absence of a pre-return pooled-command oracle.

The merge report's request for a byte-original human instruction is a provenance follow-up, **not an
additional prerequisite to preparing or applying the already merged delivery**. No private pending
review or unpublished conversation needs retrieval. If a later conflict genuinely requires changing
the restored production behavior, raise that concrete decision rather than inferring permission from
a paraphrased instruction or restoring the withdrawn pool seam.

## Evidence and state boundaries

- The live index has no unmerged entries. “Not merged” in preparation means that no working-tree merge
  has been applied; the nine conflict recommendations concern only the object-database preview.
- Incoming Windows first-open and mixed ceremony outcomes remain preserved. Local unsealed JIT bundles
  supply their specific kernel/BCL evidence, not sealed/AOT or HID-driver acceptance.
- Mac reports remain PR-summary-only here until their raw artifacts are received. Their reported
  passes and permission/topology blocks retain the original source revision and fixture scope.
- No fresh Linux, Windows or macOS build/test/native/device result is claimed by this preparation.
- Frozen Core001, prior integrations, original user files and private/attestation material remain
  protected. New integration findings and receipts belong in this integration packet.

## Execution handoff

Use the finalized plan at `docs/plans/2026-11-13-windows-refactor-integration.md`. Resolve in the
isolated worktree, obtain independent review, then run the source-derived software gates. Platform
qualification is a separate, explicitly scoped lane. Preparation does not start the broader deferred
Core/applet implementation pass or update the published audit branch.
