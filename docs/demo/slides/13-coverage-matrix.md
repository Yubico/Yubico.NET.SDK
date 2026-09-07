## Applet coverage across the SDKs

| Applet | .NET v2 | ykman (py) | ykman (rust) | android | swift |
|---|:--:|:--:|:--:|:--:|:--:|
| Management | ✅ | ✅ | ✅ | ✅ | ✅ |
| PIV | ✅ | ✅ | ✅ | ✅ | ✅ |
| OATH | ✅ | ✅ | ✅ | ✅ | ✅ |
| OpenPGP | ✅ | ✅ | ✅ | ✅ | ❌ |
| FIDO2 / CTAP2 | ✅ | ➡️ | ✅ | ✅ | ✅ |
| WebAuthn | ✅ | ➡️ | ✅ | ✅ | ✅ |
| SecurityDomain | ✅ | ✅ | ✅ | ✅ | ✅ |
| YubiHSM Auth | ✅ | ✅ | ✅ | ❌ | ❌ |
| YubiOTP | ✅ | ✅ | ✅ | ✅ | ❌ |

➡️ = not implemented in-repo; delegated to **`python-fido2`**, a hard dependency
(`fido2 >=2.0,<3`). That library supplies both `Ctap2` and `Fido2Client` — and, uniquely,
`Fido2Server`, the relying-party half no other SDK here ships.

**.NET v2 and the rust experiment are the only two with all nine in one repo.**

<!-- Anchors — rust @90940e9 crates/yubikit/src/: management.rs, piv.rs, oath.rs,
     openpgp.rs, ctap2/mod.rs, webauthn/client.rs, securitydomain.rs, hsmauth.rs,
     yubiotp.rs (all verified present).
     android @f462685: SecurityDomainSession core/.../smartcard/scp/,
     WebAuthnClient fido/.../client/, YubiHSM absent settings.gradle.kts:35-39.
     swift @c76ae973 release/1.4.0: OpenPGP/YubiHSM/YubiOTP absent,
     Capability.swift:24,30,20 carry only the bits.
     python @4ca60f7: delegation ykman/diagnostics.py:207, dependency pyproject.toml:21;
     python-fido2 @5bc9d3a Ctap2 ctap2/base.py:246, Fido2Client client/__init__.py:1066,
     Fido2Server server.py:158. -->
