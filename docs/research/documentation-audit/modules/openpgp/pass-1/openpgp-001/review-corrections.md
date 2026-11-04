# OpenPGP correction re-review — accepted

**Reviewer:** GPT-6.1 Sol, independent content/original-citation reviewer, 2026-11-04.
**Verdict: PASS. PGP-REV-001–005 are resolved; all 59 product paths are content-accepted on the
exact pre-format identities in `evidence/review-accepted.json`. No further required content fixes.**

This follow-up supersedes the outstanding-correction verdict in the original `review.md` without
changing that historical record, its source/snapshot evidence, the original editor report or prior data.
Acceptance covers documentation content and citations, not the coordinator's pending execution gates.

## Re-review scope and identity

Read `edit-corrections.md` and independently verified its working-byte SHA256:
`306152bb41b0d150f37087467a4a64df1bdcb19a2431fc80cacec2baaf29085d`.
Re-read the seven corrected semantic units in full, including the complete interface/exception/collection
contracts, discretionary property/parser, README metadata task and complete relevant fake-response/helper
units. Checked their full local behavior dependencies: logical credential versus P2, retry extraction and
fallback, KDF error fallback, signature-policy state, reset/blocking sequence, and generation-time parse/write.
The original review already covers the other 52 authored paths; fresh raw hashes show they are unchanged.

The reviewed set is now **59 product paths, including 49 C# files**. All 49 C# token streams independently
match baseline `fe180282a2d77b15eaaa5868d6c8b00e251d1f05` after removing comments and whitespace.
This preserves API declarations, imports, executable strings, assertions and directives. The ordered compact
path/raw/body manifest independently reproduces the editor's
`ee40ed42dc14b34da939e55960f85c3ea293ba17a056ac4b3e1d1f8d059d9480`.
This is lexical preservation evidence, not a compiler or formatter result.

All **12 complete README C# bodies / 197 body lines** retain the original independent-review hashes.
The metadata correction changes prose only. Complete helper/callback/resource scopes remain accepted;
snippet compilation is still a separate gate.

The immutable original editor report remains SHA256
`5f91b64a4ad7f8309eee0731714b06e48c528d7c42f8a2efd2b9100e2013698c`;
the immutable original review is SHA256
`071656cf82debad15b01c323bf1b304451449ea9460f61cef5c51d8e1f2bf848`.
Neither was edited in this follow-up.

## Required findings — full-unit acceptance

| Finding | Verdict | Rechecked result and evidence |
|---|---|---|
| PGP-REV-001 | PASS | User VERIFY exception XML and retry-property/class/constructor text describe reported/extracted/queried counts. The local extended-6982 Reset-counter caveat is explicit, with fresh `AttemptsUser` inspection and diagnosis before retries/recovery. Typed zero is no longer presented as proof of User blocking. Source retry dispatch, Card §§7.2.2/7.9 and retained distinct-counter/device observations agree. Admin-specific text remains properly scoped. |
| PGP-REV-002 | PASS | Policy summary now states per-signature consumption versus retention while card access state remains valid. The complete remarks preserve Admin authentication and state-ending selection/reset/unverify conditions. Card §4.3/§7.2.2 governs card state, independently of host-object lifetime. |
| PGP-REV-003 | PASS | Both retained 6A82 comments identify file/application not found and explain the current broad F9 APDU-error fallback into `KdfNone`. The comments make only fallback/raw-payload claims, rather than actual KDF-absence claims. Fake bytes, helper names and asserted behavior are unchanged. Card §7.9 distinguishes 6A82 from 6A88. |
| PGP-REV-004 | PASS | Reset summary now describes the attempted PIN-blocking/TERMINATE/ACTIVATE sequence. Full remarks keep the current ignored-status/partial-state/readback/KDF-lifetime caveats and ATT exceptions. Source and Card §§7.2.16–17 support that wording, including possible dummy ACTIVATE success. |
| PGP-REV-005 / C022 | PASS | Collection, discretionary property, interface Get/Set and guide all explain zero as unspecified, unsigned card seconds and the current signed-int carrier. They preserve original-bit interpretation above int.MaxValue without treating negative API values as pre-1970 card dates. The setter is accurately described as writing the supplied four-byte representation without date validation or special zero handling. Card §4.4.1 CD table (printed p24, extraction927–930) supplies the sentinel/four-byte layout; original RFC4880 §3.5 supplies unsigned seconds since1970 UTC. Parser/API policy remains deferred under PGP-IMP-018. |

The earlier independent original-citation checks remain applicable: exact Card3.4.1 title/hash, actor and
strength, RFC/card encoding boundaries, vendor extension/reset/trust/ROCA qualifications and bounded firmware
history. The correction introduces no packet-profile upgrade or stronger normative claim. Timestamp prose
matches the actual shift/or parser and four-byte writer, rather than inventing a validation policy.

The corrections meet the approved affirmative house voice: summaries lead with behavior, actors and usable
actions; local qualifications retain their purpose. Remaining negatives address consequential blocking,
absence, recovery or interpretation errors. No preference-driven rewrite is requested.

## Sampling and final hardware context

Read coordinator `sampling-accepted.json`: **11 accepted clean samples**, spanning eight API/internal,
narrative, example and internal-test units. Its timestamp material miss is recorded and excluded after
editing. The coordinator reports the final eligible clean denominator as32 and owns that sampling/coverage
record. This follow-up accepts the corrected GenerationTimes unit, not a claim that the original miss was clean.

Read authoritative `hardware/results.md`, consistent with the previously reviewed late raw evidence:
49 existing integration methods passed; the four intended-contract supplemental failures remain failures;
ordinary-mode attestation verifies issuer signature/generated-key binding, not independent manufacturer-root
trust. Same-key X25519 vendor agreement and correct-CRT read corroborate the two current SDK limitations.
Final restoration/readback reports ordinary defaults/material absence, all four UIF Off, factory PIN acceptance,
and preserved ATT public identity, with all public fields matching baseline. No new hardware run was performed.

**Cancellation qualification:** the original review's “admitted operation” describes only the observed SDK call
that later returned an APDU failure. Cancellation request2074ms and terminal6600/Failed14811ms are observed
prompt-clock results; fresh-session host-verified recovery preceded reset. Native admission, interruption,
timing mechanisms and causal explanations remain unverified. Neither a `Cancelled` outcome nor prompt
cancellation enforcement is claimed by this acceptance.

## Remaining gates and ownership

- Coordinator applies scoped formatting, checks token/directive preservation and refreshes exact post-format
  identities. This acceptance is bound to the current pre-format bytes; content changes need re-review.
- Compilation, unit/example/tool validation, remaining repository gates and final evidence/publication review
  remain separate. No build, test, formatter, hardware, product edit, agent or commit ran in this follow-up.
- Preserve the 14 deferred engineering groups, four Core questions, C022/IMP-018 dependency and historical
  chronology/DOC-002 gaps. Content acceptance supplies no implementation fix or all-platform qualification.

**Outcome:** five required review groups resolved, seven corrected paths re-reviewed, 52 prior paths unchanged,
and all59 current product paths accepted for documentation content/citations.
