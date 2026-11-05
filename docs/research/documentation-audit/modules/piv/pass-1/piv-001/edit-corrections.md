# PIV required review corrections

## Correction checkpoint

Applied PIV-REV-001–003 to the six product paths requested in `review.md`. The corrections are ready
for independent re-review at the identities below. The campaign still has 57 edited product paths,
including 48 C# files; this batch adds no product path. Only this correction record is newly authored.

Editor: GPT-6.1 Sol, November 5, 2026. Source baseline:
`727fe43afc430c5d8364d868a6436de50626e476`. Read the complete review, the six affected units, the actual
F9 helper/result construction and the independent Ed25519 host-verification method before editing.
Loaded the receive-review skill. Retained the approved affirmative, task-oriented voice and nearby
ownership, partial-state and oracle conditions without preference-driven rewrites.

The original `edit-report.md` remains byte-identical, SHA-256
`60f5ed5a96112b9743cc398d2cad134dc8721315b08371ffb57c2cbffc423eab`.
Other records and product paths were left with their owners. No executable code, assertions, imports,
directives, runtime help/output strings or API snapshots changed. No formatter, build, test runner,
hardware operation, agent or commit was invoked.

## Changes and complete-unit checks

### PIV-REV-001 — Actual F9 result and ownership

`GetIntermediateCertificateAsync` calls `AttestationResult.Succeeded(intermediateCert)`. Its first
argument populates `AttestationCertificate`; the optional `IntermediateCertificate` remains null.
The corrected XML reads F9 from the actual populated property and prints its subject on success.
Each returned property retains a single disposal scope; the example introduces no disposing alias
for a certificate selected through a fallback. The first, slot-attestation example remains unchanged
and also disposes both returned properties.

The result property's summary now covers both slot attestation and this helper's F9 signer result.
The tool README tells JSON consumers that `attest-intermediate` puts F9 metadata in
`attestationCertificate`, leaving `intermediateCertificate` null. The actual helper, record construction,
CLI fallback and JSON serializer remain unchanged. This corrects documentation of the existing result
shape rather than changing the runtime contract.

### PIV-REV-002 — Partial credential changes

The complete `SetPinAttemptsAsync` contract retains management authentication, verified PIN, the
factory PIN/PUK values and the warning that unchanged limits still reset both credentials. Its final
sentences now explain that subsequent failures can leave **one or both** at defaults and direct callers
to inspect state and complete the remaining changes before deployment. Successful changes remain
applied independently. The concrete facade still inherits this interface contract through its existing
`inheritdoc`; neither facade behavior nor the README's accepted recovery-plan wording changed.

### PIV-REV-003 — Local crypto oracles

The complete P-256 method hashes with SHA-256 and verifies the returned DER signature with the
generated public key and that same digest. Its XML now describes that actual oracle, with no
SHA-384/truncation claim. The method name, setup, body and assertions remain unchanged.

The Ed25519 comment now describes the selected short/padded and long/common-prefix differential
checks plus repeatability. It explains why these catch the specified 32-byte padding/truncation
regressions while a deterministic hash-first implementation can also distinguish the pairs. It points
to `PivAuditHardwareReproTests.YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages` in
`AuditV2/PivAuditHardwareReproTests.cs` for the separate OpenSSL message/key-verification oracle.
That method verifies 10- and 64-byte original messages using the generated public key; the comment
adds no new execution claim. All differential assertions and the genuine X25519 TODO remain unchanged.

## Exact product identities

“Before” is the first independent review/editor checkpoint, rather than the immutable source baseline.
The baseline byte identities remain in the preserved original edit report. Only these six identities
changed relative to `evidence/review-identities.md`; the other 51 product paths match that checkpoint.

