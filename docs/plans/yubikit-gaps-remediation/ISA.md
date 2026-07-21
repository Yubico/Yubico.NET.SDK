---
task: "Remediate verified major YubiKit v1 to v2 gaps"
slug: 20260721-122020_yubikit-major-gap-remediation
project: Yubico.NET.SDK
effort: E4
effort_source: explicit
phase: execute
progress: 0/105
mode: orchestrated-parallel
started: 2026-07-21T12:20:20Z
updated: 2026-07-21T12:31:13Z
---

## Problem

The point-in-time analysis in `docs/migration/v1-to-v2-gaps.md` identified major public API and observable-behavior gaps between the v1 SDK and an older YubiKit v2 integration branch. The current `yubikit-gaps` branch is based on the newer `yubikit-protocol-refactor` tip, so every finding may have changed and must be re-verified before code is written. Confirmed gaps should be restored in a way that preserves the v2 architecture, security guarantees, asynchronous model, and house style.

The work spans foundational Core APIs and five applet modules. Parallel execution creates integration risks: branches can diverge, public API designs can conflict, test runners can contend, and a physical YubiKey cannot safely serve concurrent hardware tests. Each module therefore needs an isolated worktree, test-first implementation, cross-vendor DevTeam review, an independent clean review, and serialized integration back into `yubikit-gaps`.

## Vision

V1 consumers can migrate to YubiKit v2 without losing the confirmed high-impact capabilities in scope, while v2 retains its explicit async-first and module-oriented design. Each restored capability is tested as a public contract, reviewed independently, and integrated through a mechanically verified branch. The final repository-wide audit finds no correctness, security, DRY, or near-term maintenance defects introduced by the effort.

## Out of Scope

- U2F/CTAP1 registration and authentication. This is explicitly deferred.
- Adding .NET Framework or `netstandard` target frameworks.
- Adding synchronous facades over the v2 asynchronous API.
- Reintroducing the v1 global `KeyCollector` delegate. Module-appropriate retry and notification APIs may be added where a confirmed capability gap requires them.
- Minor and cosmetic findings from the handoff report unless they are necessary to implement or test an in-scope major finding correctly.
- Reworking features already present on `yubikit-gaps`; re-verification must close stale findings without churn.
- Modifying or exercising existing real YubiHSM Auth credentials during hardware verification.

## Principles

- The current branch and executable tests outrank the historical handoff report.
- Public APIs are contracts; additive compatibility is preferred over breaking replacement.
- Restore capabilities, not v1 implementation accidents.
- Secret material has explicit ownership, bounded lifetime, and deterministic zeroization.
- Tests prove protocol bytes and observable behavior, not framework mechanics.
- Independent review findings are evidence to verify, not instructions to accept blindly.
- Parallel development ends at the integration boundary; merges and hardware access are serialized.
- The smallest complete implementation is better than a broad speculative abstraction.

## Constraints

- All work starts from `yubikit-gaps` at `21e61cc6ad4fd95022b750ac4741c3511016af65` or a documented later integrated commit.
- Every historical finding is re-verified against current v2 code and v1 `develop` before implementation.
- Module work occurs in dedicated git worktrees and branches. The orchestrator alone merges reviewed work back into `yubikit-gaps`.
- Core completes review and integration before dependent PIV and YubiOTP worktrees are created.
- Build commands use `dotnet toolchain.cs build`; test commands use `dotnet toolchain.cs test`.
- Hardware operations are serialized and use only an explicitly disposable YubiHSM Auth credential on the connected Alpha 58 YubiKey.
- Tests requiring insertion, removal, or touch are not run unattended.
- Sensitive buffers follow the repository zeroization rules in `AGENTS.md` and module guidance.
- A module is not complete until its DevTeam cross-vendor review and separate independent review both return a clean pass after fixes.
- Reviewer agents are read-only. Only Engineer agents or the orchestrator may edit.
- Each module loop is limited to three Engineer/DevTeam review iterations; unresolved high-severity findings block integration.
- Existing unrelated worktree changes are never reverted or included in module commits.

## Goal

Re-verify and remediate every in-scope major finding from `docs/migration/v1-to-v2-gaps.md` on `yubikit-gaps`, with tested public contracts, safe hardware evidence where required, two clean independent reviews per workstream, serialized integration, and a clean final CodeAudit, full build, test, and formatting gate. Completion means every stable ISC below is checked with mechanical evidence or explicitly closed by a recorded current-branch disposition.

