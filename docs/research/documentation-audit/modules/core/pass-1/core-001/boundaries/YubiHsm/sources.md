# Source authority and bounded qualifications

Access date for this boundary: 2026-11-12. Source identities, paths and SHA-256 values
are serialized in `source-identities.json`. Source roles below are distinct from execution evidence.
No package import, provider, connector or device command ran.

## Reused applet originals and exact applicability

| Source / revision / status | Passage / actor / strength | Applied conclusion and bound |
| --- | --- | --- |
| Yubico Technical Manual, “YubiHSM Auth Specifics”, living unnumbered vendor manual, copyright 2026 | Opening support; “YubiHSM 2 Secure Channel”; Architecture; Flowchart steps 3–11; “Using … Shell”. YubiKey, HSM, host library, provisioning operator. Descriptive product guidance, not RFC-style normative applet grammar. | 5.4.3+ CCID app; matching long-lived enrollment; separate host/HSM flow. The secure-channel section explicitly says HSM messages are not APDUs and HSM has two static keys versus SCP03's three. Symmetric-focused text does not define asymmetric receipts or CALCULATE's 56-byte tail format. Original page reread through webfetch. |
| Vendor .NET v1 manual, “YubiHSM Auth overview”, live page Edit link targets develop, no numbered revision | “Management key” and “Credential”. Applet behavior, descriptive requirements. | Eight retries, success-before-exhaustion restoration, credential deletion versus management blocking/reset. Reread original page. Its blanket immutability sentence is stale for password-change support and is excluded from 5.8 authority. |
| YubiKey Manager `yubikit/hsmauth.py`, shared pin `4ca60f706af930459138d8dc0f0f953480e1c7a4`, project 5.9.3-dev.0 | Complete 718-line host encoder/parser and complete 65-line `tests/test_hsmauth.py`, independently read in existing clean checkout; manifest hashes match. Implementation/test comparator only. | SELECT/config/SCP order; widths/tags/gates; P-256 provider decoding on public-key path; raw key slices; password string-vs-bytes distinction; fixed ASCII/Greek PBKDF2 expected pairs. Source agreement supplies no device/native/conformance proof. |
| Prior hsmauth path revision `296cb587ba15f57672356d8445393a96b41803fb`, 2025-09-23 author metadata | `SessionKeys.parse:203–220`, calculation 596–653 and challenge 695–718; full current pin has identical file hash `e3573d5580928b84818939b6eb1ae0083c7d01793589746eace80db32fe2cab3` | Existing published links remain pinned and byte-correlated, without claiming their commit date is firmware release/publication. |
| Python initial `31d39f64b73e534787382dab4675c0274616627b`, 2023-07-13; removal `47c801e2f4d4b64ac6931e39d962bdbee72c7553`, 2023-08-01 | Retained exact original parser/diff in frozen `evidence/cryptogram-sources.md` V2; host implementation history | 56-byte handling predates this audit; former field called card_crypto. Removal retained slicing. No tail meaning/introduction/firmware boundary inferred. Original-history captures are reused; this boundary did not refetch/re-execute them. |
| Vendor C tree `6195c1b687aab56308e9598a0d66941813c94ae5`; asymmetric introduction `08937b47458a9e0f30ea94a16885d7a82fb4dabb` | Frozen cryptogram source record V3, `ykhsmauth_calculate_ex:561–631` and header 97–107; host encoder/parser, corroborating implementation | Suppresses an 8-byte symmetric cryptogram; strict 48-byte parse; comment “Ignore host crypto for now”. That comment cannot independently label unretained device bytes or justify omission as authenticated workaround. |
| Shared Python version history `c910e5d25ba675ac2bfc97d35f14a4cc0314864f` and retained Core helper | Exact 0.0.1 patch versus C#'s general major-zero optimistic policy; implementation, not hardware identity | Keep SDK/direct Python 0.0.1, ykman display 5.8.0 and Management 5.8 beta observations separate. Shared context's pinned source/history is reused, not a new remote freshness check. |
| python-yubihsm prior oracle source `44a5ab52e0697c67b36c6dec953db08a6b7b183a`; focused path source `055f85fcff0a1d3c0ec779dc28cb21e2762cef3c` | Frozen probe provenance and S5 focused `_derive`, `SymmetricAuth`/`AsymmetricAuth` excerpts. Host session negotiator, corroborating implementation. | Independent laboratory key/cryptogram oracle relationship, not fresh execution or applet response specification. Preserve differing tree/path pins rather than treating them as one new downloaded artifact. |