| Product path | Before SHA-256 | Corrected SHA-256 |
| --- | --- | --- |
| `src/Piv/examples/PivTool/PivExamples/Attestation.cs` | `92f56dcdd52388c152d294317baad1a01313725412a72fc63be31084dfe492b8` | `7aa99e2420e2696e57df9a0551bcd562539737f9f7900d5ecc1b5c83302a3666` |
| `src/Piv/examples/PivTool/PivExamples/Results/AttestationResult.cs` | `ca85a55a84155f04e6fba8d0cb83fd513d41963fb2698433535fced3f66bb6e8` | `13d915cd6e6080f333c82ec2aef4f2dbbd27938d79bdd2c0c6bed36b8d33d2ab` |
| `src/Piv/examples/PivTool/README.md` | `e44b2ea2df953a049694f5f7cf6a835328f23389ede05bd87e9759428d9fccb3` | `7ef84e6d8c2c0306cc9e6678b2814dfb8a86f14325e1ca0481c13e30aaf8ddf3` |
| `src/Piv/src/IPivSession.cs` | `4638725e045f4e43c8214e835695a01fb5d00c258979a4a5468171809ea69c08` | `d049f8aeff1bfa648f9ef7dc83c99264e2b920bc6e926d2b1ca930ae52f346c5` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSigningAlgorithmTests.cs` | `005aa8fa8fa59c4f566f720297a80820c07d4a84eea0b299f125713b1e66df75` | `0682b32b1cdb77292af9d9a758b0eddccca1e733ab4497183837bd4ab8cbfa10` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs` | `822f4ff418c328551bad9527fb9399dd6bb5410bd9165d18155ff347391e472b` | `098e223b1c36936b86308dec7b4be2a9bf9bcb2601427a709ec81c9020f410c9` |

## Complete-body reconciliation

Re-extracted the reviewed body inventory using the same LF-preserving Markdown/XML hashing rule:
Markdown hashes its complete fence body; XML removes indentation plus the `/// ` prefix, decodes XML
entities and joins the complete code body with a final LF. Original and current ordinals remain separate.

- All **61 original complete-body hashes** still match the immutable baseline and original ledger.
- The current inventory remains **60 bodies**: 42 module bodies plus 18 scoped shared bodies.
- Exactly one current body changes: `src/Piv/examples/PivTool/PivExamples/Attestation.cs#2`.
  The other 59 match the first review's current-body map. The tool README correction is prose;
  its two command bodies remain unchanged. No body was added or removed.
- The changed F9 body remains a **composable fragment** requiring an open real `IPivSession`,
  `CancellationToken ct`, the real PivTool helper and Console. It retains a single owner for each
  returned certificate property. It makes no attestation-chain trust or slot-binding decision.

| Body identity | Before SHA-256 | Corrected SHA-256 |
| --- | --- | --- |
| `src/Piv/examples/PivTool/PivExamples/Attestation.cs#2` | `3ce3c2cb10f875d8b1a14ba49a62357c04acf23ff36e5851b57f174f95c6c963` | `c26c99a88224f73c91b9d2821843aec7abd049d7cd4fb36c9618be9a574a0ccc` |

Complete corrected body, with XML entities decoded:

```csharp
var result = await Attestation.GetIntermediateCertificateAsync(session, ct);
// This helper returns F9 in AttestationCertificate; IntermediateCertificate remains null.
using var f9Cert = result.AttestationCertificate;
using var additionalCert = result.IntermediateCertificate;
if (result.Success && f9Cert is not null)
{
    Console.WriteLine(f9Cert.Subject);
}
```

## Static verification and coordinator handoff

Read-only Python/subprocess checks compared the complete 57-path review map: only the six requested
paths differ. For all 48 C# paths, every ordered non-full-line-comment source line still matches the
immutable baseline, preserving executable code, assertions, imports, directives and strings. All
339 XML documentation blocks parse with zero errors. All 61 original-body hashes match; current-body
comparison finds only the intended F9 example change. Scoped `git diff --check` returned exit 0.
These are static preservation/content checks, not compilation or device execution.

The coordinator owns independent re-review, expanded internal/test clean sampling after PIV-REV-003,
any final denominator changes, compilation/capture corrections and hardware/status reconciliation.
The supplied sampling checkpoint is 51 sampled of 385 nominees, pending the required expansion.
No sampling or evidence record was edited here.

The supplied hardware checkpoint reports the canonical unattended batch as 83 PASS, three multi-key
skips, zero warnings, with cleanup PASS. Supplemental P01 built, but its capture failed an
EnableNETAnalyzers flag assumption before any device operation; the tooling correction and six
supplemental unattended probes remain with the coordinator. Physical touch/hotplug and P07 are
deferred by the user. This correction batch adds no X25519 execution/qualification claim and leaves
the existing PIVIMP/PIVCORE labels and dispositions unchanged.

This record's own SHA-256 is supplied in the handoff rather than embedded recursively. The independent
reviewer/coordinator should pin the six corrected products and this record before release.