## Criteria

### Current-State Verification

- [ ] ISC-1: Every in-scope historical Major finding has a current-branch disposition recorded in this ISA Verification section as `confirmed`, `already resolved`, `intentionally excluded`, or `reclassified` with file evidence.

### Core

- [ ] ISC-2: A public, replaceable AES-GCM primitive extension point exists and a unit test proves a supplied implementation is invoked.
- [ ] ISC-3: A public, replaceable ECDH primitive extension point exists and a unit test proves a supplied implementation is invoked.
- [ ] ISC-4: A public, replaceable CMAC primitive extension point exists and a unit test proves a supplied implementation is invoked.
- [ ] ISC-5: The default cryptography-provider path passes known-answer tests without custom providers.
- [ ] ISC-6: A public sequential TLV reader passes a consecutive typed-value advancement vector test.
- [ ] ISC-7: A public TLV writer passes a consecutive-value encoding vector test.
- [ ] ISC-7.1: A public TLV writer passes a nested-value encoding vector test.
- [ ] ISC-8: The sequential TLV API rejects malformed tag encodings with its documented exception type.
- [ ] ISC-8.1: The sequential TLV API rejects malformed length encodings with its documented exception type.
- [ ] ISC-8.2: The sequential TLV API rejects truncated values with its documented exception type.
- [ ] ISC-9: TLV-owned temporary buffers containing sensitive data are zeroed before release, proven by an observable disposal test or code-path inspection.
- [ ] ISC-10: Public Base16 encode/decode passes valid round-trip vectors.
- [ ] ISC-10.1: Public Base16 decode rejects malformed input.
- [ ] ISC-11: Public Base32 encode/decode passes RFC-compatible round-trip vectors.
- [ ] ISC-11.1: Public Base32 decode rejects malformed input.
- [ ] ISC-12: Public BCD encode/decode passes supported digit round-trip vectors.
- [ ] ISC-12.1: Public BCD encode rejects unsupported input.
- [ ] ISC-13: Public ModHex encode/decode passes known Yubico round-trip vectors.
- [ ] ISC-13.1: Public ModHex decode rejects unsupported characters.

### PIV

- [ ] ISC-14: Current PIV code exposes a tested way to detect PIN-protected or PIN-derived management-key state when supported by firmware.
- [ ] ISC-14.1: Current PIV code exposes a tested way to recover PIN-protected or PIN-derived management-key state when supported by firmware.
- [ ] ISC-15: Current PIV code enables a supported PIN-only mode with protocol bytes compatible with v1 behavior.
- [ ] ISC-15.1: Current PIV code disables a supported PIN-only mode with protocol bytes compatible with v1 behavior.
- [ ] ISC-16: PIN-derived management-key material is zeroed after a successful operation.
- [ ] ISC-16.1: PIN-derived management-key material is zeroed after a failed operation.
- [ ] ISC-16.2: PIN-derived management-key material is zeroed after a cancelled operation.
- [ ] ISC-17: A module-appropriate PIV retry API reports remaining PIN attempts.
- [ ] ISC-17.1: A module-appropriate PIV retry API reports remaining PUK attempts.
- [ ] ISC-17.2: A module-appropriate PIV retry API reports remaining management-key attempts.
- [ ] ISC-18: A public CHUID object decodes and encodes all supported v1 fields, proven by golden-vector round-trip tests.
- [ ] ISC-19: A public CCC object decodes and encodes all supported v1 fields, proven by golden-vector round-trip tests.
- [ ] ISC-20: A public AdminData object decodes and encodes all supported v1 fields, proven by golden-vector round-trip tests.
- [ ] ISC-21: A public KeyHistory object decodes and encodes all supported v1 fields, proven by golden-vector round-trip tests.
- [ ] ISC-22: Typed PIV read operations produce the same encoded object data as raw `GetObjectAsync` for a golden fixture.
- [ ] ISC-22.1: Typed PIV write operations produce the same command data as raw `PutObjectAsync` for a golden fixture.

### OATH

- [ ] ISC-23: OATH exposes whether password protection is configured independently of whether the current session is unlocked, proven across select and successful validation states.
- [ ] ISC-24: OATH offers a tested module-appropriate authenticate-and-retry path for operations that fail because the applet is locked.
- [ ] ISC-25: Wrong-password failures expose a dedicated public OATH exception.
- [ ] ISC-25.1: Locked-session failures expose a dedicated public OATH exception.
- [ ] ISC-25.2: The dedicated OATH exception retains structured status or retry information when the protocol supplies it.
- [ ] ISC-26: OATH authenticate-and-retry stops when its cancellation token is cancelled.
- [ ] ISC-26.1: OATH authenticate-and-retry zeroes password-derived material after completion.

