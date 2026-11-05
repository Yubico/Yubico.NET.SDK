# PIV required-correction independent re-review

**PASS — PIV-REV-001–003 are resolved for documentation content. All 57 authored product paths are
accepted at the current, pre-formatter identities in `evidence/review-accepted.json`.** No further
required correction or optional rewrite is requested. This supersedes the first review's six-path
content hold; its historical observations and identities remain preserved.

Reviewer: independent GPT-6.1 Sol, November 5, 2026. Baseline:
`727fe43afc430c5d8364d868a6436de50626e476`. Independently verified correction report SHA-256
`50bd6dc6b21a15efba8091ae9813df1261d3741a8f647ab71859da164be8a3f0` and unchanged original editor-report
SHA-256 `60f5ed5a96112b9743cc398d2cad134dc8721315b08371ffb57c2cbffc423eab`.

## Complete-unit adjudication

| Review group | Rechecked behavior, dependent claims and outcome |
| --- | --- |
| PIV-REV-001 | Read both complete Attestation XML examples and helper bodies, the complete result record, tool guide, actual CLI F9 fallback and JSON serializer. The F9 helper supplies the first `Succeeded` argument, populating `AttestationCertificate` and leaving `IntermediateCertificate` null. The corrected example reads and displays that populated property. Its two using declarations own the returned certificate and null optional property respectively, with no fallback alias or double-dispose of the same certificate. The slot-attestation example remains coherent. Result XML and JSON guidance now describe the actual F9 field. The class's caller trust-policy/key-binding responsibilities remain; this change adds no manufacturer-root validation claim or runtime change. **Accepted.** |
| PIV-REV-002 | Re-read the complete retry-limit contract, neighboring credential contracts and concrete facade delegation/inheritance against the separate protocol operations reviewed previously. SET PIN RETRIES still restores both public defaults even for unchanged limits. Subsequent independent changes can leave one or both credentials at defaults. The revised contract now says this and directs inspection/completion before deployment, retaining management authentication and verified-PIN requirements. Existing README partial-state/recovery guidance remains consistent. **Accepted.** |
| PIV-REV-003 | Re-read the complete P-256/SHA-256 method and the Ed25519 differential method, setup, assertions and neighboring X25519 TODO. The P-256 XML matches SHA256.HashData and DER host VerifyHash against the generated key. The Ed25519 comment now claims the selected 32-byte padding/truncation checks and repeatability only, explicitly allowing the hash-first counterexample. Independently followed its exact reference to the complete AuditV2 test and OpenSSL helper: the test verifies original 10- and 64-byte messages against the generated public key and asserts the verifier's successful exit. Its prerequisite and oracle remain distinct from this local differential test and from execution status. RFC 8032 PureEd25519 message/signature semantics remain consistent with the previously accepted original-citation review. **Accepted.** |

All six corrected paths were independently rechecked at their actual current hashes. The other 51
paths match the first accepted content checkpoint exactly. The correction preserves the approved
affirmative task/actor/behavior-first voice and useful local conditions without editorial churn.
Original consequential citation adjudication in `review.md` remains accepted; these corrections add
no new standards revision, federal-profile adoption or device qualification claim.

## Fresh static verification

Read-only Python/subprocess checks establish:

- Exactly six of the original 57 paths changed after the first review; all six match the correction
  report. All 51 previously accepted path identities remain unchanged. The accepted JSON enumerates
  every actual current path/hash and distinguishes re-reviewed corrections from unchanged acceptance.
- Every ordered non-full-line-comment source line in all 48 C# paths still matches the immutable
  baseline. Declarations/signatures, executable bodies, assertions, imports, directives and runtime
  strings remain preserved. All 339 XML documentation blocks parse without errors.
- All 61 original complete-body hashes match. The current count stays 60; only
  `src/Piv/examples/PivTool/PivExamples/Attestation.cs#2` changed, to
  `c26c99a88224f73c91b9d2821843aec7abd049d7cd4fb36c9618be9a574a0ccc`. The other 59 current body
  identities are unchanged. The corrected F9 body remains a composable real-helper fragment, with
  an open `IPivSession`, token and explicit returned-certificate disposal.
- Coordinator sampling records contain 51 initial plus 10 expanded distinct samples, across 15 paths.
  Independently checked every sampled file against its recorded exact file hash: unchanged. Sampling
  semantic selection/read conclusions remain coordinator-owned; no additional sample miss is reported.
  Initial manifest SHA-256: `35c82fe7b36172a97d2575a1ee9527543d3a51f40de23f608b64d54ca98aece8`;
  expansion SHA-256: `d491cbf44bb343a5715800bf331ee2cb691b5088e5e235e0958a638ddbcd371f`.

## Scope and next gates

This is content acceptance, not compilation, formatting, execution, implementation completion or
publication. This reviewer changed only this record and `evidence/review-accepted.json`; no product
edit, build, test, hardware operation, formatter, agent or commit was run. The coordinator's next
whitespace-only formatting/source-preservation checks and complete-example/full-tool extraction
must reconcile resulting hashes against this accepted content checkpoint.

Supplied current hardware status remains canonical existing-batch **83 PASS, three multi-key skips,
zero warnings, cleanup PASS**. Supplemental P01 built, but its capture guard failed an
EnableNETAnalyzers assumption before hardware execution; no P01 device result is inferred. Pending
supplemental execution does not block this content verdict. Physical touch/hotplug and cancellation
hardware qualification remain unverified under the user's physical-coordination deferral. Reconcile
late outcomes separately without upgrading these claims. Stable PIVIMP/PIVCORE dispositions remain
unchanged; no runtime fixes or Core implementation audit are included.
