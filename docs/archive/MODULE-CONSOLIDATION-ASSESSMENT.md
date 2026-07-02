# Module Consolidation Assessment

This document captures the read-only module exploration performed before v2 consolidation work. It is a baseline for future refactors, not an instruction to change source code immediately.

The assessment should be read together with `docs/SDK-HOUSE-STYLE.md`.

## Execution Governance

All consolidation refactors from this assessment execute on branch `yubikit-consolidation`, based from `yubikit-applets` at commit `bfc6bdd5`.

Before any implementation, verification, integration, or agent delegation, the orchestrator must run `git status --short --branch` and confirm the worktree is on `yubikit-consolidation`. If the branch differs, stop before changing source code.

Integration work is read-only by default. Destructive or persistent-state tests are skipped completely for this consolidation program unless a later human decision supersedes this rule in writing.

The YubiKey 5.8 beta hardware target is serial `103`; Management `GetDeviceInfoAsync` remains the firmware source of truth.

After each successful phase, send `/Ping` only after the phase commit and compact summary are complete.

## Executive Summary

The SDK has strong foundations:

- Core `ApplicationSession` and protocol ownership patterns.
- Core APDU, SCP, TLV, HID, logging, and platform primitives.
- Public `IYubiKey` extension creation patterns in most modules.
- Hardware integration infrastructure through `Tests.Shared`.
- Good module-specific security awareness in several sensitive areas.

The main inconsistency is not lack of architecture. It is inconsistent architectural rhythm:

- Some modules have clean session/backend separation.
- Some modules build protocol commands inline in large session classes.
- Some modules use helpers well, while others duplicate parsing, credential handling, or test setup.
- Some module documentation is stale relative to the v2 source.

The consolidation goal is to make modules read like one intentional SDK without recreating the v1 problem of hiding protocol behavior behind operation-specific command classes.

## Key Learning

The SDK should not move toward classes like `AuthenticateCommand`, `PutKeyCommand`, `GetDataCommand`, or `VerifyPinCommand`.

The v1 SDK made some flows obscure by putting logic into special APDU command classes. Android and Python SDK experience points in the other direction: keep protocol flow flat where flat code is clearer.

The intended v2 consolidation style is:

```text
Session method
  -> validate inputs
  -> ensure feature support
  -> build plain APDU/DTO visibly
  -> transmit through Core protocol/backend
  -> parse response with small pure helper if needed
  -> zero sensitive buffers in finally
```

Future refactors should standardize this flat flow. They should not introduce a command-object hierarchy.

## Cross-Module Health Matrix

| Module | Overall | Complexity | Maturity | DRY | Rolling Own | Maintainability | Top Consolidation Target |
|---|---:|---:|---:|---:|---:|---:|---|
| `YubiOtp` | B+ | B | B+ | B | B | B | Extract protocol codecs from session |
| `OpenPgp` | B+ | B | B+ | B | B | B | Shared flat APDU/test seam |
| `Tests.Shared` | B | B | B | C | C | B | Move duplicated session helpers here |
| `Core` | B | B | B | C+ | B | B | Unify DI/docs/protocol lifecycle |
| `Management` | B- | B | B | B- | B | B- | Secure config payload ownership |
| `Piv` | B- | C+ | B | C | C | C+ | Flat APDU flow cleanup |
| `Fido2` | B- | C+ | B | C | C+ | B- | Canonical CTAP request pipeline |
| `WebAuthn` | B- | B- | C+ | B- | B- | C+ | Public client factory/API coherence |
| `Oath` | B- | B | B- | C | C | C+ | OATH chained-response/flat APDU seam |
| `SecurityDomain` | B- | C+ | B | C+ | B- | C+ | Split only where flat flow improves locality |
| `YubiHsm` | B- | B | B | C+ | C+ | B- | Sensitive APDU payload lifecycle |
| `Cli.Shared` | B- | A- | B- | C | C+ | B | Secure prompt and selector adoption |
| `Cli` | C+ | C | C | C | C | C | Implement global settings and split commands |
| `Cli.Commands` | C | C | C | D | C | C | Consolidate auth/parsing/session helpers |
| `Tests.TestProject` | C | B | C | C | C | C | Decide template/demo/test purpose |

## Strongest Models

