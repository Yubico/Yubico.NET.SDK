# OpenPGP hardware results

## Device and starting state

Serial **103**, YubiKey 5 NFC Enhanced PIN USB-A, Linux/Omarchy, USB SmartCard. Device metadata
**5.8.0.beta.0**, CLI OpenPGP card **3.4**/application **5.8.0**; SDK and direct Python session
version **0.0.1**. Keep each actor's observation distinct. User authorized all test configurations,
resets and ad hoc work; physical touch remains coordinated.

Complete baseline is hardware/vendor-baseline-v2.json. Three normal key slots/certificates empty;
ATT Imported with a 793-byte certificate, DER SHA256
94afb703099edb4aaaee443c6e197c379f7f909d23f717f0868ef73057e4a16a. Actual UIF DO reads all Off;
User/Reset/Admin retries 3/0/3, policy Once, KDF disabled, counter zero. V1 baseline's low-version
Python certificate rejection and synthesized UIF values remain retained separately; source-checked
public APDU reads supplied actual V2 observations, without overriding private session version.

## Existing integration methods

**All 49 methods passed**, zero skips and zero compiler/analyzer warnings: 48-method non-KDF batch
(`integration-nonkdf-complete`, 181.5 seconds) plus isolated KDF (`integration-kdf`, 5.5 seconds).
The initial 120-second invocation stopped after 24 reported passes without a final result; preserved
as interrupted. No test processes survived inspection, independent cleanup passed, and the complete
48-method rerun used a 600-second allowance. The original interruption is not counted as suite success.

The 49-method source/effect/assertion matrix is hardware-test-plan.md and evidence/hardware-methods.json.
Every reached helper resets before its body and disposes after; none performs final applet cleanup.
The four old-firmware early-return conditions were false for this key. All selected callbacks executed.
Existing RSA/P256/P384 import-signatures, P256 authentication, RSA plaintext and P256 ECDH equality
provide host comparisons; metadata/nonempty-output tests retain their narrower meanings.

Independent vendor cleanup after the interruption, non-KDF batch and KDF case passed full readback,
factory User ordinary/extended and Admin verification, and ATT certificate identity preservation.

## Supplemental probes

