# PIV unchanged candidate-associated files — bounded disposition review

**Required documentation corrections: two additional C# paths, plus the existing tool README's
dependent output-format clause.** CertificatesMenu and DataObjectProtocol can remain unchanged with
their existing public qualifications and deferred engineering dispositions. No optional style rewrite
or runtime fix is requested.

Independent reviewer: GPT-6.1 Sol, November 5, 2026. Baseline:
`727fe43afc430c5d8364d868a6436de50626e476`. Read complete relevant semantic units and narrow producer,
consumer and already-retained RFC passages. Read-only byte comparison confirms all four paths below
are still identical to that baseline. This review does not expand into Core implementation.

## Explicit four-file dispositions

| Path | Disposition | Supported reason |
| --- | --- | --- |
| `src/Piv/examples/PivTool/Cli/Menus/CertificatesMenu.cs` | **Unchanged accepted with deferred PIVIMP-006 limitations.** | The export method reads/writes `result.CsrPem` after the certificate-export helper, and CSR generation omits PIN verification. Those are current runtime/sample defects already explained in the tool README, which directs readers to CLI CSR/export or direct SDK tasks. The menu's summary and local authentication comments add no contrary promise. Certificate ownership gaps remain in the same deferred group. Changing executable handling, output strings or adding repetitive warnings here is unnecessary in this documentation-only disposition. |
| `src/Piv/examples/PivTool/PivExamples/Results/KeyGenerationResult.cs` | **Required output-format XML clarification, C009 (with C004 format distinction).** | `PublicKey` says only “public key bytes.” The real helper exports `ExportSubjectPublicKeyInfo()` and passes that encoding to `Succeeded`; the CLI serializes exactly those bytes. Consumers need the concrete encoding to import/use the result. A generic DTO shape does not eliminate the task contract of the SDK example result. The currently unspecified format leaves SPKI versus raw PIV metadata TLV inference to callers. Clarify the property for helper-produced results and the existing README JSON field description; leave DTO storage/constructor behavior unchanged. |
| `src/Piv/src/Cryptography/PrivateKeyOperations.cs` | **Required comment correction, C003/C008 within existing PIVIMP-007.** | The ModPow guard's purpose is sound, but its “astronomically unlikely” claim is unsupported for the generic raw output accepted here. Correct the mathematical explanation without changing guard, exception string, RSA operations, parsing, ownership or cleanup. Detailed reasoning follows. |
| `src/Piv/src/DataObjects/DataObjectProtocol.cs` | **Unchanged accepted with deferred PIVIMP-010 limitation.** | EncodeObjectId casts/truncates arbitrary integer inputs into one/two/three wire bytes; no request-range validation exists. Existing public GetObject/PutObject XML and README already disclose the unguarded range and direct callers to defined object identifiers. Internal comments describe the TLV construction and contain no claim of range enforcement. Keep implementation/range-policy changes deferred. Existing owned response-copy/clear and write-buffer cleanup descriptions remain coherent with the complete methods. |

## Required output-format correction

`KeyGenerationResult.cs:39–42` currently states:

> Gets the public key bytes when successful.

`PivExamples/KeyGeneration.cs:77–84` receives the actual `IPublicKey`, calls
`ExportSubjectPublicKeyInfo()`, and passes those bytes to the result. `JsonOutput.cs:97–109` emits
the same bytes as `publicKeyBase64`. The result factory stores a caller-supplied memory view, so the
documentation can qualify the producer without inventing validation in that factory. Suitable
replacement meaning:

> Gets the DER-encoded SubjectPublicKeyInfo returned by KeyGeneration.GenerateKeyAsync on success.

If documenting the factory input locally, preserve the distinction between its expected encoding
and actual validation; no new runtime guard is requested. The property clarification is required,
while duplicating it on every accessor is unnecessary.

Reconcile `src/Piv/examples/PivTool/README.md:85` alongside it, identifying key generation's
`publicKeyBase64` as base64 of DER SubjectPublicKeyInfo. This is a material output contract rather
than a naming preference: slot-info metadata also has a `publicKeyBase64` field, but its source is
`SlotMetadata.PublicKey`, the raw PIV public-key TLV encoding. Keep those operation-specific meanings
distinct; the already accepted metadata documentation remains correct. No JSON field/body changes
or universal promise about arbitrary user-constructed DTO instances is requested.

