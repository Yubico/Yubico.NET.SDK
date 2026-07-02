# ISA: PreviewSign Public API Revamp

## Problem

The current v2 `PreviewSign` API has drifted from the reviewed legacy .NET `develop` design and from the Swift/Android model SDKs. The generic public surface is too ARKG-opinionated: it exposes typed `CoseSignArgs` in the main signing params, validates ARKG-specific shapes in generic paths, and risks implying that ARKG is the canonical `previewSign` workflow rather than one experimental algorithm using the extension.

The legacy .NET SDK, reviewed with WebAuthn expertise, established a more correct contract: `previewSign` is algorithm-agile, passes algorithm-specific bytes through unchanged, preserves future/unknown algorithms, and exposes only the generic extension model publicly. For v2, ARKG derivation and helper APIs may remain public for internal testing and long-horizon experimentation, but they must not define the generic `PreviewSign` API.

## Vision

The v2 SDK presents `PreviewSign` as a clean, draft/experimental, algorithm-agile WebAuthn/CTAP extension. Developers can use the generic API without learning ARKG, while internal teams can still exercise public ARKG helpers for derivation and verification experiments. The resulting surface feels like the legacy .NET API evolved into modern v2 architecture, not a separate ARKG-specific feature bolted onto WebAuthn.

## Out of Scope

This work does not stabilize ARKG as a supported production algorithm. It does not remove public ARKG helpers from v2. It does not solve all hardware gaps around multi-credential probing or full WebAuthn authentication integration unless already covered by existing behavior. It does not redesign the entire Fido2/WebAuthn extension framework. It does not require copying the legacy CBOR parser implementation; v2-native CBOR parsing is acceptable if the wire behavior, public semantics, and documentation contract match legacy.

## Principles

- Generic extension APIs must be algorithm-agile.
- Experimental algorithms must not hard-code the shape of a draft extension.
- Public documentation should describe the WebAuthn/CTAP contract, not an internal test algorithm.
- Unknown/future well-formed data should be preserved as raw bytes where the spec allows it.
- ARKG helpers can exist, but only as explicitly ARKG-scoped experimental convenience APIs.
- Legacy reviewed source and model SDKs are higher-confidence design inputs than current accidental v2 drift.
- Tests should verify wire compatibility and public API intent, not just implementation convenience.
- Legacy source XML documentation should be treated as the prose authority for generic `previewSign` docs.

## Constraints

- Use `../Yubico.NET.SDK-Legacy` `develop` source as primary API/docs authority.
- Use `../yubikit-swift` and optionally `../yubikit-android` as corroborating v2 model SDKs.
- Use legacy source XML comments for near-1:1 generic `previewSign` documentation; do not use stale generated DocFX YAML as source of truth.
- CBOR implementation may use v2 architecture and parsing helpers, but encoded keys, payload shape, pass-through semantics, and malformed-data behavior must match legacy intent.
- Use repo build/test wrappers only: `dotnet toolchain.cs build/test`; never `dotnet build` or `dotnet test` directly.
- Keep ARKG derivation/helpers public for v2 internal testing.
- Do not let ARKG helpers appear as the canonical generic `PreviewSignSigningParams` path.
- Respect Fido2/WebAuthn dependency direction: WebAuthn may depend on Fido2; Fido2 must not depend on WebAuthn.
- Do not commit unrelated changes.
- Run `/DevTeam` review/fix before commit after targeted tests pass.
- `/Ping` Dennis after each successful execution phase; use `/PingAndWait` only if a blocking decision is needed.

## Goal

Revamp the v2 `PreviewSign` public-facing API so the generic Fido2 and WebAuthn surfaces match the reviewed legacy .NET design and Swift/Android model SDKs, while preserving public ARKG helper APIs as explicitly experimental conveniences. The work is complete when generic `previewSign` uses raw algorithm-specific `additionalArgs`, docs are near-1:1 with legacy source XML for generic `previewSign`, docs no longer present ARKG as canonical, targeted tests pass, `/DevTeam` review findings are resolved, and the intended changes are committed.

## Criteria

