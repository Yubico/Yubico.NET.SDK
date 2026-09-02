# Single-Connection / Single-Session Ownership Model

## Assignment

Implement this specification through an autonomous `/Craftsman` flow, one run per stack layer. Deliver the work as four narrow GitHub stacked PRs based on the branch that was current when this specification was approved: `review-copilot-code-review-assessment`.

The repository is a .NET 10 / C# 14 pre-release SDK. Breaking changes to alpha-only public helpers are allowed when required by this specification.

## Product Contract

Dain Nilsson confirmed the supported ownership model:

1. A physical YubiKey has at most one live connection at a time across CCID, FIDO HID, and OTP HID.
2. A connection hosts at most one live application session at a time.
3. Sequential reuse is supported: dispose session N, then create session N+1 over the same open connection.
4. Parallel connections or sessions are unsupported and must fail deterministically rather than being silently accommodated.

Canonical SDKs may treat parallel physical-device connections as undefined behavior. This SDK must provide the safer behavior: a second in-process connection attempt throws `ConnectionInUseException` before native open.

## Invariants To Preserve

- Keep `ConnectionSessionGuard`: one live session per connection, holder-checked detach, and sequential reuse.
- Keep the `ApplicationSession` borrow/own split, `OwnConnection()`, deferred binding in `Construct`, and cleanup after initialization failure.
- Keep `DisposalGate`: disposal completes once, the physical connection is torn down before its registry lease is released, and all disposal callers observe completion.
- A second connection attempt never waits for a live connection to end.
- Discovery remains nonblocking; a connection may wait cancellably for an active discovery read, and waiting connections retain priority over later discovery.
- Protocols never dispose borrowed connections.
- Once a stateful exchange begins, do not abort it between constituent transmits. This protects APDU chaining, CTAP/OTP frames, and SCP state.

## A. Device-Wide Connection Lease

### Current behavior

`src/Core/src/Devices/DeviceConnectionRegistry.cs` leases per interface ID. A CCID connection and a FIDO HID or OTP HID connection to the same merged physical device can therefore coexist.

### Required behavior

A connection claims every known interface ID belonging to the physical `YubiKeyDevice`. Opening any second interface on that key must throw `ConnectionInUseException` before native open.

Do not use the merged `DeviceId` as the registry key. It encodes merge evidence and is not stable across scans. Keep stable interface IDs and acquire their ownership records atomically as one logical lease.

Illustrative pseudocode:

```csharp
internal static async ValueTask<IDisposable> AcquireConnectionAsync(
    IReadOnlyCollection<string> interfaceIds,
    CancellationToken cancellationToken)
{
    string[] orderedIds = [.. interfaceIds.Order(StringComparer.Ordinal)];
    var acquired = new List<IDisposable>(orderedIds.Length);

    try
    {
        foreach (string id in orderedIds)
        {
            acquired.Add(await GetOwnership(id)
                .AcquireConnectionAsync(id, cancellationToken)
                .ConfigureAwait(false));
        }

        return new CompositeRegistration(acquired);
    }
    catch
    {
        for (int i = acquired.Count - 1; i >= 0; i--)
            acquired[i].Dispose();

        throw;
    }
}
```

The example is semantic, not mandatory syntax. Prefer existing repository patterns and avoid LINQ or allocations when a simpler implementation fits.

Requirements:

- Sort IDs with ordinal comparison before acquisition so racing connects use one deterministic order.
- Remove duplicate IDs before acquisition.
- If any acquisition waits, is cancelled, or fails, release every earlier claim in reverse order.
- Return one idempotent registration that releases all member claims in reverse order.
- Standalone devices use a one-element lease scope and retain current behavior.
- `TryAcquireDiscovery(string interfaceId)` remains per-interface and nonblocking.
- A connection holding all member IDs causes discovery on any member to be refused.
- Preserve the existing internal discovery path that bypasses public connection leasing while holding its discovery lease.

> **Superseded by Stage C (flat device model):** `PcscYubiKey`/`HidYubiKey` were replaced by the internal `DeviceConnectionSlot`; the paragraph below is kept as history.

Keep `PcscYubiKey` and `HidYubiKey` as pre-merge adapters that claim only their own `DeviceId`. The published `YubiKeyDevice` owns the complete sorted interface-ID set, claims it as one logical lease, and routes the requested connection type to exactly one adapter. Reuse the existing connection registration decorators and failure cleanup.

Update the exception text to state the physical-device rule, for example:

```text
This YubiKey already has a live connection in this process (held interface: '{id}').
A physical YubiKey supports one live connection at a time across all interfaces.
Dispose the existing connection first; connections are reused sequentially, not in parallel.
```

