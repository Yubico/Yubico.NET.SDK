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

Rig: macOS, two same-PID keys (103, 125), both firmware 5.8.0, PID 0x0407 (OTP+FIDO+CCID).
CCID-only and HID-only configurations are **synthesized** on key 125 via the reconfiguration
harness, which restores unconditionally.

---

## A. Session × session on one physical key

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| A1 | PIV session + `GetDeviceInfoAsync` — **the footgun** | P1 | investigating | Phase 1 exp 1 — CONFIRMED, `SW=0x6D00`, 4/4 |
| A2 | PIV + OATH concurrently on one CCID (two applets) | P2 | investigating | Phase 1 exp 2 — CONFIRMED, `SW=0x6D00` |
| A3 | PIV + PIV nested (same applet) — decides applet-keyed vs exclusive lease | P2 | **safe** | Phase 1 exp 3 — SAFE 4/4; lease must be applet-keyed |
| A4 | Management(CCID) + Management(HidOtp) via `preferredConnection`, both writing config | P2 | open | — |
| A5 | Management(CCID) + FIDO2(HidFido) — different interfaces, expected safe | P2 | open | — |
| A6 | Sessions on two **different** keys — must remain fully parallel | P1 | open | — |

## B. Transport availability

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| B1 | CCID-only key (HID disabled) — no safe fallback; must fail naming the holder | P2 | open | — |
| B2 | HID-only key (no CCID) | P2 | open | Phase 1 exp 4 shows Management answers over both HID transports |
| B3 | NFC (CCID, no HID at all) | P2 | open | — |
| B4 | `preferredConnection = SmartCard` conflicts with a held CCID — must fail loudly | P2 | open | — |
| B5 | SCP requested + CCID held — must **not** downgrade to plaintext HID | P2 | open | — |

## C. Discovery × session — already solved, must not regress

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| C1 | Discovery vs session, same interface | P1 | open | — |
| C2 | Discovery vs session, **different** interface of the same key — design claim, never hardware-verified | P2 | partial | Phase 1 exp 4 — a *session* on another interface is safe; discovery not yet the actor |

## D. Lifecycle and timing

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| D1 | Session disposed while another waits on the same interface | P2 | open | — |
| D2 | Session opened while a scan is in flight | P2 | open | — |
| D3 | Hotplug during an open session | P3 | open | — |

## E. Device identity

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| E1 | Tier flip on inserting a second same-PID key — phantom `Removed`+`Added` | P1 | open | — |
| E2 | Tier flip on removing one of two same-PID keys | P1 | open | — |

## F. Platform

| # | Case | Tier | Status | Test |
|---|---|---|---|---|
| F1 | macOS seizes HID FIDO IO reports on double-open (`IOHIDDeviceOpen` options `0x01`) | P3 | open | — |
| F2 | Windows / Linux share HID rather than seizing | P3 | platform-gap | — |
| F3 | Windows PC/SC sharing semantics under in-process contention | P3 | platform-gap | — |

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

Updated as phases complete. Empty until Phase 1 populates it.

| Tier | Total | Covered | Bounded | Platform gap | Open |
|---|---|---|---|---|---|
| P1 | 5 | 0 | 0 | 0 | 5 |
| P2 | 12 | 0 | 0 | 0 | 12 |
| P3 | 3 | 0 | 0 | 2 | 1 |
| **In scope** | **20** | **0** | **0** | **2** | **18** |

## Planned strengthening — two-key long-operation liveness

This is not a merge blocker for the ownership fix; A6's correctness/isolation requirement is covered
by `PivMultiKeyContentionTests`. Add the stronger liveness test when **two allow-listed YubiKeys with
firmware 5.7.0+** are available:

`Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey`

1. Reset and authenticate PIV on both keys; provision a PIN-gated EccP256 signing key on key B.
2. Start `GenerateKeyAsync(..., PivAlgorithm.Rsa4096)` on key A and wait 500 ms.
3. Assert RSA generation is still in flight, so the test proves genuine overlap rather than sequencing.
4. Run a PIN-gated signature on key B with a bounded deadline (4 seconds, matching the existing
   discovery-vs-RSA-4096 gate).
5. Assert key B completes within the bound and returns a valid signature, then drain and validate key
   A's RSA generation.
6. Repeat with the key roles reversed, so reader ordering cannot hide cross-key coupling.

This strengthens A6 from repeated parallel correctness (10 EccP256 signatures per key) to liveness
while another physical card is occupied by a tens-of-seconds on-card operation. RSA-4096 is required:
RSA-2048 on the current firmware-5.4.3 rig may complete too quickly to guarantee overlap.
