# Connection Ownership and Contention

Who owns a connection, which interfaces may be held concurrently, and what happens when two callers
want the same one.

Every rule below cites either the test that pins it or the hardware measurement that motivates it. A
rule with neither is not a rule — treat the behaviour as unspecified and report it.

Scope: in-process contention only. Cross-process contention is **not** covered; another process
holding a PC/SC reader surfaces as an ordinary platform sharing violation. See
[Physical Device Model](physical-device-model.md) for the device model,
[Device Discovery Guarantees](device-discovery-guarantees.md) for interface grouping, and
`src/Core/CLAUDE.md` for the working summary this document backs.

## The motivating defect

Three lines of ordinary public API used to destroy session state silently:

```csharp
await using var piv = await yubiKey.CreatePivSessionAsync();
await piv.VerifyPinAsync(pin);
var info = await yubiKey.GetDeviceInfoAsync();   // used to deselect PIV underneath the session
```

A YubiKey's CCID interface holds **one selected applet**. The Management query issued a second
`SELECT`, which deselected PIV, and the verified-PIN state went with it. The failure was silent: the
next PIV call simply behaved as if the PIN had never been verified.

**Measured, not inferred:** the deselect was reproduced on hardware and returns `SW=0x6D00`. This
measurement is the reason CCID is exclusive rather than shared, and it is why the enforcement lives
at *acquisition* rather than at the wire.

Pinned by `PivSessionContentionTests` (hardware) and `ConnectionOwnershipContractTests` /
`DeviceConnectionRegistryTests` (unit).

## Ownership: a protocol never owns its connection

**Whoever creates a connection disposes it.** Protocols and sessions are pure users.
`PcscProtocol`, `FidoHidProtocol`, `OtpHidProtocol`, and a session built by
`Session.CreateAsync(connection)` never dispose a connection handed to them.

The one exception is deliberate and internal: an `IYubiKey.Create<App>SessionAsync` convenience
entry point opens a connection the caller never sees, so it calls `ApplicationSession.OwnConnection()`
to transfer that connection's lifetime to the session it returns.

> **Landmine.** The interface lease belongs to the *connection*. A protocol that disposes a
> connection it was handed releases that lease out from under its owner. Upstream still calls
> `_connection.Dispose()` in all three protocols; this SDK deliberately does not.
> `ProtocolConnectionOwnershipTests` pins this, and those pins are load-bearing — during the
> `origin/yubikit` merge, `FidoHidProtocol`'s upstream side was a +16/−110 rewrite where "take
> theirs" was the low-friction resolution and would have silently reintroduced the defect. **Do not
> weaken those pins.**

There is no finalizer backstop. A leaked connection retains its lease for the process lifetime and
blocks later opens, so `await using` is mandatory.

## Exclusivity, and where each rule comes from

Provenance matters here and is easy to conflate. Two of these rules are ours; one is canonical.

| Interface | Policy | Provenance |
|---|---|---|
| **CCID / SmartCard** | Exclusive — a second connection is refused immediately with `ConnectionInUseException` naming the interface | **Canonically motivated.** Rust `platform/pcsc.rs` `PcscConnection::open()` tries `ShareMode::Exclusive` first, falls back to shared, and will kill `scdaemon`/`yubikey-agent` to obtain exclusivity. The mechanism differs — canonical uses cross-process PC/SC share modes, we use an in-process lease — but the intent matches |
| **OTP HID** | Exclusive | **This SDK's own strengthening. Not canonical parity.** Neither Rust nor Python yubikit enforces in-process OTP exclusivity — `HidOtpConnection::new` just opens the path, and `yubikit/core/otp.py` is a bare ABC. They do not need it because neither produces two concurrent in-process OTP handles, whereas this SDK exposes a public `ConnectAsync` a caller can invoke twice. **Cite the interleaving hazard, not canonical**: one logical OTP frame spans multiple feature reports, and separate protocol instances must not interleave them |
| **FIDO HID** | Shared — the lease *admits* a second connection | Required by the Management-over-HID fallback when CCID is held |

