# User interaction

YubiKit separates secret acquisition from physical user-presence notifications. Both are
SDK-to-application callbacks, but they have different contracts:

- `ICredentialPrompt` obtains secret bytes such as a PIN. It returns an exactly sized owned buffer,
  and the consuming component owns verification and retry policy. It is currently used by
  `WebAuthnClient` through `WebAuthnClientOptions.CredentialPrompt`; lower-level applet methods still
  take credentials directly unless their documentation says otherwise.
- `IUserPresencePrompt` reports that an operation requires or may require a physical touch. It never
  supplies a credential and does not report that the user touched the device. Configure it once in
  `SessionCreationOptions.UserPresencePrompt` for any applet session or one-shot operation that
  accepts session options.

```csharp
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Piv;

var options = new SessionCreationOptions
{
    UserPresencePrompt = ConsoleUserPresencePrompt.Instance
};

await using PivSession session = await yubiKey.CreatePivSessionAsync(
    options,
    cancellationToken);
```

`ConsoleUserPresencePrompt` is the reference terminal implementation. It writes `Touch your
YubiKey.` immediately for certain requests and debounces uncertain requests for about 300 ms so a
fast operation does not flash a speculative prompt. It does not print `UserPresenceContext.Scope`,
which may contain a relying-party identifier, credential label, or key slot.

## Request and resolution contract

For each notification, the SDK calls `OnUserPresenceRequestedAsync` before or while the device
operation waits. Only after that callback completes successfully does the SDK pair it with
`OnUserPresenceResolvedAsync` when the request ends. If the request callback throws, the SDK performs
any transport-required cancellation, drain, or reset and propagates that exception without a resolution.
Resolution reports `Completed`, `Cancelled`, `TimedOut`, or `Failed`; it does not prove whether or when
a touch occurred. The same `UserPresenceContext` instance correlates the two calls, and the context
uses reference equality so ordinary dictionaries cannot conflate equal-valued concurrent requests.
Its generic string formatting omits `Scope` to avoid disclosing that display context in logs.

The SDK coordinates each operation through one internal lifecycle handle. A transport can request that
handle when it observes a live wait, while the applet layer that understands the complete response resolves
it after status and integrity validation. The handle makes request and resolution exact-once even when those
responsibilities sit in different layers. Silent operations use an inert handle rather than nullable callback
state.

The request callback must present or schedule the indication and return promptly. It must not block
until the user touches the device. Resolution should cancel pending delayed presentation and dismiss
or mark stale any user interface owned by that request. Clearing a terminal line is optional and
must not erase unrelated output.

The supplied request cancellation token applies to callback work. Resolution always receives
`CancellationToken.None` so cleanup can still run. A resolution exception propagates when the device
operation otherwise succeeded. If an operation or cancellation exception is already being propagated,
the SDK logs the resolution exception and preserves the primary exception. Treat cancellation as a reason
to suppress or dismiss the indication, not as evidence that the device operation has already stopped.

## Certainty and transport behavior

`UserPresenceContext.Basis` describes what the SDK knows:

| Basis | Meaning | Recommended presentation |
|---|---|---|
| `DeviceWaiting` | The device or transport reported an in-flight wait for user presence. | Show immediately. |
| `PolicyRequires` | Effective applet or credential policy requires presence, but no in-flight wait was reported. | Show immediately. |
| `PolicyMayRequire` | Cached policy, missing metadata, or an unknown value prevents certainty. | Debounce briefly and suppress if resolved first. |

The source of that certainty differs by applet and transport:

- FIDO2, including WebAuthn ceremonies, can report `DeviceWaiting` over FIDO HID when the
  authenticator sends the user-presence-needed keep-alive. Smart-card FIDO paths use
  `PolicyRequires` for operations whose request requires user presence; they do not infer an
  in-flight smart-card timeout. Selection and reset can remain uncertain until HID reports a wait.
- YubiOTP challenge-response can report `DeviceWaiting` over OTP HID. Its smart-card path uses
  `PolicyRequires` when the slot configuration says touch is required.
- PIV, OATH, OpenPGP, and YubiHSM Auth are smart-card paths and therefore use policy evidence. PIV
  `Always`, OATH touch-required credentials, OpenPGP user-interaction flags `On`/`Fixed`, and a known
  touch-required YubiHSM Auth credential produce `PolicyRequires`.
- Cached PIV or OpenPGP policy, unavailable or unknown metadata, a YubiHSM Auth LIST failure, and an
  unknown touch value on a found YubiHSM Auth credential produce `PolicyMayRequire`. A missing YubiHSM
  Auth credential remains silent. No smart-card timeout is inferred from these notifications.

Applications should tolerate additional applications, scopes, and notification sites in later
versions. Do not use a presence notification to authorize an operation or infer that a touch
occurred.

## Threading and lifetime

Callbacks may run on background threads and must marshal to the application's user-interface
dispatcher before touching controls. Dispatch asynchronously and return; do not synchronously wait
for the user-interface thread. Prompt callbacks must not make re-entrant calls into the session that
raised them, because the session operation is still in progress.

Session factories snapshot `SessionCreationOptions`, but the session retains the
`IUserPresencePrompt` reference for its lifetime. The caller owns that service and must keep it alive
and dispose it, if applicable, only after all sessions using it have ended. The SDK does not dispose
it.

No-hardware tests can verify callback ordering, cancellation, correlation, and debounced user
interface behavior. Live hardware timing verification is separate and requires a human to perform
the physical touch; it is not implied by passing no-hardware behavior tests.
