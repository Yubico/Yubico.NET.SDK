# C — independent correction re-review

**Verdict: PASS for C documentation at the exact hashes below.** RC-R1 and RC-R2 are resolved;
authorized RC-O1 and RC-O2 are also accepted. No further required or optional corrections remain
from this review. This is semantic documentation acceptance, not compilation, implementation
completion, standards conformance, cryptographic/device qualification or final campaign closeout.

**Reviewer:** GPT-6.1 Sol, same independent reviewer as [review-c.md](review-c.md), not the editor.
**Date/access date:** 2026-11-04. **Checkout:** `/tmp/opencode/ctap2-001-edit-c`.
**HEAD/baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
Read [correction-c.md](correction-c.md), updated [edit-c.md](edit-c.md), and all three corrected files
in full, including unchanged bodies, private helpers, fixture builders and every assertion. The
other **24 changed-file hashes match the original review exactly**; its complete-unit review and
bounded PASS conclusions therefore remain applicable. All three intentionally unchanged C files
remain byte-identical to baseline. The baseline diff still contains exactly the same 27 C paths.

No source edits, builds, SDK tests, hardware/PIN operations, commits, delegation, integration copies
or framework experiments occurred. This new report is the only reviewer-authored output. Original
review/investigation/evidence bodies were not rewritten. The coordinator owns the historical leading
erratum and shared G/F/triage reconciliation; those record updates are not new source-review blockers.

## Correction decisions and full-unit coverage

