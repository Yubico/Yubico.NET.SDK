# FIDO boundary revision 2: R1 and R2

Applied the two mandatory documentation corrections from `../review.md` in exactly three assigned
product paths. This successor adds no runtime fix, assertion change, token-policy change or engineering
finding. The original 14-file product patch, editor receipts, report, review and review identities remain
byte-exact historical inputs. The current full product patch has 15 paths because R2 reopens one
previously retained caller.

## R1: requirement versus current peer validation

`src/Ctap2/README.md` now states CTAP's EC2/P-256/-25, coordinate, on-curve and optional-parameter
requirements separately from current enforcement. The helper checks byte-array coordinates and their
32-byte widths, imports them as P-256 through the provider, and omits peer kty/crv/alg and optional-label
checks. Catalog applicability supplies none of those checks. This omission remains
`ctap2-001:CTAP-IMP-007` / `CORE-CTAP-X006`, not intended forward compatibility.

Verification before editing covered the complete `PerformEcdhKeyAgreement` implementation and existing
remarks in `src/Ctap2/src/Pin/PinUvAuthHelpers.cs:43–117`. Correct labels emitted for the platform's own
key do not validate the peer. The README's old declarative “checks remain strict” claim was therefore
incorrect; this successor supersedes the earlier X006 retain decision only for that prose facet.

The original authority was re-read in PRIMARY's retained `investigation/B5/sources/ctap21.txt`:

- CTAP **2.1 Proposed Standard, 15 June 2021, errata edition 21 June 2022**, §6.5.6:
  `ecdh` at lines 8227–8230 parses the peer using `getPublicKey`'s representation and returns an error
  if parsing fails or the point is off-curve. `getPublicKey` at 8272–8292 specifies kty=2, alg=-25,
  crv=1 and 32-byte big-endian X/Y coordinates. These are protocol procedure requirements, not proof
  of SDK checks. §6.5.7 at 8321–8325 inherits protocol one's behavior except its listed overrides.
- §6.5.5's authenticator output table at 7020–7032 identifies the authenticator's `getPublicKey` result
  and says its COSE_Key **MUST** contain alg and **MUST NOT** contain other optional parameters.
  The earlier platform input table at 6940–6970 was also read, but its actor is the platform's own
  outgoing key; that table alone is not substituted for the peer-output restriction.

No protocol edition, validation behavior or unknown-response policy changed.

## R2: SDK reuse choice versus the fake test's oracle

`src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs` now describes the nullable
`remintPermissions` caller choice: with an existing token, registration requests re-minting;
authentication passes null and reuses the existing token for its final assertion. The comment makes
no unconditional device-acceptance or makeCredential-only invalidation claim.

The complete `ProbeCredentialsAsync` unit at old lines 149–195 checks token presence and supplied
permissions before re-minting. Authentication's call at
`src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:164–168` explicitly supplies null.
These are source-visible SDK choices; they do not establish authenticator validity of that token.

The complete test `GetAssertion_AllowList_WithToken_SignsProbeAndAssertionWithOneToken` in
`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs`
now describes its actual oracle: two requests with nonnull auth parameters and one GetAssertion
permission-token acquisition. The fake backend returns responses; `TestPinUvAuthProtocol.Authenticate`
fills each tag with a fixed sentinel independently of key/message bytes. Neither proves cryptographic
authentication, token equality from the tags, device acceptance or a universal invalidation rule.
Runtime statements, assertions and inline assertion comments remain unchanged.

The original CTAP **2.1 PS / 2022 errata**, §6.5 introductory note at retained lines 6299–6310 was
re-read: the platform **MAY** manage its copy's lifetime and **SHOULD** erase it when unneeded;
the authenticator can separately expire its token for PIN, timeout, command and error conditions.
This note supplies no universal subsequent-command acceptance rule. The original WebAuthn candidate
X012 and source register's unpinned re-mint provenance were also re-read. Existing applet
test/provenance identities remain; no new runtime or policy ID is allocated.

The caller path changes from **retain** to **edit**. Its original `CORE-WA-X002/X003/X004/X005/X006`
associations remain, with `CORE-WA-X012` added for this oracle/provenance correction. Other facets on
that caller remain retained or deferred exactly as before.

## Successor receipts and focused review handoff

- `product.patch` is the new complete product diff against editing base
  `da66f5d01ba594993974ddd26723e8c300c0a454`; product source remains
  `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
- `corrections.patch` contains only the three revision-2 corrections against the reviewed bytes.
- `path-decisions.json` and `candidate-decisions.json` are full successor populations, with the three
  path decisions, X006 and X012 updated. `changed-files.json` gives exact pre-revision/current hashes
  and ranges, while `verification.json` identifies both patch hashes.
- `examples.json` retains all 47 exact bodies/classifications with current coordinates. Bodies are
  unchanged from both the reviewed batch and original editing base; no compilation is inferred.
- `preserved-inputs.json` binds all original editor/review records and pre-revision product identities;
  `source-identities.json` binds the actual implementation and original passages used for these corrections.

Static checks cover documentation-only revision scope, unchanged non-comment C# text, XML syntax,
fence bodies, original-record preservation and untouched accepted content. The old records are not
regenerated or overwritten; the live PRIMARY editing-release hash noted by the reviewer does not
retroactively replace its historical receipt.

The independent reviewer should check the full README catalog paragraph, complete caller parameter
documentation and complete test summary/method against the source/passages above, then confirm the
retained caller's new edit disposition. This report does not self-award review PASS. Existing compiler,
consumer, coordinator-validation and publication gates remain. No build/test/formatter, crypto/native/
device execution, Git mutation or subagent occurred. Finish Core Pass 1, deliver deferred findings,
then stop; no new research, implementation, tooling framework or policy project is opened.
