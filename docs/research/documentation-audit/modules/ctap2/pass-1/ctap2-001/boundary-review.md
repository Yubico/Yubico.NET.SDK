# Fresh independent final CTAP ↔ WebAuthn boundary review

**Verdict: CHANGES REQUIRED — two narrow documentation corrections.** The integrated CTAP corrections
and the remaining inspected boundary claims pass semantic review within the limits below. Neither
correction is an implementation request. **WA-XREF-001's documentation dependency is independently
verified as satisfied; all original WA engineering IDs remain open.**

**Reviewer:** GPT-6.1 Sol, fresh independent final reviewer, not original investigator G or an editor.
**Date:** 2026-11-04. **Checkout:** `/home/rupicola/Code/y/Yubico.Yubikit.NET` (PRIMARY).
**HEAD/baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
Read root/both-module guidance, available skills, canonical workflow/style, plan, triage/addendum,
original investigation G/map/evidence, and final A–F correction follow-ups (including E's separate
crypto review). Loaded security-guidelines and verify; generic skill examples are not protocol authority.
Context7 was used for framework ownership guidance. Actual source and freshly retrieved original
clauses, not reviewer agreement, ground the conclusions.

Only reports/evidence were authored. No source edits, builds, tests, hardware, framework/crypto
experiments, formatting, commits, delegation, image rendering or integration operations occurred.
The original G map is preserved. [boundary-final.md](boundary-final.md) assesses all 29 original rows
and G-001–011, records exact source scopes/hashes and retains deferred IDs/reopening conditions.
[Fresh original evidence](evidence/g/boundary-originals.md) supplies version/status/actor/strength.

## Required corrections

### BF-R1 — requested permissions are not an issued-token guarantee

**File/claim:** `docs/architecture/webauthn-extension-processing.md:185–194`, especially line190:
“The token requested for a ceremony carries every processor's permissions.”

**Actual source:** `WebAuthnClient.Authentication.cs:110–119` and `WebAuthnClient.Registration.cs:117–127`
union extension permissions into the **requested** permissions. `WebAuthnBackend.cs:119–136` delegates
PIN issuance to `ClientPin.GetPinUvAuthTokenUsingPinAsync`; `ClientPin.cs:410–417` falls back to legacy
0x05 when pinUvAuthToken is absent/false, without requested permissions or RP ID. Returned token owners
contain bytes/protocol, not verified permission metadata. S23 §6.5.5.7/§6.5.5.7.1 grant default mc/ga
on legacy issuance (subject to noMcGaPermissionsWithClientPin), not lbw or all requested permissions.
S23 §6.10.2's device write authorization remains conditional; the SDK's stricter token-only route
is a separate supported-policy restriction.

**Required prose-only correction:** say that *the ceremony's token request includes the union of
processor permissions*, and locally qualify that legacy command fallback drops explicit permissions/RP
scope and does not guarantee privileged authorization. Suggested complete invariant:

> The ceremony's token request includes each processor's requested permissions. Legacy PIN-token
> command fallback does not carry that permission/RP request; a returned token is not proof that
> every requested operation is authorized.

Do not fix negotiation, add token introspection, broaden the pinless feature route or silently change
client policy. Reuses CTAP-DOC-005/015 and **WA-IMP-009**, CTAP-IMP-010; no duplicate engineering ID.
Coordinator must explicitly assign this existing external WebAuthn architecture path; F's authority
was only the README requirements edit. Re-review the complete permissions/invariants paragraphs after
correction and retain adjacent LargeBlobWrite/PRF/legacy distinctions.

### BF-R2 — registration unsigned envelope is key 6, not key 8

**File/claim:** `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs:83–108`, parameter XML at87:
“Top-level unsigned extension outputs (CTAP key 8).” This parameter belongs to **registration**.

**Actual source:** `PreviewSignProcessor.cs:111–119` passes
`MakeCredentialResponse.UnsignedExtensionOutputs`; its decoder handles outer **0x06** at
`src/Ctap2/src/Credentials/MakeCredentialResponse.cs:213–228`. `PreviewSignCodec.cs:132–148` then reads
the text-keyed previewSign value, whose **inner key 7** wraps the signing-key attestation. S23's
makeCredential response table is unsigned 0x06; getAssertion's table is unsigned 0x08. V4 §10.2.1
defines the nested att-obj wrapping, not replacement of the outer CTAP envelope.

