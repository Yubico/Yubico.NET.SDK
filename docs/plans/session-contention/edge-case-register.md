# Edge-case register — session contention

Companion to [ISA.md](ISA.md). Every case that could plausibly arise, with an explicit
likelihood tier and an in/out decision. This exists so coverage can be **audited** rather than
asserted — "99% of edge cases" is not a computable number, but an enumerated register with
stated in/out reasoning is something you can disagree with line by line.

## Tiers

| Tier | Meaning | Policy |
|---|---|---|
| **P1** | Happens in normal use | Must be covered by a test |
| **P2** | Happens in real deployments | Must be covered by a test or a documented bound with a pinning test |
| **P3** | Rare but real | Covered where verifiable on the available rig; otherwise recorded as a platform gap |
| **P4** | Extreme | Explicitly out, with reasoning |

Status values: `open` · `investigating` · `covered` · `bounded` (documented bound + pinning test)
· `platform-gap` (needs hardware we do not have) · `out`

Hardware evidence spans macOS (two same-PID firmware-5.8.0 keys, serials 103 and 125), Linux
(firmware-5.4.3 keys 9681620 and 20260533), and Windows 11 (the same firmware-5.8.0 serials 103 and 125).
Test names below are exact repository method names; paths are relative to the repository root.

---

## A. Session × session on one physical key

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| A1 | PIV session + `GetDeviceInfoAsync` — **the footgun** | P1 | covered | `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSessionContentionTests.cs`: `GetDeviceInfoAsync_WhilePivSessionHasVerifiedPin_DoesNotClobberSessionState`, `CreateManagementSessionAsync_WhilePivHoldsCcid_OpensOverANonSmartCardTransport`; Phase 1 pre-fix hardware result `SW=0x6D00` |
| A2 | PIV + OATH concurrently on one CCID (two applets) | P2 | covered | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ConnectionOwnershipContractTests.cs`: `ConnectAsync_SecondConnectionToHeldCcidInterface_IsRefused`, `Session_SecondLiveSessionOnOneConnection_IsRefused`; generic refusal is applet-independent |
| A3 | PIV + PIV nested (same applet) | P2 | covered | `ConnectionOwnershipContractTests.Session_SecondLiveSessionOnOneConnection_IsRefused`; `PivSessionContentionTests.SecondSession_OnOneLiveConnection_IsRefused`. Phase 1 measured nesting hardware-safe, but shared security state makes deliberate refusal the contract |
| A4 | Management(CCID) + Management(HidOtp), both writing configuration | P2 | bounded | SDK coordinates per-interface wire ownership, not semantic concurrent configuration writes across CCID and HID OTP. Callers must serialize configuration writes. Admission of distinct interfaces is pinned by `ConnectionOwnershipContractTests.ConnectAsync_CcidHeld_SameKeysHidInterfaceStillConnects`; no destructive concurrency test is run |
| A5 | Management(CCID) + FIDO2(HidFido) on distinct interfaces | P2 | covered | `ConnectionOwnershipContractTests.ConnectAsync_CcidHeld_SameKeysHidInterfaceStillConnects` and `ConnectAsync_HidInterface_AllowsConcurrentConnections` |
| A6 | Sessions on two **different** keys remain independent | P1 | covered | `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMultiKeyContentionTests.cs`: current macOS run 3/3 on firmware-5.8.0 serials 103/125, including `FindAllAsync_WithOpenSessionOnOneKey_IdentifiesOtherKeysAndPreservesSession`, `ConcurrentPivSessions_OnTwoKeys_OperateIndependently`, and `Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey` |

## B. Transport availability

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| B1 | CCID-only key — no safe fallback; fail naming held interface | P2 | covered | `src/Management/tests/Yubico.YubiKit.Management.UnitTests/IYubiKeyExtensionsTransportTests.cs`: `CreateManagementSessionAsync_CcidHeldInProcess_NoOtherTransport_Throws`; `PivSessionContentionTests.ConnectAsync_SecondSmartCardConnection_WhilePivSessionOpen_IsRefused` |
| B2 | HID-only key (no CCID) | P2 | covered | `IYubiKeyExtensionsTransportTests.CreateManagementSessionAsync_DefaultNoSmartCard_FallsBackToHidFido`, `CreateManagementSessionAsync_DefaultOnlyHidOtp_FallsBackToHidOtp`; Phase 1 hardware experiment 4 |
| B3 | NFC (CCID, no HID fallback) | P2 | covered | Same PC/SC/CCID ownership path as B1: `ConnectionOwnershipContractTests.ConnectAsync_SecondConnectionToHeldCcidInterface_IsRefused`; NFC exposes no HID route, so no fallback exists |
| B4 | `preferredConnection = SmartCard` conflicts with held CCID | P2 | covered | `IYubiKeyExtensionsTransportTests.CreateManagementSessionAsync_CcidHeldInProcess_ExplicitOverrideDoesNotFallBack` |
| B5 | SCP requested + CCID held — no plaintext HID downgrade | P2 | covered | `IYubiKeyExtensionsTransportTests.CreateManagementSessionAsync_ScpRequestedAndCcidHeld_DoesNotFallBackToPlaintextHid` |

## C. Discovery × session — already solved, must not regress

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| C1 | Discovery vs session, same interface | P1 | covered | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionRegistryTests.cs`: `IdentityRead_DeviceInUse_SkipsWithoutConnecting` |
| C2 | Discovery vs session, different member interface of same key | P2 | covered | `DeviceConnectionRegistryTests.MetadataRead_CompositeWithInUseSmartCardMember_SkipsItButTriesOtpTransport`. Discovery skips any held member, including exclusive OTP HID, while trying free members |

