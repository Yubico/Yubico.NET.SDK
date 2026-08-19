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

When discovery proves that interfaces belong to one `CompositeYubiKey`, every member receives the
same sorted lease scope. Opening any member atomically claims all IDs in that scope. Therefore:

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
session disposal detach the exact holder, so session N+1 can reuse the still-open connection.

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

`PcscProtocol`, `PcscProtocolScp`, `FidoHidProtocol`, and `OtpHidProtocol` protect complete logical
exchanges with `ExchangeGuard`. Sequential awaited calls are unchanged. If a second operation starts
while one is active, it throws `InvalidOperationException` immediately rather than queueing.

A token already canceled at entry throws before the guard is claimed. Once claimed, the logical
exchange receives `CancellationToken.None` and runs to completion so APDU chaining, CTAP/OTP frames,
and SCP state cannot be stranded between constituent transmits. The guard resets in `finally`.

The guard belongs to one protocol instance (the SCP wrapper shares its base PC/SC guard). Independently
creating multiple raw protocol instances over one connection does not create a connection-wide guard;
that lower-level usage is outside the one-application-session-per-connection ownership contract. After
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
| Held connection and PC/SC sharing failures do not trigger another transport | `SessionTransportTests`, applet `IYubiKeyExtensionsTransportTests` |
| A refused second ownership attempt does not damage the active PIV session | `PivSessionContentionTests` |
| Distinct physical keys remain independent | `PivMultiKeyContentionTests` |

## Known bounds

- In-process only; other processes are governed by platform APIs.
- No waiting for a live connection or active protocol exchange.
- Conservative discovery may leave ungrouped interface records with independent one-element scopes.
- Hotplug exception type is unspecified; failure must be bounded and must not strand the lease.

## Troubleshooting: wedged macOS OTP HID

If macOS OTP HID reports `IOHIDDeviceGetReport = 0xE00002E2` (`kIOReturnNotOpen`) and OTP interfaces
appear as standalone `hid:...` records, verify independently with `ykman --device <serial> otp info`.
Do not truncate that command's output: it may otherwise continue over CCID and hide the HID failure.
Replug the key first; restart the host only if replugging does not clear the fault.
