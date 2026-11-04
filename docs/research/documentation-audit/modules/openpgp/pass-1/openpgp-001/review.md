# Independent OpenPGP content and original-citation review

**Reviewer:** fresh GPT-6.1 Sol session, 2026-11-04. **Baseline:**
`fe180282a2d77b15eaaa5868d6c8b00e251d1f05`.
**Verdict: changes requested — five bounded documentation corrections.**
The remaining reviewed content is accepted for its recorded scope. This is neither a compilation gate
nor final hardware/evidence closeout. No product file was edited by this reviewer.

## Scope and identities

Read root/module/test instructions, testing guidance, campaign workflow/style, SDK house style, RUN
plan/findings/investigation/coverage/edit-report and original-source analysis. Independently read the
complete affected library/model, example and test units, including implementation bodies, inherited XML,
resource scopes and actual assertions. Reviewed all 58 authored product paths: 48 C# comment/XML files,
two module Markdown files, seven shared-document OpenPGP sections and one package Description.
The coordinator's expanded clean-API sample subsequently added the complete `GenerationTimes.cs` unit
for the timestamp correction below; the original authored snapshot remains 58 paths until that edit.
Shared reads were limited to assigned OpenPGP sections and immediate context. No Core or other-module
source/API/documentation investigation, builds, tests, formatter, hardware, commits or delegation occurred.

Reconciled the 40 interface operations with their seven concrete partials and 39 effectively public types.
Internal templates/import/backend members remain internal; extension/accessor/generated representations
are not extra prose tasks. Independently compared all 48 changed C# token streams with the specified
baseline: all match after removing comments and whitespace, including executable strings, imports,
assertions and directives. This lexical check establishes preservation, not compilation.

All 58 working-byte hashes match the editor checkpoint. The ordered compact path/raw/body manifest
digest independently reproduces `4b662c3884c9526c2cfce00c037b3e9b28588a17e7ae351e6e0d3343342850d0`.
`evidence/review-product-snapshot.json` records every path, raw identity and acceptance exceptions.
The README is `a9d28e98804316fce007626cc7b579554023c964b882f13ccad44c28af5ed0b5`.
All twelve complete C# groups were read, including all 197 body lines, helper, callbacks and cleanup scopes;
their independently recalculated identities match the editor inventory. They are content-reviewed,
not certified compiled by this session. See the snapshot for exact snippet hashes.

## Required corrections

### PGP-REV-001 — Remove the remaining typed-zero/blocking implication

**Required factual/safe-use correction; C001/C019, PGP-IMP-001/019.**
`src/OpenPgp/src/IOpenPgpSession.cs:129–133` still describes retries as
“0 if the PIN is now blocked.” That implication contradicts the preceding remarks and the real
extended-mode fallback: Reset Code attempts can be zero while User attempts remain two.
`src/OpenPgp/src/OpenPgpInvalidPinException.cs:48–52` similarly describes the property as the number
of attempts before “the PIN” is blocked without its local identity caveat.

Describe a reported/extracted count, identify explicit 63Cx/6983 versus 6982 fallback where useful, and
retain the actionable fresh `AttemptsUser` inspection for extended failures. A safe local exception
description is: “Reports the available retry count, or null when unavailable. Extended-mode 6982
fallback currently selects Reset Code attempts; inspect fresh User status before deciding recovery.”
The Admin exception text can remain appropriately Admin-specific. Keep every executable token unchanged.

**Evidence:** full `VerifyPwAsync`/`ResolveRemainingRetriesAsync` source; Card 3.4.1 §§7.2.2/7.9;
existing fake distinct-counter test; coordinator's stronger real-device User 3→2 / Reset 0 / typed 0
failure and distinct-salt correct-PIN failure. This is a consequential recovery decision, not a wording preference.

### PGP-REV-002 — Carry the corrected PIN-policy lifetime into the API summary

**Required factual/usefulness correction; C018.**
`src/OpenPgp/src/IOpenPgpSession.cs:270–272` still calls the policy “once per session,” although its
remarks and the edited `PinPolicy.Once` correctly describe card access state. Summary-only API views
therefore preserve the exact misconception the edit was meant to remove.