## D. Lifecycle and timing

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| D1 | Exclusive interface released, then acquired by another caller | P2 | bounded | There is no waiter for an already-held exclusive connection: second acquisition refuses immediately. Success after disposal is pinned for CCID and OTP HID by `ConnectionOwnershipContractTests.ConnectAsync_AfterFirstConnectionDisposed_SecondSucceeds` and `ConnectAsync_OtpHidConnectionDisposed_InterfaceReopens` |
| D2 | Session opened while a scan is in flight | P2 | covered | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionOwnershipTests.cs`: `ConnectAsync_OwnsInterfaceBeforePhysicalConnectionCreation`, `ConnectAsync_SessionStartingImmediatelyBeforeDiscoverySelect_CannotCrossOwnership` |
| D3 | Hotplug during an open session | P3 | covered | Confirmed on macOS hardware with an operator-coordinated removal. `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivHotplugContentionTests.cs`: `PivSession_KeyRemovedMidSession_FailsBoundedAndDoesNotStrandTheCcidLease` — the PIV call failed within bounds instead of hanging, disposal completed with the card absent, and reopening did NOT report `ConnectionInUseException`, so removal does not strand the exclusive CCID lease. The test self-fails if no removal occurs, so a passing run always means a real unplug happened |

## E. Device identity

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| E1 | Tier flip on inserting a second same-PID key — no phantom incumbent event | P1 | covered | Deterministic repository pin: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceRepositoryCompositeTests.cs`: `UpdateCache_SiblingSamePidKeyArrives_IncumbentEmitsNoRemovedOrAdded`. **Windows hardware run (Phase 12, elevated, two same-PID firmware-5.8.0 keys):** inserting the 2nd key emitted exactly **one Added** for the new key and **no** event for the incumbent, whose DeviceId stayed stable. **Platform caveat — a Windows pass does not transfer.** On Windows both keys resolve by the tier-1 **topology** (Container ID) path, which `ProtocolDeviceInfo` documents as `null` on macOS/Linux; those platforms degrade to the serial/PID tiers. **Closed on macOS in Phase 14:** with 103 alone the incumbent resolved as `ykphysical:pid:0407` (PID tier) and inserting the second same-PID key forced the flip to serial evidence — the incumbent emitted **no** event across that flip, and only the newcomer produced one Added. The serial↔PID flip is therefore hardware-confirmed, not just unit-pinned |
| E2 | Tier flip on sibling removal and final removal correlation | P1 | covered | Deterministic repository pins: `YubiKeyDeviceRepositoryCompositeTests.UpdateCache_SiblingSamePidKeyRemoved_SurvivorEmitsNoRemovedOrAdded`, `UpdateCache_TierFlipThenFinalRemoval_RemovalUsesPreviouslyAddedDeviceId`. **Windows hardware run (Phase 12):** removing one sibling emitted exactly **one Removed** for the removed key with **no** event for the survivor; the final removal emitted exactly **one Removed** whose DeviceId equalled the one previously Added. 4 physical actions → 4 events, zero phantom events, exact add/remove correlation. **Closed on macOS in Phase 14:** removing one sibling emitted exactly one Removed for it with no event for the survivor, and the final removal reported the same DeviceId previously published. 7 physical actions → 7 events, zero phantoms, on the serial/PID degraded path |

