# Final B6 source register and qualifications

Source reading and bounded research are complete. This register supersedes the
checkpoint's unfinished-work lists in `register.md`; the original checkpoint bytes
and reports remain in `../history/checkpoint-48-files/`. Missing original editions,
unretained oracle provenance and unrun execution are final qualifications, not unread
B6 units or a request for further unbounded research.

[`final-manifest.json`](final-manifest.json) is the coordinator aggregation index.
It records retained paths, exact raw hashes, URLs/access times/status and failed
requests. Original PDFs remain byte-exact. Text derivatives and tool-extracted text
are different evidence forms and are never labeled original PDF bytes.

## Original PDFs and actual clauses

| Original / exact status | Retained path and SHA-256 | Read clauses / actor and strength |
| --- | --- | --- |
| GlobalPlatform Card Specification v2.3 Amendment D, SCP03 **v1.1.2 Public Release**, March 2019, GPC_SPE_014 | `scp03-1.1.2.pdf`; `973a10834f7fe8befd98da9af65393622234b80745092bd85248804c57b648dd` | Complete §4.1.5, §6.2.1–6.2.8 and §7.1.1 through external-authenticate command/response/status introduction. Off-card SHALL applies exact CMAC KDF layout, C-MAC header modification/excluded Le, counter and empty-data rules; card SHALL applies status-only unprotected errors, response encryption/ICV and R-MAC, with external-authenticate exception. §7.1.1.6 conditionally includes a three-byte sequence counter. §7.1.2 table has absent Le. Preserve these exact final-edition coordinates rather than silently using cited review-edition subsections. |
| GlobalPlatform Card Specification v2.3 Amendment F, SCP11 **v1.3.0.13 Public Review**, August 2023, GPC_SPE_093 | `scp11-public-review-1.3.0.13.pdf`; `5db47b3df3c4e565a5ad91257395604f3482ed5db70c64853544c3b0e289fde0` | Complete §§2.1/2.2, 3.1.1/3.1.2, 4.1–4.6, 7.1.1, 7.5.1 and 7.6.2.3 through Table7-18/status introduction. Draft SHALL/MAY actors stay explicit: SD verifies submitted OCE chain/allowlist; host uses trusted SD key and verifies receipt; a/c host authentication includes first correct-MAC APDU after establishment. a/b/c key roles differ; c returns static SD key and permits replayable offline use under specified restrictions. Host/Card/CardGroup inclusion is conditional on requested parameter bit3, not universal. |

The protocol meanings support narrow observations, not blanket certification. SCP03
command-chain/response-chain physical routing, exact reader limits, provider success
and YubiKey applet authority still require the appropriate profile/execution lane.
SCP11c's static SD key can pass through the common two-agreement helper; the variable
name “ephemeral” alone establishes a comment error, not inherently wrong c mathematics.

## Closed bounded source gaps

| Qualification | Completed initial/focused investigation | Final bound |
| --- | --- | --- |
| **Q1: code-cited SCP03 1.1.2.6** | Official original located; cover/revision text says Public Review, December2019, target1.2. Focused raw download returned403. Official catalogue distinguishes final1.2, April2020. | No raw1.1.2.6 clause was inspected. Use retained final1.1.2 only for the clauses actually read; do not relabel or transfer all section numbers. |
| **Q2: final SCP11 1.4 / complete YubiKey profile** | Official catalogue confirms final1.4, March2024, and described changes. Initial plus focused original search located review and older final1.2 links; raw older final fetch403. No gated download/registration/licence acceptance was submitted. | Final1.4 wire/trust/permission clauses remain unverified. Retained review language is identified as draft, including c restrictions and host-auth phase; vendor implementation behavior is separate. |
| **Q3: NIST original sample ZIP/PDF bytes** | Official Component Testing page links exact `800-135testvectors/ansx963_2001.zip`; full ASKDFVS §6.5 and document metadata/§§3/5 read by extraction. Initial direct page/PDF and one exact-link ZIP/PDF follow-up returned403. | Local100-row consistency and20 embedded-test tuple identity are established without cryptographic recomputation. Original ZIP equality is unavailable. Sample use is informal, not formal CAVP validation, NIST endorsement or proof of full profile conformance. |
| **Q4: ISO7816-4 normative APDU clauses** | Official catalogue: edition4, May2020, confirmed2025; amendment1:2023. Initial catalogue request and focused official OBP attempt403; focused search exposed no target clauses. | Metadata/abstract is not APDU case/Le/logical-channel/corrected-length normative text. Current C# wire bytes and absence of6C retry are observed; no unseen ISO host-MUST or section number is asserted. |
| **Q5: BCL hidden buffer behavior** | Context7 `/dotnet/docs` stackalloc reference explicitly says initial content undefined. Two pinned ArrayBufferWriter implementation paths returned404. C's exact AsnWriter/provider originals reused. | Explicit stack initialization is a portability/source-quality concern, not observed garbage or current JIT/AOT crypto failure. C002 relies on visible SDK-owned copies, not inferred hidden BCL growth-buffer erasure. |
| **Q6: captured SCP11 receipt / fixture assurance** | Full deterministic test and fixed key/point/receipt inputs inspected. No immutable original device capture accompanies the claimed capture. Actual public PEMs and serials inspected as source DER. | Test verifies a particular receipt via the helper and then a nonnull SessionKeys, not all session keys/wire steps. Subject/public-key/nonempty checks do not prove trust/validity/key association/container import. |
| **Q7: actual runtime/native/device lane** | No new SDK, provider, native or device execution was authorized. Reused C/B1/B2/B3/B5 source and their exact retained qualifications. | .NET/API source, NativeShims1.18.1 source and package names do not identify a loaded binary or establish native erasure/release. Future Windows handoff and PIV physical deferrals remain explicit. |

NIST ASKDFVS extraction identifies update September16,2015, prior March3,2015,
original March23,2012. Its §6.5 IUT/CST/CAVS process compares complete key_data, uses
two sets of ten per hash and pads non-byte-width Z on the left. The local CAVS12.0
sample is dated March19,2012 and includes historical SHA-1/192-bit inputs; retain
the distinction from later allowed validation profiles and recommendations.

## Reused originals and implementation history

- RFC4493, Informational, June2006, C's retained original `rfc4493.txt`: complete §4
  subkeys/Examples1–4 and verification discussion read. Current exact full-value tests
  match those inputs/tags; consistency-only and length-only cases remain narrower.
- Yubico technical-manual snapshot is vendor guidance, not a normative GP edition or
  fresh hardware result. Inspected firmware/subset/strict-security/plaintext-termination
  and diversification passages are useful; its broader external-authenticate narrative
  is not substituted for the final command table. Remaining unrelated vendor page text
  contributes no B6 coverage or claim.
- Python pin `4ca60f706af930459138d8dc0f0f953480e1c7a4`: complete current SmartCard and
  SCP sources plus complete50-line device test read. Five bounded history file-diff
  scopes and their immutable revisions are recorded in [completion-evidence.md](../completion-evidence.md).
  Unselected applet/CLI hunks are excluded from history conclusions. Source policy,
  historical prototypes and author timestamps are distinct from standards, first
  hardware support and publication dates. No Python package or crypto provider ran.
- C's final provider/ASN.1/OpenSSL source register and B5's completed source/assertion
  handoff are hash-indexed in the final manifest. B6 reads only necessary dependencies
  and supplies actual wire/copy/phase mechanisms, without duplicating whole C/B5 reviews.

The originals' access timestamps use UTC; the session's local task date is November12.
The later metadata HEAD may advance without changing source pin `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
