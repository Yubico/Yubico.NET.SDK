# Connection Ownership and Contention

This document defines the SDK's in-process ownership contract:

> A physical YubiKey has at most one live connection, which hosts at most one live session.
> Connections and sessions are reused sequentially; overlapping ownership attempts throw.

Cross-process contention is outside this registry's scope. Another process holding PC/SC may still
surface an ordinary platform `SCardException` such as `SCARD_E_SHARING_VIOLATION`.

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
The product contract is one connection for the physical key, regardless of interface.

## One physical device, one connection

`DeviceConnectionRegistry` is keyed by stable member interface IDs, not by a composite `DeviceId`.
A composite ID describes the evidence tier that formed a group and can change between scans. Member
IDs (PC/SC reader names and HID paths) are the stable ownership records.

When discovery proves that interfaces belong to one flat `YubiKeyDevice`, it stores the interfaces in
optional SmartCard, HID FIDO, and HID OTP slots with one sorted lease scope. Opening any slot atomically
claims all IDs in that scope. Therefore:

- a second connection through the same interface throws `ConnectionInUseException`;
- a second connection through another known member interface throws before native open;
- disposing the first connection releases every member claim, in reverse acquisition order;
- standalone records retain a one-element scope and behave as before;
- failed or canceled multi-member acquisition rolls back every earlier claim.

Acquisition sorts IDs ordinally and removes duplicates. Racing connects use the same order, admit one
winner, and do not deadlock. A connection never waits for another live connection to end.

### Grouping bound

Physical-device exclusivity is only as strong as discovery's evidence. When discovery cannot prove
that standalone records belong to one key, it does not guess. Each record then has a one-element lease
scope, so protection degrades conservatively to per-interface exclusion. See
[Device Discovery Guarantees](device-discovery-guarantees.md).

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

`DisposalGate` tears down the physical connection before releasing its registry lease. Disposal runs
once; all sync and async disposal callers observe the same completion and exception. There is no
finalizer backstop. A leaked connection can retain the physical-device lease for the process lifetime.

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

## Session transport selection

Multi-transport applet entry points select exactly one transport. An explicit valid
`preferredConnection` wins. Without an override, SCP parameters select SmartCard; otherwise the first
supported transport in the applet's documented order is selected. The SDK opens that transport once.

`ConnectionInUseException`, `SCardException`, cancellation, and session-initialization failures all
propagate without trying another interface. If native open succeeds but session initialization fails,
the newly opened connection is disposed before the exception escapes.

`ManagementSession.Transport` reports the transport actually opened. Keep its constructor assignment
when resolving upstream changes; upstream does not expose an equivalent transport property.

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

A token already canceled at entry throws before the guard is claimed. Once claimed, the logical
exchange receives `CancellationToken.None` for constituent transmits so caller cancellation cannot
interrupt APDU chaining, CTAP/OTP frames, or SCP state midway. Cancellation semantics therefore differ by
protocol. A PC/SC or SCP exchange observes the caller token only at admission: if the token is canceled
afterwards, the admitted exchange still runs to completion and returns its response or failure rather
than `OperationCanceledException`. During a FIDO HID keep-alive, the caller
token can signal `CTAPHID_CANCEL`; the protocol waits for a terminal response and drains its frames
before reporting cancellation when that response is valid. During an OTP HID touch wait, the caller
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
- If a FIDO HID exchange is admitted when disposal begins on the built-in macOS connection, disposal
  terminally wakes that connection's input so the drain cannot wait indefinitely for a report. The wake
  does not dispose the connection, but it may leave it unusable, even when the exchange finishes
  concurrently with the wake. Other built-in FIDO HID connections receive no wake, and their drain waits
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
- Windows FIDO HID read/write access requires elevation.
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
| Idle FIDO HID disposal skips the terminal wake; active disposal wakes and drains without disposing a borrowed connection | `FidoHidProtocolTests`, `FidoSessionHidCrossLayerTests` |
| Held connection and PC/SC sharing failures do not trigger another transport | `SessionTransportTests`, applet `IYubiKeyExtensionsTransportTests` |
| A refused second ownership attempt does not damage the active PIV session | `PivSessionContentionTests` |
| Distinct physical keys remain independent | `PivMultiKeyContentionTests` |

## Known bounds

- In-process only; other processes are governed by platform APIs.
- Admission never waits for a live connection or active protocol exchange; only disposal drains an
  admitted exchange.
- Conservative discovery may leave ungrouped interface records with independent one-element scopes.
- Hotplug exception type is unspecified; failure must be bounded and must not strand the lease.

## Troubleshooting: wedged macOS OTP HID

If macOS OTP HID reports `IOHIDDeviceGetReport = 0xE00002E2` (`kIOReturnNotOpen`) and OTP interfaces
appear as standalone `hid:...` records, verify independently with `ykman --device <serial> otp info`.
Do not truncate that command's output: it may otherwise continue over CCID and hide the HID failure.
Replug the key first; restart the host only if replugging does not clear the fault.