### YubiOTP

- [ ] ISC-27: Static-password configuration accepts human-readable characters with an explicit keyboard layout and produces expected HID scan-code vectors.
- [ ] ISC-28: Yubico-OTP-algorithm challenge-response configuration is available through a tested public async API.
- [ ] ISC-28.1: Yubico-OTP-algorithm challenge-response calculation is available through a tested public async API.
- [ ] ISC-29: HMAC-SHA1 key inputs of invalid lengths fail before device I/O instead of being silently hashed or padded.
- [ ] ISC-29.1: Yubico OTP key inputs of invalid lengths fail before device I/O instead of being silently hashed or padded.
- [ ] ISC-30: Valid challenge-response key lengths preserve exact caller-provided key bytes through command encoding, proven by fake-connection assertions.

### YubiHSM Auth

- [ ] ISC-31: Public credential-operation retry helpers return structured retry information without parsing exception messages.
- [ ] ISC-31.1: Public management-key-operation retry helpers return structured retry information without parsing exception messages.
- [ ] ISC-31.2: Public session-key-operation retry helpers return structured retry information without parsing exception messages.
- [ ] ISC-32: Touch-requiring YubiHSM Auth operations expose an in-flight notification callback or event, proven with a fake protocol test.
- [ ] ISC-33: A disposable test credential on the connected Alpha 58 YubiKey establishes whether the list trailing byte is retries remaining or usage count.
- [ ] ISC-34: The public credential-list property name matches the hardware-verified trailing-byte semantics.
- [ ] ISC-34.1: The public credential-list property documentation matches the hardware-verified trailing-byte semantics.
- [ ] ISC-35: The disposable hardware-verification credential is deleted after the test.
- [ ] ISC-35.1: Secret inputs used by the disposable hardware-verification credential are zeroed after the test.

### Management

- [ ] ISC-36: Management exposes legacy pre-5.0 USB interface mode switching through a tested public async API.
- [ ] ISC-37: Legacy mode encoding covers supported OTP, CCID, and FIDO U2F combinations with v1-compatible bytes.
- [ ] ISC-38: Legacy challenge-response timeout is validated before device I/O.
- [ ] ISC-38.1: Legacy touch-eject is validated before device I/O.
- [ ] ISC-38.2: Legacy auto-eject timeout is validated before device I/O.
- [ ] ISC-38.3: Legacy challenge-response timeout produces v1-compatible command bytes.
- [ ] ISC-38.4: Legacy touch-eject produces v1-compatible command bytes.
- [ ] ISC-38.5: Legacy auto-eject timeout produces v1-compatible command bytes.

### Exception Taxonomy

- [ ] ISC-39: OATH public failures use the dedicated exception contract required by ISC-25.
- [ ] ISC-40: YubiOTP protocol or validation failures that callers must distinguish have a documented public module exception contract, or this ISA records evidence that existing typed Core exceptions preserve equivalent precision.
- [ ] ISC-41: YubiHSM Auth protocol or validation failures that callers must distinguish have a documented public module exception contract, or this ISA records evidence that existing typed Core exceptions preserve equivalent precision.
- [ ] ISC-42: Management protocol or validation failures that callers must distinguish have a documented public module exception contract, or this ISA records evidence that existing typed Core exceptions preserve equivalent precision.
- [ ] ISC-43: SecurityDomain secure-channel failures have a documented public typed exception contract, including retained status/cause information.
- [ ] ISC-44: OpenPGP protocol or validation failures that callers must distinguish have a documented public module exception contract, or this ISA records evidence that existing typed Core exceptions preserve equivalent precision.
- [ ] ISC-45: Core TLV failures satisfy the documented exception contract required by ISC-8.

### Review Gates

