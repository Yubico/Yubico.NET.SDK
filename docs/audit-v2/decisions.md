# Decisions needed

These are the choices that block the fix work. Each has a recommendation; the linked finding page has the evidence and the options that were rejected.

Fixes will be based on current `yubikit` (`df1ec06d`), which already contains `yubikit-async-boundaries` (merged in PR #685). Two areas overlap with that work and are called out on their pages: the smart-card transport (#22) and the SCP recovery-required mechanism (#23, N2).

Breaking changes are acceptable for 2.0, as long as they follow the house rules (root and module `CLAUDE.md`) and match how sibling applet modules handle the same kind of case.

## Recommendations

| # | Item | Recommendation | Page |
| --- | --- | --- | --- |
| 1 | PIV PIN state shared across processes | Open decision; see the options below and on the page. | [#22](findings/22-piv-pin-state-shared-across-processes.md) |
| 2 | largeBlob format | No compatibility shim for the SDK's old private format. On write, drop entries that aren't spec maps; keep spec entries we can't decrypt byte for byte. | [#13](findings/13-largeblob-private-entry-format.md) |
| 3 | makeCredential `up=false` | Reject with `ArgumentException` before anything is sent. Keep getAssertion `up=false` (exclude-list preflight needs it). | [#10](findings/10-makecredential-up-false.md) |
| 4 | Low-level PRF | Remove `ExtensionBuilder.WithPrf` (it encodes a CTAP key no spec defines). PRF stays at the WebAuthn layer, hmac-secret at the CTAP layer, like python-fido2. | [#1](findings/01-prf-not-translated-to-hmac-secret.md) |
| 5 | PRF `evalByCredential` | With several allowed credentials and a non-empty `evalByCredential`, reject (`NotSupportedException`). With exactly one allowed credential, use its override, otherwise `eval`. Never silently use the first entry. | [26a](findings/26a-prf-credential-specific-inputs-ignored.md) |
| 6 | Ed25519 maximum message length | Measure per firmware on hardware, document it, reject longer messages. Never truncate or pad. | [#24](findings/24-ed25519-message-altered.md) |
| 7 | PIV retry limits reset PIN and PUK | Document only, with a prominent XML doc warning, like ykman's docstring. No acknowledgement parameter. | [26e](findings/26e-piv-retry-limit-resets-pin-puk.md) |
| 8 | Short EC digest, oversized RSA input | Left-pad a short EC digest (keeps its integer value). Reject RSA input longer than the modulus. | [26f](findings/26f-piv-prepare-data-alters-input.md) |
| 9 | Security Domain empty allowlist | `StoreAllowListAsync` rejects an empty list; `ClearAllowListAsync` stays the explicit way to remove it. Document that GlobalPlatform treats an empty list as "accept all". | [26m](findings/26m-securitydomain-empty-allowlist.md) |
| 10 | Malformed OATH period | Omit the credential from `CalculateAllAsync` and log it; reject period ≤ 0 on create and import. | [#28](findings/28-oath-period-zero-breaks-batch.md) |
| 11 | OpenPGP KDF count | Read the count as unsigned; always hash at least one full salt + PIN input (RFC 9580 §3.7.1.3). Document that cards set up with short counts derive a different value. | [25h/26q](findings/25h-26q-openpgp-kdf-count.md) |
| 12 | Non-positive `maxCredentialCountInList` | Reject when decoding GetInfo, and guard the preflight loop. | [25g](findings/25g-nonpositive-webauthn-batch-size.md) |
| 13 | Leftover key copy in .NET | Report upstream to dotnet/runtime and document the residual. No SDK workaround. | [#17](findings/17-private-key-intermediate-copies.md) |
| 14 | Pinned Claude binary digest | Needs an owner who reviews digest updates. The rest of the CI fix (read-only generator jobs, `persist-credentials: false`) doesn't depend on it. | [#19](findings/19-docs-workflow-unpinned-installer.md) |
| 15 | SCP bare `9000` without R-MAC | Now in scope, MED. An on-path party can turn an error into a bare `9000` that the SDK accepts as authenticated success. Measure what YubiKeys send first, then require an R-MAC on success status words when R-MAC is in effect (exempting EXTERNAL AUTHENTICATE and error status words). ykman, Android and Swift all skip the check, so this is a deliberate divergence from the Python reference. | [N2](findings/N2-scp-init-slicing-and-bare-9000.md) |
| 16 | Which WebAuthn level the SDK targets | Decide Level 3 (what the report follows) or Level 2 (what `src/WebAuthn/README.md` says). It changes #6 (Level 2 zeroes the AAGUID for `none`; Level 3 keeps it) and decides whether PRF and `topOrigin` (Level 3 only) are in scope. Recommendation: Level 3, and fix the README. | [#6](findings/06-attestation-none-forwards-statement.md) |
| 17 | Timeout and cancellation after success | A deadline or cancel must never turn a completed authenticator success into `OperationCanceledException`; otherwise a created credential is lost to the caller. Make this a rule of the #9 design. | [#9](findings/09-ceremony-timeout-ignored.md) |

## New observations from writing this guide

These were found while writing and reviewing the pages. They are not in the original audit. Unless noted, they come from reading the code and were not run on hardware.

| Observation | Where |
| --- | --- |
| `ProtectSlot2` uses bit `0x40` instead of `0x80` (confirmed against Yubico's `ykdef.h` and yubikey-manager). Turning protection off also clears the challenge-response or HOTP mode bit. | [26l](findings/26l-otp-update-drops-flags.md) |
| `WithHmacSecretMakeCredential()` sends `hmac-secret-mc` without `hmac-secret`, which CTAP 2.3 §12.8 forbids. | [#1](findings/01-prf-not-translated-to-hmac-secret.md) |
| `SetPinAttemptsAsync` needs a verified PIN on the device, but the SDK only checks for management-key authentication. | [26e](findings/26e-piv-retry-limit-resets-pin-puk.md) |
| PIV management-key authentication may be card-wide state like the PIN (NIST SP 800-73-5 §2.4.2), which would widen #22. | [#22](findings/22-piv-pin-state-shared-across-processes.md) |
| A cancel during a keep-alive can discard a completed registration. | [#9](findings/09-ceremony-timeout-ignored.md) |
| SCP11c allowlist updates should carry counter tag `92`; the SDK never sends it. | [26m](findings/26m-securitydomain-empty-allowlist.md) |
| An empty YubiOTP HMAC challenge is padded with `0x01`; yubikey-manager uses `0x00`. | [26p](findings/26p-yubiotp-hmac-challenge-padding.md) |
| An overflowing or non-ASCII period prefix also aborts the OATH batch. | [#28](findings/28-oath-period-zero-breaks-batch.md) |
| A zero `maxCredentialCountInList` can make exclude-list preflight miss a discoverable credential, so a duplicate can be created. | [25g](findings/25g-nonpositive-webauthn-batch-size.md) |
| The previewSign fallback commit says it follows Swift; Swift throws instead. | [26c](findings/26c-previewsign-attestation-fallback.md) |

## Option detail for #22

The canonical Python stack (ykman on pyscard) opens the card exclusively, falls back to shared, and powers the card off on disconnect. The .NET SDK opens it shared and leaves the card as it is. Hardware shows an attacker with a shared handle can sign during the victim's session, not only after it.

| Option | What it does | Cost |
| --- | --- | --- |
| a. Document only | Explain that PIN=ONCE state belongs to the card, not the session. | Leaves the bypass in place. |
| b. Opt-in isolation policy | From VERIFY until the last private-key operation, hold the card exclusively or inside a transaction, then release with `RESET_CARD`/`UNPOWER_CARD`. | Apps must opt in. Needs testing on macOS, Windows and Linux. |
| c. Follow Python by default | Exclusive with shared fallback, and power off on close. | Breaking: other smart-card users (gpg-agent, scdaemon, browsers) can't use the card while the SDK holds it. |

Recommendation so far: (b), with (c) as the alternative if matching the Python stack's default matters more than coexistence.

## Questions for the maintainer

1. Approve or edit the 15 recommendations above, and pick an option for #22.
2. Who owns the pinned binary digest for #19?
3. For the group C design items, do you want to see each design round, or only the final one-pager?
4. For group B items, may the architect and the reviewer settle them on their own and only escalate when they disagree?

Fix groups used throughout the guide:

- **A, no-brainer.** The spec or the Python reference makes the fix obvious. Straight to a failing test and the fix.
- **B, small API decision.** One short design round, then the fix.
- **C, real design decision.** A full back-and-forth between an architect and a reviewer from another model vendor, then a one-page summary for the maintainer.
