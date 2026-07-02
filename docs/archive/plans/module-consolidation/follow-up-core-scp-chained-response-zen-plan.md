# Core SCP Chained Response Zen Refactor Implementation Plan

**Goal:** Make Core APDU response chaining obviously correct, minimal, and elegant across plain PC/SC and SCP-wrapped transports.

**Architecture:** Core should own chained-response mechanics exactly once. Applets should contribute only protocol facts, such as the send-remaining instruction byte, while SCP wrapping and response reassembly happen in one visible, testable order.

**Tech Stack:** .NET 10, C# 14, Core SmartCard APDU pipeline, SCP processors, xUnit/Microsoft Testing Platform through `dotnet toolchain.cs`.

---

## Background

Phase 3 moved OATH away from a local `CollectResponseData` loop and into Core `ChainedResponseReceiver`, configured with OATH's `INS_SEND_REMAINING` value (`0xA5`). That was correct for the approved scope and removed duplicated applet-local response collection.

Cato's Phase 3 audit surfaced the next-level elegance question: not just whether the refactor is correct, but whether Core has the most natural shape for chained responses under SCP. The current pipeline is functional for the Phase 3 fake tests and plain OATH path, but the SCP layering is not as self-evident as it should be.

The future zen solution is not more helpers. It is fewer concepts with clearer ownership.

## Zen Ideal

- One Core concept owns response chaining.
- One Core concept owns SCP wrapping/unwrapping.
- Their ordering is explicit and hard to misuse.
- Applets never implement response collection loops.
- Applets only pass immutable protocol facts: AID, feature gates, and transport constants like send-remaining INS.
- Tests prove the byte-level wire order, not just public method outcomes.
- The final pipeline reads like the protocol: format command, optionally secure it, transmit, receive all chunks, optionally unwrap, return one `ApduResponse`.

## Non-Goals

- Do not rewrite OATH operation encoding/parsing.
- Do not introduce operation-specific command classes.
- Do not introduce a broad APDU framework or strategy hierarchy.
- Do not change applet public APIs unless a failing test proves it is necessary.
- Do not run hardware tests that require User Presence, UV, touch, insert/remove, destructive reset, or persistent state without explicit human approval.

## Design Question To Resolve First

The key design decision is where SCP wrapping belongs relative to chained-response reassembly.

Two candidate shapes must be evaluated with tests before implementation:

- **Shape A:** SCP wraps each command, including the send-remaining APDU, and unwraps each response chunk before Core concatenates plaintext chunks.
- **Shape B:** Core reassembles encrypted/wrapped chunks first, then SCP unwraps the complete response once.

The correct shape depends on the YubiKey SCP response-chaining semantics. Do not infer this from aesthetics. Write characterization tests against the existing pipeline, then verify against protocol docs or safe hardware evidence if available.

## Task 1: Characterize Current Plain Chaining

**Files:**

- Modify: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/PcscProtocolTests.cs`
- Read: `src/Core/src/SmartCard/PcscProtocol.cs`
- Read: `src/Core/src/SmartCard/ChainedResponseReceiver.cs`

**Step 1: Write a focused plain PC/SC chained-response test**

Add or refine a test that proves a configured send-remaining instruction is used for a multi-chunk response and that the final `ApduResponse.Data` excludes status words.

**Step 2: Run the focused test**

Run: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscProtocol"`

Expected: pass.

**Step 3: Record baseline behavior**

Document the observed command order and response shape in the test name or comments only if the ordering is not obvious from assertions.

## Task 2: Characterize Current SCP Chaining

**Files:**

- Modify: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/Scp/PcscProtocolScpTests.cs`
- Read: `src/Core/src/SmartCard/Scp/ScpProcessor.cs`
- Read: `src/Core/src/SmartCard/Scp/ScpInitializer.cs`
- Read: `src/Core/src/SmartCard/Scp/PcscProtocolScp.cs`
- Read: `src/Core/src/SmartCard/ChainedResponseReceiver.cs`

**Step 1: Write a characterization test for SCP + SW1=0x61**

The test should assert whether the send-remaining APDU is currently transmitted through the SCP processor or through the raw chained-response delegate.

**Step 2: Make the assertion byte-level**

Assert command CLA/INS sequence at the fake connection boundary. The test should make it impossible to hide whether send-remaining is MACed/wrapped or raw.

**Step 3: Run the focused SCP tests**

Run: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscProtocolScp"`

Expected: pass if documenting current behavior, or fail if expressing the desired future behavior.

**Step 4: Decide if this is a bug fix or a clarification**

If current behavior sends raw send-remaining under active SCP and protocol evidence says it must be wrapped, treat the future phase as a bug fix. If current behavior is protocol-correct, keep the implementation and improve names/tests only.

## Task 3: Choose The Simplest Pipeline Shape

**Files:**

