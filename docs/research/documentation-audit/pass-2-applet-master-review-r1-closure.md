# Pass 2 applet master — independent R1 closure

**2026-11-13. R1 closed. Ready as an applet-by-applet user-review master, not full semantic certification.**

This additive record closes only R1 from the [original independent review](pass-2-applet-master-review.md).
It accepts the edit documented in the [correction receipt](pass-2-applet-master-correction-r1.md).
The original review is unchanged; this closure supersedes its R1 correction request and the receipt's
capture-time pending-closure status. It does not retrospectively expand the original review's sample.

## Decision and source basis

[CTAP-IMP-007](pass-2-applet-master.md#ctap-imp-007--security-critical-peer-cose-labels), current master
lines **2562–2595**, now assigns **Low** to the supported consequence: schema-invalid required labels
are not rejected when X/Y pass the existing width and fixed-P-256 provider boundary. Lines 2566–2574
do not infer weaker cryptography, off-curve acceptance, authorization bypass or secret compromise.
This addresses R1's missing high-impact causal explanation without rejecting the conformance gap.

Re-read `src/Ctap2/src/Pin/PinUvAuthHelpers.cs:41–119` against Git baseline
`6ce355468047e8004fd51cf1425808f6e8242ed3`. Working-tree bytes match that object:
SHA-256 `47d199377344b2505fa593fbe3ad4f31fb0eeb937b5f63d572ae5f702a617643`.
The original CTAP triage row at line 47 and frozen X006 definition at lines 83–92 retain the same
required-label and import/derivation exception boundaries. No provider or runtime result is inferred.

The required-label fix and its narrow no-brainer judgment remain. Exception provenance and optional-label
policy stay open. The card's literal BEFORE, schematic AFTER and acceptance/dependency text are unchanged.
**No Critical or Important blocker remains within this R1 closure scope.**

## Exact preservation check

Compared current bytes with `/tmp/opencode/ctap-r1-before/` snapshots, first verifying each snapshot
against the prior identities in the original review. The complete diff contains only the receipt's five
master edit regions and four companion edit regions: R1 impact/severity and review-status/scope wording.
No other card, local map, coverage ledger, proposal or applet region was changed.

| Input | Verified SHA-256 |
| --- | --- |
| Prior master snapshot | `066994f1841b189ea411e66d288791ace6d45779b4d9fa7bd3fbe6dd195e4db9` |
| Current master — 545,339 bytes / 8,150 lines | `960873b4bd2baba78719f902d1f98e0e75c8d388863eb3753e137da2324549ed` |
| Prior companion snapshot | `65a3afc9e662ea7cc1c6ba7f06e8b6437ff5471f1c55fd112d34c6e05bc2ddc7` |
| Current companion | `8cdd003fd0cd0c8a67b7177e964438bdd16cc54af8db0c35992f37f8b2bb2be4` |
| Original review — unchanged against snapshot and prior review identity | `d7725778b617eb67a6f719e4dd0c60d91b44bbfdbd6678c4c786321c131afadb` |
| Correction receipt | `8bbbb0a91471afde7922685cec56dafb341405947baa37b5c2d490fc7cb20a7a` |

The nine-applet region excluding CTAP-IMP-007 is byte-identical; its SHA-256 is
`0ec020528425a0cb8061c77929c11d1233ddd8a0c74767f61e67b4ef477cc602`.
All **377 complete fenced code/procedure blocks** are identical. Counted **358 Markdown link
occurrences** across master/companion/correction receipt: 340/15/3. These counts do not constitute
semantic review or a fresh all-target/URL-reachability check.

## Readiness boundary

The master and companion honestly describe the independent evidence as **ledger-level comparison plus
targeted semantic/source sampling**. Unsampled compound facets, reopening paths and proposals remain
for lazy followup when the user selects that applet. They are neither missing-known-ID findings nor
an all-nine-app deep-review completion gate imposed by this correction.

**Ready for user review** means the parent can present the master one applet at a time with those limits
visible. It does not mean every card is independently semantically certified, user-accepted or selected
for implementation. User review remains unstarted; implementation remains deferred; AFTER proposals
remain unapplied and uncompiled. No full reaudit, build/test, hardware/provider/crypto execution,
master/product/frozen/index edit, commit, push or subagent was performed. Only this closure record was added.
