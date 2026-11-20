# User interaction

YubiKit separates secret acquisition from physical user-presence notifications. Both are
SDK-to-application callbacks, but they have different contracts:

- `ICredentialPrompt` obtains secret bytes such as a PIN. It returns an exactly sized owned buffer,
  and the consuming component owns verification and retry policy. It is currently used by
  `WebAuthnClient` through `WebAuthnClientOptions.CredentialPrompt`; lower-level applet methods still
  take credentials directly unless their documentation says otherwise.
- `IUserPresencePrompt` reports that an operation requires or may require a physical touch. It never
  supplies a credential and does not report that the user touched the device. Configure it once in
  `SessionCreationOptions.UserPresencePrompt` for participating applet sessions and one-shot operations.
  Management and Security Domain paths remain silent; accepting shared session options does not imply
  that an applet retains or uses the prompt.

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
which may contain a relying-party identifier, credential label, or key slot. Inside a
`UserPresenceIntent` scope it writes `Touch your YubiKey to {intent}.` instead.

## Request and resolution contract

### Acquiring credentials

Keep acquisition and verification in separate scopes. `ConsoleCredentialReader` is a synchronous Core
reader, while `ICredentialPrompt` is the asynchronous SDK callback contract. A prompt adapter must
return an exactly sized owned byte buffer and translate the consuming applet's length/encoding rules;
the generic reader presets are not universal PIN policy.

Interactive Core input uses pooled UTF-16 characters and clears that buffer when reading finishes.
Its text limits count UTF-16 code units before encoding, rather than Unicode code points or encoded
bytes. Disallowed characters are ignored interactively; redirected text is rejected if it fails the
policy. Redirected input uses `Console.ReadLine` and therefore creates an immutable managed string
that the reader cannot zero. The separate CLI reader has different byte/Rune handling. Choose an input
path appropriate to the credential's requirements rather than assuming these paths are equivalent.

Hex mode strips spaces, colons, and hyphens and checks `ExpectedByteLength` on decoded bytes. Redirected
hex does not apply the same text-length/filter path as interactive input. Default UTF-8 conversion uses
replacement fallback; callers needing strict Unicode validation must select and validate that policy.
Allowing zero characters is also insufficient to promise a valid empty result: the current pooled
result owner requires positive allocation length.

Confirmation retains the first entry while reading the second, compares them, clears the second, and
returns the first owner only on a match. Dispose the returned owner after the complete awaited use,
including failure. This method borrows an already-open PIV session; its caller owns session disposal:

```csharp
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Piv;

static async Task ReadAndVerifyPinAsync(
    IPivSession session, ISecureCredentialReader reader, CancellationToken cancellationToken)
{
    using var credential = reader.ReadCredential(
        CredentialReaderOptions.ForPin(), cancellationToken);
    if (credential is null)
        return;

    await session.VerifyPinAsync(credential.Memory, cancellationToken);
}
```

The example's six-to-eight ASCII-digit preset is an input choice for that task, not a claim about all
PIV credential forms. Interactive cancellation is checked while waiting for an available key; queued
keystrokes and redirected `ReadLine` do not supply the same prompt cancellation responsiveness. Keep
blocking console reads off a UI dispatcher and use a genuinely asynchronous UI adapter for callbacks.

### Reporting presence

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

CTAP2 operations use the `UserPresenceOperations.Ctap2` group and values such as
`"Ctap2.MakeCredential"`. Their applet identifier remains `UserPresenceApplications.Fido2`
(value `"FIDO2"`): CTAP2 names the operation protocol, while FIDO2 names the user-facing application.
Applications migrating from an earlier alpha must update comparisons against the former
`"Fido2.Method"` operation strings; see the `ctap2-semantic-rename` migration-map entry.

| Group | Operation constants |
|---|---|
| `UserPresenceOperations.Ctap2` | `Selection`, `Reset`, `MakeCredential`, `GetAssertion` |
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
    UserPresenceOperations.Ctap2.MakeCredential => "Touch your YubiKey to create a passkey.",
    UserPresenceOperations.Ctap2.GetAssertion => "Touch your YubiKey to sign in.",
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

With an existing `WebAuthnClient`, `requestOptions` below is its
`PublicKeyCredentialRequestOptions`, not the session-creation options. Configure the underlying
CTAP2 session's `UserPresencePrompt` when creating the client or session.

