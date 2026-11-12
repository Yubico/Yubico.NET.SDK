# Independent FIDO-boundary content and citation review

**MANDATORY CORRECTIONS — not PASS.** Two source-supported documentation corrections remain.
No runtime repair, changed assertion, field filtering, new policy or engineering acceptance is requested.

Reviewed editing base `da66f5d01ba594993974ddd26723e8c300c0a454`, product pin
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`, and the exact uncommitted inputs in
[review-identities.json](review-identities.json). Coordinates below refer to those input bytes.
The reviewer is independent of the editor and source investigators. Only this review and its identity
record were written; product files, editor receipts and PRIMARY records were read-only.

## Mandatory corrections

### R1 — Describe the PIN-peer validation gap in the README's catalog boundary

- **Location:** `src/Ctap2/README.md:297–302`, specifically line 301:
  “PIN/UV curve and algorithm checks remain strict.”
- **Local concern:** `CORE-CTAP-X006`, inherited `ctap2-001:CTAP-IMP-007`.
  The editor's candidate decision retains this as an accurate contract. It is not an accurate
  present-tense description of the implementation.
- **Actual implementation:** `src/Ctap2/src/Pin/PinUvAuthHelpers.cs:58–106` checks the X/Y storage
  types and 32-byte lengths, constructs P-256 parameters and delegates point import to the provider.
  It does not read or validate the peer's kty, crv or alg labels or optional-label restrictions.
  The existing remarks at lines 43–57 explicitly disclose this gap. Emitting correct labels for the
  platform's own key at lines 109–117 is not peer validation. Core catalog applicability supplies
  none of the omitted checks.
- **Exact original authority:** retained CTAP 2.1 Proposed Standard, 15 June 2021, errata edition
  21 June 2022, §6.5.6 `ecdh`/`getPublicKey` and §6.5.7 inheritance;
  PRIMARY `investigation/B5/sources/ctap21.txt:8227–8230,8272–8292,8321–8325`.
  The profile requires EC2/P-256/-25 representation and on-curve points. These protocol requirements
  do not prove the implementation enforces them. The original boundary's
  `boundaries/Ctap2/candidates.md:83–92` preserves the implementation gap; interpreting the headline
  as merely a target requires information this README sentence does not provide.
- **Required correction:** keep the distinction between an open catalog and strict CTAP profile
  requirements, but state locally that current helper validation covers coordinates/provider P-256
  import and omits the required peer-label checks. Do not repair validation or describe the omission
  as intended forward compatibility. Update the editor's X006/path disposition and hashes accordingly.
  The prescriptive “Preserve … strict … checks” in CLAUDE is not the same declarative claim; its
  Forward compatibility section already explicitly discloses the gap.

### R2 — Finish narrowing the token-reuse explanation in the same complete test unit and retained caller

- **Locations:**
  - `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs:406–409`.
    The retained summary says a getAssertion “accepts a token already used” and “only makeCredential
    does not,” referring back to the registration test whose firmware claim was just qualified.
  - `src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs:159–163`, specifically the retained
    “a following getAssertion accepts the same token” rationale.
- **Local concerns:** `CORE-WA-X012` (oracle/provenance), with the retained ceremony path associated
  with `CORE-WA-X002/X003/X004/X005/X006`; keep the original applet test/provenance and token-policy
  identities rather than allocating a new runtime finding.
- **Actual assertions/caller:** the complete test at lines 411–429 supplies a fake backend and
  noncryptographic `TestPinUvAuthProtocol`. It observes two requests with nonnull auth parameters
  and one `GetAssertion` permission-token acquisition. It never observes an authenticator accepting
  either request. `ProbeCredentialsAsync:175–195` re-mints only when `remintPermissions` is supplied;
  authentication passes null at `WebAuthnClient.Authentication.cs:164–168`. These establish the
  SDK's reuse choice, not device acceptance or a universal command-specific invalidation rule.
- **Exact original authority/provenance:** PRIMARY `boundaries/WebAuthn/candidates.md:156–169`
  requires actual-oracle explanations; `boundaries/WebAuthn/sources.md:57–61` retains the unpinned
  re-mint rationale as qualified provenance. CTAP 2.1 PS/2022 errata §6.5 introductory note,
  retained `investigation/B5/sources/ctap21.txt:6299–6310`, explicitly separates the platform's copy
  from authenticator expiry, including timeout/command/error conditions. Neither this note nor the
  fake test establishes unconditional token acceptance. The newly edited registration summary at
  test lines 283–285 correctly avoids that inference, but the neighboring summary still makes it.
- **Required correction:** describe the observed single token acquisition and SDK reuse for the probe
  and final assertion. Qualify or remove the categorical acceptance/“only makeCredential” explanation
  in both locations. Preserve useful rationale as bounded historical provenance if retained; do not
  change token acquisition, assertions or runtime behavior. Reconcile the retained caller's path
  decision and the X012 disposition, then regenerate affected receipts.

## Verified content that does not need another rewrite

The complete 14 changed product files, surrounding example context, all 35 concern decisions and the
310-path identity/disposition population were inspected. Relevant retained contracts were checked against
their implementations/callers rather than accepting the receipt's generic retain sentence as proof.
This is not a new whole-module implementation audit or blanket acceptance of all inherited findings.
R1 and R2 are failures of that retained-content check, not requests for optional style cleanup.

- The new known-member/process qualifications agree with `YubiKeyDevice.ConnectAsync:131–181` and
  `DeviceConnectionRegistry.AcquireConnectionAsync/Claim`. Matching serials do not coordinate disjoint
  views. Both factories choose one current route; opening or initialization failure is not failover.
- Session/connection adoption and whole-workflow advice agree with the actual owning/borrowing paths.
  `ExchangeGuard.RunAsync:49–62` exits after its delegate; `ApplicationSession:43–44,470–568` supplies no
  public-operation lease. WebAuthn backend disposal at `264–282` disposes its PIN protocol before waiting
  for its session. The new prose does not turn exchange drain into ceremony admission or a disposal join.
- Both complete backends and `UserPresenceNotification.Create/RequestCoreAsync/ResolveAsync` support
  the request-capture, failed-request/no-resolution and uncancelled-terminal-callback distinctions.
  Successful CTAP status precedes typed decode and WebAuthn output construction. The conditional
  throwing-logger path can bypass backend clearing; it is correctly not called a reproduced leak.
  HID accumulation, incoming reports, APDU Data, SCP temporaries and typed results remain separate owners.
- Initialization order and detected/effective firmware agree with the full CTAP factory and
  `PcscProtocol.Configure:162–195`: initial plain probes precede options; major-zero returns before
  ordinary ForceShortApdus handling; SCP backend rebind adds no SELECT or protected CTAP probe.
  Setup flags do not supply device/profile qualification or CTAP PIN/UV permissions.
- Baseline Windows compatibility HID calls Get/Set synchronously and ignores the adapter token.
  Cooperative CANCEL/drain is not native interruption, a deadline or release proof. The future Windows
  concurrency delta was not inspected and remains **UNREADY / UNREVIEWED**.
- The reset selector now correctly distinguishes `ResetMenu.cs:98–116` (independent reselection,
  earlier transport reused) from non-forced `Program.cs:130–176` (independent reselection and later
  transport selection). Human target verification remains necessary; no target-correlation fix is claimed.
- Core console char limits/configurable encoding and ClientPin's byte guard do not enforce the prompt's
  code-point/UTF-8/profile contract. The new adapter warning preserves this boundary and prohibits silent
  normalization of an existing credential. A focused independent Context7 `/dotnet/docs` query confirmed
  UTF-16 code-unit versus scalar/UTF-8 vocabulary; actual SDK source establishes its policy.
- Request-only intent coverage, Connect-probe routing and the edited registration permission-sequence
  summary match their actual assertions. R2 is the remaining nearby overstatement.

## Citation and identity boundaries

Consequential retained originals were read, not upgraded to newer standards: CTAP 2.1 PS/2022 errata
§6.5.1 and §6.5.6/.7 distinguish platform MUSTs, authenticator baseline and helper behavior; §6.5's
platform MAY/SHOULD concerns host token copies, not guaranteed authenticator validity. CTAP 2.3 PS,
26 February 2026, §11.2.9.1.5/.7 assigns CANCEL response duties to the authenticator and a MAY to the
client; SHOULD keepalive cadence is no host completion bound.

The original CTAP 2.0 PS §5.2 response table (`ctap20.txt:2310–2360`) permits descriptor omission for
the actual singleton CTAP allowList. CTAP 2.1 and 2.3 §6.2.2 tables require it
(`ctap21.txt:5438–5499`, `ctap23.txt:5708–5768`). Authenticator identifying-field restrictions and the
multiple-account platform picker's ONLY-id return have different actors and conditions. Source tracing
through CredentialFilter, Matcher, authentication construction and cached selection preserves:

- **WA-XREF-001 / CTAP-XREF-001:** original documentation resolutions remain resolved.
- **WA-IMP-010:** selected-descriptor recovery remains a source-confirmed implementation gap, including
  narrowed original multi-entry requests. Original-singleton advice is safe but not a repair.
- **CTAP-DEC-002:** native output actor and future RP serializer remain a maintainer/coordinator decision.
  No field stripping is authorized, and absence of an SDK serializer is not a conformance exemption.

The retained WebAuthn Level 3 capture identifies W3C Recommendation, 25 August 2026, not an Editor's
Draft. The vendor sign-extension v4 capture identifies Editor's Draft, 26 August 2025; its empty-handle
permission is not an SDK constructor guarantee. No experimental example or ARKG profile was changed;
draft-10 representation and draft-09 mathematics remain separate, expired-draft authorities.

The SCP qualification is supported by retained SCP11 **1.3.0.13 Public Review**, August 2023, §4.3
(`scp11-public-review-1.3.0.13.txt:1085–1105`): host receipt verification and SD-side OCE authentication,
including the first correct-MAC post-establishment APDU for a/c, are distinct. Retained SCP03 **1.1.2
Public Release**, March 2019, §6.2.6 (`scp03-1.1.2.txt:1149–1168`) is not the code-cited Public Review
edition and is not authority for unavailable final SCP11/ISO/vendor clauses.

The PRIMARY B4/B6 and WebAuthn follow-ups and source-completion addendum supersede only historical
“return pending” language. M/P decisions, E requirements, Q gaps and remaining owner boundaries are not
renamed into intended design or accepted engineering. Native producer/header/ABI, provider/hidden-buffer,
physical/profile and historical-artifact gaps, including PIV physical deferrals, remain open.

The editor's 27 input identities match current PRIMARY bytes except the live editing release:
its recorded hash is `5240a3f2a64b3f71d2c4a673b539949780c9910ed69dad8f1d405d3a1ba367c1`;
the release read for this review is `6ea0a2f4eeff0fa9dae1cf9d67e77ca22f0b27bef3bccd649c7f2dd970ae009e`.
The current release still explicitly assigns this FIDO scope and preserves all constraints. This review
binds the current hash without overwriting the editor's historical identity receipt or asserting the
unseen historical bytes were independently compared.

## Static preservation and remaining gates

Independent read-only checks found:

- All 310 path before/current/product-pin SHA-256 values match the receipt. The product patch is
  byte-identical to the actual Git diff: `904ab905bc35609168302a65edd118cc1395bafe016b5d1bca15a3d191e3e78f`.
- All nine C# diffs are comments/XML plus final-newline changes. Manual diff inspection and an independent
  comments/whitespace-erasure check found no changed runtime tokens, imports, directives, declarations,
  defaults, attributes, assertions, API shapes or help/output strings. This is not compiler verification.
- All 47 actual Markdown fence bodies are byte-identical to base: 15 + 10 + 5 CTAP and 8 + 9 WebAuthn.
  Context/classifications retain full methods/types, supplied-context fragments, replacement alternatives,
  diagrams, scaffolds and historical fragments. Resource scopes/prerequisites are not silently invented.
  Unchanged examples are **not thereby compiled, API-bound, executed or physically qualified**.
- Changed C# XML fragments parse without syntax errors. This does not resolve cref/paramref binding.

The twelve other changed paths have no mandatory content correction identified at the exact hashes
marked `content-reviewed` in the identity record. The two paths implicated in R1/R2 and retained ceremony
caller are **not content-accepted**. The batch cannot receive PASS until those narrow corrections and
their regenerated records are independently checked.

Compiler/analyzer/XML-cref binding, consumer/example compilation with actual imports/project context,
coordinator validation, and publication remain gates. No build, SDK test, formatter, cryptographic,
native or device execution was performed or inferred. Runtime cleanup, Windows acceptance, device/SCP
interoperability, RP verification and standards conformance are not granted by this review. No Git
index/ref/commit/push or subagent action occurred.

After the corrections and existing validation gates, the coordinator should finish/publish Core Pass 1,
deliver the precise deferred-owner handoff, then stop. This review starts no new framework or Pass-2 work.
