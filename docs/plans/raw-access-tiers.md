# Raw-Access Tiers and Protocol Internalization

## Assignment

Implement this specification through the autonomous `/Craftsman` Craft workflow, one invocation per stack layer. Deliver the work as a new three-layer GitHub stack based on `single-conn/4-docs`.

The golden path is applet sessions. Advanced users must retain a strict, narrow, supported way to sequence undocumented or low-level commands without applet runtime checks. Raw connection byte I/O remains public as a lower-level, explicitly unguarded escape hatch.

## Settled Product Decisions

1. Add public raw sessions for SmartCard/APDU, FIDO HID, and OTP HID.
2. Internalize `ProtocolFactory`, `IProtocol`, `ISmartCardProtocol`, `IFidoHidProtocol`, `IOtpHidProtocol`, and protocol implementation/decorator types that have no independent public construction story.
3. Close external `ApplicationSession` protocol extension seams as needed; third-party composition should wrap a raw session instead of injecting SDK protocol internals.
4. Keep public raw connection-level methods (`TransmitAndReceiveAsync`, `SendAsync`, `ReceiveAsync`) as the at-your-own-risk expert escape hatch.
5. Do not relitigate these decisions during fit audit. Craftsman governs implementation shape and discretionary improvements only.

## Access Model

```text
Tier 0: Applet sessions
        PivSession, FidoSession, ManagementSession, ...
        Supported golden path: applet semantics, validation, firmware gates.

Tier 1: Raw sessions
        RawSmartCardSession, RawFidoHidSession, RawOtpHidSession
        Supported power-user path: framing, device/session ownership, overlap refusal,
        optional SCP for SmartCard, but no applet selection or applet feature gates.

Tier 2: Raw connections
        ISmartCardConnection, IFidoHidConnection, IOtpHidConnection
        Public expert escape hatch: raw bytes/reports, caller owns all framing,
        sequencing, response correlation, cancellation recovery, and safety.

Internal: ProtocolFactory and IProtocol family
          Session implementation machinery, not a user extension surface.
```

## Required Ownership Semantics

Raw sessions are application sessions for ownership purposes. They must inherit the existing single-connection contract:

- A grouped physical YubiKey has at most one live connection across all interfaces.
- A connection has at most one live applet or raw session.
- Creating a raw session while an applet session is live, or vice versa, throws `ConnectionInUseException` before wire I/O.
- Dispose session N before creating session N+1 over the same connection.
- A session created over a caller-owned connection borrows it.
- A convenience `IYubiKey.CreateRaw*SessionAsync` method owns and disposes the hidden connection.
- Overlapping operations on one raw session throw `InvalidOperationException` immediately.
- Once a stateful exchange is admitted, it runs to completion to avoid stranding APDU, CTAP, OTP, or SCP state.
- Raw connection methods bypass session and exchange guards; document this explicitly.

## A. Public Raw Sessions

Place transport-level raw sessions in Core under the existing session/protocol organization. Follow `ApplicationSession.Construct`, `ConnectionSessionGuard`, initialization-failure cleanup, borrow/own, and disposal patterns exactly.

### A1. RawSmartCardSession

Required public behavior:

- Accept exactly one `ISmartCardConnection` for its lifetime.
- Do not select an applet during creation.
- Do not inspect applet capabilities or apply applet firmware gates.
- Expose explicit application selection.
- Expose raw `ApduCommand` transmission with `throwOnError` control.
- Support optional SCP establishment during creation using existing key-parameter abstractions and secure key cleanup.
- Expose the minimum configuration needed to choose APDU formatting behavior. Prefer an existing public `ProtocolConfiguration` only if the fit audit confirms it is an intentional power-user surface; otherwise expose a smaller raw-session option without duplicating protocol concepts.
- Preserve `ApduResponse` data and status words when `throwOnError: false`.

Illustrative API, subject to repository naming and overload conventions:

```csharp
public sealed class RawSmartCardSession : ApplicationSession
{
    public static Task<RawSmartCardSession> CreateAsync(
        ISmartCardConnection connection,
        ScpKeyParameters? scpKeyParameters = null,
        CancellationToken cancellationToken = default);

    public Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default);

    public Task<ApduResponse> TransmitAndReceiveAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default);

    public void Configure(
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null);
}
```

The actual SCP key-parameter base type may differ. Reuse the same accepted SCP abstraction and initialization path as existing SmartCard applet sessions. Do not add a second SCP implementation.

Example use:

```csharp
await using ISmartCardConnection connection =
    await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
await using RawSmartCardSession raw =
    await RawSmartCardSession.CreateAsync(connection, cancellationToken: cancellationToken);

await raw.SelectAsync(myApplicationId, cancellationToken);
ApduResponse response = await raw.TransmitAndReceiveAsync(
    new ApduCommand(cla, ins, p1, p2, commandData),
    throwOnError: false,
    cancellationToken);
```

### A2. RawFidoHidSession

Required public behavior:

- Accept exactly one `IFidoHidConnection`.
- Expose a complete CTAP HID logical exchange using existing framing, CID, continuation packet, keep-alive, and response logic.
- Do not add FIDO2/WebAuthn operation semantics, request DTOs, feature checks, or authenticator-policy validation.
- The caller supplies the raw CTAP HID command and payload and interprets the response.

Illustrative API:

```csharp
public sealed class RawFidoHidSession : ApplicationSession
{
    public static Task<RawFidoHidSession> CreateAsync(
        IFidoHidConnection connection,
        CancellationToken cancellationToken = default);

    public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte command,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);
}
```

Use existing public command types if they are already the intended low-level currency. Do not leak an internal enum merely to match this pseudocode.

### A3. RawOtpHidSession

Required public behavior:

- Accept exactly one `IOtpHidConnection`.
- Expose a complete OTP HID logical exchange using existing report framing, sequence/polling behavior, and CRC handling.
- Do not add slot configuration semantics, OTP applet checks, or feature gates.
- The caller supplies command/slot bytes and payload and interprets the response.

Illustrative API:

```csharp
public sealed class RawOtpHidSession : ApplicationSession
{
    public static Task<RawOtpHidSession> CreateAsync(
        IOtpHidConnection connection,
        CancellationToken cancellationToken = default);

    public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte command,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);
}
```

### A4. IYubiKey convenience entry points

Add Core extension methods following applet-module conventions:

```csharp
await using RawSmartCardSession raw =
    await yubiKey.CreateRawSmartCardSessionAsync(scpKeyParameters, cancellationToken);

await using RawFidoHidSession raw =
    await yubiKey.CreateRawFidoHidSessionAsync(cancellationToken);

await using RawOtpHidSession raw =
    await yubiKey.CreateRawOtpHidSessionAsync(cancellationToken);
```

Each extension:

1. Selects exactly the corresponding transport; no fallback.
2. Opens one connection.
3. Creates the raw session.
4. Calls `OwnConnection()` only after successful session creation.
5. Disposes the connection if session creation fails, preserving the original creation exception even when disposal or cleanup logging fails.

Reuse the single-transport creation helper when it fits without widening its public surface.

## B. Protocol Internalization

After Tier 1 exists and is independently green:

- Make `ProtocolFactory` internal.
- Make `IProtocol`, `ISmartCardProtocol`, `IFidoHidProtocol`, and `IOtpHidProtocol` internal.
- Make `PcscProtocolScp` internal unless a separate public constructor/use case is discovered; its constructor is already internal.
- Audit all public types and members for protocol-type leakage. Resolve inconsistent accessibility deliberately rather than retaining dead public types.
- Adjust `ApplicationSession.Protocol`, `InitializeProtocolAsync`, and related protocol-typed protected members to `private protected` or a smaller internal seam so external subclasses cannot depend on internal protocol types.
- Keep `ApplicationSession` public if it still provides useful non-protocol inheritance; otherwise record the accessibility decision and rationale in the layer PR. Do not internalize unrelated session APIs opportunistically.
- Migrate the performance benchmark from `ProtocolFactory` to the appropriate raw session or add the benchmark assembly as a friend only if a raw-session benchmark cannot represent the measured behavior. Prefer the public raw-session path.
- Retarget `ProtocolFactoryTests`: retain internal routing tests where valuable, remove tests whose only purpose is asserting public overload shape.
- Verify all applet assemblies continue using protocol machinery through existing `InternalsVisibleTo` declarations.

This is a deliberate source and binary break in the alpha v2 API. Do not add obsolete compatibility wrappers.

## C. Tier 2 Raw Connections

Keep these public:

- `ISmartCardConnection.TransmitAndReceiveAsync(ReadOnlyMemory<byte>, ...)`
- `IFidoHidConnection.SendAsync` / `ReceiveAsync`
- `IOtpHidConnection.SendAsync` / `ReceiveAsync`

Do not add framing helpers directly to connection interfaces. Their contract is raw transport access.

XML documentation must state:

- These methods bypass `ApplicationSession`, `ConnectionSessionGuard`, and `ExchangeGuard`.
- The caller must not use a raw connection concurrently with a live session or another raw operation.
- The caller owns packet/APDU formatting, command chaining, response correlation, CRC, keep-alive handling, and recovery from partial/cancelled exchanges.
- The SDK makes no state-integrity guarantee after interrupted or interleaved Tier 2 traffic; dispose and reopen the connection when state is uncertain.

## D. Behavioral Tests

Use TDD and fake connections. No validation-only tests and no skipped placeholders.

Required tests:

1. Raw session then applet session over one connection: second creation throws before wire I/O; raw holder remains usable.
2. Applet session then raw session: second creation throws; applet holder remains usable.
3. Sequential reuse over one connection: applet -> dispose -> raw -> dispose -> applet.
4. Each raw session borrows a caller-created connection; disposing it does not dispose the connection.
5. Each `IYubiKey.CreateRaw*SessionAsync` session owns and disposes its hidden connection.
6. Failed raw-session initialization releases the session claim and disposes only an internally-created connection.
7. Overlapping operations on each raw session throw `InvalidOperationException`; awaited sequential calls succeed; failure clears the overlap guard.
8. SmartCard `throwOnError: false` returns data and a non-success status word without throwing.
9. SmartCard raw selection sends exactly the caller's AID and performs no implicit SELECT during creation.
10. Raw SmartCard SCP path uses the existing SCP processor, verifies representative wire bytes, and zeroes secret/session material on disposal.
11. FIDO raw session processes continuation and keep-alive reports through the existing FIDO HID protocol.
12. OTP raw session performs existing framing and CRC validation.
13. Public API test confirms raw sessions are public while protocol factory/interfaces are not public.
14. Existing applet session suites remain green.

If existing protocol tests already pin framing, do not duplicate all vectors in raw-session tests. Add only enough tests to prove the public raw session delegates to the correct internal machinery and preserves ownership behavior.

## E. Documentation

Add a canonical "Access Tiers" section to Core architecture documentation and update:

- Root `CLAUDE.md`
- `src/Core/CLAUDE.md`
- `src/Core/README.md`
- `src/SecurityDomain/CLAUDE.md`
- Any public API examples or architecture diagrams mentioning `ProtocolFactory`
- XML documentation for raw sessions, `ApplicationSession`, raw connection methods, and internal protocol contracts where useful

Documentation must include:

- Tier 0 golden path with an applet-session example.
- Tier 1 supported raw-session examples for all three transports.
- A raw SCP APDU example without real keys or secret logging.
- Tier 2 warning and disposal/reopen recovery advice.
- Explicit distinction between bypassing applet checks and bypassing physical transport safety.
- Migration note from `ProtocolFactory.Create(connection)` to `Raw*Session.CreateAsync(connection)`.

