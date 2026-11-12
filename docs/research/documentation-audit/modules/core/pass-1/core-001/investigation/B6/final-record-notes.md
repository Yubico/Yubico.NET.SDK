# Final record authority and verification notes

Use `final-summary.json`, `unit-dispositions.json`, `file-dispositions.json`,
`candidate-index.json`, `clean-nominees.json`, `unchanged-paths.json`,
`sources/final-source-register.md`, `sources/final-manifest.json` and
`final-verification.json` for the completed return. `investigation-B6.md` is the
coordinator-facing report. These records have no unread or awaiting-context states.

The original incomplete return is frozen in `history/checkpoint-48-files/`, with
17 byte identities in its manifest. Root `checkpoint-summary.json`,
`verification-receipt.json`, `checkpoint.py` and `verify-records.py` are also retained
historical tools/results, not the current coverage authority. Do not rerun those
older writers over final ledgers. `sources/register.md` is the original source
checkpoint, whose continuation queue is superseded by the final source register.
`finalize.py` only serializes explicit completed readings/localized annotations;
its hash checks do not infer semantic coverage. `read-vectors.py` performs static
fixture-field/transcript/consumer-tuple comparisons without computing KDF outputs.

The first final checker attempt stopped on the report's link to its own not-yet-
created `final-verification.json`. All preceding ID/hash/partition/source/data/history
assertions had passed, but that failed attempt was not a completed verification.
The corrected check permits exactly its own planned output path during prepublication
link checking, writes the receipt only after every assertion succeeds, and then was
rerun successfully. This was an evidence-record creation-order issue, not an SDK test
failure or a reason to weaken any source/reading/fixture check.

Final localization removed one speculative direct constructor-failure anchor from
AesCmac: no concrete path leaving partially generated subkeys was established.
The normal source cleanup observation remains, and the actual finalization offset,
stack initialization and provider-error boundaries remain in C004. This changes
the intermediate 277-clean/138-candidate count to **278 clean / 137 candidate**;
267 finalized context and two historical context outcomes are unchanged. No concern
group or inherited alias was renamed or merged. Exact final counts come from the
fresh summary/checker rather than the earlier commentary message.

All 80 unchanged-path records have explicit scope and future owners: 60 assigned
primary/shared paths plus 20 dependency referrals. Dependency referrals add zero
B6 coverage and preserve B5/C/applet ownership. The 16 substantive nominees are
source-review candidates for sampling; no sampling, compiler, SDK, native/provider
or hardware execution has been implied. Unavailable originals remain precise final
qualifications after bounded follow-up, not unfinished semantic reading.