Document this bound: exclusivity is only as strong as physical-device grouping. When conservative discovery cannot prove that standalone interface records belong to one physical key, each record has a one-element scope and behavior degrades to today's per-interface protection. Do not weaken discovery's evidence hierarchy to guess associations.

## B. Delete Held-Transport Fallback

Delete the behavior in `src/Core/src/Devices/YubiKeyConnectionExtensions.cs` that catches a held preferred transport and silently tries another interface.

Required changes:

- Delete `IsFallbackEligibleHeldError`.
- Delete handling for cross-process PC/SC `SCARD_E_SHARING_VIOLATION` and `SCARD_E_SERVER_TOO_BUSY`; propagate the original `SCardException`.
- Collapse `ResolveSessionTransports` from an ordered candidate list into one selected `ConnectionType`.
- Select an explicit valid override when supplied; otherwise select the first supported connection in the module's documented preference order.
- Open exactly that transport once. Do not retry another interface on `ConnectionInUseException`, `SCardException`, or session initialization failure.
- Preserve cleanup: if opening succeeds but session construction/initialization fails, dispose the newly opened connection so no lease leaks.
- Update Management, Fido2, YubiOtp, and all other affected applet extension methods.

Illustrative resolver:

```csharp
public static ConnectionType ResolveSessionTransport(
    this IYubiKey yubiKey,
    ConnectionType? preferredConnection,
    string sessionName,
    params ConnectionType[] defaultOrder)
{
    if (preferredConnection is { } requested)
    {
        ValidateConcreteTransport(requested);
        ValidateTransportForSession(requested, defaultOrder, sessionName);
        ValidateDeviceSupport(yubiKey, requested, sessionName);
        return requested;
    }

    foreach (ConnectionType candidate in defaultOrder)
    {
        if (IsConcrete(candidate) && yubiKey.SupportsConnection(candidate))
            return candidate;
    }

    throw new NotSupportedException(
        $"This YubiKey exposes no connection usable for a {sessionName} session " +
        $"(available: {yubiKey.AvailableConnections}).");
}
```

Illustrative call site:

```csharp
ConnectionType transport = yubiKey.ResolveSessionTransport(
    preferredConnection,
    "Management",
    ConnectionType.SmartCard,
    ConnectionType.HidFido,
    ConnectionType.HidOtp);

IConnection connection = await yubiKey.OpenSessionConnectionAsync(transport, cancellationToken)
    .ConfigureAwait(false);

try
{
    ManagementSession session = await ManagementSession.CreateAsync(
        connection,
        cancellationToken: cancellationToken).ConfigureAwait(false);
    session.OwnConnection();
    return session;
}
catch
{
    await connection.DisposeAsync().ConfigureAwait(false);
    throw;
}
```

Keep the typed transport-opening switch internal unless an existing public contract requires otherwise.

## C. Refuse Overlapping Protocol Exchanges

Delete `src/Core/src/Utilities/AsyncExchangeGate.cs`. It queues concurrent calls and creates a special entry-only cancellation model. Sessions are not a concurrency primitive; misuse should fail loudly.

Replace it with a small internal overlap guard used by `PcscProtocol`, `PcscProtocolScp`, `FidoHidProtocol`, and `OtpHidProtocol`.

Required semantics:

- Sequential awaited operations behave unchanged.
- If another logical exchange is active, throw `InvalidOperationException` immediately; do not queue.
- A token already cancelled at entry throws before claiming the guard.
- Once claimed, the full logical exchange runs to completion without observing caller cancellation between constituent transmits, preserving existing device-state protection.
- Always clear the active flag in `finally`, including when the exchange fails.

Illustrative implementation:

```csharp
internal sealed class ExchangeGuard
{
    private int _active;

    public async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> exchange,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref _active, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "This session already has an exchange in flight. Sessions support one operation " +
                "at a time; await each call before issuing the next.");
        }

        try
        {
            return await exchange(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _active, 0);
        }
    }
}
```

Retain a result-free overload only if it removes real duplication in current consumers.

## D. Behavioral Tests

Use TDD and fake connections. Do not add validation-only tests or skipped placeholders.

Required behavior tests:

1. Second connection to the same grouped key on another interface throws `ConnectionInUseException`; the first remains usable.
2. Second connection on the same interface still throws.
3. After disposing the first connection, connecting through any member interface succeeds.
4. If claim N fails, claims 1 through N-1 are rolled back.
5. Racing connects to one grouped key admit exactly one winner and do not deadlock.
6. Discovery on one member delays a cancellable connection claim; discovery on any member is refused while the connection is live.
7. An ungrouped standalone device behaves as before.
8. A multi-transport applet entry point propagates `ConnectionInUseException` without attempting fallback and leaks no connection.
9. A PC/SC sharing violation propagates unchanged; no HID fallback is attempted.
10. Overlapping protocol exchange throws; sequential calls succeed; the guard resets after an exchange exception.

