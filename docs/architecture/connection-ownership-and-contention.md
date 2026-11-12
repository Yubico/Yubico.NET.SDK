# Connection Ownership and Contention

This document defines the SDK's in-process ownership contract:

> A grouped physical YubiKey has at most one live connection across its known member interfaces within
> this process. A connection hosts at most one live session.
> Connections and sessions are reused sequentially; overlapping ownership attempts throw.

Cross-process contention is outside this registry's scope. Built-in PC/SC strict-exclusive open maps a
sharing violation to `ConnectionInUseException`; other platform failures can surface as `SCardException`.

## Why connection ownership is enforced

A YubiKey's CCID interface has one selected applet on the basic channel. The following sequence was
measured on hardware:

```csharp
await using var piv = await yubiKey.CreatePivSessionAsync();
await piv.VerifyPinAsync(pin);
var info = await yubiKey.GetDeviceInfoAsync();
```

Before ownership enforcement, the Management `SELECT` deselected PIV. The next PIV operation failed
with `SW=0x6D00` (*instruction not supported*). The intervening Management operation itself succeeded,
so wire-time detection was too late. The SDK therefore refuses the conflicting acquisition before
opening another native handle.

The hardware investigation also established that Management can communicate over HID while CCID is
held. That remains useful protocol evidence, but it is no longer a supported parallel ownership mode.
The supported usage contract remains one connection for the physical key, regardless of interface;
enforcement is bounded by the known-member scope described below.

## One physical device, one connection

`DeviceConnectionRegistry` is keyed by stable member interface IDs, not by a composite `DeviceId`.
A composite ID describes the evidence tier that formed a group and can change between scans. Member
IDs (PC/SC reader names and HID paths) are the stable ownership records.

When discovery groups interfaces into one flat `YubiKeyDevice`, it stores the interfaces in
optional SmartCard, HID FIDO, and HID OTP slots with one sorted lease scope. Opening any slot acquires
the member claims in order before native open, rolling back earlier claims if acquisition fails. Therefore:

- a second connection through the same interface throws `ConnectionInUseException`;
- a second connection through another known member interface throws before native open;
- confirmed teardown releases the member claims; uncertain built-in native release can retain them;
- standalone records retain a one-element scope and behave as before;
- failed or canceled multi-member acquisition rolls back every earlier claim.

Acquisition sorts IDs ordinally and removes duplicates. Racing connects use the same order, admit one
winner, and do not deadlock. A connection never waits for another live connection to end.

An earlier member's discovery read can still delay acquisition before a later held member is checked.
The eventual live-connection refusal is not a guarantee of immediate rejection at method invocation;
cancellation can end that discovery wait and roll back claims already acquired.

### Grouping bound

Physical-device exclusivity is only as strong as discovery's evidence. Separate records each have a
one-element lease scope, reducing protection to per-interface exclusion. PID-completeness grouping also
has a complementary-partials bound: a grouped record can contain slots from different same-PID keys. See
[Device Discovery Guarantees](device-discovery-guarantees.md).

The same bound applies to `YubiKeyManager.Find…Async(interfaces: ...)`: each returned `IYubiKey` is a scoped
device view that deliberately knows only in-scope members. Repeated scans and all/scoped views with any shared member ID exclude each other's connections
in either acquisition order. Two disjoint views, such as SmartCard-only and FIDO-only, have **no shared
claim**, even if they report the same serial or topology key. The registry does not register global
serial/topology aliases or import membership from earlier scans. A prior all-interface observation does
not itself expand later views' claims. Coordinate disjoint views explicitly; their independent claims
are a documented limit, not a recommendation to use one physical key concurrently. Discovery leases are
also per-interface: a scan can still probe an unclaimed sibling while a scoped connection holds another
interface of that key. Pinned by
`ScopedViews_OverlappingAllAndScopedConnectionsExcludeEachOtherInBothDirections` and
`ScopedViews_DisjointMembersEvenWithSameSerial_HaveOnlyPerInterfaceExclusion` in `FindYubiKeysScopeTests`.

