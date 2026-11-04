# Independent original-citation review

GPT-6.1 Sol, 2026-11-04. This reviewer fetched originals independently of the investigator/editor.
`review-source-manifest.json` and `review-source-followup-manifest.json` bind URLs, access date, bytes and
SHA-256 identities to the retained original responses. Initial verification plus one focused follow-up
covered the consequential clauses and pinned-file/history identities; broad specification research stopped
at the recorded GAP-SPEC-1. Context7 supplied current library guidance; executable originals settle the
vendor-parser questions below.

## Manual clauses and applicability

| Source | Title, revision/status and actor | Independently read passage and supported application |
| --- | --- | --- |
| S1 | **YubiKey Technical Manual — YubiHSM Auth Specifics**; living vendor page, copyright 2026, no numbered revision exposed; applet/HSM, host libraries and provisioning operator | “The identical long-lived keyset is protected in the YubiHSM 2 and in the YubiKey YubiHSM Auth application.” The secure-channel/architecture sections support matching symmetric enrollment. Flowchart steps 3–4 name the host and HSM challenge generators; steps 10–11 retain the later host response. The shell section separately defines HSM `authkey`, YubiKey `label` and credential `password`. Opening enablement conditions support requiring an enabled applet. |
| S2 | **YubiHSM Auth overview**; living **v1** SDK manual, Edit/docurl targets `develop`, no numbered revision exposed; applet behavior | Management key: “Supplying the correct management key before the key is blocked will reset the retry counter to 8.” Credential: “Supplying the correct password before the credential is deleted will reset the retry counter to 8.” Adjacent passages explicitly distinguish management blocking/reset from permanent credential deletion. These support the source-qualified retry explanations. The older blanket immutable-credential sentence remains excluded from newer password-change claims. |
| S3 | **Interacting with a YubiHSM 2**; living v1 SDK user manual, no numbered revision exposed; host integrator | “Once credentials are added … use the YubiHSM SDK (and bundled tools) to establish a secure session … and perform operations on it.” The secure-channel section requires identical long-lived keys and distinguishes this HSM protocol from complete SCP03. This supports external HSM-library responsibilities, without inventing a v2 connector API. |

S1–S3 provide descriptive vendor product guidance. Their operational wording supports the scoped tasks;
it supplies no versioned normative YubiKey CALCULATE-response clause. S1 primarily explains symmetric
AES authentication. Its general “each credential” language supplies no asymmetric receipt contract.

## Pinned implementation passages

- **S4**, YubiKey Manager `yubikit/hsmauth.py`, revision
  `296cb587ba15f57672356d8445393a96b41803fb`: GitHub original commit metadata confirms
  2025-09-23, “HSMAUTH: add change credential password.” The complete independently fetched file hashes
  to `e3573d5580928b84818939b6eb1ae0083c7d01793589746eace80db32fe2cab3`, matching the investigator's
  pinned bytes and the separately fetched `main` file. Read `SessionKeys.parse` (203–220), the complete
  calculation/helper group (596–693), GET_CHALLENGE (695–718), and the focused password-change/KDF passages.
  The host encoder orders EPK-OCE then EPK-SD, identifies the input public key as the HSM's, sends a
  nonempty supplied cryptogram, and includes a challenge password at its 5.7.1/development condition.
  C# documentation describes the C# gate/error path; Python's condition supplies corroboration rather
  than an inherited C# runtime promise.
- **S5**, Python YubiHSM `yubihsm/core.py`, revision
  `055f85fcff0a1d3c0ec779dc28cb21e2762cef3c`: original metadata confirms 2025-06-19.
  Complete file SHA-256 `00257ee2e85f1dafde5d5978aecdac6ec23e42aa7d2ef464bdc54f90e6fbbb7f`.
  The hardware oracle's separately pinned repository revision
  `44a5ab52e0697c67b36c6dec953db08a6b7b183a` returns byte-identical `core.py`. Read constants/_derive
  (52–68), complete SymmetricAuth negotiation (425–520), and AsymmetricAuth (523–653).
  `_derive` uses `zero[11] || type || 00 || L[2 big-endian] || 01 || context`.
  S-ENC uses K-ENC/type04; S-MAC and S-RMAC use K-MAC/type06/type07. Symmetric peer verification derives
  card crypto from S-MAC/type00/64 bits, compares constant-time, then derives/sends type01 host crypto.
  Asymmetric negotiation obtains the HSM static public key separately from EPK-OCE, builds
  `EPK-OCE || EPK-HSM`, and verifies a complete CMAC receipt before returning keys.

Both sources are **host implementation corroboration**, with no normative conformance or live-HSM
qualification implied. Context7 `/yubico/yubikey-manager` described a “48-byte response”; the actual
S4 parser slices three regions and has no strict length check. The original source controls that fact.
Context7 `/dotnet/docs` also confirms the one-shot static PBKDF2 guidance; clearing assurances remain
scoped to the actual supplied/owned buffers inspected in local source.

## Cryptogram follow-up originals

1. Initial Python file at `31d39f64b73e534787382dab4675c0274616627b` matches SHA-256
   `cc294104b018add0035544b889bd049a12911ee1a0962fd668aa2748aadfed68`.
   Original commit metadata confirms 2023-07-13, “HSMAUTH: Add lib for the YubiHSM Auth application.”
   Lines 180–201 explicitly branch on `len(response) == 56` and call `response[48:]` **card_crypto**.
   Removal patch `47c801e2f4d4b64ac6931e39d962bdbee72c7553`, dated 2023-08-01, removes that field/branch
   while retaining the three slices. Current S4 likewise takes 0:16, 16:32, 32:48 and ignores a suffix.
2. Vendor C `ykhsmauth_calculate_ex`, tree `6195c1b687aab56308e9598a0d66941813c94ae5`, matches file
   SHA-256 `e34324f4716340f867ab112c1cc4e1ac11006e21d758a3f98dcb2b6220431b14`; its header matches
   `823212991500d2a17379d3639d284de5c1d11847d28488abaaefd8eb3b42ed17`.
   Read the complete method (561–632) and header lengths (97–110). It sends tag78 only when
   `card_crypto_len > 8`, then requires `recv_len == 3 * 16` before its “Ignore host crypto for now”
   comment. The comment follows an active rejection guard; it supplies no accepted 56-byte parser path.
3. Focused original 2021-10-06 patch `08937b47458a9e0f30ea94a16885d7a82fb4dabb` includes that same
   request suppression, strict 48 guard and host-crypto comment (captured patch lines 6770–6835).
   The implementation difference is longstanding. Its date establishes host-code history only.
4. Original 2025-04-22 patch `c910e5d25ba675ac2bfc97d35f14a4cc0314864f`, “Override version checks for
   0.0.1,” changes the hsmauth SELECT result through `_override_version.patch` (248–267).
   This corroborates keeping ykman's display separate from an SDK-effective version observation.
   The override implementation and exact mapping remain outside this review.

These independently matched originals support the public compatibility qualification and the narrower
history statements. They leave the physical response's suffix meaning and firmware boundary open.
The recorded device's future raw-region comparisons can qualify that device's bytes; firmware applicability
and actual HSM authentication require their own evidence. Cryptogram omission remains isolated KDF evidence,
with peer verification and handshake completion assigned explicitly to the application's HSM integration.
