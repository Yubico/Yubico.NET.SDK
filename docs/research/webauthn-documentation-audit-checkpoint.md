# WebAuthn documentation audit checkpoint

## Scope and status

The 2026-11-03 documentation cycle reviews the complete WebAuthn module: public API documentation,
user guides, internal explanatory comments, test comments, and substantive specification citations.
Public documentation is the priority. This is a documentation-first cycle; meaningful implementation
changes are deferred with explicit documentation re-review dependencies.

Baseline: `e353b49afa15d1d104fce70bbf1e1abc59ddb132` on `yubikit-docs-audit`.
The documentation cycle is complete: inventory, investigation, triage, editing, independent review,
correction re-review, quality sampling, and final verification. The completion commit on the campaign
branch provides the repository backup boundary.

The inventory contains **131 tracked module files**: 74 source C# files, 47 test C# files, three module
Markdown documents, and seven API/project/runner support files. Source declarations and API snapshots
reconcile **44 public types and 542 snapshot entries**, including generated record members. External
coverage includes WebAuthn-specific passages in 25 active-reference files, four migration files, and
one companion Core XML example found during sampling: **30 external files** in total. Historical and
generated material retains its original context. The one Core example is a WebAuthn call-site correction,
not a Core module audit.

Three Sol 6.1 investigators read the assigned semantic units and returned **51 raw report findings before
sampling follow-ups**, with overlapping root causes. This is not a count of independent defects. Astra
coordinates triage and selective checks; fresh Sol sessions edit and independently
review the changes. The approved writing style is direct, calm and precise, preserving useful rationale
and conditions rather than targeting a lower word count.

## Documentation findings

Accepted corrections cover:

- Required application-supplied RP validation, supported origin representation, and package metadata.
- Registration/authentication examples with explicit RP responsibilities, response handling, client lifetime,
  and count-safe assertion selection.
- Eager authenticator and extension work before `GetAssertionAsync` returns; allow-list filtering versus
  discoverable-credential enumeration; `SelectAsync` retrieving an already-prepared response.
- User-handle versus identifying display-field conditions, error propagation, cooperative timeout behavior,
  PIN/UV availability, session ownership, and decoder buffer lifetime.
- Extension input/output meanings, current support limits, and security-relevant unimplemented promises.
- Obsolete adapters, implementation phases, stale test explanations, and inaccurate specification citations.

Specification checks distinguish the WebAuthn client, relying-party server, CTAP platform and authenticator.
References include WebAuthn Level 2 Recommendation (2021-04-08), Level 3 Recommendation (2026-08-25), the
applicable CTAP 2.0/2.1/2.2/2.3 publications, and Yubico's experimental sign-extension draft version 4
(2025-08-26). Citation repairs do not automatically change the implementation's standards target.

An eight-unit Astra sample of material assessed clean found a missed positional cancellation argument in
the shared user-interaction example. That triggered an expanded review of external WebAuthn examples,
including code blocks whose calls do not contain the word “WebAuthn.” The expansion confirmed that same
miss and found its companion XML example; these are two affected locations, not three new findings.

## Deferred implementation and test work

These are source-backed findings unless explicitly marked unverified. They remain engineering follow-ups;
documenting a limitation does not resolve it or make it an intended standards contract. The owner is a
future WebAuthn implementation pass, with narrower CTAP2/test ownership noted where needed.

