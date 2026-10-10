# 26b PRF enabled ignores the returned value (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (one audit row, #26; sub-item b) |
| Our verdict | Confirmed, with a correction |
| Our severity | MED |
| Root cause | SDK only |
| Fix group | A for the registration boolean. C with [#1](01-prf-not-translated-to-hmac-secret.md) for the decrypted `hmac-secret-mc` result. |
| Evidence | unit test (no hardware) |
| Since the audit | Unchanged at current yubikit (`PrfAdapter.cs`, `ExtensionPipeline.cs`, `Outputs/PrfOutput.cs`). |

## What the audit says

The audit says the registration check looks only for a `prf` entry and never reads its value. Its key claim:

> checks only whether the response contains a prf entry, then returns Enabled: true without inspecting its value.

`Enabled` is the WebAuthn (Web Authentication) registration result. It says whether the pseudo-random function (PRF) can be used with the new credential. The Client to Authenticator Protocol (CTAP) reports the underlying result as `hmac-secret`.

## What is right

- The adapter checks presence only. `PrfAdapter.ParseRegistrationOutput` looks up the `prf` key and returns `Enabled: true` without reading the value ([PrfAdapter.cs:74-84](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L74-L84); the return is at line 83). Repro [WebAuthnExtensionAuditReproTests.cs:27-35](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L27-L35) fails: `prf: false` gives `Enabled` true.
- The spec's field is a boolean named `hmac-secret`, and the adapter never reads it. A valid `hmac-secret: false` produces no PRF output at all. The adapter returns null ([PrfAdapter.cs:77-80](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L77-L80)).

## What is wrong or imprecise

- The presence-only check is as the audit describes. But the value is never parsed, so any value counts as enabled, including malformed bytes. The malformed-value handling in [ExtensionPipeline.cs:233-241](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L233-L241) never runs for this method.
- The audit does not mention the absent case. When the authenticator reports nothing, the software development kit (SDK) returns no PRF output (null). WebAuthn Level 3 says otherwise. The spec sets `enabled` to false when `hmac-secret` is not present, and says the enabled output is always present at registration.
- The WebAuthn Level 3 (L3) text places the enabled step inside the "If eval is present" branch. The §16.17.1.1 examples say the opposite: `{ prf: {} }` (no eval) returns `{ prf: { enabled: true } }`. We follow §16.17.1.1, which is the stated rule. python-fido2 also reports `enabled` without `eval`, but only when its registration processor exists (see the Python section).
- The hardware record reads `Prf.Enabled == null`. That is the absent-output path described above. See [#1](01-prf-not-translated-to-hmac-secret.md).

## Why it matters

- Who is affected: apps that enable PRF-based features after registration, based on `Enabled`.
- Today: a conforming authenticator never produces `Enabled: true` through this SDK, because the `prf` key is never returned. Apps see null.
- After the request fix in [#1](01-prf-not-translated-to-hmac-secret.md) alone, the authenticator would return `hmac-secret`, but the adapter would still look for `prf`. Registration would keep reporting null on real hardware. So this fix must ship with the request change.
- A `prf: false` reported as enabled needs an authenticator that returns the non-spec key. None is known. This is a logic error, not a live exposure.
- Precondition: PRF requested at registration.

## Specification

> Set enabled to the value of hmac-secret in the authenticator extensions output. If not present, set enabled to false. — WebAuthn L3 §10.1.4 ([prf-extension](https://www.w3.org/TR/webauthn-3/#prf-extension))

> true if, and only if, the PRF is available for use with the created credential. — WebAuthn L3 §10.1.4 (dictionary member `enabled`)

> The enabled output is always present during registration ceremonies, and never present during authentication ceremonies: — WebAuthn L3 §16.17.1.1 (examples)

> "hmac-secret": false — CTAP 2.3 §12.7 (registration response, when the authenticator could not create the CredRandom values)

Each CTAP edition in the table is a PS (Proposed Standard) document.

| Clause | CTAP 2.1 PS | CTAP 2.2 PS | CTAP 2.3 PS | Notes |
| --- | --- | --- | --- | --- |
| Registration response `hmac-secret` boolean | §12.5 | §12.7 | §12.7 | Sent only when the platform requested `hmac-secret` |
| WebAuthn `enabled` | Not in CTAP | Not in CTAP | Not in CTAP | Defined in WebAuthn L3 §10.1.4 |

## Canonical Python reference

Python reads the boolean as follows:

```python
enabled = extensions.get(HmacSecretExtension.NAME, False)
```

([extensions.py:317](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L317))

When its registration processor is created, Python reads `hmac-secret` and defaults an absent value to false. It does not report enabled for every PRF request: without hmac-secret support or a negotiated PIN/UV protocol, it returns no PRF output ([extensions.py:284-326](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L284-L326)). On the boolean itself, Python matches the spec.

## Sibling SDKs (context only)

Not checked for this finding.

## Reproduction

- Unit: [WebAuthnExtensionAuditReproTests.YESDK1634_PrFRegistrationFalseOutputIsNotEnabled](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L27-L35). Feeds `prf: false` and expects `Enabled` to be false. Result today: fails ("Actual: True").
- Hardware: not applicable to this item. The registration result on the 5.7.4 test key is described under [#1](01-prf-not-translated-to-hmac-secret.md).
- Test warning: the repro feeds the non-spec `prf` key. After the fix the key is `hmac-secret`. If the test is left as it is, the new code sees no `hmac-secret` and returns false, so the test passes for the wrong reason. Rewrite it to feed `hmac-secret`.

## Proposed fix

- Recommendation:
  1. Read `hmac-secret` as a CBOR (Concise Binary Object Representation) boolean in `PrfAdapter.ParseRegistrationOutput` ([PrfAdapter.cs:74-84](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L74-L84)). `Enabled` is true only for `true`. Absent means false.
  2. Return a `PrfRegistrationOutput` whenever PRF was requested, even when the authenticator reports nothing. Change the PRF branch in [ExtensionPipeline.cs:230-242](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L230-L242) so it no longer depends on the key being present.
  3. A non-boolean value: report disabled (`Enabled: false`). Do not report enabled, and do not throw. The verification report leaves this open. We recommend "report disabled, never enabled".
  4. Stop looking for the `prf` key.
- Options considered (group A):
  - Option A (recommended): as above. Results change only for PRF requests.
  - Option B: keep null for an absent output, and read only `hmac-secret`. Simpler, but breaks the rule that enabled is always present at registration.
- API impact (application programming interface): none. `PrfRegistrationOutput(bool Enabled)` keeps its shape ([Outputs/PrfOutput.cs:26-30](../../../src/WebAuthn/src/Extensions/Outputs/PrfOutput.cs#L26-L30)). The PRF output becomes non-null when PRF was requested.
- Proving test:
  - Rewrite `YESDK1634_PrFRegistrationFalseOutputIsNotEnabled` to feed `hmac-secret`. `false` gives `Enabled` false, and `true` gives `Enabled` true.
  - Add: an absent `hmac-secret` gives `Enabled` false, not null.
  - Add: a non-boolean `hmac-secret` gives `Enabled` false.
  - Keep a control: `prf` alone does not set `Enabled`.
- Depends on / interacts with: [#1](01-prf-not-translated-to-hmac-secret.md), the registration request. Without the request, no `hmac-secret` output comes back (CTAP 2.3 §12.7). The parse change alone can report `Enabled: false` instead of null, but cannot enable PRF on real devices. Ship it with the registration request change. The verification report notes that the registration part of #1 and 26b can ship first. Ship them together, so that a real device gives a correct result.
- Open questions for the maintainer:
  1. For a non-boolean `hmac-secret`: report disabled (our recommendation), or fail the registration?

## Check it yourself

- Unit: `dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1634_PrFRegistrationFalseOutputIsNotEnabled"`. Fails at the branch base.
- Spec: WebAuthn L3 §10.1.4 ([prf-extension](https://www.w3.org/TR/webauthn-3/#prf-extension)) and §16.17.1.1. CTAP 2.3 §12.7 ([hmac-secret](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-hmac-secret-extension)).
- Python: `fido2/ctap2/extensions.py`, lines 315-324.
