# Pass 2 review: triage policy

Status: **approved by Dennis on 2026-11-14 15:51**; review closed 2026-11-20 (AG-25). Applies to cards in `pass-2-applet-master.md`
(~159 cards; 38 marked "No-brainer: yes"). Decisions are logged in `pass-2-review-decisions.md` with the rule ID
that produced them.

## Gates (checked first; any hit sends the card to interview)

- **G1 High severity.** Any High card is reviewed by Dennis, even with precedent. *Relaxation H1 below was
  proposed (AG-17) and changed no card.*
- **G2 Spec conflict.** Sibling behaviour contradicts the governing spec (CTAP 2.x, WebAuthn L3, GlobalPlatform,
  OpenPGP card spec, NIST SP 800-73). Precedent is implementation evidence, not authority.
- **G3 Shared bug.** Siblings share the same defect. Precedent doesn't count.
- **G4 Breaking change after release.** Suspended. YubiKit v2 is alpha with no direct consumers, so breaking
  public API or behaviour does not gate a card (Dennis, 2026-11-14). Reinstate at the first public release.
- **G5 Concrete better option.** Sia can name a specific UX, safety or flow improvement over the precedent with a
  concrete user benefit. "Could be nicer" does not qualify; divergence from siblings has a support cost.

## Auto-approve rules (when no gate is hit)

- **A1 No-brainer.** The card is "No-brainer: yes" (mechanical, settled contract). Approve as written.
- **A2 Sibling consensus.** Python (yubikey-manager or python-fido2 for FIDO) plus Android and Rust agree. Adopt
  equivalent behaviour.
- **A3 Python precedent, partial confluence.** Python has precedent and at least one of Android or Rust agrees,
  and nobody contradicts it. Adopt.
- **A4 Split siblings.** Siblings disagree. Pick the stricter, fail-closed behaviour if severity is Low or
  Medium. Example: OATH-DEC-002, exact 16-byte key (Android and Rust strict, Python lenient).
- **A5 Internal precedent.** .NET-only concerns with no sibling equivalent (zeroing memory, IDisposable,
  cancellation, async, span lifetimes): follow the pattern another applet in this SDK already uses.
- **A6 Keep and document.** Current behaviour is correct or acceptable and the card only needs XML docs, guides or a
  test pinning it. Approve.

- **D0 Default (approved 2026-11-15, AG-9).** A card that hits no gate and fits no A-rule goes to Dennis for
  interview, at the lowest priority within its severity. Never auto-approve on Sia's lean. Raised by the PIV and
  YubiOTP classifiers.
- **F1 Fast-confirm (approved 2026-11-15, AG-10).** A High card whose fix is mechanical and on which the siblings
  agree gets a one-line yes from Dennis instead of a full interview. Each card still needs his explicit answer.
- **H1 High with correct sibling precedent (proposed 2026-11-18, AG-17; Q7 closed 2026-11-20 in AG-22 without the A4 extension).** A High card may
  auto-approve when sibling precedent shows the correct behaviour: A2 or A3 applies and no other gate is hit (G2 spec
  conflict, G3 shared bug, G5 better option). Open: whether A4 (split siblings, stricter fail-closed wins) also
  counts for High. Every High auto is listed at the top of the veto skim (E3), and E1 escalation applies. Would
  supersede F1 once in force. Outcome: every High card that qualified was already decided, so H1 moved no card.
- **M1 Doctrine strictness (approved 2026-11-18 20:25, AG-19; settles X7).** A Medium or Low card gated only by
  G3, or by a G5 whose better option is the repo doctrine (`CLAUDE.md:156–158`: malformed and security-critical data
  stay strict; unknown but well-formed values stay in open carriers), auto-approves when the fix only adds a
  rejection before any side effect or publication, or preserves unknown values, and well-formed traffic is unchanged.
  Not when the card needs hardware qualification, a value Dennis must set, or ties to an open X other than X7.
- **M2 Spec-mandated (approved 2026-11-18 20:25, AG-19).** A Medium or Low card whose lean follows governing spec text
  quoted on the card, with a local fix, auto-approves after the quote is re-verified. Unverifiable quote: stays gated.
  If only part of the lean follows the text, that part auto-approves and the rest stays with Dennis (split).
- **Splits (approved 2026-11-18, AG-19).** When part of a gated card already fits an A-, M1- or M2-rule, that part is
  split off as its own auto (own commit, own veto line) and the card stays with a narrower question.

## Separate tracks (not decisions)

- **T1 Qualification cards** ("qualification, no assigned defect severity"): needs a physical key, firmware or
  OS. Collect into a hardware test session list; not part of the voice review.
- **T2 Cross-cutting decisions:** the same policy appears across several applets (for example session state after
  failed authentication in OATH, PIV and YubiHSM Auth; timeout and cancellation). Decide once, apply everywhere.

## Engineer guardrails

- **E1 Stop and escalate** if implementation shows the precedent doesn't fit (older firmware, different
  transport, .NET lifetime difference), or if the change grows past the card's scope.
- **E2 Evidence pin.** Every auto-approved card records sibling file:line and commit used, so Dennis can
  spot-check.
- **E3 Veto window.** Auto-approvals are not read out. They are written to `pass-2-review-classification.md`
  with rule ID, evidence pin and pseudocode. Dennis can skim and veto any card before the engineer starts it.
  Voice time is spent only on gated cards, highest severity first.