| ID | Finding | Documentation to revisit after a fix or policy decision |
| --- | --- | --- |
| WA-IMP-001 | `Enterprise` does not send CTAP's enterprise-attestation request; `Indirect` does not use an anonymization service | Attestation preference/options, backend, registration guide |
| WA-IMP-002 | Error normalization differs across capability reads, PIN/UV acquisition, ceremony commands and extension outputs; unsupported previewSign is omitted | Public methods/errors, extension sessions/codecs, blob/signing guidance |
| WA-IMP-003 | Timeout is cooperative, can race a successful result, and oversized positive delays use the framework exception | Both options, deadline implementation, cancellation guidance and test assumptions |
| WA-IMP-004 | A discouraged-UV hardware assertion lacks capability guards | Hardware test applicability and UV guidance |
| WA-IMP-005 | Manual control-character escaping differs from exact WebAuthn `CCDToString`; public low-level builder validation differs from ceremony validation | Client-data XML/tests and serialization claims |
| WA-IMP-006 | Attestation decoding retains caller CBOR; mutation can make raw and parsed representations disagree | Decode/RawCbor/Encode ownership and generated-key attestation guidance |
| WA-IMP-007 | Default `Aaguid` has an empty byte span and nonreflexive equality | Aaguid default/equality contracts and regression tests |
| WA-IMP-008 | Shared-secret test checks post-disposal access rejection, not zeroing through a retained memory view | Test evidence descriptions and cleanup verification claims |
| WA-IMP-009 | Backend uses PIN/UV protocol 2 without negotiating protocol 1; built-in-UV token capability checks need review | Compatibility prerequisites, backend/context, UV and PRF guidance |
| WA-IMP-010 | A final assertion descriptor omitted under CTAP2.0's singleton allowance can yield an empty published ID; CTAP2.1/2.3 require that descriptor | Matcher, result IDs, selection guidance and credential-specific extensions |
| WA-IMP-011 | PRF does not enforce WebAuthn's verified-variant requirement or override UV preference | Both ceremonies, PRF inputs/results/processors, guide and architecture |
| WA-IMP-012 | `EnforceCredentialProtectionPolicy` does not enforce the promised pre-creation protection; the CTAP builder stores but does not use the flag | credProtect input/output, support filtering, builder and guide |
| WA-IMP-013 | SDK-produced PRF/blob outputs lack an owned cleanup API; PRF intermediates and input-salt temporary storage also need cleanup work | Sensitive-result lifetimes, PRF/blob codecs, narrow CTAP storage dependencies and examples |
| WA-IMP-014 | LargeBlob `Required` can succeed without blob support on the resulting credential | Support enum, effective discoverable requirement, input/output and guide |
| WA-IMP-015 | LargeBlob writes do not enforce exactly one entry in the original allow list | Request validation, processor/session comments, tests and write guidance |
| WA-IMP-016 | A null blob array converts to present empty read-only memory, so nullable presence/length do not distinguish missing from empty data; isolated .NET conversion verified | Blob result-presence and read-error contracts after a fix |
| WA-IMP-017 | Current largeBlob path supports `largeBlobKey`/storage-array operations, not the alternative direct extension; SDK writes require a token | Capability, permissions, storage and UV guidance if support expands |
| WA-IMP-018 | A present MinPinLength input requests the value even when `Requested` is false | Aggregate input, processor/codec and reused CTAP carrier |
| WA-IMP-019 | Signing parameters reject empty previewSign handles although the draft permits generated empty handles | Generated key, signing parameters, exceptions and round-trip tests |
| WA-IMP-020 | PreviewSign inner-attestation test fixture carries an extra algorithm field; minimal flags-only coverage remains useful | Fixture rationale and decoder evidence |
| WA-IMP-021 | Two-credential previewSign test does not separately assert that its returned ID belongs to the allow list before choosing a verification key | Test ID-association assertions and verification summary |

The related CTAP2 `GetAssertionResponse.User` documentation repeats an inaccurate discoverable/UV
restriction and is assigned to the later CTAP2 documentation cycle (`WA-XREF-001`). Its user handle and
identifying display fields need separate conditions.

PRF UV enforcement remains a coordinated policy follow-up. The standard requires the verified variant;
the current workaround is to request `UserVerification = Required` for evaluation. This does not resolve
all PRF conformance or sensitive-result ownership gaps. Checking credential-protection output after
creation is likewise not equivalent to preventing inadequately protected credential creation.

## Verification boundary

Final checks passed:

- Full solution build, including unit/integration project compilation: **zero warnings and errors**.
- Unit tests: **3,998 executed, zero failures, five existing skips** across 13 projects. WebAuthn:
  **371 executed, zero failures or skips**. Compiler/analyzer warnings: **zero**.
- `docs-qa`: 73 active documents and 10 package READMEs; architecture map/image freshness checks passed.
- Complexity: no new or worsened violations; two unchanged methods remain above the configured thresholds.
- Scoped formatting verification passed. Workspace loading reports pre-existing duplicate public-API
  additional-file entries; the full build has no compiler/analyzer warnings.
- Roslyn comparison confirms unchanged executable tokens and directives in all **78 edited C# files**,
  allowing only the formatter's namespace-import ordering change in one file. No API signature or
  runtime behavior fix is included.
- Three compile-only example groups passed against fresh SDK assemblies: the complete README demo;
  prompt/touch alternatives; and configuration, signing and shared Markdown/XML intent fragments with
  explicit typed application prerequisites. No example was executed against a device or RP.
- An isolated .NET conversion check resolved WA-IMP-016's presence/length uncertainty. This establishes
  the conversion result, not live blob-storage behavior.

Fresh independent reviews covered every edited semantic unit. Required corrections were applied and
re-reviewed, including specification-version attribution, test-evidence overclaims, a missing hardware
test trait in a contributor skeleton, and precise blob-result wording. The 51 overlapping investigation
findings are dispositioned; **21 implementation/test follow-ups** and one CTAP2 documentation dependency
remain explicitly deferred above.

No live WebAuthn ceremony, PIN setup, credential creation/deletion, reset or cross-platform execution has
been performed in this cycle. Source/specification review and compilation do not establish hardware
behavior, all-platform support, certification or complete WebAuthn/CTAP conformance.

The existing migration-map YAML parse issue (`DOC-002`, line 101 column 621) is outside this cycle's edits.
Core, multi-key discovery and deeper SCP investigations remain deferred; see the
[OATH checkpoint](oath-documentation-audit-checkpoint.md).

Detailed local task packets, file coverage, original evidence, review records and handoffs are maintained
in the campaign workspace under `yubikit-docs-audit/runs/webauthn-001/`. This tracked checkpoint preserves
the scope, outcomes and follow-up dependencies in the repository backup.
