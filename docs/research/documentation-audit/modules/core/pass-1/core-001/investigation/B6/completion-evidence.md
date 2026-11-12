# Completed vector, fixture and bounded history review

## Both complete vector units

The 555-line `.rsp` and 1,335-line `.txt` were reviewed as structured data in their
entirety. Headers, option changes, field order, empty SharedInfo syntax, counter
encodings, input transcripts, full output blocks, truncation and consuming assertions
were examined. `read-vectors.py` consumes every nonblank line under an explicit grammar,
rejects unknown fields and compares actual bytes across all 100 records. This is
data/representation review, not a hash-only coverage claim or a new KDF execution.
`vector-review.json` retains each record's complete input/output fields and source
line ranges. No hash output was recomputed and no cryptographic provider was imported.

Both headers say CAVS 12.0, sample, generated March 19, 2012. Their complete population is:

| Hash | Z bits | SharedInfo bits | Output bits | Counts / intermediate rounds per record |
| --- | ---: | ---: | ---: | --- |
| SHA-1 | 192 | 0 / 128 | 128 / 1024 | Each 0–9; 1 / 7 rounds |
| SHA-224 | 192 | 0 / 128 | 128 / 1024 | Each 0–9; 1 / 5 rounds |
| SHA-256 | 192 | 0 / 128 | 128 / 1024 | Each 0–9; 1 / 4 rounds |
| SHA-384 | 192 | 0 / 128 | 128 / 1024 | Each 0–9; 1 / 3 rounds |
| SHA-512 | 192 / 521 | 0 / 128 | 128 / 1024 | Each 0–9; 1 / 2 rounds |

Every detailed record agrees byte-for-byte with its `.rsp` Z, SharedInfo and key_data.
Every Counter is four-byte big-endian starting at one; each Hash input is exactly
Z || Counter || SharedInfo. Every K width agrees with the declared hash, and key_data
equals the concatenated K blocks truncated to the requested bit/byte length. The
521-bit Z group occupies 66 bytes with the high seven bits zero. These checks do not
establish that a stored K is the correct digest; the unavailable original archive
binding and unrun cryptographic equality tests are separate qualifications.

`X963KdfTests` embeds precisely the two SHA-256 groups: ten 16-byte and ten 128-byte
expected results. All 20 full triples match these files, rather than merely their
COUNT labels. Its two MemberData methods compare full derived output to expected
bytes. `Scp11X963KdfTests.TestDeriveKeyMaterial_WithTestVectors` repeats the first
empty-SharedInfo SHA-256 case and compares full output. The other X963 cases assert
length, determinism, differences or input rejection. None loads either data file at
runtime or tests SHA-1/224/384/512 support. The production helper read narrowly is
SHA-256 only, appends Z/counter/SharedInfo in that order, and truncates its final block.
Its sensitive digest/result cleanup remains C's mechanism ownership; no whole-C re-audit occurred.

The source comments' `.rsp` ranges 226–280 and 281–335 correctly cover the two entire
SHA-256 option groups. The detailed-file counterparts are 646–730 and 731–905.
Preserve these useful coordinates, while qualifying the original archive identity.
The detailed file's intermediate rounds aid explanation/debugging, not extra assertions
or evidence that any SDK/provider/hardware test ran.

## NIST provenance: resolved structure, bounded original-byte gap

The official CAVP Component Testing page explicitly links
`components/800-135testvectors/ansx963_2001.zip` and warns that sample vectors do not
replace CAVP validation. The original ASKDFVS document was read through Exa extraction:
updated September 16, 2015, previously March 3, 2015, original March 23, 2012.
The complete §6.5 passage defines two sets of ten trials per hash option, Z/SharedInfo/
output bit lengths, empty SharedInfo, padded non-byte-width Z and full key_data comparison
by CAVS. §§3/5 distinguish formal validation, sampled algorithm checks and overall
product-security endorsement. The actor is the IUT/CST lab/CAVS validation process;
this SDK's embedded sample comparisons are informal regression checks.

The 2015 text uses CAVS 17.8 and no SHA-1/192-bit example in its current configuration
description; the retained files are the older 2012 sample. Keep historical vector
identity distinct from current allowed validation profiles and recommendations.
Direct page/PDF retrieval returned 403; one focused exact-link archive/PDF follow-up
also returned 403. The original ZIP/PDF bytes were not retained, and this review makes
no equality claim to an unseen archive. No mirror or newer fixture replaced them.

