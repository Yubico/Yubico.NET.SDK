# Application Session Lifecycle Audit Plan

## Goal

Audit protocol, connection, backend, and SCP disposal ownership across the SDK before making lifecycle changes. The audit must identify stale-reference risks where one component reads a valid protocol or connection reference, another component later disposes it, and the first component continues using a broken object.

This audit is a blocking detour before returning to the module-consolidation ISA. After the lifecycle issue is audited, fixed, reviewed, and committed, continue the grand plan in `docs/plans/module-consolidation/ISA.md`.

## Scope

Audit:

- `ApplicationSession`
- `PcscProtocol`
- `PcscProtocolScp`
- `ChainedResponseReceiver`
- `ScpProcessor`
- `ScpState`
- all `ApplicationSession` inheritors
- FIDO2 backend ownership and SCP handoff
- comparable backend patterns in Management, YubiOtp, and WebAuthn

Rename after lifecycle conclusions:

- FIDO2 `FidoHidBackend` -> `HidBackend`
- FIDO2 `SmartCardFidoBackend` -> `SmartCardBackend`

Rename sequencing:

- Commit 1: lifecycle ownership fixes and tests only.
- Commit 2: mechanical FIDO2 backend rename only.
- Keep rename churn out of the lifecycle fix diff so review and bisect stay clean.

Do not rename:

- Management `FidoHidBackend`

## Out Of Scope

- No processor singleton or caching.
- No broad APDU lifecycle framework.
- No destructive hardware tests.
- No Management backend rename.
- No connection-ownership API change unless the audit proves it is necessary and separately approved.

## Audit Questions

1. Who creates each connection?
2. Who owns each connection?
3. Who creates each protocol?
4. Who owns each protocol?
5. Who creates each backend?
6. Does each backend own or borrow its protocol?
7. Can any backend dispose a protocol still referenced by a session or wrapper?
8. Can any session replace a protocol/backend while another live object still references the old one?
9. Does SCP wrapping transfer ownership cleanly?
10. Does session disposal always reach `PcscProtocolScp.Dispose()`?
11. Does `PcscProtocolScp.Dispose()` always reach `_scpProcessor.Dispose()`?
12. Does `_scpProcessor.Dispose()` always zero SCP session state?
13. Are failure paths cleaning up newly created owned objects?
14. Do sync and async disposal paths reach the same terminal disposed state, even though their implementation paths may differ?
15. Are shared test-connection wrappers preserving ownership correctly?
16. Is every `Dispose()` / `DisposeAsync()` path idempotent and double-dispose safe?
17. Do all session inheritors override disposal correctly, rather than hiding cleanup with `new` in a way that fails through base/interface references?
18. Are stale references guaranteed to fail loudly with `ObjectDisposedException` or equivalent, rather than silently corrupting state?
19. Do secret-holding types rely only on deterministic disposal, with residual risk documented when `Dispose()` is skipped?
20. Are helper names, DI extension names, and docs validated against actual source before recording audit conclusions?
21. What happens if a protocol/session is disposed while an async APDU operation is in flight?

## Lifecycle Matrix

For each audited component, record:

- Component
- Creates connection
- Owns connection
- Creates protocol
- Owns protocol
- Creates backend
- Owns backend
- Can replace protocol
- Can replace backend
- Dispose path
- DisposeAsync path
- Double-dispose safe
- Polymorphic disposal safe
- Failure cleanup path
- Reuse-after-dispose behavior
- In-flight operation disposal behavior
- Stale-reference risk
- SCP-specific risk
- Secret-zeroing evidence
- Finalizer / missed-Dispose behavior
- Existing test coverage
- Required new tests

## Components To Audit

Core:

- `ApplicationSession`
- `IApplicationSession`
- `IProtocol`
- `IConnection`
- `PcscProtocol`
- `PcscProtocolScp`
- `ChainedResponseReceiver`
- `ScpProcessor`
- `ScpState`
- `SessionKeys`

Sessions:

- `OathSession`
- `SecurityDomainSession`
- `FidoSession`
- `ManagementSession`
- `PivSession`
- `OpenPgpSession`
- `YubiOtpSession`
- `HsmAuthSession`