- `YubiOtp` is the best model for a compact session/backend split with meaningful tests.
- `OpenPgp` is the best model for rich protocol domain models that still use Core APDU/TLV infrastructure.
- `Tests.Shared` is the right foundation for integration-test consistency, but it needs more reusable module session helpers.
- `Core` provides the essential primitives, but docs and DI/composition expectations need alignment with actual source.

## Main Pattern Drift

### SmartCard Modules

SmartCard modules differ in how much protocol flow is visible, how much lives in large session files, and how testable APDU construction is.

- `YubiOtp` and `Management` have clear backend adapters.
- `OpenPgp` has strong domain models but direct APDU methods.
- `Piv`, `Oath`, `SecurityDomain`, and `YubiHsm` have substantial inline APDU/TLV construction.

This is not automatically bad. The target is not to replace inline APDUs with command classes. The target is to make inline flow consistent, shallow, and testable.

### FIDO2 And WebAuthn

FIDO2 has good transport/session boundaries but inconsistent CTAP request construction. A request builder exists, but manual CBOR appears in many places.

WebAuthn correctly delegates much CTAP behavior to Fido2, but its public construction/factory story is unclear and docs are stale.

### CLI

CLI modules have useful shared primitives, but command files still duplicate parsing, session creation, PIN/token acquisition, confirmation, and sensitive-output behavior.

### Tests

`Tests.Shared` has the right core harness, but module-specific session helpers and shared connection wrappers are duplicated across modules.

## Consolidation Principles

1. Prefer flat protocol flow over operation-specific command classes.
2. Use plain `ApduCommand` or equivalent DTOs.
3. Keep session methods readable at the wire level.
4. Extract only small pure encode/parse helpers.
5. Add fake protocol tests that assert transmitted bytes.
6. Standardize sensitive buffer ownership and zeroing.
7. Move duplicate test harness pieces into `Tests.Shared`.
8. Repair docs after source patterns are agreed.

## Revised Priority Backlog

1. Fix sensitive buffer ownership and zeroing in `Management` and `YubiHsm`.
2. Standardize the flat APDU flow convention across SmartCard modules.
3. Add fake-protocol tests that assert actual APDU bytes from public/session-level behavior.
4. Move duplicated test harness pieces like `SharedSmartCardConnection` into `Tests.Shared`.
5. Apply flat-flow cleanup to `Oath` first because of the chained-response concern.
6. Apply flat-flow cleanup to `Piv` next, preserving partial-session readability.
7. For `SecurityDomain`, split only by feature partials or pure helpers if needed; do not introduce operation-specific command classes.
8. Standardize CTAP request-building inside `Fido2`, while keeping request construction visible.
9. Repair `WebAuthn` public factory/API coherence.
10. Consolidate CLI parsing, prompting, session, and PIN/token helpers.
11. Update stale README and module `CLAUDE.md` files after decisions are implemented.

## Module Findings

### Core

Grade: B.

Strengths:

- Provides core device, protocol, APDU, SCP, TLV, HID, platform, and crypto infrastructure.
- APDU decorator pipeline and SCP lifecycle are strong architectural primitives.
- Sensitive key classes generally use sealed disposable ownership and zeroing.

Risks:

- Documentation references DI entry points and examples that appear stale or incomplete.
- Some protocol lifecycle patterns differ between SmartCard, FIDO HID, and OTP HID.
- Duplicate checksum utilities and APDU serialization paths exist.

Consolidation targets:

- Align Core docs with actual API shape.
- Standardize protocol lifecycle and async/cancellation behavior.
- Consolidate duplicate CRC/checksum helpers.

### Management

Grade: B-.

Strengths:

- Clean multi-transport backend abstraction.
- Strong use of Core TLV/APDU and `ApplicationSession` patterns.
- Public session and DI shapes are coherent.

Risks:

- Lock-code config payloads may leave sensitive encoded bytes unzeroed.
- Backend `byte[]` APIs encourage copies.
- Some documentation and version-qualifier examples are stale.

Consolidation targets:

- Define secure config payload ownership and zeroing.
- Keep backend pattern but improve memory lifecycle.
- Isolate capability/version parsing where it improves readability.

### Piv

Grade: B-.

Strengths:

- Broad feature coverage and good security hygiene in PIN/management-key paths.
- Public `IPivSession` contract is comprehensive.
- Partial class organization keeps some domain grouping.

