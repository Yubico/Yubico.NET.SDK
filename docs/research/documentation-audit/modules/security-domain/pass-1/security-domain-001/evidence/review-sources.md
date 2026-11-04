# Independent reviewer source checks

2026-11-04; fresh `github-copilot/gpt-6.1-sol` reviewer. These checks fetched originals again;
investigator excerpts were context, not proof. No final GP specification was obtained.

## R-E1 — Yubico technical manual

Original: [Secure Channel Protocol (SCP03 and SCP11) Specifics](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-scp.html).
Retrieved through Exa `web_fetch_exa`, maximum 50,000 characters; read the introductory support sections
and complete SCP03 Developer Guidance, especially Key Sets and Sessions. Living manual, no numbered
revision displayed. Actor is YubiKey firmware, not SDK argument validation.

Independently confirmed:

- Introductory support: SCP03 from 5.3.0; SCP11 from 5.7.2; each implements a subset.
- Key Sets: only whole SCP03 sets may be imported/deleted; version 255 is reserved; custom versions
  are 1–254; three AES-128 components; the first new set replaces the factory set.
- Key Sets: repeated failed authentication deletes a set (32 is an example, not a universal bound).
  Last-set deletion automatically resets the SD with the default set installed; explicit deletion
  uses P2=1. This SCP03 discussion is not evidence for every mixed SCP03/SCP11 reset state.
- Sessions: SCP03 key administration uses Issuer SD AID a000000151000000.

No device execution or complete protocol conformance follows from these statements.

## R-E2 — Original SCP11 public-review PDF

Original: [GlobalPlatform Technology, Secure Channel Protocol '11', Card Specification v2.3 – Amendment F,
Version 1.3.0.13, Public Review, August 2023, GPC_SPE_093](https://globalplatform.org/wp-content/uploads/2023/08/GPC_2.3_F_SCP11_v1.3.0.13_PublicRvw.pdf).
Retrieved independently through Exa `web_fetch_exa`, maximum 180,000 characters. Read actual title/status,
the complete relevant definition/authentication/STORE DATA sections and surrounding conditions. This is
not final v1.3, and its normative language applies to an SD implementing this draft, not automatically
to the YubiKey profile or .NET host validation.

### Citation correction: §7.5.1, not §7.5.4

The certificate-chain serial checks are in **§7.5.1, Definition and Scope**, under PERFORM SECURITY
OPERATION (printed page 50). Without intermediates, the OCE certificate is checked against the list.
With intermediates:

> if the CSN of the first (or only) CERT.KA-KLOC.ECDSA is not referenced in the allowlist, then the
> certificate (and the entire certificate chain) shall be rejected. Other CSNs found in the
> certificate chain shall not be checked

The following bullet explains that the list contains intermediate authorities directly signed by the CA.
This supports the substantive direct-CA certificate scope in the edited docs. However, the docs and
investigator evidence give the wrong subsection. **§7.5.4, Processing Rules** (printed page 54) instead
discusses BF20 authorizations at security level ANY_AUTHENTICATED. It is not the cited chain-serial passage.

### Other consequential clauses rechecked

- **§4.3 Authentication:** SCP11a/c mutually authenticate OCE and SD; SCP11b authenticates SD to OCE
  only. With no allowlist, all CA-signed certificates are accepted; removing an allowlist removes
  that restriction. Other authentication checks still apply.
- **§7.8 Certificate Store:** A6/83 reference plus BF21 store, issuer-to-device-leaf verification
  order, store associated with SK.SD.ECKA. This is distinct from the OCE chain submitted for host
  authentication. It does not say the SDK loader verifies that chain.
- **§7.9 Allowlist:** requires AUTHENTICATED, which SCP11b cannot achieve; replacement supersedes
  the old list; a zero-length list removes it. Where SCP11c is supported, tag 92 is required in
  SCP11c and forbidden otherwise; a missing/non-increasing/exhausted counter yields 6A80; counter
  and list update are atomic. Module code sends A6+70, not 92. Final/vendor applicability remains
  missing; no confirmed YubiKey defect or supported SCP11c update recipe is inferred.
- **§7.10 CA Identifier:** subtag 80 selects CA-KLOC=0 or CA-KLCC=1; identifiers link to CA public
  key or SD private key respectively. This supports terminology, not final-profile certification.
- Adjacent **§4.6 Session Replay** expressly restricts PUT KEY and DELETE [keys] in SCP11c. Do not
  turn the documentation's device-accepted-authority qualification into a claim that SCP11a/c are
  interchangeable for all administrative commands. No new compatibility guarantee was accepted.

## R-E3 — Final-version access boundary

Independently fetched the [original catalogue](https://globalplatform.org/specs-library/secure-channel-protocol-11-amendment-f/)
through Exa. It lists final v1.4 (March 2024) separately from v1.3 (November 2021), including X.509 and
GET/STORE DATA changes in v1.4's description. Catalogue text is not the final normative clauses.

One focused final-document follow-up requested
`https://globalplatform.org/wp-json/wp/v2/media/34377` through Exa; result **CRAWL_HTTP_401**.
Stopped there: no login, form submission, access-control circumvention or speculative version hunting.
Final Amendment F v1.3, Card Specification v2.3.1 clauses, exact applicable Amendment D revision/KDF and
ISO 8825 remain unverified. No accessible draft was substituted for a final release.

## R-E4 — .NET API checks

Context7 resolved `/websites/learn_microsoft_en-us_dotnet` and queried
`Convert.FromHexString(string)` exceptions. Returned original [.NET 10 API reference](https://learn.microsoft.com/en-us/dotnet/api/system.convert.fromhexstring?view=net-10.0):
null throws ArgumentNullException; odd length or non-hex characters throw FormatException. Empty
string is allowed by its length condition. The module calls this API directly; the revised exception
contract distinguishes empty collection from malformed/empty entries correctly.

Context7 also queried X509Certificate2.Thumbprint. A direct Exa fetch of the
[original .NET 10 property page](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.thumbprint?view=net-10.0)
confirmed that the property computes a SHA-1 hash. The integration assertion compares those strings;
it does not compare RawData bytes. Review R2 asks only that the comment name the observed comparison.

## R-E5 — RFC 4493

Independently fetched [RFC 4493, The AES-CMAC Algorithm, June 2006](https://www.rfc-editor.org/rfc/rfc4493.html)
through Exa; read §§2.3 and 4 and its Informational status. The Rb constant ends in 87. The 0/16/40/64-byte
vectors and common key exactly match `Scp03HandshakeFakeConnectionTests`. Those assertions concern
AES-CMAC only, not the fixture's entire SCP03 KDF, host verification or interoperability. No test ran.
