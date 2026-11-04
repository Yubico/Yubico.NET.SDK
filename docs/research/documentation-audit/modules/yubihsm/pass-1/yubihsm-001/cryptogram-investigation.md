# HSM-IMP-008 — Supplied symmetric card cryptogram yields an unsupported response length

GPT-6.1 Sol, bounded report-only follow-up,2026-11-04. The original `investigation.md` is preserved.
Ownership is limited to this file and `evidence/cryptogram-*`. No hardware operation, build/test,
formatter, Core implementation read or product edit was performed. Coordinator owns probe execution.

**Disposition:** implementation `deferred-pass-2`; current observed limitation `confirmed-for-recorded-
scope`; response-region meaning/firmware boundary `needs-evidence`; documentation qualification
`recommended-now`. Priority high: the documented cryptogram-taking path rejects a response returned
after a valid laboratory cryptogram calculation. This is separate from weaker existing test oracles
(HSM-IMP-006), and narrows the earlier HSM-IMP-007 response-validation question.

## What actually executed

Read the complete retained raw log
`validation/hardware-v1-kat-20261104-125119-840-test/01-test-AuditHsm001.Hardware.IntegrationTests.log`,
the probe body, its independent CMAC oracle and retained original-oracle excerpts. Input identities are
in `evidence/cryptogram-inputs.md`; vendor source/history captures are in `evidence/cryptogram-sources.md`.

Recorded environment: authorized serial103, USB SmartCard, Omarchy, .NET10.0.14; device metadata
`5.8.0.beta.0`, cached numeric firmware5.8.0; SDK session effective version **0.0.1**. Earlier ykman
`hsmauth info` prints5.8.0. Keep all three version readings, their actors and timestamps distinct.

Sequence, keyed to log lines:

1. Log15: the independent AES-block CMAC oracle passed all four RFC4493 vectors before device binding.
   Probe `SymmetricOracle.Derive` constructs `zero[11] || type || 00 || L[2 big-endian] || 01 || context`.
   The fixture derives S-ENC with K-ENC/type04, S-MAC with K-MAC/type06, S-RMAC with K-MAC/type07,
   and an8byte card cryptogram with **S-MAC/type00/L=0040**. Its original vendor mapping is pinned to
   python-yubihsm revision `44a5ab52e0697c67b36c6dec953db08a6b7b183a` by coordinator provenance.
2. Log23–28: session0.0.1, reset/fresh empty readback/retries8, import fixed laboratory AES keys and
   password. Omitted-card-cryptogram CALCULATE succeeded; fixture compares all three16byte keys
   constant-time and logs `three-keys=independent-match`.
3. Log29–31: one wrong-password CALCULATE produced `HsmAuthRetryException`, INS03, SW63C7, typed7.
   Probe body then asserts LIST7 before continuing. The next call's execution implies this assertion
   passed; it is source-plus-sequence evidence, not a separately printed LIST dump.
4. Probe body54–55 repeats the **same context and correct password**, this time supplying independently
   calculated8byte `fixture.Card`. Log32/38–51: the SDK raises `ArgumentException`, “CALCULATE response
   must be exactly48bytes, got56”, from SessionKeys.Parse. The all-three-key assertion at57 and LIST8
   assertion at59 are **not reached**.
5. Log35–37 records finally reset/fresh empty readback/management retries8 and verified applet-only
   cleanup. Final result: one executed test, one failure. Cleanup success is not body success.

The correct input cryptogram is valid according to the independent laboratory oracle. The log shows a
successful response passed through the SDK retry/status check into Parse; it does not retain the raw
56byte payload or explicit final SW for that successful call. Therefore the following remain unproven:
the56byte response's first48bytes match the oracle, its final8bytes are a host cryptogram, a bad card
cryptogram is rejected, and this call reset credential retries to8. No actual HSM supplied the context
or cryptogram; the probe establishes no server identity or completed HSM/connector authentication.

## Current SDK cause and vendor comparison

