# #9 Ceremony timeout is ignored (YESDK-1617)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only |
| Fix group | C (real design decision): how a ceremony deadline fits the transport cancellation rules |
| Evidence | unit test; code inspection (no hardware run) |
| Since the audit | The WebAuthn (Web Authentication API) code is unchanged at `df1ec06d`. The cancellation contract under it changed. `FidoHidProtocol.cs` was reworked (516 to 551 lines), including the caller-cancel path, and `ISmartCardConnection.BeginTransactionAsync` gained a remark about cancellation. The fix must follow the current contract. |

## What the audit says

The audit says the `Timeout` options are never used to set a deadline for the ceremony. A ceremony is a WebAuthn (Web Authentication API) operation such as registration or authentication.

> "neither property is consumed or used to establish a ceremony deadline anywhere."

## What is right

- Neither `RegistrationOptions.Timeout` nor `AuthenticationOptions.Timeout` is read anywhere in `src/WebAuthn/src`. A search finds no `CancelAfter` and no linked cancellation source ([RegistrationOptions.cs lines 66-69](../../../src/WebAuthn/src/Client/Registration/RegistrationOptions.cs#L66-L69); [AuthenticationOptions.cs lines 59-66](../../../src/WebAuthn/src/Client/Authentication/AuthenticationOptions.cs#L59-L66)).
- The caller's cancellation token is passed through every step. Cancelling it ends the ceremony ([WebAuthnClient.Registration.cs lines 65-71](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L65-L71), [WebAuthnClient.Authentication.cs lines 68-74](../../../src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs#L68-L74)).
- The audit is right that a missing argument to the backend is not the defect. The audit says the timeout "can be enforced at the client layer through cancellation." The defect is that the client never starts a timer.
- Both unit repros fail, as the verification run recorded. The 1-second test guard, not the requested 50 ms timeout, cancels the call ([WebAuthnAuditReproTests.cs lines 290-336](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L290-L336)).

## What is wrong or imprecise

- Null means "no timeout" in the SDK. `AuthenticationOptions` says so ([lines 62-65](../../../src/WebAuthn/src/Client/Authentication/AuthenticationOptions.cs#L62-L65)). `RegistrationOptions` does not say what null means ([lines 66-69](../../../src/WebAuthn/src/Client/Registration/RegistrationOptions.cs#L66-L69)). WebAuthn L3 allows a client-specific default when the timeout is absent. So "no timeout" is allowed, but it must be documented for both ceremonies. See [Specification](#specification).
- The SDK does not clamp a timeout to a range. WebAuthn L3 §5.1.3 corrects an out-of-range value to the closest value in the range. The recommended range is in §15.1. Clamping is a separate decision. See [Proposed fix](#proposed-fix).
- The audit's wording "ignored" is accurate for the timeout, but the cancellation path works. A caller who cancels the token gets an `OperationCanceledException`.

## Why it matters

- Who is affected: an app that sets `Timeout` to bound a ceremony. Today the call runs until the authenticator answers or the caller cancels. If the user never touches the key, the call stays open.
- The default is `null`, so there is no deadline unless the app sets one or cancels the token.
- The browser behaviour that apps expect is a `NotAllowedError` when the deadline passes. WebAuthn L3 §5.1.3 throws that error after the loop, when no credential was created.
- Hardware: not run. The unit tests use a fake backend.
- Related observation (code reading only; not run on hardware). If a deadline or a cancel is observed during a keep-alive, after the authenticator has already created a credential, the caller gets `OperationCanceledException` and never learns the credential ID. The HID (human interface device) protocol layer drains the terminal response, then throws even when that response is a CTAP (Client to Authenticator Protocol) success, and discards it ([FidoHidProtocol.cs lines 301-302 and 309-310 at `df1ec06d`](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs#L301-L310)). For a discoverable credential, this leaves an orphaned credential on the device, one that the relying party never received. A user-presence callback that fails during a keep-alive also discards the response, and the caller gets the callback's exception instead ([line 300](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs#L300)).
- Precondition and design (code reading only). The discard needs at least one keep-alive to be read after the cancellation, because only then is `CTAPHID_CANCEL` sent. If the terminal response arrives first, the success is returned. The drain is deliberate, so that the next exchange does not read leftover frames. The raced-answer test expects `OperationCanceledException` ([FidoHidProtocolTests.cs lines 694-741 at `df1ec06d`](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs#L694-L741)), but its payload is filler (first byte 0x5A), so no test pins the success case. Per the connection ownership document, PC/SC (Personal Computer/Smart Card) does not discard a completed answer. An admitted exchange returns its response ([lines 133-135](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/docs/architecture/connection-ownership-and-contention.md#L133-L135)).

## Specification

Quotes, verbatim. Each one is followed by its source. CTAP is the Client to Authenticator Protocol. HID is the human interface device transport that CTAP uses over USB. The two WebAuthn editions are Level 2 (L2) and Level 3 (L3).

- WebAuthn L3 §5.1.3 ([spec](https://www.w3.org/TR/webauthn-3/)): "If pkOptions.timeout is present, check if its value lies within a reasonable range as defined by the client and if not, correct it to the closest value lying within that range. Set a timer lifetimeTimer to this adjusted value."
- WebAuthn L3 §5.1.3: "If pkOptions.timeout is not present, then set lifetimeTimer to a client-specific default."
- WebAuthn L3 §5.1.4: the same sentence applies to `get()`.
- WebAuthn L3 §5.1.3, timer expiry: "If lifetimeTimer expires, For each authenticator in issuedRequests invoke the authenticatorCancel operation on authenticator and remove authenticator from issuedRequests."
- WebAuthn L3 §5.1.3, after the loop: "Throw a "NotAllowedError" DOMException."
- WebAuthn L3 §15.1: "Recommended range: 300000 milliseconds to 600000 milliseconds." and "Recommended default value: 300000 milliseconds (5 minutes)."
- CTAP 2.3 §11.2.5.3 (HID transport): "If an application wishes to abort a command after the request has been fully sent, e.g. while an authenticator is waiting for user presence, the application MAY do this by sending a CTAPHID_CANCEL command."
- CTAP 2.3 §11.2.9.1.5 (CTAPHID_CANCEL): "The CTAPHID_CANCEL command MAY be sent by the client during ongoing processing of a CTAPHID_CBOR request."
- CTAP 2.3 §11.2.9.1.7 (CTAPHID_KEEPALIVE): "It SHOULD be sent at least every 100ms and whenever the status changes." This is the authenticator's keep-alive, not the client's.

Version table:

| Clause | WebAuthn L2 | WebAuthn L3 | Notes |
| --- | --- | --- | --- |
| Client sets a timer from `timeout` (§5.1.3, §5.1.4) | Yes | Yes | Same rule |
| Client-specific default when `timeout` is absent | Yes | Yes | Allowed. The SDK uses no timeout. |
| Recommended range and default | §5.1.3: 30 s to 180 s for `discouraged` (default 120 s); 30 s to 600 s for `required` or `preferred` (default 300 s) | §15.1: 300 s to 600 s, default 300 s | The two levels differ |
| Cancellation signal on the HID transport | CTAPHID_CANCEL (CTAP 2.1, 2.2, and 2.3 all define it) | Not applicable | Transport level |

CTAP 2.1, 2.2, and 2.3 all define CTAPHID_CANCEL and the keep-alive interval of 100 ms, as a SHOULD.

## Canonical Python reference

Python is correct here. The deadline exists. Python has no default timeout and no range.

- `Fido2Client.make_credential` starts a timer when `options.timeout` is set. The timer sets the operation's event. The `finally` block cancels the timer ([fido2/client/__init__.py lines 1117-1124 and 1141-1142](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L1117-L1124)). `get_assertion` does the same ([lines 1155-1160 and 1178-1179](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L1155-L1160)).

```python
if options.timeout:
    timer = Timer(options.timeout / 1000, event.set)
    timer.daemon = True
    timer.start()
```

- The HID layer checks the event on each read. It sends CTAPHID_CANCEL when the event is set ([fido2/hid/__init__.py lines 204-208](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/hid/__init__.py#L204-L208)). So the Python deadline is also a signal. It is not a hard stop.

## Sibling SDKs (context only)

Not checked for this finding.

## Reproduction

- Unit: [WebAuthnAuditReproTests.YESDK1617_RegistrationTimeoutCancelsPendingAuthenticator](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L290-L312). A fake backend holds the registration until its token is cancelled. The timeout is 50 ms, and a 1-second guard prevents a hang. The test asserts that the deadline, not the guard, cancels. Result in the verification run: fails.
- Unit: [WebAuthnAuditReproTests.YESDK1617_AuthenticationTimeoutCancelsPendingAuthenticator](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L314-L336). The same check for getAssertion. Result: fails.
- Hardware: not run. A hardware check is proposed below.

## Proposed fix

The fix must fit the current transport contract, which is described below. The mechanism choice is the decision.

- Options considered:
  1. Linked deadline token (recommended). At ceremony entry, after validation, create a linked cancellation source with the `Timeout` value when it is set. Pass the linked token to every backend call, the PIN (personal identification number) prompt, and the exclude-list probe. Map deadline expiry to a `NotAllowed` error. Keep caller cancellation as `OperationCanceledException`. Dispose the source when the ceremony ends. Pros: matches the module rule that cancelling the token abandons a ceremony ([src/WebAuthn/CLAUDE.md line 235](../../../src/WebAuthn/CLAUDE.md#L235)). Matches the spec's timer step and its `NotAllowedError`. Small. Cons: the deadline is best effort, because the transport observes the token only at certain points. See the contract notes below.
  2. Wrap the public call in `Task.WaitAsync(deadline)` and return early. Not acceptable. The admitted exchange keeps running, and the protocol guard stays claimed. A call made next on the same session is refused with `InvalidOperationException` ([connection-ownership-and-contention.md line 121](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/docs/architecture/connection-ownership-and-contention.md#L121)). Disposal waits for the admitted exchange to drain ([lines 143-145](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/docs/architecture/connection-ownership-and-contention.md#L143-L145)).
  3. Keep the property advisory. Document it as not enforced, or mark it obsolete. No behaviour change. But a public option does nothing, and the spec's timer step stays unimplemented.
- Transport contract that the fix must follow (Core, at `df1ec06d`):
  - A caller token is checked at entry only for the exchange guard. Once an exchange is admitted, its constituent transmits run with `CancellationToken.None` ([src/Core/CLAUDE.md line 543](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/CLAUDE.md#L543)). The FIDO (Fast Identity Online) HID path is the exception. It reads the caller token as a signal during keep-alives, as described below.
  - PC/SC (Personal Computer/Smart Card) and SCP (Secure Channel Protocol) exchanges observe the caller token only at admission. After that, the exchange runs to its answer ([connection-ownership-and-contention.md lines 130-135](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/docs/architecture/connection-ownership-and-contention.md#L130-L135)). FIDO sessions can run over a SmartCard connection ([FidoSession.cs line 157](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Fido2/src/FidoSession.cs#L157)), so this applies to them too.
  - FIDO HID exchanges read the caller token during keep-alive packets. When it is cancelled during a keep-alive, the protocol sends CTAPHID_CANCEL. It then reads and validates the terminal response, and only then reports cancellation ([FidoHidProtocol.cs lines 265-272, 298-302, and 330-374](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs#L265-L272)). The authenticator SHOULD send keep-alives at least every 100 ms (CTAP 2.3 §11.2.9.1.7). Keep-alives normally provide an opportunity to send cancellation within approximately 100 ms. Returning to the caller additionally requires a valid terminal response to be drained; the keep-alive interval does not bound that time.
  - Neither signal rolls back work already sent ([connection-ownership-and-contention.md lines 139-141](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/docs/architecture/connection-ownership-and-contention.md#L139-L141)).
  - A FIDO HID disposal that began during an active exchange should be treated as a failed partial exchange. Dispose and reopen the connection ([connection-ownership-and-contention.md line 160](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/docs/architecture/connection-ownership-and-contention.md#L160)).
  - The PIN prompt (personal identification number) must honour the token. A late PIN result is zeroed after cancellation ([src/WebAuthn/CLAUDE.md lines 176-194](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/WebAuthn/CLAUDE.md#L176-L194)). The module already waits on the prompt with the caller's token ([WebAuthnClient.PinUvAuth.cs line 148](../../../src/WebAuthn/src/Client/WebAuthnClient.PinUvAuth.cs#L148)).
  - `MatchedCredential.SelectAsync` waits on the response factory with the caller's token. The factory itself runs to completion ([MatchedCredential.cs lines 83-90](../../../src/WebAuthn/src/Client/Authentication/MatchedCredential.cs#L83-L90)).
- Decisions separate from the mechanism:
  - Null means "no deadline" (current, and python-fido2) or the L3 default of 300 s? Recommended: keep "no deadline" and document the difference from L3 §15.1 for both ceremonies.
  - Clamp values to the L3 range of 300 s to 600 s? Recommended: not in this change. The repro uses 50 ms, and a clamp would change its meaning.
- API impact: behavioural. No signature change. A deadline now surfaces as `WebAuthnClientError` with `NotAllowed` instead of `OperationCanceledException`. The repro tests expect the old type.
- Proving test: the two `YESDK1617` unit tests, changed to expect `NotAllowed` for deadline expiry. Add a test that caller cancellation still raises `OperationCanceledException`.
- Hardware check (proposed, not run): with a 5-second timeout and no touch, measure how long the call takes to return. Then run a second call on the same session. This shows the drain behaviour that the transport contract describes.
- Depends on / interacts with:
  - [#8](08-toporigin-without-crossorigin.md). Validation runs first, so the deadline starts after validation.
  - The PIN prompt and the `ICredentialPrompt` contract.
  - Deferred selection (`MatchedCredential.SelectAsync`).
  - The FIDO HID keep-alive path, which changed at `df1ec06d`.
  - Success after cancel (see Why it matters). The timeout must not turn a completed success into a cancellation. As the code stands, a deadline observed during a keep-alive does exactly that, and the WebAuthn layer never sees the discarded response. The change therefore belongs in the HID protocol layer, and the timeout design depends on it (open question 4).
- Open questions for the maintainer:
  1. Is `null` "no deadline", as now, or the L3 default? Recommended: "no deadline", documented for both ceremonies.
  2. Should the deadline cover `MatchedCredential.SelectAsync`? Recommended: yes, for the wait. The factory is not interrupted.
  3. Should the timeout be clamped to the L3 range? Recommended: not in this change.
  4. Should a CTAP success that arrives after `CTAPHID_CANCEL` be returned instead of discarded? Recommended: yes. Return the response when its first byte is `CtapStatus.Success` (0x00), the byte that the HID backend already reads ([HidBackend.cs lines 73-79](../../../src/Fido2/src/Backend/HidBackend.cs#L73-L79)), and keep `OperationCanceledException` for every other terminal response. CTAP 2.3 §11.2.9.1.5 says that a cancelled request "will reply with the error CTAP2_ERR_KEEPALIVE_CANCEL". The section does not say that the client must discard a success. The change alters the cancellation rule in the connection ownership document, which says cancellation is reported "when that response is valid" ([lines 136-137](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/docs/architecture/connection-ownership-and-contention.md#L136-L137)). The raced-answer test's docstring describes this scenario, so it needs revisiting. The alternative is to keep the discard and document it, which keeps the orphan risk. This needs a maintainer decision and new HID protocol tests.

## Check it yourself

```bash
dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1617"
```

Spec: WebAuthn L3 §5.1.3 and §5.1.4 (timer steps), §15.1 (range). CTAP 2.3 §11.2.5.3, §11.2.9.1.5, and §11.2.9.1.7. Core, at `df1ec06d`: `docs/architecture/connection-ownership-and-contention.md` lines 121 and 130-141; `src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs` lines 265-302 and 330-374. Python: `fido2/client/__init__.py` lines 1117-1124, and `fido2/hid/__init__.py` lines 204-208. For the related observation, read `FidoHidProtocol.cs` lines 293-311 and the raced-answer test in `FidoHidProtocolTests.cs` lines 694-741, both at `df1ec06d`.
