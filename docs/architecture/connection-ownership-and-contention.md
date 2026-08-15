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

### What the measurement actually showed

Four experiments on a firmware-5.8.0 key (PID 0x0407, macOS), each with its prediction recorded
before the run. The details matter because three of them contradict the obvious reading:

| Experiment | Result | Repeats |
|---|---|---|
| PIV session + verified PIN, then `GetDeviceInfoAsync` | **destroys it** — `SW=0x6D00` | 4/4 |
| PIV session + verified PIN, then an **OATH** session | **destroys it** — `SW=0x6D00` | 1/1 |
| PIV session + verified PIN, then a **second PIV** session | **safe** — the sign still succeeds | 4/4 |
| Management over HID while PIV holds CCID | **safe** — PIV survives untouched | 1/1 |

1. **It is worse than "the PIN is lost."** `0x6D00` is *instruction not supported*, not `0x6982`
   *security status not satisfied*. The applet is not merely deauthenticated, it is **entirely
   deselected** — the card no longer recognises PIV instructions at all. Any message or comment
   describing this as losing the verified PIN understates it.
2. **The damage is invisible at the call site.** `GetDeviceInfoAsync` itself returns **OK**. Nothing
   tells the caller anything was disturbed; the failure only surfaces on the victim session's *next*
   operation.
3. **It is broad, not specific to `GetDeviceInfoAsync`.** Any second applet session over CCID does
   it — OATH was confirmed by the same mechanism. Teaching one convenience method to avoid a held
   CCID would fix the common case (the SDK stepping on its own session) and leave the general case
   open.
4. **Re-selecting the *same* applet was safe on this firmware.** A second PIV session did not disturb
   the first's verified PIN. That result killed an earlier design that would have keyed the lease by
   interface alone on the assumption nesting was unnecessary — the hardware said the device supports
   something that design would have forbidden. The shipped design refuses the second *connection*
   before this ever comes up, so the nesting case is unreachable through the public API today; the
   measurement is retained because it is the reason the enforcement level was chosen deliberately
   rather than by default.
5. **The HID fallback is hardware-validated, not merely plausible.** Management answers correctly
   over both HID OTP and HID FIDO while PIV holds CCID.

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
| **FIDO HID** | Exclusive | **This SDK's own strengthening.** One physical FIDO HID interface admits one SDK connection and native handle; a second attempt is refused before native open. This matches canonical practice, where neither Rust nor Python opens two concurrent host-side FIDO handles |

### Platform constraints that interact with these rules

- **macOS FIDO opens must stay non-seizing** (`kIOHIDOptionsTypeNone`). In-process ownership is enforced
  before native open; seizing would impose a separate platform-wide exclusion policy. Both canonical
  implementations open non-seizing.
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

`YubiKeyConnectionExtensions.IsFallbackEligibleHeldError` treats an in-process refusal exactly like a PC/SC
sharing violation, so Management's default order can try `SmartCard`, then `HidFido`, then `HidOtp`.
If CCID and FIDO are held but OTP is free, Management reaches OTP. An already-held OTP interface
refuses that final acquisition, and an explicit `preferredConnection` never falls back.

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
| Exclusive interfaces reopen after disposal (CCID, FIDO HID, and OTP HID) | `ConnectionOwnershipContractTests.ConnectAsync_AfterFirstConnectionDisposed_SecondSucceeds`, `.ConnectAsync_FidoHidConnectionDisposed_InterfaceReopens`, `.ConnectAsync_OtpHidConnectionDisposed_InterfaceReopens` |
| Ownership cannot be crossed by a session starting during a discovery scan | `DeviceConnectionOwnershipTests.ConnectAsync_SessionStartingImmediatelyBeforeDiscoverySelect_CannotCrossOwnership` |
| Sessions on two different keys stay independent | `PivMultiKeyContentionTests` |
| Hotplug mid-session fails bounded and does not strand the CCID lease | `PivHotplugContentionTests.PivSession_KeyRemovedMidSession_FailsBoundedAndDoesNotStrandTheCcidLease` (self-fails if no removal occurs) |
| A second FIDO HID connection is refused | `ConnectionOwnershipContractTests.ConnectAsync_SecondConnectionToHeldFidoHidInterface_IsRefusedBeforePhysicalOpen`, `FidoHidOwnershipIntegrationTests.ConnectAsync_SecondConcurrentFidoHidConnection_IsRefused` |
| Held CCID and FIDO fall back to OTP | `IYubiKeyExtensionsTransportTests.CreateManagementSessionAsync_CcidAndFidoHeldInProcess_FallsBackToHidOtp` |
| Protocols never dispose a borrowed connection | `ProtocolConnectionOwnershipTests` |

**There is no waiter for an already-held exclusive connection.** A second acquisition refuses
immediately rather than queueing. This is a documented bound, not an oversight.

### How this coverage was audited

The invariants above came from an enumerated register rather than an assertion of thoroughness —
"99% of edge cases" is not a computable number, but an enumerated list with stated in/out reasoning
is something a reviewer can disagree with line by line. Cases were tiered:

| Tier | Meaning | Policy |
|---|---|---|
| **P1** | Happens in normal use | Must be covered by a test |
| **P2** | Happens in real deployments | Must be covered by a test, or a documented bound with a pinning test |
| **P3** | Rare but real | Covered where verifiable on the available rig, else recorded as a platform gap |
| **P4** | Extreme | Explicitly out, with reasoning |

Final state: **23 rows in scope — 20 covered, 3 documented bounds, 0 open, 0 platform gaps.** The
three bounds are the ones listed under [Bounds and known gaps](#bounds-and-known-gaps).

Hardware evidence spans macOS (two same-PID firmware-5.8.0 keys), Linux (firmware 5.4.3), and
Windows 11 (the same firmware-5.8.0 keys). Cross-platform verification was not ceremonial — it
produced two production fixes that single-platform testing had missed: the macOS seizing-open defect
and the Windows OTP HID zero-access requirement.

If you add a case, add its tier and either a test or an explicit bound. A row with neither is not
coverage.

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