| ID | Complete re-reviewed unit and actual evidence | Decision |
| --- | --- | --- |
| **RC-R1** | `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs` **1–202**; especially remarks **33–53**, Decode **85–117**, reconstruction **125–146**, Encode **157–184** and SEC1 conversion **189–201**. The public text now explicitly separates draft seed placeholder-65700 / BL=-1 / KEM=-2 / derived ESP256=-9 example agreement from kty2 versus -65537, wrappers versus direct maps and required integer versus optional int/tstr dkalg. Actual decoder still requires int-3 and nested EC2 carriers; Encode still emits kty2 and bstr maps. | **Resolved / PASS.** No unsupported-placeholder inference or full-profile/vendor/production claim remains in this explanation. |
| **RC-R1** | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs` **1–252**, including static patterns, nested-map builder, all five test bodies/assertions; corrected fixture comment **72–74** names the exact agreement/disagreement. | **Resolved / PASS.** No false outer-label disagreement; no new protocol or crypto authority from fixtures. |
| **RC-R2** | Same complete test file; summary **150–160**, full routing body **162–205**. Assignments **171–174** set BL X=AA/Y=AB, KEM X=BB/Y=BC; assertions **199–204** check the first X bytes and all four complete coordinate arrays. | **Resolved / PASS.** Summary exactly matches the synthetic fixture, with useful distinct-role rationale and no device/point/conformance claim. |
| **RC-O1**, authorized | Complete seed-key file; Encode returns XML **151** now says “new byte array containing the encoded COSE map”; actual **WriteStartMap(5)** remains at164. | **Accepted / PASS.** CLR byte array is no longer confused with CBOR array. |
| **RC-O2**, authorized | `src/Ctap2/src/Credentials/GetAssertionResponse.cs` **1–246**, all fields, constructors, both Decode overloads, every switch arm and both fallback getters; User **81–85** now limits passthrough to modeled name/displayName and states icon is skipped. Narrow dependency reread: `PublicKeyCredentialTypes.cs` **339–440**, especially Parse **371–407**, whose switch models id/name/displayName and skips other fields. | **Accepted / PASS.** Original authenticator UV prohibition still includes icon; actual SDK preservation is stated separately. No privacy filter or RP verification is invented. |

Remaining adjacent limits were checked in these full units, not merely inherited from correction prose:

- ARKG alg-before-kty dispatch (including outer99) and direct-map carrier acceptance follow the
  reread `CoseKey.cs:69–234`; nested curve=1 and mathematical point validity remain unchecked.
  Coordinate slots are left-padded and encoding checks SEC1 size/prefix only. Construction borrows
  memory. All constants, writes, assignments and assertions remain unchanged.
- GetAssertion still exposes the exact2.0 singleton allowList exception versus later Required
  descriptor, no request/version-aware recovery, copied byte fields, unchecked count conversion,
  missing secret-result cleanup, ignored unsigned key8 and no complete envelope preservation.
  Received flag/hash/parser fields remain distinct from RP authentication/verification.
- Tests remain codec/layout/routing/roundtrip evidence when executed, not production crypto, valid
  points, profile-wide conformance, firmware errors, previewSign/device or FIPS proof. None ran here.
- Original review's independently checked L3 **Recommendation 25 August 2026 / §6.1.3 BE/BS**
  conclusion remains applicable because `AuthenticatorData.cs` is byte-identical to that reviewed
  hash. No additional L2 citation upgrade or automatic full-conformance claim follows.

## Original authority rechecked independently

Fetched anew, HTTP200, from
[The Asynchronous Remote Key Generation (ARKG) algorithm, draft-bradleylundberg-cfrg-arkg-10](https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html).
Original retrieval SHA256:
`d49a18c81e828982246042580ec043df9df539e476c7405e629ddabf3790adc6`.
Read the relevant §5.1 table and complete public-seed example, §5.2 algorithm tables, §8.3 proposed
registrations, and publication/status text rather than relying on the editor's verdict:

- Published **27 February 2026**, intended status **Informational**, Internet-Draft with stated
  expiry **31 August 2026**; memo permits citation only as **work in progress**, not an RFC/standard.
- §5.1: outer kty TBD **-65537**; table has required BL=-1/KEM=-2 COSE_Key and **optional dkalg=-3,
  int/tstr**. Example explicitly writes **`3: -65700`**, direct maps at-1/-2 and **`-3: -9`**.
- §5.2 Table2 explicitly names **ARKG-P256, TBD placeholder-65700**; Table3 separately names
  **ESP256-split-ARKG, placeholder-65539**, verification algorithm ESP256=-9. §8.3 repeats the
  proposed seed assignment. Proposed registration is not actual registry assignment or approval.

Also retrieved the original
[CTAP2.3 Proposed Standard 26 February 2026](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html)
through Exa and reread **§6.2.2 user response table**: user ID mandatory for discoverable credentials,
entity optional for server-side credentials, identifying name/displayName/icon MUST NOT be returned
without authenticator UV. That actor requirement is distinct from the actual SDK's modeled fields
and from the later native/browser/RP serializer decision. Other independently checked clauses and
retrieval/framework limitations in `review-c.md` remain in force; no framework probe was rerun.

Updated `correction-c.md` and `edit-c.md` correctly withdraw only the false draft-placeholder inference,
retain real profile/validation/ownership differences and distinguish standard/draft/test/device
authority. CTAP-IMP-029 and related deferred decisions are not closed. Per the coordinator's task,
current G/F were informed; this review does not accept their stale worktree prose or substitute for
the final integrated two-module boundary review.

## Preservation checks and limits

Read-only Python/ Git inspection independently confirmed:

1. Parsed the original review's exact27 SHA256 rows and compared actual current checkout bytes:
   exactly **3 correction paths differ, all24 others match**. Current path set equals baseline diff.
2. All **3 intentionally unchanged C paths** compare byte-for-byte with baseline `git show`.
3. Reconstructed each pre-correction file **in memory only** by reversing the five specified
   comment/XML replacements. Every reconstructed SHA256 equals its original reviewed hash. The
   resulting exact review-to-correction diffs contain only those five replacements—no executable
   body, assertion, literal, directive, signature, wire constant or adjacent edit changed.
   This bounded byte/replacement proof is not a Roslyn compiler/token check.
4. `git rev-parse HEAD`: exact baseline above. `git diff --check <baseline>`: **exit0**, no output.
5. Original `review-c.md` still has SHA256
   `28bb21dfcfe61b936f1213fb5860d39490274000fb0750023ea2677ed1a23db8`.

Formal Roslyn token/directive/API preservation, cref/param binding, compiler/analyzer/formatting,
build/unit/docs gates, integration and final G boundary verification remain coordinator work. The
existing zero-snippet classification and real-project fixture prerequisites are unchanged; no new
documentation example or consumer compilation claim was introduced by these comments.

## All27 final reviewed source hashes

“Same” means identical to `review-c.md`; “Corrected” identifies the full units reread above. These
are final hashes **for C review at this checkpoint**, not a commit/remote/integration hash. Every
row has semantic **PASS**, subject to the separate coordinator gates just described.

| Exact repository-relative path | Full file lines | Checkpoint | Final SHA256 |
| --- | --- | --- | --- |
| `src/Ctap2/src/Cbor/CoseKeyWriter.cs` | 1–63 | Same | `8cc48d3f05ae9abede8e9f6ed7af26ebbdb9e339dcc6b08802316b6db0911a03` |
| `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs` | 1–212 | Same | `7a13e008169055255249c08fea67fb1dd035b2996220a428e299f719dd880b8b` |
| `src/Ctap2/src/Cose/CoseAlgorithm.cs` | 1–145 | Same | `415e84ffd2374e3412d0c82c3b9f3439d311d95991d0650ecdd87431c48b0818` |
| `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs` | 1–202 | Corrected | `cda6964cd8372bd805274f57f4d3fde51e00b9dd143becc6d87829888bf820ac` |
| `src/Ctap2/src/Cose/CoseKey.cs` | 1–373 | Same | `029ae79ac85c717e2ae7b183dbf1f409a400c6cb6e0a49436591c16f8890b2b8` |
| `src/Ctap2/src/Credentials/AttestationFormat.cs` | 1–67 | Same | `8616d8f5aca1019068baab89f2cb09ef89dcce087692faacc3a42b71763435ff` |
| `src/Ctap2/src/Credentials/AttestationStatement.cs` | 1–347 | Same | `3324cb868c766b1af7d07d3785aab7c46ba440baa3d6995ed484932dad680a71` |
| `src/Ctap2/src/Credentials/AttestedCredentialData.cs` | 1–136 | Same | `b90ebb9816c2566d96e566f7349879964332f6eb24b9ddeb58fbbb11c5036141` |
| `src/Ctap2/src/Credentials/AuthenticatorData.cs` | 1–274 | Same | `b95dbc4f5299aeb0ccaed297b4dd9e2961c50e0ced079eb5c92381bd7e2041d1` |
| `src/Ctap2/src/Credentials/Ctap2CredentialOptions.cs` | 1–41 | Same | `79fe7cfe2ed2c19c121ec37d3279b58d15d379462ef3b48be811e5b4f9168899` |
| `src/Ctap2/src/Credentials/GetAssertionOptions.cs` | 1–153 | Same | `b2b52436bc4975c2e25e3b8eab12637a1816062a41dd74ee340fa954dcf4a30a` |
| `src/Ctap2/src/Credentials/GetAssertionResponse.cs` | 1–246 | Corrected | `3440938cf36d8db127535daf253e24e4568e2c22480d58205654a7db8369ee99` |
| `src/Ctap2/src/Credentials/MakeCredentialOptions.cs` | 1–176 | Same | `e2230a517910d90f9b82c454f358c706669c146c5c3e11df13dfbb695c076479` |
| `src/Ctap2/src/Credentials/MakeCredentialResponse.cs` | 1–305 | Same | `d3da37b6fd4ba90c003950c984b7ec697e44f4f49fb630bba7fb9c31b6d23bed` |
| `src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs` | 1–440 | Same | `ac31b8bb4dbdd1e51723d44512cd1b0a78ff02d42965dffcccf7e0e747aa869a` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2RequestAuditReproTests.cs` | 1–44 | Same | `3f46fb16918aa97d55a29e58c4dd8a484bd076d6355ab1cadb8d1e1c272f1eac` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2AlgorithmSupportTests.cs` | 1–155 | Same | `50b985d2592082de010d39613ed70aa4cef8e742a7677db46f3f44ba5b8b0307` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EnterpriseAttestationTests.cs` | 1–128 | Same | `49c03604a55b43dcf2765ede7ded2d159881db89ab687a5c58afe326bfd5e588` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2FipsComplianceTests.cs` | 1–100 | Same | `c69eeb635d8bc3bea074ce20264926772d9c2d12c865d8c7cfe8c8def4224521` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2GetAssertionTests.cs` | 1–302 | Same | `0c276eb580767643c1544086614e61693d942e9548e83ab7eb4922bb0f2ac431` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2MakeCredentialTests.cs` | 1–351 | Same | `213b5d57a5382e7ec470e5e353df853d78ccd55c10f1855bd9217c533be3337e` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cbor/Ctap2SessionRequestEncodingTests.cs` | 1–228 | Same | `2c595804a99b7eb5c26a69afdfd656bb4e6d1fe7813aa995add614adbbeb6bce` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs` | 1–252 | Corrected | `0af3c9586c1f0462e0e618e9be4b0acf6b5b24fc3c2118e8fe07195b9c5d5865` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/AuthenticatorDataTests.cs` | 1–150 | Same | `254f264f9db16fbddefb39702785dd909d23458c5a6ccc6809570791cbb64196` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/CredentialResponseTests.cs` | 1–396 | Same | `517f930220231eed69c8afbed1afbfece7da405daa7e224f9f6cc20ccc3b0021` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/PublicKeyCredentialTypesTests.cs` | 1–259 | Same | `06634119b36416adfaad2e93ec3e1170107bd5f88656e787533bdd56de885b77` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/MakeCredentialResponseTests.cs` | 1–81 | Same | `405ecfb6ad36af2d32f7b6ce1312f998ebdbf9323aeb986f514a8d26920f3d78` |

**Next:** coordinator may integrate these reviewed C bytes and run the separately planned preservation,
compiler/docs/unit gates and final boundary review. Reopen affected C units if those bytes or their
behavioral dependencies change. No second-sweep implementation or hardware work is authorized here.
