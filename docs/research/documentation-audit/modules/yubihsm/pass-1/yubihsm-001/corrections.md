# YubiHSM corrections and evidence progression

## Hardware finding HSM-IMP-008

The first independent KDF/retry probe executed on serial 103 (metadata 5.8.0.beta.0). It passed the
four RFC 4493 primitive vectors, matched all three keys with optional cryptogram omitted, observed
typed 63C7 and LIST seven after one wrong password, then failed parsing the supplied-valid-cryptogram
response: **expected 48 bytes, received 56**. The stack binds that failure to the cryptogram-bearing
CALCULATE. Finally reset and fresh LIST/retry readback passed. Original failure remains authoritative;
later focused probes isolate other behavior using the omission branch.

SDK session-effective version is **0.0.1**; ykman reports applet **5.8.0** and device metadata
**5.8.0.beta.0**. These observations are retained distinctly. The investigator has a bounded original-
source follow-up for response layout/version applicability; trailing bytes have no inferred meaning.
Editor received the affected XML/guide dependency. SDK runtime changes remain a Pass-2 item.

## Compiler capture timing

The first post-test hardware capture recorded an empty Csc argument list: a no-op compilation during
test execution overwrote the earlier actual command-line output. The membership verifier rejected it.
The failed capture/verifier result is retained. Rebuilding the unchanged probe and capturing immediately
passed actual membership/227 dependency checks; full original source/config/asset snapshots are retained
under hardware-probe/evidence/final-inputs-v1. Subsequent captures occur immediately after clean builds.

## Cancellation expectation and observed-status recovery

The v2 cancellation probe cancelled at 1,009 ms and drained at 14,308 ms with APDU **6982**, one **Failed**
resolution and a live body token. Its expected cancellation/6985 alternatives failed; fresh recovery was
therefore unreached. Finally reset/fresh readback succeeded. This remains a failed cancellation expectation.
V3 explicitly measures the observed 6982 outcome and then tests fresh-session recovery before resetting.
Its named method/classification separates that evidence from successful cancellation or proven timeout.
HSM-DEP-002 retains the native drain/cancellation question; no Core implementation is inspected or changed.

## Independent review record corrections

R1: editor, reviewer and coordinator path resolution confirm three parents from src/YubiHsm/tests
reach the repository root. The proposed broken-link subfinding is rejected in findings/coverage;
original investigation retains its historical mistake. Remaining C011 improvements stand.
R2: authoritative status and hardware results now reconcile all completed/failed/deferred dimensions.
Late touch/default-key cleanup evidence receives focused re-review before final closeout.

The independent cleanup helper initially supplied Python byte password without its required padding.
Original source/failure retained; explicit 16-byte padding corrected the helper. Fresh reset/default-key
PUT/LIST/DELETE and final readback passed. Shipping implementation and earlier test assertions unchanged.

## Formatter evidence schema

Scoped formatting normalized EOFs, grouped one test import, and removed four unused ordinary namespace
imports across three files. Coordinator's first transform JSON omitted fields required by the unchanged
Roslyn evidence reader; the checker rejected the manifest with KeyNotFoundException. Original recorder,
JSON and failure log are retained. Corrected schema includes verified formatter exit, empty added-import
set and proven remaining-text identity. Final Roslyn check passed all 26 files with exactly four listed
removals; all 20 API snapshots unchanged. No token comparison algorithm or warning policy was relaxed.

## Shipping compiler capture default hook

Both consumer and HsmAuthTool builds succeeded warning-free. HsmAuthTool's first capture check required
an empty CustomBeforeMicrosoftCommonProps value; the SDK supplies its standard vCurrent hook even after
the command removes external environment configuration. All native initial/current obj/bin/asset paths
matched. A narrow pinned standard-SDK-hook verification replaces this erroneous empty-value assumption;
shipping SDK/project files and actual compiler outputs remain unchanged. Preserve the first partial
capture/error and accept only complete source/reference/analyzer/config membership after correction.