| Attempt | Actual result and scope |
| --- | --- |
| v1 KDF distinct salts | **Failed setup** at F9 PUT DATA, SW6985, after Sig P256 generation. Later KDF readback, ordinary control and extended VERIFY were unreached. Pre-reset public read and full independent cleanup passed. |
| v1 extended User retry identity | **Failed intended contract assertion.** Device User retries 3→2; Reset Code retries 0→0; typed exception reports zero instead of two. Both counter assertions passed before the typed-count equality failed. Pre-reset read/full cleanup passed. PGP-IMP-001 confirmed for this scope. |
| v2 KDF reordered provisioning | **Failed intended extended-mode contract after successful controls.** F9 before key generation succeeded; fresh session read three distinct salts, verified Admin/ordinary User default credentials directly, generated Sig P256 and host-verified an ordinary signature. Extended verification of the same correct User PIN then returned6982/typed retries0 while actual User3→2, Reset0/Admin3 unchanged. Pre-reset read/full cleanup passed. |
| v2 first touch-sign attempt | **Failed ceremony.** One PolicyRequires request, followed about14 seconds later by6600/Failed; no successful signature oracle reached. Independent cleanup passed. User confirmed immediate readiness for a retry. |
| v2 touch-sign retry | **Passed.** User reconfirmed timing; On/PolicyRequires→Completed, P256/SHA256 signature host-verified, fresh read/full scoped cleanup passed. |
| v2 touch authentication | **Passed.** Ordinary User verification denied with6982 under Off policy; extended User plus On touch yielded host-verified signature and one Completed resolution. Cleanup passed. |
| v2 first touch-decryption attempt | **Failed ceremony.** Ordinary-mode6982 denial control passed; touch request ended6600/Failed about14 seconds later. Plaintext oracle unreached; cleanup passed. |
| v2 touch-decryption retry | **Passed.** User reconfirmed timing; ordinary denial control, extended User/On touch, known RSA-PKCS1 plaintext equality and Completed notification passed. Cleanup passed. |
| v2 extended-only touch attestation | **Failed before successful touch operation.** Extended-only User verification followed by FB returned6982 within about4ms of PolicyRequires; Failed resolution and no certificate oracle reached. Scoped cleanup passed, while ATT UIF remained On as an explicitly observed reset exception. |
| v3 ordinary-mode touch attestation | **Passed.** Ordinary User verification plus actual touch produced one Completed resolution; certificate signature verified under the preserved issuer, certified SPKI matched generated Sig P256 key, and Att certificate bytes were unchanged. This is issuer/key-binding proof, not an independent manufacturer-root chain validation. |
| v3 no-touch cancellation/recovery | **Passed for measured scope.** Cancellation scheduled1500ms after setup; prompt clock request at2074ms, terminal at14811ms with6600/Failed. Fresh connection read state, authenticated, changed Sig UIF Off and produced a host-verified signature before any reset. Scoped vendor cleanup passed. |
| v3 X25519 SDK + same-key vendor | **SDK failed6A80** at Decipher after successful RFC Alice import/public-point match. Fresh installed Python client, before any reset/import, verified that same Alice key and derived the exact RFC7748 Alice/Bob secret using its EC wrapper. Vendor comparison/independent cleanup passed; PGP-IMP-007 confirmed. |
| v3 ATT public read + correct-CRT reference | **SDK failed6A80** at read-existing ATT public key. Independent public-only query using B603840181 parsed RSA and matched the current and baseline issuer SPKI. Neither query requested PIN verification, reset, key generation or private-key replacement. PGP-IMP-013 confirmed. |

Full v1 source/compiler context is retained under validation/hardware-probe/evidence/final-inputs-v1.
V2 changes only the control key's provisioning order and explanatory comment. The original F9 refusal
remains a distinct state/transition observation (PGP-IMP-002); broad firmware applicability is unverified.
Initial hashes use the SDK KDF, so this probe establishes credential-identity behavior rather than an
independent KDF algorithm proof. v2 confirms both the working initial credential transition and the
extended-mode identity defect on this device. It does not turn the failed intended contract into a pass.
User confirmed readiness and immediate retries where timing was missed; original failed ceremonies
remain preserved. All nine distinct supplemental .NET methods have executed: five have a successful
final measured outcome (four touch operations and cancellation/recovery), four retain intended-contract
failures (KDF identity, retry identity, X25519 decipher and ATT public read). There were 13 .NET probe
invocations including preserved earlier failures. Both independent protocol-reference comparisons passed.
The v3 four-source compiler capture includes the two added cases and ordinary-mode attestation correction;
all 228 dependency identities and full source/config/assets were reconciled immediately after compilation.

## Qualification and cleanup status

**Final cleanup passed.** After recording that Att On survives reset, the source-checked restoration
helper authenticated with the published factory Admin test credential and wrote only Att UIF Off.
Fresh readback confirmed Off and unchanged Att certificate. Another independent reset/full readback,
factory User ordinary/extended and Admin verification, and a second fresh full snapshot followed.
hardware/final-reconciliation.json verifies **all public state fields exactly match the initial V2
baseline**, including all four UIFs Off, normal key/certificate absence, 3/0/3 retries, KDF disabled,
Once/counterzero, algorithm configuration, device configuration and Att certificate identity.

The existing Att private key was never replaced. Its continued use is evidenced by the successful
certificate-signature oracle, alongside public identity preservation. All device/test processes finished
before final restoration; no further hardware job is active. Python immutable response buffers have no
secure-erasure guarantee; only public fixture material was used and no private/shared output bytes logged.
NFC, older firmware/families, other platforms and Native AOT runtime remain unverified. No timing sample
establishes native admission/interruption causality or universal timeout behavior.
