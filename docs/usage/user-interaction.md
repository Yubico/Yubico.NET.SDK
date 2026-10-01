# User interaction

YubiKit separates secret acquisition from physical user-presence notifications. Both are
SDK-to-application callbacks, but they have different contracts:

- `ICredentialPrompt` obtains secret bytes such as a PIN. It returns an exactly sized owned buffer,
  and the consuming component owns verification and retry policy. `WebAuthnClient` configures it through
  `WebAuthnClientOptions.CredentialPrompt`. PIV supports it through `SessionCreationOptions.CredentialPrompt`
  for signing and key generation; non-adopting applet factories reject this option instead of ignoring it.
  A low-level FIDO2 session does not adopt `SessionCreationOptions.CredentialPrompt`; use
  `WebAuthnClientOptions.CredentialPrompt` for WebAuthn ceremonies.
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

For PIV firmware 5.3 or newer, an optional credential provider can supply an encoded PIN when a
slot's policy requires verification for `SignOrDecryptAsync`, or raw management-key bytes when
`GenerateKeyAsync` needs authentication. The provider must decode a hex-entered management key itself;
the SDK accepts its raw 16-, 24-, or 32-byte value. Set `MaxCredentialPromptAttempts` (default 3,
positive) to bound fresh requests. The session zeroes and disposes each returned owner, including
rejected or late-arriving buffers. A `null` return throws `CredentialPromptDeclinedException`, not a
cancellation or device rejection. Direct calls to `VerifyPinAsync` and `AuthenticateAsync` remain
single attempts; other PIV operations do not prompt. Older firmware keeps the existing signing flow.

`ConsoleUserPresencePrompt` is the reference terminal implementation. It writes `Touch your
YubiKey.` immediately for certain requests and debounces uncertain requests for about 300 ms so a
fast operation does not flash a speculative prompt. It does not print `UserPresenceContext.Scope`,
which may contain a relying-party identifier, credential label, or key slot. Inside a
`UserPresenceIntent` scope it writes `Touch your YubiKey to {intent}.` instead.

## Request and resolution contract

For each notification, the SDK calls `OnUserPresenceRequestedAsync` before or while the device
operation waits. Only after that callback completes successfully does the SDK pair it with
`OnUserPresenceResolvedAsync` when the request ends. If the request callback throws, the SDK performs
any transport-required cancellation, drain, or reset and propagates that exception without a resolution.
Resolution reports `Completed`, `Cancelled`, `TimedOut`, or `Failed`; it does not prove whether or when
a touch occurred. The same `UserPresenceContext` instance correlates the two calls, and the context
uses reference equality so ordinary dictionaries cannot conflate equal-valued concurrent requests.
Its generic string formatting omits `Scope`, but caller-created contexts can contain arbitrary values
in the other fields; do not assume formatting is always safe to log.

`UserPresenceContext.Operation` identifies the SDK operation awaiting presence, and `Application`
identifies the applet. SDK-created contexts set both, using `UserPresenceOperations` and
`UserPresenceApplications`; `Operation` is optional for caller-created contexts. Operations are grouped by
applet, and each value is unique across applets (`"Piv.Decrypt"` and `"OpenPgp.Decrypt"` differ), so a
prompt can switch on `Operation` alone. The values are stable identifiers, not localized display text.
They name the SDK method without `Async`, so some, such as `Piv.SignOrDecrypt`, are not single
user-facing verbs. The application knows the user's intent (why an action is being performed); the SDK
does not.

| Group | Operation constants |
|---|---|
| `UserPresenceOperations.Fido2` | `Selection`, `Reset`, `MakeCredential`, `GetAssertion` |
| `UserPresenceOperations.Piv` | `SignOrDecrypt`, `Decrypt`, `CalculateSecret` |
| `UserPresenceOperations.OpenPgp` | `Sign`, `Decrypt`, `Authenticate`, `AttestKey` |
| `UserPresenceOperations.Oath` | `Calculate`, `CalculateCode` |
| `UserPresenceOperations.YubiOtp` | `CalculateHmacSha1`, `CalculateYubicoOtp` |
| `UserPresenceOperations.YubiHsmAuth` | `CalculateSessionKeysSymmetric`, `CalculateSessionKeysAsymmetric` |