Backends:

- FIDO2 `FidoHidBackend`
- FIDO2 `SmartCardFidoBackend`
- Management `FidoHidBackend`
- Management `SmartCardBackend`
- YubiOtp `OtpHidBackend`
- YubiOtp `SmartCardBackend`
- WebAuthn `FidoSessionWebAuthnBackend`

## Known Risk To Prove Or Refute

FIDO2 SCP handoff may currently create a stale disposed reference:

1. `FidoSession` creates `SmartCardFidoBackend(baseProtocol)`.
2. `InitializeCoreAsync()` wraps `baseProtocol` into `PcscProtocolScp`.
3. `FidoSession` disposes the old backend.
4. `SmartCardFidoBackend.Dispose()` disposes `baseProtocol`.
5. New backend wraps `PcscProtocolScp`, whose base protocol may now be disposed.

This must be reproduced with a failing unit test before any fix.

## Initial Ownership Hypothesis

Preferred model:

- Sessions own protocols.
- Protocols own internally-created connections under current APIs.
- Backends are non-owning transport adapters.
- SCP wrappers own SCP processor state.
- `ApplicationSession.Protocol` owns final effective protocol disposal.

Connection ownership is a separate design decision:

- Current APIs imply sessions own connections created by `IYubiKeyExtensions`.
- Direct `CreateAsync(connection, ...)` currently behaves as ownership transfer.
- A future user-owned connection model may require `leaveOpen` / `ownsConnection` options and API review.
- Do not bundle this into the FIDO2 lifecycle fix without explicit approval.

## Outcome

Committed results:

- `2e167a3f fix(fido2): remove backend protocol ownership`
- `bf6ce564 refactor(fido2): rename transport backends`

Current ownership conclusion:

- Sessions own protocols.
- `ApplicationSession.Protocol` owns final effective protocol disposal.
- FIDO2 backends are non-owning transport adapters.
- `PcscProtocolScp` owns SCP processor state and the wrapped base protocol.

Deferred ownership question:

- Revisit whether `ApplicationSession` should own connection lifecycle.
- Decide whether direct `CreateAsync(connection, ...)` callers should retain connection ownership instead.
- Consider an explicit ownership option such as `leaveOpen` or `ownsConnection` if both models are needed.
- Keep this as a separate API/lifecycle design phase, not part of the completed FIDO2 backend lifecycle fix.

## Required Tests

Core:

- `PcscProtocolScp.Dispose_DisposesScpProcessor`
- `PcscProtocolScp.Dispose_DisposesChainedInnerScpProcessor`
- `PcscProtocol.Dispose_Twice_DisposesConnectionOnce`
- `PcscProtocolScp.Dispose_Twice_DisposesScpProcessorAndBaseProtocolOnce`
- `ScpProcessor.Dispose_ZeroesSessionKeysAndMacChain`
- `DisposedProtocol_TransmitOrSelect_FailsLoudly`
- Existing `Dispose_DisposesBaseProtocol` remains.

FIDO2:

- `CreateAsync_WithSmartCardScp_DoesNotDisposeBaseProtocolDuringBackendHandoff`
- `Dispose_WithSmartCardScp_DisposesEffectiveProtocol`
- `Backend_Dispose_DoesNotDisposeBorrowedProtocol`
- `Backend_Dispose_PreventsFurtherBackendUse`
- `DisposeAsync_ViaBaseOrIAsyncDisposableReference_ReachesDerivedCleanup`
- `Dispose_ViaBaseReference_ReachesDerivedCleanup`

Session lifecycle:

- Verify sync and async disposal paths both reach the same final disposed state.
- Verify `DisposeAsyncCore()` disposes the effective protocol because `ApplicationSession.DisposeAsync()` later calls `Dispose(disposing: false)`.
- Verify failed initialization cleans up owned connection/protocol objects.
- Verify reset/reinitialize flows do not retain stale protocol/backend references.
- Verify disposal during an in-flight async operation has defined behavior or is explicitly out of contract.