## Required ModPow comment correction

`PrivateKeyOperations.cs:240–243` explains that re-wrapping needs `b < mod`, then says:

> This is astronomically unlikely with a fresh random modulus of equal bit length ...

The antecedent is a raw output integer at or above the dummy modulus. Rechecked retained RFC 8017:

- §7.1.1, step 2.i (`rfc8017.txt:1255–1259`) defines EME-OAEP as
  `EM = 0x00 || maskedSeed || maskedDB`, length k octets.
- §7.2.1, step 2.b (`:1580–1589`) defines EME-PKCS1-v1_5 as
  `EM = 0x00 || 0x02 || PS || 0x00 || M`, also length k.

For a valid k-byte block of either encryption encoding, the leading 00 bounds its unsigned big-endian
integer below `2^(8(k−1))`. A full-width 8k-bit dummy modulus is at least `2^(8k−1)`. Thus **valid
full-width encoded blocks are deterministically below that modulus**, independent of how its primes
were sampled. This is a representation argument, not a probability estimate or a new federal-profile
claim. It concerns encryption padding here, not EMSA signature encoding.

Malformed/raw blocks have no such leading-zero guarantee. The current crypto-response parser checks
TLV tags and returns their value without full width/padding validation, so those inputs can reach
ModPow. There is no universal tiny probability of `b >= mod`; an appropriately wide all-FF value,
for example, exceeds any modulus of that width. The existing guard rejects before modular reduction
would change the integer. It is not complete padding/response validation and should not be described
as such. The later host unpadding still performs its own checks.

Suitable replacement comment meaning, leading with behavior and the local reason:

```text
The re-wrap step requires b < mod to preserve the original integer through exponentiation.
Valid full-width PKCS#1 v1.5 and OAEP encryption blocks begin with 00 and fit below a
same-width full-bit-length dummy modulus. Reject raw output at or above the modulus before
modular reduction could change its value; host unpadding validates the encoding afterward.
```

Retain the guard and existing exception string untouched in Pass 1. In particular, correcting this
comment does not endorse retrying malformed ciphertext just because the runtime message mentions
retry, and it does not settle PIVIMP-007's separate memory/validation questions.

## Scope and next acceptance

These are supported omissions/false commentary, so late timing does not justify accepting them as
clean. Adding the two required C# documentation paths raises the authored scope from **57 to 59
product paths and 48 to 50 C# paths**; the tool README is already in scope. Reconcile coverage,
candidate dispositions, accepted identities and compiler provenance accordingly. The existing ten
PIVIMP groups retain their stable identities and counts; these are documentation obligations within
those groups, not two new independent runtime bugs.

Coordinator-reported current v3 examples and final verification remain evidence for their exact
prior inputs. Preserve those contexts and outputs before editing, then re-review the corrected
complete units and bind affected compilation to the new final source hashes. A comment/XML-only
change supplies no reason to repeat device behaviors; hardware outcomes and physical deferral
remain unchanged. This record requests no build/test/hardware execution.

## Input identities

```text
src/Piv/examples/PivTool/Cli/Menus/CertificatesMenu.cs 726f0a38fe528da4ff3db34021900126f94253f0331db772621bebde79a7c39f
src/Piv/examples/PivTool/PivExamples/Results/KeyGenerationResult.cs 40fc6deada93c639a1f2fcb5e542a87d7f8998859d422e02a68986fd9900fb3b
src/Piv/src/Cryptography/PrivateKeyOperations.cs a7f46ccbe9bf1d2f8839fb7403f5344a07bd700eb4da3b9e021b75ce4245066a
src/Piv/src/DataObjects/DataObjectProtocol.cs 6c2d4917f5b40d5734678b243b4b5faa873640a79fefaf9d3cb41ca2182c6bb2
```

Only this record was authored. No product files, prior reviews, manifests or archives were edited;
no builds, tests, hardware, formatting, agents or commits were run. No optional prose churn is requested.