Risks:

- Inline APDU/TLV construction is large and hard to unit test.
- Module docs still reference v1/legacy patterns like KeyCollector that are not present in current source.
- Feature gates may be confused by PIV app version versus device firmware version.

Consolidation targets:

- Preserve flat session flow while extracting only pure encode/parse helpers.
- Add fake APDU tests for high-risk byte encodings.
- Add shared PIV integration helper for reset/auth/default credentials.

### Fido2

Grade: B-.

Strengths:

- Correctly separates HID and NFC SmartCard backends.
- Strong PIN/UV protocol abstraction.
- Broad feature and integration coverage.

Risks:

- Request construction is split between a builder and many manual CBOR writers.
- COSE modeling overlaps with Core COSE.
- Sensitive auth params and extension buffers have inconsistent zeroing contracts.

Consolidation targets:

- Establish one visible CTAP request-building convention.
- Centralize pure CBOR copy/parser utilities where duplicated.
- Clarify COSE sharing between Core and Fido2.

### WebAuthn

Grade: B-.

Strengths:

- Clear orchestration over Fido2.
- Strong test seam through `IWebAuthnBackend`.
- Good PIN/token zeroing with owned disposable token session.

Risks:

- Public client construction is unclear because the production Fido2 backend is internal.
- `WebAuthnClient` is large and mixes ceremony orchestration, validation, token handling, request mapping, and response mapping.
- Module docs reference nonexistent or renamed APIs.

Consolidation targets:

- Decide public factory/session creation story.
- Split only where it improves ceremony readability without hiding Fido2 delegation.
- Repair docs to match actual API.

### Oath

Grade: B-.

Strengths:

- Compact public API.
- Good security handling for access keys and credential secrets.
- Integration reset helper exists.

Risks:

- `OathSession` is a monolithic inline APDU/TLV implementation.
- Local OATH chained-response handling may conflict with Core default APDU chained-response behavior.
- Unit tests focus on models/parsers but not APDU bytes.

Consolidation targets:

- Investigate Core chained-response configuration for OATH `INS_SEND_REMAINING`.
- Keep APDU flow flat but add fake protocol tests.
- Consolidate Base32/otpauth parsing with CLI/example usage.

### YubiOtp

Grade: B+.

Strengths:

- Clean dual-transport backend abstraction.
- Good protocol tests and meaningful integration coverage.
- Strong sensitive buffer cleanup.

Risks:

- Session contains NDEF encoding, HMAC padding, payload assembly, backend creation, and version parsing.
- Some integration cleanup paths could leave slot state if failures occur mid-test.

Consolidation targets:

- Extract local pure protocol codecs where they reduce session noise.
- Standardize firmware/version parsing if shared with Management.
- Keep backend shape as a model for other multi-transport modules.

### OpenPgp

Grade: B+.

Strengths:

- Strong protocol domain models for KDF, attributes, app data, key templates, and OpenPGP metadata.
- Good use of Core APDU/TLV infrastructure.
- Solid security hygiene around KDF, PINs, templates, and crypto payloads.

Risks:

- Session APDU flows are not unit-testable enough.
- Some helper utilities may overlap with Core crypto/PIV/WebAuthn needs.
- Public mutable dictionary-derived models may weaken API discipline.

Consolidation targets:

- Add fake APDU tests where feasible.
- Consider shared DER/DigestInfo/OID helpers only if reuse is proven.
- Preserve OpenPGP-specific model richness.

### SecurityDomain

Grade: B-.

Strengths:

- Good docs and integration coverage around SCP03/SCP11 lifecycle.
- Reuses Core SCP/TLV/APDU infrastructure.
- Reset behavior is documented and test-harness-aware.

Risks:

- One large session owns command construction, parsing, reset, KCV crypto, cert storage, allowlists, and raw transmit bypass.
- Some broad catch/log patterns repeat.
- Test certificate helper logic is duplicated.

Consolidation targets:

- Do not introduce `PutKeyCommand` or `GetDataCommand` classes.
- Keep flow flat, but move feature areas into partials or pure helpers if locality improves.
- Extract test certificate utilities into shared test helpers.

### YubiHsm

Grade: B-.

Strengths:

- Clear single-session applet implementation.
- Uses Core APDU/TLV and `ApplicationSession` infrastructure.
- Good session-key ownership model.

