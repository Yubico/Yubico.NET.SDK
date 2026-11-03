# Wave1 complete

Integrated commits `618adbec` (CLI) and `c79e4c1e` (protocol) into the audit branch at `c90dced1` after independent re-review and warning-free gates.

Closed: IMP-001, IMP-002, IMP-008, IMP-009, IMP-011, IMP-012, IMP-013, IMP-014. The prior upstream closure of IMP-003 is unchanged. Remaining implementation work is Base32/creation validation, parser-cleanup regression evidence, and reset recovery; contract/provenance items retain their recorded limits.

- Combined unit suite: 3,828 executed, zero failed, five skipped; OATH311.
- Combined OATH integration smoke on125:20 executed including offline cases, zero failed, five skipped.
- Revised targeted protocol cases passed separately on9681620/5.4.3 and125/5.8.0.alpha.2. Never report skipped counterpart rows as device verification.
- Full build, docs/architecture checks, applicable complexity and scoped formatting passed. Example parser complexity was reviewed explicitly rather than inferred from a shipping-only tool result.
- No push performed. The parent's unrelated untracked files remain untouched.

CORE-001 remains a separate two-key discovery blocker. Marker observations do not establish whether individual touch-configured HOTP requires touch, and wrong-key status evidence does not resolve every authentication-failure classification.

Evidence: `completion.json`, `review-round-1.md`, `review-round-2.md`, reviewed file manifests, archived red/green results, both branch gates, combined gates, and hardware logs.