This is an open set; future SDK versions can add identifiers, so keep a default branch. Use
`Application` for applet-wide fallback wording. For example, an application can supply its own wording:

```csharp
using Yubico.YubiKit.Core.Credentials;

string message = context.Operation switch
{
    UserPresenceOperations.Fido2.MakeCredential => "Touch your YubiKey to create a passkey.",
    UserPresenceOperations.Fido2.GetAssertion => "Touch your YubiKey to sign in.",
    UserPresenceOperations.Piv.SignOrDecrypt => "Touch your YubiKey for the PIV key operation.",
    _ when context.Application == UserPresenceApplications.Oath => "Touch your YubiKey to generate a code.",
    _ => "Touch your YubiKey to continue."
};
```

### Adding the application's intent

`Operation` says what the key is doing; only the application knows why the user is doing it, such as
approving a payment. Wrap the SDK call in `UserPresenceIntent.BeginScope`, and every notification that
call raises carries the text in `UserPresenceContext.Intent`. No per-call prompt state is needed, and the
same prompt instance stays registered for the whole session:

```csharp
using (UserPresenceIntent.BeginScope("approve the transfer of 500 EUR"))
{
    await client.GetAssertionAsync(options, cancellationToken);
}
```

```csharp
public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
{
    string operation = context.Operation switch
    {
        UserPresenceOperations.Fido2.GetAssertion => "sign-in",
        UserPresenceOperations.Fido2.MakeCredential => "passkey registration",
        _ => "key operation"
    };

    // "Touch your YubiKey to approve the transfer of 500 EUR (sign-in)."
    Console.WriteLine(context.Intent is { } intent
        ? $"Touch your YubiKey to {intent} ({operation})."
        : $"Touch your YubiKey to confirm the {operation}.");
    return default;
}
```

- The SDK copies the intent into the context when the operation starts, so it does not matter which layer
  or thread later raises the notification. Changing or ending the scope during the operation does not
  change a notification already in progress.
- Scopes follow the async flow, like `Activity.Current`. Concurrent operations in other flows keep their own
  intent, and work started inside the scope, including `Task.Run`, inherits it.
- Always dispose the scope, normally with `using`. A scope that is never disposed stays active for the rest
  of the async flow that opened it. In synchronous code or top-level statements, every later operation then
  reports that intent. Inside an `async` method, the scope ends when the method returns.
- Scopes nest, and the innermost undisposed scope replaces outer ones rather than being combined with them.
  A helper that opens its own scope therefore overrides the caller's wording until the helper's scope is
  disposed. Disposing a scope removes only that scope, even out of order or more than once, so a finished
  scope is never reported again.
- Work started inside a scope that begins its SDK operation after the scope is disposed, such as a
  fire-and-forget task, gets the next outer undisposed scope, or no intent.
- Open the scope in the method that awaits the SDK call, or in a synchronous helper. Do not open it inside
  an `async` helper and return it: the scope object is still undisposed, but the intent does not flow back
  to the caller, so the caller's operation gets no intent.
- If your prompt describes the operation, show the intent next to it rather than instead of it. One
  application call can issue several device operations, and the operation lets the user notice a touch
  that does not match what they expected.
- Write the intent as a lowercase verb phrase, such as `"approve the transfer"`, so it reads naturally after
  "Touch your YubiKey to", which is how `ConsoleUserPresencePrompt` shows it.
- `Intent` is application-supplied display text. The SDK never logs it, and `ToString` omits it.

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
`IUserPresencePrompt` reference for its lifetime, and PIV retains `ICredentialPrompt` the same way.
The caller owns those services and must keep them alive
and dispose it, if applicable, only after all sessions using it have ended. The SDK does not dispose
it.

No-hardware tests can verify callback ordering, cancellation, correlation, and debounced user
interface behavior. Live hardware timing verification is separate and requires a human to perform
the physical touch; it is not implied by passing no-hardware behavior tests.
