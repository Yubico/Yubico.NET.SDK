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

Hardware evidence spans macOS (two same-PID firmware-5.8.0 keys, serials 103 and 125) and Linux
(firmware-5.4.3 keys 9681620 and 20260533). Windows remains unavailable. Test names below are exact
repository method names; paths are relative to the repository root.

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
| D3 | Hotplug during an open session | P3 | open | — |

## E. Device identity

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| E1 | Tier flip on inserting a second same-PID key — no phantom incumbent event | P1 | covered | Deterministic repository pin only: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceRepositoryCompositeTests.cs`: `UpdateCache_SiblingSamePidKeyArrives_IncumbentEmitsNoRemovedOrAdded`; no physical hotplug claim |
| E2 | Tier flip on sibling removal and final removal correlation | P1 | covered | Deterministic repository pins only: `YubiKeyDeviceRepositoryCompositeTests.UpdateCache_SiblingSamePidKeyRemoved_SurvivorEmitsNoRemovedOrAdded`, `UpdateCache_TierFlipThenFinalRemoval_RemovalUsesPreviouslyAddedDeviceId`; no physical hotplug claim |

## F. Platform

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| F1 | macOS physical HID FIDO double-open (`IOHIDDeviceOpen` options `0x01`) | P3 | platform-gap | Requires a human-coordinated macOS hardware double-open run; classified as a platform gap rather than a product-contract blocker |
| F2 | Platform-divergent HID sharing semantics | P3 | platform-gap | Linux hardware gates are broader: FIDO HID is shared and OTP HID is SDK-exclusive. Windows behavior remains unverified |
| F3 | Windows PC/SC sharing semantics under contention | P3 | platform-gap | Requires Windows hardware; seam-level in-process ownership is platform-independent, but native PC/SC behavior is unverified |

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
| P2 | 12 | 10 | 2 | 0 | 0 |
| P3 | 4 | 0 | 0 | 3 | 1 |
| **In scope** | **21** | **15** | **2** | **3** | **1** |

ISC-2 passes: every P1/P2 row is covered or has a documented bound and pinning test. P3 remains
explicitly non-blocking: D3 is open for a human-coordinated/fake follow-up, and F1-F3 require unavailable
platform hardware.

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
