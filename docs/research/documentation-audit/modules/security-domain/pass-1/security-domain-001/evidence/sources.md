# Focused original-source evidence

Access date: **2026-11-04**. Investigator: `github-copilot/gpt-6.1-sol`. Excerpts below are deliberately small; no downloaded specification corpus is retained. Search snippets were discovery aids, not final-release evidence. No hardware or experiments were run.

**Coordinator erratum, 2026-11-04:** E2's certificate-chain serial passage is in **§7.5.1**, not §7.5.4.
The original investigator text below is retained as history. See [independent original checks](review-sources.md)
and [corrections](../corrections.md), which also add the draft §4.6 SCP11c command restrictions. Do not
propagate the superseded subsection pointer.

## E1 — Yubico implementation guidance

Original: [Secure Channel Protocol (SCP03 and SCP11) Specifics — YubiKey Technical Manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-scp.html). Living vendor manual; no numbered revision displayed. Read the complete retrieved SCP03 developer-guidance sections, and the introductory SCP support/key sections, through Exa `web_fetch_exa`.

Sections **Yubico SCP03 Developer Guidance / Key Sets** and **Sessions**:

> The YubiKey only allows putting or deleting a whole key set at a time. Manipulating the individual keys within the set, is not allowed.

> 255 is the factory default version and therefore that version number is reserved. When importing a key set, the version is set to a value in the range 1-254.

> By default there is one key set installed with key version 255 and the value `404142434445464748494a4b4c4d4e4f` for all three keys. These keys are known as the test keys. When a new key set is installed, it replaces the default key set. The YubiKey supports only AES-128 for all three keys.

> When authentication with a key set fails repeatedly (for example, 32 times in a row) that key set is deleted. When the last key set is deleted, the security domain is automatically reset with the default key set installed. To delete the last key set on purpose and force a reset, send the delete instruction with `p2=1`.

> To manage SCP03 keys, a session needs to be established with the AID `a000000151000000` - the Issuer Security Domain.

Actor/strength/applicability: statements of **YubiKey firmware behavior**, especially SCP03, not SDK validation promises or general GlobalPlatform mandates. The manual's “for example, 32” is not a universal retry-count contract. This supports correcting the README's implication that the factory KVN remains for later deletion after first import, and describing last-key deletion/reset consequences. It does not settle mixed SCP03/SCP11 reset state or all firmware variants.

Introductory sections state SCP03 available from **5.3.0**, SCP11 from **5.7.2**, and describe Yubico as implementing a **subset** of the GP protocols. These are vendor applicability conditions, not claims of complete SCP conformance. The manual contains some generalizations about ECC transport triples and other inconsistencies; do not copy it wholesale or treat its conceptual flow as byte-level authority.

## E2 — SCP11 original draft, explicitly not v1.3 final

