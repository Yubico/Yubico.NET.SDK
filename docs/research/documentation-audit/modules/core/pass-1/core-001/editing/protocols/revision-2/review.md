# Protocol revision 2 — focused independent re-review

**PASS — content and citations for the full successor batch at the exact hashes below.**
R1–R4 are resolved. No required correction remains from this focused re-review. This approval combines
the original review's supported content with the verified eight-file correction delta; it does not
claim a new full audit, compilation, runtime qualification, or publication.

| Review subject | SHA-256 |
| --- | --- |
| Complete successor `full-transfer.diff` | `7d2f50621bddf029cc3b494a256552ea9169d3a2d142caeb101a72d7ebd50b5e` |
| Focused `correction.diff` | `86e72eb988961fca3b8241905c548056316ce858b108e2530de7f9215812628e` |
| Original reviewed full diff, preserved | `4a89837083fd34826639a55d1bd988574565bc8a4da508975820e0acfa3755d1` |

Base/current HEAD remains `da66f5d01ba594993974ddd26723e8c300c0a454`; product source remains
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Integrate the **complete successor**, not the correction
delta alone. The approval is bound to the successor path/candidate records in `review-identities.json`.

## Correction decisions

| Finding | Decision and checked anchors |
| --- | --- |
| **R1 — exact GP locators and variants** | **Resolved.** `ScpState.Scp11.cs:24–46,103–104,131–134` distinguishes parameter coding (§7.1.1), a/c MUTUAL AUTHENTICATE (§7.6) and b INTERNAL AUTHENTICATE (§7.7). Request references are §7.6.2.3 for a/c and §7.7.2.3 for b, including the latter's explicit agreement modifications. Response references are §7.6.3.1/Table 7-18 for a/c and §7.7.3.1/Table 7-22 for b. The returned SD key is static for c and ephemeral for a/b. `CoreScpAuditReproTests.cs:142–161` correctly limits its malformed-tag cases to b. `ScpState.cs:32–35` now cites §6.2.3 for the SCP03 EXTERNAL AUTHENTICATE zero chain. |
| **R1 — SCP03 response prefix** | **Resolved.** `ScpState.Scp03.cs:40–48,69–77` and `CoreScpAuditReproTests.cs:123–139` describe 29 mandatory bytes and the conditional three-byte counter for pseudo-random challenge generation, using §7.1.1.6/Table 7-3. They distinguish the parser's minimum prefix from exact profile conformance. |
| **R2 — unknown-firmware test oracle** | **Resolved.** `ScpExtensionsTests.cs:163–181,202–223` now states that null firmware fails configured-processor admission and that its broad exception/conditional-message assertions do not require the current ArgumentException or prove handshake entry. The actual path is `PcscProtocol.cs:60,69–79` → `PcscProtocol.Scp.cs:27–37` → `ScpInitializer.cs:58–60`, before the support switch at `:64–86`. Names, assertions, and null-firmware policy remain unchanged. The path moves from retain to edit in the successor; FPT-073/FPT-079 retain their parameter-lifetime and stronger-oracle holds. |
| **R3 — security-level applicability** | **Resolved.** `ScpProcessor.cs:46,78–82,158–162`, `ScpState.cs:73–77`, `ScpEmptyResponseTests.cs:9–14`, and successor `citations.md:48–65` retain the selected-R-MAC condition and the C-DECRYPTION plus C-MAC condition. `ScpInitializer.cs:31–32` selects 0x33. The inspected release permits other levels; errors remain status-only and genuine EXTERNAL AUTHENTICATE remains the exception. The local instruction-only INS82 exemption is still explicitly qualified, not repaired. |
| **R4 — agreement versus KDF widths** | **Resolved.** `Scp11X963Kdf.cs:21–24` now separates two 32-byte agreements, the 64-byte concatenation, and the 80-byte X9.63 output. Actual length checks/copying at `:155–163` and the `5 * 16` request/slices at `:48–69` support those widths and five-value assignment. No cryptographic computation was performed. |

All original candidate/proposal identities survive: B5 C001–C009, B6 C001–C016 and FPT-058–082.
The successor citation addendum supersedes only the identified R1–R4 passages; the original imprecise
citations and original review remain immutable historical inputs, not current corrected authority.

## Original passages checked

The consequential passages were checked against text extracted directly from the retained PDF bytes,
not merely against the editor's citation table:

