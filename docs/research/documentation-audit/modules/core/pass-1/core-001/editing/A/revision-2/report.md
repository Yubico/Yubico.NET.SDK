# Packet A revision 2 — bounded R1/R2 corrections

## Result and immutable predecessor

R1 and R2 are verified against the complete merger, vector-test and topology-test files and the current
null-resolver implementation. Both corrections are supported. Only the three C# paths below changed
relative to the independently reviewed editor batch. All executable lines, assertions, method names,
imports, directives, runtime strings and defaults remain unchanged.

The worktree remains `/home/rupicola/Code/y/yubikit-core-docs-edit-discovery`, branch
`yubikit-core-docs-edit-discovery`, HEAD/base `da66f5d01ba594993974ddd26723e8c300c0a454`.
Source comparison pin remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.

The original `edit-A.md`, all initial `editing/A/*` files and the independent review are immutable
predecessors, not rewritten current-state manifests. In particular, the initial product patch remains
SHA-256 `28cfedee32cba340d041a90edadd79afff3d65ced04317a8863d43854890a517`.
`predecessor-identities.json` captures all 13 original editor/review files before these corrections;
`verification.json` records their fresh byte-preservation check afterward. The original R2 retain
rationale is superseded here, not carried forward as an accepted retain.

## Verified rationale and exact corrections

| Review / original candidates | Corrected file | Source reasoning and resulting wording |
| --- | --- | --- |
| R1 / CORE-A-C010, CORE-A-C011 / FPT-010, FPT-011 | `src/Core/src/Devices/CompositeDeviceMerger.cs` | `ResolveContestedSerials` derives its result solely from the supplied descriptors: a serial must appear under multiple PIDs, and exactly one anchored PID census must match its expected connections to win. No age, expiry or next-scan state exists. Repeated stale-complete/fresh-incomplete evidence can select the same stale group repeatedly. The comment retains completeness as a heuristic, the same-physical-key identity rationale, dead-member connection failure and standalone ambiguity; it removes the one-scan bound and conditions regrouping on evidence used by the implemented tiers. |
| R1 / CORE-A-C010, CORE-A-C011 / FPT-010, FPT-011 | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CompositeDeviceMergerVectorTests.cs` | `Merge` takes the PID shortcut when count is one, the connection set is complete, and no member's serial is contested across PIDs. `CanMergeByPidWithoutSerial` compares connection bits, not differing serials within that PID group. Both partial-evidence tests invoke one merge and assert the resulting count, identifier, connection flags and cross-key members. Comments now state that misattribution can persist and that adding different same-PID serials alone need not change the shortcut. No first-subsequent-scan or automatic serial-scan healing is promised; the exact single-scan oracle remains explicit. |
| R2 / CORE-A-C009 / FPT-009 | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/WindowsDeviceTopologyResolverTests.cs` | `NullResolver_AlwaysReportsUnknown_PinnedPlatformBound` calls `NullDeviceTopologyResolver.Instance` for SmartCard and FIDO, then asserts false and null. The complete null resolver sets the output to null and returns false; the selector currently chooses it off Windows. The corrected comment describes this SDK selection and these false/null results, rather than asserting that macOS/Linux mapping APIs do not exist. Test name, selector, seams and assertions are preserved. |

This is static source reasoning, not an executed counterexample, a new policy decision, or an additional
implementation finding. The complete topology test context and vector helpers were read; no broader
rewrite was made. The other 34 previously edited paths remain byte-identical to their accepted review
hashes. The other 12 candidate-associated retained paths remain byte-identical with their supported
retain dispositions. The other 41 inventory-retained paths receive identity/preservation checking only,
not a new content acceptance claim.

## Successor artifacts and transfer boundary

All paths in this table are within `editing/A/revision-2/`.

| Artifact | Scope |
| --- | --- |
| `product.patch` | **Current full transfer diff from the original base**, covering all 37 edited C# paths, not just the three correction paths. Explicit `a/` and `b/` prefixes. |
| `changed-path-sha256.json` | All 37 edited paths, source/base/previous-reviewed/current hashes and current edit dispositions. |
| `path-dispositions.json` | All 90 permitted C# paths: 37 edited, 53 retained; R2 is explicitly EDIT. Previous and current hashes allow unchanged acceptance to be checked precisely. |
| `candidate-dispositions.json` | All 21 original candidate/FPT/alias identities, revised per-path decisions and focused review needs. Original evidence units are referenced by the preserved predecessor candidate-record identity rather than duplicated or relabeled. |
| `correction-paths.json` | The three corrected files, R1/R2 links, exact previous/current hashes, current XML-block boundaries and preservation result. |
| `example-bodies.json` | Byte-identical successor copy of the original three example records, with independently rechecked current bodies, boundaries and hashes. Classifications remain unchanged. |
| `predecessor-identities.json` | Frozen identities of all original editor/review files captured before correction. |
| `verification.json` | Fresh static checks, cumulative product-patch hash and current counts; no compiler or runtime claim. |
| `output-identities.json` | All successor artifact hashes, excluding itself to avoid a self-hash cycle. |

`verification.json` is the authoritative successor patch SHA-256 receipt; the original patch and its
review remain preserved beside this successor directory. No original generator was rerun, because it
would overwrite the initial evidence. The successor records use the same full-line-comment comparison,
XML parsing and example extraction without introducing a new checker/framework.

## Focused reviewer handoff and stopping point

Re-review the three corrected comment regions with `Merge`, `CanMergeByPidWithoutSerial`,
`ResolveContestedSerials`, the two complete partial-evidence tests/helpers, and the complete null-resolver
test/selector context. Confirm that persistence is conditioned on grouping inputs actually consulted,
not merely elapsed scans or any serial observation. Confirm that R2's manifest disposition is EDIT and
that the 34 accepted edited hashes and 12 supported retained hashes are unchanged. Whole-batch successor
acceptance remains pending that focused review and the coordinator's existing final validation gates.

The three example identities remain:

- Internal scope expressions: `9ff8535b320dc909b8670d626b05c18dc8e362ec80c1c1c5237b6a631c46811e`.
- Internal match expressions: `a6daf07849fb4729187f4c98a5d3b471a34ed7216a69276cd8f0350ec448fb20`.
- Complete monitor method: `d1620db72041e4a0b935b9fe662bca97261788f471db337171739bd7c66870bf`.

All remain `VALID-NOT-CHECKED / NOT-RUN` for compilation/execution. FPX-01/A-E01 and A-E02 through A-E06
remain unexecuted. All original native/physical/profile/cross-process/historical-failure qualifications,
Windows-delta review, PIV physical deferrals and shared-document recommendations remain in force.
No SDK build/test, formatter, compiler, native/device/crypto execution, Git mutation or subagent ran.
After the coordinator completes Core Pass 1 and its agreed handoff, stop; no Pass-2 work is introduced.
