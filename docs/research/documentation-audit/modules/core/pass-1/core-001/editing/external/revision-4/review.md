# Focused independent R1/R2 re-review — revision 4

## Decision: PASS for the full 37-file external batch at the bound identities

Both mandatory corrections from `../revision-3/review.md` are satisfied. No remaining mandatory
correction was found in the focused successor. This acceptance combines the earlier full content/citation
review of the 35-file batch with fresh source verification of R1/R2 and exact preservation of everything
outside those corrections; it is not a new whole-audit review or runtime qualification.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-external`.
Unchanged HEAD/base: `da66f5d01ba594993974ddd26723e8c300c0a454`.
Product source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Full patch: `final-successor-source.patch`, 92,857 bytes, SHA-256
`4738181238db7103d65e47addac0a96c8a936e8c4a464a991885abd8ea788d68`.
`review-identities.json` binds the patch, complete current 164-path manifest, reviewed records and
focused correction identities. The verdict applies to these exact bytes, not a later unreviewed change.

## R1: authorization prose and real configuration — satisfied

All three corrected paths are explicitly linked to **CORE-A-C013 / FPT-013**, retaining inherited
`management-001:MGMT-XREF-003`. The previously generic retained helper paths now have source anchors
and actual candidate associations in both the current path map and candidate updates.

| Current source | Verified correction and supporting behavior |
| --- | --- |
| `src/Tests.Shared/README.md:9–10,80–87,335–337` | Overview, Safety and Known Limitations now agree: empty configured serials exit unless unknown serials are enabled; individual rejected views and failed identification reads are filtered; an empty fixture list raises `SkipException` at row binding. This is not a new gate or a claim that authorization prevents discovery I/O. |
| `src/Tests.Shared/Infrastructure/AllowList.cs:19–33,44–46` | XML matches the complete constructor48–67: the exit branch56 is exactly `_allowedSerials.Count == 0 && !AllowUnknownSerials`. `IsDeviceAllowed:85–86` returns a Boolean and does not exit. Harness filtering and lazy-binding behavior are identified separately. |
| `src/Tests.Shared/Infrastructure/AppSettingsAllowListProvider.cs:24–38` | XML identifies JSON and uses the real `AllowUnknownSerials=false`, not unsupported `EnableHardFail`. Constants42–44 and getters63–96 read the two named keys. The serial-less authorization explanation retains the readable-metadata condition. No actual configuration value changed. |

Fresh source reads of `YubiKeyTestInfrastructure.cs:193–309` and
`YubiKeyTestState.cs:315–347` confirm identification before per-device authorization, filtered
nonmatches/read failures, cached empty results and `SkipException` at binding. Exhausted transient
scan retries remain a separate propagated initialization failure.

The provider's entire executable `OnInvalidInputErrorMessage` raw string remains byte-identical to
the fixed base, including its obsolete `EnableHardFail=true` example at current121. The explicit
`R1_protected_runtime_output` referral retains A-C013/FPT-013/inherited identity and the non-D reason.
That deliberately protected output is not presented as fixed by the XML correction.

## R2: hash-keyed classifications and current inventory — satisfied

The seven corrections are keyed by exact path/version/body SHA-256. Every immutable original entry
equals its entry in the frozen revision-2 inventory. Every replacement equals its materialized entry
in the new `example-inventory.json`. The successor contains **247 bodies: 243 initial plus four
retained historical primary bodies**, with no new C# consumer example.

- Provider base/current JSON is correctly classified as configuration, not composable C#. The original
  `f44d4bb4…dee888` bodies remain in frozen history/overlay. Only the current body becomes
  `d5539c5c…d30b4e`, exactly matching the actual current XML JSON and its real false setting.
- TestCategories base/current bodies are shell recipes, not C#. Their raw XML body/hash
  `5a25bdc6…19a4d7` retains literal `&amp;`. The separately labeled derivative replaces only those
  references with `&`; recomputed derivative hash is `5c293e08…7e67a2`. No shell execution or renderer
  result is inferred.
- Both base/current CLAUDE AllowList bodies are abbreviated implementation sketches, not complete
  replacements or compilation participants. Current CLAUDE32–33 explicitly supplies that status;
  omitted real property/logging behavior is not silently supplied by an alleged executable context.
- The old IYubiKeyFilter base body `ae523949…e93a5` is correctly marked superseded and invalid for the
  audited APIs. Its nonexistent types are not treated as missing imports. The corrected current
  WithYubiKey/YubiKeyTestState body remains unchanged and separately classified.

All other **236 initial entries** and the **four primary historical entries** are exactly unchanged.
All seven successor body hashes and both shell derivative records were independently recomputed.
NOT-RUN / NOT-APPLICABLE classifications do not claim compilation, configuration loading or execution.

## Independent preservation checks

- Fresh Git diff with explicit `a/` and `b/` prefixes exactly equals the complete named patch and contains
  **37 product paths**. Comparing full patch sections to revision 3 finds only the README section
  changed plus two new helper-file sections; all other 34 sections are byte-identical.
- All **164 unique current path hashes** match actual files. Exactly the three R1 paths differ from
  the reviewed revision-3 164-path manifest. Current dispositions are 37 EDIT, 99 RETAIN,
  22 HOLD-HISTORICAL and 6 HOLD; the 26-path B2/B3/B4 verification/benchmark ownership is unchanged.
- Both added C# files preserve every non-full-line-comment source line, including directives,
  imports, declarations, predicates, branches and the complete runtime raw string. Their XML
  documentation parses. Final-newline representation is separate from executable content.
- Reversing only the two exact README R1 passage replacements reproduces its reviewed raw SHA-256
  `ac76332e3643fa62ce6ad5dffaeb4369ca2cbe319e91f5f990dd03edceb65157`.
- All **47 frozen output identities**, **two prior review identities** and **13 revision-4 bound
  output identities** match. Historical hashes were preserved, not falsely claimed current after
  the authorized README correction.
- All three script files retain their accepted revision-3 raw hashes. Their base/current
  non-comment hashes were freshly recomputed and match the exact proof in the prior review.
- `git diff --check` exits 0 and HEAD remains the fixed base. No product/editor record, runtime
  string, assertion, configuration, validation tool/profile, Git index/ref, commit or push was changed
  by this re-review. No build/test/formatter/native/device/crypto/provider execution or subagent occurred.

## Coordinator handoff and stopping point

The full 37-file documentation batch may proceed to the separately owned existing final gates.
Compiler/token/API/example verification and qualified source/input reconciliation remain coordinator
responsibilities. In particular, comment-only script changes still change compiler/toolchain input
hashes; neither the prior review nor this PASS makes an old profile binding current.

All runtime/data/policy/experiment, native producer/header/loaded-path, final-profile and physical
qualifications remain unchanged. No hardware PASSED evidence follows from compilation or an outer
timeout; exchange drain is not whole-workflow/native completion. Windows concurrency remains
UNREADY/UNREVIEWED, PIV physical deferrals stand, serial 9681620's cause remains unexplained, and
serial 125's setup 6A88 before RESET supplies no protected-reset result.

Finish Core Pass 1 publication, provide the deferred-finding handoff, then stop. No broad re-audit,
new tooling framework, Pass-2 project or additional hardware campaign is requested.
