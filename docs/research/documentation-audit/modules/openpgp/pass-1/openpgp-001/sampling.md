# Coordinator clean-unit sampling

Astra, 2026-11-04. Investigator nominated40 deduplicated clean units. Editor removed seven overlapping
units, leaving33 before independent review. The editor's material PinPolicy.Once miss triggered expanded
API sampling. Coordinator read12 complete files/units across API, narrative, example and internal-test,
with11 clean and one substantive timestamp-contract miss confirmed by independent review. The final
population is **32 eligible units**, with **11 accepted clean samples**, including eight API units.
The timestamp unit was corrected and removed from the clean denominator. Exact hashes/population are
in sampling-final.json; all accepted sample bytes still match the initial baseline.

## Expanded sample

| Whole-file unit | Independent check | Result |
| --- | --- | --- |
| GenerationTimes.cs | Read collection and local four-byte parser; Card3.4.1 CD table says zero=unspecified, while signed-int decoding needs raw-bit interpretation. Reviewer independently confirmed RFC4880§3.5 unsigned encoding. | PGP-C022/PGP-REV-005 corrected and re-reviewed; excluded from final clean sample |
| KeyInformation.cs | Public dictionary interfaces/owned internal storage match slot/status mapping; no provenance attestation promised. | PASS |
| SupportedAlgorithm.cs | One immutable slot/attribute reference per entry; supports repeated slots in enclosing list rather than a uniqueness claim. | PASS |
| IYubiKeyExtensions.cs | Read whole factory/borrowed internal overload; public path resolves SmartCard, initializes and marks connection owned; caller owns returned session. Inherited lifecycle questions stay Core dependencies. | PASS |
| RsaImportFormat.cs | Standard/CRT and optional modulus values match Card3.4.1§4.4.3.9; enum contract distinct from PGP-IMP-014's actual import choice. | PASS |
| KeyFingerprints.cs | 20-byte per-slot dictionary contract matches local parser's complete-slice copies; public read-only access and host-managed fingerprint guidance agree. | PASS |
| EcImportFormat.cs | Private-only and FF public-included format identifiers match Card3.4.1§4.4.3.9. | PASS |
| ExtendedLengthInfo.cs | Read parser and Card3.4.1§4.1.3.1; two02 uint16 values are complete command/response APDU byte maxima, including SM. Descriptor reporting does not claim transport enforcement. | PASS |
| KeyStatus.cs | None/Generated/Imported values0/1/2 match Card3.4.1 key-information table. | PASS |
| PACKAGE_README.md | Accurate module tasks, transitive Core dependency, scoped alpha status and documentation links; C017's encryption overclaim belonged to csproj Description instead. | PASS |
| DigestInfoTests.cs | Read complete test and DigestInfo helper; four DER headers match RFC8017§9.2 notes; concatenation test checks its actual length/endpoints rather than cryptographic execution. | PASS |
| AccessChangePinCommand.cs | Read complete body/options; supplied-or-prompted current/new PIN wrappers are disposed and actual ChangePin call precedes success output. Wider immutable-input limitations are documented in the tool guide. | PASS |

After the timestamp question, four additional API files were read to expand beyond the first five
API selections. Source checks used existing original excerpts; no Core source or other-module body was
investigated. No additional material miss was found in that expansion. The final reconciliation passed
all11 sample identities and32 eligible units; minimum required was four. The original PinPolicy and
GenerationTimes misses remain documented alongside their independently accepted corrections.
