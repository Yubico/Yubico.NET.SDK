# Astra OpenPGP boundary sample verdicts

Read all 22 complete pinned units in `read-boundary-OpenPgp.md`. Final current-source mapping and
clean-credit reconciliation remain pending; no device or cryptographic operation was executed.

| Unit suffix | Verdict and scope |
| --- | --- |
| `869f7f727a2bacd9` | Supported: metadata selects EC algorithm identifiers while card/key-slot applicability remains caller-owned. |
| `d5e9bd9e80ac58c8` | Supported: attribute-switch deletion explicitly retains destructive partial-state and NEO qualifications, followed by cache refresh. |
| `873ce41195e42542` | Supported: read-existing P1=81 path returns response data; existing ATT-read findings remain open. |
| `758748a063d82837` | Supported: positive requested length becomes Le; no exact returned-length or randomness-quality guarantee inferred. |
| `c7086050193e168d` | Supported: command scopes its opened session and converts caught exceptions to the displayed failure result. |
| `bcb85f9e4e5ea743` | Supported: the failed OID lookup maps to the shown ArgumentException. |
| `4dbf70904bbff16f` | Supported: private constructor delegates connection/prompt initialization to the base. |
| `8b2a95f01db9bf2c` | **SAMPLE-PGP-001:** advisory UIF fallback calls the logger before choosing PolicyMayRequire; a throwing provider can interrupt it. The unconditional comment needs qualification. Isolated editor correction assigned. |
| `710814862611f595` | **SAMPLE-PGP-001:** the matching guide's unconditional failed-read fallback statement needs the same logging-success condition. Zero clean credit. |
| `33e76ca38aabdeb3` | Supported within existing algorithm/profile evidence: P-256 helper checks template/point representation, scopes the verifier and session, borrows PIN/message, and clears its returned signature. Trust is explicitly separate from card key retrieval. |
| `14baa3192f491ff3` | Supported: test asserts RSA attribute width and nonempty returned public bytes; no independent RSA operation or physical execution inferred. |
| `62fb7fa48b9550d8` | Supported: successful path changes, verifies, restores and verifies PIN. No unconditional restoration after earlier failure or all-buffer cleanup claim accepted. |
| `6295ba49ce88cf2b` | Supported: host generation inability maps to explicit test skip; no device support inferred. |
| `c8d2b18f767cdae2` | Supported: captured import APDUs are joined and parsed into the stated CRT/header/value components; no all-sensitive-copy erasure claim. |
| `9a2795debe7eb7e0` | Supported: assertions identify SELECT, GET VERSION and application-data requests among captured commands. |
| `8a438c950ba16617` | Supported: cached UIF mapping, absence of GET DATA, prompt context identity and command counts are explicitly asserted. |
| `15b19a2931c6434a` | Supported: fake attestation failure checks Att scope and failed outcome; this does not resolve the existing actual attestation-read defect. |
| `24e90797dbd9ae65` | Supported: scripted mutation refresh clears the prior local UIF override as shown by no notifications; not physical persistence evidence. |
| `683eb679b38bf0b7` | Supported: exact resolution exception identity and recorded Completed outcome are asserted; successful decryption result delivery is not implied. |
| `f654c14528cd489f` | Supported as a synthetic app-data fixture: temporary TLVs are disposed in the shown protected scopes. No full card-format conformance or all-allocation-failure cleanup inferred. |
| `aceb51379b5ee76b` | Supported within the actual oracle: value length and ordered header tags are checked, not every emitted component byte. |
| `7ad9dd6d6d1845d6` | Supported: application-session interface inheritance declaration. |

All IDs have prefix `core-001:unit:`. The two SAMPLE-PGP-001 units receive zero final clean credit
and require expansion of their implementation and narrative strata. Existing PIN/KDF/X25519/ATT
engineering findings, profile limits and historical evidence remain unchanged.