**Admission is not a promise of concurrent conversations.** FIDO HID admitting a second connection
does not mean two handles can hold concurrent CTAP conversations. On macOS the input report is
delivered to whichever handle's run loop runs, so two handles do not demultiplex — pinned on
hardware by `FidoHidSharingIntegrationTests.SendOnFirst_ReceiveOnSecond_RevealsReportMisrouting`.
Drive CTAP over one FIDO connection at a time, which is also canonical practice: neither Rust nor
Python opens two concurrent host-side FIDO handles. `CTAPHID_LOCK` is device-side channel
arbitration, not a host handle policy.

### Platform constraints that interact with these rules

- **macOS FIDO opens must stay non-seizing** (`kIOHIDOptionsTypeNone`). Seizing makes the platform
  refuse the second open with `kIOReturnExclusiveAccess` (`0xE00002C5`), contradicting the shared-FIDO
  contract. Both canonical implementations open non-seizing. This was a real defect, found on
  hardware and fixed.
- **Windows OTP HID feature reports must open with `DESIRED_ACCESS.NONE`.** The OTP interface is a
  keyboard top-level collection; Windows refuses `GENERIC_READ | GENERIC_WRITE` on the system
  keyboard even for an administrator. The OTP protocol uses only `HidD_GetFeature`/`SetFeature`,
  which succeed on a zero-access handle. `NONE` is confirmed sufficient across every enumerated
  reachable feature-report call site; the IO/FIDO connection keeps read/write. Note this is **not**
  legacy-SDK parity — v1 opens the feature connection with `GENERIC_WRITE`.
- **Windows FIDO HID requires elevation.** Windows admits read/write on the FIDO HID top-level
  collection only to an administrator, so the CCID-held Management fallback — which routes over FIDO
  HID — requires an elevated process on Windows. A non-elevated failure here is a platform
  characteristic, not a code defect.

## Session binding

`ConnectionSessionGuard` allows **one live `ApplicationSession` per connection** and refuses a second
with the offending session's name. Sequential reuse is supported: dispose one session before creating
the next over the same connection.

Binding happens in `ApplicationSession.Construct`, **after** construction succeeds — not during it. A
derived constructor that throws must not strand the guard, and making the stranded state
unrepresentable is preferred to cleaning it up afterwards. Two cleanup-based designs were rejected as
unsafe before this one.

> **Landmine.** `DisposalGate` makes a failed teardown **terminal and shared**: a later dispose
> replays the same exception instance to every caller. This interacts with
> `DisposeAfterInitializationFailure`, which suppresses cleanup failures to preserve the primary
> exception. Upstream's equivalent test can use `using`; ours cannot.

## Discovery interaction

Discovery and connections coordinate through `DeviceConnectionRegistry` on a nonblocking exclusive
discovery lease:

- Discovery **skips immediately** while any member connection is active, including exclusive OTP HID.
- Connections **wait, cancellably**, while discovery holds the interface across connect, Management
  exchange, and disposal.
- Discovery uses only the internal `IDiscoveryConnectionProvider` path. Wrapper or custom `IYubiKey`
  implementations without it are skipped rather than driven through public `ConnectAsync`.
- Idle coordinator entries are retained for the process lifetime to avoid unsafe eviction races, and
  are bounded by the number of unique interface IDs observed.

`YubiKeyConnectionExtensions.IsHeldTransportError` treats an in-process refusal exactly like a PC/SC
sharing violation, so Management's default order can try `SmartCard`, then `HidFido`, then `HidOtp`.
An already-held OTP interface refuses that final acquisition, and an explicit `preferredConnection`
never falls back.

> **Landmine.** `ManagementSession.Transport` exists only on this branch — upstream has no `Transport`
> concept. It was silently lost during merge resolution and **only the PIV hardware contention tests
> caught it**: no unit test covers it, and reading the diff did not reveal it. If you restructure
> `ManagementSession`, run PIV integration.

