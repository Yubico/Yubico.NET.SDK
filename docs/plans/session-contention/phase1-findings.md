# Phase 1 findings — session contention

Hardware experiments against key `ykphysical:103` (firmware 5.8.0, PID 0x0407, macOS).
Each experiment recorded its prediction before running. Harness:
`/var/folders/.../opencode/contention-exp/` (`dotnet run -c Release -- <1|2|3|4>`).

## Results

| # | Experiment | Prediction | Result | Repeats |
|---|---|---|---|---|
| 1 | PIV session + verified PIN, then `GetDeviceInfoAsync` | sign fails | **CONFIRMED** — `SW=0x6D00` | 4/4 |
| 2 | PIV session + verified PIN, then an OATH session | sign fails | **CONFIRMED** — `SW=0x6D00` | 1/1 |
| 3 | PIV session + verified PIN, then a **second PIV** session | unknown | **SAFE** — sign succeeds | 4/4 |
| 4 | Management over HID while PIV holds CCID | safe | **SAFE** — both HID transports, PIV survives | 1/1 |

## What the evidence changes

### 1. The failure is worse than "PIN is destroyed"

The status word is `0x6D00` — *instruction not supported* — not `0x6982` *security status not
satisfied*. The PIV applet is not merely deauthenticated, it is **entirely deselected**: the card
no longer recognises PIV instructions. Any error message or documentation describing this as
"losing the verified PIN" understates it.

`GetDeviceInfoAsync` itself returns **OK**. Nothing at the call site indicates that anything was
disturbed. The damage is only observable on the *next* operation of the victim session.

### 2. The problem is BROAD, not narrow

This was the open question from the first consultation. Experiment 2 settles it: **any** second
applet session on the CCID interface destroys the first, not just `GetDeviceInfoAsync`. OATH does
it too, and by the same mechanism.

Consequence: a policy that only teaches `GetDeviceInfoAsync` to avoid a held CCID fixes the
**common** case — the SDK stepping on its own session through internal convenience code — but does
not fix the general case of two applet sessions. Both are needed; the policy is not sufficient
alone.

### 3. Same-applet nesting is SAFE — this overturns the planning assumption

Fable's open question was whether to key the lease by `(interface, applet)` or make it simply
exclusive per interface, and it suspected exclusive-per-interface would be simpler and that
"nobody needs nesting."

**The hardware says otherwise.** A second PIV session opened while the first holds a verified PIN
does *not* disturb it — the subsequent sign succeeds, 4/4. Re-selecting the **same** applet
preserves the security state on this firmware.

So an exclusive-per-interface lease would forbid something the device demonstrably supports, and
would break the nesting case for no safety benefit. **The lease must be applet-keyed.**

This is the clearest example in this effort of why the experiments came before the fix design: the
simpler-looking option was the wrong one, and only hardware could say so.

### 4. HID is a genuinely safe alternate route

Management answers correctly over **both** HID OTP and HID FIDO while a PIV session holds CCID, and
the PIV session survives untouched. The transport-preference policy is therefore not merely
plausible — it is hardware-validated. `ManagementTransportOrder` already lists both HID transports
after SmartCard, so the route exists and simply is not chosen.

Cost note from the Phase 0 baseline: a Management session over HID OTP costs roughly **25x** one
over SmartCard (253 ms vs 11 ms p50). Preferring HID when CCID is held is correct, but it is not
free, and the choice should be observable.

## Bearing on the fix

| Finding | Consequence for the design |
|---|---|
| `0x6D00`, silent at the call site | The failure must become loud and named; a caller cannot currently detect it |
| Broad, not narrow | Transport policy alone is insufficient; the lease itself must become honest |
| Same-applet is safe | Lease keyed by `(interface, applet)`, **not** exclusive per interface |
| HID route works | The policy's fallback target is validated, and already in the transport order |

## Register rows resolved

`A1` confirmed · `A2` confirmed · `A3` safe (documented, no fix needed) · `C2` **partially
answered**: experiment 4 shows a *session* on another interface is safe, which is strong evidence
for the discovery claim at `CompositeMetadataReader.cs:53-55`, though discovery itself was not the
actor tested.

## Not yet tested

`A4` (two Management sessions writing config across transports), `A5`, `B1`–`B5`, `D1`–`D3`,
`E1`–`E2`, `F1`. `F2`/`F3` remain platform gaps requiring Windows and Linux hardware.

All results are macOS-scoped on one firmware revision (5.8.0). The `0x6D00` behaviour is a property
of the card's applet model and is expected to be firmware-general, but that is an inference, not a
measurement.