- **SCP03 1.1.2 Public Release, March 2019:** §6.2.3, printed p. 24, explicitly sets the EXTERNAL
  AUTHENTICATE MAC chain to 16 zero bytes; §6.2.5, pp. 26–28, distinguishes status-only errors and the
  EXTERNAL AUTHENTICATE exception and explicitly says R-MAC is optional; §6.2.6, p. 29, expressly
  requires both C-DECRYPTION and C-MAC before its counter/no-encryption-for-absent-data rules apply;
  §7.1.1.6/Table 7-3, p. 34, lists mandatory fields and the conditional counter; §7.1.2.3/Table 7-6,
  p. 35, identifies the selected security-level bits.
- **SCP11 1.3.0.13 Public Review, August 2023:** §7.1.1/Table 7-2, p. 45, is parameter coding;
  §§7.6–7.7 distinguish the commands and variants. §7.6.3.1/Table 7-18, p. 58, lists the a/c returned
  keys and receipt; §7.7.2.3/Table 7-21, p. 61, explicitly incorporates a/c processing with b's own
  agreements; §7.7.3.1/Table 7-22, p. 62, lists b's ephemeral SD key and receipt. SD/host roles,
  mandatory/conditional fields and the retained first-protected-command authentication qualification
  have not been generalized into verified final-profile conformance.

PDF SHA-256 values remain `973a10834f7fe8befd98da9af65393622234b80745092bd85248804c57b648dd`
and `5db47b3df3c4e565a5ad91257395604f3482ed5db70c64853544c3b0e289fde0`, respectively.

## Transfer and preservation checks

- All **91** successor base/current raw file hashes match the path records. The population is
  **57 edited / 22 retained / 12 held**. Exactly eight paths differ from the original reviewed subject;
  only `ScpExtensionsTests.cs` changes disposition, from retain to edit. All 12 held paths stay held.
- The complete transfer equals the live full-index product diff byte-for-byte. Its **49 unchanged file
  diff blocks** equal the original reviewed blocks; the other eight are the focused corrections read
  above. Visual delta inspection and a limited comment/inline-comment comparison find no executable
  change. This comparison is not a C# lexer or the coordinator's stronger preservation gate.
- Original editor records, `edit-protocols.md`, and both original review files retain their exact recorded
  hashes. Original review hashes remain `5f831b0ca0b48c7ffbb068719b51c58e86b3e1e41e390674d2ad45adae64ab79`
  and `53c1df7dd500fdb3d61856a2408c0b11d2734f546e75948f44b9b0e3fb4a5bc2`.
  Snapshot **text** comparisons explicitly normalize BOM/newlines; raw-byte claims use raw hashes,
  not re-encoded normalized snapshot text.
- The complete example and its classification remain unchanged; helper body SHA-256 remains
  `3e0457b3fedca0f971d59cfbff209e3cd61308f507e8b9283a066f79c5a201aa`.
  `../examples.json` and `../outside-owner-handoffs.json` remain byte-identical and applicable.
  Original indices, inherited association keys and D/M/P/E/Q/B facets are preserved. No new outside-owner
  referral or acceptance is introduced. Product `git diff --check` exits 0.

## Acceptance boundary and deferred work

This is a **content/citation PASS for the full successor batch**, not completion of the coordinator's
compiler-bound token/import/directive/signature/default/attribute/assertion/runtime-string/API-snapshot
preservation or meaningful example/cref compilation gates. Those remain pending and must use these
exact successor identities.

All original implementation/policy and qualification deferrals remain: empty-command encryption,
counter mutation/notification, INS82 phase admission, copied plaintext/response ownership, protected
chaining, pre-state-return cleanup, parameter aliases, test-harness cleanup, ATR C016/FPT-082, final
SCP11 1.4/vendor profile, uninspected SCP03 1.1.2.6 review clauses, ISO/NIST/capture gaps, actual native
producer/provider/physical release, PIV physical deferrals and the future user-ready Windows delta.
Serial1256A88's two 0x6A88 setup failures remain **before EXTERNAL AUTHENTICATE and RESET**; plaintext
reset success adds no secure-reset evidence. Historical serial9681620 remains unexplained.

Only this revision's `review.md` and `review-identities.json` were written. No product/editor-record
changes, new validation framework, full re-audit, SDK build/test/formatter, protocol cryptography,
provider/native/device execution, Git mutation or subagent occurred. After the coordinator's existing
gates, finish Core Pass 1 publication with the explicit deferred handoff, then stop.