Use, for example, “Sets whether signature authorization is consumed after each signature or retained
while the card access state remains valid.” Preserve the nearby verification and invalidation conditions.
Card §4.3 and §7.2.2 bind this to reset/application selection/specific commands, independently of a host
object lifetime. The tool's unchanged executable display string remains a separately documented sample
limitation; this correction authorizes no runtime-string edit.

### PGP-REV-003 — Correct the retained 6A82 test explanations

**Required factual/oracle correction; C016/C019, PGP-IMP-016/019.**
`src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpInvalidPinExceptionTests.cs:215–217`
labels simulated `6A82` as “Referenced data not found” and concludes that no KDF is configured.
`src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs:728–730` likewise calls
that simulated response “KDF DO absent.” Card §7.9 assigns `6A82` to file/application not found and
`6A88` to referenced data/DO not found. The implementation catches **any** F9 APDU error and caches
`KdfNone`; those fake bytes demonstrate that broad fallback, not actual KDF absence.

Correct comments only: name `6A82` accurately and state that the current broad error fallback sends
raw PIN bytes through `KdfNone`. Preserve responses/assertions/helper names in this Pass 1. No source
or hardware behavior needs to change to fix the explanation.

### PGP-REV-004 — Make the reset summary describe the attempted sequence

**Required factual/safe-use correction; C005, PGP-IMP-005.**
`src/OpenPgp/src/IOpenPgpSession.cs:641–644` unconditionally says it performs a factory reset and
“Blocks both PINs, terminates, and reactivates the applet.” The new remarks correctly explain that
blocking is assumed, lifecycle statuses are ignored and a normal return establishes no complete
reset postcondition. Summary-only consumers should receive the same contract.

Use “Attempts the OpenPGP application reset sequence: PIN blocking, TERMINATE and ACTIVATE,” keeping
the SDK threshold and full independent-readback/ATT/partial-state remarks. This aligns with the edited
tool summary and README without promising a completed erasure.

**Evidence:** complete reset/blocking implementation, limited three-counter integration assertions,
and Card §§7.2.16–17. ACTIVATE can even return 9000 as a dummy command in operational state; command
issuance and SDK return are not a complete erasure oracle.

### PGP-REV-005 — Explain unspecified generation time and the signed carrier limitation

**Required factual/usefulness correction; coordinator PGP-C022, extension of PGP-IMP-018.**
The coordinator's expanded API sample identified a material clean-unit miss, independently confirmed
here. Card3.4.1 §4.4.1, printed p24, CD table (retained extraction927–930) says each field is four
big-endian bytes of seconds since1970 and default00000000 means **not specified**. RFC4880 §3.5
defines the OpenPGP time field as unsigned four-octet seconds since1970 UTC. Current
`DiscretionaryDataObjects.ParseGenerationTimes` combines four bytes into signed `int`: wire80000000
becomes int.MinValue, rather than a genuine negative/pre-1970 timestamp. `SetGenerationTimeAsync`
writes the supplied int's four bit-pattern bytes without a range/sentinel policy. Source prose must
describe that observed limitation without promoting negative epochs into an intended card contract.

Required dependent documentation units:

- `src/OpenPgp/src/GenerationTimes.cs:19–22`: zero means unspecified; signed int exposes raw high-bit
  wire values negatively. Preserve original bytes/bit pattern when inspecting the full unsigned range.
- `src/OpenPgp/src/DiscretionaryDataObjects.cs:71–74`: the property needs the same sentinel/carrier
  caveat locally or a useful cross-reference to the complete collection contract.
- `src/OpenPgp/src/IOpenPgpSession.cs` GetGenerationTimes/SetGenerationTime units: distinguish card
  time semantics from signed carrier, document zero clearing/unspecified metadata and four-byte write
  behavior; do not describe a high-bit result as an actual negative Unix timestamp.
- `src/OpenPgp/README.md:262–266`: preserve the separate metadata task and add zero's meaning plus
  the current signed-range limitation where readers choose/interpret generation dates.

A compact supported explanation is: “Zero denotes an unspecified generation time. The card stores
four big-endian bytes; this API currently exposes them as a signed int, so values with the high bit
set appear negative. Preserve the raw bits when inspecting those values rather than interpreting
them as dates before1970.” The editor can adapt this to each surface without duplicating an audit
transcript. No carrier/API/parser/assertion change is authorized in Pass1. Reopen after PGP-IMP-018
settles signed/unsigned representation, sentinel and validation/forward-compatibility policy.

