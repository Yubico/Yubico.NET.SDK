# Historical measurement provenance for B1 clean sample

November 12, 2026. Bounded existing-record lookup for `core-001:unit:f1d4372202bdaa9b`, `docs/architecture/connection-ownership-and-contention.md`, **Why connection ownership is enforced**. Audit source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. This addendum preserves the original sample/unit identity and all earlier B1 records.

## Exact original report

Commit **`64efe84487143235a42121db962c9328cfec0aa8`**, July 30, 2026, **docs(phase1): measure the CCID contention hazard on hardware**, introduced:

`docs/plans/session-contention/phase1-findings.md`

Git blob: `738d5c433b5583aab2590b8b72391b3d2b01f796`.
Report-byte SHA-256: `fb602ed05f1fe4f9a6253d72195756879995df9b3e31e4a3ef36815fad78851b`.

| Sampled historical claim | Original report anchors and recorded scope |
| --- | --- |
| PIV verification → Management query succeeds → later PIV command fails with `0x6D00` | Lines **3–5** identify `ykphysical:103`, firmware **5.8.0**, PID **0x0407**, **macOS**, and an ad-hoc `contention-exp` harness. Results row **11** reports verified PIN followed by `GetDeviceInfoAsync`, subsequent signing failure `SW=0x6D00`, **4/4** repeats. Lines **20–26** interpret the response as deselection and explicitly report that the intervening `GetDeviceInfoAsync` returned **OK**. |
| Management HID communicates while PIV holds CCID, with PIV surviving | Results row **14** records experiment 4, **1/1**, “both HID transports, PIV survives.” Lines **55–60** explicitly name **HID OTP and HID FIDO** and report successful Management answers while PIV holds CCID. The report gives one experiment-level repeat count, rather than separate repeat counts or captures for each transport. |

Lines **87–89** limit the results to macOS and firmware 5.8.0; broader firmware applicability is explicitly called an inference. The same commit message summarizes both observations. Retrieve the exact report without restoring files:

```text
git show 64efe84487143235a42121db962c9328cfec0aa8:docs/plans/session-contention/phase1-findings.md
```

## Report migration and corroborating historical excerpt

- **`51808c955b5d6962fbcc5baf6b24c200df303d0f`**, August 8, 2026, promoted the contract and motivating measurement into `docs/architecture/connection-ownership-and-contention.md`.
- **`220c50c122f9bd7e0451b6586bb9bfd3fd8b8ac9`**, August 8, 2026, **docs(architecture): carry the phase-1 measurements and coverage audit forward**, explicitly says it restores the deleted Phase-1 report's evidence. Its added **What the measurement actually showed** table retains the 4/4 PIV/Management failure and 1/1 HID-survival results, macOS/5.8.0/PID scope, successful intervening call and both named HID transports. This is report migration, rather than an independent measurement.
- **`aadd89f15f13e496d3ff15fff70640a6f1f55ed4`**, August 3, 2026, **test(piv,oath): pin ISC-1 on hardware and retarget tests that encoded the old contract**, records a later **Linux, firmware 5.4.3** pre-change RED at `b0ce52a0`. Its commit message includes the exact excerpt: `Sign/decrypt operation failed for slot 0x9A: Instruction code not supported or invalid (SW=0x6D00)`, plus the transport assertion `Expected: Not SmartCard / Actual: SmartCard`. Treat this as a retained report excerpt for that later scenario, rather than raw evidence for the original macOS experiment.
- The pre-promotion ISA is accessible at **`6d25c90f765c34c493bbb9d52b225cb84bae5265:docs/plans/session-contention/ISA.md`**, blob `095ac0ea9c7e5884c201f061553a2012f4b9980a`. Lines **711–743** retain a post-change Linux `isc1-probe` transcript reporting successful VerifyPin, Management over **HidFido**, and PIV still answering on serial **9681620**; this is a different fixture/route and a report-embedded transcript. Lines **745–769** describe promoting the ephemeral probe into integration tests and retain the RED excerpt above. Neither supplies the original macOS raw captures.

All three measurement/migration commits checked above (`64efe844…`, `220c50c…`, `aadd89f1…`) are ancestors of the audit source pin. Later policy deliberately refuses grouped-key parallel connections; these historical interface observations grant no current parallel-ownership recommendation.

## Raw-capture gap and sample qualification

**No original raw APDU/report capture, complete runner log or original `contention-exp` harness was located in this bounded repository/history lookup.** The original report names only the abbreviated ephemeral path `/var/folders/.../opencode/contention-exp/`, with no reproducible full path or retained capture hash. The later `aadd89f1…` commit explicitly describes the original ad-hoc harnesses as no longer existing. This lookup did not search another machine, recreate a harness or contact hardware.

For the sample verdict, the two statements have **traceable historical-report support**, with the exact fixture, OS, firmware, repeats and limitations above. They have **no fresh physical verification or independently reviewed original raw capture** in this lookup. B1's earlier clean disposition supplies source-context review, rather than raw measurement attestation. If the paragraph remains, qualify its authority as the historical investigation's recorded observations; stronger measurement acceptance remains a source gap for Astra to disposition.

Keep the layers separate: application selection/security-status semantics require their own applicable normative citation; **`0x6D00` after this particular sequence and successful Management-over-HID coexistence are reported empirical outcomes**. The report's deselection interpretation is not a general rule that every deselection, lost PIN status or PC/SC contention must produce `0x6D00`. The retained NIST retrieval distinction in [triage-clarifications.md](triage-clarifications.md) remains unchanged and supplies no independent measurement proof.

Only this new clarification file was written. No prior record or product file was edited; no web research, build, test, native/device operation, Git mutation or subagent action occurred.