Original: [GlobalPlatform Technology, Secure Channel Protocol '11', Card Specification v2.3 – Amendment F, Version 1.3.0.13, Public Review, August 2023, GPC_SPE_093](https://globalplatform.org/wp-content/uploads/2023/08/GPC_2.3_F_SCP11_v1.3.0.13_PublicRvw.pdf).

Read complete sections **4.3**, **7.8**, **7.9**, **7.10**, **7.4**, and the relevant certificate-chain processing passage in **7.5.4**, from the original PDF text returned by Exa. This draft targets v1.4 and must **not** be cited as final v1.3. The original catalogue separately lists final v1.3 (November 2021) and v1.4 (March 2024): [catalogue](https://globalplatform.org/specs-library/secure-channel-protocol-11-amendment-f/). The draft's revision history explicitly identifies added X.509 and CA-KLCC identifier functionality after 1.3. Therefore do not silently relabel all implementation features as final-1.3 conformance.

**§4.3 Authentication**:

> For SCP11a and SCP11c only: The OCE authenticates itself to the SD by providing a certificate signed by the CA-KLOC and by providing the first APDU after secure channel establishment with a correct MAC. If an allowlist with one or more Certificate Serial Number entries exists in the SD for the CA-KLOC’s public key, the SD also verifies that the certificate is contained in an allowlist. Else the SD accepts all certificates signed by the CA-KLOC.

> special care shall be taken never to empty/remove the allowlist (i.e. if created, the allowlist shall always contain at least one certificate) because no restrictions apply (i.e. all certificates are accepted) once an allowlist is removed.

> SCP11a and SCP11c provide Mutual Authentication between the OCE and the SD. SCP11b provides Authentication of the SD to the OCE only.

**§7.9 STORE DATA (Allowlist)**:

> Update of the allowlist shall only be accepted if the Security Level is AUTHENTICATED.
> Note: Security Level AUTHENTICATED cannot be achieved with SCP11b.

> To remove an allowlist, the allowlist TLV of the command shall have a length of zero.

> Tag '92' shall be present in the command if it is received in an SCP11c session and shall not be present otherwise.

The rest of §7.9 specifies a positive two-byte anti-replay counter up to 32767, `6A80` rejection when missing/non-increasing/exhausted, and atomic counter/allowlist update. SDK `StoreAllowListAsync` and `ClearAllowListAsync` emit A6 + 70 only, with no tag 92. **This is a vendor-profile/applicability question, not a confirmed YubiKey defect**, because final/profile evidence is unavailable.

**§7.5.4**, certificate chaining:

> if the CSN of the first (or only) CERT.KA-KLOC.ECDSA is not referenced in the allowlist, then the certificate (and the entire certificate chain) shall be rejected. Other CSNs found in the certificate chain shall not be checked

> The allowlist only contains CSNs of intermediate Key Authorities directly signed by the CA. Thus all certificates issued by any allowlisted intermediate Key Authority will be accepted.

Actor/strength/applicability: “shall” requirements on **the SD implementation** in this **draft**. They are not .NET argument validation. Distinguish direct OCE certificates from CA-signed intermediate authorities rather than promising filtering of arbitrary leaf serials. The module integration test uses a CA-issued intermediate serial but does not test rejection of an unlisted alternative.

**§7.8 Certificate Store** requires A6/83 reference plus BF21 certificate store; chain starts with certificate signed by CA-KLCC, ends with CERT.SD.ECKA, each verified using its predecessor. Certificates are stored/returned, not locally trust-validated by this SDK method. §7.10 CA Identifier defines subtag 80 value 0 for CA-KLOC or 1 for CA-KLCC and links the identifier to the specified public CA key or SD private key, respectively. §7.4 defines FF33/FF34 and associated KID/KVN. These support terminology/shape explanation, but final-version applicability is still a recorded gap.

## E3 — Framework API documentation (Context7)

Required Context7 resolution selected `/websites/learn_microsoft_en-us_dotnet` and `/dotnet/docs`.

Query: `Convert.FromHexString(string) exceptions FormatException odd length invalid hexadecimal`.
Original returned: [Convert.FromHexString, .NET 10](https://learn.microsoft.com/en-us/dotnet/api/system.convert.fromhexstring?view=net-10.0).

> ArgumentNullException: `s` is `null`.
> FormatException: The length of `s`, is not zero or a multiple of 2.
> FormatException: The format of `s` is invalid. `s` contains a non-hex character.

The module calls this API directly without a catch. Its XML's `ArgumentException` claim for malformed hex is wrong; use `FormatException` separately from empty-collection `ArgumentException`. An empty **string** is accepted by the BCL and creates a zero-length serial TLV; only an empty **collection** is rejected locally.

Query: `X509CertificateLoader LoadCertificate returns X509Certificate2 certificate disposal IDisposable`.
Context7 returned [SYSLIB0057](https://github.com/dotnet/docs/blob/main/docs/fundamentals/syslib-diagnostics/syslib0057.md): use `LoadCertificate` for X.509, `LoadPkcs12[Collection]` for PKCS12, rather than obsolete content-sniffing constructors. The result did not directly establish every lifetime claim; module `GetCertificatesAsync` and its full handle-based unit assertions establish the local successful/failure ownership contract. Do not inflate this retrieval into a framework lifetime experiment.

## E4 — Fixture reference

Original: [RFC 4493, The AES-CMAC Algorithm, June 2006](https://www.rfc-editor.org/rfc/rfc4493.html), **Informational**, explicitly “does not specify an Internet standard of any kind.” Read §2.3 and §4, including complete four vectors. §2.3 defines Rb `00000000000000000000000000000087`. §4 key is `2b7e151628aed2a6abf7158809cf4f3c`; expected CMACs:

| Message bytes | Expected MAC |
| --- | --- |
| 0 | bb1d6929e95937287fa37d129b756746 |
| 16 | 070a16b46b4d4144f79bdd9dd04a287c |
| 40 | dfa66747de9ae63030ca32611497c827 |
| 64 | 51f0bebf7e3b9d92fc49741779363cfe |

These exactly match the module test data and assertion of `AesCmac`, **not** independent SCP03 KDF/MAC-scope/handshake conformance verification. No test was executed here.

## Access failures, follow-up bound and remaining gaps

1. Initial Yubico search returned `yk5-scp-specifics.html`; direct `webfetch` returned **404**. Focused alternate `yk5-apps-scp.html` succeeded through Exa and supplied E1.
2. GP catalogue HTML succeeded through `webfetch`; its “v2.3.1” URL now resolves to **Card Specification v2.4**. Do not automatically upgrade the module's citation target. SCP11 catalogue displays current v1.4 and an older final-1.3 download, using `download_spec(34377, 'committee-se')` rather than a direct PDF link.
3. Standard-library `urllib` requests to those catalogue URLs returned **403 Forbidden**. An attempted convenience parser failed with **ModuleNotFoundError: bs4** before fetching anything; no package was installed.
4. Focused final-version searches did not locate accessible final 1.3 / Card 2.3.1 PDFs; returned drafts were retained only as explicitly labeled evidence. Exa fetch of `https://globalplatform.org/wp-json/wp/v2/media/34377` returned **CRAWL_HTTP_401**. No authentication, access-control circumvention, form submission, or further version hunting was attempted.
5. Final **Card Specification v2.3.1 §11 / §11.11**, final **Amendment F v1.3 §§4.3/7.9**, and applicable **Amendment D** exact revision/KDF clauses remain **not verified from final originals**. ISO 8825 is not independently verified. Narrow raw-command documentation can describe the emitted bytes/local requirements without claiming certification. Coordinator may supply an authorized final copy later; consequential final-version claims must then be reopened.
6. Feature constants for key attestation/allowlist at 5.7.0 are local descriptors, not proof of device/model applicability or enforcement. E1 settles SCP03/11 broad minima only. Do not invent a key-attestation API: none is declared in this module.

This completes initial research plus focused follow-up. Unresolved version/profile questions are handed off rather than expanded into a standards archaeology or Core investigation.