- [ ] ISC-1: Generic Fido2 signing params expose raw optional `AdditionalArgs` rather than requiring typed `CoseSignArgs`.
- [ ] ISC-2: Generic WebAuthn signing params expose raw optional `AdditionalArgs` and pass it unchanged to Fido2.
- [ ] ISC-3: Registration input remains ordered algorithms plus flags, with docs aligned to legacy `PreviewSignOptions` intent.
- [ ] ISC-4: Registration output preserves raw key handle, raw COSE public key bytes, selected algorithm, and attestation object data.
- [ ] ISC-5: Authentication output preserves raw signature bytes without claiming DER, ECDSA `r||s`, or ARKG-specific format generically.
- [ ] ISC-6: ARKG derivation and verification helpers remain public.
- [ ] ISC-7: ARKG helper docs explicitly frame them as experimental/convenience APIs, not the generic `previewSign` contract.
- [ ] ISC-8: Generic docs state `tbs` and `additionalArgs` are algorithm-specific and passed through unchanged, matching legacy source XML wording as closely as v2 naming allows.
- [ ] ISC-9: Generated-key output containing a concrete non-ARKG algorithm identifier and opaque COSE public key fixture is preserved through generic output APIs instead of being rejected for being non-ARKG.
- [ ] ISC-10: Fido2 unit tests verify exact CBOR for registration and authentication, including optional raw `additionalArgs`.
- [ ] ISC-11: WebAuthn unit tests verify raw `additionalArgs` passthrough through adapter boundaries.
- [ ] ISC-12: Existing ARKG derivation/KAT tests still pass or are updated to use ARKG-specific helper paths.
- [ ] ISC-13: Public docs and XML comments contain no stale generic references to typed `CoseSignArgs` as the required/canonical path.
- [ ] ISC-14: `src/Fido2/CLAUDE.md` and `src/WebAuthn/CLAUDE.md` are updated if affected API/docs guidance changes.
- [ ] ISC-15: Targeted Fido2 PreviewSign tests pass through `dotnet toolchain.cs test --project Fido2 --filter "FullyQualifiedName~PreviewSign"`.
- [ ] ISC-16: Targeted WebAuthn PreviewSign tests pass through `dotnet toolchain.cs test --project WebAuthn --filter "FullyQualifiedName~PreviewSign"`.
- [ ] ISC-17: `/DevTeam` review is run after tests pass.
- [ ] ISC-18: All verified `/DevTeam` findings are fixed or explicitly rejected with rationale.
- [ ] ISC-19: Targeted tests are rerun after `/DevTeam` fixes.
- [ ] ISC-20: Commit contains only intended PreviewSign/API/docs/test changes.
- [ ] ISC-21: Legacy generic PreviewSign documentation is ported near-1:1 from source XML comments for `PreviewSignOptions`, generate-key input, sign input, generated-key output, and signature output.
- [ ] ISC-22: CBOR algorithm handling preserves verified legacy wire keys from `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignExtension.cs`: registration algorithm key `3`, flags key `4`, authentication key handle key `2`, `tbs`/signature key `6`, authentication `additionalArgs` key `7`, and generated-key attestation-object key `7` in the MakeCredential output context.
- [ ] ISC-23: Generated-key decode combines signed extension output for the selected algorithm with unsigned extension output for attestation object, matching legacy semantics.
- [ ] ISC-23.1: Before authoring CBOR tests, legacy source is re-read to verify the integer keys and signed/unsigned output split used as the test oracle.
- [ ] ISC-23.2: Unknown/future algorithm preservation is verified with at least one concrete non-ARKG algorithm identifier and opaque COSE public key payload that round-trips through generic output APIs without requiring ARKG parsing.
- [ ] ISC-23.3: Tests separately verify key `7` in authentication input is `additionalArgs` and key `7` in MakeCredential unsigned output is `attestationObject`, preventing context-insensitive parsing.
- [ ] ISC-23.4: ARKG helpers provide or preserve a public bridge that emits raw CBOR `additionalArgs` bytes usable by the generic `AdditionalArgs` property without making generic params depend on `CoseSignArgs`.
- [ ] ISC-23.5: Generated-key key handle and COSE public key are extracted from attested credential data inside the attestation object, not from separate `previewSign` extension map keys.
- [ ] ISC-23.6: CBOR encoder/decoder code keeps MakeCredential and GetAssertion key constants or parsing paths context-specific so key `7` cannot be handled by a shared context-free meaning.
- [ ] Anti-ISC-24: Generic PreviewSign docs must not describe ARKG as the only supported algorithm.
- [ ] Anti-ISC-25: Generic PreviewSign signing params must not require callers to construct ARKG-specific typed args.
- [ ] Anti-ISC-26: Do not remove public ARKG helpers in v2.
- [ ] Anti-ISC-27: Do not use direct `dotnet test` or `dotnet build`.
- [ ] Anti-ISC-28: Do not stage or commit unrelated dirty worktree changes.
- [ ] Anti-ISC-29: Do not copy stale legacy generated DocFX YAML ARKG-specific docs into the generic v2 docs.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
|---|---|---|---|---|
| ISC-1 | API inspection | Read Fido2 signing params public members | `AdditionalArgs` present; generic dependency on `CoseSignArgs` absent | `Read` / `Grep` |
| ISC-2 | API inspection | Read WebAuthn signing params and adapter | raw `AdditionalArgs` passed unchanged | `Read` / `Grep` |
| ISC-3 | API inspection | Read registration input and XML docs | algorithms ordered, flags documented | `Read` |
| ISC-4 | API inspection | Read registration output | raw key/public key/algorithm/attestation preserved | `Read` |
| ISC-5 | Docs/API inspection | Grep generic docs for signature format claims | no generic DER/ARKG-only signature wording | `Grep` |
| ISC-6 | API inspection | Grep ARKG helper public types/members | helpers still public | `Grep` |
| ISC-7 | Docs inspection | Read ARKG helper XML docs | experimental/convenience framing present | `Read` |
| ISC-8 | Docs inspection | Compare generic docs to legacy source XML | near-1:1 wording where names map | `Read` / `Grep` |
| ISC-9 | Unit tests | Unknown/future algorithm data test using synthetic non-ARKG algorithm/public-key fixtures shaped from legacy generated-key output semantics | raw algorithm and public key remain observable through generic output APIs for that fixture | `dotnet toolchain.cs test --project Fido2 --filter "FullyQualifiedName~PreviewSign"` |
| ISC-10 | Unit tests | Fido2 PreviewSign CBOR tests | passes | `dotnet toolchain.cs test --project Fido2 --filter "FullyQualifiedName~PreviewSign"` |
| ISC-11 | Unit tests | WebAuthn PreviewSign adapter tests | passes | `dotnet toolchain.cs test --project WebAuthn --filter "FullyQualifiedName~PreviewSign"` |
| ISC-12 | Unit tests | ARKG helper/KAT tests | passes or intentionally updated | `dotnet toolchain.cs test --project Fido2 --filter "FullyQualifiedName~PreviewSign"` |
| ISC-13 | Docs grep | Search `CoseSignArgs` in public docs/XML | no stale generic references | `Grep` |
| ISC-14 | Docs readback | Read module docs | affected guidance updated | `Read` |
| ISC-15 | Test command | Fido2 targeted test run | exit 0 | `bash` |
| ISC-16 | Test command | WebAuthn targeted test run | exit 0 | `bash` |
| ISC-17 | Review | Run `/DevTeam` | review completes | `/DevTeam` |
| ISC-18 | Review fix verification | Inspect findings and fixes | all accepted findings resolved | `Read` / tests |
| ISC-19 | Regression test | Rerun targeted tests after fixes | exit 0 | `bash` |
| ISC-20 | Git inspection | `git status`, `git diff`, staged files | only intended files | `bash` |
| ISC-21 | Docs comparison | Compare v2 generic docs to legacy source XML | near-1:1 for generic semantics | `Read` / `Grep` |
| ISC-22 | CBOR tests | Assert exact key mapping and optional args | pass | unit tests |
| ISC-23 | Decode tests | Assert signed/unsigned generated-key decode pairing | pass | unit tests |
| ISC-23.1 | Source verification | Re-read legacy `PreviewSignExtension.cs` before test edits | key constants and output split documented in verification notes | `Read` |
| ISC-23.2 | Unit tests | Decode/preserve synthetic non-ARKG generated key output shaped from legacy signed/unsigned output contract | raw algorithm/public key preserved; no ARKG helper required | `dotnet toolchain.cs test --project Fido2 --filter "FullyQualifiedName~PreviewSign"` |
| ISC-23.3 | Unit tests | Context-specific key `7` tests for authentication input and registration output | both contexts pass independently | `dotnet toolchain.cs test --project Fido2 --filter "FullyQualifiedName~PreviewSign"` |
| ISC-23.4 | Unit tests/API inspection | ARKG raw additionalArgs bridge exists and is usable from generic signing params | ARKG helper bytes pass through generic encoder unchanged | `Read` / targeted tests |
| ISC-23.5 | Decode tests/API inspection | Generated-key decode derives key handle/public key from attested credential data | no invented public-key extension map key | `Read` / targeted tests |
| ISC-23.6 | Code inspection/unit tests | MakeCredential and GetAssertion key handling are context-specific | no shared context-free key `7` interpretation | `Read` / targeted tests |
| Anti-ISC-24 | Docs grep | Search ARKG-only wording | absent in generic docs | `Grep` |
| Anti-ISC-25 | API grep | Search generic params for required ARKG typed arg | absent | `Grep` |
| Anti-ISC-26 | API grep | Search public ARKG helper surface | present | `Grep` |
| Anti-ISC-27 | Command discipline | Review commands used | no direct `dotnet test/build` | transcript |
| Anti-ISC-28 | Git inspection | Review staged files | unrelated files unstaged | `bash` |
| Anti-ISC-29 | Docs inspection | Compare against legacy source, not stale YAML | stale ARKG YAML not copied | `Read` |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
|---|---|---|---|---|
| Fido2 Generic API Alignment | Change Fido2 PreviewSign signing params and encoder-facing API to raw `AdditionalArgs` | ISC-1, ISC-8, ISC-22, Anti-ISC-25 | none | true |
| WebAuthn Generic API Alignment | Change WebAuthn signing params and adapter to raw passthrough | ISC-2, ISC-11 | Fido2 Generic API Alignment | false |
| Registration/Output Preservation | Ensure registration inputs/outputs retain legacy generic shape and signed/unsigned generated-key decode semantics | ISC-3, ISC-4, ISC-9, ISC-23, ISC-23.5 | Legacy Source Reverification | true |
| ARKG Helper Isolation | Keep ARKG helpers public but separate from generic API and provide/preserve a raw-byte bridge for `additionalArgs` | ISC-6, ISC-7, ISC-12, ISC-23.4, Anti-ISC-26 | Fido2 Generic API Alignment | true |
| Legacy Source Reverification | Re-read legacy source files immediately before implementation to establish CBOR/documentation oracle | ISC-21, ISC-22, ISC-23.1 | none | false |
| Legacy Documentation Port | Port/rewrite XML/module docs around generic pass-through design with near-1:1 legacy source wording | ISC-5, ISC-8, ISC-13, ISC-14, ISC-21, Anti-ISC-24, Anti-ISC-29 | Fido2 Generic API Alignment; WebAuthn Generic API Alignment; Legacy Source Reverification | true |
| Test Rewrite | Update Fido2/WebAuthn tests for raw passthrough, concrete unknown algorithm preservation, context-specific key `7`, signed/unsigned output split, generated-key attested credential data extraction, and ARKG helper compatibility | ISC-9, ISC-10, ISC-11, ISC-12, ISC-15, ISC-16, ISC-22, ISC-23, ISC-23.2, ISC-23.3, ISC-23.4, ISC-23.5, ISC-23.6 | Fido2 Generic API Alignment; WebAuthn Generic API Alignment; Registration/Output Preservation; ARKG Helper Isolation | false |
| Review/Fix Loop | Run DevTeam, resolve findings, rerun tests | ISC-17, ISC-18, ISC-19 | tests passing | false |
| Commit | Stage explicit intended files and commit | ISC-20, Anti-ISC-28 | Review/Fix Loop | false |
| Phase Pings | Notify user after each successful execution phase | continuity requirement | each phase success | false |