ISO's official catalogue identifies ISO/IEC 7816-4:2020, edition 4, published May 2020,
confirmed in 2025, and amendment 1:2023. It supplies metadata/abstract, not the inspected
APDU clauses. Initial direct access and focused OBP follow-up returned 403; the focused
official search exposed no freely readable target clauses. Exact APDU case/Le/logical
channel/6C actor requirements therefore remain a closed bounded source qualification.
Current formatter bytes and missing retry/routing behavior are source facts; no
unseen ISO section number or host-MUST requirement is assigned to them.

## SCP11 fixture bytes and actual assertions

Static base64/DER source inspection (without certificate loading or cryptographic
operations) confirms the first certificate is issued by Example OCE Root CA to Example
OCE Intermediate; the second has Root CA in both issuer and subject. Their encoded
serials are `7F4971B0AD51F84C9DA9928B2D5FEF5E16B2920A` and
`49FE7DC08A4228EACD18D73914C3D30FDCC31950`. The listed root serial
`6B90028800909F9FFCD641346933242748FBE9AD` is different. C013 retains this concrete
fixture metadata mismatch, not a new accepted finding or evidence that the skipped
allowlist ceremony failed. The current executed-shaped tests check Subject substrings,
public-key objects, byte nonemptiness and password text; they do not check those serials,
chain trust, key association, validity or container import. No fixture assertions changed.

## Bounded Python checks completed

Shared source pin is `4ca60f706af930459138d8dc0f0f953480e1c7a4`. Complete current SmartCard
and SCP source were read in the first stage; complete `tests/device/test_scp.py` is now
read. Its fixture gates firmware 5.7.2, requires SCP11b reference 13/01, takes the last
certificate's public key and skips when that reference is absent. Its 15 parameter
combinations cover five nonempty ECHO sizes 10/255/256/512/2048 and default/force-short/
no-config. It selects Management **before** SCP and compares the entire reply to the
random payload. It tests no empty command, injected failure, a/c provisioning, tamper,
final-spec conformance, source erasure or reset/restoration; no Python test ran here.

The following exact retained diffs were independently read, with actual changed
functions rather than titles used as evidence:

| Commit | Exact bounded read | Source conclusion |
| --- | --- | --- |
| `652e5825a2ecea8f9b4e3c55da6bf7b802594263` | Complete patch | May 21, 2025 author metadata; explicit short-APDU/NFC-reader rationale; whole already-wrapped message MAC precedes short command splitting; introduces ECHO test. Not earliest hardware support. |
| `e55c9c2f37c48b0c6d7eaa5eff7794d58adf9b8c` | Complete SmartCard implementation diff and complete SCP device-test diff | Extracts formatters, command/response decorators and reconfiguration graph; response assembly stays inside SCP; SELECT rebuilds plain graph; initialization uses configured formatter. Tests change to three configuration modes. Other applet/CLI files in this large patch are out of this bounded mechanism read. |
| `8706337cc0682ccffd6baae6563e02d4bf993036` | Complete new `yubikit/core/scp03.py` diff | September 26, 2023 author metadata; `_derive` exact layout, encryption padding, MAC-chain update and response-counter construction already present. No claim to first protocol or hardware implementation. |
| `7fd50da5db6f844f49947ce888c8ffab9b8e4eff` | Complete `yubikit/core/scp03.py` diff | May 3, 2024 prototype: b uses ephemeral OCE twice, fixed usage/type/length, 80/88 command, X9.63 SHA-256 five-key derivation and receipt verification. Prototype status remains explicit; no a/c authority inferred. |
| `a10d3b3b249c6dcdba3691824e24758d40994647` | Complete rename/refactor `yubikit/core/scp.py` → `core/smartcard/scp.py` diff | May 8, 2024 author metadata; parameter dataclasses and SendApdu callback replace protocol coupling, unused keyInfo/diversification retained, ordered returned TLVs and full receipt transcript. Not an original GP standard. |

Patch-file hashes are rechecked against C's shared manifest and retained in B6's final
source manifest. Nonselected CLI/applet hunks are explicitly excluded from history
claims, not silently counted read. OATH floor/reset history remains C/boundary evidence:
`1b87964ba258e3ce39fd2959888dfe7cf81a6843` identifies the policy introduction, with no
new hardware-rationale explanation. This finishes B6's bounded history task rather
than requesting an unbounded ancestry search or whole sibling SDK audit.