## F. Platform

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| F1 | macOS physical HID FIDO double-open (`IOHIDDeviceOpen` options `0x01`) | P3 | covered | Closed on macOS hardware and it found a real defect. Seizing made the platform refuse the second open with `kIOReturnExclusiveAccess` (`0xE00002C5`) while the lease admitted it, so the shared-FIDO contract was false. Fixed by opening non-seizing, matching both canonical implementations. Pinned by `src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/FidoHidSharingIntegrationTests.cs`: `ConnectAsync_SecondConcurrentFidoHidConnection_IsAdmitted` (regression pin) with `ConnectAsync_SingleFidoHidConnection_CompletesCtapHidInit` as the baseline |
| F4 | Two FIDO HID handles do not demultiplex input reports | P2 | bounded | Discovered while closing F1; confirmed cross-platform in Phase 11. Shared FIDO admits a second connection but the transport does not route input reports per handle: on both macOS and Windows, CTAPHID_INIT sent on one handle is readable on the other. Bound: drive CTAP over one FIDO connection at a time on every platform. Pinned by `FidoHidSharingIntegrationTests.SendOnFirst_ReceiveOnSecond_RevealsReportMisrouting`, which passes precisely because the report is misrouted and will fail if the transport ever demultiplexes |
| F2 | Platform-divergent HID sharing semantics | P3 | covered | Verified on all three platforms. Linux and macOS: FIDO HID shared, OTP HID SDK-exclusive. Windows (Phase 11, elevated, serials 103/125): `FidoHidSharingIntegrationTests` 3/3 — FIDO HID admits a second connection and, like macOS, does not demultiplex (see F4). OTP HID is openable on Windows after the Phase 11 feature-report open fix (row F5) |
| F3 | Windows PC/SC sharing semantics under contention | P3 | covered | Closed on Windows hardware (Phase 11). `PivSessionContentionTests` 5/5 elevated, including `ConnectAsync_SecondSmartCardConnection_WhilePivSessionOpen_IsRefused`, whose message assertion confirms the Windows PC/SC identity surfaces as `pcsc:...` under contention. Characterization, not a defect: the CCID-held Management fallback routes through FIDO HID, which Windows admits only to an elevated process, so that fallback requires Administrator on Windows |
| F5 | Windows OTP HID feature connection opened the keyboard collection read/write | P2 | covered | Found and fixed in Phase 11. The OTP interface is a keyboard top-level collection and the OTP protocol uses only HID feature-report IOCTLs (`HidD_GetFeature`/`SetFeature`), which succeed on a zero-access handle; but `HidDDevice.OpenFeatureConnection()` opened it `GENERIC_READ | GENERIC_WRITE`, which Windows refuses on the system keyboard even when elevated, so OTP HID could not be opened at all. Fix in `src/Core/src/Native/Windows/HidD/HidDDevice.cs`: open the feature connection with `DESIRED_ACCESS.NONE` (IO/FIDO connection stays read/write), matching the legacy Yubico .NET SDK. Pinned by `CompositeDiscoveryIntegrationTests.ConnectAsync_TypedTransports_OnEveryReturnedDevice_Succeed` (was RED with access-denied, now 5/5) and end-to-end by `YubiOtpSessionIntegrationTests.CalculateHmacSha1_WithKnownKey_ReturnsExpectedResponse` over HidOtp |

---

## Explicitly out (P4)