Execution phases are fixed for auditability:

1. ISA + Cato plan approval.
2. Legacy source reverification.
3. Fido2 generic API alignment.
4. ARKG helper isolation.
5. WebAuthn generic API alignment.
6. Legacy documentation port.
7. Targeted PreviewSign tests.
8. DevTeam review/fix loop and retest.
9. Commit.

## Decisions

- 2026-06-04: Legacy .NET `develop` source is the primary source of truth for generic PreviewSign API/docs; generated legacy doc YAML may be stale and must not override source.
- 2026-06-04: Swift and Android are corroborating model SDKs for v2-era design, especially around `additionalArgs` raw passthrough.
- 2026-06-04: ARKG remains public in v2 because v2 will not ship soon and public helpers are useful internally.
- 2026-06-04: Public ARKG helpers are an intentional v2 divergence from legacy, driven by user preference and internal testing needs. Legacy fidelity applies to the generic `previewSign` contract and docs, not to whether ARKG helpers are public in this v2 branch.
- 2026-06-04: ARKG must be isolated as an explicit helper/convenience layer and must not define the generic PreviewSign contract.
- 2026-06-04: v2-native CBOR parsing/encoding is allowed, but wire shape and public semantics must match legacy.
- 2026-06-04: Execution will be phase-based with `/Ping` after each successful phase, `/DevTeam` review/fix before commit, and explicit-file staging only.
- 2026-06-04: Legacy source XML was live-read before implementation planning. `PreviewSignExtension.EncodeSignInput` documents `toBeSigned` and `additionalArgs` as algorithm-specific values encoded unchanged, and `GetAssertionParameters.AddPreviewSignExtension` documents them as passed through unchanged.
- 2026-06-04: Legacy source XML was live-read for generic-ness. ARKG-specific derivation helpers were found under legacy test utilities, not the main generic `PreviewSignExtension` public docs.
- 2026-06-04: Legacy wire keys were live-read from `PreviewSignExtension.cs`: MakeCredential keys `Algorithm = 3`, `Flags = 4`, `AttestationObject = 7`; GetAssertion keys `KeyHandle = 2`, `TbsOrSignature = 6`, `AdditionalArgs = 7`.
- 2026-06-04: Unknown/future algorithm preservation tests will use synthetic fixtures. They prove the generic parser/API does not force ARKG parsing; they do not claim hardware support for non-ARKG previewSign algorithms.
- 2026-06-04: Swift and Android corroboration reduces single-oracle risk: Swift exposes WebAuthn `SigningParams` with raw `additionalArgs: Data?`; Android exposes raw `additionalArgs` bytes and raw integer algorithms. These models support the same generic API shape as legacy .NET.
- 2026-06-04: Targeted test commands are scoped unit-test commands. `--smoke` is reserved for integration-test runs and is not required for filtered unit-test verification unless integration tests are invoked.