An owned monitor has independent discovery evidence and scans All; a one-shot `interfaces` request neither
pauses nor restricts it. Do not assume a monitor is a hardware broker for independent scoped/raw views.
For explicit metadata comparisons over different interfaces, finish scans and dispose tracking before
opening sequential live connections when sibling overlap cannot be ruled out. Disposing a monitor does
not dispose application-owned sessions or connections. Disposal stops listeners, cancels and awaits the
scan loop, retires discovery, then completes the feed; it has no separate timeout. Discovery waits are
bounded by the discovery budgets, not a hard total wall-clock limit. Already-open reads retain actual
admission/leases until native teardown; committed history may still drain after disposal.

manager watch creates a producer; monitor watch subscribes to an existing one.

## One connection, one session

`ConnectionSessionGuard` allows one live `ApplicationSession` on a connection. A second session throws
`ConnectionInUseException` before applet initialization. Sequential reuse is supported:

```csharp
await using var connection = await device.ConnectAsync<ISmartCardConnection>();

await using (var piv = await PivSession.CreateAsync(connection))
    await piv.VerifyPinAsync(pin);

await using var oath = await OathSession.CreateAsync(connection);
```

`ApplicationSession.Construct` binds only after construction succeeds. Initialization failure and
session disposal detach the exact holder, so session N+1 can reuse the still-open connection after
healthy idle or sequential use. Detaching is ownership bookkeeping, not proof that connection or device
state is reusable; the cases that require reopening are listed under
[Protocol exchange overlap](#protocol-exchange-overlap).

## Ownership and disposal

Whoever creates a connection disposes it. Protocols and direct `Session.CreateAsync(connection)` calls
borrow the connection and never dispose it. Convenience `IYubiKey.Create<App>SessionAsync` methods open
a hidden connection and call `OwnConnection()`, transferring that connection's lifetime to the returned
session.

For the generic `CreateSessionOverTransportAsync` callback helper, successful results need an explicit
connection owner too. The helper cleans up an open when the callback fails; after success, it cannot
infer that an arbitrary returned object owns the connection. Returning only a directly created borrowed
session loses deterministic ownership unless the result also exposes/owns and disposes that connection.
Prefer the applet/raw convenience factories or an explicit result owner with session-before-connection cleanup.

`DisposalGate` sequences connection teardown before registry release. Built-in native owners distinguish
terminal managed work, drained references, and proven physical release; unknown release retains the
claim. A custom connection's successful `Dispose` supplies only that implementation's guarantee, and
the pinned Windows HID compatibility route can defer actual SafeHandle close while native work holds
a reference. Do not equate every managed disposal return with native release. Disposal runs
once; all sync and async disposal callers observe the same completion and exception. Sessions have no
finalizer recovery. Some built-in connections request best-effort native finalizer cleanup; it is not a
guarantee of timely teardown or lease release. A leaked connection can retain the physical-device lease
for the process lifetime. Dispose deterministically to observe completion and failures.

## Discovery coordination

Discovery leases remain per-interface and nonblocking:

- discovery on any claimed member is refused while a grouped connection is live;
- a connection may wait cancellably for an active discovery read;
- while waiting, its earlier member claims prevent later discovery from overtaking it;
- cancellation or failure rolls back those earlier claims;
- discovery uses `IDiscoveryConnectionProvider`, bypassing public connection registration while it
  already owns the discovery lease.

Idle ownership records remain for the process lifetime to avoid unsafe remove/recreate races. Their
count is bounded by unique interface IDs observed.

Separately, discovery's native-read admission serializes potentially sibling USB probes by PID before
they request a worker or interface lease. It covers both identity and metadata reads, including connection
disposal, across finders and evidence epochs. An unknown USB PID conservatively acquires all known PID
gates atomically. Pending requests reserve no gates and await release notifications, so an unknown request
blocked by one PID does not prevent an available different PID from progressing. These fixed gates are
scheduling only: they contain no metadata and add no public connection claims.
Once an interface lease is acquired, it owns this admission until native release; an abandoned waiter or
retired scope cannot release it. Built-in native owners that cannot prove teardown retain it with their
quarantined interface claim. See [native discovery scheduling](device-discovery-guarantees.md#native-discovery-scheduling)
for the throughput tradeoff and pinning tests. Direct raw reads outside the finder and caller-owned public
connections do not acquire these discovery PID gates; the grouping bound above remains unchanged.

## Session transport selection

Multi-transport applet entry points select exactly one transport. An explicit valid
`preferredConnection` wins. Without an override, SCP parameters select SmartCard; otherwise the first
supported transport in the applet's documented order is selected. The SDK opens that transport once.

`ConnectionInUseException`, `SCardException`, cancellation, and session-initialization failures all
propagate without trying another interface. If native open succeeds but session initialization fails,
the newly opened connection is disposed before the exception escapes.

The inherited `ManagementSession.ConnectionType` reports the transport actually opened. Assert it when
a test's meaning depends on exercising a specific interface; filtering the device alone does not pin it.

## Protocol exchange overlap

`PcscProtocol`, `PcscProtocolScp`, `FidoHidProtocol`, and `OtpHidProtocol` protect each complete logical
exchange with `ExchangeGuard`. A logical exchange is one guarded protocol call: an APDU with its
command and response chaining, one CTAP HID request with its keep-alives, or one OTP HID report sequence.
Sequential awaited calls are unchanged. If an exchange starts while another exchange on the same protocol is
admitted, it throws `InvalidOperationException` immediately rather than queueing.

The guard does not make a public operation atomic. Operations such as `GetDeviceInfoAsync`, which reads
one page per exchange, release the guard between their exchanges. An overlapping call to such an
operation may be refused, or it may run its own exchanges in a gap and complete. It can also cause the
first operation's next exchange to be refused. Each admitted exchange stays intact on the wire, but no
ordering or all-or-nothing guarantee spans exchanges. Callers must await each public operation before
starting the next one on the same session.

Session disposal drains the admitted exchange, not every remaining step of that public workflow.
Post-exchange parsing, cache updates, later exchanges, and application callback work need the caller's
complete awaited scope. Finish the operation before disposal. A callback must neither dispose its own
session nor wait for a disposal that needs the callback to return.

A token already canceled at entry throws before the guard is claimed. Once claimed, the logical
exchange receives `CancellationToken.None` for constituent transmits so caller cancellation cannot
interrupt APDU chaining, CTAP/OTP frames, or SCP state midway. Cancellation semantics therefore differ by
protocol. A PC/SC or SCP exchange observes the caller token only at admission: if the token is canceled
afterwards, the admitted exchange still runs to completion and returns its response or failure rather
than `OperationCanceledException`. During a FIDO HID keep-alive, the caller
token can signal `CTAPHID_CANCEL` after observing a keep-alive; the protocol then waits for the
original terminal response and drains its frames. CANCEL has no response of its own. Core currently
returns a late success payload beginning with `0x00` when the original command byte is unencoded CBOR
`0x10`. An encoded `0x90` request can correlate with the terminal response but currently misses that
success classification and can surface cancellation instead. This normalization gap remains separate
from whether the authenticator actually canceled the request and from the no-response CANCEL rule.
With no further report, the
wait can remain pending indefinitely. During an OTP HID touch wait, the caller
token can trigger a dummy-report reset;
reuse requires that reset to succeed. Neither signal rolls back work already sent. A failed partial
APDU exchange or failed OTP reset requires disposing and reopening the connection. The guard resets
in `finally`, but this alone is not proof that device state is reusable.

Session disposal closes the protocol guard atomically with respect to operation admission. New operations are
refused immediately; an operation already admitted is drained before protocol state, SCP session keys, or an
owned connection are disposed. `DisposeAsync` awaits that drain. Synchronous `Dispose` blocks for the same result
and must not be called from inside the operation being drained.

What the drain leaves behind differs by protocol:

- A SmartCard (PC/SC or SCP) disposal waits for the admitted exchange to finish on the wire. When that
  exchange succeeds, a borrowed connection remains usable by the next session.
- A FIDO HID disposal that finds no admitted exchange does not touch a borrowed connection, which remains
  usable by the next session.
- If a FIDO HID exchange is admitted when disposal begins on the built-in macOS/Linux connection, disposal
  terminally wakes that connection's input so the drain cannot wait indefinitely for a report. The wake
  does not dispose the connection, but it may leave it unusable, even when the exchange finishes
  concurrently with the wake. Windows/custom compatibility connections receive no equivalent wake, and their drain waits
  for the exchange's native I/O.

Treat a FIDO HID disposal that began while an exchange was active like a failed partial exchange: dispose
and reopen the connection instead of creating another session over it.

The guard belongs to one protocol instance (the SCP wrapper shares its base PC/SC guard). Overlapping
logical exchanges are rejected, not serialized. Independently creating multiple raw protocol instances
over one connection does not create a connection-wide guard; that lower-level usage is outside the
one-application-session-per-connection ownership contract. Direct raw-connection calls bypass the guard,
so excluding them from a live session is the caller's responsibility. After
admission, liveness is bounded by the underlying native operation rather than caller cancellation. This is
the deliberate tradeoff for never abandoning a stateful exchange halfway through its wire sequence.

## Platform notes

- macOS FIDO opens remain non-seizing (`kIOHIDOptionsTypeNone`); the registry, not a platform seize
  option, owns in-process admission.
- Windows OTP HID feature reports use `DESIRED_ACCESS.NONE`; keyboard collections reject ordinary
  read/write access.
- Windows FIDO HID read/write access may require elevation under the host's access policy. Enumeration
  metadata alone does not establish that a report handle can be opened or used.
- Cross-process PC/SC contention is not converted into an SDK transport retry.

## Invariants and pinning tests

| Invariant | Pinned by |
|---|---|
| Cross-interface connection on a grouped key is refused; first remains usable; disposal permits reopen | `ConnectionOwnershipContractTests.ConnectAsync_CcidHeld_GroupedKeysHidInterfaceIsRefused` |
| Same-interface second connections are refused and standalone devices reopen | `ConnectionOwnershipContractTests` |
| Multi-member claims deduplicate, roll back, coordinate discovery, and admit one racing winner | `DeviceConnectionRegistryTests` |
| One live session per connection and sequential session reuse | `SessionConstructionGuardTests`, `ConnectionOwnershipContractTests` |
| Protocols never dispose borrowed connections | `ProtocolConnectionOwnershipTests` |
| Overlapping exchanges throw; sequential calls and post-failure reuse succeed | `ExchangeGuardTests`, `PcscProtocolConcurrencyTests`, `FidoHidProtocolConcurrencyTests`, `OtpHidProtocolConcurrencyTests` |
| Disposal refuses new operations and drains an admitted exchange before protocol/SCP/connection teardown | `ExchangeGuardTests`, `RawSessionYubiKeyExtensionsTests`, `PcscProtocolScpTests` |
| SmartCard cancellation after admission returns the drained response; a borrowed connection is reused after the drain | `RawSmartCardNativeBoundaryTests` |
| Idle FIDO HID disposal skips the terminal wake; active disposal wakes and drains without disposing a borrowed connection | `FidoHidProtocolTests`, `Ctap2SessionHidCrossLayerTests` |
| Held connection and PC/SC sharing failures do not trigger another transport | `SessionTransportTests`, applet `IYubiKeyExtensionsTransportTests` |
| A refused second ownership attempt does not damage the active PIV session | `PivSessionContentionTests` |
| Distinct physical keys remain independent | `PivMultiKeyContentionTests` |

## Known bounds

- In-process only; other processes are governed by platform APIs.
- Admission never waits for a live connection or active protocol exchange; only disposal drains an
  admitted exchange.
- Separate discovery records have independent member scopes; PID-only grouping retains its G2 bound.
- Hotplug exception type is unspecified. Native completion and release may remain unbounded; uncertain
  cleanup is not permission to release a claim or reopen the interface.

## Troubleshooting: wedged macOS OTP HID

If macOS OTP HID reports `IOHIDDeviceGetReport = 0xE00002E2` (`kIOReturnNotPermitted`) and OTP interfaces
appear as standalone `hid:...` records, verify independently with `ykman --device <serial> otp info`.
Do not truncate that command's output: it may otherwise continue over CCID and hide the HID failure.
Check host privacy/access policy as well as device state. Apple's inspected IOHID implementation can
return NotPermitted when required authorization is denied; the code alone does not identify that as
the cause of every failure. `kIOReturnNotOpen` is the different value `0xE00002CD`. The retained numeric
error belongs to the historical observation; correcting its name does not diagnose the original fault.
Replugging can help a device-state fault, but cannot substitute for required host authorization.