| Case | Why out |
|---|---|
| Cross-process contention | The registry is in-process **by contract**, stated in its own docs. Changing that is a different effort with a different design. |
| Device removed mid-APDU | Physical-layer failure. The SDK surfaces the transport error; there is no correct in-SDK recovery. |
| Concurrent firmware update | Requires a deliberate destructive operation the SDK does not orchestrate. |
| More than ~4 keys attached | Beyond realistic deployment for the contention scenarios here. Discovery itself is already bounded and tested for scale. |
| Adversarial local process | Outside the SDK's threat model; cross-process isolation is the OS's job. |

## Coverage summary

| Tier | Total | Covered | Bounded | Platform gap | Open |
|---|---|---|---|---|---|
| P1 | 5 | 5 | 0 | 0 | 0 |
| P2 | 14 | 11 | 3 | 0 | 0 |
| P3 | 4 | 4 | 0 | 0 | 0 |
| **In scope** | **23** | **20** | **3** | **0** | **0** |

ISC-2 passes: every P1/P2 row is covered or has a documented bound and pinning test. **No open rows and
no platform gaps remain.** D3 was closed by an operator-coordinated hotplug run on macOS hardware. F2 and
F3 were closed on Windows hardware in Phase 11, which also generalized F4 from a macOS bound to a
cross-platform one and added F5 — a real Windows OTP HID open defect, found by the verification and fixed.

F1 moved from platform gap to covered on macOS hardware, and closing it produced a production fix plus one
new bounded row (F4). F2's claim of platform-divergent HID sharing now has direct evidence on all three
platforms: Linux and macOS shared FIDO HID, and Windows admits a second FIDO connection while (like macOS)
not demultiplexing input reports. F5 is the second production fix this cross-platform verification produced:
OTP HID feature reports must open the keyboard collection with zero desired access, because Windows refuses
read/write on the system keyboard even for an elevated process.

**Phase 14 update:** E1/E2 now have hardware confirmation on both tiers. The macOS run exercised the
serial↔PID flip the Windows rig structurally could not, observed 7 events for 7 physical actions with zero
phantom incumbent/survivor events, and additionally demonstrated the published-object retention contract on
hardware (the live repository kept `ykphysical:pid:0407` while an independent fresh scan reported
`ykphysical:103`). A Linux run is nice-to-have, not required: Linux has no Container ID either, so it
exercises the same degraded tiers now covered. Original Phase 12 note follows.

E1 and E2 gained partial hardware evidence in Phase 12: an operator-coordinated insert/remove of two
same-PID firmware-5.8.0 keys on Windows produced 4 events for 4 physical actions with zero phantom
incumbent/survivor events and exact add/remove DeviceId correlation. That run confirmed only the Windows
tier-1 **topology** (Container ID) path — a code path `ProtocolDeviceInfo` documents as absent on
macOS/Linux, where discovery degrades to the serial/PID tiers. The serial↔PID flip the E1/E2 unit tests
force is therefore not exercised on the Windows rig, and its hardware confirmation on **macOS/Linux is
still required** (and matters more, being the degraded path where a phantom incumbent event is most
plausible). Until then that flip absorption stays unit-pinned only. E1/E2 keep "covered" status on the
strength of the deterministic repository pins; the outstanding macOS/Linux runs are hardware corroboration,
not the sole coverage.

## Completed strengthening — two-key long-operation liveness

This was not a merge blocker for the ownership fix, but two allow-listed firmware-5.7.0+ YubiKeys are
now available and the stronger liveness test is implemented and executed:

`Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey`

The test resets and authenticates PIV on both keys, provisions PIN-gated EccP256 signing keys, starts
RSA-4096 generation on key A, and verifies after 500 ms that generation remains in flight. The key-B
signature must complete within four seconds and before key A finishes. The test then drains and validates
key A, repeats with the roles reversed, and then attempts both PIV resets while preserving any primary
test failure.

This strengthens A6 from repeated parallel correctness (10 EccP256 signatures per key) to liveness
while another physical card is occupied by a tens-of-seconds on-card operation. The current post-review
macOS class run passed 3/3 with firmware-5.8.0 serials 103 and 125; RSA liveness took 3 minutes 20 seconds.
Both directions met the four-second bound while the counterpart RSA task remained incomplete; RSA-4096
was then drained and validated, and both PIV applications were reset independently.