Symmetric CALCULATE writes a TAG_RESPONSE whenever its nullable cardCryptogram has a value. The shared
calculation path checks status/retry, then unconditionally passes **all response.Data** to
`SessionKeys.Parse`. Parse requires exactly48 and copies3×16; the finally path clears the received raw
response even when Parse fails. The failure is a local parser-contract mismatch after device processing,
not evidence that the card cryptogram was rejected by the applet.

Focused vendor results:

| Original source | Actual source behavior | Supported conclusion |
|---|---|---|
| Current Python hsmauth, path revision296cb587 (2025-09-23); main byte-identical on access | Sends nonempty supplied card_crypto, returns the full response, parser slices0:16/16:32/32:48 with no strict-length guard | Python accepts a56byte input by ignoring its suffix. This is parser behavior, not proof about our returned bytes. |
| Initial Python commit31d39f64 (2023-07-13) | Explicitly tests len(response)==56 and stores response[48:] in **card_crypto** | Vendor host code recognized56long before this run; no source-supported claim of a recent firmware introduction. |
| Removal commit47c801e2 (2023-08-01) | Removes optional card_crypto field/56branch, retains three slices | Tail exposure was removed, not an added strict48 guard. The patch gives no reason/firmware boundary or definitive host-tail label. |
| Current C ykhsmauth, tree6195c1b6 | Deliberately sends TAG_RESPONSE only for card_crypto_len>8; requires exactly3×16 response; has later comment “Ignore host crypto for now” | C avoids supplying an8byte symmetric cryptogram and rejects56. Its comment alone cannot define an accepted56format. |
| C asymmetric-support commit08937b47 (2021-10-06) | Adds the same suppression and host-crypto comment while retaining strict48 | Longstanding host behavior, not proof of a new firmware response format. |

Vendor code/history supports **known historical56handling**. It does not settle the meaning of this
actual payload. The historical field's card-crypto name and C's host-crypto comment must both be
preserved; selecting one by intuition would overstate the evidence. No versioned original applet manual
clause explaining `keys || host-cryptogram`, its exact firmware applicability, or introduction was located
in the one research round plus one follow-up. Stop at this explicit gap rather than extrapolating from
generic HSM AUTHENTICATE SESSION commands.

The vendor Python version patch (“Override version checks for0.0.1”, c910e5d2) further explains why
ykman's displayed version cannot be treated as an independent raw SELECT observation. That patch was
read; its actual mapping and Core's implementation were outside scope. This issue is observed on the
recorded development key, with production-firmware applicability still unqualified.

## Desired SDK contract — decision proposal, not current behavior

The calculation API should support documented, valid cryptogram-bearing exchanges for each supported
applet response variant, validate expected framing, preserve required returned authentication material
where the host handshake needs it, and keep secret ownership/zeroing explicit. Its docs should identify
whether the caller or applet validates each cryptogram and who completes the HSM handshake.

After the actual56byte regions are independently identified and supported variants agreed, maintainers
can choose either session-layer response decoding that passes a verified48byte key region into the
existing standalone Parse, or a separately modeled calculation result that includes needed additional
material. A blanket `length>=48` truncation or silently widening Parse is **not** the proposed fix;
unrelated lengths and malformed authentication data should retain deliberate rejection semantics.
Existing public Parse's exact48 contract and the applet's complete wire response are distinct concerns.
No runtime/API change belongs in this documentation-only follow-up.

## Truthful documentation proposed now

Suggested task-oriented limitation, scoped to where supplied-cryptogram flows are shown:

> The current calculation path accepts a48-byte key response. On the recorded development YubiKey,
> supplying a valid laboratory symmetric card cryptogram produced a56-byte response, and the SDK
> raised `ArgumentException` while parsing it. This cryptogram-bearing path remains unsupported on
> that device pending response-format handling and verification. Integrators should validate their
> complete HSM authentication flow against their target firmware before relying on this API.