The coordinator owns C022 triage, clean-denominator removal and further API-category expansion;
this review independently adjudicates the correction and does not replace that sampling record.

## Accepted content and editorial assessment

The guide supplies a coherent actor/task-first route through discovery, credential changes/recovery,
KDF provisioning, generation/import, metadata, algorithm-specific signing/verification, raw decipher,
certificate storage/attestation, policy and deliberate reset. Typed borrowed inputs and complete owned
session/private-key/certificate/result lifetimes make the composable fragments usable. The KDF example
keeps the aliased object alive through provisioning teardown, separately clears owned hashes/salts and
verifies ordinary User/Admin access in a fresh session. It explicitly qualifies distinct-salt extended use.

Memory guidance clears actual owned returned storage via `MemoryMarshal.AsMemory`, instead of an
ineffective new copy. Successful RSA/EdDSA transfer, ECDSA conversion/clearing and decipher copy/clearing
match their full local implementations. Borrowed model/static/input memory stays outside that recipe.
Current X25519 wrapper and malformed ATT CRT limitations have supported safer actions and remain defects,
not invented intended contracts. Test descriptions distinguish response shape, host cryptographic proof,
certificate overwrite and trust. Package Description accurately describes raw decipher/agreement and
does not alter references or APIs.

The required affirmative voice is substantially met. The remaining negatives explain consequential
limits and are usually paired with an action. No broad style rewrite is requested. Optional only:
align the stray XML-comment indentation at `OpenPgpSession.cs:32` during the coordinator's scoped
formatting pass. It has no content impact and is not an acceptance blocker.

Original consequential citations are independently accepted with the exact actor, strength and profile
limits recorded in `evidence/review-original-citations.md`. This includes the live-revalidated Card PDF
hash/title, RFC S2K versus F9 encoding, host DigestInfo versus card padding, Ed25519 seed/raw-message
formats, RFC7748 versus vendor wrapper, ATT reset/fixed/trust exceptions, exact ROCA advisory boundaries
and conservative Python/C# gates. Historical vendor/manual versus development-source introduction
ambiguity remains explicit. Do not turn source-history agreement into device qualification.

## Late hardware reconciliation — bounded coordinator evidence

These are coordinator-supplied execution results, not reviewer executions or final raw-evidence certification:

- All 49 existing integration methods passed, with zero reported skips/warnings: completed 48-method
  non-KDF run (181.5 seconds, both slow RSA cases) plus isolated KDF. Retain the original interrupted
  120-second invocation with 24 reported passes as a separate incomplete attempt.
- The wrong extended PIN probe returned typed zero while fresh User attempts changed 3→2 and Reset
  attempts remained zero. Distinct-salt v1 setup wrote F9 after key generation and failed 6985 before
  its intended oracle. v2 configured F9 first, verified fresh ordinary User/Admin access and a host
  signature control, then a correct extended PIN failed 6982 / typed zero / User 3→2. Retain failures
  as evidence for PGP-IMP-001/002, without a runtime fix or universal firmware-state rule.
- Sign and Authentication `On` prompt cases passed independent ECDSA verification. Decipher retry
  passed plaintext comparison and ordinary-mode 6982-denial control. The initial human-ready sign
  and decipher requests failed 6600 after about 14 seconds; retain them beside later human-confirmed
  successes. The SDK's `Failed` classification is accurate; elapsed time alone supplies no causal
  human-touch attribution.
- Attestation v2 verified only extended User mode, then FB01 failed 6982 in about 4 ms with
  `PolicyRequires`→`Failed`; no certificate oracle was reached. The v3 ordinary-mode supplemental
  retry passed: certificate signature verified under the preserved issuer, certified SPKI matched
  the generated key, ATT certificate was unchanged and notification completed. The retained v2 failure
  and v3 success establish different authorization-route results on this key; no all-firmware exclusive
  rule or automatic CA trust follows. Current product guidance already chooses that ordinary route.
