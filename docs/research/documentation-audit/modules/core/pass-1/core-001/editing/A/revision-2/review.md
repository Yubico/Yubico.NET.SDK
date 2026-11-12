# Packet A revision 2 — focused independent re-review

## PASS — full successor documentation batch

**R1 and R2 are satisfied.** The full 37-file successor documentation batch is accepted at product-patch
SHA-256 `028c2a45a04d8522cf6211a6d76724e8ce0c84b3dc6a32dfdc128f584dcf7228`, with the exact path
hashes in this revision's `changed-path-sha256.json` and `path-dispositions.json`. No further content
correction is required by this review. This is documentation/content acceptance, **not** compiler,
token/API/cref binding, example compilation, runtime qualification or whole-Core publication.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-discovery`.
HEAD/base: `da66f5d01ba594993974ddd26723e8c300c0a454`.
Original source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.

## Focused correction decisions

| Correction / candidates | Accepted current wording and source reasoning |
| --- | --- |
| **R1 — CORE-A-C010/C011; FPT-010/011** | `CompositeDeviceMerger.cs:224–236` now explicitly permits the same stale group to win repeatedly and states that the rule has no age/expiry mechanism. The complete `Merge`, PID count/completeness checks, topology grouping, `ResolveContestedSerials`, serial/deduction and publication helpers were re-read. The winner depends on this scan's anchored censuses, not elapsed scans; the revised rationale accurately preserves the heuristic and same-key identifier distinction without guaranteeing one-scan expiry or convergence. |
| **R1 — the two partial-evidence vectors** | `CompositeDeviceMergerVectorTests.cs:253–299`, including both complete test bodies and the descriptor/member/slot helpers at 773–795, now explains persistence and makes regrouping depend on evidence the implemented tiers actually use. The complete PID-count-one shortcut at `CompositeDeviceMerger.cs:108–114` consults connection completeness and cross-PID contested serials; `CanMergeByPidWithoutSerial:192–198` does not test same-PID serial disagreement. The comments therefore correctly say that different same-PID serials alone need not bypass the shortcut. Both tests remain single-scan member-set oracles, not demonstrations of healing. |
| **R2 — CORE-A-C009; FPT-009** | `WindowsDeviceTopologyResolverTests.cs:211–222` now describes this SDK's current null-resolver choice and the actual false/null assertions, rather than absence of OS mapping APIs. The selector test at 224–235, its local helper context, and complete `NullDeviceTopologyResolver`/`DeviceTopologyResolver` implementations were checked: the null resolver returns false/null and the factory currently selects it off Windows. Successor path/candidate dispositions correctly mark this file **EDIT**. The earlier inaccurate retain rationale is not carried forward. |

Accepted corrected file identities:

| File | Current SHA-256 |
| --- | --- |
| `src/Core/src/Devices/CompositeDeviceMerger.cs` | `232ca280c1b12bb29fcbc34b783bdfcab9dc6786c287ed848fcf82d8ad79e4a2` |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CompositeDeviceMergerVectorTests.cs` | `77ca59e5a983ee204198c65d1f95647fa52234ba216e1b8e213c3f28c6c6ab03` |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/WindowsDeviceTopologyResolverTests.cs` | `a6e12a8ff10cb6309fcf3c2f3b1280307c30f81be87bf0797369dd469015328c` |

The corrections retain useful rationale and precise local conditions in the approved calm engineering
voice. They change no grouping policy, assertions, test names or runtime failure text. No optional style
churn is requested.

## Independently verified identity and executable-preservation boundary

Fresh read-only checks, separate from the editor's receipt, confirmed:

- The actual full base-to-current product diff, with explicit `a/` and `b/` prefixes, is byte-identical
  to the successor `product.patch` at the accepted hash above.
- The original reviewed product was reconstructed **in memory** from the base and immutable predecessor
  patch, and every reconstructed path hash matched the predecessor path manifest. Comparing it with
  the checkout found **exactly the three correction paths**. The correction delta was inspected directly:
  it changes comment/XML lines and adds the recorded final newline to the previously retained topology
  test file. No executable change was found.
- All **90** packet C# paths match current, predecessor and source/base manifests. Base product bytes
  equal the original source pin. Exact filtered non-comment lines match both predecessor and original
  base across all 90, preserving imports, directives, declarations, defaults, assertions and runtime
  strings. This check plus source reading is not substituted for the coordinator's token/compiler proof.
- The other **34 accepted edited hashes** and **12 supported candidate-associated retained hashes**
  remain unchanged. Current totals are **37 EDIT / 53 RETAIN**. The other 41 inventory-retained paths
  retain their previous identity-only scope; this focused return does not newly content-audit them.
- All **13 immutable predecessor editor/review files** match their frozen identities, including the
  original review hashes independently retained from the first review. All listed successor artifact
  hashes and the original input identities also match. No prior review, citation original or editor
  evidence was rewritten.
- All **21 candidate identities**, proposal mappings, facets, inherited aliases, held triggers and
  out-of-scope dependencies are preserved. Original evidence-reference unit-ID sets match the immutable
  predecessor records; path decisions and current hashes join to the successor path manifest.
- The three example records are byte-identical to their predecessor; current body extraction and each
  body hash still match. No example gained compilation or execution credit.
- **187 XML blocks** across the 37 edited paths parsed as well-formed XML; scoped `git diff --check`
  exited **0**.

Exact accepted successor and predecessor identities are recorded in `review-identities.json`. Acceptance
combines this bounded correction review with the prior review's unchanged accepted content/citation
scope; it does not re-review unrelated material or create new source authority.

## Remaining coordinator gates and handoff

The coordinator's existing compiler/token/API/cref and compiled-example checks remain later gates.
Examples remain **VALID-NOT-CHECKED / NOT-RUN**. FPX-01/A-E01 and A-E02 through A-E06 remain
unexecuted. All prior aliasing/post-disposal, cache-expiry, FIPS/wide-field, logger, native/physical/profile,
cross-process, historical-failure and Windows-delta qualifications remain in force. No implementation
facet, applet boundary or shared narrative is closed merely by this PASS.

Only this revision's `review.md` and `review-identities.json` were created by the reviewer. No product
edit, build, test, formatter, compiler, crypto/native/device execution, Git mutation or subagent occurred.
Proceed through the existing Core Pass-1 final validation and agreed handoff, then stop; no new
framework, broad reinvestigation or Pass-2 work is requested.