## Implementation Principles

- Prove with failing tests first.
- Fix ownership semantics, not symptoms.
- Do not add more `Dispose()` calls without proving ownership.
- Prefer non-owning backends unless a backend clearly creates and owns its protocol.
- Keep lifecycle fix and FIDO2 backend rename in separate commits.
- Avoid public API changes unless the audit proves they are necessary.
- Treat `Dispose()` idempotency as part of the ownership contract, not a cosmetic cleanup.
- Treat `new DisposeAsync()` on session inheritors as a high-risk smell until proven safe through base/interface disposal tests.
- Prove SCP key zeroing by observing secret buffers or state effects, not only by asserting that `Dispose()` was called.
- Validate documentation and DI/helper names against source before using them as audit evidence.

## Verification

Run:

- `dotnet format --verify-no-changes --include <touched files>`
- `dotnet toolchain.cs -- test --project Core`
- `dotnet toolchain.cs -- test --project Fido2`
- additional module tests only if touched

Optional hardware:

- Only if unit tests cannot prove a real SCP path.
- No touch/user-presence tests.
- No destructive Management configuration tests.

## Cato Review Findings

Cato review command sequence:

- Route: `bun ~/.claude/PAI/TOOLS/AgentHarnessRouter.ts --surface cato --role auditor --primary-model "openai/gpt-5.5" --cwd "$(pwd)" --dry-run --json`
- Initial audit: `bun ~/.claude/PAI/TOOLS/AgentHarnessRouter.ts --surface cato --role auditor --primary-model "openai/gpt-5.5" --cwd "$(pwd)" --prompt-file /tmp/opencode/cato-lifecycle-audit-plan-prompt.txt --output /tmp/opencode/cato-lifecycle-audit-plan.jsonl --execute --json --timeout-ms 150000`
- Initial result: timed out after producing partial findings.
- Concise rerun: `bun ~/.claude/PAI/TOOLS/AgentHarnessRouter.ts --surface cato --role auditor --primary-model "openai/gpt-5.5" --cwd "$(pwd)" --prompt-file /tmp/opencode/cato-lifecycle-audit-plan-rerun-prompt.txt --output /tmp/opencode/cato-lifecycle-audit-plan-rerun.jsonl --execute --json --timeout-ms 240000`
- Reviewer model: `google-vertex-anthropic/claude-opus-4-8@default`.
- Verdict: `concerns`, medium criticality.

Cato findings incorporated into this plan:

- Double-dispose idempotency must be audited and tested explicitly; `PcscProtocol.Dispose()` currently has no `_disposed` guard.
- Sync and async disposal should not be assumed equivalent. The correct invariant is that both paths reach the same terminal disposed state.
- `FidoSession.DisposeAsync()` uses `new`; polymorphic disposal through base/interface references must be audited and tested.
- SCP key-zeroing must be verified directly, not only through disposal call-chain tests.
- Secret lifetime and finalizer absence must be documented, including residual risk when callers skip deterministic disposal.
- Docs and DI/helper names must be validated against source during the audit.
- The lifecycle fix and FIDO2 backend rename should land as separate commits.
- Reuse-after-dispose behavior should be checked so stale references fail loudly.
- Disposal during in-flight async APDU operations is a separate concurrency angle that must be classified as supported, unsupported, or guarded.

## Cato Review Brief

Ask Cato to review this plan for missing angles, especially:

- stale reference after disposal
- async vs sync disposal mismatch
- failed initialization cleanup
- direct `CreateAsync(connection)` ownership ambiguity
- extension-created connection ownership
- backend ownership consistency
- SCP key-zeroing guarantees
- reset/reinitialize lifecycle
- shared test connection wrappers
- WebAuthn wrapping FIDO sessions
- DI factory ownership
- double-dispose idempotency
- finalizer absence and secret lifetime
- public API compatibility if connection ownership changes
- whether rename should be separated from lifecycle fix

## Approval Gate

Do not start implementation until:

- this plan file is written
- Cato review is complete
- this plan is updated with Cato findings
- the audit investigation is explicitly approved