```csharp
using (UserPresenceIntent.BeginScope("approve the transfer of 500 EUR"))
{
    await client.GetAssertionAsync(requestOptions, cancellationToken: cancellationToken);
}
```

```csharp
public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
{
    string operation = context.Operation switch
    {
        UserPresenceOperations.Ctap2.GetAssertion => "sign-in",
        UserPresenceOperations.Ctap2.MakeCredential => "passkey registration",
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
- `Intent` is application-supplied display text, and `ToString` omits it. Keep secrets out of it:
  an application callback can include that text in an exception that reaches a logging provider.

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
  `PolicyRequires` when cached OTP status marks the slot keyboard-touch-triggered. That indicator is not
  a read-back of challenge-response `RequireTouch`: this is the current notification heuristic, not
  established policy certainty. Unknown/non-triggered smart-card state remains silent; no notification
  does not prove that the calculation needs no touch.
- PIV, OATH, OpenPGP, and YubiHSM Auth are smart-card paths and therefore use policy evidence. PIV
  `Always`, OATH touch-required credentials, OpenPGP user-interaction flags `On`/`Fixed`, and a known
  touch-required YubiHSM Auth credential produce `PolicyRequires`.
- Cached PIV or OpenPGP policy, unavailable or unknown metadata, a YubiHSM Auth LIST failure, and an
  unknown touch value on a found YubiHSM Auth credential produce `PolicyMayRequire`. A missing YubiHSM
  Auth credential remains silent. No smart-card timeout is inferred from these notifications.

For PIV, notifications cover `SignOrDecrypt`, `Decrypt` and `CalculateSecret`. Explicit-algorithm signing
and agreement look up touch policy only when a prompt is configured; metadata-driven signing/decryption
already require slot metadata. Default/Never policy and an empty slot stay silent. A request callback
failure prevents the private-key command. A successful response is parsed before Completed resolution,
then output transfers or host RSA unpadding runs; local padding failure can therefore follow Completed.
Management authentication and biometric verification use separate ceremonies. Cancellation remains
cooperative: await terminal work and transport drain, then inspect state before retrying a mutation.
See the [PIV task guide](../../src/Piv/README.md#user-interaction) for policy and ownership details.

For OpenPGP, attestation reports scope `Att` because ATT performs the private-key operation. The session
uses cached UIF, reads the slot directly if that entry is absent, and reports `PolicyMayRequire` on a failed
lookup other than cancellation. `Off` and firmware without UIF support stay silent. Device APDU errors resolve as
`Failed`, including 6600 under the current classification. After cancellation, await operation drain and inspect state before retrying a mutation.
See the [OpenPGP guide](../../src/OpenPgp/README.md#configure-touch-and-signature-policy) for fixed-policy recovery
and the ATT reset exception.

For YubiHSM Auth, a configured prompt causes a LIST policy lookup before CALCULATE. Cancellation of that
lookup propagates; other lookup failures produce the advisory notification. With no prompt, the session
sends CALCULATE directly. See the [module guide](../../src/YubiHsm/README.md#user-interaction) for its
credential and calculation workflow.
After requesting cancellation, await the YubiHSM Auth operation's terminal result and cleanup. An
in-flight SmartCard command can continue until its response arrives and end with an APDU failure;
follow that outcome when deciding recovery rather than assuming the device has stopped.

Applications should tolerate additional applications, scopes, and notification sites in later
versions. Do not use a presence notification to authorize an operation or infer that a touch
occurred.

## Threading and lifetime

Callbacks may run on background threads and must marshal to the application's user-interface
dispatcher before touching controls. Dispatch asynchronously and return; do not synchronously wait
for the user-interface thread. Prompt callbacks must not make re-entrant calls into the session that
raised them, because the session operation is still in progress.
This includes sync or async disposal: a callback that disposes its own session can wait for the
operation that is waiting for that callback. Schedule shutdown outside the callback and await the
complete operation before disposing. The protocol's per-exchange drain does not cover every later
exchange, parse, or cache update in a public workflow.

Session factories snapshot `SessionCreationOptions`, but the session retains the
`IUserPresencePrompt` reference for its lifetime. The caller owns that service and must keep it alive
and dispose it, if applicable, only after all sessions using it have ended. The SDK does not dispose
it.

No-hardware tests can verify callback ordering, cancellation, correlation, and debounced user
interface behavior. Live hardware timing verification is separate and requires a human to perform
the physical touch; it is not implied by passing no-hardware behavior tests.
