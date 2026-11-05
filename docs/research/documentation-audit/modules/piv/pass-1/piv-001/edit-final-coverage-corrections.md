# PIV final coverage documentation corrections

## Bounded correction checkpoint

Applied the two required documentation corrections from `review-unchanged-candidates.md` and the
dependent tool README clause. Ready for independent recheck; formatting, final accepted identities
and affected compiler provenance remain with the coordinator. This batch changes exactly three product
paths and adds this record. The authored scope becomes **59 product paths, including 50 C# files**.

Editor: GPT-6.1 Sol, November 5, 2026. Source baseline:
`727fe43afc430c5d8364d868a6436de50626e476`. The exact required-review SHA-256 is
`4889eb9f1d46397251f6ee261e2eb23fc3f946769432f6928407b286fee38476`.
Read the complete affected units, the actual key-generation producer, Core's public
`IPublicKey.ExportSubjectPublicKeyInfo` contract and the retained RFC 8017 passages. No Core
implementation investigation was opened.

These corrections follow the initial acceptance because final coverage review examined two originally
candidate-associated files whose required commentary had remained unchanged. They clarify existing
C003/C004/C008/C009 obligations; they introduce no independent runtime-bug count. CertificatesMenu
and DataObjectProtocol retain the review's unchanged acceptance and existing PIVIMP-006/PIVIMP-010
public qualifications. All ten PIVIMP groups and three PIVCORE groups keep their identities/dispositions.

## Source-backed changes

### Key-generation output format

`KeyGeneration.GenerateKeyAsync` exports the generated public key through
`ExportSubjectPublicKeyInfo()` and passes those bytes to the result. `KeyGenerationResult.PublicKey`
now names the helper-produced **DER SubjectPublicKeyInfo** encoding. The factory remarks explain its
actual storage behavior: it retains the supplied memory view and performs no encoding validation;
callers constructing results should supply DER SubjectPublicKeyInfo. The DTO body is unchanged.

The README's existing key-generation JSON clause now identifies `publicKeyBase64` as base64 of DER
SubjectPublicKeyInfo. This is operation-specific: slot-info's identically named field still carries
raw PIV public-key TLVs from metadata. No JSON field, serializer or metadata contract changed.

### RSA re-wrap guard rationale

The ModPow comment now explains that `b < mod` preserves the raw integer through re-wrapping.
Valid full-width PKCS#1 v1.5/OAEP encryption blocks begin with 00 and fit below a same-width,
full-bit-length dummy modulus. RFC 8017 §7.1.1 step 2.i (`evidence/rfc8017.txt:1255–1259`) and
§7.2.1 step 2.b (`:1580–1589`) establish those encryption encodings. For k-byte blocks,
`b < 2^(8(k−1))` while that modulus is at least `2^(8k−1)`; the fit is deterministic.

The guard rejects raw output at or above the modulus before modular reduction could change its
value; host unpadding validates the encoding afterward. Malformed/raw blocks have no universal
tiny-probability bound. The comment makes no blanket retry or invalid-input guarantee. The guard,
exception text containing “Retry the operation”, RSA operations, parsing, ownership and cleanup remain
unchanged. PIVIMP-007 retains its separate validation/memory/error-handling follow-up.

## Exact changed-path identities

The first two before hashes equal the immutable baseline. The README before hash is its current
accepted v3 identity. These are the only product paths owned by this correction batch.

| Product path | Before SHA-256 | Corrected SHA-256 |
| --- | --- | --- |
| `src/Piv/examples/PivTool/PivExamples/Results/KeyGenerationResult.cs` | `40fc6deada93c639a1f2fcb5e542a87d7f8998859d422e02a68986fd9900fb3b` | `1a6f731e791aadbcf3cc9fe4b0eb395b980086a12e3ed8749eff09715bd641f9` |
| `src/Piv/src/Cryptography/PrivateKeyOperations.cs` | `a7f46ccbe9bf1d2f8839fb7403f5344a07bd700eb4da3b9e021b75ce4245066a` | `234c593aec0edb0ce9150491e01b075db9431873500cb3f3f1a773930595a8d6` |
| `src/Piv/examples/PivTool/README.md` | `7ef84e6d8c2c0306cc9e6678b2814dfb8a86f14325e1ca0481c13e30aaf8ddf3` | `f99a39262fbb13470081c013ce5bc8bdb9a7470ae7e5f22d7c855571e451f71a` |

## Static preservation and complete bodies

Read-only checks confirmed:

- The other **56 current accepted product paths** match `evidence/review-accepted-v3.json` byte for
  byte. Only the assigned README differs within that prior 57-path scope.
- Across all **50 C# paths**, every ordered source line outside full-line comments matches the
  immutable baseline. Executable code, assertions, imports, directives, runtime strings and declarations
  are preserved. All **354 XML documentation blocks** parse with zero errors.
- All **61 original complete-body hashes** match the original baseline ledger. All **60 current
  complete bodies** remain unchanged from the current accepted examples: 42 module and 18 shared.
  The new C# edits contain no code example; the README edit changes prose outside its command fences.
  No body was added, removed or reclassified.
- Scoped `git diff --check` returned exit 0.

An initial body comparison used the first review map and flagged the already accepted CLAUDE annotation
correction. Reconciliation used `review-example-correction.md`'s current fence #6 identity
`4464f14bda4ed114d1cbbb0c3994bc0af74fab338095a7eb6ee9ba5a756d213e` and the previously corrected F9
body identity `c26c99a88224f73c91b9d2821843aec7abd049d7cd4fb36c9618be9a574a0ccc`. The final comparison
has zero current-body mismatches. Neither prior example was edited here.

The original records remain byte-identical:

| Preserved record | SHA-256 |
| --- | --- |
| `edit-report.md` | `60f5ed5a96112b9743cc398d2cad134dc8721315b08371ffb57c2cbffc423eab` |
| `edit-corrections.md` | `50bd6dc6b21a15efba8091ae9813df1261d3741a8f647ab71859da164be8a3f0` |

These are static content/preservation checks, not compiler or execution results. No other product file,
record, assertion, runtime code, help string, import, directive or API snapshot was edited. No build,
test runner, formatter, hardware operation, agent or commit was invoked. Existing tested binaries and
the coordinator's preservation record remain untouched. Existing hardware outcomes and both restored
devices supply no reason for another hardware run; this comment-only batch adds no qualification claim.

The coordinator owns independent recheck, formatting of the two new C# documentation paths, final
59-path token/API/accepted-hash reconciliation and fresh affected documentation compilation/current Csc
capture. This record's own hash is supplied in the handoff rather than embedded recursively.
