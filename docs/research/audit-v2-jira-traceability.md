# v2 audit: Jira-to-merged-work traceability

Snapshot: 2026-10-30. Reviewed local `yubikit` at `ddf3cdc2` (also `origin/yubikit` at inspection), live Jira issue metadata, and GitHub PR metadata.

## Scope and interpretation

This is a **mention/disposition pass**, not a correctness or regression-test verification. No tests were run and no Jira issues were changed. A fixing commit's presence or a PR's claim does not establish that a vulnerability is fixed.

Jira parent `YESDK-1608` has 28 direct children. `YESDK-1633` and `YESDK-1634` have another 26 subtasks, included below so the umbrella issues do not conceal missing coverage. All 54 issues returned in Backlog. These are type Bug, not Security bug.

- **Claimed remediation:** a merged commit or PR specifically describes the corresponding change (including intentional removal or documentation).
- **Deferred/residual:** a recorded disposition exists, but it is not a complete fix claim.
- **No specific remediation found:** no explicit fixing/disposition match beyond the general out-of-scope record. This is not proof that current code remains vulnerable.

Subtask mappings use issue titles and the audit's 25a–25i / 26a–26q identifiers, not necessarily literal Jira-key mentions. The queried umbrella/exception tickets had empty descriptions and comments, so a later semantic pass needs the original audit detail as well.

## Merged PR evidence

All six PRs have GitHub state MERGED, base `yubikit`, and merge commits confirmed as ancestors of the inspected local `yubikit`.