Original URLs: <https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-yubihsm-auth.html>;
<https://docs.yubico.com/yesdk/users-manual/application-yubihsm-auth/yubihsm-auth-overview.html>.
Exact immutable vendor-code URLs and prior research attempts are retained in
`modules/yubihsm/pass-1/yubihsm-001/evidence/sources.md` and `cryptogram-sources.md`, relative to the
documentation-audit root. Their original access date is 2026-11-04, not this boundary's access date.

## Family originals reused with edition/actor separation

* **GlobalPlatform SCP03** Card Specification v2.3 Amendment D, **1.1.2 Public Release**, March 2019,
  GPC_SPE_014, retained PDF SHA256
  `973a10834f7fe8befd98da9af65393622234b80745092bd85248804c57b648dd`. Reread derivative lines 784–944,
  exact §5.6 and §6.2.1/6.2.2: SD/card termination SHALL conditions and host/card KDF/authentication
  roles. Application selection can end a Secure Channel under that specification. Applet-specific
  reset/reselect behavior still requires profile evidence. B6's inspected final §6.2 empty/status
  mechanics remain B6-owned and are applied as concerns at actual empty HSM command sites. The
  code-cited 1.1.2.6 **Public Review** is a different unavailable edition; no section-number transfer.
* **SCP11** retained **1.3.0.13 Public Review**, August 2023, GPC_SPE_093, PDF SHA256
  `5db47b3df3c4e565a5ad91257395604f3482ed5db70c64853544c3b0e289fde0`. B6's exact inspected §4.3
  distinguishes host receipt verification from SD-side a/c first-protected-APDU authentication. This
  boundary reuses that qualified family conclusion rather than claiming fresh full-draft inspection.
  Final 1.4/exact YubiKey profile remains unverified. HSM's inherited setup flag supplies no host-management
  or external HSM authorization.
* **ISO 7816-4:2020**, edition 4/confirmed 2025, amendment 1:2023: B6 identifies metadata/abstract only;
  target APDU clauses remain unavailable. This report describes current bytes/absence of 6C replay
  without inventing unseen normative host requirements.
* B1's PC/SC/API originals and B2–B4's native-shim/platform qualifications retain their exact producer,
  header/runtime and release-proof limits. This SmartCard applet does not inherit HID implementation
  guarantees. NativeShims 1.18.1 source is distinct from a deployed/reproducibly bound binary.
* C's credential/TLV/crypto/provider originals establish carrier/copy/contained-owner scope, not automatic
  mathematical or HSM-profile validation. There is no HSM CALCULATE call to Core ECDH/CMAC result KDF.

## Framework API check

Context7 resolved `.NET` to `/dotnet/docs` and queried `MemoryMarshal.TryGetArray`/underlying owner
semantics. Its returned general span/range material did not establish the targeted method contract.
One focused follow-up read the original API page:
<https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.memorymarshal.trygetarray?view=net-10.0>.
Page source revision `16cd75d9c914a6fe1d29f7ae891c6166f4b5fefe`, updated 2026-09-09, .NET 10 among its
monikers (page's default/canonical moniker is 11). Definition: retrieves an array segment from the
underlying buffer; failure yields default segment/false. Remarks caution against writing through returned
read-only-memory segments. This is a managed API contract, not SDK owner transfer or runtime erasure
evidence. HSM source and sealed APDU constructor independently establish the present array identity.
Core's explicit `Clear()` is the appropriate supported owner API.

Reuse prior .NET 10 ZeroMemory contract (“fills the provided buffer with zeros”) and static PBKDF2
evidence from HSM S6/C's pinned framework register. This boundary ran no framework/crypto experiment and
promises no erasure of independent copies, immutable strings or provider internals.

## Closed research bounds and precise remaining gaps

The prior applet investigation and cryptogram follow-up each retained their initial pass plus focused
follow-up. No versioned normative YubiKey applet CALCULATE response grammar, production-firmware boundary
or raw 56-byte region identification emerged. Those searches are reused as completed bounded research;
generic Core agreement and generic HSM AUTHENTICATE commands cannot fill that gap. Seek exact vendor
profile/revision or separately authorized actual response evidence rather than extending broad searches.
SCP final/profile/ISO gaps retain B6's closed bounds. The single framework follow-up resolved only the
underlying-array API fact. Live HSM/connector, deployed native binary and platform/device lanes remain
qualification tasks, separate from completed source reading.