Do not present omission as an authenticated workaround. The successful omission case establishes a
laboratory three-key KDF match only. An application choosing a host-side authentication design must still
verify HSM authentication material and complete the external handshake; that design has not been tested
here. Avoid claiming all48byte expectations are malformed-response rejection, all development keys
return56, or production5.8 introduced this behavior. Retain the useful parameter meaning while adding
this current support limitation.

Affected docs to reopen: symmetric calculation XML including cardCryptogram/result/exceptions; module
README connector sketch and constraints/security qualifications; SessionKeys ExpectedResponseLength/Parse
wording that calls48 the complete universal raw response; module CLAUDE calculation/ownership notes;
HsmAuthTool calculation diagnostic comments; any shared migration/current-map or comparison statement
promoting the complete cryptogram-bearing path to qualification. Generic NativeAOT link-only limits remain
unchanged. No unaffected other-module prose needs a new review.

## Minimal coordinator-owned raw-response probe proposal — not executed

Reuse the authorized dedicated key, fixed laboratory fixture, serialized SmartCard connection and
existing cleanup harness. Bypass **only the SessionKeys parser** in an isolated external probe; preserve
normal applet selection and complete APDU response/status handling. No Core implementation exploration
or product edit is needed. A raw CALCULATE still performs applet computation and is covered by the
existing YubiKey-only probe authorization; “raw-read” describes response inspection, not zero side effects.

1. Import a fresh touchfalse laboratory credential with known keys/password; verify LIST8. Use the same
   independently self-tested oracle. Run omitted-cryptogram and correct8byte-cryptogram requests with
   identical context before introducing any wrong-password attempt.
2. Capture complete response **data length and separate SW**, before parser interpretation. Retain exact
   encoded request lengths/tag order and whether tag78 was supplied. Keep raw secret-bearing bytes
   transient in owned arrays; persist comparison booleans, lengths, offsets, source hashes and status,
   not session-key payloads. Coordinator may retain a protected diagnostic capture separately if truly
   needed, following secret-storage policy.
3. Only if the actual returned data is56bytes, compare candidate regions0..16,16..32,32..48 independently
   to expected S-ENC/S-MAC/S-RMAC. Separately compare candidate48..56 with independently derived
   **S-MAC/type01/L=0040 host cryptogram** and **S-MAC/type00/L=0040 card cryptogram**. Record both
   equality booleans. Name regions only after matching actual bytes. A second distinct context is useful
   to demonstrate context binding and reduce interpretation ambiguity.
4. Read retries after the successful raw call, before any reset. If testing card-cryptogram rejection is
   needed, make one separate deliberately modified-cryptogram request with correct password; record
   exact SW/length and LIST afterward, without assuming its retry effect. Do not mix this with key-region
   identification or claim cryptographic validation solely from an OK response.
5. Zero oracle/request/response/candidate buffers in finally, drain outstanding work, and perform the
   coordinator's explicit applet reset/readback. Report the56case, content comparisons and success-reset
   observation separately from the original SDK test's failure. Keep the failed raw log immutable.

Passing that isolated probe can support a **recorded-device** byte-layout interpretation and informs
Pass2 implementation. It still supplies no normative firmware boundary or genuine HSM/connector
interoperability. Firmware/vendor confirmation and relevant supported-device regression cases remain
part of contract qualification.

## Reopening trigger and evidence status

HSM-IMP-008 reopens when a maintainer selects the response-variant/result contract and supplies actual
raw-response identification plus regression tests for accepted/rejected lengths and cryptogram cases.
Re-review every dependent48/raw-response/support statement and resource lifetime on that change.
Current state: confirmed SDK parse failure on recorded development device; omitted-cryptogram all-three-
key match and typed63C7 observed; cryptogram-response content, successful retry restoration after that
call, production-firmware applicability and live HSM authentication remain unverified. Coordinator can
continue independent omission-only lifecycle/KDF probes if useful, labeling their restricted scope;
they do not close this cryptogram-bearing support gap.
