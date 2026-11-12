# Focused independent FIDO revision-2 review

**PASS — the full 15-path successor documentation batch**, at full product-patch SHA-256
`8f34ede47c7e39c70a021393150b3a4c4fc4ae9cdc3d761f2de9444273b9740c`.
R1 and R2 are resolved. No mandatory content correction remains from this focused re-review.

This acceptance combines the earlier review's twelve unchanged accepted product paths with the three
corrected paths below. It supersedes only the earlier R1/R2 documentation holds, not its engineering,
qualification or validation limits. Base remains `da66f5d01ba594993974ddd26723e8c300c0a454`; product
source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Exact accepted inputs and the immutable
predecessor review are bound in [review-identities.json](review-identities.json).

## R1: requirement versus actual PIN-peer validation — resolved

`src/Ctap2/README.md:297–305` now separates the required EC2/P-256/-25 representation, coordinate widths,
on-curve point and optional-parameter restrictions from current coordinate checks and provider import.
It explicitly names omitted peer kty/crv/alg and optional-parameter checks as a validation gap, not
intended forward compatibility. It does not imply the catalog supplies those missing checks.

The complete `PerformEcdhKeyAgreement` implementation and its remarks at
`src/Ctap2/src/Pin/PinUvAuthHelpers.cs:43–117` support the distinction: X/Y must be byte arrays of length
32; import forces P-256; no peer-label validation is performed. Correct labels emitted for the platform's
own key do not validate the peer. No provider/cryptographic experiment was needed or performed.

The retained original is **CTAP 2.1 Proposed Standard, 15 June 2021, errata edition 21 June 2022**:

- §6.5.6 `ecdh` and `getPublicKey`, PRIMARY `investigation/B5/sources/ctap21.txt:8227–8230,8272–8292`,
  specify peer parsing, on-curve rejection and the key representation. §6.5.7 at `8321–8325` inherits
  protocol one's behavior except its listed overrides.
- §6.5.5's authenticator output table at `7020–7032` describes the authenticator's public key sent to
  the platform: the COSE key **MUST** contain alg and **MUST NOT** contain other optional parameters.
  This is the correct peer-output actor, not just the platform's outgoing-key table.

These are protocol requirements, not evidence that SDK validation already enforces them. The updated
X006 decision correctly supersedes the earlier README retain rationale while preserving
`ctap2-001:CTAP-IMP-007`, the original definition and implementation/policy/evidence holds.

## R2: SDK reuse choice and actual fake-test oracle — resolved

`src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs:149–195`, particularly the complete
`remintPermissions` documentation at `159–164`, now describes the actual conditional re-mint and the
callers' choices. Registration supplies MakeCredential permissions
(`WebAuthnClient.Registration.cs:166–171`); authentication supplies null
(`WebAuthnClient.Authentication.cs:164–168`). The implementation re-mints only when a token exists and
permissions are supplied (`Ceremony.cs:190–193`). The new text explicitly disclaims guaranteed
authenticator acceptance and retains token-state/expiry conditions.

The full summary and method at
`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs:406–431`
now describe exactly two requests with nonnull auth parameters and one GetAssertion permission-token
acquisition. `TestSupport/TestPinUvAuthProtocol.cs:45–50` produces a fixed sentinel tag independently
of key/message values. Neither tags nor fake responses prove cryptographic authentication, token
identity from tag equality, device acceptance or makeCredential-only invalidation. The corrected
summary does not claim those stronger results. Assertions and runtime code remain unchanged.

CTAP 2.1 PS/2022 errata §6.5's introductory note, retained `ctap21.txt:6299–6310`, gives the platform
a **MAY** for managing its copy and a **SHOULD** for erasing it when unneeded; authenticator token expiry
is separate. It supplies no universal subsequent-command acceptance rule. The corrections preserve this
actor/strength distinction and do not adopt a new token policy.

The retained caller is now **edit**, retaining `CORE-WA-X002/X003/X004/X005/X006` and adding
`CORE-WA-X012`. X012's path list includes that caller. The updated path reasons and X006/X012
documentation dispositions accurately describe the narrow correction. Other concerns, original
definitions, aliases and implementation/policy/unrun-evidence dispositions are unchanged.

## Preservation verified for the successor

Read-only static checks independently confirmed:

- The actual complete Git diff against the editing base is byte-identical to successor `product.patch`
  and has the full SHA-256 above. The full batch contains exactly the expected 15 product paths.
- The three actual correction deltas match their pre-revision snapshots and current hashes. The two
  C# deltas change only XML documentation and the caller file's final newline: all non-XML text,
  declarations, imports, directives, runtime strings, defaults, attributes, APIs and assertions remain
  unchanged. XML fragments parse; this is not compiler or cref binding verification.
- All twelve original editor/review records match `preserved-inputs.json`; the predecessor patch and
  both predecessor review files remain immutable. The other 307 packet product paths are unchanged
  from pre-revision identities, including the twelve previously accepted changed paths.
- All 310 successor current-path hashes match actual files. Only the three correction path records
  differ from the predecessor; only X006 and X012 candidate records differ. Their non-documentation
  holds and original definitions remain identical.
- All 47 actual fence bodies and their recorded hashes/classifications are unchanged. Only applicable
  coordinates move. Prior acceptance of surrounding example/resource context remains in force;
  unchanged bodies do **not** gain compilation, API-binding, execution or physical qualification.

No new library/API question arose; the focused check used actual SDK source and retained originals,
not an inferred API guarantee. No full re-audit or new tooling framework was started.

## Unchanged holds and remaining gates

WA-XREF-001 / CTAP-XREF-001 remain documentation-resolved. WA-IMP-010 descriptor recovery remains an
implementation gap. CTAP-DEC-002 native-output actor/future RP serializer remains a maintainer/coordinator
decision; no field stripping or conformance exemption is authorized. Future Windows concurrency stays
UNREADY / UNREVIEWED. Native producer/header, provider, physical/profile/SCP and PIV physical deferrals
retain the earlier handoff and owners.

**This is content/citation PASS, not publication or engineering acceptance.** Coordinator compiler/token/API
preservation, analyzer/XML-cref binding, consumer/example compilation and existing validation/publication
gates remain. No build, test, formatter, cryptographic, native or device execution occurred. Only the two
revision-2 review files were written; no product/editor/PRIMARY record, Git index/ref/commit/push or
subagent action occurred. Finish and publish Core Pass 1 through its remaining gates, deliver the deferred
handoff, then stop.