## Invariants and their pinning tests

| Invariant | Pinned by |
|---|---|
| PIV session + `GetDeviceInfoAsync` does not clobber session state | `PivSessionContentionTests.GetDeviceInfoAsync_WhilePivSessionHasVerifiedPin_DoesNotClobberSessionState` |
| Management routes over a non-SmartCard transport while PIV holds CCID | `PivSessionContentionTests.CreateManagementSessionAsync_WhilePivHoldsCcid_OpensOverANonSmartCardTransport` |
| A second SmartCard connection is refused while a PIV session is open | `PivSessionContentionTests.ConnectAsync_SecondSmartCardConnection_WhilePivSessionOpen_IsRefused` |
| CCID-only key with no fallback fails naming the held interface | `IYubiKeyExtensionsTransportTests.CreateManagementSessionAsync_CcidHeldInProcess_NoOtherTransport_Throws` |
| Exclusive interfaces reopen after disposal (CCID and OTP HID) | `ConnectionOwnershipContractTests.ConnectAsync_AfterFirstConnectionDisposed_SecondSucceeds`, `.ConnectAsync_OtpHidConnectionDisposed_InterfaceReopens` |
| Ownership cannot be crossed by a session starting during a discovery scan | `DeviceConnectionOwnershipTests.ConnectAsync_SessionStartingImmediatelyBeforeDiscoverySelect_CannotCrossOwnership` |
| Sessions on two different keys stay independent | `PivMultiKeyContentionTests` |
| Hotplug mid-session fails bounded and does not strand the CCID lease | `PivHotplugContentionTests.PivSession_KeyRemovedMidSession_FailsBoundedAndDoesNotStrandTheCcidLease` (self-fails if no removal occurs) |
| A second FIDO HID connection is admitted | `FidoHidSharingIntegrationTests.ConnectAsync_SecondConcurrentFidoHidConnection_IsAdmitted` |
| Two FIDO handles do not demultiplex | `FidoHidSharingIntegrationTests.SendOnFirst_ReceiveOnSecond_RevealsReportMisrouting` |
| Protocols never dispose a borrowed connection | `ProtocolConnectionOwnershipTests` |

**There is no waiter for an already-held exclusive connection.** A second acquisition refuses
immediately rather than queueing. This is a documented bound, not an oversight.

## Bounds and known gaps

- **In-process only.** Cross-process contention is out of scope.
- **No queueing on exclusive interfaces** — see above.
- **Removal-time exception type is unspecified.** Hotplug is pinned to fail *bounded* and not strand
  the lease; the exception type is not asserted.
- **macOS removal does not promptly wake a blocked read.** `MacOSHidIOReportConnection`'s removal
  callback is intentionally empty: it previously called `CFRunLoopStop` on an `IOHIDDeviceRef`, which
  is undefined behaviour and never woke the read anyway, because the run loop that should be stopped
  is the one captured inside `GetReport`. A read in progress during removal runs to its
  `CFRunLoopRunInMode` timeout. Waking it promptly requires threading the scheduled run loop through
  as callback context plus hardware unplug testing, and is deliberately deferred.

## Troubleshooting: wedged macOS OTP HID

If macOS OTP HID starts failing with `IOHIDDeviceGetReport = 0xE00002E2` (`kIOReturnNotOpen`) and OTP
interfaces appear as standalone `hid:...` rows instead of merging into composite devices, the host —
not the SDK — is wedged.

Confirm it independently of this SDK:

```bash
ykman --device <serial> otp info
```

The discriminator is the line `WARNING: Failed opening device`. **Never pipe this through `head`** —
doing so lets the command fall back to CCID and exit 0 while HID is still broken.

Remedy: **replug the keys first.** An earlier note in this project claimed a full restart was required;
that has since been contradicted — a replug cleared the fault and returned Core integration to a full
pass. Restart only if replugging does not.
