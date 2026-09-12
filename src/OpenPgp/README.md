# OpenPGP

`Yubico.YubiKit.OpenPgp` provides key management and cryptographic operations for the YubiKey OpenPGP card.

## User-presence notifications

Supply `SessionCreationOptions.UserPresencePrompt` when creating an `OpenPgpSession` to receive advisory
notifications before signing, decryption, authentication, and attestation APDUs. The session reads the relevant
key's UIF from cached application-related data when present: `On`/`Fixed` produce `PolicyRequires`, `Cached`/`CachedFixed` produce
`PolicyMayRequire`, and `Off` is silent. Contexts use application `OpenPGP` and the `KeyRef` name as scope;
attestation uses `KeyRef.Att` because the attestation key performs that operation.

If the per-key UIF is absent from cached application-related data, the operation performs one direct GET DATA.
`SetUifAsync` updates the session's cached value after a successful write. If a supported UIF read fails for a reason
other than cancellation, the operation continues with `PolicyMayRequire`; firmware without UIF support remains
silent. Resolution reports `Completed`, `Cancelled`, or `Failed`. No OpenPGP status word is currently verified as
a distinct user-presence timeout, so `TimedOut` is not inferred.