| PR | Content | Merge commit | Merged (UTC date) |
| --- | --- | --- | --- |
| [#698](https://github.com/Yubico/Yubico.NET.SDK/pull/698) | Main `audit/v2-fixes` work | `8cc9db85` | 2026-10-23 |
| [#707](https://github.com/Yubico/Yubico.NET.SDK/pull/707) | OATH device identity, 26k | `95ede1a6` | 2026-10-27 |
| [#708](https://github.com/Yubico/Yubico.NET.SDK/pull/708) | OpenPGP key import, 26i | `3f290f96` | 2026-10-27 |
| [#709](https://github.com/Yubico/Yubico.NET.SDK/pull/709) | OpenPGP KDF, 25h/26q | `91361eaf` | 2026-10-27 |
| [#710](https://github.com/Yubico/Yubico.NET.SDK/pull/710) | CI hardening, #19/N4 | `7e1dffbc` | 2026-10-29 |
| [#711](https://github.com/Yubico/Yubico.NET.SDK/pull/711) | Missing SCP R-MAC, N2; additional finding without a Jira key in its PR | `db301466` | 2026-10-27 |

The existing [close-out snapshot](audit-v2-close-out.md) predates these merges. Its statements that child PRs await landing are historical, not the current merge state. Its residual dispositions remain relevant evidence, not newly verified conclusions.

## Direct children of YESDK-1608

Unless qualified otherwise, rows record claimed remediation only.

| Jira | Finding | Merged evidence / disposition |
| --- | --- | --- |
| YESDK-1609 | PRF to hmac-secret | #698, `6ef013ae`, `0e04532e`; PR explicitly records UV-policy divergence |
| YESDK-1610 | Inconsistent required UV request | #698, `d4692cec` |
| YESDK-1611 | Invalid direct largeBlob write shape | #698, `fac30e75`; unsupported direct extension removed |
| YESDK-1612 | Incomplete/inconsistent extensions | **Deferred:** #698 explicitly excludes unsigned getAssertion outputs; `eb7d262a` removes its repro after firmware measurement; close-out row #4 |
| YESDK-1613 | WebAuthn largeBlob result path | #698, `fa2b1426` |
| YESDK-1614 | Attestation=None forwards attestation | #698, `7ecc8482` |
| YESDK-1615 | residentKey=preferred becomes false | #698, `844bf545` |
| YESDK-1616 | topOrigin without crossOrigin | #698, `207d3e04` |
| YESDK-1617 | Ignored ceremony timeout | #698, `0d9529e8` |
| YESDK-1618 | makeCredential up=false | #698, `ff428d1f` |
| YESDK-1619 | credBlob retrieval CBOR type | #698, `bc3e1730` |
| YESDK-1620 | Fragment length | #698, `74ab341b` (subject abbreviates this key as `-1620`) |
| YESDK-1621 | Incompatible large-blob format | #698, `74ab341b` |
| YESDK-1622 | Corrupt checksum handling | #698, `74ab341b` (abbreviated `-1622`) |
| YESDK-1623 | Nonstandard RP-ID override | **No specific remediation found.** Close-out excludes original audit #15; no literal key match in reachable commit messages or the latest 200 merged PRs targeting yubikit |
| YESDK-1624 | Plaintext retention | #698, `74ab341b` (abbreviated `-1624`) |
| YESDK-1625 | Private-key intermediate copies | **Prior SDK fix plus runtime residual:** original audit explicitly records SDK-owned copies fixed in `65e36386`, before this audit-fixes branch. #698, `6ff76c34` documents the remaining macOS runtime scalar copy; close-out #17 says upstream issue draft not filed. Do not treat this as a complete fix |
| YESDK-1626 | Unmanaged-memory leak | **No specific remediation found.** Close-out excludes original audit #18; no literal key match in the same history/PR search |
| YESDK-1627 | CI installer/token trust | #710, `1e03e662`, `9ba429ba`; PR explicitly limits integrity coverage and leaves patch symlink/mode checks as follow-up |
| YESDK-1628 | Truncated long-form TLV headers | #698, `ed9cd735`; combined with 25e |
| YESDK-1629 | Undersized device-info TLV values | **No specific remediation found.** Close-out excludes original audit #21; no literal key match in the same history/PR search. Pagination fix 25b is not evidence for this distinct parser issue |
| YESDK-1630 | Cross-process PIV PIN state | #698, `c15c5146`, `4c0d4c89`; PR explicitly records shared-fallback exposure |
| YESDK-1631 | Short SCP response host crash | #698, `169d3694`, `668f8217`, `77bee74a`; #711 is additional N2 hardening, not a substitute for this mapping |
| YESDK-1632 | Ed25519 message mutation | #698, `8a7860fa` |
| YESDK-1633 | Numeric-boundary umbrella | #698 and #709; all nine Jira subtasks mapped below |
| YESDK-1634 | API-footgun umbrella | #698, #707, #708, #709; all 17 Jira subtasks mapped below, including non-defect/documentation dispositions |
| YESDK-1635 | Mismatched private/public components | #698, `39a519e6` |
| YESDK-1636 | Malformed credential breaks OTP batch | #698, `dbe1bc06`; also 25i |

## Numeric-boundary subtasks (YESDK-1633)

| Jira | Audit item / finding | Claimed remediation evidence |
| --- | --- | --- |
| YESDK-1637 | 25a: negative password length | #698, `30add9ec` |
| YESDK-1638 | 25h: OpenPGP KDF count | #709, `dac49a38` |
| YESDK-1639 | 25b: device-info pagination wrap | #698, `3645b20f` |
| YESDK-1640 | 25g: nonpositive WebAuthn batches | #698, `6476153d`, `94e95e57` |
| YESDK-1641 | 25i: invalid OATH periods | #698, `dbe1bc06` |
| YESDK-1642 | 25c: large RNG intervals | #698, `78c44513`; helper removed in favor of runtime API |
| YESDK-1643 | 25d: HKDF counter overflow | #698, `9ef6588d`; runtime HKDF replaces implementation |
| YESDK-1644 | 25e: BER-TLV length overflow | #698, `ed9cd735` |
| YESDK-1645 | 25f: TLV encoder underallocation | #698, `a8066061` |

## API-footgun subtasks (YESDK-1634)

| Jira | Audit item / finding | Claimed remediation or recorded disposition |
| --- | --- | --- |
| YESDK-1646 | 26g: redirected input policy | #698, `958b63ef` |
| YESDK-1647 | 26h: confirmation buffer cleanup | #698, `d184fc19` |
| YESDK-1648 | 26i: OpenPGP import target | #708, `9e4992b8` |
| YESDK-1649 | 26e: retry limits reset PIN/PUK | #698, `8c6640f7`; documentation disposition, not removal of device behavior |
| YESDK-1650 | 26j: attestation overwrites certificate | #698, `7e958c38`; documentation disposition |
| YESDK-1651 | 26k: OATH credential identity | #707, `160587eb` |
| YESDK-1652 | 26l: discarded OTP protection setting | #698, `66bf43f8` |
| YESDK-1653 | 26m: empty certificate allowlist | #698, `2865c5cc` |
| YESDK-1654 | 26a: ignored credential-specific PRF inputs | #698, `0e04532e`, with selection work `2acd0989` |
| YESDK-1655 | 26c: previewSign attestation substitution | #698, `94f94e87` |
| YESDK-1656 | 26n: mutable configuration builder | #698, `cee665c3`; builder replaced |
| YESDK-1657 | 26b: PRF enabled value | #698, `0e04532e` |
| YESDK-1658 | 26o: OATH retry seed becomes zeros | **Not-a-defect disposition:** #698 and close-out explicitly record it; not a fix claim and not a Jira closure |
| YESDK-1659 | 26p: HMAC padding collisions | #698, `ccb8753f`; original collision premise attributed to device semantics, empty-input parity changed and behavior documented |
| YESDK-1660 | 26f: EC/RSA input formatting | #698, `8a7860fa` |
| YESDK-1661 | 26q: KDF count excludes PIN | #709, `dac49a38` (PR labels its Jira umbrella as YESDK-1633 but explicitly covers 26q) |
| YESDK-1662 | 26d: previewSign selects first entry | #698, `abbf00bc` |

## First-pass conclusion

- 23 of the 28 direct children have specific merged remediation mentions, including the two umbrellas and changes that intentionally remove APIs or document behavior.
- One is explicitly deferred (1612), one has a documented runtime residual rather than an established complete fix (1625), and three lack a specific remediation match (1623, 1626, 1629).
- All 26 discovered subtasks have an individual change/disposition mapping; that includes documentation-only and not-a-defect outcomes, not 26 claims of bug fixes.
- Merge ancestry is confirmed. Historical test claims have only been read, not re-run or endorsed. No issue is ready to close solely on this report.

Next pass: obtain original audit detail for the exceptions, verify each claimed behavior against current code and regression tests, and decide Jira dispositions only after reviewing that evidence.

## Subsequent evidence review

The original report was located in the Sunthings vault. It has 28 top-level findings (including grouped #25/#26) and seven non-finding considerations; 54 above is a Jira-record count, not a count of distinct audit findings. See [close-out evidence review](audit-v2-closeout-review.md) for scope reconciliation, the current-code gaps, and the incomplete N4 monthly-migration workflow coverage. The mention-only results above remain historical traceability, not closure recommendations.