- Modify if needed: `src/Core/src/SmartCard/ChainedResponseReceiver.cs`
- Modify if needed: `src/Core/src/SmartCard/Scp/ScpProcessor.cs`
- Modify if needed: `src/Core/src/SmartCard/Scp/ScpInitializer.cs`
- Modify if needed: `src/Core/src/SmartCard/PcscProtocol.cs`

**Step 1: Prefer composition over a new abstraction**

Try to preserve the existing decorator pipeline. Do not introduce a new interface unless a failing test cannot be fixed by reordering existing processors.

**Step 2: Make ordering visible in construction**

The processor construction should make the order readable at the call site. A reviewer should not need to mentally simulate four classes to answer whether send-remaining is raw or SCP-wrapped.

**Step 3: Keep applet configuration as data**

Continue using `ProtocolConfiguration.InsSendRemaining` or a clearer equivalent name. Do not move applet-specific branching into Core.

**Step 4: Avoid clever generic names**

Prefer names that say exactly what the protocol does, such as `SendRemainingInstruction`, `ChainedResponseReceiver`, or `ResponseChainingProcessor`.

## Task 4: Implement The Minimal Correction

**Files:**

- Modify: only the Core SmartCard/SCP files required by the failing characterization test
- Test: Core SmartCard/SCP unit tests

**Step 1: Make the desired SCP chaining test fail**

Run: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscProtocolScp"`

Expected: fail for the exact command-order reason being fixed.

**Step 2: Change only processor ordering or delegation**

Implement the smallest change that makes send-remaining handling happen at the correct layer.

**Step 3: Run focused tests**

Run: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscProtocolScp"`

Expected: pass.

**Step 4: Run Core unit tests**

Run: `dotnet toolchain.cs -- test --project Core`

Expected: pass, allowing existing hardware-related skips.

## Task 5: Preserve OATH Behavior

**Files:**

- Read: `src/Oath/src/OathSession.cs`
- Test: `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathSessionTests.cs`
- Test: `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathSessionTests.cs`

**Step 1: Run OATH fake APDU tests**

Run: `dotnet toolchain.cs -- test --project Oath --filter "FullyQualifiedName~OathSessionTests"`

Expected: OATH chained-response tests still prove `0xA5` and reject `0xC0`.

**Step 2: Run read-only OATH integration smoke**

Run: `dotnet toolchain.cs -- test --integration --project Oath.IntegrationTests --smoke --filter "FullyQualifiedName~OathSessionTests.ManagementPreflight_Serial103_ReportsFirmware"`

Expected: pass on serial `103` with Management firmware `5.8.x`.

Run: `dotnet toolchain.cs -- test --integration --project Oath.IntegrationTests --smoke --filter "FullyQualifiedName~OathSessionTests.OathSession_Create_ReadsSelectMetadataWithoutReset"`

Expected: pass without mutating OATH state.

## Task 6: Review For Zen, Not Just Correctness

**Files:**

- Modify: `docs/plans/module-consolidation/phase-N-core-scp-chained-response-learnings.md`
- Read: `docs/plans/module-consolidation/ISA.md`
- Read: `docs/SDK-HOUSE-STYLE.md`

**Step 1: Ask the elegance question explicitly**

Before Cato, write a short self-review answering: could this be done with fewer names, fewer layers, flatter flow, or clearer ownership?

**Step 2: Run DevTeam review**

Ask the reviewer to check correctness, protocol order, and whether a simpler shape exists within scope.

**Step 3: Run Cato audit**

Cato must evaluate correctness and the zen design question added to `docs/plans/module-consolidation/ISA.md`.

**Step 4: Capture deferred risks**

The learning note must say whether SCP chained-response hardware coverage was executed, skipped, or deferred, and why.

## Acceptance Criteria

- Core has one obvious response-chaining path.
- SCP response chaining has byte-level unit coverage showing whether send-remaining is wrapped or raw.
- OATH continues to configure `0xA5` without local response collection.
- No operation-specific command classes are introduced.
- No broad helper layer hides APDU construction.
- Core and OATH focused tests pass through `dotnet toolchain.cs`.
- Safe read-only OATH integration smoke passes or has a human-approved skip.
- Cato explicitly answers whether a more elegant design exists.

## Commit Strategy

- Commit 1: characterization tests only, if they document current behavior.
- Commit 2: minimal Core pipeline correction or naming/order clarification.
- Commit 3: learning note and any plan/house-style documentation update.

Do not commit unrelated untracked files. Stage exact paths only.

## Open Questions For Human Approval

- Should this follow-up run before the next applet consolidation phase, or remain deferred until SCP work is prioritized?
- If real OATH+SCP chained-response hardware coverage requires persistent credentials, should a human coordinate that state, or should fake APDU tests remain the ceiling?
- Should `InsSendRemaining` be renamed to a more readable public property, or is the additive API from Phase 3 now fixed enough to avoid churn?
