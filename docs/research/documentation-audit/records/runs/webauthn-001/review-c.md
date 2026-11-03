# Independent review C — WebAuthn documentation

2026-11-03; fresh independent GPT-6.1 Sol reviewer C. Repository baseline:
`e353b49afa15d1d104fce70bbf1e1abc59ddb132` (`e353b49a`).

## Verdict

**Two required documentation corrections remain: C-R1, the hardware-test example's missing
user-presence trait; and C-R2, the architecture page's now-superseded conversion uncertainty.** The
consumer journey, RP boundary, ownership guidance, extension limitations, and source/specification
distinctions otherwise pass this assigned semantic review. This is not
campaign acceptance, compiled-snippet verification, or a claim that the deferred implementation
defects have been fixed.

No repository file was edited. No build, test, formatter, package installation, hardware operation,
commit, staging, or delegation was performed. The sole authored file is this report.

## Required factual/usefulness correction

### C-R1 — mark the hardware-test skeleton with the trait required by its documented filtering policy

**Priority:** required contributor-example safety/usefulness correction; documentation only.

**Location:** `src/WebAuthn/CLAUDE.md:486–509`, “Key Pattern,” particularly the attributes at
488–490. The surrounding policy is at 470–484, and the unattended filtering commands are at 517–524.

The example is a `Registration_WithPreviewSign_ReturnsGeneratedSigningKey` integration-test skeleton
with `[SkippableTheory]` and `[WithYubiKey]`, but no
`[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`. The same guide says that
ceremony/previewSign hardware tests must carry this trait and identifies smoke or
`Category!=RequiresUserPresence` as the non-UP lane. Copying this pattern into a new test does not
give those filters the metadata needed to exclude it. Even before the omitted ceremony logic is
filled in, its setup calls a PIN-normalizing helper that can mutate device state.

**Original/source evidence:**

- `scripts/toolchain/Conventions.cs:23–35` constructs the smoke filter from `Category!=Slow` and
  `Category!=RequiresUserPresence`; test method names and approval comments are not exclusions.