Risks:

- Encoded APDU payloads containing sensitive material are not consistently zeroed after transmit.
- No fake protocol seam for byte-level APDU tests.
- Local CLI duplicates shared CLI argument parsing.

Consolidation targets:

- Add a visible sensitive-payload transmit pattern that zeros encoded payloads.
- Add fake protocol tests without changing production flow into command classes.
- Replace local CLI parser with `Cli.Shared` where applicable.

### Cli.Shared

Grade: B-.

Strengths:

- Good shared device-selection, output, confirmation, menu, and lifecycle primitives.
- Useful base for consolidating example and unified CLI behavior.

Risks:

- Secret prompts return immutable strings.
- Some examples still duplicate selectors and output helpers despite referencing `Cli.Shared`.
- No tests for shared parser/formatter/selector behavior.

Consolidation targets:

- Add secure credential prompt model if CLI work continues.
- Replace duplicated example selectors/output helpers.
- Add pure utility tests.

### Cli And Cli.Commands

Grades: C+ and C.

Strengths:

- Unified command tree and shared lifecycle base exist.
- Shared output/confirmation helpers are used.
- Many byte-array secrets are zeroed after use.

Risks:

- Global `--serial`, `--transport`, and `--interactive` settings are declared but not honored.
- Large command files duplicate parsing, prompting, session creation, and FIDO/PIV/OpenPGP auth flows.
- PINs/passwords are frequently stored as immutable strings.
- No command/helper tests found.

Consolidation targets:

- Implement or remove unused global options.
- Centralize hex/base32 parsing and secure prompts.
- Add shared session/token helpers.
- Split largest command files by command family.

### Tests.Shared

Grade: B.

Strengths:

- Strong allow-list-driven hardware authorization model.
- Good xUnit v2 integration-data attribute flow.
- Broad adoption across module integration tests.

Risks:

- Docs are stale around `YubiKeyTheory` versus current `[Theory] + [WithYubiKey]`.
- `SharedSmartCardConnection` is duplicated in multiple modules.
- Module-specific session helpers repeat patterns outside `Tests.Shared`.

Consolidation targets:

- Move shared connection wrappers into `Tests.Shared`.
- Standardize app-session helper shape.
- Bring Core integration tests under consistent allow-list handling or document exception.

### Tests.TestProject

Grade: C.

Strengths:

- Small ASP.NET Core DI demo/test host.
- Demonstrates `AddYubiKeyManager()` and `WebApplicationFactory` shape.

Risks:

- Route mismatch likely makes one test row fail.
- Hardware-dependent test lacks expected trait/harness integration.
- Docs describe it as xUnit v3/test-project template, but source uses xUnit v2 and a Web SDK sample.
- Purpose is unclear: template, demo, AOT sample, or integration test.

Consolidation targets:

- Decide the purpose before refactoring.
- Fix route mismatch and hardware traiting if it remains a test.
- Align docs with actual shape.

## Recommended First Documentation Updates

1. Adopt `docs/SDK-HOUSE-STYLE.md` as the front-door consolidation style guide.
2. Update root/module docs to avoid implying operation-specific command classes are desirable.
3. Replace stale examples in module READMEs after code decisions are made.
4. Add a short checklist to future PR descriptions referencing flat protocol flow, sensitive payload zeroing, and test strategy.

## Recommended First Code PRs

These require separate human approval before execution.

1. `YubiHsm`: zero encoded sensitive APDU payloads after transmit.
2. `Management`: zero encoded config payloads containing lock codes.
3. `Tests.Shared`: move duplicated `SharedSmartCardConnection` into shared test infrastructure.
4. `Oath`: investigate and fix/confirm OATH chained-response behavior without introducing command classes.
5. `Cli.Commands`: implement or remove unused global options.

## Risks To Watch

- Overcorrecting into v1-style command objects.
- Hiding protocol flow behind generic helpers.
- Treating docs as canonical when source has drifted.
- Adding tests that only validate guard clauses.
- Fixing DRY violations by creating abstractions that are harder to read than duplication.

## Final North Star

The SDK should feel planned and consistent, but not over-abstracted.

The common style is: flat, readable protocol flow; Core primitives; small pure helpers; explicit security cleanup; and tests that prove the bytes.
