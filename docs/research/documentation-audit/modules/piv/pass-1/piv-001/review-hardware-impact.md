# PIV late hardware content-impact review

**PASS — no required late correction to the accepted product documentation or PIVIMP/PIVCORE
dispositions.** The new evidence supports the scoped SDK claims and confirms the already documented
PivTool signing defect. Keep its two desired-contract failures visible. No optional rewrite is requested.

Independent reviewer: GPT-6.1 Sol, November 5, 2026. Baseline:
`727fe43afc430c5d8364d868a6436de50626e476`. This bounded follow-up checks the accepted claim map
against the six actual supplemental oracles, recorded outcomes and final restoration. It does not
repeat the module/specification audit or constitute final evidence acceptance.

## Accepted-content continuity

Read-only identity checks link the previously accepted 57 paths through the 48 recorded whitespace-only
C# transformations to all 57 actual final file hashes. Each transformation retains the recorded imports
and names the accepted input hash; the other nine paths retain their accepted bytes. No new content
change is indicated by this chain. Coordinator records:

- `validation/format-transform.json`, SHA-256
  `83c44161061686576e738fd55d99d5b067d3ed7d971c69f0cc7db7537b57508d`.
- `reviewed-files.json`, SHA-256
  `f3d53c24a507c47d99705c7ce53baf8c0f29b828511fd6e9bb3e16fbd7867602`.

## Actual supplemental outcomes and claim impact

Read each `validation/hardware-probe/evidence/p0*-v4/result.json` and substantive
`probe-events.jsonl` event against the complete six methods in `HardwareProbeTests.cs` and their
verification helpers. Each case executed once with zero skips, build/capture/cleanup exit 0 and zero
recorded warnings. Recorded runtime assembly hashes match compiled identities, and pre/post-test
binary identity maps match. This ties the actual sample failures to PivTool rather than an invented
replacement helper. Full compiler-closure/provenance acceptance remains a separate coordinator gate.

| Case | Actual status and oracle | Accepted claims and ledger impact |
| --- | --- | --- |
| **P01 X25519** | **PASS.** Imported RFC 7748 Alice scalar produces the expected public u-coordinate in slot 9D. The 32-byte raw result for Bob's public key equals the RFC expected secret. A fresh Python/vendor session on the same installed key, before cleanup, independently reports matching Alice public bytes and RFC agreement. | Supports `IPivSession.CalculateSecretAsync` and README raw-agreement/KDF responsibilities for this device/transport. This is imported-key known-vector agreement, not an all-firmware or all-generated-key qualification. The existing generation-only integration method still has its narrower oracle and genuine TODO; PIVIMP-008's shipping-test follow-up remains accurate. |
| **P02 PIN policy** | **PASS.** First Always signature verifies on the host. A second operation without re-verification produces no output and throws `InvalidOperationException`; the strict assertion checks its “Security status not satisfied” message. Re-verification restores a host-verified signature; Once permits two host-verified signatures. | Supports the current Once/Always policy descriptions. **6982 is source-mapping provenance, not an independently captured raw status for that rejected operation.** The original one-signature policy method remains narrower than this supplemental probe; no test-body fix or PIVIMP-008 closure follows. |
| **P03 keys/certificates** | **PASS.** Two software-issued certificate DER encodings, with a public key unrelated to the device-generated key, remain in 9A/82 through key move and deletion. Source/destination metadata, moved-key SPKI equality and host signature verification establish the separate key transition. | Supports README and interface key/certificate independence and the caller's enrollment-binding responsibility. Supplementary binding evidence does not strengthen the original weak move method's assertions or close its shipping-test follow-up. |
| **P04 attestation** | **PASS.** The returned certificate's RSA/SHA-256 issuer signature verifies using the preserved F9 public key, and generated/metadata/attested SPKIs match. F9 certificate DER remains unchanged. | Supports retrieval plus issuer/key-binding guidance. **Manufacturer-chain trust was not evaluated.** The application still validates its chosen trust chain and binds the intended slot. Existing weak attestation-test commentary and PIVIMP-008 remain accurate for those unchanged tests. |
| **P05 actual tool RSA** | **FAIL — expected desired-contract failure**, executed 1, failed 1, skipped 0, test exit 1. The actual tool reports Success=true, but its result fails original-message SHA-256/PKCS#1 verification. The direct SDK control with complete host EMSA encoding verifies. Caller input bytes remain unchanged. | Confirms existing **PIVIMP-005**, C003/C009, tool README and Signing XML: RSA needs host EMSA framing omitted by the generic hash-first helper. Preserve the failing original-message assertion; the positive direct-SDK control and successful harness/cleanup do not turn this product-contract failure into a pass. |
| **P06 actual tool Ed25519** | **FAIL — expected desired-contract failure**, executed 1, failed 1, skipped 0, test exit 1. OpenSSL verifies the direct SDK signature over the original message, rejects the actual tool signature over that message, and verifies that same tool signature over SHA256(message). Caller input bytes remain unchanged. | Confirms the same **PIVIMP-005** group: the tool signs its hash rather than the original Ed25519 message. This strengthens the accepted local-versus-independent-oracle distinction. Preserve the strict failed original-message assertion; no runtime fix or separate new defect count is implied. |
| **P07 cancellation/fresh recovery** | **DEFERRED / NOT RUN** under the user's physical-coordination deferral. No cancellation-with-later-touch/recovery result exists. | Retain cooperative-cancellation guidance and PIVCORE-002's native interruption/drain/serialization questions. Unattended completion and a timeout token establish no physical cancellation guarantee. |