- The actual named test in
  `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:36–39`
  carries the missing trait. Its body performs registration at 65–67.
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnTestHelpers.cs:44–81`
  establishes the setup consequence: it can set the fixed PIN (56–59), change it for ForcePinChange
  (62–67), or submit the known PIN for verification (70–76).

This is a retained omission in an example that this batch substantively revised, **not an allegation
that the editor removed a trait or changed a real test's behavior**. The added approval comment and
surrounding human-authorization requirements are useful, but they do not supply test-filter metadata.

**Smallest correction:** add the following attribute to the Markdown skeleton, adjacent to its other
attributes, while retaining the explicit PIN-state approval warning:

```csharp
[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
```

Do not change executable test files or imply that this trait alone authorizes PIN/device operations.
A narrow reread of the resulting example and its adjacent lane/filter instructions is sufficient to
close this documentation finding; no device run is needed.

### C-R2 — remove the architecture page's superseded nullable-blob uncertainty

**Priority:** required factual consistency correction following the coordinator's new evidence;
documentation only.

**Location:** `docs/architecture/webauthn-extension-processing.md:215–217`, especially “The
nullable-array conversion edge remains unverified, not an established runtime outcome.”

The coordinator supplied an isolated .NET 10 conversion experiment during this review and updated
the public output XML, processor comment, module CLAUDE and checkpoint accordingly. The architecture
page still describes the old uncertainty. This now conflicts with those source-backed qualifications.

I read `experiment_nullable_blob.cs` completely and the coordinator's result entry in
`runs/webauthn-001/validation.md:16`: a null `byte[]` supplied to the nullable-memory record-constructor
parameter yields `HasValue=True`, `Length=0`. The experiment has no SDK/device I/O. I also checked the
unchanged assignment in `LargeBlobProcessor.cs:148–150` and the storage interface/implementation:
`ILargeBlobStorage.GetBlobAsync` returns `Task<byte[]?>`; `LargeBlobStorage.GetBlobAsync:179–189`
returns `array.FindAndDecrypt(...)`, with no intervening nullable-memory handling. This is sufficient
to explain the conversion at that assignment; it is not evidence that a live storage read was run.

**Smallest correction:** replace the obsolete uncertainty sentence with the precise conditional
limitation, for example:

> When storage returns a null byte array, the current conversion produces a present empty memory
> value, losing the distinction between missing data and a stored empty blob.

Keep the adjacent read-error and unsupported/absent-result qualifications. The README's broader
warning that nullable `Blob` is not a unique diagnostic remains compatible and needs no mandatory
rewrite. The final module CLAUDE clarification at 420–422 is accepted: it identifies the conversion's
lost distinction without claiming hardware/blob-storage verification or an implementation fix.

## Optional editorial preferences

None requested. I do not recommend another preference-driven rewrite of the accepted prose.

## Exact reviewed-file coverage

The eight C-edited Markdown files, Description, and Core companion example were all reviewed in their
resulting form, not merely as isolated diff lines:

| File | Extent and disposition |
| --- | --- |
| `src/WebAuthn/README.md` | Entire 518-line document, including all C# fences, requirements, original-result reuse, RP handoff, count handling, prompt/factory alternatives, extension sections, policy examples, constraints, security, and historical migration section. No additional required correction found. |
| `src/WebAuthn/PACKAGE_README.md` | Entire document. Required origin/options/validator, client-only scope, direct CTAP2 administration, alpha caveat, dependency and task links are consistent. Installation commands were read, not executed. |
| `src/WebAuthn/CLAUDE.md` | Entire document, including tree, logging, implementation fragments, ownership, RP policy, permissions, extension processing/signing, limitations, hardware lanes and test skeleton. The final nullable-blob clarification at 420–422 was also reread and accepted. C-R1 above; no other required correction found. |
| `docs/architecture/webauthn-extension-processing.md` | Entire 236-line page, including schematics, both complete ceremony flows, probing, invariants, lifetime/error policy and non-goals. C-R2 follows from the coordinator's later conversion evidence. |
| `docs/v2-highlights.md` | Complete document read; WebAuthn section at 18–30 reviewed with adjacent package, session and credential-collection context. Revised section describes client steps, not an entire client-plus-RP ceremony or established sibling parity. Unrelated feature/roadmap claims were not re-audited. |
| `docs/v1-to-v2-comparison.md` | Full resulting “New in v2” section and surrounding “Already fixed”/“Verified at full parity” context (90–179). The C-edited WebAuthn bullet is appropriately qualified. This is not a new whole-file historical/parity audit. |
| `src/Management/examples/ManagementTool/README.md` | Full resulting Supported Transports section with Usage/Security/Requirements/Dependencies context (45–80). The changed FIDO/CTAPHID phrase correctly distinguishes transport from WebAuthn. No broader ManagementTool implementation/security review claimed. |
| `docs/usage/user-interaction.md` | Entire 208-line guide read, with the full intent/prerequisite/callback and notification-lifetime sections checked. Correct request-options type and named third argument; this remains a scoped intent fragment, not a purported complete login example. |
| `src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj` | Entire project and baseline diff read. Description accurately names client registration/authentication, typed extension processing and the experimental vendor draft; the project diff changes Description only. No compiler/package validation performed. |
| `src/Core/src/Credentials/UserPresenceIntent.cs` | Corrected XML code invocation and its complete surrounding type remarks/scope contract (17–79) read, plus baseline diff. The invocation now matches the actual WebAuthn method's third cancellation parameter. This is not a Core implementation audit. |

The five additional paths in editor C's 15-path manifest are A/B correction ownership, with independent
narrow re-reviews assigned elsewhere. I checked them only for consistency with the above prose:

- `src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs`: resulting CredentialId/User
  contract and response fields (complete type).
- `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs`: complete resulting file, including the
  corrected version attribution, actual final-ID construction, narrowing and eager outputs.
- `src/WebAuthn/src/Client/CredentialFilter.cs`: complete resulting file, including version-qualified
  singleton handling and actual probe branches.
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs`:
  corrected no-match summary and full relevant fake test (380–402), with adjacent probe context.
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs`: corrected
  two-credential summary and complete relevant method (210–259), helper context, and the named
  registration test used as evidence for C-R1. No fresh A/B approval is substituted here.

`docs/research/webauthn-documentation-audit-checkpoint.md` was read completely **for consistency only**.
It correctly remains in progress, retains the deferred ledger and now records WA-IMP-016 as an
isolated-conversion-confirmed missing-versus-empty limitation, and does not convert this review into
final gate or hardware evidence. Final results remain the
coordinator's responsibility.

## Source-backed reader-journey and contract checks

- **One local demo, not fabricated RP integration:** the README has one owned client, one retained
  registration result, a separate assertion challenge/options object, an original-single-entry allow
  list, a zero-count guard before indexing, and borrowed-PIN clearing in `finally`. Its production
  instructions assign challenges/options/account handle to the RP and specify exact client-data bytes,
  attestation/assertion material, optional user handle and extension-result handoff. Only independent
  RP verification completes registration/login; no invented upload or response-serialization API appears.
- **Exact API shape:** source declarations support `CreateWebAuthnClientAsync(origin, clientOptions,
  sessionOptions, cancellationToken)`, the two ceremony methods' `(options, pinBytes, cancellationToken)`
  order, the demonstrated entity/descriptor/extension constructors, result property names, and
  `SelectAsync`. The ordinary previewSign dictionary needs no inaccessible comparer. The two external
  intent examples now name `cancellationToken:` instead of binding a token to the PIN parameter.
- **Ownership:** `WebAuthnClient.cs` and `IYubiKeyExtensions.cs` support successful session transfer,
  client disposal, factory-failure cleanup and caller-retained prompts. Configuration examples are
  explicitly replacements rather than appended overlapping clients. `WebAuthnClient.PinUvAuth.cs`
  and `WebAuthnClient.Ceremony.cs` support exact-sized prompt ownership, fresh prompt retries,
  rejected/accepted PIN cleanup and eventual late-buffer cleanup. None promises that disposing a
  client clears PRF/blob outputs.
- **Selection/counts:** authentication construction and `CredentialMatcher`/`MatchedCredential` agree
  with allow-list narrowing versus discoverable enumeration, normal empty results, eagerly completed
  extension work, and cached selection without another device operation. Missing-descriptor guidance
  does not guess the first member of a multi-entry original allow list.
- **UV and errors:** `UvDecision.cs`, backend protocol selection, the shared ceremony catch and output
  preparation boundaries support configured-versus-obtainable verification, permission aggregation,
  one required-UV escalation, protocol-2 limits, nonuniversal error normalization and cooperative
  cancellation/success races. GetInfo/token/output failures are not presented as universally mapped.
- **Extensions:** both session factories and the PRF/largeBlob/previewSign/credProps processors support
  the documented capability gating, client-derived credProps, null-container versus absent individual
  output distinction, and two-category output isolation. The docs disclose PRF UV non-enforcement and
  result cleanup gaps; credProtect non-enforcement; largeBlob Required, original-singleton write and
  read/error limits; present minPinLength behavior; and previewSign arguments, separate key,
  inner-attestation trust boundary, optional support/result and empty-handle limitation. The nullable
  array conversion was initially uncertain; the coordinator's isolated conversion experiment and
  unchanged assignment now support the exact missing-versus-empty limitation. The final CLAUDE
  qualification is accurate; C-R2 identifies the one stale architecture sentence.

These checks used executable source paths and original publications to establish behavior, not an
assumption that a sibling investigation report or newly edited XML is authoritative. Investigation
reports and triage were used to identify the accepted scope and follow-up dependencies.

## Primary-reference checks and limits

Independently retrieved primary material on 2026-11-03:

- [WebAuthn L3 Recommendation, 2026-08-25](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/):
  RP-ID/non-Web-binding syntax distinction; user-handle constraints; RP registration/assertion
  verification and persistence; extension optionality/client processing; PRF §10.1.4 UV override and
  salt mapping; largeBlob §10.1.5 original-singleton write rule; challenges; credential-ID privacy.
  The PRF MUST/override is not rewritten as optional SDK policy, and §9.4 is not used to invent a
  general malformed-output suppression mandate.
- [WebAuthn L2 Recommendation, 2021-04-08](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/#enumdef-userverificationrequirement):
  §5.8.6 describes RP preference intent, not the SDK's exact PIN-first/availability algorithm.
- [CTAP2.0 PS, 2019-01-30](https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html),
  [CTAP2.1 PS, 2021-06-15](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-20210615.html),
  [CTAP2.2 PS, 2025-07-14](https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html),
  and [CTAP2.3 PS, 2026-02-26](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html):
  the singleton credential-descriptor omission is a 2.0 allowance, not a 2.1/2.3 allowance; hmac-secret
  is §12.5 in 2.1 and §12.7 in 2.2/2.3; direct largeBlob §12.4 is distinct from the implemented storage
  route. CTAP2.3 material also confirms the platform's credProtect SHOULD NOT, GetInfo list/transport
  qualifications and conditional NFC/contact presence terminology. These checks do not prove that a
  particular YubiKey/reader executes every specification-permitted path.
- [Yubico sign-extension draft v4](https://yubicolabs.github.io/webauthn-sign-extension/4/), Editor's
  Draft 2025-08-26, §10.2.1: separate signing key; nonempty allow-list/map coverage and selected input;
  Required-derived flags; algorithm-specific TBS and CBOR `COSE_Sign_Args`; inner attestation;
  algorithm-role distinction; permitted empty handle. It is not CTAP4 or an adopted W3C signing standard.
- The README's pinned [python-fido2 client source](https://github.com/Yubico/python-fido2/blob/9d5991d6785e20bd487c8b6c996df6ce2226642a/fido2/client/__init__.py)
  was retrieved as original raw source and checked for `ClientDataCollector` and the default collector's
  verifier delegation. This supports the bounded analogy, not cross-SDK behavioral parity.
- Context7 `/dotnet/docs` confirmed named/optional argument binding for supplying a named third
  argument while omitting the optional second argument. Local SDK declarations determine the actual
  parameter names and order.

FIDO originals were read through Exa extraction. One guessed February 2025 CTAP2.2 URL failed;
the actual July publication was then retrieved and used. A first local HTML-extraction attempt lacked
BeautifulSoup; a standard-library-only read succeeded without installing anything. Neither failure
is a repository/build/test failure. No conclusion relies on those failed retrievals.

This is not an exhaustive fresh normative audit of every A/B XML citation. The retained Android
transport-forwarding precedent was not independently fetched; local response construction and the
primary transport contracts establish the documentation claim without it. Network link availability,
every fragment anchor, package-feed installability, all-platform/device support, full sensitive-memory
cleanup, exact browser serialization conformance and certification are not verified here.

## Handoff

Apply C-R1 in the Markdown example and C-R2 in the architecture paragraph, retaining all accepted
qualifications, then perform their narrow semantic rechecks. These require no executable changes.

The coordinator reports passing full build/unit/docs gates, compiled-example groups and token checks
in `validation.md`; I read that summary but did not rerun or independently certify those commands or
their archived logs. The same record distinguishes the later blob-comment update and its remaining
final gates. I did not run the conversion experiment myself. Reviewer B's blob follow-up and reviewers
A/B's assigned narrow re-reviews are separate review evidence. None of these checks establishes live
blob behavior, hardware qualification or full standards conformance.