- [ ] ISC-46: Core DevTeam cross-vendor review verdict is PASS after all high- and medium-severity correctness findings are resolved.
- [ ] ISC-47: PIV DevTeam cross-vendor review verdict is PASS after all high- and medium-severity correctness findings are resolved.
- [ ] ISC-48: OATH DevTeam cross-vendor review verdict is PASS after all high- and medium-severity correctness findings are resolved.
- [ ] ISC-49: YubiOTP DevTeam cross-vendor review verdict is PASS after all high- and medium-severity correctness findings are resolved.
- [ ] ISC-50: YubiHSM Auth DevTeam cross-vendor review verdict is PASS after all high- and medium-severity correctness findings are resolved.
- [ ] ISC-51: Management DevTeam cross-vendor review verdict is PASS after all high- and medium-severity correctness findings are resolved.
- [ ] ISC-52: Exception-taxonomy DevTeam cross-vendor review verdict is PASS after all high- and medium-severity correctness findings are resolved.
- [ ] ISC-53: Core independent read-only review returns PASS with no findings.
- [ ] ISC-54: PIV independent read-only review returns PASS with no findings.
- [ ] ISC-55: OATH independent read-only review returns PASS with no findings.
- [ ] ISC-56: YubiOTP independent read-only review returns PASS with no findings.
- [ ] ISC-57: YubiHSM Auth independent read-only review returns PASS with no findings.
- [ ] ISC-58: Management independent read-only review returns PASS with no findings.
- [ ] ISC-59: Exception-taxonomy independent read-only review returns PASS with no findings.

### Build, Test, and Final Audit

- [ ] ISC-60: `dotnet toolchain.cs build --project Core` exits 0 after Core integration.
- [ ] ISC-61: `dotnet toolchain.cs test --project Core` exits 0 after Core integration.
- [ ] ISC-62: Focused PIV unit tests exit 0 after PIV integration.
- [ ] ISC-62.1: Focused OATH unit tests exit 0 after OATH integration.
- [ ] ISC-62.2: Focused YubiOTP unit tests exit 0 after YubiOTP integration.
- [ ] ISC-62.3: Focused YubiHSM unit tests exit 0 after YubiHSM integration.
- [ ] ISC-62.4: Focused Management unit tests exit 0 after Management integration.
- [ ] ISC-62.5: Focused SecurityDomain unit tests exit 0 after exception-taxonomy integration.
- [ ] ISC-62.6: Focused OpenPGP unit tests exit 0 after exception-taxonomy integration.
- [ ] ISC-63: Final CodeAudit of all changed source and test paths reports no unresolved high- or medium-severity findings.
- [ ] ISC-64: Final `dotnet toolchain.cs build` exits 0 on integrated `yubikit-gaps`.
- [ ] ISC-65: Final `dotnet toolchain.cs test` exits 0 on integrated `yubikit-gaps`.
- [ ] ISC-66: Final `dotnet format --verify-no-changes` exits 0 on integrated `yubikit-gaps`.

### Anti-Criteria

