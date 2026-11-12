# OpenPGP boundary documentation edit return

**OpenPGP documentation edits are prepared, uncommitted, in the isolated
`/home/rupicola/Code/y/yubikit-core-docs-edit-pgp-piv` worktree.** They apply only the
source-supported documentation facets of CORE-PGP-X001–X008. Independent content/citation
review and the coordinator's existing final validation/publication gates remain pending.

The worktree stays at `da66f5d01ba594993974ddd26723e8c300c0a454`; product source pin is
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Latest completed primary OpenPGP boundary inputs
were consumed read-only. PIV remains unreleased and untouched.

## Patch and records

All new editing records are in [editing/PGP-boundary](editing/PGP-boundary/).

| Record | Purpose |
| --- | --- |
| [Complete product patch](editing/PGP-boundary/product.patch) | Entire uncommitted patch for the seventeen product documentation files; apply from repository root |
| [Changed files](editing/PGP-boundary/changed-files.json) | Raw before/after SHA-256, exact old/new ranges, final-newline state and candidate associations |
| [Path decisions](editing/PGP-boundary/path-decisions.json) | All 93 original OpenPGP paths, explicit edit/retain/ineligible reasons and identities |
| [Candidate decisions](editing/PGP-boundary/candidate-decisions.md) | X001–X008 applied facets, retained accurate text, holds and separate engineering/policy/qualification gates |
| [Outside-owner referrals](editing/PGP-boundary/outside-owner-handoffs.json) | 25 exact read-only external paths with owners, applicability and reopening triggers |
| [Example inventory](editing/PGP-boundary/example-inventory.json) | Seventeen exact retained Markdown bodies plus two XML protocol fragments; classification and no fresh compilation/runtime credit |
| [Citation notes](editing/PGP-boundary/citation-notes.md) | Exact retained authority/status/section/actor/strength/applicability and remaining source gaps |
| [Preserved provenance](editing/PGP-boundary/preserved-provenance.json) | 15 inherited keys, 21 documentation dependencies and fourteen deferred engineering groups, unchanged identities/statuses |
| [Input identities](editing/PGP-boundary/input-identities.json) | Consumed primary report/ledger identities; no frozen input was rewritten |
| [Static verification](editing/PGP-boundary/static-verification.json) / [limits](editing/PGP-boundary/verification-notes.md) | Comment-only preservation, XML, local links, exact example bodies and whitespace; not SDK execution |

## Applied corrections

- Actual SmartCard factory binding, hidden/borrowed connection ownership, failure cleanup,
  optional SCP setup and effective/detected/device/AID version provenance.
- Synchronous KDF cleanup before protocol drain versus asynchronous cleanup after Core teardown;
  current alias/replacement/reset and serialization/parser-copy gaps remain explicit.
- Complete-public-task lifetimes for inputs, keys and prompts, per-exchange admission/cancellation,
  response transfer/copy ownership, and the attestation certificate callback-failure gap.
- Reset retains its effective version and secure graph, performs plain reselect without a new
  handshake and supplies no complete erasure/channel-restoration postcondition.
- Version fallback covers every non-OK or short successful response and caught APDU exception.
  PutKey's modeled EC list now matches current representations without promising device support.
- Reader units and CLI bypass are explicit. Test guidance stays within actual assertions; the
  uncontrolled Unverify policy is not presented as isolated causal proof.

The original PIN/KDF identity, X25519 framing and ATT-read findings remain confirmed for their
recorded scope. No catalog or documentation change fixes applet behavior, adds native/device
acceptance or resolves a policy. Existing historical PGP-DEP labels are not new Core bugs.

Static checks cover seventeen changed files, 93 path dispositions, 25 outside-owner referrals,
seventeen unchanged Markdown examples, two unchanged XML protocol fragments, 146 well-formed
XML comment blocks and eleven resolving local Markdown targets. No builds/tests/formatter/
crypto/native/device operations or Git index/ref/commit/push/subagent work ran. Product edits are
restricted to `.cs` comments/XML and `.md` files under the released `src/OpenPgp/` packet paths;
executable tokens, assertions, imports/directives, API snapshots and configuration are preserved.

Next: fresh independent review and coordinator integration/publication through the existing Core
Pass-1 gates. PIV physical deferrals and the user's Windows concurrency future handoff remain.
Finish published Core Pass 1, deliver the deferred finding handoff, then stop; no Pass-2 project
or hardware operation is started by this return.
