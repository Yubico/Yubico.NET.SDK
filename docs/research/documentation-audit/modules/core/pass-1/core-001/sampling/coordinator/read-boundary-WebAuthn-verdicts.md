# Astra WebAuthn boundary sample verdicts

Read all 19 complete pinned units in `read-boundary-WebAuthn.md`. Final mapping and clean-credit
reconciliation remain pending. No ceremony, cryptographic operation or device test was executed.

## Consequential PRF citation

Read the PRF section directly from the dated W3C WebAuthn Level 3 Recommendation URL
`https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension` (HTTP 200).
Section 10.1.4 says that, when implemented over hmac-secret, the exposed single PRF **MUST** be the
one used when user verification is performed, overriding UserVerificationRequirement if necessary.
This is the client extension's requirement, conditional on that implementation, not a blanket
assertion about every authenticator. The SDK limitation and Required mitigation in the sampled guide
accurately preserve the missing enforcement and separate result-lifetime issue.

| Unit suffix | Verdict and scope |
| --- | --- |
| `b4125585d8579c68` | Supported: substantive registration-response summary, despite preceding imports/license. |
| `7d926f1edba94468` | Supported: substantive credProtect translation summary. |
| `06d5faa281acff8e` | Supported: client options are validated before the underlying CTAP2 factory; a failed client construction disposes the newly opened session. Transport/version policy is delegated, not retried here. |
| `ec91cffcba17a31e` | Supported: project dependency, test-proxy access and AOT opt-in comments match declarations; this is not an AOT runtime proof. |
| `c8377e0a58dac11c` | Administrative maintenance policy: read and retained, but no substantive clean contract credit. Replace for quota if needed. |
| `4886f819fa316d28` | Supported: source has no logger calls; guidance explicitly separates underlying/application logging and sensitive-data policy. |
| `546f13cbc632a4a1` | Supported within the stated source limits: cooperative cancellation, late-success race, lower-level exceptions and no transport fallback remain distinct. |
| `bf53e9ad2aa19caf` | Supported with the exact PRF citation above; no full conformance or erasure qualification. |
| `a78b316e99da06d2` | Supported: fake request captures `rk:true` on the advertised-capable path; no actual credential storage proof. |
| `7fe08c46f10afee5` | Ineligible preamble: license/imports/namespace only; zero clean credit. |
| `e29522f04d40349a` | **SAMPLE-WA-001 documentation miss:** assertions compare CredentialId/Signature bytes and one backend call, not instance identity or all response fields. Focused patch `d70a50aa…` passed review and remains queued until compilation releases the primary tree. Zero clean credit. |
| `2f2513b5ffd4c56b` | Supported: named null-prompt fixture declaration; no stronger behavior claimed by this unit. |
| `1b932c40537768a9` | Supported: explicit key-count and ordered-key assertions validate the illustrated CTAP2 CBOR ordering. |
| `651df80c5416339e` | Supported: the exact storage-write failure yields Written=false; commentary excludes read failures and cancellation. |
| `35e46ef0a6a3636d` | Supported within the preserved previewSign draft-v4 scope: nested generated-key fields are checked, not attestation trust or signature verification. |
| `4e15fa5e2d4d3425` | Supported: substantive processor test overview retains the experimental draft and selected-credential actor. |
| `8705722ec03b98c1` | Supported: non-concrete transport values throw before the fake records a connection request. |
| `dd7a8c8d3beb3ad5` | Supported: selection-probe fixture declaration. |
| `c6719030f9881ef4` | Ineligible preamble: license/imports/namespace only; zero clean credit. |

All IDs have the prefix `core-001:unit:`. Expand the test/helper category after SAMPLE-WA-001;
keep eligibility replacements separate from that material-miss expansion.