## Changelog

- Conjectured: The current v2 API should make typed ARKG args the canonical signing params because ARKG is the only actively tested algorithm.
  Refuted by: Legacy .NET source, Swift, and Android all expose raw algorithm-specific `additionalArgs` in the generic API.
  Learned: Test convenience leaked into public generic API design.
  Criterion now: ISC-1, ISC-2, ISC-8, Anti-ISC-25 require raw passthrough generic API.

- Conjectured: Public ARKG helpers should be removed to match legacy most closely.
  Refuted by: User clarified v2 can keep ARKG public because it will not ship soon and is useful internally.
  Learned: The ideal state is not strict removal; it is isolation from the generic contract.
  Criterion now: ISC-6, ISC-7, Anti-ISC-26 preserve public ARKG helpers with experimental framing.

- Conjectured: Matching legacy means copying legacy CBOR parsing structure.
  Refuted by: User clarified v2 can do CBOR parsing in its own v2 way.
  Learned: The invariant is legacy public semantics and wire behavior, not identical parser internals.
  Criterion now: ISC-22 and ISC-23 verify wire keys and signed/unsigned semantics while allowing v2 implementation style.

## Verification

Plan-level verification recorded before implementation:

- Live-read legacy generic docs in `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignExtension.cs` lines 64-108: generate-key docs use ordered algorithms and flags; sign-input docs state `toBeSigned` and `additionalArgs` are algorithm-specific and encoded unchanged.
- Live-read legacy public WebAuthn/Fido2 integration docs in `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/GetAssertionParameters.cs` lines 398-427: `toBeSigned` and `additionalArgs` are algorithm-specific and passed through unchanged.
- Live-read legacy wire constants in `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignExtension.cs` lines 48-62: MakeCredential keys are `3`, `4`, `7`; GetAssertion keys are `2`, `6`, `7`.
- Live-read current v2 drift in `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/src/Fido2/src/Extensions/PreviewSignExtension.cs` lines 105-148 and `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs` lines 49-72: generic signing params currently expose typed `CoseSignArgs`.
- Live-grepped Swift corroboration in `/Users/Dennis.Dyall/Code/y/yubikit-swift/YubiKit/YubiKit/FIDO/WebAuthn/Extensions/WebAuthnPreviewSign.swift`: `SigningParams` exposes `additionalArgs: Data?` and registration/authentication types use `generateKey`, `signByCredential`, `generatedKey`, and `signature`.
- Live-grepped Swift/Android model SDKs for `previewSign`, `additionalArgs`, and ARKG naming; results confirmed ARKG is mentioned as an example/algorithm identifier rather than the generic signing params type.
- Cato review concern accepted: key `7` has different meanings by context, so implementation must avoid a context-insensitive parser and tests must verify both contexts separately.
- Cato review concern accepted: generated-key key handle and public key are not separate `previewSign` map keys; they are extracted from attested credential data inside the attestation object in the unsigned output.
- Cato review concern accepted: source reverification is intentionally redundant as a pre-edit guardrail, even though plan-level source reads were already performed.

Expected execution evidence:

- Cato review JSON for this ISA.
- Fido2 targeted test command output with exit 0.
- WebAuthn targeted test command output with exit 0.
- Grep/read evidence that generic signing params use `AdditionalArgs`.
- Grep/read evidence that stale generic `CoseSignArgs` docs are removed.
- Grep/read evidence that generic docs are near-1:1 with legacy source XML.
- `/DevTeam` review summary and resolved findings.
- `git status` and `git diff` evidence before commit.
- Commit hash for the final intended change set.
