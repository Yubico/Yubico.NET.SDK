# OATH

`Yubico.YubiKit.Oath` manages TOTP and HOTP credentials through the YubiKey OATH application.

## User-presence notifications

Supply `SessionCreationOptions.UserPresencePrompt` when creating an `OathSession` to receive advisory
notifications for individual calculations. `CalculateAsync` and `CalculateCodeAsync` notify only when the
credential's `TouchRequired` metadata is explicitly `true`; `false` and unknown (`null`) remain silent. The
context uses application `OATH`, basis `PolicyRequires`, and the public `issuer:name` or `name` display identity.

`CalculateAllAsync` is intentionally silent: it identifies credentials that need touch and returns `null` for
their codes rather than waiting for user presence. Resolution reports `Completed`, `Cancelled`, or `Failed`.
No OATH device response is currently verified as a distinct user-presence timeout, so `TimedOut` is not inferred.
