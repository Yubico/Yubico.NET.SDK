# How the findings were verified, and how they will be fixed

## Baselines

| Commit | What it is |
| --- | --- |
| `55e02064`, `65e36386` | Commits the audit looked at. |
| `fdfcd6fd` | `yubikit` when we verified the findings. The repro tests and all line references in this guide are at this commit (the base of this branch). |
| `df1ec06d` | Current `yubikit`. It includes `yubikit-async-boundaries` (PR #685). Each finding page says whether its code path changed since `fdfcd6fd`. Fixes will be based here. |

## Verification method

1. **Re-trace each claim at HEAD.** One verification agent per area (WebAuthn requests, extensions, largeBlob, low-level FIDO2, PIV and keys, Core and CLI, other applets, CI) followed the audit's source-to-sink path in the code, ignoring the audit's wording. Their reports are in [`evidence/agent-reports/`](evidence/agent-reports/). The shared instructions they got are in [`BRIEF.md`](evidence/agent-reports/BRIEF.md).
2. **Quote the governing spec.** Every verdict cites the normative text, verbatim, with section number. This guide adds which spec versions contain each clause (CTAP 2.1, 2.2, 2.3; WebAuthn Level 2, Level 3), because the test YubiKeys implement CTAP 2.1 (firmware 5.7.4) and CTAP 2.2 (firmware 5.8.0).
3. **Compare with the canonical Python implementation.** `python-fido2` for FIDO2 and WebAuthn, and `yubikey-manager` (`yubikit`) for PIV, OATH, OpenPGP, YubiOTP, Security Domain and Management. Android, Swift and the legacy .NET SDK are shown as context only.
4. **Reproduce with a failing test.** Each repro test asserts the spec-correct behaviour, so it fails today and will pass once the bug is fixed. They are in `src/*/tests/*/AuditV2/` and tagged `[Trait("Audit", "YESDK-16xx")]`.
5. **Hardware.** Findings that need a real device were run on two allow-listed test keys (YubiKey 5C NFC, firmware 5.7.4; YubiKey 5 NFC, firmware 5.8.0 alpha), one test at a time. Touch and PIN tests were run in one session with the maintainer present. Results: [`evidence/hardware-results.md`](evidence/hardware-results.md).
6. **Cross-vendor review.** Reviewers from a different model vendor than the author checked spec quotes, test failure reasons and remediation advice. Their corrections are in the report as "Review correction" bullets.

## Verdicts

| Verdict | Meaning |
| --- | --- |
| Confirmed | The bug exists as described. |
| Confirmed, with a correction | The bug exists, but part of the audit's description, reach or severity is wrong. The page explains which part. |
| Partial | Some of the described problems remain, others are already fixed. |
| By design | The behaviour comes from the device and can't be changed by the SDK; it needs documentation. |
| Not reproduced | The code does not behave as described. |

## Root-cause labels

| Label | Meaning | Consequence for the fix |
| --- | --- | --- |
| SDK only | The canonical Python implementation is correct. | Match the Python behaviour. |
| SDK and Python share it | Python has the same issue. | Prove it against the spec first; record it in [`python-divergences.md`](python-divergences.md). |
| .NET runtime | The problem is inside the runtime. | Document it and report it upstream. |
| Device behaviour | The YubiKey behaves this way. | Document it. |
| Replace with runtime API | Our hand-written code duplicates a correct runtime API. | Delete ours and call the runtime. |

## Known limits

- Only macOS was used for hardware tests. The #22 result that sequential processes do not reproduce is a macOS observation with an unknown mechanism; Windows and Linux were not tested.
- The raw logs of the first verification run were lost when the OS cleared a temporary folder. Their key lines are quoted in the report and in the hardware results.
- `#5` was not run on hardware (the unit repro is enough). The python-fido2 to SDK large-blob read was replaced by the stronger #13 observation.

## How fixes will be done

1. **Evidence gate before any code.** For each finding: the spec clause in the version that applies, and what the Python reference does. If the spec isn't violated in the governing version, the item is dropped. If Python has the same bug, it must be proven against the spec and checked for a deliberate choice (git blame, issues, changelog).
2. **Proof with every fix.** One commit per finding (one per `#25`/`#26` sub-item). Each commit contains the fix, its test, and an evidence file with: the test failing on the unfixed code, the test passing on the fix, the verbatim spec quote with version, and the Python permalink. A revert check confirms the test really fails without the fix.
3. **Grouped worktrees.** Five parallel groups, each touching its own files so they don't conflict: WebAuthn/CTAP requests and extensions; largeBlob; PIV and key material; Core hardening and CLI; other applets. CI goes last.
4. **Hardware discipline.** Every hardware test opens all allow-listed keys during discovery, so parallel runs collide. Unit tests run in parallel; no-touch hardware tests take a machine-wide lock; touch and PIN tests wait for one session with the maintainer.
5. **One pull request** into `yubikit`, one commit per report item.
