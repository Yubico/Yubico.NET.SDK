# Citation and evidence notes

The patch primarily describes the pinned implementation, not newly inferred protocol requirements.
Complete changed source/documentation files were read before editing. The completed boundary's
caller/resource/oracle tables were reused for retained dependencies. The root/Core/OpenPGP/nested
instructions, approved `records/style.md`, SDK house style, docs-module skill, editing release and
full-family triage were consumed. No new library API question required Context7: framework behavior
was not inferred from a generic disposal pattern. Actual Core dispatch was read directly.

## Consequential authorities

| Authority / exact retained identity | Section, actor and strength | Editing applicability and limits |
| --- | --- | --- |
| Product source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; editing checkpoint `da66f5d01ba594993974ddd26723e8c300c0a454` | OpenPgpSession initialization/disposal, Backend version fallback, direct/device factories, Kdf/Config/import/template/crypto/key/reset bodies; Core ApplicationSession construction/configuration, notification resolution and disposal; Scp03/11 parameter owners, StaticKeys and PcscProtocolScp. Implementation evidence, not normative protocol language. | Exact source supports ownership, ordering, version provenance, admitted exchange and callback/result claims. Core source was read-only. Source inspection is not compiler/runtime/native or device proof. |
| OpenPGP Card functional specification, **Version 3.4.1, March 18, 2020**, Achim Pietig. Original retained PDF SHA-256 `2d2652a2bd690a6784c733ba6d88539e2af1f6e075f3a3d0af81eb5f68432782`; retained text derivative `1cf55026521edd672e98d23ddfedb5c424e5349d2a4c37ac8b5a28af3733177a`. URL filename still says 3.4. | Re-read retained text §§4.3/4.3.1/4.3.2 and §§7.2.2–4 (text lines 650–789, 1980–2169). Card checks credential format/length; terminal aligns credentials/F9. KDF presence/derived state and terminal evaluation use **shall**; at-least-64-byte capacity uses **should**. Both P2=81 and 82 address PW1. VERIFY/CHANGE tables explicitly specify absent Le. | Published application functional specification, not ISO itself or a universal vendor firmware contract. Preserves PIN/KDF identity and credential units, without importing CTAP normalization rules. Re-read GET DATA/PUT DATA/occurrence text at lines 2205–2429 supplies selection context. Card absent-Le evidence maps to CORE-B6-C001; no formatter behavior was changed. Original PDF identity is reused from the completed packet; this is not a new cover-to-cover PDF acceptance review. |
| **RFC 9580**, July 2024, IETF **Standards Track**; retained original text SHA-256 `36540e57b63d24c0b17be0748b704bdeed3d643ce99a4fb1abb6469c1eb1ff18` | Re-read status/header and §3.7.1.3. S2K implementer hashes the counted octets, with at least one full salt-plus-passphrase input; the RFC packet count is coded in one octet. | Existing Kdf XML/README primitive citation is retained. Card F9's expanded four-byte count is distinct. This does not select a newer OpenPGP packet profile, recommend a production KDF policy or prove a test vector execution. |
| Yubico **OpenPGP Attestation**, unversioned manufacturer extension HTML, retained access **2026-11-04**, SHA-256 `77da864a47a8ed09c62a61c8981b7ebfbc9f79ee930cffe108fc7344a46c1683` | Re-read retained document text: Implementation; verifier warning; Protocol Specification → Attestation Key/Data Objects and Generate Attestation. Host must address ATT using complex CRT `B6 03 84 01 81`; device overwrites the target certificate; vendor says ATT key/certificate survive reset. Verifier checks binding/trust and does not trust mutable metadata. | Manufacturer extension authority, with no invented revision/RFC status. Preserves malformed ATT CRT/read and overwrite/trust qualifications; source text is not a fresh device test or claim that a saved public certificate restores factory private provenance. |
| Python YubiKey Manager `4ca60f706af930459138d8dc0f0f953480e1c7a4`, project `5.9.3-dev.0`; frozen OpenPGP module SHA-256 `bcd266e53411a9611ee133ed5f33e2907294fbe374aa56a19672a151e86a2926` | Completed packet's selected init/KDF/PIN/reset/decipher method reading is reused. Sibling implementation comparison, with no normative strength. | Keys comment now calls it the Python implementation, rather than a canonical standard. Its specific 6985 fallback is not substituted for this C# implementation's broader fallback. No Python imports/tests/crypto/device operations ran. |

## Retained citations and stronger claims on hold

Existing RFC 8017/8032/7748 representation/crypto references, vendor ROCA/touch/reset guidance,
legacy OpenPGP OIDs and original historical hardware observations remain at their prior scoped
authority. Their neighboring product explanations and all example bodies are retained; this edit
does not claim a fresh full revalidation of those archives. `boundaries/OpenPgp/sources.md` and
`source-identities.json` remain the authority for reused source identities and bounded qualifications.

Retain B6's edition distinctions: SCP03 1.1.2 **Public Release**, March 2019, is not the code-cited
1.1.2.6 **Public Review**, December 2019. SCP11 1.3.0.13 is **Public Review**, August 2023;
final 1.4 clauses and the complete YubiKey profile remain unverified. Receipt/authentication flag,
first protected command, host authorization and applet PIN access remain separate. No unseen
normative SCP or ISO clause is quoted. ISO 7816-4:2020 exact APDU case/corrected-length/logical-channel
clauses, deployed NativeShims/header correspondence, physical release and producer erasure remain
qualified with their existing B4/B6 owners.

The actual assertion descriptions reuse the complete boundary's source-reviewed oracles; editing
adds no build, test, hardware or independent review outcome. Prior serial-103 Linux USB SmartCard
results remain historical, including applet 0.0.1 versus metadata 5.8.0.beta.0/CLI application 5.8.0,
failed setup, intended-contract failures and successful scoped controls. No stronger provider,
firmware floor, NFC, SCP-reset, native cancellation or timing-causality statement is released here.
