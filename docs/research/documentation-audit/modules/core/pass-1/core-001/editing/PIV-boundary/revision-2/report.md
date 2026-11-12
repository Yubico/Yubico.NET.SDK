# PIV boundary — revision 2 return

**The bounded example-cleanup review item is corrected using the coordinator-authorized
Markdown-fence change.** This editor successor awaits independent correction recheck; it is
not a new 513-unit investigation or accepted-review verdict. Product source remains
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; the isolated worktree remains at
`da66f5d01ba594993974ddd26723e8c300c0a454`.

## Independent successor patch

- [Full 13-file product patch](product.patch), SHA-256
  `dc35e1032236c9363640a280dc722b9c10c33c8c2567433946d5521da514a8ce`.
  Apply this **instead of** the prior full PIV patch from the checkpoint.
- [Two-file correction-only delta](successor-delta.patch), SHA-256
  `ab97bd5d8df7afaf7d4b9f1e6b591d8c9c701ab7acc1066930cfe03fd0205b72`.
  This is relative to the reviewed predecessor, not an additional patch after the full successor.
- [Changed-file identities/ranges](changed-files.json), [correction ranges](correction-ranges.json)
  and [all 121 current path decisions](path-decisions.json) retain candidate associations and
  distinguish checkpoint, reviewed predecessor and current product bytes.
- [Example inventory](example-inventory.json): **19 Markdown / 23 XML bodies**. Exactly two
  Markdown fences changed; seventeen Markdown and all 23 XML bodies remain exact. Both changed
  entries retain predecessor/current bodies, hashes, ranges and precise resource scopes.
- [Correction response](correction-review.md), [static checks](static-verification.json),
  [local targets](local-links.json), [31 owner referrals](outside-owner-handoffs.json),
  [preserved provenance](preserved-provenance.json) and [original reviewed input identities](input-identities.json)
  provide the bounded recheck material.

All prior patch/record/review bytes and the original `edit-PIV-boundary.md` remain exact.
[Predecessor identities](predecessor-identities.json) bind fifteen prior files;
`predecessor-products/` retains all thirteen reviewed product files, including old fence bodies.
This successor supersedes their current resource/accounting claims without rewriting history.

## Correction and evidence limits

`src/Piv/README.md` initializes empty output holders before acquiring the PIN and enters `try`
immediately after acquisition. Signing hashes inside the protected scope; decryption allocates
the known plaintext inside it. The existing finally blocks clear the locally owned PIN and
successfully assigned digest/signature/known-plaintext/returned-plaintext storage. Session,
certificate and host-verifier/RSA disposal scopes remain intact. Acquisition failure before a
buffer is returned supplies no successfully acquired buffer for this caller to clear; no claim
about a failing provider's internal storage is made.

Nearby task prose and `src/Piv/examples/PivTool/README.md` directions now describe these scopes
and the separate cleanup responsibilities of additional source/provider/application copies.
The README path reason and both inventory resource scopes are corrected. PIVIMP-005's actual
tool RSA/Ed25519 framing defect and every other engineering/physical qualification remain held.

Only those two Markdown files differ from the reviewed predecessor. All actual `.cs` SDK/test
files are byte-identical to it, and executable source lines remain identical to pinned Git.
Static checks cover 124 well-formed XML comment blocks, twenty resolving local targets and the
explicit two-changed/forty-unchanged example split. No compilation, allocation-failure injection,
provider/native erasure, crypto/device execution or runtime acceptance is claimed.

The corrected combined inventory is **36 Markdown / 28 XML** (OpenPGP 17/5, PIV 19/23), with
two changed Markdown fences and 62 unchanged bodies. Original eleven routed PIV keys and
PIVIMP-001–010 states remain; serial 125's 6A88 was before EXTERNAL AUTHENTICATE/RESET, not
secure-reset acceptance. F9/private/attestation material, protected PRINTED restoration scope,
physical PIN/power/cross-process/touch/hotplug/cancellation-recovery deferrals and the unready
Windows future handoff remain unchanged.

No builds/tests/formatter/crypto/provider/native/device operations, Git mutations, subagents or
new framework ran. Next: independent bounded recheck and existing Core Pass-1 publication/
validation, deferred finding handoff, then stop; no runtime policy change or Pass-2 work.
