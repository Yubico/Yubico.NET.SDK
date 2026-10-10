# Audit v2.0 pre-release: study guide

This folder explains every finding from the internal pre-release security audit of the .NET SDK v2 (`yubikit`). For each finding it covers what the audit claimed, what we found to be right and wrong, why, and what we propose to fix. Every claim is backed by a spec quote, the canonical Python implementation, a repro test or a hardware result.

Nothing in this branch fixes anything yet. The repro tests in `src/*/tests/*/AuditV2/` are written to **fail** until the matching bug is fixed.

## How to use this guide

1. Read [method.md](method.md) once: baselines, how verification was done, and what the verdict and root-cause labels mean.
2. Work through the findings, starting with the reading order below. Each page has the same sections: what the audit says, what is right, what is wrong, why it matters, specification (with a CTAP 2.1/2.2/2.3 or WebAuthn L2/L3 version table), canonical Python reference, reproduction, proposed fix, and how to check it yourself.
3. Record your answers in [decisions.md](decisions.md). It lists the 17 decisions that block the fix work, new observations found while writing this guide, and the open questions.
4. [python-divergences.md](python-divergences.md) lists where python-fido2 or yubikey-manager appears to share an issue. Nothing has been reported upstream.
5. [references.md](references.md) lists every spec, manual and sibling SDK, with versions and pinned commits.

Where a finding page and the older [verification-report.md](verification-report.md) disagree, **the finding page is newer and wins**. Both were checked by reviewers from a different model vendor, but the pages had a second round of review.

## Suggested reading order

Start with what we consider release blockers for 2.0:

1. [#2](findings/02-required-uv-sends-uv-and-pinuvauthparam.md) WebAuthn registration with required UV and a PIN always fails on PIN-only YubiKeys.
2. [#13](findings/13-largeblob-private-entry-format.md) largeBlob uses a private format; the SDK cannot write on a key that holds spec-format data.
3. [#24](findings/24-ed25519-message-altered.md) Ed25519 PIV signatures are made over an altered message.
4. [26c](findings/26c-previewsign-attestation-fallback.md) previewSign can hand back the authentication key as if it were the signing key.
5. [#1](findings/01-prf-not-translated-to-hmac-secret.md), [26a](findings/26a-prf-credential-specific-inputs-ignored.md), [26b](findings/26b-prf-enabled-ignores-value.md) PRF does not work against spec-conforming authenticators.
6. [#22](findings/22-piv-pin-state-shared-across-processes.md) another local process can use a verified PIV PIN (needs a policy decision).
7. [#23](findings/23-short-scp-response-crashes-process.md) a one-byte response on a secure channel terminates the process; with [N2](findings/N2-scp-init-slicing-and-bare-9000.md), a bare `9000` is accepted without a response MAC.
8. [26m](findings/26m-securitydomain-empty-allowlist.md), [26e](findings/26e-piv-retry-limit-resets-pin-puk.md), [26f](findings/26f-piv-prepare-data-alters-input.md), [26i](findings/26i-openpgp-import-target-mismatch.md), [#28](findings/28-oath-period-zero-breaks-batch.md).
9. The WebAuthn request-shaping group: [#6](findings/06-attestation-none-forwards-statement.md), [#7](findings/07-preferred-resident-key-not-requested.md), [#8](findings/08-toporigin-without-crossorigin.md), [#9](findings/09-ceremony-timeout-ignored.md), [#10](findings/10-makecredential-up-false.md).

Then the rest in table order.

## All findings

Severities: HIGH, MED, LOW, INFO. "Ours" is our rating after verification. Fix groups: **A** no-brainer (spec or Python makes the fix obvious), **B** small API decision, **C** real design decision.

| Item | Finding | Jira | Audit | Our verdict | Ours | Root cause | Fix group |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [1](findings/01-prf-not-translated-to-hmac-secret.md) | WebAuthn PRF is not translated to CTAP hmac-secret | YESDK-1609 | MED | Confirmed, with a correction | MED | SDK only | C |
| [2](findings/02-required-uv-sends-uv-and-pinuvauthparam.md) | Required UV sends uv and pinUvAuthParam | YESDK-1610 | MED | Confirmed, with a correction | HIGH | SDK only | A |
| [3](findings/03-direct-largeblob-write-shape.md) | Direct CTAP largeBlob write has an invalid shape | YESDK-1611 | MED | Confirmed, with a correction | LOW (YubiKey) / MED | SDK only | C |
| [4](findings/04-getassertion-unsigned-extension-outputs.md) | Unsigned getAssertion extension outputs are not decoded | YESDK-1612 | MED | Confirmed, with a correction | LOW (YubiKey) / MED | SDK and Python share it | A |
| [5](findings/05-largeblob-supported-misreported.md) | WebAuthn largeBlob supported result ignores largeBlobKey | YESDK-1613 | MED | Confirmed | MED | SDK only | A |
| [6](findings/06-attestation-none-forwards-statement.md) | Attestation none forwards the authenticator statement | YESDK-1614 | MED | Confirmed | MED | SDK and Python share it | A |
| [7](findings/07-preferred-resident-key-not-requested.md) | Preferred resident key is not requested | YESDK-1615 | MED | Confirmed, with a correction | MED | SDK only | A |
| [8](findings/08-toporigin-without-crossorigin.md) | topOrigin sent with crossOrigin false | YESDK-1616 | MED | Confirmed | MED | SDK only | B |
| [9](findings/09-ceremony-timeout-ignored.md) | Ceremony timeout is ignored | YESDK-1617 | MED | Confirmed | MED | SDK only | C |
| [10](findings/10-makecredential-up-false.md) | makeCredential sends up false | YESDK-1618 | MED | Confirmed | MED | SDK only | A |
| [11](findings/11-credblob-wrong-cbor-type.md) | credBlob retrieval uses the wrong CBOR type | YESDK-1619 | MED | Confirmed | MED | SDK only | B |
| [12](findings/12-largeblob-max-fragment-length.md) | The largeBlob fragment length is fixed at 1024 bytes | YESDK-1620 | MED | Confirmed | MED | SDK only | A |
| [13](findings/13-largeblob-private-entry-format.md) | Large-blob entries use a private, incompatible format | YESDK-1621 | MED | Confirmed | HIGH | SDK only | C |
| [14](findings/14-largeblob-corrupt-checksum.md) | Corrupt large-blob checksum throws instead of resetting | YESDK-1622 | MED | Confirmed | MED | SDK only | A |
| [16](findings/16-largeblob-plaintext-retention.md) | Large-blob plaintext is not cleared from memory | YESDK-1624 | MED | Confirmed, with a correction | LOW-MED | SDK only | A |
| [17](findings/17-private-key-intermediate-copies.md) | Private-key intermediate copies | YESDK-1625 | MED | Partial | LOW | .NET runtime | A |
| [19](findings/19-docs-workflow-unpinned-installer.md) | Documentation workflow runs an unpinned installer with a write token | YESDK-1627 | MED | Confirmed, with a correction | MED | CI configuration | A |
| [22](findings/22-piv-pin-state-shared-across-processes.md) | PIV PIN state is shared across connections and processes | YESDK-1630 | MED | Confirmed, with a correction | MED | Device behaviour + SDK release policy | C |
| [23](findings/23-short-scp-response-crashes-process.md) | Short SCP response crashes the host process | YESDK-1631 | MED | Confirmed | MED | SDK only | B |
| [24](findings/24-ed25519-message-altered.md) | Ed25519 PIV signing alters the caller's message | YESDK-1632 | MED/HIGH | Confirmed | HIGH | SDK only | A |
| [25a](findings/25a-cli-otp-length-unchecked.md) | CLI static password length unchecked | YESDK-1633 | MED | Confirmed | LOW | SDK only | A |
| [25b](findings/25b-deviceinfo-pagination-loop.md) | Device-info pagination never terminates | YESDK-1633 | MED | Confirmed | LOW-MED | SDK and Python share it | A |
| [25c](findings/25c-getint32-wide-range.md) | RandomNumberGenerator.GetInt32 wrapper returns a constant for wide ranges | YESDK-1633 | MED | Confirmed | LOW | Replace with runtime API | A |
| [25d](findings/25d-hkdf-counter-overflow.md) | HKDF block counter wraps at the maximum output length | YESDK-1633 | MED | Confirmed, with a correction | INFO-LOW | Replace with runtime API | A |
| [25e](findings/25e-tlv-long-form-length.md) | BER-TLV long-form length decoding, with #20 | YESDK-1633 (#20: YESDK-1628) | MED / LOW | Confirmed, with a correction | LOW | SDK only | A |
| [25f](findings/25f-tlv-encode-dictionary-underallocates.md) | TlvHelper.EncodeDictionary underallocates its output buffer | YESDK-1633 | MED | Confirmed | LOW | SDK only | A |
| [25g](findings/25g-nonpositive-webauthn-batch-size.md) | Nonpositive WebAuthn batch size | YESDK-1633 | MED | Confirmed, with a correction | LOW-MED | SDK and Python share it | A |
| [25h/26q](findings/25h-26q-openpgp-kdf-count.md) | OpenPGP KDF iteration count handling | YESDK-1633, YESDK-1634 | MED | Confirmed | MED | SDK and Python share it | C |
| [26a](findings/26a-prf-credential-specific-inputs-ignored.md) | Credential-specific PRF inputs are ignored | YESDK-1634 | MED | Confirmed | MED | SDK only | C |
| [26b](findings/26b-prf-enabled-ignores-value.md) | PRF enabled ignores the returned value | YESDK-1634 | MED | Confirmed, with a correction | MED | SDK only | A |
| [26c](findings/26c-previewsign-attestation-fallback.md) | Missing previewSign attestation substitutes the authentication key | YESDK-1634 | MED | Confirmed | HIGH | SDK only | A |
| [26d](findings/26d-previewsign-first-entry.md) | FIDO previewSign chooses the first credential's parameters | YESDK-1634 | MED | Confirmed | MED | SDK only | B |
| [26e](findings/26e-piv-retry-limit-resets-pin-puk.md) | PIV retry-limit change resets the PIN and PUK | YESDK-1634 | MED | Confirmed | HIGH | Device behaviour | A |
| [26f](findings/26f-piv-prepare-data-alters-input.md) | PIV input formatting alters short EC digests and truncates oversized RSA blocks | YESDK-1634 | MED | Confirmed | MED | SDK only | B |
| [26g](findings/26g-credential-reader-redirected-input.md) | Credential reader accepts redirected input that violates its policy | YESDK-1634 | MED | Confirmed | LOW-MED | SDK only | B |
| [26h](findings/26h-credential-reader-confirmation-leak.md) | Credential reader confirmation strands the first credential buffer | YESDK-1634 | MED | Confirmed | LOW | SDK only | A |
| [26i](findings/26i-openpgp-import-target-mismatch.md) | OpenPGP import writes to the template slot, not the requested slot | YESDK-1634 | MED | Confirmed | MED | SDK only | B |
| [26j](findings/26j-openpgp-attestation-overwrites-certificate.md) | OpenPGP attestation overwrites the cardholder certificate | YESDK-1634 | MED | By design | MED | Device behaviour | A |
| [26k](findings/26k-oath-device-id-ignored.md) | OATH operations ignore the credential's device identity | YESDK-1634 | MED | Confirmed | MED | SDK only | B |
| [26l](findings/26l-otp-update-drops-flags.md) | OTP update configuration drops AllowUpdate and ProtectSlot2 | YESDK-1634 | MED | Confirmed, with a correction | MED / LOW | SDK only | B |
| [26m](findings/26m-securitydomain-empty-allowlist.md) | Security Domain empty allowlist removes the restriction | YESDK-1634 | MED | Confirmed, with a correction | HIGH | SDK and Python share it | A |
| [26n](findings/26n-management-builder-reuse.md) | Management builder reuse changes an already-built configuration | YESDK-1634 | MED | Confirmed | MED | SDK only | A |
| [26o](findings/26o-oath-retry-zero-seed-not-reproduced.md) | OATH authentication retry zero seed is not reproduced | YESDK-1634 | MED | Not reproduced | None | None | None |
| [26p](findings/26p-yubiotp-hmac-challenge-padding.md) | YubiOTP HMAC challenge padding is ambiguous | YESDK-1634 | MED | Confirmed, with a correction | LOW-MED | SDK and Python share it | C |
| [27](findings/27-mismatched-key-components-accepted.md) | Mismatched public and private key components are accepted | YESDK-1635 | LOW/MED | Confirmed | MED | SDK only | C |
| [28/25i](findings/28-oath-period-zero-breaks-batch.md) | OATH period zero breaks batch calculation | YESDK-1636 | MED/LOW | Confirmed | MED | SDK and Python share it | A |
| [N1](findings/N1-duplicate-pubkeycredparams.md) | Duplicate pubKeyCredParams entries accepted | – | – | Confirmed | LOW | SDK and Python share it | A |
| [N2](findings/N2-scp-init-slicing-and-bare-9000.md) | SCP initialization slicing and bare 9000 response | – | – | Confirmed | LOW (slicing), MED (bare 9000) | SDK only; bare 9000 shared with Python | A (slicing), C (bare 9000) |
| [N3](findings/N3-cli-static-password-and-confirmation.md) | CLI static password encoding and redirected-input confirmation | – | – | Confirmed | LOW | SDK only | A |
| [N4](findings/N4-ci-lockless-installs-and-credential-colocation.md) | CI lockless installs and credential co-location | – | – | Confirmed | LOW-MED | CI configuration | A |

Not verified (LOW only in the audit): #15, #18, #21, and the audit's "items to consider". #20 is covered under [25e](findings/25e-tlv-long-form-length.md).

## Contents of this folder

| Path | What it is |
| --- | --- |
| [original-audit.md](original-audit.md) | The audit, unchanged except that the reviewer name is removed. |
| [verification-report.md](verification-report.md) | Our first consolidated verification and remediation report, with review corrections. |
| [findings/](findings/) | One page per finding (this guide). |
| [decisions.md](decisions.md) | Decisions and open questions for the maintainer. |
| [method.md](method.md) | Baselines, method, labels, known limits, and how fixes will be done. |
| [python-divergences.md](python-divergences.md) | Where the Python reference appears to share an issue. |
| [references.md](references.md) | Specs, manuals and SDKs with versions. |
| [evidence/agent-reports/](evidence/agent-reports/) | The per-area verification reports, and the brief the verification agents followed. |
| [evidence/hardware-results.md](evidence/hardware-results.md) | What ran on real YubiKeys, and the results. |
| [evidence/scripts/](evidence/scripts/) | The two-process PIV repro (#22), the python-fido2 large-blob interop step (#13), and the CI patch sketch (#19). |

## Running the repro tests

```bash
# Unit repros; every failure is an intended repro
dotnet toolchain.cs -- test --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"

# No-touch hardware repros (reset PIV/OATH on allow-listed keys)
dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests --filter "FullyQualifiedName~AuditV2"
dotnet toolchain.cs -- test --integration --project Oath --filter "FullyQualifiedName~AuditV2"
```

Each finding page has its own "Check it yourself" commands.

Abbreviations: CTAP is Client to Authenticator Protocol; UV is user verification; PRF is the WebAuthn pseudo-random function extension; SCP is Secure Channel Protocol; MAC is message authentication code; PIV is Personal Identity Verification; OATH is the one-time-password applet; RP is relying party.