**Required prose-only correction:** identify “makeCredential response key 0x06”; optionally also
name the separate previewSign nested key7. Do not change any codec/body/constant, add getAssertion
unsigned support, claim attestation verification, or upgrade first-introduction history. Reuses
CTAP-DOC-015/017, CTAP-IMP-030/038 and WA-IMP-006/019 dependencies without creating a new bug count.
Coordinator must explicitly assign this unedited WebAuthn C# comment dependency. Re-review the complete
registration XML plus call/decoder chain after correction; token preservation/compiler checks remain
coordinator work.

## Optional, non-blocking precision

**BF-O1:** `docs/architecture/webauthn-extension-processing.md:118–122` says retry key agreements/salts
are “re-derived against the new token.” `PrfProcessor.cs:136–169` performs independent key agreement
through backend EncapsulateAsync; the shared secret is not derived from the PIN/UV token. “Fresh
per-attempt key agreement and encrypted salts are prepared after reacquisition” is clearer. Retain the
following correct sentence that key agreement needs no token. This is wording precision, not another
engineering defect or a request to force CTAP/PRF token parity.

## PASS and retained decisions

- **User/privacy:** CTAP response layout and User remarks now distinguish opaque id from identifying
  name/displayName/icon, discoverable from server-side, required authenticator privacy from actual
  modeled passthrough. Current WebAuthn response/match XML and handle-only RP handoff agree.
  **WA-XREF-001 can close as documentation only.** CTAP-IMP-026 and **DEC-002** remain: selected
  native display retention versus browser/RP serializer boundary is not settled or forcibly filtered.
- **Descriptor:** exact actual CTAP2.0 singleton allowList exception versus required 2.1 original,
  errata and 2.3 field is correct. Final matcher/builder still publish empty IDs; **WA-IMP-010 is unfixed**.
  Probe recovery and original-singleton application advice are not a fix for multi-entry narrowing.
- **UV/protocol:** token requests omit uv entirely in WebAuthn; raw encoder and FidoTool still combine
  it, candidly qualified. Built-in UV capability/configuration differs from PIN orchestration and
  permission-token support. Legacy subcommand fallback retains selected V2; **WA-IMP-009 remains open**.
- **Extensions:** verified-PRF MUST and missing override/cleanup are disclosed; credProtect enforcement
  is inert, platform strength stays SHOULD NOT, sensitive credBlob requires level3+enforcement, and
  stronger policy is not failure. largeBlobKey envelope/storage differs from unsigned direct extension;
  original WebAuthn write cardinality, missing key/Required gap, absent/empty/error and post-assertion
  write/read boundaries remain explicit. minPIN disclosure is not configuration; false facade remains
  unfixed. No higher-level supported policy is forced into raw CTAP parity.
- **Signing:** experimental V4/ARKG profile, algorithm-specific TBS/args, inner trust and fixed verifier
  limits survive. **Seed -65700 agrees with draft-10**; outer kty/wrapping/dkalg differences do not.
- **Lifetime/outcomes:** response copies lack general secret owners; scoped tokens/shared secrets are
  not end-to-end erasure. Stateful continuation adjacency, callback terminal-before-decode, failure
  transfer clearing, caller-owned prompts/inputs and cancellation-success race/no rollback are truthful.
- **Requirements/COORD-001:** standards advertisement, advertised/configured features, authorization,
  firmware metadata and current transport/FIDO AID are separate. Missing decoded encrypted fields
  become present empty nullable memory; RawData is the wire-presence path. No SKU/hardware qualification
  follows. F's only WebAuthn change is README requirements, not a whole-module re-audit.
- **Diagrams:** accepted L2/L3b source traces encoding/backend/typed Decode accurately. Generated
  SVG/PNG/manifest freshness remains coordinator-owned pending work, not this review's PASS claim.

## Snapshot checks and next gate

Fresh read-only hash comparison identified exactly **162 accepted content paths** across final A–F
manifests. **160 exact-byte matches**; the two csproj differences were independently compared with
the accepted F checkout and are **CRLF/LF only**. HEAD remains baseline. This is snapshot identity,
not a fresh semantic audit of all162 paths or executable/compiler validation. Original map SHA256:
`097c23d16d9f72f16a0a05db730b5bee4a5e0fda6a6d872fe59e2e668c713c36`.

Final boundary acceptance requires explicit owner assignment, BF-R1/BF-R2 prose corrections and focused
re-review at new hashes. All original WA-IMP-001–021 remain open; the boundary investigation reconfirms
only its named dependencies, not the entirety of their implementation. No Pass2, compiler/test/token,
hardware, image or campaign-closeout claim is made. Coordinator owns those separate gates and ledger
updates, including recording the independently satisfied WA-XREF-001 documentation dependency.
