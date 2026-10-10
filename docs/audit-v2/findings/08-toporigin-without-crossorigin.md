# #8 topOrigin sent with crossOrigin false (YESDK-1616)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only |
| Fix group | B (small API decision): reject or omit |
| Evidence | unit test; code inspection (no hardware run) |
| Since the audit | Unchanged at current yubikit. `WebAuthnClientData.cs`, the client entry points, and the option classes are identical at the branch base and at `df1ec06d`. |

## What the audit says

The audit says the SDK writes `topOrigin` into the client data when `crossOrigin` is false. WebAuthn (Web Authentication API) allows `topOrigin` only for cross-origin calls. The SDK (software development kit) builds the client data, and nothing checks `crossOrigin` first.

> "is accepted, serialized, hashed, sent to the authenticator, and returned despite WebAuthn requiring topOrigin only when crossOrigin is true."

## What is right

- The SDK writes `topOrigin` whenever it is not null ([WebAuthnClientData.cs lines 120-125](../../../src/WebAuthn/src/Client/WebAuthnClientData.cs#L120-L125)). The check does not look at `crossOrigin`.
- The value flows from the options into the client data without change: [WebAuthnClient.Registration.cs lines 77-82](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L77-L82) and [WebAuthnClient.Authentication.cs lines 80-85](../../../src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs#L80-L85).
- WebAuthn Level 3 (L3) defines `topOrigin` only for cross-origin calls. Level 2 (L2) has no `topOrigin` member at all, so the rule comes from L3 only. See [Specification](#specification).
- Both unit repros fail, as the verification run recorded. Each one finds a `topOrigin` key in the client data.

## What is wrong or imprecise

- The XML docs are wrong in three places. The `WebAuthnClientData.Create` doc says a null `crossOrigin` omits the field ([WebAuthnClientData.cs line 64](../../../src/WebAuthn/src/Client/WebAuthnClientData.cs#L64)), but the code always writes it ([lines 116-118](../../../src/WebAuthn/src/Client/WebAuthnClientData.cs#L116-L118)). The `AuthenticationOptions.CrossOrigin` doc says the field appears only when true ([lines 69-74](../../../src/WebAuthn/src/Client/Authentication/AuthenticationOptions.cs#L69-L74)), but the code always writes it. The `topOrigin` docs say the field is included whenever it is set ([AuthenticationOptions.cs lines 76-82](../../../src/WebAuthn/src/Client/Authentication/AuthenticationOptions.cs#L76-L82)). That describes the defect as intended behaviour. The fix must correct all three.
- `"crossOrigin": false` is not a defect. WebAuthn L3 §5.8.1.1 always serializes `crossOrigin`, and writes `false` when the member is absent. The always-written field matches the spec. Only the docs are wrong.
- The repro tests expect `topOrigin` to be omitted. The recommended fix rejects the input instead, as the verification report notes. If the maintainer picks rejection, the tests must change.

## Why it matters

- Who is affected: an app that sets `TopOrigin` without `CrossOrigin=true`. The signed client data then contains a top-level origin, which the spec reserves for cross-origin calls. It also says the call is not cross-origin, because `crossOrigin` is `false`.
- The relying party's reaction depends on its own checks. This was not tested with a relying party or on hardware.
- Preconditions: a caller passes `TopOrigin`, for example an embedded page that sets the top-level origin but forgets `CrossOrigin`.

## Specification

Quotes, verbatim. Each one is followed by its source.

- WebAuthn L3 §5.8.1 ([spec](https://www.w3.org/TR/webauthn-3/)): "It is set only if the call was made from context that is not same-origin with its ancestors, i.e. if crossOrigin is true."
- WebAuthn L3 §5.8.1.1 (serialization): "If topOrigin is present:" The member is appended whenever it is present. The restriction is on setting it.
- WebAuthn L3 §5.8.1.1 (serialization): "If crossOrigin is not present, or is false:" The value `false` is written in that case.
- WebAuthn L3 change list: "New client data attribute topOrigin"

Version table:

| Clause | WebAuthn L2 | WebAuthn L3 | Notes |
| --- | --- | --- | --- |
| `topOrigin` member | Not defined (no match in the L2 text) | Yes, §5.8.1 | L3 only |
| `topOrigin` set only when `crossOrigin` is true | Not applicable | Yes, §5.8.1 | The rule this finding is about |
| `crossOrigin` always written (false when absent) | Yes, §5.8.1.1 | Yes, §5.8.1.1 | The SDK matches the spec here |
| CTAP (Client to Authenticator Protocol) clause | None | None | Client data is built by the client, not by CTAP |

## Canonical Python reference

Python has no equivalent. The Python client never sets `topOrigin`, so the contradiction cannot occur.

- `CollectedClientData.create` takes `cross_origin` and always writes `"crossOrigin"` ([fido2/webauthn.py lines 392-416](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/webauthn.py#L392-L416)). It has no `topOrigin` parameter. Extra keys can only come in through `**kwargs`, and the client does not pass any.

## Sibling SDKs (context only)

Not checked for this finding.

## Reproduction

- Unit: [WebAuthnAuditReproTests.YESDK1616_SameOriginRegistrationOmitsTopOriginFromClientData](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L251-L268). Sets `CrossOrigin=false` and a `TopOrigin`, then asserts that `topOrigin` is absent from the client data. Result in the verification run: fails.
- Unit: [WebAuthnAuditReproTests.YESDK1616_SameOriginAssertionOmitsTopOriginFromClientData](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L270-L288). The same check for getAssertion. Result: fails.
- Hardware: not applicable. The client data is built locally.

## Proposed fix

- Options considered (group B):
  1. Reject (recommended in the verification report). At option validation, throw an invalid-request error when `TopOrigin` is set and `CrossOrigin` is not `true`. Put the check in `ValidateRegistrationOptions` and `ValidateAuthenticationOptions` ([WebAuthnClient.Validation.cs lines 24-70](../../../src/WebAuthn/src/Client/WebAuthnClient.Validation.cs#L24-L70)). Pros: the caller learns about the contradiction, and no data is dropped silently. Cons: a behaviour change for callers who set `TopOrigin` without `CrossOrigin=true`.
  2. Omit. In `WebAuthnClientData.BuildJson`, write `topOrigin` only when `crossOrigin` is `true` ([WebAuthnClientData.cs lines 120-125](../../../src/WebAuthn/src/Client/WebAuthnClientData.cs#L120-L125)). Pros: no new error, and the output conforms to the spec. Cons: the caller's input is dropped silently, so the caller may believe the top-level origin was sent.
  3. Derive. Treat a non-null `TopOrigin` as `CrossOrigin = true`. Not recommended. It sets a signed flag that the caller did not state.
- Recommendation: option 1, reject. Fix the XML docs listed under "What is wrong or imprecise" in the same change.
- API impact: behavioural (options 1 and 2). No signature change.
- Proving test: the two `YESDK1616` unit tests. Under option 1, change them to expect the rejection. Under option 2, they already assert omission. Add a control: `CrossOrigin=true` with a `TopOrigin` keeps `topOrigin` in the client data.
- Depends on / interacts with: [#9](09-ceremony-timeout-ignored.md). Validation runs before the ceremony deadline starts, so a rejected request does not use up the deadline.
- Open questions for the maintainer: reject (recommended) or omit?

## Check it yourself

```bash
dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1616"
```

Spec: WebAuthn L3 §5.8.1 and §5.8.1.1. To confirm that L2 has no `topOrigin`, search the L2 text for the word.