- [ ] ISC-67: Anti: this effort adds a U2F/CTAP1 session or command implementation.
- [ ] ISC-68: Anti: this effort adds synchronous wrappers using `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.
- [ ] ISC-69: Anti: this effort adds target frameworks other than the repository-configured v2 target.
- [ ] ISC-70: Anti: this effort introduces a global cross-applet `KeyCollector` callback.
- [ ] ISC-71: Anti: hardware verification reads, changes, locks, or deletes any pre-existing YubiHSM Auth credential.

## Test Strategy

| ISC range | Type | Check | Threshold | Tool |
|---|---|---|---|---|
| ISC-1 | current-state audit | Re-check each handoff Major against current v1/v2 source | Every in-scope finding dispositioned with file evidence | `git show`, Glob, Grep, Read; evidence in this ISA |
| ISC-2, ISC-3, ISC-4, ISC-5 | unit/known-answer | Custom crypto providers are invoked and defaults remain correct | All vectors pass; no secret copies escape | Core unit tests via `dotnet toolchain.cs test --project Core` |
| ISC-6, ISC-7, ISC-7.1, ISC-8, ISC-8.1, ISC-8.2, ISC-9, ISC-10, ISC-10.1, ISC-11, ISC-11.1, ISC-12, ISC-12.1, ISC-13, ISC-13.1 | contract/vector | TLV and codec public contracts | Golden vectors and malformed boundaries pass | Core unit tests |
| ISC-14, ISC-14.1, ISC-15, ISC-15.1, ISC-16, ISC-16.1, ISC-16.2, ISC-17, ISC-17.1, ISC-17.2, ISC-18, ISC-19, ISC-20, ISC-21, ISC-22, ISC-22.1 | protocol/vector/security | PIN-only commands and typed objects | Exact APDU/TLV vectors; secret zeroing reviewed | PIV fake-connection and data-object tests |
| ISC-23, ISC-24, ISC-25, ISC-25.1, ISC-25.2, ISC-26, ISC-26.1 | state/error/security | Protected/unlocked state and retry flow | State distinction and typed errors pass | OATH fake protocol tests |
| ISC-27, ISC-28, ISC-28.1, ISC-29, ISC-29.1, ISC-30 | protocol/vector | Keyboard and challenge-response encoding | Exact HID/command vectors; invalid lengths make zero I/O | YubiOTP fake-connection tests |
| ISC-31, ISC-31.1, ISC-31.2, ISC-32, ISC-33, ISC-34, ISC-34.1, ISC-35, ISC-35.1 | unit + hardware | Retry/touch contracts and trailing-byte semantics | Fake tests pass; disposable hardware credential created, observed, deleted | YubiHSM unit tests + serialized integration test |
| ISC-36, ISC-37, ISC-38, ISC-38.1, ISC-38.2, ISC-38.3, ISC-38.4, ISC-38.5 | protocol/vector | Legacy mode public API and encoding | Exact legacy command bytes | Management fake-backend tests |
| ISC-39, ISC-40, ISC-41, ISC-42, ISC-43, ISC-44, ISC-45 | API/error audit | Typed exception precision by module | Each criterion implemented or evidence-backed as already equivalent | Focused tests + public API inspection |
| ISC-46, ISC-47, ISC-48, ISC-49, ISC-50, ISC-51, ISC-52, ISC-53, ISC-54, ISC-55, ISC-56, ISC-57, ISC-58, ISC-59 | review | DevTeam and independent review | PASS; no findings for independent gate | Cross-vendor Reviewer + `pr-reviewer-readonly` |
| ISC-60, ISC-61, ISC-62, ISC-62.1, ISC-62.2, ISC-62.3, ISC-62.4, ISC-62.5, ISC-62.6, ISC-63, ISC-64, ISC-65, ISC-66 | repository verification | Builds, tests, audit, formatting | Every command exits 0; no unresolved High/Medium audit findings | Repo toolchain, CodeAudit, dotnet format |
| ISC-67, ISC-68, ISC-69, ISC-70, ISC-71 | anti-probe | Excluded architecture and hardware safety | Zero prohibited additions or existing-credential operations | Grep, diff review, hardware test fixture log |

## Features

```yaml
- name: CoreFoundation
  description: Re-verify and restore crypto extensibility, sequential TLV APIs, codecs, and typed TLV errors.
  satisfies: [ISC-2, ISC-3, ISC-4, ISC-5, ISC-6, ISC-7, ISC-7.1, ISC-8, ISC-8.1, ISC-8.2, ISC-9, ISC-10, ISC-10.1, ISC-11, ISC-11.1, ISC-12, ISC-12.1, ISC-13, ISC-13.1, ISC-45, ISC-46, ISC-53, ISC-60, ISC-61]
  depends_on: []
  parallelizable: false

- name: PivParity
  description: Re-verify and restore PIN-only behavior, module-appropriate retry reporting, and typed PIV data objects.
  satisfies: [ISC-14, ISC-14.1, ISC-15, ISC-15.1, ISC-16, ISC-16.1, ISC-16.2, ISC-17, ISC-17.1, ISC-17.2, ISC-18, ISC-19, ISC-20, ISC-21, ISC-22, ISC-22.1, ISC-47, ISC-54]
  depends_on: [CoreFoundation]
  parallelizable: true

- name: OathParity
  description: Re-verify and restore persistent protection state, authentication retry ergonomics, and typed failures.
  satisfies: [ISC-23, ISC-24, ISC-25, ISC-25.1, ISC-25.2, ISC-26, ISC-26.1, ISC-39, ISC-48, ISC-55]
  depends_on: [CoreFoundation]
  parallelizable: true

- name: YubiOtpParity
  description: Re-verify and restore keyboard-aware static passwords, Yubico OTP challenge-response, and strict key validation.
  satisfies: [ISC-27, ISC-28, ISC-28.1, ISC-29, ISC-29.1, ISC-30, ISC-40, ISC-49, ISC-56]
  depends_on: [CoreFoundation]
  parallelizable: true

- name: YubiHsmParity
  description: Re-verify and restore retry/touch behavior and hardware-verify the credential trailing byte with a disposable credential.
  satisfies: [ISC-31, ISC-31.1, ISC-31.2, ISC-32, ISC-33, ISC-34, ISC-34.1, ISC-35, ISC-35.1, ISC-41, ISC-50, ISC-57, ISC-71]
  depends_on: [CoreFoundation]
  parallelizable: true