- No-touch cancellation was requested at prompt-clock2074ms (1500ms scheduled after setup), but
  the admitted operation reached 6600/`Failed` at14811ms. The probe passed its observation/recovery
  oracle; it did **not** establish prompt cancellation or a `Cancelled` terminal outcome. A fresh
  session host-verified a signature before any reset. The guide's drain/fresh-inspection advice remains
  supported without diagnosing inherited Core implementation in this module review.
- The SDK X25519 probe imported RFC7748 Alice and matched its public key, then deciphering Bob failed
  6A80. Installed vendor code on the same key, through a fresh connection without reset/import/generation,
  completed the RFC shared-secret match with the EC wrapper. The source-confirmed PGP-IMP-007 is now
  confirmed for this scope; the current safer RSA/NIST guidance remains accurate.
- SDK ATT public read failed6A80. An explicit read-only query with correct CRT B603840181 succeeded,
  returned the RSA public template and matched current/baseline certificate SPKI. PGP-IMP-013 is now
  confirmed for this scope. No ATT key was replaced; target attestation remains independent of that CRT.
- Vendor readback after reset found ATT UIF still `On`, ordinary UIF `Off`, retries 3/0/3, KDF None,
  empty ordinary material and the same ATT certificate. This supports preserving ATT exceptions and
  shows that cleanup must explicitly restore changeable ATT `Off`; reset alone is insufficient.
  The coordinator subsequently restored ATT with a source-checked0020 public configuration write.
  `hardware/restore-att-uif.json` records actual On→Off reads, raw0.0.1 retained and no private-key
  replacement. Final independent cleanup and factory User/sign, User/extended and Admin verification
  passed. `hardware/final-reconciliation.json` reports all public-state fields matching baseline,
  post-verification fresh state identical, all four UIF Off, empty ordinary material and ATT identity
  preserved. No device jobs remain planned; content acceptance still leaves final evidence review separate.

For this late impact check, independently read the retained v2/v3 attestation and cancellation run results,
their focused oracle/timing output, X25519/ATT SDK failure logs, same-key vendor result, correct-CRT read,
ATT restoration and final-reconciliation records. Exact paths are under
`validation/hardware-probe/evidence/v3-touch-attest/`, `v3-cancel/`, `v3-x25519-same-key/`,
`validation/x25519-sdk.log`, `validation/att-read-sdk.log` and `hardware/`. This narrows the pending
hardware statements in the editor's earlier checkpoint without changing product prose or treating a
passing observational cancellation probe as successful cancellation enforcement.

Keep raw applet/SDK/direct-Python 0.0.1 separate from CLI 5.8 and metadata 5.8 beta. Earlier vendor
version-gated certificate/UIF results were not actual reads; the corrected helper's explicit reads are
the baseline. No generic Core defect is diagnosed. Linux USB beta-device results establish no NEO,
older stable firmware, NFC, other-platform or AOT-runtime qualification.

## Re-review and closeout requirements

1. Editor/coordinator resolves PGP-REV-001–005 in comments/XML/guide prose only and returns exact corrected hashes
   for full affected-unit re-review. Refresh product/snippet identities after any additional edits or formatter.
2. Coordinator removes edited clean-unit overlaps and expands API-category sampling for the editor's
   PinPolicy.Once clean-file miss. This review does not certify the coordinator's clean sampling.
3. Keep all compilation, full 19-source tool validation, formatter/API/solution/unit/docs-package/
   architecture/complexity and final hardware/readback gates separately evidenced. Pending compilation
   and human ceremonies are not passes.
4. Reconcile late evidence across the dependency map before publication. Preserve the 14 overlapping
   deferred implementation groups, four Core questions and historical chronology/DOC-002 gaps.
   PGP-DEP-001–021 are documentation dependencies, not 21 Core bugs.

**Current acceptance:** 52 of the original58 authored paths have no required content correction;
six contain required corrections, and the sampled previously clean `GenerationTimes.cs` requires a
59th product-path edit. These seven affected paths remain awaiting correction/re-review. No final
blanket acceptance is issued for the stale identities or pending gates.

Final read-only record check: snapshot JSON parses; all58 authored raw identities plus the sampled
GenerationTimes identity match working bytes; all12 snippet hashes and197 body lines match. No identity
drift was observed during this review. Required-correction count is five groups across seven paths.
