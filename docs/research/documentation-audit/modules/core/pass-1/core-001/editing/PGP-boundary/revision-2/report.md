# OpenPGP boundary — revision 2 return

**The three bounded review corrections are applied and statically checked.** This is an
editor successor, pending independent correction recheck, not a new module investigation or
an accepted-review verdict. Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; the isolated
worktree remains at `da66f5d01ba594993974ddd26723e8c300c0a454`. Current primary product integration
is not substituted for that pin.

## Independent successor patch

- [Full 17-file product patch](product.patch), SHA-256
  `7766e7411dd9847aaaf57b86937a61c720fe0c1a02a8ba7c790f711b61ce47b4`.
  Apply this **instead of** the prior full OpenPGP patch when starting from the checkpoint.
- [Two-file correction-only delta](successor-delta.patch), SHA-256
  `ea4ada3491c1f89d366aceee13c1421f3ce87bfeceaff2c4b75874355095b210`.
  This is relative to the reviewed predecessor, not an additional patch after the full successor.
- [Changed-file identities/ranges](changed-files.json), [correction ranges](correction-ranges.json)
  and [all 93 current path decisions](path-decisions.json) retain candidate associations and
  distinguish checkpoint, reviewed predecessor and current bytes.
- [Example inventory](example-inventory.json): **17 Markdown / five XML bodies**, all unchanged.
  The three omitted protocol layouts now have exact current/source coordinates and body hashes.
- [Correction review response](correction-review.md), [static checks](static-verification.json),
  [local targets](local-links.json), [outside-owner referrals](outside-owner-handoffs.json),
  [preserved provenance](preserved-provenance.json) and [input identities](input-identities.json)
  provide the bounded recheck material. Input identities are the original reviewed inputs, not
  a claim that the live primary editing release has stopped changing.

The original patch, all prior editing records, review and review identities, and the original
`edit-PGP-boundary.md` remain byte-exact. [Predecessor identities](predecessor-identities.json)
bind sixteen prior files; `predecessor-products/` retains all seventeen reviewed product files.
Historical statements in the original return are not rewritten: this successor corrects the
example denominator and replaces the current product patch.

## Scope and evidence

Only `src/OpenPgp/README.md` and `src/OpenPgp/CLAUDE.md` differ from the reviewed product.
The reset task refreshes its cache but returns `Task`; the example separately reads and returns
metadata. Shared-state guidance now lists `IOpenPgpBackend? _backend` and inherited effective
`ApplicationSession.FirmwareVersion`, without inventing a retained raw-version field.

No C# file or example/layout body changed in revision 2. All seventeen full-patch source/doc
paths remain accounted for; all 93 disposition paths have current decisions. Static checks
cover preserved C# executable lines, 146 well-formed XML comment blocks, eleven resolving local
targets and exact body identities. They are not compiler/cref/API/example/runtime acceptance.

The corrected combined example denominator is **36 Markdown / 28 XML**: OpenPGP 17/5 and
PIV 19/23. OpenPGP retains all 22 bodies. PIV's separate successor changes two Markdown fences
and explicitly preserves their predecessors; those changes are not counted as unchanged.

All CORE-PGP-X001–X008 facets and original 15 routed keys, 21 PGP-DEP dependencies and fourteen
engineering groups retain identity. PIN/KDF identity, X25519 framing, ATT read/provisioning and
other implementation/policy/provider/SCP qualifications are not fixed or resolved. PIV physical
deferrals and the unready Windows concurrency future handoff remain.

No build/test/formatter/crypto/provider/native/device execution, Git mutation or subagent ran.
Next: independent bounded recheck and existing coordinator Core Pass-1 publication/validation,
then deferred finding handoff and stop. No Pass-2, broader investigation or tooling framework.