- name: ManagementParity
  description: Re-verify and restore legacy pre-5.0 mode switching and typed failures.
  satisfies: [ISC-36, ISC-37, ISC-38, ISC-38.1, ISC-38.2, ISC-38.3, ISC-38.4, ISC-38.5, ISC-42, ISC-51, ISC-58]
  depends_on: [CoreFoundation]
  parallelizable: true

- name: ExceptionTaxonomy
  description: Reconcile public typed error precision across changed modules plus SecurityDomain and OpenPGP after module integrations.
  satisfies: [ISC-39, ISC-40, ISC-41, ISC-42, ISC-43, ISC-44, ISC-45, ISC-52, ISC-59]
  depends_on: [PivParity, OathParity, YubiOtpParity, YubiHsmParity, ManagementParity]
  parallelizable: false

- name: IntegratedQualityGate
  description: Serialize merges, run CodeAudit, fix verified findings, and pass full build/test/format gates.
  satisfies: [ISC-1, ISC-62, ISC-62.1, ISC-62.2, ISC-62.3, ISC-62.4, ISC-62.5, ISC-62.6, ISC-63, ISC-64, ISC-65, ISC-66, ISC-67, ISC-68, ISC-69, ISC-70]
  depends_on: [ExceptionTaxonomy]
  parallelizable: false
```

## Decisions

- 2026-07-21 12:20 UTC: Use E4 because the effort changes multiple public packages, includes security-sensitive credential flows and hardware verification, and requires parallel branch integration plus independent review.
- 2026-07-21 12:20 UTC: Treat `docs/migration/v1-to-v2-gaps.md` as hypotheses. Every agent must re-verify current code before implementing; stale findings are closed with evidence rather than recreated.
- 2026-07-21 12:20 UTC: Defer U2F by explicit user instruction. Preserve the v2 async-only and target-framework architecture and do not resurrect the global v1 `KeyCollector`.
- 2026-07-21 12:20 UTC: Core integrates first. Phase-one module branches are created from the post-Core integrated commit to avoid duplicating foundational types and to reduce merge conflicts.
- 2026-07-21 12:20 UTC: Hardware verification uses a newly created disposable YubiHSM Auth credential only. Existing credentials are outside the test surface.
- 2026-07-21 12:20 UTC: The 105 ISCs are below the E4 soft floor of 128. Further splitting would manufacture implementation-detail probes rather than independently falsifiable outcomes; every identified major gap, review gate, integration gate, and anti-criterion already has a named mechanical probe.

## Changelog

- 2026-07-21 conjectured: the historical gap report can directly seed implementation work. / refuted by: the report itself identifies an older v2 baseline while `yubikit-gaps` is based on the later `yubikit-protocol-refactor` tip. / learned: branch archaeology must precede remediation or agents may recreate features already merged or reverse intentional refactors. / criterion now: ISC-1 requires a current-state disposition with file evidence for every in-scope Major finding.
- 2026-07-21 conjectured: all module branches can start from the initial `yubikit-gaps` commit. / refuted by: PIV and YubiOTP may consume restored Core TLV, codec, or cryptography contracts, which would force duplicate implementations or late rebases. / learned: foundational API changes should cross both review gates and integrate before dependent worktrees branch. / criterion now: the CoreFoundation feature is a declared dependency of every phase-one module feature.
- 2026-07-21 conjectured: 71 broad criteria were sufficiently atomic for orchestration. / refuted by: the E4 completeness probe identified independently failing operations and boundaries in 23 criteria. / learned: protocol configuration, execution, cleanup, and per-module tests need separate evidence even when implemented together. / criterion now: stable child IDs split those criteria without renumbering their parents.

## Verification

Verification evidence is appended as each stable ISC passes. No criterion is checked based only on an agent summary; command output, source locations, reviewer verdicts, or hardware observations must be recorded here. The initial E4 completeness pass found and corrected compound criteria before implementation began.

- E4 scaffold gate (2026-07-21): independent CheckCompleteness agent returned `PASS`; 12/12 sections present, 105 unique atomic criteria, 0 granularity violations, 0 ID-stability violations, 5 anti-criteria, and complete Feature/Test Strategy traceability. The 105/128 tier-floor shortfall is the acknowledged non-blocking soft warning recorded in Decisions.
