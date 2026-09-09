## Human interaction — six SDK answers

| Concern | .NET v1 | .NET v2 today | Swift 1.4 | Android | Rust / Python |
|---|---|---|---|---|---|
| Cancel | `SignalUserCancel` | `CancellationToken`; `CTAPHID_CANCEL` | cancel closure in status stream | `CommandState.cancel()` | callback / `Event` |
| Touch | `TouchRequest` | predictive PIV + YubiHSM; **no WebAuthn signal** | `.waitingForUser` from keepalive | keepalive status callback | touch callback from `UPNEEDED` |
| Secret input | `KeyCollector` / `KeyEntryData` | async owned bytes via `ICredentialPrompt` | `Authorization` closure / `String` | caller-provided `char[]` | `Ctap2Pin` / `str` |
| Long PIV work | blocking | `GenerateKeyAsync` | `generateKey` is async | blocking | blocking |

> **Audience question:** exact “waiting for touch” event, or speculative “Touch your YubiKey” prompt?

**Constraints, not preferences**

| YubiKey | Specification | Platform / dependency |
|---|---|---|
| CTAP `UPNEEDED` is real-time. PIV and YubiHSM callbacks fire before an operation; cached PIV touch and pre-5.3 metadata make timing unknowable. | User presence and user verification are distinct. CTAP defines `CTAPHID_CANCEL` while keepalives report processing or a presence wait. | Stay cross-platform and UI-agnostic; do not prescribe an app's UI thread or executor. Every new dependency must pass the Native AOT checks. |

<!-- Anchors: .NET v1@f57aa2d6 Fido2Session.cs:104, MakeCredential.cs:168-173,
     KeyEntryData.cs:33,202, PivSession.KeyPairs.cs:48,162;
     .NET v2 src/Core/src/Credentials/ICredentialPrompt.cs:20-67,
     src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs:232-270,
     src/Core/src/Utilities/ExchangeGuard.cs:17-30,
     src/Piv/src/PivSession.cs:80-100,452-463,
     src/YubiHsm/src/HsmAuthSession.cs:892-933, src/WebAuthn/CLAUDE.md "No Progress";
     swift@c76ae973 WebAuthn.swift:37-61, Authorization.swift:23-48,111,
     PIVSession.swift:255-262; android@f462685 CommandState.java:24-65,
     FidoProtocol.java:122,150, Ctap2Client.java:275, PivSession.java:1098;
     rust@90940e9 webauthn/client.rs:74-78, ctap2/client_pin.rs:40-69,
     piv.rs:2019; python-fido2@5bc9d3a client/__init__.py:243-265,359-363,
     hid/__init__.py:205-231; .NET platform/AOT Directory.Build.targets:17-23,
     src/Core/src/Native/SdkPlatformInfo.cs:43-53. -->