Retarget or delete tests whose sole contract is held-transport fallback or cross-interface coexistence, including relevant cases in `HeldExceptionPropagationTests`, `ConnectSessionTransportTests`, `FidoHidOwnershipIntegrationTests`, and `PivSessionContentionTests`. Preserve `SessionConstructionGuardTests` and `ProtocolConnectionOwnershipTests` unchanged unless compilation requires a mechanical rename.

## E. Documentation

Update:

- `docs/architecture/connection-ownership-and-contention.md`
- `src/Core/CLAUDE.md`
- XML documentation on the registry, exception, connection APIs, resolver, guard, and applet extension methods

Use this concise contract consistently:

> A physical YubiKey has at most one live connection, which hosts at most one live session. Connections and sessions are reused sequentially; overlapping ownership attempts throw.

Delete fallback claims and per-interface-parallelism claims. Preserve the measured CCID applet-selection rationale, in-process limitation, no-finalizer warning, discovery-grouping bound, and updated invariant-to-test map.

## Stack Delivery

Create a new four-layer GitHub stack based on `review-copilot-code-review-assessment`, not `develop`:

| Layer | Suggested branch | Concern |
|---|---|---|
| 1 | `single-conn/1-exchange-guard` | Exchange guard plus focused tests |
| 2 | `single-conn/2-device-lease` | Device-wide multi-interface lease plus focused tests |
| 3 | `single-conn/3-drop-fallback` | Fallback deletion, applet call sites, focused tests |
| 4 | `single-conn/4-docs` | Documentation and XML-doc reconciliation |

Use the repository's `workflow-stack` guidance and `gh stack`:

```bash
gh stack init
gh stack add single-conn/1-exchange-guard
# implement, verify, explicitly stage, commit
gh stack add single-conn/2-device-lease
# repeat for remaining layers
gh stack submit
```

If lower-layer changes require propagation:

```bash
gh stack rebase
gh stack push
gh stack submit
```

Do not stage existing unrelated `.gitignore` changes or `.humanlayer/`. Never use `git add .`, `git add -A`, or `git commit -a`.

## Craftsman Execution Protocol

Run the `/Craftsman` Craft workflow in autonomous mode separately for each layer.

- Spec-mandated contract changes are approved scope; do not treat them as discretionary fit findings.
- Phase 0: establish a tracer-bullet baseline with one passing behavior test for the layer.
- Phase 1 slice boundaries:
  - Layer 1: the four protocol consumers, utilities, and their tests.
  - Layer 2: Core Devices, concrete device implementations, and ownership tests.
  - Layer 3: `YubiKeyConnectionExtensions`, affected applet extension methods, and fallback tests.
  - Layer 4: documentation only; a heavyweight fit audit is unnecessary.
- Phase 1.5: apply the Craftsman Value/Cost gate only to discretionary reshaping. Autonomous C3 work proceeds only at V3. Defer lesser cross-cutting opportunities.
- Phase 2: at most two reshaping passes; state at least two alternatives plus leave-as-is before material discretionary changes.
- Phase 3: run the full DevTeam correctness review and Simplify Apply sweep on the settled layer.
- Phase 4: put the reshape rationale, gate decisions, and deferred owner decisions in that PR's body.

The multi-interface claim rather than merged-device-ID keying is a settled design decision. Do not relitigate it during fit audit.

## Verification

Read root and affected module `CLAUDE.md` files before changes. Use only repository toolchain commands:

```bash
dotnet toolchain.cs build
dotnet toolchain.cs test
dotnet toolchain.cs -- resilience --fast
```

During development, focus tests by module and method. Run full build and unit tests before the final stack submission. Run resilience verification for layers 2 and 3 and at final convergence.

Format only staged C# files:

```bash
dotnet format Yubico.YubiKit.sln \
  --include $(git diff --name-only --cached -- '*.cs') \
  --verify-no-changes
```

## Acceptance Criteria

- A second connection to any known interface of a held physical key throws `ConnectionInUseException` before native open.
- One live session per connection and sequential session reuse remain green.
- No held-transport fallback remains. Search for `IsFallbackEligibleHeldError`, `ConnectSessionTransportAsync`, and `held-transport` yields no relevant source or documentation references.
- `AsyncExchangeGate` is removed. Overlapping protocol calls throw and sequential calls remain valid.
- Failed claims, failed session creation, and disposal do not leak leases.
- Discovery coordination and conservative physical-device grouping remain correct.
- Build, unit tests, and fast resilience verification pass.
- Four narrow stacked PRs are submitted on `review-copilot-code-review-assessment`.
- Each PR body includes its Craftsman Phase 4 report.
- Unrelated `.gitignore` and `.humanlayer/` changes are untouched.
