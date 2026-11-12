# Protocol editor revision 2

R1–R4 are corrected after checking the actual code and retained original-source passages. No review finding
was contradicted by that evidence. The successor is ready for focused independent re-review, not declared
review-approved, compiled, tested or published.

## Identity and preserved review subject

- Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-protocols`.
- Branch: `yubikit-core-docs-edit-protocols`.
- Base/current HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`.
- Product source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
- Reviewed original full diff SHA-256:
  `4a89837083fd34826639a55d1bd988574565bc8a4da508975820e0acfa3755d1`.

All original editor and review records, including the original diff, `edit-protocols.md`, source ledgers
and imprecise historical citation notes, remain unchanged. `reviewed-input.json` captures the original
editor/review record hashes and all 91 reviewed C# texts/hashes before revision. New records live only here.
New primary A/C integration is not imported into this worktree or source pin.

## Focused correction evidence

| Review | Correction and evidence | Original local/proposal IDs |
| --- | --- | --- |
| R1 | SCP11 a/c request §7.6.2.3 versus response §7.6.3.1/Table 7-18; b request §7.7.2.3 versus response §7.7.3.1/Table 7-22. INTERNAL AUTHENTICATE is §7.7; §7.1.1 is parameter coding. Correct common-parser and b-test comments. SCP03 §7.1.1.6/Table 7-3 gives 29 mandatory bytes plus the conditional pseudo-random challenge counter; initial EXTERNAL AUTHENTICATE zero chain is §6.2.3. | CORE-B6-C006/C009/C010/C013; FPT-072/075/076/079 |
| R2 | Null firmware fails ScpInitializer's configured-processor admission before support branching. Correct both unknown-firmware test comments; broad exception/message assertions do not prove initialization proceeded. Change the retained-path decision to edit. | CORE-B6-C013; FPT-079. C007/FPT-073 parameter-lifetime referral remains held. |
| R3 | Restore R-MAC-selected applicability for protected 9000/62xx/63xx; Core selects 0x33. Restore the C-DECRYPTION plus C-MAC condition for §6.2.6 counter/absent-data rules. Preserve error status-only and EXTERNAL AUTHENTICATE exceptions and the current instruction-only exemption concern. | CORE-B6-C005/C010/C011/C013; FPT-071/076/077/079 |
| R4 | State two 32-byte P-256 agreements → 64-byte concatenation → X9.63 output of 80 bytes → five 16-byte values. Verified length checks, allocation, KDF request and slices rather than computing crypto. | CORE-B6-C009/C010; FPT-075/076 |

Exact clauses, printed pages, roles, normative conditions and source/code anchors are in [citations.md](citations.md).
SCP03 remains the inspected **1.1.2 Public Release** and SCP11 the inspected **1.3.0.13 Public Review**;
no unavailable final/review edition is substituted.

## Successor transfer records

- [full-transfer.diff](full-transfer.diff): complete current primary product patch from the unchanged base,
  including the original edits and these corrections. It is the integration patch, not just R1–R4.
- [correction.diff](correction.diff): focused eight-file delta from the reviewed subject for re-review.
- [path-decisions.json](path-decisions.json): all **91** current primary C# hashes/decisions, original IDs,
  base/current/reviewed hashes and refreshed changed-unit overlaps: **57 edit, 22 retain, 12 hold**.
- [candidate-decisions.json](candidate-decisions.json): successor FPT-058–082 decisions with the newly
  edited ScpExtensionsTests path removed from retain lists. Original candidate IDs/indices survive.
- [receipt.json](receipt.json): exact full/delta patch hashes, source PDF hashes, static checks and limits.
- [artifact-manifest.json](artifact-manifest.json): hashes of this successor's records, excluding itself.

The prior [complete example bodies/classifications](../examples.json) remain the exact unchanged examples
for this revision; the composable helper's reviewed body hash remains
`3e0457b3fedca0f971d59cfbff209e3cd61308f507e8b9283a066f79c5a201aa`.
The prior [outside-owner handoffs](../outside-owner-handoffs.json) also remain unchanged and applicable.
No extra applet/shared-guide/external-source editing authority is inferred from the correction review.

## Holds and re-review boundary

All original M/P/E/Q/B holds, source-run aliases, ATR C016/FPT-082 hold, parameter/test-harness ownership,
counter/empty-command/INS82/copy/chaining issues, final/vendor/source gaps, future Windows readiness and
native/runtime/physical deferrals remain in force. Serial1256A88's 0x6A88 failures remain before EXTERNAL
AUTHENTICATE and RESET; no secure-reset success is inferred. No broad re-audit or implementation work began.

Static checks cover old-record byte identity, all 91 current path hashes, authorized path scope, limited
noncomment-line preservation, XML well-formedness and `git diff --check`. They are not compiler-bound
token/import/directive/API/cref checks. No build/test/formatter/crypto/native/device execution, Git mutation
or subagent ran. The coordinator's existing stronger preservation/example compilation gates remain pending.

Re-review the eight-file correction delta against the verified clauses and early-admission path, then use
the complete transfer patch and current 91-path hashes for integration. Finish Core Pass 1 publication
with the explicit deferred handoff, then stop; no Pass 2 or new validation framework is requested.
