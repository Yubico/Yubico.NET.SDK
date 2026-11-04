# Integrated boundary correction checkpoint

2026-11-04. Astra explicitly owns the two newly assigned narrow dependencies in PRIMARY; no overlapping
editor is writing them. Read applicable WebAuthn instructions and full affected paragraphs/registration
XML. [Fresh boundary review](boundary-review.md) supplies original clauses and exact source chain.

- **BF-R1:** `docs/architecture/webauthn-extension-processing.md` now distinguishes requested ceremony/
  processor permission union from actual issued-token authorization, including legacy fallback omitting
  explicit permission/RP fields. No token negotiation, introspection or authorization behavior changed.
- **BF-R2:** `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs` registration parameter XML names
  outer makeCredential key0x06; getAssertion key0x08 and nested previewSign key7 remain different layers.
  No codec body, constant, API, response modeling or attestation verification changed.
- **Authorized BF-O1:** fresh per-attempt key agreement/encrypted salts are prepared after token
  reacquisition, not mathematically derived from the PIN/UV token. The adjacent no-token requirement
  distinction remains explicit.

Required correction re-review pending. The initial boundary review independently confirms WA-XREF-001's
documentation condition is satisfied, but original WA engineering findings and native-result privacy
decision DEC-002 remain open. No source fix or live ceremony is implied by these prose corrections.

Follow-up accepted BF-R1/BF-R2/BF-O1, then required BF-R3 after coordinator raised adjacent guide wording.
Astra explicitly assigns and corrects only `src/WebAuthn/CLAUDE.md` Known Gotchas item3: draft policy
flags belong inside generated-key attestation; tolerated outer key4 is parser behavior, not a normative
optional field or verified beta/device variant. No parser or output-policy change. Final narrow follow-up
is pending; the initial/follow-up reports retain their original scope and hashes.

[Final boundary follow-up](boundary-review-final.md) accepted BF-R3 and retained BF-R1/BF-R2/BF-O1
acceptance. No required boundary correction remains. It records the three newly affected paths and
pre-format hashes; final formatter transformations are separately preserved and verified. WA-XREF-001
is closed as a documentation dependency only; no WA engineering item or DEC-002 decision was closed.
