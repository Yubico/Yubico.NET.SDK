# PIV final coverage corrections — independent content re-review

**PASS — both required coverage corrections and the dependent README clause are resolved. All 59
current post-format product paths are accepted for content at `evidence/review-accepted-v4.json`.**
No additional correction or optional rewrite is requested. Older accepted manifests and reviews remain
historical, immutable checkpoints.

Independent reviewer: GPT-6.1 Sol, November 5, 2026. Baseline:
`727fe43afc430c5d8364d868a6436de50626e476`. Independently verified editor correction record
`edit-final-coverage-corrections.md`, SHA-256
`f4575863b2ab2317becad96d9f01ab3474e8692cb97965abce9e8f48c845f761`.

## Complete-unit verdicts

| Unit | Independent adjudication |
| --- | --- |
| `PivExamples/Results/KeyGenerationResult.cs` | **Accepted.** Read the complete record, property XML and factory/storage body against the actual key-generation producer. The producer calls ExportSubjectPublicKeyInfo and supplies its bytes to Succeeded. PublicKey now identifies helper-produced DER SPKI. Factory remarks correctly say that it retains the supplied memory view and performs no encoding validation, then direct callers to supply DER SPKI. This separates the expected task format from what arbitrary user construction actually enforces; no copying, ownership transfer or validation was invented. |
| `PivTool/README.md` JSON task section | **Accepted.** The qualifier explicitly belongs to **key generation's** publicKeyBase64. The serializer base64-encodes the producer's SPKI bytes. Slot-info's identically named nested metadata field still base64-encodes SlotMetadata.PublicKey raw PIV TLVs; neither this correction nor its grammar extends the SPKI claim to that field. Existing raw-metadata contracts, operation-specific crypto limits and result ownership remain intact. |
| `Cryptography/PrivateKeyOperations.cs` DecryptAsync/ModPow units | **Accepted.** The comment now leads with the b < mod invariant and explains deterministic fit for valid, same-width PKCS#1 v1.5/OAEP encryption blocks with a leading 00 below a full-bit-length dummy modulus. It distinguishes rejecting raw b >= mod before modular reduction from subsequent host unpadding. The caller creates the same-size dummy RSA key, calls ModPow, then dummy.Decrypt; the guard, exception and finally cleanup match that explanation. The removed probability assertion is absent. The unchanged runtime “Retry the operation” string remains subject to existing PIVIMP-007 error/validation work and is not endorsed as a general recovery instruction. |

The representation argument remains the one checked against retained RFC 8017 §7.1.1 step 2.i and
§7.2.1 step 2.b in the preceding review: valid k-byte encryption blocks satisfy
`b < 2^(8(k−1))`, while a full 8k-bit modulus satisfies `mod >= 2^(8k−1)`. Raw/malformed outputs
need not meet the leading-zero premise. This adds neither a probability bound nor complete response
validation or a new normative profile claim. No new specification fetch or Core implementation audit
was needed.

## Four candidate-associated files reconciled

The prior four-file closure now has explicit final dispositions:

- **KeyGenerationResult:** required output-format XML corrected and accepted here.
- **PrivateKeyOperations:** required false probability rationale corrected and accepted here.
- **CertificatesMenu:** unchanged accepted commentary; already qualified GUI export/CSR/ownership
  limitations remain deferred under PIVIMP-006.
- **DataObjectProtocol:** unchanged accepted commentary; public XML/guide disclose raw object-ID range
  handling and PIVIMP-010 retains the request-validation decision.

Both unchanged files retain the exact baseline identities recorded in `review-unchanged-candidates.md`.
All ten PIVIMP and three PIVCORE identities/dispositions remain; no new runtime-bug count follows.
The correction preserves the user's affirmative, actor/behavior-first style and useful conditions.

## Fresh static reconciliation

Read-only Python checks independently establish:

- The authored product scope is **59 unique paths / 50 C# paths**. Of the prior v3 57 paths, only the
  assigned tool README changed; the other 56 match their accepted v3 bytes exactly. The two added C#
  paths match their current post-format identities below.
- Each retained raw pre-format content string hashes to its receipt in
  `validation/late-pre-format-content.json`; removing whitespace from raw/current versions yields
  identical content for both new C# paths. The original 48-path transformation chain remains unchanged.
- Across all 50 C# paths, ordered non-comment, nonblank source lines match the immutable baseline
  after trimming indentation/trailing whitespace. This preserves executable bodies, assertions,
  declarations/signatures, imports, directives and strings for the observed formatting/comment diff.
  All **354 XML documentation blocks** parse without errors.
- All **61 original complete-body hashes** match the original ledger. All **60 current complete
  bodies** match accepted identities, including the corrected F9 and CLAUDE annotation bodies.
  The new edits contain no examples and the README change is outside its command fences.
- Sampling manifests contain 51 initial, ten test expansion and three internal-prose expansion
  distinct units: **64 total**. Every recorded sampled-file hash still matches its current source.
  The new internal expansion manifest SHA-256 is
  `d4a74cd52e1de3574421570845772d38a02902e299c110f237a24518c74addea`.
  Semantic sampling remains coordinator-owned; the two comment-free backend context reads add no
  sampled prose unit. No additional miss is reported.

Current corrected post-format identities:

```text
src/Piv/examples/PivTool/PivExamples/Results/KeyGenerationResult.cs 88d34bce20a922d1f8701252f8cdbb2919b91d19630cee3ac5eebf1ec400e895
src/Piv/src/Cryptography/PrivateKeyOperations.cs 03bc6bb6a8a0bfacefa07317064a7487133df1bf21e2cef2d54fb440f5ac25cb
src/Piv/examples/PivTool/README.md f99a39262fbb13470081c013ce5bc8bdb9a7470ae7e5f22d7c855571e451f71a
```

## Compilation and execution boundary

Coordinator-reported v3 example compilation (45 groups/47 bodies, full 36-source tool, zero warnings)
and the 619 live reference/analyzer identity entries establish their exact **earlier source context**.
They do not automatically validate bytes changed afterward. Preserve those successful contexts and
bind the new final affected compiler capture to the v4 accepted identities; that work remains pending
after this verdict. This content acceptance supplies no new compiler or runtime execution claim.

Existing hardware impact remains unchanged: 86 canonical methods passed with three physical-lane
deferrals; four supplemental PASS and two desired-contract FAIL confirm the same PIVIMP-005 tool
defect; both devices' captured baseline state and original allowlist were restored. P07 and physical
touch/hotplug/cancellation qualification remain deferred/unverified. Comment/prose edits supply no
reason for device reruns or broader firmware/profile/OS qualification.

Only this record and `evidence/review-accepted-v4.json` were authored. No product edits, builds, tests,
hardware operations, formatting, agents or commits were run here.
