## Human interaction — six SDK answers

| Concern | .NET v1 | .NET v2 today | Swift 1.4 | Android | Rust / Python |
|---|---|---|---|---|---|
| Cancel | `SignalUserCancel` | `CancellationToken`; `CTAPHID_CANCEL` | cancel closure in status stream | `CommandState.cancel()` | callback / `Event` |
| Touch | `TouchRequest` | one `IUserPresencePrompt`; `Basis` grades the evidence | `.waitingForUser` from keepalive | keepalive status callback | touch callback from `UPNEEDED` |
| Secret input | `KeyCollector` / `KeyEntryData` | async owned bytes via `ICredentialPrompt` | `Authorization` closure / `String` | caller-provided `char[]` | `Ctap2Pin` / `str` |
| Long PIV work | blocking | `GenerateKeyAsync` | `generateKey` is async | blocking | blocking |

> **Audience question, now answered:** exact “waiting for touch” event *or* speculative prompt? **Both** — one callback that says which.

**Constraints, not preferences**

| YubiKey | Specification | Platform / dependency |
|---|---|---|
| CTAP `UPNEEDED` and OTP HID report a live wait; smart-card applets cannot. Cached PIV touch and pre-5.3 metadata stay unknowable, so v2 labels the uncertainty. | User presence and user verification are distinct. CTAP defines `CTAPHID_CANCEL` while keepalives report processing or a presence wait. | Stay cross-platform and UI-agnostic; do not prescribe an app's UI thread or executor. Every new dependency must pass the Native AOT checks. |

<!-- Anchors: .NET v1@f57aa2d6 Fido2Session.cs:104, MakeCredential.cs:168-173,
     KeyEntryData.cs:33,202, PivSession.KeyPairs.cs:48,162;
     .NET v2 src/Core/src/Credentials/IUserPresencePrompt.cs:31-64,
     src/Core/src/Credentials/UserPresenceBasis.cs:18-42,
     src/Core/src/Credentials/ICredentialPrompt.cs:98-116,
     src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs:284-312,
     src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs:211-230,
     src/Core/src/Utilities/ExchangeGuard.cs:36-63,
     src/Piv/src/PivSession.cs:433;
     swift@c76ae973 WebAuthn.swift:37-61, Authorization.swift:23-48,111,
     PIVSession.swift:255-262; android@f462685 CommandState.java:24-65,
     FidoProtocol.java:122,150, Ctap2Client.java:275, PivSession.java:1098;
     rust@90940e9 webauthn/client.rs:74-78, ctap2/client_pin.rs:40-69,
     piv.rs:2019; python-fido2@5bc9d3a client/__init__.py:243-265,359-363,
     hid/__init__.py:205-231; .NET platform/AOT Directory.Build.targets:17-23,
     src/Core/src/Native/SdkPlatformInfo.cs:43-53. -->

---

## User presence — one prompt for every applet

Configure it once. FIDO2, WebAuthn, PIV, OATH, OpenPGP, YubiHSM Auth and YubiOTP all use it.

```csharp
await using var piv = await key.CreatePivSessionAsync(new SessionCreationOptions
{
    UserPresencePrompt = ConsoleUserPresencePrompt.Instance   // reference terminal impl
});
```

**The SDK tells you how good its evidence is — `UserPresenceContext.Basis`**

| Basis | Evidence | Present it |
|---|---|---|
| `DeviceWaiting` | the transport saw the wait: CTAP `UPNEEDED`, OTP HID | immediately |
| `PolicyRequires` | effective policy requires touch, no live wait observed | immediately |
| `PolicyMayRequire` | cached policy, missing or unknown metadata | debounce ~300 ms |

**Delta:** every peer SDK surfaces touch only on the FIDO path — PIV, OATH and OpenPGP expose touch
as *policy*, never as a notification. v2 covers the smart-card applets too, and pays for it by
admitting when it is guessing.

<!-- Anchors: src/Core/src/Credentials/IUserPresencePrompt.cs:31-64,
     UserPresenceBasis.cs:18-42, UserPresenceContext.cs:27-48,
     src/Core/src/Sessions/SessionCreationOptions.cs:61,
     src/Cli.Shared/src/Output/ConsoleUserPresencePrompt.cs:22-23,52,79,
     src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs:284-288,
     src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs:211-230,
     src/Piv/src/PivSession.cs:832-840, src/YubiHsm/src/HsmAuthSession.cs:873-905,
     src/YubiOtp/src/YubiOtpSession.cs:599-628, docs/usage/user-interaction.md;
     peers: android PivSession.java:84-90, rust yubikit/src/piv.rs:597-612,
     swift@c76ae973 PIV/PIVDataTypes.swift:22-32 and PIV/PIVSession.swift:246-267
     (policy only; StatusStream exists only under YubiKit/YubiKit/FIDO/),
     ykman yubikit/piv.py (no touch callback). -->

---

## User presence — the request / resolve pairing

- `OnUserPresenceRequestedAsync` → present the indication and **return**. Never block on the human.
- `OnUserPresenceResolvedAsync` fires **exactly once**, and only if the request callback succeeded.
- Outcome is `Completed` / `Cancelled` / `TimedOut` / `Failed`. None of them means “the user touched”.
- Resolution always gets `CancellationToken.None`, so your cleanup still runs after cancellation.
- `UserPresenceContext` compares by **reference**, and `ToString()` omits `Scope` — relying-party ids
  stay out of logs.
- Callbacks can land on any thread, and must not re-enter the session that raised them.

One internal handle owns the lifecycle, so the transport that *sees* the wait and the applet layer
that *understands* the response can each do their half without double-firing.

**Breaking:** `OnTouchRequired` is gone from PIV and YubiHSM Auth — four public members removed from
each, on the interface and on the class. Set `SessionCreationOptions.UserPresencePrompt` instead.

<!-- Anchors: src/Core/src/Credentials/IUserPresencePrompt.cs:31-64,
     UserPresenceOutcome.cs:18-30, UserPresenceContext.cs:41-48,
     UserPresenceNotification.cs:30,75-92,100-181,
     docs/usage/user-interaction.md "Request and resolution contract",
     "Threading and lifetime"; breaking change d628ce5f commit body,
     src/Piv/src/PublicAPI.Unshipped.txt, src/YubiHsm/src/PublicAPI.Unshipped.txt. -->