## Craftsman Execution Protocol

Run autonomous Craftsman separately for every layer.

### Common phase rules

- Spec-mandated changes are pre-approved scope. Apply the Value/Cost gate only to discretionary reshaping.
- Phase 0: establish a working tracer bullet and one passing behavior test.
- Phase 1: perform cross-vendor fit audit plus Simplify review over the layer slice.
- Phase 1.5: autonomous C3 work proceeds only at V3; at most one discretionary C3 per layer. Defer lesser cross-cutting findings in the PR body.
- Phase 2: no more than two reshape passes; state at least two alternatives plus leave-as-is before material discretionary changes.
- Phase 3: full DevTeam correctness review and Simplify Apply on the settled shape.
- Phase 4: put reshape rationale, gate decisions, and deferred owner decisions in the PR body.

### Layer-specific boundaries

| Layer | Slice | Phase 0 tracer |
|---|---|---|
| 1 | Core raw sessions, existing protocol delegation, session/ownership tests | A raw SmartCard session holds a connection; creating an applet session is refused before wire I/O |
| 2 | Protocol factory/interfaces, ApplicationSession accessibility, benchmark, API tests | Full solution builds with protocol factory/interfaces internal and benchmark migrated |
| 3 | Access-tier docs, READMEs/CLAUDE.md, XML docs, architecture diagrams | `dotnet toolchain.cs -- docs-architecture` passes |

The `ProtocolConfiguration` exposure question is discretionary and belongs in layer 1's fit audit. Prefer the smallest coherent public API.

## Stack Delivery

Create a new three-layer `gh stack` based on `single-conn/4-docs`. Do not add commits to PRs #558-#561.

Suggested branches:

```text
raw-tiers/1-raw-sessions
raw-tiers/2-internalize
raw-tiers/3-docs
```

Each layer is one independently reviewable PR. Use the repository `workflow-stack` guidance, `gh stack init`, `gh stack add`, `gh stack push`, and `gh stack submit`.

The current worktree contains unrelated modified `.gitignore` and untracked `.humanlayer/`. Never edit, stage, remove, or include them. Include this plan document in layer 3.

## Verification

Read root and affected module `CLAUDE.md` files and `docs/SDK-HOUSE-STYLE.md` before editing. Use only repository toolchain commands:

```bash
dotnet toolchain.cs build
dotnet toolchain.cs test
dotnet toolchain.cs -- resilience --fast
dotnet toolchain.cs -- docs-architecture
```

Use focused project/filter commands during development. Do not run hardware integration tests without human coordination. Compile affected integration-test projects where useful.

Format only staged C# files:

```bash
dotnet format Yubico.YubiKit.sln \
  --include $(git diff --name-only --cached -- '*.cs') \
  --verify-no-changes
```

Stage explicit paths only. Never use `git add .`, `git add -A`, or `git commit -a`.

## Acceptance Criteria

- Applet sessions remain the documented golden path.
- Public raw sessions exist for SmartCard, FIDO HID, and OTP HID.
- Raw sessions participate in `ConnectionSessionGuard` and sequential reuse.
- Raw SmartCard supports explicit SELECT, raw APDU responses, and optional SCP without applet checks.
- Raw FIDO and OTP sessions reuse the existing framing/protocol implementations.
- `ProtocolFactory` and the `IProtocol` family are no longer public API.
- Public raw connection byte/report methods remain available and are clearly documented as unguarded Tier 2 access.
- No obsolete compatibility wrappers are added.
- Build, unit tests, fast resilience checks, formatting, and architecture docs validation pass.
- Three clean stacked PRs are submitted on `single-conn/4-docs`, each with a Craftsman Phase 4 report.
- Independent aggregate review has no unresolved correctness findings within the approved contract.
- Existing `.gitignore` and `.humanlayer/` worktree changes remain untouched.
