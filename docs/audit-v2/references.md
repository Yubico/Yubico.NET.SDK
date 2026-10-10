# References

## FIDO and WebAuthn

| Spec | Version used | URL |
| --- | --- | --- |
| Client to Authenticator Protocol (CTAP) | 2.1 Proposed Standard, with errata (2022-06-21) | https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html |
| CTAP | 2.2 Proposed Standard (2025-07-14) | https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html |
| CTAP | 2.3 Proposed Standard (2026-02-26) | https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html |
| Web Authentication | Level 2, W3C Recommendation (2021-04-08) | https://www.w3.org/TR/2021/REC-webauthn-2-20210408/ |
| Web Authentication | Level 3 | https://www.w3.org/TR/webauthn-3/ |

The test keys report these versions in GetInfo: firmware 5.7.4 `U2F_V2, FIDO_2_0, FIDO_2_1_PRE, FIDO_2_1`; firmware 5.8.0 adds `FIDO_2_2`.

## Smart card, PIV, OATH, OpenPGP, GlobalPlatform

| Spec or manual | URL |
| --- | --- |
| NIST SP 800-73-5, Interfaces for PIV (Part 2) | https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73pt2-5.pdf |
| FIPS 186-5, Digital Signature Standard | https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.186-5.pdf |
| Yubico PIV extensions | https://developers.yubico.com/PIV/Introduction/Yubico_extensions.html |
| YubiKey technical manual, PIV | https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-piv.html |
| YKOATH protocol | https://developers.yubico.com/OATH/YKOATH_Protocol.html |
| Key Uri Format (otpauth) | https://github.com/google/google-authenticator/wiki/Key-Uri-Format |
| OpenPGP smart card application 3.4.1 | https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.1.pdf |
| Yubico OpenPGP attestation | https://developers.yubico.com/PGP/Attestation.html |
| GlobalPlatform Card Specification and amendments D (SCP03) and F (SCP11) | https://globalplatform.org/specs-library/ |
| Microsoft `SCardDisconnect` (dispositions) | https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scarddisconnect |
| Microsoft `SCardConnect` (share modes) | https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scardconnecta |

## RFCs

| RFC | Topic | URL |
| --- | --- | --- |
| 5869 | HKDF | https://www.rfc-editor.org/rfc/rfc5869 |
| 5915 | EC private key structure | https://www.rfc-editor.org/rfc/rfc5915 |
| 6238 | TOTP | https://www.rfc-editor.org/rfc/rfc6238 |
| 8032 | EdDSA (Ed25519) | https://www.rfc-editor.org/rfc/rfc8032 |
| 9580 | OpenPGP (S2K, §3.7.1.3) | https://www.rfc-editor.org/rfc/rfc9580 |

## Canonical Python implementation (authority for "what correct looks like")

| Project | Pinned commit | Covers |
| --- | --- | --- |
| [Yubico/python-fido2](https://github.com/Yubico/python-fido2/tree/5bc9d3a1c8c34a3c4ca408366e630b620db47faa) | `5bc9d3a` (main, 2026-04-16; latest release 2.2.0) | CTAP2, WebAuthn client, extensions, large blobs |
| [Yubico/yubikey-manager](https://github.com/Yubico/yubikey-manager/tree/4ca60f706af930459138d8dc0f0f953480e1c7a4) | `4ca60f7` (5.9.2) | `yubikit/` library: PIV, OATH, OpenPGP, YubiOTP, Security Domain, Management, SCP; `ykman/` CLI and PC/SC handling |

## Sibling SDKs (context only, never used as proof)

| Project | Pinned commit |
| --- | --- |
| [Yubico/yubikit-android](https://github.com/Yubico/yubikit-android/tree/f46268563437ac52910001222a77741d229b9b99) | `f462685` |
| [Yubico/yubikit-swift](https://github.com/Yubico/yubikit-swift/tree/8cd5583a489a2f7ab7a1c7669518e68b26b6e475) | `8cd5583` |
| Legacy .NET SDK v1 ([Yubico/Yubico.NET.SDK `develop`](https://github.com/Yubico/Yubico.NET.SDK/tree/941874e91a77616f5d7063f2952a0291c7e7c8f8)) | `941874e` |

## GitHub Actions hardening (for #19)

- GitHub, "Security hardening for GitHub Actions": https://docs.github.com/en/actions/security-for-github-actions/security-guides/security-hardening-for-github-actions
- OpenSSF Scorecard checks (Pinned-Dependencies, Token-Permissions): https://github.com/ossf/scorecard/blob/main/docs/checks.md