**Supplemental total: four PASS, two FAIL (expected defect confirmations), P07 deferred.** The two
failures retain the intended assertions; neither was weakened to obtain a green result. Recorded
build/compiler/capture-assumption failures before hardware execution remain separate from SDK defects.

## Existing suite and restoration boundary

The canonical existing unattended batch passed 83 single-key methods with three multi-key skips.
A later dedicated two-key run passed those three methods with zero skips: **86 of 89 distinct existing
methods passed**, with three physical-lane methods deferred (read-only presence prerequisite/printer,
touch signing and hotplug). The earlier skips remain historical fixture outcomes. Multi-key liveness,
nonempty signatures and timing assertions retain their actual scope; they do not establish independent
cross-key cryptographic binding. These counts exclude the separately reported supplemental cases.

All six supplemental `vendor-after.json` receipts record independent reset, factory PIN/AES-192
authentication and after-reset/after-auth readbacks with zero baseline differences. Independently
compared `hardware/final-103.json` and `hardware/final-125.json` to their corresponding baseline
objects: every captured stable field matches, excluding only top-level `utc` and `mode`; each has
25 slot observations and its distinct preserved F9 identity. This agrees with
`hardware/final-reconciliation.json`. Original tracked shared allowlist bytes also match the preserved
original exactly.

**Protected PRINTED bytes were deliberately excluded.** Final snapshots explicitly say PRINTED was
never requested; public ADMIN flags supply only their recorded software-state view. Captured-state
equality therefore supplies no separately observed byte-absence claim for protected PRINTED. Factory
authentication, public-state equality and F9 preservation are separate evidence dimensions; private
key export or a new attestation manufacturer-trust decision is not implied.

## Disposition

Keep accepted C003/C004/C006/C009/C010 claims and existing PIVIMP-005/PIVIMP-008/PIVCORE dispositions.
The successful supplements add bounded oracles without fixing or expanding the unchanged shipping
tests. F9 preservation and raw-result ownership qualifications remain appropriate. No old PR745 bug
is reintroduced and no Pass-1 runtime fix is included.

Apply these outcomes only to the recorded Linux USB SmartCard setup and development 5.8 devices
(primary 103 beta; secondary 125 alpha for the two-key work; raw PIV version remains 00 00 01).
They add no older-release, Bio, NFC/FIPS-profile, other-OS, Native AOT, physical-touch/cache/hotplug or
native-cancellation qualification. Current results supersede the earlier “pending supplemental”
checkpoint for status reconciliation; historical review receipts remain historical.

Only this file was authored. No product edits, builds, tests, hardware commands, formatting, agents
or commits were run here. Final unit/example compilation and the coordinator's full evidence/release
gates remain pending separately; this content-impact PASS does not stand in for them.
